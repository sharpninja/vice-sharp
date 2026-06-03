using System.Diagnostics;
using System.Globalization;
using System.IO;
using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Services;

public interface ILocalVideoFrameSource
{
    ValueTask<GetVideoFrameResponse> GetFrameAsync(string sessionId, CancellationToken cancellationToken = default);
}

public sealed class LocalVideoFrameSource : ILocalVideoFrameSource
{
    // BRANDING/PERF-DIAG: gated frame-pump instrumentation. When
    // VICESHARP_FRAME_LOG=1, every 50 produced frames a line is appended to
    // %TEMP%/vicesharp-frame-log.txt with the average per-call RunFrame time,
    // the status-bar EffectiveClockPercent, and measured fps. Off by default
    // (the env var is read once), so the hot path is unchanged in normal runs.
    private static readonly bool FrameLogEnabled =
        string.Equals(Environment.GetEnvironmentVariable("VICESHARP_FRAME_LOG"), "1", StringComparison.Ordinal);
    private static readonly string FrameLogPath =
        Path.Combine(Path.GetTempPath(), "vicesharp-frame-log.txt");

    private readonly EmulatorRuntimeRegistry _registry;
    private readonly object _frameLogLock = new();
    private long _loggedFrames;
    private double _runFrameMsSum;

    public LocalVideoFrameSource(EmulatorRuntimeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public ValueTask<GetVideoFrameResponse> GetFrameAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_registry.TryGet(sessionId, out var session))
            return ValueTask.FromResult(new GetVideoFrameResponse(HostProtocolMapper.MissingSessionStatus(sessionId), null));

        lock (session.SyncRoot)
        {
            var runStart = FrameLogEnabled ? Stopwatch.GetTimestamp() : 0L;
            if (session.RunState == EmulatorRunState.Running)
            {
                if (!session.LimiterEnabled)
                {
                    var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency / 50;
                    do
                    {
                        session.Machine.RunFrame();
                        session.RecordFrame();
                        session.AdvanceHostAutomationFrame();
                    } while (Stopwatch.GetTimestamp() < deadline);
                }
                else
                {
                    session.Machine.RunFrame();
                    session.RecordFrame();
                    session.AdvanceHostAutomationFrame();
                }
            }

            if (FrameLogEnabled && session.RunState == EmulatorRunState.Running)
                LogFrameTiming(session, Stopwatch.GetElapsedTime(runStart).TotalMilliseconds);

            var videoChip = session.Machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
            if (videoChip is null)
                return ValueTask.FromResult(new GetVideoFrameResponse(RpcStatus.Unavailable("The session has no video chip."), null));

            var frame = new byte[videoChip.FrameBuffer.Length];
            videoChip.FrameBuffer.CopyTo(frame, 0);
            return ValueTask.FromResult(new GetVideoFrameResponse(
                RpcStatus.Ok(),
                new VideoFrameDto(videoChip.FrameWidth, videoChip.FrameHeight, session.Machine.GetState().Cycle, frame)));
        }
    }

    private void LogFrameTiming(EmulatorRuntimeSession session, double runFrameMs)
    {
        const int flushEvery = 50;
        lock (_frameLogLock)
        {
            _loggedFrames++;
            _runFrameMsSum += runFrameMs;
            if (_loggedFrames % flushEvery != 0)
                return;

            var avgRunFrameMs = _runFrameMsSum / flushEvery;
            _runFrameMsSum = 0;

            var nominalClockHz = session.Architecture.MasterClockHz;
            var effectiveClockPercent = nominalClockHz > 0
                ? session.EffectiveClockHz / nominalClockHz * 100.0
                : 0.0;

            var line = string.Create(CultureInfo.InvariantCulture,
                $"frames={_loggedFrames} arch=\"{session.Architecture.MachineName}\" limiter={session.LimiterEnabled} rate={session.LimiterRatePercent:0} avgRunFrameMs={avgRunFrameMs:0.000} budgetMs={1000.0 / 50.125:0.00} effClockPercent={effectiveClockPercent:0.0} fps={session.MeasuredFramesPerSecond:0.0} nominalMHz={nominalClockHz / 1e6:0.000}{Environment.NewLine}");

            try
            {
                File.AppendAllText(FrameLogPath, line);
            }
            catch
            {
                // Diagnostics only; never let logging affect the frame pump.
            }
        }
    }
}
