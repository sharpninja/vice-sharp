namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Core;
using ViceSharp.Core.FlashCarts;
using ViceSharp.Core.Vic20;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-VIC20-005 acceptance tests. Iteration 2. The cycle budgets are the
/// VICE TYPE_B constants already named on Flash040Core.
/// </summary>
public sealed class FrVic20005AcceptanceTests
{
    /// <summary>
    /// FR: FR-VIC20-005.
    /// Use case: FE3, Ultimem, Mega-Cart, and a standard block image mount
    /// into their documented address windows.
    /// Acceptance: FE3 preset "8k" accepts a write at $2000. Ultimem and
    /// Mega-Cart attach through the media host.
    /// </summary>
    [Fact]
    public async Task Carts_MountIntoDocumentedRegions()
    {
        var (_, fe3Session, fe3Service) = CreateHost();
        var fe3 = new byte[FinalExpansion3Cartridge.FlashSize];
        var fe3Attach = await fe3Service.AttachMediaAsync(
            new AttachMediaRequest(fe3Session.SessionId, MediaSlot.Cartridge, "", "fe3.bin", Payload: fe3),
            TestContext.Current.CancellationToken);
        AssertApplied(fe3Attach);
        var fe3Port = fe3Session.Machine.Devices.GetAll<IVic20ExpansionCartPort>().Single();
        Assert.Equal(Vic20ExpansionCartKind.FinalExpansion3, fe3Port.AttachedKind);
        fe3Port.ApplyConfigPreset("8k");
        Assert.True(fe3Port.TryWriteMapped(0x2000, 0x5A));
        Assert.True(fe3Port.TryReadMapped(0x2000, out var fe3Value));
        Assert.Equal(0x5A, fe3Value);

        var (_, ultimemSession, ultimemService) = CreateHost();
        var ultimem = await ultimemService.AttachMediaAsync(
            new AttachMediaRequest(ultimemSession.SessionId, MediaSlot.Cartridge, "", "ultimem.bin", Payload: new byte[0x100000]),
            TestContext.Current.CancellationToken);
        AssertApplied(ultimem);
        Assert.Equal(
            Vic20ExpansionCartKind.Ultimem,
            ultimemSession.Machine.Devices.GetAll<IVic20ExpansionCartPort>().Single().AttachedKind);

        var (_, megaSession, megaService) = CreateHost();
        var mega = await megaService.AttachMediaAsync(
            new AttachMediaRequest(megaSession.SessionId, MediaSlot.Cartridge, "", "mega.bin", Payload: new byte[0x10000]),
            TestContext.Current.CancellationToken);
        AssertApplied(mega);
        Assert.Equal(
            Vic20ExpansionCartKind.MegaCart,
            megaSession.Machine.Devices.GetAll<IVic20ExpansionCartPort>().Single().AttachedKind);

        var (_, blkSession, blkService) = CreateHost();
        var blkImage = new byte[0x2000];
        blkImage[0] = 0x3C;
        var blk = await blkService.AttachMediaAsync(
            new AttachMediaRequest(blkSession.SessionId, MediaSlot.Cartridge, "", "blk5.bin", Payload: blkImage),
            TestContext.Current.CancellationToken);
        AssertApplied(blk);
        var blkPort = blkSession.Machine.Devices.GetAll<IVic20ExpansionCartPort>().Single();
        Assert.Equal(Vic20ExpansionCartKind.StandardBlock, blkPort.AttachedKind);
        Assert.True(blkPort.TryReadMapped(0xA000, out var blkValue));
        Assert.Equal(0x3C, blkValue);
    }

    /// <summary>
    /// FR: FR-VIC20-005.
    /// Use case: FE3 byte programming ANDs with the existing byte. Sector
    /// and chip erase finish only after the VICE cycle budgets.
    /// Acceptance: A second program can only clear bits. Chip erase is still
    /// busy one cycle before EraseChipCycles and complete on that cycle.
    /// </summary>
    [Fact]
    public void Fe3Programming_UsesAndSemanticsAndViceEraseBudgets()
    {
        var data = new byte[Flash040Core.Size];
        Array.Fill(data, (byte)0xFF);
        var flash = new Flash040Core(data);
        Program(flash, 0x1000, 0xA5);
        Program(flash, 0x1000, 0x0F);
        Assert.Equal(0x05, flash.Peek(0x1000));

        Array.Fill(data, (byte)0x55);
        flash = new Flash040Core(data);
        ChipErase(flash);
        flash.AdvanceCycles(Flash040Core.EraseChipCycles - 1);
        Assert.Equal(Flash040Core.State.ChipErase, flash.FlashState);
        Assert.Equal(0x55, flash.Peek(0));
        flash.AdvanceCycles(1);
        Assert.Equal(0xFF, flash.Peek(0));
    }

    /// <summary>
    /// FR: FR-VIC20-005.
    /// Use case: Dirty FE3 flash is returned for write-back, and dirty
    /// Mega-Cart NVRAM is returned separately.
    /// Acceptance: Programming marks the flash dirty. Mega-Cart NVRAM is a
    /// different buffer from the ROM image.
    /// </summary>
    [Fact]
    public void DirtyFlashAndNvram_AreReturnedSeparately()
    {
        var fe3Port = new Vic20ExpansionCartPort(new BasicBus()) { WriteBack = true };
        var fe3 = new byte[FinalExpansion3Cartridge.FlashSize];
        Array.Fill(fe3, (byte)0xFF);
        fe3Port.AttachFinalExpansion3(fe3);
        fe3Port.ApplyConfigPreset("flash");
        Assert.True(fe3Port.TryWriteMapped(0xA555, 0xAA));
        Assert.True(fe3Port.TryWriteMapped(0xA2AA, 0x55));
        Assert.True(fe3Port.TryWriteMapped(0xA555, 0xA0));
        Assert.True(fe3Port.TryWriteMapped(0xA010, 0x42));
        var fe3Image = fe3Port.FlushImage();
        Assert.NotNull(fe3Image);
        Assert.Equal(0x42, fe3Image[0x6010]);
        Assert.Null(fe3Port.FlushNvram());

        var ultimemPort = new Vic20ExpansionCartPort(new BasicBus()) { WriteBack = true };
        ultimemPort.AttachUltimem(new byte[0x100000]);
        ultimemPort.ApplyConfigPreset("ram-all");
        Assert.True(ultimemPort.TryWriteMapped(0x2000, 0x3C));
        Assert.True(ultimemPort.TryReadMapped(0x2000, out var ultimemSeen));
        Assert.Equal(0x3C, ultimemSeen);
        var ultimemImage = ultimemPort.FlushImage();
        Assert.NotNull(ultimemImage);
        // Power-up BLK1 bank register is 3, so $2000 lands at image offset 3 * $2000.
        Assert.Equal(0x3C, ultimemImage[0x6000]);
        Assert.Null(ultimemPort.FlushNvram());

        var megaPort = new Vic20ExpansionCartPort(new BasicBus()) { WriteBack = true };
        megaPort.AttachMegaCart(new byte[0x10000]);
        megaPort.ApplyConfigPreset("nvram");
        Assert.True(megaPort.TryWriteMapped(0x0500, 0x77));
        Assert.Null(megaPort.FlushImage());
        var nvram = megaPort.FlushNvram();
        Assert.NotNull(nvram);
        Assert.Equal(MegaCartCartridge.NvramSize, nvram.Length);
        Assert.Equal(0x77, nvram[0x0500]);
    }

    /// <summary>
    /// FR: FR-VIC20-005.
    /// Use case: Detach writes dirty state through a temporary file in the
    /// same directory and replaces the image atomically. A clean image is
    /// left unchanged.
    /// Acceptance: Detach of a programmed FE3 image stores the programmed
    /// byte. A clean image keeps its original bytes.
    /// </summary>
    [Fact]
    public async Task Detach_PersistsDirtyStateAndLeavesCleanImages()
    {
        var directory = Path.Combine(Path.GetTempPath(), "vicesharp-fe3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var dirtyPath = Path.Combine(directory, "fe3.bin");
        var cleanPath = Path.Combine(directory, "clean.bin");
        var dirty = new byte[FinalExpansion3Cartridge.FlashSize];
        var clean = new byte[FinalExpansion3Cartridge.FlashSize];
        Array.Fill(dirty, (byte)0xFF);
        Array.Fill(clean, (byte)0xA5);
        await File.WriteAllBytesAsync(dirtyPath, dirty, TestContext.Current.CancellationToken);
        await File.WriteAllBytesAsync(cleanPath, clean, TestContext.Current.CancellationToken);
        try
        {
            var (_, session, service) = CreateHost();
            var attach = await service.AttachMediaAsync(
                new AttachMediaRequest(session.SessionId, MediaSlot.Cartridge, dirtyPath, "fe3.bin"),
                TestContext.Current.CancellationToken);
            Assert.True(attach.Status.IsSuccess);
            var port = session.Machine.Devices.GetAll<IVic20ExpansionCartPort>().Single();
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
            Assert.Equal(0x42, (await File.ReadAllBytesAsync(dirtyPath, TestContext.Current.CancellationToken))[0x6010]);
            Assert.Equal(0xA5, (await File.ReadAllBytesAsync(cleanPath, TestContext.Current.CancellationToken))[0]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// FR: FR-VIC20-005.
    /// Use case: The VIC-20 machine builds and boots with no cartridge.
    /// Acceptance: CreateVic20Machine returns a CPU and no attached
    /// expansion cart.
    /// </summary>
    [Fact]
    public void Machine_BuildsWithoutACartridge()
    {
        var machine = MachineTestFactory.CreateVic20Machine(new Vic20Descriptor("vic20"));
        var port = machine.Devices.GetAll<IVic20ExpansionCartPort>().Single();

        Assert.NotNull(machine.Devices.GetByRole(DeviceRole.Cpu));
        Assert.Equal(Vic20ExpansionCartKind.None, port.AttachedKind);
    }

    private static void AssertApplied(AttachMediaResponse response)
    {
        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.True(response.Attachment?.AppliedToRuntime, response.Attachment?.Error ?? response.Status.Message);
    }

    private static (EmulatorRuntimeRegistry Registry, EmulatorRuntimeSession Session, MediaServiceHost Service) CreateHost()
    {
        var machine = MachineTestFactory.CreateVic20Machine(new Vic20Descriptor("vic20"));
        var session = new EmulatorRuntimeSession("vic20-fr-vic20-005", machine.Architecture, machine);
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        return (registry, session, new MediaServiceHost(registry));
    }

    private static void Program(Flash040Core flash, uint address, byte value)
    {
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0xA0);
        flash.Store(address, value);
    }

    private static void ChipErase(Flash040Core flash)
    {
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0x80);
        flash.Store(0x555, 0xAA);
        flash.Store(0x2AA, 0x55);
        flash.Store(0x555, 0x10);
    }
}
