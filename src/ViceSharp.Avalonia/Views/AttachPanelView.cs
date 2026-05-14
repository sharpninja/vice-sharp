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

        var limiter = new TextBlock
        {
            Text = $"Limiter {ViewModel.LimiterRatePercent:0}%",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold
        };
        stack.Children.Add(limiter);

        var slider = new Slider
        {
            Minimum = 10,
            Maximum = 400,
            Value = ViewModel.LimiterRatePercent,
            TickFrequency = 10,
            IsSnapToTickEnabled = true
        };
        slider.PropertyChanged += (_, args) =>
        {
            if (args.Property == RangeBase.ValueProperty)
            {
                ViewModel.LimiterRatePercent = slider.Value;
                limiter.Text = $"Limiter {ViewModel.LimiterRatePercent:0}%";
            }
        };
        stack.Children.Add(slider);

        var apply = new Button
        {
            Content = "Apply",
            Padding = new Thickness(10, 5)
        };
        apply.Click += async (_, _) => await ViewModel.ApplyLimiterAsync().ConfigureAwait(true);
        stack.Children.Add(apply);

        stack.Children.Add(new TextBlock
        {
            Text = "Display crop and scale controls will use this tab.",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
            TextWrapping = TextWrapping.Wrap
        });

        return stack;
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
