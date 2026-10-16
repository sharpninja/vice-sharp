using Xunit;

[assembly: AssemblyFixture(typeof(ViceSharp.TestHarness.Audio.AssemblyAudioSilence))]

namespace ViceSharp.TestHarness.Audio;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// TR-QA-TESTSILENCE-001 (operator 2026-07-14: "I can STILL hear the unit tests").
/// ASSEMBLY-level audio silence: mutes the test host process's Windows audio session
/// once for the ENTIRE test run and restores the prior state when the run ends.
/// Linux selects SDL's dummy driver in the test process before any device is opened.
/// </summary>
/// <remarks>
/// The per-class <see cref="WindowsAudioSessionMute"/> fixtures are correct but raced
/// under xUnit's parallel class execution: a fast audio class disposing restored the
/// unmuted state while the 3-second WinMM throughput diagnostic was still playing.
/// This fixture brackets every class, so nested class fixtures always capture (and
/// restore) an already-muted state. Windows muting remains best effort; Linux
/// tests must never select a physical device, including opt-in live-audio diagnostics.
/// </remarks>
public sealed partial class AssemblyAudioSilence : IDisposable
{
    private readonly WindowsAudioSessionMute _mute = new();
    private readonly string? _previousSdl2Driver;
    private readonly string? _previousSdl3Driver;
    private readonly string? _previousNativeSdl2Driver;
    private readonly string? _previousNativeSdl3Driver;
    private bool _disposed;

    /// <summary>Set both SDL2 and sdl2-compat driver names only within this test process.</summary>
    public AssemblyAudioSilence()
    {
        if (OperatingSystem.IsLinux())
        {
            // SDL2/SDL_hints.h: SDL_AUDIODRIVER must be selected before SDL_Init.
            // sdl2-compat renames it to SDL_AUDIO_DRIVER for SDL3:
            // https://github.com/libsdl-org/sdl2-compat/blob/main/src/sdl2_compat.c
            _previousSdl2Driver = Environment.GetEnvironmentVariable("SDL_AUDIODRIVER");
            _previousSdl3Driver = Environment.GetEnvironmentVariable("SDL_AUDIO_DRIVER");
            _previousNativeSdl2Driver = Marshal.PtrToStringUTF8(GetNativeEnvironment("SDL_AUDIODRIVER"));
            _previousNativeSdl3Driver = Marshal.PtrToStringUTF8(GetNativeEnvironment("SDL_AUDIO_DRIVER"));
            SetDriver("SDL_AUDIODRIVER", "dummy", "dummy");
            SetDriver("SDL_AUDIO_DRIVER", "dummy", "dummy");
        }
    }

    /// <summary>True when the process session was acquired and muted for the run.</summary>
    public bool IsEngaged => OperatingSystem.IsLinux() || _mute.IsEngaged;

    /// <summary>Restores the pre-run mute state. Idempotent.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _mute.Dispose();
        if (OperatingSystem.IsLinux())
        {
            SetDriver("SDL_AUDIODRIVER", _previousSdl2Driver, _previousNativeSdl2Driver);
            SetDriver("SDL_AUDIO_DRIVER", _previousSdl3Driver, _previousNativeSdl3Driver);
        }
    }

    // .NET's managed environment and libc's native environment can differ on
    // Linux. SDL reads native getenv, so preserve and restore both independently.
    private static void SetDriver(string name, string? managedValue, string? nativeValue)
    {
        Environment.SetEnvironmentVariable(name, managedValue);
        int result = nativeValue is null ? UnsetNativeEnvironment(name) : SetNativeEnvironment(name, nativeValue, 1);
        if (result != 0)
            throw new InvalidOperationException("Could not isolate the test process's SDL audio driver.");
    }

    [LibraryImport("libc", EntryPoint = "getenv", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr GetNativeEnvironment(string name);
    [LibraryImport("libc", EntryPoint = "setenv", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int SetNativeEnvironment(string name, string value, int overwrite);
    [LibraryImport("libc", EntryPoint = "unsetenv", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int UnsetNativeEnvironment(string name);
}
