// FEAT-XKEYCAPMODEL-001: row horizontal-alignment converter for the virtual keyboard. #if HAS_UWP.
#if HAS_UWP
namespace ViceSharp.Xbox.Controls;

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

/// <summary>
/// FEAT-XKEYCAPMODEL-001 (operator 2026-07-18): maps a <c>VirtualKeyboardRow.Centered</c> flag
/// to a <see cref="HorizontalAlignment"/> - <see cref="HorizontalAlignment.Center"/> for the
/// space-bar row (centred under the block like the real machine) and
/// <see cref="HorizontalAlignment.Left"/> for the staggered rows above it.
/// </summary>
public sealed partial class RowAlignmentConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? HorizontalAlignment.Center : HorizontalAlignment.Left;

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
#endif
