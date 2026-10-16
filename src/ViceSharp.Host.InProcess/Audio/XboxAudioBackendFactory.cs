using ViceSharp.Abstractions;
using ViceSharp.Core.Media;

namespace ViceSharp.Host.Audio;

/// <summary>
/// Selects the console (UWP/Xbox) real-time audio backend for live SID playback,
/// or null when no real-time output is available so headless / test contexts stay
/// SILENT and deterministic: no audio device is opened, the SID never touches the
/// audio path, and cycle-accurate pacing/replay is unperturbed.
///
/// This is the console counterpart to <see cref="AudioBackendFactory"/> (the desktop
/// WinMm selector). It shares the identical <c>VICESHARP_AUDIO</c> env gate
/// (<see cref="AudioBackendFactory.IsAudioEnabled"/>) so the two heads can never
/// diverge on when audio engages, and - like the desktop path
/// (<c>DefaultEmulatorRuntimeFactory.CreateDefaultAudioTap</c>) - wraps the real
/// device backend in a <see cref="CaptureAudioTap"/> so a runtime StartCapture(Audio)
/// can attach a WAV recorder without rebuilding the machine. Master volume/mute is
/// applied once downstream in <see cref="AudioSampleConverter"/> (via
/// <see cref="MasterAudioControl.EffectiveGain"/>) by the device backend; this
/// selector never touches the gain.
///
/// It is deliberately pure of XAudio2 interop: no P/Invoke lives here. The real
/// source-voice device (<c>XAudio2SourceVoiceBackend</c>) arrives in PLAN-XBOXUWP
/// S18 (IMPL-XBOXUWP-018) and is supplied here as the <paramref name="deviceBackendFactory"/>.
/// On console the head passes that creator; in tests / headless nothing is passed,
/// so <see cref="CreateDefault"/> returns null and the SID is inert.
/// </summary>
public static class XboxAudioBackendFactory
{
    /// <summary>
    /// Creates the console live-audio backend, or null to stay silent.
    ///
    /// Returns null whenever audio is disabled/headless (the shared
    /// <c>VICESHARP_AUDIO</c> gate is unset or "0") - the gate wins even when a
    /// <paramref name="deviceBackendFactory"/> is supplied, so a headless context
    /// that happens to wire a creator still opens no device. When audio is enabled
    /// AND a <paramref name="deviceBackendFactory"/> is supplied, it is invoked to
    /// obtain the real device backend, which is wrapped in a
    /// <see cref="CaptureAudioTap"/> exactly as the desktop path wraps WinMm. When
    /// audio is enabled but no creator is supplied (or the creator returns null),
    /// null is returned - this selector never fabricates a device.
    /// </summary>
    /// <param name="deviceBackendFactory">
    /// Optional factory for the real console device backend (S18's XAudio2 source
    /// voice). Only invoked when audio is enabled. Left null in tests / headless so
    /// no device is opened.
    /// </param>
    /// <returns>
    /// A <see cref="CaptureAudioTap"/> wrapping the device backend when audio is
    /// enabled and a device was produced; otherwise null.
    /// </returns>
    public static IAudioBackend? CreateDefault(Func<IAudioBackend?>? deviceBackendFactory = null)
    {
        // Identical opt-in gate as the desktop path: only an interactive console head
        // sets VICESHARP_AUDIO=1. Test/headless contexts leave it unset and run
        // silently, so the suite never opens an audio device. The gate wins even when
        // a deviceBackendFactory is supplied.
        if (!AudioBackendFactory.IsAudioEnabled())
            return null;

        // Enabled but no device creator: do NOT fabricate a device. The real XAudio2
        // source-voice backend is supplied by S18; until then (and in any context that
        // does not pass it) there is nothing to open, so stay silent.
        if (deviceBackendFactory is null)
            return null;

        var device = deviceBackendFactory();
        if (device is null)
            return null;

        // Wrap exactly as the desktop path wraps WinMm: the tap sits permanently in
        // the SID -> output path and is a transparent pass-through until a recorder
        // attaches.
        return new CaptureAudioTap(device);
    }
}
