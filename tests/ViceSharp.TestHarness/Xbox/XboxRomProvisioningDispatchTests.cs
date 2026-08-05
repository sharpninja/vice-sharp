namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Threading;
using ViceSharp.TestHarness.Xbox.Fakes;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP S40 (IMPL-XBOXUWP-040), area XROM. Guards the UI-thread dispatch of
/// <see cref="XboxRomProvisioningViewModel"/>. The verified download runs on a background thread
/// (the awaits keep <c>ConfigureAwait(false)</c>), so its <c>PropertyChanged</c> is raised off the
/// UI thread; without dispatch the XAML binding marshals it and throws <c>RPC_E_WRONG_THREAD</c>
/// (0x8001010E). The VM captures the <see cref="SynchronizationContext"/> at construction and posts
/// notifications back to it. These tests prove: raised off the captured context -&gt; dispatched;
/// raised on it (or none captured) -&gt; inline.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxRomProvisioningDispatchTests
{
    [Fact]
    public void PropertyChanged_RaisedOffCapturedContext_DispatchesToIt()
    {
        var context = new RecordingSyncContext();

        // Build ON the context (so it is captured), then leave it: the subsequent raise is off it.
        var vm = BuildOnContext(context);

        var raised = 0;
        vm.PropertyChanged += (_, _) => raised++;

        vm.ConfirmDownload(); // IsDownloadConfirmed false->true -> PropertyChanged, now off-context.

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
            var vm = new XboxRomProvisioningViewModel(
                new FakeRomAcquirer(),
                new FakeStoragePicker(null),
                new RomProvisionEvaluator(RomProvisionTestData.Catalog),
                NewTempPath(),
                RomProfile.Standard);

            var raised = 0;
            vm.PropertyChanged += (_, _) => raised++;

            vm.ConfirmDownload(); // still ON the captured context -> raise inline, no dispatch.

            Assert.Equal(0, context.PostCount);
            Assert.True(raised >= 1);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private static XboxRomProvisioningViewModel BuildOnContext(RecordingSyncContext context)
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            return new XboxRomProvisioningViewModel(
                new FakeRomAcquirer(),
                new FakeStoragePicker(null),
                new RomProvisionEvaluator(RomProvisionTestData.Catalog),
                NewTempPath(),
                RomProfile.Standard);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private static string NewTempPath() =>
        Path.Combine(Path.GetTempPath(), "vicesharp-dispatch-" + Guid.NewGuid().ToString("N"));
}
