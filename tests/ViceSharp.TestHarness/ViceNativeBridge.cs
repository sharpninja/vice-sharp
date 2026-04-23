using ViceSharp.Core;

namespace ViceSharp.TestHarness;

/// <summary>
/// Test-local convenience wrapper around the shared VICE native interop.
/// </summary>
public static class ViceNativeBridge
{
    public static bool IsAvailable => ViceNative.IsAvailable;
    public static string AvailabilityMessage => ViceNative.AvailabilityMessage;

    public static IntPtr CreateMachine() => ViceNative.Create();
    public static void DestroyMachine(IntPtr machine) => ViceNative.Destroy(machine);
    public static void ResetMachine(IntPtr machine) => ViceNative.ResetNative(machine);
    public static void StepCycle(IntPtr machine) => ViceNative.StepNative(machine);
    public static byte GetCpuRegister(IntPtr machine, int registerId) => ViceNative.GetCpuRegister(machine, registerId);

    public static void GetVicState(IntPtr machine, ref ViceVicState state)
    {
        var nativeState = new ViceNative.ViceVicState();
        ViceNative.GetVicState(machine, ref nativeState);

        state.Cycle = nativeState.Cycle;
        state.RasterLine = nativeState.RasterLine;
        state.RasterCycle = nativeState.RasterCycle;
        state.BadLine = nativeState.BadLine;
        state.DisplayState = nativeState.DisplayState;
        state.SpriteDma = nativeState.SpriteDma;
        state.Registers = nativeState.GetRegisters();
    }

    public static void GetCiaState(IntPtr machine, int ciaIndex, ref ViceCiaState state)
    {
        var nativeState = new ViceNative.ViceCiaState();
        ViceNative.GetCiaState(machine, ciaIndex, ref nativeState);

        state.PortA = nativeState.PortA;
        state.PortB = nativeState.PortB;
        state.DdrA = nativeState.DdrA;
        state.DdrB = nativeState.DdrB;
        state.TimerA = nativeState.TimerA;
        state.TimerB = nativeState.TimerB;
        state.Icr = nativeState.Icr;
        state.Cra = nativeState.Cra;
        state.Crb = nativeState.Crb;
        state.InterruptFlag = nativeState.InterruptFlag;
    }

    public static void GetSidState(IntPtr machine, ref ViceSidState state)
    {
        var nativeState = new ViceNative.ViceSidState();
        ViceNative.GetSidState(machine, ref nativeState);

        state.Registers = nativeState.GetRegisters();
        state.Accumulators = nativeState.GetAccumulators();
        state.Envelopes = nativeState.GetEnvelopes();
        state.FilterState = nativeState.FilterState;
    }

    public struct ViceVicState
    {
        public uint Cycle;
        public ushort RasterLine;
        public byte RasterCycle;
        public byte BadLine;
        public byte DisplayState;
        public byte SpriteDma;
        public byte[] Registers;
    }

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

    public struct ViceSidState
    {
        public byte[] Registers;
        public uint[] Accumulators;
        public byte[] Envelopes;
        public uint FilterState;
    }
}
