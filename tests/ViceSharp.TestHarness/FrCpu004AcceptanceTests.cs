namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Pla;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-CPU-004 acceptance tests for the 6510 port at $0000/$0001.
/// Iteration 0. These tests name the requirement under tests/ and assert
/// the criteria already written in the requirement. They do not add criteria.
/// </summary>
public sealed class FrCpu004AcceptanceTests
{
    /// <summary>
    /// FR: FR-CPU-004.
    /// Use case: Address $0000 is the data direction register. A bit written
    /// as 1 makes the matching $0001 bit an output. VICE stores the whole
    /// byte, including bits 6 and 7.
    /// Acceptance: Writing $FF to $0000 reads back $FF. With only bit 0
    /// configured as an output and the data latch clear, $0001 bit 0 reads
    /// 0 and the input bits follow the C64 pull-up mask.
    /// </summary>
    [Fact]
    public void Address0000_IsTheDataDirectionRegister()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        machine.Bus.Write(0x0000, 0xFF);

        Assert.Equal(0xFF, machine.Bus.Read(0x0000));

        machine.Bus.Write(0x0000, 0x01);
        machine.Bus.Write(0x0001, 0x00);

        // Output bit 0 is the latch (0). Input bits come from the C64
        // pull-up 0x17 with bit 0 removed: 0x16.
        Assert.Equal(0x16, machine.Bus.Read(0x0001));
    }

    /// <summary>
    /// FR: FR-CPU-004.
    /// Use case: Bits 0-2 of $0001 are LORAM, HIRAM, and CHAREN. Changing
    /// them changes PLA banking for the next CPU read, with no extra delay.
    /// Acceptance: Clearing LORAM makes $A000 read the RAM written under
    /// BASIC. Restoring $37 makes the BASIC ROM byte visible again on the
    /// following read.
    /// </summary>
    [Fact]
    public void PortBits0Through2_ChangePlaBankingOnTheNextRead()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var basicRomFirstByte = MachineTestFactory.LoadC64Rom("basic").Span[0];

        machine.Bus.Write(0xA000, 0x42);
        Assert.Equal(basicRomFirstByte, machine.Bus.Read(0xA000));

        machine.Bus.Write(0x0001, 0x36);

        Assert.Equal(0x42, machine.Bus.Read(0xA000));

        machine.Bus.Write(0x0001, 0x37);

        Assert.Equal(basicRomFirstByte, machine.Bus.Read(0xA000));
    }

    /// <summary>
    /// FR: FR-CPU-004.
    /// Use case: The written criterion names bit 3. VICE c64pla.c drives
    /// the cassette motor from bit 5: tapeport_set_motor runs when
    /// ((dir &amp; data) &amp; 0x20) changes, and the motor is on when that
    /// mask is 0. This fact follows x64sc.
    /// Acceptance: With bit 5 configured as an output, a 0 turns the
    /// datasette motor on and a 1 turns it off.
    /// </summary>
    [Fact]
    public void PortBit3_ControlsTheCassetteMotorActiveLow()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var datasette = Assert.IsType<Datasette>(machine.Devices.GetAll<ITapeDevice>().Single());

        machine.Bus.Write(0x0000, 0x2F);
        machine.Bus.Write(0x0001, 0x17);

        Assert.True(datasette.MotorEnabled);

        machine.Bus.Write(0x0001, 0x37);

        Assert.False(datasette.MotorEnabled);
    }

    /// <summary>
    /// FR: FR-CPU-004.
    /// Use case: Bit 4 reads the cassette switch sense and is active low.
    /// Acceptance: With bit 4 configured as an input, PLAY pressed makes
    /// $0001 bit 4 read 0. PLAY released makes bit 4 read 1.
    /// </summary>
    [Fact]
    public void PortBit4_ReadsCassetteSenseActiveLow()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var datasette = Assert.IsType<Datasette>(machine.Devices.GetAll<ITapeDevice>().Single());

        machine.Bus.Write(0x0000, 0x0F);

        datasette.PlayPressed = false;
        Assert.True(datasette.SenseLine);
        Assert.Equal(0x10, machine.Bus.Read(0x0001) & 0x10);

        datasette.PlayPressed = true;
        Assert.False(datasette.SenseLine);
        Assert.Equal(0x00, machine.Bus.Read(0x0001) & 0x10);
    }

    /// <summary>
    /// FR: FR-CPU-004.
    /// Use case: Bit 5 is unused on the C64. As an input it reads the
    /// external pull-up, not the data latch.
    /// Acceptance: After DDR bit 5 is cleared, writing the data latch does
    /// not set bit 5. The read follows the board pull-up, whose bit 5 is 0.
    /// </summary>
    [Fact]
    public void PortBit5_InputReadFollowsPullUpNotTheLatch()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        machine.Bus.Write(0x0001, 0x3F);
        machine.Bus.Write(0x0000, 0x0F);

        Assert.Equal(0x1F, machine.Bus.Read(0x0001));
        Assert.Equal(0x00, machine.Bus.Read(0x0001) & 0x20);
    }

    /// <summary>
    /// FR: FR-CPU-004.
    /// Use case: When a port bit changes from output to input, the pin
    /// keeps the level it was driving and decays toward the pull-up over
    /// approximately 350 microseconds.
    /// Acceptance: Bit 5 driven low, then made an input against a pull-up,
    /// still reads 0 on the first sample. The decay interval has not elapsed.
    /// </summary>
    [Fact]
    public void OutputToInput_HoldsTheDrivenLevelBeforeDischarge()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var pla = Assert.IsType<Mos906114>(machine.Devices.GetByRole(DeviceRole.Pla));
        pla.InputPullUp = (byte)(pla.InputPullUp | 0x20);

        machine.Bus.Write(0x0000, 0x3F);
        machine.Bus.Write(0x0001, 0x17);
        Assert.Equal(0x00, machine.Bus.Read(0x0001) & 0x20);

        machine.Bus.Write(0x0000, 0x1F);

        Assert.Equal(0x00, machine.Bus.Read(0x0001) & 0x20);
    }

    /// <summary>
    /// FR: FR-CPU-004.
    /// Use case: Bits 6 and 7 float. When either bit changes from output
    /// to input it keeps the level it was driving, then discharges.
    /// Acceptance: Bit 6 driven high still reads 1 on the first sample
    /// after it becomes an input. After VICE's falloff interval it reads 0.
    /// Bit 5 is not this capacitor on a C64.
    /// </summary>
    [Fact]
    public void OutputToInput_Bits6And7HoldThenDischarge()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        machine.Bus.Write(0x0000, 0xFF);
        machine.Bus.Write(0x0001, 0x40);
        Assert.Equal(0x40, machine.Bus.Read(0x0001) & 0x40);

        machine.Bus.Write(0x0000, 0x3F);
        Assert.Equal(0x40, machine.Bus.Read(0x0001) & 0x40);

        machine.Clock.Step(350000 - 1);
        Assert.Equal(0x40, machine.Bus.Read(0x0001) & 0x40);

        machine.Clock.Step(1);
        Assert.Equal(0x00, machine.Bus.Read(0x0001) & 0x40);
    }

    /// <summary>
    /// FR: FR-CPU-004.
    /// Use case: The processor port is what the CPU sees at $0000 and $0001.
    /// The address decoder must not return the RAM byte underneath.
    /// Acceptance: After the RAM byte at $0000 is changed without a port
    /// write, a bus read of $0000 still returns the data direction register.
    /// </summary>
    [Fact]
    public void Port_IsWhatTheBusReadsAt0000And0001()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var ram = Assert.IsAssignableFrom<IMemory>(machine.Devices.GetByRole(DeviceRole.SystemRam));
        var direction = machine.Bus.Read(0x0000);

        ram.Span[0x0000] = 0xA5;

        Assert.NotEqual(0xA5, machine.Bus.Read(0x0000));
        Assert.Equal(direction, machine.Bus.Read(0x0000));
    }
}
