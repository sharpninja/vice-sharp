namespace ViceSharp.TestHarness.C1541;

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR/TR: FR-IECVDRIVE-001 / TEST-IECVDRIVE-005.
/// Reproduces the GUI's exact host flow for a True-Drive-OFF disk load: create a
/// simulated session through the runtime factory, attach the disk through
/// MediaServiceHost (not by poking the drive directly), then autostart LOAD.
/// This is the path the app actually exercises - the direct-InsertDisk test
/// bypassed the media service - so it catches any gap between "disk attached in
/// the UI" and "disk visible to the KERNAL serial trap".
/// </summary>
[Collection("NativeVice")]
public sealed class HostMediaSimulatedLoadTests
{
    private static readonly byte[] ProgramAt0801 =
    {
        0x07, 0x08, 0x0A, 0x00, 0x80, 0x00, 0x00, 0x00,
    };

    [Fact]
    public async System.Threading.Tasks.Task Factory_SimulatedSession_AttachDiskViaMediaService_ThenLoads()
    {
        var diskPath = WriteMinimalLoadableD64();
        try
        {
            var profile = C64MachineProfiles.C64Pal;
            var builder = new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider());
            var factory = new DefaultEmulatorRuntimeFactory(builder, new[] { new C64Descriptor(profile) }, profile.Id);
            var registry = new EmulatorRuntimeRegistry();
            var media = new MediaServiceHost(registry);

            var session = factory.Create(new CreateEmulatorSessionRequest(profile.Id, TrueDrive: false));
            session.Machine.Should().NotBeOfType<CoordinatorMachine>("True Drive OFF must be the simulated machine");
            registry.Add(session);

            var attach = await media.AttachMediaAsync(
                new AttachMediaRequest(session.SessionId, MediaSlot.Drive8, diskPath),
                TestContext.Current.CancellationToken);

            attach.Status.IsSuccess.Should().BeTrue();
            attach.Attachment!.AppliedToRuntime.Should().BeTrue(
                "the simulated drive must accept the disk at runtime so the serial trap can serve it");

            var automation = HostKeyboardAutomation.CreateC64Drive8Autostart();
            const int maxFrames = 2000;
            var loaded = false;
            for (var frame = 0; frame < maxFrames && !loaded; frame++)
            {
                session.Machine.RunFrame();
                automation.AdvanceFrame(session.Machine);
                loaded = session.Machine.Bus.Peek(0x0801) == 0x07
                    && session.Machine.Bus.Peek(0x0803) == 0x0A
                    && session.Machine.Bus.Peek(0x0805) == 0x80;
            }

            automation.LastError.Should().BeNullOrEmpty();
            loaded.Should().BeTrue(
                "attaching via MediaServiceHost then LOAD\"*\",8,1 must stream the program (the real app flow)");
        }
        finally
        {
            try { File.Delete(diskPath); } catch { /* best effort */ }
        }
    }

    private static string WriteMinimalLoadableD64()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        image.Format();

        var directory = image.GetSector(18, 1);
        directory[0] = 0x00;
        directory[1] = 0xFF;
        directory[2] = 0x82;
        directory[3] = 17;
        directory[4] = 0;
        const string name = "TEST";
        for (var i = 0; i < 16; i++)
            directory[5 + i] = (byte)(i < name.Length ? name[i] : 0xA0);

        var file = image.GetSector(17, 0);
        var payload = new byte[] { 0x01, 0x08 }.Concat(ProgramAt0801).ToArray();
        file[0] = 0x00;
        file[1] = (byte)(2 + payload.Length - 1);
        payload.CopyTo(file.Slice(2));

        var path = Path.Combine(Path.GetTempPath(), $"vicesharp-hostmedia-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, image.ToArray());
        return path;
    }
}
