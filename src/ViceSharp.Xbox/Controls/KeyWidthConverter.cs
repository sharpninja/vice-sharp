// PLAN-XKEYBOARD-001 K2: width-units -> pixels for the real-C64 keyboard dock. #if HAS_UWP.
#if HAS_UWP
namespace ViceSharp.Xbox.Controls;

using System;
using Windows.UI.Xaml.Data;

/// <summary>
/// Converts a <see cref="ViceSharp.Xbox.ViewModels.VirtualKeyEntry.EffectiveWidthUnits"/>
/// value into a pixel width for a keyboard tile (one key unit =
/// <see cref="UnitPixels"/>), so the XAML dock renders the authentic C64 key widths
/// (RETURN 2 units, SPACE 9, SHIFT/CTRL 1.5) straight from the portable layout model.
/// </summary>
public sealed partial class KeyWidthConverter : IValueConverter
{
    /// <summary>The pixel width of one key unit at 10-foot scale.</summary>
    public double UnitPixels { get; set; } = 52;

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is double units && units > 0 ? units * UnitPixels : UnitPixels;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException("KeyWidthConverter is one-way.");
}
#endif
