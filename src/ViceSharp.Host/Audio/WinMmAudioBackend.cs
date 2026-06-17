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
/// 16-bit mono PCM and streamed through a small pool of double-buffered
/// <c>WAVEHDR</c> blocks; if the device queue is full (the emulator briefly
/// ran ahead of real time, e.g. in warp) the newest block is dropped rather
/// than allowed to accumulate unbounded latency. All native calls are guarded
/// so a machine with no audio device degrades to a silent no-op instead of
/// throwing.
///
/// Blittable structs are read/written with <see cref="Unsafe"/> rather than
/// <see cref="Marshal"/> struct marshalling, which keeps the whole type clear
/// of <c>[RequiresDynamicCode]</c> APIs and therefore AOT-clean for the MSI.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed unsafe partial class WinMmAudioBackend : IAudioBackend, IDisposable
{
    private const int SampleRate = 44100;
    private const int BufferCount = 8;
    // Largest submission we accept into a single WAVEHDR block. The SID flushes
    // in batches of 256 floats; 4096 samples (8 KiB) leaves generous headroom.
    private const int MaxSamplesPerBuffer = 4096;

    private const uint WaveMapper = 0xFFFFFFFF;
    private const uint CallbackNull = 0x0000_0000;
    private const uint WhdrDone = 0x0000_0001;
    private const uint MmsyserrNoerror = 0;

    private static readonly int HeaderSize = Unsafe.SizeOf<WaveHdr>();

    private readonly object _lock = new();

    private IntPtr _handle;
    private IntPtr[] _headerPtrs = Array.Empty<IntPtr>();
    private IntPtr[] _dataPtrs = Array.Empty<IntPtr>();
    private bool[] _inFlight = Array.Empty<bool>();
    private int[] _bufferSamples = Array.Empty<int>();
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

        var result = waveOutOpen(out _handle, WaveMapper, in format, IntPtr.Zero, IntPtr.Zero, CallbackNull);
        if (result != MmsyserrNoerror)
        {
            _disabled = true;
            return;
        }

        _headerPtrs = new IntPtr[BufferCount];
        _dataPtrs = new IntPtr[BufferCount];
        _inFlight = new bool[BufferCount];
        _bufferSamples = new int[BufferCount];

        for (var i = 0; i < BufferCount; i++)
        {
            _dataPtrs[i] = Marshal.AllocHGlobal(MaxSamplesPerBuffer * 2);
            _headerPtrs[i] = Marshal.AllocHGlobal(HeaderSize);
            Unsafe.Write((void*)_headerPtrs[i], default(WaveHdr));
        }
    }

    public void SubmitSamples(ReadOnlySpan<float> samples)
    {
        if (_disabled || samples.IsEmpty)
            return;

        lock (_lock)
        {
            if (_disabled || _disposed || _paused)
                return;

            RecycleCompletedBuffers();

            var slot = FindFreeSlot();
            if (slot < 0)
                return; // device queue full: drop rather than build latency

            var sampleCount = Math.Min(samples.Length, MaxSamplesPerBuffer);
            var bytes = sampleCount * 2;

            var dest = new Span<byte>((void*)_dataPtrs[slot], bytes);
            AudioSampleConverter.ConvertToPcm16(samples[..sampleCount], dest);

            var header = new WaveHdr
            {
                lpData = _dataPtrs[slot],
                dwBufferLength = (uint)bytes,
                dwFlags = 0
            };
            Unsafe.Write((void*)_headerPtrs[slot], header);

            var size = (uint)HeaderSize;
            if (waveOutPrepareHeader(_handle, _headerPtrs[slot], size) != MmsyserrNoerror)
                return;
            if (waveOutWrite(_handle, _headerPtrs[slot], size) != MmsyserrNoerror)
            {
                waveOutUnprepareHeader(_handle, _headerPtrs[slot], size);
                return;
            }

            _inFlight[slot] = true;
            _bufferSamples[slot] = sampleCount;
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
                RecycleCompletedBuffers();
                var total = 0;
                for (var i = 0; i < _inFlight.Length; i++)
                {
                    if (_inFlight[i])
                        total += _bufferSamples[i];
                }

                return total;
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
            RecycleCompletedBuffers();
            _paused = false;
        }
    }

    private int FindFreeSlot()
    {
        for (var i = 0; i < _inFlight.Length; i++)
        {
            if (!_inFlight[i])
                return i;
        }

        return -1;
    }

    private void RecycleCompletedBuffers()
    {
        var size = (uint)HeaderSize;
        for (var i = 0; i < _inFlight.Length; i++)
        {
            if (!_inFlight[i])
                continue;

            var flags = Unsafe.Read<WaveHdr>((void*)_headerPtrs[i]).dwFlags;
            if ((flags & WhdrDone) != 0)
            {
                waveOutUnprepareHeader(_handle, _headerPtrs[i], size);
                _inFlight[i] = false;
                _bufferSamples[i] = 0;
            }
        }
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
                var size = (uint)HeaderSize;
                for (var i = 0; i < _headerPtrs.Length; i++)
                {
                    if (_inFlight[i])
                        waveOutUnprepareHeader(_handle, _headerPtrs[i], size);
                }

                waveOutClose(_handle);
                _handle = IntPtr.Zero;
            }

            foreach (var ptr in _headerPtrs)
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }

            foreach (var ptr in _dataPtrs)
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }

            _headerPtrs = Array.Empty<IntPtr>();
            _dataPtrs = Array.Empty<IntPtr>();
            _inFlight = Array.Empty<bool>();
            _bufferSamples = Array.Empty<int>();
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

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutOpen(out IntPtr phwo, uint deviceId, in WaveFormatEx format, IntPtr callback, IntPtr instance, uint flags);

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutPrepareHeader(IntPtr hwo, IntPtr header, uint size);

    [LibraryImport("winmm.dll")]
    private static partial uint waveOutWrite(IntPtr hwo, IntPtr header, uint size);

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
