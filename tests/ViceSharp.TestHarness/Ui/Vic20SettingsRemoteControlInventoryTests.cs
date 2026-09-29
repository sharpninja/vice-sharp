using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Avalonia.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Avalonia.Views;
using Xunit;
using AvaloniaApp = ViceSharp.Avalonia.App;

namespace ViceSharp.TestHarness.Ui;

/// <summary>
/// FR: FR-UISETTINGS-001, FR: FR-VIC20-002, TEST-UISET-002.
/// RemoteControl inventory: every Settings control has a stable AutomationId
/// and is mutable when ALLOW_ACTIONS is on.
/// </summary>
public sealed class Vic20SettingsRemoteControlInventoryTests
{
    internal static readonly string[] SettingsInventoryAutomationIds =
    [
        "Settings.MachineProfile",
        "Settings.Vic20MemoryPreset",
        "Settings.Vic20Blk0",
        "Settings.Vic20Blk1",
        "Settings.Vic20Blk2",
        "Settings.Vic20Blk3",
        "Settings.Vic20Blk5",
        "Settings.ExpansionCartKind",
        "Settings.ExpansionCartPreset",
        "Settings.ExpansionCartWriteBack",
        "Settings.FileSystemIecRootPath",
        "Settings.FileSystemIecUnit",
        "Limiter.WarpToggle",
        "Limiter.SpeedCycle",
        "Settings.LimiterRate",
        "Settings.PacingStrategy",
        "Settings.Renderer",
        "Settings.DisplayScale",
        "Settings.CropMode",
        "Settings.AspectMode",
        "Settings.Palette",
        "Settings.AudioMode",
        "Settings.InputMode",
        "Settings.PrimaryJoystickPort",
        "Settings.SwapJoystickPorts",
        "Settings.ResourceMode",
        "Settings.SaveSettingsOnExit",
        "Settings.SaveTransientValuesOnExit",
        "Settings.Validate",
        "Settings.Apply",
        "Settings.Revert",
        "Settings.ApplyRestart",
    ];

    /// <summary>
    /// FR: FR-UISETTINGS-001, TEST-UISET-002.
    /// Use case: RemoteControl SetProperty is deny-by-default until ALLOW_ACTIONS.
    /// Acceptance: ApplyRemoteControlActionGates(false) leaves the allow-list empty.
    /// </summary>
    [Fact]
    public void ApplyRemoteControlActionGates_WhenActionsOff_LeavesAllowListEmpty()
    {
        var options = new AvaloniaRemoteControlOptions();
        AvaloniaApp.ApplyRemoteControlActionGates(options, allowActions: false);
        options.AllowRemoteActions.Should().BeFalse();
        options.AllowedMutableProperties.Should().BeEmpty();
    }

    /// <summary>
    /// FR: FR-UISETTINGS-001, TEST-UISET-002.
    /// Use case: Live Settings mutations need IsChecked/SelectedItem/SelectedIndex/Value/Text.
    /// Acceptance: ApplyRemoteControlActionGates(true) allow-lists those property names.
    /// </summary>
    [Fact]
    public void ApplyRemoteControlActionGates_WhenActionsOn_AllowsCheckedSelectedValueText()
    {
        var options = new AvaloniaRemoteControlOptions();
        AvaloniaApp.ApplyRemoteControlActionGates(options, allowActions: true);
        options.AllowRemoteActions.Should().BeTrue();
        options.AllowedMutableProperties.Should().Contain(nameof(CheckBox.IsChecked));
        options.AllowedMutableProperties.Should().Contain(nameof(ComboBox.SelectedIndex));
        options.AllowedMutableProperties.Should().Contain(nameof(ComboBox.SelectedItem));
        options.AllowedMutableProperties.Should().Contain(nameof(RangeBase.Value));
        options.AllowedMutableProperties.Should().Contain(nameof(TextBox.Text));
    }

    /// <summary>
    /// FR: FR-UISETTINGS-001, TEST-UISET-002.
    /// Use case: AXAML must expose Settings.* ids for the RemoteControl client.
    /// Acceptance: SettingsView.axaml contains every inventory AutomationId except Sidebar.*.
    /// </summary>
    [Fact]
    public void SettingsViewAxaml_DeclaresInventoryAutomationIds()
    {
        var path = Path.Combine(RepoRoot, "src", "ViceSharp.Avalonia", "Views", "SettingsView.axaml");
        var text = File.ReadAllText(path);
        var missing = SettingsInventoryAutomationIds
            .Where(id => !text.Contains($"AutomationId=\"{id}\"", StringComparison.Ordinal))
            .ToArray();
        missing.Should().BeEmpty($"SettingsView.axaml missing AutomationId: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// FR: FR-UISETTINGS-001, TEST-UISET-002.
    /// Use case: InvokeClick on TabItem did not select Settings; the tab needs an id.
    /// Acceptance: AttachPanelView assigns Sidebar.Settings and Sidebar.Tabs.
    /// </summary>
    [Fact]
    public void AttachPanelViewSource_DeclaresSidebarSettingsAutomationIds()
    {
        var path = Path.Combine(RepoRoot, "src", "ViceSharp.Avalonia", "Views", "AttachPanelView.cs");
        var text = File.ReadAllText(path);
        text.Should().Contain("Sidebar.Settings");
        text.Should().Contain("Sidebar.Tabs");
    }

    /// <summary>
    /// FR: FR-UISETTINGS-001, FR: FR-VIC20-002, TEST-UISET-002.
    /// Use case: Headless tree snapshot of SettingsView with VIC-20 selected.
    /// Acceptance: Every inventory AutomationId is present on a node.
    /// </summary>
    [AvaloniaFact]
    public async Task RemoteControl_SettingsView_ExposesInventoryAutomationIds()
    {
        var (window, _, snapshot) = await ShowSettingsAsync(allowActions: false);
        try
        {
            var missing = SettingsInventoryAutomationIds
                .Where(id => snapshot.Nodes.All(node => node.AutomationId != id))
                .ToArray();
            missing.Should().BeEmpty($"snapshot missing AutomationId: {string.Join(", ", missing)}");
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// FR: FR-UISETTINGS-001, TEST-UISET-002.
    /// Use case: Fail-closed mutation when the allow-list is empty.
    /// Acceptance: SetProperty IsChecked on Settings.Vic20Blk0 does not succeed.
    /// </summary>
    [AvaloniaFact]
    public async Task RemoteControl_SettingsView_DeniedMutation_WhenAllowListEmpty()
    {
        var (window, vm, snapshot) = await ShowSettingsAsync(allowActions: false);
        try
        {
            var node = snapshot.Nodes.Single(n => n.AutomationId == "Settings.Vic20Blk0");
            var options = Options.Create(new AvaloniaRemoteControlOptions());
            var provider = new AvaloniaControlTreeSnapshotProvider(options, new InlineRemoteControlDispatcher());
            var mutation = new RemoteControlPropertyMutationService(
                provider,
                options,
                new InlineRemoteControlDispatcher(),
                NullLogger<RemoteControlPropertyMutationService>.Instance);

            var result = await mutation.SetPropertyAsync(node.Id, nameof(CheckBox.IsChecked), "true");
            result.Succeeded.Should().BeFalse("deny-by-default must reject IsChecked");
            vm.Vic20Blk0.Should().BeFalse();
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// FR: FR-UISETTINGS-001, FR: FR-VIC20-002, TEST-UISET-002.
    /// Use case: Drive every mutable Settings inventory control through RemoteControl.
    /// Acceptance: Each SetProperty succeeds and the snapshot (or VM for object combos)
    /// reflects the new IsChecked / SelectedItem / SelectedIndex / Value / Text.
    /// </summary>
    [AvaloniaFact]
    public async Task RemoteControl_SettingsView_MutatesInventoryControls_WhenActionsAllowed()
    {
        var (window, vm, _) = await ShowSettingsAsync(allowActions: true);
        try
        {
            var options = new AvaloniaRemoteControlOptions();
            AvaloniaApp.ApplyRemoteControlActionGates(options, allowActions: true);
            var wrapped = Options.Create(options);
            var provider = new AvaloniaControlTreeSnapshotProvider(wrapped, new InlineRemoteControlDispatcher());
            var mutation = new RemoteControlPropertyMutationService(
                provider,
                wrapped,
                new InlineRemoteControlDispatcher(),
                NullLogger<RemoteControlPropertyMutationService>.Instance);

            await MutateAsync(window, provider, mutation, "Settings.MachineProfile", nameof(ComboBox.SelectedIndex), "4");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedMachineProfile.Id.Should().Be("vic20ntsc");

            await MutateAsync(window, provider, mutation, "Settings.Vic20MemoryPreset", nameof(ComboBox.SelectedIndex), "5");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedVic20MemoryPreset.Should().Be("All");
            vm.Vic20Blk0.Should().BeTrue();
            vm.Vic20Blk1.Should().BeTrue();
            vm.Vic20Blk2.Should().BeTrue();
            vm.Vic20Blk3.Should().BeTrue();
            vm.Vic20Blk5.Should().BeTrue();

            await MutateAsync(window, provider, mutation, "Settings.ExpansionCartKind", nameof(ComboBox.SelectedIndex), "1");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedExpansionCartKind.Should().Be("fe3");

            await MutateAsync(window, provider, mutation, "Settings.ExpansionCartPreset", nameof(ComboBox.SelectedIndex), "1");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedExpansionCartPreset.Should().Be("flash");

            await MutateAndAssertChecked(window, vm, provider, mutation, "Settings.ExpansionCartWriteBack", expected: true);
            vm.ExpansionCartWriteBack.Should().BeTrue();

            await MutateAndAssertProperty(window, provider, mutation, "Settings.FileSystemIecRootPath", nameof(TextBox.Text), @"C:\vic20-uiec-test");
            Dispatcher.UIThread.RunJobs();
            vm.FileSystemIecRootPath.Should().Be(@"C:\vic20-uiec-test");

            await MutateAsync(window, provider, mutation, "Settings.FileSystemIecUnit", nameof(NumericUpDown.Value), "10");
            Dispatcher.UIThread.RunJobs();
            vm.FileSystemIecUnit.Should().Be(10);

            await MutateAndAssertChecked(window, vm, provider, mutation, "Limiter.WarpToggle", expected: true);
            vm.IsWarpMode.Should().BeTrue();

            await MutateAsync(window, provider, mutation, "Settings.LimiterRate", nameof(RangeBase.Value), "200");
            Dispatcher.UIThread.RunJobs();
            vm.LimiterRatePercent.Should().Be(200);

            await MutateAsync(window, provider, mutation, "Settings.PacingStrategy", nameof(ComboBox.SelectedIndex), "0");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedPacingStrategy.Should().Be("Semaphore");

            await MutateAsync(window, provider, mutation, "Settings.Renderer", nameof(ComboBox.SelectedIndex), "1");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedRenderer.Should().Be("Software");

            await MutateAsync(window, provider, mutation, "Settings.DisplayScale", nameof(ComboBox.SelectedIndex), "0");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedDisplayScale.Should().Be("1x");

            await MutateAsync(window, provider, mutation, "Settings.CropMode", nameof(ComboBox.SelectedIndex), "0");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedCropMode.Should().Be("Full frame");

            await MutateAsync(window, provider, mutation, "Settings.AspectMode", nameof(ComboBox.SelectedIndex), "0");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedAspectMode.Should().Be("Square pixels");

            await MutateAsync(window, provider, mutation, "Settings.Palette", nameof(ComboBox.SelectedIndex), "1");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedPalette.Should().Be("Pepto");

            await MutateAsync(window, provider, mutation, "Settings.AudioMode", nameof(ComboBox.SelectedIndex), "1");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedAudioMode.Should().Be("Muted");

            await MutateAsync(window, provider, mutation, "Settings.InputMode", nameof(ComboBox.SelectedIndex), "1");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedInputMode.Should().Be("Keyboard only");

            await MutateAsync(window, provider, mutation, "Settings.PrimaryJoystickPort", nameof(ComboBox.SelectedIndex), "1");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedPrimaryJoystickPort.Should().Be("Joystick 1");

            await MutateAndAssertChecked(window, vm, provider, mutation, "Settings.SwapJoystickPorts", expected: true);
            vm.SwapJoystickPorts.Should().BeTrue();

            await MutateAsync(window, provider, mutation, "Settings.ResourceMode", nameof(ComboBox.SelectedIndex), "1");
            Dispatcher.UIThread.RunJobs();
            vm.SelectedResourceMode.Should().Be("Use configured paths");

            await MutateAndAssertChecked(window, vm, provider, mutation, "Settings.SaveSettingsOnExit", expected: true);
            vm.SaveSettingsOnExit.Should().BeTrue();

            await MutateAndAssertChecked(window, vm, provider, mutation, "Settings.SaveTransientValuesOnExit", expected: true);
            vm.SaveTransientValuesOnExit.Should().BeTrue();

            var after = await provider.CaptureSnapshotAsync(window);
            foreach (var id in SettingsInventoryAutomationIds)
            {
                after.Nodes.Should().Contain(node => node.AutomationId == id, id);
            }
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>
    /// FR: FR-UISETTINGS-001, TEST-UISET-002.
    /// Use case: Settings tab must be selectable without relying on TabItem InvokeClick.
    /// Acceptance: Sidebar.Tabs SelectedIndex=1 selects Settings; Sidebar.Settings exists.
    /// </summary>
    [AvaloniaFact]
    public async Task RemoteControl_AttachPanel_SelectsSettingsTab_BySelectedIndex()
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var panel = new AttachPanelView(vm);
        var window = new Window { Content = panel, Width = 420, Height = 720 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var options = new AvaloniaRemoteControlOptions();
        AvaloniaApp.ApplyRemoteControlActionGates(options, allowActions: true);
        var wrapped = Options.Create(options);
        var provider = new AvaloniaControlTreeSnapshotProvider(wrapped, new InlineRemoteControlDispatcher());
        var snapshot = await provider.CaptureSnapshotAsync(window);

        snapshot.Nodes.Should().Contain(n => n.AutomationId == "Sidebar.Settings");
        var tabs = snapshot.Nodes.Single(n => n.AutomationId == "Sidebar.Tabs");
        var mutation = new RemoteControlPropertyMutationService(
            provider,
            wrapped,
            new InlineRemoteControlDispatcher(),
            NullLogger<RemoteControlPropertyMutationService>.Instance);

        var result = await mutation.SetPropertyAsync(tabs.Id, nameof(TabControl.SelectedIndex), "1");
        result.Succeeded.Should().BeTrue(MutationError(result));
        Dispatcher.UIThread.RunJobs();
        vm.ActiveTab.Should().Be(SidebarTab.Settings);
        window.Close();
    }

    private static async Task MutateAndAssertChecked(
        Window window,
        AttachPanelViewModel vm,
        AvaloniaControlTreeSnapshotProvider provider,
        RemoteControlPropertyMutationService mutation,
        string automationId,
        bool expected)
    {
        await MutateAsync(window, provider, mutation, automationId, nameof(CheckBox.IsChecked), expected ? "true" : "false");
        Dispatcher.UIThread.RunJobs();
        var snapshot = await provider.CaptureSnapshotAsync(window);
        var node = snapshot.Nodes.Single(n => n.AutomationId == automationId);
        ReadProperty(node, nameof(CheckBox.IsChecked)).Should().BeEquivalentTo(expected ? "True" : "False", automationId);
        _ = vm;
    }

    private static async Task MutateAndAssertProperty(
        Window window,
        AvaloniaControlTreeSnapshotProvider provider,
        RemoteControlPropertyMutationService mutation,
        string automationId,
        string property,
        string value)
    {
        await MutateAsync(window, provider, mutation, automationId, property, value);
        Dispatcher.UIThread.RunJobs();
        var snapshot = await provider.CaptureSnapshotAsync(window);
        var node = snapshot.Nodes.Single(n => n.AutomationId == automationId);
        ReadProperty(node, property).Should().Be(value, $"{automationId}.{property}");
    }

    private static async Task MutateAsync(
        Window window,
        AvaloniaControlTreeSnapshotProvider provider,
        RemoteControlPropertyMutationService mutation,
        string automationId,
        string property,
        string value)
    {
        var snapshot = await provider.CaptureSnapshotAsync(window);
        var node = snapshot.Nodes.FirstOrDefault(n => n.AutomationId == automationId);
        node.Should().NotBeNull(automationId);
        var result = await mutation.SetPropertyAsync(node!.Id, property, value);
        result.Succeeded.Should().BeTrue($"{automationId}.{property}={value}: {MutationError(result)}");
    }

    private static string? ReadProperty(RemoteControlNodeSnapshot node, string name)
        => node.Properties.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.Ordinal))?.Value;

    private static string MutationError(object result)
    {
        foreach (var name in new[] { "ErrorMessage", "Message", "Error", "FailureReason", "Detail" })
        {
            var value = result.GetType().GetProperty(name)?.GetValue(result) as string;
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return result.ToString() ?? result.GetType().Name;
    }

    private static async Task<(Window Window, AttachPanelViewModel Vm, RemoteControlTreeSnapshot Snapshot)> ShowSettingsAsync(bool allowActions)
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        vm.SelectedMachineProfile = vm.MachineProfiles.Single(p => p.Id == "vic20ntsc");
        var view = new SettingsView { DataContext = vm };
        var window = new Window { Content = view, Width = 420, Height = 900 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Measure(new Size(420, 900));
        window.Arrange(new Rect(0, 0, 420, 900));
        Dispatcher.UIThread.RunJobs();

        var options = new AvaloniaRemoteControlOptions();
        if (allowActions)
            AvaloniaApp.ApplyRemoteControlActionGates(options, allowActions: true);
        var provider = new AvaloniaControlTreeSnapshotProvider(Options.Create(options), new InlineRemoteControlDispatcher());
        var snapshot = await provider.CaptureSnapshotAsync(window);
        return (window, vm, snapshot);
    }

    private static string RepoRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
        }
    }
}
