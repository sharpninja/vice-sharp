namespace ViceSharp.TestHarness.HostShells;

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S31 (IMPL-XBOXUWP-031): the UWP-on-Xbox-console head ships a
/// valid Package.appxmanifest, guarded so it applies ONLY to the real UWP build
/// (ViceSharpXboxUwp==true) while the workload-free net10.0 fallback ignores it.
///
/// FR: FR-XBOXPKG-003. TR: TR-XBOXPKG-002. Use case: the MSIX for the Dev-Mode
/// Xbox console must carry a schema-valid manifest that targets the Xbox device
/// family, declares an x64 identity whose Publisher matches the S0 self-signed
/// dev cert (CN=ViceSharpDev per docs/xbox/on-console-setup-runbook.md), and
/// declares the general internetClient capability the operator's chosen HTTPS
/// ROM-download path (FR-XROM-002) requires. The fallback build must not try to
/// process the manifest so the whole solution keeps building without the UWP
/// workload.
///
/// Acceptance (TEST-XBOXPKG-003), all validated off-console by raw file read:
///   - src/ViceSharp.Xbox/Package.appxmanifest exists and parses as valid XML.
///   - It declares TargetDeviceFamily Name="Windows.Xbox" with
///     MinVersion 10.0.19041.0 and MaxVersionTested 10.0.26100.0.
///   - Identity ProcessorArchitecture == "x64".
///   - It declares the internetClient capability.
///   - The csproj includes Package.appxmanifest ONLY inside an ItemGroup guarded
///     by the ViceSharpXboxUwp==true condition (the fallback TFM ignores it).
///
/// RED before S31 lands: the manifest does not exist, so XDocument.Load throws
/// and the csproj carries no AppxManifest include.
/// </summary>
public sealed class XboxManifestTests
{
    [Fact]
    [Trait("Category", "Xbox")]
    public void Manifest_Exists_AndParsesAsValidXml()
    {
        Assert.True(
            File.Exists(ManifestPath),
            $"Expected the UWP head manifest at '{ManifestPath}'.");

        // Must not throw: XDocument.Load enforces well-formed XML.
        var document = XDocument.Load(ManifestPath);
        Assert.NotNull(document.Root);
        Assert.Equal("Package", document.Root!.Name.LocalName);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Manifest_TargetsXboxDeviceFamily_WithPinnedVersions()
    {
        var document = XDocument.Load(ManifestPath);

        var xbox = Elements(document, "TargetDeviceFamily")
            .FirstOrDefault(e =>
                string.Equals(
                    (string?)e.Attribute("Name"),
                    "Windows.Xbox",
                    StringComparison.Ordinal));

        Assert.True(
            xbox is not null,
            "Expected a TargetDeviceFamily Name=\"Windows.Xbox\" element.");

        Assert.Equal("10.0.19041.0", (string?)xbox!.Attribute("MinVersion"));
        Assert.Equal("10.0.26100.0", (string?)xbox.Attribute("MaxVersionTested"));
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Manifest_Identity_IsX64_WithDevCertPublisher()
    {
        var document = XDocument.Load(ManifestPath);

        var identity = Elements(document, "Identity").FirstOrDefault();
        Assert.True(identity is not null, "Expected an Identity element.");

        Assert.Equal("x64", (string?)identity!.Attribute("ProcessorArchitecture"));

        // Publisher must match the S0 self-signed dev cert subject so the
        // sideloaded MSIX validates (docs/xbox/on-console-setup-runbook.md).
        Assert.Equal("CN=ViceSharpDev", (string?)identity.Attribute("Publisher"));
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Manifest_DeclaresInternetClientCapability()
    {
        var document = XDocument.Load(ManifestPath);

        var declaresInternetClient = Elements(document, "Capability")
            .Any(e =>
                string.Equals(
                    (string?)e.Attribute("Name"),
                    "internetClient",
                    StringComparison.Ordinal));

        Assert.True(
            declaresInternetClient,
            "Expected a Capability Name=\"internetClient\" (needed for the chosen HTTPS ROM download).");
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Csproj_IncludesManifest_OnlyUnderUwpCondition()
    {
        var csproj = File.ReadAllText(CsprojPath);

        Assert.Contains("Package.appxmanifest", csproj);

        // Every ItemGroup that mentions Package.appxmanifest MUST be guarded by
        // the ViceSharpXboxUwp==true condition, so the workload-free net10.0
        // fallback never tries to process the manifest.
        var itemGroups = Regex.Matches(
            csproj,
            "<ItemGroup\\b[^>]*>.*?</ItemGroup>",
            RegexOptions.Singleline);

        var manifestGroups = itemGroups
            .Where(m => m.Value.Contains("Package.appxmanifest", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            manifestGroups.Count > 0,
            "Expected an ItemGroup that includes Package.appxmanifest.");

        foreach (var group in manifestGroups)
        {
            Assert.True(
                group.Value.Contains("'$(ViceSharpXboxUwp)'=='true'", StringComparison.Ordinal),
                "Expected the Package.appxmanifest ItemGroup to be guarded by the ViceSharpXboxUwp==true condition.");
        }
    }

    private static System.Collections.Generic.IEnumerable<XElement> Elements(
        XDocument document,
        string localName)
        => document.Descendants()
            .Where(e => string.Equals(e.Name.LocalName, localName, StringComparison.Ordinal));

    private static string ManifestPath => Path.Combine(
        RepoRoot,
        "src",
        "ViceSharp.Xbox",
        "Package.appxmanifest");

    private static string CsprojPath => Path.Combine(
        RepoRoot,
        "src",
        "ViceSharp.Xbox",
        "ViceSharp.Xbox.csproj");

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
