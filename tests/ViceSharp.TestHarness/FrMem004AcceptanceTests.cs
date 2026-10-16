namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using Xunit;

/// <summary>
/// FR-MEM-004 acceptance tests. Iteration 1. VIC bank selection is observed
/// through the VIC-II video memory reader after a CIA2 port A write.
/// </summary>
public sealed class FrMem004AcceptanceTests
{
    /// <summary>
    /// FR: FR-MEM-004.
    /// Use case: CIA2 port A bits 0-1 are active low. The VIC bank is the
    /// inverted pair: 11 is bank 0, 10 is bank 1, 01 is bank 2, 00 is bank 3.
    /// Acceptance: With bits 0-1 as outputs, each write makes VIC address 2
    /// read the marker stored at that bank's CPU base.
    /// </summary>
    [Fact]
    public void Cia2PortABits0And1_SelectTheVicBankActiveLow()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Bus.Write(0x0002, 0x10);
        machine.Bus.Write(0x4002, 0x11);
        machine.Bus.Write(0x8002, 0x12);
        machine.Bus.Write(0xC002, 0x13);
        machine.Bus.Write(0xDD02, 0x03);
        var vic = Vic(machine);

        machine.Bus.Write(0xDD00, 0x03);
        Assert.Equal(0x10, vic.ReadVideoMemory(0x0002));

        machine.Bus.Write(0xDD00, 0x02);
        Assert.Equal(0x11, vic.ReadVideoMemory(0x0002));

        machine.Bus.Write(0xDD00, 0x01);
        Assert.Equal(0x12, vic.ReadVideoMemory(0x0002));

        machine.Bus.Write(0xDD00, 0x00);
        Assert.Equal(0x13, vic.ReadVideoMemory(0x0002));
    }

    /// <summary>
    /// FR: FR-MEM-004.
    /// Use case: VIC-II addresses are relative to the selected bank start.
    /// Acceptance: The same VIC address 0 reads $4000 in bank 1 and $C000
    /// in bank 3.
    /// </summary>
    [Fact]
    public void VicAddresses_AreRelativeToTheSelectedBank()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Bus.Write(0x4000, 0x21);
        machine.Bus.Write(0xC000, 0x23);
        machine.Bus.Write(0xDD02, 0x03);
        var vic = Vic(machine);

        machine.Bus.Write(0xDD00, 0x02);
        Assert.Equal(0x21, vic.ReadVideoMemory(0));

        machine.Bus.Write(0xDD00, 0x00);
        Assert.Equal(0x23, vic.ReadVideoMemory(0));
    }

    /// <summary>
    /// FR: FR-MEM-004.
    /// Use case: In banks 0 and 2, VIC addresses $1000-$1FFF read character
    /// ROM instead of RAM.
    /// Acceptance: Banks 0 and 2 return the character ROM byte. Bank 1
    /// returns the RAM byte at $5000.
    /// </summary>
    [Fact]
    public void Banks0And2_ReadCharacterRomInThe1000Window()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var chargen = MachineTestFactory.LoadC64Rom("characters").Span[0];
        machine.Bus.Write(0x1000, 0x5A);
        machine.Bus.Write(0x5000, 0x5A);
        machine.Bus.Write(0x9000, 0x5A);
        machine.Bus.Write(0xDD02, 0x03);
        var vic = Vic(machine);

        machine.Bus.Write(0xDD00, 0x03);
        Assert.Equal(chargen, vic.ReadVideoMemory(0x1000));

        machine.Bus.Write(0xDD00, 0x01);
        Assert.Equal(chargen, vic.ReadVideoMemory(0x1000));

        machine.Bus.Write(0xDD00, 0x02);
        Assert.Equal(0x5A, vic.ReadVideoMemory(0x1000));
    }

    /// <summary>
    /// FR: FR-MEM-004.
    /// Use case: A bank write takes effect for the next VIC read.
    /// Acceptance: Writing the bank-3 value makes the following VIC read
    /// return the byte at $C000.
    /// </summary>
    [Fact]
    public void BankSwitch_TakesEffectOnTheNextVicRead()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Bus.Write(0xC000, 0x77);
        machine.Bus.Write(0xDD02, 0x03);
        var vic = Vic(machine);

        machine.Bus.Write(0xDD00, 0x00);

        Assert.Equal(0x77, vic.ReadVideoMemory(0));
    }

    /// <summary>
    /// FR: FR-MEM-004.
    /// Use case: The active bank's base address is what later VIC reads use.
    /// Acceptance: After selecting bank 2, VIC address 0 reads $8000.
    /// </summary>
    [Fact]
    public void ActiveBankAndBaseAddress_AreReported()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Bus.Write(0x8000, 0x66);
        machine.Bus.Write(0xDD02, 0x03);
        machine.Bus.Write(0xDD00, 0x01);

        Assert.Equal(0x66, Vic(machine).ReadVideoMemory(0));
    }

    private static Mos6569 Vic(IMachine machine)
    {
        return Assert.IsType<Mos6569>(machine.Devices.GetByRole(DeviceRole.VideoChip));
    }
}
