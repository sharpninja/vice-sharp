namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// FEAT-XKEYCAPMODEL-001 (operator 2026-07-18: "keycap size, shape and color should match
/// the exact model of computer being emulated"). The four visually-distinct C64 keycap
/// colour schemes the virtual keyboard skins itself with, chosen by the active machine
/// model. The C64's electronics and keyboard matrix are identical across cases, so only the
/// cap/legend COLOURS differ (the per-model keycap SHAPE differences are too subtle to show
/// at 10-foot scale, per the operator); every board variant maps onto one of these four.
/// </summary>
public enum KeycapSkin
{
    /// <summary>The original breadbin: beige main keys, dark legends, dark-brown function keys.</summary>
    Breadbin,

    /// <summary>The C64C wedge case: uniform warm grey caps with dark legends.</summary>
    C64C,

    /// <summary>The SX-64 portable: uniform matte dark-grey caps with off-white legends.</summary>
    Sx64,

    /// <summary>The C64GS game system: uniform dark-brown caps with beige legends.</summary>
    C64Gs,
}
