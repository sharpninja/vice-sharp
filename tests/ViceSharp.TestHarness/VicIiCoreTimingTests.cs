namespace ViceSharp.TestHarness;

using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using ViceSharp.Abstractions;
using Xunit;

public sealed class VicIiCoreTimingTests
{
    [Fact]
    public void RasterIrq_AssertsAtCompareCycleAndClearsByWriteOneToD019()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(new BasicBus(), irq);

        vic.Write(0xD012, 0x00);
        vic.Write(0xD01A, 0x01);

        Advance(vic, 57);
        Assert.False(irq.IsAsserted);
        Assert.Equal(0x00, vic.Read(0xD019));

        vic.Tick();

        Assert.True(irq.IsAsserted);
        Assert.Equal(0x81, vic.Read(0xD019));
        Assert.Equal(0x81, vic.Read(0xD019));

        vic.Write(0xD019, 0x01);

        Assert.False(irq.IsAsserted);
        Assert.Equal(0x00, vic.Read(0xD019));
    }

    [Fact]
    public void RasterIrq_UsesWrittenCompareLineInsteadOfCurrentRasterRegister()
    {
        var irq = new InterruptLine(InterruptType.Irq);
        var vic = new Mos6569(new BasicBus(), irq);

        vic.Write(0xD012, 0x01);
        vic.Write(0xD01A, 0x01);

        Advance(vic, 58);
        Assert.False(irq.IsAsserted);

        Advance(vic, Mos6569.PalCyclesPerLine - 58);
        Assert.Equal(0x01, vic.CurrentRasterLine);

        Advance(vic, 58);
        Assert.True(irq.IsAsserted);
        Assert.Equal(0x81, vic.Read(0xD019));
    }

    [Fact]
    public void BadLine_RequiresDenVisibleRangeAndYScrollMatch()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));

        Advance(vic, 0x30 * Mos6569.PalCyclesPerLine);
        Assert.Equal(0x30, vic.CurrentRasterLine);
        Assert.False(vic.IsBadLine);

        vic.Write(0xD011, 0x10);
        Assert.True(vic.IsBadLine);

        vic.Write(0xD011, 0x11);
        Assert.False(vic.IsBadLine);

        Advance(vic, Mos6569.PalCyclesPerLine);

        Assert.Equal(0x31, vic.CurrentRasterLine);
        Assert.True(vic.IsBadLine);
    }

    [Fact]
    public void BadLine_DmaStealingWindowTracksCharacterFetchCycles()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));

        vic.Write(0xD011, 0x10);
        Advance(vic, 0x30 * Mos6569.PalCyclesPerLine);

        Assert.True(vic.IsBadLine);
        Assert.False(vic.IsDmaStealing);

        Advance(vic, 14);
        Assert.Equal(14, vic.RasterX);
        Assert.True(vic.IsDmaStealing);
        Assert.True(vic.IsVideoMatrixAccess);

        Advance(vic, 40);
        Assert.Equal(54, vic.RasterX);
        Assert.False(vic.IsDmaStealing);
        Assert.True(vic.IsCharacterAccess);
    }

    private static void Advance(Mos6569 vic, int cycles)
    {
        for (var cycle = 0; cycle < cycles; cycle++)
            vic.Tick();
    }
}
