namespace ViceSharp.Xbox.ViewModels;

using System;

/// <summary>
/// FIX-XNTSCFPS-001 (PLAN-XBOXUWP, area XVIDEO). Pure render-cadence math: the render
/// timer's interval derives from the ACTIVE machine's refresh rate, so an NTSC session
/// presents at ~59.826 Hz and a PAL session at ~50.125 Hz (the fixed 20 ms interval
/// capped every machine at ~50 fps and read as "half speed" on the operator's NTSC HUD).
/// </summary>
/// <remarks>
/// Portable (System only, TR-MVVM-001) so the math is unit-testable headless; the
/// #if HAS_UWP surface consumes it when the head applies the session's refresh rate.
/// </remarks>
public static class VideoCadence
{
    /// <summary>The historical default interval, used when the refresh rate is unknown.</summary>
    public const double DefaultIntervalMs = 20.0;

    private const double MinIntervalMs = 4.0;
    private const double MaxIntervalMs = 40.0;

    /// <summary>
    /// The render-timer interval in milliseconds for a machine refresh rate: HALF the frame
    /// period (Nyquist against the coarse dispatcher timer), clamped to [4, 40] ms; rates not
    /// greater than zero return the 20 ms default. The DispatcherQueueTimer quantizes
    /// intervals UP to the ~15.6 ms system tick, so a full-period interval (16.7 ms NTSC)
    /// lands on 2 ticks = ~32 Hz (measured on-device 2026-07-14: FPS 32.4 at interval
    /// 16.7 ms); the half-period interval lands on ONE tick (~64 Hz checks), and the pull's
    /// new-frame dedupe keeps actual presents at the machine's own frame rate.
    /// </summary>
    /// <param name="refreshHz">The active machine's refresh rate in Hz.</param>
    /// <returns>The timer interval in milliseconds.</returns>
    public static double IntervalMsFor(double refreshHz)
    {
        if (refreshHz <= 0 || double.IsNaN(refreshHz) || double.IsInfinity(refreshHz))
            return DefaultIntervalMs;

        return Math.Clamp(500.0 / refreshHz, MinIntervalMs, MaxIntervalMs);
    }
}
