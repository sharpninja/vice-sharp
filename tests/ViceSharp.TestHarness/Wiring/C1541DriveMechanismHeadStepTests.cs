namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-WIRING-007 (Phase G2b).
/// Use case: A 1541 drive firmware steps the head via VIA2 PB0/PB1 4-phase
/// Gray code, controls the motor via PB2, and reads sector bytes from the
/// disk via PA. This test set exercises the head-step decoder, motor
/// transitions, and PA byte stream against a known D64 image.
/// </summary>
public sealed class C1541DriveMechanismHeadStepTests
{
    private static Via6522 BuildIsolatedVia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        return new Via6522(bus, irq) { BaseAddress = 0x1C00, Size = 0x0400 };
    }

    private static D64DiskImageDevice MakeImageWithDirectoryMarker()
    {
        var bytes = new byte[D64Image.DiskSize35Track];
        // Sector 18/0 is the BAM; mark first 4 bytes with a recognisable pattern.
        // Sector offset for track 18 sector 0: 17 tracks * 21 sectors_avg... use
        // the D64Image SectorOffset internally; here we just inject via track 18.
        // Track 18 sector 0 starts at offset 0x16500 in the 35-track layout.
        bytes[0x16500] = 0xCA;
        bytes[0x16501] = 0xFE;
        bytes[0x16502] = 0xBA;
        bytes[0x16503] = 0xBE;
        return new D64DiskImageDevice(new D64Image(bytes));
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: Writing PB2=1 (motor on) starts the motor and exposes the
    /// VICE-style GCR track stream for track 18.
    /// Acceptance: Reading PA after motor-on returns sync, then the encoded
    /// sector-0 data mark and first three BAM bytes at the VICE data offset.
    /// </summary>
    [Fact]
    public void MotorOn_PaRead_StreamsBytesFromTrack18Sector0()
    {
        var via = BuildIsolatedVia();
        var disk = MakeImageWithDirectoryMarker();
        new C1541DriveMechanismDevice(disk).ConnectVia2(via);
        // DDRA left at default 0 (all inputs) so PortAInput surfaces.

        // PB2 set to enable motor; PB0/PB1 match VICE half-track 36's coil phase.
        via.Write(0x1C02, 0xFF); // DDRB outputs
        via.Write(0x1C00, 0x06); // PB = motor on, phase 2

        var expected = new byte[5];
        GcrCodec.EncodeBlock(new byte[] { 0x07, 0xCA, 0xFE, 0xBA }, expected);
        ReadUntilSequence(via, expected, 8_000).Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: VICE stores a checksum byte after the 256 sector payload bytes
    /// in the 325-byte GCR data block.
    /// Acceptance: the generated track stream decodes sector 18/0 with the
    /// payload bytes and XOR checksum matching the source D64 sector.
    /// </summary>
    [Fact]
    public void GcrDataBlock_Track18Sector0_ContainsViceChecksum()
    {
        var disk = MakeImageWithDirectoryMarker();
        var image = disk.Image;
        var state = new C1541DriveMechanismDevice.DriveHeadState();
        EnableMotorWithoutSeek(state);

        AssertGcrDataBlockMatchesSector(state, image, 18, 0, "track 18 sector 0");
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007.
    /// Use case: Program file loads advance from the first track-1 sector to
    /// later sectors on the same track, so the generated track stream must keep
    /// the full payload and checksum intact beyond the first sector.
    /// Acceptance: a direct GCR scan of track 1 decodes sector 20 with all 256
    /// payload bytes and the VICE-style XOR checksum matching the D64 sector.
    /// </summary>
    [Fact]
    public void GcrDataBlock_Track1Sector20_ContainsViceChecksum()
    {
        var bytes = new byte[D64Image.DiskSize35Track];
        for (var i = 0; i < 256; i++)
            bytes[20 * 256 + i] = (byte)(i * 37 + 11);
        var image = new D64Image(bytes);
        var state = new C1541DriveMechanismDevice.DriveHeadState();
        EnableMotorWithoutSeek(state);
        for (var i = 0; i < 17; i++)
            StepOutOneTrack(state);

        AssertGcrDataBlockMatchesSector(state, image, 1, 20, "track 1 sector 20");
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007.
    /// Use case: VICE treats a run of sync bits as SYNC; an isolated encoded
    /// GCR byte value of $FF inside a data block is still payload and must
    /// raise byte-ready.
    /// Acceptance: after reading the encoded group for payload bytes
    /// F5 45 ED 50, whose GCR form contains an isolated $FF byte, IsSync is
    /// false on that $FF byte.
    /// </summary>
    [Fact]
    public void IsSync_IsFalseForIsolatedDataByteFF()
    {
        var bytes = new byte[D64Image.DiskSize35Track];
        var sectorOffset = 20 * 256;
        bytes[sectorOffset + 0] = 0x01;
        bytes[sectorOffset + 1] = 0x00;
        bytes[sectorOffset + 115] = 0xF5;
        bytes[sectorOffset + 116] = 0x45;
        bytes[sectorOffset + 117] = 0xED;
        bytes[sectorOffset + 118] = 0x50;
        var image = new D64Image(bytes);
        var state = new C1541DriveMechanismDevice.DriveHeadState();
        EnableMotorWithoutSeek(state);
        for (var i = 0; i < 17; i++)
            StepOutOneTrack(state);

        Span<byte> expected = stackalloc byte[5];
        GcrCodec.EncodeBlock([0xF5, 0x45, 0xED, 0x50], expected);
        expected[2].Should().Be(0xFF);

        var matched = 0;
        for (var i = 0; i < 8_000; i++)
        {
            var value = state.ReadNextByte(image);
            if (value == expected[matched])
            {
                matched++;
                if (matched == 3)
                {
                    value.Should().Be(0xFF);
                    state.IsSync(image).Should().BeFalse();
                    return;
                }
            }
            else
            {
                matched = value == expected[0] ? 1 : 0;
            }
        }

        Assert.Fail("the generated track stream did not contain the expected isolated GCR $FF payload byte");
    }

    private static void AssertGcrDataBlockMatchesSector(
        C1541DriveMechanismDevice.DriveHeadState state,
        D64Image image,
        int track,
        int sectorNumber,
        string because)
    {
        var captured = new byte[8_000];
        for (var i = 0; i < captured.Length; i++)
            captured[i] = state.ReadNextByte(image);

        Span<byte> encoded = stackalloc byte[5];
        Span<byte> decoded = stackalloc byte[260];
        var found = false;
        var expectedSector = image.GetSector(track, sectorNumber);
        for (var start = 0; start + 324 < captured.Length; start++)
        {
            for (var group = 0; group < 65; group++)
            {
                captured.AsSpan(start + group * 5, 5).CopyTo(encoded);
                GcrCodec.DecodeBlock(encoded, decoded.Slice(group * 4, 4));
            }

            if (decoded[0] != 0x07 || decoded[1] != expectedSector[0] || decoded[2] != expectedSector[1] || decoded[3] != expectedSector[2])
                continue;

            found = true;
            byte expectedChecksum = 0;
            for (var i = 0; i < 256; i++)
            {
                decoded[1 + i].Should().Be(expectedSector[i], $"decoded sector byte {i} should match D64 {because}");
                expectedChecksum ^= expectedSector[i];
            }

            decoded[257].Should().Be(expectedChecksum);
            break;
        }

        found.Should().BeTrue($"the generated GCR stream should contain {because}");
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: With motor off, PA reads return the high-Z default (0xFF)
    /// regardless of disk mount.
    /// Acceptance: PA read with PB2=0 returns 0xFF.
    /// </summary>
    [Fact]
    public void MotorOff_PaRead_ReturnsHighZ()
    {
        var via = BuildIsolatedVia();
        var disk = MakeImageWithDirectoryMarker();
        new C1541DriveMechanismDevice(disk).ConnectVia2(via);
        via.Write(0x1C02, 0xFF);
        via.Write(0x1C00, 0x00); // motor off

        via.Read(0x1C01).Should().Be(0xFF);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: VICE derives the old stepper position from the current
    /// half-track and moves one half-track per adjacent coil transition.
    /// Acceptance: two inward adjacent transitions move half-track 36 to 38
    /// and logical track 18 to 19.
    /// </summary>
    [Fact]
    public void HeadStep_InwardGraySequence_IncrementsTrack()
    {
        var state = new C1541DriveMechanismDevice.DriveHeadState();
        state.Track.Should().Be(18);
        state.HalfTrack.Should().Be(36);

        EnableMotorWithoutSeek(state);
        StepInOneTrack(state);

        state.HalfTrack.Should().Be(38);
        state.Track.Should().Be(19);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: VICE derives the old stepper position from the current
    /// half-track and moves one half-track per adjacent coil transition.
    /// Acceptance: two outward adjacent transitions move half-track 36 to 34
    /// and logical track 18 to 17.
    /// </summary>
    [Fact]
    public void HeadStep_OutwardGraySequence_DecrementsTrack()
    {
        var state = new C1541DriveMechanismDevice.DriveHeadState();

        EnableMotorWithoutSeek(state);
        StepOutOneTrack(state);

        state.HalfTrack.Should().Be(34);
        state.Track.Should().Be(17);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: Track is clamped to [1, 35] - cannot step out below 1 or
    /// in beyond 35.
    /// Acceptance: Many outward steps from track 18 stop at 1; many inward
    /// at 35.
    /// </summary>
    [Fact]
    public void HeadStep_Clamps_AtTrack1_And_Track35()
    {
        var stateOut = new C1541DriveMechanismDevice.DriveHeadState();
        EnableMotorWithoutSeek(stateOut);
        for (int i = 0; i < 50; i++)
        {
            StepOutOneTrack(stateOut);
        }
        stateOut.Track.Should().Be(1);

        var stateIn = new C1541DriveMechanismDevice.DriveHeadState();
        EnableMotorWithoutSeek(stateIn);
        for (int i = 0; i < 50; i++)
        {
            StepInOneTrack(stateIn);
        }
        stateIn.Track.Should().Be(35);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: SectorCount per track follows the 1541 standard zone layout.
    /// Acceptance: 21 sectors on tracks 1-17, 19 on 18-24, 18 on 25-30, 17
    /// on 31-35.
    /// </summary>
    [Theory]
    [InlineData(1, 21)]
    [InlineData(17, 21)]
    [InlineData(18, 19)]
    [InlineData(24, 19)]
    [InlineData(25, 18)]
    [InlineData(30, 18)]
    [InlineData(31, 17)]
    [InlineData(35, 17)]
    public void SectorCount_FollowsZoneLayout(int track, int expected)
    {
        C1541DriveMechanismDevice.DriveHeadState.SectorCount(track).Should().Be(expected);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: The GCR track stream advances from one sector frame to the
    /// next using VICE's D64 track-18 frame size.
    /// Acceptance: After the sector-0 frame, sector 1 starts with sync and an
    /// encoded header whose sector byte is 1.
    /// </summary>
    [Fact]
    public void PaRead_AcrossSectorBoundary_AdvancesSector()
    {
        var via = BuildIsolatedVia();
        var bytes = new byte[D64Image.DiskSize35Track];
        // Tag start of sector 0 and sector 1 on track 18 with distinct markers.
        // Track 18 sector 0 @ 0x16500; sector 1 @ 0x16500 + 256 = 0x16600.
        bytes[0x16500] = 0x11;
        bytes[0x16600] = 0x22;
        var disk = new D64DiskImageDevice(new D64Image(bytes));
        new C1541DriveMechanismDevice(disk).ConnectVia2(via);

        via.Write(0x1C02, 0xFF);
        via.Write(0x1C00, 0x06); // motor on, phase 2

        var expected = new byte[5];
        GcrCodec.EncodeBlock(new byte[] { 0x08, 0x13, 0x01, 0x12 }, expected);
        ReadUntilSequence(via, expected, 8_000).Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: VICE preserves and scales GCR_head_offset when the head moves
    /// instead of restarting each newly selected half-track at byte zero.
    /// Acceptance: after advancing on track 18 and stepping to track 19, the
    /// next selected track keeps the same cursor because both tracks are in
    /// the same 1541 speed zone.
    /// </summary>
    [Fact]
    public void HeadStep_PreservesGcrCursorAcrossTrackChange()
    {
        var state = new C1541DriveMechanismDevice.DriveHeadState();
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        EnableMotorWithoutSeek(state);

        const int advancedBytes = 1024;
        for (var i = 0; i < advancedBytes; i++)
            state.ReadNextByte(image);

        StepInOneTrack(state);
        state.ReadCurrentByte(image);

        state.Track.Should().Be(19);
        GetPrivateInt(state, "_gcrByteIndex").Should().Be(advancedBytes);
    }

    private static byte[] ReadBytes(Via6522 via, int count)
    {
        var result = new byte[count];
        for (var i = 0; i < result.Length; i++)
            result[i] = via.Read(0x1C01);
        return result;
    }

    private static bool ReadUntilSequence(Via6522 via, ReadOnlySpan<byte> expected, int maxReads)
    {
        var matched = 0;
        for (var i = 0; i < maxReads; i++)
        {
            var value = via.Read(0x1C01);
            if (value == expected[matched])
            {
                matched++;
                if (matched == expected.Length)
                    return true;
            }
            else
            {
                matched = value == expected[0] ? 1 : 0;
            }
        }

        return false;
    }

    private static int GetPrivateInt(object target, string fieldName)
    {
        var field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.Should().NotBeNull();
        return (int)field!.GetValue(target)!;
    }

    private static void EnableMotorWithoutSeek(C1541DriveMechanismDevice.DriveHeadState state)
    {
        var phase = (state.HalfTrack - 2) & C1541DriveMechanismDevice.DriveHeadState.StepPhaseMask;
        state.UpdatePortBOutput(0x00, (byte)(C1541DriveMechanismDevice.DriveHeadState.MotorBit | phase));
    }

    private static void StepInOneTrack(C1541DriveMechanismDevice.DriveHeadState state)
    {
        var phase = (state.HalfTrack - 2) & C1541DriveMechanismDevice.DriveHeadState.StepPhaseMask;
        state.UpdatePortBOutput(
            (byte)(C1541DriveMechanismDevice.DriveHeadState.MotorBit | phase),
            (byte)(C1541DriveMechanismDevice.DriveHeadState.MotorBit | ((phase + 1) & C1541DriveMechanismDevice.DriveHeadState.StepPhaseMask)));
        phase = (state.HalfTrack - 2) & C1541DriveMechanismDevice.DriveHeadState.StepPhaseMask;
        state.UpdatePortBOutput(
            (byte)(C1541DriveMechanismDevice.DriveHeadState.MotorBit | phase),
            (byte)(C1541DriveMechanismDevice.DriveHeadState.MotorBit | ((phase + 1) & C1541DriveMechanismDevice.DriveHeadState.StepPhaseMask)));
    }

    private static void StepOutOneTrack(C1541DriveMechanismDevice.DriveHeadState state)
    {
        var phase = (state.HalfTrack - 2) & C1541DriveMechanismDevice.DriveHeadState.StepPhaseMask;
        state.UpdatePortBOutput(
            (byte)(C1541DriveMechanismDevice.DriveHeadState.MotorBit | phase),
            (byte)(C1541DriveMechanismDevice.DriveHeadState.MotorBit | ((phase - 1) & C1541DriveMechanismDevice.DriveHeadState.StepPhaseMask)));
        phase = (state.HalfTrack - 2) & C1541DriveMechanismDevice.DriveHeadState.StepPhaseMask;
        state.UpdatePortBOutput(
            (byte)(C1541DriveMechanismDevice.DriveHeadState.MotorBit | phase),
            (byte)(C1541DriveMechanismDevice.DriveHeadState.MotorBit | ((phase - 1) & C1541DriveMechanismDevice.DriveHeadState.StepPhaseMask)));
    }
}
