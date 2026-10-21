using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ViceSharp.Build.Testing;

/// <summary>
/// PLAN-NATIVERESTORE-001 D2 (round 5 N14): reads the TRX files of a run and classifies every case as passed,
/// failed, blocked (a failure whose message starts with <c>BLOCKED:</c>, the third outcome the cumulative target
/// reports beside pass and fail), skipped or not executed. A run with zero selected cases is nonpassing.
/// </summary>
internal static class TrxOutcomes
{
    /// <summary>Prefix a real-bound VSF case uses when it fails because of a named blocker.</summary>
    internal const string BlockedPrefix = "BLOCKED:";

    internal sealed record TrxCase(string Project, string FullName, string Outcome, string? Message);

    internal sealed record TrxSummary(int Total, int Passed, int Failed, int Blocked, int Skipped, int NotExecuted, IReadOnlyList<TrxCase> Cases)
    {
        public IEnumerable<TrxCase> BlockedCases => Cases.Where(c => c.Outcome == "Blocked");
        public IEnumerable<TrxCase> FailedCases => Cases.Where(c => c.Outcome == "Failed");
    }

    /// <summary>Summarizes every TRX file given.</summary>
    internal static TrxSummary Summarize(IEnumerable<string> trxPaths)
    {
        var cases = new List<TrxCase>();
        foreach (var path in trxPaths)
        {
            var document = XDocument.Load(path);
            var project = Path.GetFileNameWithoutExtension(path);
            var methods = document.Descendants().Where(e => e.Name.LocalName == "UnitTest")
                .ToDictionary(
                    e => e.Attribute("id")!.Value,
                    e =>
                    {
                        var method = e.Descendants().First(x => x.Name.LocalName == "TestMethod");
                        return method.Attribute("className")!.Value + "." + method.Attribute("name")!.Value;
                    });
            foreach (var result in document.Descendants().Where(e => e.Name.LocalName == "UnitTestResult"))
            {
                var id = result.Attribute("testId")!.Value;
                var outcome = result.Attribute("outcome")?.Value ?? "NotExecuted";
                var message = result.Descendants().FirstOrDefault(e => e.Name.LocalName == "Message")?.Value;
                if (outcome == "Failed" && message is not null && message.TrimStart().StartsWith(BlockedPrefix, StringComparison.Ordinal))
                    outcome = "Blocked";
                cases.Add(new TrxCase(project, methods.TryGetValue(id, out var name) ? name : result.Attribute("testName")!.Value, outcome, message));
            }
        }
        return new TrxSummary(
            cases.Count,
            cases.Count(c => c.Outcome == "Passed"),
            cases.Count(c => c.Outcome == "Failed"),
            cases.Count(c => c.Outcome == "Blocked"),
            cases.Count(c => c.Outcome == "NotExecuted" && c.Message is null ? false : c.Outcome is "Skipped" or "Inconclusive"),
            cases.Count(c => c.Outcome is "NotExecuted" or "Aborted" or "Timeout" or "Pending" or "InProgress"),
            cases);
    }

    /// <summary>The cumulative target's verdict: passing only when at least one case ran and every case passed.</summary>
    internal static (bool Passing, string Reason) Evaluate(TrxSummary summary)
    {
        if (summary.Total == 0)
            return (false, "no case selected: the cumulative gate has nothing to attest");
        var problems = new List<string>();
        if (summary.Blocked > 0) problems.Add($"{summary.Blocked} BLOCKED: " + string.Join(", ", summary.BlockedCases.Select(c => c.FullName)));
        if (summary.Failed > 0) problems.Add($"{summary.Failed} failed: " + string.Join(", ", summary.FailedCases.Select(c => c.FullName)));
        if (summary.Skipped > 0) problems.Add($"{summary.Skipped} skipped (never counted as passing)");
        if (summary.NotExecuted > 0) problems.Add($"{summary.NotExecuted} not executed");
        return problems.Count == 0
            ? (true, $"{summary.Passed} of {summary.Total} passed")
            : (false, string.Join("; ", problems));
    }
}
