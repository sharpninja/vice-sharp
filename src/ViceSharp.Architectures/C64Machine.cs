using ViceSharp.Chips;
using ViceSharp.Core;

namespace ViceSharp.Architectures;

/// <summary>
/// Complete Commodore 64 Machine
/// </summary>
public sealed class C64Machine
{
    public ulong Cycles { get; private set; }
    public bool IsRunning { get; private set; }

    public SystemBus Bus { get; } = new SystemBus();
    public Mos6510 CPU { get; } = new Mos6510();
    public VicII VIC { get; } = new VicII();
    public Mos6526 CIA1 { get; } = new Mos6526();
    public Mos6526 CIA2 { get; } = new Mos6526();
    public Mos6581 SID { get; } = new Mos6581();

    public byte[] ScreenBuffer => VIC.ScreenBuffer;

    public C64Machine()
    {
        Reset();
    }

    public void Reset()
    {
        Cycles = 0;
        IsRunning = false;

        Bus.Reset();
        CPU.Reset();
        VIC.Reset();
        CIA1.Reset();
        CIA2.Reset();
        SID.Reset();
    }

    public void Step()
    {
        Cycles++;

        CPU.Step();
        VIC.Step();
        CIA1.Step();
        CIA2.Step();
        SID.Step();
    }

    public void RunFrame()
    {
        // 63 cycles per line * 312 lines = 19656 cycles per PAL frame
        for (int i = 0; i < 19656; i++)
        {
            Step();
        }
    }
}

/// <summary>
/// C64 VIC-II standard palette
/// </summary>
public static class C64Palette
{
    public static readonly uint[] Colors =
    [
        0xFF000000, // 0: Black
        0xFFFFFFFF, // 1: White
        0xFF880000, // 2: Red
        0xFFAAFFEE, // 3: Cyan
        0xFFCC44CC, // 4: Purple
        0xFF00CC55, // 5: Green
        0xFF0000AA, // 6: Blue
        0xFFEEEE77, // 7: Yellow
        0xFFDD8855, // 8: Orange
        0xFF664400, // 9: Brown
        0xFFFF7777, // 10: Light Red
        0xFF333333, // 11: Dark Grey
        0xFF777777, // 12: Grey
        0xFFAAFF66, // 13: Light Green
        0xFF0088FF, // 14: Light Blue
        0xFFBBBBBB  // 15: Light Grey
    ];
}
