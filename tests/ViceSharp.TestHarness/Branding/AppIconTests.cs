namespace ViceSharp.TestHarness.Branding;

using System.IO;
using FluentAssertions;
using Xunit;

/// <summary>
/// BRANDING-ICON-001: the approved ViceSharp logo (logo.svg, the modernized
/// Commodore chicken-head C mark) must be applied as the application icon on
/// every shipping head: the Avalonia desktop GUI (Windows PE icon + runtime
/// window icon), the macOS bundle, the iOS app, the Android launcher, and the
/// Windows MSI (Add/Remove Programs + Start-menu shortcut).
///
/// These are file/wiring assertions (raw reads) rather than reflection: the
/// iOS / Android assets only compile under their platform workloads, which are
/// not present on the net10.0 test runtime, so we verify the generated asset
/// files exist on disk and that each project references them correctly.
///
/// Assets are generated from logo.svg by tools/generate-icons.py; re-run that
/// script after any logo change to refresh every size in one pass.
/// </summary>
public sealed class AppIconTests
{
    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
            {
                dir = dir.Parent;
            }

            dir.Should().NotBeNull("the test harness must locate ViceSharp.slnx by walking up from AppContext.BaseDirectory");
            return dir!.FullName;
        }
    }

    private static string PathUnder(params string[] parts) => Path.Combine([RepoRoot, .. parts]);

    private static void AssertFileNonEmpty(string path, string because)
    {
        File.Exists(path).Should().BeTrue(because + $" (expected at {path})");
        new FileInfo(path).Length.Should().BeGreaterThan(64, $"{path} must be a real asset, not an empty placeholder");
    }

    [Fact]
    public void ApprovedLogo_SourceSvg_ExistsAtRepoRoot()
    {
        var svg = PathUnder("logo.svg");
        AssertFileNonEmpty(svg, "the approved logo.svg is the single source every app icon is generated from");
        File.ReadAllText(svg).Should().Contain("<svg", "logo.svg must be a real SVG document");
    }

    [Fact]
    public void Windows_Avalonia_HasIcoEmbedAndRuntimeWindowIcon()
    {
        AssertFileNonEmpty(
            PathUnder("src", "ViceSharp.Avalonia", "Assets", "vicesharp.ico"),
            "the Windows PE / Explorer icon (.ico) embedded via <ApplicationIcon>");
        AssertFileNonEmpty(
            PathUnder("src", "ViceSharp.Avalonia", "Assets", "vicesharp-icon.png"),
            "the Avalonia runtime WindowIcon (PNG, loaded as an AvaloniaResource)");

        var csproj = File.ReadAllText(PathUnder("src", "ViceSharp.Avalonia", "ViceSharp.Avalonia.csproj"));
        csproj.Should().Contain("<ApplicationIcon>", "the Windows exe must embed the icon via <ApplicationIcon>");
        csproj.Should().Contain("vicesharp.ico", "the <ApplicationIcon> must point at the generated .ico");
        csproj.Should().Contain("AvaloniaResource", "the window-icon PNG must be packed as an AvaloniaResource");

        var window = File.ReadAllText(PathUnder("src", "ViceSharp.Avalonia", "MainWindow.axaml"));
        window.Should().Contain("Icon=", "the main window must set a runtime icon");
        window.Should().Contain("vicesharp-icon.png", "the window Icon must reference the generated PNG");
    }

    [Fact]
    public void MacOS_HasIcnsWiredInPlistAndProject()
    {
        AssertFileNonEmpty(
            PathUnder("src", "ViceSharp.Host.MacOS", "Assets", "AppIcon.icns"),
            "the macOS bundle icon (.icns)");

        var plist = File.ReadAllText(PathUnder("src", "ViceSharp.Host.MacOS", "Info.plist"));
        plist.Should().Contain("CFBundleIconFile", "Info.plist must declare the bundle icon file");
        plist.Should().Contain("AppIcon", "CFBundleIconFile must name the AppIcon icns");

        var csproj = File.ReadAllText(PathUnder("src", "ViceSharp.Host.MacOS", "ViceSharp.Host.MacOS.csproj"));
        csproj.Should().Contain("AppIcon.icns", "the macOS project must include the icns so it lands in the bundle");
    }

    [Fact]
    public void iOS_HasAppIconSetWiredInProject()
    {
        AssertFileNonEmpty(
            PathUnder("src", "ViceSharp.Host.iOS", "Assets.xcassets", "AppIcon.appiconset", "Contents.json"),
            "the iOS asset-catalog AppIcon set manifest");
        AssertFileNonEmpty(
            PathUnder("src", "ViceSharp.Host.iOS", "Assets.xcassets", "AppIcon.appiconset", "AppIcon-1024.png"),
            "the iOS 1024px marketing icon (must be opaque: iOS rejects alpha)");

        var csproj = File.ReadAllText(PathUnder("src", "ViceSharp.Host.iOS", "ViceSharp.Host.iOS.csproj"));
        csproj.Should().Contain("Assets.xcassets", "the iOS project must include the asset catalog under the iOS TFM");
    }

    [Theory]
    [InlineData("mipmap-mdpi")]
    [InlineData("mipmap-hdpi")]
    [InlineData("mipmap-xhdpi")]
    [InlineData("mipmap-xxhdpi")]
    [InlineData("mipmap-xxxhdpi")]
    public void Android_HasLauncherIconForEachDensity(string density)
    {
        AssertFileNonEmpty(
            PathUnder("src", "ViceSharp.Host.Android", "Resources", density, "ic_launcher.png"),
            $"the Android legacy launcher icon for {density}");
        AssertFileNonEmpty(
            PathUnder("src", "ViceSharp.Host.Android", "Resources", density, "ic_launcher_foreground.png"),
            $"the Android adaptive-icon foreground for {density}");
    }

    [Fact]
    public void Android_HasAdaptiveIconAndIsWiredInActivity()
    {
        AssertFileNonEmpty(
            PathUnder("src", "ViceSharp.Host.Android", "Resources", "mipmap-anydpi-v26", "ic_launcher.xml"),
            "the Android adaptive-icon (API 26+) descriptor");
        AssertFileNonEmpty(
            PathUnder("src", "ViceSharp.Host.Android", "Resources", "values", "colors.xml"),
            "the adaptive-icon background colour resource");

        var activity = File.ReadAllText(PathUnder("src", "ViceSharp.Host.Android", "MainActivity.cs"));
        activity.Should().Contain("@mipmap/ic_launcher", "the [Application] attribute must point android:icon at the launcher mipmap");
    }

    [Fact]
    public void Installer_Msi_ReferencesProductIcon()
    {
        var wxs = File.ReadAllText(PathUnder("installer", "ViceSharp.wxs"));
        wxs.Should().Contain("ARPPRODUCTICON", "the MSI must show the product icon in Add/Remove Programs");
        wxs.Should().Contain("ProductIcon.ico", "the MSI Icon element id (with .ico extension) must be defined and referenced");
    }
}
