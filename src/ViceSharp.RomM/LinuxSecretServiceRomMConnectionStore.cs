using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.RomM;

/// <summary>FR-ROMM-CONN-001 / TR-ROMM-SEC-001: persists Linux credentials in Secret Service.</summary>
public sealed class LinuxSecretServiceRomMConnectionStore : IRomMConnectionStore
{
    private static readonly JsonTypeInfo<LinuxStoredRomMConnection> StoredInfo =
        (JsonTypeInfo<LinuxStoredRomMConnection>)RomMJsonContext.Default.GetTypeInfo(typeof(LinuxStoredRomMConnection))!;
    private static readonly JsonTypeInfo<RomMConnection> LegacyInfo =
        (JsonTypeInfo<RomMConnection>)RomMJsonContext.Default.GetTypeInfo(typeof(RomMConnection))!;
    private readonly string _path;
    private readonly Func<IReadOnlyList<string>, string?, CancellationToken, Task<SecretToolResult>> _run;
    private readonly Action<string, string> _publish;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Creates a store backed by the current user's Secret Service keyring.</summary>
    /// <param name="path">Connection metadata path; credential values are never stored here.</param>
    public LinuxSecretServiceRomMConnectionStore(string path)
        : this(path, RunSecretToolAsync)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("This credential store requires Linux Secret Service.");
    }

    internal LinuxSecretServiceRomMConnectionStore(
        string path,
        Func<IReadOnlyList<string>, string?, CancellationToken, Task<SecretToolResult>> run,
        Action<string, string>? publish = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
        _run = run ?? throw new ArgumentNullException(nameof(run));
        _publish = publish ?? ((source, destination) => File.Move(source, destination, overwrite: true));
    }

    /// <inheritdoc />
    public async Task<RomMConnection?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path))
                return null;

            var json = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("secret_id", out _))
            {
                var stored = JsonSerializer.Deserialize(json, StoredInfo)
                    ?? throw new InvalidOperationException("RomM connection metadata is invalid.");
                ValidateSecretId(stored.SecretId);
                var result = await RunCheckedAsync("lookup", stored.SecretId, null, cancellationToken).ConfigureAwait(false);
                var token = result.StandardOutput.TrimEnd('\r', '\n');
                if (string.IsNullOrWhiteSpace(token))
                    throw KeyringUnavailable();
                return new RomMConnection(stored.BaseUrl, stored.AuthMode, token, stored.Username);
            }

            if (document.RootElement.TryGetProperty("protected_token", out _))
                throw new InvalidOperationException("This RomM connection was protected on Windows. Connect again to save it in the Linux keyring.");

            var legacy = JsonSerializer.Deserialize(json, LegacyInfo)
                ?? throw new InvalidOperationException("RomM connection metadata is invalid.");
            ArgumentException.ThrowIfNullOrWhiteSpace(legacy.Token);
            await SaveUnlockedAsync(legacy, cancellationToken).ConfigureAwait(false);
            return legacy;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(RomMConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(connection.Token);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await SaveUnlockedAsync(connection, cancellationToken).ConfigureAwait(false);
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
            var previous = await ReadStoredAsync(cancellationToken).ConfigureAwait(false);
            if (previous is not null)
                await RunCheckedAsync("clear", previous.SecretId, null, cancellationToken).ConfigureAwait(false);
            File.Delete(_path);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SaveUnlockedAsync(RomMConnection connection, CancellationToken cancellationToken)
    {
        var previous = await ReadStoredAsync(cancellationToken).ConfigureAwait(false);
        var secretId = Guid.NewGuid().ToString("N");
        var temp = _path + "." + secretId + ".tmp";
        var committed = false;
        try
        {
            await RunCheckedAsync("store", secretId, connection.Token, cancellationToken).ConfigureAwait(false);
            var stored = new LinuxStoredRomMConnection(connection.BaseUrl, connection.AuthMode, connection.Username, secretId);
            var json = JsonSerializer.Serialize(stored, StoredInfo);
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var options = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.Asynchronous,
            };
            if (OperatingSystem.IsLinux())
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

            await using (var stream = new FileStream(temp, options))
            await using (var writer = new StreamWriter(stream))
                await writer.WriteAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            _publish(temp, _path);
            committed = true;
        }
        finally
        {
            if (!committed)
                await TryClearSecretAsync(secretId).ConfigureAwait(false);
            if (File.Exists(temp))
                File.Delete(temp);
        }

        if (previous is not null)
            await TryClearSecretAsync(previous.SecretId).ConfigureAwait(false);
    }

    private async Task<LinuxStoredRomMConnection?> ReadStoredAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
            return null;
        var json = await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("secret_id", out _))
            return null;
        var stored = JsonSerializer.Deserialize(json, StoredInfo);
        if (stored is not null)
            ValidateSecretId(stored.SecretId);
        return stored;
    }

    private async Task<SecretToolResult> RunCheckedAsync(string command, string secretId, string? input, CancellationToken cancellationToken)
    {
        ValidateSecretId(secretId);
        IReadOnlyList<string> arguments = command == "store"
            ? [command, "--label=ViceSharp RomM", "application", "ViceSharp", "connection", secretId]
            : [command, "application", "ViceSharp", "connection", secretId];
        var result = await _run(arguments, input, cancellationToken).ConfigureAwait(false);
        if (result.ExitCode != 0)
            throw KeyringUnavailable();
        return result;
    }

    private async Task TryClearSecretAsync(string secretId)
    {
        try
        {
            await RunCheckedAsync("clear", secretId, null, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Failed cleanup must not replace the original error or invalidate a committed save.
        }
    }

    private static void ValidateSecretId(string secretId)
    {
        if (!Guid.TryParseExact(secretId, "N", out _))
            throw new InvalidOperationException("RomM connection metadata contains an invalid keyring reference.");
    }

    private static InvalidOperationException KeyringUnavailable()
        => new("RomM credentials are unavailable. Install libsecret's secret-tool, unlock your login keyring, and connect again.");

    private static async Task<SecretToolResult> RunSecretToolAsync(
        IReadOnlyList<string> arguments, string? input, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        var start = new ProcessStartInfo("/usr/bin/secret-tool")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start())
                throw KeyringUnavailable();
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            if (input is not null)
                await process.StandardInput.WriteAsync(input.AsMemory(), timeout.Token).ConfigureAwait(false);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            await error.ConfigureAwait(false); // Drain diagnostics, but never expose potentially sensitive output.
            return new SecretToolResult(process.ExitCode, await output.ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            await StopProcessAsync(process).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("RomM keyring access timed out. Unlock your login keyring and connect again.");
        }
        catch (Exception ex) when (ex is Win32Exception or IOException)
        {
            await StopProcessAsync(process).ConfigureAwait(false);
            throw KeyringUnavailable();
        }
    }

    private static async Task StopProcessAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException) { }
        catch (Win32Exception) { }
    }
}

internal sealed record SecretToolResult(int ExitCode, string StandardOutput);
internal sealed record LinuxStoredRomMConnection(string BaseUrl, RomMAuthMode AuthMode, string? Username, string SecretId);
