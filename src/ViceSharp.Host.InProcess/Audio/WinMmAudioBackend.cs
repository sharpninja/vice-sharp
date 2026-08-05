using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ViceSharp.Abstractions;

namespace ViceSharp.Host.Audio;

/// <summary>
/// Windows real-time audio output backend built on the always-present
/// <c>winmm.dll</c> <c>waveOut</c> API via source-generated P/Invoke
/// (<see cref="LibraryImportAttribute"/>), so it is fully NativeAOT/trim safe
/// and needs no bundled native library. SID float samples are converted to
/// 16-bit mono PCM and written into one looping WaveOut ring buffer. Free
/// space is calculated from <c>waveOutGetPosition</c> against the write cursor,
/// matching VICE's WMM driver so audio back-pressure reflects samples Windows
/// has actually played rather than whether a submitted header has returned.
/// All native calls are guarded so a machine with no audio device degrades to
/// a silent no-op instead of throwing.
///
/// Blittable structs are read/written with <see cref="Unsafe"/> rather than
/// <see cref="Marshal"/> struct marshalling, which keeps the whole type clear
/// of <c>[RequiresDynamicCode]</c> APIs and therefore AOT-clean for the MSI.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed unsafe partial class WinMmAudioBackend : IAudioBackend, IDisposable
{
    private const int SampleRate = 44100;
    private const int BytesPerSample = 2;
    private const int FragmentSampleCount = 256;
    private const int BufferFragmentCount = 8;
    private const int BufferSampleCount = FragmentSampleCount * BufferFragmentCount;
    private const int BufferBytes = BufferSampleCount * BytesPerSample;
    private const int FragmentBytes = FragmentSampleCount * BytesPerSample;
    private const int QueueFullPollMilliseconds = 1;

    /// <summary>Live Windows audio waits for a buffer instead of dropping PCM when the device queue is full.</summary>
    internal const bool DropsSamplesWhenDeviceQueueFull = false;

    private const uint WaveMapper = 0xFFFFFFFF;
    private const uint CallbackNull = 0x0000_0000;
    private const uint WaveAllowSync = 0x0000_0002;
    private const uint WhdrDone = 0x0000_0001;
    private const uint WhdrBeginLoop = 0x0000_0004;
    private const uint WhdrEndLoop = 0x0000_0008;
    private const uint TimeBytes = 0x0000_0004;
    private const uint MmsyserrNoerror = 0;

    private static readonly int HeaderSize = Unsafe.SizeOf<WaveHdr>();

    private readonly object _lock = new();

    private IntPtr _handle;
    private IntPtr _headerPtr;
    private IntPtr _dataPtr;
    private bool _headerPrepared;
    private int _writeCursorBytes;
    private int _playCursorSubtractBytes;
    private bool _disabled;
    private bool _paused;
    private bool _disposed;

    public WinMmAudioBackend()
    {
        if (!OperatingSystem.IsWindows())
        {
            _disabled = true;
            return;
        }

        try
        {
            Initialize();
        }
        catch
        {
            // No audio device / driver: stay silent rather than failing the run.
            _disabled = true;
        }
    }

    private void Initialize()
    {
        var format = new WaveFormatEx
        {
            wFormatTag = 1, // WAVE_FORMAT_PCM
            nChannels = 1,
            nSamplesPerSec = SampleRate,
            wBitsPerSample = 16,
            nBlockAlign = 2, // channels * bitsPerSample / 8
            nAvgBytesPerSec = SampleRate * 2,
            cbSize = 0
        };

        var result = waveOutOpen(out _handle, WaveMapper, in format, IntPtr.Zero, IntPtr.Zero, CallbackNull | WaveAllowSync);
        if (result != MmsyserrNoerror)
        {
            _disabled = true;
            return;
        }

        _dataPtr = Marshal.AllocHGlobal(BufferBytes);
        _headerPtr = Marshal.AllocHGlobal(HeaderSize);
        new Span<byte>((void*)_dataPtr, BufferBytes).Clear();
        Unsafe.Write((void*)_headerPtr, default(WaveHdr));
        _writeCursorBytes = BufferBytes - FragmentBytes;
    }

    public void SubmitSamples(ReadOnlySpan<float> samples)
    {
        if (_disabled || samples.IsEmpty)
            return;

        while (!samples.IsEmpty)
        {
            var sampleCount = Math.Min(samples.Length, BufferSampleCount);
            if (!WaitAndSubmitBuffer(samples[..sampleCount]))
                return;

            samples = samples[sampleCount..];
        }
    }

    public int QueuedSampleCount
    {
        get
        {
            if (_disabled)
                return 0;

            lock (_lock)
            {
                return (BufferBytes - GetAvailableBytesLocked()) / BytesPerSample;
            }
        }
    }

    public int AvailableSampleCount
    {
        get
        {
            if (_disabled)
                return int.MaxValue;

            lock (_lock)
            {
                return GetAvailableBytesLocked() / BytesPerSample;
            }
        }
    }

    public void Pause()
    {
        if (_disabled)
            return;

        lock (_lock)
        {
            if (_disposed || _paused)
                return;
            _paused = true;
            waveOutPause(_handle);
        }
    }

    public void Resume()
    {
        if (_disabled)
            return;

        lock (_lock)
        {
            if (_disposed || !_paused)
                return;
            _paused = false;
            waveOutRestart(_handle);
        }
    }

    public void Stop()
    {
        if (_disabled)
            return;

        lock (_lock)
        {
            if (_disposed)
                return;
            waveOutReset(_handle);
            UnprepareHeaderLocked();
            new Span<byte>((void*)_dataPtr, BufferBytes).Clear();
            _writeCursorBytes = BufferBytes - FragmentBytes;
            _playCursorSubtractBytes = 0;
            _paused = false;
        }
    }

    private bool WaitAndSubmitBuffer(ReadOnlySpan<float> samples)
    {
        var requiredBytes = samples.Length * BytesPerSample;
        while (true)
        {
            lock (_lock)
            {
                if (_disabled || _disposed || _paused)
                    return false;

                if (!EnsureLoopingBufferLocked())
                    return false;

                if (GetAvailableBytesLocked() >= requiredBytes)
                {
                    WriteSamplesLocked(samples);
                    return true;
                }
            }

            Thread.Sleep(QueueFullPollMilliseconds);
        }
    }

    private bool EnsureLoopingBufferLocked()
    {
        var flags = _headerPrepared ? Unsafe.Read<WaveHdr>((void*)_headerPtr).dwFlags : WhdrDone;
        if (_headerPrepared && (flags & WhdrDone) == 0)
            return true;

        waveOutReset(_handle);
        UnprepareHeaderLocked();
        new Span<byte>((void*)_dataPtr, BufferBytes).Clear();
        _writeCursorBytes = BufferBytes - FragmentBytes;
        _playCursorSubtractBytes = 0;

        var header = new WaveHdr
        {
            lpData = _dataPtr,
            dwBufferLength = BufferBytes,
            dwFlags = WhdrBeginLoop | WhdrEndLoop,
            dwLoops = 0x7fff_ffff
        };
        Unsafe.Write((void*)_headerPtr, header);

        var size = (uint)HeaderSize;
        if (waveOutPrepareHeader(_handle, _headerPtr, size) != MmsyserrNoerror)
            return false;
        _headerPrepared = true;

        if (waveOutWrite(_handle, _headerPtr, size) != MmsyserrNoerror)
        {
            UnprepareHeaderLocked();
            return false;
        }

        return true;
    }

    private void WriteSamplesLocked(ReadOnlySpan<float> samples)
    {
        var firstSamples = Math.Min(samples.Length, (BufferBytes - _writeCursorBytes) / BytesPerSample);
        if (firstSamples > 0)
        {
            var firstDest = new Span<byte>((void*)IntPtr.Add(_dataPtr, _writeCursorBytes), firstSamples * BytesPerSample);
            AudioSampleConverter.ConvertToPcm16(samples[..firstSamples], firstDest, MasterAudioControl.EffectiveGain);
        }

        var remaining = samples[firstSamples..];
        if (!remaining.IsEmpty)
        {
            var secondDest = new Span<byte>((void*)_dataPtr, remaining.Length * BytesPerSample);
            AudioSampleConverter.ConvertToPcm16(remaining, secondDest, MasterAudioControl.EffectiveGain);
        }

        _writeCursorBytes = (_writeCursorBytes + samples.Length * BytesPerSample) % BufferBytes;
    }

    private int GetAvailableBytesLocked()
    {
        if (_disposed || _paused || _headerPtr == IntPtr.Zero)
            return 0;

        var flags = _headerPrepared ? Unsafe.Read<WaveHdr>((void*)_headerPtr).dwFlags : WhdrDone;
        if (!_headerPrepared || (flags & WhdrDone) != 0)
            return BufferBytes;

        return TryGetPlayCursorBytesLocked(out var playCursor)
            ? ComputeAvailableBytes(_writeCursorBytes, playCursor, BufferBytes)
            : 0;
    }

    private bool TryGetPlayCursorBytesLocked(out int playCursor)
    {
        var time = new MmTime { wType = TimeBytes };
        if (waveOutGetPosition(_handle, ref time, (uint)Unsafe.SizeOf<MmTime>()) != MmsyserrNoerror)
        {
            playCursor = 0;
            return false;
        }

        playCursor = NormalizePlayCursor(time.cb, ref _playCursorSubtractBytes, BufferBytes);
        return true;
    }

    internal static int NormalizePlayCursor(uint reportedBytes, ref int subtractBytes, int bufferBytes)
    {
        var cursor = (long)reportedBytes - subtractBytes;
        if (cursor >= bufferBytes)
        {
            subtractBytes += (int)(cursor / bufferBytes) * bufferBytes;
            cursor %= bufferBytes;
        }

        if (cursor < 0)
            cursor = 0;

        return (int)cursor;
    }

    internal static int ComputeAvailableBytes(int writeCursorBytes, int playCursorBytes, int bufferBytes)
    {
        var used = writeCursorBytes - playCursorBytes;
        if (used < 0)
            used += bufferBytes;

        return bufferBytes - used;
    }

    private void UnprepareHeaderLocked()
    {
        if (!_headerPrepared)
            return;

        waveOutUnprepareHeader(_handle, _headerPtr, (uint)HeaderSize);
        _headerPrepared = false;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;
            _disposed = true;

            if (!_disabled && _handle != IntPtr.Zero)
            {
                waveOutReset(_handle);
                UnprepareHeaderLocked();
                waveOutClose(_handle);
                _handle = IntPtr.Zero;
            }

            if (_headerPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_headerPtr);
                _headerPtr = IntPtr.Zero;
            }

            if (_dataPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_dataPtr);
                _dataPtr = IntPtr.Zero;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormatEx
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHdr
    {
        public IntPtr lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public IntPtr dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public IntPtr lpNext;
        public IntPtr reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MmTime
    {
        public uint wType;
        public uint cb;
        public uint reserved;
    }

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutOpen(out IntPtr phwo, uint deviceId, in WaveFormatEx format, IntPtr callback, IntPtr instance, uint flags);

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutPrepareHeader(IntPtr hwo, IntPtr header, uint size);

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutWrite(IntPtr hwo, IntPtr header, uint size);

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutGetPosition(IntPtr hwo, ref MmTime time, uint size);

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutUnprepareHeader(IntPtr hwo, IntPtr header, uint size);

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutReset(IntPtr hwo);

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutPause(IntPtr hwo);

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutRestart(IntPtr hwo);

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutClose(IntPtr hwo);
}
