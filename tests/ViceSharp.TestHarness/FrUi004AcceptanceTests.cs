namespace ViceSharp.TestHarness;

using global::Avalonia.Controls;
using global::Avalonia.Headless.XUnit;
using global::Avalonia.VisualTree;
using NSubstitute;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Avalonia.Views;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-UI-004 acceptance tests for the docked and popped monitor.
/// Iteration 1. One view model is shared. Focus and a second session
/// are part of the criteria.
/// </summary>
public sealed class FrUi004AcceptanceTests
{
    /// <summary>
    /// FR: FR-UI-004.
    /// Use case: The monitor control executes a command through the host
    /// boundary and shows the host output.
    /// Acceptance: ExecuteMonitorCommandAsync sends the command text and
    /// appends the host output to MonitorOutput.
    /// </summary>
    [Fact]
    public async Task MonitorCommand_GoesThroughTheHostAndShowsOutput()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.SessionId.Returns("monitor-session");
        host.ExecuteMonitorCommandAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MonitorCommandResponse(RpcStatus.Ok(), "PC: 0000", null));
        var vm = new AttachPanelViewModel(host)
        {
            MonitorCommand = "r"
        };

        await vm.ExecuteMonitorCommandAsync(TestContext.Current.CancellationToken);

        await host.Received(1).ExecuteMonitorCommandAsync("r", Arg.Any<CancellationToken>());
        Assert.Contains("PC: 0000", vm.MonitorOutput, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR: FR-UI-004.
    /// Use case: The monitor can sit in the sidebar or in another window
    /// without a second emulator session.
    /// Acceptance: The docked panel and the popped panel are different
    /// controls and both use the same view model and host client.
    /// </summary>
    [AvaloniaFact]
    public void DockedAndPoppedPanels_ShareOneViewModel()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.SessionId.Returns("monitor-session");
        var vm = new AttachPanelViewModel(host);
        var panel = new AttachPanelView(vm);

        var docked = panel.CreateMonitorPanel(includePopOut: true);
        var popped = panel.CreateMonitorPanel(includePopOut: false);

        Assert.NotSame(docked, popped);
        Assert.Same(vm, panel.ViewModel);
        Assert.Contains("Pop out", ButtonLabels(docked), StringComparer.Ordinal);
        Assert.DoesNotContain("Pop out", ButtonLabels(popped), StringComparer.Ordinal);
    }

    /// <summary>
    /// FR: FR-UI-004.
    /// Use case: Output typed in one place is the output shown in the other.
    /// Acceptance: Changing MonitorOutput updates the text box in both panels.
    /// </summary>
    [AvaloniaFact]
    public async Task DockedAndPoppedOutput_StaySynchronized()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.SessionId.Returns("monitor-session");
        host.ExecuteMonitorCommandAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new MonitorCommandResponse(RpcStatus.Ok(), "synced-line", null));
        var vm = new AttachPanelViewModel(host)
        {
            MonitorCommand = "r"
        };
        var panel = new AttachPanelView(vm);
        var docked = panel.CreateMonitorPanel(includePopOut: true);
        var popped = panel.CreateMonitorPanel(includePopOut: false);

        await vm.ExecuteMonitorCommandAsync(TestContext.Current.CancellationToken);

        Assert.Contains("synced-line", OutputText(docked), StringComparison.Ordinal);
        Assert.Contains("synced-line", OutputText(popped), StringComparison.Ordinal);
    }

    /// <summary>
    /// FR: FR-UI-004.
    /// Use case: The monitor takes keyboard focus only while the user is
    /// in its command box.
    /// Acceptance: Showing the panel does not focus the command box.
    /// </summary>
    [AvaloniaFact]
    public void MonitorPanel_DoesNotTakeFocusWhenShown()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.SessionId.Returns("monitor-session");
        var panel = new AttachPanelView(new AttachPanelViewModel(host));
        var docked = panel.CreateMonitorPanel(includePopOut: true);
        var window = new Window { Content = docked, Width = 400, Height = 400 };
        window.Show();

        var command = docked.GetVisualDescendants().OfType<TextBox>().Single(box => box.IsReadOnly == false);
        Assert.False(command.IsFocused);
    }

    private static IEnumerable<string> ButtonLabels(Control root)
    {
        return root.GetVisualDescendants().OfType<Button>().Select(button => button.Content as string ?? "");
    }

    private static string OutputText(Control root)
    {
        return root.GetVisualDescendants().OfType<TextBox>().Single(box => box.IsReadOnly).Text ?? "";
    }
}
