namespace ViceSharp.TestHarness.Xbox;

using System.Threading;
using ViceSharp.TestHarness.Xbox.Fakes;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// FIX-XSETBLANK-001 (operator 2026-07-14: "Settings broken" / "Setting still broken").
/// The on-device receipt proved the ViewModel was FULLY populated (computers=5,
/// models=14, selections + volume all correct in the refresh log) while every picker on
/// screen stayed BLANK: <see cref="XboxSettingsViewModel"/> raised
/// <c>PropertyChanged</c> on whatever thread ran the refresh continuations, and
/// off-UI-thread notifications never update UWP bindings. Same defect class (and same
/// fix) as <see cref="XboxRomProvisioningViewModel"/>: capture the
/// <see cref="SynchronizationContext"/> at construction and POST notifications to it.
/// </summary>
/// <remarks>
/// FR: FR-XBOXUI-004 (settings page binds the canonical host settings). TR:
/// TR-MVVM-001. Use case: the Settings page refreshes on navigation; the adopted values
/// must reach the XAML bindings regardless of which thread the gateway continuations
/// resumed on. Acceptance: PropertyChanged raised OFF the captured context dispatches
/// (Post) to it and still reaches subscribers; raised ON the captured context (or with
/// none captured) it raises inline with no dispatch.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class XboxSettingsDispatchTests
{
    private const string SessionId = "session-dispatch";

    [Fact]
    public void PropertyChanged_RaisedOffCapturedContext_DispatchesToIt()
    {
        var context = new RecordingSyncContext();

        // Build ON the context (so it is captured), then leave it: the raise is off it.
        var vm = BuildOnContext(context);

        var raised = 0;
        vm.PropertyChanged += (_, _) => raised++;

        vm.SelectedRenderer = "changed-off-context";

        Assert.True(
            context.PostCount >= 1,
            "PropertyChanged raised off the captured context must be dispatched (Post) to it.");
        Assert.True(raised >= 1, "The dispatched notification must still reach subscribers.");
    }

    [Fact]
    public void PropertyChanged_RaisedOnCapturedContext_RaisesInline()
    {
        var context = new RecordingSyncContext();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var vm = new XboxSettingsViewModel(new FakeXboxSettingsGateway(), SessionId);

            var raised = 0;
            vm.PropertyChanged += (_, _) => raised++;

            vm.SelectedRenderer = "changed-on-context";

            Assert.Equal(0, context.PostCount);
            Assert.True(raised >= 1);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private static XboxSettingsViewModel BuildOnContext(RecordingSyncContext context)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            return new XboxSettingsViewModel(new FakeXboxSettingsGateway(), SessionId);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
