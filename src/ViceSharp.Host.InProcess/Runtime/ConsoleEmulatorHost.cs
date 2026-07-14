using System.Globalization;
using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;

namespace ViceSharp.Host.Runtime;

// PLAN-XBOXUWP slice S3 (IMPL-XBOXUWP-003): the Kestrel-free in-process host facade
// for the UWP/Xbox head. It composes the existing POCO service graph
// (EmulatorHostService, InputServiceHost, LocalVideoFrameSource, EmulationPumpService)
// with `new` only - NO WebApplication, NO Kestrel, NO Grpc.AspNetCore - so the
// AppContainer partition never drags in the server stack (FR-INPROC-001, TR-INPROC-002).
// The live worker uses the AppContainer-safe XboxManagedFrameGate (S2). It also exposes a
// single-threaded, bit-exact deterministic stepper that NEVER starts the pump
// (TR-INPROC-003), and a per-frame input hook fired at the frame boundary (TR-INPROC-005).

/// <summary>
/// A physical control port on the head, mapped explicitly (swap-immune) to a C64
/// joystick control port. Unlike <see cref="InputPort.PrimaryJoystick"/>, these never
/// honour the session's SwapJoystickPorts setting.
/// </summary>
public enum ConsoleJoyPort
{
    /// <summary>C64 control port 1 (CIA1 port B, $DC01).</summary>
    Joystick1,

    /// <summary>C64 control port 2 (CIA1 port A, $DC00).</summary>
    Joystick2,
}

/// <summary>Options for creating an emulation session through the console host facade.</summary>
/// <param name="ArchitectureId">The architecture to instantiate (default <c>"c64"</c>).</param>
public sealed record ConsoleSessionOptions(string ArchitectureId = "c64");

/// <summary>Result of a session-creation request.</summary>
/// <param name="Success">True when the session was created.</param>
/// <param name="SessionId">The new session id, or empty on failure.</param>
/// <param name="Error">A human-readable failure reason, or null on success.</param>
public sealed record ConsoleSessionResult(bool Success, string SessionId, string? Error);

/// <summary>Presentable geometry of a session's video frame.</summary>
/// <param name="Width">Frame width in pixels.</param>
/// <param name="Height">Frame height in pixels.</param>
/// <param name="BufferLength">Length in bytes of the video chip's frame buffer.</param>
public readonly record struct FrameGeometry(int Width, int Height, int BufferLength);

/// <summary>
/// The console/Xbox head's emulator surface: session lifecycle, input, and a
/// lock-free frame pull. All methods are Kestrel-free and run in-process.
/// </summary>
public interface IConsoleEmulatorHost : IAsyncDisposable
{
    /// <summary>
    /// Per-frame input hook (TR-INPROC-005): invoked once per completed emulated frame
    /// with the session id and frame number, at the frame boundary on the emulation
    /// worker thread. Handlers must be short. Null by default.
    /// </summary>
    Action<string, long>? PerFrameInputHook { get; set; }

    /// <summary>Creates a session, starts it running, and lazily starts the shared emulation worker.</summary>
    ConsoleSessionResult StartC64Session(ConsoleSessionOptions? options = null);

    /// <summary>Pauses the emulation worker for a session (leaves it resident).</summary>
    void Pause(string sessionId);

    /// <summary>Resumes a paused session.</summary>
    void Resume(string sessionId);

    /// <summary>Warm-resets a session (reboots into a running machine).</summary>
    void ResetWarm(string sessionId);

    /// <summary>Cold-resets a session (reboots into a running machine).</summary>
    void ResetCold(string sessionId);

    /// <summary>Closes and removes a session.</summary>
    void CloseSession(string sessionId);

    /// <summary>
    /// Copies the latest committed frame into <paramref name="destination"/> with no
    /// allocation and no emulation lock. Returns false when the session is unknown, no
    /// frame has been committed, or the destination is too small.
    /// </summary>
    bool TryCopyLatestFrame(string sessionId, Span<byte> destination, out int width, out int height, out long cycle);

    /// <summary>
    /// Reports the session's video-frame geometry (width, height, buffer length).
    /// Returns false when the session is unknown or has no video chip.
    /// </summary>
    bool TryGetFrameGeometry(string sessionId, out FrameGeometry geometry);

    /// <summary>
    /// Sets a joystick control-port state. The port is mapped explicitly and is
    /// swap-immune (independent of the session's SwapJoystickPorts setting).
    /// </summary>
    void SetJoystick(string sessionId, ConsoleJoyPort port, byte directionMask, bool fireButton);

    /// <summary>Sets a keyboard key state on a session.</summary>
    void SetKey(string sessionId, string key, bool pressed);

    /// <summary>
    /// Sets the C64 RESTORE line state on a session. RESTORE is a hardware NMI wired
    /// directly to the CPU, not a key-matrix key: the virtual keyboard's RESTORE tile
    /// drives this dedicated seam, never <see cref="SetKey"/>. Routes to the session's
    /// <see cref="IMachineKeyboardInput.SetRestoreState(bool)"/> via the same device
    /// lookup as <see cref="SetKey"/>.
    /// </summary>
    void SetRestoreState(string sessionId, bool pressed);
}

/// <summary>
/// Single-threaded, bit-exact frame stepper (TR-INPROC-003). Sessions created here are
/// stepped under the session lock via <see cref="EmulatorHostService.StepFrameAsync"/>
/// and NEVER touch the emulation pump/gate worker, so replay is deterministic.
/// </summary>
public interface IConsoleDeterministicStepper
{
    /// <summary>
    /// Creates a session for deterministic stepping WITHOUT starting the emulation
    /// worker. The session is resident but inert until stepped.
    /// </summary>
    ConsoleSessionResult CreateDeterministicSession(ConsoleSessionOptions? options = null);

    /// <summary>
    /// Advances the session exactly <paramref name="frameCount"/> frames single-threaded
    /// (under the session lock, never via the pump) and returns the master clock's
    /// TotalCycles afterwards.
    /// </summary>
    long StepFramesDeterministic(string sessionId, int frameCount);
}

/// <summary>Dependencies for composing a <see cref="ConsoleHost"/>.</summary>
/// <param name="Descriptors">The architectures the host can instantiate.</param>
/// <param name="DefaultArchitectureId">The architecture used when a request omits one.</param>
/// <param name="RomProvider">Optional ROM provider (required for ROM-backed machines such as the C64).</param>
/// <param name="AudioBackend">Optional audio backend; null keeps the machine silent and timing-clean.</param>
public sealed record ConsoleHostDependencies(
    IReadOnlyList<IArchitectureDescriptor> Descriptors,
    string DefaultArchitectureId,
    IRomProvider? RomProvider = null,
    IAudioBackend? AudioBackend = null);

/// <summary>
/// Composition root for the in-process console/Xbox host. Wires the POCO service graph
/// with <c>new</c> - no DI container, no WebApplication, no Kestrel.
/// </summary>
public static class ConsoleHostComposition
{
    /// <summary>
    /// Builds a <see cref="ConsoleHost"/> from an explicit set of architectures. Used by
    /// the head (and tests) to inject a specific descriptor set (e.g. the ROM-independent
    /// minimal machine).
    /// </summary>
    public static ConsoleHost Build(ConsoleHostDependencies dependencies)
    {
        ArgumentNullException.ThrowIfNull(dependencies);

        var architectureBuilder = dependencies.RomProvider is not null
            ? new ArchitectureBuilder(dependencies.RomProvider, dependencies.AudioBackend)
            : new ArchitectureBuilder(dependencies.AudioBackend);

        var factory = new DefaultEmulatorRuntimeFactory(
            architectureBuilder,
            dependencies.Descriptors,
            dependencies.DefaultArchitectureId);

        return new ConsoleHost(factory);
    }

    /// <summary>
    /// Builds a <see cref="ConsoleHost"/> over the default runtime factory, which
    /// auto-detects installed C64 ROMs and selects the C64 as the default architecture
    /// when present (falling back to the minimal machine otherwise). This is the path
    /// the real head uses.
    /// </summary>
    public static ConsoleHost BuildDefault() => new(new DefaultEmulatorRuntimeFactory());
}

/// <summary>
/// The concrete in-process console/Xbox host facade (see <see cref="ConsoleHostComposition"/>).
/// </summary>
public sealed class ConsoleHost : IConsoleEmulatorHost, IConsoleDeterministicStepper
{
    private readonly EmulatorRuntimeRegistry _registry;
    private readonly EmulatorHostService _hostService;
    private readonly InputServiceHost _input;
    private readonly LocalVideoFrameSource _video;
    private readonly SettingsServiceHost _settings;
    private readonly MediaServiceHost _media;
    private readonly SnapshotServiceHost _snapshots;
    private readonly EmulationPumpService _pump;
    private readonly object _pumpGate = new();
    private bool _pumpStarted;

    internal ConsoleHost(IEmulatorRuntimeFactory runtimeFactory)
    {
        ArgumentNullException.ThrowIfNull(runtimeFactory);

        _registry = new EmulatorRuntimeRegistry();
        _hostService = new EmulatorHostService(_registry, runtimeFactory);
        _input = new InputServiceHost(_registry);
        _video = new LocalVideoFrameSource(_registry);
        // AppContainer-safe managed gate (S2): no OS timer, no aux thread, no P/Invoke.
        _pump = new EmulationPumpService(_registry, new XboxManagedFrameGate());
        // PLAN-XBOXUWP S34: the POCO settings/media/snapshot services over the same
        // registry, so the UWP head's seam adapter can drive IXboxSettingsGateway and the
        // AppCommandDispatcher without re-composing the graph. All Kestrel-free.
        _settings = new SettingsServiceHost(_registry, runtimeFactory, _pump);
        _media = new MediaServiceHost(_registry);
        _snapshots = new SnapshotServiceHost(_registry);
        // Bridge the pump's per-frame boundary to the facade hook, wired once at
        // composition so it fires on both the live worker and synchronous PumpSession.
        _pump.FramePumped = (session, frame) => PerFrameInputHook?.Invoke(session.SessionId, frame);
    }

    /// <inheritdoc />
    public Action<string, long>? PerFrameInputHook { get; set; }

    /// <summary>
    /// The shared emulation worker/pacing pump. Exposed so the head can control its
    /// lifecycle and so diagnostics can read the applied worker-affinity mask.
    /// </summary>
    public EmulationPumpService Pump => _pump;

    /// <summary>The session registry backing this host (composition detail; exposed for the host head and tests).</summary>
    internal EmulatorRuntimeRegistry Registry => _registry;

    /// <summary>
    /// The emulator lifecycle/limiter/autostart service (PLAN-XBOXUWP S34). The UWP head's
    /// <c>AppCommandDispatcher</c> routes resets, autostart, and warp through this.
    /// </summary>
    public IEmulatorHost HostService => _hostService;

    /// <summary>
    /// The input service (key/joystick state, keyboard-map list/select) (PLAN-XBOXUWP S34).
    /// The head's settings gateway routes keyboard-map queries through this.
    /// </summary>
    public IInputService InputService => _input;

    /// <summary>The session settings service (get/update/validate/profiles) (PLAN-XBOXUWP S34).</summary>
    public ISettingsService Settings => _settings;

    /// <summary>The media service (attach/detach/list) (PLAN-XBOXUWP S34).</summary>
    public IMediaService Media => _media;

    /// <summary>The snapshot service (quick save/load) (PLAN-XBOXUWP S34).</summary>
    public ISnapshotService Snapshots => _snapshots;

    /// <summary>
    /// The machine-owned keyboard-input surface for a session, or <c>null</c> when the
    /// session is unknown or has no keyboard device (PLAN-XBOXUWP S34). Mirrors the device
    /// lookup <see cref="InputServiceHost"/> uses; drives the virtual-keyboard ViewModel.
    /// </summary>
    public IMachineKeyboardInput? GetKeyboardInput(string sessionId)
        => _registry.TryGet(sessionId, out var session)
            ? session.Machine.Devices.All.OfType<IMachineKeyboardInput>().FirstOrDefault()
            : null;

    /// <summary>
    /// The machine-owned joystick/control-port surface for a session, or <c>null</c> when the
    /// session is unknown or has no joystick device (PLAN-XBOXUWP S34).
    /// </summary>
    public IMachineJoystickInput? GetJoystickInput(string sessionId)
        => _registry.TryGet(sessionId, out var session)
            ? session.Machine.Devices.All.OfType<IMachineJoystickInput>().FirstOrDefault()
            : null;

    /// <summary>
    /// The live architecture's video standard (PAL/NTSC) for a session, or <c>null</c> when the
    /// session is unknown (FIX-XASPECT-001). Drives the head's TRUE composite pixel-aspect
    /// display: the aspect must track the ACTIVE session, including a model-change rebuild
    /// under the same session id.
    /// </summary>
    /// <param name="sessionId">The session whose video standard is requested.</param>
    /// <returns>The architecture's <see cref="VideoStandard"/>, or <c>null</c>.</returns>
    public VideoStandard? GetVideoStandard(string sessionId)
        => _registry.TryGet(sessionId, out var session)
            ? session.Architecture.VideoStandard
            : null;

    /// <summary>
    /// The live machine profile's nominal clock in Hz for a session, or <c>null</c> when the
    /// session is unknown or its architecture carries no profile (FEAT-XPERFHUD-001). Drives
    /// the head's performance-HUD speed-percent line (measured cycle rate vs nominal).
    /// </summary>
    /// <param name="sessionId">The session whose nominal clock is requested.</param>
    /// <returns>The profile's nominal clock in Hz, or <c>null</c>.</returns>
    public double? GetMachineClockHz(string sessionId)
        => _registry.TryGet(sessionId, out var session)
            && session.Architecture is IProfiledArchitectureDescriptor profiled
                ? profiled.MachineProfile.NominalClockHz
                : null;

    /// <summary>
    /// The number of frame rows the session's video standard actually writes into the fixed
    /// VIC frame buffer (FIX-XNTSCFILL-001: NTSC 246 of 272; PAL the full 272), or <c>null</c>
    /// when the session is unknown or has no VIC-II video chip. Displays crop to this height
    /// so NTSC content fills the screen instead of carrying its in-frame black band.
    /// </summary>
    /// <param name="sessionId">The session whose frame content height is requested.</param>
    /// <returns>The written content rows, or <c>null</c>.</returns>
    public int? GetFrameContentHeight(string sessionId)
        => _registry.TryGet(sessionId, out var session)
            && session.Machine.Devices.GetByRole(DeviceRole.VideoChip) is Chips.VicIi.Mos6569 vic
                ? Chips.VicIi.VideoRenderer.GetContentLines(vic.VisibleLines)
                : null;

    /// <inheritdoc />
    public ConsoleSessionResult StartC64Session(ConsoleSessionOptions? options = null)
    {
        var create = CreateSessionCore(options);
        if (!create.Success)
            return create;

        // Start the session running, then lazily start the shared worker.
        _hostService.StartAsync(new SessionRequest(create.SessionId)).GetAwaiter().GetResult();
        EnsurePumpStarted();
        return create;
    }

    /// <inheritdoc />
    public ConsoleSessionResult CreateDeterministicSession(ConsoleSessionOptions? options = null)
        // Deliberately does NOT start (or leave running) the pump: deterministic
        // stepping is single-threaded via StepFrameAsync under the session lock.
        => CreateSessionCore(options);

    /// <inheritdoc />
    public long StepFramesDeterministic(string sessionId, int frameCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(frameCount, 0);

        var response = _hostService
            .StepFrameAsync(new StepFrameRequest(sessionId, frameCount))
            .GetAwaiter()
            .GetResult();

        if (response.Status.Code != RpcStatusCode.Ok)
            throw new InvalidOperationException($"StepFrame failed for session '{sessionId}': {response.Status.Message}");

        if (!_registry.TryGet(sessionId, out var session))
            throw new InvalidOperationException($"Session '{sessionId}' was not found.");

        lock (session.SyncRoot)
            return session.Machine.Clock.TotalCycles;
    }

    /// <summary>
    /// Returns a deterministic, stable fingerprint of the session's machine state
    /// (CPU registers + master cycle) as a hex string, for bit-exact replay assertions.
    /// </summary>
    public string GetDeterministicStateHash(string sessionId)
    {
        if (!_registry.TryGet(sessionId, out var session))
            return string.Empty;

        MachineState state;
        lock (session.SyncRoot)
            state = session.Machine.GetState();

        // FNV-1a over the stable state fields; deterministic and allocation-light.
        var hash = 1469598103934665603UL;
        hash = Fnv1a(hash, state.A);
        hash = Fnv1a(hash, state.X);
        hash = Fnv1a(hash, state.Y);
        hash = Fnv1a(hash, state.S);
        hash = Fnv1a(hash, state.P);
        hash = Fnv1a(hash, (byte)(state.PC & 0xFF));
        hash = Fnv1a(hash, (byte)(state.PC >> 8));
        var cycle = (ulong)state.Cycle;
        for (var i = 0; i < 8; i++)
            hash = Fnv1a(hash, (byte)(cycle >> (i * 8)));

        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public void Pause(string sessionId)
        => _hostService.PauseAsync(new SessionRequest(sessionId)).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void Resume(string sessionId)
        => _hostService.ResumeAsync(new SessionRequest(sessionId)).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void ResetWarm(string sessionId)
        => _hostService.WarmResetAsync(new SessionRequest(sessionId)).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void ResetCold(string sessionId)
        => _hostService.ColdResetAsync(new SessionRequest(sessionId)).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void CloseSession(string sessionId)
        => _hostService.CloseSessionAsync(new SessionRequest(sessionId)).GetAwaiter().GetResult();

    /// <inheritdoc />
    public bool TryCopyLatestFrame(string sessionId, Span<byte> destination, out int width, out int height, out long cycle)
        => _video.TryCopyFrameInto(sessionId, destination, out width, out height, out cycle);

    /// <inheritdoc />
    public bool TryGetFrameGeometry(string sessionId, out FrameGeometry geometry)
    {
        geometry = default;
        if (!_registry.TryGet(sessionId, out var session))
            return false;

        if (session.Machine.Devices.GetByRole(DeviceRole.VideoChip) is not IVideoChip video)
            return false;

        lock (session.SyncRoot)
            geometry = new FrameGeometry(video.FrameWidth, video.FrameHeight, video.FrameBuffer.Length);

        return true;
    }

    /// <inheritdoc />
    public void SetJoystick(string sessionId, ConsoleJoyPort port, byte directionMask, bool fireButton)
    {
        // Explicit, swap-immune mapping: ConsoleJoyPort -> InputPort.Joystick{1,2}
        // (never PrimaryJoystick, which is the only port that honours SwapJoystickPorts).
        var inputPort = port == ConsoleJoyPort.Joystick1 ? InputPort.Joystick1 : InputPort.Joystick2;
        _input.SetJoystickStateAsync(new SetJoystickStateRequest(sessionId, inputPort, directionMask, fireButton))
            .GetAwaiter()
            .GetResult();
    }

    /// <inheritdoc />
    public void SetKey(string sessionId, string key, bool pressed)
        => _input.SetKeyStateAsync(new SetKeyStateRequest(sessionId, key, pressed)).GetAwaiter().GetResult();

    /// <inheritdoc />
    public void SetRestoreState(string sessionId, bool pressed)
        => _input.SetRestoreStateAsync(sessionId, pressed).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_pumpStarted)
            await _pump.StopAsync(CancellationToken.None).ConfigureAwait(false);
        _pump.Dispose();
    }

    private ConsoleSessionResult CreateSessionCore(ConsoleSessionOptions? options)
    {
        var architectureId = (options ?? new ConsoleSessionOptions()).ArchitectureId;
        var response = _hostService
            .CreateSessionAsync(new CreateEmulatorSessionRequest(architectureId))
            .GetAwaiter()
            .GetResult();

        return response.Status.Code == RpcStatusCode.Ok
            ? new ConsoleSessionResult(true, response.SessionId, null)
            : new ConsoleSessionResult(false, string.Empty, response.Status.Message);
    }

    private void EnsurePumpStarted()
    {
        if (_pumpStarted)
            return;

        lock (_pumpGate)
        {
            if (_pumpStarted)
                return;

            _pump.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            _pumpStarted = true;
        }
    }

    private static ulong Fnv1a(ulong hash, byte value)
    {
        hash ^= value;
        return hash * 1099511628211UL;
    }
}
