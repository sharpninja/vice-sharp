namespace ViceSharp.TestHarness;

using System.Runtime.InteropServices;

/// <summary>
/// Native bridge to original VICE emulator for golden reference state
/// </summary>
public static class ViceNativeBridge
{
    private const string ViceLibName = "vice_x64";

    [DllImport(ViceLibName, EntryPoint = "vice_machine_create")]
    public static extern IntPtr CreateMachine();

    [DllImport(ViceLibName, EntryPoint = "vice_machine_reset")]
    public static extern void ResetMachine(IntPtr machine);

    [DllImport(ViceLibName, EntryPoint = "vice_machine_step_cycle")]
    public static extern void StepCycle(IntPtr machine);

    [DllImport(ViceLibName, EntryPoint = "vice_cpu_get_register")]
    public static extern byte GetCpuRegister(IntPtr machine, int registerId);

    [DllImport(ViceLibName, EntryPoint = "vic_get_state")]
    public static extern void GetVicState(IntPtr machine, ref ViceVicState state);

    [DllImport(ViceLibName, EntryPoint = "cia_get_state")]
    public static extern void GetCiaState(IntPtr machine, int ciaIndex, ref ViceCiaState state);

    [DllImport(ViceLibName, EntryPoint = "sid_get_state")]
    public static extern void GetSidState(IntPtr machine, ref ViceSidState state);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ViceVicState
    {
        public uint Cycle;
        public ushort RasterLine;
        public byte RasterCycle;
        public byte BadLine;
        public byte DisplayState;
        public byte SpriteDma;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x40)]
        public byte[] Registers;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ViceCiaState
    {
        public byte PortA;
        public byte PortB;
        public byte DdrA;
        public byte DdrB;
        public ushort TimerA;
        public ushort TimerB;
        public byte Icr;
        public byte Cra;
        public byte Crb;
        public byte InterruptFlag;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ViceSidState
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
        public byte[] Registers;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public uint[] Accumulators;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Envelopes;
        public uint FilterState;
    }
}