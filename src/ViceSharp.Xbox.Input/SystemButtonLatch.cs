namespace ViceSharp.Xbox.Input;

/// <summary>
/// The per-frame hysteresis latch threaded through
/// <see cref="XboxSystemButtons.Evaluate"/> (prior-in / next-out) so the evaluator
/// stays free of static state.
/// </summary>
/// <remarks>
/// <para>
/// Only the analog triggers need a latch. A digital button's edge is fully
/// determined by the prior and current button flags, so no digital state is carried
/// here. A trigger, however, can sit inside the hysteresis band (0.4..0.6) where its
/// raw value alone cannot say whether warp is currently on or off; the latch records
/// that phase so a value inside the band HOLDS the prior on/off state instead of
/// re-triggering.
/// </para>
/// </remarks>
/// <param name="LeftTriggerHeld">Whether the left trigger's hold is currently active.</param>
/// <param name="RightTriggerHeld">Whether the right trigger's hold is currently active.</param>
public readonly record struct SystemButtonLatch(
    bool LeftTriggerHeld,
    bool RightTriggerHeld)
{
    /// <summary>
    /// The initial latch: both triggers released. Equal to <c>default</c>.
    /// </summary>
    public static SystemButtonLatch Initial => default;
}
