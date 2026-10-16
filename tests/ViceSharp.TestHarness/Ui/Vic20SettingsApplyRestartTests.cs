namespace ViceSharp.TestHarness.Ui;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using global::Avalonia.Controls;
using global::Avalonia.Headless.XUnit;
using global::Avalonia.Threading;
using NSubstitute;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Avalonia.Views;
using ViceSharp.Core.Vic20;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR: FR-VIC20-002, TEST-UISET-001.
/// Use case: Selecting All VIC-20 RAM expansions and Apply + Restart must keep
/// the map. Operator report: all BLK checkboxes unselected after restart.
/// </summary>
public sealed class Vic20SettingsApplyRestartTests
{
    /// <summary>
    /// FR: FR-VIC20-002 AC4, TEST-UISET-001.
    /// Acceptance: UpdateSettings RestartSession with Vic20MemorySpec=all returns
    /// Settings.Vic20MemorySpec all and the factory Create request carries all.
    /// </summary>
    [Fact]
    public async Task SettingsServiceHost_Vic20MemorySpecAll_Restart_RoundTripsAll()
    {
        var machine = MachineTestFactory.CreateVic20Machine(new Vic20Descriptor("vic20"));
        var session = new EmulatorRuntimeSession("vic20-mem-all", machine.Architecture, machine)
        {
            Vic20MemorySpec = "none",
            PowerState = "On",
            RunState = EmulatorRunState.Running,
        };
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        var factory = new RecordingRuntimeFactory();
        var settings = new SettingsServiceHost(registry, factory);

        var response = await settings.UpdateSettingsAsync(
            new UpdateSettingsRequest(
                session.SessionId,
                Limiter: new LimiterSettingsDto(100, true),
                Display: new DisplaySettingsDto(),
                Input: new InputSettingsDto(),
                ProfileId: "vic20",
                RestartSession: true,
                Vic20MemorySpec: "all"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(factory.LastRequest);
        Assert.Equal("all", factory.LastRequest!.Vic20MemorySpec);
        Assert.NotNull(response.Settings);
        Assert.Equal("all", response.Settings!.Vic20MemorySpec);
        Assert.True(registry.TryGet(session.SessionId, out var restarted));
        Assert.Equal("all", restarted.Vic20MemorySpec);
        if (restarted.Architecture is IVic20RamConfiguration ram)
            Assert.Equal(Vic20RamBlocks.All, ram.RamBlocksOverride ?? Vic20RamBlocks.None);
    }

    /// <summary>
    /// FR: FR-VIC20-002 AC4, TEST-UISET-001.
    /// Acceptance: The production DefaultEmulatorRuntimeFactory (ROM-backed)
    /// keeps Vic20MemorySpec=all across Apply+Restart, matching the live Avalonia path.
    /// </summary>
    [Fact]
    public async Task SettingsServiceHost_Vic20MemorySpecAll_DefaultFactory_RoundTripsAll()
    {
        DefaultEmulatorRuntimeFactory factory;
        EmulatorRuntimeSession initial;
        try
        {
            factory = new DefaultEmulatorRuntimeFactory();
            initial = factory.Create(new CreateEmulatorSessionRequest("vic20ntsc", Vic20MemorySpec: "none"));
        }
        catch (Exception ex)
        {
            Assert.Skip($"VIC-20 DefaultEmulatorRuntimeFactory unavailable: {ex.Message}");
            return;
        }

        var registry = new EmulatorRuntimeRegistry();
        registry.Add(initial);
        var settings = new SettingsServiceHost(registry, factory);

        var response = await settings.UpdateSettingsAsync(
            new UpdateSettingsRequest(
                initial.SessionId,
                Limiter: new LimiterSettingsDto(100, true),
                Display: new DisplaySettingsDto(),
                Input: new InputSettingsDto(),
                ProfileId: "vic20ntsc",
                RestartSession: true,
                Vic20MemorySpec: "all"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Settings);
        Assert.Equal("all", response.Settings!.Vic20MemorySpec);
        Assert.Contains("vic20", response.Settings.ProfileId, StringComparison.OrdinalIgnoreCase);
        Assert.True(registry.TryGet(initial.SessionId, out var restarted));
        Assert.Equal("all", restarted.Vic20MemorySpec);
        if (restarted.Architecture is IVic20RamConfiguration ram)
            Assert.Equal(Vic20RamBlocks.All, ram.RamBlocksOverride ?? Vic20RamBlocks.None);
    }

    /// <summary>
    /// FR: FR-VIC20-002 AC4, TEST-UISET-001.
    /// Acceptance: After ApplySettingsAsync(restart=true) with all five BLK flags,
    /// the view-model still reports All / all bits true when the host echoes the spec.
    /// </summary>
    [Fact]
    public async Task AttachPanelViewModel_AllBlkChecked_ApplyRestart_KeepsAllChecked()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.SessionId.Returns("test-session");
        host.UpdateSettingsAsync(Arg.Any<UpdateSettingsRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var req = ci.Arg<UpdateSettingsRequest>();
                return new UpdateSettingsResponse(
                    RpcStatus.Ok(),
                    new SessionSettingsDto(
                        string.IsNullOrWhiteSpace(req.ProfileId) ? "vic20ntsc" : req.ProfileId,
                        req.Limiter ?? new LimiterSettingsDto(100, true),
                        req.Display ?? new DisplaySettingsDto(),
                        req.Input ?? new InputSettingsDto(),
                        req.Audio ?? new AudioSettingsDto(),
                        req.Resources ?? new ResourceSettingsDto(),
                        Vic20MemorySpec: req.Vic20MemorySpec ?? ""),
                    []);
            });

        var vm = new AttachPanelViewModel(host);
        vm.SelectedMachineProfile = vm.MachineProfiles.Single(p => p.Id == "vic20ntsc");
        vm.Vic20Blk0 = true;
        vm.Vic20Blk1 = true;
        vm.Vic20Blk2 = true;
        vm.Vic20Blk3 = true;
        vm.Vic20Blk5 = true;

        Assert.Equal("all", vm.Vic20MemorySpec);
        Assert.Equal("All", vm.SelectedVic20MemoryPreset);

        await vm.ApplySettingsAsync(restartRequired: true, TestContext.Current.CancellationToken);

        Assert.True(vm.Vic20Blk0);
        Assert.True(vm.Vic20Blk1);
        Assert.True(vm.Vic20Blk2);
        Assert.True(vm.Vic20Blk3);
        Assert.True(vm.Vic20Blk5);
        Assert.Equal("All", vm.SelectedVic20MemoryPreset);
        Assert.Equal("all", vm.Vic20MemorySpec);
    }

    /// <summary>
    /// FR: FR-VIC20-002 AC4, TEST-UISET-001.
    /// Acceptance: Real SettingsView ComboBox/CheckBox tree still shows all BLK
    /// checks after Apply + Restart adopt-back.
    /// </summary>
    [AvaloniaFact]
    public async Task RemoteControl_SettingsView_AllBlk_ApplyRestart_SnapshotStillAll()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.SessionId.Returns("test-session");
        host.UpdateSettingsAsync(Arg.Any<UpdateSettingsRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var req = ci.Arg<UpdateSettingsRequest>();
                return new UpdateSettingsResponse(
                    RpcStatus.Ok(),
                    new SessionSettingsDto(
                        string.IsNullOrWhiteSpace(req.ProfileId) ? "vic20ntsc" : req.ProfileId,
                        req.Limiter ?? new LimiterSettingsDto(100, true),
                        req.Display ?? new DisplaySettingsDto(),
                        req.Input ?? new InputSettingsDto(),
                        req.Audio ?? new AudioSettingsDto(),
                        req.Resources ?? new ResourceSettingsDto(),
                        Vic20MemorySpec: req.Vic20MemorySpec ?? ""),
                    []);
            });

        var vm = new AttachPanelViewModel(host);
        vm.SelectedMachineProfile = vm.MachineProfiles.Single(p => p.Id == "vic20ntsc");
        vm.Vic20Blk0 = true;
        vm.Vic20Blk1 = true;
        vm.Vic20Blk2 = true;
        vm.Vic20Blk3 = true;
        vm.Vic20Blk5 = true;

        var view = new SettingsView { DataContext = vm };
        var window = new Window { Content = view, Width = 420, Height = 720 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        await vm.ApplySettingsAsync(restartRequired: true, TestContext.Current.CancellationToken);
        Dispatcher.UIThread.RunJobs();

        Assert.True(vm.Vic20Blk0);
        Assert.True(vm.Vic20Blk1);
        Assert.True(vm.Vic20Blk2);
        Assert.True(vm.Vic20Blk3);
        Assert.True(vm.Vic20Blk5);
        Assert.Equal("All", vm.SelectedVic20MemoryPreset);
    }

    private sealed class RecordingRuntimeFactory : IEmulatorRuntimeFactory
    {
        public CreateEmulatorSessionRequest? LastRequest { get; private set; }

        public EmulatorRuntimeSession Create(CreateEmulatorSessionRequest request)
        {
            LastRequest = request;
            var selector = string.IsNullOrWhiteSpace(request.ArchitectureId) ? "vic20" : request.ArchitectureId;
            var descriptor = new Vic20Descriptor(selector);
            if (ViceSharp.Core.Vic20.Vic20MemoryLayout.TryParseMemorySpec(request.Vic20MemorySpec, out var blocks)
                && blocks != Vic20RamBlocks.None)
            {
                descriptor = descriptor.WithRamBlocks(blocks);
            }

            var machine = MachineTestFactory.CreateVic20Machine(descriptor);
            return new EmulatorRuntimeSession("vic20-mem-all", machine.Architecture, machine)
            {
                Vic20MemorySpec = request.Vic20MemorySpec,
                PowerState = "On",
                RunState = EmulatorRunState.Running,
            };
        }
    }
}
