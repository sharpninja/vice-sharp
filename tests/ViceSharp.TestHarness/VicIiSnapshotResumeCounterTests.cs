namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 remediation Phase 6 (audit
/// docs/audit-vicii-vs-vice-2026-07-06.md L10): a mid-frame snapshot resume
/// must restore the video counters exactly like VICE's snapshot module, which
/// loads vcbase, vc, rc, vmli, the refresh counter and the sprite DMA state
/// directly instead of re-deriving them from the raster position
/// (vicii-snapshot.c:105-108,131,223-227,250,270).
/// </summary>
public sealed class VicIiSnapshotResumeCounterTests
{
    /// <summary>
    /// FR: FR-VIC-CYCLE, TR: TR-VIC-SNAPRESUME-001, TEST: TEST-VIC-SNAPRESUME-01.
    /// Use case: resuming a .vsf lockstep snapshot mid-frame (after the
    /// display started) needs vc/vcbase/rc/vmli/refresh and the sprite DMA
    /// mask seeded from the snapshot, because they only re-derive at frame
    /// top (VICE restores them explicitly, vicii-snapshot.c:223-227/:250).
    /// Acceptance: injecting counters vc=123, vcbase=120, rc=5, vmli=3,
    /// refresh=$A7 and spriteDma=$21 makes the corresponding live state
    /// accessors report exactly those values.
    /// </summary>
    [Fact]
    public void InjectSnapshotState_Restores_VideoCounters_And_SpriteDma()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        var regs = new byte[64];
        regs[0x11] = 0x1B;

        vic.InjectSnapshotState(
            regs,
            rasterLine: 100,
            inLineCycle: 20,
            allowBadLines: true,
            idleState: false,
            videoCounter: 123,
            videoCounterBase: 120,
            rowCounter: 5,
            videoMatrixLineIndex: 3,
            refreshCounter: 0xA7,
            spriteDmaActiveMask: 0x21);

        Assert.Equal(123, vic.CurrentVideoMatrixCounter);
        Assert.Equal(5, vic.CurrentRowCounter);
        Assert.Equal(3, vic.CurrentVideoMatrixSlot);
        Assert.True(vic.IsSpriteDmaActive(0));
        Assert.True(vic.IsSpriteDmaActive(5));
        Assert.False(vic.IsSpriteDmaActive(1));

        // vcbase is observable through the cycle-13 VC reload of the next line
        // (vicii-cycle.c:543-545): after the VC-update cycle vc equals vcbase.
        int budget = vic.TotalLines * vic.CyclesPerLine;
        while (budget-- > 0 && !(vic.CurrentRasterLine == 101 && vic.RasterX == 13))
            vic.Tick();
        Assert.Equal(120, vic.CurrentVideoMatrixCounter);

        // The refresh counter is observable through the refresh fetch address
        // consumption: ConsumeRefreshCounter returns the current value and
        // decrements (vicii_fetch_refresh, vicii-fetch.c:203-206).
        var vic2 = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic2.Reset();
        vic2.InjectSnapshotState(regs, rasterLine: 100, inLineCycle: 20, refreshCounter: 0xA7);
        Assert.Equal(0xA7, vic2.ConsumeRefreshCounter());
    }
}
