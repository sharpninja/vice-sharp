using System.Diagnostics.CodeAnalysis;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Adhoc;
using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Core;
using ViceSharp.Monitor;
using ViceSharp.RomFetch;

Console.WriteLine("ViceSharp - Commodore 64 Debug Monitor");
Console.WriteLine("=====================================");
Console.WriteLine();

int cycles = 100000;
string? traceFile = null;
string romPath = "roms";
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

IMachine machine;
ISystemCoordinator coordinator;
MultiSystemBuildResult? multiBuild = null;

if (machineYamlPath is not null)
{
    if (IsMultiSystemYamlSuppressed(machineYamlPath))
    {
        Console.WriteLine($"Loading multi-system topology from: {machineYamlPath}");
        var (built, errorMulti) = LoadMultiSystemSuppressed(machineYamlPath, romPath);
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
    var romProvider = new RomProvider(romPath);
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
var state = machine.GetState();
Console.WriteLine($"Initial PC: ${state.PC:X4}");

DeterministicTraceLogger? logger = null;
if (traceFile != null)
{
    logger = new DeterministicTraceLogger(machine, traceFile);
    Console.WriteLine($"Trace logging to: {traceFile}");
}

Console.WriteLine($"Executing {cycles} cycles...");

for (int i = 0; i < cycles; i++)
{
    coordinator.Step();
    logger?.LogInstruction();
}

logger?.Flush();

var finalState = machine.GetState();
Console.WriteLine($"Final PC: ${finalState.PC:X4}");
Console.WriteLine($"Total cycles: {machine.Clock.TotalCycles}");
Console.WriteLine($"Host cycles: {coordinator.TotalHostCycles}");
Console.WriteLine();
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
