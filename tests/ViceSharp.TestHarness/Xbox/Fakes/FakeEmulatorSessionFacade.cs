namespace ViceSharp.TestHarness.Xbox.Fakes;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Abstractions;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S21 (IMPL-XBOXUWP-021). Off-console test double for
/// <see cref="IEmulatorSessionFacade"/>. Mints a session id on
/// <see cref="CreateSessionAsync"/>, records every lifecycle call (in order) on
/// <see cref="Calls"/>, and exposes a <see cref="FakeVideoFramePull"/> plus canned
/// joystick/keyboard input doubles so the 10-foot ViewModels can be unit-tested
/// against a fake host head.
/// </summary>
public sealed class FakeEmulatorSessionFacade : IEmulatorSessionFacade
{
    private int _sessionCounter;

    /// <summary>
    /// Ordered log of lifecycle calls, one entry per call, formatted
    /// <c>"{Operation}:{sessionId}"</c> (e.g. <c>"Create:fake-session-1"</c>,
    /// <c>"Start:fake-session-1"</c>). Lets a test assert both the set of calls and
    /// their exact order.
    /// </summary>
    public List<string> Calls { get; } = new();

    /// <summary>The canned lock-free frame pull returned by <see cref="VideoFrames"/>.</summary>
    public FakeVideoFramePull Frames { get; } = new();

    /// <summary>The canned joystick input double returned by <see cref="GetJoystickInput"/>.</summary>
    public FakeMachineJoystickInput Joystick { get; } = new();

    /// <summary>The canned keyboard input double returned by <see cref="GetKeyboardInput"/>.</summary>
    public FakeMachineKeyboardInput Keyboard { get; } = new();

    /// <summary>The most recently minted session id (null before <see cref="CreateSessionAsync"/>).</summary>
    public string? LastSessionId { get; private set; }

    /// <inheritdoc />
    public ILocalVideoFramePull VideoFrames => Frames;

    /// <inheritdoc />
    public ValueTask<string> CreateSessionAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var id = $"fake-session-{Interlocked.Increment(ref _sessionCounter)}";
        LastSessionId = id;
        Calls.Add($"Create:{id}");
        return ValueTask.FromResult(id);
    }

    /// <inheritdoc />
    public ValueTask StartAsync(string sessionId, CancellationToken ct = default) => Record("Start", sessionId, ct);

    /// <inheritdoc />
    public ValueTask PauseAsync(string sessionId, CancellationToken ct = default) => Record("Pause", sessionId, ct);

    /// <inheritdoc />
    public ValueTask ResumeAsync(string sessionId, CancellationToken ct = default) => Record("Resume", sessionId, ct);

    /// <inheritdoc />
    public ValueTask ColdResetAsync(string sessionId, CancellationToken ct = default) => Record("ColdReset", sessionId, ct);

    /// <inheritdoc />
    public ValueTask WarmResetAsync(string sessionId, CancellationToken ct = default) => Record("WarmReset", sessionId, ct);

    /// <inheritdoc />
    public IMachineJoystickInput? GetJoystickInput(string sessionId) => Joystick;

    /// <inheritdoc />
    public IMachineKeyboardInput? GetKeyboardInput(string sessionId) => Keyboard;

    private ValueTask Record(string operation, string sessionId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        Calls.Add($"{operation}:{sessionId}");
        return ValueTask.CompletedTask;
    }
}
