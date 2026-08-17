namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-VIC20-SOUND-001 / AC-SD-01 / TEST-VIC20-SD-01: native xvic audio export surface.
/// Use case: lockstep harness captures mono PCM from vice_vic20_render_samples.
/// Acceptance: when vice_xvic is present, render returns &gt;0 samples for a fixed cycle budget.
/// </summary>
[Collection("NativeVice")]
public sealed class Vic20NativeAudioExportTests
{
    [Fact]
    public void RenderVic20Samples_AfterReset_ReturnsSamples()
    {
        if (!ViceNativeXvic.IsAvailable)
            return;

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var buf = new short[4096];
        var n = native.RenderVic20Samples(buf, sampleRate: 44100, cyclesPerSec: 1_108_405, deltaTCycles: 50_000);
        Assert.True(n > 0, $"expected samples from xvic oracle, got {n}");
    }
}
