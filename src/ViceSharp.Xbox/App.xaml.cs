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
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Startup;
using ViceSharp.Protocol;
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
    private EmulatorView? _emulatorView;
    private VirtualKeyboardOverlay? _keyboardOverlay;

    // FIX-XKBDINPUT-001: the machine keyboard seam physical keys inject through, plus the
    // currently held (injected-down) key names so ups always pair with downs and the menu
    // can force-release everything (no stuck C64 keys).
    private IMachineKeyboardInput? _machineKeyboard;
    private readonly System.Collections.Generic.HashSet<string> _pressedKeys = new(StringComparer.Ordinal);

    // FEAT-XDEFAULTCART-001: the canonical vice.ini settings (Core INI reader/writer) the
    // default-cartridge policy and user media selections persist through.
    private ViceSharp.Core.Configuration.ViceSettings? _viceSettings;
    private string _sessionId = string.Empty;
    private string _c64Directory = string.Empty;
    private Frame? _frame;
    private bool _bootStarted;

    /// <summary>Creates the application, bootstraps data paths, and disables mouse mode.</summary>
    public App()
    {
        // Diagnostics FIRST (PLAN-XBOXUWP, area diagnostics): the deployed head boots to a black
        // screen and exits with code 1 ~13s after launch (an unhandled exception on a
        // background/timer thread). Debug.WriteLine is invisible in a packaged UWP app, so stand
        // up the ILogger factory + a readable LocalState\vicesharp.log BEFORE anything that can
        // throw, and register the global unhandled-exception handlers so the crashing exception is
        // captured to the log instead of only dying with code 1.
        ConfigureLogging();
        RegisterGlobalExceptionHandlers();

        // Process-entry data-path bootstrap, BEFORE any host/ROM/keymap resolution.
        ConfigureDataPaths();

        InitializeComponent();

        // 10-foot UI: the pointer only appears when a control explicitly requests it.
        RequiresPointerMode = ApplicationRequiresPointerMode.WhenRequested;
    }

    /// <summary>The running application instance (the composition root the pages read from).</summary>
    public static App Instance => (App)Current;

    /// <summary>
    /// The process-wide logger factory built at construction. It fans out to the Debug and
    /// Console sinks and (when the AppContainer LocalFolder is available) a
    /// <see cref="ViceSharp.Xbox.Logging.FileLoggerProvider"/> that persists a readable
    /// <c>LocalState\vicesharp.log</c>. <c>null</c> only if factory construction itself failed.
    /// </summary>
    public static ILoggerFactory? LoggerFactory { get; private set; }

    /// <summary>
    /// Creates a category logger from <see cref="LoggerFactory"/>, or a null logger when the
    /// factory was not built. Never returns <c>null</c>, so callers (including
    /// <see cref="ViceSharp.Xbox.Controls.VideoSurfaceHost"/>) can log unconditionally.
    /// </summary>
    /// <param name="category">The logger category (e.g. "App", "Video").</param>
    /// <returns>An <see cref="ILogger"/> that is always safe to call.</returns>
    internal static ILogger CreateLogger(string category) =>
        (LoggerFactory ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)
            .CreateLogger(category);

    private static void ConfigureLogging()
    {
        try
        {
            // LocalFolder is AppContainer-writable in a packaged UWP app but throws when the head
            // is run un-packaged (design-time). Guard it: skip the file sink rather than fail.
            string? logPath = null;
            try
            {
                logPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "vicesharp.log");
            }
            catch
            {
                logPath = null;
            }

            LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(b =>
            {
                b.SetMinimumLevel(LogLevel.Trace);
                b.AddDebug();
                b.AddConsole();
                if (logPath is not null)
                    b.AddProvider(new ViceSharp.Xbox.Logging.FileLoggerProvider(logPath));
            });

            CreateLogger("App").LogInformation("App starting; log file: {LogPath}", logPath ?? "(none)");
        }
        catch (Exception ex)
        {
            // Logging must never take down the app it is diagnosing.
            System.Diagnostics.Debug.WriteLine($"[ViceSharp.Xbox] logging init failed: {ex}");
        }
    }

    private void RegisterGlobalExceptionHandlers()
    {
        try
        {
            // UWP-level unhandled exceptions (UI thread + most framework paths). e.Handled=true
            // keeps the process alive so the log survives and the user can read the crash cause.
            this.UnhandledException += (s, e) =>
            {
                CreateLogger("App").LogError(e.Exception, "UWP UnhandledException: {Message}", e.Message);
                e.Handled = true;
            };

            // CLR-level unhandled exceptions on background/threadpool threads (the ~13s code-1
            // path). Cannot be cancelled; log it (critical) before the runtime tears down.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                CreateLogger("App").LogCritical(
                    e.ExceptionObject as Exception,
                    "AppDomain UnhandledException (terminating={Terminating})",
                    e.IsTerminating);

            // Faulted Tasks whose exception was never observed (a common silent-crash source).
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                CreateLogger("App").LogError(e.Exception, "UnobservedTaskException");
                e.SetObserved();
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ViceSharp.Xbox] exception-handler registration failed: {ex}");
        }
    }

    /// <summary>The explicit couch-UI navigation back-stack + overlay flags.</summary>
    public NavigationViewModel Navigation { get; } = new();

    /// <summary>
    /// FEAT-XPERFHUD-001: the letterbox performance HUD's portable rate math. The video
    /// surface records samples into it per render tick; its machine facts (nominal clock,
    /// standard, pixel aspect) are set by <see cref="ApplyVideoAspectForCurrentSession"/>.
    /// </summary>
    internal VideoPerfStatsViewModel PerfStats { get; } = new();

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

    /// <summary>The first-run ROM-provisioning ViewModel (built only when boot is blocked at launch).</summary>
    public XboxRomProvisioningViewModel? ProvisioningVm { get; private set; }

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
        var log = CreateLogger("App");
        log.LogInformation(
            "OnLaunched entry; c64Directory={C64Directory}",
            string.IsNullOrEmpty(_c64Directory) ? "(none)" : _c64Directory);

        try
        {
            // First-run gate: only build the host + C64 session when the ROMs are already present.
            // Otherwise defer (no "c64 not registered" throw) and show the provisioning page; the
            // post-download OnProvisioningChanged rebuilds the host once the ROMs land.
            var assessment = string.IsNullOrEmpty(_c64Directory)
                ? null
                : new RomProvisionEvaluator().Evaluate(_c64Directory, RomProfile.Standard);

            if (assessment is null)
                log.LogInformation("ROM assessment skipped (no c64 directory resolved)");
            else
                log.LogInformation(
                    "ROM assessment: State={State} IsBootBlocked={IsBootBlocked}",
                    assessment.State, assessment.IsBootBlocked);

            if (assessment is not null && !assessment.IsBootBlocked)
            {
                log.LogInformation("Boot not blocked: building host and session at launch");
                BuildHostAndSession();
            }
            else
            {
                log.LogInformation("BuildHostAndSession NOT run at launch (deferred to provisioning gate)");
            }
        }
        catch (Exception ex)
        {
            // Defensive: OnLaunched has no ambient handler, so any throw between here and
            // Window.Activate would fail-fast (0xC0000409) with NO window. Degrade to the shell.
            log.LogError(ex, "launch host build failed");
        }

        // The root: an always-present in-emulator base view (the video surface), a
        // transparent Frame carrying the pushable pages above it, and the two overlays.
        // PLAN-XKEYBOARD-001 K2: two rows: the emulator lives in the star row and the
        // virtual keyboard DOCKS in the bottom Auto row, so showing the keyboard SHRINKS
        // the emulator instead of occluding it (the Auto row collapses when hidden).
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Catch shell-menu keyboard navigation even when a focused child already handled the
        // key (handledEventsToo: true). ESC toggles the menu; WASD/arrows drive focus while the
        // menu is open. Tab and Enter/Space are left to native XAML focus handling.
        root.AddHandler(
            Windows.UI.Xaml.UIElement.KeyDownEvent,
            new Windows.UI.Xaml.Input.KeyEventHandler(OnRootKeyDown),
            handledEventsToo: true);

        // FIX-XKBDINPUT-001: matching KeyUp handler so injected C64 keys are released
        // exactly when the physical key is (down/up pairing, not down-only taps).
        root.AddHandler(
            Windows.UI.Xaml.UIElement.KeyUpEvent,
            new Windows.UI.Xaml.Input.KeyEventHandler(OnRootKeyUp),
            handledEventsToo: true);

        var emulatorView = new EmulatorView();
        _emulatorView = emulatorView;
        _videoSurface = emulatorView.SurfaceHost;
        Grid.SetRow(emulatorView, 0);
        root.Children.Add(emulatorView);

        // FEAT-XPERFHUD-001 toggle: apply the persisted performance-counters preference
        // (head-local UWP LocalSettings; default ON) to the freshly created HUD.
        emulatorView.SetPerfStatsVisible(IsPerfHudVisible);

        // The shell Frame and quick menu are full-window overlays (both rows); the
        // keyboard dock owns the bottom row (K2: shrink, never occlude).
        var frame = new Frame { Background = null };
        _frame = frame;
        Grid.SetRow(frame, 0);
        Grid.SetRowSpan(frame, 2);
        root.Children.Add(frame);

        var keyboardOverlay = new VirtualKeyboardOverlay { Visibility = Visibility.Collapsed };
        _keyboardOverlay = keyboardOverlay;
        Grid.SetRow(keyboardOverlay, 1);
        var quickMenu = new QuickMenuOverlay { Visibility = Visibility.Collapsed };
        Grid.SetRow(quickMenu, 0);
        Grid.SetRowSpan(quickMenu, 2);
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

        // FIX-XFOCUSGATE-001 (operator: "When emulator screen is not the focused
        // surface, stop sending joystick input"): gate the global gamepad pump on
        // window activation and force-release any held injected keys on deactivation
        // (a KeyUp for a key held across a focus loss never arrives).
        Window.Current.Activated += (_, e) =>
        {
            var active = e.WindowActivationState != Windows.UI.Core.CoreWindowActivationState.Deactivated;
            _gamepad?.SetWindowActive(active);
            if (!active)
            {
                ReleaseAllPressedKeys();
            }
        };

        Window.Current.Activate();

        if (VideoPull is not null)
        {
            // ROMs were complete at launch: boot straight into the running C64 with the shell
            // menu HIDDEN. The always-present EmulatorView is the at-rest surface; the menu is
            // brought up on demand via the Menu button / ESC (ShowMenu navigates HomePage when
            // the Frame is empty, which it is at launch).
            log.LogInformation("Boot branch: emulator (VideoPull present) -> starting surface + gamepad");
            _videoSurface.Attach(VideoPull);
            _videoSurface.AttachStats(PerfStats);
            ApplyVideoAspectForCurrentSession();
            _videoSurface.Start();
            log.LogInformation("video surface started");
            _gamepad?.Start();
            log.LogInformation("gamepad started (present={GamepadPresent})", _gamepad is not null);
            _bootStarted = true;
            HideMenu();
        }
        else
        {
            // First run / boot blocked: show the provisioning gate. No session, video, or gamepad
            // until the ROMs are acquired; OnProvisioningChanged then rebuilds the host + boots.
            log.LogInformation("Boot branch: provisioning gate (no VideoPull) -> RomProvisioningPage");
            ProvisioningVm = BuildProvisioningVm();
            ProvisioningVm.PropertyChanged += OnProvisioningChanged;
            frame.Navigate(typeof(RomProvisioningPage));
        }
    }

    private XboxRomProvisioningViewModel BuildProvisioningVm() =>
        new(
            new ViceSharp.Xbox.RomProvisioning.RomFetchRomAcquirer(),
            new ViceSharp.Xbox.RomProvisioning.UwpStoragePicker(),
            new RomProvisionEvaluator(),
            _c64Directory,
            RomProfile.Standard);

    private void OnProvisioningChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // React only to the boot-gate signals; RefreshAsync raises these repeatedly (import + download).
        if (e.PropertyName is not (nameof(XboxRomProvisioningViewModel.IsBootBlocked)
                or nameof(XboxRomProvisioningViewModel.State)))
            return;
        if (_bootStarted || ProvisioningVm is null || ProvisioningVm.IsBootBlocked)
            return;

        _bootStarted = true; // single-shot: the assessment can flip Complete more than once.
        ProvisioningVm.PropertyChanged -= OnProvisioningChanged;
        try
        {
            // Fresh ConsoleHostComposition.BuildDefault(): the new DefaultEmulatorRuntimeFactory
            // re-scans LocalFolder\C64 (VICESHARP_ROM_PATH persists) and now registers "c64".
            BuildHostAndSession();
            if (VideoPull is not null)
            {
                _videoSurface?.Attach(VideoPull);
                _videoSurface?.Start();
            }

            _gamepad?.Start();

            // After first-run ROM download, also boot straight into the running emulator with
            // the shell menu hidden (menu is on-demand via Menu button / ESC), mirroring the
            // normal-boot branch in OnLaunched.
            HideMenu();
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "post-provision boot failed");
        }
    }

    private void ConfigureDataPaths()
    {
        try
        {
            var folder = new LocalDataFolder(ApplicationData.Current.LocalFolder.Path);
            var packagedC64 = Path.Combine(
                Package.Current.InstalledLocation.Path, "Assets", "vice-data", "C64");
            XboxDataPathBridge.Configure(folder, packagedC64);

            // The writable C64 ROM dir the first-run download lands in and the post-download
            // factory re-scan reads (LocalFolder\C64). VICESHARP_ROM_PATH is set by Configure
            // above and persists for the process, so the rebuilt factory finds it.
            _c64Directory = folder.C64Path;
        }
        catch
        {
            // Design-time / un-packaged: leave the resolver on its default probe order.
            try { _c64Directory = new LocalDataFolder(ApplicationData.Current.LocalFolder.Path).C64Path; }
            catch { _c64Directory = string.Empty; }
        }
    }

    private void BuildHostAndSession()
    {
        var log = CreateLogger("App");
        log.LogInformation("BuildHostAndSession: composing in-process host");

        // FIX-XNOAUDIO-001 (operator: "No audio!"): live SID audio is ON by default for
        // this interactive head, exactly like the desktop Program.cs; an explicit
        // VICESHARP_AUDIO=0 still disables it. Without this the shared opt-in gate stayed
        // unset in the packaged app and the backend was never created.
        if (Environment.GetEnvironmentVariable("VICESHARP_AUDIO") is null)
            Environment.SetEnvironmentVariable("VICESHARP_AUDIO", "1");

        // Produce the console XAudio2 backend (null when audio is disabled/headless). It is
        // decoupled from the SID ring and engages only when VICESHARP_AUDIO is enabled.
        AudioBackend = XboxAudioWiring.CreateBackend();

        // FIX-XNOAUDIO-001 second gap: the backend must be THREADED into the host's
        // architecture builder or the SID never reaches the device.
        var host = ConsoleHostComposition.BuildDefault(AudioBackend);
        _host = host;

        // FEAT-XSETPERSIST-001: reuse the settings persisted by earlier applies. The persisted
        // ProfileId boots the session as the last-configured machine directly (no
        // boot-then-restart flicker); everything else is re-applied live after the facade is up.
        var settingsPath = ResolveSettingsPersistPath();
        SessionSettingsDto? persisted = null;
        if (settingsPath is not null && XboxSettingsStore.TryLoad(settingsPath, out var loaded))
            persisted = loaded;

        var session = persisted is not null
            ? host.StartC64Session(new ConsoleSessionOptions(persisted.ProfileId))
            : host.StartC64Session();
        if (!session.Success && persisted is not null)
        {
            // The persisted profile no longer starts (e.g. its ROM set went missing):
            // fall back to the default machine rather than failing the boot.
            log.LogWarning(
                "BuildHostAndSession: persisted profile '{ProfileId}' failed to start; falling back to default",
                persisted.ProfileId);
            persisted = null;
            session = host.StartC64Session();
        }
        _sessionId = session.SessionId;
        log.LogInformation(
            "BuildHostAndSession: session started id={SessionId} (persisted profile: {PersistedProfile})",
            _sessionId, persisted?.ProfileId ?? "(none)");

        var facade = new InProcessSessionFacade(host, _sessionId)
        {
            SettingsPersistPath = settingsPath,
        };
        _facade = facade;

        // Re-apply the remaining persisted settings live (limiter/display/input/audio/resources;
        // the profile already matches, so no restart occurs). Fire-and-forget with logging: boot
        // must not block on it, and a failure only costs the restored preferences.
        if (persisted is not null)
            _ = ReapplyPersistedSettingsAsync(facade, _sessionId, persisted);

        // FEAT-XDEFAULTCART-001: the standing vice.ini cartridge (first boot: the embedded
        // S-Blox default, extracted + recorded in vice.ini as normal) attaches at boot with a
        // cart-boot cold reset; user media selections keep vice.ini current via the facade hook.
        try
        {
            _viceSettings = ViceSharp.Core.Configuration.ViceSettings.OpenAt(ApplicationData.Current.LocalFolder.Path);
            facade.MediaSelectionChanged = (slot, path) =>
            {
                try
                {
                    DefaultCartridgeBoot.NoteUserMediaSelection(_viceSettings!, slot, path);
                }
                catch (Exception ex)
                {
                    CreateLogger("App").LogError(ex, "recording media selection in vice.ini failed");
                }
            };

            _ = AttachBootCartridgeAsync(facade, host, _sessionId, _viceSettings, _c64Directory);
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "default-cartridge boot wiring failed");
        }

        // The facade implements both seam interfaces, so disambiguate the ctor overload.
        VideoPull = new VideoFramePullViewModel((IEmulatorSessionFacade)facade, _sessionId);
        SettingsVm = new XboxSettingsViewModel(facade, _sessionId);
        DeviceSetupVm = new XboxDeviceSetupViewModel(facade);
        _machineKeyboard = host.GetKeyboardInput(_sessionId);
        KeyboardVm = _machineKeyboard is { } keyboard
            ? new VirtualKeyboardViewModel(keyboard)
            : null;

        var dispatcher = new AppCommandDispatcher(
            host.HostService,
            host.Snapshots,
            host.Settings,
            onExit: Exit,
            onOpenMenu: ShowMenu,
            onCloseMenu: HideMenu,
            onUiNavigate: HandleUiNavigate);

        _gamepad = new WinRtGamepadSource(host, InputContext, dispatcher, _sessionId);
        log.LogInformation(
            "BuildHostAndSession: built VideoPull={VideoPullCreated} gamepad={GamepadCreated} keyboardVm={KeyboardVmCreated} audio={AudioBackendCreated}",
            VideoPull is not null, _gamepad is not null, KeyboardVm is not null, AudioBackend is not null);

        // The Home page's Start/Resume intents boot/resume the C64 AND dismiss the shell menu so
        // the always-running emulator is unobstructed: HomePage renders a translucent menu card
        // over the running emulator, so collapsing the Frame removes the card to reveal the full C64.
        Home.StartNewRequested += (_, _) => { host.ResetCold(_sessionId); HideMenu(); };
        Home.ResumeRequested += (_, _) => { host.Resume(_sessionId); HideMenu(); };
    }

    /// <summary>
    /// FEAT-XSETPERSIST-001: the LocalState path of the real-time settings persistence file,
    /// or <c>null</c> when the AppContainer LocalFolder is unavailable (persistence disabled).
    /// </summary>
    private static string? ResolveSettingsPersistPath()
    {
        try
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, "settings.json");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// FEAT-XSETPERSIST-001: re-applies the persisted non-profile settings to the freshly
    /// booted session (the profile already matched at StartC64Session, so this stays a live
    /// apply with no restart). Failures are logged and cost only the restored preferences.
    /// </summary>
    private static async Task ReapplyPersistedSettingsAsync(
        InProcessSessionFacade facade, string sessionId, SessionSettingsDto persisted)
    {
        try
        {
            var response = await facade.UpdateSettingsAsync(new UpdateSettingsRequest(
                sessionId,
                Limiter: persisted.Limiter,
                Display: persisted.Display,
                Input: persisted.Input,
                ProfileId: persisted.ProfileId,
                RestartSession: false,
                Audio: persisted.Audio,
                Resources: persisted.Resources)).ConfigureAwait(false);

            CreateLogger("App").LogInformation(
                "persisted settings re-applied: success={Success} profile={ProfileId}",
                response.Status.IsSuccess, persisted.ProfileId);
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "persisted settings re-apply failed");
        }
    }

    /// <summary>
    /// Rebuilds the on-screen keyboard's ViewModel against the CURRENT session's keyboard-input
    /// seam. A model-change apply requests a session rebuild under the SAME <see cref="SessionId"/>,
    /// which leaves the boot-time <see cref="KeyboardVm"/> pointing at a now-dead
    /// <c>IMachineKeyboardInput</c>; this re-fetches the live seam and re-points the overlay so
    /// the virtual keyboard keeps driving the recreated machine. Fully guarded: it runs on the UI
    /// thread and degrades (Debug trace) rather than throwing.
    /// </summary>
    internal void RebuildKeyboardForCurrentSession()
    {
        try
        {
            if (_host is null || string.IsNullOrEmpty(_sessionId))
                return;

            _machineKeyboard = _host.GetKeyboardInput(_sessionId);
            KeyboardVm = _machineKeyboard is { } keyboard
                ? new VirtualKeyboardViewModel(keyboard)
                : null;

            // Re-point a live overlay (if one was created) at the rebuilt VM, mirroring how
            // OnLaunched binds it, so a keyboard already on screen binds the fresh input seam.
            if (_keyboardOverlay is not null && KeyboardVm is not null)
                _keyboardOverlay.DataContext = KeyboardVm;
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "keyboard rebuild failed");
        }
    }

    /// <summary>
    /// FEAT-XKEYCAPCASE-001: whether the live machine currently runs the LOWERCASE
    /// charset (drives the virtual keyboard's letter keycap glyphs). Guarded: reports
    /// uppercase when the facade/session is not up yet.
    /// </summary>
    /// <returns><c>true</c> while the lowercase charset is active.</returns>
    internal bool IsCharsetLowercase()
    {
        try
        {
            return _facade is not null
                && !string.IsNullOrEmpty(_sessionId)
                && _facade.GetCharsetLowercase(_sessionId);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// FEAT-XDEFAULTCART-001: attaches the boot cartridge resolved from vice.ini (first
    /// boot: the embedded S-Blox default) and cold-resets so the cartridge actually boots
    /// (a cartridge only takes over at reset; VICE's CartridgeReset default does the same).
    /// Fire-and-forget with logging: a failure costs only the default cartridge.
    /// </summary>
    private async Task AttachBootCartridgeAsync(
        InProcessSessionFacade facade,
        ViceSharp.Host.Runtime.ConsoleHost host,
        string sessionId,
        ViceSharp.Core.Configuration.ViceSettings settings,
        string? c64Directory)
    {
        try
        {
            var cartridgeDirectory = string.IsNullOrEmpty(c64Directory)
                ? Path.Combine(ApplicationData.Current.LocalFolder.Path, "C64")
                : c64Directory;

            var cartridge = DefaultCartridgeBoot.ResolveBootCartridge(settings, cartridgeDirectory);
            if (cartridge is null)
            {
                CreateLogger("App").LogInformation("boot cartridge: none configured");
                return;
            }

            var payload = await Task.Run(() => File.ReadAllBytes(cartridge)).ConfigureAwait(false);
            var response = await facade.AttachMediaAsync(
                    MediaSlot.Cartridge,
                    cartridge,
                    isReadOnly: true,
                    payload,
                    Path.GetFileName(cartridge))
                .ConfigureAwait(false);

            if (response.Status.IsSuccess)
            {
                // The cartridge maps in live; a cold reset makes it BOOT (cart takes over
                // the reset vector, exactly like VICE's attach-with-CartridgeReset).
                host.ResetCold(sessionId);
                CreateLogger("App").LogInformation(
                    "boot cartridge attached + cold reset: {Cartridge} ({Bytes} bytes)",
                    cartridge, payload.Length);
            }
            else
            {
                CreateLogger("App").LogWarning(
                    "boot cartridge attach failed: {Message}", response.Status.Message);
            }
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "boot cartridge attach failed");
        }
    }

    /// <summary>
    /// FIX-XASPECT-001: pushes the ACTIVE session's TRUE composite pixel aspect into the video
    /// surface (VICE vicii.c vicii_get_pixel_aspect, mirrored by
    /// <see cref="ViceSharp.Chips.VicIi.VideoRenderer.GetPixelAspectRatio"/>: PAL 0.93650794,
    /// NTSC 0.75). Called at boot and re-called after a model-change apply restarts the session
    /// under the same id (an NTSC model must not display with PAL geometry). The profile-level
    /// <see cref="VideoStandard"/> enum carries Pal|Ntsc; the PAL-N / old-NTSC profile variants
    /// collapse onto those two composite standards. Fully guarded: degrades to the last-applied
    /// aspect and logs rather than throwing.
    /// </summary>
    internal void ApplyVideoAspectForCurrentSession()
    {
        try
        {
            var standard = _facade is not null && !string.IsNullOrEmpty(_sessionId)
                ? _facade.GetVideoStandard(_sessionId)
                : null;

            var system = standard == VideoStandard.Ntsc
                ? ViceSharp.Chips.VicIi.Mos6569.TvSystem.NTSC
                : ViceSharp.Chips.VicIi.Mos6569.TvSystem.PAL;
            var aspect = ViceSharp.Chips.VicIi.VideoRenderer.GetPixelAspectRatio(system);

            _videoSurface?.SetPixelAspect(aspect);

            // FIX-XNTSCFILL-001: crop the display to the standard's WRITTEN frame rows so an
            // NTSC machine grows to fill the window (246 content rows of the fixed 272-row
            // frame) and switching back to PAL shrinks to fit (the full 272).
            var contentHeight = _facade is not null && !string.IsNullOrEmpty(_sessionId)
                ? _facade.GetFrameContentHeight(_sessionId) ?? 0
                : 0;
            _videoSurface?.SetSourceContentHeight(contentHeight);

            // FIX-XNTSCFPS-001: pace the render loop at the ACTIVE machine's refresh rate
            // (NTSC ~59.826 Hz, PAL ~50.125 Hz). The fixed 20 ms cadence capped every
            // machine at ~50 fps nominal; with per-tick paint cost it fell to ~22 fps on
            // the operator's HUD while SPD ~100% proved the emulation itself held real time.
            var refreshHz = _facade is not null && !string.IsNullOrEmpty(_sessionId)
                ? _facade.GetRefreshRateHz(_sessionId) ?? 0d
                : 0d;
            _videoSurface?.SetTargetRefreshRate(refreshHz);

            // FEAT-XPERFHUD-001: feed the HUD the same machine facts (nominal clock for the
            // speed-percent line; 0 = unknown, which omits the line rather than fabricating).
            var clockHz = _facade is not null && !string.IsNullOrEmpty(_sessionId)
                ? _facade.GetMachineClockHz(_sessionId) ?? 0d
                : 0d;
            PerfStats.SetMachine(clockHz, standard == VideoStandard.Ntsc ? "NTSC" : "PAL", aspect);

            CreateLogger("App").LogInformation(
                "video aspect applied: standard={Standard} pixelAspect={PixelAspect} clockHz={ClockHz} contentRows={ContentRows} refreshHz={RefreshHz} surface={SurfacePresent}",
                standard, aspect, clockHz, contentHeight, refreshHz, _videoSurface is not null);
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "video aspect apply failed");
        }
    }

    /// <summary>True when the shell-menu Frame is currently shown over the running emulator.</summary>
    private bool IsMenuOpen => _frame is not null && _frame.Visibility == Visibility.Visible;

    /// <summary>Toggles the shell menu: show it if hidden, hide it if shown (ESC / Menu button).</summary>
    private void ToggleMenu()
    {
        if (IsMenuOpen)
        {
            HideMenu();
        }
        else
        {
            ShowMenu();
        }
    }

    /// <summary>
    /// Applies one shell-menu navigation command on the UI thread (invoked by the input
    /// dispatcher's onUiNavigate callback and by the keyboard handler). Directional commands
    /// move XAML focus, UiActivate invokes the focused Button, and UiBack pops the Frame's
    /// back-stack (or hides the menu when there is nothing to go back to).
    /// </summary>
    /// <param name="command">The UI navigation command to apply.</param>
    private void HandleUiNavigate(ViceSharp.Xbox.Input.AppCommand command)
    {
        switch (command)
        {
            case ViceSharp.Xbox.Input.AppCommand.UiNavigateUp:
                FocusManager.TryMoveFocus(FocusNavigationDirection.Up);
                break;
            case ViceSharp.Xbox.Input.AppCommand.UiNavigateDown:
                FocusManager.TryMoveFocus(FocusNavigationDirection.Down);
                break;
            case ViceSharp.Xbox.Input.AppCommand.UiNavigateLeft:
                FocusManager.TryMoveFocus(FocusNavigationDirection.Left);
                break;
            case ViceSharp.Xbox.Input.AppCommand.UiNavigateRight:
                FocusManager.TryMoveFocus(FocusNavigationDirection.Right);
                break;
            case ViceSharp.Xbox.Input.AppCommand.UiActivate:
                if (FocusManager.GetFocusedElement() is Windows.UI.Xaml.Controls.Button b)
                {
                    var peer = new Windows.UI.Xaml.Automation.Peers.ButtonAutomationPeer(b);
                    (peer.GetPattern(Windows.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
                        as Windows.UI.Xaml.Automation.Provider.IInvokeProvider)?.Invoke();
                }

                break;
            case ViceSharp.Xbox.Input.AppCommand.UiBack:
                if (_frame?.CanGoBack == true)
                {
                    _frame.GoBack();
                }
                else
                {
                    HideMenu();
                }

                break;

            // FIX-XKBDINPUT-001: the virtual-keyboard overlay toggle (View) and the
            // operator's key chords (remapped 2026-07-14: X=INST/DEL, Y=SPACE,
            // B=RUN/STOP, LB=cursor-left, RB=SHIFT+cursor-left).
            case ViceSharp.Xbox.Input.AppCommand.ToggleVirtualKeyboard:
                ToggleKeyboardOverlay();
                break;
            case ViceSharp.Xbox.Input.AppCommand.KeyboardKeyDelete:
                InjectC64Key("Delete");
                break;
            case ViceSharp.Xbox.Input.AppCommand.KeyboardKeySpace:
                InjectC64Key("Space");
                break;
            case ViceSharp.Xbox.Input.AppCommand.KeyboardKeyRunStop:
                InjectC64Key("RunStop");
                break;
            case ViceSharp.Xbox.Input.AppCommand.KeyboardKeyCursorLeft:
                InjectC64Key("Left");
                break;
            case ViceSharp.Xbox.Input.AppCommand.KeyboardKeyShiftCursorLeft:
                InjectShiftedC64Key("Left");
                break;

            // Trigger modifiers (operator 2026-07-14: LT = C=, RT = SHIFT): true HOLD
            // semantics through the machine seam, tracked in the pressed set so menu
            // entry / overlay close force-release them like any other held key.
            case ViceSharp.Xbox.Input.AppCommand.KeyboardModifierCommodoreDown:
                InjectModifier("Commodore", down: true);
                // FEAT-XKEYCAPPETSCII-001: keycaps show the C= graphics while held.
                _keyboardOverlay?.SetExternalCommodore(true);
                break;
            case ViceSharp.Xbox.Input.AppCommand.KeyboardModifierCommodoreUp:
                InjectModifier("Commodore", down: false);
                _keyboardOverlay?.SetExternalCommodore(false);
                break;
            case ViceSharp.Xbox.Input.AppCommand.KeyboardModifierShiftDown:
                InjectModifier("LeftShift", down: true);
                // FEAT-XKEYCAPSHIFT-001: keycaps show the shifted legends while held.
                _keyboardOverlay?.SetExternalShift(true);
                break;
            case ViceSharp.Xbox.Input.AppCommand.KeyboardModifierShiftUp:
                InjectModifier("LeftShift", down: false);
                _keyboardOverlay?.SetExternalShift(false);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Holds or releases one C64 modifier key through the machine seam, tracked in the
    /// pressed set (so <see cref="ReleaseAllPressedKeys"/> covers a modifier left held
    /// by any exit path). Idempotent per direction.
    /// </summary>
    private void InjectModifier(string keyName, bool down)
    {
        if (_machineKeyboard is not { } keyboard)
            return;

        if (down)
        {
            if (_pressedKeys.Add(keyName))
                keyboard.SetKeyState(keyName, true);
        }
        else if (_pressedKeys.Remove(keyName))
        {
            keyboard.SetKeyState(keyName, false);
        }
    }

    /// <summary>
    /// Toggles the docked virtual-keyboard overlay via the navigation model's overlay flag
    /// (K2: the dock shrinks the emulator row) and, when opening, moves XAML focus into the
    /// keyboard so the D-pad immediately navigates tiles and A presses the focused tile
    /// (FIX-XKBDINPUT-001).
    /// </summary>
    private void ToggleKeyboardOverlay()
    {
        Navigation.IsVirtualKeyboardOpen = !Navigation.IsVirtualKeyboardOpen;

        if (Navigation.IsVirtualKeyboardOpen && _keyboardOverlay is not null)
        {
            var first = FocusManager.FindFirstFocusableElement(_keyboardOverlay);
            (first as Windows.UI.Xaml.Controls.Control)?.Focus(Windows.UI.Xaml.FocusState.Programmatic);
        }
        else if (!Navigation.IsVirtualKeyboardOpen)
        {
            // Closing the keyboard force-releases anything still held (trigger modifiers
            // included) so no key can stick inside the machine across the dismissal.
            ReleaseAllPressedKeys();
        }
    }

    /// <summary>Injects one C64 key down/up through the machine keyboard seam (chords).</summary>
    /// <param name="keyName">The C64 keyboard-map key name.</param>
    private void InjectC64Key(string keyName)
    {
        if (_machineKeyboard is not { } keyboard)
            return;

        keyboard.SetKeyState(keyName, true);
        keyboard.SetKeyState(keyName, false);
    }

    /// <summary>Injects SHIFT + one C64 key (hardware-style wrap) through the seam.</summary>
    /// <param name="keyName">The C64 keyboard-map key name to shift.</param>
    private void InjectShiftedC64Key(string keyName)
    {
        if (_machineKeyboard is not { } keyboard)
            return;

        keyboard.SetKeyState("LeftShift", true);
        keyboard.SetKeyState(keyName, true);
        keyboard.SetKeyState(keyName, false);
        keyboard.SetKeyState("LeftShift", false);
    }

    /// <summary>
    /// Root-level KeyDown handler (registered with handledEventsToo). ESC toggles the shell
    /// menu; WASD/arrows drive focus navigation only while the menu is open. With the menu
    /// CLOSED, every mappable physical key types straight into the running C64
    /// (FIX-XKBDINPUT-001, operator: "Emulator not receiving keyboard input"): the key is
    /// injected DOWN here and released by <see cref="OnRootKeyUp"/>, with the held set
    /// tracked so menu entry can force-release everything.
    /// </summary>
    private void OnRootKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Escape: ToggleMenu(); e.Handled = true; return;
            case Windows.System.VirtualKey.W: case Windows.System.VirtualKey.Up:    if (IsMenuOpen) { HandleUiNavigate(ViceSharp.Xbox.Input.AppCommand.UiNavigateUp);    e.Handled = true; return; } break;
            case Windows.System.VirtualKey.S: case Windows.System.VirtualKey.Down:  if (IsMenuOpen) { HandleUiNavigate(ViceSharp.Xbox.Input.AppCommand.UiNavigateDown);  e.Handled = true; return; } break;
            case Windows.System.VirtualKey.A: case Windows.System.VirtualKey.Left:  if (IsMenuOpen) { HandleUiNavigate(ViceSharp.Xbox.Input.AppCommand.UiNavigateLeft);  e.Handled = true; return; } break;
            case Windows.System.VirtualKey.D: case Windows.System.VirtualKey.Right: if (IsMenuOpen) { HandleUiNavigate(ViceSharp.Xbox.Input.AppCommand.UiNavigateRight); e.Handled = true; return; } break;

            // FIX-XDPADSKIP-001 (operator: "Using the dpad to move between buttons is
            // too sensitive and skips buttons"): the SAME physical press arrives twice:
            // once through the polled pipeline (UiNavigate -> TryMoveFocus / UiActivate
            // -> peer.Invoke) and once as a native Gamepad* virtual key driving XAML's
            // XY focus engine - two moves (or two activations) per press. The polled
            // pipeline is the single navigator: swallow the native gamepad keys while a
            // menu or the virtual keyboard owns navigation.
            case Windows.System.VirtualKey.GamepadDPadUp:
            case Windows.System.VirtualKey.GamepadDPadDown:
            case Windows.System.VirtualKey.GamepadDPadLeft:
            case Windows.System.VirtualKey.GamepadDPadRight:
            case Windows.System.VirtualKey.GamepadLeftThumbstickUp:
            case Windows.System.VirtualKey.GamepadLeftThumbstickDown:
            case Windows.System.VirtualKey.GamepadLeftThumbstickLeft:
            case Windows.System.VirtualKey.GamepadLeftThumbstickRight:
            case Windows.System.VirtualKey.GamepadA:
            case Windows.System.VirtualKey.GamepadB:
                if (IsMenuOpen || Navigation.IsVirtualKeyboardOpen)
                {
                    e.Handled = true;
                    return;
                }

                break;
        }

        // Physical keyboard -> C64 (menu closed only; key auto-repeat collapses onto the
        // held set so the machine sees one clean down per physical press).
        if (!IsMenuOpen
            && _machineKeyboard is { } keyboard
            && PhysicalKeyMap.TryTranslate((int)e.Key, out var keyName))
        {
            if (_pressedKeys.Add(keyName))
            {
                keyboard.SetKeyState(keyName, true);
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// Root-level KeyUp counterpart (FIX-XKBDINPUT-001): releases exactly the C64 keys this
    /// app pressed (pairing through the held set, so ups after a menu force-release or for
    /// keys the menu consumed are no-ops).
    /// </summary>
    private void OnRootKeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (PhysicalKeyMap.TryTranslate((int)e.Key, out var keyName)
            && _pressedKeys.Remove(keyName)
            && _machineKeyboard is { } keyboard)
        {
            keyboard.SetKeyState(keyName, false);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Force-releases every C64 key this app currently holds (FIX-XKBDINPUT-001): called on
    /// menu entry so a key held while ESC/Menu fires can never stick down inside the machine.
    /// </summary>
    private void ReleaseAllPressedKeys()
    {
        if (_pressedKeys.Count == 0)
            return;

        if (_machineKeyboard is { } keyboard)
        {
            foreach (var key in _pressedKeys)
            {
                keyboard.SetKeyState(key, false);
            }
        }

        _pressedKeys.Clear();
    }

    /// <summary>Reveals the shell-menu Frame over the running emulator (Menu button -> OpenMainMenu).</summary>
    private void ShowMenu()
    {
        // FIX-XKBDINPUT-001: entering the menu force-releases any held C64 keys.
        ReleaseAllPressedKeys();

        // FEAT-XMENUPAUSE-001 (operator: "Emulator needs to pause when opening the menu
        // and unpause when done"): the machine freezes while the shell menu is up.
        TryPauseEmulation();

        if (_frame is null)
        {
            return;
        }

        if (_frame.Content is null)
        {
            _frame.Navigate(typeof(HomePage));
        }

        _frame.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// FEAT-XMENUSNAP-001: the menu's durable snapshot slot in LocalState.
    /// </summary>
    private static string SnapshotSlotPath
        => Path.Combine(ApplicationData.Current.LocalFolder.Path, "snapshot-slot1.json");

    /// <summary>
    /// FEAT-XMENUSNAP-001 (operator: "Add SAVE and LOAD buttons that can save and load
    /// snapshots"): captures the machine - held PAUSED by the open menu, so the state is
    /// frozen at a clean point - and persists it to the LocalState slot via the
    /// AOT-safe source-generated JSON context. Dismisses the menu on success (which
    /// resumes the machine); failures log and keep the menu up.
    /// </summary>
    internal async Task SaveSnapshotAsync()
    {
        try
        {
            if (_host is null || string.IsNullOrEmpty(_sessionId))
                return;

            var response = await _host.Snapshots
                .CaptureSnapshotAsync(new SessionRequest(_sessionId));
            if (!response.Status.IsSuccess || response.Snapshot is null)
            {
                CreateLogger("App").LogWarning(
                    "snapshot save failed: {Message}", response.Status.Message);
                return;
            }

            var json = System.Text.Json.JsonSerializer.Serialize(
                response.Snapshot, SnapshotJsonContext.Default.SnapshotDto);
            var path = SnapshotSlotPath;
            await Task.Run(() => File.WriteAllText(path, json));

            CreateLogger("App").LogInformation(
                "snapshot saved: {Path} ({Bytes} payload bytes, cycle {Cycle})",
                path, response.Snapshot.Payload.Length, response.Snapshot.Cycle);
            HideMenu();
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "snapshot save failed");
        }
    }

    /// <summary>
    /// FEAT-XMENUSNAP-001: restores the LocalState snapshot slot into the paused
    /// machine and dismisses the menu (resuming at the restored state). A missing or
    /// unreadable slot logs and keeps the menu up; nothing is fabricated.
    /// </summary>
    internal async Task LoadSnapshotAsync()
    {
        try
        {
            if (_host is null || string.IsNullOrEmpty(_sessionId))
                return;

            var path = SnapshotSlotPath;
            if (!File.Exists(path))
            {
                CreateLogger("App").LogWarning("snapshot load: no saved snapshot at {Path}", path);
                return;
            }

            var json = await Task.Run(() => File.ReadAllText(path));
            var snapshot = System.Text.Json.JsonSerializer.Deserialize(
                json, SnapshotJsonContext.Default.SnapshotDto);
            if (snapshot is null)
            {
                CreateLogger("App").LogWarning("snapshot load: slot file was empty/invalid: {Path}", path);
                return;
            }

            var response = await _host.Snapshots
                .RestoreSnapshotAsync(new RestoreSnapshotRequest(_sessionId, snapshot));
            if (!response.Status.IsSuccess)
            {
                CreateLogger("App").LogWarning(
                    "snapshot restore failed: {Message}", response.Status.Message);
                return;
            }

            CreateLogger("App").LogInformation(
                "snapshot restored: {Path} (cycle {Cycle})", path, snapshot.Cycle);
            HideMenu();
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "snapshot load failed");
        }
    }

    /// <summary>
    /// FEAT-XMENUPAUSE-001: pauses the session for the shell menu. Guarded and
    /// session-locked (a menu opened before the host is built is a no-op).
    /// </summary>
    private void TryPauseEmulation()
    {
        try
        {
            if (_host is null || string.IsNullOrEmpty(_sessionId))
                return;

            _host.Pause(_sessionId);
            CreateLogger("App").LogInformation("menu open: emulation paused");
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "pausing for the menu failed");
        }
    }

    /// <summary>
    /// FEAT-XMENUPAUSE-001: resumes the session when the shell menu goes away. The
    /// host's Resume is idempotent, so boot-time HideMenu calls and the Home page's own
    /// Resume/Start flows stay harmless.
    /// </summary>
    private void TryResumeEmulation()
    {
        try
        {
            if (_host is null || string.IsNullOrEmpty(_sessionId))
                return;

            _host.Resume(_sessionId);
            CreateLogger("App").LogInformation("menu closed: emulation resumed");
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "resuming after the menu failed");
        }
    }

    /// <summary>
    /// Mouse-friendly menu dismissal (operator 2026-07-14): hides the shell menu without
    /// resetting anything; FEAT-XMENUPAUSE-001 then resumes the machine that ShowMenu
    /// paused (the menu freezes gameplay; every dismissal unfreezes it). Called by the
    /// HomePage Close Menu button and its click-outside-the-card handler.
    /// </summary>
    internal void DismissMenu() => HideMenu();

    /// <summary>
    /// FEAT-XPERFHUD-001 toggle (operator 2026-07-14: "Need a toggle in settings for
    /// performance counters"): the persisted show/hide preference for the letterbox HUD.
    /// Head-local (UWP LocalSettings key "ShowPerfHud", persisted in real time), because HUD
    /// visibility is a display preference of THIS head, not a host session setting. Defaults
    /// to ON, and degrades to ON when LocalSettings is unavailable.
    /// </summary>
    internal bool IsPerfHudVisible
    {
        get
        {
            try
            {
                return ApplicationData.Current.LocalSettings.Values["ShowPerfHud"] is not bool visible
                    || visible;
            }
            catch
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Persists the performance-counters preference and applies it to the live HUD
    /// immediately (no restart). Called by the Settings page toggle.
    /// </summary>
    /// <param name="visible"><c>true</c> to show the letterbox HUD.</param>
    internal void SetPerfHudVisible(bool visible)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values["ShowPerfHud"] = visible;
        }
        catch (Exception ex)
        {
            CreateLogger("App").LogError(ex, "perf-HUD preference persist failed");
        }

        _emulatorView?.SetPerfStatsVisible(visible);
        CreateLogger("App").LogInformation("perf HUD visibility set: {Visible}", visible);
    }

    /// <summary>Hides the shell-menu Frame to expose the always-running emulator (Start/Resume, CloseMenu).</summary>
    private void HideMenu()
    {
        if (_frame is not null)
        {
            _frame.Visibility = Visibility.Collapsed;
        }

        // FEAT-XMENUPAUSE-001: every dismissal path unfreezes the machine (idempotent).
        TryResumeEmulation();
    }
}
#endif
