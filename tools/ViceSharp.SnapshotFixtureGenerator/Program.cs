using System.Globalization;
using ViceSharp.ViceCompatibility.SnapshotFixtures;

namespace ViceSharp.SnapshotFixtureGeneratorTool;

/// <summary>
/// Commands: generate (one run on this host, writes the candidate receipt, exit code = candidate exit code: 0
/// accepted candidate, 3 blocked, 1 failed), classify (reads candidate receipts of every host, writes the
/// classification, exit 0 accepted or 3 blocked) and publish (copies the accepted candidate to the fixture path and
/// upserts the manifest entry; refuses a blocked classification). Options are "--name value" pairs; repeatable
/// options (--rom, --machine-arg, --receipt, --host) collect every value.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0) return Usage();
        var options = Parse(args.Skip(1));
        try
        {
            return args[0] switch
            {
                "generate" => Generate(options),
                "classify" => Classify(options),
                "publish" => Publish(options),
                _ => Usage(),
            };
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or InvalidDataException or FormatException)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    private static int Generate(Dictionary<string, List<string>> o)
    {
        var spec = new SnapshotFixtureSpec(
            One(o, "fixture-id"), One(o, "machine"), One(o, "model"), Many(o, "machine-arg"),
            ulong.Parse(One(o, "trigger"), CultureInfo.InvariantCulture), Path.GetFullPath(One(o, "observer")), Path.GetFullPath(One(o, "data")),
            Many(o, "rom"), Path.GetFullPath(One(o, "candidate")), Path.GetFullPath(One(o, "stderr")), One(o, "host"),
            o.TryGetValue("limit-margin", out var margin) ? ulong.Parse(margin[0], CultureInfo.InvariantCulture) : 50_000);
        var candidate = new SnapshotFixtureGenerator(new ProcessCommandRunner()).Generate(spec);
        FixturePublisher.SaveCandidate(candidate, Path.GetFullPath(One(o, "receipt")));
        Console.WriteLine($"{candidate.FixtureId} on {candidate.Host}: {candidate.Outcome}; trigger {candidate.Trigger}, write clock {Format(candidate.WriteClock)}, writer result {candidate.WriterResult}, MAINCPU clock {Format(candidate.MainCpuClock)}, sha256 {candidate.CandidateSha256 ?? "absent"}");
        Console.WriteLine(candidate.Reason);
        return candidate.ExitCode;
    }

    private static int Classify(Dictionary<string, List<string>> o)
    {
        var fixtureId = One(o, "fixture-id");
        var trigger = ulong.Parse(One(o, "trigger"), CultureInfo.InvariantCulture);
        var candidates = Many(o, "receipt").Select(p => FixturePublisher.LoadCandidate(Path.GetFullPath(p))).ToArray();
        var runs = o.TryGetValue("runs-per-host", out var r) ? int.Parse(r[0], CultureInfo.InvariantCulture) : 2;
        var classification = SnapshotFixtureClassifier.Classify(fixtureId, trigger, candidates, Many(o, "host"), runs);
        FixturePublisher.SaveClassification(classification, Path.GetFullPath(One(o, "out")));
        if (o.TryGetValue("blockers", out var blockers) && blockers.Count == 1)
            FixtureBlockers.Apply(Path.GetFullPath(blockers[0]), classification, SnapshotFixtureClassifier.BlockerId, DateTime.Now.ToString("s", CultureInfo.InvariantCulture));
        Console.WriteLine($"{fixtureId}: {classification.Outcome}" + (classification.AcceptedSha256 is null ? "" : $" sha256 {classification.AcceptedSha256}"));
        foreach (var clock in classification.Clocks)
            Console.WriteLine($"  {clock.Host}: trigger {clock.Trigger}, write clock {Format(clock.WriteClock)}, writer result {clock.WriterResult}, MAINCPU clock {Format(clock.MainCpuClock)}, {clock.Outcome}");
        foreach (var reason in classification.Reasons)
            Console.WriteLine("  " + reason);
        return classification.IsAccepted ? 0 : FixtureCandidate.BlockedExit;
    }

    private static int Publish(Dictionary<string, List<string>> o)
    {
        var classificationPath = Path.GetFullPath(One(o, "classification"));
        var classification = System.Text.Json.JsonSerializer.Deserialize(File.ReadAllText(classificationPath), FixtureManifestJsonContext.Default.FixtureClassification)
            ?? throw new InvalidDataException("empty classification");
        var candidate = FixturePublisher.LoadCandidate(Path.GetFullPath(One(o, "receipt")));
        var entry = new FixtureManifestEntry
        {
            Path = One(o, "manifest-path"), Sha256 = candidate.CandidateSha256 ?? "", Length = candidate.CandidateLength ?? 0,
            Machine = candidate.Machine, Model = candidate.Model, Command = candidate.Command, TriggerClock = candidate.Trigger,
            WriteClock = candidate.WriteClock ?? 0, RomHashes = [.. candidate.Roms.Select(x => x.Sha256 ?? "")], MediaHashes = [],
            Hosts = [.. Many(o, "host")], ObserverSha256 = candidate.ObserverSha256,
        };
        var result = FixturePublisher.Publish(classification, candidate.CandidatePath ?? throw new InvalidDataException("candidate path missing"), Path.GetFullPath(One(o, "fixture")), Path.GetFullPath(One(o, "manifest")), entry);
        Console.WriteLine($"published {result.FixturePath} (changed {result.FixtureChanged}); manifest {result.ManifestPath} (changed {result.ManifestChanged})");
        return 0;
    }

    private static Dictionary<string, List<string>> Parse(IEnumerable<string> args)
    {
        var options = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        string? name = null;
        foreach (var a in args)
        {
            if (a.StartsWith("--", StringComparison.Ordinal)) { name = a[2..]; options.TryAdd(name, []); continue; }
            if (name is null) throw new ArgumentException($"value '{a}' without an option");
            options[name].Add(a);
            name = null;
        }
        return options;
    }

    private static string One(Dictionary<string, List<string>> o, string name)
        => o.TryGetValue(name, out var v) && v.Count == 1 ? v[0] : throw new ArgumentException($"--{name} is required exactly once");

    private static IReadOnlyList<string> Many(Dictionary<string, List<string>> o, string name)
        => o.TryGetValue(name, out var v) ? v : [];

    private static string Format(ulong? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "none";

    private static int Usage()
    {
        Console.Error.WriteLine("usage: generate|classify|publish --option value ... (see Program.cs)");
        return 64;
    }
}
