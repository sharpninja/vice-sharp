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
                default:
                    if (a.StartsWith("--machine-yaml=", StringComparison.Ordinal))
                        machineYaml = a["--machine-yaml=".Length..];
                    else if (a.StartsWith("--cycles=", StringComparison.Ordinal) &&
                             long.TryParse(a["--cycles=".Length..], out var nv))
                        cycles = nv;
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
            Verbose = verbose,
            ShowHelp = help,
            Unknown = unknown,
        };
    }

    private static string NormalizeBinaryName(string name)
    {
        var trimmed = Path.GetFileNameWithoutExtension(name).ToLowerInvariant();
        return trimmed;
    }
}
