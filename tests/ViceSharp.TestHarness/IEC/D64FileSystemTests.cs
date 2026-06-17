namespace ViceSharp.TestHarness.IEC;

using System.Linq;
using FluentAssertions;
using ViceSharp.Chips.IEC;
using Xunit;

/// <summary>
/// FR/TR: FR-IECVDRIVE-001 / TEST-IECVDRIVE-001.
/// Use case: the host-side virtual drive (used when True Drive is OFF) must read
/// a D64 image at the DOS level - enumerate the directory, resolve CBM filename
/// patterns, stream a file's bytes (including its 2-byte PRG load address), and
/// synthesise the LOAD"$" directory listing - mirroring VICE vdrive-iec/vdrive-dir.
/// </summary>
public sealed class D64FileSystemTests
{
    // "10 END" tokenised BASIC at $0801 (same as the true-drive load test).
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

        var bam = image.GetSector(18, 0);
        WritePadded(bam, 0x90, "GAMES", 16);
        WritePadded(bam, 0xA2, "I2", 5);
        // Mark a couple of free sectors so "BLOCKS FREE" is non-zero.
        bam[0x04 + (1 - 1) * 4] = 21;  // track 1: 21 free
        bam[0x04 + (2 - 1) * 4] = 19;  // track 2: 19 free

        var dir = image.GetSector(18, 1);
        dir[0] = 0x00; // no further directory sector
        dir[1] = 0xFF;
        WriteDirEntry(dir, 0, 0x82, 17, 0, "TEST", blocks: 1);
        WriteDirEntry(dir, 1, 0x82, 19, 5, "GAME", blocks: 5);

        var file = image.GetSector(17, 0);
        file[0] = 0x00;
        file[1] = (byte)(2 + PrgStream.Length - 1);
        PrgStream.CopyTo(file.Slice(2));

        return image;
    }

    private static void WriteDirEntry(System.Span<byte> dir, int index, byte type, byte track, byte sector, string name, int blocks)
    {
        var slot = 2 + index * 32;
        dir[slot + 0] = type;
        dir[slot + 1] = track;
        dir[slot + 2] = sector;
        for (var i = 0; i < 16; i++)
            dir[slot + 3 + i] = (byte)(i < name.Length ? name[i] : 0xA0);
        dir[slot + 0x1E] = (byte)(blocks & 0xFF);
        dir[slot + 0x1F] = (byte)(blocks >> 8);
    }

    private static void WritePadded(System.Span<byte> sector, int offset, string text, int length)
    {
        for (var i = 0; i < length; i++)
            sector[offset + i] = (byte)(i < text.Length ? text[i] : 0xA0);
    }

    [Fact]
    public void EnumerateDirectory_ReturnsLiveEntriesWithMetadata()
    {
        var fs = new D64FileSystem(BuildDisk());

        var entries = fs.EnumerateDirectory();

        entries.Should().HaveCount(2);
        entries[0].StartTrack.Should().Be(17);
        entries[0].StartSector.Should().Be(0);
        entries[0].Blocks.Should().Be(1);
        DecodeName(entries[0].NamePetscii).Should().Be("TEST");
        DecodeName(entries[1].NamePetscii).Should().Be("GAME");
        entries[1].Blocks.Should().Be(5);
    }

    [Fact]
    public void TryFindFile_Star_ReturnsFirstEntry()
    {
        var fs = new D64FileSystem(BuildDisk());

        fs.TryFindFile("*"u8, out var entry).Should().BeTrue();
        DecodeName(entry.NamePetscii).Should().Be("TEST");
    }

    [Fact]
    public void TryFindFile_ExactName_Matches()
    {
        var fs = new D64FileSystem(BuildDisk());

        fs.TryFindFile("GAME"u8, out var entry).Should().BeTrue();
        entry.StartTrack.Should().Be(19);
        entry.StartSector.Should().Be(5);
    }

    [Fact]
    public void TryFindFile_PrefixWildcard_Matches()
    {
        var fs = new D64FileSystem(BuildDisk());

        fs.TryFindFile("TE*"u8, out var entry).Should().BeTrue();
        DecodeName(entry.NamePetscii).Should().Be("TEST");
    }

    [Fact]
    public void TryFindFile_DrivePrefixAndOptions_AreStripped()
    {
        var fs = new D64FileSystem(BuildDisk());

        fs.TryFindFile("0:GAME,P,R"u8, out var entry).Should().BeTrue();
        DecodeName(entry.NamePetscii).Should().Be("GAME");
    }

    [Fact]
    public void TryFindFile_Missing_ReturnsFalse()
    {
        var fs = new D64FileSystem(BuildDisk());

        fs.TryFindFile("NOPE"u8, out _).Should().BeFalse();
    }

    [Fact]
    public void ReadFileStream_ReturnsRawPrgIncludingLoadAddress()
    {
        var fs = new D64FileSystem(BuildDisk());

        var bytes = fs.ReadFileStream(17, 0);

        bytes.Should().Equal(PrgStream);
        // First two bytes are the little-endian load address $0801.
        bytes[0].Should().Be(0x01);
        bytes[1].Should().Be(0x08);
    }

    [Fact]
    public void BuildDirectoryListing_HasLoadAddressNameAndTrailer()
    {
        var fs = new D64FileSystem(BuildDisk());

        var listing = fs.BuildDirectoryListing();

        // PRG load address $0401.
        listing[0].Should().Be(0x01);
        listing[1].Should().Be(0x04);

        // Disk name appears in the reverse-video header line.
        ContainsAscii(listing, "GAMES").Should().BeTrue();
        // Each file name is present.
        ContainsAscii(listing, "TEST").Should().BeTrue();
        ContainsAscii(listing, "GAME").Should().BeTrue();
        ContainsAscii(listing, "PRG").Should().BeTrue();
        // Free-blocks trailer and BASIC end-of-program marker.
        ContainsAscii(listing, "BLOCKS FREE.").Should().BeTrue();
        listing[^1].Should().Be(0x00);
        listing[^2].Should().Be(0x00);
    }

    private static string DecodeName(byte[] petscii)
    {
        var end = System.Array.IndexOf(petscii, (byte)0xA0);
        if (end < 0) end = petscii.Length;
        return System.Text.Encoding.ASCII.GetString(petscii, 0, end);
    }

    private static bool ContainsAscii(byte[] haystack, string needle)
    {
        var n = System.Text.Encoding.ASCII.GetBytes(needle);
        for (var i = 0; i + n.Length <= haystack.Length; i++)
        {
            var match = true;
            for (var j = 0; j < n.Length; j++)
            {
                if (haystack[i + j] != n[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }
}
