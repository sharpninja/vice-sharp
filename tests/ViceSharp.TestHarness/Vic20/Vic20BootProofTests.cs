namespace ViceSharp.TestHarness.Vic20;

using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FR-PRF-005 / FR-VIC20-002. READY-like fingerprint after boot with real ROMs.
/// </summary>
public sealed class Vic20BootProofTests
{
    /// <summary>
    /// Use case: unexpanded VIC-20 with official ROMs boots through KERNAL/BASIC
    /// and paints READY. into screen RAM.
    /// Acceptance: within 400 frames, screen codes 18,5,1,4,25 appear in $1000-$1FFF.
    /// </summary>
    [Fact]
    public void Vic20_Boot_Reaches_Ready_Prompt()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        const int maxFrames = 400;

        for (var frame = 0; frame < maxFrames; frame++)
        {
            machine.RunFrame();
            if (TryFindReady(machine, out var addr))
            {
                Assert.InRange(addr, 0x1000, 0x1FFF);
                return;
            }
        }

        Assert.Fail(DumpBootFailure(machine, maxFrames));
    }

    [Fact]
    public void Vic20_AfterReset_RomsAndResetVector_FromProductionBuilder()
    {
        var machine = MachineTestFactory.CreateVic20Machine();
        var basic = MachineTestFactory.LoadVic20Rom("basic-901486-01.bin").Span;
        var kernal = MachineTestFactory.LoadVic20Rom("kernal.901486-07.bin").Span;

        Assert.Equal(basic[0], machine.Bus.Read(0xC000));
        Assert.Equal(kernal[0], machine.Bus.Read(0xE000));
        var lo = machine.Bus.Read(0xFFFC);
        var hi = machine.Bus.Read(0xFFFD);
        var vector = (ushort)(lo | (hi << 8));
        Assert.Equal(vector, machine.GetState().PC);
        Assert.NotEqual((ushort)0, vector);
    }

    [Fact]
    public void Vic20_CartPrg_MapsIntoBlkRegion()
    {
        // Minimal PRG: load at $A000, 4 payload bytes
        var prg = new byte[] { 0x00, 0xA0, 0x11, 0x22, 0x33, 0x44 };
        var cart = Vic20Cartridge.FromPrg(prg);
        var descriptor = new Architectures.Vic20.Vic20Descriptor().WithCartridge(cart);
        var machine = MachineTestFactory.CreateVic20Machine(descriptor);

        Assert.Equal(0x11, machine.Bus.Read(0xA000));
        Assert.Equal(0x44, machine.Bus.Read(0xA003));
    }

    private static bool TryFindReady(IMachine machine, out ushort addr)
    {
        for (var a = 0x1000; a < 0x2000 - 5; a++)
        {
            if (machine.Bus.Peek((ushort)a) == 18
                && machine.Bus.Peek((ushort)(a + 1)) == 5
                && machine.Bus.Peek((ushort)(a + 2)) == 1
                && machine.Bus.Peek((ushort)(a + 3)) == 4
                && machine.Bus.Peek((ushort)(a + 4)) == 25)
            {
                addr = (ushort)a;
                return true;
            }
        }

        addr = 0;
        return false;
    }

    private static string DumpBootFailure(IMachine machine, int maxFrames)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"READY not found after {maxFrames} frames. PC=${machine.GetState().PC:X4}");
        for (var i = 0; i < 88; i++)
        {
            var c = machine.Bus.Peek((ushort)(0x1E00 + i));
            sb.Append(c is >= 1 and <= 26 ? (char)('A' + c - 1) : c == 0x20 ? ' ' : '?');
        }
        return sb.ToString();
    }
}
