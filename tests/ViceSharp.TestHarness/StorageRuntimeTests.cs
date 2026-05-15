using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Tape;
using Xunit;

namespace ViceSharp.TestHarness;

public sealed class StorageRuntimeTests
{
    [Fact]
    public void D64_Attach_And_ReadSector_Returns_Deterministic_Bytes()
    {
        var imageData = new byte[D64Image.DiskSize35Track];
        var sectorOffset = Track18Sector1Offset();
        for (var i = 0; i < 256; i++)
        {
            imageData[sectorOffset + i] = (byte)(255 - i);
        }

        Assert.True(IecD64Attachment.TryAttach(8, imageData, out var attachment));

        var sector = new byte[256];
        Assert.True(attachment!.TryReadSector(18, 1, sector));
        Assert.Equal(255, sector[0]);
        Assert.Equal(128, sector[127]);
        Assert.Equal(0, sector[255]);
    }

    [Fact]
    public void D64_Attach_Rejects_Invalid_Size()
    {
        Assert.False(IecD64Attachment.TryAttach(8, new byte[D64Image.DiskSize35Track - 1], out var attachment));
        Assert.Null(attachment);
    }

    [Fact]
    public void D64_FirstProgram_ReadsDirectoryEntryAndPrgPayload()
    {
        var programBytes = CreateBasicProgramPrg();
        var imageData = CreateD64WithFirstProgram("BOOT", programBytes);
        var image = new D64Image(imageData);

        Assert.True(image.TryReadFirstProgram(out var program, out var error), error);
        Assert.NotNull(program);
        Assert.Equal("BOOT", program!.FileName);
        Assert.Equal(0x0801, program.LoadAddress);
        Assert.Equal(programBytes[2..], program.Payload);
        Assert.Equal((ushort)(0x0801 + program.Payload.Length), program.EndAddress);
    }

    [Fact]
    public void D64_FirstProgram_ReportsMissingPrg()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);

        Assert.False(image.TryReadFirstProgram(out var program, out var error));
        Assert.Null(program);
        Assert.Contains("PRG", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void D64_FirstProgram_ReadsMultiSectorPrgChain()
    {
        var payload = Enumerable.Range(0, 400).Select(index => (byte)(index & 0xFF)).ToArray();
        var programBytes = new byte[payload.Length + 2];
        programBytes[0] = 0x01;
        programBytes[1] = 0x08;
        payload.CopyTo(programBytes.AsSpan(2));
        var image = new D64Image(CreateD64WithFirstProgram("CHAIN", programBytes));

        Assert.True(image.TryReadFirstProgram(out var program, out var error), error);
        Assert.NotNull(program);
        Assert.Equal("CHAIN", program!.FileName);
        Assert.Equal(0x0801, program.LoadAddress);
        Assert.Equal(payload, program.Payload);
    }

    [Fact]
    public void D64_FirstProgram_ReportsLoopedPrgChain()
    {
        var payload = Enumerable.Range(0, 400).Select(index => (byte)(index & 0xFF)).ToArray();
        var programBytes = new byte[payload.Length + 2];
        programBytes[0] = 0x01;
        programBytes[1] = 0x08;
        payload.CopyTo(programBytes.AsSpan(2));
        var image = new D64Image(CreateD64WithFirstProgram("LOOP", programBytes));
        image.WriteSectorByte(17, 1, 0, 17);
        image.WriteSectorByte(17, 1, 1, 1);

        Assert.False(image.TryReadFirstProgram(out var program, out var error));
        Assert.Null(program);
        Assert.Contains("loop", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tap_Attach_And_Datasette_ReadPulse_Requires_Motor_And_Play()
    {
        var tap = CreateTapImage(1, [10, 0, 0x40, 0x1F, 0x00]);

        Assert.True(TapImage.TryAttach(tap, out var image));

        var datasette = new Datasette();
        Assert.True(datasette.Attach(image!));
        Assert.True(datasette.HasTape);

        Assert.False(datasette.TryReadNextPulse(out _));

        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;

        Assert.True(datasette.TryReadNextPulse(out var shortPulse));
        Assert.Equal(80, shortPulse);

        Assert.True(datasette.TryReadNextPulse(out var overflowPulse));
        Assert.Equal(8000, overflowPulse);

        Assert.False(datasette.TryReadNextPulse(out _));
    }

    [Fact]
    public void Tap_V0_OverflowPulse_Reads_ThreeByteCycleCount()
    {
        var tap = CreateTapImage(0, [0, 0x40, 0x1F, 0x00]);

        Assert.True(TapImage.TryAttach(tap, out var image));

        var reader = image!.CreatePulseReader();
        Assert.True(reader.TryReadNextPulse(out var cycles));
        Assert.Equal(8000, cycles);
        Assert.False(reader.TryReadNextPulse(out _));
    }

    [Fact]
    public void Tap_Attach_Rejects_Truncated_Pulse_Data()
    {
        var tap = CreateTapImage(1, [10, 20]);
        tap[16] = 3;

        Assert.False(TapImage.TryAttach(tap, out var image));
        Assert.Null(image);
    }

    private static byte[] CreateTapImage(byte version, byte[] pulseData)
    {
        var image = new byte[20 + pulseData.Length];
        "C64-TAPE-RAW"u8.CopyTo(image);
        image[12] = version;
        image[16] = (byte)pulseData.Length;
        image[17] = (byte)(pulseData.Length >> 8);
        image[18] = (byte)(pulseData.Length >> 16);
        image[19] = (byte)(pulseData.Length >> 24);
        pulseData.CopyTo(image.AsSpan(20));
        return image;
    }

    private static int Track18Sector1Offset()
    {
        var offset = 0;
        for (var track = 1; track < 18; track++)
        {
            offset += 21 * 256;
        }

        return offset + 256;
    }

    private static byte[] CreateD64WithFirstProgram(string fileName, byte[] programBytes)
    {
        ReadOnlySpan<(int Track, int Sector)> sectors = [(17, 0), (17, 1), (17, 2)];
        if (programBytes.Length > sectors.Length * 254)
            throw new ArgumentOutOfRangeException(nameof(programBytes), "Test PRG exceeds the helper's sector chain.");

        var image = new D64Image();
        image.Format();

        const int directoryEntryOffset = 2;
        image.WriteSectorByte(18, 1, directoryEntryOffset, 0x82);
        image.WriteSectorByte(18, 1, directoryEntryOffset + 1, 17);
        image.WriteSectorByte(18, 1, directoryEntryOffset + 2, 0);

        for (var index = 0; index < 16; index++)
        {
            var value = index < fileName.Length
                ? (byte)char.ToUpperInvariant(fileName[index])
                : (byte)0xA0;
            image.WriteSectorByte(18, 1, directoryEntryOffset + 3 + index, value);
        }

        var written = 0;
        for (var sectorIndex = 0; written < programBytes.Length; sectorIndex++)
        {
            var (track, sector) = sectors[sectorIndex];
            var remaining = programBytes.Length - written;
            var chunkLength = Math.Min(254, remaining);
            var last = written + chunkLength >= programBytes.Length;
            var next = last ? default : sectors[sectorIndex + 1];

            image.WriteSectorByte(track, sector, 0, last ? (byte)0 : (byte)next.Track);
            image.WriteSectorByte(track, sector, 1, last ? (byte)(chunkLength + 1) : (byte)next.Sector);
            for (var offset = 0; offset < chunkLength; offset++)
                image.WriteSectorByte(track, sector, 2 + offset, programBytes[written + offset]);

            written += chunkLength;
        }

        return image.ToArray();
    }

    private static byte[] CreateBasicProgramPrg()
    {
        return
        [
            0x01, 0x08,
            0x0B, 0x08,
            0x0A, 0x00,
            0x99,
            0x22, (byte)'O', (byte)'K', 0x22,
            0x00,
            0x00, 0x00
        ];
    }
}
