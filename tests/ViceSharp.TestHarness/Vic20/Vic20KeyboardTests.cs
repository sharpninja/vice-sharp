namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Chips.IEC;
using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FR-VIC20-003, FR-VIC20-004. Keyboard matrix and joystick via VIA ports.
/// </summary>
public sealed class Vic20KeyboardTests
{
    [Fact]
    public void Matrix_SetKey_AppearsInScanRowsForColumns()
    {
        var matrix = new Vic20KeyboardMatrix();
        // Press key at row 0, column 0 (digit '1' on VIC-20 matrix)
        matrix.SetKey(0, 0, true);

        // Column 0 driven low -> row 0 bit low
        var rows = matrix.ScanRowsForColumns(columnSelectActiveLow: 0xFE);
        Assert.Equal(0xFE, rows); // bit 0 cleared

        matrix.SetKey(0, 0, false);
        rows = matrix.ScanRowsForColumns(0xFE);
        Assert.Equal(0xFF, rows);
    }

    [Fact]
    public void Machine_KeyboardInject_VisibleOnVia2PortA()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var matrix = machine.Devices.GetAll<Vic20KeyboardMatrix>().Single();
        var via2 = machine.Devices.GetAll<Via6522>().Single(v => v.BaseAddress == 0x9120);

        // Select column 0: DDRB=$FF, ORB=$FE (drive bit0 low)
        machine.Bus.Write(0x9122, 0xFF); // DDRB
        machine.Bus.Write(0x9120, 0xFE); // ORB

        matrix.SetKey(1, 0, true); // row1 col0 = '3'

        var pra = machine.Bus.Read(0x9121); // ORA/IRA
        Assert.True((pra & 0x02) == 0, $"expected row1 low, PRA=${pra:X2}");
    }

    [Fact]
    public void Joystick_Right_ClearsVia2PortBBit7()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var matrix = machine.Devices.GetAll<Vic20KeyboardMatrix>().Single();

        matrix.JoystickMask = 0x08; // right
        // DDRB input (0) so PortBInput contributes
        machine.Bus.Write(0x9122, 0x00);
        var prb = machine.Bus.Read(0x9120);
        Assert.True((prb & 0x80) == 0, $"expected PB7 low for right, PRB=${prb:X2}");
    }

    [Fact]
    public void DualVia_Windows_RemainAt9110And9120()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var vias = machine.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).ToArray();
        Assert.Equal(2, vias.Length);
        Assert.Equal(0x9110, vias[0].BaseAddress);
        Assert.Equal(0x9120, vias[1].BaseAddress);
    }
}
