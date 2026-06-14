using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Tape;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

public sealed class StorageRuntimeTests
{
    /// <summary>
    /// FR: FR-DRV-001, FR: FR-DRV-004, TR: TR-CYCLE-001.
    /// Use case: After attaching a 35-track D64 image to drive 8 with a
    /// known byte pattern at track 18 sector 1, reading that sector back
    /// must return the exact byte pattern.
    /// Acceptance: TryAttach succeeds; the 256 bytes read from track 18
    /// sector 1 match the seeded descending pattern (sector[0]=255,
    /// sector[127]=128, sector[255]=0).
    /// </summary>
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

    /// <summary>
    /// FR: FR-DRV-001, TR: TR-CYCLE-001.
    /// Use case: A D64 image whose length does not match the canonical
    /// 35-track size must be rejected by the attach helper.
    /// Acceptance: TryAttach returns false and the out attachment is
    /// null when the byte array is one byte shorter than expected.
    /// </summary>
    [Fact]
    public void D64_Attach_Rejects_Invalid_Size()
    {
        Assert.False(IecD64Attachment.TryAttach(8, new byte[D64Image.DiskSize35Track - 1], out var attachment));
        Assert.Null(attachment);
    }

    /// <summary>
    /// FR: FR-DRV-001, FR: FR-CFG-005, TR: TR-CYCLE-001.
    /// Use case: Reading the first PRG on a D64 image must locate the
    /// directory entry, follow the PRG chain, strip the two-byte load
    /// header, and report the parsed program.
    /// Acceptance: TryReadFirstProgram succeeds, returns a Program whose
    /// FileName matches the seeded name, LoadAddress is $0801, Payload
    /// equals the PRG body, and EndAddress matches Payload.Length.
    /// </summary>
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

    /// <summary>
    /// FR: FR-DRV-001, TR: TR-CYCLE-001.
    /// Use case: An empty D64 image with no directory entries must
    /// report a clear "no PRG" diagnostic rather than returning a
    /// bogus program.
    /// Acceptance: TryReadFirstProgram returns false, Program is null,
    /// and the error message mentions "PRG".
    /// </summary>
    [Fact]
    public void D64_FirstProgram_ReportsMissingPrg()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);

        Assert.False(image.TryReadFirstProgram(out var program, out var error));
        Assert.Null(program);
        Assert.Contains("PRG", error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-DRV-001, TR: TR-CYCLE-001.
    /// Use case: A PRG that exceeds one sector chains across multiple
    /// (track,sector) pairs; the loader must walk the chain and
    /// reassemble the full payload.
    /// Acceptance: TryReadFirstProgram succeeds, FileName/LoadAddress
    /// match the chain seed, and Payload equals the original
    /// pre-chunked bytes.
    /// </summary>
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

    /// <summary>
    /// FR: FR-DRV-001, TR: TR-CYCLE-001.
    /// Use case: A corrupt D64 may have a PRG chain that loops back
    /// onto itself; the loader must detect this rather than read
    /// forever.
    /// Acceptance: TryReadFirstProgram returns false, Program is null,
    /// and the error message mentions "loop".
    /// </summary>
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

    /// <summary>
    /// FR: FR-TAP-001, FR: FR-TAP-003, TR: TR-CYCLE-001.
    /// Use case: Attach a TAP v1 image to the datasette; pulse reads
    /// must remain blocked until both the motor is enabled and PLAY is
    /// pressed; then pulses come back in order including a 24-bit
    /// overflow pulse.
    /// Acceptance: With motor off, TryReadNextPulse returns false. With
    /// motor and play asserted, the first pulse equals 80 cycles, the
    /// next (overflow) returns 8000 cycles, then end-of-tape.
    /// </summary>
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

    /// <summary>
    /// FR: FR-TAP-002, FR: FR-TAP-003, TR: TR-CYCLE-001.
    /// Use case: A v0 TAP image uses a 0-byte overflow marker followed
    /// by a 24-bit cycle count for long pulses; the pulse reader must
    /// decode it correctly.
    /// Acceptance: First pulse returns 8000 cycles (matching the 24-bit
    /// little-endian value $001F40), then end-of-tape.
    /// </summary>
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

    /// <summary>
    /// FR: FR-TAP-002, TR: TR-CYCLE-001.
    /// Use case: A TAP image whose header pulse-data length is larger
    /// than the actual data must be rejected so the datasette never
    /// reads past the buffer.
    /// Acceptance: TryAttach returns false and the out image handle is
    /// null when the declared length exceeds the supplied pulse bytes.
    /// </summary>
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
