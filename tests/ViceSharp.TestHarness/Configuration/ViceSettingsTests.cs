namespace ViceSharp.TestHarness.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using ViceSharp.Core.Configuration;
using Xunit;

/// <summary>
/// SETTINGS-INI-001: emulator settings managed through IConfiguration over the
/// canonical vice.ini + the ViceSharp-only vice-sharp.ini, with a custom
/// reader/writer. These cover the round-trip guarantees that protect the user's
/// shared VICE config and the two-file routing.
/// </summary>
public sealed class ViceSettingsTests : IDisposable
{
    private readonly string _dir;

    public ViceSettingsTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vicesharp-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort temp cleanup */ }
    }

    private void WriteViceIni(string content) => File.WriteAllText(Path.Combine(_dir, "vice.ini"), content);

    [Fact]
    public void Configuration_ExposesViceResourcesAsKeys()
    {
        WriteViceIni("[C64SC]\nVICIIModel=3\nWIC64MACAddress=\"08:d1:f9:0a:0c:0e\"\n\n");

        var settings = ViceSettings.OpenAt(_dir);

        Assert.Equal("3", settings.Configuration["C64SC:VICIIModel"]);
        Assert.Equal("08:d1:f9:0a:0c:0e", settings.Configuration["C64SC:WIC64MACAddress"]); // unquoted in the model
        Assert.Equal("3", settings.Get("C64SC", "VICIIModel"));
    }

    [Fact]
    public void SetVice_Save_UpdatesViceIni_PreservingUnknownResourcesAndQuoting()
    {
        WriteViceIni("[C64SC]\nVICIIModel=3\nWIC64MACAddress=\"08:d1:f9:0a:0c:0e\"\nDrive8Type=1541\n\n");
        var settings = ViceSettings.OpenAt(_dir);

        settings.SetVice("C64SC", "VICIIModel", "1");
        settings.Save();

        var written = File.ReadAllText(Path.Combine(_dir, "vice.ini"));
        Assert.Contains("VICIIModel=1", written);
        Assert.Contains("Drive8Type=1541", written);                      // resource ViceSharp doesn't manage: preserved
        Assert.Contains("WIC64MACAddress=\"08:d1:f9:0a:0c:0e\"", written); // string quoting: preserved

        Assert.Equal("1", ViceSettings.OpenAt(_dir).Get("C64SC", "VICIIModel"));
    }

    [Fact]
    public void SetViceSharp_Save_WritesToViceSharpIni_NotViceIni()
    {
        WriteViceIni("[C64SC]\nVICIIModel=3\n\n");
        var settings = ViceSettings.OpenAt(_dir);

        settings.SetViceSharp("ViceSharp", "RendererBackend", "skia", quote: true);
        settings.Save();

        Assert.DoesNotContain("RendererBackend", File.ReadAllText(Path.Combine(_dir, "vice.ini")));

        var viceSharpIni = File.ReadAllText(Path.Combine(_dir, "vice-sharp.ini"));
        Assert.Contains("RendererBackend=\"skia\"", viceSharpIni);

        Assert.Equal("skia", ViceSettings.OpenAt(_dir).Get("ViceSharp", "RendererBackend"));
    }

    [Fact]
    public void Save_NewFile_HonoursExplicitQuoting()
    {
        var settings = ViceSettings.OpenAt(_dir);

        settings.SetVice("C64SC", "VICIIModel", "3");                       // bare int
        settings.SetVice("C64SC", "WIC64IPAddress", "10.0.0.1", quote: true); // quoted string
        settings.Save();

        var written = File.ReadAllText(Path.Combine(_dir, "vice.ini"));
        Assert.Contains("VICIIModel=3", written);
        Assert.Contains("WIC64IPAddress=\"10.0.0.1\"", written);
    }

    [Fact]
    public void Open_UsesIniFolderFromAppConfiguration()
    {
        var appConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("ViceSharp:ConfigDirectory", _dir) })
            .Build();

        var settings = ViceSettings.Open(appConfig);

        Assert.Equal(Path.Combine(Path.GetFullPath(_dir), "vice.ini"), settings.ViceIniPath);
        Assert.Equal(Path.Combine(Path.GetFullPath(_dir), "vice-sharp.ini"), settings.ViceSharpIniPath);
    }
}
