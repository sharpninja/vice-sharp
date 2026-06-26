namespace ViceSharp.TestHarness.C1541;

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// FR/TR: FR-IECLOAD-001 / TEST-IECLOAD-001.
/// Use case: A C64 host and a true-drive 1541 (6502 + VIA1/VIA2 + DOS ROM)
/// run under one SystemCoordinator, wired CIA2 &lt;-&gt; IEC &lt;-&gt; VIA1. Typing
/// LOAD"*",8,1 must drive the full cycle-accurate IEC protocol: the C64 sends
/// the command, the drive DOS searches the directory, reads the file via GCR,
/// and streams it back over IEC into C64 RAM. This locks the end-to-end
/// faithful true-drive LOAD that the GUI single-system path does not yet use.
/// </summary>
[Collection("NativeVice")]
public sealed class TrueDriveLoadTests
{
    // A minimal BASIC program "10 END" at $0801. PRG payload is the 2-byte
    // load address followed by the tokenised line and end-of-program marker.
    private static readonly byte[] ProgramAt0801 =
    {
        0x07, 0x08, // link to next line ($0807)
        0x0A, 0x00, // line number 10
        0x80,       // END token
        0x00,       // end of line
        0x00, 0x00, // end of program
    };

    private static string WriteMinimalLoadableD64()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        image.Format(); // BAM at 18/0 points the directory at 18/1.

        var directory = image.GetSector(18, 1);
        directory[0] = 0x00; // no further directory sector
        directory[1] = 0xFF;
        directory[2] = 0x82; // file type: closed PRG
        directory[3] = 17;   // first file track
        directory[4] = 0;    // first file sector
        const string name = "TEST";
        for (var i = 0; i < 16; i++)
            directory[5 + i] = (byte)(i < name.Length ? name[i] : 0xA0);

        var file = image.GetSector(17, 0);
        var payload = new byte[] { 0x01, 0x08 }.Concat(ProgramAt0801).ToArray();
        file[0] = 0x00;                          // last sector in the chain
        file[1] = (byte)(2 + payload.Length - 1); // index of last used byte
        payload.CopyTo(file.Slice(2));

        var path = Path.Combine(Path.GetTempPath(), $"vicesharp-truedrive-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, image.ToArray());
        return path;
    }

    [Fact]
    public void Load_FromTrueDrive1541_StreamsProgramIntoC64Ram()
    {
        var diskPath = WriteMinimalLoadableD64();
        try
        {
            var provider = MachineTestFactory.CreateC64RomProvider();
            var builder = new ArchitectureBuilder(provider);
            var host = builder.Build(new C64Descriptor(C64MachineProfiles.C64Pal));
            var drive = builder.Build(new C1541Descriptor(8, diskPath));

            var iec = IecInterSystemBus.Create();
            var hostEndpoint = iec.AttachEndpoint("c64");
            var driveEndpoint = iec.AttachEndpoint("drive-8");

            SystemCoordinator? coord = null;
            var cia2 = (Mos6526)host.Devices.GetByRole(DeviceRole.Cia2)!;
            host.Devices.GetAll<C64Cia2InterfaceDevice>().First().ConnectCia2(
                cia2,
                iec: hostEndpoint,
                synchronizeIec: () => coord!.SynchronizePeripheralSystemsToHost());

            var driveVias = drive.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).ToArray();
            drive.Devices.GetAll<C1541IecInterfaceDevice>().First()
                .ConnectVia1(driveVias[0], driveEndpoint, iec);
            drive.Devices.GetAll<C1541DriveMechanismDevice>().First().ConnectVia2(driveVias[1]);

            coord = new SystemCoordinator();
            coord.AttachSystem(host);
            coord.AttachSystem(drive);
            coord.AttachBus(iec);
            coord.Reset();

            var automation = HostKeyboardAutomation.CreateC64Drive8Autostart();

            const int maxFrames = 2000;
            const int cyclesPerFrame = 19656;
            var loaded = false;
            for (var frame = 0; frame < maxFrames && !loaded; frame++)
            {
                coord.Step(cyclesPerFrame);
                automation.AdvanceFrame(host);
                loaded = host.Bus.Peek(0x0801) == 0x07
                    && host.Bus.Peek(0x0803) == 0x0A
                    && host.Bus.Peek(0x0805) == 0x80;
            }

            automation.LastError.Should().BeNullOrEmpty();
            loaded.Should().BeTrue(
                $"LOAD\"*\",8,1 over the true-drive IEC bus should place the program at $0801 " +
                $"(got {host.Bus.Peek(0x0801):X2} {host.Bus.Peek(0x0802):X2} {host.Bus.Peek(0x0803):X2} " +
                $"{host.Bus.Peek(0x0804):X2} {host.Bus.Peek(0x0805):X2})");

            for (var i = 0; i < ProgramAt0801.Length; i++)
                host.Bus.Peek((ushort)(0x0801 + i)).Should().Be(ProgramAt0801[i], $"program byte {i}");
        }
        finally
        {
            try { File.Delete(diskPath); } catch { /* best effort */ }
        }
    }
}
