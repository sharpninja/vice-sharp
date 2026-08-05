namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S5 (IMPL-XBOXUWP-005). FR-XBOXTOPO-001,
/// TR-XBOXAOT-001 (the core + Host.InProcess graph links clean under Native AOT
/// with zero IL2xxx/IL3xxx) and TR-XBOXAOT-003 (no shipped Xbox path invokes the
/// ViceNative/WinMm P/Invoke clusters). TEST-XBOXAOT-001.
///
/// <para>
/// This is the STATIC, reflection-free half of the AOT gate: a raw-source audit
/// that proves the exact dependency graph the ViceSharp.Xbox (UWP-on-console)
/// head reuses is free of the runtime patterns that Native AOT / IL trimming
/// cannot see through. The dynamic half (the real <c>dotnet publish
/// /p:PublishAot=true</c> link over the head) runs in CI/dev and is documented in
/// the slice receipts; this file guards the code-side invariant so a regression is
/// caught by <c>dotnet test</c> on any agent, with no UWP workload and no linker.
/// </para>
///
/// <para>
/// Shipped Xbox-head dependency graph (why each directory is audited):
/// <list type="bullet">
///   <item><description><c>src/ViceSharp.Abstractions</c> - the emulator contract
///     (interfaces/value types) the head and every core library reference.</description></item>
///   <item><description><c>src/ViceSharp.Core</c> - bus, clock, mutation queue,
///     snapshots. EXCLUDING <c>ViceNative.cs</c>: that file is the parity-test-only
///     native VICE P/Invoke binding (<c>[LibraryImport]</c> over <c>vice_x64</c>).
///     It is NEVER called on the Xbox path (the head drives the managed core only),
///     and a never-invoked P/Invoke is AOT-safe, so it is intentionally out of the
///     audited set per TR-XBOXAOT-003.</description></item>
///   <item><description><c>src/ViceSharp.Chips</c> - CPU/VIC-II/SID/CIA/VIA/PLA;
///     machine-agnostic emulation.</description></item>
///   <item><description><c>src/ViceSharp.Architectures</c> - machine definitions
///     (C64). YamlDotNet here is analyzer-only static source-gen, not a runtime
///     reflective path.</description></item>
///   <item><description><c>src/ViceSharp.Host.InProcess</c> - the Kestrel-free
///     POCO host facade the head composes with <c>new</c> (no gRPC/ASP.NET).</description></item>
/// </list>
/// </para>
///
/// <para>
/// The scan is precise: it strips comments, string/char/verbatim/raw literals, and
/// interpolated-string text BEFORE matching, so a banned token inside a comment
/// (e.g. the "dynamic drive attach" note in <c>IecBusDevice.cs</c>) or a string is
/// not a false hit, and identifiers that merely contain a banned word (e.g.
/// <c>EmitLiveResampleTick</c>) never match. The bare <c>using System.Reflection;</c>
/// import (present in <c>DiagnosticsServiceHost.cs</c>, which only reads an assembly
/// attribute via <c>GetCustomAttribute</c>) is NOT banned - attributes are
/// AOT-preserved; only the enumerated reflective CALL patterns are hostile.
/// </para>
///
/// <para>
/// Acceptance (TEST-XBOXAOT-001):
/// none of the shipped graph's source files contain <c>Activator.CreateInstance</c>,
/// <c>MakeGenericType</c>/<c>MakeGenericMethod</c>, <c>Type.GetType(</c>,
/// <c>.GetType().GetMethod</c>, <c>.GetProperty(</c>, <c>System.Text.Json</c>,
/// <c>Newtonsoft</c>, <c>System.Reflection.Emit</c>, <c>ILGenerator</c>, or the
/// <c>dynamic</c> type keyword; and <c>ViceSharp.Host.InProcess.csproj</c> carries
/// no <c>Microsoft.AspNetCore</c> / <c>Grpc.AspNetCore</c> reference (belt-and-suspenders
/// for the AppContainer/Kestrel-free invariant).
/// </para>
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxAotLinkTests
{
    /// <summary>The one Core file excluded from the audit (parity-test-only native P/Invoke).</summary>
    private const string ExcludedCoreFile = "ViceNative.cs";

    /// <summary>Directories, relative to the repo root, that make up the shipped Xbox-head graph.</summary>
    private static readonly string[] ShippedGraphRelativeDirs =
    {
        Path.Combine("src", "ViceSharp.Abstractions"),
        Path.Combine("src", "ViceSharp.Core"),
        Path.Combine("src", "ViceSharp.Chips"),
        Path.Combine("src", "ViceSharp.Architectures"),
        Path.Combine("src", "ViceSharp.Host.InProcess"),
    };

    /// <summary>
    /// The AOT/trim-hostile runtime patterns. Each is matched against the
    /// comment/literal-stripped source so only real usages count.
    /// </summary>
    private static readonly (Regex Pattern, string Name)[] BannedPatterns =
    {
        (new Regex(@"\bActivator\s*\.\s*CreateInstance\b", RegexOptions.Compiled), "Activator.CreateInstance"),
        (new Regex(@"\bMakeGenericType\b", RegexOptions.Compiled), "MakeGenericType"),
        (new Regex(@"\bMakeGenericMethod\b", RegexOptions.Compiled), "MakeGenericMethod"),
        (new Regex(@"\bType\s*\.\s*GetType\s*\(", RegexOptions.Compiled), "Type.GetType("),
        (new Regex(@"\.\s*GetType\s*\(\s*\)\s*\.\s*GetMethod\b", RegexOptions.Compiled), ".GetType().GetMethod"),
        (new Regex(@"\.\s*GetProperty\s*\(", RegexOptions.Compiled), ".GetProperty("),
        (new Regex(@"\bSystem\s*\.\s*Text\s*\.\s*Json\b", RegexOptions.Compiled), "System.Text.Json"),
        (new Regex(@"\bNewtonsoft\b", RegexOptions.Compiled), "Newtonsoft"),
        (new Regex(@"\bSystem\s*\.\s*Reflection\s*\.\s*Emit\b", RegexOptions.Compiled), "System.Reflection.Emit"),
        (new Regex(@"\bILGenerator\b", RegexOptions.Compiled), "ILGenerator"),
        (new Regex(@"(?<![\w.])dynamic\b", RegexOptions.Compiled), "dynamic (as a type)"),
    };

    /// <summary>
    /// TR-XBOXAOT-001 / TR-XBOXAOT-003, TEST-XBOXAOT-001.
    /// Use case: the reused core + Host.InProcess graph must be Native-AOT / trim
    /// clean so the console head can ship AOT.
    /// Acceptance: every shipped source file (all five graph dirs, excluding
    /// <c>obj</c>/<c>bin</c> and <c>ViceNative.cs</c>) is free of the AOT/trim-hostile
    /// runtime patterns. <c>DiagnosticsServiceHost.cs</c> (which carries a benign
    /// <c>using System.Reflection;</c> for an attribute read) is in the scanned set
    /// and must still pass; <c>ViceNative.cs</c> is proven excluded.
    /// </summary>
    [Fact]
    public void ShippedGraph_ContainsNoAotHostilePatterns()
    {
        var repoRoot = RepoRoot;
        var files = EnumerateShippedSources(repoRoot).ToArray();

        // Non-vacuity: the walk actually reached the graph. Anchor on two known
        // files and prove the native-interop file is excluded.
        Assert.True(files.Length > 50, $"Expected the shipped graph walk to find many source files, found {files.Length}.");
        Assert.Contains(files, f => string.Equals(Path.GetFileName(f), "ConsoleEmulatorHost.cs", StringComparison.Ordinal));
        Assert.Contains(files, f => string.Equals(Path.GetFileName(f), "DiagnosticsServiceHost.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(files, f => string.Equals(Path.GetFileName(f), ExcludedCoreFile, StringComparison.Ordinal));

        var violations = new List<string>();
        foreach (var file in files)
        {
            foreach (var name in ScanFile(file))
                violations.Add($"{Path.GetRelativePath(repoRoot, file)} -> {name}");
        }

        Assert.True(
            violations.Count == 0,
            "AOT/trim-hostile runtime patterns found in the shipped Xbox-head graph " +
            "(these break Native AOT / IL trimming):\n  " + string.Join("\n  ", violations));
    }

    /// <summary>
    /// TR-XBOXAOT-001, TEST-XBOXAOT-001. Belt-and-suspenders for the AppContainer
    /// invariant: the extracted in-process host that the head composes must never
    /// drag the Kestrel/ASP.NET or gRPC-server stack (reflection-heavy, AppContainer-
    /// hostile) into the AOT graph.
    /// Acceptance: <c>ViceSharp.Host.InProcess.csproj</c> references neither
    /// <c>Microsoft.AspNetCore</c> nor <c>Grpc.AspNetCore</c>.
    /// </summary>
    [Fact]
    public void HostInProcessCsproj_HasNoAspNetCoreOrGrpcServer()
    {
        var csprojPath = Path.Combine(
            RepoRoot, "src", "ViceSharp.Host.InProcess", "ViceSharp.Host.InProcess.csproj");

        Assert.True(File.Exists(csprojPath), $"Expected the in-process host csproj at '{csprojPath}'.");

        var csproj = File.ReadAllText(csprojPath);
        Assert.DoesNotContain("Microsoft.AspNetCore", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Grpc.AspNetCore", csproj, StringComparison.Ordinal);
    }

    /// <summary>
    /// Non-vacuity + precision guard for the scanner itself (TEST-XBOXAOT-001).
    /// The audit is only trustworthy if it flags real hostile usages AND ignores the
    /// same tokens when they appear in comments, strings, or benign identifiers.
    /// </summary>
    [Fact]
    public void Scanner_FlagsRealHostileUsage_ButIgnoresCommentsStringsAndBenignImports()
    {
        // Real, code-level hostile usages -> MUST be flagged.
        Assert.NotEmpty(ScanText("var t = Type.GetType(name);"));
        Assert.NotEmpty(ScanText("var o = Activator.CreateInstance(t);"));
        Assert.NotEmpty(ScanText("dynamic d = o;"));
        Assert.NotEmpty(ScanText("List<dynamic> xs = null;"));
        Assert.NotEmpty(ScanText("var m = obj.GetType().GetMethod(name);"));
        Assert.NotEmpty(ScanText("var p = ty.GetProperty(name);"));
        Assert.NotEmpty(ScanText("var g = ty.MakeGenericType(u);"));
        Assert.NotEmpty(ScanText("var g = mi.MakeGenericMethod(u);"));
        Assert.NotEmpty(ScanText("using Newtonsoft.Json;"));
        Assert.NotEmpty(ScanText("using System.Text.Json;"));
        Assert.NotEmpty(ScanText("using System.Reflection.Emit;"));
        Assert.NotEmpty(ScanText("ILGenerator il = null;"));

        // Benign: banned tokens only inside a line comment, block comment, or string
        // literal (regular + verbatim) -> MUST be ignored.
        Assert.Empty(ScanText("// dynamic drive attach; Activator.CreateInstance; Newtonsoft"));
        Assert.Empty(ScanText("/* Type.GetType( and System.Text.Json and ILGenerator */"));
        Assert.Empty(ScanText("var s = \"Activator.CreateInstance and dynamic and Newtonsoft\";"));
        Assert.Empty(ScanText("var v = @\"Type.GetType( and dynamic and System.Text.Json\";"));

        // Benign: the exact shapes present in the real graph.
        //  - EmitLiveResampleTick-style identifiers must NOT match System.Reflection.Emit.
        Assert.Empty(ScanText("public void EmitLiveResampleTick() { }"));
        //  - bare System.Reflection import + an attribute read (DiagnosticsServiceHost.cs).
        Assert.Empty(ScanText("using System.Reflection; var v = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();"));
        //  - an instance GetType() with no reflective chain is AOT-safe.
        Assert.Empty(ScanText("var rt = obj.GetType();"));
    }

    /// <summary>Enumerates the shipped graph's C# sources, skipping obj/bin and the excluded native file.</summary>
    private static IEnumerable<string> EnumerateShippedSources(string repoRoot)
    {
        foreach (var relativeDir in ShippedGraphRelativeDirs)
        {
            var dir = Path.Combine(repoRoot, relativeDir);
            if (!Directory.Exists(dir))
                throw new DirectoryNotFoundException($"Shipped-graph directory not found: '{dir}'.");

            foreach (var file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildArtifact(file))
                    continue;
                if (string.Equals(Path.GetFileName(file), ExcludedCoreFile, StringComparison.Ordinal))
                    continue;
                yield return file;
            }
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
        foreach (var (pattern, name) in BannedPatterns)
        {
            if (pattern.IsMatch(code))
                hits.Add(name);
        }

        return hits;
    }

    /// <summary>
    /// Blanks out line/block comments and string/char/verbatim/raw/interpolated
    /// literals so pattern matching sees only real code tokens. Newlines are
    /// preserved outside multi-line literals; literal bodies collapse to a single
    /// space. Interpolated-string bodies are treated as opaque (a banned token inside
    /// an interpolation hole would be hidden - an accepted, documented limitation; no
    /// such case exists in the shipped graph).
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
