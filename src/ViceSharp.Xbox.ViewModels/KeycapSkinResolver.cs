namespace ViceSharp.Xbox.ViewModels;

using System;

/// <summary>
/// FEAT-XKEYCAPMODEL-001. Resolves the <see cref="KeycapSkin"/> for the active machine from
/// its profile id / display name. The head knows the emulated machine only as a profile
/// string (the board-model enum lives in the architectures layer, off-limits to the UI), so
/// this matches the model tokens a C64 profile carries and falls back to the breadbin - the
/// overwhelmingly common case, and the safe default when a variant cannot be identified.
/// Portable (no UI types), so the matching is unit-tested.
/// </summary>
public static class KeycapSkinResolver
{
    /// <summary>
    /// Picks the keycap skin for a machine profile. Matches (case-insensitively) the model
    /// tokens the C64 profiles carry in their id or display name; unknown / breadbin-class
    /// variants (Breadbox, BreadboxOld, Drean, PET64, Ultimax, Japanese) map to
    /// <see cref="KeycapSkin.Breadbin"/>.
    /// </summary>
    /// <param name="profileId">The active machine profile id (may be null/empty).</param>
    /// <param name="displayName">The active machine profile display name (may be null/empty).</param>
    /// <returns>The keycap skin to apply.</returns>
    public static KeycapSkin Resolve(string? profileId, string? displayName)
    {
        var s = ((profileId ?? string.Empty) + " " + (displayName ?? string.Empty)).ToLowerInvariant();

        // SX-64 portable ("sx64" / "sx-64" / "sx 64").
        if (s.Contains("sx64") || s.Contains("sx-64") || s.Contains("sx 64"))
            return KeycapSkin.Sx64;

        // C64GS game system (check before the generic "c64c" so "c64gs" is not misread).
        if (s.Contains("c64gs") || s.Contains("game system") || s.Contains("gamesystem"))
            return KeycapSkin.C64Gs;

        // C64C wedge case ("c64c" / "64c" / "c-64c").
        if (s.Contains("c64c") || s.Contains("64c") || s.Contains("c-64c"))
            return KeycapSkin.C64C;

        return KeycapSkin.Breadbin;
    }
}
