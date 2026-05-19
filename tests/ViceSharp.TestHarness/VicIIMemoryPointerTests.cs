namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D018 / $D016 register decoding).
/// VIC-II $D018 (memory pointers) encodes the video matrix base in bits
/// 7-4 (VM13..VM10, shifted &lt;&lt; 10) and the character bitmap base in
/// bits 3-1 (CB13..CB11, shifted &lt;&lt; 11). Bit 0 is unused on the
/// real chip and floats high (always reads as 1). $D016 (control
/// register 2) carries XSCROLL in bits 2-0, CSEL in bit 3 (38 vs 40
/// columns), MCM in bit 4 (multicolor text mode), and bits 7-6 are
/// unused on the real chip and float high.
/// </summary>
public sealed class VicIIMemoryPointerTests
{
    private const ushort MemoryPointers = 0xD018;
    private const ushort ControlRegister2 = 0xD016;

    private static Mos6569 BuildVic()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D018 decoding).
    /// Use case: Software writes $D018 to point the video matrix at a
    /// new $0400-aligned page. Bits 7-4 (VM13..VM10) shifted &lt;&lt; 10
    /// give the absolute base inside the current VIC bank.
    /// Acceptance: Write $14 (default) -&gt; ScreenMemoryBase == $0400.
    /// Write $44 (VM bits = $4) -&gt; ScreenMemoryBase == $1000.
    /// </summary>
    [Fact]
    public void MemoryPointers_VideoMatrixBaseDerivedFromBits7To4()
    {
        var vic = BuildVic();

        vic.Write(MemoryPointers, 0x14);
        Assert.Equal(0x0400, vic.ScreenMemoryBase);

        vic.Write(MemoryPointers, 0x44);
        Assert.Equal(0x1000, vic.ScreenMemoryBase);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D018 decoding).
    /// Use case: Software writes $D018 to point character/bitmap data
    /// at a new $0800-aligned page. Bits 3-1 (CB13..CB11) shifted
    /// &lt;&lt; 11 give the absolute base inside the current VIC bank.
    /// Acceptance: Write $04 (CB bits = $2 once bit 0 is dropped)
    /// -&gt; CharacterBase == $1000. Write $06 (CB bits = $3) -&gt;
    /// CharacterBase == $1800.
    /// </summary>
    [Fact]
    public void MemoryPointers_CharacterBitmapBaseDerivedFromBits3To1()
    {
        var vic = BuildVic();

        vic.Write(MemoryPointers, 0x04);
        Assert.Equal(0x1000, vic.CharacterBase);

        vic.Write(MemoryPointers, 0x06);
        Assert.Equal(0x1800, vic.CharacterBase);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D018 decoding).
    /// Use case: Bit 0 of $D018 is not wired to anything on the real
    /// chip and floats high. Reads always report bit 0 set, regardless
    /// of what was written.
    /// Acceptance: After Write($D018, $14), Read returns $15 (bit 0
    /// forced to 1). After Write($D018, $00), Read returns $01. After
    /// Write($D018, $FF), Read still returns $FF.
    /// </summary>
    [Fact]
    public void MemoryPointers_ReadAlwaysSetsBit0()
    {
        var vic = BuildVic();

        vic.Write(MemoryPointers, 0x14);
        Assert.Equal(0x15, vic.Read(MemoryPointers));

        vic.Write(MemoryPointers, 0x00);
        Assert.Equal(0x01, vic.Read(MemoryPointers));

        vic.Write(MemoryPointers, 0xFF);
        Assert.Equal(0xFF, vic.Read(MemoryPointers));
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D016 decoding).
    /// Use case: Bits 2-0 of $D016 are the X scroll value (0..7
    /// pixels) consumed by the pixel sequencer. Routing only - the
    /// pixel-sequencer effect is a future slice.
    /// Acceptance: Write $D016 = $07 -&gt; XScroll == 7. Write $D016 =
    /// $03 -&gt; XScroll == 3. Write $D016 = $18 (bit 3 set, low bits
    /// clear) -&gt; XScroll == 0.
    /// </summary>
    [Fact]
    public void ControlRegister2_XScrollDerivedFromLowThreeBits()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister2, 0x07);
        Assert.Equal(7, vic.XScroll);

        vic.Write(ControlRegister2, 0x03);
        Assert.Equal(3, vic.XScroll);

        vic.Write(ControlRegister2, 0x18);
        Assert.Equal(0, vic.XScroll);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D016 decoding).
    /// Use case: Bit 3 of $D016 is CSEL (column select). When set, the
    /// display is 40 columns wide; when clear, the display is 38
    /// columns and the left/right border encroaches one column.
    /// Acceptance: Write $D016 with bit 3 set -&gt; Columns ==
    /// Wide40. Write $D016 with bit 3 clear -&gt; Columns == Normal38.
    /// </summary>
    [Fact]
    public void ControlRegister2_CselSelectsBetween38And40Columns()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister2, 0x08);
        Assert.Equal(Mos6569.ColumnMode.Wide40, vic.Columns);

        vic.Write(ControlRegister2, 0x00);
        Assert.Equal(Mos6569.ColumnMode.Normal38, vic.Columns);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D016 decoding).
    /// Use case: Bit 4 of $D016 is MCM (multicolor mode). Combined
    /// with $D011 bits, it picks between standard text, multicolor
    /// text, bitmap, and extended-background modes.
    /// Acceptance: Write $D016 with bit 4 set -&gt; DisplayMode ==
    /// MulticolorText (assuming $D011 bits 5/6 clear, default).
    /// Write $D016 with bit 4 clear -&gt; DisplayMode == StandardText.
    /// </summary>
    [Fact]
    public void ControlRegister2_McmBitSelectsMulticolorText()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister2, 0x10);
        Assert.Equal(Mos6569.VideoMode.MulticolorText, vic.DisplayMode);

        vic.Write(ControlRegister2, 0x00);
        Assert.Equal(Mos6569.VideoMode.StandardText, vic.DisplayMode);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 $D016 decoding).
    /// Use case: Bits 7-6 of $D016 are unconnected on the real chip
    /// and float high. Reads always report them as 1 regardless of
    /// what was written.
    /// Acceptance: After Write($D016, $00), Read returns $C0. After
    /// Write($D016, $1F), Read returns $DF (low 5 bits preserved,
    /// upper 2 forced high). After Write($D016, $FF), Read returns
    /// $FF.
    /// </summary>
    [Fact]
    public void ControlRegister2_ReadAlwaysSetsBits7And6()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister2, 0x00);
        Assert.Equal(0xC0, vic.Read(ControlRegister2));

        vic.Write(ControlRegister2, 0x1F);
        Assert.Equal(0xDF, vic.Read(ControlRegister2));

        vic.Write(ControlRegister2, 0xFF);
        Assert.Equal(0xFF, vic.Read(ControlRegister2));
    }
}
