using Gb64Import;
using Xunit;

/// <summary>
/// FR: FR-GB64-003, TR: TR-GB64-TAG-001, TEST-GB64-003.
/// Use case: VERSION.NFO fields become RomM filename tags.
/// </summary>
public class NfoAndTagTests
{
    private const string SampleNfo = """
        ==============================================================================
        GB-Version:        1
        Filename:          0\000WHITE_26153_01.zip
        Screenshot:        0\000_White_Dash.png
        SID:               GAMES\A-F\Boulder_Dash.sid
        ==============================================================================

        ==============================================================================
        GAME INFO:
        ------------------------------------------------------------------------------
        Unique-ID:         26153
        Name:              000 White Dash
        Language:          English
        Genre:             Arcade - Boulder Dash
        Players:           1 - 2
        Control:           Joystick Port 1
        ==============================================================================

        ==============================================================================
        VERSION INFO:
        ------------------------------------------------------------------------------
        Cracked/Crunched:  (None)
        Trainers:          0
        True Drive Emul.:  Yes
        Pal/NTSC:          PAL
        ==============================================================================
        """;

    [Fact]
    public void Parse_Nfo_CoreFields()
    {
        var nfo = Gb64Nfo.Parse(SampleNfo);
        Assert.Equal("1", nfo.GbVersion);
        Assert.Equal("26153", nfo.UniqueId);
        Assert.Equal("000 White Dash", nfo.Name);
        Assert.Equal("English", nfo.Language);
        Assert.Equal("PAL", nfo.PalNtsc);
        Assert.Equal("Yes", nfo.TrueDriveEmul);
        Assert.Equal(@"0\000_White_Dash.png", nfo.Screenshot);
        Assert.Equal(@"GAMES\A-F\Boulder_Dash.sid", nfo.Sid);
    }

    [Fact]
    public void BuildStem_MinimumProfile()
    {
        var nfo = Gb64Nfo.Parse(SampleNfo);
        var stem = RomTagBuilder.BuildBaseStem(nfo);
        Assert.Contains("000 White Dash", stem);
        Assert.Contains("(En)", stem);
        Assert.Contains("(E)", stem);
        Assert.Contains("(PAL)", stem);
        Assert.Contains("(rev-01)", stem);
        Assert.Contains("(gb64-26153)", stem);
        Assert.Contains("(TrueDrive)", stem);
    }

    [Fact]
    public void Sanitize_RemovesInvalidChars()
    {
        Assert.Equal("A-B", RomTagBuilder.SanitizeName("A/B"));
        Assert.Equal("Hello World", RomTagBuilder.SanitizeName("Hello   World"));
    }

    [Fact]
    public void MultiLanguage_EmitsMultipleTags()
    {
        var nfo = new Gb64Nfo
        {
            Name = "Test",
            UniqueId = "1",
            GbVersion = "2",
            Language = "English / German",
            PalNtsc = "NTSC",
        };
        var stem = RomTagBuilder.BuildBaseStem(nfo);
        Assert.Contains("(En)", stem);
        Assert.Contains("(De)", stem);
        Assert.Contains("(U)", stem);
        Assert.Contains("(NTSC)", stem);
        Assert.Contains("(rev-02)", stem);
        Assert.Contains("(gb64-1)", stem);
    }

    [Fact]
    public void ParseZipStem_ExtractsIdAndRev()
    {
        var (id, rev, shortName) = RomTagBuilder.ParseZipStem("000WHITE_26153_01.zip");
        Assert.Equal("26153", id);
        Assert.Equal("01", rev);
        Assert.Equal("000WHITE", shortName);
    }

    [Fact]
    public void IsMediaFile_RecognizesC64Extensions()
    {
        Assert.True(RomTagBuilder.IsMediaFile("game.d64"));
        Assert.True(RomTagBuilder.IsMediaFile("tape.T64"));
        Assert.False(RomTagBuilder.IsMediaFile("VERSION.NFO"));
        Assert.False(RomTagBuilder.IsMediaFile("readme.txt"));
    }

    [Fact]
    public void DocExample_4AcesPinball_PalNtscMaybe()
    {
        // docs/gb64-romm-tag-mapping.md example:
        // 4 Aces Pinball (En) (E) (PAL) (NTSC-maybe) (rev-01) (gb64-12134).t64
        var nfo = new Gb64Nfo
        {
            Name = "4 Aces Pinball",
            Language = "English",
            PalNtsc = "PAL(+NTSC?)",
            UniqueId = "12134",
            GbVersion = "1",
            Trainers = "0",
            CrackedCrunched = "(None)",
        };
        var stem = RomTagBuilder.BuildBaseStem(nfo);
        Assert.Equal(
            "4 Aces Pinball (En) (E) (PAL) (NTSC-maybe) (rev-01) (gb64-12134)",
            stem);
    }

    [Fact]
    public void DocExample_BrushUp_TrueDrive()
    {
        // Brush Up Your English II (De) (E) (PAL) (NTSC-maybe) (rev-02) (gb64-14766) (TrueDrive).d64
        // Doc example omits Pal when not stated; if Pal absent we still emit rev+id+TrueDrive.
        // When Pal/NTSC is PAL(+NTSC?) (common), full stem includes video tags.
        var nfo = new Gb64Nfo
        {
            Name = "Brush Up Your English II",
            Language = "German",
            PalNtsc = "PAL(+NTSC?)",
            UniqueId = "14766",
            GbVersion = "2",
            TrueDriveEmul = "Yes",
        };
        var stem = RomTagBuilder.BuildBaseStem(nfo);
        Assert.Equal(
            "Brush Up Your English II (De) (E) (PAL) (NTSC-maybe) (rev-02) (gb64-14766) (TrueDrive)",
            stem);
    }

    [Fact]
    public void Cracked_EmitsGroupCode()
    {
        var nfo = new Gb64Nfo
        {
            Name = "Some Game",
            Language = "English",
            PalNtsc = "PAL",
            UniqueId = "99999",
            GbVersion = "1",
            Trainers = "3",
            CrackedCrunched = "Fairlight (FLT)",
        };
        var stem = RomTagBuilder.BuildBaseStem(nfo);
        Assert.Contains("(Trainers-3)", stem);
        Assert.Contains("(Cracked)", stem);
        Assert.Contains("(FLT)", stem);
        Assert.Contains("(gb64-99999)", stem);
    }

    [Fact]
    public void RewriteSidInText_ReplacesSidLine()
    {
        var nfo = """
            GB-Version:        1
            SID:               GAMES\M-R\Plasto.sid
            Unique-ID:         1
            """;
        var rewritten = SidNfoFixer.RewriteSidInText(nfo, @"MUSICIANS\C\Cecile\Plasto.sid");
        Assert.Contains(@"MUSICIANS\C\Cecile\Plasto.sid", rewritten);
        Assert.DoesNotContain(@"GAMES\M-R\Plasto.sid", rewritten);
        var parsed = Gb64Nfo.Parse(rewritten);
        Assert.Equal(@"MUSICIANS\C\Cecile\Plasto.sid", parsed.Sid);
    }

    [Fact]
    public void RewriteSidInText_ClearToNone()
    {
        var nfo = "SID:               GAMES\\0-9\\2112.sid\nName:              Test\n";
        var rewritten = SidNfoFixer.RewriteSidInText(nfo, "(None)");
        Assert.Equal("(None)", Gb64Nfo.Parse(rewritten).Sid);
    }

    [Fact]
    public void Parse_FirstSidWins_IgnoresSecondaryCgscLine()
    {
        var nfoText = """
            SID:               (None)
            Name:              Aftermath
            Compute's Gazette Sid Collection:
            SID: /CGSC/Lazy_Mode/Nice_to_Know.mus
            """;
        var nfo = Gb64Nfo.Parse(nfoText);
        Assert.Equal("(None)", nfo.Sid);
    }

    [Fact]
    public void RewriteSidInText_ClearsSecondaryPathLines()
    {
        var nfoText = """
            SID:               /CGSC/Lazy_Mode/Nice_to_Know.mus
            Name:              Aftermath
            Compute's Gazette Sid Collection:
            SID: /CGSC/Lazy_Mode/Nice_to_Know.mus
            """;
        var rewritten = SidNfoFixer.RewriteSidInText(nfoText, "(None)");
        Assert.DoesNotContain("Nice_to_Know.mus", rewritten);
        Assert.Equal("(None)", Gb64Nfo.Parse(rewritten).Sid);
    }
}
