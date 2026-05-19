namespace ViceSharp.TestHarness;

using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Direct (in-process, no gRPC) unit tests for the
/// <see cref="InputServiceHost"/> RPC surface (all five public methods on
/// <see cref="IInputService"/>: SetKeyState / SetJoystickState /
/// GetInputState / ListKeyboardMaps / SetKeyboardMap) at the host-RPC
/// boundary. Complements <see cref="HostInputServiceTests"/>, which
/// already covers the runtime delegation paths (CIA1 keyboard matrix
/// scans, joystick CIA port wiring, composite shift handling, primary
/// joystick port routing). This suite focuses on the standard host-RPC
/// contracts that <see cref="HostInputServiceTests"/> does NOT cover:
/// missing-session resolution for every method, pre-cancelled token
/// observation, request-validation edge cases (empty key, joystick port
/// = Keyboard, missing keyboard-map id + payload, unknown keyboard-map
/// id), constructor null guard, and the read-only success paths for
/// GetInputState / ListKeyboardMaps. Uses
/// <see cref="MinimalHostArchitectureDescriptor"/> so the tests do not
/// require C64 ROM assets on disk.
/// </summary>
public sealed class InputServiceHostTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls SetKeyState with a session id the host
    /// runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status (carrying the unknown session id) and a null input state
    /// payload.
    /// </summary>
    [Fact]
    public async Task SetKeyStateAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var service = new InputServiceHost(registry);

        var response = await service.SetKeyStateAsync(
            new SetKeyStateRequest("ghost-session", "Space", true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.InputState);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls SetJoystickState with a session id the
    /// host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and a null input state payload.
    /// </summary>
    [Fact]
    public async Task SetJoystickStateAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var service = new InputServiceHost(registry);

        var response = await service.SetJoystickStateAsync(
            new SetJoystickStateRequest("ghost-session", InputPort.Joystick2, 0x01, true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.InputState);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls GetInputState with a session id the host
    /// runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and a null input state payload.
    /// </summary>
    [Fact]
    public async Task GetInputStateAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var service = new InputServiceHost(registry);

        var response = await service.GetInputStateAsync(
            new SessionRequest("ghost-session"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.InputState);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls ListKeyboardMaps with a session id the
    /// host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and an empty (but non-null) keyboard-maps collection so
    /// callers can iterate without a null check.
    /// </summary>
    [Fact]
    public async Task ListKeyboardMapsAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var service = new InputServiceHost(registry);

        var response = await service.ListKeyboardMapsAsync(
            new SessionRequest("ghost-session"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.NotNull(response.KeyboardMaps);
        Assert.Empty(response.KeyboardMaps);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls SetKeyboardMap with a session id the
    /// host runtime registry does not know about.
    /// Acceptance: The RPC returns the standard missing-session NotFound
    /// status and a null keyboard map payload.
    /// </summary>
    [Fact]
    public async Task SetKeyboardMapAsync_MissingSession_ReturnsMissingSessionStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var service = new InputServiceHost(registry);

        var response = await service.SetKeyboardMapAsync(
            new SetKeyboardMapRequest("ghost-session", "c64:gtk3_pos"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost-session", response.Status.Message);
        Assert.Null(response.KeyboardMap);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls SetKeyState against a real session but
    /// passes a blank/whitespace key.
    /// Acceptance: The host returns InvalidArgument (not Ok), no input
    /// state payload, and the message identifies the key field as
    /// required.
    /// </summary>
    [Fact]
    public async Task SetKeyStateAsync_BlankKey_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        var response = await service.SetKeyStateAsync(
            new SetKeyStateRequest(session.SessionId, "   ", true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("Key", response.Status.Message, System.StringComparison.OrdinalIgnoreCase);
        Assert.Null(response.InputState);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls SetJoystickState targeting
    /// <see cref="InputPort.Keyboard"/>, which is not a joystick port.
    /// Acceptance: The host returns InvalidArgument (not Ok), no input
    /// state payload, and the message identifies that the keyboard is
    /// not a joystick port.
    /// </summary>
    [Fact]
    public async Task SetJoystickStateAsync_KeyboardPort_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        var response = await service.SetJoystickStateAsync(
            new SetJoystickStateRequest(session.SessionId, InputPort.Keyboard, 0x01, true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("joystick", response.Status.Message, System.StringComparison.OrdinalIgnoreCase);
        Assert.Null(response.InputState);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls SetKeyboardMap on a valid session with
    /// both an empty/whitespace map id AND a null payload, so the host
    /// has nothing to identify the requested map.
    /// Acceptance: The host returns InvalidArgument and a null keyboard
    /// map; the message identifies the required fields.
    /// </summary>
    [Fact]
    public async Task SetKeyboardMapAsync_NoIdOrPayload_ReturnsInvalidArgument()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        var response = await service.SetKeyboardMapAsync(
            new SetKeyboardMapRequest(session.SessionId, "   "),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Contains("Payload", response.Status.Message);
        Assert.Null(response.KeyboardMap);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls SetKeyboardMap on a valid session with
    /// a keyboard-map id that does not match any enumerated builtin or
    /// fallback map.
    /// Acceptance: The host returns NotFound (not InvalidArgument), the
    /// returned keyboard map is null, and the message includes the
    /// unknown id so the caller can echo it.
    /// </summary>
    [Fact]
    public async Task SetKeyboardMapAsync_UnknownId_ReturnsNotFound()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        var response = await service.SetKeyboardMapAsync(
            new SetKeyboardMapRequest(session.SessionId, "c64:does-not-exist"),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("c64:does-not-exist", response.Status.Message);
        Assert.Null(response.KeyboardMap);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls GetInputState against a freshly
    /// registered minimal-architecture session that has never received
    /// any input mutations.
    /// Acceptance: Status is Ok, the returned <see cref="InputStateDto"/>
    /// is non-null, and the per-session key/joystick maps are empty
    /// (nothing has been pressed yet) while still being non-null
    /// collections.
    /// </summary>
    [Fact]
    public async Task GetInputStateAsync_FreshSession_ReturnsOkWithEmptyMaps()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        var response = await service.GetInputStateAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.InputState);
        Assert.NotNull(response.InputState.Keys);
        Assert.NotNull(response.InputState.Joysticks);
        Assert.Empty(response.InputState.Keys);
        Assert.Empty(response.InputState.Joysticks);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client calls ListKeyboardMaps against a valid
    /// session and expects at least one map (either enumerated from
    /// the VICE data directory or the embedded fallback when the
    /// directory is unavailable).
    /// Acceptance: Status is Ok and the returned keyboard-maps
    /// collection is non-empty; the C64 GTK3 positional map id is
    /// always present because it is the embedded fallback that the
    /// host yields when no .vkm files were enumerated.
    /// </summary>
    [Fact]
    public async Task ListKeyboardMapsAsync_ValidSession_ReturnsOkWithAtLeastOneMap()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        var response = await service.ListKeyboardMapsAsync(
            new SessionRequest(session.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.KeyboardMaps);
        Assert.NotEmpty(response.KeyboardMaps);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client cancels a SetKeyState RPC before the host
    /// has a chance to service it.
    /// Acceptance: Invoking SetKeyStateAsync with an already-cancelled
    /// <see cref="CancellationToken"/> throws
    /// <see cref="OperationCanceledException"/> before any session
    /// lookup or runtime mutation occurs.
    /// </summary>
    [Fact]
    public async Task SetKeyStateAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.SetKeyStateAsync(
                new SetKeyStateRequest(session.SessionId, "Space", true),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client cancels a SetJoystickState RPC before the
    /// host has a chance to service it.
    /// Acceptance: SetJoystickStateAsync with an already-cancelled
    /// token throws <see cref="OperationCanceledException"/> before
    /// any joystick mutation occurs.
    /// </summary>
    [Fact]
    public async Task SetJoystickStateAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.SetJoystickStateAsync(
                new SetJoystickStateRequest(session.SessionId, InputPort.Joystick2, 0x01, true),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client cancels a GetInputState RPC before the host
    /// has a chance to service it.
    /// Acceptance: GetInputStateAsync with an already-cancelled token
    /// throws <see cref="OperationCanceledException"/> before any
    /// session lookup or DTO projection occurs.
    /// </summary>
    [Fact]
    public async Task GetInputStateAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.GetInputStateAsync(
                new SessionRequest(session.SessionId),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client cancels a ListKeyboardMaps RPC before the
    /// host has a chance to service it.
    /// Acceptance: ListKeyboardMapsAsync with an already-cancelled
    /// token throws <see cref="OperationCanceledException"/> before
    /// any directory enumeration occurs.
    /// </summary>
    [Fact]
    public async Task ListKeyboardMapsAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.ListKeyboardMapsAsync(
                new SessionRequest(session.SessionId),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client cancels a SetKeyboardMap RPC before the host
    /// has a chance to service it.
    /// Acceptance: SetKeyboardMapAsync with an already-cancelled token
    /// throws <see cref="OperationCanceledException"/> before any
    /// keymap mutation occurs.
    /// </summary>
    [Fact]
    public async Task SetKeyboardMapAsync_CancelledToken_Throws()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateMinimalSession();
        registry.Add(session);
        var service = new InputServiceHost(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.SetKeyboardMapAsync(
                new SetKeyboardMapRequest(session.SessionId, "c64:gtk3_pos"),
                cts.Token));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 Input RPC).
    /// Use case: A client constructs an <see cref="InputServiceHost"/>
    /// without supplying a runtime registry (e.g. through a misconfigured
    /// DI container).
    /// Acceptance: The constructor throws
    /// <see cref="ArgumentNullException"/> immediately, surfacing the
    /// misconfiguration at host startup rather than at first RPC.
    /// </summary>
    [Fact]
    public void Constructor_NullRegistry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new InputServiceHost(null!));
    }

    private static EmulatorRuntimeSession CreateMinimalSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        return factory.Create(new CreateEmulatorSessionRequest());
    }
}
