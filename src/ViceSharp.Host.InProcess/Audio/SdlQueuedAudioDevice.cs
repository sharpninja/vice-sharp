using System.Runtime.InteropServices;

namespace ViceSharp.Host.Audio;

/// <summary>
/// Minimal SDL2 queued-playback ABI; no managed callback or audio thread is required.
/// SDL copies submitted bytes before returning and internally synchronizes queue operations.
/// Official ABI/contracts: https://wiki.libsdl.org/SDL2/SDL_OpenAudioDevice,
/// https://wiki.libsdl.org/SDL2/SDL_QueueAudio, https://wiki.libsdl.org/SDL2/SDL_GetQueuedAudioSize,
/// https://wiki.libsdl.org/SDL2/SDL_PauseAudioDevice, https://wiki.libsdl.org/SDL2/SDL_ClearQueuedAudio.
/// Device loss: https://wiki.libsdl.org/SDL2/SDL_GetAudioDeviceStatus.
/// Struct layout/constants also checked against the installed SDL2/SDL_audio.h and SDL.h.
/// </summary>
internal sealed unsafe partial class SdlQueuedAudioDevice : ILinuxAudioDevice
{
    private const string Library = "libSDL2-2.0.so.0";
    private const uint InitAudio = 0x00000010;
    private const ushort Signed16LittleEndian = 0x8010;
    private uint _device;
    private bool _initialized;

    public bool Open(int sampleRate, int channels, int fragmentSamples)
    {
        if (!OperatingSystem.IsLinux()) return false;
        if (SDL_InitSubSystem(InitAudio) != 0)
            throw new InvalidOperationException("SDL2 audio initialization failed: " + LastError());
        _initialized = true;
        var desired = new AudioSpec
        {
            Frequency = sampleRate,
            Format = Signed16LittleEndian,
            Channels = checked((byte)channels),
            Samples = checked((ushort)fragmentSamples),
            Callback = IntPtr.Zero,
        };
        // No format-change flags: SDL converts to the hardware format internally.
        _device = SDL_OpenAudioDevice(IntPtr.Zero, 0, in desired, out var obtained, 0);
        if (_device == 0)
            throw new InvalidOperationException("SDL2 could not open the default output: " + LastError());
        if (obtained.Frequency != sampleRate || obtained.Format != Signed16LittleEndian || obtained.Channels != channels)
            throw new InvalidOperationException("SDL2 did not preserve the requested PCM format.");
        EnsureDevicePresent();
        return true; // Newly opened SDL devices are paused until the first queued fragment.
    }

    // SDL reports software bytes not yet submitted to hardware, not the exact hardware play cursor.
    public int QueuedBytes
    {
        get
        {
            EnsureDevicePresent();
            return checked((int)Math.Min(SDL_GetQueuedAudioSize(_device), int.MaxValue));
        }
    }

    private void EnsureDevicePresent()
    {
        // STOPPED (0) indicates an unusable device. PAUSED (2) is valid and
        // must preserve the queue; queue size alone cannot distinguish loss.
        if (_device == 0 || SDL_GetAudioDeviceStatus(_device) == 0)
            throw new InvalidOperationException("The SDL2 output device stopped or disconnected.");
    }

    public bool Queue(ReadOnlySpan<byte> pcm)
    {
        fixed (byte* data = pcm)
            return SDL_QueueAudio(_device, data, checked((uint)pcm.Length)) == 0;
    }

    public void SetPaused(bool paused) => SDL_PauseAudioDevice(_device, paused ? 1 : 0);
    public void Clear() => SDL_ClearQueuedAudio(_device);

    public void Dispose()
    {
        if (_device != 0)
        {
            SDL_CloseAudioDevice(_device);
            _device = 0;
        }
        if (_initialized)
        {
            SDL_QuitSubSystem(InitAudio);
            _initialized = false;
        }
    }

    private static string LastError() => Marshal.PtrToStringUTF8(SDL_GetError()) ?? "unknown error";

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioSpec
    {
        public int Frequency;
        public ushort Format;
        public byte Channels;
        public byte Silence;
        public ushort Samples;
        public ushort Padding;
        public uint Size;
        public IntPtr Callback;
        public IntPtr UserData;
    }

    [LibraryImport(Library)] private static partial int SDL_InitSubSystem(uint flags);
    [LibraryImport(Library)] private static partial void SDL_QuitSubSystem(uint flags);
    [LibraryImport(Library)] private static partial uint SDL_OpenAudioDevice(IntPtr device, int capture,
        in AudioSpec desired, out AudioSpec obtained, int allowedChanges);
    [LibraryImport(Library)] private static partial int SDL_QueueAudio(uint device, byte* data, uint length);
    [LibraryImport(Library)] private static partial uint SDL_GetQueuedAudioSize(uint device);
    [LibraryImport(Library)] private static partial int SDL_GetAudioDeviceStatus(uint device);
    [LibraryImport(Library)] private static partial void SDL_PauseAudioDevice(uint device, int paused);
    [LibraryImport(Library)] private static partial void SDL_ClearQueuedAudio(uint device);
    [LibraryImport(Library)] private static partial void SDL_CloseAudioDevice(uint device);
    [LibraryImport(Library)] private static partial IntPtr SDL_GetError();
}
