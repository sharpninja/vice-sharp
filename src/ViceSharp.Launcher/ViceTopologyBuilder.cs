using System.Text;

namespace ViceSharp.Launcher;

/// <summary>
/// Parsed descriptor from a topology YAML that may include testbench keys.
/// ARCH-TESTBENCH-001 / CLI-LAUNCHER-001.
/// </summary>
public sealed record ViceTopologyDescriptor
{
    public bool? DebugCart { get; init; }
    public long? LimitCycles { get; init; }
    public string? HostKind { get; init; }
    public string RawYaml { get; init; } = string.Empty;
}

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

    /// <summary>
    /// Parses a topology YAML string and extracts testbench keys
    /// (debugcart, limitcycles) along with the primary host kind.
    /// Uses simple line-by-line key extraction (no YAML library required).
    /// ARCH-TESTBENCH-001 / CLI-LAUNCHER-001.
    /// </summary>
    public static ViceTopologyDescriptor ParseDescriptor(string yaml)
    {
        bool? debugCart = null;
        long? limitCycles = null;
        string? hostKind = null;

        foreach (var line in yaml.Split('\n'))
        {
            var trimmed = line.Trim();
            if (TryParseYamlBool(trimmed, "debugcart:", out var dc))
                debugCart = dc;
            else if (TryParseYamlLong(trimmed, "limitcycles:", out var lc))
                limitCycles = lc;
            else if (TryParseYamlString(trimmed, "kind:", out var k))
                hostKind ??= k; // first kind encountered = host kind
        }

        return new ViceTopologyDescriptor
        {
            DebugCart = debugCart,
            LimitCycles = limitCycles,
            HostKind = hostKind,
            RawYaml = yaml,
        };
    }

    private static bool TryParseYamlBool(string line, string prefix, out bool value)
    {
        value = false;
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var rest = line[prefix.Length..].Trim().ToLowerInvariant();
        if (rest == "true") { value = true; return true; }
        if (rest == "false") { value = false; return true; }
        return false;
    }

    private static bool TryParseYamlLong(string line, string prefix, out long value)
    {
        value = 0;
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        return long.TryParse(line[prefix.Length..].Trim(), out value);
    }

    private static bool TryParseYamlString(string line, string prefix, out string value)
    {
        value = string.Empty;
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        value = line[prefix.Length..].Trim();
        return !string.IsNullOrEmpty(value);
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
