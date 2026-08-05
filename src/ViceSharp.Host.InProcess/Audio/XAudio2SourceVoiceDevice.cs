using System;
using System.Runtime.InteropServices;

namespace ViceSharp.Host.Audio;

/// <summary>
/// The real, AppContainer-safe XAudio2 source-voice device behind
/// <see cref="ISourceVoiceDevice"/> (PLAN-XBOXUWP S18, IMPL-XBOXUWP-018; FR-XAUDIO-003,
/// TR-XAUDIO-002). It uses ONLY <c>[LibraryImport("xaudio2_9.dll")]</c> for
/// <c>XAudio2Create</c> plus blittable <c>delegate* unmanaged[Stdcall]</c> COM vtable
/// calls - no winmm, no kernel32, no runtime-marshalled signatures - so the whole type
/// is Native-AOT / trim clean and needs no bundled native library (xaudio2_9.dll is part
/// of Windows 10+).
///
/// <para>COM objects here are raw <see cref="IntPtr"/> handles; each method is invoked by
/// reading the object's vtable (first pointer field) and calling the slot as a function
/// pointer whose first argument is the <c>this</c> pointer. Only the small set of slots
/// the backend needs is bound: <c>IXAudio2</c> CreateMasteringVoice (7), CreateSourceVoice
/// (5), Release (2); and <c>IXAudio2SourceVoice</c> Start (19), Stop (20),
/// SubmitSourceBuffer (21), FlushSourceBuffers (22), GetState (25), DestroyVoice (18).</para>
///
/// <para>The device owns a native PCM ring of <c>bufferFragmentCount</c> fixed-size slots;
/// <see cref="SubmitBuffer"/> copies a fragment into the next slot (round-robin, which is
/// the physical drop-oldest) and submits that slot to the source voice. Every native call
/// is guarded so a machine with no working device degrades to <see cref="Open"/> returning
/// <see langword="false"/> (the backend then becomes a silent no-op) rather than throwing.
/// Blittable structs are passed by pointer, keeping the type clear of
/// <c>[RequiresDynamicCode]</c> marshalling.</para>
/// </summary>
internal sealed unsafe partial class XAudio2SourceVoiceDevice : ISourceVoiceDevice
{
    // IXAudio2 vtable slots (inherits IUnknown).
    private const int SlotRelease = 2;
    private const int SlotCreateSourceVoice = 5;
    private const int SlotCreateMasteringVoice = 7;

    // IXAudio2SourceVoice vtable slots (IXAudio2Voice base + source-voice methods).
    private const int SlotDestroyVoice = 18;
    private const int SlotStart = 19;
    private const int SlotStop = 20;
    private const int SlotSubmitSourceBuffer = 21;
    private const int SlotFlushSourceBuffers = 22;
    private const int SlotGetState = 25;

    private IntPtr _xaudio2;      // IXAudio2*
    private IntPtr _masterVoice;  // IXAudio2MasteringVoice*
    private IntPtr _sourceVoice;  // IXAudio2SourceVoice*
    private IntPtr _ring;         // native PCM ring: bufferFragmentCount * fragmentBytes.
    private int _fragmentBytes;
    private int _capacity;
    private int _writeSlot;
    private bool _opened;
    private bool _disposed;

    /// <inheritdoc/>
    public bool Open(int sampleRate, int channels, int fragmentBytes, int bufferFragmentCount)
    {
        if (_disposed || _opened)
            return _opened;

        if (!OperatingSystem.IsWindows())
            return false;

        if (fragmentBytes <= 0 || bufferFragmentCount <= 0)
            return false;

        try
        {
            if (XAudio2Create(out _xaudio2, 0, 0) < 0 || _xaudio2 == IntPtr.Zero)
                return false;

            // IXAudio2::CreateMasteringVoice(ppMasteringVoice, channels, rate, flags,
            //   deviceId, effectChain, category) -> default endpoint (0/0 = defaults).
            var createMaster =
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, uint, uint, uint, char*, void*, int, int>)
                Slot(_xaudio2, SlotCreateMasteringVoice);
            IntPtr master;
            if (createMaster(_xaudio2, &master, 0, 0, 0, null, null, 0) < 0 || master == IntPtr.Zero)
                return false;
            _masterVoice = master;

            var format = new WaveFormatEx
            {
                wFormatTag = 1, // WAVE_FORMAT_PCM
                nChannels = (ushort)channels,
                nSamplesPerSec = (uint)sampleRate,
                wBitsPerSample = 16,
                nBlockAlign = (ushort)(channels * 2),
                nAvgBytesPerSec = (uint)(sampleRate * channels * 2),
                cbSize = 0,
            };

            // IXAudio2::CreateSourceVoice(ppSourceVoice, pSourceFormat, flags,
            //   maxFrequencyRatio, callback, sendList, effectChain).
            var createSource =
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, WaveFormatEx*, uint, float, void*, void*, void*, int>)
                Slot(_xaudio2, SlotCreateSourceVoice);
            IntPtr source;
            if (createSource(_xaudio2, &source, &format, 0, 1.0f, null, null, null) < 0 || source == IntPtr.Zero)
                return false;
            _sourceVoice = source;

            _fragmentBytes = fragmentBytes;
            _capacity = bufferFragmentCount;
            _ring = Marshal.AllocHGlobal(fragmentBytes * bufferFragmentCount);
            new Span<byte>((void*)_ring, fragmentBytes * bufferFragmentCount).Clear();
            _writeSlot = 0;
            _opened = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public void SubmitBuffer(ReadOnlySpan<byte> pcm)
    {
        if (!_opened || _disposed || pcm.IsEmpty)
            return;

        var bytes = Math.Min(pcm.Length, _fragmentBytes);
        var slot = (byte*)IntPtr.Add(_ring, _writeSlot * _fragmentBytes);
        pcm.Slice(0, bytes).CopyTo(new Span<byte>(slot, _fragmentBytes));

        var buffer = new XAudio2Buffer
        {
            Flags = 0,
            AudioBytes = (uint)bytes,
            pAudioData = (IntPtr)slot,
            PlayBegin = 0,
            PlayLength = 0,
            LoopBegin = 0,
            LoopLength = 0,
            LoopCount = 0,
            pContext = IntPtr.Zero,
        };

        var submit =
            (delegate* unmanaged[Stdcall]<IntPtr, XAudio2Buffer*, void*, int>)
            Slot(_sourceVoice, SlotSubmitSourceBuffer);
        submit(_sourceVoice, &buffer, null);

        _writeSlot = (_writeSlot + 1) % _capacity;
    }

    /// <inheritdoc/>
    public int BuffersQueued
    {
        get
        {
            if (!_opened || _disposed)
                return 0;

            XAudio2VoiceState state = default;
            var getState =
                (delegate* unmanaged[Stdcall]<IntPtr, XAudio2VoiceState*, uint, void>)
                Slot(_sourceVoice, SlotGetState);
            getState(_sourceVoice, &state, 0);
            return (int)state.BuffersQueued;
        }
    }

    /// <inheritdoc/>
    public void Start()
    {
        if (!_opened || _disposed)
            return;

        var start = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, int>)Slot(_sourceVoice, SlotStart);
        start(_sourceVoice, 0, 0);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!_opened || _disposed)
            return;

        var stop = (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, int>)Slot(_sourceVoice, SlotStop);
        stop(_sourceVoice, 0, 0);

        var flush = (delegate* unmanaged[Stdcall]<IntPtr, int>)Slot(_sourceVoice, SlotFlushSourceBuffers);
        flush(_sourceVoice);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_sourceVoice != IntPtr.Zero)
        {
            var destroy = (delegate* unmanaged[Stdcall]<IntPtr, void>)Slot(_sourceVoice, SlotDestroyVoice);
            destroy(_sourceVoice);
            _sourceVoice = IntPtr.Zero;
        }

        if (_masterVoice != IntPtr.Zero)
        {
            var destroy = (delegate* unmanaged[Stdcall]<IntPtr, void>)Slot(_masterVoice, SlotDestroyVoice);
            destroy(_masterVoice);
            _masterVoice = IntPtr.Zero;
        }

        if (_xaudio2 != IntPtr.Zero)
        {
            var release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Slot(_xaudio2, SlotRelease);
            release(_xaudio2);
            _xaudio2 = IntPtr.Zero;
        }

        if (_ring != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_ring);
            _ring = IntPtr.Zero;
        }

        _opened = false;
    }

    /// <summary>Reads vtable slot <paramref name="index"/> of the COM object at <paramref name="comObject"/>.</summary>
    private static void* Slot(IntPtr comObject, int index)
    {
        var vtbl = *(void***)(void*)comObject;
        return vtbl[index];
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
    private struct XAudio2Buffer
    {
        public uint Flags;
        public uint AudioBytes;
        public IntPtr pAudioData;
        public uint PlayBegin;
        public uint PlayLength;
        public uint LoopBegin;
        public uint LoopLength;
        public uint LoopCount;
        public IntPtr pContext;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XAudio2VoiceState
    {
        public IntPtr pCurrentBufferContext;
        public uint BuffersQueued;
        public ulong SamplesPlayed;
    }

    [LibraryImport("xaudio2_9.dll")]
    private static partial int XAudio2Create(out IntPtr ppXAudio2, uint flags, uint processor);
}
