namespace ViceSharp.TestHarness;

using System.Collections.Specialized;
using System.ComponentModel;
using NSubstitute;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.Persistence;
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
    /// FR-PACESEL-001 / TR-PACESEL-001 / TEST-PACESEL-003.
    /// Use case: The pacing strategy applies live (the pump swaps its gate), so changing it
    /// must mark a pending settings change but NOT require a session restart.
    /// Acceptance: Setting SelectedPacingStrategy to "VICE" flags HasPendingSettingsChanges
    /// true and leaves RequiresRestart false.
    /// </summary>
    [Fact]
    public void LiveSettingChange_PacingStrategy_FlagsPendingNotRestart()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());

        viewModel.SelectedPacingStrategy = "VICE";

        Assert.True(viewModel.HasPendingSettingsChanges);
        Assert.False(viewModel.RequiresRestart);
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

    /// <summary>
    /// FR: FR-UI-003, FR: FR-HOST-006, TR: TR-UI-SHELL-001, TEST-UI-001.
    /// Use case: The peripherals tab must show IEC active/idle state on
    /// drive slots using the same host status telemetry as the status bar.
    /// Acceptance: Applying an active status marks drive 8 and drive 9
    /// active, leaves non-drive media slots idle, and applying a later idle
    /// status returns both drives to idle.
    /// </summary>
    [Fact]
    public void ApplyStatus_UpdatesDriveIecActivityFromHostTelemetry()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var drive8 = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);
        var drive9 = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Drive9);
        var tape = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Tape);

        viewModel.ApplyStatus(CreateStatus(iecActive: true, transitionCount: 6));

        Assert.True(drive8.IsIecBusActive);
        Assert.True(drive9.IsIecBusActive);
        Assert.Equal("IEC Active", drive8.IecActivityText);
        Assert.False(tape.IsIecBusActive);

        viewModel.ApplyStatus(CreateStatus(iecActive: false, transitionCount: 6));

        Assert.False(drive8.IsIecBusActive);
        Assert.False(drive9.IsIecBusActive);
        Assert.Equal("IEC Idle", drive8.IecActivityText);
    }

    /// <summary>
    /// FR: FR-UI-002, FR: FR-HOST-006, TR: TR-UI-SHELL-001, TEST-UI-001.
    /// Use case: The status bar view-model consumes the same host status
    /// payload and must add IEC activity without losing existing runtime
    /// fields.
    /// Acceptance: Active and idle statuses render the IEC state while
    /// retaining power, run state, limiter, FPS, clock, cycle, and PC text.
    /// </summary>
    [Fact]
    public void StatusBarViewModel_FormatsIecActivityWithoutDroppingRuntimeFields()
    {
        var viewModel = new StatusBarViewModel();

        viewModel.ApplyStatus(CreateStatus(iecActive: true, transitionCount: 12), RpcStatus.Ok());

        Assert.Contains("Power On", viewModel.StatusText);
        Assert.Contains("Run Running", viewModel.StatusText);
        Assert.Contains("Limiter 100%", viewModel.StatusText);
        Assert.Contains("FPS 50.0", viewModel.StatusText);
        Assert.Contains("Clock 1.000 MHz", viewModel.StatusText);
        Assert.Contains("Cycle 1234", viewModel.StatusText);
        Assert.Contains("PC C000", viewModel.StatusText);
        Assert.Contains("IEC Active", viewModel.StatusText);

        viewModel.ApplyStatus(CreateStatus(iecActive: false, transitionCount: 12), RpcStatus.Ok());

        Assert.Contains("IEC Idle", viewModel.StatusText);
    }

    /// <summary>
    /// FR: FR-UIFLYOUT-001, TR: TR-UIAXAML-VIEWS-001, TEST-UIFLYOUT-001.
    /// Use case: The shell replaces the separate Left/Right dock buttons with a
    /// single icon that flips the flyout side, so the view-model must expose a
    /// single ToggleDockSide that alternates Left and Right.
    /// Acceptance: From the default Left, ToggleDockSide makes DockSide Right and
    /// a second call returns it to Left; each flip raises PropertyChanged for
    /// DockSide and the derived PanePlacement.
    /// </summary>
    [Fact]
    public void ToggleDockSide_FlipsLeftRight()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var changes = TrackPropertyChanges(viewModel);

        Assert.Equal(AttachDockSide.Left, viewModel.DockSide);

        viewModel.ToggleDockSide();
        Assert.Equal(AttachDockSide.Right, viewModel.DockSide);
        Assert.Equal(global::Avalonia.Controls.SplitViewPanePlacement.Right, viewModel.PanePlacement);
        Assert.Contains(nameof(viewModel.DockSide), changes);
        Assert.Contains(nameof(viewModel.PanePlacement), changes);

        changes.Clear();
        viewModel.ToggleDockSide();
        Assert.Equal(AttachDockSide.Left, viewModel.DockSide);
        Assert.Equal(global::Avalonia.Controls.SplitViewPanePlacement.Left, viewModel.PanePlacement);
        Assert.Contains(nameof(viewModel.DockSide), changes);
    }

    /// <summary>
    /// FR: FR-UIFLYOUT-001, TR: TR-UIAXAML-VIEWS-001, TEST-UIFLYOUT-001.
    /// Use case: The sidebar is a flyout whose open/closed state the shell binds
    /// to SplitView.IsPaneOpen; the view-model must default to open and offer a
    /// ToggleSidebar that flips the state with notification.
    /// Acceptance: IsPaneOpen defaults to true; ToggleSidebar flips it to false
    /// then back to true, raising PropertyChanged(IsPaneOpen) on each flip.
    /// </summary>
    [Fact]
    public void ToggleSidebar_TogglesIsPaneOpen()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var changes = TrackPropertyChanges(viewModel);

        Assert.True(viewModel.IsPaneOpen);

        viewModel.ToggleSidebar();
        Assert.False(viewModel.IsPaneOpen);
        Assert.Contains(nameof(viewModel.IsPaneOpen), changes);

        changes.Clear();
        viewModel.ToggleSidebar();
        Assert.True(viewModel.IsPaneOpen);
        Assert.Contains(nameof(viewModel.IsPaneOpen), changes);
    }

    /// <summary>
    /// FR: FR-UIFLYOUT-001, TR: TR-UIAXAML-VIEWS-001, TEST-UIFLYOUT-001.
    /// Use case: The AXAML shell binds SplitView.PanePlacement to the view-model
    /// so the flyout renders on the correct edge; the mapping from the
    /// dock-side enum to the Avalonia placement must be deterministic.
    /// Acceptance: Left maps to SplitViewPanePlacement.Left and Right maps to
    /// SplitViewPanePlacement.Right, both consistent with DockSide.
    /// </summary>
    [Fact]
    public void PanePlacement_MatchesDockSide()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());

        viewModel.DockLeft();
        Assert.Equal(global::Avalonia.Controls.SplitViewPanePlacement.Left, viewModel.PanePlacement);

        viewModel.DockRight();
        Assert.Equal(global::Avalonia.Controls.SplitViewPanePlacement.Right, viewModel.PanePlacement);
    }

    /// <summary>
    /// FR: FR-UIPERIPHERAL-001, TR: TR-UIAXAML-VIEWS-001, TEST-UIPERIPHERAL-001.
    /// Use case: A single reusable card template renders every peripheral; its
    /// drive-only affordances (IEC activity, True Drive toggle) are gated on
    /// SupportsTrueDrive so the same template adapts per slot kind, and True
    /// Drive is a settable binding surface that defaults off.
    /// Acceptance: Drive 8 and Drive 9 report SupportsTrueDrive true while Tape
    /// and Cartridge report false; TrueDrive defaults false and toggling it
    /// raises PropertyChanged so the card checkbox stays in sync.
    /// </summary>
    [Fact]
    public void PeripheralCard_BindingSurface_TrueDrivePerSlotKind()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var drive8 = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);
        var drive9 = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Drive9);
        var tape = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Tape);
        var cartridge = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Cartridge);

        Assert.True(drive8.SupportsTrueDrive);
        Assert.True(drive9.SupportsTrueDrive);
        Assert.False(tape.SupportsTrueDrive);
        Assert.False(cartridge.SupportsTrueDrive);

        Assert.False(drive8.TrueDrive);
        var changes = TrackPropertyChanges(drive8);
        drive8.TrueDrive = true;
        Assert.True(drive8.TrueDrive);
        Assert.Contains(nameof(drive8.TrueDrive), changes);
    }

    /// <summary>
    /// FR: FR-UIPERIPHERAL-001, TR: TR-MVVM-001, TEST-UIPERIPHERAL-001.
    /// Use case: The reusable card's Attach button routes through the panel
    /// view-model's host-backed picker pipeline rather than owning the dialog,
    /// so the card stays presentation-only.
    /// Acceptance: With no FilePicker set, AttachFromPickerAsync surfaces an
    /// inline "File picker is unavailable." error; with a picker that returns a
    /// path the attach is routed to the host (observed here as the disconnected
    /// host reporting a validation error on the slot).
    /// </summary>
    [Fact]
    public async Task AttachFromPicker_RoutesThroughHostAttachPipeline()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var drive8 = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);

        await viewModel.AttachFromPickerAsync(drive8, TestContext.Current.CancellationToken);
        Assert.True(drive8.HasValidationError);
        Assert.Equal("File picker is unavailable.", drive8.ValidationError);

        var pickerInvoked = false;
        viewModel.FilePicker = _ =>
        {
            pickerInvoked = true;
            return Task.FromResult<string?>(@"C:\does-not-exist\demo.d64");
        };

        await viewModel.AttachFromPickerAsync(drive8, TestContext.Current.CancellationToken);
        Assert.True(pickerInvoked);
        Assert.True(drive8.HasValidationError);
    }

    /// <summary>
    /// FR: FR-UISETTINGS-001, TR: TR-UIAXAML-VIEWS-001, TEST-UISETTINGS-001.
    /// Use case: The SettingsView UserControl binds its combo boxes two-way to
    /// the view-model's selection properties; changing one must stage a pending
    /// change, and Revert must restore the last applied local state so the
    /// settings form behaves predictably without a host round-trip.
    /// Acceptance: Changing SelectedRenderer and SelectedPalette flips
    /// HasPendingSettingsChanges true and raises PropertyChanged for the
    /// selections; RevertSettings restores the original values and clears the
    /// pending flag.
    /// </summary>
    [Fact]
    public void SettingsSelections_StagePendingChange_AndRevertRestores()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var originalRenderer = viewModel.SelectedRenderer;
        var originalPalette = viewModel.SelectedPalette;
        var changes = TrackPropertyChanges(viewModel);

        viewModel.SelectedRenderer = "Software";
        viewModel.SelectedPalette = "Amber";

        Assert.Equal("Software", viewModel.SelectedRenderer);
        Assert.Equal("Amber", viewModel.SelectedPalette);
        Assert.True(viewModel.HasPendingSettingsChanges);
        Assert.Contains(nameof(viewModel.SelectedRenderer), changes);
        Assert.Contains(nameof(viewModel.SelectedPalette), changes);

        viewModel.RevertSettings();

        Assert.Equal(originalRenderer, viewModel.SelectedRenderer);
        Assert.Equal(originalPalette, viewModel.SelectedPalette);
        Assert.False(viewModel.HasPendingSettingsChanges);
    }

    /// <summary>
    /// FR: FR-DRVLED-001, TR: TR-UIAXAML-VIEWS-001, TEST-DRVLED-001.
    /// Use case: The peripheral card shows a per-drive activity LED. The LED is
    /// a VM-level state that lights for drives during IEC activity (simulated
    /// proxy) and can be set faithfully from VIA2 PB3 telemetry (true-drive),
    /// but never lights for non-drive slots.
    /// Acceptance: SetIecActivity(true) lights Drive 8's LedOn and raises
    /// PropertyChanged; the Tape slot never lights; SetDriveLed(true) lights a
    /// drive and is a no-op on a non-drive slot.
    /// </summary>
    [Fact]
    public void DriveLed_LightsForDrivesOnly()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        var drive8 = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);
        var tape = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Tape);
        var changes = TrackPropertyChanges(drive8);

        drive8.SetIecActivity(true);
        Assert.True(drive8.LedOn);
        Assert.Contains(nameof(drive8.LedOn), changes);

        drive8.SetIecActivity(false);
        Assert.False(drive8.LedOn);

        tape.SetIecActivity(true);
        Assert.False(tape.LedOn);

        drive8.SetDriveLed(true);
        Assert.True(drive8.LedOn);

        tape.SetDriveLed(true);
        Assert.False(tape.LedOn);
    }

    /// <summary>
    /// FR: FR-UISETTINGS-001, TR: TR-UIAXAML-VIEWS-001, TEST-UISETTINGS-001.
    /// Use case: The Settings Warp checkbox binds to the derived IsWarpMode; when
    /// a Revert (or host settings load) changes the underlying limiter state, the
    /// checkbox must update, which requires PropertyChanged for the derived
    /// IsWarpMode (not only LimiterEnabled).
    /// Acceptance: With Warp turned on locally, RevertSettings restores the
    /// limiter, sets IsWarpMode back to false, and raises PropertyChanged for
    /// IsWarpMode so the bound checkbox refreshes.
    /// </summary>
    [Fact]
    public void RevertSettings_RaisesIsWarpModeNotification()
    {
        var viewModel = new AttachPanelViewModel(new DisconnectedHostProtocolClient());
        viewModel.IsWarpMode = true;
        Assert.True(viewModel.IsWarpMode);

        var changes = TrackPropertyChanges(viewModel);
        viewModel.RevertSettings();

        Assert.False(viewModel.IsWarpMode);
        Assert.Contains(nameof(viewModel.IsWarpMode), changes);
    }

    /// <summary>
    /// FR: FR-DRVTRUE-001, TR: TR-MVVM-001, TEST-DRVTRUE-001.
    /// Use case: Toggling a drive's True Drive (from the card checkbox or the
    /// menu) must drive the host to rebuild the session as a true-drive rig for
    /// that device; because only one true-drive rig is supported, enabling one
    /// drive disables the other.
    /// Acceptance: Setting Drive 8 TrueDrive routes SetTrueDriveAsync(true, 8);
    /// then setting Drive 9 TrueDrive turns Drive 8 off and routes
    /// SetTrueDriveAsync(true, 9); clearing it routes SetTrueDriveAsync(false, _).
    /// </summary>
    [Fact]
    public async Task TrueDriveToggle_DrivesHostSessionAndIsSingleSelection()
    {
        var host = Substitute.For<IHostProtocolClient>();
        host.SetTrueDriveAsync(Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        var viewModel = new AttachPanelViewModel(host);
        var drive8 = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Drive8);
        var drive9 = viewModel.Slots.Single(slot => slot.Slot == MediaSlot.Drive9);

        drive8.TrueDrive = true;
        await host.Received(1).SetTrueDriveAsync(true, 8, Arg.Any<string?>(), Arg.Any<CancellationToken>());

        drive9.TrueDrive = true;
        Assert.False(drive8.TrueDrive); // single true-drive
        await host.Received(1).SetTrueDriveAsync(true, 9, Arg.Any<string?>(), Arg.Any<CancellationToken>());

        drive9.TrueDrive = false;
        await host.Received(1).SetTrueDriveAsync(false, Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FR: FR-INP-001 / FR-CFG-006, TR: TR-MVVM-001.
    /// Use case: After the user picks a keyboard map the host round-trips a fresh
    /// KeyboardMapDto (IsSelected = true). The bound ComboBox can only show the
    /// selection when SelectedKeyboardMap is the SAME instance present in the
    /// KeyboardMaps source; a foreign instance leaves the combo blank (the reported
    /// regression: the combo briefly shows the pick then blanks).
    /// Acceptance: After SelectKeyboardMapAsync, SelectedKeyboardMap has the chosen
    /// Id and is reference-contained in KeyboardMaps.
    /// </summary>
    [Fact]
    public async Task SelectKeyboardMap_SelectsListInstance_SoComboDoesNotBlank()
    {
        var host = CreateKeyboardMapHost();
        var viewModel = new AttachPanelViewModel(host);
        await viewModel.RefreshKeyboardMapsAsync(TestContext.Current.CancellationToken);

        await viewModel.SelectKeyboardMapAsync("c64:gtk3_sym", TestContext.Current.CancellationToken);

        Assert.NotNull(viewModel.SelectedKeyboardMap);
        Assert.Equal("c64:gtk3_sym", viewModel.SelectedKeyboardMap!.Id);
        Assert.Contains(viewModel.SelectedKeyboardMap, viewModel.KeyboardMaps);
    }

    /// <summary>
    /// FR: FR-INP-001 / FR-CFG-006, TR: TR-MVVM-001.
    /// Use case: On startup the persisted keyboard map id must be restored as the
    /// active selection and stay visible in the combo (the reported regression: the
    /// selection reverted to the first map after restart).
    /// Acceptance: After ApplyPersistedTransientAsync with a saved id,
    /// SelectedKeyboardMap has that id and is reference-contained in KeyboardMaps.
    /// </summary>
    [Fact]
    public async Task RestorePersistedTransient_RestoresKeyboardMapSelection()
    {
        var host = CreateKeyboardMapHost();
        var viewModel = new AttachPanelViewModel(host);
        await viewModel.RefreshKeyboardMapsAsync(TestContext.Current.CancellationToken);

        await viewModel.ApplyPersistedTransientAsync(
            new PersistedTransient([], "c64:keyrah", null),
            TestContext.Current.CancellationToken);

        Assert.NotNull(viewModel.SelectedKeyboardMap);
        Assert.Equal("c64:keyrah", viewModel.SelectedKeyboardMap!.Id);
        Assert.Contains(viewModel.SelectedKeyboardMap, viewModel.KeyboardMaps);
    }

    private static KeyboardMapDto[] BuildKeyboardMaps() =>
    [
        new KeyboardMapDto("c64:gtk3_pos", "GTK3 pos", "c64", "builtin", "", true, true),
        new KeyboardMapDto("c64:gtk3_sym", "GTK3 sym", "c64", "builtin", "", false, true),
        new KeyboardMapDto("c64:keyrah", "GTK3 keyrah", "c64", "builtin", "", false, true),
    ];

    private static IHostProtocolClient CreateKeyboardMapHost()
    {
        var host = Substitute.For<IHostProtocolClient>();
        var maps = BuildKeyboardMaps();

        host.ListKeyboardMapsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ListKeyboardMapsResponse>(new ListKeyboardMapsResponse(RpcStatus.Ok(), maps)));

        // RefreshAsync (invoked from ApplyPersistedTransientAsync) lists media and
        // settings; return minimal Ok responses so the restore path runs end-to-end.
        host.ListMediaAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ListMediaResponse>(new ListMediaResponse(RpcStatus.Ok(), [])));
        host.ListSettingsProfilesAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<ListSettingsProfilesResponse>(new ListSettingsProfilesResponse(RpcStatus.Ok(), [])));
        host.GetSettingsAsync(Arg.Any<CancellationToken>())
            .Returns(new ValueTask<GetSettingsResponse>(new GetSettingsResponse(RpcStatus.Ok(), null)));

        // The real host returns a FRESH KeyboardMapDto (IsSelected = true) for the
        // chosen id - a different instance than the one in the list.
        host.SetKeyboardMapAsync(
                Arg.Any<string>(),
                Arg.Any<byte[]?>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var id = call.ArgAt<string>(0);
                var template = maps.FirstOrDefault(map => map.Id == id) ?? maps[0];
                return new ValueTask<KeyboardMapResponse>(new KeyboardMapResponse(
                    RpcStatus.Ok(),
                    new KeyboardMapDto(id, template.DisplayName, "c64", "builtin", "", true, true)));
            });

        return host;
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

    private static EmulatorStatusDto CreateStatus(bool iecActive, long transitionCount)
        => new(
            "session",
            "C64",
            EmulatorRunState.Running,
            1234,
            new MachineStateDto(0, 0, 0, 0, 0, 0xC000, 1234),
            PowerState: "On",
            LimiterRatePercent: 100,
            MeasuredFps: 50,
            FrameCount: 60,
            NominalClockHz: 1_000_000,
            EffectiveClockHz: 1_000_000,
            EffectiveClockPercent: 100,
            Pc: 0xC000,
            IecBusActive: iecActive,
            IecBusTransitionCount: transitionCount,
            IecBusActivityState: iecActive ? "Active" : "Idle");
}
