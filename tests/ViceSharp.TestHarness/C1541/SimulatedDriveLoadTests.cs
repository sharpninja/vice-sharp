namespace ViceSharp.TestHarness.C1541;

using System.Linq;
using FluentAssertions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// FR/TR: FR-IECVDRIVE-001 / TEST-IECVDRIVE-004.
/// Use case: with True Drive OFF (the default single-system C64 the GUI runs),
/// a disk attached to the simulated drive and LOAD"*",8,1 typed must load the
/// program into RAM via the KERNAL serial traps + host-side vdrive - WITHOUT
/// hanging on the (unimplemented) real IEC bit-bang protocol. This is the
/// regression gate for the "Loading disk with True Drive off is frozen" bug:
/// the program must appear at $0801 within a bounded frame budget.
/// </summary>
[Collection("NativeVice")]
public sealed class SimulatedDriveLoadTests
{
    private static readonly byte[] ProgramAt0801 =
    {
        0x07, 0x08, // link to next line
        0x0A, 0x00, // line number 10
        0x80,       // END token
        0x00,       // end of line
        0x00, 0x00, // end of program
    };

    private static D64Image BuildMinimalLoadableDisk()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        image.Format();

        var directory = image.GetSector(18, 1);
        directory[0] = 0x00;
        directory[1] = 0xFF;
        directory[2] = 0x82; // closed PRG
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

        return image;
    }

    [Fact]
    public void Load_FromSimulatedDrive_StreamsProgramIntoRam_WithoutFreezing()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var builder = new ArchitectureBuilder(provider);
        var machine = builder.Build(new C64Descriptor(C64MachineProfiles.C64Pal));

        // True Drive OFF: attach the disk to the simulated IEC drive 8.
        var drive8 = machine.Devices.GetAll<IecDrive>().First(d => d.DriveNumber == 8);
        drive8.InsertDisk(BuildMinimalLoadableDisk().ToArray());
        drive8.HasDisk.Should().BeTrue();

        var automation = HostKeyboardAutomation.CreateC64Drive8Autostart();

        const int maxFrames = 2000;
        var loaded = false;
        var frames = 0;
        for (; frames < maxFrames && !loaded; frames++)
        {
            machine.RunFrame();
            automation.AdvanceFrame(machine);
            loaded = machine.Bus.Peek(0x0801) == 0x07
                && machine.Bus.Peek(0x0803) == 0x0A
                && machine.Bus.Peek(0x0805) == 0x80;
        }

        automation.LastError.Should().BeNullOrEmpty();
        loaded.Should().BeTrue(
            $"LOAD\"*\",8,1 on the simulated drive (True Drive OFF) must place the program at $0801 " +
            $"within {maxFrames} frames instead of hanging on the unimplemented IEC protocol " +
            $"(got {machine.Bus.Peek(0x0801):X2} {machine.Bus.Peek(0x0802):X2} {machine.Bus.Peek(0x0803):X2} " +
            $"{machine.Bus.Peek(0x0804):X2} {machine.Bus.Peek(0x0805):X2})");

        for (var i = 0; i < ProgramAt0801.Length; i++)
            machine.Bus.Peek((ushort)(0x0801 + i)).Should().Be(ProgramAt0801[i], $"program byte {i}");
    }
}
