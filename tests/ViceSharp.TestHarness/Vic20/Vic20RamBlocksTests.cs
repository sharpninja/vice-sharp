namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core.Vic20;
using Xunit;

/// <summary>
/// FR-VIC20-002 / xvic -memory parity.
/// Use case: VICE cmdline_memory accepts none/3k/8k/16k/24k/all and block
/// tokens 0,1,2,3,5 or 04,20,40,60,a0 (comma-separated). Managed must parse,
/// format, and install the same RAM windows.
/// Acceptance: round-trip + IsInstalledRam match VICE block ranges.
/// </summary>
public sealed class Vic20RamBlocksTests
{
    [Theory]
    [InlineData("none", Vic20RamBlocks.None)]
    [InlineData("", Vic20RamBlocks.None)]
    [InlineData("3k", Vic20RamBlocks.Blk0)]
    [InlineData("8k", Vic20RamBlocks.Blk1)]
    [InlineData("16k", Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2)]
    [InlineData("24k", Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2 | Vic20RamBlocks.Blk3)]
    [InlineData("all", Vic20RamBlocks.All)]
    [InlineData("0", Vic20RamBlocks.Blk0)]
    [InlineData("1", Vic20RamBlocks.Blk1)]
    [InlineData("2", Vic20RamBlocks.Blk2)]
    [InlineData("3", Vic20RamBlocks.Blk3)]
    [InlineData("5", Vic20RamBlocks.Blk5)]
    [InlineData("04", Vic20RamBlocks.Blk0)]
    [InlineData("20", Vic20RamBlocks.Blk1)]
    [InlineData("40", Vic20RamBlocks.Blk2)]
    [InlineData("60", Vic20RamBlocks.Blk3)]
    [InlineData("a0", Vic20RamBlocks.Blk5)]
    [InlineData("A0", Vic20RamBlocks.Blk5)]
    [InlineData("60,a0", Vic20RamBlocks.Blk3 | Vic20RamBlocks.Blk5)]
    [InlineData("0,1,2,3,5", Vic20RamBlocks.All)]
    [InlineData("3k,8k", Vic20RamBlocks.Blk0 | Vic20RamBlocks.Blk1)]
    public void ParseMemorySpec_MatchesXvic(string spec, Vic20RamBlocks expected)
    {
        Assert.True(Vic20MemoryLayout.TryParseMemorySpec(spec, out var blocks));
        Assert.Equal(expected, blocks);
    }

    [Theory]
    [InlineData("bogus")]
    [InlineData("7")]
    [InlineData("30")]
    [InlineData("all,bogus")]
    public void ParseMemorySpec_RejectsUnknownToken(string spec)
    {
        Assert.False(Vic20MemoryLayout.TryParseMemorySpec(spec, out _));
    }

    [Theory]
    [InlineData(Vic20RamBlocks.None, "none")]
    [InlineData(Vic20RamBlocks.Blk0, "3k")]
    [InlineData(Vic20RamBlocks.Blk1, "8k")]
    [InlineData(Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2, "16k")]
    [InlineData(Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2 | Vic20RamBlocks.Blk3, "24k")]
    [InlineData(Vic20RamBlocks.All, "all")]
    [InlineData(Vic20RamBlocks.Blk3 | Vic20RamBlocks.Blk5, "60,a0")]
    [InlineData(Vic20RamBlocks.Blk0 | Vic20RamBlocks.Blk5, "04,a0")]
    public void FormatMemorySpec_CanonicalXvicForm(Vic20RamBlocks blocks, string expected)
    {
        Assert.Equal(expected, Vic20MemoryLayout.FormatMemorySpec(blocks));
    }

    [Theory]
    [InlineData(Vic20ExpansionKind.Unexpanded, Vic20RamBlocks.None)]
    [InlineData(Vic20ExpansionKind.Exp3K, Vic20RamBlocks.Blk0)]
    [InlineData(Vic20ExpansionKind.Exp8K, Vic20RamBlocks.Blk1)]
    [InlineData(Vic20ExpansionKind.Exp16K, Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2)]
    [InlineData(Vic20ExpansionKind.Exp24K, Vic20RamBlocks.Blk1 | Vic20RamBlocks.Blk2 | Vic20RamBlocks.Blk3)]
    [InlineData(Vic20ExpansionKind.Exp32K, Vic20RamBlocks.All)]
    public void ExpansionKind_ToBlocks_Presets(Vic20ExpansionKind kind, Vic20RamBlocks expected)
    {
        Assert.Equal(expected, Vic20MemoryLayout.ToBlocks(kind));
    }

    [Theory]
    [InlineData(Vic20RamBlocks.None, 0x0000, true)]
    [InlineData(Vic20RamBlocks.None, 0x1000, true)]
    [InlineData(Vic20RamBlocks.None, 0x0400, false)]
    [InlineData(Vic20RamBlocks.None, 0x2000, false)]
    [InlineData(Vic20RamBlocks.Blk0, 0x0400, true)]
    [InlineData(Vic20RamBlocks.Blk0, 0x0FFF, true)]
    [InlineData(Vic20RamBlocks.Blk0, 0x2000, false)]
    [InlineData(Vic20RamBlocks.Blk1, 0x2000, true)]
    [InlineData(Vic20RamBlocks.Blk1, 0x3FFF, true)]
    [InlineData(Vic20RamBlocks.Blk1, 0x4000, false)]
    [InlineData(Vic20RamBlocks.Blk2, 0x4000, true)]
    [InlineData(Vic20RamBlocks.Blk3, 0x6000, true)]
    [InlineData(Vic20RamBlocks.Blk5, 0xA000, true)]
    [InlineData(Vic20RamBlocks.Blk5, 0xBFFF, true)]
    [InlineData(Vic20RamBlocks.Blk3 | Vic20RamBlocks.Blk5, 0x6000, true)]
    [InlineData(Vic20RamBlocks.Blk3 | Vic20RamBlocks.Blk5, 0xA000, true)]
    [InlineData(Vic20RamBlocks.Blk3 | Vic20RamBlocks.Blk5, 0x2000, false)]
    [InlineData(Vic20RamBlocks.All, 0x0400, true)]
    [InlineData(Vic20RamBlocks.All, 0xA000, true)]
    public void IsInstalledRam_Blocks(Vic20RamBlocks blocks, int address, bool installed)
    {
        Assert.Equal(installed, Vic20MemoryLayout.IsInstalledRam(blocks, (ushort)address));
    }

    [Fact]
    public void SystemRam_CustomBlocks_Blk3AndBlk5Writable()
    {
        var blocks = Vic20RamBlocks.Blk3 | Vic20RamBlocks.Blk5;
        var ram = new Vic20SystemRam(blocks);
        Assert.Equal(blocks, ram.RamBlocks);
        Assert.True(ram.HandlesAddress(0x6000));
        Assert.True(ram.HandlesAddress(0xA000));
        Assert.False(ram.HandlesAddress(0x2000));
        Assert.False(ram.HandlesAddress(0x0400));

        ram.Write(0x6000, 0x11);
        ram.Write(0xA000, 0x22);
        Assert.Equal(0x11, ram.Read(0x6000));
        Assert.Equal(0x22, ram.Read(0xA000));
    }

    [Fact]
    public void ParseBoardModel_StillSupportsLegacyExpLabels()
    {
        Assert.Equal(Vic20ExpansionKind.Exp8K, Vic20MemoryLayout.ParseBoardModel("Exp8K"));
        Assert.Equal(Vic20ExpansionKind.Exp32K, Vic20MemoryLayout.ParseBoardModel("32K"));
        Assert.Equal(Vic20ExpansionKind.Unexpanded, Vic20MemoryLayout.ParseBoardModel("Unexpanded"));
    }
}
