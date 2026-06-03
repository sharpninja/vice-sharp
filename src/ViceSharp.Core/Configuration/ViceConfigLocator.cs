using Microsoft.Extensions.Configuration;

namespace ViceSharp.Core.Configuration;

/// <summary>
/// Resolves the folder that holds the ViceSharp INI files (vice.ini and
/// vice-sharp.ini). The folder is the only thing the deployed appsettings.json
/// configures - via <c>ViceSharp:ConfigDirectory</c>. When that value is empty
/// (the default), the canonical Classic VICE config directory is used so
/// ViceSharp shares one config with VICE. Environment variables in a configured
/// path are expanded.
/// </summary>
public static class ViceConfigLocator
{
    /// <summary>appsettings.json key that selects the INI folder.</summary>
    public const string ConfigDirectoryKey = "ViceSharp:ConfigDirectory";

    public const string ViceIniFileName = "vice.ini";
    public const string ViceSharpIniFileName = "vice-sharp.ini";

    /// <summary>
    /// The INI folder: the configured <see cref="ConfigDirectoryKey"/> (env vars
    /// expanded, made absolute), or the VICE default when unset/blank.
    /// </summary>
    public static string ResolveConfigDirectory(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configured = configuration[ConfigDirectoryKey];
        if (string.IsNullOrWhiteSpace(configured))
            return GetViceDefaultDirectory();

        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(configured.Trim()));
    }

    /// <summary>
    /// The canonical Classic VICE config directory:
    /// <c>%APPDATA%\vice</c> on Windows, else <c>$XDG_CONFIG_HOME/vice</c> or
    /// <c>~/.config/vice</c>.
    /// </summary>
    public static string GetViceDefaultDirectory()
    {
        if (OperatingSystem.IsWindows())
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "vice");

        var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var configHome = string.IsNullOrWhiteSpace(xdg)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
            : xdg;

        return Path.Combine(configHome, "vice");
    }

    /// <summary>Full path to vice.ini under the resolved config directory.</summary>
    public static string ViceIniPath(IConfiguration configuration) =>
        Path.Combine(ResolveConfigDirectory(configuration), ViceIniFileName);

    /// <summary>Full path to vice-sharp.ini under the resolved config directory.</summary>
    public static string ViceSharpIniPath(IConfiguration configuration) =>
        Path.Combine(ResolveConfigDirectory(configuration), ViceSharpIniFileName);
}
