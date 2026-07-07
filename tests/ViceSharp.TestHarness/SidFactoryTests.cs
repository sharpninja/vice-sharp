namespace ViceSharp.TestHarness;

using NSubstitute;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 Phase 0 (P0-7) / TR-SID-ORACLE-001.
/// The repo had two disagreeing SID construction paths: Commodore64 built a
/// backend-less Sid6581 directly (audio emission inert) while
/// ArchitectureBuilder built a profile-selected Sid6581/Sid8580 with the
/// audio backend and clock configuration. SidFactory is now the single
/// canonical constructor both paths delegate to, so the model, base address
/// and audio-clock behavior can never diverge between machines.
/// </summary>
public sealed class SidFactoryTests
{
    private const double PalMasterClockHz = 985248.0;

    /// <summary>
    /// FR: FR-SID-8580, TR: TR-SID-ORACLE-001, TEST: TEST-SID-WIRING-P0-01.
    /// Use case: machine profiles select the SID model; every construction
    /// path must resolve the same model for the same profile.
    /// Acceptance: a null profile and a 6581 profile produce a Sid6581 (not
    /// the 8580 subtype); a profile whose SidModel contains "8580" produces a
    /// Sid8580; all carry base address 0xD400.
    /// </summary>
    [Fact]
    public void Create_SelectsModelFromProfile()
    {
        var bus = new BasicBus();

        var defaulted = SidFactory.Create(bus, profile: null, audioBackend: null, PalMasterClockHz);
        Assert.IsType<Sid6581>(defaulted, exactMatch: true);
        Assert.Equal(0xD400, defaulted.BaseAddress);

        var profile6581 = Substitute.For<IMachineProfile>();
        profile6581.SidModel.Returns("6581");
        var sid6581 = SidFactory.Create(bus, profile6581, audioBackend: null, PalMasterClockHz);
        Assert.IsType<Sid6581>(sid6581, exactMatch: true);
        Assert.Equal(0xD400, sid6581.BaseAddress);

        var profile8580 = Substitute.For<IMachineProfile>();
        profile8580.SidModel.Returns("MOS8580");
        var sid8580 = SidFactory.Create(bus, profile8580, audioBackend: null, PalMasterClockHz);
        Assert.IsType<Sid8580>(sid8580);
        Assert.Equal(0xD400, sid8580.BaseAddress);
    }

    /// <summary>
    /// FR: FR-SID-CLOCK, TR: TR-SID-ORACLE-001, TEST: TEST-SID-WIRING-P0-02.
    /// Use case: live-audio emission must engage only when a real backend
    /// exists (headless/test hosts stay timing-clean and silent).
    /// Acceptance: with no backend the created SID reports
    /// IsAudioTimingSource false; with a backend it reports true (the factory
    /// called ConfigureAudioClock with the master clock).
    /// </summary>
    [Fact]
    public void Create_ConfiguresAudioClockOnlyWithBackend()
    {
        var bus = new BasicBus();

        var silent = SidFactory.Create(bus, profile: null, audioBackend: null, PalMasterClockHz);
        Assert.False(silent.IsAudioTimingSource);

        var backend = Substitute.For<IAudioBackend>();
        var audible = SidFactory.Create(bus, profile: null, backend, PalMasterClockHz);
        Assert.True(audible.IsAudioTimingSource);
    }
}
