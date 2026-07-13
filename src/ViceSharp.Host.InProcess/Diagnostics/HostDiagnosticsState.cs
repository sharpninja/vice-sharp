using ViceSharp.Protocol;

namespace ViceSharp.Host.Diagnostics;

public sealed class HostDiagnosticsState
{
    private readonly object _syncRoot = new();

    private Uri? _endpoint;
    private string _currentSessionId = string.Empty;
    private DateTimeOffset _lastStatusUpdateUtc;
    private DateTimeOffset _lastFrameUpdateUtc;

    public HostDiagnosticsState()
    {
        StartedAtUtc = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset StartedAtUtc { get; }

    public void UpdateEndpoint(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        lock (_syncRoot)
        {
            _endpoint = endpoint;
        }
    }

    public void UpdateCurrentSession(string? sessionId)
    {
        lock (_syncRoot)
        {
            _currentSessionId = sessionId ?? string.Empty;
        }
    }

    public void MarkStatusUpdated(DateTimeOffset? timestampUtc = null)
    {
        lock (_syncRoot)
        {
            _lastStatusUpdateUtc = timestampUtc ?? DateTimeOffset.UtcNow;
        }
    }

    public void MarkFrameUpdated(DateTimeOffset? timestampUtc = null)
    {
        lock (_syncRoot)
        {
            _lastFrameUpdateUtc = timestampUtc ?? DateTimeOffset.UtcNow;
        }
    }

    public HostDiagnosticsStateSnapshot Snapshot()
    {
        lock (_syncRoot)
        {
            return new HostDiagnosticsStateSnapshot(
                _endpoint,
                _currentSessionId,
                StartedAtUtc,
                _lastStatusUpdateUtc,
                _lastFrameUpdateUtc);
        }
    }

    public UiDiagnosticsDto ToUiDiagnostics()
    {
        var snapshot = Snapshot();
        return new UiDiagnosticsDto(
            snapshot.CurrentSessionId,
            snapshot.LastStatusUpdateUtc,
            snapshot.LastFrameUpdateUtc);
    }
}

public sealed record HostDiagnosticsStateSnapshot(
    Uri? Endpoint,
    string CurrentSessionId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset LastStatusUpdateUtc,
    DateTimeOffset LastFrameUpdateUtc);
