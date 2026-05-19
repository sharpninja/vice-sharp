namespace ViceSharp.TestHarness;

using System;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Avalonia.Host;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Unit tests for <see cref="DisconnectedHostProtocolClient"/>, the
/// no-op <see cref="IHostProtocolClient"/> implementation used by the
/// Avalonia shell when no emulator host is connected. The disconnected
/// client must satisfy the same interface as the live gRPC client while
/// never throwing on lifecycle, settings, media, input, monitor, or
/// video methods; every response carries an Unavailable status. Tests
/// also verify the SessionId remains empty (no lazy bootstrap) and
/// that cancellation tokens are honoured before any synthetic response
/// is produced.
/// </summary>
public sealed class DisconnectedHostProtocolClientTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 DisconnectedHostProtocolClient).
    /// Use case: Before any host connection is established, the Avalonia
    /// shell binds against the disconnected client. The SessionId must
    /// be empty so UI elements that key off a session (status badge,
    /// command routing) recognise the unconnected state without
    /// special-casing null.
    /// Acceptance: SessionId is an empty string on a freshly-constructed
    /// disconnected client, regardless of any constructor message.
    /// </summary>
    [Fact]
    public void SessionId_IsEmptyString()
    {
        var client = new DisconnectedHostProtocolClient();
        var customMessageClient = new DisconnectedHostProtocolClient("Custom disconnected reason");

        Assert.Equal(string.Empty, client.SessionId);
        Assert.Equal(string.Empty, customMessageClient.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 DisconnectedHostProtocolClient).
    /// Use case: When the UI queries emulator status while no host is
    /// connected, the client must return a status indicating the host is
    /// unavailable so the UI can render a disconnected banner instead
    /// of treating the call as success or throwing.
    /// Acceptance: GetStatusAsync returns RpcStatusCode.Unavailable, the
    /// constructor message is propagated through the status message, and
    /// the EmulatorStatus payload is null.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_ReturnsUnavailableAndNullPayload()
    {
        const string Message = "Host offline for tests.";
        var client = new DisconnectedHostProtocolClient(Message);

        var response = await client.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Unavailable, response.Status.Code);
        Assert.Equal(Message, response.Status.Message);
        Assert.Null(response.EmulatorStatus);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 DisconnectedHostProtocolClient).
    /// Use case: Every lifecycle command (Start/Pause/Resume, step,
    /// rewind, reset, autostart, limiter rate) the UI can dispatch must
    /// return a uniform Unavailable status when no host is connected
    /// rather than throwing NotSupported or returning Ok. This lets the
    /// UI surface the disconnect once via the status code instead of
    /// per-command exception handling.
    /// Acceptance: Each lifecycle method returns RpcStatusCode.Unavailable
    /// with a null EmulatorStatus payload.
    /// </summary>
    [Fact]
    public async Task LifecycleCommands_ReturnUnavailableAndNullPayload()
    {
        var client = new DisconnectedHostProtocolClient();
        var ct = TestContext.Current.CancellationToken;

        var results = new[]
        {
            await client.StartAsync(ct),
            await client.PauseAsync(ct),
            await client.ResumeAsync(ct),
            await client.StepCycleAsync(1, ct),
            await client.StepFrameAsync(1, ct),
            await client.RewindCycleAsync(1, ct),
            await client.RewindFrameAsync(1, ct),
            await client.ColdResetAsync(ct),
            await client.WarmResetAsync(ct),
            await client.ResetAndAutostartDrive8Async(ct),
            await client.SetLimiterRateAsync(50.0, ct),
        };

        foreach (var response in results)
        {
            Assert.Equal(RpcStatusCode.Unavailable, response.Status.Code);
            Assert.Null(response.EmulatorStatus);
        }
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 DisconnectedHostProtocolClient).
    /// Use case: Settings panel queries (list profiles, get settings,
    /// update, validate) issued while disconnected must yield
    /// well-formed empty payloads so the panel can render its empty
    /// state without null-reference checks against the collection
    /// properties.
    /// Acceptance: ListSettingsProfilesAsync returns Unavailable with an
    /// empty Profiles collection; GetSettingsAsync returns Unavailable
    /// with null Settings; UpdateSettingsAsync returns Unavailable with
    /// null Settings and empty Diagnostics; ValidateSettingsResourcesAsync
    /// returns Unavailable with an empty Resources collection.
    /// </summary>
    [Fact]
    public async Task SettingsAccessors_ReturnUnavailableWithEmptyCollections()
    {
        var client = new DisconnectedHostProtocolClient();
        var ct = TestContext.Current.CancellationToken;
        var updateRequest = new UpdateSettingsRequest(SessionId: string.Empty);
        var validateRequest = new ValidateSettingsResourcesRequest(SessionId: string.Empty);

        var profiles = await client.ListSettingsProfilesAsync(ct);
        var settings = await client.GetSettingsAsync(ct);
        var update = await client.UpdateSettingsAsync(updateRequest, ct);
        var validate = await client.ValidateSettingsResourcesAsync(validateRequest, ct);

        Assert.Equal(RpcStatusCode.Unavailable, profiles.Status.Code);
        Assert.NotNull(profiles.Profiles);
        Assert.Empty(profiles.Profiles);

        Assert.Equal(RpcStatusCode.Unavailable, settings.Status.Code);
        Assert.Null(settings.Settings);

        Assert.Equal(RpcStatusCode.Unavailable, update.Status.Code);
        Assert.Null(update.Settings);
        Assert.NotNull(update.Diagnostics);
        Assert.Empty(update.Diagnostics);

        Assert.Equal(RpcStatusCode.Unavailable, validate.Status.Code);
        Assert.NotNull(validate.Resources);
        Assert.Empty(validate.Resources);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 DisconnectedHostProtocolClient).
    /// Use case: Media attach/detach commands the user may invoke before
    /// a host exists must short-circuit cleanly. ListMediaAsync should
    /// surface an empty Attachments list so the AttachPanel binds to a
    /// stable empty enumeration; AttachMediaAsync (both overloads) and
    /// DetachMediaAsync should report Unavailable so the slot
    /// view-models can render a disconnected state.
    /// Acceptance: ListMediaAsync returns Unavailable with an empty
    /// Attachments collection; both AttachMediaAsync overloads return
    /// Unavailable with null Attachment; DetachMediaAsync returns
    /// Unavailable with null Attachment.
    /// </summary>
    [Fact]
    public async Task MediaCommands_ReturnUnavailableAndEmptyOrNullPayloads()
    {
        var client = new DisconnectedHostProtocolClient();
        var ct = TestContext.Current.CancellationToken;

        var list = await client.ListMediaAsync(ct);
        var attachPath = await client.AttachMediaAsync(MediaSlot.Drive8, "image.d64", isReadOnly: true, ct);
        var attachPayload = await client.AttachMediaAsync(
            MediaSlot.Drive8,
            "image.d64",
            isReadOnly: true,
            payload: new byte[] { 0x01, 0x02 },
            displayName: "image.d64",
            cancellationToken: ct);
        var detach = await client.DetachMediaAsync(MediaSlot.Drive8, ct);

        Assert.Equal(RpcStatusCode.Unavailable, list.Status.Code);
        Assert.NotNull(list.Attachments);
        Assert.Empty(list.Attachments);

        Assert.Equal(RpcStatusCode.Unavailable, attachPath.Status.Code);
        Assert.Null(attachPath.Attachment);

        Assert.Equal(RpcStatusCode.Unavailable, attachPayload.Status.Code);
        Assert.Null(attachPayload.Attachment);

        Assert.Equal(RpcStatusCode.Unavailable, detach.Status.Code);
        Assert.Null(detach.Attachment);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 DisconnectedHostProtocolClient).
    /// Use case: Keyboard input plumbing (SetKeyStateAsync, list maps,
    /// set map) must accept calls even while disconnected so the UI
    /// keyboard handler can fire keystrokes without conditional logic
    /// to suppress them; the disconnected client absorbs them and
    /// reports Unavailable.
    /// Acceptance: SetKeyStateAsync, ListKeyboardMapsAsync, and
    /// SetKeyboardMapAsync each return RpcStatusCode.Unavailable with
    /// well-formed (null or empty) payloads.
    /// </summary>
    [Fact]
    public async Task InputCommands_ReturnUnavailable()
    {
        var client = new DisconnectedHostProtocolClient();
        var ct = TestContext.Current.CancellationToken;

        var setKey = await client.SetKeyStateAsync("Space", isPressed: true, cancellationToken: ct);
        var listMaps = await client.ListKeyboardMapsAsync(ct);
        var setMap = await client.SetKeyboardMapAsync("default", cancellationToken: ct);

        Assert.Equal(RpcStatusCode.Unavailable, setKey.Status.Code);
        Assert.Null(setKey.InputState);

        Assert.Equal(RpcStatusCode.Unavailable, listMaps.Status.Code);
        Assert.NotNull(listMaps.KeyboardMaps);
        Assert.Empty(listMaps.KeyboardMaps);

        Assert.Equal(RpcStatusCode.Unavailable, setMap.Status.Code);
        Assert.Null(setMap.KeyboardMap);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 DisconnectedHostProtocolClient).
    /// Use case: Monitor and video frame queries issued while
    /// disconnected (e.g. an opened debugger panel or a pending frame
    /// request) must return cleanly. The monitor response must include
    /// an empty Output string so the monitor console can render it
    /// without null guard; the video frame must surface null Frame so
    /// the surface compositor knows to show its disconnected
    /// placeholder.
    /// Acceptance: ExecuteMonitorCommandAsync returns Unavailable with
    /// an empty Output and null EmulatorStatus; GetFrameAsync returns
    /// Unavailable with a null Frame.
    /// </summary>
    [Fact]
    public async Task MonitorAndVideoCommands_ReturnUnavailable()
    {
        var client = new DisconnectedHostProtocolClient();
        var ct = TestContext.Current.CancellationToken;

        var monitor = await client.ExecuteMonitorCommandAsync("registers", ct);
        var frame = await client.GetFrameAsync(ct);

        Assert.Equal(RpcStatusCode.Unavailable, monitor.Status.Code);
        Assert.Equal(string.Empty, monitor.Output);
        Assert.Null(monitor.EmulatorStatus);

        Assert.Equal(RpcStatusCode.Unavailable, frame.Status.Code);
        Assert.Null(frame.Frame);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 DisconnectedHostProtocolClient).
    /// Use case: When the UI cancels an in-flight operation (panel
    /// closed, user navigated away), the disconnected client must
    /// honour the cancellation token rather than swallow it and return
    /// a synthetic Unavailable status. This keeps cancellation
    /// semantics identical between the disconnected and connected
    /// clients so consumers see no behavioural drift.
    /// Acceptance: Calling GetStatusAsync with an already-cancelled
    /// token throws OperationCanceledException. The same contract is
    /// validated on a representative lifecycle command
    /// (StartAsync) and a representative settings call
    /// (GetSettingsAsync).
    /// </summary>
    [Fact]
    public async Task Methods_ThrowOnCancelledToken()
    {
        var client = new DisconnectedHostProtocolClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await client.GetStatusAsync(cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await client.StartAsync(cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await client.GetSettingsAsync(cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await client.GetFrameAsync(cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 DisconnectedHostProtocolClient).
    /// Use case: The disconnected client is a pure value-style adapter
    /// with no transport, channel, file handle, or background thread to
    /// release. To make this contract explicit and prevent regressions
    /// that introduce hidden resources, the type must not implement
    /// IDisposable or IAsyncDisposable. This is the "Dispose is a
    /// no-op" requirement expressed as a stronger static invariant.
    /// Acceptance: DisconnectedHostProtocolClient does not implement
    /// IDisposable or IAsyncDisposable; instances can be created and
    /// abandoned without resource concerns.
    /// </summary>
    [Fact]
    public void Client_HasNoDisposableContract()
    {
        var client = new DisconnectedHostProtocolClient();

        Assert.IsNotType<IDisposable>(client, exactMatch: false);
        Assert.IsNotType<IAsyncDisposable>(client, exactMatch: false);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 DisconnectedHostProtocolClient).
    /// Use case: The disconnected client doubles as the safe fallback
    /// the UI binds against; it must satisfy the full
    /// IHostProtocolClient surface so the same view-models can swap in
    /// a live client without code-path changes. This protects against
    /// drift where a new interface method is added but the disconnected
    /// stub is forgotten.
    /// Acceptance: DisconnectedHostProtocolClient is assignable to
    /// IHostProtocolClient, and the parameter-less constructor produces
    /// an instance with the default "No emulator host is connected."
    /// status message.
    /// </summary>
    [Fact]
    public async Task DefaultConstructor_ImplementsInterfaceWithDefaultMessage()
    {
        IHostProtocolClient client = new DisconnectedHostProtocolClient();

        var status = await client.GetStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Unavailable, status.Status.Code);
        Assert.Equal("No emulator host is connected.", status.Status.Message);
        Assert.Equal(string.Empty, client.SessionId);
    }
}
