namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Architectures.Vic20;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// PAL/NTSC profile restarts must keep VIC-20 expansion cart selection (and related session
/// fields), not reset them to none/start.
/// </summary>
public sealed class Vic20SettingsRestartPreservesExpansionTests
{
    [Fact]
    public async Task RestartSession_PalToNtsc_PreservesExpansionCartSelection()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = new EmulatorRuntimeRegistry();
        var factory = CreateVic20RuntimeFactory();
        var host = new EmulatorHostService(registry, factory);
        var settings = new SettingsServiceHost(registry, factory);

        var created = await host.CreateSessionAsync(new CreateEmulatorSessionRequest("vic20"), ct);
        Assert.Equal(RpcStatusCode.Ok, created.Status.Code);

        Assert.True(registry.TryGet(created.SessionId, out var session));
        session.Vic20ExpansionCartKind = "fe3";
        session.Vic20ExpansionWriteBack = true;
        session.Vic20ExpansionConfigPreset = "full";
        session.FileSystemIecUnit = 10;

        var updated = await settings.UpdateSettingsAsync(
            new UpdateSettingsRequest(
                created.SessionId,
                ProfileId: "vic20ntsc",
                RestartSession: true,
                Vic20ExpansionCartKind: "fe3",
                Vic20ExpansionWriteBack: true,
                Vic20ExpansionConfigPreset: "full",
                FileSystemIecUnit: 10),
            ct);

        Assert.Equal(RpcStatusCode.Ok, updated.Status.Code);
        Assert.NotNull(updated.Settings);
        Assert.Equal("vic20ntsc", updated.Settings.ProfileId);
        Assert.Equal("fe3", updated.Settings.Vic20ExpansionCartKind);
        Assert.True(updated.Settings.Vic20ExpansionWriteBack);
        Assert.Equal("full", updated.Settings.Vic20ExpansionConfigPreset);
        Assert.Equal(10, updated.Settings.FileSystemIecUnit);

        Assert.True(registry.TryGet(created.SessionId, out var after));
        Assert.Equal("fe3", after.Vic20ExpansionCartKind);
        Assert.True(after.Vic20ExpansionWriteBack);
        Assert.Equal("full", after.Vic20ExpansionConfigPreset);
        Assert.Equal(10, after.FileSystemIecUnit);
    }

    [Fact]
    public async Task RestartSession_NtscToPal_PreservesExpansionWhenRequestOmitsCartFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = new EmulatorRuntimeRegistry();
        var factory = CreateVic20RuntimeFactory();
        var host = new EmulatorHostService(registry, factory);
        var settings = new SettingsServiceHost(registry, factory);

        var created = await host.CreateSessionAsync(new CreateEmulatorSessionRequest("vic20ntsc"), ct);
        Assert.Equal(RpcStatusCode.Ok, created.Status.Code);
        Assert.True(registry.TryGet(created.SessionId, out var session));
        session.Vic20ExpansionCartKind = "ultimem";
        session.Vic20ExpansionConfigPreset = "ram-all";
        session.Vic20ExpansionWriteBack = true;

        // Profile-only restart request (no expansion fields) must still preserve cart selection.
        var updated = await settings.UpdateSettingsAsync(
            new UpdateSettingsRequest(created.SessionId, ProfileId: "vic20", RestartSession: true),
            ct);

        Assert.Equal(RpcStatusCode.Ok, updated.Status.Code);
        Assert.Equal("ultimem", updated.Settings!.Vic20ExpansionCartKind);
        Assert.Equal("ram-all", updated.Settings.Vic20ExpansionConfigPreset);
        Assert.True(updated.Settings.Vic20ExpansionWriteBack);
    }

    private static IEmulatorRuntimeFactory CreateVic20RuntimeFactory()
    {
        var descriptors = Vic20MachineProfiles.All
            .Select(profile => (ViceSharp.Abstractions.IArchitectureDescriptor)new Vic20Descriptor(profile))
            .ToList();
        return new DefaultEmulatorRuntimeFactory(
            new ViceSharp.Core.ArchitectureBuilder(MachineTestFactory.CreateVic20RomProvider()),
            descriptors,
            "vic20");
    }
}
