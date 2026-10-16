namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core;
using Xunit;

public sealed class BranchCycleCountTests
{
    private sealed class SimpleRam : IMemory
    {
        private readonly byte[] _m;
        public SimpleRam(byte[] m) { _m = m; Id = new DeviceId(1); }
        public DeviceId Id { get; }
        public string Name => "ram";
        public Span<byte> Span => _m;
        public bool HandlesAddress(ushort a) => true;
        public byte Read(ushort a) => _m[a];
        public byte Peek(ushort a) => _m[a];
        public void Write(ushort a, byte v) => _m[a] = v;
        public void Reset() { }
    }

    [Fact(Explicit = true)]
    public void TakenBne_CycleTrace()
    {
        var ram = new byte[65536];
        ram[0x1000] = 0xD0;
        ram[0x1001] = 0x02;
        ram[0x1004] = 0xEA;
        var bus = new BasicBus();
        bus.RegisterDevice(new SimpleRam(ram));
        var cpu = new Mos6502(bus);
        cpu.P = 0x20; // Z clear => BNE taken
        cpu.S = 0xFF;
        cpu.PC = 0x1000;
        // drain reset bootstrap
        for (var i = 0; i < 20; i++) cpu.Tick();
        cpu.PC = 0x1000;
        cpu.P = 0x20;
        var lines = new System.Text.StringBuilder();
        for (var t = 1; t <= 10; t++)
        {
            cpu.Tick();
            lines.AppendLine($"t{t}: PC=${cpu.PC:X4} cyc={cpu.DebugCycle} op={cpu.DebugOpcode:X2} bound={cpu.IsInstructionBoundary}");
            if (t > 3 && cpu.IsInstructionBoundary && cpu.PC == 0x1004)
            {
                lines.AppendLine($"arrived target at t={t}");
                break;
            }
        }
        throw new Xunit.Sdk.XunitException(lines.ToString());
    }

    [Fact(Explicit = true)]
    public void NotTakenBne_CycleTrace()
    {
        var ram = new byte[65536];
        ram[0x1000] = 0xD0;
        ram[0x1001] = 0x02;
        ram[0x1002] = 0xEA;
        var bus = new BasicBus();
        bus.RegisterDevice(new SimpleRam(ram));
        var cpu = new Mos6502(bus);
        cpu.S = 0xFF;
        for (var i = 0; i < 20; i++) cpu.Tick();
        cpu.PC = 0x1000;
        cpu.P = 0x22; // Z set => BNE not taken
        var lines = new System.Text.StringBuilder();
        for (var t = 1; t <= 10; t++)
        {
            cpu.Tick();
            lines.AppendLine($"t{t}: PC=${cpu.PC:X4} cyc={cpu.DebugCycle} op={cpu.DebugOpcode:X2} bound={cpu.IsInstructionBoundary}");
            if (t > 1 && cpu.IsInstructionBoundary && cpu.PC == 0x1002)
            {
                lines.AppendLine($"fallthrough at t={t}");
                break;
            }
        }
        throw new Xunit.Sdk.XunitException(lines.ToString());
    }
}
