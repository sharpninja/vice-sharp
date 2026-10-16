namespace ViceSharp.TestHarness.HostShells;

using System;
using System.IO;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): the realized UWP XAML head
/// (src/ViceSharp.Xbox). S34 adds the App composition root, the six couch-UI pages,
/// the always-present video surface + overlays, the TV focus visuals, and the platform
/// adapters that wire the done ViewModels/host to WinRT gamepad, Win2D video, and
/// XAudio2 audio.
///
/// This is a Tier-H (headless / CI) file-shape guard: it runs on any net10.0 agent WITHOUT
/// the windows-app / UWP workload, because it only reads the head's project + XAML text
/// off disk. It is a PLAIN [Fact] with [Trait("Category","Xbox")] - NO [ViceFact] and NO
/// Assert.Skip - so it always executes. It EXTENDS <see cref="XboxUwpHeadTests"/> (S4)
/// without disturbing it.
///
/// FR: FR-Host-UI-Boundary / FR-XBOXUI-*. TR: TR-MVVM-001 (the head consumes the portable
/// ViewModels + Input libraries; it never re-implements their logic).
/// Acceptance:
///   TEST-XBOXHEAD-034a: the head csproj now project-references
///     ViceSharp.Xbox.ViewModels and ViceSharp.Xbox.Input (in addition to the five core
///     references), and still carries the S4 shape (conditional UWP TFM, UseUwp, PublishAot,
///     DisableRuntimeMarshalling).
///   TEST-XBOXHEAD-034b: the head csproj keeps the reference-ban (no Host, desktop UI,
///     Protocol, Monitor project reference; no gRPC / web-host / desktop-UI package).
///   TEST-XBOXHEAD-034c: a head XAML file (FocusVisuals.xaml or App.xaml) disables mouse
///     mode app-wide via RequiresPointer="Never".
///   TEST-XBOXHEAD-034d: App.xaml plus the six couch-UI page files exist.
/// </summary>
public sealed class XboxUwpHeadShellTests
{
    [Fact]
    [Trait("Category", "Xbox")]
    public void Csproj_ReferencesViewModelsAndInput_AndKeepsUwpShape()
    {
        var csproj = ReadCsproj();

        // TEST-XBOXHEAD-034a: the two portable UI libraries are now wired in.
        Assert.Contains("ViceSharp.Xbox.ViewModels.csproj", csproj);
        Assert.Contains("ViceSharp.Xbox.Input.csproj", csproj);

        // The five core references remain.
        Assert.Contains("ViceSharp.Abstractions.csproj", csproj);
        Assert.Contains("ViceSharp.Core.csproj", csproj);
        Assert.Contains("ViceSharp.Chips.csproj", csproj);
        Assert.Contains("ViceSharp.Architectures.csproj", csproj);
        Assert.Contains("ViceSharp.Host.InProcess.csproj", csproj);

        // The S4 UWP shape is intact.
        Assert.Contains("net10.0-windows10.0.26100.0", csproj);
        Assert.Contains("UseUwp", csproj);
        Assert.Contains("PublishAot", csproj);
        Assert.Contains("DisableRuntimeMarshalling", csproj);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void Csproj_KeepsReferenceBan()
    {
        var csproj = ReadCsproj();

        // TEST-XBOXHEAD-034b: no direct desktop-host / desktop-UI / wire-protocol / monitor
        // project reference (Protocol + Monitor arrive transitively through Host.InProcess).
        Assert.DoesNotContain("ViceSharp.Host.csproj", csproj);
        Assert.DoesNotContain("ViceSharp.Avalonia.csproj", csproj);
        Assert.DoesNotContain("ViceSharp.Protocol.csproj", csproj);
        Assert.DoesNotContain("ViceSharp.Monitor.csproj", csproj);

        // No gRPC / web-host / desktop-UI package references.
        Assert.DoesNotContain("Grpc.", csproj);
        Assert.DoesNotContain("Microsoft.AspNetCore", csproj);
        Assert.DoesNotContain("Avalonia.", csproj);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void FocusVisuals_DisablesMouseModeAppWide()
    {
        var focusVisuals = Path.Combine(HeadDirectory, "Styles", "FocusVisuals.xaml");
        var appXaml = Path.Combine(HeadDirectory, "App.xaml");

        var text = string.Empty;
        if (File.Exists(focusVisuals))
            text += File.ReadAllText(focusVisuals);
        if (File.Exists(appXaml))
            text += File.ReadAllText(appXaml);

        // TEST-XBOXHEAD-034c: mouse mode is off app-wide (10-foot UI, controller only).
        Assert.Contains("RequiresPointer=\"Never\"", text);
    }

    [Fact]
    [Trait("Category", "Xbox")]
    public void AppAndSixPages_Exist()
    {
        // TEST-XBOXHEAD-034d: the App composition root exists.
        Assert.True(
            File.Exists(Path.Combine(HeadDirectory, "App.xaml")),
            "Expected the UWP App.xaml composition root.");

        // The six couch-UI pages (five pushable pages + the always-present in-emulator view).
        string[] pages =
        [
            "HomePage.xaml",
            "SettingsPage.xaml",
            "DeviceSetupPage.xaml",
            "InputMappingPage.xaml",
            "AboutPage.xaml",
            "EmulatorView.xaml",
        ];

        var viewsDirectory = Path.Combine(HeadDirectory, "Views");
        foreach (var page in pages)
        {
            var path = Path.Combine(viewsDirectory, page);
            Assert.True(File.Exists(path), $"Expected couch-UI page '{path}'.");
        }
    }

    private static string ReadCsproj()
    {
        var csprojPath = Path.Combine(HeadDirectory, "ViceSharp.Xbox.csproj");

        Assert.True(
            File.Exists(csprojPath),
            $"Expected Xbox UWP head csproj at '{csprojPath}'.");

        return File.ReadAllText(csprojPath);
    }

    private static string HeadDirectory =>
        Path.Combine(RepoRoot, "src", "ViceSharp.Xbox");

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
