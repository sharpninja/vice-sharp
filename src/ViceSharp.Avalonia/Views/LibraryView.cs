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
    private readonly TextBox _baseUrlBox;
    private readonly ComboBox _discoveredBox;
    private readonly TextBlock _detailName;
    private readonly TextBlock _detailMeta;
    private readonly TextBlock _detailSummary;
    private readonly ItemsControl _detailFiles;
    private readonly ComboBox _listPicker;
    private readonly Button _addToList;

    /// <summary>Creates the view over its host view-model.</summary>
    /// <param name="viewModel">The RomM library host view-model.</param>
    public LibraryView(RomMLibraryViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;

        _baseUrlBox = new TextBox { PlaceholderText = "http://localhost:8080/", Text = _viewModel.BaseUrl, MinWidth = 200 };
        _baseUrlBox.TextChanged += (_, _) => _viewModel.BaseUrl = _baseUrlBox.Text ?? string.Empty;

        var token = new TextBox { PlaceholderText = "Client API token", PasswordChar = '•', MinWidth = 150 };
        token.TextChanged += (_, _) => _viewModel.Token = token.Text ?? string.Empty;

        // AC-CONN-07: scan the LAN and offer the discovered servers instead of forcing a typed URL.
        var scan = new Button { Content = "Scan LAN" };
        scan.Click += async (_, _) => await _viewModel.ScanAsync();

        _discoveredBox = new ComboBox
        {
            PlaceholderText = "Discovered...",
            MinWidth = 160,
            ItemsSource = _viewModel.DiscoveredServers,
            ItemTemplate = new FuncDataTemplate<DiscoveredRomM>((server, _) => new TextBlock
            {
                Text = server is null ? string.Empty : $"{server.BaseUrl.Host}:{server.BaseUrl.Port}",
            }),
        };
        _discoveredBox.SelectionChanged += (_, _) =>
        {
            if (_discoveredBox.SelectedItem is DiscoveredRomM server)
            {
                _viewModel.SelectDiscovered(server);
            }
        };

        var connect = new Button { Content = "Connect" };
        connect.Click += async (_, _) => await _viewModel.ConnectAsync();

        var connectRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                new TextBlock { Text = "RomM", VerticalAlignment = VerticalAlignment.Center },
                _baseUrlBox,
                scan,
                _discoveredBox,
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

            if (_list.SelectedItem is RomTile selected)
            {
                _ = _viewModel.ShowDetailAsync(selected.Id);
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

        // AC-AUI-03: the right-hand details pane (cover placeholder, metadata, About, files, add-to-list),
        // fed by RomMLibraryViewModel.SelectedDetail when a title is selected.
        _detailName = new TextBlock { FontWeight = FontWeight.SemiBold, FontSize = 16, TextWrapping = TextWrapping.Wrap };
        _detailMeta = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(150, 156, 168)) };
        _detailSummary = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap };
        _detailFiles = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<RomFile>((file, _) => new TextBlock
            {
                Text = file is null ? string.Empty : $"{file.FileName}  ({file.SizeBytes} bytes)",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
            }),
        };

        _listPicker = new ComboBox { PlaceholderText = "Add to list...", MinWidth = 160, ItemTemplate = CollectionTemplate() };
        _addToList = new Button { Content = "Add" };
        _addToList.Click += async (_, _) => await AddSelectedToListAsync();

        var detailPanel = new StackPanel
        {
            Width = 240,
            Margin = new Thickness(12, 0, 0, 0),
            Spacing = 8,
            Children =
            {
                new Border { Height = 150, Background = new SolidColorBrush(Color.FromRgb(26, 30, 36)) },
                _detailName,
                _detailMeta,
                new TextBlock { Text = "About", FontWeight = FontWeight.SemiBold, FontSize = 12 },
                _detailSummary,
                new TextBlock { Text = "Files", FontWeight = FontWeight.SemiBold, FontSize = 12 },
                _detailFiles,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children = { _listPicker, _addToList },
                },
            },
        };
        DockPanel.SetDock(detailPanel, Dock.Right);

        var root = new DockPanel { Margin = new Thickness(10) };
        root.Children.Add(top);
        root.Children.Add(detailPanel);
        root.Children.Add(_list);
        Content = root;

        _viewModel.PropertyChanged += OnViewModelChanged;
    }

    private static FuncDataTemplate<LibraryCollection> CollectionTemplate() =>
        new((collection, _) => new TextBlock { Text = collection?.Name ?? string.Empty });

    private async Task AddSelectedToListAsync()
    {
        if (_viewModel.SelectedDetail is { } detail && _listPicker.SelectedItem is LibraryCollection target && !target.ReadOnly)
        {
            await detail.AddToCollectionAsync(target.Id);
        }
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
        else if (e.PropertyName == nameof(RomMLibraryViewModel.Collections))
        {
            _listPicker.ItemsSource = _viewModel.Collections?.Collections;
        }
        else if (e.PropertyName == nameof(RomMLibraryViewModel.SelectedDetail))
        {
            UpdateDetail();
        }
        else if (e.PropertyName == nameof(RomMLibraryViewModel.BaseUrl) && _baseUrlBox.Text != _viewModel.BaseUrl)
        {
            _baseUrlBox.Text = _viewModel.BaseUrl;
        }
    }

    private void UpdateDetail()
    {
        RomDetail? detail = _viewModel.SelectedDetail?.Detail;
        _detailName.Text = detail?.Name ?? string.Empty;
        _detailMeta.Text = detail?.PlatformSlug is { Length: > 0 } slug ? slug.ToUpperInvariant() : string.Empty;
        _detailSummary.Text = string.IsNullOrWhiteSpace(detail?.Summary) ? "(no description)" : detail!.Summary;
        _detailFiles.ItemsSource = detail?.Files;
    }

    private void UpdateButtons()
    {
        bool canAttach = _viewModel.Browse?.CanAttach == true;
        _attach.IsEnabled = canAttach;
        _attachPlay.IsEnabled = canAttach;
    }
}
