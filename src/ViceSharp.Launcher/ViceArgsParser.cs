namespace ViceSharp.Launcher;

/// <summary>
/// Parses VICE-compatible command-line argument arrays into a
/// <see cref="ViceArgs"/> bundle. Mirrors VICE 3.x flag conventions for
/// the subset of flags the current ViceSharp substrate consumes:
///
///   -8 path                attach D64 to drive 8
///   -9 path                attach D64 to drive 9
///   -cart path             attach cartridge image
///   +truedrive             enable true-drive emulation
///   -truedrive             disable true-drive emulation
///   --machine-yaml path    explicit machine topology YAML
///   -m path                short form of --machine-yaml
///   --cycles N             cycle budget for the run
///   -v / --verbose         verbose output
///   --help / -h / -?       show help
///   -debugcart / +debugcart   enable / disable debugcart (ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 / FR-CFG-005 AC6)
///   --limitcycles / -limitcycles N   (ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 / FR-CFG-005 AC7)
///   trailing *.prg or -autostart   (ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 / FR-CFG-005 AC8)
///
/// VICE sources: debugcart.c (regression $D7FF signaling), autostart/mon_file.c,
/// vice.texi, testbench harness patterns ("x64sc -debugcart -limitcycles 100000000 program.prg").
///
/// Unknown flags are collected into <see cref="ViceArgs.Unknown"/> rather
/// than thrown - VICE itself is lenient about unrecognised flags.
/// </summary>
public static class ViceArgsParser
{
    public static ViceArgs Parse(string binaryName, string[] args)
    {
        ArgumentNullException.ThrowIfNull(binaryName);
        ArgumentNullException.ThrowIfNull(args);

        string? drive8 = null;
        string? drive9 = null;
        string? cart = null;
        bool? trueDrive = null;
        string? machineYaml = null;
        long? cycles = null;
        bool verbose = false;
        bool help = false;
        // ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 / FR-CFG-005 AC6-8: testbench flags for gated recognition
        // (prevents landing in Unknown for harness invocations using debug cart + bounded cycles + prg autostart)
        bool? debugCart = null;
        long? limitCycles = null;
        string? autostartPrg = null;
        var unknown = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            string? Take() => i + 1 < args.Length ? args[++i] : null;

            switch (a)
            {
                case "-8": drive8 = Take(); break;
                case "-9": drive9 = Take(); break;
                case "-cart": cart = Take(); break;
                case "+truedrive": trueDrive = true; break;
                case "-truedrive": trueDrive = false; break;
                case "--machine-yaml":
                case "-m":
                    machineYaml = Take(); break;
                case "--cycles":
                    if (long.TryParse(Take(), out var n)) cycles = n;
                    else unknown.Add($"--cycles (non-numeric)");
                    break;
                case "-v":
                case "--verbose":
                    verbose = true; break;
                case "--help":
                case "-h":
                case "-?":
                    help = true; break;
                // ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 / FR-CFG-005 AC6: -debugcart enables, +debugcart disables.
                // (enables $D7FF result writes for automatic regression test harness signaling per debugcart.c)
                case "-debugcart": debugCart = true; break;
                case "+debugcart": debugCart = false; break;
                // ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 / FR-CFG-005 AC7: limitcycles (testbench bounded execution)
                case "--limitcycles":
                case "-limitcycles":
                    if (long.TryParse(Take(), out var lc)) limitCycles = lc;
                    else unknown.Add($"--limitcycles (non-numeric)");
                    break;
                // ARCH-TESTBENCH-001 / CLI-LAUNCHER-001 / FR-CFG-005 AC8: basic -autostart for prg
                case "-autostart":
                    autostartPrg = Take(); break;
                default:
                    if (a.StartsWith("--machine-yaml=", StringComparison.Ordinal))
                        machineYaml = a["--machine-yaml=".Length..];
                    else if (a.StartsWith("--cycles=", StringComparison.Ordinal) &&
                             long.TryParse(a["--cycles=".Length..], out var nv))
                        cycles = nv;
                    // AC7 = form for consistency with cycles
                    else if (a.StartsWith("--limitcycles=", StringComparison.Ordinal) &&
                             long.TryParse(a["--limitcycles=".Length..], out var lcv))
                        limitCycles = lcv;
                    // AC8 / AC6-8: trailing .prg positional (classic VICE "x64sc ... testcase.prg" autostart)
                    // or any bare *.prg arg; do not treat as unknown (testbench recognition gate)
                    else if (a.EndsWith(".prg", StringComparison.OrdinalIgnoreCase))
                        autostartPrg = a;
                    else
                        unknown.Add(a);
                    break;
            }
        }

        return new ViceArgs
        {
            BinaryName = NormalizeBinaryName(binaryName),
            Drive8Image = drive8,
            Drive9Image = drive9,
            CartridgeImage = cart,
            TrueDrive = trueDrive,
            MachineYamlPath = machineYaml,
            Cycles = cycles,
            // ARCH-TESTBENCH-001 etc: wire the three testbench flags (minimal addition only for this gated slice)
            DebugCart = debugCart,
            LimitCycles = limitCycles,
            AutostartPrg = autostartPrg,
            Verbose = verbose,
            ShowHelp = help,
            Unknown = unknown,
        };
    }

    /// <summary>
    /// Returns a usage/help text string listing all recognized flags.
    /// VICE convention: help is printed when --help / -h / -? is present.
    /// CLI-LAUNCHER-001 / FR-CFG-005.
    /// </summary>
    public static string GetHelpText() =>
        """
        ViceSharp - VICE-compatible C64 substrate

        Usage: x64sc [options] [program.prg]

          -8 path             Attach disk image to drive 8
          -9 path             Attach disk image to drive 9
          -cart path          Attach cartridge image
          +truedrive          Enable true-drive emulation
          -truedrive          Disable true-drive emulation
          --machine-yaml path Machine topology YAML file (-m path)
          --cycles N          Cycle budget for the run
          -v / --verbose      Verbose output
          --help / -h / -?    Show this help text
          -debugcart          Enable debug cartridge device ($D7FF signaling)
          +debugcart          Disable debug cartridge device
          --limitcycles N     Bounded execution cycle limit (testbench)
          -autostart path     Autostart PRG/SID/T64 file
          [program.prg]       Positional PRG autostart (testbench style)
        """;

    private static string NormalizeBinaryName(string name)
    {
        var trimmed = Path.GetFileNameWithoutExtension(name).ToLowerInvariant();
        return trimmed;
    }
}
