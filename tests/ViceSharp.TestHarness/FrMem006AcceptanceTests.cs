namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using Xunit;

/// <summary>
/// FR-MEM-006 acceptance tests for zero page and the stack page.
/// Iteration 0. Criteria are copied from the requirement. No new cycle
/// counts are introduced.
/// </summary>
public sealed class FrMem006AcceptanceTests
{
    /// <summary>
    /// FR: FR-MEM-006.
    /// Use case: Addresses $0002 through $00FF are ordinary RAM.
    /// Acceptance: Bytes written at $0002 and $00FF read back unchanged.
    /// </summary>
    [Fact]
    public void ZeroPageAboveThePort_IsRam()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        machine.Bus.Write(0x0002, 0x5A);
        machine.Bus.Write(0x00FF, 0xA5);

        Assert.Equal(0x5A, machine.Bus.Read(0x0002));
        Assert.Equal(0xA5, machine.Bus.Read(0x00FF));
    }

    /// <summary>
    /// FR: FR-MEM-006.
    /// Use case: $0000 and $0001 are the 6510 port, per FR-CPU-004, not the
    /// RAM bytes at those addresses.
    /// Acceptance: Changing only the RAM byte at $0001 leaves the bus read
    /// equal to the processor port.
    /// </summary>
    [Fact]
    public void Addresses0000And0001_AreTheProcessorPort()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var ram = Assert.IsAssignableFrom<IMemory>(machine.Devices.GetByRole(DeviceRole.SystemRam));
        var port = machine.Bus.Read(0x0001);

        ram.Span[0x0001] = (byte)(port ^ 0xFF);

        Assert.Equal(port, machine.Bus.Read(0x0001));
    }

    /// <summary>
    /// FR: FR-MEM-006.
    /// Use case: Stack accesses use $0100 plus the 8-bit stack pointer.
    /// The pointer wraps inside that page.
    /// Acceptance: PHA with S equal to 0 writes A to $0100 and leaves S
    /// equal to $FF. No address outside $0100-$01FF is written.
    /// </summary>
    [Fact]
    public void Pha_WithStackPointerZero_WrapsInsideTheStackPage()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var cpu = Cpu(machine);
        var ram = Assert.IsAssignableFrom<IMemory>(machine.Devices.GetByRole(DeviceRole.SystemRam));
        var below = ram.Span[0x00FF];
        var above = ram.Span[0x0200];

        machine.Bus.Write(0xC000, 0x48);
        cpu.PC = 0xC000;
        cpu.A = 0x5A;
        cpu.S = 0x00;

        cpu.ExecuteInstruction();

        Assert.Equal(0x5A, machine.Bus.Read(0x0100));
        Assert.Equal(0xFF, cpu.S);
        Assert.Equal(below, ram.Span[0x00FF]);
        Assert.Equal(above, ram.Span[0x0200]);
    }

    /// <summary>
    /// FR: FR-MEM-006.
    /// Use case: JSR pushes the address of the last byte of the JSR
    /// instruction, which is PC+2, high byte first and then low byte.
    /// Acceptance: JSR $9000 at $C000 leaves $C0 at $01FF and $02 at $01FE,
    /// S equal to $FD, and PC equal to $9000.
    /// </summary>
    [Fact]
    public void Jsr_PushesPcPlusTwoHighByteFirst()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var cpu = Cpu(machine);

        machine.Bus.Write(0xC000, 0x20);
        machine.Bus.Write(0xC001, 0x00);
        machine.Bus.Write(0xC002, 0x90);
        cpu.PC = 0xC000;
        cpu.S = 0xFF;

        cpu.ExecuteInstruction();

        Assert.Equal(0xC0, machine.Bus.Read(0x01FF));
        Assert.Equal(0x02, machine.Bus.Read(0x01FE));
        Assert.Equal(0xFD, cpu.S);
        Assert.Equal(0x9000, cpu.PC);
    }

    /// <summary>
    /// FR: FR-MEM-006.
    /// Use case: RTS pulls the low byte first, then the high byte, and adds
    /// one to that address.
    /// Acceptance: With $02 at $01FE and $C0 at $01FF, RTS leaves PC at
    /// $C003 and S at $FF.
    /// </summary>
    [Fact]
    public void Rts_PullsLowByteFirstThenAddsOne()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var cpu = Cpu(machine);

        machine.Bus.Write(0x9000, 0x60);
        machine.Bus.Write(0x01FE, 0x02);
        machine.Bus.Write(0x01FF, 0xC0);
        cpu.PC = 0x9000;
        cpu.S = 0xFD;

        cpu.ExecuteInstruction();

        Assert.Equal(0xC003, cpu.PC);
        Assert.Equal(0xFF, cpu.S);
    }

    /// <summary>
    /// FR: FR-MEM-006.
    /// Use case: Stack underflow and overflow wrap the 8-bit pointer and
    /// do not raise a host exception.
    /// Acceptance: PLA with S equal to $FF reads $0100 and leaves S equal
    /// to 0. The call returns.
    /// </summary>
    [Fact]
    public void Pla_WithStackPointerFf_WrapsWithoutThrowing()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var cpu = Cpu(machine);

        machine.Bus.Write(0xC000, 0x68);
        machine.Bus.Write(0x0100, 0x3C);
        cpu.PC = 0xC000;
        cpu.S = 0xFF;

        var exception = Record.Exception(() => cpu.ExecuteInstruction());

        Assert.Null(exception);
        Assert.Equal(0x3C, cpu.A);
        Assert.Equal(0x00, cpu.S);
    }

    private static Mos6502 Cpu(IMachine machine)
    {
        return Assert.IsType<Mos6502>(machine.Devices.GetByRole(DeviceRole.Cpu));
    }
}
