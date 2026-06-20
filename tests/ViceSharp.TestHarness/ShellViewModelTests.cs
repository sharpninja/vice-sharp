namespace ViceSharp.TestHarness;

using System.Threading;
using NSubstitute;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-UIMENUBAR-001 / TEST-UIMENUBAR-001: verifies the shell menu/transport
/// command surface (<see cref="ShellViewModel"/>) routes to the host protocol
/// client and the panel view-model. Uses a mock <see cref="IHostProtocolClient"/>
/// so the menu wiring is provable without constructing the Avalonia window.
/// </summary>
public sealed class ShellViewModelTests
{
    private static IHostProtocolClient CreateHost()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.PauseAsync(Arg.Any<CancellationToken>()).Returns(Command());
        host.ResumeAsync(Arg.Any<CancellationToken>()).Returns(Command());
        host.StepCycleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Command());
        host.StepFrameAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Command());
        host.RewindCycleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Command());
        host.RewindFrameAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Command());
        host.ColdResetAsync(Arg.Any<CancellationToken>()).Returns(Command());
        host.WarmResetAsync(Arg.Any<CancellationToken>()).Returns(Command());
        host.ResetAndAutostartDrive8Async(Arg.Any<CancellationToken>()).Returns(Command());
        host.UpdateSettingsAsync(Arg.Any<UpdateSettingsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<UpdateSettingsResponse>(
                new UpdateSettingsResponse(RpcStatus.Ok(), null, System.Array.Empty<SettingApplyDiagnosticDto>())));
        host.DetachMediaAsync(Arg.Any<MediaSlot>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<DetachMediaResponse>(new DetachMediaResponse(RpcStatus.Ok(), null)));
        host.AttachMediaAsync(Arg.Any<MediaSlot>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<AttachMediaResponse>(new AttachMediaResponse(RpcStatus.Ok(), null)));
        return host;
    }

    private static ValueTask<EmulatorCommandResponse> Command()
        => new(new EmulatorCommandResponse(RpcStatus.Ok(), null));

    private static (ShellViewModel Shell, IHostProtocolClient Host, AttachPanelViewModel Panel) CreateShell()
    {
        var host = CreateHost();
        var panel = new AttachPanelViewModel(host);
        return (new ShellViewModel(host, panel), host, panel);
    }

    /// <summary>
    /// FR: FR-MED-001, TR: TR-MEDIA-001, TEST-MED-001 (x64sc screenshot parity).
    /// Use case: the Snapshot -&gt; Save screenshot menu action must drive the host capture
    ///   RPC with the chosen file path and image format.
    /// Acceptance: ShellViewModel.CaptureScreenshotAsync forwards the path and format exactly
    ///   once to IHostProtocolClient.CaptureFrameAsync and returns its response.
    /// </summary>
    [Fact]
    public async Task CaptureScreenshotAsync_ForwardsPathAndFormat_ToHost()
    {
        var (shell, host, _) = CreateShell();
        host.CaptureFrameAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<CaptureFrameResponse>(
                new CaptureFrameResponse(RpcStatus.Ok(), new CaptureArtifactDto("shot.png", "png", 0))));

        var response = await shell.CaptureScreenshotAsync("shot.png", "png", TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        await host.Received(1).CaptureFrameAsync("shot.png", "png", Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FR: FR-UIMENUBAR-001, TR: TR-MVVM-001, TEST-UIMENUBAR-001.
    /// Use case: The Debug/transport menu items and the status-bar transport
    /// buttons must invoke the corresponding host protocol commands.
    /// Acceptance: Each transport method on the shell forwards exactly once to
    /// the matching IHostProtocolClient call.
    /// </summary>
    [Fact]
    public async Task TransportCommands_RouteToHost()
    {
        var (shell, host, _) = CreateShell();
        var ct = TestContext.Current.CancellationToken;

        await shell.PauseAsync(ct);
        await shell.ResumeAsync(ct);
        await shell.StepCycleAsync(1, ct);
        await shell.StepFrameAsync(1, ct);
        await shell.RewindCycleAsync(1, ct);
        await shell.RewindFrameAsync(1, ct);
        await shell.ColdResetAsync(ct);
        await shell.WarmResetAsync(ct);
        await shell.AutostartDrive8Async(ct);

        await host.Received(1).PauseAsync(Arg.Any<CancellationToken>());
        await host.Received(1).ResumeAsync(Arg.Any<CancellationToken>());
        await host.Received(1).StepCycleAsync(1, Arg.Any<CancellationToken>());
        await host.Received(1).StepFrameAsync(1, Arg.Any<CancellationToken>());
        await host.Received(1).RewindCycleAsync(1, Arg.Any<CancellationToken>());
        await host.Received(1).RewindFrameAsync(1, Arg.Any<CancellationToken>());
        await host.Received(1).ColdResetAsync(Arg.Any<CancellationToken>());
        await host.Received(1).WarmResetAsync(Arg.Any<CancellationToken>());
        await host.Received(1).ResetAndAutostartDrive8Async(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FR: FR-UIMENUBAR-001, TR: TR-MVVM-001, TEST-UIMENUBAR-001.
    /// Use case: The Settings menu's Warp and Swap-joysticks items must toggle
    /// the corresponding view-model state and push the change through the host
    /// settings pipeline.
    /// Acceptance: ToggleWarpAsync flips IsWarpMode and SwapJoysticksAsync flips
    /// SwapJoystickPorts, and each results in an UpdateSettings host call.
    /// </summary>
    [Fact]
    public async Task WarpAndSwapJoysticks_ToggleVmStateAndApply()
    {
        var (shell, host, panel) = CreateShell();
        var ct = TestContext.Current.CancellationToken;

        Assert.False(panel.IsWarpMode);
        await shell.ToggleWarpAsync(ct);
        Assert.True(panel.IsWarpMode);

        Assert.False(panel.SwapJoystickPorts);
        await shell.SwapJoysticksAsync(ct);
        Assert.True(panel.SwapJoystickPorts);

        await host.Received().UpdateSettingsAsync(Arg.Any<UpdateSettingsRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FR: FR-UIMENUBAR-001, FR: FR-DRVTRUE-001, TR: TR-MVVM-001, TEST-UIMENUBAR-001.
    /// Use case: The Settings menu's per-drive True Drive items must toggle that
    /// drive's True Drive selection, and must do nothing for non-drive slots.
    /// Acceptance: ToggleTrueDrive on Drive 8 flips its TrueDrive; on Tape it is
    /// a no-op (Tape has no True Drive support).
    /// </summary>
    [Fact]
    public void ToggleTrueDrive_FlipsDrivesOnly()
    {
        var (shell, _, panel) = CreateShell();
        var drive8 = panel.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);
        var tape = panel.Slots.Single(slot => slot.Slot == MediaSlot.Tape);

        Assert.False(drive8.TrueDrive);
        shell.ToggleTrueDrive(MediaSlot.Drive8);
        Assert.True(drive8.TrueDrive);

        shell.ToggleTrueDrive(MediaSlot.Tape);
        Assert.False(tape.TrueDrive);
    }

    /// <summary>
    /// FR: FR-UIMENUBAR-001, FR: FR-UIFLYOUT-001, TR: TR-MVVM-001, TEST-UIMENUBAR-001.
    /// Use case: Menu navigation items open the sidebar flyout and select the
    /// matching tab so the requested panel is visible.
    /// Acceptance: ShowSettings/ShowMonitor/ShowPeripherals each open the pane
    /// and set ActiveTab; ToggleSidebar and ToggleDockSide change the flyout.
    /// </summary>
    [Fact]
    public void Navigation_OpensPaneAndSelectsTab()
    {
        var (shell, _, panel) = CreateShell();

        shell.ShowSettings();
        Assert.True(panel.IsPaneOpen);
        Assert.Equal(SidebarTab.Settings, panel.ActiveTab);

        shell.ShowMonitor();
        Assert.Equal(SidebarTab.Monitor, panel.ActiveTab);

        shell.ShowPeripherals();
        Assert.Equal(SidebarTab.Peripherals, panel.ActiveTab);

        shell.ToggleSidebar();
        Assert.False(panel.IsPaneOpen);

        Assert.Equal(AttachDockSide.Left, panel.DockSide);
        shell.ToggleDockSide();
        Assert.Equal(AttachDockSide.Right, panel.DockSide);
    }

    /// <summary>
    /// FR: FR-UIMENUBAR-001, TR: TR-MVVM-001, TEST-UIMENUBAR-001.
    /// Use case: The File menu's Detach item must route to the host detach call
    /// for the chosen drive.
    /// Acceptance: DetachAsync(Drive8) invokes IHostProtocolClient.DetachMediaAsync
    /// for Drive 8.
    /// </summary>
    [Fact]
    public async Task DetachCommand_RoutesToHost()
    {
        var (shell, host, _) = CreateShell();

        await shell.DetachAsync(MediaSlot.Drive8, TestContext.Current.CancellationToken);

        await host.Received(1).DetachMediaAsync(MediaSlot.Drive8, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FR: FR-UIMENUBAR-001, TR: TR-MVVM-001, TEST-UIMENUBAR-001.
    /// Use case: The File menu's Attach item picks a file then routes to the
    /// host attach call for the chosen drive.
    /// Acceptance: With a file picker supplying a path, AttachAsync(Drive8)
    /// invokes IHostProtocolClient.AttachMediaAsync for Drive 8.
    /// </summary>
    [Fact]
    public async Task AttachCommand_PicksThenRoutesToHost()
    {
        var (shell, host, panel) = CreateShell();
        panel.FilePicker = _ => Task.FromResult<string?>(@"C:\does-not-exist\demo.d64");

        await shell.AttachAsync(MediaSlot.Drive8, TestContext.Current.CancellationToken);

        await host.Received(1).AttachMediaAsync(
            MediaSlot.Drive8, Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}
