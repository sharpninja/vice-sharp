namespace ViceSharp.Xbox.ViewModels;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ViceSharp.Xbox.Input;

/// <summary>
/// PLAN-XBOXUWP S30 + FEAT-XCTRLBIND-001. Controls-page ViewModel: shows the locked
/// joystick scheme and the system-button profile, and lets the player remap unlocked
/// buttons (not Menu/View/A/B/Guide) through <see cref="IBindingStore"/>.
/// </summary>
/// <remarks>
/// Locked forever: left stick+D-pad / right stick / A fire / B fire / Guide, plus Menu
/// and View (plan default: menu control fixed). Remappable: X, Y, LB, RB, LT, RT, L3, R3.
/// Last assignment wins: rebinding a command clears any other input that held it.
/// Pure MVVM (TR-MVVM-001): portable Input types only.
/// </remarks>
public sealed class InputMappingViewModel : INotifyPropertyChanged
{
    /// <summary>Inputs the player may rebind on the Controls page.</summary>
    private static readonly BindableInput[] RemappableInputs =
    {
        BindableInput.X,
        BindableInput.Y,
        BindableInput.LeftShoulder,
        BindableInput.RightShoulder,
        BindableInput.LeftTrigger,
        BindableInput.RightTrigger,
        BindableInput.LeftThumbstick,
        BindableInput.RightThumbstick,
    };

    /// <summary>Commands offered when rebinding an unlocked input.</summary>
    public static readonly AppCommand[] RemappableCommands =
    {
        AppCommand.AutostartDrive8,
        AppCommand.WarmReset,
        AppCommand.ColdReset,
        AppCommand.QuickSaveState,
        AppCommand.QuickLoadState,
        AppCommand.WarpHoldOn,
        AppCommand.SwapJoystickPorts,
        AppCommand.ToggleWarp,
        AppCommand.None,
    };

    private readonly IBindingStore _store;
    private BindingProfile _profile;
    private List<InputMappingRow> _rows = new();
    private string _statusText = string.Empty;

    /// <summary>
    /// Creates the ViewModel, loading the profile from <paramref name="store"/> (or an
    /// in-memory store seeded with defaults when null).
    /// </summary>
    public InputMappingViewModel(IBindingStore? store = null)
    {
        _store = store ?? new InMemoryBindingStore();
        _profile = _store.Load();
        RebuildRows();
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raised by <see cref="RequestOpenVirtualKeyboard"/>: the shell should open the
    /// on-screen virtual-keyboard overlay.
    /// </summary>
    public event EventHandler? OpenVirtualKeyboardRequested;

    /// <summary>Ordered mapping rows (locked joystick + system buttons).</summary>
    public IReadOnlyList<InputMappingRow> Rows => _rows;

    /// <summary>Status line for save/reset feedback.</summary>
    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
                return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>The live editable profile (not yet necessarily saved).</summary>
    public BindingProfile Profile => _profile;

    /// <summary>Raises the <see cref="OpenVirtualKeyboardRequested"/> intent.</summary>
    public void RequestOpenVirtualKeyboard() =>
        OpenVirtualKeyboardRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Rebinds <paramref name="input"/> to <paramref name="command"/> when the input is
    /// remappable. Menu/View and unknown inputs are ignored. Last assignment wins: any
    /// other input carrying the same command is cleared (bound to <see cref="AppCommand.None"/>).
    /// </summary>
    public bool TryRebind(BindableInput input, AppCommand command)
    {
        if (!IsRemappable(input))
            return false;

        var activation = command switch
        {
            AppCommand.WarpHoldOn => BindingActivation.Hold,
            AppCommand.None => BindingActivation.Press,
            _ => BindingActivation.Press,
        };

        var next = _profile;
        if (command != AppCommand.None)
        {
            // Clear any other input that already owns this command (last assignment wins).
            foreach (var row in next.Gameplay)
            {
                if (row.Input != input && row.Command == command)
                    next = next.WithBinding(row.Input, AppCommand.None, BindingActivation.Press);
            }
        }

        next = next.WithBinding(input, command, activation);
        // Drop None rows so the profile stays sparse (unbound inputs absent).
        next = next with
        {
            Gameplay = next.Gameplay.Where(b => b.Command != AppCommand.None).ToArray(),
        };

        _profile = next;
        RebuildRows();
        StatusText = $"Rebound {InputLabel(input)}.";
        return true;
    }

    /// <summary>Persists the current profile through the binding store.</summary>
    public void Save()
    {
        _store.Save(_profile);
        StatusText = "Controls saved.";
    }

    /// <summary>Resets to <see cref="BindingProfile.Default"/> and persists.</summary>
    public void ResetToDefaults()
    {
        _store.ResetToDefaults();
        _profile = _store.Load();
        RebuildRows();
        StatusText = "Controls reset to defaults.";
    }

    /// <summary>True when the input can be rebound on this page.</summary>
    public static bool IsRemappable(BindableInput input) =>
        Array.IndexOf(RemappableInputs, input) >= 0;

    private void RebuildRows()
    {
        _rows = BuildRows(_profile);
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(Profile));
    }

    private static List<InputMappingRow> BuildRows(BindingProfile profile)
    {
        var rows = new List<InputMappingRow>
        {
            // Locked S9 joystick bundle (not rebindable).
            new("Left stick + D-pad", "JOY2 (move)", IsLocked: true),
            new("Right stick", "JOY1 (move)", IsLocked: true),
            new("A", "JOY2 fire", IsLocked: true),
            new("B", "JOY1 fire", IsLocked: true),
            new("Guide", "Reserved (system button)", IsLocked: true),
            // Menu / View fixed per plan (menu control).
            new("Menu", "Open main menu", IsLocked: true, Input: BindableInput.Menu),
            new("View", "Toggle virtual keyboard", IsLocked: true, Input: BindableInput.View),
        };

        // Remappable system buttons: always show every remappable input (unbound => "Unbound").
        foreach (var input in RemappableInputs)
        {
            var binding = profile.Gameplay.FirstOrDefault(b => b.Input == input);
            var action = binding is null || binding.Command == AppCommand.None
                ? "Unbound"
                : ActionLabel(binding.Command);
            rows.Add(new InputMappingRow(InputLabel(input), action, IsLocked: false, Input: input));
        }

        return rows;
    }

    /// <summary>Human-readable controller label for a bindable system input.</summary>
    public static string InputLabel(BindableInput input) => input switch
    {
        BindableInput.Menu => "Menu",
        BindableInput.View => "View",
        BindableInput.X => "X",
        BindableInput.Y => "Y",
        BindableInput.LeftShoulder => "LB",
        BindableInput.RightShoulder => "RB",
        BindableInput.LeftTrigger => "LT",
        BindableInput.RightTrigger => "RT",
        BindableInput.LeftThumbstick => "L3",
        BindableInput.RightThumbstick => "R3",
        _ => input.ToString(),
    };

    /// <summary>Human-readable action label for a bound application command.</summary>
    public static string ActionLabel(AppCommand command) => command switch
    {
        AppCommand.OpenMainMenu => "Open main menu",
        AppCommand.ToggleVirtualKeyboard => "Toggle virtual keyboard",
        AppCommand.AutostartDrive8 => "Autostart drive 8",
        AppCommand.WarmReset => "Warm reset",
        AppCommand.ColdReset => "Cold reset",
        AppCommand.QuickSaveState => "Quick save state",
        AppCommand.QuickLoadState => "Quick load state",
        AppCommand.WarpHoldOn => "Warp (hold)",
        AppCommand.SwapJoystickPorts => "Swap joystick ports",
        AppCommand.ToggleWarp => "Toggle warp",
        AppCommand.None => "Unbound",
        _ => command.ToString(),
    };

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
