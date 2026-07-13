namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Linq;
using System.Threading.Tasks;
using ViceSharp.Abstractions;
using ViceSharp.Protocol;
using ViceSharp.TestHarness.Xbox.Fakes;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP S27 (IMPL-XBOXUWP-027), area XDEV / XMVVM. TEST-XSET-001 off-console
/// coverage for <see cref="XboxDeviceSetupViewModel"/> + <see cref="XboxMediaSlotViewModel"/>:
/// the 10-foot device-setup ViewModel that reproduces the desktop
/// <c>AttachPanelViewModel</c> / <c>AttachSlotViewModel</c> device behaviors (typed
/// drive/tape/cartridge attach + eject, true-drive toggle, drive-model selection, and
/// the single-true-drive-rig invariant) against the portable
/// <see cref="IXboxSettingsGateway"/> seam. Every test drives a
/// <see cref="FakeXboxSettingsGateway"/> so the suite runs with no engine, host,
/// console, or XAML dependency (TR-MVVM-001) and uses plain <see cref="FactAttribute"/>
/// (no console gate).
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxDeviceSetupViewModelTests
{
    /// <summary>
    /// FR-XDEV-001, FR-XDEV-002, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: the Device Setup page presents the four fixed peripheral cards
    /// (Drive 8 / Drive 9 / Tape / Cartridge) with the same slot kinds and file
    /// patterns the desktop uses, and the two drive cards expose the implemented
    /// drive-model selector.
    /// Acceptance: the four slots carry the exact <see cref="MediaSlot"/> values and
    /// file patterns from <c>AttachPanelViewModel.cs:57-62</c>; both drive slots are
    /// true-drive capable with <c>AvailableDriveModels</c> == <see cref="DriveModelCatalog.Implemented"/>;
    /// the tape and cartridge slots are not true-drive capable and expose no models.
    /// </summary>
    [Fact]
    public void Slots_HaveExpectedKindsPatternsAndDriveModels()
    {
        var vm = new XboxDeviceSetupViewModel(new FakeXboxSettingsGateway());

        Assert.Equal(4, vm.Slots.Count);

        var drive8 = vm.Slots[0];
        Assert.Equal(MediaSlot.Drive8, drive8.Slot);
        Assert.Equal(new[] { "*.d64", "*.g64" }, drive8.FilePatterns);
        Assert.True(drive8.SupportsTrueDrive);
        Assert.Same(DriveModelCatalog.Implemented, drive8.AvailableDriveModels);
        Assert.Equal(DriveModel.C1541, drive8.SelectedDriveModel);

        var drive9 = vm.Slots[1];
        Assert.Equal(MediaSlot.Drive9, drive9.Slot);
        Assert.Equal(new[] { "*.d64", "*.g64" }, drive9.FilePatterns);
        Assert.True(drive9.SupportsTrueDrive);
        Assert.Same(DriveModelCatalog.Implemented, drive9.AvailableDriveModels);

        var tape = vm.Slots[2];
        Assert.Equal(MediaSlot.Tape, tape.Slot);
        Assert.Equal(new[] { "*.tap" }, tape.FilePatterns);
        Assert.False(tape.SupportsTrueDrive);
        Assert.Empty(tape.AvailableDriveModels);

        var cartridge = vm.Slots[3];
        Assert.Equal(MediaSlot.Cartridge, cartridge.Slot);
        Assert.Equal(new[] { "*.crt", "*.bin", "*.rom" }, cartridge.FilePatterns);
        Assert.False(cartridge.SupportsTrueDrive);
        Assert.Empty(cartridge.AvailableDriveModels);
    }

    /// <summary>
    /// FR-XDEV-002, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: the drive-model selector must offer exactly the implemented 1541-family
    /// drive models, and each model's integer value must be VICE's canonical drive-type
    /// number so it round-trips through the host true-drive rebuild without translation.
    /// Acceptance: <see cref="DriveModelCatalog.Implemented"/> is 1541, 1540, 1541-II in
    /// order; the backing integers are 1541, 1540, and 1542 (the 1541-II is drive type
    /// 1542, NOT 1541).
    /// </summary>
    [Fact]
    public void DriveModelCatalog_IsThe1541Family_With1541IIAs1542()
    {
        Assert.Equal(
            new[] { DriveModel.C1541, DriveModel.C1540, DriveModel.C1541II },
            DriveModelCatalog.Implemented);

        Assert.Equal(1541, (int)DriveModel.C1541);
        Assert.Equal(1540, (int)DriveModel.C1540);
        Assert.Equal(1542, (int)DriveModel.C1541II);
    }

    /// <summary>
    /// FR-XDEV-001, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: attaching a disk from the Device Setup page sends the chosen path to
    /// the host media boundary for the card's slot and reflects the resulting
    /// attachment on the card.
    /// Acceptance: <see cref="XboxDeviceSetupViewModel.AttachAsync"/> calls the gateway
    /// with the slot's <see cref="MediaSlot"/> and the path (read-only flag forwarded),
    /// and the slot becomes attached from the response.
    /// </summary>
    [Fact]
    public async Task AttachAsync_SendsPathAndSlotToGateway_AndUpdatesSlot()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeXboxSettingsGateway();
        var vm = new XboxDeviceSetupViewModel(gateway);
        var drive8 = vm.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);

        await vm.AttachAsync(drive8, @"C:\games\zork.d64", isReadOnly: true, cancellationToken: ct);

        Assert.Equal(MediaSlot.Drive8, gateway.AttachedSlot);
        Assert.Equal(@"C:\games\zork.d64", gateway.AttachedPath);
        Assert.True(gateway.AttachedReadOnly);
        Assert.True(drive8.IsAttached);
        Assert.True(drive8.IsReadOnly);
        Assert.Equal(@"C:\games\zork.d64", drive8.FilePath);
    }

    /// <summary>
    /// FR-XDEV-001, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: on a sandboxed console the head reads the picked file's bytes and hands
    /// them to the ViewModel, so an attach with a payload must use the payload overload
    /// and carry the display name.
    /// Acceptance: <see cref="XboxDeviceSetupViewModel.AttachAsync"/> with a non-empty
    /// payload calls the gateway's payload overload, forwarding the bytes and the display
    /// name.
    /// </summary>
    [Fact]
    public async Task AttachAsync_WithPayload_UsesPayloadOverloadAndDisplayName()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeXboxSettingsGateway();
        var vm = new XboxDeviceSetupViewModel(gateway);
        var drive8 = vm.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);
        var payload = new byte[] { 1, 2, 3, 4 };

        await vm.AttachAsync(drive8, @"C:\games\zork.d64", isReadOnly: false, payload, "zork.d64", ct);

        Assert.Equal(MediaSlot.Drive8, gateway.AttachedSlot);
        Assert.Same(payload, gateway.AttachedPayload);
        Assert.Equal("zork.d64", gateway.AttachedDisplayName);
        Assert.True(drive8.IsAttached);
    }

    /// <summary>
    /// FR-XDEV-001, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: ejecting a card detaches whatever media occupies it through the host
    /// media boundary and returns the card to empty.
    /// Acceptance: <see cref="XboxDeviceSetupViewModel.EjectAsync"/> calls
    /// <see cref="IXboxSettingsGateway.DetachMediaAsync"/> for the slot and clears the
    /// slot's attached state.
    /// </summary>
    [Fact]
    public async Task EjectAsync_DetachesSlot_AndClearsIt()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeXboxSettingsGateway();
        var vm = new XboxDeviceSetupViewModel(gateway);
        var tape = vm.Slots.Single(slot => slot.Slot == MediaSlot.Tape);

        await vm.AttachAsync(tape, @"C:\tapes\game.tap", isReadOnly: false, cancellationToken: ct);
        Assert.True(tape.IsAttached);

        await vm.EjectAsync(tape, ct);

        Assert.Equal(MediaSlot.Tape, gateway.DetachedSlot);
        Assert.False(tape.IsAttached);
        Assert.Equal(string.Empty, tape.FilePath);
    }

    /// <summary>
    /// FR-XDEV-001, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: opening the Device Setup page lists the media currently attached to the
    /// session and mirrors it onto the cards.
    /// Acceptance: <see cref="XboxDeviceSetupViewModel.RefreshAsync"/> reads
    /// <see cref="IXboxSettingsGateway.ListMediaAsync"/> and marks the reported slots
    /// attached while leaving the others empty.
    /// </summary>
    [Fact]
    public async Task RefreshAsync_PopulatesSlotsFromListMedia()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeXboxSettingsGateway();
        await gateway.AttachMediaAsync(MediaSlot.Drive8, @"C:\a.d64", isReadOnly: false, ct);
        var vm = new XboxDeviceSetupViewModel(gateway);

        await vm.RefreshAsync(ct);

        var drive8 = vm.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);
        var drive9 = vm.Slots.Single(slot => slot.Slot == MediaSlot.Drive9);
        Assert.True(drive8.IsAttached);
        Assert.Equal(@"C:\a.d64", drive8.FilePath);
        Assert.False(drive9.IsAttached);
    }

    /// <summary>
    /// FR-XDEV-002, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: changing the drive model while that drive's true-drive rig is ACTIVE
    /// rebuilds the rig with the newly selected model; selecting the 1541-II must send
    /// VICE drive type 1542.
    /// Acceptance: with Drive 8 true-drive active,
    /// <see cref="XboxDeviceSetupViewModel.SelectDriveModelAsync"/> to
    /// <see cref="DriveModel.C1541II"/> calls
    /// <c>SetTrueDriveAsync(true, 8, diskPath, 1542)</c>.
    /// </summary>
    [Fact]
    public async Task SelectDriveModel_OnActiveTrueDrive_RebuildsRigWith1542For1541II()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeXboxSettingsGateway();
        var vm = new XboxDeviceSetupViewModel(gateway);
        var drive8 = vm.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);

        await vm.AttachAsync(drive8, @"C:\disk.d64", isReadOnly: false, cancellationToken: ct);
        await vm.SetTrueDriveAsync(drive8, enabled: true, cancellationToken: ct);
        var callsBefore = gateway.SetTrueDriveCallCount;

        await vm.SelectDriveModelAsync(drive8, DriveModel.C1541II, ct);

        Assert.Equal(DriveModel.C1541II, drive8.SelectedDriveModel);
        Assert.True(gateway.SetTrueDriveCallCount > callsBefore);
        Assert.True(gateway.TrueDrive);
        Assert.Equal(8, gateway.TrueDriveDevice);
        Assert.Equal(@"C:\disk.d64", gateway.TrueDriveDiskImagePath);
        Assert.Equal(1542, gateway.TrueDriveModel);
    }

    /// <summary>
    /// FR-XDEV-002, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: changing the drive model while that drive's true-drive rig is INACTIVE
    /// only records the pending selection and must not touch the host (no rig rebuild).
    /// Acceptance: <see cref="XboxDeviceSetupViewModel.SelectDriveModelAsync"/> on a slot
    /// whose true-drive is off updates <c>SelectedDriveModel</c> and issues no
    /// <c>SetTrueDriveAsync</c> call.
    /// </summary>
    [Fact]
    public async Task SelectDriveModel_OnInactiveSlot_MakesNoGatewayCall()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeXboxSettingsGateway();
        var vm = new XboxDeviceSetupViewModel(gateway);
        var drive8 = vm.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);

        await vm.SelectDriveModelAsync(drive8, DriveModel.C1541II, ct);

        Assert.Equal(DriveModel.C1541II, drive8.SelectedDriveModel);
        Assert.False(drive8.IsTrueDrive);
        Assert.Equal(0, gateway.SetTrueDriveCallCount);
        Assert.Null(gateway.TrueDrive);
    }

    /// <summary>
    /// FR-XDEV-003, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: only one true-drive rig may exist at a time, so enabling true-drive on
    /// the second drive must disable it on the first (single-rig invariant).
    /// Acceptance: with Drive 8 true-drive active, enabling Drive 9 true-drive via
    /// <see cref="XboxDeviceSetupViewModel.SetTrueDriveAsync"/> leaves exactly one slot
    /// with <c>IsTrueDrive</c> true (Drive 9) and the final rebuild targets device 9.
    /// </summary>
    [Fact]
    public async Task EnablingTrueDriveOnSecondDrive_DisablesFirst_SingleRig()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeXboxSettingsGateway();
        var vm = new XboxDeviceSetupViewModel(gateway);
        var drive8 = vm.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);
        var drive9 = vm.Slots.Single(slot => slot.Slot == MediaSlot.Drive9);

        await vm.SetTrueDriveAsync(drive8, enabled: true, cancellationToken: ct);
        Assert.True(drive8.IsTrueDrive);

        await vm.SetTrueDriveAsync(drive9, enabled: true, cancellationToken: ct);

        Assert.False(drive8.IsTrueDrive);
        Assert.True(drive9.IsTrueDrive);
        Assert.Single(vm.Slots, slot => slot.IsTrueDrive);
        Assert.True(gateway.TrueDrive);
        Assert.Equal(9, gateway.TrueDriveDevice);
    }

    /// <summary>
    /// FR-XDEV-003, TR-XMVVM-001. TEST-XSET-001.
    /// Use case: turning off the active drive's true-drive returns the session to the
    /// buffered (simulated) drive.
    /// Acceptance: after enabling Drive 8 true-drive, disabling it via
    /// <see cref="XboxDeviceSetupViewModel.SetTrueDriveAsync"/> leaves no slot true-drive
    /// and issues <c>SetTrueDriveAsync(false, ...)</c>.
    /// </summary>
    [Fact]
    public async Task DisablingTrueDrive_IssuesSetTrueDriveFalse()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeXboxSettingsGateway();
        var vm = new XboxDeviceSetupViewModel(gateway);
        var drive8 = vm.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);

        await vm.SetTrueDriveAsync(drive8, enabled: true, cancellationToken: ct);
        await vm.SetTrueDriveAsync(drive8, enabled: false, cancellationToken: ct);

        Assert.False(drive8.IsTrueDrive);
        Assert.DoesNotContain(vm.Slots, slot => slot.IsTrueDrive);
        Assert.False(gateway.TrueDrive);
    }
}
