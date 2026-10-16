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
/// Desktop Lists tab: auto-connect via shared host, Recents + server collections, List/Grid members,
/// Attach / Attach+play (cache-aware).
/// </summary>
public sealed class ListsView : UserControl
{
    private readonly RomMLibraryViewModel _host;
    private readonly ListBox _collectionsList;
    private readonly ListBox _membersList;
    private readonly ItemsControl _membersGrid;
    private readonly ScrollViewer _membersGridScroll;
    private readonly TextBox _newName;
    private readonly TextBlock _status;
    private readonly TextBlock _selected;
    private readonly Button _attach;
    private readonly Button _attachPlay;
    private readonly Button _listMode;
    private readonly Button _gridMode;
    private bool _grid;

    public ListsView(RomMLibraryViewModel host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        DataContext = host;

        _collectionsList = new ListBox
        {
            Width = 220,
            ItemTemplate = new FuncDataTemplate<LibraryCollection>((collection, _) => new TextBlock
            {
                Text = collection is null
                    ? string.Empty
                    : $"{collection.Name}  ({collection.Count}){(collection.ReadOnly ? "  ·  ro" : string.Empty)}",
            }),
        };
        _collectionsList.SelectionChanged += async (_, _) =>
        {
            if (_collectionsList.SelectedItem is LibraryCollection col)
            {
                if (_host.Collections is not null && col.Id != -1)
                {
                    _host.Collections.SelectedCollection = col;
                }

                await _host.LoadListMembersAsync(col);
                BindMembers();
            }
        };

        _membersList = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<RomTile>((tile, _) => new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock { Text = tile?.Name ?? string.Empty, FontWeight = FontWeight.SemiBold },
                    new TextBlock
                    {
                        Text = tile?.FileName ?? string.Empty,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(150, 156, 168)),
                    },
                },
            }),
        };
        _membersList.SelectionChanged += (_, _) =>
        {
            _host.SelectedListTile = _membersList.SelectedItem as RomTile;
            UpdateSelectionChrome();
        };

        _membersGrid = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new WrapPanel()),
            ItemTemplate = new FuncDataTemplate<RomTile>((tile, _) =>
            {
                var border = new Border
                {
                    Width = 140,
                    Height = 150,
                    Margin = new Thickness(6),
                    Background = new SolidColorBrush(Color.FromRgb(26, 30, 36)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8),
                    Child = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock
                            {
                                Text = tile?.Name ?? string.Empty,
                                FontWeight = FontWeight.SemiBold,
                                TextWrapping = TextWrapping.Wrap,
                                MaxHeight = 80,
                            },
                            new TextBlock
                            {
                                Text = tile?.FileName ?? string.Empty,
                                FontSize = 10,
                                Foreground = new SolidColorBrush(Color.FromRgb(150, 156, 168)),
                                TextTrimming = TextTrimming.CharacterEllipsis,
                            },
                        },
                    },
                };
                border.PointerPressed += (_, _) =>
                {
                    _host.SelectedListTile = tile;
                    UpdateSelectionChrome();
                };
                return border;
            }),
        };
        _membersGridScroll = new ScrollViewer
        {
            Content = _membersGrid,
            IsVisible = false,
            HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };

        _listMode = new Button { Content = "List" };
        _listMode.Click += (_, _) => SetGrid(false);
        _gridMode = new Button { Content = "Grid" };
        _gridMode.Click += (_, _) => SetGrid(true);

        _attach = new Button { Content = "Attach", IsEnabled = false };
        _attach.Click += async (_, _) =>
        {
            await _host.AttachListSelectionAsync(autostart: false);
            await RefreshRailAsync();
        };
        _attachPlay = new Button { Content = "Attach + play", IsEnabled = false };
        _attachPlay.Click += async (_, _) =>
        {
            await _host.AttachListSelectionAsync(autostart: true);
            await RefreshRailAsync();
        };

        _newName = new TextBox { PlaceholderText = "New list name", MinWidth = 140 };
        var create = new Button { Content = "Create" };
        create.Click += async (_, _) =>
        {
            if (_host.Collections is not null && !string.IsNullOrWhiteSpace(_newName.Text))
            {
                await _host.Collections.CreateAsync(_newName.Text!.Trim());
                _newName.Text = string.Empty;
                await RefreshRailAsync();
            }
        };
        var refresh = new Button { Content = "Refresh" };
        refresh.Click += async (_, _) =>
        {
            if (_host.Collections is not null)
            {
                await _host.Collections.RefreshAsync();
            }

            await RefreshRailAsync();
        };
        var delete = new Button { Content = "Delete" };
        delete.Click += async (_, _) =>
        {
            if (_host.Collections?.SelectedCollection is { ReadOnly: false, Id: not -1 } selected)
            {
                await _host.Collections.DeleteAsync(selected.Id);
                await RefreshRailAsync();
            }
        };

        _selected = new TextBlock { Text = "No game selected", FontSize = 12 };
        _status = new TextBlock
        {
            Text = _host.Status,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(176, 184, 196)),
            TextWrapping = TextWrapping.Wrap,
        };

        var viewRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { _listMode, _gridMode, _attach, _attachPlay },
        };
        var manageRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { _newName, create, refresh, delete },
        };

        var membersHost = new Grid();
        membersHost.Children.Add(_membersList);
        membersHost.Children.Add(_membersGridScroll);

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("230,*") };
        var left = new Border { Margin = new Thickness(0, 0, 8, 0), Child = _collectionsList };
        var right = new Border { Child = membersHost };
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        body.Children.Add(left);
        body.Children.Add(right);

        var top = new StackPanel { Spacing = 8, Children = { viewRow, manageRow, _selected, _status } };
        DockPanel.SetDock(top, Dock.Top);

        var root = new DockPanel { Margin = new Thickness(10) };
        root.Children.Add(top);
        root.Children.Add(body);
        Content = root;

        _host.PropertyChanged += OnHostChanged;
        SetGrid(false);
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        await _host.TryAutoConnectAsync();
        await RefreshRailAsync();
    }

    private bool _refreshingRail;

    private async Task RefreshRailAsync()
    {
        // GetListsRailAsync assigns RecentGames (new list instance every load). Listening for
        // RecentGames and calling RefreshRailAsync re-entered until stack overflow on startup.
        if (_refreshingRail)
        {
            return;
        }

        _refreshingRail = true;
        try
        {
            _collectionsList.ItemsSource = await _host.GetListsRailAsync();
            BindMembers();
            UpdateSelectionChrome();
            _status.Text = _host.Status;
        }
        finally
        {
            _refreshingRail = false;
        }
    }

    private void BindMembers()
    {
        _membersList.ItemsSource = _host.ListMemberTiles;
        _membersGrid.ItemsSource = _host.ListMemberTiles;
    }

    private void SetGrid(bool grid)
    {
        _grid = grid;
        _membersList.IsVisible = !grid;
        _membersGridScroll.IsVisible = grid;
        _listMode.Opacity = grid ? 0.55 : 1;
        _gridMode.Opacity = grid ? 1 : 0.55;
    }

    private void UpdateSelectionChrome()
    {
        RomTile? tile = _host.SelectedListTile;
        _selected.Text = tile is null
            ? "No game selected"
            : tile.Launchable
                ? $"Selected  {tile.Name} · {tile.FileName}"
                : $"Selected  {tile.Name} (not launchable)";
        bool can = tile?.Launchable == true;
        _attach.IsEnabled = can;
        _attachPlay.IsEnabled = can;
    }

    private void OnHostChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Do not react to RecentGames: GetListsRailAsync sets it, and that would re-enter
        // RefreshRailAsync forever (APPCRASH 0xc00000fd on startup when ListsView constructs).
        // Marshal to UI: Status/tiles can change after attach downloads on a worker thread.
        void apply()
        {
            if (e.PropertyName is nameof(RomMLibraryViewModel.Collections)
                or nameof(RomMLibraryViewModel.IsConnected))
            {
                _ = RefreshRailAsync();
            }
            else if (e.PropertyName == nameof(RomMLibraryViewModel.ListMemberTiles))
            {
                BindMembers();
            }
            else if (e.PropertyName == nameof(RomMLibraryViewModel.Status))
            {
                _status.Text = _host.Status;
            }
            else if (e.PropertyName == nameof(RomMLibraryViewModel.SelectedListTile))
            {
                UpdateSelectionChrome();
            }
        }

        if (Dispatcher.UIThread.CheckAccess())
            apply();
        else
            Dispatcher.UIThread.Post(apply);
    }
}
