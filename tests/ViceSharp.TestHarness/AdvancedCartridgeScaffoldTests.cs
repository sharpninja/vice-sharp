namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Core;
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
    /// FR-CART-AR.
    /// Use case: 32K Action Replay (4 banks of 8K) selects an active ROML
    /// bank via a write to $DE00 (bits 0-3 = bank index, bit 7 = hide).
    /// Acceptance: writing $DE00 with bits 0-3 = N causes the bus read at
    /// $8010 to return the per-bank sentinel byte for each of bank 0..3.
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

    /// <summary>
    /// FR-CART-AR.
    /// Use case: bit 7 of the Action Replay $DE00 register hides the
    /// cartridge so ROML releases and $8000 reads fall through to RAM.
    /// Acceptance: after writing $DE00 = $80 the RAM sentinel ($A5)
    /// shows at $8010 instead of the cart bank-0 sentinel.
    /// </summary>
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

    /// <summary>
    /// FR-CART-AR.
    /// Use case: Action Replay images must be exactly 32K (4 banks of 8K).
    /// Smaller or larger images must be rejected at attach time.
    /// Acceptance: AttachCartridge throws ArgumentException for 8K and 64K
    /// image sizes.
    /// </summary>
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
    /// FR-CART-EASYFLASH.
    /// Use case: EasyFlash (64K..1024K image, 8..128 banks of 8K) selects
    /// an active ROML bank via a write to $DE00; bits 0-5 of the value
    /// are the bank index modulo the bank-count.
    /// Acceptance: writing $DE00 with bits 0-5 = N causes the bus read at
    /// $8010 to return the sentinel for bank N (N in 0, 4, 7 covered).
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

    /// <summary>
    /// FR-CART-EASYFLASH.
    /// Use case: EasyFlash requires at least 64K (8 banks of 8K) up to a
    /// 1024K (128 banks) ceiling; smaller, non-multiple, or off-by-one
    /// images must be rejected at attach time.
    /// Acceptance: AttachCartridge throws ArgumentException for 8K, 24K,
    /// and 64K+1 image sizes.
    /// </summary>
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
    /// FR-CART-SS5.
    /// Use case: Super Snapshot V5 (64K, 4 banks of 16K with ROML+ROMH)
    /// selects an active 16K bank via a write to $DE00 with the bank
    /// index in bits 2-4 (not 0-1).
    /// Acceptance: writing $DE00 with the bank field set causes the bus
    /// reads at $8010 (ROML) and $A010 (ROMH) to return the per-bank
    /// sentinels for each of bank 0..3.
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

    /// <summary>
    /// FR-CART-SS5.
    /// Use case: bit 1 of the Super Snapshot V5 $DE00 register hides the
    /// cartridge so ROML releases and $8000 reads fall through to RAM.
    /// Acceptance: after writing $DE00 = $02 the seeded RAM byte ($33)
    /// shows at $8010 instead of the bank-0 cart sentinel.
    /// </summary>
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
    /// FR-CART-RRNET.
    /// Use case: RR-Net (64K, 4 banks of 16K with ROML+ROMH) selects an
    /// active 16K bank via a write to $DE00 with bits 0-3 = bank index.
    /// The Ethernet I/O window at $DE00-$DE0F is stubbed in this slice.
    /// Acceptance: writing $DE00 with bits 0-3 = N causes the bus reads
    /// at $8010 (ROML) and $A010 (ROMH) to return the per-bank sentinels
    /// for each of bank 0..3.
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

    /// <summary>
    /// FR-CART-RRNET.
    /// Use case: bit 7 of the RR-Net $DE00 register hides the cartridge
    /// so ROML releases and $8000 reads fall through to RAM.
    /// Acceptance: after writing $DE00 = $80 the seeded RAM byte ($BB)
    /// shows at $8010 instead of the bank-0 cart sentinel.
    /// </summary>
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
