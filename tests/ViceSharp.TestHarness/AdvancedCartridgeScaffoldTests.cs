namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cartridges;
using Xunit;

/// <summary>
/// FR/TR: FR-CART-AR, FR-CART-EASYFLASH, FR-CART-SS5, FR-CART-RRNET,
/// BACKFILL-CART-001. Minimum-viable scaffold tests for four advanced
/// cartridge mappers landed in this slice: Action Replay V4/V5 (CRT type 1),
/// EasyFlash (CRT type 32), Super Snapshot V5 (CRT type 4), RR-Net
/// (CRT type 25). Each mapper supports bank switching and a hide bit;
/// freeze ROMs, LED/NMI lines, flash writes, and Ethernet I/O are deferred
/// to follow-up deepening slices.
/// </summary>
public sealed class AdvancedCartridgeScaffoldTests
{
    private const int BankSize = StandardCartridgeImage.RomBankSize;

    // ----------- Action Replay -----------

    /// <summary>
    /// FR-CART-AR: 32K (4 banks of 8K) Action Replay. Bank reg at $DE00:
    /// bits 0-3 = bank, bit 7 = hide.
    /// </summary>
    [Theory]
    [InlineData(0x00, 0x00)]
    [InlineData(0x01, 0x10)]
    [InlineData(0x02, 0x20)]
    [InlineData(0x03, 0x30)]
    public void ActionReplay_BankSwitching(byte writeValue, byte expectedSentinel)
    {
        var image = BuildBankImage8K(4);
        var (machine, port) = CreateMachine();

        port.AttachCartridge(image, CartridgeMappingMode.ActionReplay);
        machine.Bus.Write(0xDE00, writeValue);

        machine.Bus.Read(0x8010).Should().Be(expectedSentinel);
    }

    [Fact]
    public void ActionReplay_Bit7_Hides()
    {
        var image = BuildBankImage8K(4);
        var (machine, port) = CreateMachine();
        SeedRam(machine, 0x8010, 0xA5);

        port.AttachCartridge(image, CartridgeMappingMode.ActionReplay);
        machine.Bus.Read(0x8010).Should().Be((byte)0x00);

        machine.Bus.Write(0xDE00, 0x80);
        machine.Bus.Read(0x8010).Should().Be((byte)0xA5);
    }

    [Theory]
    [InlineData(BankSize)]              // 8K  too small
    [InlineData(BankSize * 8)]          // 64K too large
    public void ActionReplay_InvalidSize_Throws(int size)
    {
        var (_, port) = CreateMachine();
        Action act = () => port.AttachCartridge(new byte[size], CartridgeMappingMode.ActionReplay);
        act.Should().Throw<ArgumentException>();
    }

    // ----------- EasyFlash -----------

    /// <summary>
    /// FR-CART-EASYFLASH: 64K..1024K image (8..128 banks of 8K). Bank reg
    /// at $DE00 bits 0-5.
    /// </summary>
    [Theory]
    [InlineData(0x00, 0x00)]
    [InlineData(0x04, 0x40)]
    [InlineData(0x07, 0x70)]
    public void EasyFlash_BankSwitching(byte writeValue, byte expectedSentinel)
    {
        var image = BuildBankImage8K(8);   // 64K
        var (machine, port) = CreateMachine();

        port.AttachCartridge(image, CartridgeMappingMode.EasyFlash);
        machine.Bus.Write(0xDE00, writeValue);

        machine.Bus.Read(0x8010).Should().Be(expectedSentinel);
    }

    [Theory]
    [InlineData(BankSize)]              // 8K  too small
    [InlineData(BankSize * 3)]          // 24K too small + odd
    [InlineData(BankSize * 8 + 1)]      // off by one
    public void EasyFlash_InvalidSize_Throws(int size)
    {
        var (_, port) = CreateMachine();
        Action act = () => port.AttachCartridge(new byte[size], CartridgeMappingMode.EasyFlash);
        act.Should().Throw<ArgumentException>();
    }

    // ----------- Super Snapshot V5 -----------

    /// <summary>
    /// FR-CART-SS5: 64K image (4 banks of 16K, ROML+ROMH). Bank reg at
    /// $DE00 bits 2-4; bit 1 hides.
    /// </summary>
    [Theory]
    [InlineData(0b0_000_00,  0x00, 0xC0)]  // bank 0
    [InlineData(0b0_001_00,  0x10, 0xD0)]  // bank 1
    [InlineData(0b0_010_00,  0x20, 0xE0)]  // bank 2
    [InlineData(0b0_011_00,  0x30, 0xF0)]  // bank 3
    public void SuperSnapshot_BankSwitching(byte writeValue, byte romlSentinel, byte romhSentinel)
    {
        var image = BuildBankImage16K(4);
        var (machine, port) = CreateMachine();

        port.AttachCartridge(image, CartridgeMappingMode.SuperSnapshotV5);
        machine.Bus.Write(0xDE00, writeValue);

        machine.Bus.Read(0x8010).Should().Be(romlSentinel);
        machine.Bus.Read(0xA010).Should().Be(romhSentinel);
    }

    [Fact]
    public void SuperSnapshot_Bit1_Hides()
    {
        var image = BuildBankImage16K(4);
        var (machine, port) = CreateMachine();
        SeedRam(machine, 0x8010, 0x33);

        port.AttachCartridge(image, CartridgeMappingMode.SuperSnapshotV5);
        machine.Bus.Read(0x8010).Should().Be((byte)0x00);

        machine.Bus.Write(0xDE00, 0x02);
        machine.Bus.Read(0x8010).Should().Be((byte)0x33);
    }

    // ----------- RR-Net -----------

    /// <summary>
    /// FR-CART-RRNET: 64K image (4 banks of 16K). Bank reg at $DE00 bits
    /// 0-3, bit 7 hides. Ethernet I/O at $DE00-$DE0F is stubbed.
    /// </summary>
    [Theory]
    [InlineData(0x00, 0x00, 0xC0)]
    [InlineData(0x01, 0x10, 0xD0)]
    [InlineData(0x02, 0x20, 0xE0)]
    [InlineData(0x03, 0x30, 0xF0)]
    public void RRNet_BankSwitching(byte writeValue, byte romlSentinel, byte romhSentinel)
    {
        var image = BuildBankImage16K(4);
        var (machine, port) = CreateMachine();

        port.AttachCartridge(image, CartridgeMappingMode.RRNet);
        machine.Bus.Write(0xDE00, writeValue);

        machine.Bus.Read(0x8010).Should().Be(romlSentinel);
        machine.Bus.Read(0xA010).Should().Be(romhSentinel);
    }

    [Fact]
    public void RRNet_Bit7_Hides()
    {
        var image = BuildBankImage16K(4);
        var (machine, port) = CreateMachine();
        SeedRam(machine, 0x8010, 0xBB);

        port.AttachCartridge(image, CartridgeMappingMode.RRNet);
        machine.Bus.Read(0x8010).Should().Be((byte)0x00);

        machine.Bus.Write(0xDE00, 0x80);
        machine.Bus.Read(0x8010).Should().Be((byte)0xBB);
    }

    // ----------- helpers -----------

    private static (IMachine machine, ICartridgePort port) CreateMachine()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var port = machine.Devices.GetAll<ICartridgePort>().Single();
        return (machine, port);
    }

    private static void SeedRam(IMachine machine, ushort address, byte value)
    {
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory
            ?? throw new InvalidOperationException("no system RAM");
        memory.Span[address] = value;
    }

    private static byte[] BuildBankImage8K(int banks)
    {
        var img = new byte[banks * BankSize];
        for (var b = 0; b < banks; b++)
        {
            img[b * BankSize + 0x10] = (byte)(b * 0x10);
        }
        return img;
    }

    /// <summary>
    /// Build a 16K-banked image (ROML+ROMH). ROML sentinel at $10 = N*0x10;
    /// ROMH sentinel at $10 = 0xC0 + N*0x10.
    /// </summary>
    private static byte[] BuildBankImage16K(int banks)
    {
        var img = new byte[banks * BankSize * 2];
        for (var b = 0; b < banks; b++)
        {
            var bankBase = b * BankSize * 2;
            img[bankBase + 0x10] = (byte)(b * 0x10);
            img[bankBase + BankSize + 0x10] = (byte)(0xC0 + b * 0x10);
        }
        return img;
    }
}
