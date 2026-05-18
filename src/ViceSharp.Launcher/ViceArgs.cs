namespace ViceSharp.Launcher;

/// <summary>
/// Parsed VICE-style command-line arguments. Subset of flags meaningful to
/// the current ViceSharp substrate; unknown flags are recorded under
/// <see cref="Unknown"/> instead of throwing so a CLI invocation always
/// runs and surfaces the rejected flag in a final report.
/// </summary>
public sealed class ViceArgs
{
    /// <summary>VICE binary name (lowercased, no extension) - e.g. "x64sc", "c1541".</summary>
    public string BinaryName { get; init; } = "";

    /// <summary>D64 image path attached to drive 8 (-8 flag).</summary>
    public string? Drive8Image { get; init; }

    /// <summary>D64 image path attached to drive 9 (-9 flag).</summary>
    public string? Drive9Image { get; init; }

    /// <summary>Cartridge image path (-cart flag).</summary>
    public string? CartridgeImage { get; init; }

    /// <summary>True if +truedrive specified; false if -truedrive specified; null if neither.</summary>
    public bool? TrueDrive { get; init; }

    /// <summary>Machine YAML path (--machine-yaml / -m).</summary>
    public string? MachineYamlPath { get; init; }

    /// <summary>Maximum cycles to run (--cycles N). Defaults to a sensible value when null.</summary>
    public long? Cycles { get; init; }

    /// <summary>Verbose output flag.</summary>
    public bool Verbose { get; init; }

    /// <summary>True if --help requested.</summary>
    public bool ShowHelp { get; init; }

    /// <summary>Unknown / unsupported flags collected at parse time.</summary>
    public IReadOnlyList<string> Unknown { get; init; } = Array.Empty<string>();
}
