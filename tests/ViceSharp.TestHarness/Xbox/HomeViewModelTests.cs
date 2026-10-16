namespace ViceSharp.TestHarness.Xbox;

using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S30 (IMPL-XBOXUWP-030). The launch / home page ViewModel
/// (<see cref="HomeViewModel"/>) in <c>ViceSharp.Xbox.ViewModels</c>: it models the
/// couch-UI entry point as pure bindable state plus intent events (Start New, Resume,
/// Show About), with a <see cref="HomeViewModel.SetCanResume(bool)"/> seam so the shell
/// can flag whether a prior session exists.
/// </summary>
/// <remarks>
/// Pure MVVM (TR-MVVM-001): no engine, host, or XAML reference. Resume is gated on
/// <see cref="HomeViewModel.CanResume"/>: with no prior session Resume raises nothing.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class HomeViewModelTests
{
    /// <summary>
    /// IMPL-XBOXUWP-030 default-state guard.
    /// Use case: on first launch there is no prior session, so the Resume affordance is
    /// disabled until the shell flags one.
    /// Acceptance: a fresh <see cref="HomeViewModel"/> reports
    /// <see cref="HomeViewModel.CanResume"/> == false and a non-empty
    /// <see cref="HomeViewModel.Title"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Default_HasNoPriorSession()
    {
        var vm = new HomeViewModel();

        Assert.False(vm.CanResume);
        Assert.False(string.IsNullOrWhiteSpace(vm.Title));
    }

    /// <summary>
    /// IMPL-XBOXUWP-030 CanResume-reflects-state guard.
    /// Use case: when the shell discovers a resumable session it flags it, and the Home
    /// page reflects that so the Resume affordance lights up (and clears again when the
    /// session is gone).
    /// Acceptance: <see cref="HomeViewModel.SetCanResume(bool)"/> drives
    /// <see cref="HomeViewModel.CanResume"/> true then false, raising
    /// <see cref="HomeViewModel.CanResumeChanged"/> on each real change.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void SetCanResume_ReflectsState_AndRaisesChange()
    {
        var vm = new HomeViewModel();
        int changes = 0;
        vm.CanResumeChanged += (_, _) => changes++;

        vm.SetCanResume(true);
        Assert.True(vm.CanResume);
        Assert.Equal(1, changes);

        // Idempotent: setting the same value raises nothing.
        vm.SetCanResume(true);
        Assert.Equal(1, changes);

        vm.SetCanResume(false);
        Assert.False(vm.CanResume);
        Assert.Equal(2, changes);
    }

    /// <summary>
    /// IMPL-XBOXUWP-030 start-new-intent guard.
    /// Use case: the primary Home action starts a fresh emulator session; the ViewModel
    /// exposes it as an intent event the shell wires to the host.
    /// Acceptance: <see cref="HomeViewModel.StartNew"/> raises
    /// <see cref="HomeViewModel.StartNewRequested"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void StartNew_RaisesStartNewIntent()
    {
        var vm = new HomeViewModel();
        int raised = 0;
        vm.StartNewRequested += (_, _) => raised++;

        vm.StartNew();

        Assert.Equal(1, raised);
    }

    /// <summary>
    /// IMPL-XBOXUWP-030 resume-intent-gated guard.
    /// Use case: Resume must only be reachable when a prior session exists; otherwise the
    /// action is a no-op (no intent, nothing for the shell to resume).
    /// Acceptance: with <see cref="HomeViewModel.CanResume"/> false,
    /// <see cref="HomeViewModel.Resume"/> returns false and raises nothing; after
    /// <see cref="HomeViewModel.SetCanResume(bool)"/> true it returns true and raises
    /// <see cref="HomeViewModel.ResumeRequested"/> exactly once.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Resume_IsGatedOnCanResume()
    {
        var vm = new HomeViewModel();
        int raised = 0;
        vm.ResumeRequested += (_, _) => raised++;

        Assert.False(vm.Resume());
        Assert.Equal(0, raised);

        vm.SetCanResume(true);
        Assert.True(vm.Resume());
        Assert.Equal(1, raised);
    }

    /// <summary>
    /// IMPL-XBOXUWP-030 about-entry guard.
    /// Use case: Home is the entry into the About / GPL disclosure page; the ViewModel
    /// exposes that navigation as an intent event.
    /// Acceptance: <see cref="HomeViewModel.ShowAbout"/> raises
    /// <see cref="HomeViewModel.ShowAboutRequested"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void ShowAbout_RaisesAboutIntent()
    {
        var vm = new HomeViewModel();
        int raised = 0;
        vm.ShowAboutRequested += (_, _) => raised++;

        vm.ShowAbout();

        Assert.Equal(1, raised);
    }
}
