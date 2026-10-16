namespace ViceSharp.TestHarness.Xbox;

using System;
using System.IO;
using System.Threading.Tasks;
using ViceSharp.TestHarness.Xbox.Fakes;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. TEST-XSET-001 off-console coverage for
/// <see cref="XboxRomProvisioningViewModel"/>: the first-run provisioning ViewModel that
/// evaluates ROM state, imports a picked file (validated by the 64MB ceiling + size + MD5),
/// and runs the confirm-gated verified download. Every test drives the fake
/// <see cref="IRomAcquirer"/> / <see cref="IStoragePicker"/> seams and the synthetic
/// <see cref="RomProvisionTestData.Catalog"/>, so the suite needs no engine, host, console,
/// network, or real ROM data (TR-MVVM-001, TR-XPATH-001). Plain <see cref="FactAttribute"/>.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxRomProvisioningViewModelTests
{
    private static string NewTempC64Directory() =>
        Path.Combine(Path.GetTempPath(), "vicesharp-xbox-rom-" + Guid.NewGuid().ToString("N"));

    private static XboxRomProvisioningViewModel CreateViewModel(
        string directory,
        IRomAcquirer acquirer,
        IStoragePicker picker,
        RomProfile profile = RomProfile.Standard) =>
        new XboxRomProvisioningViewModel(
            acquirer,
            picker,
            new RomProvisionEvaluator(RomProvisionTestData.Catalog),
            directory,
            profile);

    /// <summary>
    /// FR-XROM-002, TR-XPATH-001. TEST-XSET-001.
    /// Use case: a storage import of a file above the 64MB ceiling must be refused before
    /// its bytes are read, leaving provisioning state untouched.
    /// Acceptance: importing an oversize picked file does not change <c>State</c> /
    /// <c>IsBootBlocked</c>, writes no ROM file, and never reads the file's bytes.
    /// </summary>
    [Fact]
    public async Task ImportAsync_OversizeFile_RejectedStateUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var directory = NewTempC64Directory();
        try
        {
            var picker = new FakeStoragePicker(RomProvisionTestData.PickedOversize(RomRole.Kernal));
            var vm = CreateViewModel(directory, new FakeRomAcquirer(), picker);
            await vm.RefreshAsync(ct);

            Assert.Equal(RomProvisionState.NotProvisioned, vm.State);

            await vm.ImportAsync(RomRole.Kernal, ct);

            Assert.Equal(RomProvisionState.NotProvisioned, vm.State);
            Assert.True(vm.IsBootBlocked);
            Assert.False(File.Exists(Path.Combine(directory, RomProvisionTestData.FileName(RomRole.Kernal))));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// FR-XROM-002, TR-XPATH-001. TEST-XSET-001.
    /// Use case: a storage import of a correctly-sized file whose bytes fail the MD5 check
    /// (wrong/corrupt dump) must be rejected without mutating provisioning state.
    /// Acceptance: importing a hash-mismatched picked file leaves <c>State</c> unchanged and
    /// writes no ROM file into the C64 directory.
    /// </summary>
    [Fact]
    public async Task ImportAsync_HashMismatch_RejectedStateUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        var directory = NewTempC64Directory();
        try
        {
            var picker = new FakeStoragePicker(RomProvisionTestData.PickedWrongBytes(RomRole.Kernal));
            var vm = CreateViewModel(directory, new FakeRomAcquirer(), picker);
            await vm.RefreshAsync(ct);

            Assert.Equal(RomProvisionState.NotProvisioned, vm.State);

            await vm.ImportAsync(RomRole.Kernal, ct);

            Assert.Equal(RomProvisionState.NotProvisioned, vm.State);
            Assert.True(vm.IsBootBlocked);
            Assert.False(File.Exists(Path.Combine(directory, RomProvisionTestData.FileName(RomRole.Kernal))));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// FR-XROM-002, TR-XPATH-001. TEST-XSET-001.
    /// Use case: a valid picked file for a role must be validated (size + MD5) and copied
    /// into the writable C64 directory, advancing provisioning.
    /// Acceptance: importing the three valid core ROMs one at a time writes each file and
    /// ends at <see cref="RomProvisionState.Complete"/> with <c>IsBootBlocked</c> false.
    /// </summary>
    [Fact]
    public async Task ImportAsync_ValidFiles_WritesAndCompletes()
    {
        var ct = TestContext.Current.CancellationToken;
        var directory = NewTempC64Directory();
        try
        {
            var vm = CreateViewModel(
                directory,
                new FakeRomAcquirer(),
                new FakeStoragePicker(RomProvisionTestData.PickedValid(RomRole.Basic)));
            await vm.RefreshAsync(ct);

            await vm.ImportAsync(RomRole.Basic, ct);
            Assert.True(File.Exists(Path.Combine(directory, RomProvisionTestData.FileName(RomRole.Basic))));

            // Fresh VMs sharing the directory import the remaining two roles.
            var vmKernal = CreateViewModel(
                directory,
                new FakeRomAcquirer(),
                new FakeStoragePicker(RomProvisionTestData.PickedValid(RomRole.Kernal)));
            await vmKernal.ImportAsync(RomRole.Kernal, ct);

            var vmChargen = CreateViewModel(
                directory,
                new FakeRomAcquirer(),
                new FakeStoragePicker(RomProvisionTestData.PickedValid(RomRole.Chargen)));
            await vmChargen.ImportAsync(RomRole.Chargen, ct);

            Assert.Equal(RomProvisionState.Complete, vmChargen.State);
            Assert.False(vmChargen.IsBootBlocked);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// FR-XROM-002, TR-XPATH-001. TEST-XSET-001.
    /// Use case: the verified-HTTPS download is a confirm-gated action; invoking it without
    /// an explicit confirm must be a no-op (no network call).
    /// Acceptance: <c>DownloadAsync</c> without <c>ConfirmDownload</c> never calls the
    /// acquirer and leaves <c>State</c> / <c>IsBootBlocked</c> unchanged.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_WithoutConfirm_DoesNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var directory = NewTempC64Directory();
        try
        {
            var acquirer = new FakeRomAcquirer(succeed: true);
            var vm = CreateViewModel(directory, acquirer, new FakeStoragePicker(null));
            await vm.RefreshAsync(ct);

            await vm.DownloadAsync(ct);

            Assert.Equal(0, acquirer.CallCount);
            Assert.Equal(RomProvisionState.NotProvisioned, vm.State);
            Assert.True(vm.IsBootBlocked);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    /// <summary>
    /// FR-XROM-001, FR-XROM-002, TR-XPATH-001. TEST-XSET-001.
    /// Use case: after the user explicitly confirms, the download acquires the verified core
    /// set and provisioning transitions to Complete, unblocking boot.
    /// Acceptance: <c>ConfirmDownload</c> then <c>DownloadAsync</c> calls the acquirer once,
    /// lands the three ROMs, and ends at <see cref="RomProvisionState.Complete"/> with
    /// <c>IsBootBlocked</c> false (which was true beforehand).
    /// </summary>
    [Fact]
    public async Task DownloadAsync_AfterConfirm_DownloadsAndCompletes()
    {
        var ct = TestContext.Current.CancellationToken;
        var directory = NewTempC64Directory();
        try
        {
            var acquirer = new FakeRomAcquirer(succeed: true);
            var vm = CreateViewModel(directory, acquirer, new FakeStoragePicker(null));
            await vm.RefreshAsync(ct);

            Assert.True(vm.IsBootBlocked);

            vm.ConfirmDownload();
            await vm.DownloadAsync(ct);

            Assert.Equal(1, acquirer.CallCount);
            Assert.Equal(RomProvisionState.Complete, vm.State);
            Assert.False(vm.IsBootBlocked);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
