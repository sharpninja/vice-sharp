using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;

// Nuke-generated Azure DevOps pipelines (regenerated on every local build run),
// both dispatching to the self-hosted "Default" agent pool via
// DefaultPoolAzurePipelinesAttribute (the runners carry the native toolchain
// and the NUGET_API_KEY environment - no pipeline secret, no vmImage):
// - azure-pipelines.ci.yml: Test on branch pushes.
// - azure-pipelines.release.yml: PublishNuget on v* tags only - the tag-gated
//   reproducible NuGet release path (single self-sufficient job; PublishNuget's
//   own gate re-verifies tag/version/pack integrity).
[DefaultPoolAzurePipelines(
    "ci",
    AzurePipelinesImage.WindowsLatest,
    InvokedTargets = new[] { nameof(CiTest) },
    TriggerBranchesInclude = new[] { "master", "main", "feat/*" },
    PullRequestsBranchesInclude = new[] { "master", "main" },
    CacheKeyFiles = new string[0])]
[DefaultPoolAzurePipelines(
    "release",
    AzurePipelinesImage.WindowsLatest,
    // PublishChocolatey exists and is manually invokable, but is intentionally
    // NOT in the release pipeline yet (held per operator until the CHOCO_API_KEY
    // agent env + community moderation flow are set up).
    InvokedTargets = new[]
    {
        nameof(PublishNuget),
        nameof(PublishGitHubRelease),
        nameof(PublishWinget),
        nameof(PublishScoop),
    },
    TriggerTagsInclude = new[] { "v*" },
    TriggerBranchesInclude = new string[0],
    CacheKeyFiles = new string[0])]
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

    /// <summary>
    /// CI variant of <see cref="Test"/>: dependency-free and self-sufficient
    /// (restores and builds in-job) because the Nuke Azure generator gives
    /// every target its own job on a fresh agent and never moves artifacts
    /// between jobs - run 1029's Test job proved --skip + --no-build on a
    /// clean workspace finds no test assemblies. Local flows keep using Test.
    /// Single process: the .vsf-resume residue that once forced a
    /// SnapshotResume partition is fixed at the mechanism (PLAN-NATIVERESIDUE-001:
    /// TrueEmulation resource re-baseline + vicii.rc/idle_state clears at
    /// machine create in vice-shim.c; TEST-NATIVE-RESIDUE-01/02 guard it).
    /// ROM self-sufficiency: agents have no VICE data root and the repo ships
    /// no ROMs, so runs 1040-1064 failed 342 managed tests with
    /// DirectoryNotFoundException from MachineTestFactory.CreateC64RomProvider.
    /// EnsureCiRomRoot downloads the required dumps (hash-pinned) from the
    /// VICE-Team svn-mirror into artifacts and points VICESHARP_ROM_PATH at
    /// them for the test process only; when the machine already provides a
    /// root (env var or the untracked vendored data tree) it does nothing.
    /// </summary>
    Target CiTest => _ => _
        .Executes(() =>
        {
            var romRoot = EnsureCiRomRoot();
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetFilter("Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy")
                .When(_ => romRoot is not null, x => x
                    .SetProcessEnvironmentVariable("VICESHARP_ROM_PATH", romRoot)));
        });

    /// <summary>
    /// Returns a VICE data root for CI test runs, or null when the machine
    /// already provides one (VICESHARP_ROM_PATH / VICE_DATA_PATH env, or the
    /// untracked vendored tree at native/vice/vice/data that developer
    /// machines carry). Downloads are pinned to SHA256 values verified
    /// 2026-07-08 against both the local authoritative tree and the mirror.
    /// </summary>
    static string? EnsureCiRomRoot()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VICESHARP_ROM_PATH")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VICE_DATA_PATH")))
        {
            return null;
        }

        var vendored = RootDirectory / "native" / "vice" / "vice" / "data";
        if (File.Exists(vendored / "C64" / "kernal-901227-03.bin"))
        {
            return null;
        }

        var romRoot = RootDirectory / "artifacts" / "vice-data";
        var roms = new (string RelativePath, string Sha256)[]
        {
            ("C64/basic-901226-01.bin", "89878CEA0A268734696DE11C4BAE593EAAA506465D2029D619C0E0CBCCDFA62D"),
            ("C64/kernal-901227-03.bin", "83C60D47047D7BEAB8E5B7BF6F67F80DAA088B7A6A27DE0D7E016F6484042721"),
            ("C64/chargen-901225-01.bin", "FD0D53B8480E86163AC98998976C72CC58D5DD8EB824ED7B829774E74213B420"),
            // Variant-model ROMs referenced by C64MachineProfiles (run 1068:
            // 56 profile tests failed on missing GS/SX-64/Japanese/old-rev sets).
            ("C64/kernal-901227-01.bin", "F585600A2E47D0224459E878184D3F4CCE2665BFCCE65E61732530C6D89C4269"),
            ("C64/kernal-901227-02.bin", "675CF19A3E04BF68E41CC7A9010B353E51845ED9069DB65496C52B79F1E1385F"),
            ("C64/kernal-390852-01.bin", "0A644F5E7F6EF8CB67B07E7A80F3F9A35584D9B131294C64F6E747A7E5C8A789"),
            ("C64/kernal-251104-04.bin", "81AD3A08920F2782AB031BB3AE02789D1F5F216C3D24D262E752A2B13E9FAF73"),
            ("C64/kernal-901246-01.bin", "B2B430A134514AF11F3ED64C0929FC5241D8436BA7B12514EC02D1B2CB2FD533"),
            ("C64/kernal-906145-02.bin", "48AF48070EC9ADDA79AE7F65B772C422062EB12E69214A7CF788E6947A3B4117"),
            ("C64/chargen-906143-02.bin", "0F4A5B0F9EC0D86639BA316053E330FDD849692623A41F8DE4A420FCD47C55EE"),
            // Keymap data file (not a ROM): C64VkmKeyboardTests loads the
            // positional map through ViceDataPathResolver (run 1069's last 2 fails).
            ("C64/gtk3_pos.vkm", "DF7332C52430EC344C9821EAD8F16650CA31DC5676D0C40C5915E5594BD3493D"),
            ("DRIVES/dos1541-325302-01+901229-05.bin", "D1D45AFB46FD4E2B48D93CA367B889D75654A3B7ACF73044E51F6D880C09369E"),
            ("DRIVES/dos1541ii-251968-03.bin", "326C289C38753323D7E8167897447CF61EF35189D82EB8D75210ECE949ADDA7C"),
        };

        using var http = new System.Net.Http.HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ViceSharp-Build/1.0");
        foreach (var (relativePath, sha256) in roms)
        {
            var target = romRoot / relativePath;
            if (File.Exists(target) && HashMatches(target, sha256))
            {
                continue;
            }

            var url = $"https://raw.githubusercontent.com/VICE-Team/svn-mirror/main/vice/data/{relativePath}";
            var data = http.GetByteArrayAsync(url).GetAwaiter().GetResult();
            var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data));
            if (!string.Equals(actual, sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"ROM hash mismatch for {relativePath}: expected {sha256}, got {actual}. Refusing to stage an unverified dump.");
            }

            target.Parent.CreateDirectory();
            File.WriteAllBytes(target, data);
            Serilog.Log.Information("CiTest ROM bootstrap: staged {Rom} ({Bytes} bytes, sha256 verified)", relativePath, data.Length);
        }

        return romRoot;
    }

    static bool HashMatches(AbsolutePath path, string expectedSha256)
    {
        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
        return string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }

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
        .Executes(PackAllNugetPackages);

    /// <summary>
    /// The vX.Y.Z release tag on HEAD, or null. When present it wins the pack
    /// version: PublishNuget's gate requires packed versions to equal the tag
    /// (run 1054 failed exactly there - GitVersion's commit-height scheme
    /// yields X.Y.&lt;height&gt;, not the tagged X.Y.Z).
    /// </summary>
    string? HeadReleaseTagVersion()
    {
        var tags = ProcessTasks.StartProcess("git", "tag --points-at HEAD", RootDirectory, logOutput: false)
            .AssertZeroExitCode()
            .Output.Select(o => o.Text.Trim())
            .Where(t => System.Text.RegularExpressions.Regex.IsMatch(t, @"^v\d+\.\d+\.\d+$"))
            .ToList();
        return tags.Count == 1 ? tags[0].TrimStart('v') : null;
    }

    // Shared by PackNuget (local flow, after Compile) and PublishNuget (the
    // Nuke-generated single-job release pipeline, which restores/builds and
    // packs on one agent because the Azure generator never downloads
    // artifacts between jobs - its download side is unimplemented upstream).
    void PackAllNugetPackages()
    {
            var version = string.IsNullOrWhiteSpace(PackageVersionOverride)
                ? HeadReleaseTagVersion() ?? MsiVersion
                : PackageVersionOverride;
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
                // No NoRestore: each pack restores+builds its own project graph,
                // keeping the single-job CI release agent-agnostic (nothing here
                // depends on a prior whole-solution build).
                DotNetPack(s => s
                    .SetProject(RootDirectory / "src" / spec.Name / $"{spec.Name}.csproj")
                    .SetConfiguration(Configuration)
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
    }

    [Parameter("NuGet feed for PublishNuget. Default nuget.org v3.")]
    readonly string NugetSource = "https://api.nuget.org/v3/index.json";

    /// <summary>
    /// Publish the v-tagged release packages to the NuGet feed. Deliberately
    /// dependency-free so the Nuke-generated Azure release pipeline is a
    /// single self-sufficient job (the generator gives every target its own
    /// job on a fresh agent and never downloads artifacts between jobs):
    /// restores/builds the solution and packs fresh from the tagged checkout,
    /// then pushes. The API key comes from the NUGET_API_KEY environment
    /// variable and is never logged (Nuke redacts the ApiKey setting).
    /// Duplicate versions are skipped so re-runs are safe.
    /// </summary>
    Target PublishNuget => _ => _
        .Executes(() =>
        {
            var apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
            Assert.True(!string.IsNullOrWhiteSpace(apiKey), "NUGET_API_KEY environment variable is not set");

            // Build + pack in-job (reproducible from the tagged checkout). Only
            // the bundle's package project graph needs a prior restore+build
            // (its PackageId is injected pack-time and cannot survive an
            // implicit restore); the individual packs restore+build their own
            // graphs. Deliberately NOT the whole solution: the release agent
            // is a lean Linux runner and must not drag in the native-oracle
            // test harness build.
            DotNetBuild(s => s
                .SetProjectFile(PackageProject)
                .SetConfiguration(Configuration));
            PackAllNugetPackages();

            // PUBLISH-GATE: packages ship only from a tagged release commit
            // whose version is NEW: either a fresh minor (vX.Y.0, first tag
            // of its major.minor line) or a build-level increment within the
            // line (vX.Y.Z strictly above every existing vX.Y.* tag).
            // Guarantees published packages are exactly reproducible from a
            // tag and versions only move forward.
            var headTags = ProcessTasks.StartProcess("git", "tag --points-at HEAD", RootDirectory, logOutput: false)
                .AssertZeroExitCode()
                .Output.Select(o => o.Text.Trim())
                .Where(t => System.Text.RegularExpressions.Regex.IsMatch(t, @"^v\d+\.\d+\.\d+$"))
                .ToList();
            Assert.True(headTags.Count == 1,
                $"PublishNuget requires HEAD to carry exactly one release tag (vX.Y.Z); found: [{string.Join(", ", headTags)}]");

            var releaseTag = headTags[0];
            var parts = releaseTag.TrimStart('v').Split('.');
            var releaseBuild = int.Parse(parts[2]);

            var otherSameMinorBuilds = ProcessTasks.StartProcess("git", $"tag --list v{parts[0]}.{parts[1]}.*", RootDirectory, logOutput: false)
                .AssertZeroExitCode()
                .Output.Select(o => o.Text.Trim())
                .Where(t => t.Length > 0 && t != releaseTag)
                .Select(t => int.Parse(t.Split('.')[2]))
                .ToList();
            Assert.True(otherSameMinorBuilds.All(b => b < releaseBuild),
                $"PublishNuget requires {releaseTag} to be the highest build of the v{parts[0]}.{parts[1]}.* line; " +
                $"existing builds: [{string.Join(", ", otherSameMinorBuilds.OrderBy(b => b))}]");

            var expectedVersion = releaseTag.TrimStart('v');
            var packages = PackagesOutputDirectory.GlobFiles("*.nupkg");
            Assert.True(packages.Count > 0, $"no packages found in {PackagesOutputDirectory}");
            Assert.True(packages.All(p => p.Name.EndsWith($".{expectedVersion}.nupkg", StringComparison.OrdinalIgnoreCase)),
                $"packed versions do not match release tag {releaseTag}; repack from the tagged commit");

            foreach (var package in packages.OrderBy(p => p.Name))
            {
                // A push error fails the target (DotNetNuGetPush throws on
                // non-zero exit); indexing latency is handled by the monitor
                // below and never fails the target.
                DotNetNuGetPush(s => s
                    .SetTargetPath(package)
                    .SetSource(NugetSource)
                    .SetApiKey(apiKey)
                    .EnableSkipDuplicate());
            }

            Serilog.Log.Information("Pushed {Count} packages to {Source}; monitoring indexing", packages.Count, NugetSource);

            // Resolution monitor: poll the v3 flat container until every
            // pushed id@version is queryable, up to 10 minutes. Timeout is
            // reported, not fatal (nuget.org indexing latency is not a
            // publish failure).
            var packageIds = packages
                .Select(p => p.NameWithoutExtension)
                .Select(n => n.Substring(0, n.Length - expectedVersion.Length - 1))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var pending = new HashSet<string>(packageIds, StringComparer.OrdinalIgnoreCase);
            var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(10);
            using var http = new System.Net.Http.HttpClient();

            while (pending.Count > 0 && DateTime.UtcNow < deadline)
            {
                foreach (var id in pending.ToList())
                {
                    try
                    {
                        var url = $"https://api.nuget.org/v3-flatcontainer/{id.ToLowerInvariant()}/index.json";
                        var json = http.GetStringAsync(url).GetAwaiter().GetResult();
                        using var doc = JsonDocument.Parse(json);
                        var found = doc.RootElement.GetProperty("versions").EnumerateArray()
                            .Any(v => string.Equals(v.GetString(), expectedVersion, StringComparison.OrdinalIgnoreCase));
                        if (found)
                        {
                            pending.Remove(id);
                            Serilog.Log.Information("Resolved on nuget.org: {Id} {Version}", id, expectedVersion);
                        }
                    }
                    catch (Exception)
                    {
                        // 404 until first indexing; transient errors retry on
                        // the next sweep.
                    }
                }

                if (pending.Count > 0)
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(20));
            }

            if (pending.Count == 0)
            {
                Serilog.Log.Information("All {Count} packages resolved on nuget.org at {Version}", packageIds.Count, expectedVersion);
            }
            else
            {
                Serilog.Log.Warning(
                    "Publish succeeded but {Count} package(s) not yet resolvable after 10 minutes (indexing latency): {Ids}",
                    pending.Count,
                    string.Join(", ", pending.OrderBy(i => i)));
            }

            // Registry listing: after the validated push, enumerate every
            // ViceSharp package nuget.org reports (search API), so the run
            // log records the full published surface. Non-fatal on search
            // hiccups.
            try
            {
                var searchJson = http
                    .GetStringAsync("https://azuresearch-usnc.nuget.org/query?q=ViceSharp&prerelease=true&take=100")
                    .GetAwaiter().GetResult();
                using var searchDoc = JsonDocument.Parse(searchJson);
                var listed = searchDoc.RootElement.GetProperty("data").EnumerateArray()
                    .Select(e => (
                        Id: e.GetProperty("id").GetString() ?? string.Empty,
                        Version: e.GetProperty("version").GetString() ?? string.Empty,
                        Downloads: e.TryGetProperty("totalDownloads", out var d) ? d.GetInt64() : 0))
                    .Where(e => e.Id.StartsWith("ViceSharp", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                Serilog.Log.Information("nuget.org lists {Count} ViceSharp package(s):", listed.Count);
                foreach (var entry in listed)
                    Serilog.Log.Information("  {Id} {Version} (downloads: {Downloads})", entry.Id, entry.Version, entry.Downloads);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Package listing after publish failed (non-fatal): {Message}", ex.Message);
            }
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

    [Parameter("owner/name of the GitHub mirror used for releases (gh --repo).")]
    readonly string GitHubRepo = "sharpninja/vice-sharp";

    /// <summary>
    /// On a vX.Y.Z tag, create (or update) the GitHub release for that tag on
    /// the GitHub mirror and attach the MSI installer plus the NuGet packages.
    /// GitHub is a downstream mirror, so the tagged commit + tag are pushed to
    /// it first (authenticated through the release agent's gh CLI). The target
    /// is self-sufficient: the generated release job runs with --skip, so it
    /// packs its own .nupkg files and builds the MSI via a nested Nuke
    /// invocation rather than relying on DependsOn. Idempotent - a re-run of the
    /// same tag clobber-uploads assets instead of failing. The nuget.org push
    /// stays in PublishNuget's own job.
    /// </summary>
    Target PublishGitHubRelease => _ => _
        .Description("Create/update the GitHub release for the HEAD vX.Y.Z tag with the MSI + NuGet packages.")
        .Executes(() =>
        {
            var version = HeadReleaseTagVersion()
                ?? throw new InvalidOperationException(
                    "PublishGitHubRelease requires HEAD to carry exactly one vX.Y.Z release tag.");
            var tag = "v" + version;

            var gh = FindOnPath("gh.exe") ?? FindOnPath("gh")
                ?? throw new InvalidOperationException(
                    "gh CLI not found on PATH; the release agent must have an authenticated GitHub CLI.");

            var repoUrl = $"https://github.com/{GitHubRepo}.git";
            // The mirror must carry the tagged commit before a release can point
            // at it. Push the tag (which carries its commit + missing ancestors)
            // to GitHub, authenticating git through gh's credential helper. This
            // is the one point the release reaches out to the GitHub mirror.
            RunProcess("git",
                $"-c credential.helper= -c credential.helper=\"!gh auth git-credential\" push {repoUrl} {tag}",
                throwOnNonZero: true);

            // NuGet packages: packed in-process from the tagged checkout (the
            // nuget.org push stays in PublishNuget's own job; here we only need
            // the .nupkg files as release assets).
            PackAllNugetPackages();
            var assets = PackagesOutputDirectory.GlobFiles("*.nupkg")
                .Select(p => p.ToString()).ToList();

            // MSI: WiX packaging is Windows-only. Build it through a nested Nuke
            // invocation so PublishMsi's Restore dependency runs (the generated
            // release job invokes this target with --skip, so a DependsOn here
            // would be skipped).
            if (OperatingSystem.IsWindows())
            {
                RunProcess("pwsh.exe",
                    "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \""
                    + (RootDirectory / "build.ps1") + "\" PublishMsi --configuration Release",
                    throwOnNonZero: true);
                Assert.FileExists(MsiOutputPath);
                assets.Insert(0, MsiOutputPath);
            }
            else
            {
                Serilog.Log.Warning("Skipping MSI asset: WiX packaging requires Windows.");
            }

            var assetArgs = string.Join(" ", assets.Select(a => "\"" + a + "\""));

            // Idempotent: create the release, or clobber-upload assets onto an
            // existing one so a re-run of the same tag does not fail.
            var exists = RunProcess(gh, $"release view {tag} --repo {GitHubRepo}", throwOnNonZero: false) == 0;
            if (exists)
            {
                Serilog.Log.Information("GitHub release {Tag} exists; uploading {Count} asset(s) with --clobber.", tag, assets.Count);
                RunProcess(gh, $"release upload {tag} {assetArgs} --clobber --repo {GitHubRepo}", throwOnNonZero: true);
            }
            else
            {
                RunProcess(gh, $"release create {tag} {assetArgs} --repo {GitHubRepo} --title {tag} --generate-notes", throwOnNonZero: true);
            }

            Serilog.Log.Information("GitHub release {Tag} published to {Repo} ({Count} asset(s)).", tag, GitHubRepo, assets.Count);
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

    [Parameter("Publish the MSI payload with native AOT instead of the standard ReadyToRun+single-file+trimmed JIT publish. Values: auto, true, false. Default auto (false).")]
    readonly string MsiAotEnabled = "auto";

    [Parameter("Allow packaging a Debug-configuration MSI (unoptimized emulator; ~10x slower). Values: auto, true, false. Default auto (false).")]
    readonly string MsiAllowDebug = "auto";

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
            // A Debug MSI runs the per-cycle emulator on unoptimized JIT code (~10x slower;
            // a deployed Debug build measured ~50% of real time on a machine whose Release
            // build sustains ~500%). Local builds default Configuration to Debug, so require
            // an explicit opt-in before packaging one.
            if (isDebugMsi && !ResolveAutoBoolean(MsiAllowDebug, false, nameof(MsiAllowDebug)))
                throw new InvalidOperationException(
                    "PublishMsi with Configuration=Debug produces an unoptimized emulator. " +
                    "Pass --configuration Release (recommended) or --msi-allow-debug true to package a Debug build deliberately.");
            var aotEnabled = ResolveAutoBoolean(MsiAotEnabled, false, nameof(MsiAotEnabled));
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

            // Step 1: self-contained publish of ViceSharp.Avalonia. The standard
            // MSI payload is ALWAYS ReadyToRun + single-file + trimmed JIT (fast
            // startup, one exe, no unoptimized-deploy surprises); --msi-aot-enabled
            // true switches to a native AOT publish instead (AOT is inherently a
            // fully-trimmed single native exe, and PublishReadyToRun/
            // PublishSingleFile do not apply to it).
            Serilog.Log.Information(
                "Publishing ViceSharp.Avalonia ({Configuration}, self-contained {Mode}, DebugGrpc={DebugGrpc}, {Rid}) -> {Out}",
                Configuration,
                aotEnabled ? "native AOT" : "ReadyToRun+single-file+trimmed JIT",
                enableDebugGrpc,
                MsiRuntimeIdentifier,
                AvaloniaPublishDir);
            DotNetPublish(s =>
            {
                s = s
                    .SetProject(AvaloniaProject)
                    .SetConfiguration(Configuration)
                    .SetRuntime(MsiRuntimeIdentifier)
                    // Stamp the GitVersion semver into the published assembly (AssemblyVersion,
                    // FileVersion, InformationalVersion) so the running app can show it in the
                    // window title. Without this the exe stays at the default 1.0.0 and every
                    // deployed build looks identical. Matches the MSI ProductVersion below.
                    .SetVersion(MsiVersion)
                    .SetSelfContained(true)
                    .SetProperty("ILLinkTreatWarningsAsErrors", "false")
                    .SetProperty("DebugType", isDebugMsi ? "portable" : "none")
                    .SetProperty("DebugSymbols", isDebugMsi ? "true" : "false")
                    .SetProperty("DocumentationFile", string.Empty)
                    .SetProperty("GenerateDocumentationFile", "false")
                    .SetProperty("CopyDocumentationFilesFromPackages", "false");

                return aotEnabled
                    ? s.SetProperty("PublishAot", "true")
                    : s
                        .SetProperty("PublishAot", "false")
                        .SetProperty("PublishTrimmed", "true")
                        .SetProperty("TrimMode", "partial")
                        .SetProperty("PublishReadyToRun", "true")
                        .SetProperty("PublishSingleFile", "true")
                        .SetProperty("IncludeNativeLibrariesForSelfExtract", "true");
            });

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
        .DependsOn(PublishGitHubRelease)
        .Executes(() =>
        {
            var version = HeadReleaseTagVersion()
                ?? throw new InvalidOperationException(
                    "PublishWinget requires HEAD to carry exactly one vX.Y.Z release tag.");

            WingetOutputDir.CreateOrCleanDirectory();

            // Hash the exact MSI attached to the GitHub release (WiX MSIs are not
            // byte-reproducible, so a rebuilt MSI would checksum differently than
            // the asset winget actually downloads and validates).
            var sha = Sha256Hex(DownloadReleasedMsi(version));
            Serilog.Log.Information("Released MSI SHA256: {Sha}", sha);

            var (publisher, identifier) = SplitWingetPackageId(WingetPackageId);
            var manifestsDir = WingetOutputDir / "manifests" / publisher.Substring(0, 1).ToLowerInvariant() / publisher / identifier / version;
            manifestsDir.CreateDirectory();

            // Version manifest (root).
            var versionYaml =
$@"# yaml-language-server: $schema=https://aka.ms/winget-manifest.version.1.6.0.schema.json
PackageIdentifier: {WingetPackageId}
PackageVersion: {version}
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.6.0
";
            System.IO.File.WriteAllText(manifestsDir / $"{WingetPackageId}.yaml", versionYaml);

            // Installer manifest.
            var installerYaml =
$@"# yaml-language-server: $schema=https://aka.ms/winget-manifest.installer.1.6.0.schema.json
PackageIdentifier: {WingetPackageId}
PackageVersion: {version}
InstallerType: wix
Scope: machine
InstallModes:
  - silent
  - silentWithProgress
UpgradeBehavior: install
Installers:
  - Architecture: x64
    InstallerUrl: https://github.com/sharpninja/vice-sharp/releases/download/v{version}/ViceSharp.msi
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
PackageVersion: {version}
PackageLocale: en-US
Publisher: {publisher}
PackageName: ViceSharp
License: GPL-2.0-or-later
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

            // Submit the manifests as a PR to microsoft/winget-pkgs. Install the
            // Microsoft.WingetCreate .NET tool if it is not already on PATH, and
            // authenticate with WINGET_PAT or, failing that, the release agent's
            // gh CLI token.
            var wingetcreate = FindOnPath("wingetcreate.exe") ?? FindOnPath("wingetcreate");
            if (wingetcreate is null)
            {
                // wingetcreate is NOT a dotnet global tool (not on nuget.org); it
                // ships as a standalone exe. Download the self-contained build so
                // there is no .NET runtime version dependency on the agent.
                Serilog.Log.Information("wingetcreate not found; downloading the standalone self-contained build.");
                var exe = TemporaryDirectory / "wingetcreate.exe";
                using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(5) })
                {
                    var bytes = http.GetByteArrayAsync("https://aka.ms/wingetcreate/latest/self-contained").GetAwaiter().GetResult();
                    System.IO.File.WriteAllBytes(exe, bytes);
                }
                wingetcreate = exe.FileExists() ? exe.ToString() : null;
            }

            var token = Environment.GetEnvironmentVariable("WINGET_PAT");
            if (string.IsNullOrEmpty(token))
            {
                var gh = FindOnPath("gh.exe") ?? FindOnPath("gh");
                if (gh is not null)
                    token = ProcessTasks.StartProcess(gh, "auth token", RootDirectory, logOutput: false)
                        .Output.Select(o => o.Text.Trim()).FirstOrDefault(t => t.Length > 0);
            }

            if (wingetcreate is null)
                Serilog.Log.Warning("wingetcreate unavailable; manifests left in {Dir} for a manual winget-pkgs PR.", manifestsDir);
            else if (string.IsNullOrEmpty(token))
                Serilog.Log.Warning("No WINGET_PAT and no gh token; manifests staged at {Dir} but NOT submitted.", manifestsDir);
            else if (!WingetPackageExists(WingetPackageId))
            {
                // First-time: the package does not exist in winget-pkgs yet, so
                // submit the freshly-authored complete manifests to create it.
                Serilog.Log.Information("{Id} not in winget-pkgs; submitting the first-time manifests.", WingetPackageId);
                RunProcess(wingetcreate, $"submit \"{manifestsDir}\" --token {token}", throwOnNonZero: true);
            }
            else
            {
                // Existing: canonical version bump. `update` fetches the current
                // manifest and changes only the version + installer, preserving
                // any locale edits made by winget-pkgs maintainers.
                Serilog.Log.Information("{Id} exists in winget-pkgs; submitting a version update to {Version}.", WingetPackageId, version);
                RunProcess(wingetcreate, $"update {WingetPackageId} --version {version} --urls \"{ReleaseMsiUrl(version)}\" --submit --token {token}", throwOnNonZero: true);
            }
        });

    /// <summary>
    /// True when the package already exists in microsoft/winget-pkgs, so a
    /// release should submit a version UPDATE rather than create a first-time
    /// NEW package. Probes the manifests folder through the GitHub contents API
    /// with the agent's gh CLI (no winget client needed on the agent). Falls
    /// back to false (first-time submit) when gh is unavailable - a plain submit
    /// also creates a new package, so this only affects new-vs-update wording.
    /// </summary>
    bool WingetPackageExists(string packageId)
    {
        var gh = FindOnPath("gh.exe") ?? FindOnPath("gh");
        if (gh is null) return false;
        var (publisher, identifier) = SplitWingetPackageId(packageId);
        var path = $"manifests/{publisher.Substring(0, 1).ToLowerInvariant()}/{publisher}/{identifier}";
        var exit = RunProcess(gh, $"api repos/microsoft/winget-pkgs/contents/{path} --silent", throwOnNonZero: false);
        Serilog.Log.Information("winget-pkgs {Path} -> {Result}.", path, exit == 0 ? "exists (update)" : "not found (first-time)");
        return exit == 0;
    }

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

    // ---- Package-manager publishing (winget / chocolatey / scoop) -----------
    // All three key off the single GitHub release MSI asset. WiX MSIs are NOT
    // byte-reproducible (per-build package code), so a checksum MUST be taken
    // over the exact released asset, never a locally rebuilt MSI. Hence these
    // targets DependsOn(PublishGitHubRelease) and download the released MSI.

    string ReleaseMsiUrl(string version) =>
        $"https://github.com/{GitHubRepo}/releases/download/v{version}/ViceSharp.msi";

    /// <summary>Download the MSI attached to the GitHub release for <paramref name="version"/> and return its local path.</summary>
    AbsolutePath DownloadReleasedMsi(string version)
    {
        var url = ReleaseMsiUrl(version);
        var dest = TemporaryDirectory / "ViceSharp.released.msi";
        Serilog.Log.Information("Downloading released MSI: {Url}", url);
        using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var bytes = http.GetByteArrayAsync(url).GetAwaiter().GetResult();
        System.IO.File.WriteAllBytes(dest, bytes);
        Serilog.Log.Information("Downloaded {Bytes:N0} bytes.", bytes.Length);
        return dest;
    }

    static string Sha256Hex(AbsolutePath file)
    {
        using var stream = System.IO.File.OpenRead(file);
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(stream));
    }

    AbsolutePath ChocolateySourceDir => RootDirectory / "packaging" / "chocolatey";
    AbsolutePath ChocolateyOutputDir => ArtifactsDirectory / "chocolatey";

    /// <summary>
    /// Build + push the Chocolatey package for the HEAD vX.Y.Z tag: stage the
    /// nuspec + tools, inject the released MSI URL + SHA256 into
    /// chocolateyinstall.ps1, `choco pack`, then `choco push` to
    /// community.chocolatey.org (subject to community moderation). The push is
    /// skipped - package left under artifacts/chocolatey - when ChocoApiKey or
    /// choco itself is unavailable.
    /// </summary>
    Target PublishChocolatey => _ => _
        .DependsOn(PublishGitHubRelease)
        .Executes(() =>
        {
            var version = HeadReleaseTagVersion()
                ?? throw new InvalidOperationException(
                    "PublishChocolatey requires HEAD to carry exactly one vX.Y.Z release tag.");

            var choco = FindOnPath("choco.exe") ?? FindOnPath("choco");
            if (choco is null)
            {
                Serilog.Log.Warning("choco not on PATH; skipping the Chocolatey package.");
                return;
            }

            var url = ReleaseMsiUrl(version);
            var sha = Sha256Hex(DownloadReleasedMsi(version));

            // Stage a copy of the package and inject the url + checksum into the
            // install script (the source stays a token template).
            var stage = ChocolateyOutputDir / "pkg";
            stage.CreateOrCleanDirectory();
            (stage / "tools").CreateDirectory();
            System.IO.File.Copy(ChocolateySourceDir / "vice-sharp.nuspec", stage / "vice-sharp.nuspec", overwrite: true);
            System.IO.File.Copy(ChocolateySourceDir / "tools" / "chocolateyuninstall.ps1", stage / "tools" / "chocolateyuninstall.ps1", overwrite: true);
            var install = System.IO.File.ReadAllText(ChocolateySourceDir / "tools" / "chocolateyinstall.ps1")
                .Replace("__URL__", url)
                .Replace("__SHA256__", sha);
            System.IO.File.WriteAllText(stage / "tools" / "chocolateyinstall.ps1", install);

            RunProcess(choco,
                $"pack \"{stage / "vice-sharp.nuspec"}\" --version {version} --outputdirectory \"{ChocolateyOutputDir}\"",
                throwOnNonZero: true);

            var nupkg = ChocolateyOutputDir.GlobFiles($"vice-sharp.{version}.nupkg").FirstOrDefault()
                ?? throw new InvalidOperationException($"choco pack did not produce vice-sharp.{version}.nupkg.");

            // Secrets reach the self-hosted release agent through its machine
            // environment (the same way PublishNuget reads NUGET_API_KEY).
            var chocoApiKey = Environment.GetEnvironmentVariable("CHOCO_API_KEY");
            if (string.IsNullOrWhiteSpace(chocoApiKey))
            {
                Serilog.Log.Warning("CHOCO_API_KEY not set; packed {Nupkg} but NOT pushing to chocolatey.org.", nupkg);
                return;
            }
            RunProcess(choco, $"push \"{nupkg}\" --source https://push.chocolatey.org/ --api-key {chocoApiKey}", throwOnNonZero: true);
            Serilog.Log.Information("Pushed {Nupkg} to chocolatey.org (pending community moderation).", nupkg);
        });

    AbsolutePath ScoopManifestPath => RootDirectory / "bucket" / "vice-sharp.json";

    /// <summary>
    /// Update the in-repo Scoop manifest (bucket/vice-sharp.json) for the HEAD
    /// vX.Y.Z tag - version, released MSI URL, and SHA256 (lowercase hex) - and
    /// push it to the GitHub mirror's default branch (authenticated via gh's
    /// credential helper) so `scoop update` sees the new version. Users add the
    /// bucket with:
    ///   scoop bucket add vicesharp https://github.com/sharpninja/vice-sharp
    /// The commit is based on the mirror's current master (not the detached tag
    /// checkout), so it lands as a normal master commit.
    /// </summary>
    Target PublishScoop => _ => _
        .DependsOn(PublishGitHubRelease)
        .Executes(() =>
        {
            var version = HeadReleaseTagVersion()
                ?? throw new InvalidOperationException(
                    "PublishScoop requires HEAD to carry exactly one vX.Y.Z release tag.");

            var url = ReleaseMsiUrl(version);
            var sha = Sha256Hex(DownloadReleasedMsi(version)).ToLowerInvariant();
            var repoUrl = $"https://github.com/{GitHubRepo}.git";

            // Base the manifest change on the mirror's current master so it lands
            // as a clean master commit (the pipeline runs on a detached tag).
            RunProcess("git", $"fetch --depth 1 {repoUrl} master", throwOnNonZero: true);
            RunProcess("git", "checkout -B scoop-publish FETCH_HEAD", throwOnNonZero: true);

            var json = System.Text.Json.Nodes.JsonNode.Parse(System.IO.File.ReadAllText(ScoopManifestPath))!;
            json["version"] = version;
            json["architecture"]!["64bit"]!["url"] = url;
            json["architecture"]!["64bit"]!["hash"] = sha;
            var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            System.IO.File.WriteAllText(ScoopManifestPath, json.ToJsonString(opts) + "\n");

            RunProcess("git", $"add \"{ScoopManifestPath}\"", throwOnNonZero: true);
            RunProcess("git",
                $"-c user.email=ci@vicesharp -c user.name=\"ViceSharp CI\" commit -m \"chore(scoop): vice-sharp {version}\"",
                throwOnNonZero: false); // no-op if the manifest is unchanged
            RunProcess("git",
                $"-c credential.helper= -c credential.helper=\"!gh auth git-credential\" push {repoUrl} HEAD:master",
                throwOnNonZero: true);
            Serilog.Log.Information("Scoop manifest for {Version} pushed to the mirror's master.", version);
        });
}
