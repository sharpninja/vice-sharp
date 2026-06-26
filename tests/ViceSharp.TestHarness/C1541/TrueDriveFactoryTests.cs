namespace ViceSharp.TestHarness.C1541;

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR/TR: FR-DRVTRUE-001 / TEST-DRVTRUE-001.
/// Use case: The runtime factory gates the cycle-accurate true-drive rig behind
/// an opt-in flag. With it off (the default) a session is the plain C64 machine
/// so the simulated-drive path and native lockstep parity are unchanged; with it
/// on, a C64 session runs a coordinator rig (C64 host + emulated 1541 over IEC)
/// that advances without error. The end-to-end true-drive LOAD itself is locked
/// by <see cref="TrueDriveLoadTests"/>.
/// </summary>
public sealed class TrueDriveFactoryTests
{
    private static DefaultEmulatorRuntimeFactory CreateFactory()
    {
        var profile = C64MachineProfiles.C64Pal;
        var builder = new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider());
        return new DefaultEmulatorRuntimeFactory(builder, new[] { new C64Descriptor(profile) }, profile.Id);
    }

    [Fact]
    public void Create_WithTrueDriveOff_BuildsPlainMachine()
    {
        var factory = CreateFactory();
        var request = new CreateEmulatorSessionRequest(C64MachineProfiles.C64Pal.Id);

        var session = factory.Create(request, trueDrive: false);

        session.Machine.Should().NotBeOfType<CoordinatorMachine>(
            "true-drive off must keep the existing simulated-drive machine so parity is unaffected");
    }

    [Fact]
    public void Create_WithTrueDriveOn_BuildsCoordinatorRigThatRuns()
    {
        var factory = CreateFactory();
        var request = new CreateEmulatorSessionRequest(C64MachineProfiles.C64Pal.Id);

        var session = factory.Create(request, trueDrive: true);

        var rig = session.Machine.Should().BeOfType<CoordinatorMachine>().Subject;
        rig.Architecture.MachineName.Should().Be(C64MachineProfiles.C64Pal.DisplayName);

        // The rig exposes its live IEC bus and the session monitors THAT bus
        // (not the host's unused always-on bus), so True Drive activity is real.
        rig.IecBus.Should().NotBeNull();
        session.IecBusActivity.Should().NotBeNull();

        // The rig advances a frame (host + drive in lockstep) without throwing.
        var step = () => rig.RunFrame();
        step.Should().NotThrow();
    }

    [Fact]
    public void Create_HonorsTrueDriveFieldOnTheRequest()
    {
        var factory = CreateFactory();

        var off = factory.Create(new CreateEmulatorSessionRequest(C64MachineProfiles.C64Pal.Id, TrueDrive: false));
        off.Machine.Should().NotBeOfType<CoordinatorMachine>();

        var on = factory.Create(new CreateEmulatorSessionRequest(C64MachineProfiles.C64Pal.Id, TrueDrive: true));
        on.Machine.Should().BeOfType<CoordinatorMachine>(
            "Create(request) must honor request.TrueDrive so the session request carries the selection end-to-end");
    }

    /// <summary>
    /// FR/TR: FR-DRVTRUE-001 / FR-IECLOAD-001 / TEST-DRVTRUE-001.
    /// Use case: This is the GUI's flow - a disk is attached, then True Drive is
    /// enabled, which rebuilds the session as a true-drive rig. The attached disk
    /// path rides the session request so the rebuilt rig boots with the disk
    /// inserted; LOAD must then stream the program over the true-drive IEC bus.
    /// Acceptance: A factory session created with TrueDrive + TrueDriveDiskImagePath
    /// places the directory's first program at $0801 via autostart LOAD.
    /// </summary>
    [Fact]
    public void Create_TrueDriveWithDiskPath_LoadsProgramOverIec()
    {
        var diskPath = WriteMinimalLoadableD64();
        try
        {
            var factory = CreateFactory();
            var session = factory.Create(new CreateEmulatorSessionRequest(
                C64MachineProfiles.C64Pal.Id,
                TrueDrive: true,
                TrueDriveDevice: 8,
                TrueDriveDiskImagePath: diskPath));
            var rig = session.Machine.Should().BeOfType<CoordinatorMachine>().Subject;

            var automation = HostKeyboardAutomation.CreateC64Drive8Autostart();
            const int maxFrames = 2000;
            var loaded = false;
            for (var frame = 0; frame < maxFrames && !loaded; frame++)
            {
                rig.RunFrame();
                automation.AdvanceFrame(rig.Host);
                loaded = rig.Bus.Peek(0x0801) == 0x07
                    && rig.Bus.Peek(0x0803) == 0x0A
                    && rig.Bus.Peek(0x0805) == 0x80;
            }

            automation.LastError.Should().BeNullOrEmpty();
            loaded.Should().BeTrue(
                "the factory-built true-drive rig (disk path carried in the session request) must LOAD over IEC");
        }
        finally
        {
            try { File.Delete(diskPath); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// FR/TR: FR-DRVTRUE-001 / TEST-DRVTRUE-001.
    /// Use case: A true-drive session boots with the disk inserted at build time,
    /// so the host media list must report the drive as attached - otherwise the
    /// UI and ListMedia show the drive empty even though the 1541 holds the disk.
    /// Acceptance: A true-drive session created with a disk path registers a
    /// MediaAttachment for the drive slot (attached, correct path).
    /// </summary>
    [Fact]
    public void Create_TrueDriveWithDiskPath_RegistersMediaAttachment()
    {
        var diskPath = WriteMinimalLoadableD64();
        try
        {
            var factory = CreateFactory();
            var session = factory.Create(new CreateEmulatorSessionRequest(
                C64MachineProfiles.C64Pal.Id,
                TrueDrive: true,
                TrueDriveDevice: 8,
                TrueDriveDiskImagePath: diskPath));

            session.MediaAttachments.Should().ContainKey(MediaSlot.Drive8);
            var attachment = session.MediaAttachments[MediaSlot.Drive8];
            attachment.IsAttached.Should().BeTrue();
            attachment.AppliedToRuntime.Should().BeTrue();
            attachment.FilePath.Should().Be(diskPath);
        }
        finally
        {
            try { File.Delete(diskPath); } catch { /* best effort */ }
        }
    }

    // Minimal in-memory D64 with a single "10 END" PRG at 17/0 and a one-entry
    // directory, mirroring TrueDriveLoadTests so this exercises the factory path.
    private static readonly byte[] ProgramAt0801 =
    {
        0x07, 0x08, 0x0A, 0x00, 0x80, 0x00, 0x00, 0x00,
    };

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

        var path = Path.Combine(Path.GetTempPath(), $"vicesharp-truedrive-factory-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, image.ToArray());
        return path;
    }
}
