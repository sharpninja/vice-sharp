namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-1541 (RUNTIME-1541-002 D64 write).
/// Use case: A 1541 drive program issues a sector-write to a mounted D64.
/// The D64DiskImageDevice must persist the bytes into its in-memory image
/// so subsequent reads observe the new data, while leaving every other
/// sector untouched. Out-of-range track/sector requests are rejected so a
/// runaway drive program cannot corrupt the rest of the image.
/// Acceptance: WriteSector + read-back round-trip; multi-sector writes
/// remain independent; track 0/36, sector 21 on track 1 (etc.) raise
/// ArgumentOutOfRangeException; writing one sector leaves every other
/// sector byte-identical to its pre-write state.
/// </summary>
public sealed class D64SectorWriteTests
{
    private static D64DiskImageDevice MakeBlankDevice()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        return new D64DiskImageDevice(image);
    }

    private static byte[] MakePattern(byte seed)
    {
        var bytes = new byte[256];
        for (var i = 0; i < 256; i++)
            bytes[i] = (byte)((seed + i) & 0xFF);
        return bytes;
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002 D64 write).
    /// Use case: A drive program writes a 256-byte block to track 1
    /// sector 0; reading the same block back must return the bytes
    /// the program wrote.
    /// Acceptance: After WriteSector(1, 0, payload), the bytes pulled
    /// from D64Image.GetSector(1, 0) equal payload byte-for-byte.
    /// </summary>
    [Fact]
    public void WriteSector_ReadBack_RoundTrips()
    {
        var device = MakeBlankDevice();
        var payload = MakePattern(seed: 0x37);

        device.WriteSector(1, 0, payload);

        var actual = device.Image.GetSector(1, 0).ToArray();
        actual.Should().Equal(payload);
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002 D64 write).
    /// Use case: A drive program writes two different sectors on
    /// different tracks; each must retain its own payload regardless
    /// of write order.
    /// Acceptance: After writing payloadA to (5, 2) then payloadB to
    /// (20, 4), reads from (5, 2) return payloadA and reads from
    /// (20, 4) return payloadB.
    /// </summary>
    [Fact]
    public void WriteSector_MultipleSectors_AreIndependent()
    {
        var device = MakeBlankDevice();
        var payloadA = MakePattern(seed: 0x11);
        var payloadB = MakePattern(seed: 0xC3);

        device.WriteSector(5, 2, payloadA);
        device.WriteSector(20, 4, payloadB);

        device.Image.GetSector(5, 2).ToArray().Should().Equal(payloadA);
        device.Image.GetSector(20, 4).ToArray().Should().Equal(payloadB);
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002 D64 write).
    /// Use case: A garbled drive program asks to write to track 0 or
    /// track 36; the device must refuse rather than write past the
    /// image bounds.
    /// Acceptance: WriteSector with track 0 or track 36 throws
    /// ArgumentOutOfRangeException naming the track parameter.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(36)]
    [InlineData(-1)]
    [InlineData(100)]
    public void WriteSector_OutOfRangeTrack_Throws(int track)
    {
        var device = MakeBlankDevice();
        var payload = MakePattern(seed: 0x5A);

        var act = () => device.WriteSector(track, 0, payload);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("track");
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002 D64 write).
    /// Use case: Track 1 has 21 sectors numbered 0-20; track 31 has 17
    /// sectors numbered 0-16. A drive program that asks for sector 21
    /// on track 1 or sector 17 on track 31 must be refused.
    /// Acceptance: WriteSector with an out-of-range sector for the
    /// given track throws ArgumentOutOfRangeException naming sector.
    /// </summary>
    [Theory]
    [InlineData(1, 21)]   // tracks 1-17 have 21 sectors (0-20)
    [InlineData(17, 21)]
    [InlineData(18, 19)]  // tracks 18-24 have 19 sectors (0-18)
    [InlineData(25, 18)]  // tracks 25-30 have 18 sectors (0-17)
    [InlineData(31, 17)]  // tracks 31-35 have 17 sectors (0-16)
    [InlineData(35, 17)]
    [InlineData(1, -1)]
    public void WriteSector_OutOfRangeSector_Throws(int track, int sector)
    {
        var device = MakeBlankDevice();
        var payload = MakePattern(seed: 0x42);

        var act = () => device.WriteSector(track, sector, payload);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be("sector");
    }

    /// <summary>
    /// FR/TR: FR-1541 (RUNTIME-1541-002 D64 write).
    /// Use case: Pre-seed the entire image with a sentinel value, then
    /// write a single sector. Every other sector on every other track
    /// must retain the sentinel.
    /// Acceptance: After WriteSector(5, 3, payload), only the bytes at
    /// the (5, 3) sector offset differ from the sentinel; all other
    /// 174,592 bytes (174,848 - 256) still equal the sentinel.
    /// </summary>
    [Fact]
    public void WriteSector_DoesNotMutateOtherSectors()
    {
        const byte sentinel = 0xA5;
        var imageBytes = new byte[D64Image.DiskSize35Track];
        Array.Fill(imageBytes, sentinel);
        var device = new D64DiskImageDevice(new D64Image(imageBytes));
        var payload = MakePattern(seed: 0x77);

        device.WriteSector(5, 3, payload);

        // Compute the (5, 3) sector offset by walking the per-track
        // sector counts the same way D64Image does.
        var sectorOffset = 0;
        for (var t = 1; t < 5; t++)
            sectorOffset += SectorsPerTrack(t) * 256;
        sectorOffset += 3 * 256;

        var snapshot = device.Image.ToArray();
        for (var i = 0; i < snapshot.Length; i++)
        {
            if (i >= sectorOffset && i < sectorOffset + 256)
            {
                snapshot[i].Should().Be(payload[i - sectorOffset],
                    $"byte {i} is inside the written sector (5, 3)");
            }
            else
            {
                snapshot[i].Should().Be(sentinel,
                    $"byte {i} sits outside the written sector and must keep the sentinel");
            }
        }
    }

    private static int SectorsPerTrack(int track) => track switch
    {
        >= 1 and <= 17 => 21,
        >= 18 and <= 24 => 19,
        >= 25 and <= 30 => 18,
        >= 31 and <= 35 => 17,
        _ => 0,
    };
}
