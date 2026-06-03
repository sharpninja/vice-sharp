namespace ViceSharp.TestHarness.Configuration;

using ViceSharp.Core.Configuration;
using Xunit;

/// <summary>
/// SETTINGS-INI-001: ViceSharp reads and writes the canonical Classic VICE
/// vice.ini (and the ViceSharp-only vice-sharp.ini). <see cref="ViceIniDocument"/>
/// is the custom reader/writer underneath the IConfiguration layer. Because it
/// edits the user's <em>shared</em> VICE config, lossless round-trip is the key
/// guarantee: unknown sections/resources and value quoting must survive a
/// read-modify-write so ViceSharp never corrupts a Classic VICE config.
/// </summary>
public sealed class ViceIniDocumentTests
{
    private const string Sample =
        "[Version]\n" +
        "ConfigVersion=3.8\n" +
        "\n" +
        "[C64SC]\n" +
        "SaveResourcesOnExit=1\n" +
        "VICIIModel=3\n" +
        "SidModel=0\n" +
        "WIC64MACAddress=\"08:d1:f9:0a:0c:0e\"\n" +
        "Drive8Type=1541\n" +
        "\n";

    [Fact]
    public void Get_ReadsIntAndUnquotesString()
    {
        var doc = ViceIniDocument.Parse(Sample);

        Assert.Equal("3.8", doc.Get("Version", "ConfigVersion"));
        Assert.Equal("3", doc.Get("C64SC", "VICIIModel"));
        Assert.Equal("08:d1:f9:0a:0c:0e", doc.Get("C64SC", "WIC64MACAddress"));   // quotes stripped
        Assert.Null(doc.Get("C64SC", "NoSuchResource"));
        Assert.Null(doc.Get("NoSuchSection", "VICIIModel"));
    }

    [Fact]
    public void Parse_ThenSerialize_RoundTripsLosslessly()
    {
        var doc = ViceIniDocument.Parse(Sample);

        // Same sections, keys, values, and quoting come back out.
        Assert.Equal(Sample, doc.ToIniString());
    }

    [Fact]
    public void Set_UpdatesExistingValue_AndPreservesEverythingElse()
    {
        var doc = ViceIniDocument.Parse(Sample);

        doc.Set("C64SC", "VICIIModel", "1");

        Assert.Equal("1", doc.Get("C64SC", "VICIIModel"));
        // Unrelated resources (including ones ViceSharp doesn't know) survive.
        Assert.Equal("1541", doc.Get("C64SC", "Drive8Type"));
        Assert.Equal("08:d1:f9:0a:0c:0e", doc.Get("C64SC", "WIC64MACAddress"));
        Assert.Equal("3.8", doc.Get("Version", "ConfigVersion"));
    }

    [Fact]
    public void Set_UpdatingQuotedString_PreservesQuotesByDefault()
    {
        var doc = ViceIniDocument.Parse(Sample);

        doc.Set("C64SC", "WIC64MACAddress", "aa:bb:cc:dd:ee:ff");   // no quote arg

        Assert.Contains("WIC64MACAddress=\"aa:bb:cc:dd:ee:ff\"", doc.ToIniString());
    }

    [Fact]
    public void Set_AddsNewKeyToExistingSection()
    {
        var doc = ViceIniDocument.Parse(Sample);

        doc.Set("C64SC", "VICIIFilter", "2");

        Assert.Equal("2", doc.Get("C64SC", "VICIIFilter"));
        Assert.Contains("VICIIFilter=2", doc.ToIniString());
    }

    [Fact]
    public void Set_AddsNewSectionWhenMissing()
    {
        var doc = ViceIniDocument.Parse(Sample);

        doc.Set("C128", "VDCRevision", "1");

        Assert.Equal("1", doc.Get("C128", "VDCRevision"));
        Assert.Contains("[C128]", doc.ToIniString());
    }

    [Fact]
    public void SetString_QuotesValue_AndGetUnquotes()
    {
        var doc = ViceIniDocument.Parse(Sample);

        doc.Set("C64SC", "WIC64IPAddress", "192.168.41.165", quote: true);

        Assert.Contains("WIC64IPAddress=\"192.168.41.165\"", doc.ToIniString());
        Assert.Equal("192.168.41.165", doc.Get("C64SC", "WIC64IPAddress"));
    }

    [Fact]
    public void Remove_DropsKey_AndReportsResult()
    {
        var doc = ViceIniDocument.Parse(Sample);

        Assert.True(doc.Remove("C64SC", "SidModel"));
        Assert.False(doc.Remove("C64SC", "SidModel"));
        Assert.Null(doc.Get("C64SC", "SidModel"));
        Assert.DoesNotContain("SidModel", doc.ToIniString());
    }
}
