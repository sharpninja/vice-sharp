namespace ViceSharp.Xbox.Input;

/// <summary>
/// One row of a <see cref="BindingProfile"/>: a bindable system input mapped to the
/// application command it emits, with the activation style that governs the emission.
/// </summary>
/// <remarks>
/// Value type semantics (record): two bindings are equal when their
/// <see cref="Input"/>, <see cref="Command"/>, and <see cref="Activation"/> match,
/// which is what the S10 locked-table guard asserts row by row.
/// </remarks>
/// <param name="Input">The bindable gamepad input.</param>
/// <param name="Command">
/// The command emitted. For a <see cref="BindingActivation.Hold"/> binding this is
/// the ON (activate-edge) command; the evaluator derives the paired OFF command
/// (e.g. <see cref="AppCommand.WarpHoldOn"/> pairs with
/// <see cref="AppCommand.WarpHoldOff"/>).
/// </param>
/// <param name="Activation">How input activeness turns into an emission.</param>
public sealed record ButtonBinding(
    BindableInput Input,
    AppCommand Command,
    BindingActivation Activation);
