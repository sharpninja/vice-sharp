#if IOS
using global::Avalonia;
using global::Avalonia.iOS;
using global::Foundation;
using global::UIKit;
using SharedApp = global::ViceSharp.Avalonia.App;

namespace ViceSharp.Host.iOSHost;

/// <summary>
/// iOS entry-point delegate for the ViceSharp Avalonia host. Reuses the shared
/// <see cref="SharedApp"/> from <c>ViceSharp.Avalonia</c> so the mobile shell
/// does not duplicate UI code. PLATFORM-CROSS-001.
/// </summary>
[Register("AppDelegate")]
public sealed class AppDelegate : AvaloniaAppDelegate<SharedApp>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}

internal static class EntryPoint
{
    public static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
#endif
