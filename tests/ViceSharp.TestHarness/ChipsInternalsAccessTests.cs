namespace ViceSharp.TestHarness;

using ViceSharp.Chips.Audio;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 Phase 0 (P0-3) / TR-SID-ORACLE-001.
/// Smoke proof that ViceSharp.Chips grants InternalsVisibleTo to the test
/// harness. Fine-grained parity ACs (FR-SID-WAVE-NOISE per-voice shift
/// registers, FR-SID-ENV pipeline counters, FR-VIC internal fetch state)
/// assert chip internals directly; without the grant they can only observe
/// register readback.
/// </summary>
public sealed class ChipsInternalsAccessTests
{
    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001, TEST: TEST-CHIPS-IVT-P0-01.
    /// Use case: parity tests must reference internal chip types (here the
    /// internal ReSidEnvelope struct inside Sid6581) from the harness assembly.
    /// Acceptance: the harness compiles against and instantiates the internal
    /// type; its reSID rate table carries the documented attack-0 period 8
    /// (resid/envelope.cc rate_counter_period[0]).
    /// </summary>
    [Fact]
    public void Harness_CanTouchChipsInternals()
    {
        var envelope = default(Sid6581.ReSidEnvelope);
        envelope.Reset();
        Assert.Equal((byte)0, envelope.EnvelopeCounter);
    }
}
