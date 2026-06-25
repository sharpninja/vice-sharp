using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViceSharp.Host.Diagnostics;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.Host;

public sealed class DebugAttachFilePublisher : IAsyncDisposable
{
    private readonly string _path;
    private readonly HostDiagnosticsState _diagnosticsState;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public DebugAttachFilePublisher(string path, HostDiagnosticsState diagnosticsState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(diagnosticsState);

        _path = path;
        _diagnosticsState = diagnosticsState;
    }

    public static string DefaultPath
    {
        get
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "ViceSharp", "debug-attach.json");
        }
    }

    public async ValueTask WriteAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var state = _diagnosticsState.Snapshot();
            var payload = new DebugAttachFilePayload
            {
                SchemaVersion = 1,
                ProcessId = Environment.ProcessId,
                Endpoint = state.Endpoint?.ToString() ?? string.Empty,
                CurrentSessionId = state.CurrentSessionId,
                ProtocolPackage = ViceSharpProtocol.Package,
                AppVersion = AppVersion,
                StartedAtUtc = state.StartedAtUtc,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
                AuthMode = "none"
            };
            var json = JsonSerializer.Serialize(payload, DebugAttachJsonContext.Default.DebugAttachFilePayload);
            var tempPath = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, _path, overwrite: true);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask UpdateCurrentSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _diagnosticsState.UpdateCurrentSession(sessionId);
        await WriteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
        finally
        {
            _writeLock.Release();
            _writeLock.Dispose();
        }
    }

    public async Task<string> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
            return string.Empty;

        return await File.ReadAllTextAsync(_path, cancellationToken).ConfigureAwait(false);
    }

    private static string AppVersion
    {
        get
        {
            var assembly = typeof(DebugAttachFilePublisher).Assembly;
            return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? "0.0.0";
        }
    }
}

internal sealed record DebugAttachFilePayload
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("processId")]
    public int ProcessId { get; init; }

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; init; } = string.Empty;

    [JsonPropertyName("currentSessionId")]
    public string? CurrentSessionId { get; init; }

    [JsonPropertyName("protocolPackage")]
    public string ProtocolPackage { get; init; } = string.Empty;

    [JsonPropertyName("appVersion")]
    public string AppVersion { get; init; } = string.Empty;

    [JsonPropertyName("startedAtUtc")]
    public DateTimeOffset StartedAtUtc { get; init; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; init; }

    [JsonPropertyName("authMode")]
    public string AuthMode { get; init; } = string.Empty;
}

[JsonSerializable(typeof(DebugAttachFilePayload))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class DebugAttachJsonContext : JsonSerializerContext
{
}
