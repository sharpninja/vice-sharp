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
    private readonly Action<string>? _log;

    // FEAT-XKBDJOYDETACH-001 hardening: the LIVE keyboard-open truth (Navigation.
    // IsVirtualKeyboardOpen), read every poll. The push-based SetKeyboardActive only arms the
    // instance the shell holds in its field; reading the shared truth here means EVERY pump -
    // including any stale one - detaches while the keyboard is up, so the C64 never sees a
    // gamepad stick behind an open keyboard regardless of which pump the shell armed.
    private readonly Func<bool>? _isKeyboardOpen;

    private long _frameIndex;
    private XboxInputConfig _config = XboxInputConfig.Default;

    // DIAG-XKBDJOYDETACH-001: one-line receipts for why the detach did/did not engage.
    private bool _loggedPollThread;
    private bool _lastLoggedKeyboardActive;
    private InputContext _lastLoggedContext = (InputContext)(-1);

    // FIX-XFOCUSGATE-001 (operator: "When emulator screen is not the focused surface,
    // stop sending joystick input"): Windows.Gaming.Input reads the gamepad GLOBALLY,
    // so the pump self-gates on window activation.
    private bool _windowActive = true;

    // FEAT-XKBDJOYDETACH-001 (operator: "When virtual keyboard is active, detach gamepad
    // from joysticks. Restore on closing"): while the on-screen keyboard is up the
    // stick/D-pad drive the keyboard, so the C64 joysticks are held neutral.
    private bool _keyboardActive;

    /// <summary>Creates the gamepad source and its per-frame poll timer (not yet started).</summary>
    /// <param name="host">The in-process host that receives joystick state.</param>
    /// <param name="context">The single input-context authority.</param>
    /// <param name="dispatcher">The command dispatcher marshaling commands to host services.</param>
    /// <param name="sessionId">The active session id.</param>
    public WinRtGamepadSource(
        ConsoleHost host,
        XboxInputContext context,
        AppCommandDispatcher dispatcher,
        string sessionId,
        Action<string>? log = null,
        Func<bool>? isKeyboardOpen = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        ArgumentException.ThrowIfNullOrEmpty(sessionId);
        _sessionId = sessionId;
        _log = log;
        _isKeyboardOpen = isKeyboardOpen;

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

    /// <summary>
    /// FEAT-XKBDJOYDETACH-001 (operator: "When virtual keyboard is active, detach gamepad
    /// from joysticks. Restore on closing virtual keyboard"): while the on-screen keyboard is
    /// up the stick/D-pad drive the keyboard, so both C64 joysticks are held NEUTRAL. Entry
    /// pushes a one-shot neutral to release any held direction/fire; closing lets the next
    /// poll drive the joysticks from gameplay again. The context still ticks (keyboard chords
    /// and navigation keep flowing) - only the joystick OUTPUT is detached.
    /// </summary>
    /// <param name="active"><c>true</c> while the virtual keyboard is open.</param>
    public void SetKeyboardActive(bool active)
    {
        _log?.Invoke($"SetKeyboardActive({active}) was={_keyboardActive} thread={Environment.CurrentManagedThreadId}");

        if (_keyboardActive == active)
            return;

        _keyboardActive = active;

        if (active)
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

        // FEAT-XKBDJOYDETACH-001: keep both joysticks detached (neutral) while the virtual
        // keyboard is up; otherwise push the resolved ports (explicit, swap-immune mapping).
        // The detach fires on EITHER the armed flag OR the live keyboard-open truth, so a pump
        // the shell never armed (a stale instance) still cannot leak a stick to the C64.
        bool keyboardOpen = _keyboardActive || (_isKeyboardOpen?.Invoke() ?? false);
        var joy1 = keyboardOpen ? JoystickPortState.Neutral : resolution.Joy1;
        var joy2 = keyboardOpen ? JoystickPortState.Neutral : resolution.Joy2;
        _host.SetJoystick(_sessionId, ConsoleJoyPort.Joystick1, joy1.DirectionMask, joy1.Fire);
        _host.SetJoystick(_sessionId, ConsoleJoyPort.Joystick2, joy2.DirectionMask, joy2.Fire);

        // DIAG-XKBDJOYDETACH-001: prove which thread polls and whether the detach is armed at
        // the moment the resolved ports go live (logged only on the first tick and on any
        // kbd/context change, so it is near-silent during steady state).
        if (_log is not null)
        {
            if (!_loggedPollThread)
            {
                _log($"poll thread={Environment.CurrentManagedThreadId}");
                _loggedPollThread = true;
            }

            InputContext ctx = _context.Context;
            if (_keyboardActive != _lastLoggedKeyboardActive || ctx != _lastLoggedContext)
            {
                bool nonNeutral = joy1.DirectionMask != 0 || joy1.Fire || joy2.DirectionMask != 0 || joy2.Fire;
                _log($"poll state: kbdActive={_keyboardActive} liveKbdOpen={keyboardOpen} ctx={ctx} pushNonNeutral={nonNeutral} thread={Environment.CurrentManagedThreadId}");
                _lastLoggedKeyboardActive = _keyboardActive;
                _lastLoggedContext = ctx;
            }

            // Smoking gun: the resolver produced a LIVE stick while the keyboard is open. It is
            // now forced neutral above, but this proves a pump was polling with the keyboard up
            // (the pre-fix leak) - if it ever logs with pushNonNeutral, the detach is bypassed.
            bool resolvedLive = resolution.Joy1.DirectionMask != 0 || resolution.Joy1.Fire
                || resolution.Joy2.DirectionMask != 0 || resolution.Joy2.Fire;
            if (resolvedLive && keyboardOpen)
            {
                _log($"detach caught a live stick while keyboard open: armed={_keyboardActive} ctx={_context.Context} pushedLive={joy1.DirectionMask != 0 || joy1.Fire || joy2.DirectionMask != 0 || joy2.Fire}");
            }
        }

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
