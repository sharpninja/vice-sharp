namespace ViceSharp.Xbox.Input;

/// <summary>
/// The persisted Xbox-head input preferences: the model of what the INI
/// <c>[ViceSharpXbox]</c> section will hold (PLAN-XBOXUWP S12, IMPL-XBOXUWP-012).
/// This is a pure model only - no INI is read or written here; that wiring is a
/// later slice (S29). <see cref="XboxInputConfigResolver"/> turns these prefs into
/// the ONE <see cref="XboxInputConfig"/> that the converter and mapper consume.
/// </summary>
/// <remarks>
/// The binding profile is NOT a second config store for the converter: the prefs
/// merely REFERENCE the binding file by path
/// (<see cref="BindingProfilePath"/> -&gt; <c>bindings.v1.json</c>), which an
/// <see cref="IBindingStore"/> loads into the <see cref="BindingProfile"/> that
/// drives the system-button evaluator. Deadzone/swap (for the converter/mapper) and
/// the binding profile (for the evaluator) are distinct concerns unified by this one
/// prefs record.
/// </remarks>
/// <param name="LeftStickDeadzone">
/// Radial deadzone for the primary (left) stick, 0..1. Because
/// <see cref="XboxInputConfig"/> carries a single quantization deadzone today, this
/// is the value the resolver canonicalizes into
/// <see cref="XboxInputConfig.StickDeadzone"/>.
/// </param>
/// <param name="RightStickDeadzone">
/// Radial deadzone for the secondary (right) stick, 0..1. Captured from the INI for
/// a future per-stick config; the current single-deadzone
/// <see cref="XboxInputConfig"/> canonicalizes on <see cref="LeftStickDeadzone"/>.
/// </param>
/// <param name="SwapPorts">Whether the two joystick ports are swapped.</param>
/// <param name="BindingProfilePath">
/// Path to the versioned binding profile file (<c>bindings.v1.json</c>) that an
/// <see cref="IBindingStore"/> loads; a reference, not an inline config.
/// </param>
public readonly record struct XboxInputPrefs(
    double LeftStickDeadzone,
    double RightStickDeadzone,
    bool SwapPorts,
    string BindingProfilePath);
