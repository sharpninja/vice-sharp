#if ANDROID
using global::Android.App;
using global::Android.Content.PM;
using global::Android.OS;
using global::Avalonia;
using global::Avalonia.Android;
using SharedApp = global::ViceSharp.Avalonia.App;

namespace ViceSharp.Host.AndroidHost;

/// <summary>
/// Android entry-point activity for the ViceSharp Avalonia host. Reuses the
/// shared <see cref="SharedApp"/> from <c>ViceSharp.Avalonia</c> so the
/// mobile shell does not duplicate UI code. PLATFORM-CROSS-001.
/// </summary>
[Activity(
    Label = "ViceSharp",
    Theme = "@style/Theme.AppCompat.NoActionBar",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public sealed class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
    }
}

/// <summary>
/// Android application object that wires the Avalonia AppBuilder to the
/// shared ViceSharp <see cref="SharedApp"/>.
/// </summary>
[Application]
public sealed class MainApplication : AvaloniaAndroidApplication<SharedApp>
{
    public MainApplication(nint javaReference, global::Android.Runtime.JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
#endif
