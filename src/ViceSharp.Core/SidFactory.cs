namespace ViceSharp.Core;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;

/// <summary>
/// Canonical SID construction (PLAN-VICEPARITY-001 P0-7). Every machine path
/// (ArchitectureBuilder, the legacy Commodore64 benchmark machine) creates its
/// SID here so the model selection, base address and audio-clock wiring can
/// never diverge. The parity oracle assumes one construction contract:
/// profile-selected 6581/8580 at $D400, with 44.1 kHz emission engaged only
/// when a real audio backend exists (headless and test hosts stay
/// timing-clean and silent).
/// </summary>
public static class SidFactory
{
    /// <summary>C64 SID base address ($D400).</summary>
    public const ushort C64BaseAddress = 0xD400;

    /// <summary>
    /// Create the SID for a machine. The model comes from
    /// <paramref name="profile"/> (SidModel containing "8580" selects the
    /// Sid8580; anything else, including no profile, selects the Sid6581).
    /// When <paramref name="audioBackend"/> is present the SID's audio clock
    /// is configured from <paramref name="masterClockHz"/>; otherwise the
    /// audio path stays untouched (parity-preserving).
    /// </summary>
    public static Sid6581 Create(IBus bus, IMachineProfile? profile, IAudioBackend? audioBackend, double masterClockHz)
    {
        var sid = profile is not null &&
                  profile.SidModel.Contains("8580", StringComparison.OrdinalIgnoreCase)
            ? new Sid8580(bus, audioBackend) { BaseAddress = C64BaseAddress }
            : new Sid6581(bus, audioBackend) { BaseAddress = C64BaseAddress };

        // Drive live-audio emission at 44.1 kHz only when a backend is present;
        // otherwise the SID never touches the audio path (parity-preserving).
        if (audioBackend is not null)
            sid.ConfigureAudioClock(masterClockHz);

        return sid;
    }
}
