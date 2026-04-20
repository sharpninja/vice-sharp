namespace ViceSharp.Chips.VicIi;

/// <summary>
/// VIC-II color palette with VICE-compatible colors.
/// </summary>
public static class VicPalette
{
    /// <summary>
    /// VIC-II color entry (RGB)
    /// </summary>
    public readonly record struct Color(byte R, byte G, byte B);
    
    // VICE-compatible palette colors
    public static readonly Color[] Colors = new[]
    {
        new Color(0x00, 0x00, 0x00), // 0: Black
        new Color(0xFF, 0xFF, 0xFF), // 1: White
        new Color(0x96, 0x28, 0x35), // 2: Red
        new Color(0x5B, 0xD6, 0xC1), // 3: Cyan
        new Color(0x9B, 0x27, 0xB1), // 4: Purple
        new Color(0x5C, 0xB5, 0x32), // 5: Green
        new Color(0x1B, 0x1B, 0x8E), // 6: Blue (low red, low green, high blue)
        new Color(0xDF, 0xE5, 0x6C), // 7: Yellow
        new Color(0x9B, 0x52, 0x1C), // 8: Orange
        new Color(0x5A, 0x33, 0x00), // 9: Brown
        new Color(0xDA, 0x46, 0x44), // 10: Light Red
        new Color(0x44, 0x44, 0x44), // 11: Dark Grey
        new Color(0x77, 0x77, 0x77), // 12: Grey
        new Color(0xAD, 0xFF, 0x6C), // 13: Light Green
        new Color(0x6B, 0x5E, 0xD1), // 14: Light Blue
        new Color(0xAA, 0xAA, 0xAA), // 15: Light Grey
    };
    
    /// <summary>
    /// Get RGB color from palette index
    /// </summary>
    public static Color GetColor(int index) => Colors[index & 0x0F];
    
    /// <summary>
    /// Convert palette index to RGB tuple
    /// </summary>
    public static (byte R, byte G, byte B) ToRgb(int index) => 
        (Colors[index & 0x0F].R, Colors[index & 0x0F].G, Colors[index & 0x0F].B);
    
    /// <summary>
    /// Get 16-bit RGB565 color from palette index
    /// </summary>
    public static ushort ToRgb565(int index)
    {
        var c = Colors[index & 0x0F];
        return (ushort)(((c.R >> 3) << 11) | ((c.G >> 2) << 5) | (c.B >> 3));
    }
}
