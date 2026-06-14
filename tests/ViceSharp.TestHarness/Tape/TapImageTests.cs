namespace ViceSharp.TestHarness.Tape;

using FluentAssertions;
using ViceSharp.Chips.Tape;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-TAPE (BACKFILL-TAPE chip-level tests).
/// Use case: TapImage parses the .TAP container header and exposes the raw
/// pulse stream; TapPulseReader iterates pulses honoring the v0/v1 format
/// rules (non-zero byte = byte * 8 cycles, zero byte in v1 = 24-bit LE
/// long pulse). Coverage backfill for RUNTIME-TAPE-001's chip surface.
/// </summary>
public sealed class TapImageTests
{
    private const string Signature = "C64-TAPE-RAW";

    /// <summary>
    /// FR/TR: FR-TAPE
    /// Use case: A well-formed 20-byte TAP header followed by a declared
    /// pulse-data block loads successfully and exposes the version and
    /// pulse-data length to the caller.
    /// Acceptance: TryAttach returns true; Version == 0; PulseDataLength ==
    /// declared size (0x10).
    /// </summary>
    [Fact]
    public void TryAttach_ValidHeaderV0_ParsesVersionAndLength()
    {
        var image = BuildTapImage(version: 0, pulseData: new byte[0x10]);

        var ok = TapImage.TryAttach(image, out var tap);

        ok.Should().BeTrue();
        tap.Should().NotBeNull();
        tap!.Version.Should().Be(0);
        tap.PulseDataLength.Should().Be(0x10);
    }

    /// <summary>
    /// FR/TR: FR-TAPE
    /// Use case: A buffer whose first 12 bytes are not the canonical
    /// "C64-TAPE-RAW" signature must be rejected so the datasette never
    /// pretends to play random bytes.
    /// Acceptance: TryAttach returns false; out image is null.
    /// </summary>
    [Fact]
    public void TryAttach_BadSignature_ReturnsFalse()
    {
        var bytes = new byte[24];
        // Wrong signature: "BAD-SIGNATURE" prefix
        var bad = "BAD-SIGNATUR"u8;
        bad.CopyTo(bytes.AsSpan(0, 12));
        // Plausible version + size to prove rejection is signature-driven.
        bytes[12] = 0;
        bytes[16] = 4;

        var ok = TapImage.TryAttach(bytes, out var tap);

        ok.Should().BeFalse();
        tap.Should().BeNull();
    }

    /// <summary>
    /// FR/TR: FR-TAPE
    /// Use case: Pulse iteration for v0/v1 short-form bytes returns
    /// byte * 8 host cycles per pulse, which matches VICE's TAP timing
    /// convention used by RUNTIME-TAPE-001.
    /// Acceptance: For pulse data [0x08, 0x10, 0x20], reader yields 64,
    /// 128, 256 cycles, then reports no more pulses.
    /// </summary>
    [Fact]
    public void PulseReader_ShortFormBytes_YieldsByteTimesEight()
    {
        var image = BuildTapImage(version: 0, pulseData: new byte[] { 0x08, 0x10, 0x20 });
        TapImage.TryAttach(image, out var tap).Should().BeTrue();

        var reader = tap!.CreatePulseReader();

        reader.TryReadNextPulse(out var p1).Should().BeTrue();
        p1.Should().Be(64);
        reader.TryReadNextPulse(out var p2).Should().BeTrue();
        p2.Should().Be(128);
        reader.TryReadNextPulse(out var p3).Should().BeTrue();
        p3.Should().Be(256);
        reader.TryReadNextPulse(out _).Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: FR-TAPE
    /// Use case: A datasette with a tape inserted but the motor or play
    /// button disengaged must NOT emit pulses, mirroring physical motor
    /// gating that the CIA1 timing depends on.
    /// Acceptance: With MotorEnabled=false (default), TryReadNextPulse
    /// returns false even though HasTape is true.
    /// </summary>
    [Fact]
    public void Datasette_MotorOff_DoesNotEmitPulses()
    {
        var datasette = new Datasette();
        datasette.InsertTape(BuildTapImage(version: 0, pulseData: new byte[] { 0x08, 0x10 }));
        datasette.PlayPressed = true;
        datasette.MotorEnabled = false;

        var emitted = datasette.TryReadNextPulse(out var cycles);

        emitted.Should().BeFalse();
        cycles.Should().Be(0);
        datasette.HasTape.Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: FR-TAPE
    /// Use case: With motor on and play pressed, the datasette streams
    /// pulses from the inserted tape image into the cycle-budget domain
    /// (consumed by the CIA1 FLAG line and KERNAL tape routines).
    /// Acceptance: TryReadNextPulse returns true and yields byte * 8
    /// cycles for the first pulse byte.
    /// </summary>
    [Fact]
    public void Datasette_MotorOnAndPlay_EmitsPulses()
    {
        var datasette = new Datasette();
        datasette.InsertTape(BuildTapImage(version: 0, pulseData: new byte[] { 0x08, 0x10 }));
        datasette.MotorEnabled = true;
        datasette.PlayPressed = true;

        var emitted = datasette.TryReadNextPulse(out var cycles);

        emitted.Should().BeTrue();
        cycles.Should().Be(64);
    }

    private static byte[] BuildTapImage(byte version, byte[] pulseData)
    {
        var buffer = new byte[20 + pulseData.Length];
        var sig = System.Text.Encoding.ASCII.GetBytes(Signature);
        sig.CopyTo(buffer, 0);
        buffer[12] = version;
        // bytes 13..15 are reserved/zero
        var len = pulseData.Length;
        buffer[16] = (byte)(len & 0xFF);
        buffer[17] = (byte)((len >> 8) & 0xFF);
        buffer[18] = (byte)((len >> 16) & 0xFF);
        buffer[19] = (byte)((len >> 24) & 0xFF);
        pulseData.CopyTo(buffer, 20);
        return buffer;
    }
}
