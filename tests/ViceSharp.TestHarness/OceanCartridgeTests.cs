namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-CART-OCEAN, BACKFILL-CART-001 (Ocean CRT type 5).
/// Use case: Ocean is one of the most common 1980s C64 game cart formats.
/// Image is 32K to 512K split into 8K ROML banks. Writes to $DE00 take
/// bits 0-5 of the data byte (mod bank-count) as the active bank index;
/// there is no disable bit. Source: VICE src/c64/cart/ocean.c.
/// </summary>
public sealed class OceanCartridgeTests
{
    private const int BankSize = StandardCartridgeImage.RomBankSize;

    /// <summary>
    /// FR-CART-OCEAN.
    /// Use case: attach a 32K Ocean image; bank 0 must be mapped at
    /// $8000-$9FFF before any write to $DE00.
    /// Acceptance: bus reads at $8000 / $8001 return the bank-0 sentinel bytes.
    /// </summary>
    [Fact]
    public void Attach_32K_MapsBank0_AtRomLowByDefault()
    {
        var image = BuildBankImage(4);
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();

        port.AttachCartridge(image, CartridgeMappingMode.Ocean);

        machine.Bus.Read(0x8000).Should().Be((byte)0);
        machine.Bus.Read(0x8001).Should().Be((byte)1);
    }

    /// <summary>
    /// FR-CART-OCEAN.
    /// Use case: writing $DE00 = N selects Ocean bank N for $8000-$9FFF.
    /// Sentinel bytes at offset 0x10 in each bank prove the switch.
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
        port.AttachCartridge(image, CartridgeMappingMode.Ocean);

        machine.Bus.Write(0xDE00, (byte)bank);

        machine.Bus.Read(0x8010).Should().Be(sentinel,
            $"writing $DE00={bank:X2} must map Ocean bank {bank}");
    }

    /// <summary>
    /// FR-CART-OCEAN.
    /// Use case: bit 7 of $DE00 is NOT a disable bit on Ocean (unlike
    /// Magic Desk); bits 6-7 must be ignored while bits 0-5 still select
    /// the bank.
    /// Acceptance: writing $DE00 = $C2 selects bank 2 (bus read at $8010
    /// returns the bank-2 sentinel $20).
    /// </summary>
    [Fact]
    public void WriteToDe00_HighBits_AreIgnored()
    {
        var image = BuildBankImage(8);
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();
        port.AttachCartridge(image, CartridgeMappingMode.Ocean);

        // 0xC2 = bank 2 with high bits set; Ocean masks to bits 0-5.
        machine.Bus.Write(0xDE00, 0xC2);

        machine.Bus.Read(0x8010).Should().Be((byte)0x20,
            "Ocean must mask the bank-select to bits 0-5; high bits are ignored");
    }

    /// <summary>
    /// FR-CART-OCEAN.
    /// Use case: invalid Ocean image sizes (not a 4..64-bank multiple of 8K)
    /// must be rejected at attach time.
    /// Acceptance: AttachCartridge throws ArgumentException for 8K, 24K,
    /// and off-by-one image sizes.
    /// </summary>
    [Theory]
    [InlineData(BankSize)]              // 8K - too small
    [InlineData(BankSize * 3)]          // 24K - not a bank multiple
    [InlineData(BankSize * 4 + 1)]      // off by one
    public void Attach_InvalidSize_Throws(int size)
    {
        var image = new byte[size];
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();

        var act = () => port.AttachCartridge(image, CartridgeMappingMode.Ocean);

        act.Should().Throw<ArgumentException>();
    }

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
