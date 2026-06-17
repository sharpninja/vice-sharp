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
                .SetFilter("Category!=Determinism"));
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

    Target PublishAot => _ => _
        .DependsOn(Clean, Restore)
        .Executes(() =>
        {
            Serilog.Log.Information("PublishAot target — no publishable projects yet");
        });

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

    [Parameter("Runtime identifier for the AOT publish that feeds PublishMsi. Default win-x64.")]
    readonly string MsiRuntimeIdentifier = "win-x64";

    AbsolutePath AvaloniaProject => RootDirectory / "src" / "ViceSharp.Avalonia" / "ViceSharp.Avalonia.csproj";

    AbsolutePath AvaloniaPublishDir =>
        RootDirectory / "src" / "ViceSharp.Avalonia" / "bin" / "Release" / "net10.0" / MsiRuntimeIdentifier / "publish";

    AbsolutePath InstallerProject => RootDirectory / "installer" / "ViceSharp.Installer.wixproj";

    AbsolutePath InstallerOutputDir => ArtifactsDirectory / "installer";

    AbsolutePath MsiOutputPath => InstallerOutputDir / "ViceSharp.msi";

    AbsolutePath WingetOutputDir => ArtifactsDirectory / "winget";

    /// <summary>
    /// Publish the ViceSharp.Avalonia desktop GUI as a NativeAOT,
    /// self-contained, trimmed, single-file Windows binary, then pack it
    /// into a per-machine MSI via the WixToolset.Sdk wixproj at
    /// installer/. The MSI lands at artifacts/installer/ViceSharp.msi
    /// with version <see cref="MsiVersion"/>. AOT publish collapses the
    /// previously-needed 391-file publish tree down to one native exe
    /// plus a handful of native dependencies (Skia, HarfBuzz, ANGLE),
    /// so the MSI is a fraction of the non-AOT footprint.
    /// </summary>
    Target PublishMsi => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            // Step 1: self-contained JIT publish of ViceSharp.Avalonia.
            // NativeAOT was disabled deliberately (BUG-THROTTLE-001): the tiered
            // JIT + dynamic PGO runs the dispatch-heavy cycle-accurate emulation
            // measurably faster than trimmed AOT (the per-cycle instruction stepping
            // cost that AOT could not optimise away), and a low-pause background GC
            // (set in the .csproj) keeps the worker from stalling. ReadyToRun gives
            // a fast cold start while the JIT still re-optimises hot paths at runtime.
            // No trimming: it would risk Avalonia/gRPC reflection without AOT's safety.
            Serilog.Log.Information("Publishing ViceSharp.Avalonia (self-contained JIT + ReadyToRun, {Rid}) -> {Out}",
                MsiRuntimeIdentifier, AvaloniaPublishDir);
            DotNetPublish(s => s
                .SetProject(AvaloniaProject)
                .SetConfiguration("Release")
                .SetRuntime(MsiRuntimeIdentifier)
                .SetSelfContained(true)
                .SetProperty("PublishAot", "false")
                .SetProperty("PublishTrimmed", "false")
                .SetProperty("PublishReadyToRun", "true")
                .SetProperty("PublishSingleFile", "false")
                // Strip managed PDBs / XML doc files at publish time.
                .SetProperty("DebugType", "none")
                .SetProperty("DebugSymbols", "false")
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
            string[] excludeGlobs = ["**/*.pdb", "**/*.xml", "**/*.dbg", "**/createdump.exe"];
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
            Serilog.Log.Information("Building installer wixproj with PublishedRoot={Root}", AvaloniaPublishDir);
            DotNetBuild(s => s
                .SetProjectFile(InstallerProject)
                .SetConfiguration("Release")
                .SetProperty("ProductVersion", MsiVersion)
                .SetProperty("PublishedRoot", AvaloniaPublishDir));

            // Step 3: copy the produced MSI to artifacts/installer/.
            var built = (RootDirectory / "installer" / "bin" / "x64" / "Release" / "en-US")
                .GlobFiles("*.msi").FirstOrDefault();
            if (built is null || !built.FileExists())
                built = (RootDirectory / "installer" / "bin" / "x64" / "Release")
                    .GlobFiles("*.msi").FirstOrDefault();
            if (built is null || !built.FileExists())
                throw new InvalidOperationException(
                    "WiX did not produce an MSI under installer/bin/x64/Release. Check the wixproj build log.");

            built.CopyToDirectory(InstallerOutputDir, ExistsPolicy.FileOverwrite);
            var dest = InstallerOutputDir / built.Name;
            if (dest != MsiOutputPath)
                dest.Rename(MsiOutputPath.Name, ExistsPolicy.FileOverwrite);

            Serilog.Log.Information("MSI ready: {Path} ({Size:N0} bytes)", MsiOutputPath, new System.IO.FileInfo(MsiOutputPath).Length);
        });

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
