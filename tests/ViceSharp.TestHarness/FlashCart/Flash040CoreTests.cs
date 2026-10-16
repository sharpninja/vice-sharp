namespace ViceSharp.TestHarness.FlashCart;

using ViceSharp.Core.FlashCarts;
using Xunit;

/// <summary>
/// FR: FE3 flash program path, TR: flash040 TYPE_B FSM, TEST: Flash040Core command sequences.
/// Use case: FE3 MODE_FLASH programming uses AM29F040B unlock + byte program / erase.
/// Acceptance: magic AA/55, program AND, chip erase to 0xFF, autoselect manufacturer/device IDs.
/// VICE source: native/vice/vice/src/core/flash040core.c FLASH040_TYPE_B.
/// </summary>
public sealed class Flash040CoreTests
{
    [Fact]
    public void ByteProgram_UnlockSequence_AndsWithExistingOnes()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0xFF);
        var flash = new Flash040Core(data);

        // Direct poke without unlock must not change data (READ ignores non-magic).
        flash.Store(0x1000, 0x00);
        Assert.Equal(0xFF, flash.Peek(0x1000));
        Assert.Equal(Flash040Core.State.Read, flash.FlashState);

        ProgramByte(flash, 0x1000, 0xA5);
        Assert.Equal(0xA5, flash.Peek(0x1000));
        Assert.True(flash.Dirty);
        Assert.Equal(Flash040Core.State.Read, flash.FlashState);

        // Second program can only clear bits (0xA5 & 0x0F = 0x05).
        ProgramByte(flash, 0x1000, 0x0F);
        Assert.Equal(0x05, flash.Peek(0x1000));
    }

    [Fact]
    public void ChipErase_UnlockSequence_FillsFf()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0x55);
        var flash = new Flash040Core(data);

        // AA@555, 55@2AA, 80@555, AA@555, 55@2AA, 10@555
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0x80);
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0x10);

        // TYPE_B chip erase completes after EraseChipCycles (not instant).
        Assert.Equal(Flash040Core.State.ChipErase, flash.FlashState);
        flash.AdvanceCycles(Flash040Core.EraseChipCycles);

        Assert.Equal(0xFF, flash.Peek(0));
        Assert.Equal(0xFF, flash.Peek(Flash040Core.Size - 1));
        Assert.Equal(Flash040Core.State.Read, flash.FlashState);
        Assert.True(flash.Dirty);
    }

    [Fact]
    public void SectorErase_OnlyClearsTargetSector()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0x11);
        var flash = new Flash040Core(data);

        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0x80);
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        // Sector 1 starts at 0x10000
        flash.Store(0x10000, 0x30);

        // Timeout then one sector cycle budget (TYPE_B).
        flash.AdvanceCycles(Flash040Core.EraseSectorTimeoutCycles + Flash040Core.EraseSectorCycles);

        Assert.Equal(0x11, flash.Peek(0));
        Assert.Equal(0xFF, flash.Peek(0x10000));
        Assert.Equal(0xFF, flash.Peek(0x1FFFF));
        Assert.Equal(0x11, flash.Peek(0x20000));
    }

    [Fact]
    public void Autoselect_ReturnsManufacturerAndDeviceIds()
    {
        var data = new byte[Flash040Core.Size];
        var flash = new Flash040Core(data);

        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0x90);

        Assert.Equal(Flash040Core.ManufacturerId, flash.Read(0x0000));
        Assert.Equal(Flash040Core.DeviceId, flash.Read(0x0001));

        flash.Store(0x0000, 0xF0); // reset
        Assert.Equal(Flash040Core.State.Read, flash.FlashState);
    }

    /// <summary>
    /// Magic addresses use TYPE_B mask 0x7FF so FE3 BLK5 faddr 0x6555/0x62AA work.
    /// </summary>
    [Fact]
    public void Magic_AcceptsFe3Blk5MappedAddresses()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0xFF);
        var flash = new Flash040Core(data);

        // faddr as calc_addr($A555, bank0, BLK5_BASE=0x6000) = 0x6555
        flash.Store(0x6555, 0xAA);
        flash.Store(0x62AA, 0x55);
        flash.Store(0x6555, 0xA0);
        flash.Store(0x6000, 0xC3);
        Assert.Equal(0xC3, flash.Peek(0x6000));
    }

    private static void ProgramByte(Flash040Core flash, uint addr, byte value)
    {
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0xA0);
        flash.Store(addr, value);
    }
}
