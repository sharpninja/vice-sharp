using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia.Views;

public sealed class AttachPanelView : UserControl
{
    private static readonly Thickness PanelPadding = new(10);
    private static readonly Thickness SlotPadding = new(8);
    private static readonly IBrush SlotBorderBrush = new SolidColorBrush(Color.FromRgb(74, 78, 86));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(185, 57, 57));

    public AttachPanelView(AttachPanelViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        DataContext = viewModel;
        Content = BuildContent();
    }

    public AttachPanelViewModel ViewModel { get; }

    public Func<AttachSlotViewModel, Task<string?>>? PickFileAsync { get; set; }

    public Func<Task<string?>>? PickKeyboardMapFileAsync { get; set; }

    public Action? PopOutMonitorRequested { get; set; }

    private Control BuildContent()
    {
        var root = new DockPanel
        {
            MinWidth = 260,
            MaxWidth = 360,
            Background = new SolidColorBrush(Color.FromRgb(31, 34, 39))
        };

        var header = new StackPanel
        {
            Margin = PanelPadding,
            Spacing = 8
        };

        header.Children.Add(new TextBlock
        {
            Text = "Attach",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold
        });

        var dockControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        dockControls.Children.Add(CreateDockButton("Left", () => ViewModel.DockLeft()));
        dockControls.Children.Add(CreateDockButton("Right", () => ViewModel.DockRight()));
        header.Children.Add(dockControls);

        var status = new TextBlock
        {
            Text = ViewModel.StatusText,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
            TextWrapping = TextWrapping.Wrap
        };
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.StatusText))
                status.Text = ViewModel.StatusText;
        };
        header.Children.Add(status);

        var tabs = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        tabs.Children.Add(CreateTabButton("Peripherals", () => ViewModel.ShowPeripherals()));
        tabs.Children.Add(CreateTabButton("Settings", () => ViewModel.ShowSettings()));
        tabs.Children.Add(CreateTabButton("Monitor", () => ViewModel.ShowMonitor()));
        header.Children.Add(tabs);

        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        var content = new ContentControl
        {
            Content = CreateActiveTabContent()
        };

        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.ActiveTab))
                content.Content = CreateActiveTabContent();
        };

        root.Children.Add(content);
        return root;
    }

    private Button CreateDockButton(string label, Action action)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(10, 5),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        button.Click += (_, _) => action();
        return button;
    }

    private Button CreateTabButton(string label, Action action)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(7, 4),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        button.Click += (_, _) => action();
        return button;
    }

    private Control CreateActiveTabContent()
    {
        return ViewModel.ActiveTab switch
        {
            SidebarTab.Settings => CreateSettingsPanel(),
            SidebarTab.Monitor => CreateMonitorPanel(includePopOut: true),
            _ => CreatePeripheralsPanel()
        };
    }

    private Control CreatePeripheralsPanel()
    {
        var stack = new StackPanel
        {
            Margin = new Thickness(10, 0, 10, 10),
            Spacing = 8
        };

        stack.Children.Add(CreateKeyboardMapPanel());

        foreach (var slot in ViewModel.Slots)
            stack.Children.Add(CreateSlotPanel(slot));

        return new ScrollViewer { Content = stack };
    }

    private Control CreateKeyboardMapPanel()
    {
        var panel = new Border
        {
            BorderBrush = SlotBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = SlotPadding,
            Background = new SolidColorBrush(Color.FromRgb(39, 43, 49))
        };

        var stack = new StackPanel { Spacing = 7 };
        panel.Child = stack;
        stack.Children.Add(new TextBlock
        {
            Text = "Keyboard Map",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        });

        var selector = new ComboBox
        {
            ItemsSource = ViewModel.KeyboardMaps,
            SelectedItem = ViewModel.SelectedKeyboardMap,
            MinHeight = 30,
            ItemTemplate = new FuncDataTemplate<KeyboardMapDto>((map, _) => new TextBlock
            {
                Text = map?.DisplayName ?? string.Empty
            })
        };
        selector.SelectionChanged += (_, _) =>
        {
            if (selector.SelectedItem is KeyboardMapDto map)
                ViewModel.SelectedKeyboardMap = map;
        };
        stack.Children.Add(selector);

        var status = new TextBlock
        {
            Text = ViewModel.KeyboardMapStatus,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
            TextWrapping = TextWrapping.Wrap
        };
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.KeyboardMapStatus))
                status.Text = ViewModel.KeyboardMapStatus;
            else if (args.PropertyName == nameof(AttachPanelViewModel.SelectedKeyboardMap))
                selector.SelectedItem = ViewModel.SelectedKeyboardMap;
        };
        stack.Children.Add(status);

        var custom = new Button
        {
            Content = "Custom VKM",
            Padding = new Thickness(9, 5)
        };
        custom.Click += async (_, _) => await SelectCustomKeyboardMapAsync().ConfigureAwait(true);
        stack.Children.Add(custom);

        return panel;
    }

    private Control CreateSettingsPanel()
    {
        var stack = new StackPanel
        {
            Margin = new Thickness(10, 0, 10, 10),
            Spacing = 10
        };

        stack.Children.Add(CreateProfileSection());
        stack.Children.Add(CreateLimiterSection());
        stack.Children.Add(CreateDisplaySection());
        stack.Children.Add(CreateStatusSection());
        stack.Children.Add(CreateSettingsActionRow());
        stack.Children.Add(CreateSettingsValidationPanel());

        var status = new TextBlock
        {
            Text = ViewModel.SettingsStatusText,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
            TextWrapping = TextWrapping.Wrap
        };
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.SettingsStatusText))
                status.Text = ViewModel.SettingsStatusText;
        };
        stack.Children.Add(status);

        return new ScrollViewer { Content = stack };
    }

    private Control CreateProfileSection()
    {
        var stack = CreateSection("Machine");
        var selector = new ComboBox
        {
            ItemsSource = ViewModel.MachineProfiles,
            SelectedItem = ViewModel.SelectedMachineProfile,
            MinHeight = 30,
            ItemTemplate = new FuncDataTemplate<MachineProfileOption>((profile, _) => new TextBlock
            {
                Text = profile?.DisplayName ?? string.Empty
            })
        };
        selector.SelectionChanged += (_, _) =>
        {
            if (selector.SelectedItem is MachineProfileOption profile)
                ViewModel.SelectedMachineProfile = profile;
        };
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.SelectedMachineProfile))
                selector.SelectedItem = ViewModel.SelectedMachineProfile;
        };
        stack.Children.Add(selector);
        var status = new TextBlock
        {
            Text = ViewModel.SelectedMachineProfile.StatusText,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
            TextWrapping = TextWrapping.Wrap
        };
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.SelectedMachineProfile))
                status.Text = ViewModel.SelectedMachineProfile.StatusText;
        };
        stack.Children.Add(status);
        return stack;
    }

    private Control CreateLimiterSection()
    {
        var stack = CreateSection("Limiter");
        var limiter = new TextBlock
        {
            Text = $"Target {ViewModel.LimiterRatePercent:0}% ({AttachPanelViewModel.LimiterMinimumPercent:0}-{AttachPanelViewModel.LimiterMaximumPercent:0}%)",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold
        };
        stack.Children.Add(limiter);
        var slider = new Slider
        {
            Minimum = AttachPanelViewModel.LimiterMinimumPercent,
            Maximum = AttachPanelViewModel.LimiterMaximumPercent,
            Value = ViewModel.LimiterRatePercent,
            TickFrequency = 10,
            IsSnapToTickEnabled = true
        };
        slider.PropertyChanged += (_, args) =>
        {
            if (args.Property == RangeBase.ValueProperty)
            {
                ViewModel.LimiterRatePercent = slider.Value;
                limiter.Text = $"Target {ViewModel.LimiterRatePercent:0}% ({AttachPanelViewModel.LimiterMinimumPercent:0}-{AttachPanelViewModel.LimiterMaximumPercent:0}%)";
            }
        };
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.LimiterRatePercent))
            {
                slider.Value = ViewModel.LimiterRatePercent;
                limiter.Text = $"Target {ViewModel.LimiterRatePercent:0}% ({AttachPanelViewModel.LimiterMinimumPercent:0}-{AttachPanelViewModel.LimiterMaximumPercent:0}%)";
            }
        };
        stack.Children.Add(slider);
        // Dedicated Warp toggle for VICE-like uncapped speed (ideal for profiling with dotTrace)
        stack.Children.Add(CreateBooleanRow(
            "Warp Mode (Alt+W)",
            () => ViewModel.IsWarpMode,
            value => ViewModel.IsWarpMode = value,
            nameof(AttachPanelViewModel.IsWarpMode)));

        var warpNote = new TextBlock
        {
            Text = "Warp = uncapped speed (same as VICE -warp). Use with dotTrace for profiling.",
            FontSize = 11,
            Foreground = Brushes.OrangeRed,
            FontWeight = FontWeight.SemiBold
        };
        stack.Children.Add(warpNote);
        return stack;
    }

    private Control CreateDisplaySection()
    {
        var stack = CreateSection("Display");
        stack.Children.Add(CreateComboRow(
            "Renderer",
            ViewModel.RendererModes,
            () => ViewModel.SelectedRenderer,
            value => ViewModel.SelectedRenderer = value,
            nameof(AttachPanelViewModel.SelectedRenderer)));
        stack.Children.Add(CreateComboRow(
            "Scale",
            ViewModel.DisplayScales,
            () => ViewModel.SelectedDisplayScale,
            value => ViewModel.SelectedDisplayScale = value,
            nameof(AttachPanelViewModel.SelectedDisplayScale)));
        stack.Children.Add(CreateComboRow(
            "Crop",
            ViewModel.CropModes,
            () => ViewModel.SelectedCropMode,
            value => ViewModel.SelectedCropMode = value,
            nameof(AttachPanelViewModel.SelectedCropMode)));
        stack.Children.Add(CreateComboRow(
            "Aspect",
            ViewModel.AspectModes,
            () => ViewModel.SelectedAspectMode,
            value => ViewModel.SelectedAspectMode = value,
            nameof(AttachPanelViewModel.SelectedAspectMode)));
        stack.Children.Add(CreateComboRow(
            "Palette",
            ViewModel.PaletteModes,
            () => ViewModel.SelectedPalette,
            value => ViewModel.SelectedPalette = value,
            nameof(AttachPanelViewModel.SelectedPalette)));
        return stack;
    }

    private Control CreateStatusSection()
    {
        var stack = CreateSection("Audio / Input / Resources");
        stack.Children.Add(CreateComboRow(
            "Audio",
            ViewModel.AudioModes,
            () => ViewModel.SelectedAudioMode,
            value => ViewModel.SelectedAudioMode = value,
            nameof(AttachPanelViewModel.SelectedAudioMode)));
        stack.Children.Add(CreateComboRow(
            "Input",
            ViewModel.InputModes,
            () => ViewModel.SelectedInputMode,
            value => ViewModel.SelectedInputMode = value,
            nameof(AttachPanelViewModel.SelectedInputMode)));
        stack.Children.Add(CreateComboRow(
            "Primary",
            ViewModel.PrimaryJoystickPorts,
            () => ViewModel.SelectedPrimaryJoystickPort,
            value => ViewModel.SelectedPrimaryJoystickPort = value,
            nameof(AttachPanelViewModel.SelectedPrimaryJoystickPort)));
        stack.Children.Add(CreateBooleanRow(
            "Swap ports",
            () => ViewModel.SwapJoystickPorts,
            value => ViewModel.SwapJoystickPorts = value,
            nameof(AttachPanelViewModel.SwapJoystickPorts)));
        stack.Children.Add(CreateComboRow(
            "Resources",
            ViewModel.ResourceModes,
            () => ViewModel.SelectedResourceMode,
            value => ViewModel.SelectedResourceMode = value,
            nameof(AttachPanelViewModel.SelectedResourceMode)));
        return stack;
    }

    private Control CreateSettingsActionRow()
    {
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        var apply = CreateSettingsButton("Apply", async () => await ViewModel.ApplySettingsAsync(restartRequired: false).ConfigureAwait(true));
        var validate = CreateSettingsButton("Validate", async () => await ViewModel.ValidateSettingsAsync().ConfigureAwait(true));
        var revert = CreateSettingsButton("Revert", () =>
        {
            ViewModel.RevertSettings();
            return Task.CompletedTask;
        });
        var restart = CreateSettingsButton("Apply + Restart", async () => await ViewModel.ApplySettingsAsync(restartRequired: true).ConfigureAwait(true));

        void RefreshEnabled()
        {
            apply.IsEnabled = ViewModel.HasPendingSettingsChanges;
            revert.IsEnabled = ViewModel.HasPendingSettingsChanges;
            restart.IsEnabled = ViewModel.HasPendingSettingsChanges || ViewModel.RequiresRestart;
        }

        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(AttachPanelViewModel.HasPendingSettingsChanges) or nameof(AttachPanelViewModel.RequiresRestart))
                RefreshEnabled();
        };
        RefreshEnabled();

        buttons.Children.Add(validate);
        buttons.Children.Add(apply);
        buttons.Children.Add(revert);
        buttons.Children.Add(restart);
        return buttons;
    }

    private Control CreateSettingsValidationPanel()
    {
        var stack = new StackPanel { Spacing = 5 };
        var header = new TextBlock
        {
            Text = "Validation",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            IsVisible = ViewModel.HasSettingsValidationResults
        };
        stack.Children.Add(header);

        var results = new ItemsControl
        {
            ItemsSource = ViewModel.SettingsValidationResults,
            IsVisible = ViewModel.HasSettingsValidationResults,
            ItemTemplate = new FuncDataTemplate<SettingsResourceValidationDto>((resource, _) =>
            {
                var brush = resource is { IsValid: false }
                    ? ErrorBrush
                    : new SolidColorBrush(Color.FromRgb(124, 183, 136));
                return new TextBlock
                {
                    Text = resource is null
                        ? string.Empty
                        : $"{resource.ResourceKey}: {resource.Message}",
                    Foreground = brush,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 1)
                };
            })
        };
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.HasSettingsValidationResults))
            {
                header.IsVisible = ViewModel.HasSettingsValidationResults;
                results.IsVisible = ViewModel.HasSettingsValidationResults;
            }
        };
        stack.Children.Add(results);
        return stack;
    }

    private Button CreateSettingsButton(string label, Func<Task> action)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(9, 5),
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        button.Click += async (_, _) => await action().ConfigureAwait(true);
        return button;
    }

    private StackPanel CreateSection(string title)
    {
        var stack = new StackPanel
        {
            Spacing = 7,
            Margin = new Thickness(0, 0, 0, 2)
        };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        });
        return stack;
    }

    private Control CreateComboRow(
        string label,
        IEnumerable<string> items,
        Func<string> getValue,
        Action<string> setValue,
        string propertyName)
    {
        var row = new DockPanel { LastChildFill = true };
        var text = new TextBlock
        {
            Text = label,
            Width = 78,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12
        };
        DockPanel.SetDock(text, Dock.Left);
        row.Children.Add(text);

        var combo = new ComboBox
        {
            ItemsSource = items,
            SelectedItem = getValue(),
            MinHeight = 30,
            FontSize = 12
        };
        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is string value)
                setValue(value);
        };
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == propertyName)
                combo.SelectedItem = getValue();
        };
        row.Children.Add(combo);
        return row;
    }

    private Control CreateBooleanRow(
        string label,
        Func<bool> getValue,
        Action<bool> setValue,
        string propertyName)
    {
        var checkBox = new CheckBox
        {
            Content = label,
            IsChecked = getValue(),
            FontSize = 12
        };
        checkBox.Click += (_, _) => setValue(checkBox.IsChecked == true);
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == propertyName)
                checkBox.IsChecked = getValue();
        };
        return checkBox;
    }

    public Control CreateMonitorPanel(bool includePopOut)
    {
        var stack = new StackPanel
        {
            Margin = new Thickness(10, 0, 10, 10),
            Spacing = 8
        };

        var output = new TextBox
        {
            Text = ViewModel.MonitorOutput,
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 240,
            FontFamily = FontFamily.Parse("Consolas"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.MonitorOutput))
            {
                output.Text = ViewModel.MonitorOutput;
                output.CaretIndex = output.Text?.Length ?? 0;
            }
        };
        stack.Children.Add(output);

        var command = new TextBox
        {
            Text = ViewModel.MonitorCommand,
            PlaceholderText = "Command",
            MinHeight = 30,
            FontFamily = FontFamily.Parse("Consolas")
        };
        command.KeyDown += async (_, args) =>
        {
            if (args.Key == Key.Enter)
            {
                ViewModel.MonitorCommand = command.Text ?? string.Empty;
                await ViewModel.ExecuteMonitorCommandAsync().ConfigureAwait(true);
                args.Handled = true;
            }
        };
        stack.Children.Add(command);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };
        var run = new Button
        {
            Content = "Run",
            Padding = new Thickness(9, 5)
        };
        run.Click += async (_, _) =>
        {
            ViewModel.MonitorCommand = command.Text ?? string.Empty;
            await ViewModel.ExecuteMonitorCommandAsync().ConfigureAwait(true);
        };
        buttons.Children.Add(run);

        if (includePopOut)
        {
            var pop = new Button
            {
                Content = "Pop out",
                Padding = new Thickness(9, 5)
            };
            pop.Click += (_, _) => PopOutMonitorRequested?.Invoke();
            buttons.Children.Add(pop);
        }

        stack.Children.Add(buttons);
        return stack;
    }

    private Control CreateSlotPanel(AttachSlotViewModel slot)
    {
        var panel = new Border
        {
            BorderBrush = SlotBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = SlotPadding,
            Background = new SolidColorBrush(Color.FromRgb(39, 43, 49))
        };

        var stack = new StackPanel { Spacing = 7 };
        panel.Child = stack;

        var titleRow = new DockPanel();
        titleRow.Children.Add(new TextBlock
        {
            Text = slot.Title,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        });
        var kind = new TextBlock
        {
            Text = slot.MediaKind,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        DockPanel.SetDock(kind, Dock.Right);
        titleRow.Children.Add(kind);
        stack.Children.Add(titleRow);

        var status = new TextBlock
        {
            Text = slot.StatusText,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(status);

        var error = new TextBlock
        {
            Text = slot.ValidationError,
            FontSize = 12,
            Foreground = ErrorBrush,
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(error);

        var readOnly = new CheckBox
        {
            Content = "Read only",
            FontSize = 12,
            IsChecked = slot.IsReadOnly
        };
        readOnly.Click += (_, _) => slot.IsReadOnly = readOnly.IsChecked == true;
        slot.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachSlotViewModel.StatusText))
                status.Text = slot.StatusText;
            else if (args.PropertyName == nameof(AttachSlotViewModel.ValidationError))
                error.Text = slot.ValidationError;
            else if (args.PropertyName == nameof(AttachSlotViewModel.IsReadOnly))
                readOnly.IsChecked = slot.IsReadOnly;
        };
        stack.Children.Add(readOnly);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        var attach = new Button
        {
            Content = "Attach",
            Padding = new Thickness(9, 5)
        };
        attach.Click += async (_, _) => await AttachFromPickerAsync(slot).ConfigureAwait(true);
        buttons.Children.Add(attach);

        var eject = new Button
        {
            Content = "Eject",
            Padding = new Thickness(9, 5)
        };
        eject.Click += async (_, _) => await ViewModel.EjectAsync(slot).ConfigureAwait(true);
        buttons.Children.Add(eject);

        stack.Children.Add(buttons);

        var recent = new ComboBox
        {
            PlaceholderText = "Recent",
            ItemsSource = slot.RecentFiles,
            FontSize = 12,
            MinHeight = 28
        };
        recent.SelectionChanged += async (_, _) =>
        {
            if (recent.SelectedItem is string filePath)
            {
                recent.SelectedItem = null;
                await ViewModel.AttachAsync(slot, filePath).ConfigureAwait(true);
            }
        };
        stack.Children.Add(recent);

        return panel;
    }

    private async Task AttachFromPickerAsync(AttachSlotViewModel slot)
    {
        if (PickFileAsync is null)
        {
            slot.MarkError("File picker is unavailable.");
            return;
        }

        var filePath = await PickFileAsync(slot).ConfigureAwait(true);
        if (!string.IsNullOrWhiteSpace(filePath))
            await ViewModel.AttachAsync(slot, filePath).ConfigureAwait(true);
    }

    private async Task SelectCustomKeyboardMapAsync()
    {
        if (PickKeyboardMapFileAsync is null)
            return;

        var filePath = await PickKeyboardMapFileAsync().ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        var payload = await File.ReadAllBytesAsync(filePath).ConfigureAwait(true);
        await ViewModel.SelectCustomKeyboardMapAsync(filePath, payload).ConfigureAwait(true);
    }
}
