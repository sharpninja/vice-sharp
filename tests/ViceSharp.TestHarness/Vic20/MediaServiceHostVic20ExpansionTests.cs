namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Architectures.Vic20;
using ViceSharp.Core.Vic20;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Honest MediaServiceHost attach/detach for FE3 / Ultimem / Mega-Cart through
/// the real validation + apply path (not direct cart construction).
/// </summary>
public sealed class MediaServiceHostVic20ExpansionTests
{
    [Fact]
    public async Task AttachMedia_Fe3_512K_Bin_AppliesViaMediaServiceHost()
    {
        var (registry, session, service) = CreateVic20MediaHost();
        var payload = new byte[FinalExpansion3Cartridge.FlashSize];
        payload[0x10] = 0xC3;

        var response = await service.AttachMediaAsync(
            new AttachMediaRequest(session.SessionId, MediaSlot.Cartridge, "", "fe3.bin", Payload: payload),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Attachment);
        Assert.True(response.Attachment!.AppliedToRuntime, response.Attachment.Error);
        Assert.Equal("fe3", session.Vic20ExpansionCartKind);

        var port = session.Machine.Devices.GetAll<IVic20ExpansionCartPort>().Single();
        Assert.Equal(Vic20ExpansionCartKind.FinalExpansion3, port.AttachedKind);
        port.ApplyConfigPreset("8k");
        Assert.True(port.TryWriteMapped(0x2000, 0x5A));
        Assert.True(port.TryReadMapped(0x2000, out var v));
        Assert.Equal(0x5A, v);
    }

    [Fact]
    public async Task AttachMedia_Ultimem_1MB_Bin_AppliesViaMediaServiceHost()
    {
        var (registry, session, service) = CreateVic20MediaHost();
        var payload = new byte[0x100000];
        payload[0x2000] = 0x42;

        var response = await service.AttachMediaAsync(
            new AttachMediaRequest(session.SessionId, MediaSlot.Cartridge, "", "ultimem.bin", Payload: payload),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.True(response.Attachment!.AppliedToRuntime, response.Attachment.Error);
        Assert.Equal("ultimem", session.Vic20ExpansionCartKind);
        Assert.Equal(Vic20ExpansionCartKind.Ultimem,
            session.Machine.Devices.GetAll<IVic20ExpansionCartPort>().Single().AttachedKind);
    }

    [Fact]
    public async Task AttachMedia_MegaCart_64K_Bin_AppliesViaMediaServiceHost()
    {
        var (registry, session, service) = CreateVic20MediaHost();
        var payload = new byte[0x10000];
        payload[0] = 0x11;

        var response = await service.AttachMediaAsync(
            new AttachMediaRequest(session.SessionId, MediaSlot.Cartridge, "", "mega.bin", Payload: payload),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.True(response.Attachment!.AppliedToRuntime, response.Attachment.Error);
        Assert.Equal("megacart", session.Vic20ExpansionCartKind);
        Assert.Equal(Vic20ExpansionCartKind.MegaCart,
            session.Machine.Devices.GetAll<IVic20ExpansionCartPort>().Single().AttachedKind);
    }

    [Fact]
    public async Task DetachMedia_Fe3_EjectsVic20ExpansionPort()
    {
        var (registry, session, service) = CreateVic20MediaHost();
        var payload = new byte[FinalExpansion3Cartridge.FlashSize];

        var attach = await service.AttachMediaAsync(
            new AttachMediaRequest(session.SessionId, MediaSlot.Cartridge, "", "fe3.bin", Payload: payload),
            TestContext.Current.CancellationToken);
        Assert.True(attach.Status.IsSuccess);

        var detach = await service.DetachMediaAsync(
            new DetachMediaRequest(session.SessionId, MediaSlot.Cartridge),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, detach.Status.Code);
        Assert.Equal(Vic20ExpansionCartKind.None,
            session.Machine.Devices.GetAll<IVic20ExpansionCartPort>().Single().AttachedKind);
        Assert.Equal("none", session.Vic20ExpansionCartKind);
    }

    private static (EmulatorRuntimeRegistry Registry, EmulatorRuntimeSession Session, MediaServiceHost Service)
        CreateVic20MediaHost()
    {
        var machine = MachineTestFactory.CreateVic20Machine(new Vic20Descriptor("vic20"));
        var session = new EmulatorRuntimeSession("vic20-media-test", machine.Architecture, machine);
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        return (registry, session, new MediaServiceHost(registry));
    }
}
