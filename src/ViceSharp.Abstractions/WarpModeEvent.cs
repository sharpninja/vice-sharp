namespace ViceSharp.Abstractions;

/// <summary>
/// Published when the emulator's warp-mode or limiter state is established or changes.
/// </summary>
public readonly record struct WarpModeEvent(
    bool IsWarpMode,
    bool LimiterEnabled,
    double LimiterRatePercent,
    long Cycle)
{
    /// <summary>Pub/Sub topic used for warp-mode and limiter notifications.</summary>
    public static readonly Topic Topic = Topic.FromName("emulator.warp-mode");
}
