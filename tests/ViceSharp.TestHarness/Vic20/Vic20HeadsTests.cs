namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Chips.IEC;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Launcher;
using ViceSharp.Protocol;
using ViceSharp.RomFetch;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// Heads / session create / picker surfaces for VIC-20.
/// </summary>
public sealed class Vic20HeadsTests
{
    [Fact]
    public void Launcher_Xvic_HostKind_IsVic20()
    {
        var yaml = ViceTopologyBuilder.BuildYaml(ViceArgsParser.Parse("xvic", Array.Empty<string>()));
        var desc = ViceTopologyBuilder.ParseDescriptor(yaml);
        Assert.Equal("Vic20", desc.HostKind);
    }

    [Fact]
    public void SessionFactory_CanCreateVic20Session()
    {
        var romProvider = MachineTestFactory.CreateVic20RomProvider();
        // Also need C64 ROMs if factory registers both; use Vic20-only descriptor map.
        var builder = new Core.ArchitectureBuilder(romProvider);
        var factory = new DefaultEmulatorRuntimeFactory(
            builder,
            [new Vic20Descriptor()],
            defaultArchitectureId: "vic20");

        var session = factory.Create(new CreateEmulatorSessionRequest("vic20"));
        Assert.NotNull(session);
        Assert.Contains("VIC-20", session.Architecture.MachineName, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(session.Machine.Devices.GetByRole(Abstractions.DeviceRole.VideoChip));
    }

    [Fact]
    public void XboxComputerPicker_ListsVic20_Enabled()
    {
        // Structural: ComputerOption catalog includes xvic enabled after Iteration 2.
        var options = XboxSettingsViewModel.CreateDefaultComputerOptions();
        var vic = Assert.Single(options, o => o.FamilyId == "xvic");
        Assert.True(vic.IsAvailable, "VIC-20 must be selectable when ROMs path is available");
    }

    /// <summary>
    /// FR-PRF-005 / FR-VIC20. Avalonia and UWP both consume SettingsServiceHost.ListProfiles.
    /// Acceptance: catalog includes vic20 + vic20ntsc with Machine xvic; resolve accepts aliases.
    /// </summary>
    [Fact]
    public async Task SettingsService_ListProfiles_IncludesVic20Variants()
    {
        var registry = new EmulatorRuntimeRegistry();
        var romProvider = MachineTestFactory.CreateVic20RomProvider();
        // Factory needs at least one architecture to create a session for ListProfiles.
        var factory = new DefaultEmulatorRuntimeFactory(
            new Core.ArchitectureBuilder(romProvider),
            [new Vic20Descriptor(), new Architectures.C64.C64Descriptor()],
            defaultArchitectureId: "vic20");
        var emulatorHost = new EmulatorHostService(registry, factory);
        var settings = new SettingsServiceHost(registry, factory);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest("vic20"),
            TestContext.Current.CancellationToken);
        var list = await settings.ListProfilesAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, list.Status.Code);
        Assert.Contains(list.Profiles, p => p.Id == "vic20" && p.Machine == "xvic" && p.IsAvailable);
        Assert.Contains(list.Profiles, p => p.Id == "vic20ntsc" && p.Machine == "xvic");
        Assert.Contains(list.Profiles, p => p.Id == "c64" && p.Machine == "x64sc");
    }

    [Fact]
    public async Task SettingsService_UpdateProfile_AcceptsVic20Ntsc()
    {
        var registry = new EmulatorRuntimeRegistry();
        var romProvider = MachineTestFactory.CreateVic20RomProvider();
        // C64 ROMs also required if CreateSession uses c64 first - use vic20 only.
        var factory = new DefaultEmulatorRuntimeFactory(
            new Core.ArchitectureBuilder(romProvider),
            Vic20MachineProfiles.All.Select(p => new Vic20Descriptor(p)).Cast<Abstractions.IArchitectureDescriptor>(),
            defaultArchitectureId: "vic20");
        var emulatorHost = new EmulatorHostService(registry, factory);
        var settings = new SettingsServiceHost(registry, factory);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest("vic20"),
            TestContext.Current.CancellationToken);
        var updated = await settings.UpdateSettingsAsync(
            new UpdateSettingsRequest(
                created.SessionId,
                Limiter: null,
                Display: null,
                Input: null,
                ProfileId: "vic20ntsc",
                RestartSession: true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, updated.Status.Code);
        Assert.Equal("vic20ntsc", updated.Settings!.ProfileId);
        Assert.Contains("NTSC", updated.Settings.ProfileId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Profile restart must replace the live machine with VIC-20 (not stay on C64).
    /// </summary>
    [Fact]
    public async Task SettingsService_RestartToVic20_RebuildsMachineAsVic20()
    {
        var root = FindDualRomDataRoot();
        Assert.False(string.IsNullOrEmpty(root), "Need a VICE data root with C64 + VIC20 ROMs");

        var previous = Environment.GetEnvironmentVariable("VICESHARP_ROM_PATH");
        Environment.SetEnvironmentVariable("VICESHARP_ROM_PATH", root);
        try
        {
            var factory = new DefaultEmulatorRuntimeFactory();
            var registry = new EmulatorRuntimeRegistry();
            var emulatorHost = new EmulatorHostService(registry, factory);
            var settings = new SettingsServiceHost(registry, factory);

            var created = await emulatorHost.CreateSessionAsync(
                new CreateEmulatorSessionRequest("c64"),
                TestContext.Current.CancellationToken);
            Assert.True(created.Status.IsSuccess, created.Status.Message);
            Assert.True(registry.TryGet(created.SessionId, out var before));
            Assert.Contains("64", before!.Architecture.MachineName, StringComparison.OrdinalIgnoreCase);

            var updated = await settings.UpdateSettingsAsync(
                new UpdateSettingsRequest(
                    created.SessionId,
                    Limiter: null,
                    Display: null,
                    Input: null,
                    ProfileId: "vic20",
                    RestartSession: true),
                TestContext.Current.CancellationToken);

            Assert.True(updated.Status.IsSuccess, updated.Status.Message);
            Assert.Equal("vic20", updated.Settings!.ProfileId);

            Assert.True(registry.TryGet(created.SessionId, out var session));
            Assert.Contains("VIC-20", session!.Architecture.MachineName, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("xvic", (session.Architecture as IProfiledArchitectureDescriptor)!.MachineProfile.Family);
            Assert.Equal(2, session.Machine.Devices.GetAll<Via6522>().Count());
        }
        finally
        {
            Environment.SetEnvironmentVariable("VICESHARP_ROM_PATH", previous);
        }
    }

    private static string? FindDualRomDataRoot()
    {
        foreach (var root in ViceDataPathResolver.FindDataRoots())
        {
            var provider = new RomProvider(root, []);
            if (new C64RomSet().IsComplete(provider) && new Vic20RomSet().IsComplete(provider))
                return root;
        }

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (!File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
                continue;
            var data = Path.Combine(dir.FullName, "native", "vice", "vice", "data");
            if (!Directory.Exists(data))
                return null;
            var provider = new RomProvider(data, []);
            if (new C64RomSet().IsComplete(provider) && new Vic20RomSet().IsComplete(provider))
                return data;
            return null;
        }

        return null;
    }
}
