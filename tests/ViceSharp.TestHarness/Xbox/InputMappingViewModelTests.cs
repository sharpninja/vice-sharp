namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Linq;
using ViceSharp.Xbox.Input;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S30 (IMPL-XBOXUWP-030). The read-only input-mapping page
/// ViewModel (<see cref="InputMappingViewModel"/>) in <c>ViceSharp.Xbox.ViewModels</c>:
/// a bindable, ordered display of the LOCKED controller mapping (the joystick bundle
/// plus the <see cref="BindingProfile.Default"/> system buttons). It is display-only;
/// remap persistence is owned by other slices (S12 / S26).
/// </summary>
/// <remarks>
/// The joystick mapping is the S9 locked scheme: left stick + D-pad and A drive JOY2
/// (JOY2 fire), the right stick and B drive JOY1 (JOY1 fire), and Guide is reserved.
/// The system buttons are the S10 <see cref="BindingProfile.Default"/> table (Menu,
/// View, X, Y, LB, RB, LT, L3). Pure MVVM (TR-MVVM-001): no engine, host, or XAML
/// reference.
/// </remarks>
[Trait("Category", "Xbox")]
public sealed class InputMappingViewModelTests
{
    /// <summary>
    /// IMPL-XBOXUWP-030 locked-joystick-rows guard.
    /// Use case: the page must show the LOCKED joystick bundle so the player can see that
    /// the left stick + D-pad and A drive JOY2, and the right stick and B drive JOY1.
    /// Acceptance: <see cref="InputMappingViewModel.Rows"/> contains a row mapping "A" to
    /// a JOY2 fire label, a row mapping the left stick / D-pad to JOY2, a row mapping "B"
    /// to a JOY1 fire label, and a row mapping the right stick to JOY1.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Rows_IncludeLockedJoystickMapping()
    {
        var vm = new InputMappingViewModel();
        var rows = vm.Rows;

        Assert.NotEmpty(rows);

        // A -> JOY2 fire.
        Assert.Contains(rows, r =>
            r.InputLabel.Contains("A", StringComparison.Ordinal)
            && r.ActionLabel.Contains("JOY2", StringComparison.Ordinal)
            && r.ActionLabel.Contains("fire", StringComparison.OrdinalIgnoreCase));

        // Left stick + D-pad -> JOY2 (movement).
        Assert.Contains(rows, r =>
            r.InputLabel.Contains("Left stick", StringComparison.OrdinalIgnoreCase)
            && r.InputLabel.Contains("D-pad", StringComparison.OrdinalIgnoreCase)
            && r.ActionLabel.Contains("JOY2", StringComparison.Ordinal));

        // B -> JOY1 fire.
        Assert.Contains(rows, r =>
            r.InputLabel.Contains("B", StringComparison.Ordinal)
            && r.ActionLabel.Contains("JOY1", StringComparison.Ordinal)
            && r.ActionLabel.Contains("fire", StringComparison.OrdinalIgnoreCase));

        // Right stick -> JOY1 (movement).
        Assert.Contains(rows, r =>
            r.InputLabel.Contains("Right stick", StringComparison.OrdinalIgnoreCase)
            && r.ActionLabel.Contains("JOY1", StringComparison.Ordinal));
    }

    /// <summary>
    /// IMPL-XBOXUWP-030 Guide-reserved guard.
    /// Use case: the Guide button is reserved by the platform and is never mappable, so
    /// the page shows it as reserved rather than implying it can be bound.
    /// Acceptance: <see cref="InputMappingViewModel.Rows"/> contains a "Guide" row whose
    /// action label reads as reserved.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Rows_ShowGuideAsReserved()
    {
        var vm = new InputMappingViewModel();

        Assert.Contains(vm.Rows, r =>
            r.InputLabel.Contains("Guide", StringComparison.OrdinalIgnoreCase)
            && r.ActionLabel.Contains("Reserved", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// IMPL-XBOXUWP-030 default-system-button-rows guard.
    /// Use case: the page must show every default system-button binding so the player
    /// learns the locked control scheme (Menu -> main menu, View -> virtual keyboard,
    /// X -> autostart, Y -> reset, LB -> quick save, RB -> quick load, LT -> warp,
    /// L3 -> swap ports).
    /// Acceptance: <see cref="InputMappingViewModel.Rows"/> has a row for each of the
    /// eight <see cref="BindingProfile.Default"/> inputs (Menu, View, X, Y, LB, RB, LT,
    /// L3) whose action label matches the bound function.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Rows_IncludeDefaultSystemButtonBindings()
    {
        var vm = new InputMappingViewModel();
        var rows = vm.Rows;

        AssertRow(rows, "Menu", "menu");
        AssertRow(rows, "View", "keyboard");
        AssertRow(rows, "X", "Autostart");
        AssertRow(rows, "Y", "reset");
        AssertRow(rows, "LB", "save");
        AssertRow(rows, "RB", "load");
        AssertRow(rows, "LT", "Warp");
        AssertRow(rows, "L3", "Swap");
    }

    /// <summary>
    /// IMPL-XBOXUWP-030 non-empty / stable guard.
    /// Use case: the rows are a fixed, read-only description of the locked scheme, so the
    /// list must be non-empty and identical every time it is read (no per-read rebuild
    /// that could reorder or vary).
    /// Acceptance: <see cref="InputMappingViewModel.Rows"/> is non-empty and equal
    /// element-by-element across two reads.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void Rows_AreNonEmptyAndStable()
    {
        var vm = new InputMappingViewModel();

        Assert.NotEmpty(vm.Rows);
        Assert.True(vm.Rows.SequenceEqual(vm.Rows));

        var again = new InputMappingViewModel();
        Assert.True(vm.Rows.SequenceEqual(again.Rows));
    }

    /// <summary>
    /// IMPL-XBOXUWP-030 open-virtual-keyboard-intent guard.
    /// Use case: from the mapping page the player can jump to the on-screen virtual
    /// keyboard overlay; the ViewModel exposes that as an intent event (still read-only:
    /// no remap here).
    /// Acceptance: <see cref="InputMappingViewModel.RequestOpenVirtualKeyboard"/> raises
    /// <see cref="InputMappingViewModel.OpenVirtualKeyboardRequested"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void RequestOpenVirtualKeyboard_RaisesIntent()
    {
        var vm = new InputMappingViewModel();
        int raised = 0;
        vm.OpenVirtualKeyboardRequested += (_, _) => raised++;

        vm.RequestOpenVirtualKeyboard();

        Assert.Equal(1, raised);
    }

    /// <summary>
    /// FEAT-XCTRLBIND-001: remappable system buttons can be rebound, saved, reloaded;
    /// Menu/View stay locked; last assignment wins for a shared command.
    /// </summary>
    [Fact]
    public void Rebind_Save_Load_AndReset_Work_AndLocksHold()
    {
        var store = new InMemoryBindingStore();
        var vm = new InputMappingViewModel(store);

        Assert.False(vm.TryRebind(BindableInput.Menu, AppCommand.ColdReset));
        Assert.False(vm.TryRebind(BindableInput.View, AppCommand.ColdReset));
        Assert.True(vm.TryRebind(BindableInput.X, AppCommand.ColdReset));
        // Last assignment wins: Y previously had WarmReset; give WarmReset to X's prior owner path
        // by rebinding Y to ColdReset - X should drop ColdReset.
        Assert.True(vm.TryRebind(BindableInput.Y, AppCommand.ColdReset));
        Assert.DoesNotContain(vm.Profile.Gameplay, b => b.Input == BindableInput.X && b.Command == AppCommand.ColdReset);
        Assert.Contains(vm.Profile.Gameplay, b => b.Input == BindableInput.Y && b.Command == AppCommand.ColdReset);

        vm.Save();
        var reloaded = new InputMappingViewModel(store);
        Assert.Contains(reloaded.Profile.Gameplay, b => b.Input == BindableInput.Y && b.Command == AppCommand.ColdReset);

        reloaded.ResetToDefaults();
        Assert.Contains(reloaded.Profile.Gameplay, b => b.Input == BindableInput.X && b.Command == AppCommand.AutostartDrive8);
        Assert.Contains(reloaded.Rows, r => r.InputLabel == "Menu" && r.IsLocked);
        Assert.Contains(reloaded.Rows, r => r.InputLabel == "X" && !r.IsLocked);
    }

    private static void AssertRow(
        System.Collections.Generic.IReadOnlyList<InputMappingRow> rows,
        string inputLabel,
        string actionFragment)
    {
        Assert.Contains(rows, r =>
            string.Equals(r.InputLabel, inputLabel, StringComparison.Ordinal)
            && r.ActionLabel.Contains(actionFragment, StringComparison.OrdinalIgnoreCase));
    }
}
