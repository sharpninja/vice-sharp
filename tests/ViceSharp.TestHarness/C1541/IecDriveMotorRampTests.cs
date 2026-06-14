namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TRUEDRIVE-1541-002 / TR-DRV-EDGE-001.
/// Use case: The managed IecDrive motor ramp and head-sector simulation.
///
/// The Commodore 1541 disk drive motor requires approximately 300ms to spin
/// up to operating speed (rotational velocity = 5 revolutions/second for
/// tracks 1-17, through 3 revolutions/second for tracks 31-35). During the
/// ramp-up period the drive head cannot reliably read or write sectors.
/// The managed IecDrive.Tick() must model this ramp before enabling head
/// rotation tracking.
///
/// Motor ramp constant: ~300ms at the 1541 clock (1,000,000 Hz) = 300,000
/// drive cycles. This matches the Commodore 1541 hardware specification for
/// motor spin-up time.
///
/// BAM sector: D64 track 18, sector 0 contains the Block Availability Map.
/// The first two bytes are track-link (0x12) and sector-link (0x01) per the
/// D64 format specification. IecDrive.ReadSector(18, 0) must return these
/// bytes from an attached D64 image.
///
/// VICE reference:
///   drive/drive.c: drive_cpu_execute motor state tracking,
///   drive/iec/wd1770.c: head step timing (not modeled here - D64 emulation
///   uses sector-level reads without physical flux simulation).
/// </summary>
public sealed class IecDriveMotorRampTests
{
    // At 1,000,000 Hz drive clock, 300ms = 300,000 cycles.
    private const int MotorRampCycles = 300_000;

    private static D64Image MakeMinimalD64()
    {
        var bytes = new byte[D64Image.DiskSize35Track];
        // Tag track 18 sector 0 (BAM) with canonical D64 link bytes.
        // Offset for track 18 sector 0 in a 35-track D64 is 0x16500.
        bytes[0x16500] = 0x12; // next-track link = 18 (decimal)
        bytes[0x16501] = 0x01; // next-sector link = 1 (first directory sector)
        return new D64Image(bytes);
    }

    /// <summary>
    /// TR-DRV-EDGE-001 / FR-1541.
    /// Use case: When the motor is off, Tick() must not advance the head
    /// rotation counter. Verifies that IecDrive.MotorRotationCycles stays
    /// at 0 when the motor is not enabled.
    /// Acceptance: After 1000 Tick() calls with motor off, MotorRotationCycles
    /// remains 0.
    /// VICE drive/drive.c: motor off = no rotation tracking.
    /// </summary>
    [Fact]
    public void MotorOff_TickDoesNotAdvanceRotation()
    {
        var drive = new IecDrive(8);
        drive.Reset();
        // Motor is off after Reset.
        drive.MotorOn.Should().BeFalse();

        for (var i = 0; i < 1000; i++)
            drive.Tick();

        drive.MotorRotationCycles.Should().Be(0,
            "rotation must not advance when motor is off (VICE drive motor state machine).");
    }

    /// <summary>
    /// TR-DRV-EDGE-001 / FR-1541.
    /// Use case: When the motor is enabled, Tick() enters the ramp-up phase.
    /// During the ramp period (first 300,000 cycles after motor on), the
    /// head rotation counter must not advance (motor is spinning up).
    /// Acceptance: After SetMotor(true) + 299,999 Tick() calls, MotorRotationCycles
    /// is 0 (ramp not yet complete).
    /// VICE drive/drive.c: motor needs ramp time before reliable sector access.
    /// </summary>
    [Fact]
    public void MotorOn_HeadStationary_DuringRamp()
    {
        var drive = new IecDrive(8);
        drive.Reset();
        drive.SetMotor(true);

        // Tick through ramp-1 cycles: ramp must not complete yet.
        for (var i = 0; i < MotorRampCycles - 1; i++)
            drive.Tick();

        drive.MotorRotationCycles.Should().Be(0,
            "rotation must not start until ramp (300,000 cycles) is complete " +
            "(Commodore 1541 hardware 300ms spin-up spec).");
    }

    /// <summary>
    /// TR-DRV-EDGE-001 / FR-1541.
    /// Use case: After the 300,000-cycle ramp, the motor is at speed and
    /// the head rotation counter must start advancing with each Tick().
    /// Acceptance: After SetMotor(true) + 300,001 Tick() calls, MotorRotationCycles
    /// is greater than 0 (rotation has begun).
    /// VICE drive/drive.c: motor speed reached -> head rotation begins.
    /// </summary>
    [Fact]
    public void MotorOn_HeadAdvances_AfterRampComplete()
    {
        var drive = new IecDrive(8);
        drive.Reset();
        drive.SetMotor(true);

        for (var i = 0; i < MotorRampCycles + 1; i++)
            drive.Tick();

        drive.MotorRotationCycles.Should().BeGreaterThan(0,
            "rotation counter must advance after the 300,000-cycle motor ramp " +
            "(Commodore 1541 hardware 300ms spin-up spec).");
    }

    /// <summary>
    /// TR-DRV-EDGE-001 / FR-1541.
    /// Use case: Turning the motor off resets the ramp counter. After motor
    /// off + motor on, the drive must go through the full ramp again before
    /// rotation starts.
    /// Acceptance: After full ramp + motor off + 1 Tick + motor on + 299,999
    /// Tick calls, MotorRotationCycles is 0 (ramp restarted from zero).
    /// </summary>
    [Fact]
    public void MotorOff_ThenOn_ResetsRamp()
    {
        var drive = new IecDrive(8);
        drive.Reset();

        // Complete ramp + 1 extra rotation tick.
        drive.SetMotor(true);
        for (var i = 0; i < MotorRampCycles + 1; i++)
            drive.Tick();
        drive.MotorRotationCycles.Should().BeGreaterThan(0, "precondition: rotation started.");

        // Turn motor off and back on.
        drive.SetMotor(false);
        drive.Tick();
        drive.SetMotor(true);

        // 299,999 more ticks - ramp should restart from zero.
        for (var i = 0; i < MotorRampCycles - 1; i++)
            drive.Tick();

        drive.MotorRotationCycles.Should().Be(0,
            "motor ramp must restart from zero after motor off/on cycle.");
    }

    /// <summary>
    /// TR-DRV-EDGE-001 / FR-1541.
    /// Use case: IecDrive.ReadSector(18, 0) returns the BAM bytes from an
    /// attached D64 image. Track 18, sector 0 is the Block Availability Map;
    /// its first byte must be 0x12 (track-link) and second must be 0x01
    /// (sector-link) per the D64 format spec.
    /// Acceptance: ReadSector(18, 0) returns true; SectorBuffer[0] = 0x12;
    /// SectorBuffer[1] = 0x01 (D64 BAM marker bytes from the minimal image).
    /// VICE drive/iec/iecieee.c: sector read path for D64 images.
    /// </summary>
    [Fact]
    public void ReadSector_Track18Sector0_ReturnsBamLinkBytes()
    {
        var image = MakeMinimalD64();
        var drive = new IecDrive(8, image);

        var result = drive.ReadSector(18, 0);

        result.Should().BeTrue("ReadSector must succeed when a D64 image is attached.");
        drive.SectorBuffer[0].Should().Be(0x12,
            "BAM track-link (byte 0 of track 18 sector 0) must be 0x12 per D64 format.");
        drive.SectorBuffer[1].Should().Be(0x01,
            "BAM sector-link (byte 1 of track 18 sector 0) must be 0x01 per D64 format.");
    }
}
