using Gb64Import;

// Usage:
//   Gb64Import --games <gb64/Games> --out <runtime/library/roms/c64> [--hvsc ...] [--limit N] [--force]
//   Gb64Import --validate [--games ...] [--out ...] [--limit N]
// Defaults relative to repo root (cwd or parent of tools).

static string Arg(string[] a, string name, string? def = null)
{
    for (var i = 0; i < a.Length - 1; i++)
        if (a[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            return a[i + 1];
    return def ?? "";
}

static bool Flag(string[] a, string name) =>
    a.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));

var repo = Directory.GetCurrentDirectory();
// walk up for tools folder
for (var d = new DirectoryInfo(repo); d is not null; d = d.Parent)
{
    if (Directory.Exists(Path.Combine(d.FullName, "gb64", "Games")) ||
        Directory.Exists(Path.Combine(d.FullName, "tools", "Gb64Import")))
    {
        repo = d.FullName;
        break;
    }
}

var games = Arg(args, "--games", Path.Combine(repo, "gb64", "Games"));
var romsOut = Arg(args, "--out", Path.Combine(repo, "runtime", "library", "roms", "c64"));
var hvsc = Arg(args, "--hvsc", Path.Combine(repo, "runtime", "library", "hvsc"));
var shots = Arg(args, "--screenshots", Path.Combine(repo, "runtime", "library", "screenshots"));
var limitStr = Arg(args, "--limit", "0");
_ = int.TryParse(limitStr, out var limit);
var force = Flag(args, "--force");
var validate = Flag(args, "--validate");
var validateAssets = Flag(args, "--validate-assets");
var fixSids = Flag(args, "--fix-sids");
var dryRun = Flag(args, "--dry-run");
var dopStr = Arg(args, "--dop", "0");
_ = int.TryParse(dopStr, out var dop);

if (!Directory.Exists(games))
{
    Console.Error.WriteLine($"error: games root not found: {games}");
    return 2;
}

if (fixSids)
{
    var logPath = Arg(args, "--log", Path.Combine(repo, "runtime", "library", "sid-nfo-fix-receipt.txt"));
    var fopt = new SidNfoFixer.Options
    {
        GamesRoot = Path.GetFullPath(games),
        HvscRoot = Path.GetFullPath(hvsc),
        RomsOut = Directory.Exists(romsOut) ? Path.GetFullPath(romsOut) : null,
        DryRun = dryRun,
        Limit = limit,
        LogPath = logPath,
    };
    Console.Error.WriteLine($"fix-sids games={fopt.GamesRoot}");
    Console.Error.WriteLine($"fix-sids hvsc={fopt.HvscRoot}");
    Console.Error.WriteLine($"fix-sids dryRun={fopt.DryRun}");
    var fr = SidNfoFixer.Run(fopt);
    if (fr.Samples.Count > 0)
    {
        Console.Error.WriteLine("Samples:");
        foreach (var s in fr.Samples)
            Console.Error.WriteLine("  " + s);
    }
    return fr.Failed > 0 ? 1 : 0;
}

if (validateAssets)
{
    var aopt = new AssetPathValidator.Options
    {
        GamesRoot = Path.GetFullPath(games),
        HvscRoot = Path.GetFullPath(hvsc),
        ScreenshotsRoot = Path.GetFullPath(shots),
        Limit = limit,
        MaxDegreeOfParallelism = dop > 0 ? dop : Math.Max(2, Environment.ProcessorCount / 2),
    };
    var a = AssetPathValidator.Run(aopt);
    if (a.ShotSamples.Count > 0)
    {
        Console.Error.WriteLine("Sample missing screenshots:");
        foreach (var s in a.ShotSamples)
            Console.Error.WriteLine("  " + s);
    }
    if (a.SidSamples.Count > 0)
    {
        Console.Error.WriteLine("Sample missing SIDs:");
        foreach (var s in a.SidSamples)
            Console.Error.WriteLine("  " + s);
    }
    if (a.ShotMissingPrefixes.Count > 0)
    {
        Console.Error.WriteLine("Missing screenshot prefixes:");
        foreach (var kv in a.ShotMissingPrefixes)
            Console.Error.WriteLine($"  {kv.Value,6}  {kv.Key}");
    }
    if (a.SidMissingPrefixes.Count > 0)
    {
        Console.Error.WriteLine("Missing SID prefixes:");
        foreach (var kv in a.SidMissingPrefixes)
            Console.Error.WriteLine($"  {kv.Value,6}  {kv.Key}");
    }

    // Pass only if every *declared* path resolves (blanks are OK)
    var pass = a.ShotMissing == 0 && a.SidMissing == 0 && a.NfoPresent > 0;
    Console.Error.WriteLine(pass
        ? $"VALIDATE-ASSETS PASS shots={a.ShotOk}/{a.ShotDeclared} sids={a.SidOk}/{a.SidDeclared}"
        : $"VALIDATE-ASSETS FAIL shots missing={a.ShotMissing}/{a.ShotDeclared} sids missing={a.SidMissing}/{a.SidDeclared}");
    return pass ? 0 : 1;
}

if (validate)
{
    if (!Directory.Exists(romsOut))
    {
        Console.Error.WriteLine($"error: roms out not found: {romsOut}");
        return 2;
    }
    var vopt = new MetadataValidator.ValidateOptions
    {
        GamesRoot = Path.GetFullPath(games),
        RomsOut = Path.GetFullPath(romsOut),
        Limit = limit,
        MaxDegreeOfParallelism = dop > 0 ? dop : Math.Max(2, Environment.ProcessorCount / 2),
    };
    Console.Error.WriteLine($"validate games={vopt.GamesRoot}");
    Console.Error.WriteLine($"validate out={vopt.RomsOut}");
    var v = MetadataValidator.Run(vopt);
    if (v.Samples.Count > 0)
    {
        Console.Error.WriteLine("Sample mismatches:");
        foreach (var s in v.Samples)
            Console.Error.WriteLine("  " + s);
    }
    // Pass if every checked item with media is OK
    var actionable = v.Checked - v.NoMedia;
    var pass = v.Mismatch == 0 && v.Ok == actionable;
    Console.Error.WriteLine(pass
        ? $"VALIDATE PASS ok={v.Ok}/{actionable}"
        : $"VALIDATE FAIL ok={v.Ok} mismatch={v.Mismatch} missing={v.MissingOnDisk}");
    return pass ? 0 : 1;
}

var opt = new ImportOptions
{
    GamesRoot = Path.GetFullPath(games),
    RomsOut = Path.GetFullPath(romsOut),
    HvscRoot = Directory.Exists(hvsc) ? Path.GetFullPath(hvsc) : null,
    ScreenshotsRoot = Directory.Exists(shots) ? Path.GetFullPath(shots) : null,
    Limit = limit,
    Force = force,
    MaxDegreeOfParallelism = dop > 0 ? dop : Math.Max(2, Environment.ProcessorCount / 2),
};

Console.Error.WriteLine($"repo={repo}");
Console.Error.WriteLine($"games={opt.GamesRoot}");
Console.Error.WriteLine($"out={opt.RomsOut}");
Console.Error.WriteLine($"hvsc={opt.HvscRoot ?? "(none)"}");
Console.Error.WriteLine($"screenshots={opt.ScreenshotsRoot ?? "(none)"}");

var result = Importer.Run(opt);
if (result.Errors.Count > 0)
{
    Console.Error.WriteLine("Sample errors:");
    foreach (var e in result.Errors.Take(20))
        Console.Error.WriteLine("  " + e);
}

return result.Failed > 0 && result.Written == 0 ? 1 : 0;
