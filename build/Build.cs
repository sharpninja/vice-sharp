using System;
using System.Linq;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;

sealed partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Solution]
    readonly Solution Solution = null!;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
            DotNetClean(s => s
                .SetProject(Solution));
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true));
        });

    Target GitCommit => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Git("add -A");
            Git("commit -m \"wip: $(Get-Date -Format 'yyyy-MM-dd HH:mm')\"");
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)
                .SetNoBuild(true)
                // Exclude Determinism (its own target), the slow, host-bound,
                // non-deterministic aiUnit AI reviews (AiReview target), the
                // quarantined not-yet-remediated parity ACs (ParityPending,
                // admitted per slice as they flip green: PLAN-VICEPARITY-001),
                // and the legacy renderer tests awaiting per-cycle replacement
                // (ParityLegacy, deleted as V-slices land).
                .SetFilter("Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy"));
        });

    Target DeterminismTest => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)
                .SetNoBuild(true)
                .SetFilter("Category=Determinism"));
        });

    // Whole VICE-parity suite including quarantined (ParityPending) red tests:
    // the remediation burn-down. Non-blocking in CI; blocking locally at each
    // slice exit (PLAN-VICEPARITY-001).
    Target ParityTest => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)
                .SetNoBuild(true)
                .SetFilter("Category=Parity"));
        });

    [Parameter("Override the ViceSharp.Core NuGet package version. Default: GitVersion {Major}.{Minor}.{CommitsSinceVersionSource} (same scheme as the MSI).")]
    readonly string PackageVersionOverride = null!;

    AbsolutePath PackageProject => RootDirectory / "src" / "ViceSharp.Core.Package" / "ViceSharp.Core.Package.csproj";

    AbsolutePath PackagesOutputDirectory => ArtifactsDirectory / "packages";

    /// <summary>
    /// Pack the five emulation-core assemblies (Abstractions, Chips, RomFetch,
    /// Core, Architectures) into the single ViceSharp.Core NuGet and verify
    /// the package contents before declaring success.
    /// </summary>
    Target PackNuget => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var version = string.IsNullOrWhiteSpace(PackageVersionOverride) ? MsiVersion : PackageVersionOverride;
            PackagesOutputDirectory.CreateOrCleanDirectory();

            DotNetPack(s => s
                .SetProject(PackageProject)
                .SetConfiguration(Configuration)
                // PackageId lives here, not in the csproj: setting it in the
                // project (or letting pack's implicit restore see it as a
                // global property) collides with the real ViceSharp.Core
                // project at restore time (ambiguous project name). Compile
                // already restored, so pack skips restore; the (incremental)
                // build must run because the assembly-collection target
                // depends on ResolveReferences (NoBuild would trip NETSDK1085
                // on the referenced projects).
                .EnableNoRestore()
                .SetProperty("PackageId", "ViceSharp.Core")
                .SetProperty("PackageVersion", version)
                .SetOutputDirectory(PackagesOutputDirectory));

            // Gate: exactly the five core assemblies in lib/net10.0, no
            // placeholder assembly, no bundled external dependency DLLs, and
            // the three external runtime dependencies present in the nuspec.
            var nupkg = PackagesOutputDirectory / $"ViceSharp.Core.{version}.nupkg";
            Assert.FileExists(nupkg);

            using var zip = System.IO.Compression.ZipFile.OpenRead(nupkg);
            var libFiles = zip.Entries
                .Where(e => e.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.FullName.Replace('\\', '/'))
                .OrderBy(n => n)
                .ToList();

            foreach (var name in new[]
                     {
                         "ViceSharp.Abstractions",
                         "ViceSharp.Architectures",
                         "ViceSharp.Chips",
                         "ViceSharp.Core",
                         "ViceSharp.RomFetch",
                     })
            {
                Assert.True(
                    libFiles.Contains($"lib/net10.0/{name}.dll"),
                    $"{name}.dll missing from the package lib folder");
            }

            Assert.True(
                !libFiles.Any(f => f.Contains("ViceSharp.Core.Package")),
                "the packaging placeholder assembly leaked into the package");
            Assert.True(
                !libFiles.Any(f => f.Contains("YamlDotNet") || f.Contains("Microsoft.Extensions")),
                "external dependency assemblies must be nuspec dependencies, not bundled in lib/");

            var nuspecEntry = zip.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            using var nuspecReader = new System.IO.StreamReader(nuspecEntry.Open());
            var nuspec = nuspecReader.ReadToEnd();
            foreach (var dependencyId in new[]
                     {
                         "Microsoft.Extensions.Configuration.Abstractions",
                         "Microsoft.Extensions.Configuration",
                         "YamlDotNet",
                     })
            {
                Assert.True(
                    nuspec.Contains($"id=\"{dependencyId}\"", StringComparison.Ordinal),
                    $"nuspec is missing the {dependencyId} dependency");
            }

            Serilog.Log.Information(
                "ViceSharp.Core {Version}: five core assemblies verified, dependencies present -> {Path}",
                version,
                nupkg);

            // ----- Individual packages -------------------------------------
            // Tool: dotnet tool package (embeds everything under tools/).
            // RewriteDeps: project refs to the bundled assemblies must become
            //   the single ViceSharp.Core dependency (NuGet has no dep-id
            //   override for project references).
            // Embeds: platform host shells embed all ViceSharp assemblies via
            //   ViceSharpEmbedPack.targets (ViceSharp.Avalonia is a tool and
            //   cannot be a package dependency).
            var individual = new (string Name, bool Tool, bool RewriteDeps, bool Embeds)[]
            {
                ("ViceSharp.Protocol", false, false, false),
                ("ViceSharp.Monitor", false, true, false),
                ("ViceSharp.Launcher", false, true, false),
                ("ViceSharp.AdhocHelper", false, true, false),
                ("ViceSharp.Host", false, true, false),
                ("ViceSharp.SourceGen", false, false, false),
                ("ViceSharp.Avalonia", true, false, false),
                ("ViceSharp.Console", true, false, false),
                ("ViceSharp.Host.MacOS", false, false, true),
                ("ViceSharp.Host.Android", false, false, true),
                ("ViceSharp.Host.iOS", false, false, true),
                ("ViceSharp.Host.Xbox", false, false, true),
            };

            foreach (var spec in individual)
            {
                DotNetPack(s => s
                    .SetProject(RootDirectory / "src" / spec.Name / $"{spec.Name}.csproj")
                    .SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .SetProperty("PackageVersion", version)
                    .SetOutputDirectory(PackagesOutputDirectory));

                var packagePath = PackagesOutputDirectory / $"{spec.Name}.{version}.nupkg";
                Assert.FileExists(packagePath);

                if (spec.RewriteDeps)
                    RewriteBundledDependenciesToViceSharpCore(packagePath, version);

                VerifyPackage(packagePath, spec.Name, spec.Tool, spec.RewriteDeps, spec.Embeds);
            }

            Serilog.Log.Information(
                "Packed {Count} individual packages + ViceSharp.Core bundle {Version} -> {Dir}",
                individual.Length,
                version,
                PackagesOutputDirectory);
        });

    [Parameter("NuGet feed for PublishNuget. Default nuget.org v3.")]
    readonly string NugetSource = "https://api.nuget.org/v3/index.json";

    /// <summary>
    /// Publish every package produced by PackNuget to the NuGet feed.
    /// The API key comes from the NUGET_API_KEY environment variable and is
    /// never logged (Nuke redacts the ApiKey setting). Duplicate versions are
    /// skipped so re-runs are safe.
    /// </summary>
    Target PublishNuget => _ => _
        .DependsOn(PackNuget)
        .Executes(() =>
        {
            var apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
            Assert.True(!string.IsNullOrWhiteSpace(apiKey), "NUGET_API_KEY environment variable is not set");

            // PUBLISH-GATE: packages ship only from a tagged release commit
            // carrying a FRESH minor version (vX.Y.0, first tag of that
            // major.minor line). Guarantees published packages are exactly
            // reproducible from a tag and each publish opens a new minor.
            var headTags = Git("tag --points-at HEAD", logOutput: false)
                .Select(o => o.Text.Trim())
                .Where(t => System.Text.RegularExpressions.Regex.IsMatch(t, @"^v\d+\.\d+\.\d+$"))
                .ToList();
            Assert.True(headTags.Count == 1,
                $"PublishNuget requires HEAD to carry exactly one release tag (vX.Y.Z); found: [{string.Join(", ", headTags)}]");

            var releaseTag = headTags[0];
            var parts = releaseTag.TrimStart('v').Split('.');
            Assert.True(parts[2] == "0",
                $"PublishNuget requires a fresh minor (vX.Y.0); tag {releaseTag} has a non-zero patch");

            var sameMinorTags = Git($"tag --list v{parts[0]}.{parts[1]}.*", logOutput: false)
                .Select(o => o.Text.Trim())
                .Where(t => t.Length > 0)
                .ToList();
            Assert.True(sameMinorTags.Count == 1 && sameMinorTags[0] == releaseTag,
                $"PublishNuget requires the minor to be fresh; existing v{parts[0]}.{parts[1]}.* tags: [{string.Join(", ", sameMinorTags)}]");

            var expectedVersion = releaseTag.TrimStart('v');
            var packages = PackagesOutputDirectory.GlobFiles("*.nupkg");
            Assert.True(packages.Count > 0, $"no packages found in {PackagesOutputDirectory}");
            Assert.True(packages.All(p => p.Name.EndsWith($".{expectedVersion}.nupkg", StringComparison.OrdinalIgnoreCase)),
                $"packed versions do not match release tag {releaseTag}; repack from the tagged commit");

            foreach (var package in packages.OrderBy(p => p.Name))
            {
                DotNetNuGetPush(s => s
                    .SetTargetPath(package)
                    .SetSource(NugetSource)
                    .SetApiKey(apiKey)
                    .EnableSkipDuplicate());
            }

            Serilog.Log.Information("Published {Count} packages to {Source}", packages.Count, NugetSource);
        });

    /// <summary>
    /// The four sub-assembly ids that only exist inside the ViceSharp.Core
    /// bundle; a nuspec must never depend on them. (A dependency on
    /// ViceSharp.Core itself is correct: that IS the bundle.)
    /// </summary>
    static readonly string[] BundledOnlyIds =
    {
        "ViceSharp.Abstractions",
        "ViceSharp.Chips",
        "ViceSharp.RomFetch",
        "ViceSharp.Architectures",
    };

    static void RewriteBundledDependenciesToViceSharpCore(AbsolutePath nupkgPath, string version)
    {
        using var archive = System.IO.Compression.ZipFile.Open(nupkgPath, System.IO.Compression.ZipArchiveMode.Update);
        var nuspecEntry = archive.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));

        System.Xml.Linq.XDocument doc;
        using (var read = nuspecEntry.Open())
        {
            doc = System.Xml.Linq.XDocument.Load(read);
        }

        var rewritten = false;
        foreach (var group in doc.Descendants().Where(e => e.Name.LocalName == "group" || e.Name.LocalName == "dependencies").ToList())
        {
            var bundled = group.Elements()
                .Where(e => e.Name.LocalName == "dependency"
                            && BundledOnlyIds.Contains((string?)e.Attribute("id") ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (bundled.Count == 0)
                continue;

            var ns = bundled[0].Name.Namespace;
            bundled.ForEach(e => e.Remove());
            rewritten = true;

            var hasCore = group.Elements()
                .Any(e => e.Name.LocalName == "dependency"
                          && string.Equals((string?)e.Attribute("id"), "ViceSharp.Core", StringComparison.OrdinalIgnoreCase));
            if (!hasCore)
            {
                group.Add(new System.Xml.Linq.XElement(
                    ns + "dependency",
                    new System.Xml.Linq.XAttribute("id", "ViceSharp.Core"),
                    new System.Xml.Linq.XAttribute("version", version),
                    new System.Xml.Linq.XAttribute("exclude", "Build,Analyzers")));
            }
        }

        Assert.True(rewritten, $"{nupkgPath}: expected bundled-assembly dependencies to rewrite, found none");

        nuspecEntry.Delete();
        var replacement = archive.CreateEntry(nuspecEntry.FullName);
        using var write = replacement.Open();
        doc.Save(write);
    }

    static void VerifyPackage(AbsolutePath nupkgPath, string packageId, bool tool, bool rewritten, bool embeds)
    {
        using var zip = System.IO.Compression.ZipFile.OpenRead(nupkgPath);
        var entries = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToList();

        var nuspecName = entries.Single(e => e.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        using var reader = new System.IO.StreamReader(zip.Entries.Single(e => e.FullName == nuspecName).Open());
        var nuspec = reader.ReadToEnd();

        if (tool)
        {
            Assert.True(nuspec.Contains("DotnetTool", StringComparison.OrdinalIgnoreCase), $"{packageId}: missing DotnetTool package type");
            Assert.True(entries.Any(e => e.StartsWith("tools/", StringComparison.OrdinalIgnoreCase) && e.EndsWith($"{packageId}.dll", StringComparison.OrdinalIgnoreCase)),
                $"{packageId}: tool payload missing");
        }
        else if (packageId == "ViceSharp.SourceGen")
        {
            Assert.True(entries.Contains("analyzers/dotnet/cs/ViceSharp.SourceGen.dll"), $"{packageId}: analyzer payload missing");
            Assert.True(!entries.Any(e => e.StartsWith("lib/", StringComparison.OrdinalIgnoreCase)), $"{packageId}: analyzer package must not carry lib/");
        }
        else
        {
            Assert.True(entries.Any(e => e.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) && e.EndsWith($"{packageId}.dll", StringComparison.OrdinalIgnoreCase)),
                $"{packageId}: lib payload missing");
        }

        if (embeds)
        {
            Assert.True(entries.Any(e => e.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) && e.EndsWith("ViceSharp.Avalonia.dll", StringComparison.OrdinalIgnoreCase)),
                $"{packageId}: embedded ViceSharp assemblies missing");
        }

        if (rewritten || embeds || tool)
        {
            foreach (var id in BundledOnlyIds)
            {
                Assert.True(!nuspec.Contains($"id=\"{id}\"", StringComparison.OrdinalIgnoreCase),
                    $"{packageId}: nuspec still depends on bundled assembly {id}");
            }
        }

        if (rewritten)
        {
            Assert.True(nuspec.Contains("id=\"ViceSharp.Core\"", StringComparison.OrdinalIgnoreCase),
                $"{packageId}: rewritten nuspec must depend on ViceSharp.Core");
        }
    }

    Target RomFetch => _ => _
        .Executes(() =>
        {
            Serilog.Log.Information("RomFetch target — tool not yet implemented");
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Serilog.Log.Information("Pack target — reserved for future NuGet packaging");
        });

    Target RunConsole => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetRun(s => s
                .SetProjectFile(RootDirectory / "src" / "ViceSharp.Console" / "ViceSharp.Console.csproj")
                .SetConfiguration(Configuration)
                .SetNoBuild(true));
        });

    Target RunAvalonia => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetRun(s => s
                .SetProjectFile(RootDirectory / "src" / "ViceSharp.Avalonia" / "ViceSharp.Avalonia.csproj")
                .SetConfiguration(Configuration)
                .SetNoBuild(true));
        });

    /// <summary>
    /// REPO-MAINT-001 wiki publishing. Regenerates the wiki source set from
    /// the MCP requirements store and pushes the Azure DevOps + GitHub wiki
    /// repos via tools/Publish-Wiki.ps1. Requires the ADO_PAT and
    /// GITHUB_TOKEN environment variables (each target is skipped if its
    /// token is absent so the target is safe to invoke without both).
    /// </summary>
    Target PublishWiki => _ => _
        .Executes(() =>
        {
            var script = RootDirectory / "tools" / "Publish-Wiki.ps1";
            Serilog.Log.Information("Invoking wiki publisher: {Script}", script);
            var args = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + script + "\" -Target both -RegenerateSource";
            RunProcess("pwsh.exe", args, throwOnNonZero: true);
        });

    // ---- MSI / winget pipeline ----------------------------------------------

    [Parameter("Override the MSI/winget ProductVersion. Default: GitVersion {Major}.{Minor}.{CommitsSinceVersionSource}.")]
    readonly string MsiVersionOverride = null!;

    string? _msiVersionCache;

    /// <summary>
    /// MSI/winget ProductVersion, derived from GitVersion so each build past a
    /// new commit gets a fresh, upgradeable version (0.1.&lt;commitHeight&gt;).
    /// Falls back to the git commit count if GitVersion is unavailable. The WiX
    /// MajorUpgrade (AllowSameVersionUpgrades) makes same-version redeploys
    /// reinstall. Override with --msi-version-override.
    /// </summary>
    string MsiVersion => _msiVersionCache ??= ResolveMsiVersion();

    string ResolveMsiVersion()
    {
        if (!string.IsNullOrWhiteSpace(MsiVersionOverride))
            return MsiVersionOverride;

        // Primary: GitVersion (the repo's chosen versioning tool).
        try
        {
            var json = string.Join(
                "\n",
                ProcessTasks.StartProcess(
                        "dotnet-gitversion",
                        $"/targetpath \"{RootDirectory}\" /output json",
                        logOutput: false)
                    .AssertZeroExitCode()
                    .Output.Select(o => o.Text));
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            var version =
                $"{r.GetProperty("Major").GetInt32()}." +
                $"{r.GetProperty("Minor").GetInt32()}." +
                $"{r.GetProperty("CommitsSinceVersionSource").GetInt32()}";
            Serilog.Log.Information("MSI version from GitVersion: {Version}", version);
            return version;
        }
        catch (Exception ex)
        {
            // Fallback: git commit height - monotonic and always available.
            Serilog.Log.Warning("GitVersion unavailable ({Message}); using git commit-count fallback", ex.Message);
            var count = string.Concat(
                    ProcessTasks.StartProcess("git", "rev-list --count HEAD", RootDirectory, logOutput: false)
                        .AssertZeroExitCode()
                        .Output.Select(o => o.Text))
                .Trim();
            return $"0.1.{(string.IsNullOrWhiteSpace(count) ? "0" : count)}";
        }
    }

    [Parameter("Winget package identifier (publisher.identifier). Default sharpninja.ViceSharp.")]
    readonly string WingetPackageId = "sharpninja.ViceSharp";

    [Parameter("Runtime identifier for the desktop publish that feeds PublishMsi. Default win-x64.")]
    readonly string MsiRuntimeIdentifier = "win-x64";

    [Parameter("Disable AOT-style MSI publish outputs. Values: auto, true, false. Default auto (true for Debug, false otherwise).")]
    readonly string MsiAotDisabled = "auto";

    [Parameter("Enable both debug gRPC surfaces in the installed MSI environment. Values: auto, true, false. Default auto (true for Debug, false otherwise).")]
    readonly string MsiEnableDebugGrpc = "auto";

    [Parameter("Bearer token installed for the Avalonia RemoteControl debug gRPC surface when --msi-enable-debug-grpc is true.")]
    readonly string MsiRemoteControlToken = "vicesharp-debug-local";

    [Parameter("Loopback port installed for the Avalonia RemoteControl debug gRPC surface when --msi-enable-debug-grpc is true.")]
    readonly int MsiRemoteControlPort = 53535;

    AbsolutePath AvaloniaProject => RootDirectory / "src" / "ViceSharp.Avalonia" / "ViceSharp.Avalonia.csproj";

    AbsolutePath AvaloniaPublishDir =>
        RootDirectory / "src" / "ViceSharp.Avalonia" / "bin" / Configuration / "net10.0" / MsiRuntimeIdentifier / "publish";

    AbsolutePath InstallerProject => RootDirectory / "installer" / "ViceSharp.Installer.wixproj";

    AbsolutePath InstallerOutputDir => ArtifactsDirectory / "installer";

    AbsolutePath MsiOutputPath => InstallerOutputDir / "ViceSharp.msi";

    AbsolutePath WingetOutputDir => ArtifactsDirectory / "winget";

    /// <summary>
    /// Publish the ViceSharp.Avalonia desktop GUI as a self-contained
    /// Windows desktop application, then pack it into a per-machine MSI via
    /// the WixToolset.Sdk wixproj at installer/. The MSI lands at
    /// artifacts/installer/ViceSharp.msi with version <see cref="MsiVersion"/>.
    /// </summary>
    Target PublishMsi => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            var isDebugMsi = string.Equals(Configuration, "Debug", StringComparison.OrdinalIgnoreCase);
            var aotDisabled = ResolveAutoBoolean(MsiAotDisabled, isDebugMsi, nameof(MsiAotDisabled));
            var enableDebugGrpc = ResolveAutoBoolean(MsiEnableDebugGrpc, isDebugMsi, nameof(MsiEnableDebugGrpc));
            if (enableDebugGrpc)
            {
                if (string.IsNullOrWhiteSpace(MsiRemoteControlToken))
                    throw new InvalidOperationException("MsiRemoteControlToken must be non-empty when debug gRPC surfaces are enabled.");
                if (MsiRemoteControlToken.Contains(';', StringComparison.Ordinal))
                    throw new InvalidOperationException("MsiRemoteControlToken cannot contain ';' because WiX DefineConstants uses ';' separators.");
                if (MsiRemoteControlPort <= 0 || MsiRemoteControlPort > 65535)
                    throw new InvalidOperationException("MsiRemoteControlPort must be in the range 1..65535.");
            }

            // Step 1: self-contained JIT publish of ViceSharp.Avalonia.
            // Native AOT stays disabled so the debug gRPC surfaces and debugger
            // attach path remain usable, but the MSI payload is trimmed and
            // single-file so installed startup does not crawl through hundreds of
            // separate framework/package assemblies.
            Serilog.Log.Information(
                "Publishing ViceSharp.Avalonia ({Configuration}, self-contained trimmed single-file JIT, ReadyToRun={ReadyToRun}, DebugGrpc={DebugGrpc}, {Rid}) -> {Out}",
                Configuration,
                !aotDisabled,
                enableDebugGrpc,
                MsiRuntimeIdentifier,
                AvaloniaPublishDir);
            DotNetPublish(s => s
                .SetProject(AvaloniaProject)
                .SetConfiguration(Configuration)
                .SetRuntime(MsiRuntimeIdentifier)
                // Stamp the GitVersion semver into the published assembly (AssemblyVersion,
                // FileVersion, InformationalVersion) so the running app can show it in the
                // window title. Without this the exe stays at the default 1.0.0 and every
                // deployed build looks identical. Matches the MSI ProductVersion below.
                .SetVersion(MsiVersion)
                .SetSelfContained(true)
                .SetProperty("PublishAot", "false")
                .SetProperty("PublishTrimmed", "true")
                .SetProperty("TrimMode", "partial")
                .SetProperty("ILLinkTreatWarningsAsErrors", "false")
                .SetProperty("PublishReadyToRun", aotDisabled ? "false" : "true")
                .SetProperty("PublishSingleFile", "true")
                .SetProperty("IncludeNativeLibrariesForSelfExtract", "true")
                .SetProperty("DebugType", isDebugMsi ? "portable" : "none")
                .SetProperty("DebugSymbols", isDebugMsi ? "true" : "false")
                .SetProperty("DocumentationFile", string.Empty)
                .SetProperty("GenerateDocumentationFile", "false")
                .SetProperty("CopyDocumentationFilesFromPackages", "false"));

            var exePath = AvaloniaPublishDir / "ViceSharp.Avalonia.exe";
            if (!exePath.FileExists())
                throw new InvalidOperationException(
                    $"Expected Avalonia entry exe at {exePath} but it was not produced. PublishMsi cannot continue.");

            // Step 1b: prune oversized files we don't want in the MSI.
            // WiX 5's <Files Include="**"> harvest is all-or-nothing per
            // glob, so apply the exclusion here. The native PDBs are the
            // big ticket items (libSkiaSharp.pdb ~84 MB,
            // libHarfBuzzSharp.pdb ~21 MB) and end users never attach a
            // debugger from Program Files.
            string[] excludeGlobs = isDebugMsi
                ? ["**/*.xml", "**/*.dbg", "**/createdump.exe"]
                : ["**/*.pdb", "**/*.xml", "**/*.dbg", "**/createdump.exe"];
            long deletedBytes = 0;
            int deletedCount = 0;
            foreach (var glob in excludeGlobs)
            {
                foreach (var f in AvaloniaPublishDir.GlobFiles(glob))
                {
                    var size = new System.IO.FileInfo(f).Length;
                    f.DeleteFile();
                    deletedBytes += size;
                    deletedCount++;
                }
            }
            Serilog.Log.Information("Pruned {Count} debug/doc files from publish dir ({Bytes:N0} bytes / {Mb:F1} MB)",
                deletedCount, deletedBytes, deletedBytes / 1024.0 / 1024.0);

            // Step 2: build the WiX installer project.
            InstallerOutputDir.CreateOrCleanDirectory();
            Serilog.Log.Information(
                "Building installer wixproj with PublishedRoot={Root}, DebugGrpc={DebugGrpc}, RemoteControlPort={RemoteControlPort}",
                AvaloniaPublishDir,
                enableDebugGrpc,
                MsiRemoteControlPort);
            DotNetBuild(s => s
                .SetProjectFile(InstallerProject)
                .SetConfiguration(Configuration)
                .SetProperty("ProductVersion", MsiVersion)
                .SetProperty("PublishedRoot", AvaloniaPublishDir)
                .SetProperty("EnableDebugGrpc", enableDebugGrpc ? "true" : "false")
                .SetProperty("RemoteControlToken", enableDebugGrpc ? MsiRemoteControlToken : string.Empty)
                .SetProperty("RemoteControlPort", enableDebugGrpc ? MsiRemoteControlPort.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty));

            // Step 3: copy the produced MSI to artifacts/installer/.
            var built = (RootDirectory / "installer" / "bin" / "x64" / Configuration / "en-US")
                .GlobFiles("*.msi").FirstOrDefault();
            if (built is null || !built.FileExists())
                built = (RootDirectory / "installer" / "bin" / "x64" / Configuration)
                    .GlobFiles("*.msi").FirstOrDefault();
            if (built is null || !built.FileExists())
                throw new InvalidOperationException(
                    $"WiX did not produce an MSI under installer/bin/x64/{Configuration}. Check the wixproj build log.");

            built.CopyToDirectory(InstallerOutputDir, ExistsPolicy.FileOverwrite);
            var dest = InstallerOutputDir / built.Name;
            if (dest != MsiOutputPath)
                dest.Rename(MsiOutputPath.Name, ExistsPolicy.FileOverwrite);

            Serilog.Log.Information("MSI ready: {Path} ({Size:N0} bytes)", MsiOutputPath, new System.IO.FileInfo(MsiOutputPath).Length);
        });

    static bool ResolveAutoBoolean(string value, bool autoValue, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
            return autoValue;
        if (bool.TryParse(value, out var parsed))
            return parsed;
        if (value is "1" or "0")
            return value == "1";

        throw new InvalidOperationException($"{parameterName} must be auto, true, false, 1, or 0.");
    }

    /// <summary>
    /// Install the most recent PublishMsi output silently via msiexec.
    /// The per-machine MSI requires elevation; when the current process is
    /// not already running as Administrator the target self-elevates via
    /// `gsudo` (preferred) or `sudo` (PowerShell 7.5+ on Windows 11) so a
    /// plain `pwsh ./build.ps1 InstallMsi` works for the user with a
    /// single UAC prompt. Exit code 3010 (success-reboot-required) is
    /// treated as success.
    /// </summary>
    Target InstallMsi => _ => _
        .DependsOn(PublishMsi)
        .Executes(() =>
        {
            if (!MsiOutputPath.FileExists())
                throw new InvalidOperationException(
                    $"MSI not found at {MsiOutputPath}. Run PublishMsi first.");

            var logPath = InstallerOutputDir / "install.log";
            var msiArgs = $"/i \"{MsiOutputPath}\" /qn /norestart /log \"{logPath}\"";

            int exitCode;
            if (IsCurrentProcessElevated())
            {
                Serilog.Log.Information("Installing {Msi} via msiexec /qn (already elevated)", MsiOutputPath);
                exitCode = RunProcess("msiexec.exe", msiArgs, throwOnNonZero: false);
            }
            else
            {
                var elevator = FindOnPath("gsudo.exe") ?? FindOnPath("gsudo")
                            ?? FindOnPath("sudo.exe") ?? FindOnPath("sudo");
                if (elevator is null)
                    throw new InvalidOperationException(
                        "InstallMsi needs Administrator privileges to write Program Files but "
                      + "neither gsudo nor sudo was found on PATH. Either install gsudo "
                      + "(`winget install gerardog.gsudo`) or run this target from an already "
                      + "elevated PowerShell session.");

                Serilog.Log.Information(
                    "Installing {Msi} via {Elevator} -> msiexec /qn (will trigger UAC prompt)",
                    MsiOutputPath, System.IO.Path.GetFileName(elevator));
                var elevatedArgs = $"msiexec.exe {msiArgs}";
                exitCode = RunProcess(elevator, elevatedArgs, throwOnNonZero: false);
            }

            if (exitCode is not (0 or 3010))
                throw new InvalidOperationException(
                    $"msiexec exited with code {exitCode}. See {logPath} for details "
                  + "(MSI error 1925 = elevation required; 1603 = generic install failure; "
                  + "consult the log's last 'Error' line).");

            Serilog.Log.Information("InstallMsi complete (exit {ExitCode})", exitCode);
        });

    static bool IsCurrentProcessElevated()
    {
        if (!OperatingSystem.IsWindows())
            return true;  // non-Windows: leave it to the user; msiexec is a no-op anyway
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generate winget manifests for the latest PublishMsi output and stage
    /// them under artifacts/winget/. The manifests follow the winget singleton
    /// schema 1.6 (Version + Installer + DefaultLocale). When
    /// `wingetcreate` is available on PATH the target also runs
    /// `wingetcreate submit` to open a PR against microsoft/winget-pkgs;
    /// otherwise the manifests are left in artifacts/winget/ for manual PR.
    /// </summary>
    Target PublishWinget => _ => _
        .DependsOn(PublishMsi)
        .Executes(() =>
        {
            if (!MsiOutputPath.FileExists())
                throw new InvalidOperationException(
                    $"MSI not found at {MsiOutputPath}. PublishWinget depends on PublishMsi.");

            WingetOutputDir.CreateOrCleanDirectory();

            // Compute SHA256 of the MSI (winget manifest requires it).
            string sha;
            using (var stream = System.IO.File.OpenRead(MsiOutputPath))
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(stream);
                sha = Convert.ToHexString(hash);
            }
            Serilog.Log.Information("MSI SHA256: {Sha}", sha);

            var (publisher, identifier) = SplitWingetPackageId(WingetPackageId);
            var manifestsDir = WingetOutputDir / "manifests" / publisher.Substring(0, 1).ToLowerInvariant() / publisher / identifier / MsiVersion;
            manifestsDir.CreateDirectory();

            // Version manifest (root).
            var versionYaml =
$@"# yaml-language-server: $schema=https://aka.ms/winget-manifest.version.1.6.0.schema.json
PackageIdentifier: {WingetPackageId}
PackageVersion: {MsiVersion}
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.6.0
";
            System.IO.File.WriteAllText(manifestsDir / $"{WingetPackageId}.yaml", versionYaml);

            // Installer manifest.
            var installerYaml =
$@"# yaml-language-server: $schema=https://aka.ms/winget-manifest.installer.1.6.0.schema.json
PackageIdentifier: {WingetPackageId}
PackageVersion: {MsiVersion}
InstallerType: wix
Scope: machine
InstallModes:
  - silent
  - silentWithProgress
UpgradeBehavior: install
Installers:
  - Architecture: x64
    InstallerUrl: https://github.com/sharpninja/vice-sharp/releases/download/v{MsiVersion}/ViceSharp.msi
    InstallerSha256: {sha}
    InstallerType: wix
ManifestType: installer
ManifestVersion: 1.6.0
";
            System.IO.File.WriteAllText(manifestsDir / $"{WingetPackageId}.installer.yaml", installerYaml);

            // Default locale manifest.
            var localeYaml =
$@"# yaml-language-server: $schema=https://aka.ms/winget-manifest.defaultLocale.1.6.0.schema.json
PackageIdentifier: {WingetPackageId}
PackageVersion: {MsiVersion}
PackageLocale: en-US
Publisher: {publisher}
PackageName: ViceSharp
License: MIT
ShortDescription: VICE-compatible Commodore 64 emulator (Avalonia GUI).
Description: |
  ViceSharp is a .NET 10 Commodore 64 emulator with VICE compatibility.
  This package installs the ViceSharp Avalonia desktop GUI: a full host
  with monitor, drive / tape attach, debug stepping, snapshot, frame
  capture, and cycle-accurate VIC-II / SID / CIA emulation.
Moniker: vicesharp
Tags:
  - commodore
  - c64
  - emulator
  - vice
PublisherUrl: https://github.com/sharpninja
PackageUrl: https://github.com/sharpninja/vice-sharp
ManifestType: defaultLocale
ManifestVersion: 1.6.0
";
            System.IO.File.WriteAllText(manifestsDir / $"{WingetPackageId}.locale.en-US.yaml", localeYaml);

            Serilog.Log.Information("Winget manifests staged at {Dir}", manifestsDir);

            // Optional: invoke `wingetcreate submit` if available.
            var wingetcreate = FindOnPath("wingetcreate.exe") ?? FindOnPath("wingetcreate");
            if (wingetcreate is not null)
            {
                Serilog.Log.Information("Found wingetcreate at {Path}; submitting manifests", wingetcreate);
                var token = Environment.GetEnvironmentVariable("WINGET_PAT");
                var tokenArg = string.IsNullOrEmpty(token) ? string.Empty : $" --token {token}";
                var args = $"submit \"{manifestsDir}\"{tokenArg}";
                RunProcess(wingetcreate, args, throwOnNonZero: true);
            }
            else
            {
                Serilog.Log.Information(
                    "wingetcreate not on PATH; manifests left in {Dir} for manual PR to microsoft/winget-pkgs.",
                    manifestsDir);
            }
        });

    static (string publisher, string identifier) SplitWingetPackageId(string id)
    {
        var parts = id.Split('.', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (id, id);
    }

    static string? FindOnPath(string name)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in path.Split(System.IO.Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            try
            {
                var candidate = System.IO.Path.Combine(dir, name);
                if (System.IO.File.Exists(candidate)) return candidate;
            }
            catch { }
        }
        return null;
    }

    static int RunProcess(string fileName, string args, bool throwOnNonZero)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(fileName, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        p.OutputDataReceived += (_, e) => { if (e.Data != null) Serilog.Log.Information(e.Data); };
        p.ErrorDataReceived  += (_, e) => { if (e.Data != null) Serilog.Log.Warning(e.Data); };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();
        if (throwOnNonZero && p.ExitCode != 0)
            throw new InvalidOperationException($"{System.IO.Path.GetFileName(fileName)} exited with code {p.ExitCode}.");
        return p.ExitCode;
    }
}
