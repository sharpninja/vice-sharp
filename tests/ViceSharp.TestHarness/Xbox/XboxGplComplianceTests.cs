namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S32 (IMPL-XBOXUWP-032), areas XBOXPKG / XBOXGPL (R8). The
/// UWP-on-Xbox-console head must ship GPL-2.0-or-later compliance packaging:
/// bundle the GPL license text + third-party notices, VENDOR + package the GPL
/// VICE <c>*.vkm</c> keymap data (so <see cref="XboxDataPathBridge"/> first-run
/// seeding from <c>InstalledLocation\Assets\vice-data\C64\*.vkm</c> works), and
/// GUARANTEE the MSIX payload carries no Commodore ROM binaries.
///
/// <para>
/// ViceSharp is a derivative work of VICE and ships under GPL-2.0-or-later; the
/// VICE keymaps are themselves GPL-2.0-or-later VICE data, redistributed here
/// under the same license with attribution. ROMs (kernal/basic/chargen) are
/// Commodore-copyrighted and are NEVER bundled: they are user-imported or fetched
/// over verified HTTPS at runtime (S28).
/// </para>
///
/// <para>
/// FR: FR-XBOXGPL-006 (the MSIX bundles COPYING + THIRD_PARTY_NOTICES.md incl. the
/// vkm GPL attribution + the *.vkm Content, bundles zero ROM *.bin, and exposes the
/// source URL), FR-XROM-003 (shipped GPL *.vkm keymaps provisioned into the writable
/// C64 folder). TR: TR-XBOXCI-005. TEST-XBOXGPL-001.
/// </para>
///
/// <para>
/// This whole gate is Tier H: validated OFF-console by raw file / csproj reads plus a
/// single compiled-constant check against the <c>ViceSharp.Xbox.ViewModels</c>
/// assembly. No emulator, no UWP workload, no MSIX tooling is required, so it runs
/// under <c>dotnet test</c> on any agent.
/// </para>
///
/// <para>
/// Documented fallback (only if the repo/data root carries NO <c>*.vkm</c> at all):
/// the head still ships <c>Assets/vice-data/C64/</c> with a README, and
/// <see cref="Head_PackagesC64Vkm_AsContentUnderUwpGuard"/> asserts the folder plus a
/// <c>vkm-manifest.txt</c> of expected map names instead of the binary files. The
/// real VICE data root is present here, so the primary path applies and real maps are
/// vendored.
/// </para>
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxGplComplianceTests
{
    /// <summary>The C64 keymap the managed runtime selects by default (c64:gtk3_pos).</summary>
    private const string DefaultKeymapFileName = "gtk3_pos.vkm";

    /// <summary>ROM file-name patterns that must NEVER appear in the head payload.</summary>
    private static readonly Regex RomBinaryPattern = new(
        @"^(kernal|basic|chargen)[-\w]*\.bin$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// FR: FR-XBOXGPL-006. TR: TR-XBOXCI-005. TEST-XBOXGPL-001.
    /// Use case: A GPL-2.0-or-later derivative must ship the verbatim license text so
    /// the MSIX can bundle it (GPL section-1) under <c>Licenses/</c>.
    /// Acceptance: a GPL license text file exists at the repo root (COPYING or LICENSE)
    /// and its text mentions the GNU General Public License and version 2.
    /// </summary>
    [Fact]
    public void Repo_ShipsGplLicenseTextFile_MentioningGplVersion2()
    {
        var licensePath = ResolveLicenseFile();

        Assert.False(
            string.IsNullOrEmpty(licensePath),
            $"Expected a GPL license text file (COPYING or LICENSE) at the repo root '{RepoRoot}'.");

        var text = File.ReadAllText(licensePath!);

        Assert.Contains("GENERAL PUBLIC LICENSE", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Version 2", text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-XBOXGPL-006. TR: TR-XBOXCI-005. TEST-XBOXGPL-001.
    /// Use case: The bundled third-party notices must attribute VICE and specifically
    /// disclose that the packaged <c>*.vkm</c> keymap data is GPL-2.0-or-later VICE data
    /// redistributed under the same license.
    /// Acceptance: THIRD_PARTY_NOTICES.md exists at the repo root and contains the "VICE"
    /// attribution, the "GPL-2.0-or-later" identifier, and a keymap/.vkm note tying the
    /// bundled keymap data to that license.
    /// </summary>
    [Fact]
    public void ThirdPartyNotices_AttributesVice_AndBundledVkmKeymaps()
    {
        var noticesPath = Path.Combine(RepoRoot, "THIRD_PARTY_NOTICES.md");

        Assert.True(
            File.Exists(noticesPath),
            $"Expected THIRD_PARTY_NOTICES.md at '{noticesPath}'.");

        var notices = File.ReadAllText(noticesPath);

        Assert.Contains("VICE", notices, StringComparison.Ordinal);
        Assert.Contains("GPL-2.0-or-later", notices, StringComparison.Ordinal);

        // The vkm keymap note: the bundled keymap data is disclosed as GPL VICE data.
        Assert.Contains("keymap", notices, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".vkm", notices, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-XBOXGPL-006 / FR-XROM-003. TR: TR-XBOXCI-005. TEST-XBOXGPL-001.
    /// Use case: The GPL VICE keymaps must be VENDORED into the head and packaged as
    /// Content so the first-run <c>XboxDataPathBridge</c> seed
    /// (<c>InstalledLocation\Assets\vice-data\C64\*.vkm</c>) has files to copy.
    /// Acceptance: <c>src/ViceSharp.Xbox/Assets/vice-data/C64/</c> contains at least one
    /// <c>*.vkm</c> (including the default gtk3_pos.vkm) - or, per the documented
    /// no-vkm-in-repo fallback, the folder plus a vkm-manifest.txt of expected names;
    /// and the csproj includes the vkm glob as Content guarded by ViceSharpXboxUwp==true.
    /// </summary>
    [Fact]
    public void Head_PackagesC64Vkm_AsContentUnderUwpGuard()
    {
        var c64AssetsDir = Path.Combine(
            RepoRoot, "src", "ViceSharp.Xbox", "Assets", "vice-data", "C64");

        Assert.True(
            Directory.Exists(c64AssetsDir),
            $"Expected the vendored C64 keymap assets directory at '{c64AssetsDir}'.");

        var vkmFiles = Directory
            .EnumerateFiles(c64AssetsDir, "*.vkm", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .ToArray();

        if (vkmFiles.Length == 0)
        {
            // Documented fallback: no VICE *.vkm anywhere in the repo/data root. The
            // head still ships the folder + a manifest of expected names.
            var manifestPath = Path.Combine(c64AssetsDir, "vkm-manifest.txt");
            Assert.True(
                File.Exists(manifestPath),
                "No *.vkm were vendored; the documented fallback requires a vkm-manifest.txt "
                + $"of expected map names in '{c64AssetsDir}'.");
            Assert.Contains(
                DefaultKeymapFileName,
                File.ReadAllText(manifestPath),
                StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            // Primary path: real GPL VICE keymaps vendored, including the runtime default.
            Assert.Contains(
                vkmFiles,
                f => string.Equals(f, DefaultKeymapFileName, StringComparison.OrdinalIgnoreCase));
        }

        // The head csproj packages the keymaps as Content, guarded to the UWP head only.
        var csproj = ReadHeadCsproj();
        var normalized = csproj.Replace('\\', '/');

        Assert.Contains("Assets/vice-data/C64/*.vkm", normalized, StringComparison.Ordinal);

        var vkmItemGroups = ItemGroupsMentioning(normalized, "Assets/vice-data/C64/*.vkm");
        Assert.True(
            vkmItemGroups.Count > 0,
            "Expected an ItemGroup that includes the Assets/vice-data/C64/*.vkm Content.");

        foreach (var group in vkmItemGroups)
        {
            Assert.True(
                group.Contains("'$(ViceSharpXboxUwp)'=='true'", StringComparison.Ordinal),
                "Expected the vkm Content ItemGroup to be guarded by the ViceSharpXboxUwp==true condition "
                + "so the workload-free net10.0 fallback never tries to pack MSIX content.");
            Assert.Contains("Content", group, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// FR: FR-XBOXGPL-006. TR: TR-XBOXCI-005. TEST-XBOXGPL-001.
    /// Use case: Commodore ROMs are copyrighted and must never be redistributed; the
    /// head payload must be ROM-free while still allowing the GPL vkm keymap assets.
    /// Acceptance: no file named like kernal-*.bin / basic-*.bin / chargen-*.bin exists
    /// anywhere under src/ViceSharp.Xbox/, and the head csproj declares no *.bin Content
    /// glob (so a ROM can never be swept into the package).
    /// </summary>
    [Fact]
    public void Head_ShipsNoCommodoreRomBinaries_AndCsprojHasNoRomBinGlob()
    {
        var headDir = Path.Combine(RepoRoot, "src", "ViceSharp.Xbox");

        var romPayload = EnumerateSourceFiles(headDir)
            .Where(f => RomBinaryPattern.IsMatch(Path.GetFileName(f)))
            .Select(f => Path.GetRelativePath(RepoRoot, f))
            .ToArray();

        Assert.True(
            romPayload.Length == 0,
            "The Xbox head must not ship Commodore ROM binaries, but found:\n  "
            + string.Join("\n  ", romPayload));

        // Defense-in-depth: the csproj carries no *.bin glob at all, so a ROM dropped
        // into the head can never be picked up as packaged Content.
        var csproj = ReadHeadCsproj();
        Assert.DoesNotContain(".bin", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("kernal", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chargen", csproj, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-XBOXGPL-006. TR: TR-XBOXCI-005. TEST-XBOXGPL-001.
    /// Use case: The GPL source offer must surface a compiled, reachable source-repository
    /// URL so the About screen (S30) can disclose where the corresponding source lives.
    /// Acceptance: <see cref="AboutInfo.SourceUrl"/> (compiled into
    /// <c>ViceSharp.Xbox.ViewModels</c>) is non-empty and parses as an absolute https URL.
    /// </summary>
    [Fact]
    public void AboutInfo_SourceUrl_IsCompiledNonEmptyHttpsUrl()
    {
        Assert.False(
            string.IsNullOrWhiteSpace(AboutInfo.SourceUrl),
            "AboutInfo.SourceUrl must be a non-empty compiled constant.");

        Assert.True(
            Uri.TryCreate(AboutInfo.SourceUrl, UriKind.Absolute, out var uri)
                && uri!.Scheme == Uri.UriSchemeHttps,
            $"AboutInfo.SourceUrl must be an absolute https URL, but was '{AboutInfo.SourceUrl}'.");
    }

    /// <summary>Returns the repo-root COPYING or LICENSE path, or null if neither exists.</summary>
    private static string? ResolveLicenseFile()
    {
        foreach (var candidate in new[] { "COPYING", "LICENSE", "LICENSE.txt", "LICENSE.md" })
        {
            var path = Path.Combine(RepoRoot, candidate);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    /// <summary>Reads the UWP head csproj (asserts it exists first).</summary>
    private static string ReadHeadCsproj()
    {
        var csprojPath = Path.Combine(
            RepoRoot, "src", "ViceSharp.Xbox", "ViceSharp.Xbox.csproj");

        Assert.True(File.Exists(csprojPath), $"Expected the Xbox head csproj at '{csprojPath}'.");
        return File.ReadAllText(csprojPath);
    }

    /// <summary>All &lt;ItemGroup&gt;...&lt;/ItemGroup&gt; blocks whose text mentions the needle.</summary>
    private static IReadOnlyList<string> ItemGroupsMentioning(string csproj, string needle)
        => Regex
            .Matches(csproj, @"<ItemGroup\b[^>]*>.*?</ItemGroup>", RegexOptions.Singleline)
            .Select(m => m.Value)
            .Where(v => v.Contains(needle, StringComparison.Ordinal))
            .ToList();

    /// <summary>Enumerates real source/content files under a directory, skipping obj/bin.</summary>
    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        if (!Directory.Exists(root))
            yield break;

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (IsBuildArtifact(file))
                continue;
            yield return file;
        }
    }

    private static bool IsBuildArtifact(string path)
        => path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.AltDirectorySeparatorChar}obj{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.AltDirectorySeparatorChar}bin{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);

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
