namespace ViceSharp.TestHarness.Audio;

using ViceSharp.Host.Audio;
using Xunit;

/// <summary>
/// FR-AUDIOOUT-001 / TR-AUDIOOUT-MASTER-001: process-wide master output volume + mute,
/// applied to the emulator's samples just before they reach the sound device (independent
/// of the emulated SID $D418 volume). Drives the status-bar mute toggle + volume slider.
/// </summary>
public sealed class MasterAudioControlTests
{
    /// <summary>
    /// FR-AUDIOOUT-001 / TR-AUDIOOUT-MASTER-001.
    /// Use case: the master control exposes a single effective gain the backend multiplies by.
    /// Acceptance: EffectiveGain equals Volume when not muted, 0 when muted, and Volume clamps to [0, 1].
    /// </summary>
    [Fact]
    public void EffectiveGain_ReflectsVolumeAndMute_AndClamps()
    {
        try
        {
            MasterAudioControl.Muted = false;
            MasterAudioControl.Volume = 0.5f;
            Assert.Equal(0.5f, MasterAudioControl.EffectiveGain, 3);

            MasterAudioControl.Muted = true;
            Assert.Equal(0f, MasterAudioControl.EffectiveGain);

            MasterAudioControl.Muted = false;
            MasterAudioControl.Volume = 5f;
            Assert.Equal(1f, MasterAudioControl.Volume);
            MasterAudioControl.Volume = -3f;
            Assert.Equal(0f, MasterAudioControl.Volume);
        }
        finally
        {
            MasterAudioControl.Muted = false;
            MasterAudioControl.Volume = 1f;
        }
    }

    /// <summary>
    /// FR-AUDIOOUT-001 / TR-AUDIOOUT-MASTER-001.
    /// Use case: the float-to-PCM converter scales by the supplied master gain so the backend
    ///   can attenuate or silence output without a separate pass.
    /// Acceptance: gain 1 yields full scale, gain 0.5 ~half, gain 0 silence.
    /// </summary>
    [Fact]
    public void ConvertToPcm16_AppliesGain()
    {
        var samples = new[] { 1.0f };

        var full = new byte[2];
        AudioSampleConverter.ConvertToPcm16(samples, full, 1f);
        Assert.Equal(32767, (short)(full[0] | (full[1] << 8)));

        var half = new byte[2];
        AudioSampleConverter.ConvertToPcm16(samples, half, 0.5f);
        Assert.InRange((short)(half[0] | (half[1] << 8)), 16000, 16600);

        var muted = new byte[2];
        AudioSampleConverter.ConvertToPcm16(samples, muted, 0f);
        Assert.Equal(0, (short)(muted[0] | (muted[1] << 8)));
    }
}
