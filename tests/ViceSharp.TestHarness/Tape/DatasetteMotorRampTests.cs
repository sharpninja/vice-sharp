namespace ViceSharp.TestHarness.Tape;

using ViceSharp.Chips.Tape;
using Xunit;

/// <summary>
/// FR/TR: RUNTIME-TAPE-002 / TR-TAPE-EDGE-001.
/// Use case: The managed Datasette motor must enforce a spin-up ramp delay
/// before delivering tape pulses. On real hardware the Datasette motor
/// requires ~32ms to reach operating speed; in VICE this is modeled as a
/// fixed alarm delay of MOTOR_DELAY = 32,000 cycles (datasette/datasette.c:62).
/// The C64 clock runs at 985,248 Hz; the test uses the minimum IEC spec
/// ATN window (12,808 cycles = 13ms at 985,248 Hz) as the lower bound to
/// confirm the ramp is active.
///
/// Acceptance: After setting MotorEnabled = true (with PlayPressed = true
/// and a TAP image inserted), TryReadNextPulse must return false for at
/// least 12,808 cycles (ramp not yet complete). After the full 32,000-cycle
/// ramp, TryReadNextPulse must return true.
///
/// VICE datasette/datasette.c:62: #define MOTOR_DELAY 32000.
/// </summary>
public sealed class DatasetteMotorRampTests
{
    // VICE MOTOR_DELAY constant: datasette/datasette.c:62.
    // 32,000 cycles = ~32ms at C64 PAL 985,248 Hz clock.
    private const int MotorRampCycles = 32_000;

    // Minimum spec lower bound for the ramp (13ms at 985,248 Hz = 12,808 cycles).
    // TryReadNextPulse must still return false at this point during ramp.
    private const int MinRampCycles = 12_808;

    private static byte[] BuildMinimalTap()
    {
        // Minimal TAP v0 image: 20-byte header + one 0x10 byte pulse (16 * 8 = 128 cycles).
        var bytes = new byte[21];
        var sig = System.Text.Encoding.ASCII.GetBytes("C64-TAPE-RAW");
        sig.CopyTo(bytes, 0);
        bytes[12] = 0;     // version 0
        bytes[16] = 1;     // data length = 1
        bytes[20] = 0x10;  // pulse byte = 16 (16 * 8 = 128 cycles)
        return bytes;
    }

    /// <summary>
    /// TR-TAPE-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: Motor must enforce ramp-up delay before pulse delivery.
    /// Without a TAP image, TryReadNextPulse must always return false
    /// (no tape) regardless of motor and play state.
    /// Acceptance: After MotorEnabled=true + PlayPressed=true, TryReadNextPulse
    /// returns false when no tape is inserted.
    /// </summary>
    [Fact]
    public void NoTape_TryReadNextPulse_ReturnsFalse_Regardless()
    {
        var ds = new Datasette();
        ds.MotorEnabled = true;
        ds.PlayPressed = true;

        for (var i = 0; i < MotorRampCycles + 1; i++)
            ds.Tick();

        Assert.False(ds.TryReadNextPulse(out _),
            "TryReadNextPulse must return false when no tape is inserted (VICE: reader is null).");
    }

    /// <summary>
    /// TR-TAPE-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: After setting MotorEnabled = true with a tape inserted and
    /// PlayPressed = true, TryReadNextPulse must return false for at least
    /// MinRampCycles (12,808) cycles - the ramp is still active.
    /// Acceptance: After 12,808 Tick() calls, TryReadNextPulse still returns false.
    /// VICE datasette.c:62 MOTOR_DELAY=32000 > 12808 so ramp is active at 12808.
    /// </summary>
    [Fact]
    public void MotorOn_WithTape_PulseBlockedDuringRamp()
    {
        var ds = new Datasette();
        ds.InsertTape(BuildMinimalTap());
        ds.MotorEnabled = true;
        ds.PlayPressed = true;

        for (var i = 0; i < MinRampCycles; i++)
            ds.Tick();

        Assert.False(ds.TryReadNextPulse(out _),
            $"TryReadNextPulse must return false during the motor ramp " +
            $"(first {MinRampCycles} cycles; VICE MOTOR_DELAY=32000 datasette.c:62).");
    }

    /// <summary>
    /// TR-TAPE-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: After the full 32,000-cycle motor ramp, TryReadNextPulse
    /// must return true (the motor is at speed and the tape delivers pulses).
    /// Acceptance: After MotorRampCycles Tick() calls, TryReadNextPulse returns true.
    /// VICE datasette.c:62 MOTOR_DELAY=32000.
    /// </summary>
    [Fact]
    public void MotorOn_WithTape_PulseAvailable_AfterFullRamp()
    {
        var ds = new Datasette();
        ds.InsertTape(BuildMinimalTap());
        ds.MotorEnabled = true;
        ds.PlayPressed = true;

        for (var i = 0; i < MotorRampCycles; i++)
            ds.Tick();

        Assert.True(ds.TryReadNextPulse(out _),
            $"TryReadNextPulse must return true after the {MotorRampCycles}-cycle motor ramp " +
            "(VICE MOTOR_DELAY=32000 datasette.c:62).");
    }

    /// <summary>
    /// TR-TAPE-EDGE-001 / RUNTIME-TAPE-002.
    /// Use case: If MotorEnabled is set to false during or after the ramp,
    /// the ramp counter resets. Setting motor off then on requires a full
    /// new ramp before pulses are delivered.
    /// Acceptance: After a complete ramp, motor off + motor on + 12,808 ticks,
    /// TryReadNextPulse still returns false (ramp reset).
    /// VICE datasette.c:1196 (motor stop: motor_stop_clk = MOTOR_DELAY).
    /// </summary>
    [Fact]
    public void MotorOff_ThenOn_ResetsRamp()
    {
        var ds = new Datasette();
        ds.InsertTape(BuildMinimalTap());
        ds.MotorEnabled = true;
        ds.PlayPressed = true;

        // Complete the ramp.
        for (var i = 0; i < MotorRampCycles; i++)
            ds.Tick();
        Assert.True(ds.TryReadNextPulse(out _), "Precondition: pulse available after ramp.");

        // Motor off resets ramp.
        ds.Rewind();
        ds.MotorEnabled = false;
        ds.Tick();
        ds.MotorEnabled = true;

        // First 12,808 cycles after motor re-enable: still in ramp.
        for (var i = 0; i < MinRampCycles; i++)
            ds.Tick();

        Assert.False(ds.TryReadNextPulse(out _),
            "Motor off then on must restart the ramp from zero " +
            "(VICE datasette.c motor_stop_clk reset on motor off).");
    }
}
