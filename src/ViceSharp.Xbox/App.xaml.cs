// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): the UWP Application composition root.
//
// The ENTIRE file is #if HAS_UWP-guarded, so on the workload-free net10.0 fallback it
// compiles to nothing (no App type at all) and the fallback WinExe entry point stays the
// trivial Program.Main. On the real UWP build (ViceSharpXboxUwp=true) HAS_UWP is defined,
// the XAML compiler emits the other half of this partial (App.g.cs + the generated Main),
// and this is the live app entry point named by the appxmanifest.
#if HAS_UWP
namespace ViceSharp.Xbox;

using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Startup;
using ViceSharp.Xbox.Controls;
using ViceSharp.Xbox.Input;
using ViceSharp.Xbox.Platform;
using ViceSharp.Xbox.ViewModels;
using ViceSharp.Xbox.Views;

/// <summary>
/// The UWP-on-Xbox-console application. On launch it (1) points the emulator's data
/// resolution at the AppContainer-writable LocalFolder BEFORE any host build, (2) composes
/// the Kestrel-free in-process host, (3) constructs the seam adapters and the done
/// ViewModels, (4) puts up the always-present video surface with the pushable pages and the
/// overlays over it, and (5) starts the WinRT gamepad poll. Mouse mode is off app-wide.
/// </summary>
public sealed partial class App : Application
{
    private ConsoleHost? _host;
    private InProcessSessionFacade? _facade;
    private WinRtGamepadSource? _gamepad;
    private VideoSurfaceHost? _videoSurface;
    private string _sessionId = string.Empty;

    /// <summary>Creates the application, bootstraps data paths, and disables mouse mode.</summary>
    public App()
    {
        // Process-entry data-path bootstrap, BEFORE any host/ROM/keymap resolution.
        ConfigureDataPaths();

        InitializeComponent();

        // 10-foot UI: the pointer only appears when a control explicitly requests it.
        RequiresPointerMode = ApplicationRequiresPointerMode.WhenRequested;
    }

    /// <summary>The running application instance (the composition root the pages read from).</summary>
    public static App Instance => (App)Current;

    /// <summary>The explicit couch-UI navigation back-stack + overlay flags.</summary>
    public NavigationViewModel Navigation { get; } = new();

    /// <summary>The single input-context authority the gamepad and navigation both drive.</summary>
    public XboxInputContext InputContext { get; } = new();

    /// <summary>The emulator-session host facade (built once the session exists).</summary>
    public IEmulatorSessionFacade Facade =>
        _facade ?? throw new InvalidOperationException("The session facade is not built yet.");

    /// <summary>The pure ~50 Hz video-frame pull the surface renders.</summary>
    public VideoFramePullViewModel? VideoPull { get; private set; }

    /// <summary>The Home page ViewModel.</summary>
    public HomeViewModel Home { get; } = new();

    /// <summary>The Settings page ViewModel (built once the session exists).</summary>
    public XboxSettingsViewModel? SettingsVm { get; private set; }

    /// <summary>The Device-Setup page ViewModel (built once the session exists).</summary>
    public XboxDeviceSetupViewModel? DeviceSetupVm { get; private set; }

    /// <summary>The (read-only) input-mapping page ViewModel.</summary>
    public InputMappingViewModel InputMappingVm { get; } = new();

    /// <summary>The About page ViewModel.</summary>
    public AboutViewModel AboutVm { get; } = new();

    /// <summary>The on-screen virtual-keyboard ViewModel (built once the session exists).</summary>
    public VirtualKeyboardViewModel? KeyboardVm { get; private set; }

    /// <summary>The active emulator session id.</summary>
    public string SessionId => _sessionId;

    /// <summary>
    /// The console (XAudio2) live-audio backend produced by the Xbox audio wiring, or
    /// <c>null</c> when audio is disabled/headless. Exposed so the SID output path is wired
    /// to it during dev-PC iteration.
    /// </summary>
    public IAudioBackend? AudioBackend { get; private set; }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        BuildHostAndSession();

        // The root: an always-present in-emulator base view (the video surface), a
        // transparent Frame carrying the pushable pages above it, and the two overlays.
        var root = new Grid();

        var emulatorView = new EmulatorView();
        _videoSurface = emulatorView.SurfaceHost;
        root.Children.Add(emulatorView);

        var frame = new Frame { Background = null };
        root.Children.Add(frame);

        var keyboardOverlay = new VirtualKeyboardOverlay { Visibility = Visibility.Collapsed };
        var quickMenu = new QuickMenuOverlay { Visibility = Visibility.Collapsed };
        if (KeyboardVm is not null)
            keyboardOverlay.DataContext = KeyboardVm;
        root.Children.Add(keyboardOverlay);
        root.Children.Add(quickMenu);

        // Overlays are boolean flags on the navigation model, never stack entries.
        Navigation.StateChanged += (_, _) =>
        {
            keyboardOverlay.Visibility =
                Navigation.IsVirtualKeyboardOpen ? Visibility.Visible : Visibility.Collapsed;
            quickMenu.Visibility =
                Navigation.IsQuickMenuOpen ? Visibility.Visible : Visibility.Collapsed;
        };

        // Reconcile the single input context with the navigation stack (kept alive for the
        // app lifetime; disposed implicitly at process exit).
        _ = new InputContextObserver(Navigation, InputContext);

        Window.Current.Content = root;
        Window.Current.Activate();

        // Start the pure render pull and the per-frame gamepad poll.
        if (VideoPull is not null)
        {
            _videoSurface.Attach(VideoPull);
            _videoSurface.Start();
        }

        _gamepad?.Start();

        frame.Navigate(typeof(HomePage));
    }

    private void ConfigureDataPaths()
    {
        try
        {
            var folder = new LocalDataFolder(ApplicationData.Current.LocalFolder.Path);
            var packagedC64 = Path.Combine(
                Package.Current.InstalledLocation.Path, "Assets", "vice-data", "C64");
            XboxDataPathBridge.Configure(folder, packagedC64);
        }
        catch
        {
            // Design-time / un-packaged: leave the resolver on its default probe order.
        }
    }

    private void BuildHostAndSession()
    {
        // Produce the console XAudio2 backend (null when audio is disabled/headless). It is
        // decoupled from the SID ring and engages only when VICESHARP_AUDIO is enabled.
        AudioBackend = XboxAudioWiring.CreateBackend();

        var host = ConsoleHostComposition.BuildDefault();
        _host = host;

        var session = host.StartC64Session();
        _sessionId = session.SessionId;

        var facade = new InProcessSessionFacade(host, _sessionId);
        _facade = facade;

        // The facade implements both seam interfaces, so disambiguate the ctor overload.
        VideoPull = new VideoFramePullViewModel((IEmulatorSessionFacade)facade, _sessionId);
        SettingsVm = new XboxSettingsViewModel(facade, _sessionId);
        DeviceSetupVm = new XboxDeviceSetupViewModel(facade);
        KeyboardVm = host.GetKeyboardInput(_sessionId) is { } keyboard
            ? new VirtualKeyboardViewModel(keyboard)
            : null;

        var dispatcher = new AppCommandDispatcher(
            host.HostService,
            host.Snapshots,
            host.Settings,
            onExit: Exit);

        _gamepad = new WinRtGamepadSource(host, InputContext, dispatcher, _sessionId);

        // The Home page's Start/Resume intents drive the session lifecycle.
        Home.StartNewRequested += (_, _) => host.ResetCold(_sessionId);
        Home.ResumeRequested += (_, _) => host.Resume(_sessionId);
    }
}
#endif
