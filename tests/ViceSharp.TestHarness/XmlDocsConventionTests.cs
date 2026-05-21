namespace ViceSharp.TestHarness;

using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// Enforces the QA-XMLDOCS-001 contract: every test method declared with
/// <c>[Fact]</c>, <c>[Theory]</c>, <c>[ViceFact]</c>, or <c>[ViceTheory]</c>
/// MUST carry an XML doc comment that identifies the requirement(s) under
/// test, the use case being exercised, and the acceptance criteria.
///
/// Required tokens (case-sensitive) in the XML doc block immediately above
/// a test method:
///   - <c>FR-</c> or <c>TR-</c> (a functional or technical requirement id)
///   - <c>Use case:</c>
///   - <c>Acceptance:</c>
///
/// Rollout policy: this convention uses a zero-violation ratchet. Landing new
/// tests is fine only if their XMLDOCS are present.
///
/// Set the environment variable <c>VICESHARP_XMLDOCS_ENFORCE=1</c> at any
/// time to force zero violations regardless of the ratchet baseline.
/// QA-XMLDOCS-001 is closed when <see cref="ExpectedMaxViolations"/> remains 0.
/// </summary>
public sealed class XmlDocsConventionTests
{
    /// <summary>
    /// Ratchet baseline. Reduce this as XMLDOCS coverage grows.
    /// MUST never be raised: a PR that adds new undocumented tests should
    /// document them, not loosen the ratchet.
    /// </summary>
    private const int ExpectedMaxViolations = 0;
    // QA-XMLDOCS-001 closed 2026-05-18 - all top-level TestHarness methods
    // carry FR/TR + Use case + Acceptance markers. Adhoc + Benchmark
    // tests retrofitted in this same merge to keep the count from drifting.

    private static readonly Regex TestMethodPattern = new(
        @"(?<doc>(?:^[ \t]*///[^\n]*\n)+)?[ \t]*\[(?:Xunit\.)?(?<attr>Fact|Theory|ViceFact|ViceTheory)(?:Attribute)?(?:\([^)]*\))?\][^\n]*\n(?:[ \t]*\[[^\]]+\][^\n]*\n)*[ \t]*public\s+(?:async\s+)?(?:Task|ValueTask|void)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\(",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// FR: FR-QA-XMLDOCS, TR: TR-QA-XMLDOCS-CONVENTION.
    /// Use case: Continuous-integration verification that every xUnit test
    /// method in the test harness assembly carries an XML doc block citing
    /// the requirement, use case, and acceptance criteria.
    /// Acceptance: The number of test methods missing the required XMLDOCS
    /// tokens is less than or equal to the recorded ratchet baseline; when
    /// the <c>VICESHARP_XMLDOCS_ENFORCE</c> environment variable is set to
    /// <c>1</c>, the count MUST be zero.
    /// </summary>
    [Fact]
    public void TestMethods_HaveRequiredXmlDocs()
    {
        var sourceDirectory = ResolveSourceDirectory();
        var sourceFiles = Directory.EnumerateFiles(sourceDirectory, "*.cs", SearchOption.TopDirectoryOnly);

        var violations = new List<string>();

        foreach (var file in sourceFiles)
        {
            var relative = Path.GetFileName(file);
            if (relative.Equals(typeof(XmlDocsConventionTests).Name + ".cs", StringComparison.OrdinalIgnoreCase))
                continue;

            var contents = File.ReadAllText(file);
            foreach (Match match in TestMethodPattern.Matches(contents))
            {
                var methodName = match.Groups["name"].Value;
                var doc = match.Groups["doc"].Success ? match.Groups["doc"].Value : string.Empty;

                if (!HasRequirementCitation(doc) || !HasUseCase(doc) || !HasAcceptance(doc))
                    violations.Add($"{relative}::{methodName}");
            }
        }

        var enforce = string.Equals(
            Environment.GetEnvironmentVariable("VICESHARP_XMLDOCS_ENFORCE"),
            "1",
            StringComparison.Ordinal);

        if (enforce)
        {
            Assert.True(
                violations.Count == 0,
                FormatFailure(violations, sourceDirectory, expected: 0));
            return;
        }

        Assert.True(
            violations.Count <= ExpectedMaxViolations,
            FormatFailure(violations, sourceDirectory, expected: ExpectedMaxViolations));
    }

    private static bool HasRequirementCitation(string doc)
    {
        return doc.Contains("FR-", StringComparison.Ordinal)
            || doc.Contains("TR-", StringComparison.Ordinal);
    }

    private static bool HasUseCase(string doc) =>
        doc.Contains("Use case:", StringComparison.OrdinalIgnoreCase);

    private static bool HasAcceptance(string doc) =>
        doc.Contains("Acceptance:", StringComparison.OrdinalIgnoreCase);

    private static string FormatFailure(IReadOnlyList<string> violations, string sourceDirectory, int expected)
    {
        var sample = string.Join(
            Environment.NewLine,
            violations.Take(20).Select(v => "  - " + v));

        var overflow = violations.Count > 20
            ? Environment.NewLine + $"  ... and {violations.Count - 20} more"
            : string.Empty;

        return
            $"QA-XMLDOCS-001: found {violations.Count} test method(s) missing required XMLDOCS tokens "
            + $"(FR-/TR-, 'Use case:', 'Acceptance:'); expected at most {expected}." + Environment.NewLine
            + $"Source dir scanned: {sourceDirectory}" + Environment.NewLine
            + "Sample violations:" + Environment.NewLine
            + sample
            + overflow;
    }

    private static string ResolveSourceDirectory()
    {
        var metadata = typeof(XmlDocsConventionTests)
            .Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => string.Equals(
                a.Key,
                "TestHarnessSourceDirectory",
                StringComparison.Ordinal));

        if (metadata?.Value is { Length: > 0 } recordedPath && Directory.Exists(recordedPath))
            return recordedPath;

        // Fallback: walk up from the test assembly's bin/ until we find this file.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "ViceSharp.TestHarness.csproj");
            if (File.Exists(candidate))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException(
            "Unable to resolve ViceSharp.TestHarness source directory for XMLDOCS scan. "
            + "Ensure the TestHarnessSourceDirectory AssemblyMetadata attribute is set in the csproj.");
    }
}
