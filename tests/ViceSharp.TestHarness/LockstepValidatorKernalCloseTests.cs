namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using Xunit;

public sealed class LockstepValidatorKernalCloseTests
{
    /// <summary>
    /// FR: BACKFILL-LOCKSTEP-001, TR: TR-CYCLE-001.
    /// Use case: The x64sc D64 isolation window must subscribe to the KERNAL
    /// CLOSE routine call, not merely the public KERNAL jump-table address.
    /// Acceptance: The C64 public CLOSE vector at $FFC3 is resolved through
    /// its indirect <c>JMP ($031C)</c> vector to the live close routine target.
    /// </summary>
    [Fact]
    public void ResolveKernalJumpTarget_FollowsC64CloseIndirectVector()
    {
        var memory = new byte[0x10000];
        memory[0xFFC3] = 0x6C; // JMP ($031C)
        memory[0xFFC4] = 0x1C;
        memory[0xFFC5] = 0x03;
        memory[0x031C] = 0x91;
        memory[0x031D] = 0xF2;
        var bus = new BasicBus();
        bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, memory));

        var target = LockstepValidator.ResolveKernalJumpTarget(bus, 0xFFC3);

        Assert.Equal((ushort)0xF291, target);
    }
}
