namespace ViceSharp.Launcher;

/// <summary>
/// Parsed VICE-style command-line arguments. Subset of flags meaningful to
/// the current ViceSharp substrate; unknown flags are recorded under
/// <see cref="Unknown"/> instead of throwing so a CLI invocation always
/// runs and surfaces the rejected flag in a final report.
/// </summary>
/// <remarks>
/// ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 (gated minimal slice):
/// Extended for FR-CFG-005 AC6 (debugcart), AC7 (limitcycles), AC8 (prg autostart)
/// per reconciliation + VICE sources (debugcart.c for $D7FF test signaling,
/// autostart/mon_file.c, vice.texi harness patterns). These three are now
/// recognized so they do not pollute Unknown for testbench invocations.
/// </remarks>
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

    /// <summary>
    /// Debug cart enabled (-debugcart) or disabled (+debugcart). ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 / FR-CFG-005 AC6.
    /// VICE: native/vice/vice/src/vic20/cart/debugcart.c (and c64) - "used for automatic regression testing";
    /// writes result byte to $D7FF for harness observation of deterministic exit without UI.
    /// </summary>
    public bool? DebugCart { get; init; }

    /// <summary>
    /// Cycle limit for bounded test runs (--limitcycles / -limitcycles N). ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 / FR-CFG-005 AC7.
    /// VICE: testbench harness patterns + cmdline handling for CI regression (matches --cycles but specific to test cart signaling).
    /// </summary>
    public long? LimitCycles { get; init; }

    /// <summary>
    /// PRG file for autostart (trailing *.prg positional or -autostart). ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 / FR-CFG-005 AC8.
    /// VICE: native/vice/vice/src/autostart.c + mon_file.c (autostart_autodetect_opt_prgname), vice.texi CLI autostart options.
    /// Classic: "x64sc ... program.prg" or -autostart for testprogs harness.
    /// </summary>
    public string? AutostartPrg { get; init; }

    /// <summary>Verbose output flag.</summary>
    public bool Verbose { get; init; }

    /// <summary>True if --help requested.</summary>
    public bool ShowHelp { get; init; }

    /// <summary>Unknown / unsupported flags collected at parse time.</summary>
    public IReadOnlyList<string> Unknown { get; init; } = Array.Empty<string>();
}
