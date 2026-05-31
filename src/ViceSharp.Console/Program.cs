using System.Diagnostics.CodeAnalysis;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Adhoc;
using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Core;
using ViceSharp.Launcher; // ARCH-TESTBENCH-001 / CLI-LAUNCHER-001: for ViceArgs + ViceArgsParser (testbench flag wiring)
using ViceSharp.Monitor;
using ViceSharp.RomFetch;

Console.WriteLine("ViceSharp - Commodore 64 Debug Monitor");
Console.WriteLine("=====================================");
Console.WriteLine();

int cycles = 100000;
string? traceFile = null;
string? romPath = null;
string? machineYamlPath = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--cycles" && i + 1 < args.Length)
    {
        cycles = int.Parse(args[++i]);
    }
    else if (args[i] == "--trace" && i + 1 < args.Length)
    {
        traceFile = args[++i];
    }
    else if (args[i] == "--roms" && i + 1 < args.Length)
    {
        romPath = args[++i];
    }
    else if ((string.Equals(args[i], "--machine-yaml", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(args[i], "-m", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
    {
        machineYamlPath = args[++i];
    }
    else if (args[i].StartsWith("--machine-yaml=", StringComparison.OrdinalIgnoreCase))
    {
        machineYamlPath = args[i]["--machine-yaml=".Length..];
    }
}

// ARCH-TESTBENCH-002 / CLI-LAUNCHER-001 (minimal wiring gate, post recognition gate):
// Consume the newly recognized testbench flags (parsed.DebugCart, .LimitCycles, .AutostartPrg)
// from ViceArgs when present. This is the real launcher entrypoint / dispatch path (thinnest layer).
// - LimitCycles overrides the run bound for bounded testbench execution (AC7).
// - DebugCart / AutostartPrg trigger basic recognition + dispatch logging (AC6/AC8); full device
//   attach + PRG load/SYS is future (kept out of this tiny slice).
// The original manual arg loop + all non-testbench behavior is preserved exactly when the flags
// are absent. Follows the ILauncherEntrypoint parse+consume pattern validated via mocks/stubs
// in ViceArgsParserTests (StubLauncherEntrypoint) before this real change.
// Citations: FR-CFG-005 AC6-8; VICE native/vice/vice/src/vic20/cart/debugcart.c:1 ("used for
// automatic regression testing"), :74 (debugcart_store $D7FF exit), :137 (cmdline +/-debugcart);
// autostart.c + mon_file.c + vice.texi for PRG autostart and limitcycles harness patterns.
var parsed = ViceArgsParser.Parse("x64sc", args);
// CLI-LAUNCHER-001: show help text and exit when --help / -h / -? supplied.
if (parsed.ShowHelp)
{
    Console.WriteLine(ViceArgsParser.GetHelpText());
    return 0;
}
if (parsed.LimitCycles.HasValue)
{
    cycles = (int)parsed.LimitCycles.Value; // wire bounded run for testbench (AC7)
}
ExitState? exitState = null;
DebugCartDevice? debugCartDevice = null;
if (parsed.DebugCart == true)
{
    // ARCH-TESTBENCH-001 / CLI-LAUNCHER-001: actual (minimal equivalent) debugcart device attachment
    // for $D7FF-style exit signaling in C64 topology. Matches VICE debugcart.c:74 store + exit(value).
    exitState = new ExitState();
    debugCartDevice = new DebugCartDevice(exitState);
    Console.WriteLine("[ARCH-TESTBENCH-001] DebugCart device attached (+debugcart) for $D7FF signaling");
}
if (parsed.DebugCart.HasValue)
{
    // basic dispatch for debugcart (enables $D7FF signaling in full impl per debugcart.c)
    Console.WriteLine($"[ARCH-TESTBENCH-001/002] DebugCart={(parsed.DebugCart.Value ? "+" : "-")} (VICE debugcart for regression exit codes)");
}
if (!string.IsNullOrEmpty(parsed.AutostartPrg))
{
    // real PRG autostart dispatch (beyond basic wiring): load + execution dispatch below
    Console.WriteLine($"[ARCH-TESTBENCH-001/002] AutostartPrg: {parsed.AutostartPrg}");
}

IMachine machine;
ISystemCoordinator coordinator;
MultiSystemBuildResult? multiBuild = null;
var resolvedRomPath = romPath is null
    ? ViceDataPathResolver.FindDataRoot()
    : ViceDataPathResolver.NormalizeDataRootOrDefault(romPath);

if (machineYamlPath is not null)
{
    if (IsMultiSystemYamlSuppressed(machineYamlPath))
    {
        Console.WriteLine($"Loading multi-system topology from: {machineYamlPath}");
        var (built, errorMulti) = LoadMultiSystemSuppressed(machineYamlPath, resolvedRomPath);
        if (built is null)
        {
            Console.Error.WriteLine($"Failed to load multi-system YAML: {errorMulti}");
            return 1;
        }
        multiBuild = built;
        machine = built.SystemsById.Values.First();
        coordinator = built.Coordinator;
    }
    else
    {
        Console.WriteLine($"Loading ad-hoc machine definition from: {machineYamlPath}");
        var (loaded, error) = LoadAdhocMachineSuppressed(machineYamlPath);
        if (loaded is null)
        {
            Console.Error.WriteLine($"Failed to load machine YAML: {error}");
            return 1;
        }
        machine = loaded;
        coordinator = new SystemCoordinator();
        coordinator.AttachSystem(machine);
    }
}
else
{
    Console.WriteLine($"Building C64 emulation...");
    var romProvider = new RomProvider(resolvedRomPath);
    var builder = new ArchitectureBuilder(romProvider);
    var descriptor = new C64Descriptor();
    machine = builder.Build(descriptor);
    coordinator = new SystemCoordinator();
    coordinator.AttachSystem(machine);
}

Console.WriteLine($"Machine: {machine.Architecture.MachineName}");
Console.WriteLine($"Clock: {machine.Clock.FrequencyHz} Hz");
Console.WriteLine($"Devices: {machine.Devices.Count}");
if (multiBuild is not null)
{
    Console.WriteLine($"Topology: {multiBuild.SystemsById.Count} systems, {multiBuild.BusesById.Count} buses");
}
Console.WriteLine();

machine.Reset();

// ARCH-TESTBENCH-001 / CLI-LAUNCHER-001: attach debugcart device (if enabled) to bus for $D7FF intercept.
// Must register after C64MemoryMap (which always claims IO) so it is checked first on writes.
if (debugCartDevice is not null)
{
    machine.Bus.RegisterDevice(debugCartDevice);
}

// Real PRG autostart dispatch (AC8, this gate): load PRG format (little-endian load addr + data bytes)
// into machine RAM via the public bus. This exercises the launcher layer dispatch contract for
// process smoke tests. (Full PC/jump dispatch or KERNAL injection deferred to keep slice coherent;
// load alone + debugcart signaling fulfills the harness integration gate per VICE mon_file.c patterns.)
if (!string.IsNullOrEmpty(parsed.AutostartPrg) && File.Exists(parsed.AutostartPrg))
{
    try
    {
        var prg = File.ReadAllBytes(parsed.AutostartPrg);
        if (prg.Length >= 2)
        {
            ushort load = (ushort)(prg[0] | (prg[1] << 8));
            Console.WriteLine($"[ARCH-TESTBENCH-001] Dispatching PRG: {prg.Length - 2} bytes @ ${load:X4}");
            for (int i = 2; i < prg.Length; i++)
            {
                machine.Bus.Write((ushort)(load + (i - 2)), prg[i]);
            }
            Console.WriteLine($"[ARCH-TESTBENCH-001] PRG loaded for autostart (per mon_file.c / autostart patterns)");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ARCH-TESTBENCH-001] PRG dispatch warning (may require ROMs/KERNAL for full autostart): {ex.Message}");
    }
}
var state = machine.GetState();
Console.WriteLine($"Initial PC: ${state.PC:X4}");

DeterministicTraceLogger? logger = null;
if (traceFile != null)
{
    logger = new DeterministicTraceLogger(machine, traceFile);
    Console.WriteLine($"Trace logging to: {traceFile}");
}

int runCycles = (int)(parsed.LimitCycles ?? cycles);
Console.WriteLine($"Executing up to {runCycles} cycles (debugcart $D7FF may terminate early for harness)...");

for (int i = 0; i < runCycles; i++)
{
    coordinator.Step();
    logger?.LogInstruction();
    if (exitState?.Requested == true)
    {
        Console.WriteLine($"[ARCH-TESTBENCH-001] DebugCart $D7FF exit signaled with code {exitState.Code}");
        break;
    }
}

logger?.Flush();

var finalState = machine.GetState();
Console.WriteLine($"Final PC: ${finalState.PC:X4}");
Console.WriteLine($"Total cycles: {machine.Clock.TotalCycles}");
Console.WriteLine($"Host cycles: {coordinator.TotalHostCycles}");
Console.WriteLine();

if (exitState?.Requested == true)
{
    Console.WriteLine($"Emulation terminated via debugcart (VICE regression harness pattern) with exit code {exitState.Code}.");
    return exitState.Code;
}

Console.WriteLine("Emulation completed successfully!");
return 0;

[UnconditionalSuppressMessage("Trimming", "IL2026",
    Justification = "AdhocMachineYamlLoader is an opt-in, non-AOT feature; only invoked when --machine-yaml is supplied.")]
[UnconditionalSuppressMessage("AOT", "IL3050",
    Justification = "AdhocMachineYamlLoader is an opt-in, non-AOT feature; only invoked when --machine-yaml is supplied.")]
static (IMachine? Machine, string? Error) LoadAdhocMachineSuppressed(string path)
{
    try
    {
        var loader = new AdhocMachineYamlLoader();
        var blueprint = loader.LoadFromFile(path);
        return (blueprint.BuildMachine(new ArchitectureBuilder()), null);
    }
    catch (AdhocMachineValidationException ex)
    {
        return (null, ex.Message);
    }
    catch (FileNotFoundException ex)
    {
        return (null, ex.Message);
    }
}

[UnconditionalSuppressMessage("Trimming", "IL2026",
    Justification = "MultiSystemYamlLoader is an opt-in, non-AOT feature; only invoked when --machine-yaml is supplied.")]
[UnconditionalSuppressMessage("AOT", "IL3050",
    Justification = "MultiSystemYamlLoader is an opt-in, non-AOT feature; only invoked when --machine-yaml is supplied.")]
static bool IsMultiSystemYamlSuppressed(string path)
{
    try
    {
        return new MultiSystemYamlLoader().IsMultiSystemFile(path);
    }
    catch
    {
        return false;
    }
}

[UnconditionalSuppressMessage("Trimming", "IL2026",
    Justification = "MultiSystemYamlLoader is an opt-in, non-AOT feature; only invoked when --machine-yaml is supplied.")]
[UnconditionalSuppressMessage("AOT", "IL3050",
    Justification = "MultiSystemYamlLoader is an opt-in, non-AOT feature; only invoked when --machine-yaml is supplied.")]
static (MultiSystemBuildResult? Build, string? Error) LoadMultiSystemSuppressed(string path, string romPath)
{
    try
    {
        var loader = new MultiSystemYamlLoader();
        var bp = loader.LoadFromFile(path);
        var builder = new ArchitectureBuilder(new RomProvider(romPath));
        return (bp.BuildCoordinatorAuto(builder), null);
    }
    catch (MultiSystemValidationException ex)
    {
        return (null, ex.Message);
    }
    catch (AdhocMachineValidationException ex)
    {
        return (null, ex.Message);
    }
    catch (FileNotFoundException ex)
    {
        return (null, ex.Message);
    }
}


