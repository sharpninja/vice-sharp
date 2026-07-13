// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): the seam adapter that wires the done ViewModels to
// the in-process host. #if HAS_UWP-guarded in full; it is compiled only on the real UWP
// build (the fallback never needs it, and the head's default entry point stays Program.cs).
#if HAS_UWP
namespace ViceSharp.Xbox.Platform;

using System;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Abstractions;
using ViceSharp.Protocol;
using ViceSharp.Xbox.ViewModels;
using ConsoleHost = ViceSharp.Host.Runtime.ConsoleHost;
using ConsoleJoyPort = ViceSharp.Host.Runtime.ConsoleJoyPort;

/// <summary>
/// Adapts the Kestrel-free in-process <see cref="ConsoleHost"/> to the three ViewModel
/// seams: <see cref="IEmulatorSessionFacade"/> (session lifecycle + the video-pull surface +
/// the machine input devices), <see cref="ILocalVideoFramePull"/> (the pure lock-free frame
/// copy), and <see cref="IXboxSettingsGateway"/> (settings / media / keyboard-map, narrowed
/// from the in-process protocol services). It is a thin pass-through: it owns no emulator
/// state and adds no locking (each host service locks its own session internally).
/// </summary>
public sealed class InProcessSessionFacade : IEmulatorSessionFacade, ILocalVideoFramePull, IXboxSettingsGateway
{
    private readonly ConsoleHost _host;
    private readonly string _sessionId;

    /// <summary>Creates the facade over a composed host and its active session.</summary>
    /// <param name="host">The in-process console host.</param>
    /// <param name="sessionId">The active session id all requests are addressed to.</param>
    public InProcessSessionFacade(ConsoleHost host, string sessionId)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        _sessionId = sessionId;
    }

    // ---- IEmulatorSessionFacade ---------------------------------------------

    /// <inheritdoc />
    public ILocalVideoFramePull VideoFrames => this;

    /// <inheritdoc />
    public ValueTask<string> CreateSessionAsync(CancellationToken ct = default)
        => ValueTask.FromResult(_sessionId);

    /// <inheritdoc />
    public ValueTask StartAsync(string sessionId, CancellationToken ct = default)
    {
        // The session is started at creation; Resume is the idempotent "run" entry.
        _host.Resume(sessionId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask PauseAsync(string sessionId, CancellationToken ct = default)
    {
        _host.Pause(sessionId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ResumeAsync(string sessionId, CancellationToken ct = default)
    {
        _host.Resume(sessionId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ColdResetAsync(string sessionId, CancellationToken ct = default)
    {
        _host.ResetCold(sessionId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask WarmResetAsync(string sessionId, CancellationToken ct = default)
    {
        _host.ResetWarm(sessionId);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IMachineJoystickInput? GetJoystickInput(string sessionId) => _host.GetJoystickInput(sessionId);

    /// <inheritdoc />
    public IMachineKeyboardInput? GetKeyboardInput(string sessionId) => _host.GetKeyboardInput(sessionId);

    // ---- ILocalVideoFramePull (pure sink) -----------------------------------

    /// <inheritdoc />
    public bool TryCopyFrameInto(string sessionId, Span<byte> destination, out int width, out int height, out long cycle)
        => _host.TryCopyLatestFrame(sessionId, destination, out width, out height, out cycle);

    /// <inheritdoc />
    public bool TryGetFrameGeometry(string sessionId, out FrameGeometry geometry)
    {
        if (_host.TryGetFrameGeometry(sessionId, out var host))
        {
            geometry = new FrameGeometry(host.Width, host.Height, host.BufferLength);
            return true;
        }

        geometry = default;
        return false;
    }

    // ---- IXboxSettingsGateway (settings / media / keyboard maps) -------------

    /// <inheritdoc />
    public ValueTask<GetSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
        => _host.Settings.GetSettingsAsync(new SessionRequest(_sessionId), cancellationToken);

    /// <inheritdoc />
    public ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default)
        => _host.Settings.UpdateSettingsAsync(request, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ValidateSettingsResourcesResponse> ValidateSettingsResourcesAsync(
        ValidateSettingsResourcesRequest request,
        CancellationToken cancellationToken = default)
        => _host.Settings.ValidateResourcesAsync(request, cancellationToken);

    /// <inheritdoc />
    public ValueTask<ListSettingsProfilesResponse> ListSettingsProfilesAsync(CancellationToken cancellationToken = default)
        => _host.Settings.ListProfilesAsync(new SessionRequest(_sessionId), cancellationToken);

    /// <inheritdoc />
    public ValueTask<ListMediaResponse> ListMediaAsync(CancellationToken cancellationToken = default)
        => _host.Media.ListMediaAsync(new SessionRequest(_sessionId), cancellationToken);

    /// <inheritdoc />
    public ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        CancellationToken cancellationToken = default)
        => _host.Media.AttachMediaAsync(
            new AttachMediaRequest(_sessionId, slot, filePath, DisplayName: "", IsReadOnly: isReadOnly),
            cancellationToken);

    /// <inheritdoc />
    public ValueTask<AttachMediaResponse> AttachMediaAsync(
        MediaSlot slot,
        string filePath,
        bool isReadOnly,
        byte[] payload,
        string displayName,
        CancellationToken cancellationToken = default)
        => _host.Media.AttachMediaAsync(
            new AttachMediaRequest(_sessionId, slot, filePath, displayName, isReadOnly, payload),
            cancellationToken);

    /// <inheritdoc />
    public ValueTask<DetachMediaResponse> DetachMediaAsync(
        MediaSlot slot,
        CancellationToken cancellationToken = default)
        => _host.Media.DetachMediaAsync(new DetachMediaRequest(_sessionId, slot), cancellationToken);

    /// <inheritdoc />
    public ValueTask<ListKeyboardMapsResponse> ListKeyboardMapsAsync(CancellationToken cancellationToken = default)
        => _host.InputService.ListKeyboardMapsAsync(new SessionRequest(_sessionId), cancellationToken);

    /// <inheritdoc />
    public ValueTask<KeyboardMapResponse> SetKeyboardMapAsync(
        string keyboardMapId,
        byte[]? payload = null,
        string displayName = "",
        string sourcePath = "",
        CancellationToken cancellationToken = default)
        => _host.InputService.SetKeyboardMapAsync(
            new SetKeyboardMapRequest(_sessionId, keyboardMapId, payload, displayName, sourcePath),
            cancellationToken);

    /// <inheritdoc />
    /// <remarks>
    /// True-drive is a machine-config choice: the in-process host rebuilds the session's rig
    /// (via C64TrueDriveRigBuilder) at creation from the request's true-drive flags. The
    /// narrow console surface does not yet expose a runtime rig-rebuild, so this is a
    /// best-effort no-op here; the full rebuild-and-re-attach wiring is completed on the
    /// equipped dev PC (Tier D). It never fabricates state, so callers stay consistent.
    /// </remarks>
    public ValueTask SetTrueDriveAsync(
        bool enabled,
        int driveDevice = 8,
        string? diskImagePath = null,
        int driveModel = (int)DriveModel.C1541,
        CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;
}
#endif
