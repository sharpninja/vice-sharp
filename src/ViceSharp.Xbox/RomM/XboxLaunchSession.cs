#if HAS_UWP
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using ViceSharp.Xbox.Platform;

namespace ViceSharp.Xbox.RomM;

/// <summary>
/// PLAN-ROMM-001 X2 (AC-LAUNCH-05). The concrete <see cref="IXboxLaunchSession"/> over the head's
/// in-process session: payload-attach through <see cref="InProcessSessionFacade"/>, disk autostart
/// through the console host service, and cold reset through the facade. Pure C# over the facade/host
/// (no UWP types), so it also builds under the net10.0 fallback.
/// </summary>
public sealed class XboxLaunchSession : IXboxLaunchSession
{
    private readonly InProcessSessionFacade _facade;
    private readonly ConsoleHost _host;
    private readonly string _sessionId;

    /// <summary>Creates the launch session.</summary>
    /// <param name="facade">The in-process session facade (bound to <paramref name="sessionId"/>).</param>
    /// <param name="host">The console host (for the autostart service).</param>
    /// <param name="sessionId">The active session id.</param>
    public XboxLaunchSession(InProcessSessionFacade facade, ConsoleHost host, string sessionId)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _sessionId = string.IsNullOrEmpty(sessionId)
            ? throw new ArgumentException("Session id is required.", nameof(sessionId))
            : sessionId;
    }

    /// <inheritdoc />
    public async ValueTask<bool> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        byte[] payload,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        AttachMediaResponse response = await _facade
            .AttachMediaAsync(slot, filePath, isReadOnly, payload, displayName, cancellationToken)
            .ConfigureAwait(false);
        return response.Status.IsSuccess;
    }

    /// <inheritdoc />
    public async ValueTask AutostartDrive8Async(CancellationToken cancellationToken = default) =>
        await _host.HostService
            .ResetAndAutostartDrive8Async(new ResetAndAutostartDrive8Request(_sessionId), cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public ValueTask ColdResetAsync(CancellationToken cancellationToken = default) =>
        _facade.ColdResetAsync(_sessionId, cancellationToken);
}
#endif
