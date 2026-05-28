namespace ViceSharp.TestHarness;

using System.Collections.Specialized;
using System.ComponentModel;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// Unit tests targeting <see cref="AttachPanelViewModel"/> and
/// <see cref="AttachSlotViewModel"/> as MVVM view-models in isolation.
/// Complements <see cref="AvaloniaBoundaryTests"/> (which covers
/// host-protocol boundary behaviour) by exercising default state,
/// PropertyChanged notifications, slot-level state transitions, and
/// limiter clamping at the view-model layer. BACKFILL-HOSTUI-001.
/// </summary>
public sealed class AttachPanelViewModelTests
{
    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachPanelViewModel).
    /// Use case: A freshly constructed attach panel must expose a
    /// deterministic default state so the UI binds against known
    /// initial values before the host responds.
    /// Acceptance: DockSide defaults to Left, ActiveTab is Peripherals,
    /// StatusText is "Disconnected", limiter is enabled at 100 percent,
    /// the four canonical media slots are populated with their MediaSlot
    /// identifiers, machine profile placeholders are present, and there
    /// are no pending settings changes.
    /// </summary>
    [Fact]
    public void Constructor_ProducesExpectedDefaultState()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());

        Assert.Equal(AttachDockSide.Left, viewModel.DockSide);
        Assert.Equal(SidebarTab.Peripherals, viewModel.ActiveTab);
        Assert.Equal("Disconnected", viewModel.StatusText);
        Assert.Equal(100, viewModel.LimiterRatePercent);
        Assert.True(viewModel.LimiterEnabled);
        Assert.False(viewModel.HasPendingSettingsChanges);
        Assert.False(viewModel.RequiresRestart);
        Assert.False(viewModel.HasSettingsValidationResults);
        Assert.Equal(4, viewModel.Slots.Count);
        Assert.Collection(
            viewModel.Slots,
            slot => Assert.Equal(MediaSlot.Drive8, slot.Slot),
            slot => Assert.Equal(MediaSlot.Drive9, slot.Slot),
            slot => Assert.Equal(MediaSlot.Tape, slot.Slot),
            slot => Assert.Equal(MediaSlot.Cartridge, slot.Slot));
        Assert.NotEmpty(viewModel.MachineProfiles);
        Assert.Equal(viewModel.MachineProfiles[0], viewModel.SelectedMachineProfile);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachPanelViewModel).
    /// Use case: The view-model must reject a null host client at
    /// construction so the UI fails fast rather than crashing later
    /// during a host call.
    /// Acceptance: Constructor with a null IHostProtocolClient throws
    /// <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_RejectsNullHostClient()
    {
        Assert.Throws<ArgumentNullException>(() => new AttachPanelViewModel(null!));
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachPanelViewModel).
    /// Use case: Toggling the dock side via DockLeft/DockRight is a
    /// pure view-model state change and MUST raise a PropertyChanged
    /// event so an Avalonia binding can react.
    /// Acceptance: DockRight raises PropertyChanged("DockSide") and
    /// DockSide becomes Right; DockLeft raises the same notification
    /// and DockSide becomes Left.
    /// </summary>
    [Fact]
    public void DockSideToggle_RaisesPropertyChanged()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var changes = TrackPropertyChanges(viewModel);

        viewModel.DockRight();
        Assert.Equal(AttachDockSide.Right, viewModel.DockSide);
        Assert.Contains(nameof(viewModel.DockSide), changes);

        changes.Clear();
        viewModel.DockLeft();
        Assert.Equal(AttachDockSide.Left, viewModel.DockSide);
        Assert.Contains(nameof(viewModel.DockSide), changes);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachPanelViewModel).
    /// Use case: The sidebar tab selection (Peripherals/Settings/
    /// Monitor) is a UI-only navigation concern and must drive
    /// PropertyChanged so the active tab indicator updates.
    /// Acceptance: ShowSettings and ShowMonitor change ActiveTab and
    /// raise PropertyChanged("ActiveTab"); ShowPeripherals restores
    /// the default tab.
    /// </summary>
    [Fact]
    public void ActiveTabNavigation_RaisesPropertyChanged()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var changes = TrackPropertyChanges(viewModel);

        viewModel.ShowSettings();
        Assert.Equal(SidebarTab.Settings, viewModel.ActiveTab);
        Assert.Contains(nameof(viewModel.ActiveTab), changes);

        changes.Clear();
        viewModel.ShowMonitor();
        Assert.Equal(SidebarTab.Monitor, viewModel.ActiveTab);
        Assert.Contains(nameof(viewModel.ActiveTab), changes);

        changes.Clear();
        viewModel.ShowPeripherals();
        Assert.Equal(SidebarTab.Peripherals, viewModel.ActiveTab);
        Assert.Contains(nameof(viewModel.ActiveTab), changes);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachPanelViewModel).
    /// Use case: Limiter rate is a bounded percentage; values outside
    /// the [LimiterMinimumPercent, LimiterMaximumPercent] range must be
    /// clamped at the view-model so the UI cannot push invalid values
    /// over the host boundary.
    /// Acceptance: Setting LimiterRatePercent above the maximum clamps
    /// to LimiterMaximumPercent; setting below the minimum clamps to
    /// LimiterMinimumPercent; both clamped values mark
    /// HasPendingSettingsChanges true and raise PropertyChanged.
    /// </summary>
    [Fact]
    public void LimiterRatePercent_ClampsOutOfRangeValues()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var changes = TrackPropertyChanges(viewModel);

        viewModel.LimiterRatePercent = 9_999;
        Assert.Equal(AttachPanelViewModel.LimiterMaximumPercent, viewModel.LimiterRatePercent);
        Assert.Contains(nameof(viewModel.LimiterRatePercent), changes);
        Assert.True(viewModel.HasPendingSettingsChanges);

        changes.Clear();
        viewModel.LimiterRatePercent = -50;
        Assert.Equal(AttachPanelViewModel.LimiterMinimumPercent, viewModel.LimiterRatePercent);
        Assert.Contains(nameof(viewModel.LimiterRatePercent), changes);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachPanelViewModel).
    /// Use case: Changing a settings field whose change requires a
    /// session restart must surface the RequiresRestart flag so the UI
    /// can prompt the user before applying.
    /// Acceptance: Setting SelectedResourceMode to a non-default value
    /// marks both HasPendingSettingsChanges and RequiresRestart true
    /// and emits PropertyChanged for both flags.
    /// </summary>
    [Fact]
    public void RestartRelevantSettingChange_FlagsRequiresRestart()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var changes = TrackPropertyChanges(viewModel);

        viewModel.SelectedResourceMode = "Use configured paths";

        Assert.True(viewModel.HasPendingSettingsChanges);
        Assert.True(viewModel.RequiresRestart);
        Assert.Contains(nameof(viewModel.HasPendingSettingsChanges), changes);
        Assert.Contains(nameof(viewModel.RequiresRestart), changes);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachSlotViewModel).
    /// Use case: A freshly constructed attach slot exposes the
    /// canonical identifiers (Slot, Title, MediaKind, FilePatterns)
    /// the file-picker dialog needs while presenting an empty state.
    /// Acceptance: Constructor records the supplied identifiers,
    /// IsAttached is false, FilePath is empty, StatusText is "Empty",
    /// ValidationError is empty, HasValidationError is false, and
    /// RecentFiles is empty.
    /// </summary>
    [Fact]
    public void AttachSlotViewModel_DefaultsToEmptyState()
    {
        var patterns = new[] { "*.d64", "*.g64" };
        var slot = new AttachSlotViewModel(MediaSlot.Drive8, "Drive 8", "Disk", patterns);

        Assert.Equal(MediaSlot.Drive8, slot.Slot);
        Assert.Equal("Drive 8", slot.Title);
        Assert.Equal("Disk", slot.MediaKind);
        Assert.Same(patterns, slot.FilePatterns);
        Assert.False(slot.IsAttached);
        Assert.Equal(string.Empty, slot.FilePath);
        Assert.Equal("Empty", slot.StatusText);
        Assert.Equal(string.Empty, slot.ValidationError);
        Assert.False(slot.HasValidationError);
        Assert.False(slot.IsReadOnly);
        Assert.Empty(slot.RecentFiles);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachSlotViewModel).
    /// Use case: When the host reports a successful attachment the
    /// slot view-model must reflect the attached state, capture the
    /// canonical path, surface a friendly status, and remember the
    /// original picker path in RecentFiles.
    /// Acceptance: After ApplyAttachment with a recent-file path, the
    /// slot reports IsAttached=true, IsReadOnly mirrors the DTO,
    /// FilePath matches the host-returned path, StatusText shows the
    /// display name, and RecentFiles contains the picker path.
    /// </summary>
    [Fact]
    public void AttachSlotViewModel_ApplyAttachmentReflectsHostState()
    {
        var slot = new AttachSlotViewModel(MediaSlot.Drive8, "Drive 8", "Disk", new[] { "*.d64" });
        var changes = TrackPropertyChanges(slot);

        var attachment = new MediaAttachmentDto(
            MediaSlot.Drive8,
            @"C:\host\managed\demo.d64",
            "demo.d64",
            true,
            true,
            true);

        slot.ApplyAttachment(attachment, @"C:\picker\demo.d64");

        Assert.True(slot.IsAttached);
        Assert.True(slot.IsReadOnly);
        Assert.Equal(@"C:\host\managed\demo.d64", slot.FilePath);
        Assert.Equal("demo.d64", slot.StatusText);
        Assert.False(slot.HasValidationError);
        Assert.Contains(@"C:\picker\demo.d64", slot.RecentFiles);
        Assert.Contains(nameof(slot.IsAttached), changes);
        Assert.Contains(nameof(slot.FilePath), changes);
        Assert.Contains(nameof(slot.StatusText), changes);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachSlotViewModel).
    /// Use case: A staged attachment (AppliedToRuntime=false) must be
    /// distinguished from an applied attachment so the UI can mark it
    /// pending until the runtime processes the change.
    /// Acceptance: ApplyAttachment with AppliedToRuntime=false appends
    /// the " staged" suffix to StatusText and still reports
    /// IsAttached=true.
    /// </summary>
    [Fact]
    public void AttachSlotViewModel_StagedAttachmentMarksStatusAsStaged()
    {
        var slot = new AttachSlotViewModel(MediaSlot.Drive8, "Drive 8", "Disk", new[] { "*.d64" });

        slot.ApplyAttachment(new MediaAttachmentDto(
            MediaSlot.Drive8,
            @"C:\host\managed\demo.d64",
            "demo.d64",
            true,
            false,
            false));

        Assert.True(slot.IsAttached);
        Assert.EndsWith(" staged", slot.StatusText);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachSlotViewModel).
    /// Use case: MarkEmpty must reset the slot to its empty state and
    /// MarkError must surface the host's failure message so the UI
    /// can show inline validation.
    /// Acceptance: MarkEmpty clears FilePath, IsAttached and
    /// ValidationError and resets StatusText to "Empty"; MarkError
    /// stores the message in ValidationError, flips HasValidationError
    /// to true, and sets StatusText to "Error".
    /// </summary>
    [Fact]
    public void AttachSlotViewModel_MarkEmptyAndMarkErrorTransitionsAreVisible()
    {
        var slot = new AttachSlotViewModel(MediaSlot.Drive8, "Drive 8", "Disk", new[] { "*.d64" });
        slot.ApplyAttachment(new MediaAttachmentDto(
            MediaSlot.Drive8,
            @"C:\host\managed\demo.d64",
            "demo.d64",
            true,
            false,
            true));

        var changes = TrackPropertyChanges(slot);

        slot.MarkEmpty();
        Assert.False(slot.IsAttached);
        Assert.Equal(string.Empty, slot.FilePath);
        Assert.Equal("Empty", slot.StatusText);
        Assert.Equal(string.Empty, slot.ValidationError);
        Assert.False(slot.HasValidationError);
        Assert.Contains(nameof(slot.IsAttached), changes);

        changes.Clear();
        slot.MarkError("Disk image unreadable.");
        Assert.True(slot.HasValidationError);
        Assert.Equal("Disk image unreadable.", slot.ValidationError);
        Assert.Equal("Error", slot.StatusText);
        Assert.Contains(nameof(slot.ValidationError), changes);
        Assert.Contains(nameof(slot.StatusText), changes);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachSlotViewModel).
    /// Use case: AddRecentFile must build a most-recent-first MRU list
    /// without duplicates and capped at six entries so the UI can show
    /// a stable recent-files menu.
    /// Acceptance: Adding a path twice keeps a single entry at index 0;
    /// adding seven unique paths leaves exactly six in RecentFiles with
    /// the most recent path at index 0; an empty/whitespace path is
    /// ignored.
    /// </summary>
    [Fact]
    public void AttachSlotViewModel_AddRecentFileBuildsCappedMru()
    {
        var slot = new AttachSlotViewModel(MediaSlot.Drive8, "Drive 8", "Disk", new[] { "*.d64" });

        slot.AddRecentFile("a.d64");
        slot.AddRecentFile("b.d64");
        slot.AddRecentFile("a.d64");

        Assert.Equal(2, slot.RecentFiles.Count);
        Assert.Equal("a.d64", slot.RecentFiles[0]);
        Assert.Equal("b.d64", slot.RecentFiles[1]);

        slot.AddRecentFile("c.d64");
        slot.AddRecentFile("d.d64");
        slot.AddRecentFile("e.d64");
        slot.AddRecentFile("f.d64");
        slot.AddRecentFile("g.d64");

        Assert.Equal(6, slot.RecentFiles.Count);
        Assert.Equal("g.d64", slot.RecentFiles[0]);

        var beforeWhitespace = slot.RecentFiles.ToArray();
        slot.AddRecentFile("   ");
        slot.AddRecentFile(string.Empty);
        Assert.Equal(beforeWhitespace, slot.RecentFiles.ToArray());
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 AttachSlotViewModel).
    /// Use case: The slot's IsReadOnly property is user-controlled
    /// (the only public setter on the slot view-model) so the UI can
    /// toggle a read-only checkbox before attaching media.
    /// Acceptance: Setting IsReadOnly to a new value raises
    /// PropertyChanged for IsReadOnly; setting the same value again
    /// does not raise the event.
    /// </summary>
    [Fact]
    public void AttachSlotViewModel_IsReadOnlySetterRaisesPropertyChangedOnce()
    {
        var slot = new AttachSlotViewModel(MediaSlot.Drive8, "Drive 8", "Disk", new[] { "*.d64" });
        var changes = TrackPropertyChanges(slot);

        slot.IsReadOnly = true;
        Assert.True(slot.IsReadOnly);
        Assert.Single(changes, nameof(slot.IsReadOnly));

        changes.Clear();
        slot.IsReadOnly = true;
        Assert.Empty(changes);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 + Warp Mode addition).
    /// Use case: The UI must be able to toggle the limiter on/off to
    /// enter "Warp" (uncapped) mode for profiling or fast-forward.
    /// Acceptance: Toggling LimiterEnabled raises PropertyChanged for
    /// both LimiterEnabled and HasPendingSettingsChanges, and the
    /// value is reflected immediately.
    /// </summary>
    [Fact]
    public void LimiterEnabled_ToggleRaisesPropertyChanged()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var changes = TrackPropertyChanges(viewModel);

        Assert.True(viewModel.LimiterEnabled);

        viewModel.LimiterEnabled = false;
        Assert.False(viewModel.LimiterEnabled);
        Assert.Contains(nameof(viewModel.LimiterEnabled), changes);
        Assert.Contains(nameof(viewModel.HasPendingSettingsChanges), changes);

        changes.Clear();
        viewModel.LimiterEnabled = true;
        Assert.True(viewModel.LimiterEnabled);
        Assert.Contains(nameof(viewModel.LimiterEnabled), changes);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 + Warp Mode addition).
    /// Use case: When the limiter is disabled this represents Warp
    /// (uncapped) mode, which is the intended fast path for
    /// dotTrace / performance work. The view-model must still allow
    /// rate changes to be staged even while disabled.
    /// Acceptance: With LimiterEnabled=false, changing the rate still
    /// clamps correctly and marks HasPendingSettingsChanges.
    /// </summary>
    [Fact]
    public void Limiter_WhenDisabled_RateChangesStillClampAndMarkPending()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        viewModel.LimiterEnabled = false;

        var changes = TrackPropertyChanges(viewModel);

        viewModel.LimiterRatePercent = 9999;
        Assert.Equal(AttachPanelViewModel.LimiterMaximumPercent, viewModel.LimiterRatePercent);
        Assert.Contains(nameof(viewModel.LimiterRatePercent), changes);
        Assert.True(viewModel.HasPendingSettingsChanges);
    }

    /// <summary>
    /// FR: FR-Host-UI-Boundary, TR: TR-MVVM-001
    /// (BACKFILL-HOSTUI-001 + Warp Mode addition).
    /// Use case: The UI exposes "Warp Mode" as the inverse of the limiter.
    /// Toggling Warp Mode must correctly enable/disable the limiter so that
    /// when Warp is active the speed limiter is removed (uncapped speed),
    /// and when Warp is turned off the limiter is re-enabled.
    /// Acceptance: Setting IsWarpMode = true sets LimiterEnabled = false
    /// and raises PropertyChanged for both; setting IsWarpMode = false
    /// sets LimiterEnabled = true and raises the notifications. The two
    /// properties remain perfect inverses.
    /// </summary>
    [Fact]
    public void IsWarpMode_ToggleRemovesAndEnablesLimiter()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var changes = TrackPropertyChanges(viewModel);

        // Default state: limiter on → warp off
        Assert.True(viewModel.LimiterEnabled);
        Assert.False(viewModel.IsWarpMode);

        // Turn Warp on → limiter must be disabled
        viewModel.IsWarpMode = true;
        Assert.False(viewModel.LimiterEnabled);
        Assert.True(viewModel.IsWarpMode);
        Assert.Contains(nameof(viewModel.IsWarpMode), changes);
        Assert.Contains(nameof(viewModel.LimiterEnabled), changes);
        Assert.True(viewModel.HasPendingSettingsChanges);

        // Turn Warp off → limiter must be re-enabled
        changes.Clear();
        viewModel.IsWarpMode = false;
        Assert.True(viewModel.LimiterEnabled);
        Assert.False(viewModel.IsWarpMode);
        Assert.Contains(nameof(viewModel.IsWarpMode), changes);
        Assert.Contains(nameof(viewModel.LimiterEnabled), changes);
    }

    private static List<string> TrackPropertyChanges(INotifyPropertyChanged source)
    {
        var changes = new List<string>();
        source.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is not null)
                changes.Add(args.PropertyName);
        };
        return changes;
    }
}
