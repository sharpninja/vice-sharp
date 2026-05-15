using ViceSharp.Core;
using ViceSharp.Abstractions;

namespace ViceSharp.TestHarness;

/// <summary>
/// Test-local convenience wrapper around the shared VICE native interop.
/// </summary>
public static class ViceNativeBridge
{
    public static bool IsAvailable => ViceNative.IsAvailable;
    public static string AvailabilityMessage => ViceNative.AvailabilityMessage;

    public static IntPtr CreateMachine(string? modelSelector = null)
    {
        var machine = string.IsNullOrWhiteSpace(modelSelector)
            ? ViceNative.Create()
            : ViceNative.CreateModel(modelSelector);

        if (machine == IntPtr.Zero)
            throw new InvalidOperationException($"Native VICE failed to create a machine for model '{modelSelector ?? "default"}'.");

        return machine;
    }
    public static void DestroyMachine(IntPtr machine) => ViceNative.Destroy(machine);
    public static void ResetMachine(IntPtr machine) => ViceNative.ResetNative(machine);
    public static void StepCycle(IntPtr machine) => ViceNative.StepNative(machine);
    public static int GetModel(IntPtr machine) => ViceNative.GetModel(machine);
    public static byte GetCpuRegister(IntPtr machine, int registerId) => ViceNative.GetCpuRegister(machine, registerId);
    public static byte ReadMemory(IntPtr machine, ushort address) => ViceNative.ReadMemory(machine, address);
    public static byte PeekRam(IntPtr machine, ushort address) => ViceNative.PeekRam(machine, address);
    public static void WriteMemory(IntPtr machine, ushort address, byte value) => ViceNative.WriteMemory(machine, address, value);
    public static void AttachCartridge(IntPtr machine, ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode)
        => ViceNative.AttachCartridge(machine, image, mappingMode);
    public static void AttachDisk(IntPtr machine, uint unit, uint drive, string path)
        => ViceNative.AttachDisk(machine, unit, drive, path);
    public static void DetachDisk(IntPtr machine, uint unit, uint drive)
        => ViceNative.DetachDisk(machine, unit, drive);
    public static void SetKeyboardMatrixKey(IntPtr machine, int row, int column, bool pressed)
        => ViceNative.SetKeyboardMatrixKey(machine, row, column, pressed);
    public static void StoreCia1Register(IntPtr machine, byte registerIndex, byte value)
        => ViceNative.StoreCia1Register(machine, registerIndex, value);
    public static byte ReadCia1Register(IntPtr machine, byte registerIndex)
        => ViceNative.ReadCia1Register(machine, registerIndex);

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

    public static void GetInterruptState(IntPtr machine, ref ViceInterruptState state)
    {
        var nativeState = new ViceNative.ViceInterruptState();
        ViceNative.GetInterruptState(machine, ref nativeState);

        state.IrqAsserted = nativeState.IrqAsserted;
        state.NmiAsserted = nativeState.NmiAsserted;
        state.GlobalPending = nativeState.GlobalPending;
        state.IrqSourceCount = nativeState.IrqSourceCount;
        state.NmiSourceCount = nativeState.NmiSourceCount;
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

    public struct ViceInterruptState
    {
        public byte IrqAsserted;
        public byte NmiAsserted;
        public byte GlobalPending;
        public byte IrqSourceCount;
        public byte NmiSourceCount;
    }
}
