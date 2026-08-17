namespace ViceSharp.TestHarness.FlashCart;

using ViceSharp.Core.FlashCarts;
using Xunit;

/// <summary>
/// FR-VIC20-CART-001 / AC-CT-05 / TEST-VIC20-CT-05: FE3 flash040 TYPE_B erase
/// latency and status timing match VICE flash040core.c (AM29F040B).
/// Use case: chip erase stays in CHIP_ERASE for erase_chip_cycles; sector erase
/// uses erase_sector_timeout_cycles then erase_sector_cycles per sector.
/// Acceptance: data not 0xFF until cycles elapse; status reads DQ toggle during erase.
/// VICE: native/vice/vice/src/core/flash040core.c flash_types[FLASH040_TYPE_B]
/// (timeout=50, sector=1_000_000, chip=8_000_000).
/// </summary>
public sealed class Flash040EraseLatencyTests
{
    [Fact]
    public void ChipErase_DoesNotCompleteUntilChipCyclesElapse()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0x55);
        var flash = new Flash040Core(data);

        IssueChipErase(flash);

        Assert.Equal(Flash040Core.State.ChipErase, flash.FlashState);
        Assert.Equal(0x55, flash.Peek(0));

        flash.AdvanceCycles(Flash040Core.EraseChipCycles - 1);
        Assert.Equal(Flash040Core.State.ChipErase, flash.FlashState);
        Assert.Equal(0x55, flash.Peek(0));

        flash.AdvanceCycles(1);
        Assert.Equal(Flash040Core.State.Read, flash.FlashState);
        Assert.Equal(0xFF, flash.Peek(0));
        Assert.Equal(0xFF, flash.Peek(Flash040Core.Size - 1));
        Assert.True(flash.Dirty);
    }

    [Fact]
    public void SectorErase_TimeoutThenSectorCycles_ThenRead()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0x11);
        var flash = new Flash040Core(data);

        IssueSectorErase(flash, sectorAddr: 0x10000);

        Assert.Equal(Flash040Core.State.SectorEraseTimeout, flash.FlashState);
        Assert.Equal(0x11, flash.Peek(0x10000));

        flash.AdvanceCycles(Flash040Core.EraseSectorTimeoutCycles - 1);
        Assert.Equal(Flash040Core.State.SectorEraseTimeout, flash.FlashState);

        flash.AdvanceCycles(1);
        Assert.Equal(Flash040Core.State.SectorErase, flash.FlashState);
        // Sector data not yet erased until sector cycles complete.
        Assert.Equal(0x11, flash.Peek(0x10000));

        flash.AdvanceCycles(Flash040Core.EraseSectorCycles - 1);
        Assert.Equal(Flash040Core.State.SectorErase, flash.FlashState);
        Assert.Equal(0x11, flash.Peek(0x10000));

        flash.AdvanceCycles(1);
        Assert.Equal(Flash040Core.State.Read, flash.FlashState);
        Assert.Equal(0x11, flash.Peek(0));
        Assert.Equal(0xFF, flash.Peek(0x10000));
        Assert.Equal(0xFF, flash.Peek(0x1FFFF));
        Assert.Equal(0x11, flash.Peek(0x20000));
    }

    [Fact]
    public void ChipErase_StatusRead_TogglesWhileBusy()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0xAA);
        var flash = new Flash040Core(data);
        IssueChipErase(flash);

        var a = flash.Read(0);
        var b = flash.Read(0);
        Assert.NotEqual(a & 0x40, b & 0x40);
        Assert.Equal(0x08, a & 0x08);
    }

    [Fact]
    public void SectorEraseTimeout_NonConfirmWriteCancelsPendingErase()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0x11);
        var flash = new Flash040Core(data);
        IssueSectorErase(flash, sectorAddr: 0x10000);

        flash.Store(0x10000, 0x00);

        Assert.Equal(Flash040Core.State.Read, flash.FlashState);
        Assert.Equal(0, flash.EraseCyclesRemaining);
        flash.AdvanceCycles(
            Flash040Core.EraseSectorTimeoutCycles
            + Flash040Core.EraseSectorCycles);
        Assert.Equal(0x11, flash.Peek(0x10000));
    }

    [Fact]
    public void SectorErase_SuspendAndResumeUseViceCycleBudget()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0x11);
        var flash = new Flash040Core(data);
        IssueSectorErase(flash, sectorAddr: 0x10000);
        flash.AdvanceCycles(Flash040Core.EraseSectorTimeoutCycles);

        flash.Store(0x10000, 0xB0);

        Assert.Equal(Flash040Core.State.SectorEraseSuspend, flash.FlashState);
        Assert.Equal(0, flash.EraseCyclesRemaining);
        flash.AdvanceCycles(Flash040Core.EraseSectorCycles);
        Assert.Equal(0x11, flash.Peek(0x10000));

        flash.Store(0x10000, 0x30);

        Assert.Equal(Flash040Core.State.SectorErase, flash.FlashState);
        Assert.Equal(Flash040Core.EraseSectorCycles, flash.EraseCyclesRemaining);
        flash.AdvanceCycles(Flash040Core.EraseSectorCycles);
        Assert.Equal(Flash040Core.State.Read, flash.FlashState);
        Assert.Equal(0xFF, flash.Peek(0x10000));
    }

    [Fact]
    public void ChipErase_IgnoresResetWriteWhileBusy()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0x55);
        var flash = new Flash040Core(data);
        IssueChipErase(flash);

        flash.Store(0, 0xF0);

        Assert.Equal(Flash040Core.State.ChipErase, flash.FlashState);
        flash.AdvanceCycles(Flash040Core.EraseChipCycles);
        Assert.Equal(Flash040Core.State.Read, flash.FlashState);
        Assert.Equal(0xFF, flash.Peek(0));
    }

    private static void IssueChipErase(Flash040Core flash)
    {
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0x80);
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0x10);
    }

    private static void IssueSectorErase(Flash040Core flash, uint sectorAddr)
    {
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0x80);
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(sectorAddr, 0x30);
    }
}
