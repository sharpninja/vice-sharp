// FEAT-XKEYCAPMODEL-001: per-model keycap colours for the virtual keyboard. #if HAS_UWP.
#if HAS_UWP
namespace ViceSharp.Xbox.Controls;

using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Media;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// FEAT-XKEYCAPMODEL-001 (operator 2026-07-18). The cap / legend / border brushes for each
/// <see cref="KeycapSkin"/>, matching the real machines: the breadbin's beige main keys with
/// dark legends and dark-brown function keys, the C64C's uniform warm grey, the SX-64's matte
/// dark grey with off-white legends, and the C64GS's uniform dark brown with beige legends.
/// Brushes are created once per skin and shared across every keycap (no per-apply allocation).
/// </summary>
internal static class KeycapSkinPalette
{
    /// <summary>The cap, legend, and border brushes for one keycap group.</summary>
    internal readonly record struct Brushes(SolidColorBrush Cap, SolidColorBrush Legend, SolidColorBrush Border);

    /// <summary>The main-key and function-key brush groups for a skin.</summary>
    internal readonly record struct SkinBrushes(Brushes Main, Brushes Function);

    private static readonly Dictionary<KeycapSkin, SkinBrushes> Cache = new();

    /// <summary>Returns the cached brushes for a skin, building them on first use.</summary>
    public static SkinBrushes For(KeycapSkin skin)
    {
        if (Cache.TryGetValue(skin, out var cached))
            return cached;

        var built = Build(skin);
        Cache[skin] = built;
        return built;
    }

    private static SkinBrushes Build(KeycapSkin skin) => skin switch
    {
        KeycapSkin.C64C => Uniform(0xC4B89C, 0x33302A, 0x9A8E72),
        KeycapSkin.Sx64 => Uniform(0x4A4A4A, 0xE8E4D8, 0x2E2E2E),
        KeycapSkin.C64Gs => Uniform(0x5A3B1E, 0xD8C7A0, 0x3A2612),
        // Breadbin: beige main keys with dark legends, dark-brown function keys with beige legends.
        _ => new SkinBrushes(
            new Brushes(Rgb(0xD8C7A0), Rgb(0x3A2E1E), Rgb(0xA08A5E)),
            new Brushes(Rgb(0x5A3B1E), Rgb(0xD8C7A0), Rgb(0x3A2612))),
    };

    private static SkinBrushes Uniform(uint cap, uint legend, uint border)
    {
        var group = new Brushes(Rgb(cap), Rgb(legend), Rgb(border));
        return new SkinBrushes(group, group);
    }

    private static SolidColorBrush Rgb(uint rgb) =>
        new(Color.FromArgb(0xFF, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
}
#endif
