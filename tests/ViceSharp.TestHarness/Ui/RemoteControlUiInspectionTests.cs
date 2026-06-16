using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Avalonia.Views;
using ViceSharp.Protocol;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(ViceSharp.TestHarness.Ui.HeadlessTestApp))]

namespace ViceSharp.TestHarness.Ui;

/// <summary>Minimal headless Avalonia application for [AvaloniaFact] UI tests.</summary>
public sealed class HeadlessTestApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<HeadlessTestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>
/// FR: FR-UIPERIPHERAL-001, FR: FR-DRVTRUE-001, TR: TR-UIAXAML-VIEWS-001,
/// TEST-UIPERIPHERAL-001, TEST-DRVTRUE-001.
/// Use case: This is the automated "app-launch visual gate" - it renders the
/// real reusable PeripheralCardView in a headless Avalonia app and drives it
/// through the embedded Avalonia.RemoteControl engine (the same snapshot +
/// mutation runtime exposed over gRPC), proving the redesigned AXAML renders
/// and its bindings are live.
/// Acceptance: A RemoteControl tree snapshot of the rendered card contains the
/// True Drive checkbox (by automation id) and the Attach button, and a
/// RemoteControl mutation of the checkbox's IsChecked flows through the two-way
/// binding to flip AttachSlotViewModel.TrueDrive from false to true.
/// </summary>
public sealed class RemoteControlUiInspectionTests
{
    [AvaloniaFact]
    public async Task RemoteControl_Snapshots_And_Drives_TrueDriveToggle_OnPeripheralCard()
    {
        var panel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var slot = panel.Slots.Single(s => s.Slot == MediaSlot.Drive8);
        var card = new PeripheralCardView { DataContext = slot, Panel = panel };
        var window = new Window { Content = card, Width = 320, Height = 460 };

        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(320, 460));
        window.Arrange(new Rect(0, 0, 320, 460));
        Dispatcher.UIThread.RunJobs();

        var rcOptions = new AvaloniaRemoteControlOptions();
        rcOptions.AllowedMutableProperties.Add(nameof(CheckBox.IsChecked));
        var options = Options.Create(rcOptions);
        var provider = new AvaloniaControlTreeSnapshotProvider(options, new InlineRemoteControlDispatcher());

        var snapshot = await provider.CaptureSnapshotAsync(window);

        var toggle = snapshot.Nodes.FirstOrDefault(node => node.AutomationId == "Drive.TrueDriveToggle");
        toggle.Should().NotBeNull("the redesigned PeripheralCardView must render the True Drive checkbox");
        snapshot.Nodes.Should().Contain(
            node => node.TypeName == "Button",
            "the reusable card renders its Attach/Eject buttons");

        slot.TrueDrive.Should().BeFalse();

        var mutation = new RemoteControlPropertyMutationService(
            provider,
            options,
            new InlineRemoteControlDispatcher(),
            NullLogger<RemoteControlPropertyMutationService>.Instance);

        var result = await mutation.SetPropertyAsync(toggle!.Id, nameof(CheckBox.IsChecked), "true");

        result.Succeeded.Should().BeTrue("IsChecked is allow-listed for mutation");
        slot.TrueDrive.Should().BeTrue(
            "driving the True Drive checkbox via RemoteControl must flow through the two-way binding to the view-model");
    }
}
