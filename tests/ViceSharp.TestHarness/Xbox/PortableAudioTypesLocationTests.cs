namespace ViceSharp.TestHarness.Xbox;

using ViceSharp.Abstractions;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S6 (IMPL-XBOXUWP-006). FR-XAUDIO-002 / TR-XAUDIO-003:
/// <c>MasterAudioControl</c> (master volume + mute) and <c>AudioSampleConverter</c>
/// (float -&gt; PCM16 with gain applied once) are relocated into
/// <c>ViceSharp.Abstractions</c> so ViewModels - which reference only Abstractions -
/// can bind master volume/mute, and both the desktop WinMm backend and the future
/// Xbox XAudio2 backend share one master-gain + format path. This is a topology
/// guard: it asserts the two types now live in the same assembly as
/// <see cref="IAudioBackend"/> (ViceSharp.Abstractions), independent of their
/// (unchanged) behavior, which the existing MasterAudioControlTests guard.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class PortableAudioTypesLocationTests
{
    /// <summary>
    /// FR-XAUDIO-002 / TR-XAUDIO-003 (IMPL-XBOXUWP-006), TEST location guard.
    /// Use case: an assembly that references only the emulator contract
    /// (ViceSharp.Abstractions) must be able to reach <c>MasterAudioControl</c> so
    /// ViewModels can drive master volume/mute without depending on the host.
    /// Acceptance: <c>typeof(MasterAudioControl).Assembly</c> equals
    /// <c>typeof(IAudioBackend).Assembly</c> (both live in ViceSharp.Abstractions).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void MasterAudioControl_LivesInAbstractionsAssembly()
    {
        Assert.Equal(typeof(IAudioBackend).Assembly, typeof(MasterAudioControl).Assembly);
    }

    /// <summary>
    /// FR-XAUDIO-002 / TR-XAUDIO-003 (IMPL-XBOXUWP-006), TEST location guard.
    /// Use case: the shared float-to-PCM16 conversion (with master gain applied
    /// once) must be reachable from the contract assembly so both the WinMm and the
    /// XAudio2 backends consume the identical converter.
    /// Acceptance: <c>typeof(AudioSampleConverter).Assembly</c> equals
    /// <c>typeof(IAudioBackend).Assembly</c> (both live in ViceSharp.Abstractions).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void AudioSampleConverter_LivesInAbstractionsAssembly()
    {
        Assert.Equal(typeof(IAudioBackend).Assembly, typeof(AudioSampleConverter).Assembly);
    }
}
