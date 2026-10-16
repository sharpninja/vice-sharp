// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): the XAudio2 audio wiring. #if HAS_UWP-guarded in
// full. The XAudio2 interop degrades to silence off-device, so this is safe on the dev PC.
#if HAS_UWP
namespace ViceSharp.Xbox.Platform;

using ViceSharp.Abstractions;
using ViceSharp.Host.Audio;

/// <summary>
/// Produces the console live-audio backend for the head. It composes the shared
/// <see cref="XboxAudioBackendFactory"/> selector with the real XAudio2 source-voice device
/// (<see cref="XAudio2SourceVoiceBackend"/>): audio engages only when VICESHARP_AUDIO is
/// enabled, and the device degrades to a silent no-op when it cannot be opened (headless /
/// non-Windows / driver failure), so nothing crashes off-device.
/// </summary>
public static class XboxAudioWiring
{
    /// <summary>
    /// Creates the console audio backend, or <c>null</c> when audio is disabled/headless.
    /// </summary>
    /// <returns>
    /// A capture-tapped XAudio2 backend when audio is enabled and a device was produced;
    /// otherwise <c>null</c> (the SID then stays off the audio path, timing-clean).
    /// </returns>
    public static IAudioBackend? CreateBackend()
        => XboxAudioBackendFactory.CreateDefault(() => new XAudio2SourceVoiceBackend());
}
#endif
