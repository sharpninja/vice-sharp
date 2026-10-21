using System.Globalization;
using System.Security.Cryptography;

namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 (R12, N21, N25): launches an observer binary with <c>VICESHARP_SNAPSHOT_AT</c> set to the
/// literal trigger and a candidate path, verifies the ROM allowlist before the launch, reads the checkpoint record,
/// and classifies the run. A candidate is accepted only when the writer succeeded, the measured write clock equals
/// the trigger and the candidate's MAINCPU clock equals it too; a later boundary is BLOCKED (never approximated,
/// nothing re-armed); any other defect is a failure. The run never publishes a fixture or touches a manifest.
/// </summary>
public sealed class SnapshotFixtureGenerator
{
    /// <summary>Environment variable read by tools/immutable-vice/snapshot-checkpoint.c.</summary>
    public const string CheckpointVariable = "VICESHARP_SNAPSHOT_AT";

    private readonly ICommandRunner _runner;
    private readonly IReadOnlyDictionary<string, string> _allowlist;

    /// <summary>Creates a generator over a runner; the allowlist defaults to the base fixture subset.</summary>
    public SnapshotFixtureGenerator(ICommandRunner runner, IReadOnlyDictionary<string, string>? allowlist = null)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _allowlist = allowlist ?? RomAllowlist.BaseFixtureSubset;
    }

    /// <summary>
    /// The exact observer command line: -default, the model, the machine arguments, -seed 1 (deterministic RAM
    /// init), +logcolorize, -sounddev dummy, the data root and the absolute cycle limit of trigger plus margin.
    /// </summary>
    public static string[] BuildArguments(SnapshotFixtureSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var limit = checked(spec.Trigger + spec.LimitMargin);
        return
        [
            "-default", "-model", spec.Model, .. spec.MachineArguments,
            "-seed", "1", "+logcolorize", "-sounddev", "dummy", "-directory", spec.DataRoot,
            "-limitcycles", limit.ToString(CultureInfo.InvariantCulture)
        ];
    }

    /// <summary>Runs one generation and classifies it; never throws for a launch defect, always returns a candidate.</summary>
    public FixtureCandidate Generate(SnapshotFixtureSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var roms = RomAllowlist.Verify(spec.DataRoot, spec.RequiredRoms, _allowlist);
        var arguments = BuildArguments(spec);
        var command = string.Join(' ', new[] { spec.ObserverPath }.Concat(arguments).Select(Quote));
        var baseline = new FixtureCandidate
        {
            FixtureId = spec.FixtureId, Host = spec.Host, Machine = spec.Machine, Model = spec.Model, Trigger = spec.Trigger,
            Command = command, Roms = roms, CandidatePath = spec.CandidatePath, WriterResult = -3, ExitCode = FixtureCandidate.FailedExit,
            BlockedIds = [spec.FixtureId],
        };
        if (roms.Count == 0)
            return Fail(baseline, "no ROM file listed for verification; the launch is refused");
        var badRoms = roms.Where(r => !r.Ok).ToArray();
        if (badRoms.Length > 0)
            return Fail(baseline, "ROM allowlist: " + string.Join("; ", badRoms.Select(r => r.RelativePath + " " + r.Reason)));
        if (!File.Exists(spec.ObserverPath))
            return Fail(baseline, $"observer binary missing: {spec.ObserverPath}");
        baseline = Copy(baseline, c => c.ObserverSha256 = HashFile(spec.ObserverPath));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(spec.CandidatePath))!);
        if (File.Exists(spec.CandidatePath))
            File.Delete(spec.CandidatePath);
        var environment = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [CheckpointVariable] = spec.Trigger.ToString(CultureInfo.InvariantCulture) + ":" + spec.CandidatePath,
        };
        var request = new CommandRequest(spec.ObserverPath, arguments, Path.GetDirectoryName(Path.GetFullPath(spec.ObserverPath))!, environment, spec.StderrPath);
        var result = _runner.Run(request);
        baseline = Copy(baseline, c => { c.ProcessExitCode = result.ExitCode; c.TimedOut = result.TimedOut; c.SnapshotLines = result.SnapshotLines; });

        var checkpoint = result.SnapshotLines.Select(ParseCheckpoint).FirstOrDefault(p => p is not null);
        if (result.TimedOut)
            return Fail(WithBytes(baseline, spec.CandidatePath), "the observer did not exit within the budget; no checkpoint accepted");
        if (checkpoint is null)
            return Fail(WithBytes(Copy(baseline, c => c.WriterResult = -1), spec.CandidatePath), "no VICE_SNAP checkpoint record: the limit was reached before the trap, or the observer is not the checkpoint build");
        var (writeClock, writerResult) = checkpoint.Value;
        baseline = Copy(baseline, c => { c.WriteClock = writeClock; c.WriterResult = writerResult; });
        if (writerResult != 0)
            return Fail(WithBytes(baseline, spec.CandidatePath), $"machine_write_snapshot returned {writerResult} at write clock {writeClock}");
        baseline = WithBytes(baseline, spec.CandidatePath);
        if (writeClock != spec.Trigger)
            return Block(baseline, $"{SnapshotFixtureClassifier.BlockerId}: trigger {spec.Trigger}, measured write clock {writeClock} (delta {(long)(writeClock - spec.Trigger)} cycles); the trap ran at the next instruction boundary, nothing re-armed");
        if (baseline.CandidateSha256 is null)
            return Fail(baseline, "the writer reported success but no candidate bytes exist");
        ulong mainCpuClock;
        try
        {
            mainCpuClock = X64ScNativeLaunch.ReadMainCpu(File.ReadAllBytes(spec.CandidatePath)).Clock;
        }
        catch (InvalidDataException e)
        {
            return Fail(baseline, "candidate MAINCPU unreadable: " + e.Message);
        }
        baseline = Copy(baseline, c => c.MainCpuClock = mainCpuClock);
        if (mainCpuClock != spec.Trigger)
            return Block(baseline, $"{SnapshotFixtureClassifier.BlockerId}: the candidate's MAINCPU clock {mainCpuClock} differs from the trigger {spec.Trigger} (write clock {writeClock})");
        if (result.ExitCode != 0)
            return Fail(baseline, $"the observer exited with code {result.ExitCode} after the checkpoint");
        return Copy(baseline, c => { c.ExitCode = FixtureCandidate.AcceptedExit; c.BlockedIds = []; c.Reason = "accepted candidate: write clock and MAINCPU clock equal the trigger"; });
    }

    private static (ulong WriteClock, int WriterResult)? ParseCheckpoint(string line)
    {
        var parts = line.Split(',');
        if (parts.Length >= 4 && parts[0] == "VICE_SNAP" && parts[1] == "checkpoint" && ulong.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var clock))
            return (clock, 0);
        if (parts.Length >= 5 && parts[0] == "VICE_SNAP" && parts[1] == "checkpoint-fail"
            && ulong.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var failClock)
            && int.TryParse(parts[4], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var code))
            return (failClock, code == 0 ? -1 : code);
        return null;
    }

    private static FixtureCandidate WithBytes(FixtureCandidate c, string candidatePath)
    {
        if (!File.Exists(candidatePath))
            return Copy(c, x => { x.CandidateSha256 = null; x.CandidateLength = null; });
        var info = new FileInfo(candidatePath);
        return Copy(c, x => { x.CandidateSha256 = HashFile(candidatePath); x.CandidateLength = info.Length; });
    }

    private static FixtureCandidate Fail(FixtureCandidate c, string reason) => Copy(c, x => { x.ExitCode = FixtureCandidate.FailedExit; x.BlockedIds = [c.FixtureId]; x.Reason = reason; });
    private static FixtureCandidate Block(FixtureCandidate c, string reason) => Copy(c, x => { x.ExitCode = FixtureCandidate.BlockedExit; x.BlockedIds = [c.FixtureId]; x.Reason = reason; });

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string Quote(string s) => s.Contains(' ') ? "\"" + s + "\"" : s;

    private sealed class Mutable
    {
        public string? ObserverSha256; public int ProcessExitCode; public bool TimedOut; public IReadOnlyList<string> SnapshotLines = [];
        public ulong? WriteClock; public int WriterResult; public ulong? MainCpuClock; public string? CandidateSha256; public long? CandidateLength;
        public int ExitCode; public IReadOnlyList<string> BlockedIds = []; public string Reason = "";
    }

    private static FixtureCandidate Copy(FixtureCandidate c, Action<Mutable> mutate)
    {
        var m = new Mutable
        {
            ObserverSha256 = c.ObserverSha256, ProcessExitCode = c.ProcessExitCode, TimedOut = c.TimedOut, SnapshotLines = c.SnapshotLines,
            WriteClock = c.WriteClock, WriterResult = c.WriterResult, MainCpuClock = c.MainCpuClock, CandidateSha256 = c.CandidateSha256,
            CandidateLength = c.CandidateLength, ExitCode = c.ExitCode, BlockedIds = c.BlockedIds, Reason = c.Reason,
        };
        mutate(m);
        return new FixtureCandidate
        {
            FixtureId = c.FixtureId, Host = c.Host, Machine = c.Machine, Model = c.Model, Trigger = c.Trigger, Command = c.Command, Roms = c.Roms,
            CandidatePath = c.CandidatePath, ObserverSha256 = m.ObserverSha256, ProcessExitCode = m.ProcessExitCode, TimedOut = m.TimedOut,
            SnapshotLines = m.SnapshotLines, WriteClock = m.WriteClock, WriterResult = m.WriterResult, MainCpuClock = m.MainCpuClock,
            CandidateSha256 = m.CandidateSha256, CandidateLength = m.CandidateLength, ExitCode = m.ExitCode, BlockedIds = m.BlockedIds, Reason = m.Reason,
        };
    }
}
