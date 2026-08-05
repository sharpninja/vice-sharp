using FluentAssertions;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Tape;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness.IEC;

/// <summary>
/// T64 tape-container support: extract first PRG and materialize a loadable single-file D64.
/// </summary>
public sealed class T64ImageTests
{
    /// <summary>Builds a minimal valid T64 with one PRG entry (load $0801, 8 data bytes).</summary>
    private static byte[] BuildMinimalT64()
    {
        // Header 64 bytes + 1 directory entry * 32 + data
        const int maxEntries = 1;
        var data = new byte[64 + (32 * maxEntries) + 8];
        // Signature
        var sig = "C64S tape image file"u8.ToArray();
        sig.CopyTo(data.AsSpan(0));
        // version 1.1, max entries 1, used 1
        data[0x20] = 0x01;
        data[0x21] = 0x01;
        data[0x22] = 0x01;
        data[0x23] = 0x00;
        data[0x24] = 0x01;
        data[0x25] = 0x00;
        // tape name
        "TEST TAPE"u8.CopyTo(data.AsSpan(0x28));

        int entry = 0x40;
        data[entry] = 0x01; // normal tape file
        data[entry + 1] = 0x82; // PRG
        data[entry + 2] = 0x01; // load lo $0801
        data[entry + 3] = 0x08;
        data[entry + 4] = 0x09; // end $0809
        data[entry + 5] = 0x08;
        int dataOffset = 64 + 32;
        data[entry + 8] = (byte)(dataOffset & 0xFF);
        data[entry + 9] = (byte)((dataOffset >> 8) & 0xFF);
        "HELLO"u8.CopyTo(data.AsSpan(entry + 0x10));

        // program body (no load address stored in body)
        for (var i = 0; i < 8; i++)
        {
            data[dataOffset + i] = (byte)(0x10 + i);
        }

        return data;
    }

    [Fact]
    public void TryOpen_And_ExtractFirstProgram()
    {
        byte[] t64 = BuildMinimalT64();
        T64Image.TryOpen(t64, out T64Image? image).Should().BeTrue();
        image!.TryExtractFirstProgram(out byte[] prg).Should().BeTrue();
        prg.Should().HaveCount(10);
        prg[0].Should().Be(0x01);
        prg[1].Should().Be(0x08);
        prg[2].Should().Be(0x10);
        prg[9].Should().Be(0x17);
    }

    [Fact]
    public void FromPrg_BuildsD64ThatIecAccepts()
    {
        byte[] t64 = BuildMinimalT64();
        T64Image.TryOpen(t64, out T64Image? image).Should().BeTrue();
        image!.TryExtractFirstProgram(out byte[] prg).Should().BeTrue();

        byte[] d64 = D64SingleFileBuilder.FromPrg(prg, "HELLO");
        d64.Should().HaveCount(D64Image.DiskSize35Track);
        IecD64Attachment.TryAttach(8, d64, out _).Should().BeTrue();

        var fs = new D64FileSystem(new D64Image(d64));
        fs.TryFindFile("*"u8, out D64DirectoryEntry entry).Should().BeTrue();
        entry.StartTrack.Should().BeGreaterThan(0);
        byte[] stream = fs.ReadFileStream(entry.StartTrack, entry.StartSector);
        stream.Should().Equal(prg);
    }
}
