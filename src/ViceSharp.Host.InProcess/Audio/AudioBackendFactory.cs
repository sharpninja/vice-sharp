using ViceSharp.Abstractions;

namespace ViceSharp.Host.Audio;

/// <summary>
/// Creates the platform-default real-time audio backend for live SID playback,
/// or null when no real-time output is available (non-Windows hosts and
/// headless/test contexts fall back to silent emulation). The returned backend
/// is wired into the SID at machine-build time; when it is null the SID never
/// touches the audio path.
/// </summary>
public static class AudioBackendFactory
{
    public static IAudioBackend? CreateDefault()
    {
        // Opt-in: only the interactive app enables live audio (it sets
        // VICESHARP_AUDIO=1 at startup). Test/headless contexts leave the
        // variable unset and run silently, so the suite never opens an audio
        // device. Users can force-disable with VICESHARP_AUDIO=0.
        if (!IsAudioEnabled())
            return null;

        if (OperatingSystem.IsWindows())
            return new WinMmAudioBackend();

        return null;
    }

    private static bool IsAudioEnabled()
    {
        var value = Environment.GetEnvironmentVariable("VICESHARP_AUDIO");
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Equals("1", StringComparison.Ordinal)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("on", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
