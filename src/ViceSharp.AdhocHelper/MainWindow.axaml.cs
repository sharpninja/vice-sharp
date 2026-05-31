using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace ViceSharp.AdhocHelper;

/// <summary>
/// Single-window Avalonia UI for the ad-hoc machine YAML helper.
/// Wires a YAML editor, a Validate button, a status label, and the
/// File menu to the headless <see cref="AdhocHelperViewModel"/>.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AdhocHelperViewModel _viewModel = new();
    private readonly TextBox _editor;
    private readonly TextBlock _statusLabel;

    public MainWindow()
    {
        InitializeComponent();
        Title = "ViceSharp Ad-hoc Machine Helper";
        Width = 900;
        Height = 600;
        MinWidth = 600;
        MinHeight = 400;

        _editor = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New, monospace"),
            FontSize = 13,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalAlignment.Left,
        };

        _statusLabel = new TextBlock
        {
            Margin = new Thickness(8, 6),
            FontSize = 13,
            Text = "Paste or open a machine YAML and click Validate.",
            TextWrapping = TextWrapping.Wrap,
        };

        var validateButton = new Button
        {
            Content = "Validate",
            Padding = new Thickness(12, 4),
            Margin = new Thickness(8, 6, 6, 6),
        };
        validateButton.Click += OnValidateClick;

        var bottomBar = new DockPanel
        {
            LastChildFill = true,
            Background = new SolidColorBrush(Color.FromRgb(245, 246, 248)),
        };
        DockPanel.SetDock(validateButton, Dock.Left);
        bottomBar.Children.Add(validateButton);
        bottomBar.Children.Add(_statusLabel);

        var menu = BuildMenu();

        var root = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(menu, Dock.Top);
        DockPanel.SetDock(bottomBar, Dock.Bottom);
        root.Children.Add(menu);
        root.Children.Add(bottomBar);
        root.Children.Add(_editor);

        Content = root;

        _viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AdhocHelperViewModel.YamlText))
            {
                if (!ReferenceEquals(_editor.Text, _viewModel.YamlText))
                    _editor.Text = _viewModel.YamlText;
            }
            else if (args.PropertyName == nameof(AdhocHelperViewModel.ValidationMessage))
            {
                _statusLabel.Text = _viewModel.ValidationMessage ?? string.Empty;
            }
            else if (args.PropertyName == nameof(AdhocHelperViewModel.CurrentFilePath))
            {
                Title = string.IsNullOrWhiteSpace(_viewModel.CurrentFilePath)
                    ? "ViceSharp Ad-hoc Machine Helper"
                    : $"ViceSharp Ad-hoc Machine Helper - {_viewModel.CurrentFilePath}";
            }
        };

        _editor.TextChanged += (_, _) => _viewModel.YamlText = _editor.Text ?? string.Empty;
    }

    private Menu BuildMenu()
    {
        var openItem = new MenuItem { Header = "_Open..." };
        openItem.Click += async (_, _) => await OpenAsync();

        var saveItem = new MenuItem { Header = "_Save" };
        saveItem.Click += async (_, _) => await SaveAsync();

        var saveAsItem = new MenuItem { Header = "Save _As..." };
        saveAsItem.Click += async (_, _) => await SaveAsAsync();

        var exitItem = new MenuItem { Header = "E_xit" };
        exitItem.Click += (_, _) => Close();

        var fileMenu = new MenuItem
        {
            Header = "_File",
            ItemsSource = new object[] { openItem, saveItem, saveAsItem, new Separator(), exitItem },
        };

        return new Menu
        {
            ItemsSource = new object[] { fileMenu },
        };
    }

    private void OnValidateClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.YamlText = _editor.Text ?? string.Empty;
        _viewModel.Validate();
    }

    private async Task OpenAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Open ad-hoc machine YAML",
            FileTypeFilter =
            [
                new FilePickerFileType("Machine YAML")
                {
                    Patterns = ["*.yaml", "*.yml", "*.machine.yaml"],
                },
                new FilePickerFileType("All files")
                {
                    Patterns = ["*.*"],
                },
            ],
        }).ConfigureAwait(true);

        if (files.Count == 0) return;
        var path = files[0].TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return;

        _viewModel.OpenFromPath(path);
        _editor.Text = _viewModel.YamlText;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_viewModel.CurrentFilePath))
        {
            await SaveAsAsync().ConfigureAwait(true);
            return;
        }

        _viewModel.YamlText = _editor.Text ?? string.Empty;
        _viewModel.SaveToPath(_viewModel.CurrentFilePath!);
        _statusLabel.Text = $"Saved {_viewModel.CurrentFilePath}";
    }

    private async Task SaveAsAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save ad-hoc machine YAML",
            DefaultExtension = "yaml",
            SuggestedFileName = "machine.yaml",
            FileTypeChoices =
            [
                new FilePickerFileType("Machine YAML")
                {
                    Patterns = ["*.yaml", "*.yml"],
                },
            ],
        }).ConfigureAwait(true);

        if (file is null) return;
        var path = file.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return;

        _viewModel.YamlText = _editor.Text ?? string.Empty;
        _viewModel.SaveToPath(path);
        _statusLabel.Text = $"Saved {path}";
    }
}
