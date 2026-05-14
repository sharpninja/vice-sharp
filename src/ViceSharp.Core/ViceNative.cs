using System.Reflection;
using System.Runtime.InteropServices;
using ViceSharp.Abstractions;

namespace ViceSharp.Core;

/// <summary>
/// Native VICE library interop binding
/// </summary>
public static unsafe partial class ViceNative
{
    private const string LibraryName = "vice_x64";
    private static readonly string[] AlternateLibraryNames = ["vice_x64", "vice-shim"];
    private static readonly string[] RelativeSearchDirectories =
    [
        "",
        "native",
        Path.Combine("runtimes", "win-x64", "native"),
        Path.Combine("runtimes", "linux-x64", "native"),
        Path.Combine("runtimes", "osx-x64", "native"),
        Path.Combine("runtimes", "osx-arm64", "native")
    ];

    private static readonly Lazy<string?> ResolvedLibraryPath = new(FindLibraryPath);

    static ViceNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(ViceNative).Assembly, ResolveLibrary);
    }

    public static bool IsAvailable => ResolvedLibraryPath.Value is not null;
    public static string AvailabilityMessage => ResolvedLibraryPath.Value is { } path
        ? $"Native VICE library resolved at '{path}'."
        : "Native VICE library not found. Build or copy 'vice_x64'/'vice-shim' into the test output or a searched native directory.";

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_create")]
    public static partial IntPtr Create();

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_create_model", StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr CreateModel(string modelSelector);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_destroy")]
    public static partial void Destroy(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_reset")]
    public static partial void ResetNative(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_step_cycle")]
    public static partial void StepNative(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_attach_cartridge")]
    private static partial int AttachCartridgeNative(IntPtr instance, byte* image, int length, int mappingMode);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_peek_ram")]
    private static partial byte PeekRamNative(IntPtr instance, ushort address);

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

    [LibraryImport(LibraryName, EntryPoint = "vice_vic_get_state")]
    public static partial void GetVicState(IntPtr instance, ref ViceVicState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_cia_get_state")]
    public static partial void GetCiaState(IntPtr instance, int ciaIndex, ref ViceCiaState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_sid_get_state")]
    public static partial void GetSidState(IntPtr instance, ref ViceSidState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_interrupt_get_state")]
    public static partial void GetInterruptState(IntPtr instance, ref ViceInterruptState state);

    public static IViceNative CreateInstance(string? modelSelector = null)
    {
        if (!IsAvailable)
            throw new DllNotFoundException(AvailabilityMessage);

        var handle = string.IsNullOrWhiteSpace(modelSelector)
            ? Create()
            : CreateModel(modelSelector);

        if (handle == IntPtr.Zero)
            throw new InvalidOperationException($"Native VICE failed to create a machine for model '{modelSelector ?? "default"}'.");

        return new ViceNativeInstance(handle);
    }

    public static byte GetCpuRegister(IntPtr instance, int registerId)
    {
        return registerId switch
        {
            0 => GetA(instance),
            1 => GetX(instance),
            2 => GetY(instance),
            3 => GetP(instance),
            4 => GetS(instance),
            5 => (byte)(GetPC(instance) & 0xFF),
            6 => (byte)(GetPC(instance) >> 8),
            _ => throw new ArgumentOutOfRangeException(nameof(registerId), registerId, "Expected CPU register id 0-6.")
        };
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ViceVicState
    {
        public uint Cycle;
        public ushort RasterLine;
        public byte RasterCycle;
        public byte BadLine;
        public byte DisplayState;
        public byte SpriteDma;
        public fixed byte Registers[64];

        public readonly byte[] GetRegisters()
        {
            fixed (byte* registers = Registers)
            {
                return new ReadOnlySpan<byte>(registers, 64).ToArray();
            }
        }
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
        public fixed byte Registers[32];
        public fixed uint Accumulators[3];
        public fixed byte Envelopes[3];

        public uint FilterState;

        public readonly byte[] GetRegisters()
        {
            fixed (byte* registers = Registers)
            {
                return new ReadOnlySpan<byte>(registers, 32).ToArray();
            }
        }

        public readonly uint[] GetAccumulators()
        {
            fixed (uint* accumulators = Accumulators)
            {
                return new ReadOnlySpan<uint>(accumulators, 3).ToArray();
            }
        }

        public readonly byte[] GetEnvelopes()
        {
            fixed (byte* envelopes = Envelopes)
            {
                return new ReadOnlySpan<byte>(envelopes, 3).ToArray();
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ViceInterruptState
    {
        public byte IrqAsserted;
        public byte NmiAsserted;
        public byte GlobalPending;
        public byte IrqSourceCount;
        public byte NmiSourceCount;
    }

    private static IntPtr ResolveLibrary(string libraryName, Assembly _, DllImportSearchPath? __)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
            return IntPtr.Zero;

        var path = ResolvedLibraryPath.Value;
        return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
    }

    private static string? FindLibraryPath()
    {
        foreach (var root in EnumerateSearchRoots(AppContext.BaseDirectory))
        {
            foreach (var relativeDirectory in RelativeSearchDirectories)
            {
                var candidateDirectory = Path.Combine(root, relativeDirectory);
                foreach (var libraryName in AlternateLibraryNames)
                {
                    foreach (var fileName in GetCandidateFileNames(libraryName))
                    {
                        var candidatePath = Path.Combine(candidateDirectory, fileName);
                        if (File.Exists(candidatePath))
                            return candidatePath;
                    }
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchRoots(string startingDirectory)
    {
        var current = new DirectoryInfo(startingDirectory);
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private static IEnumerable<string> GetCandidateFileNames(string baseName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return $"{baseName}.dll";
            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return $"lib{baseName}.dylib";
            yield return $"{baseName}.dylib";
            yield break;
        }

        yield return $"lib{baseName}.so";
        yield return $"{baseName}.so";
    }

    private sealed class ViceNativeInstance : IViceNative
    {
        private readonly IntPtr _instance;
        private long _cycleBaseline;

        public ViceNativeInstance(IntPtr instance)
        {
            _instance = instance;
        }

        public void Reset()
        {
            ResetNative(_instance);
            _cycleBaseline = ReadNativeCycle();
        }

        public void Step() => StepNative(_instance);

        public void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode)
        {
            if (image.IsEmpty)
                throw new ArgumentException("Cartridge image must not be empty.", nameof(image));

            var imageBytes = image.ToArray();
            fixed (byte* imagePointer = imageBytes)
            {
                var result = AttachCartridgeNative(_instance, imagePointer, imageBytes.Length, (int)mappingMode);
                if (result != 0)
                    throw new InvalidOperationException($"Native VICE failed to attach cartridge image. Error code: {result}.");
            }
        }

        public byte PeekRam(ushort address) => PeekRamNative(_instance, address);

        public MachineState GetState()
        {
            return new MachineState
            {
                A = GetA(_instance),
                X = GetX(_instance),
                Y = GetY(_instance),
                S = GetS(_instance),
                P = GetP(_instance),
                PC = GetPC(_instance),
                Cycle = Math.Max(0, ReadNativeCycle() - _cycleBaseline)
            };
        }

        public void Dispose()
        {
            Destroy(_instance);
        }

        private long ReadNativeCycle()
        {
            var state = new ViceVicState();
            GetVicState(_instance, ref state);
            return state.Cycle;
        }
    }
}
