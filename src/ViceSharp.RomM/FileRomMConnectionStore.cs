using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>
/// FR-ROMM-CONN-001 (AC-CONN-05). File-backed connection metadata with the credential protected
/// for the current Windows user. Legacy plaintext files are migrated on first successful load.
/// </summary>
public sealed class FileRomMConnectionStore : IRomMConnectionStore
{
    private static readonly JsonTypeInfo<RomMConnection> LegacyInfo =
        (JsonTypeInfo<RomMConnection>)RomMJsonContext.Default.GetTypeInfo(typeof(RomMConnection))!;

    private static readonly JsonTypeInfo<StoredRomMConnection> StoredInfo =
        (JsonTypeInfo<StoredRomMConnection>)RomMJsonContext.Default.GetTypeInfo(typeof(StoredRomMConnection))!;

    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ViceSharp.RomM.Connection.v1");
    private readonly string _path;

    /// <summary>Creates the store.</summary>
    /// <param name="path">The file path where protected connection metadata is persisted.</param>
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
        using JsonDocument document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("protected_token", out _))
        {
            StoredRomMConnection? stored = JsonSerializer.Deserialize(json, StoredInfo);
            return stored is null
                ? null
                : new RomMConnection(stored.BaseUrl, stored.AuthMode, Unprotect(stored.ProtectedToken), stored.Username);
        }

        // Secure migration for the pre-protection shape. Rewriting completes before the
        // credential is returned so callers cannot silently continue using plaintext storage.
        RomMConnection? legacy = JsonSerializer.Deserialize(json, LegacyInfo);
        if (legacy is null)
        {
            return null;
        }

        await SaveAsync(legacy, cancellationToken).ConfigureAwait(false);
        return legacy;
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

        var stored = new StoredRomMConnection(
            connection.BaseUrl,
            connection.AuthMode,
            Protect(connection.Token),
            connection.Username);
        string json = JsonSerializer.Serialize(stored, StoredInfo);
        string temp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temp, json, cancellationToken).ConfigureAwait(false);
            File.Move(temp, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }

        return Task.CompletedTask;
    }

    private static string Protect(string token)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "FileRomMConnectionStore requires a platform credential protector; use a platform-specific IRomMConnectionStore.");
        }

        byte[] plaintext = Encoding.UTF8.GetBytes(token);
        byte[] protectedBytes = ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);
        CryptographicOperations.ZeroMemory(plaintext);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string protectedToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "FileRomMConnectionStore requires a platform credential protector; use a platform-specific IRomMConnectionStore.");
        }

        byte[] protectedBytes = Convert.FromBase64String(protectedToken);
        byte[] plaintext = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
        try
        {
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}

internal sealed record StoredRomMConnection(
    string BaseUrl,
    RomMAuthMode AuthMode,
    string ProtectedToken,
    string? Username);
