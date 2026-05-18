using System.Text;

namespace ViceSharp.Launcher;

/// <summary>
/// Maps a <see cref="ViceArgs"/> bundle to a multi-system YAML topology
/// string the main ViceSharp.Console host can consume via
/// <c>--machine-yaml</c>.
///
/// Binary name -> topology kind:
///   x64 / x64sc      C64 host (+ optional drives + cart)
///   c1541            standalone 1541 (disk-only tool mode)
///   x128 / xvic / ...  reserved; throws NotSupportedException for now
///
/// True-drive flag controls whether drives attach via kind C1541 (true-
/// drive substrate, default true when -8/-9 is set) or stay as a non-
/// emulated cheap path (deferred; current implementation always uses
/// kind C1541).
/// </summary>
public static class ViceTopologyBuilder
{
    public static string BuildYaml(ViceArgs args)
    {
        return args.BinaryName switch
        {
            "x64" or "x64sc" => BuildC64Topology(args),
            "c1541" => BuildC1541Standalone(args),
            "x128" or "xvic" or "xpet" or "xplus4" or "xcbm2" or "xcbm5x0" or "vsid" or "petcat" or "cartconv"
                => throw new NotSupportedException(
                    $"Binary '{args.BinaryName}' is not yet supported by the ViceSharp substrate. " +
                    "Supported: x64, x64sc, c1541."),
            _ when args.MachineYamlPath is not null => File.ReadAllText(args.MachineYamlPath),
            _ => throw new InvalidOperationException(
                $"Unknown binary name '{args.BinaryName}' and no --machine-yaml supplied."),
        };
    }

    private static string BuildC64Topology(ViceArgs args)
    {
        if (args.MachineYamlPath is not null)
            return File.ReadAllText(args.MachineYamlPath);

        var yaml = new StringBuilder();
        yaml.AppendLine("schemaVersion: 1");
        yaml.AppendLine("coordinator:");
        yaml.AppendLine("  host:");
        yaml.AppendLine("    id: c64-host");
        yaml.AppendLine("    kind: C64");
        if (args.Drive8Image is not null || args.Drive9Image is not null)
        {
            yaml.AppendLine("    busAttachments:");
            yaml.AppendLine("      - busId: IEC");
            yaml.AppendLine("        endpointName: c64");
        }

        var hasPeripherals = args.Drive8Image is not null || args.Drive9Image is not null;
        if (hasPeripherals)
        {
            yaml.AppendLine("  peripherals:");
            if (args.Drive8Image is not null)
                AppendDrive(yaml, 8, args.Drive8Image);
            if (args.Drive9Image is not null)
                AppendDrive(yaml, 9, args.Drive9Image);
        }
        if (hasPeripherals)
        {
            yaml.AppendLine("  buses:");
            yaml.AppendLine("    - id: IEC");
            yaml.AppendLine("      signals: [ATN, CLK, DATA, SRQ]");
        }

        return yaml.ToString();
    }

    private static void AppendDrive(StringBuilder yaml, int deviceNumber, string imagePath)
    {
        yaml.AppendLine($"    - id: drive-{deviceNumber}");
        yaml.AppendLine($"      kind: C1541");
        yaml.AppendLine($"      deviceNumber: {deviceNumber}");
        yaml.AppendLine($"      diskImagePath: {imagePath.Replace('\\', '/')}");
        yaml.AppendLine($"      busAttachments:");
        yaml.AppendLine($"        - busId: IEC");
        yaml.AppendLine($"          endpointName: drive-{deviceNumber}");
    }

    private static string BuildC1541Standalone(ViceArgs args)
    {
        // c1541 tool runs a standalone 1541 with optional D64 mounted.
        var yaml = new StringBuilder();
        yaml.AppendLine("schemaVersion: 1");
        yaml.AppendLine("coordinator:");
        yaml.AppendLine("  host:");
        yaml.AppendLine("    id: drive-tool");
        yaml.AppendLine("    kind: C1541");
        yaml.AppendLine("    deviceNumber: 8");
        if (args.Drive8Image is not null)
            yaml.AppendLine($"    diskImagePath: {args.Drive8Image.Replace('\\', '/')}");
        yaml.AppendLine("  buses: []");
        return yaml.ToString();
    }
}
