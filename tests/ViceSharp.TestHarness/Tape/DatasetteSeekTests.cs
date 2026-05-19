namespace ViceSharp.TestHarness.Tape;

using FluentAssertions;
using ViceSharp.Chips.Tape;
using Xunit;

/// <summary>
/// FR/TR: FR-TAPE (BACKFILL-TAPE rewind + seek).
/// Use case: Datasette exposes Rewind() and SeekTo(int pulseIndex) so the
/// host can rewind the tape to position 0 or jump to a specific pulse
/// index without ejecting and re-inserting. Mirrors real-hardware rewind
/// and fast-forward controls.
/// </summary>
public sealed class DatasetteSeekTests
{
    private const string Signature = "C64-TAPE-RAW";

    /// <summary>
    /// FR/TR: FR-TAPE (BACKFILL-TAPE rewind + seek)
    /// Use case: After playing some pulses, Rewind() returns the cursor to
    /// pulse 0 so the next read yields the first pulse again, mirroring a
    /// physical rewind to the start of tape.
    /// Acceptance: Insert [0x08, 0x10, 0x20]. Read two pulses. Rewind().
    /// Next TryReadNextPulse returns true with cycles == 64 (first pulse).
    /// </summary>
    [Fact]
    public void Rewind_ReturnsToFirstPulse()
    {
        var datasette = new Datasette();
        datasette.InsertTape(BuildTapImage(version: 0, pulseData: new byte[] { 0x08, 0x10, 0x20 }));
        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;

        datasette.TryReadNextPulse(out _).Should().BeTrue();
        datasette.TryReadNextPulse(out _).Should().BeTrue();

        datasette.Rewind();

        var emitted = datasette.TryReadNextPulse(out var cycles);
        emitted.Should().BeTrue();
        cycles.Should().Be(64);
    }

    /// <summary>
    /// FR/TR: FR-TAPE (BACKFILL-TAPE rewind + seek)
    /// Use case: SeekTo(0) is the indexed form of Rewind(): both must
    /// reposition the cursor at pulse 0 so callers can use either spelling.
    /// Acceptance: Insert [0x08, 0x10, 0x20]. Read one pulse. SeekTo(0)
    /// returns true. Next TryReadNextPulse returns cycles == 64.
    /// </summary>
    [Fact]
    public void SeekTo_Zero_IsEquivalentToRewind()
    {
        var datasette = new Datasette();
        datasette.InsertTape(BuildTapImage(version: 0, pulseData: new byte[] { 0x08, 0x10, 0x20 }));
        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;

        datasette.TryReadNextPulse(out _).Should().BeTrue();

        var seekOk = datasette.SeekTo(0);
        seekOk.Should().BeTrue();

        var emitted = datasette.TryReadNextPulse(out var cycles);
        emitted.Should().BeTrue();
        cycles.Should().Be(64);
    }

    /// <summary>
    /// FR/TR: FR-TAPE (BACKFILL-TAPE rewind + seek)
    /// Use case: SeekTo(1) skips the first pulse so the next read returns
    /// the second pulse, supporting fast-forward to a known offset.
    /// Acceptance: Insert [0x08, 0x10, 0x20]. SeekTo(1) returns true. Next
    /// TryReadNextPulse returns cycles == 128 (0x10 * 8).
    /// </summary>
    [Fact]
    public void SeekTo_One_SkipsFirstPulse()
    {
        var datasette = new Datasette();
        datasette.InsertTape(BuildTapImage(version: 0, pulseData: new byte[] { 0x08, 0x10, 0x20 }));
        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;

        var seekOk = datasette.SeekTo(1);
        seekOk.Should().BeTrue();

        var emitted = datasette.TryReadNextPulse(out var cycles);
        emitted.Should().BeTrue();
        cycles.Should().Be(128);
    }

    /// <summary>
    /// FR/TR: FR-TAPE (BACKFILL-TAPE rewind + seek)
    /// Use case: SeekTo with an index beyond the tape's pulse count must
    /// fail gracefully (return false) and leave the cursor unchanged so
    /// callers can probe boundaries without crashing or losing position.
    /// Acceptance: Insert 3-pulse tape. Read first pulse. SeekTo(100)
    /// returns false. Next TryReadNextPulse returns the SECOND pulse
    /// (cursor was not moved by the failed seek).
    /// </summary>
    [Fact]
    public void SeekTo_OutOfRange_ReturnsFalseAndLeavesCursorUnchanged()
    {
        var datasette = new Datasette();
        datasette.InsertTape(BuildTapImage(version: 0, pulseData: new byte[] { 0x08, 0x10, 0x20 }));
        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;

        datasette.TryReadNextPulse(out var first).Should().BeTrue();
        first.Should().Be(64);

        var seekOk = datasette.SeekTo(100);
        seekOk.Should().BeFalse();

        var emitted = datasette.TryReadNextPulse(out var cycles);
        emitted.Should().BeTrue();
        cycles.Should().Be(128);
    }

    /// <summary>
    /// FR/TR: FR-TAPE (BACKFILL-TAPE rewind + seek)
    /// Use case: Calling Rewind() on a datasette with no tape inserted
    /// must be a safe no-op so callers do not have to guard with HasTape.
    /// Acceptance: New Datasette (no tape). Rewind() does not throw.
    /// SeekTo(0) returns false. HasTape remains false.
    /// </summary>
    [Fact]
    public void Rewind_NoTape_IsNoOp()
    {
        var datasette = new Datasette();

        var rewind = () => datasette.Rewind();
        rewind.Should().NotThrow();

        datasette.SeekTo(0).Should().BeFalse();
        datasette.HasTape.Should().BeFalse();
    }

    private static byte[] BuildTapImage(byte version, byte[] pulseData)
    {
        var buffer = new byte[20 + pulseData.Length];
        var sig = System.Text.Encoding.ASCII.GetBytes(Signature);
        sig.CopyTo(buffer, 0);
        buffer[12] = version;
        var len = pulseData.Length;
        buffer[16] = (byte)(len & 0xFF);
        buffer[17] = (byte)((len >> 8) & 0xFF);
        buffer[18] = (byte)((len >> 16) & 0xFF);
        buffer[19] = (byte)((len >> 24) & 0xFF);
        pulseData.CopyTo(buffer, 20);
        return buffer;
    }
}
