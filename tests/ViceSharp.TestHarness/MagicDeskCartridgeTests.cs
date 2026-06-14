namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-CART-MAGICDESK, BACKFILL-CART-001 (Magic Desk / CRT type 19).
/// Use case: Magic Desk is one of the most-used advanced C64 cartridge
/// mappers. The image is split into 8K ROML banks; writes to $DE00 select
/// the active bank (bits 0-6) and an "I/O disable" bit (bit 7). When bit 7
/// is high the cartridge releases ROML so $8000-$9FFF reads fall through
/// to RAM/BASIC again. Source: VICE src/c64/cart/magicdesk.c.
/// </summary>
public sealed class MagicDeskCartridgeTests
{
    private const int BankSize = StandardCartridgeImage.RomBankSize;

    /// <summary>
    /// FR-CART-MAGICDESK acceptance criterion 1.
    /// Use case: attach a 32K Magic Desk image; bank 0 must be mapped at
    /// $8000-$9FFF immediately after reset before any write to $DE00.
    /// Acceptance: bus reads at $8000 and $8001 match bank 0 sentinel bytes.
    /// </summary>
    [Fact]
    public void Attach_32K_MapsBank0_AtRomLowByDefault()
    {
        var image = BuildBankImage(4);  // 4 banks of 8K = 32K
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();

        port.AttachCartridge(image, CartridgeMappingMode.MagicDesk);

        machine.Bus.Read(0x8000).Should().Be((byte)0, "bank 0 first byte is 0");
        machine.Bus.Read(0x8001).Should().Be((byte)1, "bank 0 second byte is 1 (sentinel)");
    }

    /// <summary>
    /// FR-CART-MAGICDESK acceptance criterion 2.
    /// Use case: writing $DE00 = bank index selects the matching 8K bank for
    /// $8000-$9FFF. Each bank carries a unique sentinel byte at offset 0x10.
    /// Acceptance: bus read at $8010 returns the per-bank sentinel for each
    /// of bank 0..3.
    /// </summary>
    [Theory]
    [InlineData(0, 0x00)]
    [InlineData(1, 0x10)]
    [InlineData(2, 0x20)]
    [InlineData(3, 0x30)]
    public void WriteToDe00_SelectsBank(int bank, byte sentinel)
    {
        var image = BuildBankImage(4);
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();
        port.AttachCartridge(image, CartridgeMappingMode.MagicDesk);

        machine.Bus.Write(0xDE00, (byte)bank);

        // Sentinel byte at offset 0x10 within each bank is unique.
        machine.Bus.Read(0x8010).Should().Be(sentinel,
            $"writing $DE00={bank:X2} must map bank {bank} so $8010 returns {sentinel:X2}");
    }

    /// <summary>
    /// FR-CART-MAGICDESK acceptance criterion 3.
    /// Use case: writing $DE00 with bit 7 set disables the cartridge so ROML
    /// releases and $8000 reads fall through to RAM under ROM. Clearing bit 7
    /// and re-selecting a bank restores the cartridge view.
    /// Acceptance: after writing $DE00 = $80 the RAM sentinel is visible at
    /// $8010; after writing $DE00 = $02 the bank-2 sentinel is visible.
    /// </summary>
    [Fact]
    public void WriteToDe00_Bit7_DisablesCartridge_AndReenables()
    {
        var image = BuildBankImage(4);
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory
            ?? throw new InvalidOperationException("C64 did not expose system RAM.");

        // Seed RAM under ROML with a sentinel so the disable test has a
        // distinguishable value to fall through to.
        memory.Span[0x8010] = 0xA5;

        port.AttachCartridge(image, CartridgeMappingMode.MagicDesk);

        // Sanity: cart visible, bank 0 - 0xA5 RAM is shadowed by ROML.
        machine.Bus.Read(0x8010).Should().Be((byte)0x00);

        // Disable: bit 7 of the $DE00 write releases ROML; RAM sentinel returns.
        machine.Bus.Write(0xDE00, 0x80);
        machine.Bus.Read(0x8010).Should().Be((byte)0xA5,
            "with disable bit set, $8010 must read the RAM sentinel (cart released ROML)");

        // Re-enable bank 2.
        machine.Bus.Write(0xDE00, 0x02);
        machine.Bus.Read(0x8010).Should().Be((byte)0x20,
            "re-enabling with bank 2 selected must restore the bank 2 sentinel");
    }

    /// <summary>
    /// FR-CART-MAGICDESK validation.
    /// Use case: invalid Magic Desk image sizes (not a multiple of 8K, below
    /// the 4-bank minimum, or otherwise malformed) must be rejected at
    /// attach time.
    /// Acceptance: AttachCartridge throws ArgumentException for 8K, 16K,
    /// 24K, and off-by-one image sizes.
    /// </summary>
    [Theory]
    [InlineData(BankSize)]              // 8K - too small (need >= 4 banks)
    [InlineData(BankSize * 2)]          // 16K - too small
    [InlineData(BankSize * 3)]          // 24K - not a power-of-two-ish bank multiple
    [InlineData(BankSize * 4 + 1)]      // off by one byte
    public void Attach_InvalidSize_Throws(int size)
    {
        var image = new byte[size];
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();

        var act = () => port.AttachCartridge(image, CartridgeMappingMode.MagicDesk);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Build a Magic Desk image with <paramref name="banks"/> banks of 8K,
    /// where bank N is filled with the byte (N * 0x10) at offset 0x10 and
    /// the byte N at offset 0x00. This lets tests assert the active bank
    /// from a single read.
    /// </summary>
    private static byte[] BuildBankImage(int banks)
    {
        var img = new byte[banks * BankSize];
        for (var b = 0; b < banks; b++)
        {
            img[b * BankSize + 0x00] = (byte)b;
            img[b * BankSize + 0x01] = (byte)(b + 1);
            img[b * BankSize + 0x10] = (byte)(b * 0x10);
        }
        return img;
    }
}
