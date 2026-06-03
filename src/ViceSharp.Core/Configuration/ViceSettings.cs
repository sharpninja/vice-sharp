using Microsoft.Extensions.Configuration;

namespace ViceSharp.Core.Configuration;

/// <summary>
/// Emulator settings managed through IConfiguration over the two INI files:
/// the canonical Classic VICE <c>vice.ini</c> and the ViceSharp-only
/// <c>vice-sharp.ini</c>. Both are layered into one read-only
/// <see cref="Configuration"/>; writes are routed to the correct file
/// (<see cref="SetVice"/> -> vice.ini, <see cref="SetViceSharp"/> ->
/// vice-sharp.ini) and persisted by <see cref="Save"/> via the custom INI
/// reader/writer, so VICE's own resources are never disturbed.
///
/// The config folder comes from <see cref="ViceConfigLocator"/> (appsettings.json
/// or the VICE default).
/// </summary>
public sealed class ViceSettings
{
    private readonly ViceIniConfigurationProvider _vice;
    private readonly ViceIniConfigurationProvider _viceSharp;
    private readonly IConfigurationRoot _configuration;

    private ViceSettings(ViceIniConfigurationProvider vice, ViceIniConfigurationProvider viceSharp)
    {
        _vice = vice;
        _viceSharp = viceSharp;
        // ConfigurationRoot loads each provider; vice-sharp.ini layers last.
        _configuration = new ConfigurationRoot([_vice, _viceSharp]);
    }

    /// <summary>Open the settings rooted at the folder resolved from app configuration (appsettings.json).</summary>
    public static ViceSettings Open(IConfiguration appConfiguration)
    {
        ArgumentNullException.ThrowIfNull(appConfiguration);
        return OpenAt(ViceConfigLocator.ResolveConfigDirectory(appConfiguration));
    }

    /// <summary>Open the settings rooted at an explicit config folder.</summary>
    public static ViceSettings OpenAt(string configDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);

        var vice = new ViceIniConfigurationProvider(new ViceIniConfigurationSource
        {
            FilePath = Path.Combine(configDirectory, ViceConfigLocator.ViceIniFileName),
        });
        var viceSharp = new ViceIniConfigurationProvider(new ViceIniConfigurationSource
        {
            FilePath = Path.Combine(configDirectory, ViceConfigLocator.ViceSharpIniFileName),
        });

        return new ViceSettings(vice, viceSharp);
    }

    /// <summary>The layered, read-only view of both INI files.</summary>
    public IConfiguration Configuration => _configuration;

    public string ViceIniPath => _vice.FilePath;
    public string ViceSharpIniPath => _viceSharp.FilePath;

    /// <summary>Read a resource (vice-sharp.ini shadows vice.ini if both define it). Null if absent.</summary>
    public string? Get(string section, string key) => _configuration[ToKey(section, key)];

    /// <summary>Set a canonical VICE resource (persists to vice.ini).</summary>
    public void SetVice(string section, string key, string value, bool? quote = null) =>
        _vice.SetResource(section, key, value, quote);

    /// <summary>Set a ViceSharp-only setting that exceeds vice.ini (persists to vice-sharp.ini).</summary>
    public void SetViceSharp(string section, string key, string value, bool? quote = null) =>
        _viceSharp.SetResource(section, key, value, quote);

    /// <summary>Persist both files.</summary>
    public void Save()
    {
        _vice.Save();
        _viceSharp.Save();
    }

    private static string ToKey(string section, string key) =>
        $"{section}{ConfigurationPath.KeyDelimiter}{key}";
}
