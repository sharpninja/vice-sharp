using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Audio;

namespace ViceSharp.Architectures.C64;

public sealed class Commodore64 : IMachine
{
    public IBus Bus => _bus;
    public IClock Clock => _clock;
    public IDeviceRegistry Devices { get; } = null!;
    public IArchitectureDescriptor Architecture { get; } = null!;

    private readonly IBus _bus;
    private readonly IClock _clock;
    private readonly IInterruptLine _irqLine;
    private readonly IInterruptLine _nmiLine;

    // C64 Chips
    private readonly Mos6502 _cpu;
    private readonly Mos6569 _vic;
    private readonly Mos6526 _cia1;
    private readonly Mos6526 _cia2;
    private readonly Sid6581 _sid;

    // System RAM
    private readonly byte[] _ram = new byte[0x10000];
    private readonly byte[] _colorRam = new byte[0x0400];
    private readonly byte[] _romBasic = new byte[0x2000];
    private readonly byte[] _romKernal = new byte[0x2000];
    private readonly byte[] _romChar = new byte[0x1000];

    public Commodore64(IBus bus, IClock clock, IInterruptLine irqLine, IInterruptLine nmiLine)
    {
        _bus = bus;
        _clock = clock;
        _irqLine = irqLine;
        _nmiLine = nmiLine;

        // Initialize chips
        _cpu = new Mos6502(bus);
        _vic = new Mos6569(bus, irqLine);
        _cia1 = new Mos6526(bus, irqLine) { BaseAddress = 0xDC00 };
        _cia2 = new Mos6526(bus, nmiLine) { BaseAddress = 0xDD00 };
        _sid = new Sid6581(bus);

        // Register devices on bus
        _bus.RegisterDevice(_cpu);
        _bus.RegisterDevice(_vic);
        _bus.RegisterDevice(_cia1);
        _bus.RegisterDevice(_cia2);
        _bus.RegisterDevice(_sid);

        // System RAM
        _bus.RegisterDevice(new RamDevice(0x0000, 0xFFFF, _ram));
        
        // Color RAM
        _bus.RegisterDevice(new RamDevice(0xD800, 0xDBFF, _colorRam));
        
        // ROMs
        _bus.RegisterDevice(new RomDevice(0xA000, 0xBFFF, _romBasic));
        _bus.RegisterDevice(new RomDevice(0xE000, 0xFFFF, _romKernal));
        _bus.RegisterDevice(new RomDevice(0xD000, 0xDFFF, _romChar));

        // Register clock devices
        _clock.Register(_cpu);
        _clock.Register(_vic);
        _clock.Register(_cia1);
        _clock.Register(_cia2);
        _clock.Register(_sid);
    }

    public void Reset()
    {
        _cpu.Reset();
        _vic.Reset();
        _cia1.Reset();
        _cia2.Reset();
        _sid.Reset();
    }

    public void RunFrame()
    {
        // Run exactly one PAL frame (312 lines × 63 cycles = 19656 cycles)
        _clock.Step(19656);
    }

    public void StepInstruction()
    {
        // Execute single CPU instruction
        while (true)
        {
            _clock.Step();
            
            // Step completed when PC advances to next instruction
            // Implementation pending proper public API
            break;
        }
    }

    public void LoadRom(string path, ushort address)
    {
        // ROM loading implementation
    }

    public IReadOnlyList<IDevice> GetDevices()
    {
        return new IDevice[] { _cpu, _vic, _cia1, _cia2, _sid };
    }

    /// <summary>
    /// Get current full machine state snapshot
    /// </summary>
    public MachineState GetState()
    {
        return new MachineState
        {
            A = _cpu.A,
            X = _cpu.X,
            Y = _cpu.Y,
            S = _cpu.S,
            P = _cpu.P,
            PC = _cpu.PC,
            Cycle = _clock.TotalCycles
        };
    }
}
