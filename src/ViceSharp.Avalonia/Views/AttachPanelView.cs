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

    public AttachPanelView(AttachPanelViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ViewModel = viewModel;
        DataContext = viewModel;
        Content = BuildContent();
    }

    public AttachPanelViewModel ViewModel { get; }

    /// <summary>Media file picker; forwarded to the view-model so the reusable
    /// <see cref="PeripheralCardView"/> can request an attach (FR-UIPERIPHERAL-001).</summary>
    public Func<AttachSlotViewModel, Task<string?>>? PickFileAsync
    {
        get => ViewModel.FilePicker;
        set => ViewModel.FilePicker = value;
    }

    public Func<Task<string?>>? PickKeyboardMapFileAsync { get; set; }

    public Action? PopOutMonitorRequested { get; set; }

    private Control BuildContent()
    {
        var root = new DockPanel
        {
            // MinWidth + MaxWidth previously clamped the sidebar to 260..360;
            // the new compact slot layout works as low as ~210 and the host
            // grid lets the user drag wider via the splitter, so loosen the
            // bounds so horizontal space tracks the host column.
            MinWidth = 210,
            MaxWidth = 480,
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

        // Dock side is now controlled by the shell's single side-toggle button
        // (FR-UIFLYOUT-001); the panel no longer owns Left/Right buttons.
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

        // FR-SIDEBARUI-001: full-height collapse expander on the INNER edge (facing the
        // video), so it sits Right when the panel is anchored Left and Left when anchored
        // Right. Docked first so it spans the panel height; flips edge + glyph with the anchor.
        var collapse = new Button
        {
            Content = ViewModel.CollapseGlyph,
            FontSize = 11,
            Padding = new Thickness(1),
            Width = 18,
            VerticalAlignment = VerticalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(Color.FromRgb(39, 43, 49))
        };
        ToolTip.SetTip(collapse, "Collapse panel");
        collapse.Click += (_, _) => ViewModel.ToggleSidebar();
        DockPanel.SetDock(collapse, ViewModel.CollapseExpanderDock);
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.CollapseExpanderDock))
                DockPanel.SetDock(collapse, ViewModel.CollapseExpanderDock);
            else if (args.PropertyName == nameof(AttachPanelViewModel.CollapseGlyph))
                collapse.Content = ViewModel.CollapseGlyph;
        };
        root.Children.Add(collapse);

        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        // FR-SIDEBARUI-001: real TabControl for the sidebar sections. SelectedIndex tracks the
        // SidebarTab enum order (Peripherals=0, Settings=1, Monitor=2, History=3).
        var tabs = new TabControl { Padding = new Thickness(0) };
        tabs.Items.Add(new TabItem { Header = "Peripherals", Content = CreatePeripheralsPanel() });
        tabs.Items.Add(new TabItem { Header = "Settings", Content = new SettingsView { DataContext = ViewModel } });
        tabs.Items.Add(new TabItem { Header = "Monitor", Content = CreateMonitorPanel(includePopOut: true) });
        tabs.Items.Add(new TabItem { Header = "History", Content = new TickHistoryView { DataContext = ViewModel.TickHistory } });
        tabs.SelectedIndex = (int)ViewModel.ActiveTab;
        tabs.SelectionChanged += (_, _) =>
        {
            if (tabs.SelectedIndex >= 0)
                ViewModel.ActiveTab = (SidebarTab)tabs.SelectedIndex;
        };
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AttachPanelViewModel.ActiveTab))
                tabs.SelectedIndex = (int)ViewModel.ActiveTab;
        };

        root.Children.Add(tabs);
        return root;
    }

    private Control CreatePeripheralsPanel()
    {
        var stack = new StackPanel
        {
            Margin = new Thickness(10, 0, 10, 10),
            Spacing = 8
        };

        stack.Children.Add(CreateKeyboardMapPanel());

        // FR-UIPERIPHERAL-001: every peripheral is rendered by the single
        // reusable PeripheralCardView, bound per slot via the ItemsControl.
        var cards = new ItemsControl
        {
            ItemsSource = ViewModel.Slots,
            ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel { Spacing = 8 }),
            ItemTemplate = new FuncDataTemplate<AttachSlotViewModel>(
                (_, _) => new PeripheralCardView { Panel = ViewModel },
                supportsRecycling: true)
        };
        stack.Children.Add(cards);

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
            MinHeight = 28,
            HorizontalAlignment = HorizontalAlignment.Stretch,
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
            Padding = new Thickness(7, 4),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            FontSize = 12
        };
        custom.Click += async (_, _) => await SelectCustomKeyboardMapAsync().ConfigureAwait(true);
        stack.Children.Add(custom);

        return panel;
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

        // FR-SIDEBARUI-001: action buttons wrap when the panel is narrow.
        var buttons = new WrapPanel { Orientation = Orientation.Horizontal };
        var run = new Button
        {
            Content = "Run",
            Padding = new Thickness(9, 5),
            Margin = new Thickness(0, 0, 6, 6)
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
                Padding = new Thickness(9, 5),
                Margin = new Thickness(0, 0, 6, 6)
            };
            pop.Click += (_, _) => PopOutMonitorRequested?.Invoke();
            buttons.Children.Add(pop);
        }

        stack.Children.Add(buttons);
        return stack;
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
