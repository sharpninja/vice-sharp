namespace ViceSharp.Host.Audio;

/// <summary>
/// Process-wide master output volume + mute for the emulator's audio. The audio
/// backend multiplies its samples by <see cref="EffectiveGain"/> just before they
/// reach the sound device, so this is the app's master level - independent of the
/// emulated SID $D418 volume that programs write. The UI sets it on a UI thread;
/// the audio thread reads it, so the fields are volatile. It lives in the host
/// (not Abstractions) so the Avalonia UI can drive it directly - the same
/// in-process coupling the lock-free video frame path uses - without crossing the
/// runtime-internals boundary the UI is forbidden from referencing.
/// </summary>
public static class MasterAudioControl
{
    private static volatile float _volume = 1f;
    private static volatile bool _muted;

    /// <summary>Master output volume in [0, 1]. Default 1 (full).</summary>
    public static float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>When true, output is silenced without losing the stored <see cref="Volume"/>.</summary>
    public static bool Muted
    {
        get => _muted;
        set => _muted = value;
    }

    /// <summary>The gain the audio backend applies: 0 when muted, otherwise <see cref="Volume"/>.</summary>
    public static float EffectiveGain => _muted ? 0f : _volume;
}
