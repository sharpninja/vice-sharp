namespace ViceSharp.TestHarness;

using NSubstitute;
using ViceSharp.Abstractions;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001 (Limiter speed-cycle button).
/// The Settings Limiter panel gains a speed button next to the Warp toggle.
/// The button label always shows the speed a click will APPLY: "200%" only
/// when the limiter is enabled at exactly 100 percent, otherwise "100%"
/// (warp, fast-forward, or any non-100 rate). Clicking "100%" leaves warp,
/// re-enables live sound (rate 100 is under the live-audio ceiling), and
/// sets the limiter to 100; clicking "200%" moves to 200 leaving the SID
/// enabled (200 is the live-audio boundary, not past it). Dragging the
/// slider past 200 suspends the SID host-side and the label reads "100%".
/// </summary>
public sealed class SpeedCycleButtonTests
{
    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001, TEST: TEST-UI-SPEEDCYCLE-01.
    /// Use case: the label is derived from limiter state.
    /// Acceptance: enabled at 100 shows "200%"; warp shows "100%"; rates
    /// 150, 251, and 1000 show "100%"; returning to enabled 100 shows
    /// "200%" again and raises PropertyChanged for the label.
    /// </summary>
    [Fact]
    public void SpeedCycleLabel_Derives_From_Limiter_State()
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        Assert.Equal("200%", vm.SpeedCycleLabel);

        var raised = 0;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AttachPanelViewModel.SpeedCycleLabel))
                raised++;
        };

        vm.IsWarpMode = true;
        Assert.Equal("100%", vm.SpeedCycleLabel);
        vm.IsWarpMode = false;
        Assert.Equal("200%", vm.SpeedCycleLabel);

        vm.LimiterRatePercent = 150;
        Assert.Equal("100%", vm.SpeedCycleLabel);
        vm.LimiterRatePercent = 251;
        Assert.Equal("100%", vm.SpeedCycleLabel);
        vm.LimiterRatePercent = 100;
        Assert.Equal("200%", vm.SpeedCycleLabel);

        Assert.True(raised >= 4, $"SpeedCycleLabel change notifications: {raised}");
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001, TEST: TEST-UI-SPEEDCYCLE-02.
    /// Use case: clicking while the button reads "100%" (warp or
    /// fast-forward active) disables warp and sets the limiter to 100, which
    /// re-enables live sound host-side; the label flips to "200%".
    /// Acceptance: from warp at rate 500, CycleSpeedAsync yields limiter
    /// enabled, rate 100, label "200%".
    /// </summary>
    [Fact]
    public async Task CycleSpeed_From_Warp_Applies_100_And_Offers_200()
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient())
        {
            LimiterRatePercent = 500,
            IsWarpMode = true,
        };

        await vm.CycleSpeedAsync(TestContext.Current.CancellationToken);

        Assert.False(vm.IsWarpMode);
        Assert.True(vm.LimiterEnabled);
        Assert.Equal(100, vm.LimiterRatePercent);
        Assert.Equal("200%", vm.SpeedCycleLabel);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001, TEST: TEST-UI-SPEEDCYCLE-03.
    /// Use case: clicking while the button reads "200%" (limiter enabled at
    /// 100) moves the limiter to 200 leaving the SID enabled (200 is the
    /// live-audio boundary); the label flips back to "100%".
    /// Acceptance: from enabled 100, CycleSpeedAsync yields rate 200 with
    /// the limiter still enabled and label "100%".
    /// </summary>
    [Fact]
    public async Task CycleSpeed_At_100_Applies_200_Leaving_Sid_Enabled()
    {
        var vm = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        Assert.Equal(100, vm.LimiterRatePercent);
        Assert.True(vm.LimiterEnabled);

        await vm.CycleSpeedAsync(TestContext.Current.CancellationToken);

        Assert.True(vm.LimiterEnabled);
        Assert.Equal(200, vm.LimiterRatePercent);
        Assert.Equal("100%", vm.SpeedCycleLabel);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-AUDIO-SPEED-001, TEST: TEST-UI-SPEEDCYCLE-04.
    /// Use case: dragging the Limiter slider must apply the target speed LIVE
    /// (debounced through the rate-only limiter RPC), so the user actually
    /// reaches 50 percent (or any band value) without hunting for the Apply
    /// button - matching the live Warp and speed buttons. Rapid successive
    /// changes coalesce into one trailing call with the final value, and
    /// host-echoed refreshes (ApplyWarpMode) never re-trigger it.
    /// Acceptance: after a burst of slider values ending at 50, exactly one
    /// SetLimiterRateAsync(50) lands within the debounce window; an
    /// ApplyWarpMode echo triggers none.
    /// </summary>
    [Fact]
    public async Task Slider_Changes_Apply_Live_Debounced()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.SessionId.Returns(string.Empty);
        host.SetLimiterRateAsync(Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(new EmulatorCommandResponse(RpcStatus.Ok(), null));

        var vm = new AttachPanelViewModel(host);

        vm.LimiterRatePercent = 80;
        vm.LimiterRatePercent = 60;
        vm.LimiterRatePercent = 50;

        await Task.Delay(900, TestContext.Current.CancellationToken);

        await host.Received(1).SetLimiterRateAsync(50, Arg.Any<CancellationToken>());
        host.ClearReceivedCalls();

        vm.ApplyWarpMode(new WarpModeEvent(false, true, 120, 0));
        await Task.Delay(900, TestContext.Current.CancellationToken);

        await host.DidNotReceive().SetLimiterRateAsync(Arg.Any<double>(), Arg.Any<CancellationToken>());
    }
}
