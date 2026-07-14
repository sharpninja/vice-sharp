// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): the WinRT gamepad source. #if HAS_UWP-guarded in
// full (it references Windows.Gaming.Input, absent on the net10.0 fallback).
#if HAS_UWP
namespace ViceSharp.Xbox.Platform;

using System;
using Windows.Gaming.Input;
using Windows.System;
using ViceSharp.Xbox.Input;
using ConsoleHost = ViceSharp.Host.Runtime.ConsoleHost;
using ConsoleJoyPort = ViceSharp.Host.Runtime.ConsoleJoyPort;

/// <summary>
/// Polls gamepad 0 once per frame (~50 Hz), maps its <see cref="GamepadReading"/> field-for-
/// field into a <see cref="GamepadSnapshot"/> (the button bits cast straight through the
/// WinRT-identical <see cref="GamepadButtonFlags"/>), and feeds the single-consumer
/// <see cref="XboxInputContext"/>. The resolved joystick ports are pushed to the machine
/// through the swap-immune <see cref="ConsoleHost.SetJoystick"/>, and the discrete commands
/// are marshaled onto the session-locked host services by the <see cref="AppCommandDispatcher"/>.
/// </summary>
public sealed class WinRtGamepadSource
{
    private readonly ConsoleHost _host;
    private readonly XboxInputContext _context;
    private readonly AppCommandDispatcher _dispatcher;
    private readonly string _sessionId;
    private readonly DispatcherQueueTimer _timer;

    private long _frameIndex;
    private XboxInputConfig _config = XboxInputConfig.Default;

    // FIX-XFOCUSGATE-001 (operator: "When emulator screen is not the focused surface,
    // stop sending joystick input"): Windows.Gaming.Input reads the gamepad GLOBALLY,
    // so the pump self-gates on window activation.
    private bool _windowActive = true;

    /// <summary>Creates the gamepad source and its per-frame poll timer (not yet started).</summary>
    /// <param name="host">The in-process host that receives joystick state.</param>
    /// <param name="context">The single input-context authority.</param>
    /// <param name="dispatcher">The command dispatcher marshaling commands to host services.</param>
    /// <param name="sessionId">The active session id.</param>
    public WinRtGamepadSource(
        ConsoleHost host,
        XboxInputContext context,
        AppCommandDispatcher dispatcher,
        string sessionId)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        _sessionId = sessionId;

        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(XboxInputContext.FrameDurationMs);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => PollOnce();
    }

    /// <summary>Starts the per-frame poll.</summary>
    public void Start() => _timer.Start();

    /// <summary>Stops the per-frame poll.</summary>
    public void Stop() => _timer.Stop();

    /// <summary>
    /// Gates the pump on window activation (FIX-XFOCUSGATE-001): the gamepad API is
    /// global, so a backgrounded window would keep driving the C64. Deactivation pushes
    /// a ONE-SHOT neutral to both control ports (releasing any held direction/fire) and
    /// halts polling; reactivation resumes it (the context machine's prior snapshot
    /// survives, so a button still held across the gap does not re-edge).
    /// </summary>
    /// <param name="active"><c>true</c> when the app window is the focused surface.</param>
    public void SetWindowActive(bool active)
    {
        if (_windowActive == active)
            return;

        _windowActive = active;

        if (!active)
        {
            _host.SetJoystick(_sessionId, ConsoleJoyPort.Joystick1, JoystickPortState.Neutral.DirectionMask, JoystickPortState.Neutral.Fire);
            _host.SetJoystick(_sessionId, ConsoleJoyPort.Joystick2, JoystickPortState.Neutral.DirectionMask, JoystickPortState.Neutral.Fire);
        }
    }

    private void PollOnce()
    {
        if (!_windowActive)
            return;

        var gamepad = Gamepad.Gamepads.Count > 0 ? Gamepad.Gamepads[0] : null;
        var snapshot = gamepad is null ? GamepadSnapshot.Neutral : Read(gamepad);

        var resolution = _context.Tick(_frameIndex++, snapshot);

        // Push both control ports (explicit, swap-immune mapping).
        _host.SetJoystick(_sessionId, ConsoleJoyPort.Joystick1, resolution.Joy1.DirectionMask, resolution.Joy1.Fire);
        _host.SetJoystick(_sessionId, ConsoleJoyPort.Joystick2, resolution.Joy2.DirectionMask, resolution.Joy2.Fire);

        // Dispatch discrete commands off the poll thread (each host call locks internally).
        foreach (var command in resolution.Commands)
        {
            var result = _dispatcher.DispatchAsync(_sessionId, command).AsTask().GetAwaiter().GetResult();
            if (result.ToggleSwapPorts)
                _config = _config with { SwapPorts = !_config.SwapPorts };
        }
    }

    private static GamepadSnapshot Read(Gamepad gamepad)
    {
        var reading = gamepad.GetCurrentReading();
        return new GamepadSnapshot(
            reading.LeftThumbstickX,
            reading.LeftThumbstickY,
            reading.RightThumbstickX,
            reading.RightThumbstickY,
            reading.LeftTrigger,
            reading.RightTrigger,
            // The bits are WinRT-identical, so the cast is a straight reinterpret (no remap).
            (GamepadButtonFlags)reading.Buttons,
            reading.Timestamp);
    }
}
#endif
