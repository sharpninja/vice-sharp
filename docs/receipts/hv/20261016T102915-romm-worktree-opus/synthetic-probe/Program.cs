// Independent hostile-review probe for the non-Xbox RomM desktop integration.
// Not product code. Drives the real LibraryView/ListsView/RomMLibraryViewModel headlessly
// with in-memory fakes (no network, no native code) and prints JSON observations.
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Avalonia.Views;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;

namespace ReviewProbe;

public sealed class ProbeApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());
}

public static class Program
{
    private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Dictionary<string, object?> Results = new();

    public static int Main()
    {
        AppBuilder.Configure<ProbeApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
            .SetupWithoutStarting();
        if (SynchronizationContext.Current is null)
            SynchronizationContext.SetSynchronizationContext(
                (SynchronizationContext)Activator.CreateInstance(typeof(AvaloniaSynchronizationContext), nonPublic: true)!);
        Run("P1_gridRealization", P1_GridRealization);
        Run("P2_tabReturn", P2_TabReturn);
        Run("P2b_tabReturnProductionOrder", P2b_TabReturnProductionOrder);
        Run("P3_listsDeleteTarget", P3_ListsDeleteTarget);
        Run("P4_railAllGamesAfterRecents", P4_RailAllGamesAfterRecents);
        Run("P5_baselineMainLibraryView", P5_BaselineMainLibraryView);
        Console.WriteLine(JsonSerializer.Serialize(Results, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static void Run(string name, Func<object> probe)
    {
        try { Results[name] = probe(); }
        catch (Exception ex) { Results[name] = new { error = ex.ToString() }; }
    }

    private static void Pump(int ms = 150)
    {
        var sw = Stopwatch.StartNew();
        do { Dispatcher.UIThread.RunJobs(); Thread.Sleep(2); } while (sw.ElapsedMilliseconds < ms);
        Dispatcher.UIThread.RunJobs();
    }

    private static void WaitFor(Func<bool> condition, int timeoutMs = 4000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs) { Dispatcher.UIThread.RunJobs(); Thread.Sleep(2); }
    }

    private static void SetProp(object target, string name, object? value) =>
        target.GetType().GetProperty(name, Any)!.GetSetMethod(true)!.Invoke(target, new[] { value });

    private static object? GetField(object target, string name) => target.GetType().GetField(name, Any)!.GetValue(target);

    private static T Named<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private sealed record Connected(RomMLibraryViewModel Vm, LibraryBrowseViewModel Browse, FakeLibraryGateway Gateway, FakeCollectionsGateway Collections);

    private static Connected Connect(int total, bool collections, bool recents, bool assignBrowse = true)
    {
        string dir = Path.Combine(Path.GetTempPath(), "vsreview-probe-vm-" + Guid.NewGuid().ToString("N"), "cache");
        var vm = new RomMLibraryViewModel(new NoNetworkTarget(), dir);
        typeof(RomMLibraryViewModel).GetField("_autoConnectTried", Any)!.SetValue(vm, true); // never touch the LAN
        var gateway = new FakeLibraryGateway(total);
        var browse = new LibraryBrowseViewModel(gateway, new NoLauncher(), new C64Machine(), dir,
            recents: recents ? new EmptyRecents() : null);
        Task init = browse.InitializeAsync();
        WaitFor(() => init.IsCompleted);
        if (init.IsFaulted) throw init.Exception!;
        Pump(50);
        var collectionsGateway = new FakeCollectionsGateway();
        typeof(RomMLibraryViewModel).GetField("_gateway", Any)!.SetValue(vm, gateway);
        typeof(RomMLibraryViewModel).GetField("_collectionsGateway", Any)!.SetValue(vm, collectionsGateway);
        SetProp(vm, "ConnectedServerUrl", new Uri("http://127.0.0.1:9/")); // tiles have no cover, so no request is made
        if (assignBrowse) SetProp(vm, "Browse", browse); // production order: Browse is published after connect
        if (collections)
        {
            var cvm = new CollectionsViewModel(collectionsGateway);
            Task refresh = cvm.RefreshAsync();
            WaitFor(() => refresh.IsCompleted);
            Pump(50);
            SetProp(vm, "Collections", cvm);
        }
        Pump(50);
        return new Connected(vm, browse, gateway, collectionsGateway);
    }

    // B: is the RomM game grid virtualized, and how many tiles/cover images are realized?
    private static object P1_GridRealization()
    {
        var c = Connect(total: 600, collections: false, recents: false);
        var view = new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show();
        Pump(300);
        window.UpdateLayout();
        Pump(100);
        var grid = Named<ListBox>(window, "RomMGameGrid");
        var scroller = grid.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        int realized = grid.GetVisualDescendants().OfType<LibraryCoverImage>().Count();
        int visible = 0;
        if (scroller is not null)
        {
            var viewport = new Rect(scroller.Bounds.Size);
            foreach (var image in grid.GetVisualDescendants().OfType<LibraryCoverImage>())
            {
                Point? p = image.TranslatePoint(new Point(0, 0), scroller);
                if (p is { } origin && new Rect(origin, image.Bounds.Size).Intersects(viewport)) visible++;
            }
        }
        int initialItemCount = c.Browse.Items.Count;
        Task more = c.Vm.LoadMoreAsync();
        WaitFor(() => more.IsCompleted);
        Click(view.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == "Load more"));
        Pump(300);
        window.UpdateLayout();
        Pump(100);
        int realizedAfterMore = grid.GetVisualDescendants().OfType<LibraryCoverImage>().Count();
        var panel = grid.ItemsPanelRoot;
        var result = new
        {
            itemsPanel = panel?.GetType().FullName,
            itemsPanelIsVirtualizing = panel is VirtualizingPanel,
            itemCount = initialItemCount,
            realizedTileImages = realized,
            tileImagesIntersectingViewport = visible,
            itemCountAfterTwoLoadMorePages = c.Browse.Items.Count,
            realizedTileImagesAfterLoadMore = realizedAfterMore,
        };
        window.Close();
        return result;
    }

    // C: does returning to the Library tab keep the selection, scroll target and cover loader?
    private static object P2_TabReturn()
    {
        var c = Connect(total: 60, collections: false, recents: false);
        var view = new LibraryView(c.Vm);
        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "Library", Content = view });
        tabs.Items.Add(new TabItem { Header = "Other", Content = new TextBlock { Text = "other tab" } });
        var window = new Window { Width = 1000, Height = 600, Content = tabs };
        window.Show();
        Pump(300);
        var grid = Named<ListBox>(window, "RomMGameGrid");
        grid.SelectedIndex = 3;
        Pump(200);
        var before = new
        {
            gridSelectedIndex = grid.SelectedIndex,
            browseSelectedGroup = c.Browse.SelectedGroup?.Name,
            canAttach = c.Browse.CanAttach,
            loader = GetField(view, "_coverLoader")?.GetHashCode(),
        };
        object? loaderBefore = GetField(view, "_coverLoader");
        tabs.SelectedIndex = 1;
        Pump(200);
        bool detachedOnOtherTab = TopLevel.GetTopLevel(view) is null;
        object? loaderWhileAway = GetField(view, "_coverLoader");
        tabs.SelectedIndex = 0;
        Pump(300);
        object? loaderAfter = GetField(view, "_coverLoader");
        var result = new
        {
            before,
            detachedOnOtherTab,
            loaderNullWhileAway = loaderWhileAway is null,
            after = new
            {
                gridSelectedIndex = grid.SelectedIndex,
                browseSelectedGroup = c.Browse.SelectedGroup?.Name,
                canAttach = c.Browse.CanAttach,
                newLoaderInstance = loaderAfter is not null && !ReferenceEquals(loaderBefore, loaderAfter),
            },
        };
        window.Close();
        return result;
    }

    // C (production order): view constructed and shown first, Browse published afterwards.
    private static object P2b_TabReturnProductionOrder()
    {
        var c = Connect(total: 60, collections: false, recents: false, assignBrowse: false);
        var view = new LibraryView(c.Vm);
        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "Library", Content = view });
        tabs.Items.Add(new TabItem { Header = "Other", Content = new TextBlock { Text = "other tab" } });
        var window = new Window { Width = 1000, Height = 600, Content = tabs };
        window.Show();
        Pump(300);
        SetProp(c.Vm, "Browse", c.Browse);
        Pump(300);
        var grid = Named<ListBox>(window, "RomMGameGrid");
        grid.SelectedIndex = 3;
        Pump(200);
        var before = new { gridSelectedIndex = grid.SelectedIndex, browseSelectedGroup = c.Browse.SelectedGroup?.Name, canAttach = c.Browse.CanAttach };
        tabs.SelectedIndex = 1;
        Pump(200);
        bool detachedOnOtherTab = TopLevel.GetTopLevel(view) is null;
        tabs.SelectedIndex = 0;
        Pump(300);
        var after = new { gridSelectedIndex = grid.SelectedIndex, browseSelectedGroup = c.Browse.SelectedGroup?.Name, canAttach = c.Browse.CanAttach };
        window.Close();
        return new { before, detachedOnOtherTab, after };
    }

    // A: after choosing a collection in the Library rail, what does the Lists tab Delete act on?
    private static object P3_ListsDeleteTarget()
    {
        var c = Connect(total: 10, collections: true, recents: false);
        var library = new LibraryView(c.Vm);
        var lists = new ListsView(c.Vm);
        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "Library", Content = library });
        tabs.Items.Add(new TabItem { Header = "Lists", Content = lists });
        var window = new Window { Width = 1100, Height = 650, Content = tabs };
        window.Show();
        Pump(400);
        tabs.SelectedIndex = 1;
        Pump(300);
        var listsRail = (ListBox)GetField(lists, "_collectionsList")!;
        var railItems = ((System.Collections.IEnumerable?)listsRail.ItemsSource)?.Cast<LibraryCollection>().ToList() ?? new();
        listsRail.SelectedItem = railItems.First(x => x.Id == 2); // operator highlights "Y list" on the Lists tab
        Pump(400);
        tabs.SelectedIndex = 0;
        Pump(300);
        var libraryRail = (ListBox)GetField(library, "_collectionsList")!;
        libraryRail.SelectedItem = c.Vm.Collections!.Collections.First(x => x.Id == 1); // then picks "X list" in the Library rail
        Pump(600);
        var state = new
        {
            selectedTab = tabs.SelectedIndex,
            listsRailHighlightedId = (listsRail.SelectedItem as LibraryCollection)?.Id,
            collectionsSelectedId = c.Vm.Collections!.SelectedCollection?.Id,
            listMemberIds = c.Vm.ListMemberTiles.Select(t => t.Id).ToArray(),
        };
        var delete = lists.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == "Delete");
        Click(delete);
        Pump(500);
        var result = new { state, deleteRequestsForIds = c.Collections.Deleted.ToArray() };
        window.Close();
        return result;
    }

    // Baseline: main fa259f2 LibraryView under the same harness (realization and tab return).
    private static object P5_BaselineMainLibraryView()
    {
        var c = Connect(total: 600, collections: false, recents: false, assignBrowse: false);
        var view = new ReviewProbe.Baseline.BaselineLibraryView(c.Vm);
        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "Library", Content = view });
        tabs.Items.Add(new TabItem { Header = "Other", Content = new TextBlock { Text = "other tab" } });
        var window = new Window { Width = 1000, Height = 600, Content = tabs };
        window.Show();
        Pump(300);
        SetProp(c.Vm, "Browse", c.Browse); // main binds the grid only when Browse is published
        Pump(300);
        window.UpdateLayout();
        Pump(100);
        var grid = (ListBox)GetField(view, "_list")!;
        int realizedContainers = grid.GetVisualDescendants().OfType<ListBoxItem>().Count();
        var panel = grid.ItemsPanelRoot;
        grid.SelectedIndex = 3;
        Pump(200);
        var before = new { gridSelectedIndex = grid.SelectedIndex, browseSelectedGroup = c.Browse.SelectedGroup?.Name, canAttach = c.Browse.CanAttach };
        tabs.SelectedIndex = 1;
        Pump(200);
        bool detachedOnOtherTab = TopLevel.GetTopLevel(view) is null;
        tabs.SelectedIndex = 0;
        Pump(300);
        var after = new { gridSelectedIndex = grid.SelectedIndex, browseSelectedGroup = c.Browse.SelectedGroup?.Name, canAttach = c.Browse.CanAttach };
        var result = new
        {
            itemsPanel = panel?.GetType().FullName,
            itemsPanelIsVirtualizing = panel is VirtualizingPanel,
            itemCount = c.Browse.Items.Count,
            realizedContainers,
            before,
            detachedOnOtherTab,
            after,
        };
        window.Close();
        return result;
    }

    // D: the rail "All games" after searching then entering Recents.
    private static object P4_RailAllGamesAfterRecents()
    {
        var c = Connect(total: 30, collections: false, recents: true);
        var view = new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show();
        Pump(300);
        var search = Named<TextBox>(window, "RomMSearch");
        search.Text = "zork";
        Pump(900);
        string? queryAfterSearch = c.Gateway.LastQuery?.SearchTerm;
        var actionRecents = view.GetVisualDescendants().OfType<Button>()
            .First(b => (b.Content as string) == "Recents");
        Click(actionRecents);
        Pump(500);
        bool inRecents = c.Browse.IsShowingRecents;
        var rail = Named<Border>(window, "RomMLibraryRail");
        var railAllGames = rail.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == "All games");
        Click(railAllGames);
        Pump(900);
        var result = new
        {
            queryAfterSearch,
            inRecentsBeforeRailClick = inRecents,
            searchBoxTextAfter = search.Text,
            browseSearchTextAfter = c.Browse.SearchText,
            lastBrowseQuerySearchTermAfter = c.Gateway.LastQuery?.SearchTerm,
            showingRecentsAfter = c.Browse.IsShowingRecents,
            itemCountAfter = c.Browse.Items.Count,
        };
        window.Close();
        return result;
    }
}

public sealed class FakeLibraryGateway(int total) : IRomMLibraryGateway
{
    public LibraryQuery? LastQuery { get; private set; }
    public Task<int> ResolvePlatformIdAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(19);
    public Task<LibraryPage> BrowseAsync(LibraryQuery query, CancellationToken cancellationToken = default)
    {
        LastQuery = query;
        IEnumerable<int> ids = Enumerable.Range(1, total);
        if (!string.IsNullOrWhiteSpace(query.SearchTerm)) ids = ids.Where(i => i % 3 == 0); // a filtered result set
        var all = ids.ToList();
        var page = all.Skip(query.Offset).Take(query.Limit)
            .Select(i => new RomTile(i, $"Game {i:D4}", $"game{i:D4}.d64", "c64", 174848, null, true)).ToList();
        return Task.FromResult(new LibraryPage(page, all.Count, query.Offset, new Dictionary<string, int>()));
    }
    public Task<RomDetail> GetRomAsync(int romId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RomDetail(romId, $"Game {romId:D4}", "summary", "c64", null, Array.Empty<RomFile>(), Array.Empty<int>()));
    public Task<AcquiredGame> DownloadAsync(int romId, string fileName, long expectedSizeBytes, string cacheDir,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("No download in the probe.");
}

public sealed class FakeCollectionsGateway : IRomMCollectionsGateway
{
    private readonly List<LibraryCollection> _items =
    [
        new(1, "X list", 2, false, new[] { 1, 2 }),
        new(2, "Y list", 1, false, new[] { 3 }),
    ];
    public List<int> Deleted { get; } = new();
    public Task<IReadOnlyList<LibraryCollection>> GetCollectionsAsync(bool includeSmartVirtual, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LibraryCollection>>(_items.ToList());
    public Task<LibraryCollection> CreateCollectionAsync(string name, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException();
    public Task RenameCollectionAsync(int id, string newName, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException();
    public Task DeleteCollectionAsync(int id, CancellationToken cancellationToken = default) { Deleted.Add(id); return Task.CompletedTask; }
    public Task AddRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RemoveRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class EmptyRecents : IRecentsStore
{
    public Task<IReadOnlyList<RecentGame>> LoadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RecentGame>>(Array.Empty<RecentGame>());
    public Task RecordAsync(RecentGame game, int capacity = RecentGame.DefaultCapacity, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class NoLauncher : IGameLauncher
{
    public Task<LaunchOutcome> LaunchAsync(AcquiredGame game, MediaSlot slot, bool autostart, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("No launch in the probe.");
}

public sealed class C64Machine : ICurrentMachineProvider
{
    public string GetActivePlatformSlug() => "c64";
    public event EventHandler PlatformChanged { add { } remove { } }
}

public sealed class NoNetworkTarget : IGameLaunchTarget
{
    public Task<RpcStatus> DropAndStartFileAsync(string filePath, CancellationToken ct = default) => throw new InvalidOperationException();
    public Task<RpcStatus> AttachFileAsync(MediaSlot slot, string filePath, CancellationToken ct = default) => throw new InvalidOperationException();
}
