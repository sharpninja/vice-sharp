using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using ViceSharp.Host.Diagnostics;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public sealed class DiagnosticsServiceHost : IDiagnosticsService
{
    private const int MinimumWatchIntervalMs = 10;
    private const int DefaultWatchIntervalMs = 1000;

    private readonly EmulatorRuntimeRegistry _registry;
    private readonly HostDiagnosticsState _diagnosticsState;

    public DiagnosticsServiceHost(
        EmulatorRuntimeRegistry registry,
        HostDiagnosticsState diagnosticsState)
    {
        _registry = registry;
        _diagnosticsState = diagnosticsState;
    }

    public ValueTask<GetHostInfoResponse> GetHostInfoAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new GetHostInfoResponse(RpcStatus.Ok(), BuildHostInfo()));
    }

    public ValueTask<ListSessionsResponse> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sessions = _registry.Snapshot().Select(ToSummary).ToArray();
        return ValueTask.FromResult(new ListSessionsResponse(RpcStatus.Ok(), sessions));
    }

    public ValueTask<GetCurrentSessionResponse> GetCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var currentSessionId = _diagnosticsState.Snapshot().CurrentSessionId;
        if (string.IsNullOrWhiteSpace(currentSessionId))
            return ValueTask.FromResult(new GetCurrentSessionResponse(RpcStatus.NotFound("No current UI session is registered."), null));

        if (!_registry.TryGet(currentSessionId, out var session))
            return ValueTask.FromResult(new GetCurrentSessionResponse(HostProtocolMapper.MissingSessionStatus(currentSessionId), null));

        return ValueTask.FromResult(new GetCurrentSessionResponse(RpcStatus.Ok(), ToSummary(session)));
    }

    public ValueTask<PerformanceSnapshotResponse> GetPerformanceSnapshotAsync(
        PerformanceSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var sessionId = ResolveSessionId(request.SessionId);
        if (string.IsNullOrWhiteSpace(sessionId))
            return ValueTask.FromResult(new PerformanceSnapshotResponse(RpcStatus.NotFound("No current UI session is registered."), null));

        if (!_registry.TryGet(sessionId, out var session))
            return ValueTask.FromResult(new PerformanceSnapshotResponse(HostProtocolMapper.MissingSessionStatus(sessionId), null));

        return ValueTask.FromResult(new PerformanceSnapshotResponse(RpcStatus.Ok(), BuildSnapshot(session)));
    }

    public async IAsyncEnumerable<PerformanceSnapshotResponse> WatchPerformanceAsync(
        WatchPerformanceRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var interval = TimeSpan.FromMilliseconds(Math.Max(MinimumWatchIntervalMs, request.IntervalMs <= 0 ? DefaultWatchIntervalMs : request.IntervalMs));

        while (!cancellationToken.IsCancellationRequested)
        {
            yield return await GetPerformanceSnapshotAsync(
                new PerformanceSnapshotRequest(request.SessionId, request.IntervalMs),
                cancellationToken);

            await Task.Delay(interval, cancellationToken);
        }
    }

    private string ResolveSessionId(string requestedSessionId)
    {
        if (!string.IsNullOrWhiteSpace(requestedSessionId))
            return requestedSessionId;

        return _diagnosticsState.Snapshot().CurrentSessionId;
    }

    private PerformanceSnapshotDto BuildSnapshot(EmulatorRuntimeSession session)
    {
        return new PerformanceSnapshotDto(
            BuildHostInfo(),
            HostProtocolMapper.ToStatusDto(session),
            BuildProcessDiagnostics(),
            BuildPumpDiagnostics(),
            _diagnosticsState.ToUiDiagnostics());
    }

    private HostInfoDto BuildHostInfo()
    {
        var state = _diagnosticsState.Snapshot();
        var assembly = typeof(DiagnosticsServiceHost).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "0.0.0";
        var buildSha = string.Empty;
        var plus = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        if (plus >= 0 && plus < informationalVersion.Length - 1)
            buildSha = informationalVersion[(plus + 1)..];

        return new HostInfoDto(
            Environment.ProcessId,
            state.Endpoint?.ToString() ?? string.Empty,
            ViceSharpProtocol.Package,
            "1",
            informationalVersion,
            buildSha,
            state.StartedAtUtc);
    }

    private static ProcessDiagnosticsDto BuildProcessDiagnostics()
    {
        using var process = Process.GetCurrentProcess();
        return new ProcessDiagnosticsDto(
            (long)process.TotalProcessorTime.TotalMilliseconds,
            process.WorkingSet64,
            process.PrivateMemorySize64,
            GC.GetTotalMemory(forceFullCollection: false),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            process.Threads.Count);
    }

    private PumpDiagnosticsDto BuildPumpDiagnostics()
    {
        var activeSessionCount = _registry.Snapshot().Count(session => session.RunState == EmulatorRunState.Running);
        return new PumpDiagnosticsDto(
            true,
            activeSessionCount,
            DateTimeOffset.UtcNow);
    }

    private static SessionSummaryDto ToSummary(EmulatorRuntimeSession session)
    {
        var state = session.Machine.GetState();
        var media = session.MediaAttachments.Values
            .Where(attachment => attachment.IsAttached)
            .Select(attachment => string.IsNullOrWhiteSpace(attachment.DisplayName) ? attachment.FilePath : attachment.DisplayName)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SessionSummaryDto(
            session.SessionId,
            session.SessionId,
            session.Architecture.MachineName,
            session.RunState,
            state.Cycle,
            session.FrameCount,
            string.Join("; ", media));
    }
}
