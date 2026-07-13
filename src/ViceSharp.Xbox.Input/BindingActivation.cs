namespace ViceSharp.Xbox.Input;

/// <summary>
/// How a <see cref="ButtonBinding"/> turns raw input activeness into an
/// <see cref="AppCommand"/> emission at the evaluator (S10).
/// </summary>
/// <remarks>
/// All three activations are EDGE-triggered, never level-triggered: a held button
/// emits nothing after its edge. <see cref="Press"/> and <see cref="Toggle"/> emit
/// once on the down edge (they differ only in downstream meaning: Press is a
/// momentary action, Toggle flips a downstream state); <see cref="Hold"/> emits a
/// paired on/off around a hysteresis band so warp (and similar) tracks the hold.
/// </remarks>
public enum BindingActivation
{
    /// <summary>Emit the command once on the down edge; a held button does not repeat.</summary>
    Press,

    /// <summary>
    /// Emit a paired on/off: the binding's command on the activate edge and its
    /// paired release command on the release edge, with hysteresis in between.
    /// </summary>
    Hold,

    /// <summary>
    /// Emit the command once on each down edge; a downstream flips a state, so at the
    /// evaluator it is exactly one emit per down edge.
    /// </summary>
    Toggle,
}
