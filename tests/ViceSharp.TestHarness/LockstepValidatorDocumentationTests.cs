using System.Text.RegularExpressions;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-NATIVERESTORE-001 D5 (F10): the class summary of <see cref="LockstepValidator"/> must describe what the class
/// does today (an IViceNative, register and CIA based validator bound to the retired shim, ShimLegacy until Phase E
/// item 1) and must not describe the future observer-field, half-cycle comparator as implemented.
/// XmlDocsConventionTests scans attributed test methods only, so this test reads the source file itself.
/// </summary>
public sealed partial class LockstepValidatorDocumentationTests
{
    [GeneratedRegex(@"/// <summary>\s*(?<body>(?:///.*\n)+?)\s*/// </summary>\s*public sealed partial class LockstepValidator\b")]
    private static partial Regex ClassSummary();

    private static string ReadClassSummary()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "ViceSharp.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var path = Path.Combine(root.FullName, "tests", "ViceSharp.TestHarness", "LockstepValidator.cs");
        var match = ClassSummary().Match(File.ReadAllText(path).Replace("\r\n", "\n"));
        Assert.True(match.Success, "class summary of LockstepValidator not found in " + path);
        return string.Join(' ', match.Groups["body"].Value.Split('\n').Select(l => l.Trim().TrimStart('/').Trim()));
    }

    /// <summary>
    /// TR-CYCLE-001 AC4 context, PLAN-NATIVERESTORE-001 D5 (comparator XML docs).
    /// Use case: a reader of the class summary learns the current binding and status of the validator.
    /// Acceptance: the summary names the retired shim binding, the IViceNative, register and CIA comparison, the
    /// ShimLegacy category and the Phase E rebinding, names CompareHalfCycleStates as the only implemented piece, and
    /// does not call the half-cycle comparator implemented or the validator cycle accurate against the oracle.
    /// </summary>
    [Fact]
    public void ClassSummary_DescribesTheRetiredShimBindingAndNotTheFutureComparator()
    {
        var summary = ReadClassSummary();
        foreach (var required in new[] { "retired VICE shim", "IViceNative", "CPU registers and CIA state", "ShimLegacy", "Phase E", "half-cycle comparator", "CompareHalfCycleStates", "only part implemented today" })
            Assert.Contains(required, summary, StringComparison.Ordinal);
        Assert.DoesNotContain("cycle accurate validator comparing ViceSharp against original VICE", summary, StringComparison.Ordinal);
        Assert.Contains("the comparator itself is not implemented by this class", summary, StringComparison.Ordinal);
    }
}
