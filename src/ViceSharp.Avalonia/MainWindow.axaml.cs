using Avalonia;
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
    private readonly AttachPanelView _attachPanel;
    private readonly VideoSurface _video;
    private readonly DockPanel _root = new();
    private readonly Grid _layout = new();
    private readonly TextBlock _statusText = new();
    private DockPanel? _sidebarHost;
    private bool _sidebarCollapsed;

    public MainWindow()
    {
        InitializeComponent();
        Title = "ViceSharp";
        Width = 1120;
        Height = 720;
        MinWidth = 780;
        MinHeight = 520;

        var hostConnection = CreateHostClient();
        _hostClient = hostConnection.Client;
        _localHost = hostConnection.LocalHost;
        _localVideoFrameSource = hostConnection.VideoFrameSource;
        _attachViewModel = new AttachPanelViewModel(_hostClient);
        _attachViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.DockSide))
                BuildLayout();
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

        var statusBar = CreateStatusBar();
        DockPanel.SetDock(statusBar, Dock.Bottom);
        _root.Children.Add(statusBar);
        _root.Children.Add(_layout);
        Content = _root;
        BuildLayout();
        Opened += (_, _) => _video.Focus();

        _ = _attachViewModel.RefreshAsync();

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

    private void BuildLayout()
    {
        _layout.Children.Clear();
        _layout.ColumnDefinitions.Clear();

        if (_sidebarCollapsed)
        {
            if (_attachViewModel.DockSide == AttachDockSide.Left)
            {
                _layout.ColumnDefinitions.Add(new ColumnDefinition(44, GridUnitType.Pixel));
                _layout.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                AddToColumn(CreateCollapsedSidebar(), 0);
                AddToColumn(_video, 1);
            }
            else
            {
                _layout.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
                _layout.ColumnDefinitions.Add(new ColumnDefinition(44, GridUnitType.Pixel));
                AddToColumn(_video, 0);
                AddToColumn(CreateCollapsedSidebar(), 1);
            }

            return;
        }

        if (_attachViewModel.DockSide == AttachDockSide.Left)
            _layout.ColumnDefinitions.Add(new ColumnDefinition(300, GridUnitType.Pixel));
        else
            _layout.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

        _layout.ColumnDefinitions.Add(new ColumnDefinition(4, GridUnitType.Pixel));

        if (_attachViewModel.DockSide == AttachDockSide.Left)
            _layout.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        else
            _layout.ColumnDefinitions.Add(new ColumnDefinition(300, GridUnitType.Pixel));

        var splitter = new GridSplitter
        {
            Width = 4,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch
        };

        if (_attachViewModel.DockSide == AttachDockSide.Left)
        {
            AddToColumn(CreateSidebarHost(), 0);
            AddToColumn(splitter, 1);
            AddToColumn(_video, 2);
        }
        else
        {
            AddToColumn(_video, 0);
            AddToColumn(splitter, 1);
            AddToColumn(CreateSidebarHost(), 2);
        }
    }

    private Control CreateSidebarHost()
    {
        if (_sidebarHost is not null)
            return _sidebarHost;

        var host = new DockPanel();
        var collapse = new Button
        {
            Content = "☰",
            Padding = new Thickness(8, 4),
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
            Margin = new Thickness(8, 6, 8, 0)
        };
        collapse.Click += (_, _) =>
        {
            _sidebarCollapsed = true;
            BuildLayout();
            _video.Focus();
        };
        DockPanel.SetDock(collapse, Dock.Top);
        host.Children.Add(collapse);
        host.Children.Add(_attachPanel);
        _sidebarHost = host;
        return _sidebarHost;
    }

    private Control CreateCollapsedSidebar()
    {
        var panel = new Border
        {
            Background = new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.FromRgb(31, 34, 39)),
            Child = new Button
            {
                Content = "☰",
                Padding = new Thickness(8),
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 0, 0)
            }
        };
        if (panel.Child is Button button)
        {
            button.Click += (_, _) =>
            {
                _sidebarCollapsed = false;
                BuildLayout();
                _video.Focus();
            };
        }

        return panel;
    }

    private Control CreateStatusBar()
    {
        var bar = new DockPanel
        {
            MinHeight = 34,
            Background = new global::Avalonia.Media.SolidColorBrush(global::Avalonia.Media.Color.FromRgb(26, 28, 32)),
            LastChildFill = true
        };

        var controls = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(8, 4)
        };
        controls.Children.Add(CreateStatusButton("Pause", () => _hostClient.PauseAsync().AsTask()));
        controls.Children.Add(CreateStatusButton("Resume", () => _hostClient.ResumeAsync().AsTask()));
        controls.Children.Add(CreateStatusButton("+1 cyc", () => _hostClient.StepCycleAsync(1).AsTask()));
        controls.Children.Add(CreateStatusButton("+1 frm", () => _hostClient.StepFrameAsync(1).AsTask()));
        controls.Children.Add(CreateStatusButton("-1 cyc", () => _hostClient.RewindCycleAsync(1).AsTask()));
        controls.Children.Add(CreateStatusButton("-1 frm", () => _hostClient.RewindFrameAsync(1).AsTask()));
        controls.Children.Add(CreateStatusButton("Cold", () => _hostClient.ColdResetAsync().AsTask()));
        controls.Children.Add(CreateStatusButton("Warm", () => _hostClient.WarmResetAsync().AsTask()));
        controls.Children.Add(CreateStatusButton("Run 8", () => _hostClient.ResetAndAutostartDrive8Async().AsTask()));
        DockPanel.SetDock(controls, Dock.Right);
        bar.Children.Add(controls);

        _statusText.Margin = new Thickness(10, 7, 8, 4);
        _statusText.FontSize = 12;
        _statusText.Text = "Power ? | Run ? | Limiter 100% | FPS 0.0 | Clock 0.000 MHz | Cycle 0 | PC 0000";
        _statusText.TextTrimming = global::Avalonia.Media.TextTrimming.CharacterEllipsis;
        bar.Children.Add(_statusText);

        return bar;
    }

    private Button CreateStatusButton(string label, Func<Task<Protocol.EmulatorCommandResponse>> action)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(7, 3),
            MinHeight = 24
        };
        button.Click += async (_, _) =>
        {
            try
            {
                var response = await action().ConfigureAwait(true);
                ApplyStatus(response.EmulatorStatus, response.Status);
                _video.Focus();
            }
            catch (Exception ex)
            {
                _statusText.Text = ex.Message;
            }
        };
        return button;
    }

    private void AddToColumn(Control control, int column, int columnSpan = 1)
    {
        Grid.SetColumn(control, column);
        Grid.SetColumnSpan(control, columnSpan);
        _layout.Children.Add(control);
    }

    private async Task RefreshFrameAsync()
    {
        try
        {
            var frame = _localVideoFrameSource is null
                ? await _hostClient.GetFrameAsync().ConfigureAwait(true)
                : await GetLocalFrameAsync().ConfigureAwait(true);

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
        if (!rpcStatus.IsSuccess)
        {
            _statusText.Text = rpcStatus.Message;
            return;
        }

        if (status is null)
            return;

        var clockMhz = status.EffectiveClockHz / 1_000_000.0;
        string limiterText = status.LimiterRatePercent > 0 && status.LimiterRatePercent < 1000
            ? $"Limiter {status.LimiterRatePercent:0}%"
            : "WARP";

        _statusText.Text =
            $"Power {status.PowerState} | Run {status.RunState} | {limiterText} | " +
            $"FPS {status.MeasuredFramesPerSecond:0.0} | Clock {clockMhz:0.000} MHz ({status.EffectiveClockPercent:0}%) | " +
            $"Cycle {status.Cycle} | PC {status.MachineState.Pc:X4}";
    }

    private async void OnVideoKeyDown(object? sender, KeyEventArgs e)
    {
        // Warp toggle: Alt+W (or Ctrl+W as fallback), like classic VICE
        if ((e.Key == Key.W) && (e.KeyModifiers.HasFlag(KeyModifiers.Alt) || e.KeyModifiers.HasFlag(KeyModifiers.Control)))
        {
            if (_attachViewModel is not null)
            {
                _attachViewModel.IsWarpMode = !_attachViewModel.IsWarpMode;
                e.Handled = true;
                return;
            }
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

    private async ValueTask<Protocol.GetVideoFrameResponse> GetLocalFrameAsync()
    {
        if (string.IsNullOrWhiteSpace(_hostClient.SessionId))
            await _hostClient.ListMediaAsync().ConfigureAwait(true);

        return string.IsNullOrWhiteSpace(_hostClient.SessionId)
            ? new Protocol.GetVideoFrameResponse(Protocol.RpcStatus.Unavailable("No emulator session is available."), null)
            : await _localVideoFrameSource!.GetFrameAsync(_hostClient.SessionId).ConfigureAwait(true);
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
