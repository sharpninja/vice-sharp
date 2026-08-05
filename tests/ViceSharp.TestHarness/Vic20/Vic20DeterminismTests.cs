namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using Xunit;

/// <summary>
/// Bounded multi-frame determinism substitute for native xvic lockstep when the
/// oracle binary is unavailable: two independently built machines with identical
/// ROMs must stay bit-identical on PC, A, and sample memory after N frames.
/// </summary>
public sealed class Vic20DeterminismTests
{
    [Fact]
    public void TwoMachines_SameFrames_ProduceIdenticalState()
    {
        var a = MachineTestFactory.CreateVic20Machine();
        var b = MachineTestFactory.CreateVic20Machine();

        const int frames = 32;
        for (var i = 0; i < frames; i++)
        {
            a.RunFrame();
            b.RunFrame();
        }

        var sa = a.GetState();
        var sb = b.GetState();
        Assert.Equal(sa.PC, sb.PC);
        Assert.Equal(sa.A, sb.A);
        Assert.Equal(sa.X, sb.X);
        Assert.Equal(sa.Y, sb.Y);
        Assert.Equal(sa.Cycle, sb.Cycle);

        // Sample bus fingerprint across screen + zero page + ROM
        for (ushort addr = 0x0000; addr < 0x0100; addr++)
            Assert.Equal(a.Bus.Peek(addr), b.Bus.Peek(addr));
        for (ushort addr = 0x1E00; addr < 0x1E00 + 64; addr++)
            Assert.Equal(a.Bus.Peek(addr), b.Bus.Peek(addr));
        Assert.Equal(a.Bus.Peek(0xC000), b.Bus.Peek(0xC000));
        Assert.Equal(a.Bus.Peek(0xE000), b.Bus.Peek(0xE000));
    }

    [Fact]
    public void SingleMachine_ReplayFromReset_MatchesPriorRun()
    {
        var first = CaptureFingerprint(32);
        var second = CaptureFingerprint(32);
        Assert.Equal(first, second);
    }

    private static string CaptureFingerprint(int frames)
    {
        var m = MachineTestFactory.CreateVic20Machine();
        for (var i = 0; i < frames; i++)
            m.RunFrame();
        var s = m.GetState();
        return $"{s.PC:X4}:{s.A:X2}:{s.X:X2}:{s.Y:X2}:{s.Cycle}:{m.Bus.Peek(0x1E00):X2}:{m.Bus.Peek(0x1E01):X2}";
    }
}
