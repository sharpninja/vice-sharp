namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S25 (IMPL-XBOXUWP-025) + PLAN-XKEYBOARD-001 K1, area XBOXUI/XKBD. One
/// tile on the on-screen virtual C64 keyboard: the exact key-map string it injects, the
/// glyph shown on it, its width, and which machine seam it drives
/// (<see cref="AppKeyKind"/>).
/// </summary>
/// <param name="KeyName">
/// The EXACT string the ViewModel passes to
/// <see cref="ViceSharp.Abstractions.IMachineKeyboardInput.SetKeyState(string, bool)"/>
/// for an ordinary (<see cref="AppKeyKind.Key"/>) tile, or the SHIFT key name
/// ("LeftShift"/"RightShift") a momentary tile wraps the next key in. It MUST be a name
/// the C64 keyboard map resolves. The ViewModels project cannot reference the engine, so
/// these strings are hardcoded here and validated against the real map by the tests. For
/// the <see cref="AppKeyKind.Restore"/> tile this value is not injected as a key (the
/// tile drives the RESTORE/NMI seam instead) and need not be a map key.
/// </param>
/// <param name="DisplayLabel">The glyph or caption rendered on the tile in the 10-foot UI.</param>
/// <param name="IsWide">
/// <c>true</c> for keys wider than one unit (RETURN, SPACE) so simple layouts can size
/// them; <see cref="EffectiveWidthUnits"/> carries the precise width.
/// </param>
/// <param name="Kind">
/// Which machine seam pressing the tile drives: an ordinary matrix key, the SHIFT-LOCK
/// latch, a momentary SHIFT, or the RESTORE/NMI trigger. Defaults to
/// <see cref="AppKeyKind.Key"/>.
/// </param>
/// <param name="WidthUnits">
/// The tile's width in key units for the authentic layout (PLAN-XKEYBOARD-001: RETURN 2,
/// SPACE ~9, ordinary keys 1). Values not greater than zero mean "derive from
/// <paramref name="IsWide"/>" (2 when wide, else 1); read <see cref="EffectiveWidthUnits"/>.
/// </param>
public sealed record VirtualKeyEntry(
    string KeyName,
    string DisplayLabel,
    bool IsWide,
    AppKeyKind Kind = AppKeyKind.Key,
    double WidthUnits = 0)
{
    /// <summary>
    /// The tile's width in key units: <see cref="WidthUnits"/> when positive, otherwise
    /// derived from <see cref="IsWide"/> (2 for wide tiles, 1 for ordinary ones).
    /// </summary>
    public double EffectiveWidthUnits => WidthUnits > 0 ? WidthUnits : (IsWide ? 2.0 : 1.0);
}
