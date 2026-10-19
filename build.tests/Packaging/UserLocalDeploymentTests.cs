namespace ViceSharp.Build.Tests.Packaging;

using System.Diagnostics;
using ViceSharp.Build.Packaging;
using Xunit;

/// <summary>
/// TR-BUILD-001 / TR-PLAT-001: deployments only activate retained, complete user-owned releases.
/// Linux desktop deployment contract: every case carries the Host=Linux trait, and the BuildTest target
/// excludes that trait on other hosts by filter (TR-BUILD-001 ac-12), so no case is ever skipped.
/// </summary>
[Trait("Host", "Linux")]
public sealed class UserLocalDeploymentTests
{
    [Fact]
    public void Install_RejectsNonExecutableApphostBeforeReplacingExistingRelease()
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException();
        var fixture = Fixture.Create();
        var paths = UserLocalDeployment.CreatePaths(fixture.Home, null, false);
        var first = UserLocalDeployment.Install(fixture.Payload, fixture.Icon, "1.2.3", paths);
        string apphost = Path.Combine(fixture.Payload, "ViceSharp.Avalonia");
        File.SetUnixFileMode(apphost, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        Assert.Throws<InvalidOperationException>(() => UserLocalDeployment.Install(fixture.Payload, fixture.Icon, "1.2.4", paths));
        Assert.Equal(first.ReleasePath, new DirectoryInfo(paths.Current).ResolveLinkTarget(true)!.FullName);
    }

    [Fact]
    public async Task Install_PreservesDataAndForwardsSpecialPathsAndArguments()
    {
        var fixture = Fixture.Create("home=space ' $cash % quote\" tick` slash\\");
        var paths = UserLocalDeployment.CreatePaths(fixture.Home, null, isRoot: false);
        string data = Path.Combine(paths.DataHome, "ViceSharp", "roms", "preserved.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(data)!);
        File.WriteAllText(data, "existing user data");

        var result = UserLocalDeployment.Install(fixture.Payload, fixture.Icon, "1.2.3", paths);

        Assert.StartsWith(paths.Releases + Path.DirectorySeparatorChar, result.ReleasePath, StringComparison.Ordinal);
        Assert.Equal(result.ReleasePath, new DirectoryInfo(paths.Current).ResolveLinkTarget(true)!.FullName);
        Assert.Equal("existing user data", File.ReadAllText(data));
        Assert.Equal("keymap", File.ReadAllText(Path.Combine(result.ReleasePath, "data", "C64", "synthetic.vkm")));
        string[] arguments = ["space argument", "quote'\" $HOME %f `literal` \\", "--flag"];
        var launched = await Launch(paths.Launcher, arguments, fixture.Root);
        Assert.Equal(new[] { "first" }.Concat(arguments), launched);

        string desktop = File.ReadAllText(paths.Desktop);
        Assert.Contains("Exec=/bin/sh \"", desktop, StringComparison.Ordinal);
        Assert.Contains("%%", desktop, StringComparison.Ordinal);
        Assert.Contains("\\\\$cash", desktop, StringComparison.Ordinal);
        Assert.DoesNotContain("Exec=vicesharp", desktop, StringComparison.Ordinal);
        if (Environment.GetEnvironmentVariable("VICESHARP_DEPLOY_TEST_DESKTOP_TOOLS") == "1")
        {
            await Run("desktop-file-validate", [paths.Desktop], fixture.Root);
            string game = Path.Combine(fixture.Root, "game space '$cash%\"`.t64");
            File.WriteAllText(game, "synthetic argument only");
            var desktopLaunch = await Launch("gio", ["launch", paths.Desktop, game], fixture.Root);
            Assert.Equal(new[] { "first", game }, desktopLaunch);
        }
    }

    [Fact]
    public async Task Update_RetainsOldReleaseAndPublishesNewCurrent()
    {
        var fixture = Fixture.Create();
        var paths = UserLocalDeployment.CreatePaths(fixture.Home, Path.Combine(fixture.Home, "custom data"), false);
        var first = UserLocalDeployment.Install(fixture.Payload, fixture.Icon, "1.2.3", paths);
        fixture.SetPayloadGeneration("second");

        var second = UserLocalDeployment.Install(fixture.Payload, fixture.Icon, "1.2.3", paths);

        Assert.NotEqual(first.ReleasePath, second.ReleasePath);
        Assert.True(Directory.Exists(first.ReleasePath));
        Assert.Equal(new[] { "first" }, await Launch(Path.Combine(first.ReleasePath, "ViceSharp.Avalonia"), [], fixture.Root));
        Assert.Equal(new[] { "second" }, await Launch(paths.Launcher, [], fixture.Root));
        Assert.Equal(second.ReleasePath, new DirectoryInfo(paths.Current).ResolveLinkTarget(true)!.FullName);
    }

    [Fact]
    public async Task LateFailure_RestoresEntrypointsAndKeepsPreviousReleaseUsable()
    {
        var fixture = Fixture.Create();
        var paths = UserLocalDeployment.CreatePaths(fixture.Home, null, false);
        var first = UserLocalDeployment.Install(fixture.Payload, fixture.Icon, "1.2.3", paths);
        string launcher = File.ReadAllText(paths.Launcher);
        string desktop = File.ReadAllText(paths.Desktop);
        fixture.SetPayloadGeneration("must not activate");

        Assert.Throws<IOException>(() => UserLocalDeployment.Install(fixture.Payload, fixture.Icon, "1.2.4", paths,
            () => throw new IOException("simulated late publication failure")));

        Assert.Equal(first.ReleasePath, new DirectoryInfo(paths.Current).ResolveLinkTarget(true)!.FullName);
        Assert.Equal(launcher, File.ReadAllText(paths.Launcher));
        Assert.Equal(desktop, File.ReadAllText(paths.Desktop));
        Assert.Equal(new[] { "first" }, await Launch(paths.Launcher, [], fixture.Root));
        Assert.Equal(2, Directory.GetDirectories(paths.Releases).Length);
    }

    [Theory]
    [InlineData("root")]
    [InlineData("relative")]
    [InlineData("outside")]
    [InlineData("root-home")]
    public void CreatePaths_RejectsUnsafeDestinations(string kind)
    {
        var fixture = Fixture.Create();
        Assert.ThrowsAny<ArgumentException>(() => UserLocalDeployment.CreatePaths(
            kind == "root-home" ? "/" : fixture.Home,
            kind switch { "relative" => "relative/data", "outside" => Path.Combine(fixture.Root, "outside"), _ => null },
            isRoot: kind == "root"));
        Assert.False(Directory.Exists(Path.Combine(fixture.Home, ".local")));
    }

    [Theory]
    [InlineData("destination-link")]
    [InlineData("payload-link")]
    [InlineData("unmanaged-launcher")]
    [InlineData("incomplete-payload")]
    [InlineData("unsafe-version")]
    public void Install_RejectsUnsafeInputsBeforeActivation(string kind)
    {
        var fixture = Fixture.Create();
        var paths = UserLocalDeployment.CreatePaths(fixture.Home, null, false);
        if (kind == "destination-link")
            Directory.CreateSymbolicLink(Path.Combine(fixture.Home, ".local"), fixture.Root);
        if (kind == "payload-link")
            File.CreateSymbolicLink(Path.Combine(fixture.Payload, "external"), fixture.Icon);
        if (kind == "unmanaged-launcher")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(paths.Launcher)!);
            File.WriteAllText(paths.Launcher, "user-created launcher");
        }
        if (kind == "incomplete-payload")
            File.Move(Path.Combine(fixture.Payload, "libcoreclr.so"), Path.Combine(fixture.Root, "retained-libcoreclr.so"));

        Assert.Throws<InvalidOperationException>(() => UserLocalDeployment.Install(fixture.Payload, fixture.Icon,
            kind == "unsafe-version" ? "../bad" : "1.2.3", paths));

        Assert.Null(new DirectoryInfo(paths.Current).LinkTarget);
        if (kind == "unmanaged-launcher")
            Assert.Equal("user-created launcher", File.ReadAllText(paths.Launcher));
    }

    private static async Task<string[]> Launch(string executable, string[] arguments, string workingDirectory)
    {
        string receipt = Path.Combine(workingDirectory, "launch-" + Guid.NewGuid().ToString("N") + ".txt");
        await Run(executable, arguments, workingDirectory, receipt);
        var deadline = Stopwatch.StartNew();
        while (!File.Exists(receipt) && deadline.Elapsed < TimeSpan.FromSeconds(5))
            await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.True(File.Exists(receipt), "The fake payload must actually launch and write its arguments.");
        return await File.ReadAllLinesAsync(receipt, TestContext.Current.CancellationToken);
    }

    private static async Task Run(string executable, string[] arguments, string workingDirectory, string? receipt = null)
    {
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
            start.ArgumentList.Add(argument);
        if (receipt is not null)
            start.Environment["VICESHARP_DEPLOY_TEST_RECEIPT"] = receipt;
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(process.ExitCode == 0, $"{executable} exited {process.ExitCode}: {await stdout} {await stderr}");
    }

    private sealed record Fixture(string Root, string Home, string Payload, string Icon)
    {
        internal static Fixture Create(string homeName = "home")
        {
            string root = Directory.CreateTempSubdirectory("vicesharp-user-deploy-").FullName;
            string home = Directory.CreateDirectory(Path.Combine(root, homeName)).FullName;
            string payload = Directory.CreateDirectory(Path.Combine(root, "payload")).FullName;
            foreach (string file in new[] { "ViceSharp.Avalonia.dll", "ViceSharp.Avalonia.runtimeconfig.json",
                         "libcoreclr.so", "libhostfxr.so", "Microsoft.AspNetCore.dll", "libSkiaSharp.so", "libHarfBuzzSharp.so" })
                File.WriteAllText(Path.Combine(payload, file), "synthetic runtime fixture");
            Directory.CreateDirectory(Path.Combine(payload, "data", "C64"));
            File.WriteAllText(Path.Combine(payload, "data", "C64", "synthetic.vkm"), "keymap");
            string icon = Path.Combine(root, "logo.svg");
            File.WriteAllText(icon, "<svg xmlns=\"http://www.w3.org/2000/svg\"/>");
            var fixture = new Fixture(root, home, payload, icon);
            fixture.SetPayloadGeneration("first");
            return fixture;
        }

        internal void SetPayloadGeneration(string generation)
        {
            if (!OperatingSystem.IsLinux())
                throw new PlatformNotSupportedException("The executable fixture requires Linux.");
            string file = Path.Combine(Payload, "ViceSharp.Avalonia");
            File.WriteAllText(file, $"#!/bin/sh\nprintf '%s\\n' '{generation}' \"$@\" > \"${{VICESHARP_DEPLOY_TEST_RECEIPT:?}}.partial\"\nmv -- \"${{VICESHARP_DEPLOY_TEST_RECEIPT}}.partial\" \"${{VICESHARP_DEPLOY_TEST_RECEIPT}}\"\n");
            File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }
}
