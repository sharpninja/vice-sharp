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

    [Fact]
    public async Task DetachMedia_Fe3_WriteBackPersistsDirtyFlashAtomically()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "ViceSharp.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var imagePath = Path.Combine(directory, "fe3.bin");
        var image = new byte[FinalExpansion3Cartridge.FlashSize];
        Array.Fill(image, (byte)0xFF);
        await File.WriteAllBytesAsync(
            imagePath,
            image,
            TestContext.Current.CancellationToken);

        try
        {
            var (registry, session, service) = CreateVic20MediaHost();
            var attach = await service.AttachMediaAsync(
                new AttachMediaRequest(
                    session.SessionId,
                    MediaSlot.Cartridge,
                    imagePath,
                    "fe3.bin"),
                TestContext.Current.CancellationToken);
            Assert.True(attach.Status.IsSuccess);

            var port = session.Machine.Devices
                .GetAll<IVic20ExpansionCartPort>()
                .Single();
            port.WriteBack = true;
            port.ApplyConfigPreset("flash");
            Assert.True(port.TryWriteMapped(0xA555, 0xAA));
            Assert.True(port.TryWriteMapped(0xA2AA, 0x55));
            Assert.True(port.TryWriteMapped(0xA555, 0xA0));
            Assert.True(port.TryWriteMapped(0xA010, 0x42));

            var detach = await service.DetachMediaAsync(
                new DetachMediaRequest(session.SessionId, MediaSlot.Cartridge),
                TestContext.Current.CancellationToken);

            Assert.Equal(RpcStatusCode.Ok, detach.Status.Code);
            Assert.Equal(
                0x42,
                (await File.ReadAllBytesAsync(
                    imagePath,
                    TestContext.Current.CancellationToken))[0x6010]);
            Assert.Equal(Vic20ExpansionCartKind.None, port.AttachedKind);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DetachMedia_MegaCart_WriteBackPersistsAndReloadsNvramSidecar()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "ViceSharp.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var imagePath = Path.Combine(directory, "mega.bin");
        await File.WriteAllBytesAsync(
            imagePath,
            new byte[0x10000],
            TestContext.Current.CancellationToken);

        try
        {
            var (registry, session, service) = CreateVic20MediaHost();
            var attach = await service.AttachMediaAsync(
                new AttachMediaRequest(
                    session.SessionId,
                    MediaSlot.Cartridge,
                    imagePath,
                    "mega.bin"),
                TestContext.Current.CancellationToken);
            Assert.True(attach.Status.IsSuccess);

            var port = session.Machine.Devices
                .GetAll<IVic20ExpansionCartPort>()
                .Single();
            port.WriteBack = true;
            port.ApplyConfigPreset("nvram");
            Assert.True(port.TryWriteMapped(0x0500, 0x77));

            var detach = await service.DetachMediaAsync(
                new DetachMediaRequest(session.SessionId, MediaSlot.Cartridge),
                TestContext.Current.CancellationToken);
            Assert.Equal(RpcStatusCode.Ok, detach.Status.Code);

            var nvramPath = imagePath + ".nvram";
            var persisted = await File.ReadAllBytesAsync(
                nvramPath,
                TestContext.Current.CancellationToken);
            Assert.Equal(MegaCartCartridge.NvramSize, persisted.Length);
            Assert.Equal(0x77, persisted[0x0500]);

            var (_, reloadedSession, reloadedService) = CreateVic20MediaHost();
            var reload = await reloadedService.AttachMediaAsync(
                new AttachMediaRequest(
                    reloadedSession.SessionId,
                    MediaSlot.Cartridge,
                    imagePath,
                    "mega.bin"),
                TestContext.Current.CancellationToken);
            Assert.True(reload.Status.IsSuccess);

            var reloadedPort = reloadedSession.Machine.Devices
                .GetAll<IVic20ExpansionCartPort>()
                .Single();
            reloadedPort.ApplyConfigPreset("nvram");
            Assert.True(reloadedPort.TryReadMapped(0x0500, out var value));
            Assert.Equal(0x77, value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
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
