using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ViceSharp.Avalonia.Converters;

/// <summary>
/// FR-DRVLED-001: maps a boolean LED state to a brush - a bright red when lit,
/// a dim red when dark - so the peripheral card can render an activity LED.
/// </summary>
public sealed class LedBrushConverter : IValueConverter
{
    private static readonly IBrush LitBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0x3A, 0x3A));
    private static readonly IBrush DarkBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x20, 0x20));

    public static LedBrushConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? LitBrush : DarkBrush;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
