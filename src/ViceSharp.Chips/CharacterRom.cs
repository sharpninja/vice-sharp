namespace ViceSharp.Chips;

/// <summary>
/// C64 Character Generator ROM
/// </summary>
public static class CharacterRom
{
    /// <summary>
    /// 256 characters × 8 rows × 8 columns
    /// </summary>
    public static readonly byte[] Data = new byte[2048];

    static CharacterRom()
    {
        // Initialize default character set (upper/lowercase)
        // Font will be populated from ROM at runtime
    }

    /// <summary>
    /// Get 8x8 character glyph
    /// </summary>
    public static ReadOnlySpan<byte> GetGlyph(byte character)
    {
        return new ReadOnlySpan<byte>(Data, character * 8, 8);
    }
}