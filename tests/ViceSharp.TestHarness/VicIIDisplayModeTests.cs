namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: FR-VIC (BACKFILL-VIDEO-001 display mode selection).
/// VIC-II display mode is selected by three bits across two registers:
/// $D011 bit 5 (BMM, bitmap mode), $D011 bit 6 (ECM, extended color
/// mode), and $D016 bit 4 (MCM, multicolor mode). These three bits
/// encode five valid mode combinations plus a set of invalid (ECM
/// combined with BMM or MCM) combinations that, on the real chip,
/// produce a black screen / garbage display. The DisplayModeSelection
/// property reports the abstract mode the chip is currently in.
/// Pure register decoding here; pixel-rendering effect of each mode
/// (char vs bitmap fetch + palette routing) is a future slice.
/// </summary>
public sealed class VicIIDisplayModeTests
{
    private const ushort ControlRegister1 = 0xD011;
    private const ushort ControlRegister2 = 0xD016;

    private static Mos6569 BuildVic()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        return new Mos6569(new BasicBus(), irq);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 display mode selection).
    /// Use case: Default text mode with ECM=0, BMM=0, MCM=0. This is
    /// the power-on / boot mode for the C64 KERNAL.
    /// Acceptance: $D011 = $00, $D016 = $00 -&gt; DisplayModeSelection
    /// == StandardText.
    /// </summary>
    [Fact]
    public void DisplayModeSelection_StandardText_WhenAllSelectorBitsClear()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister1, 0x00);
        vic.Write(ControlRegister2, 0x00);

        Assert.Equal(Mos6569.VicIIDisplayMode.StandardText, vic.DisplayModeSelection);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 display mode selection).
    /// Use case: Multicolor text mode (ECM=0, BMM=0, MCM=1). Selected
    /// by setting $D016 bit 4 while leaving $D011 bits 5/6 clear.
    /// Acceptance: $D011 = $00, $D016 = $10 -&gt; DisplayModeSelection
    /// == MulticolorText.
    /// </summary>
    [Fact]
    public void DisplayModeSelection_MulticolorText_WhenOnlyMcmSet()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister1, 0x00);
        vic.Write(ControlRegister2, 0x10);

        Assert.Equal(Mos6569.VicIIDisplayMode.MulticolorText, vic.DisplayModeSelection);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 display mode selection).
    /// Use case: Standard (hi-res) bitmap mode (ECM=0, BMM=1, MCM=0).
    /// Selected by setting $D011 bit 5 while leaving $D011 bit 6 and
    /// $D016 bit 4 clear.
    /// Acceptance: $D011 = $20, $D016 = $00 -&gt; DisplayModeSelection
    /// == StandardBitmap.
    /// </summary>
    [Fact]
    public void DisplayModeSelection_StandardBitmap_WhenOnlyBmmSet()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister1, 0x20);
        vic.Write(ControlRegister2, 0x00);

        Assert.Equal(Mos6569.VicIIDisplayMode.StandardBitmap, vic.DisplayModeSelection);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 display mode selection).
    /// Use case: Multicolor bitmap mode (ECM=0, BMM=1, MCM=1). Both
    /// BMM ($D011 bit 5) and MCM ($D016 bit 4) are set.
    /// Acceptance: $D011 = $20, $D016 = $10 -&gt; DisplayModeSelection
    /// == MulticolorBitmap.
    /// </summary>
    [Fact]
    public void DisplayModeSelection_MulticolorBitmap_WhenBmmAndMcmSet()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister1, 0x20);
        vic.Write(ControlRegister2, 0x10);

        Assert.Equal(Mos6569.VicIIDisplayMode.MulticolorBitmap, vic.DisplayModeSelection);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 display mode selection).
    /// Use case: Extended color mode (ECM=1, BMM=0, MCM=0). $D011 bit
    /// 6 selects ECM; the other selector bits must be clear for a
    /// valid mode.
    /// Acceptance: $D011 = $40, $D016 = $00 -&gt; DisplayModeSelection
    /// == ExtendedColor.
    /// </summary>
    [Fact]
    public void DisplayModeSelection_ExtendedColor_WhenOnlyEcmSet()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister1, 0x40);
        vic.Write(ControlRegister2, 0x00);

        Assert.Equal(Mos6569.VicIIDisplayMode.ExtendedColor, vic.DisplayModeSelection);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 display mode selection).
    /// Use case: ECM combined with BMM is an invalid mode on real
    /// hardware (chip outputs a black screen). The emulator reports
    /// this state as Invalid so higher layers can route to a
    /// well-defined fallback rather than silently picking one of the
    /// valid modes.
    /// Acceptance: $D011 = $60 (ECM + BMM), $D016 = $00 -&gt;
    /// DisplayModeSelection == Invalid.
    /// </summary>
    [Fact]
    public void DisplayModeSelection_Invalid_WhenEcmAndBmmSet()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister1, 0x60);
        vic.Write(ControlRegister2, 0x00);

        Assert.Equal(Mos6569.VicIIDisplayMode.Invalid, vic.DisplayModeSelection);
    }

    /// <summary>
    /// FR/TR: FR-VIC (BACKFILL-VIDEO-001 display mode selection).
    /// Use case: ECM combined with MCM is also an invalid mode on
    /// real hardware. Same Invalid reporting contract as ECM + BMM.
    /// Acceptance: $D011 = $40 (ECM), $D016 = $10 (MCM) -&gt;
    /// DisplayModeSelection == Invalid.
    /// </summary>
    [Fact]
    public void DisplayModeSelection_Invalid_WhenEcmAndMcmSet()
    {
        var vic = BuildVic();

        vic.Write(ControlRegister1, 0x40);
        vic.Write(ControlRegister2, 0x10);

        Assert.Equal(Mos6569.VicIIDisplayMode.Invalid, vic.DisplayModeSelection);
    }
}
