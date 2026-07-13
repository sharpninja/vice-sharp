namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S25 (IMPL-XBOXUWP-025), area XBOXUI/XKBD. One tile on the on-screen
/// virtual C64 keyboard: the exact key-map string it injects, the glyph shown on it, its
/// double-width flag, and which machine seam it drives (<see cref="AppKeyKind"/>).
/// </summary>
/// <param name="KeyName">
/// The EXACT string the ViewModel passes to
/// <see cref="ViceSharp.Abstractions.IMachineKeyboardInput.SetKeyState(string, bool)"/>
/// for an ordinary (<see cref="AppKeyKind.Key"/>) or shift-latch
/// (<see cref="AppKeyKind.ShiftLatch"/>) tile. It MUST be a name the C64 keyboard map
/// resolves. The ViewModels project cannot reference the engine, so these strings are
/// hardcoded here and validated against the real map by the S25 tests. For the
/// <see cref="AppKeyKind.Restore"/> tile this value is not injected as a key (the tile
/// drives the RESTORE/NMI seam instead) and need not be a map key.
/// </param>
/// <param name="DisplayLabel">The glyph or caption rendered on the tile in the 10-foot UI.</param>
/// <param name="IsWide">
/// <c>true</c> for the physically double-width C64 keys (RETURN, RUN/STOP, SPACE) so the
/// layout can size them across two columns; <c>false</c> for ordinary single-width tiles.
/// </param>
/// <param name="Kind">
/// Which machine seam pressing the tile drives: an ordinary matrix key, the SHIFT-LOCK
/// latch, or the RESTORE/NMI trigger. Defaults to <see cref="AppKeyKind.Key"/>.
/// </param>
public sealed record VirtualKeyEntry(
    string KeyName,
    string DisplayLabel,
    bool IsWide,
    AppKeyKind Kind = AppKeyKind.Key);
