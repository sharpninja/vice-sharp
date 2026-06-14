namespace ViceSharp.TestHarness;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cia;
using ViceSharp.Core.Input;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-INPUT-CONTROLPORT (BACKFILL-INPUT-001 joystick scan).
/// Use case: On a real C64 the two digital joystick control ports
/// share CIA1's I/O port pins with the keyboard matrix. Joystick
/// port 2 sits on CIA1 PA ($DC00) and joystick port 1 sits on CIA1
/// PB ($DC01); each contributes five active-low direction/fire bits
/// (Up=0, Down=1, Left=2, Right=3, Fire=4) and leaves the upper
/// three bits high. This fixture wires a focused CIA1 + two
/// <c>C64JoystickPort</c> instances + an idle <c>C64KeyboardMatrix</c>
/// (mirroring the production wiring in <c>C64MemoryMap</c>) and
/// exercises the joystick-to-port-bits path end to end without
/// standing up a full machine.
/// Acceptance: Neutral baseline reads PA = PB = 0xFF; a single
/// direction or fire press on port 2 pulls exactly its bit low in
/// PA while PB stays high; a press on port 1 routes to PB and
/// leaves PA untouched; multiple simultaneous bits compose as a
/// bitwise AND of their individual active-low patterns.
/// </summary>
public sealed class Cia1JoystickScanTests
{
    private static (Mos6526 cia, C64JoystickPort port1, C64JoystickPort port2) BuildCia1WithJoysticks()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var cia = new Mos6526(bus, irq) { BaseAddress = 0xDC00 };
        var keyboard = new C64KeyboardMatrix();
        var port1 = new C64JoystickPort();
        var port2 = new C64JoystickPort();

        keyboard.Initialize();
        port1.Initialize();
        port2.Initialize();
        cia.Reset();

        // Mirror the production wiring in C64MemoryMap: PA returns
        // (keyboard rows) AND (joystick port 2 lines); PB returns
        // (keyboard columns) AND (joystick port 1 lines). With an
        // idle keyboard matrix both row and column reads are 0xFF,
        // so PA/PB observe the joystick lines directly.
        cia.PortAInput = () => (byte)(keyboard.ReadRowState() & port2.ReadPortState());
        cia.PortBInput = () => (byte)(keyboard.ReadColumnState() & port1.ReadPortState());

        return (cia, port1, port2);
    }

    /// <summary>
    /// FR/TR: FR-INPUT-CONTROLPORT (BACKFILL-INPUT-001 joystick scan).
    /// Use case: With neither joystick touched, CIA1 PA ($DC00) and
    /// PB ($DC01) must both read 0xFF: the upper three bits are
    /// always driven high by the port and the lower five direction
    /// /fire bits are active low, so an idle port leaves every bit
    /// high.
    /// Acceptance: PA and PB both read 0xFF when port 1 and port 2
    /// are at neutral.
    /// </summary>
    [Fact]
    public void NoJoystickInput_PaAndPbReadAllHigh()
    {
        var (cia, _, _) = BuildCia1WithJoysticks();

        cia.Read(0xDC00).Should().Be(0xFF,
            "idle joystick port 2 drives all five direction/fire bits high");
        cia.Read(0xDC01).Should().Be(0xFF,
            "idle joystick port 1 drives all five direction/fire bits high");
    }

    /// <summary>
    /// FR/TR: FR-INPUT-CONTROLPORT (BACKFILL-INPUT-001 joystick scan).
    /// Use case: A C64 joystick is wired active low: pushing Up on
    /// joystick port 2 grounds the Up line, which clears CIA1 PA
    /// bit 0 while leaving every other bit high.
    /// Acceptance: With port 2 Up asserted, PA reads 0xFE (bit 0
    /// clear, bits 1-7 high) and PB stays at 0xFF.
    /// </summary>
    [Fact]
    public void Joystick2_UpPressed_PullsPaBit0Low()
    {
        var (cia, _, port2) = BuildCia1WithJoysticks();

        port2.State = C64JoystickPort.JoystickButtons.Up;

        cia.Read(0xDC00).Should().Be(0xFE,
            "joystick 2 Up grounds PA bit 0 (active low) while every other bit stays high");
        cia.Read(0xDC01).Should().Be(0xFF,
            "joystick 1 is idle so PB remains all-high");
    }

    /// <summary>
    /// FR/TR: FR-INPUT-CONTROLPORT (BACKFILL-INPUT-001 joystick scan).
    /// Use case: The fire button on joystick port 2 is wired to bit
    /// 4 of CIA1 PA. Pressing fire alone (no direction) clears bit
    /// 4 while every other PA bit stays high.
    /// Acceptance: With port 2 Fire asserted, PA reads 0xEF (bit 4
    /// clear, all others high) and PB stays at 0xFF.
    /// </summary>
    [Fact]
    public void Joystick2_FirePressed_PullsPaBit4Low()
    {
        var (cia, _, port2) = BuildCia1WithJoysticks();

        port2.State = C64JoystickPort.JoystickButtons.Fire;

        cia.Read(0xDC00).Should().Be(0xEF,
            "joystick 2 Fire grounds PA bit 4 (active low) while every other bit stays high");
        cia.Read(0xDC01).Should().Be(0xFF,
            "joystick 1 is idle so PB remains all-high");
    }

    /// <summary>
    /// FR/TR: FR-INPUT-CONTROLPORT (BACKFILL-INPUT-001 joystick scan).
    /// Use case: Joystick port 1 is routed to CIA1 PB, not PA.
    /// Pushing Down on port 1 must clear PB bit 1 (the Down line)
    /// and leave PA completely untouched. This confirms the two
    /// control ports map to opposite CIA1 register sides.
    /// Acceptance: With port 1 Down asserted, PB reads 0xFD (bit 1
    /// clear, all others high) and PA stays at 0xFF.
    /// </summary>
    [Fact]
    public void Joystick1_DownPressed_PullsPbBit1Low()
    {
        var (cia, port1, _) = BuildCia1WithJoysticks();

        port1.State = C64JoystickPort.JoystickButtons.Down;

        cia.Read(0xDC01).Should().Be(0xFD,
            "joystick 1 Down grounds PB bit 1 (active low) while every other bit stays high");
        cia.Read(0xDC00).Should().Be(0xFF,
            "joystick 2 is idle so PA remains all-high");
    }

    /// <summary>
    /// FR/TR: FR-INPUT-CONTROLPORT (BACKFILL-INPUT-001 joystick scan).
    /// Use case: Multiple active-low joystick lines compose as a
    /// bitwise AND of their individual patterns. Asserting Up +
    /// Left + Fire on port 2 simultaneously clears PA bits 0, 2,
    /// and 4 while leaving bits 1, 3 and the upper three bits high
    /// (PA = 0xEA).
    /// Acceptance: With port 2 Up + Left + Fire asserted, PA reads
    /// 0xEA.
    /// </summary>
    [Fact]
    public void Joystick2_UpLeftFire_PullsPaBits0_2_4Low()
    {
        var (cia, _, port2) = BuildCia1WithJoysticks();

        port2.State =
            C64JoystickPort.JoystickButtons.Up
            | C64JoystickPort.JoystickButtons.Left
            | C64JoystickPort.JoystickButtons.Fire;

        cia.Read(0xDC00).Should().Be(0xEA,
            "Up + Left + Fire on joystick 2 clears PA bits 0, 2 and 4 simultaneously");
        cia.Read(0xDC01).Should().Be(0xFF,
            "joystick 1 is idle so PB remains all-high");
    }
}
