namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S35 (IMPL-XBOXUWP-035). This is the skip-free half of the
/// desktop-intact + full-suite + AOT regression guard: it PROVES that a
/// <c>--filter Category=Xbox</c> run can never report <c>Skipped&gt;0</c> for an
/// environment reason, so the S35 acceptance criterion "Category=Xbox Skipped=0"
/// is a code-enforced invariant rather than a lucky property of the current agent.
///
/// <para>
/// Every Category=Xbox test is a Tier H, off-console gate: a plain xUnit
/// <c>[Fact]</c>/<c>[Theory]</c> that always executes. The native-gated
/// <see cref="ViceFactAttribute"/> (and the xunit.v3 dynamic-skip APIs
/// <c>Assert.Skip</c>/<c>Assert.SkipUnless</c>/<c>Assert.SkipWhen</c>) exist to skip
/// when the native VICE shim / an environment precondition is absent - legitimate for
/// the native parity suite, but forbidden inside the Category=Xbox scope, because a
/// skip there would silently erode the console program's off-console coverage and
/// break the S35 zero-skip guard.
/// </para>
///
/// <para>
/// Scan scope (documented): the union of
/// <list type="number">
///   <item><description>every <c>*.cs</c> under the designated Category=Xbox test home
///     <c>tests/ViceSharp.TestHarness/Xbox/</c> (recursive, skipping <c>obj</c>/<c>bin</c>),
///     including this file and the <c>Fakes/</c> helpers; and</description></item>
///   <item><description>every <c>*.cs</c> anywhere under
///     <c>tests/ViceSharp.TestHarness/</c> that itself declares the
///     <c>[Trait("Category","Xbox")]</c> marker - which widens the scan to the
///     Category=Xbox tests that live OUTSIDE the home folder (e.g. the
///     <c>HostShells/</c> Xbox head-shell/manifest tests), so a stray skip in one of
///     them is caught too.</description></item>
/// </list>
/// This union is exactly the set of source a <c>--filter Category=Xbox</c> run compiles
/// and executes.
/// </para>
///
/// <para>
/// The match is precise: it strips comments and string/char/verbatim/raw/interpolated
/// literals BEFORE matching (the same idiom as <see cref="XboxAotLinkTests"/>), so the
/// many XML-doc mentions of "<c>[ViceFact]</c>" / "<c>Assert.Skip</c>" that DESCRIBE this
/// convention (in this file and in sibling audio tests) are not false hits, and this
/// file's own banned-token search strings (regex/string literals) do not flag itself.
/// Only a real <c>[ViceFact]</c>/<c>[ViceTheory]</c> attribute or a real
/// <c>Assert.Skip*</c> call in executable code counts.
/// </para>
///
/// <para>
/// FR: FR-TESTGATE-002 / FR-TESTGATE-003 (delivery-process regression guard: the Xbox
/// program stays fully green and skip-free off-console). TR: TR-XBOXTOPO-003.
/// TEST-TESTGATE-009 (DesktopRegressionGuard, skip-free half).
/// </para>
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxTestConventionTests
{
    /// <summary>The Category=Xbox test home, relative to the repo root.</summary>
    private static readonly string XboxHomeRelativeDir =
        Path.Combine("tests", "ViceSharp.TestHarness", "Xbox");

    /// <summary>The whole test harness, relative to the repo root (for the widened scan).</summary>
    private static readonly string TestHarnessRelativeDir =
        Path.Combine("tests", "ViceSharp.TestHarness");

    /// <summary>Matches the <c>[Trait("Category","Xbox")]</c> marker with flexible whitespace.</summary>
    private static readonly Regex XboxCategoryTraitMarker = new(
        @"\[\s*Trait\s*\(\s*""Category""\s*,\s*""Xbox""\s*\)\s*\]",
        RegexOptions.Compiled);

    /// <summary>
    /// The environment-gated skip constructs banned inside the Category=Xbox scope.
    /// Each is matched against the comment/literal-stripped source so only real code
    /// usages count. <c>[ViceFact</c>/<c>[ViceTheory</c> catch the attribute with or
    /// without a trailing <c>Attribute</c>/argument list; <c>Assert.Skip</c> (no
    /// trailing boundary) catches <c>Assert.Skip</c>, <c>Assert.SkipUnless</c>, and
    /// <c>Assert.SkipWhen</c>, while the required <c>Assert.</c> prefix keeps benign
    /// LINQ <c>.Skip(</c> calls from matching.
    /// </summary>
    private static readonly (Regex Pattern, string Name)[] BannedSkipConstructs =
    {
        (new Regex(@"\[\s*ViceFact", RegexOptions.Compiled), "[ViceFact] (native-gated skip)"),
        (new Regex(@"\[\s*ViceTheory", RegexOptions.Compiled), "[ViceTheory] (native-gated skip)"),
        (new Regex(@"\bAssert\s*\.\s*Skip", RegexOptions.Compiled), "Assert.Skip / Assert.SkipUnless / Assert.SkipWhen"),
    };

    /// <summary>
    /// FR-TESTGATE-002 / FR-TESTGATE-003, TR-XBOXTOPO-003, TEST-TESTGATE-009.
    /// Use case: a <c>--filter Category=Xbox</c> run is the off-console proof of the
    /// console program; it must never report <c>Skipped&gt;0</c> for an environment
    /// reason, or that proof is silently hollowed out.
    /// Acceptance: no source file in the Category=Xbox scope (the Xbox home plus every
    /// widened <c>[Trait("Category","Xbox")]</c> file) contains a real <c>[ViceFact]</c>,
    /// <c>[ViceTheory]</c>, <c>Assert.Skip</c>, <c>Assert.SkipUnless</c>, or
    /// <c>Assert.SkipWhen</c>.
    /// </summary>
    [Fact]
    public void EveryXboxCategoryTestFile_UsesNoEnvironmentSkipConstruct()
    {
        var repoRoot = RepoRoot;
        var files = EnumerateXboxCategoryTestSources(repoRoot).ToArray();

        // Non-vacuity + scope proof.
        Assert.True(
            files.Length >= 30,
            $"Expected the Category=Xbox scan to reach many files, found {files.Length}.");

        // The designated home is scanned...
        Assert.Contains(
            files,
            f => string.Equals(Path.GetFileName(f), "XboxAotLinkTests.cs", StringComparison.Ordinal));

        // ...this convention test scans itself (self-coverage)...
        Assert.Contains(
            files,
            f => string.Equals(Path.GetFileName(f), "XboxTestConventionTests.cs", StringComparison.Ordinal));

        // ...and the widened scan reaches a Category=Xbox test OUTSIDE the home folder
        // (proves the union is really wider than tests/.../Xbox/).
        Assert.Contains(
            files,
            f => string.Equals(Path.GetFileName(f), "XboxUwpHeadShellTests.cs", StringComparison.Ordinal)
                && !f.Replace('\\', '/').Contains("/Xbox/", StringComparison.Ordinal));

        var violations = new List<string>();
        foreach (var file in files)
        {
            foreach (var name in ScanFile(file))
                violations.Add($"{Path.GetRelativePath(repoRoot, file)} -> {name}");
        }

        Assert.True(
            violations.Count == 0,
            "Category=Xbox test files must never use an environment-gated skip construct "
            + "([ViceFact]/[ViceTheory]/Assert.Skip*); otherwise a --filter Category=Xbox run "
            + "could report Skipped>0 for an environment reason and break the S35 zero-skip guard:\n  "
            + string.Join("\n  ", violations));
    }

    /// <summary>
    /// TEST-TESTGATE-009 (scanner non-vacuity + precision guard). The zero-skip audit is
    /// only trustworthy if the scanner actually FLAGS a real skip construct AND IGNORES
    /// the same tokens when they appear in comments, string literals, or benign
    /// identifiers. This is the positive control that keeps
    /// <see cref="EveryXboxCategoryTestFile_UsesNoEnvironmentSkipConstruct"/> from being
    /// vacuously green.
    /// </summary>
    [Fact]
    public void Scanner_FlagsRealSkipConstructs_ButIgnoresCommentsStringsAndBenignCalls()
    {
        // Real, code-level skip constructs -> MUST be flagged.
        Assert.NotEmpty(ScanText("[ViceFact] public void M() { }"));
        Assert.NotEmpty(ScanText("[ViceFact(Skip = \"x\")] public void M() { }"));
        Assert.NotEmpty(ScanText("[ViceTheory] public void M(int x) { }"));
        Assert.NotEmpty(ScanText("Assert.Skip(\"no native shim\");"));
        Assert.NotEmpty(ScanText("Assert.SkipUnless(ViceNative.IsAvailable, \"x\");"));
        Assert.NotEmpty(ScanText("Assert.SkipWhen(ci, \"x\");"));

        // Benign: the banned tokens only inside a line comment, xml-doc comment, block
        // comment, or string literal (regular + verbatim) -> MUST be ignored. These are
        // exactly the shapes present in the real Category=Xbox files.
        Assert.Empty(ScanText("// convention: no [ViceFact], no Assert.Skip"));
        Assert.Empty(ScanText("/// plain [Fact] - NO [ViceFact] and NO Assert.Skip"));
        Assert.Empty(ScanText("/* [ViceTheory] and Assert.SkipUnless mentioned */"));
        Assert.Empty(ScanText("var s = \"[ViceFact] Assert.Skip(x)\";"));
        Assert.Empty(ScanText("var v = @\"[ViceTheory] Assert.SkipWhen(c)\";"));

        // Benign: a LINQ Skip has no Assert. prefix and must NOT match.
        Assert.Empty(ScanText("var xs = items.Skip(1).Take(2);"));
        // Benign: a plain [Fact] is the whole point and must NOT match.
        Assert.Empty(ScanText("[Fact] public void M() { }"));
    }

    /// <summary>
    /// Enumerates the Category=Xbox source scope: the whole Xbox home folder plus every
    /// widened <c>[Trait("Category","Xbox")]</c> file elsewhere in the harness, deduped.
    /// </summary>
    private static IEnumerable<string> EnumerateXboxCategoryTestSources(string repoRoot)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1) The designated Category=Xbox test home: every .cs under tests/.../Xbox/.
        var homeDir = Path.Combine(repoRoot, XboxHomeRelativeDir);
        if (!Directory.Exists(homeDir))
            throw new DirectoryNotFoundException($"Xbox test home not found: '{homeDir}'.");

        foreach (var file in Directory.EnumerateFiles(homeDir, "*.cs", SearchOption.AllDirectories))
        {
            if (IsBuildArtifact(file))
                continue;
            if (seen.Add(Path.GetFullPath(file)))
                yield return file;
        }

        // 2) Widened: any .cs anywhere under the harness that declares the
        //    [Trait("Category","Xbox")] marker (e.g. the HostShells Xbox tests).
        var harnessDir = Path.Combine(repoRoot, TestHarnessRelativeDir);
        foreach (var file in Directory.EnumerateFiles(harnessDir, "*.cs", SearchOption.AllDirectories))
        {
            if (IsBuildArtifact(file))
                continue;

            var full = Path.GetFullPath(file);
            if (seen.Contains(full))
                continue;

            if (XboxCategoryTraitMarker.IsMatch(File.ReadAllText(file)) && seen.Add(full))
                yield return file;
        }
    }

    private static bool IsBuildArtifact(string path)
    {
        // Match an /obj/ or /bin/ path segment under either separator.
        return path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.AltDirectorySeparatorChar}obj{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.AltDirectorySeparatorChar}bin{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static IReadOnlyList<string> ScanFile(string path) => ScanText(File.ReadAllText(path));

    private static IReadOnlyList<string> ScanText(string source)
    {
        var code = StripCommentsAndLiterals(source);
        var hits = new List<string>();
        foreach (var (pattern, name) in BannedSkipConstructs)
        {
            if (pattern.IsMatch(code))
                hits.Add(name);
        }

        return hits;
    }

    /// <summary>
    /// Blanks out line/block comments and string/char/verbatim/raw/interpolated
    /// literals so pattern matching sees only real code tokens. Interpolated-string
    /// bodies are treated as opaque (a banned token inside an interpolation hole would
    /// be hidden - an accepted, documented limitation; no such case exists in the
    /// Category=Xbox scope). Mirrors the audited stripper in <see cref="XboxAotLinkTests"/>.
    /// </summary>
    private static string StripCommentsAndLiterals(string src)
    {
        var sb = new StringBuilder(src.Length);
        var i = 0;
        var n = src.Length;

        while (i < n)
        {
            var c = src[i];

            // Line comment: // ... EOL (also covers /// xml-doc).
            if (c == '/' && i + 1 < n && src[i + 1] == '/')
            {
                i += 2;
                while (i < n && src[i] != '\n')
                    i++;
                continue;
            }

            // Block comment: /* ... */
            if (c == '/' && i + 1 < n && src[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < n && !(src[i] == '*' && src[i + 1] == '/'))
                    i++;
                i += 2;
                sb.Append(' ');
                continue;
            }

            // Raw string literal: """ ... """ (opening run of >= 3 quotes).
            if (c == '"' && i + 2 < n && src[i + 1] == '"' && src[i + 2] == '"')
            {
                var open = 0;
                while (i < n && src[i] == '"')
                {
                    open++;
                    i++;
                }

                while (i < n)
                {
                    if (src[i] == '"')
                    {
                        var run = 0;
                        while (i < n && src[i] == '"')
                        {
                            run++;
                            i++;
                        }

                        if (run >= open)
                            break;
                    }
                    else
                    {
                        i++;
                    }
                }

                sb.Append(' ');
                continue;
            }

            // Verbatim / verbatim-interpolated string: @"..."  $@"..."  @$"..." (with "" escapes).
            if (TryConsumeVerbatimString(src, ref i, n))
            {
                sb.Append(' ');
                continue;
            }

            // Regular / interpolated string: "..." and $"..." (with \ escapes).
            if (c == '"')
            {
                i++;
                while (i < n)
                {
                    if (src[i] == '\\')
                    {
                        i += 2;
                        continue;
                    }

                    if (src[i] == '"')
                    {
                        i++;
                        break;
                    }

                    i++;
                }

                sb.Append(' ');
                continue;
            }

            // Char literal: '...'
            if (c == '\'')
            {
                i++;
                while (i < n)
                {
                    if (src[i] == '\\')
                    {
                        i += 2;
                        continue;
                    }

                    if (src[i] == '\'')
                    {
                        i++;
                        break;
                    }

                    i++;
                }

                sb.Append(' ');
                continue;
            }

            sb.Append(c);
            i++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// If a verbatim string starts at <paramref name="i"/> (@"...", $@"...", @$"..."),
    /// advances <paramref name="i"/> past its closing quote and returns true.
    /// </summary>
    private static bool TryConsumeVerbatimString(string src, ref int i, int n)
    {
        var start = i;
        var c = src[i];

        int quote;
        if (c == '@' && i + 1 < n && src[i + 1] == '"')
            quote = i + 1;
        else if (c == '@' && i + 2 < n && src[i + 1] == '$' && src[i + 2] == '"')
            quote = i + 2;
        else if (c == '$' && i + 2 < n && src[i + 1] == '@' && src[i + 2] == '"')
            quote = i + 2;
        else
            return false;

        var j = quote + 1;
        while (j < n)
        {
            if (src[j] == '"')
            {
                if (j + 1 < n && src[j + 1] == '"')
                {
                    j += 2; // "" escape
                    continue;
                }

                j++; // closing quote
                break;
            }

            j++;
        }

        i = j;
        return i > start;
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root (ViceSharp.slnx).");

            return directory.FullName;
        }
    }
}
