using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Nuke.Common.Tools.DotNet;
using ViceSharp.Build.Packaging;

sealed partial class Build
{
    /// <summary>
    /// TR-BUILD-001 / TR-PLAT-001: create a local Arch package for yay without
    /// installing it or publishing to the AUR. The desktop payload includes
    /// both .NET and ASP.NET Core so the system SDK is not a runtime dependency.
    /// </summary>
    Target PackYay => _ => _
        .Description("Create a self-contained Release desktop package for Arch Linux/yay.")
        .Executes(() =>
        {
            if (!OperatingSystem.IsLinux())
                throw new InvalidOperationException("PackYay requires Arch Linux and makepkg. Run ./build.sh PackYay on the target machine.");

            var (architecture, runtime) = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => ("x86_64", "linux-x64"),
                Architecture.Arm64 => ("aarch64", "linux-arm64"),
                _ => throw new InvalidOperationException("PackYay supports x86_64 and aarch64 hosts only.")
            };
            var makepkg = FindOnPath("makepkg")
                ?? throw new InvalidOperationException("PackYay requires makepkg (Arch Linux base-devel). Run as a normal user, not root.");
            var version = string.IsNullOrWhiteSpace(PackageVersionOverride)
                ? HeadReleaseTagVersion() ?? MsiVersion
                : PackageVersionOverride;
            if (!Regex.IsMatch(version, @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
                throw new InvalidOperationException("PackYay requires a numeric X.Y.Z version. Use --package-version-override X.Y.Z.");

            var output = ArtifactsDirectory / "yay" / version / architecture;
            output.CreateDirectory();
            // Each invocation gets a fresh publish directory: stale files from a
            // previous RID/build must never enter the package. Retain it as evidence.
            var stage = output / "staging" / Guid.NewGuid().ToString("N");
            var payload = stage / "payload";
            payload.CreateDirectory();

            DotNetPublish(s => s
                .SetProject(AvaloniaProject)
                .SetConfiguration("Release")
                .SetRuntime(runtime)
                .SetSelfContained(true)
                .SetVersion(version)
                .SetOutput(payload)
                .SetProperty("PackAsTool", "false")
                .SetProperty("UseAppHost", "true")
                .SetProperty("PublishAot", "false")
                .SetProperty("PublishTrimmed", "false")
                .SetProperty("PublishSingleFile", "false")
                .SetProperty("DebugType", "none")
                .SetProperty("DebugSymbols", "false"));

            foreach (var required in new[]
            {
                "ViceSharp.Avalonia", "ViceSharp.Avalonia.dll", "ViceSharp.Avalonia.runtimeconfig.json",
                "libcoreclr.so", "libhostfxr.so", "Microsoft.AspNetCore.dll",
                "libSkiaSharp.so", "libHarfBuzzSharp.so"
            })
                Assert.FileExists(payload / required);

            var source = RootDirectory / "packaging" / "arch";
            foreach (var file in new[] { "vicesharp", "vicesharp.desktop" })
                File.Copy(source / file, stage / file);
            File.Copy(RootDirectory / "logo.svg", stage / "vicesharp.svg");
            File.Copy(RootDirectory / "COPYING", stage / "COPYING");
            File.Copy(RootDirectory / "THIRD_PARTY_NOTICES.md", stage / "THIRD_PARTY_NOTICES.md");

            var archiveName = $"vicesharp-{version}-{architecture}.tar.gz";
            var archive = output / archiveName;
            using (var file = File.Create(archive))
            using (var gzip = new GZipStream(file, CompressionLevel.Optimal))
                TarFile.CreateFromDirectory(stage, gzip, includeBaseDirectory: false);

            string checksum;
            using (var file = File.OpenRead(archive))
                checksum = Convert.ToHexStringLower(SHA256.HashData(file));
            var recipe = File.ReadAllText(source / "PKGBUILD.in")
                .Replace("@VERSION@", version)
                .Replace("@ARCH@", architecture)
                .Replace("@ARCHIVE@", archiveName)
                .Replace("@SHA256@", checksum)
                .Replace("\r\n", "\n");
            File.WriteAllText(output / "PKGBUILD", recipe);

            // Keep makepkg's user configuration from redirecting artifacts or
            // source extraction into shared directories, or changing the suffix.
            var environment = new Dictionary<string, string>
            {
                ["PKGDEST"] = output,
                ["SRCDEST"] = output,
                ["BUILDDIR"] = output / "makepkg" / Guid.NewGuid().ToString("N"),
                ["PKGEXT"] = ".pkg.tar.zst"
            };
            var srcinfo = ProcessTasks.StartProcess(makepkg, "--printsrcinfo", output,
                    environmentVariables: environment, logOutput: false)
                .AssertZeroExitCode().Output
                .Where(line => line.Type == OutputType.Std)
                .Select(line => line.Text);
            File.WriteAllText(output / ".SRCINFO", string.Join("\n", srcinfo) + "\n");
            // makepkg writes successful checksum results to stderr. Log its
            // output as progress and use the exit code to decide success.
            var packageBuild = ProcessTasks.StartProcess(makepkg, "--force --noconfirm", output,
                environmentVariables: environment, logOutput: false);
            packageBuild.WaitForExit();
            foreach (var line in packageBuild.Output)
                Serilog.Log.Information("{Message}", line.Text);
            packageBuild.AssertZeroExitCode();

            var package = output / $"vicesharp-{version}-1-{architecture}.pkg.tar.zst";
            Assert.FileExists(package);
            Serilog.Log.Information("Arch package: {Package}", package);
            Serilog.Log.Information("Install locally with: yay -U \"{Package}\"", package);
        });

    /// <summary>
    /// TR-BUILD-001 / TR-PLAT-001: publish a self-contained desktop release and
    /// activate it only in the current user's home. Older releases and data remain intact.
    /// </summary>
    Target DeployUser => _ => _
        .Description("Build and install a self-contained Linux desktop release in user space only.")
        .Executes(() =>
        {
            var paths = UserLocalDeployment.ForCurrentUser();
            var (architecture, runtime) = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => ("x86_64", "linux-x64"),
                Architecture.Arm64 => ("aarch64", "linux-arm64"),
                _ => throw new InvalidOperationException("DeployUser supports x86_64 and aarch64 hosts only.")
            };
            var version = string.IsNullOrWhiteSpace(PackageVersionOverride)
                ? HeadReleaseTagVersion() ?? MsiVersion : PackageVersionOverride;
            if (!Regex.IsMatch(version, @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
                throw new InvalidOperationException("DeployUser requires a numeric X.Y.Z version. Use --package-version-override X.Y.Z.");
            var payload = ArtifactsDirectory / "user" / version / architecture / "staging"
                / Guid.NewGuid().ToString("N") / "payload";
            payload.CreateDirectory();
            DotNetPublish(s => s
                .SetProject(AvaloniaProject)
                .SetConfiguration("Release")
                .SetRuntime(runtime)
                .SetSelfContained(true)
                .SetVersion(version)
                .SetOutput(payload)
                .SetProperty("PackAsTool", "false")
                .SetProperty("UseAppHost", "true")
                .SetProperty("PublishAot", "false")
                .SetProperty("PublishTrimmed", "false")
                .SetProperty("PublishSingleFile", "false")
                .SetProperty("DebugType", "none")
                .SetProperty("DebugSymbols", "false"));
            File.Copy(RootDirectory / "COPYING", payload / "COPYING");
            File.Copy(RootDirectory / "THIRD_PARTY_NOTICES.md", payload / "THIRD_PARTY_NOTICES.md");
            var installed = UserLocalDeployment.Install(payload, RootDirectory / "logo.svg", version, paths);
            Serilog.Log.Information("User release: {Release}", installed.ReleasePath);
            Serilog.Log.Information("Launcher: {Launcher}; desktop entry: {Desktop}", paths.Launcher, paths.Desktop);
            Serilog.Log.Information("Keep ~/.local/bin before /usr/bin on PATH to select this user installation.");
        });

    /// <summary>Compatibility alias: deployments now always use user space, without a package manager.</summary>
    Target DeployYay => _ => _
        .Description("Compatibility alias for DeployUser; installs only in the current user's home.")
        .DependsOn(DeployUser);
}
