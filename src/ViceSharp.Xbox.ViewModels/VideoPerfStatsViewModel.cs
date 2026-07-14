namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Globalization;
using System.Text;

/// <summary>
/// FEAT-XPERFHUD-001 (PLAN-XBOXUWP, area XVIDEO). The letterbox performance HUD's pure
/// rate math: presented FPS, emulated FPS, and emulation speed percent (measured cycle
/// rate vs the machine's nominal clock), plus the active standard label and pixel aspect.
/// </summary>
/// <remarks>
/// <para>
/// A pure sink over data the render loop already has: the surface records one sample per
/// present (<see cref="RecordPresent"/>) and one per NEWLY committed emulator frame
/// (<see cref="RecordFrame"/>, deduplicated by cycle stamp), then polls
/// <see cref="TryComputeText"/> each tick; the compute throttles itself to the minimum
/// window, formats the overlay text, and resets the window. All timestamps are supplied
/// by the caller (Stopwatch ticks + frequency), so the math is fully deterministic and
/// unit-testable headless (System only, TR-MVVM-001).
/// </para>
/// <para>
/// Speed percent is measured BETWEEN frames (cycles spanned / time spanned across the
/// window's first and last distinct frames), so it needs no baseline cycle and reads
/// 100% for a real-time machine regardless of when the HUD attached. With fewer than two
/// distinct frames in the window, or an unknown nominal clock, the speed line is omitted
/// rather than fabricated.
/// </para>
/// <para>
/// Single-threaded by design: every member is called on the render-timer (dispatcher)
/// thread. No locks, and the ~2 Hz compute is the only allocation source.
/// </para>
/// </remarks>
public sealed class VideoPerfStatsViewModel
{
    private const double MinWindowSeconds = 0.5;
    private const long NoTimestamp = long.MinValue;
    private const long NoCycle = long.MinValue;

    private double _clockHz;
    private string _standardLabel = "PAL";
    private float _pixelAspect = 1f;

    private long _windowStartTs = NoTimestamp;
    private int _presents;
    private int _frames;

    // Speed-percent trackers: first/last DISTINCT frame in the window ...
    private long _firstCycle = NoCycle;
    private long _firstCycleTs;
    private long _lastCycleInWindow = NoCycle;
    private long _lastCycleTs;

    // ... and the dedupe stamp, persistent across windows (a repeated committed frame
    // must never count as new emulated progress).
    private long _lastSeenCycle = NoCycle;

    /// <summary>
    /// Sets the active machine's nominal clock, standard label, and pixel aspect (applied
    /// at boot and re-applied after a model-change session rebuild).
    /// </summary>
    /// <param name="nominalClockHz">
    /// The machine's nominal clock in Hz; values not greater than zero mean unknown, which
    /// omits the speed line.
    /// </param>
    /// <param name="standardLabel">The video-standard label (e.g. "PAL", "NTSC").</param>
    /// <param name="pixelAspect">The standard's composite pixel aspect ratio.</param>
    public void SetMachine(double nominalClockHz, string standardLabel, float pixelAspect)
    {
        _clockHz = nominalClockHz;
        _standardLabel = string.IsNullOrEmpty(standardLabel) ? "PAL" : standardLabel;
        _pixelAspect = pixelAspect > 0f ? pixelAspect : 1f;
    }

    /// <summary>Records one presented frame at the given Stopwatch timestamp.</summary>
    /// <param name="timestamp">The Stopwatch tick the present completed at.</param>
    public void RecordPresent(long timestamp)
    {
        EnsureWindow(timestamp);
        _presents++;
    }

    /// <summary>
    /// Records one pulled emulator frame at the given Stopwatch timestamp. Frames whose
    /// cycle stamp equals the previously recorded one are ignored (the emulator committed
    /// nothing new since the last pull).
    /// </summary>
    /// <param name="cycle">The emulated cycle stamp of the pulled frame.</param>
    /// <param name="timestamp">The Stopwatch tick the frame was pulled at.</param>
    public void RecordFrame(long cycle, long timestamp)
    {
        EnsureWindow(timestamp);

        if (cycle == _lastSeenCycle)
            return;

        _lastSeenCycle = cycle;
        _frames++;

        if (_firstCycle == NoCycle)
        {
            _firstCycle = cycle;
            _firstCycleTs = timestamp;
        }

        _lastCycleInWindow = cycle;
        _lastCycleTs = timestamp;
    }

    /// <summary>
    /// Computes and formats the overlay text once at least the minimum window has elapsed,
    /// then resets the window. Returns <c>false</c> (empty text) while throttled, when no
    /// sample has been recorded yet, or for a non-positive frequency.
    /// </summary>
    /// <param name="now">The current Stopwatch tick.</param>
    /// <param name="frequency">Stopwatch ticks per second.</param>
    /// <param name="text">The formatted multi-line overlay text.</param>
    /// <returns><c>true</c> when a new overlay text was produced.</returns>
    public bool TryComputeText(long now, long frequency, out string text)
    {
        text = string.Empty;
        if (frequency <= 0 || _windowStartTs == NoTimestamp)
            return false;

        var elapsed = (now - _windowStartTs) / (double)frequency;
        if (elapsed < MinWindowSeconds)
            return false;

        var builder = new StringBuilder(64);
        builder.Append("FPS ").AppendLine((_presents / elapsed).ToString("0.0", CultureInfo.InvariantCulture));
        builder.Append("EMU ").AppendLine((_frames / elapsed).ToString("0.0", CultureInfo.InvariantCulture));

        // Speed measured between the window's first and last distinct frames; omitted
        // (never fabricated) without two frames or a known nominal clock.
        if (_clockHz > 0 && _firstCycle != NoCycle && _lastCycleInWindow != _firstCycle && _lastCycleTs > _firstCycleTs)
        {
            var frameSeconds = (_lastCycleTs - _firstCycleTs) / (double)frequency;
            var speedPercent = (_lastCycleInWindow - _firstCycle) / frameSeconds / _clockHz * 100.0;
            builder.Append("SPD ")
                .Append(speedPercent.ToString("0.0", CultureInfo.InvariantCulture))
                .AppendLine("%");
        }

        // Machine line: label + nominal clock (MHz, omitted when unknown) + the LABELED
        // composite Pixel Aspect Ratio. Operator feedback 2026-07-14: a bare "NTSC 0.75"
        // read as a (wrong) clock value; PAR must say what it is.
        builder.Append(_standardLabel);
        if (_clockHz > 0)
        {
            builder.Append(' ')
                .Append((_clockHz / 1_000_000d).ToString("0.00", CultureInfo.InvariantCulture))
                .Append("MHz");
        }

        builder.Append(" PAR ")
            .Append(_pixelAspect.ToString("0.00", CultureInfo.InvariantCulture));

        text = builder.ToString();

        // Reset the window (the dedupe stamp survives; repeated cycles stay ignored).
        _windowStartTs = now;
        _presents = 0;
        _frames = 0;
        _firstCycle = NoCycle;
        _lastCycleInWindow = NoCycle;

        return true;
    }

    private void EnsureWindow(long timestamp)
    {
        if (_windowStartTs == NoTimestamp)
            _windowStartTs = timestamp;
    }
}
