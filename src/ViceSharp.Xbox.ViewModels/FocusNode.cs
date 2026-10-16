namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// One node in the directional XY-focus graph for the 10-foot (couch) UI: a
/// focusable control identified by <see cref="Id"/> together with the ids of the
/// controls that receive focus when the user presses Up, Down, Left or Right.
/// </summary>
/// <remarks>
/// <para>
/// PLAN-XBOXUWP S22 (IMPL-XBOXUWP-022), FR-XBOXUI-002 / TR-XBOXUI-002. The UWP XAML
/// shell binds each control's <c>XYFocusUp/Down/Left/Right</c> to the corresponding
/// neighbor id computed here, so the directional-focus math is a pure value that can
/// be unit-tested off-console (TR-MVVM-001).
/// </para>
/// <para>
/// A <c>null</c> neighbor means there is no target in that direction (a grid edge):
/// focus stays put rather than wrapping to the opposite side. This is a
/// <see langword="readonly"/> <see langword="record"/> <see langword="struct"/>, so
/// two nodes with the same field values are equal.
/// </para>
/// </remarks>
/// <param name="Id">The stable id of this focusable control.</param>
/// <param name="Up">The id focused on Up, or <c>null</c> at the top edge.</param>
/// <param name="Down">The id focused on Down, or <c>null</c> at the bottom edge.</param>
/// <param name="Left">The id focused on Left, or <c>null</c> at the left edge.</param>
/// <param name="Right">The id focused on Right, or <c>null</c> at the right edge.</param>
public readonly record struct FocusNode(
    string Id,
    string? Up,
    string? Down,
    string? Left,
    string? Right);
