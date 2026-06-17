using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Avalonia.Views;
using ViceSharp.Host.Services;
using Protocol = ViceSharp.Protocol;

namespace ViceSharp.Avalonia;

public partial class MainWindow : Window
{
    private readonly IHostProtocolClient _hostClient;
    private readonly IAsyncDisposable? _localHost;
    private readonly ILocalVideoFrameSource? _localVideoFrameSource;
    private readonly AttachPanelViewModel _attachViewModel;
    private readonly ShellViewModel _shell;
    private readonly Persistence.SessionPersistence _persistence = new();
    private readonly StatusBarViewModel _statusBarViewModel = new();
    private readonly AttachPanelView _attachPanel;
    private readonly VideoSurface _video;
    private TextBlock? _statusText;

    public MainWindow()
    {
        InitializeComponent();

        var hostConnection = CreateHostClient();
        _hostClient = hostConnection.Client;
        _localHost = hostConnection.LocalHost;
        _localVideoFrameSource = hostConnection.VideoFrameSource;
        _attachViewModel = new AttachPanelViewModel(_hostClient);
        _shell = new ShellViewModel(_hostClient, _attachViewModel);
        DataContext = _attachViewModel;

        _statusBarViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(StatusBarViewModel.StatusText) && _statusText is not null)
                _statusText.Text = _statusBarViewModel.StatusText;
        };

        _attachPanel = new AttachPanelView(_attachViewModel)
        {
            PickFileAsync = PickMediaFileAsync,
            PickKeyboardMapFileAsync = PickKeyboardMapFileAsync,
            PopOutMonitorRequested = OpenMonitorWindow
        };
        _video = new VideoSurface
        {
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch
        };
        _video.KeyDown += OnVideoKeyDown;
        _video.KeyUp += OnVideoKeyUp;

        // Inject the live views into the declarative shell's named hosts. The
        // sidebar pane and video content stay code-behind-owned for now; the
        // reusable PeripheralCardView / SidebarView land in S2.
        _statusText = this.FindControl<TextBlock>("PART_StatusText");
        if (_statusText is not null)
            _statusText.Text = _statusBarViewModel.StatusText;
        if (this.FindControl<ContentControl>("PART_SidebarHost") is { } sidebarHost)
            sidebarHost.Content = _attachPanel;
        if (this.FindControl<ContentControl>("PART_VideoHost") is { } videoHost)
            videoHost.Content = _video;

        Opened += (_, _) => _video.Focus();

        _ = InitializeViewModelAsync();

        var renderTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(1.0 / 50.0),
            DispatcherPriority.Render,
            async (_, _) => await RefreshFrameAsync().ConfigureAwait(true));
        renderTimer.Start();

        var statusTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(500),
            DispatcherPriority.Background,
            async (_, _) => await UpdateStatusAsync().ConfigureAwait(true));
        statusTimer.Start();
    }

    private async Task InitializeViewModelAsync()
    {
        await _attachViewModel.RefreshAsync().ConfigureAwait(true);

        Persistence.PersistedState persisted;
        try
        {
            persisted = _persistence.Load();
        }
        catch
        {
            return; // first run / unreadable config: nothing to restore
        }

        _attachViewModel.SaveSettingsOnExit = persisted.SaveSettingsOnExit;
        _attachViewModel.SaveTransientValuesOnExit = persisted.SaveTransientValuesOnExit;

        try
        {
            if (persisted.Settings is not null)
                await _attachViewModel.ApplyPersistedSettingsAsync(persisted.Settings).ConfigureAwait(true);
            if (persisted.Transient is not null)
                await _attachViewModel.ApplyPersistedTransientAsync(persisted.Transient).ConfigureAwait(true);
        }
        catch
        {
            // Restoring persisted state must never break startup.
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            var state = new Persistence.PersistedState(
                _attachViewModel.SaveSettingsOnExit,
                _attachViewModel.SaveTransientValuesOnExit,
                _attachViewModel.SaveSettingsOnExit ? _attachViewModel.CapturePersistedSettings() : null,
                _attachViewModel.SaveTransientValuesOnExit ? _attachViewModel.CapturePersistedTransient() : null);
            _persistence.Save(state);
        }
        catch
        {
            // Persistence must never block window close.
        }

        base.OnClosing(e);
    }

    protected override async void OnClosed(EventArgs e)
    {
        if (_hostClient is IDisposable disposableClient)
            disposableClient.Dispose();

        if (_localHost is not null)
            await _localHost.DisposeAsync().ConfigureAwait(false);

        base.OnClosed(e);
    }

    private static HostConnection CreateHostClient()
    {
        var endpoint = Environment.GetEnvironmentVariable("VICESHARP_HOST_URI");
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            var sessionId = Environment.GetEnvironmentVariable("VICESHARP_SESSION_ID") ?? string.Empty;
            return new HostConnection(new GrpcHostProtocolClient(uri, sessionId), null);
        }

        try
        {
            var localHost = InProcessGrpcHost.StartAsync().GetAwaiter().GetResult();
            return new HostConnection(new GrpcHostProtocolClient(localHost.Endpoint), localHost, localHost.VideoFrameSource);
        }
        catch (Exception ex)
        {
            return new HostConnection(new DisconnectedHostProtocolClient($"Could not start local emulator host: {ex.Message}"), null);
        }
    }

    // ---- Shell commands (single side-toggle + flyout open/close) -------------

    private void OnToggleSidebar(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _attachViewModel.ToggleSidebar();
        _video.Focus();
    }

    private void OnToggleDockSide(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _attachViewModel.ToggleDockSide();
        _video.Focus();
    }

    // ---- Menu + transport commands ------------------------------------------

    private void OnMenuExit(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void OnMenuPause(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RunCommandAsync(() => _shell.PauseAsync().AsTask());

    private void OnMenuResume(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RunCommandAsync(() => _shell.ResumeAsync().AsTask());

    private void OnStepCycle(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RunCommandAsync(() => _shell.StepCycleAsync(1).AsTask());

    private void OnStepFrame(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RunCommandAsync(() => _shell.StepFrameAsync(1).AsTask());

    private void OnRewindCycle(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RunCommandAsync(() => _shell.RewindCycleAsync(1).AsTask());

    private void OnRewindFrame(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RunCommandAsync(() => _shell.RewindFrameAsync(1).AsTask());

    private void OnMenuColdReset(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RunCommandAsync(() => _shell.ColdResetAsync().AsTask());

    private void OnMenuWarmReset(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RunCommandAsync(() => _shell.WarmResetAsync().AsTask());

    private void OnRunDrive8(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RunCommandAsync(() => _shell.AutostartDrive8Async().AsTask());

    private void OnMenuToggleWarp(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = _shell.ToggleWarpAsync();

    private void OnMenuSwapJoysticks(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = _shell.SwapJoysticksAsync();

    private void OnMenuTrueDrive8(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _shell.ToggleTrueDrive(Protocol.MediaSlot.Drive8);

    private void OnMenuTrueDrive9(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _shell.ToggleTrueDrive(Protocol.MediaSlot.Drive9);

    private void OnMenuAttachDrive9(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = _shell.AttachAsync(Protocol.MediaSlot.Drive9);

    private void OnMenuDetachDrive9(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = _shell.DetachAsync(Protocol.MediaSlot.Drive9);

    private void OnMenuShowSettings(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _shell.ShowSettings();

    private void OnMenuOpenMonitor(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => OpenMonitorWindow();

    private void OnMenuAbout(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var about = new Window
        {
            Title = "About ViceSharp",
            Width = 360,
            Height = 180,
            CanResize = false,
            Content = new TextBlock
            {
                Margin = new global::Avalonia.Thickness(16),
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                Text = "ViceSharp\nA C# .NET 10 port of the VICE Commodore 64 emulator."
            }
        };
        about.ShowDialog(this);
    }

    private void OnMenuSmartAttach(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = _shell.AttachAsync(Protocol.MediaSlot.Drive8);

    private void OnMenuAttachDrive8(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = _shell.AttachAsync(Protocol.MediaSlot.Drive8);

    private void OnMenuDetachDrive8(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = _shell.DetachAsync(Protocol.MediaSlot.Drive8);

    private async Task RunCommandAsync(Func<Task<Protocol.EmulatorCommandResponse>> action)
    {
        try
        {
            var response = await action().ConfigureAwait(true);
            ApplyStatus(response.EmulatorStatus, response.Status);
            _video.Focus();
        }
        catch (Exception ex)
        {
            if (_statusText is not null)
                _statusText.Text = ex.Message;
        }
    }

    private async Task RefreshFrameAsync()
    {
        try
        {
            // In-process: zero-allocation, lock-free copy of the emulation thread's
            // published frame straight into the render surface (BUG-THROTTLE-001 /
            // FR-1132) - the UI render tick never touches the emulation lock.
            if (_localVideoFrameSource is not null)
            {
                var sessionId = _hostClient.SessionId;
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    await _hostClient.ListMediaAsync().ConfigureAwait(true);
                    sessionId = _hostClient.SessionId;
                }

                if (!string.IsNullOrWhiteSpace(sessionId))
                    _video.UpdateFrom(_localVideoFrameSource, sessionId);
                return;
            }

            var frame = await _hostClient.GetFrameAsync().ConfigureAwait(true);
            if (frame.Status.IsSuccess)
                _video.SetFrame(frame.Frame);
        }
        catch
        {
            // Host connectivity failures are reflected by the attach/status panel.
        }
    }

    private async Task UpdateStatusAsync()
    {
        try
        {
            var response = await _hostClient.GetStatusAsync().ConfigureAwait(true);
            ApplyStatus(response.EmulatorStatus, response.Status);
        }
        catch
        {
            // The attach panel already shows connectivity problems.
        }
    }

    private void ApplyStatus(Protocol.EmulatorStatusDto? status, Protocol.RpcStatus rpcStatus)
    {
        _statusBarViewModel.ApplyStatus(status, rpcStatus);
        if (rpcStatus.IsSuccess && status is not null)
            _attachViewModel.ApplyStatus(status);
    }

    private async void OnVideoKeyDown(object? sender, KeyEventArgs e)
    {
        // Warp toggle: Alt+W (or Ctrl+W as fallback), like classic VICE
        if ((e.Key == Key.W) && (e.KeyModifiers.HasFlag(KeyModifiers.Alt) || e.KeyModifiers.HasFlag(KeyModifiers.Control)))
        {
            _attachViewModel.IsWarpMode = !_attachViewModel.IsWarpMode;
            await _attachViewModel.ApplySettingsAsync(false).ConfigureAwait(true);
            e.Handled = true;
            return;
        }

        await SendKeyStateAsync(e, true).ConfigureAwait(true);
    }

    private async void OnVideoKeyUp(object? sender, KeyEventArgs e)
    {
        await SendKeyStateAsync(e, false).ConfigureAwait(true);
    }

    private async Task SendKeyStateAsync(KeyEventArgs e, bool isPressed)
    {
        var key = ToHostKeyName(e);
        if (key is null)
            return;

        e.Handled = true;

        try
        {
            await _hostClient.SetKeyStateAsync(
                key,
                isPressed,
                physicalKey: e.Key.ToString(),
                text: ToHostText(e, key),
                modifiers: (int)e.KeyModifiers).ConfigureAwait(true);
        }
        catch
        {
            // Host connectivity failures are reflected by the attach/status panel.
        }
    }

    private static string ToHostText(KeyEventArgs e, string key)
    {
        if (key.Length == 1)
            return key;

        return e.Key switch
        {
            Key.Space => " ",
            Key.Return or Key.Enter => "\r",
            Key.Tab => "\t",
            _ => string.Empty
        };
    }

    private static string? ToHostKeyName(KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Alt) != 0 && e.Key == Key.F4)
            return null;

        var shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
        return e.Key switch
        {
            Key.None => null,
            Key.Return or Key.Enter => "Enter",
            Key.Back => "Backspace",
            Key.LeftShift => "LeftShift",
            Key.RightShift => "RightShift",
            Key.LeftCtrl => "LeftCtrl",
            Key.RightCtrl => "RightCtrl",
            Key.LeftAlt => "LeftAlt",
            Key.RightAlt => "RightAlt",
            Key.OemPlus => shift ? "+" : "=",
            Key.OemSemicolon or Key.Oem1 => shift ? ":" : ";",
            Key.OemQuestion or Key.Oem2 => shift ? "?" : "/",
            Key.OemQuotes or Key.Oem7 => "\"",
            Key.Multiply => "*",
            Key.Add => "+",
            Key.Subtract => "-",
            Key.Divide => "/",
            Key.Decimal => ".",
            _ => e.Key.ToString()
        };
    }

    private async Task<string?> PickMediaFileAsync(AttachSlotViewModel slot)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return null;

        var fileTypes = new[]
        {
            new FilePickerFileType(slot.MediaKind)
            {
                Patterns = slot.FilePatterns
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = fileTypes,
            Title = $"Attach {slot.Title}"
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    private async Task<string?> PickKeyboardMapFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("VICE keymap")
                {
                    Patterns = ["*.vkm"]
                }
            ],
            Title = "Select VICE keyboard map"
        });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    private void OpenMonitorWindow()
    {
        var monitorWindow = new Window
        {
            Title = "ViceSharp Monitor",
            Width = 680,
            Height = 520,
            Content = _attachPanel.CreateMonitorPanel(includePopOut: false)
        };
        monitorWindow.Show(this);
    }

    private sealed record HostConnection(
        IHostProtocolClient Client,
        IAsyncDisposable? LocalHost,
        ILocalVideoFrameSource? VideoFrameSource = null);
}
