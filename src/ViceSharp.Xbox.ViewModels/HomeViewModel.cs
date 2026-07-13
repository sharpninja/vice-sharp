namespace ViceSharp.Xbox.ViewModels;

using System;

/// <summary>
/// PLAN-XBOXUWP S30 (IMPL-XBOXUWP-030), area XBOXUI. The launch / home page ViewModel:
/// the couch-UI entry point modeled as pure bindable state (<see cref="Title"/>,
/// <see cref="CanResume"/>) plus intent events the shell wires to the host (Start New,
/// Resume, Show About).
/// </summary>
/// <remarks>
/// <para>
/// The Home page is the first screen after boot. Its primary action starts a fresh
/// session (<see cref="StartNew"/>); when a prior session exists the shell flags it via
/// <see cref="SetCanResume(bool)"/> and the Resume affordance lights up
/// (<see cref="Resume"/> is gated on <see cref="CanResume"/>). It is also the entry into
/// the About / GPL disclosure page (<see cref="ShowAbout"/>).
/// </para>
/// <para>
/// This is a deliberately host-free model: it exposes intents as events rather than
/// calling the emulator session facade itself, so all Home logic is unit-testable off
/// console. A thin shell subscribes to the intent events and performs the actual host
/// calls (create / resume session, navigate to About). Pure MVVM (TR-MVVM-001): no
/// engine, host, or XAML reference.
/// </para>
/// </remarks>
public sealed class HomeViewModel
{
    private bool _canResume;

    /// <summary>Raised when <see cref="CanResume"/> changes value. Carries no payload.</summary>
    public event EventHandler? CanResumeChanged;

    /// <summary>Raised by <see cref="StartNew"/>: the shell should start a fresh session.</summary>
    public event EventHandler? StartNewRequested;

    /// <summary>
    /// Raised by <see cref="Resume"/> when <see cref="CanResume"/> is true: the shell
    /// should resume the prior session.
    /// </summary>
    public event EventHandler? ResumeRequested;

    /// <summary>Raised by <see cref="ShowAbout"/>: the shell should open the About page.</summary>
    public event EventHandler? ShowAboutRequested;

    /// <summary>The page title (the product name, <see cref="AboutInfo.ProjectName"/>).</summary>
    public string Title => AboutInfo.ProjectName;

    /// <summary>
    /// Whether a prior session exists that Resume can return to. False by default (first
    /// launch); set by <see cref="SetCanResume(bool)"/>.
    /// </summary>
    public bool CanResume => _canResume;

    /// <summary>
    /// Flags whether a resumable prior session exists. Raises
    /// <see cref="CanResumeChanged"/> only when the value actually changes.
    /// </summary>
    /// <param name="canResume">True when a prior session can be resumed.</param>
    public void SetCanResume(bool canResume)
    {
        if (_canResume == canResume)
        {
            return;
        }

        _canResume = canResume;
        CanResumeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raises the <see cref="StartNewRequested"/> intent.</summary>
    public void StartNew() => StartNewRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Raises the <see cref="ResumeRequested"/> intent when <see cref="CanResume"/> is
    /// true; a no-op otherwise (nothing to resume).
    /// </summary>
    /// <returns>
    /// <c>true</c> when the resume intent was raised; <c>false</c> when
    /// <see cref="CanResume"/> is false and nothing was raised.
    /// </returns>
    public bool Resume()
    {
        if (!_canResume)
        {
            return false;
        }

        ResumeRequested?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <summary>Raises the <see cref="ShowAboutRequested"/> intent.</summary>
    public void ShowAbout() => ShowAboutRequested?.Invoke(this, EventArgs.Empty);
}
