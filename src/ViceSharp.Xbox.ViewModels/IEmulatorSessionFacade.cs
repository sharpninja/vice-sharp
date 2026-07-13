namespace ViceSharp.Xbox.ViewModels;

using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Abstractions;

/// <summary>
/// PLAN-XBOXUWP S21 (IMPL-XBOXUWP-021), area XBOXUI. The emulator-session host-facade
/// seam OWNED by the 10-foot ViewModels: session lifecycle, the lock-free video-pull
/// surface, and the machine-owned joystick/keyboard input surfaces.
/// </summary>
/// <remarks>
/// <para>
/// The real head implements this by adapting the in-process
/// <c>IConsoleEmulatorHost</c> facade (validated on the dev-PC / console slices),
/// while the off-console ViewModel tests bind it to a fake. Keeping the seam in the
/// ViewModels project (Abstractions + Protocol only) is what makes the 10-foot UI
/// unit-testable without the engine, the host composition, or the console
/// (TR-MVVM-001).
/// </para>
/// <para>
/// The input surfaces are the NARROW Abstractions contracts
/// (<see cref="IMachineJoystickInput"/> / <see cref="IMachineKeyboardInput"/>), never
/// a broad machine handle: the ViewModels only need to inject control-port and key
/// state. Both return <c>null</c> when the session has no such device.
/// </para>
/// </remarks>
public interface IEmulatorSessionFacade
{
    /// <summary>
    /// Creates a new single-machine console session and returns its id.
    /// </summary>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The newly created session's id.</returns>
    ValueTask<string> CreateSessionAsync(CancellationToken ct = default);

    /// <summary>Starts (runs) the given session.</summary>
    /// <param name="sessionId">The session to start.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    ValueTask StartAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Pauses the given session, halting emulation without tearing it down.</summary>
    /// <param name="sessionId">The session to pause.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    ValueTask PauseAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Resumes a previously paused session.</summary>
    /// <param name="sessionId">The session to resume.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    ValueTask ResumeAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Cold-resets (power-cycles) the given session.</summary>
    /// <param name="sessionId">The session to cold-reset.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    ValueTask ColdResetAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Warm-resets the given session.</summary>
    /// <param name="sessionId">The session to warm-reset.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    ValueTask WarmResetAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// The pure, read-only, lock-free video-pull surface for the app-shell render
    /// timer. Constructed once and shared across navigation (never torn down by
    /// opening a page or overlay).
    /// </summary>
    ILocalVideoFramePull VideoFrames { get; }

    /// <summary>
    /// The machine-owned joystick/control-port input surface for the given session,
    /// or <c>null</c> when the session has no joystick input device.
    /// </summary>
    /// <param name="sessionId">The session whose joystick input is requested.</param>
    /// <returns>The joystick input surface, or <c>null</c>.</returns>
    IMachineJoystickInput? GetJoystickInput(string sessionId);

    /// <summary>
    /// The machine-owned keyboard input surface for the given session, or <c>null</c>
    /// when the session has no keyboard input device.
    /// </summary>
    /// <param name="sessionId">The session whose keyboard input is requested.</param>
    /// <returns>The keyboard input surface, or <c>null</c>.</returns>
    IMachineKeyboardInput? GetKeyboardInput(string sessionId);
}
