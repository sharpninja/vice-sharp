using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Avalonia.Views;
using ViceSharp.Abstractions;
using ViceSharp.Host.Services;
using Protocol = ViceSharp.Protocol;

namespace ViceSharp.Avalonia;

public partial class MainWindow : Window
{
    private readonly IHostProtocolClient _hostClient;
    private readonly InProcessGrpcHost? _localHost;
    private readonly ILocalVideoFrameSource? _localVideoFrameSource;
    private readonly AttachPanelViewModel _attachViewModel;
    private readonly ShellViewModel _shell;
    private readonly Persistence.SessionPersistence _persistence = new();
    private readonly StatusBarViewModel _statusBarViewModel = new();
    private readonly IecMonitorViewModel _iecMonitorViewModel = new();
    private readonly AttachPanelView _attachPanel;
    private readonly VideoSurface _video;
    private IDisposable? _warpModeSubscription;
    private bool _videoRecordingHotkeyActive;
    private DockPanel? _contentPanel;
    private ContentControl? _sidebarHost;
    private ContentControl? _videoHost;

    public MainWindow()
    {
        InitializeComponent();

        // Opt-in UI-thread pinning (TR-HOST-AFFINITY-001): VICESHARP_UI_CPU names the
        // logical CPU the Avalonia UI thread runs on, complementing VICESHARP_EMU_CPU
        // for the emulation worker. Unset/invalid -> scheduler-managed as before.
        ViceSharp.Host.Services.ThreadAffinity.TryPinCurrentThreadFromEnvironment("VICESHARP_UI_CPU");

        // Show the running build's semantic version in the title bar so a screen capture
        // always identifies which deployed build produced it (removes "did it deploy?" doubt).
        Title = $"ViceSharp {AppSemVer}";

        var hostConnection = CreateHostClient();
        _hostClient = hostConnection.Client;
        _localHost = hostConnection.LocalHost;
        _localVideoFrameSource = hostConnection.VideoFrameSource;
        if (_hostClient is GrpcHostProtocolClient grpcClient)
            grpcClient.SessionIdChanged += OnHostClientSessionIdChanged;
        _attachViewModel = new AttachPanelViewModel(_hostClient);
        _shell = new ShellViewModel(_hostClient, _attachViewModel);
        DataContext = _attachViewModel;

        _attachPanel = new AttachPanelView(_attachViewModel)
        {
            PickFileAsync = PickMediaFileAsync,
            PickKeyboardMapFileAsync = PickKeyboardMapFileAsync,
            PopOutMonitorRequested = OpenMonitorWindow
        };
        // Fixed natural size; a Viewbox (set on PART_VideoHost below) scales it uniformly so
        // the display control wraps the image tightly (no internal letterbox) and the sidebar
        // can fill right up to it.
        _video = new VideoSurface
        {
            Width = VideoSurface.SourceWidth,
            Height = VideoSurface.SourceHeight
        };
        _video.KeyDown += OnVideoKeyDown;
        _video.KeyUp += OnVideoKeyUp;
        AddHandler(KeyDownEvent, OnGlobalKeyDown, RoutingStrategies.Tunnel);

        // Inject the live views into the declarative shell's named hosts. The
        // sidebar pane and video content stay code-behind-owned for now; the
        // reusable PeripheralCardView / SidebarView land in S2.
        if (this.FindControl<Panel>("PART_StatusHost") is { } statusHost)
            statusHost.DataContext = _statusBarViewModel;

        if (this.FindControl<Views.IecMonitorView>("PART_IecMonitor") is { } iecMonitor)
            iecMonitor.DataContext = _iecMonitorViewModel;
        _sidebarHost = this.FindControl<ContentControl>("PART_SidebarHost");
        if (_sidebarHost is not null)
            _sidebarHost.Content = _attachPanel;
        _videoHost = this.FindControl<ContentControl>("PART_VideoHost");
        if (_videoHost is not null)
        {
            _videoHost.Content = new Viewbox
            {
                Stretch = global::Avalonia.Media.Stretch.Uniform,
                Child = _video
            };
            ConfigureVideoDropSurface(_videoHost);
        }
        _contentPanel = this.FindControl<DockPanel>("PART_ContentPanel");

        // The sidebar stretches to fill the width the aspect-sized display does not use.
        // Re-flow when the sidebar is collapsed/opened or flipped to the other edge.
        _attachViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AttachPanelViewModel.IsPaneOpen)
                or nameof(AttachPanelViewModel.PanePlacement)
                or nameof(AttachPanelViewModel.DockSide))
            {
                ApplyContentLayout();
            }
        };
        ApplyContentLayout();

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

    /// <summary>
    /// The running build's semantic version for the window title. Reads the assembly
    /// informational version (GitVersion-stamped) and strips any build metadata suffix.
    /// </summary>
    private static string AppSemVer
    {
        get
        {
            var assembly = typeof(MainWindow).Assembly;
            var raw = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "0.0.0";
            return FormatSemVer(raw);
        }
    }

    /// <summary>
    /// Reduce a raw informational version to its semantic-version core: drop GitVersion's
    /// "+Branch.Sha..." build metadata (keeping any "-prerelease" tag). Testable in isolation.
    /// </summary>
    public static string FormatSemVer(string rawVersion)
    {
        if (string.IsNullOrWhiteSpace(rawVersion))
            return "0.0.0";

        var plus = rawVersion.IndexOf('+');
        return plus >= 0 ? rawVersion[..plus] : rawVersion;
    }

    // Lay out the content panel: the emulator display is docked to an edge and aspect-sized
    // (VideoSurface.MeasureOverride), and the sidebar is the stretched fill (last) child that
    // consumes the rest. Flipping DockSide moves the display's dock edge; collapsing hides the
    // sidebar and reorders so the display becomes the fill child and covers the window.
    private void ApplyContentLayout()
    {
        if (_contentPanel is null || _sidebarHost is null || _videoHost is null)
            return;

        if (_attachViewModel.IsPaneOpen)
        {
            _sidebarHost.IsVisible = true;
            var sidebarLeft = _attachViewModel.DockSide == AttachDockSide.Left;
            DockPanel.SetDock(_videoHost, sidebarLeft ? Dock.Right : Dock.Left);
            MoveToFront(_videoHost); // display docked first; sidebar is last => fills/stretches
        }
        else
        {
            _sidebarHost.IsVisible = false;
            MoveToFront(_sidebarHost); // display is last => fills the window
        }
    }

    private void MoveToFront(Control child)
    {
        var children = _contentPanel!.Children;
        var index = children.IndexOf(child);
        if (index > 0)
            children.Move(index, 0);
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
        if (_hostClient is GrpcHostProtocolClient grpcClient)
            grpcClient.SessionIdChanged -= OnHostClientSessionIdChanged;

        if (_hostClient is IDisposable disposableClient)
            disposableClient.Dispose();

        _warpModeSubscription?.Dispose();
        _warpModeSubscription = null;

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

    private async void OnMenuSaveScreenshot(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var file = await ShowSaveFilePickerAsync(topLevel, new FilePickerSaveOptions
        {
            Title = "Save screenshot",
            SuggestedFileName = "vicesharp",
            DefaultExtension = "png",
            FileTypeChoices =
            [
                new FilePickerFileType("PNG image") { Patterns = ["*.png"] },
                new FilePickerFileType("BMP image") { Patterns = ["*.bmp"] }
            ]
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        var format = global::System.IO.Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        if (format is not ("png" or "bmp"))
            format = "png";

        try
        {
            var response = await _shell.CaptureScreenshotAsync(path, format).ConfigureAwait(true);
            if (!response.Status.IsSuccess)
                ApplyStatus(null, response.Status);
        }
        catch (Exception ex)
        {
            ApplyStatus(null, Protocol.RpcStatus.Unavailable(ex.Message));
        }
    }

    private async void OnMenuRecordSound(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Toggle: stop an active recording, otherwise pick a target and start one.
        if (_shell.IsRecordingSound)
        {
            await StopRecordingAsync(_shell.StopSoundRecordingAsync()).ConfigureAwait(true);
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var file = await ShowSaveFilePickerAsync(topLevel, new FilePickerSaveOptions
        {
            Title = "Record sound",
            SuggestedFileName = "vicesharp",
            DefaultExtension = "wav",
            FileTypeChoices = [new FilePickerFileType("WAV audio") { Patterns = ["*.wav"] }]
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var status = await _shell.StartSoundRecordingAsync(path).ConfigureAwait(true);
            if (!status.IsSuccess)
                ApplyStatus(null, status);
        }
        catch (Exception ex)
        {
            ApplyStatus(null, Protocol.RpcStatus.Unavailable(ex.Message));
        }
    }

    private async void OnMenuRecordVideoMp4(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => await RecordMuxedVideoAsync(captureMicrophone: false).ConfigureAwait(true);

    private async void OnMenuRecordVideoMp4WithMicrophone(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => await RecordMuxedVideoAsync(captureMicrophone: true).ConfigureAwait(true);

    private async global::System.Threading.Tasks.Task RecordMuxedVideoAsync(bool captureMicrophone)
    {
        // Toggle: stop an active recording, otherwise pick an output file and start
        // a muxed MP4 (H.264 + AAC) recording with sound, via ffmpeg. The narration
        // variant asks ffmpeg to add the host microphone as an extra mixed audio input.
        if (_shell.IsRecordingVideo)
        {
            await StopRecordingAsync(_shell.StopVideoRecordingAsync()).ConfigureAwait(true);
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var file = await ShowSaveFilePickerAsync(topLevel, new FilePickerSaveOptions
        {
            Title = captureMicrophone
                ? "Record video (MP4 + sound + microphone)"
                : "Record video (MP4 + sound)",
            SuggestedFileName = "vicesharp",
            DefaultExtension = "mp4",
            FileTypeChoices =
            [
                new FilePickerFileType("MP4 video") { Patterns = ["*.mp4"] },
                new FilePickerFileType("Matroska video") { Patterns = ["*.mkv"] },
                new FilePickerFileType("AVI video") { Patterns = ["*.avi"] }
            ]
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        var format = global::System.IO.Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        if (format is not ("mp4" or "mkv" or "avi"))
            format = "mp4";

        try
        {
            var status = await _shell.StartVideoRecordingAsync(
                path, format, captureMicrophone: captureMicrophone).ConfigureAwait(true);
            if (!status.IsSuccess)
                ApplyStatus(null, status);
        }
        catch (Exception ex)
        {
            ApplyStatus(null, Protocol.RpcStatus.Unavailable(ex.Message));
        }

        RefreshRecordingIndicator();
    }

    private void OnMenuRecordVideoBmpAll(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RecordBmpSequenceAsync(uniqueFrames: false);

    private void OnMenuRecordVideoBmpUnique(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => _ = RecordBmpSequenceAsync(uniqueFrames: true);

    private async global::System.Threading.Tasks.Task RecordBmpSequenceAsync(bool uniqueFrames)
    {
        // Toggle: stop an active recording, otherwise pick an output folder and
        // start a numbered-BMP frame sequence. "unique" deduplicates consecutive
        // identical frames; "all" writes one BMP per emulated frame.
        if (_shell.IsRecordingVideo)
        {
            await StopRecordingAsync(_shell.StopVideoRecordingAsync()).ConfigureAwait(true);
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var folders = await ShowOpenFolderPickerAsync(topLevel, new FolderPickerOpenOptions
        {
            Title = uniqueFrames
                ? "Choose output folder (unique BMP frames)"
                : "Choose output folder (all BMP frames)",
            AllowMultiple = false
        });

        var dir = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (string.IsNullOrWhiteSpace(dir))
            return;

        var options = new global::System.Collections.Generic.Dictionary<string, string>
        {
            ["frames"] = uniqueFrames ? "unique" : "all",
        };

        try
        {
            var status = await _shell.StartVideoRecordingAsync(dir, "bmpseq", options).ConfigureAwait(true);
            if (!status.IsSuccess)
                ApplyStatus(null, status);
        }
        catch (Exception ex)
        {
            ApplyStatus(null, Protocol.RpcStatus.Unavailable(ex.Message));
        }

        RefreshRecordingIndicator();
    }

    private async global::System.Threading.Tasks.Task StopRecordingAsync(ValueTask<Protocol.RpcStatus> stopOperation)
    {
        try
        {
            var status = await stopOperation.ConfigureAwait(true);
            if (!status.IsSuccess)
                ApplyStatus(null, status);
        }
        catch (Exception ex)
        {
            ApplyStatus(null, Protocol.RpcStatus.Unavailable(ex.Message));
        }
        finally
        {
            // Whatever the outcome, reflect the live recording state on the toolbar
            // Stop button (covers every stop path: MP4, BMP sequence, and sound).
            RefreshRecordingIndicator();
        }
    }

    // The toolbar Stop button: one obvious click to end a running video recording,
    // without hunting back through the Snapshot menu's toggle. Stops the active
    // video capture; the shell call is a safe no-op if nothing is recording.
    private async void OnStopRecording(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        => await StopRecordingAsync(_shell.StopVideoRecordingAsync()).ConfigureAwait(true);

    // Shows the Stop button while a video recording is active and hides it otherwise.
    // ShellViewModel is not observable, so the menu/stop handlers call this after each
    // start/stop instead of binding.
    private void RefreshRecordingIndicator()
    {
        if (this.FindControl<global::Avalonia.Controls.Button>("PART_StopRecording") is { } stop)
            stop.IsVisible = _shell.IsRecordingVideo;
    }

    private async Task<IStorageFile?> ShowSaveFilePickerAsync(TopLevel topLevel, FilePickerSaveOptions options)
    {
        if (!await PauseBeforeSystemPickerAsync().ConfigureAwait(true))
            return null;

        return await topLevel.StorageProvider.SaveFilePickerAsync(options).ConfigureAwait(true);
    }

    private async Task<IReadOnlyList<IStorageFile>> ShowOpenFilePickerAsync(TopLevel topLevel, FilePickerOpenOptions options)
    {
        if (!await PauseBeforeSystemPickerAsync().ConfigureAwait(true))
            return [];

        return await topLevel.StorageProvider.OpenFilePickerAsync(options).ConfigureAwait(true);
    }

    private async Task<IReadOnlyList<IStorageFolder>> ShowOpenFolderPickerAsync(TopLevel topLevel, FolderPickerOpenOptions options)
    {
        if (!await PauseBeforeSystemPickerAsync().ConfigureAwait(true))
            return [];

        return await topLevel.StorageProvider.OpenFolderPickerAsync(options).ConfigureAwait(true);
    }

    private void ConfigureVideoDropSurface(Control control)
    {
        DragDrop.SetAllowDrop(control, true);
        control.AddHandler(DragDrop.DragOverEvent, OnVideoHostDragOver);
        control.AddHandler(DragDrop.DropEvent, OnVideoHostDrop);
    }

    private static string? GetDroppedLocalFilePath(DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null)
            return null;

        foreach (var item in files)
        {
            if (item is IStorageFile file)
                return file.TryGetLocalPath();
        }

        return null;
    }

    private void OnVideoHostDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = string.IsNullOrWhiteSpace(GetDroppedLocalFilePath(e))
            ? DragDropEffects.None
            : DragDropEffects.Copy;
        e.Handled = true;
    }

    private async void OnVideoHostDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        var path = GetDroppedLocalFilePath(e);
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var status = await _shell.DropAndStartFileAsync(path).ConfigureAwait(true);
            if (!status.IsSuccess && !string.IsNullOrWhiteSpace(status.Message))
                _attachViewModel.ReportStatus(status.Message);
        }
        catch (Exception ex)
        {
            _attachViewModel.ReportStatus(ex.Message);
        }
        finally
        {
            _video.Focus();
        }
    }

    private async Task<bool> PauseBeforeSystemPickerAsync()
    {
        try
        {
            var response = await _shell.PauseAsync().ConfigureAwait(true);
            ApplyStatus(response.EmulatorStatus, response.Status);
            return response.Status.IsSuccess;
        }
        catch (Exception ex)
        {
            ApplyStatus(null, Protocol.RpcStatus.Unavailable(ex.Message));
            return false;
        }
    }

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

    private async void OnMenuCopyDebugAttachInfo(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var response = await _hostClient.GetStatusAsync().ConfigureAwait(true);
            ApplyStatus(response.EmulatorStatus, response.Status);
            var attachJson = _localHost is null
                ? string.Empty
                : await _localHost.ReadDebugAttachInfoAsync().ConfigureAwait(true);
            var text = DebugAttachInfoProvider.BuildClipboardText(attachJson, response.EmulatorStatus);
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
                await clipboard.SetTextAsync(text).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ApplyStatus(null, Protocol.RpcStatus.Unavailable(ex.Message));
        }
    }

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
            ApplyStatus(null, Protocol.RpcStatus.Unavailable(ex.Message));
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
                {
                    _video.UpdateFrom(_localVideoFrameSource, sessionId);
                    _localHost?.DiagnosticsState.MarkFrameUpdated();
                }
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
            _localHost?.DiagnosticsState.MarkStatusUpdated();
        }
        catch
        {
            // The attach panel already shows connectivity problems.
        }
    }

    private void ApplyStatus(Protocol.EmulatorStatusDto? status, Protocol.RpcStatus rpcStatus)
    {
        _statusBarViewModel.ApplyStatus(status, rpcStatus);
        _iecMonitorViewModel.ApplyStatus(status, rpcStatus);
        if (rpcStatus.IsSuccess && status is not null)
            _attachViewModel.ApplyStatus(status);
    }

    private async void OnHostClientSessionIdChanged(object? sender, string sessionId)
    {
        try
        {
            await UpdateDiagnosticsSessionAsync(sessionId).ConfigureAwait(true);
        }
        catch
        {
            // Diagnostics metadata must not destabilize the UI.
        }
    }

    private async Task UpdateDiagnosticsSessionAsync(string sessionId)
    {
        if (_localHost is null || string.IsNullOrWhiteSpace(sessionId))
            return;

        _warpModeSubscription?.Dispose();
        _warpModeSubscription = null;
        _statusBarViewModel.ResetWarpModeEvent();
        await _localHost.UpdateCurrentSessionAsync(sessionId).ConfigureAwait(true);
        _warpModeSubscription = _localHost.SubscribeWarpMode(sessionId, OnWarpModePublished);
    }

    private void OnWarpModePublished(WarpModeEvent warpMode)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _statusBarViewModel.ApplyWarpMode(warpMode);
            _attachViewModel.ApplyWarpMode(warpMode);
        });
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

    private async void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsVideoRecordingHotkey(e))
            return;

        e.Handled = true;
        if (_videoRecordingHotkeyActive)
            return;

        _videoRecordingHotkeyActive = true;
        try
        {
            await RecordMuxedVideoAsync(captureMicrophone: false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ApplyStatus(null, Protocol.RpcStatus.Unavailable(ex.Message));
        }
        finally
        {
            _videoRecordingHotkeyActive = false;
        }
    }

    private static bool IsVideoRecordingHotkey(KeyEventArgs e)
    {
        const KeyModifiers required = KeyModifiers.Control | KeyModifiers.Shift;
        return e.Key == Key.R
            && (e.KeyModifiers & required) == required
            && (e.KeyModifiers & KeyModifiers.Alt) == 0;
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

        var files = await ShowOpenFilePickerAsync(topLevel, new FilePickerOpenOptions
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

        var files = await ShowOpenFilePickerAsync(topLevel, new FilePickerOpenOptions
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
        InProcessGrpcHost? LocalHost,
        ILocalVideoFrameSource? VideoFrameSource = null);
}
