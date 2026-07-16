using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// PLAN-ROMM-001 (FR-ROMM-AVUI-001). The desktop RomM library tab: connect (URL + token), search, a
/// scrollable title list scoped to the active machine, load-more paging, and attach / attach+play of the
/// selection. Programmatic UI (matching AttachPanelView), bound to a <see cref="RomMLibraryViewModel"/>.
/// Runtime browse/attach is the [V] E2E step against a live RomM server.
/// </summary>
public sealed class LibraryView : UserControl
{
    private readonly RomMLibraryViewModel _viewModel;
    private readonly ListBox _list;
    private readonly TextBlock _status;
    private readonly Button _attach;
    private readonly Button _attachPlay;

    /// <summary>Creates the view over its host view-model.</summary>
    /// <param name="viewModel">The RomM library host view-model.</param>
    public LibraryView(RomMLibraryViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;

        var baseUrl = new TextBox { PlaceholderText = "http://localhost:8080/", Text = _viewModel.BaseUrl, MinWidth = 200 };
        baseUrl.TextChanged += (_, _) => _viewModel.BaseUrl = baseUrl.Text ?? string.Empty;

        var token = new TextBox { PlaceholderText = "Client API token", PasswordChar = '•', MinWidth = 150 };
        token.TextChanged += (_, _) => _viewModel.Token = token.Text ?? string.Empty;

        var connect = new Button { Content = "Connect" };
        connect.Click += async (_, _) => await _viewModel.ConnectAsync();

        var connectRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "RomM", VerticalAlignment = VerticalAlignment.Center },
                baseUrl,
                token,
                connect,
            },
        };

        var search = new TextBox { PlaceholderText = "Search C64 library..." };
        search.TextChanged += (_, _) => _viewModel.Search(search.Text ?? string.Empty);

        _list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<RomTile>((tile, _) => new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = tile?.Name ?? string.Empty, FontWeight = FontWeight.SemiBold },
                    new TextBlock
                    {
                        Text = tile is null ? string.Empty : tile.Launchable ? tile.FileName : $"{tile.FileName} (not launchable)",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(150, 156, 168)),
                    },
                },
            }),
        };
        _list.SelectionChanged += (_, _) =>
        {
            if (_viewModel.Browse is not null)
            {
                _viewModel.Browse.SelectedTile = _list.SelectedItem as RomTile;
            }

            UpdateButtons();
        };

        _attachPlay = new Button { Content = "Attach + play", IsEnabled = false };
        _attachPlay.Click += async (_, _) => await _viewModel.AttachAsync(autostart: true);

        _attach = new Button { Content = "Attach", IsEnabled = false };
        _attach.Click += async (_, _) => await _viewModel.AttachAsync(autostart: false);

        var more = new Button { Content = "Load more" };
        more.Click += async (_, _) => await _viewModel.LoadMoreAsync();

        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { _attachPlay, _attach, more },
        };

        _status = new TextBlock
        {
            Text = _viewModel.Status,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
        };

        var top = new StackPanel { Spacing = 8, Children = { connectRow, search, actionRow, _status } };
        DockPanel.SetDock(top, Dock.Top);

        var root = new DockPanel { Margin = new Thickness(10) };
        root.Children.Add(top);
        root.Children.Add(_list);
        Content = root;

        _viewModel.PropertyChanged += OnViewModelChanged;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RomMLibraryViewModel.Status))
        {
            _status.Text = _viewModel.Status;
        }
        else if (e.PropertyName is nameof(RomMLibraryViewModel.Browse) or nameof(RomMLibraryViewModel.IsConnected))
        {
            _list.ItemsSource = _viewModel.Browse?.Items;
            UpdateButtons();
        }
    }

    private void UpdateButtons()
    {
        bool canAttach = _viewModel.Browse?.CanAttach == true;
        _attach.IsEnabled = canAttach;
        _attachPlay.IsEnabled = canAttach;
    }
}
