namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC (BACKFILL-VIDEO-001 color register access).
/// VIC-II color registers ($D020 border, $D021-$D024 background 0-3,
/// $D025-$D026 sprite multicolor 1-2, $D027-$D02E per-sprite color 0-7)
/// use only the low 4 bits for the C64 16-color palette. On real
/// hardware the upper 4 bits are unconnected and float high, so reads
/// always return $F in the upper nibble. Writes ignore the upper bits.
/// </summary>
public sealed class VicIIColorRegistersTests
{
    private const ushort BorderColor = 0xD020;
    private const ushort BackgroundColor0 = 0xD021;
    private const ushort BackgroundColor1 = 0xD022;
    private const ushort BackgroundColor2 = 0xD023;
    private const ushort BackgroundColor3 = 0xD024;
    private const ushort SpriteMulticolor0 = 0xD025;
    private const ushort SpriteMulticolor1 = 0xD026;
    private const ushort SpriteColor0 = 0xD027;
    private const ushort SpriteColor7 = 0xD02E;

    private static Mos6569 BuildVic()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 color register access).
    /// Use case: $D020 border color register encodes color in low 4
    /// bits only. Real hardware reports upper 4 bits as 1 (unconnected
    /// pins float high), so writing $05 (color 5, green) returns $F5
    /// on read.
    /// Acceptance: After Write($D020, $05), Read($D020) == $F5. After
    /// Write($D020, $FE), Read($D020) == $FE (upper nibble already $F,
    /// low nibble preserves $E).
    /// </summary>
    [Fact]
    public void BorderColor_ReadReturnsUpperNibbleSetToF()
    {
        var vic = BuildVic();

        vic.Write(BorderColor, 0x05);
        Assert.Equal(0xF5, vic.Read(BorderColor));

        vic.Write(BorderColor, 0xFE);
        Assert.Equal(0xFE, vic.Read(BorderColor));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 color register access).
    /// Use case: Background color registers $D021-$D024 follow the
    /// same low-4-bits semantic as $D020. Every value written reads
    /// back with upper nibble == $F.
    /// Acceptance: For each of $D021..$D024, writing $03 yields read
    /// $F3, and writing $00 yields read $F0.
    /// </summary>
    [Fact]
    public void BackgroundColors_ReadReturnsUpperNibbleSetToF()
    {
        var vic = BuildVic();

        for (ushort address = BackgroundColor0; address <= BackgroundColor3; address++)
        {
            vic.Write(address, 0x03);
            Assert.Equal(0xF3, vic.Read(address));

            vic.Write(address, 0x00);
            Assert.Equal(0xF0, vic.Read(address));
        }
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 color register access).
    /// Use case: Sprite multicolor registers $D025 and $D026 use the
    /// same low-4-bits encoding as the other color registers. Upper
    /// bits float high on read.
    /// Acceptance: Write $09 to each, read returns $F9. Write $0A,
    /// read returns $FA.
    /// </summary>
    [Fact]
    public void SpriteMulticolorRegisters_ReadReturnsUpperNibbleSetToF()
    {
        var vic = BuildVic();

        vic.Write(SpriteMulticolor0, 0x09);
        Assert.Equal(0xF9, vic.Read(SpriteMulticolor0));

        vic.Write(SpriteMulticolor1, 0x0A);
        Assert.Equal(0xFA, vic.Read(SpriteMulticolor1));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 color register access).
    /// Use case: Per-sprite color registers $D027-$D02E (sprites 0-7)
    /// use the same low-4-bits encoding. Each register reads back with
    /// upper nibble == $F.
    /// Acceptance: For each address in $D027..$D02E, writing the low
    /// nibble (i + 1) reads back as $F(i+1).
    /// </summary>
    [Fact]
    public void PerSpriteColorRegisters_ReadReturnsUpperNibbleSetToF()
    {
        var vic = BuildVic();

        for (int sprite = 0; sprite < 8; sprite++)
        {
            ushort address = (ushort)(SpriteColor0 + sprite);
            byte color = (byte)((sprite + 1) & 0x0F);

            vic.Write(address, color);
            Assert.Equal((byte)(0xF0 | color), vic.Read(address));
        }

        // Sanity: highest register $D02E exists and behaves like the others.
        vic.Write(SpriteColor7, 0x07);
        Assert.Equal(0xF7, vic.Read(SpriteColor7));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 color register access).
    /// Use case: Code that reads a color register, mutates it, and
    /// writes it back must observe a stable round-trip. Because writes
    /// ignore the upper 4 bits and reads always force them high, the
    /// effective color (low nibble) is preserved through read-modify-
    /// write cycles.
    /// Acceptance: Write $07 to $D020, read $F7. Write $F7 back. Read
    /// returns $F7. Low nibble (color index 7, yellow) survives.
    /// </summary>
    [Fact]
    public void BorderColor_ReadModifyWritePreservesLowNibble()
    {
        var vic = BuildVic();

        vic.Write(BorderColor, 0x07);
        byte firstRead = vic.Read(BorderColor);
        Assert.Equal(0xF7, firstRead);

        vic.Write(BorderColor, firstRead);
        Assert.Equal(0xF7, vic.Read(BorderColor));
        Assert.Equal(0x07, vic.Read(BorderColor) & 0x0F);
    }
}
