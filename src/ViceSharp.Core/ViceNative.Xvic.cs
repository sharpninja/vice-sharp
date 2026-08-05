using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using ViceSharp.Abstractions;
using ViceSharp.RomFetch;

namespace ViceSharp.Core;

/// <summary>
/// Native VICE xvic (VIC-20) library interop binding (vice_xvic.dll).
/// Separate from <see cref="ViceNative"/> / vice_x64 because VICE machine
/// globals cannot share a process image between C64SC and VIC20.
/// </summary>
public static unsafe partial class ViceNativeXvic
{
    private const string LibraryName = "vice_xvic";
    private static readonly string[] AlternateLibraryNames = ["vice_xvic"];
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

    // DllImport resolver is installed once on the Core assembly by ViceNative
    // (combined vice_x64 + vice_xvic). Do not call SetDllImportResolver here.

    public static bool IsAvailable => ResolvedLibraryPath.Value is not null;

    /// <summary>Absolute path of the resolved vice_xvic library, or null.</summary>
    public static string? ResolvedPath => ResolvedLibraryPath.Value;
    public static string AvailabilityMessage => ResolvedLibraryPath.Value is { } path
        ? $"Native VICE xvic library resolved at '{path}'."
        : "Native VICE xvic library not found. Build 'vice_xvic' via native/build-vice-shim-xvic.sh into the test output or a searched native directory.";

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

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_peek_ram")]
    private static partial byte PeekRamNative(IntPtr instance, ushort address);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_attach_disk", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int AttachDiskNative(IntPtr instance, uint unit, uint drive, string path);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_detach_disk")]
    private static partial int DetachDiskNative(IntPtr instance, uint unit, uint drive);

    [LibraryImport(LibraryName, EntryPoint = "vice_machine_set_keyboard_matrix_key")]
    private static partial int SetKeyboardMatrixKeyNative(IntPtr instance, int row, int column, int pressed);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_a")]
    public static partial byte GetA(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_x")]
    public static partial byte GetX(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_y")]
    public static partial byte GetY(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_p")]
    public static partial byte GetP(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_sp")]
    public static partial byte GetS(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_pc")]
    public static partial ushort GetPC(IntPtr instance);

    [LibraryImport(LibraryName, EntryPoint = "vice_vic_get_state")]
    public static partial void GetVicState(IntPtr instance, ref ViceNative.ViceVicState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_cpu_get_pipeline_state")]
    public static partial void GetCpuPipelineState(IntPtr instance, ref ViceNative.ViceCpuPipelineState state);

    [LibraryImport(LibraryName, EntryPoint = "vice_cia_get_state")]
    public static partial void GetCiaState(IntPtr instance, int ciaIndex, ref ViceNative.ViceCiaState state);

    public static bool IsVic20ModelSelector(string? modelSelector)
    {
        if (string.IsNullOrWhiteSpace(modelSelector))
            return false;

        return modelSelector.Equals("vic20", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("vic20pal", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("vic20ntsc", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("xvic", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("vic21", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("vic1001", StringComparison.OrdinalIgnoreCase)
            || modelSelector.Equals("supervic", StringComparison.OrdinalIgnoreCase);
    }

    public static IViceNative CreateInstance(string? modelSelector = null)
    {
        if (!IsAvailable)
            throw new DllNotFoundException(AvailabilityMessage);

        var handle = string.IsNullOrWhiteSpace(modelSelector)
            ? Create()
            : CreateModel(modelSelector);

        if (handle == IntPtr.Zero)
            throw new InvalidOperationException($"Native VICE xvic failed to create a machine for model '{modelSelector ?? "default"}'.");

        return new Vic20NativeInstance(handle);
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

            // Vic20 ROMs live under VIC20/; accept either C64 or VIC20 tree.
            if (!Directory.Exists(Path.Combine(expectedDataDirectory, "VIC20"))
                && !Directory.Exists(Path.Combine(expectedDataDirectory, "C64")))
            {
                return false;
            }

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
        var input = $"{sourceInfo.FullName}|{sourceInfo.LastWriteTimeUtc.Ticks}|{sourceInfo.Length}|{dataRoot}|xvic";
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

    private sealed class Vic20NativeInstance : IViceNative
    {
        private readonly IntPtr _instance;
        private long _cycleBaseline;

        public Vic20NativeInstance(IntPtr instance)
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
            _cycleBaseline = ReadNativeCycle();
            return result;
        }

        public int WriteSnapshot(string path) => WriteSnapshotNative(_instance, path);

        public void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode)
            => throw new NotSupportedException("Vic20 native cartridge attach is not wired in N0 (use later cart slice).");

        public void AttachDisk(uint unit, uint drive, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Disk path is required.", nameof(path));

            var result = AttachDiskNative(_instance, unit, drive, path);
            if (result != 0)
                throw new InvalidOperationException($"Native VICE xvic failed to attach disk unit {unit}, drive {drive}, path '{path}'. Error code: {result}.");
        }

        public void DetachDisk(uint unit, uint drive)
        {
            var result = DetachDiskNative(_instance, unit, drive);
            if (result != 0)
                throw new InvalidOperationException($"Native VICE xvic failed to detach disk unit {unit}, drive {drive}. Error code: {result}.");
        }

        public void SetKeyboardMatrixKey(int row, int column, bool pressed)
        {
            var result = SetKeyboardMatrixKeyNative(_instance, row, column, pressed ? 1 : 0);
            if (result != 0)
                throw new InvalidOperationException($"Native VICE xvic failed to set keyboard matrix key ({row},{column}). Error code: {result}.");
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
            var state = new ViceNative.ViceVicState();
            ViceNativeXvic.GetVicState(_instance, ref state);

            return new NativeVicState
            {
                Cycle = state.Cycle,
                RasterLine = state.RasterLine,
                RasterCycle = state.RasterCycle,
                BadLine = state.BadLine,
                DisplayState = state.DisplayState,
                SpriteDma = state.SpriteDma,
                Registers = state.GetRegisters(),
                AllowBadLines = state.AllowBadLines,
                IdleState = state.IdleState
            };
        }

        public NativeCiaState GetCiaState(int ciaIndex)
        {
            // Vic20 has VIA not CIA; export zeros for interface parity.
            var state = new ViceNative.ViceCiaState();
            ViceNativeXvic.GetCiaState(_instance, ciaIndex, ref state);
            return new NativeCiaState
            {
                PortA = state.PortA,
                PortB = state.PortB,
                DdrA = state.DdrA,
                DdrB = state.DdrB,
                TimerA = state.TimerA,
                TimerB = state.TimerB,
                TimerALatch = state.TimerALatch,
                TimerBLatch = state.TimerBLatch,
                Cra = state.Cra,
                Crb = state.Crb,
                InterruptFlags = state.InterruptFlag,
                IrqMask = state.IrqMask
            };
        }

        public NativeCpuPipelineState GetCpuPipelineState()
        {
            var state = new ViceNative.ViceCpuPipelineState();
            ViceNativeXvic.GetCpuPipelineState(_instance, ref state);

            return new NativeCpuPipelineState
            {
                Clk = state.Clk,
                LastOpcodeInfo = state.LastOpcodeInfo,
                BaLowFlags = state.BaLowFlags,
                PportData = state.PportData,
                PportDir = state.PportDir,
                PportDataRead = state.PportDataRead,
                PportDirRead = state.PportDirRead,
                GlobalPendingInt = state.GlobalPendingInt,
                IrqClk = state.IrqClk,
                NmiClk = state.NmiClk,
                IrqDelayCycles = state.IrqDelayCycles,
                NmiDelayCycles = state.NmiDelayCycles
            };
        }

        public void Dispose()
        {
            ViceNativeXvic.Destroy(_instance);
        }

        private long ReadNativeCycle()
        {
            var state = new ViceNative.ViceVicState();
            ViceNativeXvic.GetVicState(_instance, ref state);
            return state.Cycle;
        }
    }
}
