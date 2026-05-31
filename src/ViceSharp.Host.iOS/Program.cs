#if !IOS
using global::Avalonia;
using SharedApp = global::ViceSharp.Avalonia.App;

namespace ViceSharp.Host.iOSHost;

/// <summary>
/// Desktop fallback entry-point used when the iOS workload is not present on
/// the build machine and the project is built against <c>net10.0</c>. The real
/// shell uses <c>AppDelegate</c>; this stub exists so the solution remains
/// buildable everywhere.
/// </summary>
internal static class Program
{
    [System.STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<SharedApp>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
#endif
