using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.RomFetch;

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

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_read_snapshot", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int ReadSnapshotNative(IntPtr instance, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_write_snapshot", StringMarshalling = StringMarshalling.Utf8)]
    public static partial int WriteSnapshotNative(IntPtr instance, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_snapshot_last_error")]
    public static partial int SnapshotLastError();

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_attach_cartridge")]
    private static partial int AttachCartridgeNative(IntPtr instance, byte* image, int length, int mappingMode);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_attach_disk", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int AttachDiskNative(IntPtr instance, uint unit, uint drive, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_detach_disk")]
    private static partial int DetachDiskNative(IntPtr instance, uint unit, uint drive);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_peek_ram")]
    private static partial byte PeekRamNative(IntPtr instance, ushort address);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_read")]
    public static partial byte ReadMemory(IntPtr instance, ushort address);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_write")]
    public static partial void WriteMemory(IntPtr instance, ushort address, byte value);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_get_model")]
    public static partial int GetModel(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_set_keyboard_matrix_key")]
    private static partial int SetKeyboardMatrixKeyNative(IntPtr instance, int row, int column, int pressed);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_cia1_store")]
    public static partial void StoreCia1Register(IntPtr instance, byte registerIndex, byte value);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_cia1_read")]
    public static partial byte ReadCia1Register(IntPtr instance, byte registerIndex);

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

    [LibraryImport(LibraryName, EntryPoint = "vice_drivecpu_get_a")]
    public static partial byte GetDriveA(IntPtr instance, uint unit);

    [LibraryImport(LibraryName, EntryPoint = "vice_drivecpu_get_x")]
    public static partial byte GetDriveX(IntPtr instance, uint unit);

    [LibraryImport(LibraryName, EntryPoint = "vice_drivecpu_get_y")]
    public static partial byte GetDriveY(IntPtr instance, uint unit);

    [LibraryImport(LibraryName, EntryPoint = "vice_drivecpu_get_sp")]
    public static partial byte GetDriveS(IntPtr instance, uint unit);

    [LibraryImport(LibraryName, EntryPoint = "vice_drivecpu_get_p")]
    public static partial byte GetDriveP(IntPtr instance, uint unit);

    [LibraryImport(LibraryName, EntryPoint = "vice_drivecpu_get_pc")]
    public static partial ushort GetDrivePC(IntPtr instance, uint unit);

    [LibraryImport(LibraryName, EntryPoint = "vice_drive_set_true_emulation")]
    public static partial int SetDriveTrueEmulation(IntPtr instance, uint unit, int enabled);

    [LibraryImport(LibraryName, EntryPoint = "vice_drive_get_true_emulation")]
    public static partial int GetDriveTrueEmulation(IntPtr instance, uint unit);

    [LibraryImport(LibraryName, EntryPoint = "vice_vic_get_state")]
    public static partial void GetVicState(IntPtr instance, ref ViceVicState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_capture_visible_frame")]
    public static partial int CaptureVisibleFrame(IntPtr instance, [Out] byte[] buffer, int length, out int width, out int height);

    [LibraryImport(LibraryName, EntryPoint = "vice_vic_get_graphics_priority_at_raster")]
    public static partial int GetGraphicsPriorityAtRaster(IntPtr instance, ushort rasterLine, [Out] byte[] buffer, int length);

    [LibraryImport(LibraryName, EntryPoint = "vice_cia_get_state")]
    public static partial void GetCiaState(IntPtr instance, int ciaIndex, ref ViceCiaState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_sid_get_state")]
    public static partial void GetSidState(IntPtr instance, ref ViceSidState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_sid_render_samples")]
    public static partial nuint RenderSidSamples(IntPtr instance, [Out] short[] buffer, nuint n, int deltaTCycles);

    [LibraryImport(LibraryName, EntryPoint = "vice_sid_engine_read")]
    public static partial byte ReadSidEngine(IntPtr instance, ushort addr);

    [LibraryImport(LibraryName, EntryPoint = "vice_sid_clock")]
    public static partial void ClockSid(IntPtr instance, int cycles);

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

    public static void AttachCartridge(IntPtr instance, ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode)
    {
        if (image.IsEmpty)
            throw new ArgumentException("Cartridge image must not be empty.", nameof(image));

        var imageBytes = image.ToArray();
        fixed (byte* imagePointer = imageBytes)
        {
            var result = AttachCartridgeNative(instance, imagePointer, imageBytes.Length, (int)mappingMode);
            if (result != 0)
                throw new InvalidOperationException($"Native VICE failed to attach cartridge image. Error code: {result}.");
        }
    }

    public static byte PeekRam(IntPtr instance, ushort address) => PeekRamNative(instance, address);

    public static void AttachDisk(IntPtr instance, uint unit, uint drive, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Disk path is required.", nameof(path));

        var result = AttachDiskNative(instance, unit, drive, path);
        if (result != 0)
            throw new InvalidOperationException($"Native VICE failed to attach disk unit {unit}, drive {drive}, path '{path}'. Error code: {result}.");
    }

    public static void DetachDisk(IntPtr instance, uint unit, uint drive)
    {
        var result = DetachDiskNative(instance, unit, drive);
        if (result != 0)
            throw new InvalidOperationException($"Native VICE failed to detach disk unit {unit}, drive {drive}. Error code: {result}.");
    }

    public static void SetKeyboardMatrixKey(IntPtr instance, int row, int column, bool pressed)
    {
        var result = SetKeyboardMatrixKeyNative(instance, row, column, pressed ? 1 : 0);
        if (result != 0)
            throw new InvalidOperationException($"Native VICE failed to set keyboard matrix key row {row}, column {column}. Error code: {result}.");
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
        foreach (var candidatePath in EnumerateCandidateLibraryPaths())
            return TryCreateRelocatedRuntime(candidatePath, out var relocatedPath) ? relocatedPath : candidatePath;

        return null;
    }

    private static IEnumerable<string> EnumerateCandidateLibraryPaths()
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
                            yield return candidatePath;
                    }
                }
            }
        }
    }

    private static bool TryCreateRelocatedRuntime(string sourceLibraryPath, out string relocatedLibraryPath)
    {
        relocatedLibraryPath = string.Empty;

        if (!ViceDataPathResolver.TryFindDataRoot(out var dataRoot))
            return false;

        try
        {
            var sourceDirectory = Path.GetDirectoryName(sourceLibraryPath);
            if (string.IsNullOrWhiteSpace(sourceDirectory))
                return false;

            var runtimeDirectory = Path.Combine(
                Path.GetTempPath(),
                "ViceSharpNative",
                CreateRuntimeDirectoryName(sourceLibraryPath, dataRoot));
            Directory.CreateDirectory(runtimeDirectory);
            CopyNativeRuntimeFiles(sourceDirectory, runtimeDirectory);

            var expectedDataDirectory = Path.Combine(runtimeDirectory, "vice", "vice", "data");
            if (!Directory.Exists(expectedDataDirectory))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(expectedDataDirectory)!);
                Directory.CreateSymbolicLink(expectedDataDirectory, dataRoot);
            }

            if (!Directory.Exists(Path.Combine(expectedDataDirectory, "C64")))
                return false;

            relocatedLibraryPath = Path.Combine(runtimeDirectory, Path.GetFileName(sourceLibraryPath));
            return File.Exists(relocatedLibraryPath);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }

    private static string CreateRuntimeDirectoryName(string sourceLibraryPath, string dataRoot)
    {
        var sourceInfo = new FileInfo(sourceLibraryPath);
        var input = $"{sourceInfo.FullName}|{sourceInfo.LastWriteTimeUtc.Ticks}|{sourceInfo.Length}|{dataRoot}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)))[..16];
    }

    private static void CopyNativeRuntimeFiles(string sourceDirectory, string runtimeDirectory)
    {
        foreach (var sourcePath in Directory.EnumerateFiles(sourceDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            var destinationPath = Path.Combine(runtimeDirectory, Path.GetFileName(sourcePath));
            var sourceInfo = new FileInfo(sourcePath);
            var destinationInfo = new FileInfo(destinationPath);
            if (destinationInfo.Exists &&
                destinationInfo.Length == sourceInfo.Length &&
                destinationInfo.LastWriteTimeUtc >= sourceInfo.LastWriteTimeUtc)
            {
                continue;
            }

            File.Copy(sourcePath, destinationPath, overwrite: true);
            File.SetLastWriteTimeUtc(destinationPath, sourceInfo.LastWriteTimeUtc);
        }
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

        public int ReadSnapshot(string path)
        {
            var result = ReadSnapshotNative(_instance, path);
            // Re-baseline so GetState().Cycle counts forward from the loaded
            // state, mirroring Reset().
            _cycleBaseline = ReadNativeCycle();
            return result;
        }

        public int WriteSnapshot(string path) => WriteSnapshotNative(_instance, path);

        public void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode)
        {
            ViceNative.AttachCartridge(_instance, image, mappingMode);
        }

        public void AttachDisk(uint unit, uint drive, string path)
        {
            ViceNative.AttachDisk(_instance, unit, drive, path);
        }

        public void DetachDisk(uint unit, uint drive)
        {
            ViceNative.DetachDisk(_instance, unit, drive);
        }

        public void SetKeyboardMatrixKey(int row, int column, bool pressed)
        {
            ViceNative.SetKeyboardMatrixKey(_instance, row, column, pressed);
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

        public NativeVicState GetVicState()
        {
            var state = new ViceVicState();
            global::ViceSharp.Core.ViceNative.GetVicState(_instance, ref state);

            return new NativeVicState
            {
                Cycle = state.Cycle,
                RasterLine = state.RasterLine,
                RasterCycle = state.RasterCycle,
                BadLine = state.BadLine,
                DisplayState = state.DisplayState,
                SpriteDma = state.SpriteDma,
                Registers = state.GetRegisters()
            };
        }

        public void Dispose()
        {
            Destroy(_instance);
        }

        private long ReadNativeCycle()
        {
            var state = new ViceVicState();
            global::ViceSharp.Core.ViceNative.GetVicState(_instance, ref state);
            return state.Cycle;
        }
    }
}
