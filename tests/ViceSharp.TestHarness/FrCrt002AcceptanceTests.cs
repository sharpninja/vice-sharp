namespace ViceSharp.TestHarness;

using System.Buffers.Binary;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-CRT-002 acceptance tests for Ocean Type 1. Iteration 2.
/// </summary>
public sealed class FrCrt002AcceptanceTests
{
    private const int BankSize = StandardCartridgeImage.RomBankSize;

    /// <summary>
    /// FR: FR-CRT-002.
    /// Use case: A write to $DE00 selects the Ocean bank from bits 0-5.
    /// Acceptance: Banks 0 and 63 of a 512KB image appear at $8000.
    /// </summary>
    [Fact]
    public void WriteDe00_SelectsBankFromBits0Through5()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var image = BankImage(64);
        Port(machine).AttachCartridge(image, CartridgeMappingMode.Ocean);

        machine.Bus.Write(0xDE00, 0x00);
        Assert.Equal(0x00, machine.Bus.Read(0x8000));

        machine.Bus.Write(0xDE00, 0x3F);
        Assert.Equal(0x3F, machine.Bus.Read(0x8000));

        machine.Bus.Write(0xDE00, 0xFF);
        Assert.Equal(0x3F, machine.Bus.Read(0x8000));
    }

    /// <summary>
    /// FR: FR-CRT-002.
    /// Use case: The selected bank is mapped at ROML, $8000-$9FFF.
    /// VICE ocean.c maps that same bank at ROMH for every size except
    /// 512KB. A 512KB image is the 8K type B config, so $A000 is not the
    /// bank byte.
    /// Acceptance: Bank 1 of a 32KB image is at $9FFF and, per
    /// ocean_romh_read, at $A000. Bank 1 of a 512KB image stops at $9FFF.
    /// </summary>
    [Fact]
    public void SelectedBank_IsMappedAt8000Through9FFF()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        Port(machine).AttachCartridge(BankImage(4), CartridgeMappingMode.Ocean);
        machine.Bus.Write(0xDE00, 0x01);

        Assert.Equal(0x01, machine.Bus.Read(0x9FFF));
        Assert.Equal(0x01, machine.Bus.Read(0xA000));

        var wide = MachineTestFactory.CreateC64Machine();
        Port(wide).AttachCartridge(BankImage(64), CartridgeMappingMode.Ocean);
        wide.Bus.Write(0xDE00, 0x01);

        Assert.Equal(0x01, wide.Bus.Read(0x9FFF));
        Assert.NotEqual(0x01, wide.Bus.Read(0xA000));
    }

    /// <summary>
    /// FR: FR-CRT-002.
    /// Use case: Some Ocean cartridges also map ROMH.
    /// Acceptance: An Ocean image that carries a ROMH bank is readable at
    /// $A000 after the bank write.
    /// </summary>
    [Fact]
    public void RomH_CanBePresent()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var image = new byte[BankSize * 2];
        image[0] = 0x11;
        image[BankSize] = 0x22;
        Port(machine).AttachCartridge(image, CartridgeMappingMode.Ocean);
        machine.Bus.Write(0xDE00, 0x01);

        Assert.Equal(0x22, machine.Bus.Read(0xA000));
    }

    /// <summary>
    /// FR: FR-CRT-002.
    /// Use case: The maximum Ocean image is 512KB, 64 banks of 8KB.
    /// Acceptance: 64 banks attach. 65 banks are rejected.
    /// </summary>
    [Fact]
    public void MaximumSize_Is512K()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var port = Port(machine);

        var accepted = Record.Exception(() => port.AttachCartridge(BankImage(64), CartridgeMappingMode.Ocean));
        Assert.Null(accepted);

        var rejected = Record.Exception(() => port.AttachCartridge(BankImage(65), CartridgeMappingMode.Ocean));
        Assert.IsType<ArgumentException>(rejected);
    }

    /// <summary>
    /// FR: FR-CRT-002.
    /// Use case: Ocean Type 1 asserts EXROM and releases GAME, so ROML is
    /// mapped and KERNAL stays in place.
    /// Acceptance: $8000 reads the cart byte and $E000 still reads KERNAL.
    /// </summary>
    [Fact]
    public void ExromAndGame_MapRomLAndLeaveKernal()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var kernal = machine.Bus.Read(0xE000);
        Port(machine).AttachCartridge(BankImage(4), CartridgeMappingMode.Ocean);

        Assert.Equal(0x00, machine.Bus.Read(0x8000));
        Assert.Equal(kernal, machine.Bus.Read(0xE000));
    }

    /// <summary>
    /// FR: FR-CRT-002.
    /// Use case: A CRT CHIP packet names the Ocean bank it carries.
    /// Acceptance: A type 5 CRT with bank 0 byte $11 and bank 1 byte $22
    /// maps bank 1 to $8000 after a write of 1 to $DE00.
    /// </summary>
    [Fact]
    public void CrtChipPackets_IdentifyBankNumbers()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var crt = OceanCrt((0, 0x11), (1, 0x22));

        var error = Record.Exception(() => Port(machine).AttachCartridge(crt, CartridgeMappingMode.Ocean));
        Assert.Null(error);
        machine.Bus.Write(0xDE00, 0x01);
        Assert.Equal(0x22, machine.Bus.Read(0x8000));
    }

    private static ICartridgePort Port(IMachine machine)
        => machine.Devices.GetAll<ICartridgePort>().Single();

    private static byte[] BankImage(int banks)
    {
        var image = new byte[banks * BankSize];
        for (var bank = 0; bank < banks; bank++)
        {
            image[bank * BankSize] = (byte)bank;
            image[bank * BankSize + BankSize - 1] = (byte)bank;
        }

        return image;
    }

    private static byte[] OceanCrt(params (int Bank, byte Marker)[] banks)
    {
        var header = new byte[0x40];
        "C64 CARTRIDGE   "u8.CopyTo(header);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0x10), 0x40);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0x16), 5);
        var packet = 0x10 + BankSize;
        var image = new byte[header.Length + (packet * banks.Length)];
        header.CopyTo(image, 0);
        for (var i = 0; i < banks.Length; i++)
        {
            var at = header.Length + (i * packet);
            "CHIP"u8.CopyTo(image.AsSpan(at));
            BinaryPrimitives.WriteInt32BigEndian(image.AsSpan(at + 4), packet);
            BinaryPrimitives.WriteUInt16BigEndian(image.AsSpan(at + 0x0A), (ushort)banks[i].Bank);
            BinaryPrimitives.WriteUInt16BigEndian(image.AsSpan(at + 0x0C), 0x8000);
            BinaryPrimitives.WriteUInt16BigEndian(image.AsSpan(at + 0x0E), (ushort)BankSize);
            image[at + 0x10] = banks[i].Marker;
        }

        return image;
    }
}
