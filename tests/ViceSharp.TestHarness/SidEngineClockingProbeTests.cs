using System;
using ViceSharp.Core;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// PLAN-VSFLOCKSTEP-001 SID-checkpoint step 1 (probe, exploratory).
///
/// Before an engine-sensitive SID checkpoint can mean anything, the native
/// reSID engine has to actually advance its internal state (phase accumulator,
/// ADSR envelope counter) as the shim is stepped. In VICE, reSID is normally
/// clocked by the sound-rendering path, not per CPU cycle - so in the headless
/// shim with no sound device, plain StepNative may leave reSID frozen. This
/// probe gates SID voice 3 and reports whether reSID's accumulator/envelope
/// move after stepping. It does not assert engine parity; it answers "is reSID
/// clocked?" so we know whether shim work is needed next.
/// </summary>
public sealed class SidEngineClockingProbeTests
{
    private readonly ITestOutputHelper _output;

    public SidEngineClockingProbeTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// FR: FR-SID-ENV, TR: TR-SID-ORACLE-001 (PLAN-VSFLOCKSTEP-001 SID-checkpoint probe).
    /// Use case: engine-sensitive SID checkpoints only mean something if the embedded
    /// reSID actually advances its internal state; VICE clocks reSID from the
    /// sound-rendering path, not per CPU cycle, so this probe answers "is reSID clocked
    /// in the headless shim?" before any parity work relies on it.
    /// Acceptance: after gating voice 3 (sawtooth, attack=4) and rendering ~88k cycles'
    /// worth of samples (16 x 256 samples at 22 cycles/sample) through the shim's render
    /// path, the engine-computed ENV3 ($1C) read via the SID-engine accessor is strictly
    /// greater than its pre-render value. It does not assert engine parity. Skips when
    /// the native shim is unavailable.
    /// </summary>
    [Fact]
    public void Probe_NativeReSidInternalStateAdvancesOnStep()
    {
        if (!ViceNative.IsAvailable)
            Assert.Skip(ViceNative.AvailabilityMessage);

        var native = ViceNativeBridge.CreateMachine("c64");
        try
        {
            ViceNativeBridge.ResetMachine(native);

            // Gate voice 3 (regs $D40E-$D414): mid frequency, sawtooth + gate on,
            // attack rate 4 so the envelope ramps over many cycles (engine timing).
            ViceNative.WriteMemory(native, 0xD40E, 0x00); // freq lo
            ViceNative.WriteMemory(native, 0xD40F, 0x20); // freq hi
            ViceNative.WriteMemory(native, 0xD413, 0x40); // attack=4, decay=0
            ViceNative.WriteMemory(native, 0xD414, 0xF0); // sustain=15, release=0
            ViceNative.WriteMemory(native, 0xD412, 0x11); // sawtooth + gate on

            var env3Before = ViceNative.ReadSidEngine(native, 0x1C);

            // reSID advances when its samples are generated, not on StepNative.
            // Render ~88k cycles' worth (22 cycles/sample) to drive the attack;
            // the render path syncs the gated registers and clocks reSID, so its
            // engine-computed ENV3/OSC3 become readable via vice_sid_engine_read.
            var buffer = new short[256];
            for (var chunk = 0; chunk < 16; chunk++)
                ViceNative.RenderSidSamples(native, buffer, (nuint)buffer.Length, 22);

            var env3After = ViceNative.ReadSidEngine(native, 0x1C);
            var osc3After = ViceNative.ReadSidEngine(native, 0x1B);

            _output.WriteLine($"voice3 ENV3 ($1C): before={env3Before} after={env3After}");
            _output.WriteLine($"voice3 OSC3 ($1B) after=0x{osc3After:X2}");
            _output.WriteLine($"==> reSID advanced via render? envelopeRose={env3After > env3Before}");

            Assert.True(
                env3After > env3Before,
                $"reSID envelope did not advance through the render path (before={env3Before}, after={env3After}).");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(native);
        }
    }
}
