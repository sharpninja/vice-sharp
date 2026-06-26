namespace ViceSharp.TestHarness.IEC;

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Serial;
using Xunit;

/// <summary>
/// FR/TR: FR-IECVDRIVE-001 / TEST-IECVDRIVE-002.
/// Use case: replay the exact IEC command sequence the KERNAL emits for a disk
/// LOAD (LISTEN/OPEN-file/filename/UNLISTEN, then TALK/secondary/ACPTR) against
/// the host-side virtual drive and confirm it streams the right bytes with EOI
/// on the final byte - the behaviour that replaces the frozen real-IEC path.
/// </summary>
public sealed class VirtualDriveServerTests
{
    private static readonly byte[] ProgramAt0801 =
    {
        0x07, 0x08, 0x0A, 0x00, 0x80, 0x00, 0x00, 0x00,
    };

    private static readonly byte[] PrgStream =
        new byte[] { 0x01, 0x08 }.Concat(ProgramAt0801).ToArray();

    private static D64Image BuildDisk()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        image.Format();

        var dir = image.GetSector(18, 1);
        dir[0] = 0x00;
        dir[1] = 0xFF;
        dir[2] = 0x82; // PRG, closed
        dir[3] = 17;
        dir[4] = 0;
        const string name = "TEST";
        for (var i = 0; i < 16; i++)
            dir[5 + i] = (byte)(i < name.Length ? name[i] : 0xA0);
        dir[2 + 0x1E] = 1; // blocks

        var file = image.GetSector(17, 0);
        file[0] = 0x00;
        file[1] = (byte)(2 + PrgStream.Length - 1);
        PrgStream.CopyTo(file.Slice(2));

        return image;
    }

    // Replay LISTEN dev / OPEN-file ch / <name> / UNLISTEN / TALK dev / secondary / ACPTR*.
    private static List<byte> LoadOverIec(VirtualDriveServer server, string petsciiName)
    {
        // LISTEN 8 (no driver call) + OPEN file on channel 0.
        server.Open(0x28, 0xF0);
        foreach (var c in petsciiName)
            server.Write(0x28, 0xF0, (byte)c);
        server.Unlisten(0x28, 0xF0); // opens the file with the buffered name

        // TALK 8 + open talk channel 0 (already open -> no-op).
        server.ListenTalk(0x48, 0x60);

        var bytes = new List<byte>();
        for (var i = 0; i < 70000; i++) // generous bound; a real LOAD is far shorter
        {
            var (data, status) = server.Read(0x48, 0x60);
            bytes.Add(data);
            if ((status & VirtualDriveServer.SerialEof) != 0)
                break;
        }

        server.Untalk(0x48, 0x60);
        return bytes;
    }

    [Fact]
    public void Load_Star_StreamsFirstProgramWithEoiOnLastByte()
    {
        var image = BuildDisk();
        var server = new VirtualDriveServer(d => d == 8 ? image : null);

        var bytes = LoadOverIec(server, "*");

        bytes.Should().Equal(PrgStream);
    }

    [Fact]
    public void Load_ByName_StreamsProgram()
    {
        var image = BuildDisk();
        var server = new VirtualDriveServer(d => d == 8 ? image : null);

        var bytes = LoadOverIec(server, "TEST");

        bytes.Should().Equal(PrgStream);
    }

    [Fact]
    public void Load_Directory_StreamsListing()
    {
        var image = BuildDisk();
        var server = new VirtualDriveServer(d => d == 8 ? image : null);

        var bytes = LoadOverIec(server, "$");

        bytes[0].Should().Be(0x01); // $0401 load address
        bytes[1].Should().Be(0x04);
        bytes.Count.Should().BeGreaterThan(4);
    }

    [Fact]
    public void Read_BeforeOpen_ReturnsImmediateEof()
    {
        var image = BuildDisk();
        var server = new VirtualDriveServer(d => d == 8 ? image : null);

        var (data, status) = server.Read(0x48, 0x60);

        data.Should().Be(0);
        (status & VirtualDriveServer.SerialEof).Should().NotBe(0);
    }

    [Fact]
    public void HasDisk_ReflectsResolver()
    {
        var image = BuildDisk();
        var server = new VirtualDriveServer(d => d == 8 ? image : null);

        server.HasDisk(8).Should().BeTrue();
        server.HasDisk(9).Should().BeFalse();
    }
}
