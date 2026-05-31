namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cartridges;
using Xunit;

/// <summary>
/// FR/TR: FR-CART-FC3, BACKFILL-CART-001 (Final Cartridge III CRT type 60).
/// FC-III is a 64K image split into 4 banks of 16K each (ROML + ROMH). The
/// bank-register lives at $DFFF: data bits 0-1 = bank index, bit 7 = hide
/// (release ROML and ROMH). Bits 2-6 carry LED/NMI/freeze control and are
/// accepted-but-ignored in this slice. Source: VICE src/c64/cart/finalIII.c.
/// </summary>
public sealed class FinalCartridgeIIITests
{
    private const int BankSize = StandardCartridgeImage.RomBankSize;
    private const int Fc3ImageSize = BankSize * 8;  // 4 banks * 16K = 64K

    /// <summary>
    /// FR-CART-FC3.
    /// Use case: attach a 64K Final Cartridge III image; bank 0 must
    /// default-map for both ROML ($8000-$9FFF) and ROMH ($A000-$BFFF)
    /// before any $DFFF write.
    /// Acceptance: bus reads at $8010 and $A010 return the bank-0 ROML
    /// and ROMH sentinel bytes respectively.
    /// </summary>
    [Fact]
    public void Attach_64K_MapsBank0_ForRomLowAndRomHigh()
    {
        var image = BuildBankImage();
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();

        port.AttachCartridge(image, CartridgeMappingMode.FinalCartridgeIII);

        machine.Bus.Read(0x8010).Should().Be((byte)0x00, "bank 0 ROML sentinel at $8010");
        machine.Bus.Read(0xA010).Should().Be((byte)0xC0, "bank 0 ROMH sentinel at $A010");
    }

    /// <summary>
    /// FR-CART-FC3.
    /// Use case: writing $DFFF = N (bits 0-1) selects FC-III bank N for both
    /// ROML and ROMH at the same time (16K bank).
    /// Acceptance: for bank N in 0..3, bus reads at $8010 and $A010 return
    /// the per-bank ROML and ROMH sentinels.
    /// </summary>
    [Theory]
    [InlineData(0, 0x00, 0xC0)]
    [InlineData(1, 0x10, 0xD0)]
    [InlineData(2, 0x20, 0xE0)]
    [InlineData(3, 0x30, 0xF0)]
    public void WriteToDfff_SelectsBank_ForBothRomls(int bank, byte romlSentinel, byte romhSentinel)
    {
        var image = BuildBankImage();
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();
        port.AttachCartridge(image, CartridgeMappingMode.FinalCartridgeIII);

        machine.Bus.Write(0xDFFF, (byte)bank);

        machine.Bus.Read(0x8010).Should().Be(romlSentinel, $"ROML bank {bank}");
        machine.Bus.Read(0xA010).Should().Be(romhSentinel, $"ROMH bank {bank}");
    }

    /// <summary>
    /// FR-CART-FC3.
    /// Use case: bit 7 of $DFFF hides the cartridge so both ROML and ROMH
    /// release. Reads in those windows fall through to RAM/BASIC. Clearing
    /// bit 7 and re-selecting a bank restores the cartridge view.
    /// Acceptance: after $DFFF = $80 the RAM sentinel at $8010 returns;
    /// after $DFFF = $02 the bank-2 ROML and ROMH sentinels return.
    /// </summary>
    [Fact]
    public void WriteToDfff_Bit7_HidesCartridge()
    {
        var image = BuildBankImage();
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory
            ?? throw new InvalidOperationException("no system RAM");

        memory.Span[0x8010] = 0x5A;
        port.AttachCartridge(image, CartridgeMappingMode.FinalCartridgeIII);

        machine.Bus.Read(0x8010).Should().Be((byte)0x00, "cart visible by default");

        machine.Bus.Write(0xDFFF, 0x80);

        machine.Bus.Read(0x8010).Should().Be((byte)0x5A,
            "with hide bit, ROML must release and RAM under ROM shows through");

        machine.Bus.Write(0xDFFF, 0x02);  // re-show with bank 2
        machine.Bus.Read(0x8010).Should().Be((byte)0x20, "re-show with bank 2");
        machine.Bus.Read(0xA010).Should().Be((byte)0xE0, "re-show with bank 2 ROMH");
    }

    /// <summary>
    /// FR-CART-FC3.
    /// Use case: only $DFFF is the FC-III bank register. Writes to other
    /// I/O addresses ($DE00, $DF00, $DFFE) must NOT change the bank.
    /// Acceptance: after writing $02 to $DE00, $DF00, or $DFFE the bank-0
    /// sentinel still shows at $8010 (no bank change).
    /// </summary>
    [Theory]
    [InlineData(0xDE00)]
    [InlineData(0xDF00)]
    [InlineData(0xDFFE)]
    public void OtherIoWrites_DoNotChangeBank(ushort otherAddress)
    {
        var image = BuildBankImage();
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();
        port.AttachCartridge(image, CartridgeMappingMode.FinalCartridgeIII);

        machine.Bus.Write(otherAddress, 0x02);  // try to set bank 2

        machine.Bus.Read(0x8010).Should().Be((byte)0x00,
            $"writing ${otherAddress:X4} must NOT change the FC-III bank (only $DFFF does)");
    }

    /// <summary>
    /// FR-CART-FC3.
    /// Use case: only the fixed 64K image size (4 banks of 16K) is valid
    /// for Final Cartridge III. Smaller or oversize images must be rejected.
    /// Acceptance: AttachCartridge throws ArgumentException for 8K, 32K,
    /// and off-by-one image sizes.
    /// </summary>
    [Theory]
    [InlineData(BankSize)]              // 8K
    [InlineData(BankSize * 4)]          // 32K
    [InlineData(BankSize * 8 + 1)]      // off by one
    public void Attach_InvalidSize_Throws(int size)
    {
        var image = new byte[size];
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();

        var act = () => port.AttachCartridge(image, CartridgeMappingMode.FinalCartridgeIII);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Build a 64K FC-III image. Bank N has:
    ///   ROML offset 0x10:  byte = N * 0x10                       (0x00, 0x10, 0x20, 0x30)
    ///   ROMH offset 0x10:  byte = 0xC0 + (N * 0x10)              (0xC0, 0xD0, 0xE0, 0xF0)
    /// Each bank occupies 16K = ROML (offset 0..0x1FFF) + ROMH (offset 0x2000..0x3FFF).
    /// </summary>
    private static byte[] BuildBankImage()
    {
        var img = new byte[Fc3ImageSize];
        for (var b = 0; b < 4; b++)
        {
            var bankBase = b * BankSize * 2;
            img[bankBase + 0x10] = (byte)(b * 0x10);
            img[bankBase + BankSize + 0x10] = (byte)(0xC0 + b * 0x10);
        }
        return img;
    }
}
