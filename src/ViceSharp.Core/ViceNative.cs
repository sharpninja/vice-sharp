using System.Runtime.InteropServices;
using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Native VICE library interop binding
/// </summary>
public static unsafe partial class ViceNative
{
    private const string LibraryName = "vice-shim";

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_create")]
    public static partial IntPtr Create();

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_destroy")]
    public static partial void Destroy(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_reset")]
    public static partial void ResetNative(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_step_cycle")]
    public static partial void StepNative(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_a")]
    public static partial byte GetA(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_x")]
    public static partial byte GetX(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_y")]
    public static partial byte GetY(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_sp")]
    public static partial byte GetS(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_p")]
    public static partial byte GetP(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_pc")]
    public static partial ushort GetPC(IntPtr instance);

    public static IViceNative CreateInstance()
    {
        return new ViceNativeInstance(Create());
    }

    private sealed class ViceNativeInstance : IViceNative
    {
        private readonly IntPtr _instance;

        public ViceNativeInstance(IntPtr instance)
        {
            _instance = instance;
        }

        public void Reset() => ResetNative(_instance);
        public void Step() => StepNative(_instance);
        public MachineState GetState()
        {
            return new MachineState
            {
                A = GetA(_instance),
                X = GetX(_instance),
                Y = GetY(_instance),
                S = GetS(_instance),
                P = GetP(_instance),
                PC = GetPC(_instance)
            };
        }

        public void Dispose()
        {
            Destroy(_instance);
        }
    }
}