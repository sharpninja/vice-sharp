using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Video;
using ViceSharp.Chips.Interface;
using ViceSharp.Chips.Audio;

namespace ViceSharp.Architectures.C64;

public sealed class Commodore64 : IMachine
{
    public MachineId Id => new MachineId(0x0001);
    public string Name => "Commodore 64";
    public uint SystemClock => 985248;

    private readonly IBus _bus;
    private readonly IClock _clock;
    private readonly IInterruptController _interruptController;

    // C64 Chips
    private readonly Mos6502 _cpu;
    private readonly VicII _vic;
    private readonly Cia6526 _cia1;
    private readonly Cia6526 _cia2;
    private readonly Sid6581 _sid;

    // System RAM
    private readonly byte[] _ram = new byte[0x10000];
    private readonly byte[] _colorRam = new byte[0x0400];
    private readonly byte[] _romBasic = new byte[0x2000];
    private readonly byte[] _romKernal = new byte[0x2000];
    private readonly byte[] _romChar = new byte[0x1000];

    public Commodore64(IBus bus, IClock clock, IInterruptController interruptController)
    {
        _bus = bus;
        _clock = clock;
        _interruptController = interruptController;

        // Initialize chips
        _cpu = new Mos6502(bus);
        _vic = new VicII(bus, null);
        _cia1 = new Cia6526(bus, interruptController.IrqLine);
        _cia2 = new Cia6526(bus, interruptController.NmiLine);
        _sid = new Sid6581(bus);

        // Register devices on bus
        _bus.RegisterDevice(_cpu);
        _bus.RegisterDevice(_vic);
        _bus.RegisterDevice(_cia1);
        _bus.RegisterDevice(_cia2);
        _bus.RegisterDevice(_sid);

        // Map system memory
        _bus.MapMemory(0x0000, 0xFFFF, _ram, MemoryAccess.ReadWrite);
        _bus.MapMemory(0xD800, 0xDBFF, _colorRam, MemoryAccess.ReadWrite);
        _bus.MapMemory(0xA000, 0xBFFF, _romBasic, MemoryAccess.ReadOnly);
        _bus.MapMemory(0xE000, 0xFFFF, _romKernal, MemoryAccess.ReadOnly);
        _bus.MapMemory(0xD000, 0xDFFF, _romChar, MemoryAccess.ReadOnly);

        // Register clock devices
        _clock.RegisterDevice(_cpu);
        _clock.RegisterDevice(_vic);
        _clock.RegisterDevice(_cia1);
        _clock.RegisterDevice(_cia2);
        _clock.RegisterDevice(_sid);
    }

    public void Reset()
    {
        _cpu.Reset();
        _vic.Reset();
        _cia1.Reset();
        _cia2.Reset();
        _sid.Reset();
    }

    public void Run()
    {
        _clock.Run();
    }

    public void Step()
    {
        _clock.Tick();
    }

    public void LoadRom(string path, ushort address)
    {
        // ROM loading implementation
    }

    public IReadOnlyList<IDevice> GetDevices()
    {
        return new IDevice[] { _cpu, _vic, _cia1, _cia2, _sid };
    }
}