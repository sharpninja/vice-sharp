using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Library.ViewModels;

namespace ViceSharp.Avalonia.Views;

/// <summary>
/// Desktop RomM library: auto-connect, GameGroup grid (variant counts), A-Z jump, Recents toggle,
/// attach / attach+play. Bound to <see cref="RomMLibraryViewModel"/>.
/// </summary>
public sealed class LibraryView : UserControl
{
    private readonly RomMLibraryViewModel _viewModel;
    private readonly ListBox _list;
    private readonly TextBlock _status;
    private readonly Button _attach;
    private readonly Button _attachPlay;
    private readonly Button _recents;
    private readonly Button _more;
    private readonly TextBox _baseUrlBox;
    private readonly TextBox _search;
    private readonly ComboBox _discoveredBox;
    private readonly TextBlock _detailName;
    private readonly TextBlock _detailMeta;
    private readonly TextBlock _detailSummary;
    private readonly ItemsControl _detailFiles;
    private readonly ComboBox _listPicker;
    private readonly Button _addToList;
    private readonly StackPanel _azStrip;

    public LibraryView(RomMLibraryViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;

        _baseUrlBox = new TextBox { PlaceholderText = "http://localhost:8080/", Text = _viewModel.BaseUrl, MinWidth = 200 };
        _baseUrlBox.TextChanged += (_, _) => _viewModel.BaseUrl = _baseUrlBox.Text ?? string.Empty;

        var token = new TextBox { PlaceholderText = "Client API token", PasswordChar = '•', MinWidth = 150 };
        token.TextChanged += (_, _) => _viewModel.Token = token.Text ?? string.Empty;

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

        _search = new TextBox { PlaceholderText = "Search C64 library..." };
        _search.TextChanged += (_, _) => _viewModel.Search(_search.Text ?? string.Empty);

        _recents = new Button { Content = "Recents" };
        _recents.Click += async (_, _) =>
        {
            await _viewModel.ToggleRecentsAsync();
            UpdateRecentsChrome();
            BindList();
        };

        _list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<GameGroup>((group, _) =>
            {
                if (group is null)
                {
                    return new TextBlock();
                }

                return new StackPanel
                {
                    Spacing = 2,
                    Children =
                    {
                        new TextBlock { Text = group.Name, FontWeight = FontWeight.SemiBold },
                        new TextBlock
                        {
                            Text = group.Subtitle,
                            FontSize = 11,
                            Foreground = new SolidColorBrush(Color.FromRgb(150, 156, 168)),
                        },
                    },
                };
            }),
        };
        _list.SelectionChanged += (_, _) =>
        {
            if (_viewModel.Browse is not null && _list.SelectedItem is GameGroup group)
            {
                _viewModel.Browse.SelectedGroup = group;
                _ = _viewModel.ShowDetailAsync(group.Primary.Id);
            }

            UpdateButtons();
        };

        _azStrip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        foreach (char letter in Enumerable.Range('A', 26).Select(c => (char)c).Append('#'))
        {
            char target = letter;
            var button = new Button { Content = target.ToString(), MinWidth = 28, Padding = new Thickness(4, 2) };
            button.Click += async (_, _) =>
            {
                await _viewModel.JumpToLetterAsync(target);
                BindList();
            };
            _azStrip.Children.Add(button);
        }

        _attachPlay = new Button { Content = "Attach + play", IsEnabled = false };
        _attachPlay.Click += async (_, _) =>
        {
            await _viewModel.AttachAsync(autostart: true);
            UpdateRecentsChrome();
        };

        _attach = new Button { Content = "Attach", IsEnabled = false };
        _attach.Click += async (_, _) =>
        {
            await _viewModel.AttachAsync(autostart: false);
            UpdateRecentsChrome();
        };

        _more = new Button { Content = "Load more" };
        _more.Click += async (_, _) =>
        {
            await _viewModel.LoadMoreAsync();
            BindList();
        };

        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { _recents, _attachPlay, _attach, _more },
        };

        _status = new TextBlock
        {
            Text = _viewModel.Status,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
        };

        var top = new StackPanel
        {
            Spacing = 8,
            Children = { connectRow, _search, _azStrip, actionRow, _status },
        };
        DockPanel.SetDock(top, Dock.Top);

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

        // Variant list when a multi-variant group is selected
        var variantsNote = new TextBlock
        {
            Text = "Multi-variant titles: pick a file below or use Attach for the preferred variant.",
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(150, 156, 168)),
        };

        var detailPanel = new StackPanel
        {
            Width = 260,
            Margin = new Thickness(12, 0, 0, 0),
            Spacing = 8,
            Children =
            {
                new Border { Height = 150, Background = new SolidColorBrush(Color.FromRgb(26, 30, 36)) },
                _detailName,
                _detailMeta,
                variantsNote,
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
        _ = _viewModel.TryAutoConnectAsync();
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
        void apply()
        {
            if (e.PropertyName == nameof(RomMLibraryViewModel.Status))
            {
                _status.Text = _viewModel.Status;
            }
            else if (e.PropertyName is nameof(RomMLibraryViewModel.Browse)
                     or nameof(RomMLibraryViewModel.IsConnected)
                     or nameof(RomMLibraryViewModel.IsShowingRecents))
            {
                BindList();
                UpdateRecentsChrome();
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

        if (Dispatcher.UIThread.CheckAccess())
            apply();
        else
            Dispatcher.UIThread.Post(apply);
    }

    private void BindList()
    {
        _list.ItemsSource = null;
        _list.ItemsSource = _viewModel.Browse?.Items;
    }

    private void UpdateRecentsChrome()
    {
        bool recents = _viewModel.IsShowingRecents;
        _recents.Content = recents ? "All games" : "Recents";
        _search.IsEnabled = !recents;
        _azStrip.IsEnabled = !recents;
        _more.IsEnabled = !recents && _viewModel.Browse?.HasMore == true;
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
        _more.IsEnabled = !_viewModel.IsShowingRecents && _viewModel.Browse?.HasMore == true;
    }
}
