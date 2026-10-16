using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-ROMM-RECENTS-001. File-backed <see cref="IRecentsStore"/> (source-generated JSON, AOT-safe).
/// </summary>
public sealed class FileRecentsStore : IRecentsStore
{
    private static readonly JsonTypeInfo<List<RecentGame>> ListInfo =
        (JsonTypeInfo<List<RecentGame>>)RomMJsonContext.Default.GetTypeInfo(typeof(List<RecentGame>))!;

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Creates the store.</summary>
    /// <param name="path">Absolute path of the recents JSON file.</param>
    public FileRecentsStore(string path) =>
        _path = string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Path is required.", nameof(path))
            : path;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecentGame>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await ReadUnlockedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RecordAsync(
        RecentGame game,
        int capacity = RecentGame.DefaultCapacity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (capacity < 1)
        {
            capacity = RecentGame.DefaultCapacity;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            List<RecentGame> list = (await ReadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            list.RemoveAll(g => g.Id == game.Id);
            list.Insert(0, game);
            if (list.Count > capacity)
            {
                list.RemoveRange(capacity, list.Count - capacity);
            }

            await WriteUnlockedAsync(list, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
        finally
        {
            _gate.Release();
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task<List<RecentGame>> ReadUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            return new List<RecentGame>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize(json, ListInfo) ?? new List<RecentGame>();
        }
        catch (JsonException)
        {
            return new List<RecentGame>();
        }
    }

    private async Task WriteUnlockedAsync(List<RecentGame> list, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(list, ListInfo);
        await File.WriteAllTextAsync(_path, json, cancellationToken).ConfigureAwait(false);
    }
}
