using System;
using Avalonia;
using AvaloniaApp = ViceSharp.Avalonia.App;

namespace ViceSharp.Host.MacOS;

/// <summary>
/// PLATFORM-CROSS-001: macOS host shell entrypoint.
///
/// This is intentionally a thin wrapper. It reuses the existing
/// ViceSharp.Avalonia.App and MainWindow instead of duplicating UI, and
/// drives the Avalonia classic desktop lifetime. Bundle metadata is
/// declared via Info.plist and macOS-specific csproj properties.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Configures the Avalonia AppBuilder. Kept public + static so the
    /// Avalonia previewer (and integration tests) can resolve it.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<AvaloniaApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
