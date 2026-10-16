using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
    private readonly LibraryCoverImage _detailCover = new()
        { Name = "RomMDetailCover", Height = 150, Stretch = Stretch.Uniform };
    private readonly ListBox _collectionsList;
    private AvaloniaCoverImageLoader? _coverLoader;
    private LibraryBrowseViewModel? _boundBrowse;
    private bool _subscribed;
    private Window? _owner;

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
            Name = "RomMServers",
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

        _search = new TextBox { Name = "RomMSearch", PlaceholderText = "Search library..." };
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
            Name = "RomMGameGrid",
            Focusable = true,
            Background = Brushes.Transparent,
            ItemsPanel = new FuncTemplate<Panel?>(() => new LibraryTilePanel()),
            ItemTemplate = new FuncDataTemplate<GameGroup>((group, _) => BuildTile(group)),
        };
        // Fluent's default list-item padding is wider than a compact cover tile.
        // Keep the natural content size within LibraryTilePanel's fixed slot.
        _list.Styles.Add(new Style(s => s.OfType<ListBoxItem>())
        {
            Setters = { new Setter(PaddingProperty, new Thickness(4)) },
        });
        _list.AddHandler(KeyDownEvent, (_, e) =>
        {
            // Avalonia SelectingItemsControl treats an unrealized anchor as no selection
            // (12.0.5 SelectingItemsControl.MoveSelection). Restore it before its normal
            // navigation handler runs, without retaining off-screen cover requests.
            if (_list.IsFocused && _list.SelectedIndex >= 0 &&
                _list.ContainerFromIndex(_list.SelectedIndex) is null &&
                e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End)
                _list.ScrollIntoView(_list.SelectedIndex);
        }, RoutingStrategies.Tunnel);
        _list.SelectionChanged += (_, _) =>
        {
            _detailCover.Clear();
            if (_viewModel.Browse is { } browse)
            {
                var group = _list.SelectedItem as GameGroup;
                browse.SelectedGroup = group;
                if (group is not null) _ = _viewModel.ShowDetailAsync(group.Primary.Id);
            }

            UpdateDetail();
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
            Name = "RomMStatus",
            Text = _viewModel.Status,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
        };

        var top = new StackPanel
        {
            Spacing = 8,
            Children = { connectRow, _search, _azStrip, actionRow },
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
                new Border { Height = 150, Background = new SolidColorBrush(Color.FromRgb(26, 30, 36)), Child = _detailCover },
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

        var allGames = new Button { Content = "All games", HorizontalAlignment = HorizontalAlignment.Stretch };
        allGames.Click += async (_, _) => await RunRailActionAsync(async () =>
        {
            _search.Text = string.Empty;
            if (_viewModel.Browse is { } browse) await browse.ReloadAsync();
            BindList();
            UpdateRecentsChrome();
        });
        _collectionsList = new ListBox { ItemTemplate = CollectionTemplate() };
        _collectionsList.SelectionChanged += async (_, _) => await RunRailActionAsync(async () =>
        {
            if (_collectionsList.SelectedItem is LibraryCollection collection)
            {
                // The Lists rail owns selection and destructive actions. Do not create a second
                // writer that can disagree with its highlighted row.
                _collectionsList.SelectedItem = null;
                if (NavigateToTab<ListsView>() is { } lists) await lists.SelectCollectionAsync(collection.Id);
            }
        });
        var csdb = new Button { Content = "CSDb discovery", HorizontalAlignment = HorizontalAlignment.Stretch };
        csdb.Click += (_, _) => NavigateToTab<CsdbView>();
        var newList = new TextBox { Name = "RomMNewList", PlaceholderText = "+ New list (type + Enter)" };
        newList.KeyDown += async (_, e) =>
        {
            if (e.Key != Key.Enter || string.IsNullOrWhiteSpace(newList.Text) || !newList.IsEnabled) return;
            e.Handled = true;
            await RunRailActionAsync(async () =>
            {
                if (_viewModel.Collections is not { } collections) return;
                newList.IsEnabled = false;
                try { await collections.CreateAsync(newList.Text.Trim()); newList.Text = string.Empty; }
                finally { newList.IsEnabled = true; }
            });
        };
        var rail = new Border
        {
            Name = "RomMLibraryRail", Width = 200, Padding = new Thickness(10),
            Background = new SolidColorBrush(Color.FromRgb(24, 27, 33)),
            Child = new StackPanel { Spacing = 8, Children =
            {
                new TextBlock { Text = "Collections", FontWeight = FontWeight.SemiBold },
                allGames, _collectionsList, csdb, newList,
            } },
        };
        DockPanel.SetDock(rail, Dock.Left);
        var details = new Border
        {
            Padding = new Thickness(12), Background = new SolidColorBrush(Color.FromRgb(30, 34, 41)),
            Child = new ScrollViewer { Content = detailPanel },
        };
        DockPanel.SetDock(details, Dock.Right);
        var body = new DockPanel { Children = { rail, details, _list } };
        var statusBar = new Border { Padding = new Thickness(12, 6), Child = _status };
        DockPanel.SetDock(statusBar, Dock.Bottom);
        var root = new DockPanel { Margin = new Thickness(10), Children = { top, statusBar, body } };
        Content = root;

        Subscribe();
        BindList();
        BindCollections();
        UpdateDetail();
        UpdateRecentsChrome();
        UpdateButtons();
    }

    private static FuncDataTemplate<LibraryCollection> CollectionTemplate() =>
        new((collection, _) => new TextBlock { Text = collection?.Name ?? string.Empty });

    private async Task AddSelectedToListAsync()
    {
        if (_viewModel.SelectedDetail is { } detail && _list.SelectedItem is GameGroup group
            && detail.Detail.Id == group.Primary.Id
            && _listPicker.SelectedItem is LibraryCollection target && !target.ReadOnly)
        {
            await RunRailActionAsync(() => detail.AddToCollectionAsync(target.Id));
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
                RebuildCoverLoader();
                BindList();
                UpdateRecentsChrome();
                UpdateButtons();
            }
            else if (e.PropertyName == nameof(RomMLibraryViewModel.Collections))
            {
                BindCollections();
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
        if (!ReferenceEquals(_list.ItemsSource, _viewModel.Browse?.Items))
            _list.ItemsSource = _viewModel.Browse?.Items;
        // GameGrouper merges page-boundary variants into existing group objects. Refresh only
        // realized presentation; clearing ItemsSource would lose selection and restart all covers.
        foreach (var container in _list.GetRealizedContainers())
        {
            int index = _list.IndexFromContainer(container);
            if (index < 0 || index >= _list.ItemCount || _list.Items[index] is not GameGroup group) continue;
            var visuals = container.GetVisualDescendants().ToArray();
            if (visuals.OfType<TextBlock>().FirstOrDefault(t => t.Name == "RomMTileSubtitle") is { } subtitle)
                subtitle.Text = group.Subtitle;
            if (visuals.OfType<LibraryCoverImage>().FirstOrDefault() is { } image && !Equals(image.Tag, group.Cover))
                LoadTileCover(image, group);
        }
        UpdateButtons();
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
        if (_list.SelectedItem is not GameGroup group || detail?.Id != group.Primary.Id) detail = null;
        _detailName.Text = detail?.Name ?? string.Empty;
        _detailMeta.Text = detail?.PlatformSlug is { Length: > 0 } slug ? slug.ToUpperInvariant() : string.Empty;
        _detailSummary.Text = string.IsNullOrWhiteSpace(detail?.Summary) ? "(no description)" : detail!.Summary;
        _detailFiles.ItemsSource = detail?.Files;
        var loader = _coverLoader;
        _ = _detailCover.LoadAsync(ct => loader?.LoadCoverAsync(detail?.Cover, ct)
            ?? Task.FromResult<global::Avalonia.Media.Imaging.Bitmap?>(null));
    }

    private void UpdateButtons()
    {
        bool canAttach = _viewModel.Browse?.CanAttach == true;
        _attach.IsEnabled = canAttach;
        _attachPlay.IsEnabled = canAttach;
        _more.IsEnabled = !_viewModel.IsShowingRecents && _viewModel.Browse?.HasMore == true;
    }

    private Control BuildTile(GameGroup? group)
    {
        var image = new LibraryCoverImage { Width = 86, Height = 96, Stretch = Stretch.Uniform };
        image.AttachedToVisualTree += (_, _) => LoadTileCover(image, group);
        return new StackPanel
        {
            Width = 110, Margin = new Thickness(6), Spacing = 4,
            Children =
            {
                new Border { Width = 86, Height = 96, CornerRadius = new CornerRadius(6),
                    Background = new SolidColorBrush(Color.FromRgb(40, 45, 54)),
                    Child = new Panel { Children =
                    {
                        new TextBlock { Text = group?.Name ?? string.Empty, TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center }, image,
                    } } },
                new TextBlock { Text = group?.Name ?? string.Empty, FontWeight = FontWeight.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis },
                new TextBlock { Name = "RomMTileSubtitle", Text = group?.Subtitle ?? string.Empty, FontSize = 11,
                    TextWrapping = TextWrapping.Wrap, MaxLines = 2, TextTrimming = TextTrimming.CharacterEllipsis },
            },
        };
    }

    private void LoadTileCover(LibraryCoverImage image, GameGroup? group)
    {
        var loader = _coverLoader;
        image.Tag = group?.Cover;
        _ = image.LoadAsync(ct => loader?.LoadCoverAsync(group?.Cover, ct)
            ?? Task.FromResult<global::Avalonia.Media.Imaging.Bitmap?>(null));
    }

    private void BindCollections()
    {
        _collectionsList.ItemsSource = _viewModel.Collections?.Collections;
        _listPicker.ItemsSource = _viewModel.Collections?.Collections;
    }

    private void RebuildCoverLoader()
    {
        if (ReferenceEquals(_boundBrowse, _viewModel.Browse)) return;
        _boundBrowse = _viewModel.Browse;
        _detailCover.Clear();
        _coverLoader?.Dispose();
        _coverLoader = _viewModel.ConnectedServerUrl is { } server
            ? new AvaloniaCoverImageLoader(server, _viewModel.ConnectedToken) : null;
    }

    private async Task RunRailActionAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { _status.Text = $"Library operation failed: {ex.Message}"; }
    }

    private T? NavigateToTab<T>() where T : Control
    {
        var tabs = this.GetVisualAncestors().OfType<TabControl>().FirstOrDefault();
        if (tabs?.Items.OfType<TabItem>().FirstOrDefault(t => t.Content is T) is { } tab)
        {
            tabs.SelectedItem = tab;
            return (T)tab.Content!;
        }
        else
            _status.Text = "Open the corresponding library tab to continue.";
        return null;
    }

    private void Subscribe()
    {
        if (_subscribed) return;
        _viewModel.PropertyChanged += OnViewModelChanged;
        _subscribed = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Subscribe();
        RebuildCoverLoader();
        BindList();
        BindCollections();
        UpdateDetail();
        UpdateButtons();
        if (TopLevel.GetTopLevel(this) is Window owner && !ReferenceEquals(_owner, owner))
        {
            if (_owner is not null) _owner.Closed -= OnOwnerClosed;
            _owner = owner;
            owner.Closed += OnOwnerClosed;
        }
        _status.Text = _viewModel.Status;
        _ = _viewModel.TryAutoConnectAsync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelChanged;
        _subscribed = false;
        _detailCover.Clear();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnOwnerClosed(object? sender, EventArgs e)
    {
        if (_owner is not null) _owner.Closed -= OnOwnerClosed;
        _owner = null;
        _coverLoader?.Dispose();
        _coverLoader = null;
        _boundBrowse = null;
    }
}
