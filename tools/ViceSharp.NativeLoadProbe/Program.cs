using System.Runtime.InteropServices;
using ViceSharp.Abstractions;
using ViceSharp.Core;

namespace ViceSharp.NativeLoadProbe;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 load detector, fresh-process controls (round 4 N11). One scenario per process so the
/// once-per-process resolvers of <see cref="ViceNative"/> and <see cref="ViceNativeXvic"/> are observed from a cold
/// state. Scenarios: none, x64-first, xvic-first, alias (a bogus vice-shim alias file is found and fails to load),
/// failing-load (a bogus vice_x64 file is found and fails to load), failing-load-xvic, managed-fake (a managed
/// IViceNative double steps 1000 times and must log nothing). Exit codes: 0 completed, 2 the expected native load
/// failure occurred, 3 another exception, 64 unknown scenario. Bogus files are written under the probe's own output
/// directory and deleted before exit.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        var scenario = args.Length > 0 ? args[0] : "none";
        string? bogus = null;
        try
        {
            switch (scenario)
            {
                case "none":
                    return 0;
                case "x64-first":
                    Console.WriteLine($"x64 available {ViceNative.IsAvailable}");
                    Console.WriteLine($"xvic available {ViceNativeXvic.IsAvailable}");
                    return 0;
                case "xvic-first":
                    Console.WriteLine($"xvic available {ViceNativeXvic.IsAvailable}");
                    Console.WriteLine($"x64 available {ViceNative.IsAvailable}");
                    return 0;
                case "alias":
                    bogus = WriteBogusLibrary("vice-shim");
                    return TryCreate(() => ViceNative.CreateInstance());
                case "failing-load":
                    bogus = WriteBogusLibrary("vice_x64");
                    return TryCreate(() => ViceNative.CreateInstance());
                case "failing-load-xvic":
                    bogus = WriteBogusLibrary("vice_xvic");
                    return TryCreate(() => ViceNativeXvic.CreateInstance("vic20pal"));
                case "managed-fake":
                {
                    var fake = new ManagedFakeNative();
                    for (var i = 0; i < 1000; i++)
                        fake.Step();
                    Console.WriteLine($"managed fake stepped {fake.Steps}");
                    return 0;
                }
                default:
                    Console.Error.WriteLine($"unknown scenario '{scenario}'");
                    return 64;
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return 3;
        }
        finally
        {
            if (bogus is not null && File.Exists(bogus))
                File.Delete(bogus);
        }
    }

    private static int TryCreate(Func<IViceNative> create)
    {
        try
        {
            using var instance = create();
            Console.WriteLine("created (unexpected for a bogus library)");
            return 0;
        }
        catch (Exception e) when (e is DllNotFoundException or BadImageFormatException)
        {
            Console.WriteLine($"expected load failure: {e.GetType().Name}");
            return 2;
        }
    }

    private static string WriteBogusLibrary(string baseName)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "native");
        Directory.CreateDirectory(directory);
        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{baseName}.dll"
            : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"lib{baseName}.dylib"
            : $"lib{baseName}.so";
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, "not a native library: PLAN-NATIVERESTORE-001 D2 load detector control\n");
        return path;
    }

    /// <summary>A managed double of <see cref="IViceNative"/>: no native load, nothing recorded.</summary>
    private sealed class ManagedFakeNative : IViceNative
    {
        public int Steps { get; private set; }
        public void Reset() { }
        public void Step() => Steps++;
        public void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode) { }
        public void AttachDisk(uint unit, uint drive, string path) { }
        public void DetachDisk(uint unit, uint drive) { }
        public void SetKeyboardMatrixKey(int row, int column, bool pressed) { }
        public int FeedKeyboardBuffer(ReadOnlySpan<byte> petscii) => 0;
        public void FlushKeyboardBuffer() { }
        public int Autostart(string path) => 0;
        public bool AutostartInProgress() => false;
        public void StepCycles(uint cycles) => Steps += (int)cycles;
        public byte PeekRam(ushort address) => 0;
        public byte PeekBus(ushort address) => 0;
        public void WriteBus(ushort address, byte value) { }
        public int ReadSnapshot(string path) => 0;
        public int WriteSnapshot(string path) => 0;
        public MachineState GetState() => default;
        public NativeVicState GetVicState() => default;
        public Vic20VideoLockstepState GetVic20VideoState() => default;
        public bool TryCaptureVisibleFrame(byte[] bgraBuffer, out int width, out int height) { width = 0; height = 0; return false; }
        public bool TryCaptureFrameIndices(byte[] indexBuffer, out int width, out int height) { width = 0; height = 0; return false; }
        public int RenderVic20Samples(short[] buffer, int sampleRate, int cyclesPerSec, int deltaTCycles) => 0;
        public NativeCpuPipelineState GetCpuPipelineState() => default;
        public NativeCiaState GetCiaState(int ciaIndex) => default;
        public void Dispose() { }
    }
}
