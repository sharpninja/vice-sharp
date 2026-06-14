namespace ViceSharp.TestHarness.Tape;

using FluentAssertions;
using ViceSharp.Chips.Tape;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: RUNTIME-TAPE-002 / TR-TAP-EDGE-001.
/// Use case: The Datasette sense line and record mode.
///
/// SENSE line: The SENSE signal from the Datasette is physically connected to
/// CIA1 Port B bit 4 ($DC01 bit 4). When the PLAY or RECORD button is
/// pressed, SENSE goes low (bit 4 = 0). When no button is pressed, SENSE
/// is high (bit 4 = 1). The managed Datasette exposes this via a SenseLine
/// property: true = not-pressed (high), false = pressed (low).
/// VICE datasette/datasette.c: sense line state driven by PLAY/RECORD state.
///
/// Record mode: When RecordPressed = true, TryWritePulse(cycles) stores a
/// pulse value for later playback or analysis. This models the Datasette
/// record head path (write mode, used by fast-save routines).
/// VICE datasette/datasette.c: record mode stores pulses to image buffer.
/// </summary>
public sealed class DatasetteSenseLineTests
{
    /// <summary>
    /// TR-TAP-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: SenseLine must be true (high) when no Datasette buttons are
    /// pressed. This is the idle state; CIA1 $DC01 bit 4 reads as 1.
    /// Acceptance: After Reset(), SenseLine = true (no button pressed).
    /// VICE datasette.c: sense line defaults to not-pressed (high).
    /// </summary>
    [Fact]
    public void SenseLine_True_WhenNoButtonPressed()
    {
        var ds = new Datasette();
        ds.Reset();

        ds.SenseLine.Should().BeTrue(
            "SenseLine must be high (true) when PlayPressed = false " +
            "and RecordPressed = false (C64 CIA1 $DC01 bit 4 = 1; VICE datasette.c sense line).");
    }

    /// <summary>
    /// TR-TAP-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: SenseLine must be false (low) when PlayPressed is true.
    /// This is the PLAY state; CIA1 $DC01 bit 4 reads as 0.
    /// Acceptance: After PlayPressed = true, SenseLine = false.
    /// VICE datasette.c: sense line goes low when PLAY is pressed.
    /// </summary>
    [Fact]
    public void SenseLine_False_WhenPlayPressed()
    {
        var ds = new Datasette();
        ds.Reset();
        ds.PlayPressed = true;

        ds.SenseLine.Should().BeFalse(
            "SenseLine must be low (false) when PlayPressed = true " +
            "(CIA1 $DC01 bit 4 = 0; VICE datasette.c sense line active-low).");
    }

    /// <summary>
    /// TR-TAP-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: SenseLine must be false (low) when RecordPressed is true.
    /// RECORD also asserts the SENSE line (same physical signal as PLAY).
    /// Acceptance: After RecordPressed = true, SenseLine = false.
    /// VICE datasette.c: RECORD state asserts sense line just as PLAY does.
    /// </summary>
    [Fact]
    public void SenseLine_False_WhenRecordPressed()
    {
        var ds = new Datasette();
        ds.Reset();
        ds.RecordPressed = true;

        ds.SenseLine.Should().BeFalse(
            "SenseLine must be low (false) when RecordPressed = true " +
            "(CIA1 $DC01 bit 4 = 0; VICE datasette.c RECORD active-low sense).");
    }

    /// <summary>
    /// TR-TAP-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: SenseLine returns to true after Play is released.
    /// Acceptance: PlayPressed = true -> false: SenseLine transitions false -> true.
    /// </summary>
    [Fact]
    public void SenseLine_ReturnsHigh_WhenPlayReleased()
    {
        var ds = new Datasette();
        ds.Reset();
        ds.PlayPressed = true;
        ds.SenseLine.Should().BeFalse("precondition: play pressed.");

        ds.PlayPressed = false;

        ds.SenseLine.Should().BeTrue(
            "SenseLine must return high when Play is released " +
            "(CIA1 $DC01 bit 4 returns to 1).");
    }

    /// <summary>
    /// TR-TAP-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: TryWritePulse stores the cycle count when RecordPressed is true.
    /// Record mode stores pulses to an internal buffer for later inspection
    /// (models the Datasette write head writing to tape).
    /// Acceptance: RecordPressed = true, TryWritePulse(128) returns true;
    /// RecordedPulseCount increases by 1.
    /// VICE datasette.c: record mode accumulates pulses to image buffer.
    /// </summary>
    [Fact]
    public void TryWritePulse_StorePulse_WhenRecordPressed()
    {
        var ds = new Datasette();
        ds.Reset();
        ds.RecordPressed = true;
        ds.MotorEnabled = true;
        var countBefore = ds.RecordedPulseCount;

        var result = ds.TryWritePulse(128);

        result.Should().BeTrue("TryWritePulse must return true when RecordPressed and MotorEnabled.");
        ds.RecordedPulseCount.Should().Be(countBefore + 1,
            "RecordedPulseCount must increment after each successful TryWritePulse " +
            "(VICE datasette.c record buffer append).");
    }

    /// <summary>
    /// TR-TAP-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: TryWritePulse returns false when RecordPressed is false.
    /// You cannot write to the tape without the record mode engaged.
    /// Acceptance: RecordPressed = false, TryWritePulse returns false.
    /// </summary>
    [Fact]
    public void TryWritePulse_ReturnsFalse_WhenNotRecording()
    {
        var ds = new Datasette();
        ds.Reset();
        ds.RecordPressed = false;
        ds.MotorEnabled = true;

        var result = ds.TryWritePulse(128);

        result.Should().BeFalse(
            "TryWritePulse must return false when RecordPressed = false " +
            "(VICE datasette.c: write only active in record mode).");
    }

    /// <summary>
    /// TR-TAP-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: TryWritePulse returns false when motor is not enabled,
    /// even if RecordPressed is true.
    /// Acceptance: RecordPressed = true, MotorEnabled = false -> TryWritePulse returns false.
    /// VICE datasette.c: motor must be running for any tape I/O.
    /// </summary>
    [Fact]
    public void TryWritePulse_ReturnsFalse_WhenMotorOff()
    {
        var ds = new Datasette();
        ds.Reset();
        ds.RecordPressed = true;
        ds.MotorEnabled = false;

        var result = ds.TryWritePulse(128);

        result.Should().BeFalse(
            "TryWritePulse must return false when motor is off " +
            "(VICE datasette.c: motor required for tape write).");
    }
}
