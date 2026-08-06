namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// VIA1 Port A IEC idle levels must match VICE xvic (vic20iec.c / vic20via1.c).
/// </summary>
/// <remarks>
/// Use case: every-cycle lockstep residual c=522370 LDA $911F (m=$7F n=$7E).
/// Acceptance: after Vic20 machine create/reset, peek/read $911F is $7E with
/// released joystick and no tape (VICE idle open-collector bus).
/// </remarks>
public sealed class Vic20Via1IecPortTests
{
    [Fact]
    public void Via1_PortA_Idle_MatchesViceIecPaRead_7E()
    {
        var machine = MachineTestFactory.CreateVic20Machine("vic20");
        machine.Reset();

        // $911F = ORA no-handshake (same port A data as $9111 without CA clear).
        var via1 = machine.Devices.GetAll<Via6522>().First(v => v.BaseAddress == 0x9110);
        var peekF = via1.Peek(0x911F);
        var readF = via1.Read(0x911F);

        Assert.Equal(0x7E, peekF);
        Assert.Equal(0x7E, readF);
        Assert.Equal(0x7E, machine.Bus.Peek(0x911F));
    }
}
