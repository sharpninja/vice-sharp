namespace ViceSharp.TestHarness;

using System.Diagnostics;
using System.Runtime.Versioning;
using ViceSharp.Host.Audio;
using Xunit;

/// <summary>
/// Diagnostic probe for the deployed-app 50 percent speed report: measures the
/// sustained sample-accept rate of the real WinMM audio backend, which is the
/// only blocking wait inside the emulation advance path. The SID produces
/// 44100 samples per emulated second, so the backend's accept rate IS the
/// emulation speed ceiling whenever audio back-pressure engages.
/// </summary>
public sealed class WinMmAudioThroughputDiagTests
{
    /// <summary>
    /// FR: FR-Audio-SID, TR: TR-AUDIO-PACE-001, TEST: TEST-AUDIO-PACE-01.
    /// Use case: the audio ring must accept samples at the device drain rate
    /// (44100/s) so audio back-pressure paces emulation at 100 percent, not
    /// below. Acceptance: sustained accept rate over 3 seconds is at least
    /// 40000 samples/s (diagnostic threshold below the 44100 nominal).
    /// </summary>
    [Fact]
    [SupportedOSPlatform("windows")]
    public void WinMm_Backend_Accepts_Samples_At_Device_Rate()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "WinMM is Windows-only.");

        using var backend = new WinMmAudioBackend();
        var fragment = new float[256];
        for (int i = 0; i < fragment.Length; i++)
            fragment[i] = MathF.Sin(i * 0.1f) * 0.05f;

        // Warmup: fill the ring.
        for (int i = 0; i < 8; i++)
            backend.SubmitSamples(fragment);

        var sw = Stopwatch.StartNew();
        long samples = 0;
        while (sw.ElapsedMilliseconds < 3000)
        {
            backend.SubmitSamples(fragment);
            samples += fragment.Length;
        }

        sw.Stop();
        var rate = samples / sw.Elapsed.TotalSeconds;
        Assert.True(rate >= 40000, $"DIAG sustained accept rate = {rate:F0} samples/s ({rate / 44100.0 * 100:F1}% of 44100)");
    }
}
