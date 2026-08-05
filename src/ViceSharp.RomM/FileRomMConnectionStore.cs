using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-05). A portable file-backed <see cref="IRomMConnectionStore"/> that
/// round-trips the connection as source-generated JSON (the desktop default; Xbox uses the keystore).
/// </summary>
public sealed class FileRomMConnectionStore : IRomMConnectionStore
{
    private static readonly JsonTypeInfo<RomMConnection> Info =
        (JsonTypeInfo<RomMConnection>)RomMJsonContext.Default.GetTypeInfo(typeof(RomMConnection))!;

    private readonly string _path;

    /// <summary>Creates the store.</summary>
    /// <param name="path">The file path the connection is persisted to.</param>
    public FileRomMConnectionStore(string path) =>
        _path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Path is required.", nameof(path)) : path;

    /// <inheritdoc />
    public async Task<RomMConnection?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, Info);
    }

    /// <inheritdoc />
    public async Task SaveAsync(RomMConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(connection, Info);
        await File.WriteAllTextAsync(_path, json, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }

        return Task.CompletedTask;
    }
}
