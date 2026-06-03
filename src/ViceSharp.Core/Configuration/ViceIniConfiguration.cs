using Microsoft.Extensions.Configuration;

namespace ViceSharp.Core.Configuration;

/// <summary>
/// IConfiguration source backed by a Classic VICE INI file. Resources are
/// exposed as <c>Section:Resource</c> configuration keys (e.g.
/// <c>C64SC:VICIIModel</c>). The matching provider also writes changes back
/// through <see cref="ViceIniDocument"/> (read-modify-write), preserving the
/// file's unknown resources and value quoting.
/// </summary>
public sealed class ViceIniConfigurationSource : IConfigurationSource
{
    public required string FilePath { get; init; }

    /// <summary>When true (default) a missing file yields empty configuration instead of throwing.</summary>
    public bool Optional { get; init; } = true;

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new ViceIniConfigurationProvider(this);
}

/// <summary>
/// The custom reader/writer behind <see cref="ViceIniConfigurationSource"/>.
/// <see cref="Load"/> parses the INI into the configuration data;
/// <see cref="Save"/> writes it back losslessly.
/// </summary>
public sealed class ViceIniConfigurationProvider : ConfigurationProvider
{
    private readonly ViceIniConfigurationSource _source;
    private readonly Dictionary<string, bool> _quoting = new(StringComparer.OrdinalIgnoreCase);

    public ViceIniConfigurationProvider(ViceIniConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>The INI file this provider reads from and writes to.</summary>
    public string FilePath => _source.FilePath;

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(_source.FilePath))
        {
            var document = ViceIniDocument.Parse(File.ReadAllText(_source.FilePath));
            foreach (var section in document.Sections)
            {
                foreach (var (key, value) in document.Entries(section))
                    data[ToConfigKey(section, key)] = value;
            }
        }
        else if (!_source.Optional)
        {
            throw new FileNotFoundException($"Required VICE INI file not found: {_source.FilePath}", _source.FilePath);
        }

        Data = data;
    }

    /// <summary>
    /// Set a resource. <paramref name="quote"/> controls how it is written by
    /// <see cref="Save"/>: true = double-quoted (string resource), false = bare,
    /// null = preserve the existing entry's quoting (or bare if new).
    /// </summary>
    public void SetResource(string section, string key, string value, bool? quote = null)
    {
        var configKey = ToConfigKey(section, key);
        Set(configKey, value);
        if (quote.HasValue)
            _quoting[configKey] = quote.Value;
    }

    /// <summary>
    /// Write the current configuration back to the INI file via a
    /// read-modify-write: the file's existing resources keep their quoting and
    /// any resources ViceSharp does not manage are preserved verbatim.
    /// </summary>
    public void Save()
    {
        var document = File.Exists(_source.FilePath)
            ? ViceIniDocument.Parse(File.ReadAllText(_source.FilePath))
            : new ViceIniDocument();

        foreach (var (configKey, value) in Data)
        {
            if (value is null || !TrySplitConfigKey(configKey, out var section, out var resource))
                continue;

            var quote = _quoting.TryGetValue(configKey, out var q) ? q : (bool?)null;
            document.Set(section, resource, value, quote);
        }

        var directory = Path.GetDirectoryName(_source.FilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_source.FilePath, document.ToIniString());
    }

    private static string ToConfigKey(string section, string key) =>
        $"{section}{ConfigurationPath.KeyDelimiter}{key}";

    private static bool TrySplitConfigKey(string configKey, out string section, out string resource)
    {
        var index = configKey.IndexOf(ConfigurationPath.KeyDelimiter, StringComparison.Ordinal);
        if (index <= 0 || index >= configKey.Length - 1)
        {
            section = string.Empty;
            resource = string.Empty;
            return false;
        }

        section = configKey[..index];
        resource = configKey[(index + 1)..];
        return true;
    }
}

/// <summary>Builder extension for adding a VICE INI file as a configuration source.</summary>
public static class ViceIniConfigurationExtensions
{
    public static IConfigurationBuilder AddViceIni(this IConfigurationBuilder builder, string filePath, bool optional = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return builder.Add(new ViceIniConfigurationSource { FilePath = filePath, Optional = optional });
    }
}
