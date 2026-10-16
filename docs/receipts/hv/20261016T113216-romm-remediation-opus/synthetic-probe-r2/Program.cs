// Independent R2 hostile-review probe for the remediated non-Xbox RomM desktop integration.
// Not product code. Drives the real LibraryView/ListsView/RomMLibraryViewModel headlessly with
// in-memory fakes; the only socket is a local loopback listener that never answers (P1b).
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
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
        Run("P1b_scrollAwayCancelsRealHttpCoverRequests", P1b_ScrollAwayCancelsRealHttp);
        Run("P2_tabReturnAndWindowClose", P2_TabReturnAndWindowClose);
        Run("P2c_reconnectDisposesOldLoader", P2c_Reconnect);
        Run("P3_listsDeleteTarget", P3_ListsDeleteTarget);
        Run("P6_keyboardAfterScrollAway_new", () => P6_KeyboardAfterScrollAway(baseline: false));
        Run("P6_keyboardAfterScrollAway_mainBaseline", () => P6_KeyboardAfterScrollAway(baseline: true));
        Run("P6r_routedKeysAfterScrollAway_new", () => P6r_RoutedKeysAfterScrollAway(baseline: false));
        Run("P6r_routedKeysAfterScrollAway_mainBaseline", () => P6r_RoutedKeysAfterScrollAway(baseline: true));
        Run("P7_tileSizeVersusPanelSlot", P7_TileSize);
        Run("P8_verticalNavigationUsesColumns", P8_VerticalNavigation);
        Run("P9_selectionVisualAfterScrollBack", P9_SelectionVisualAfterScrollBack);
        Run("P10_resizeChangesColumns", P10_ResizeColumns);
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

    private static void Layout(Window w) { Pump(60); w.UpdateLayout(); Pump(60); w.UpdateLayout(); Pump(60); }
    private static void SetProp(object target, string name, object? value) =>
        target.GetType().GetProperty(name, Any)!.GetSetMethod(true)!.Invoke(target, new[] { value });
    private static object? GetField(object target, string name) => target.GetType().GetField(name, Any)!.GetValue(target);
    private static T Named<T>(Visual root, string name) where T : Control => root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static bool LoaderDisposed(object loader)
    {
        try { ((System.Net.Http.HttpClient)GetField(loader, "_authenticated")!).CancelPendingRequests(); return false; }
        catch (ObjectDisposedException) { return true; }
    }

    private sealed record Connected(RomMLibraryViewModel Vm, LibraryBrowseViewModel Browse, FakeLibraryGateway Gateway, FakeCollectionsGateway Collections);

    private static Connected Connect(int total, bool collections, bool covers, Uri? server = null, bool assignBrowse = true)
    {
        string dir = Path.Combine(Path.GetTempPath(), "vsreview-probe-r2-vm-" + Guid.NewGuid().ToString("N"), "cache");
        var vm = new RomMLibraryViewModel(new NoNetworkTarget(), dir);
        typeof(RomMLibraryViewModel).GetField("_autoConnectTried", Any)!.SetValue(vm, true); // never touch the LAN
        var gateway = new FakeLibraryGateway(total, covers);
        var browse = NewBrowse(gateway, dir);
        var collectionsGateway = new FakeCollectionsGateway();
        typeof(RomMLibraryViewModel).GetField("_gateway", Any)!.SetValue(vm, gateway);
        typeof(RomMLibraryViewModel).GetField("_collectionsGateway", Any)!.SetValue(vm, collectionsGateway);
        SetProp(vm, "ConnectedServerUrl", server ?? new Uri("http://127.0.0.1:9/"));
        SetProp(vm, "ConnectedToken", "probe-token");
        if (assignBrowse) SetProp(vm, "Browse", browse);
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

    private static LibraryBrowseViewModel NewBrowse(FakeLibraryGateway gateway, string dir)
    {
        var browse = new LibraryBrowseViewModel(gateway, new NoLauncher(), new C64Machine(), dir, recents: new EmptyRecents());
        Task init = browse.InitializeAsync();
        WaitFor(() => init.IsCompleted);
        if (init.IsFaulted) throw init.Exception!;
        Pump(50);
        return browse;
    }

    private static int RealizedTileImages(ListBox grid) => grid.GetVisualDescendants().OfType<LibraryCoverImage>().Count();

    private static object P1_GridRealization()
    {
        var c = Connect(total: 600, collections: false, covers: false);
        var view = new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show(); Layout(window);
        var grid = Named<ListBox>(window, "RomMGameGrid");
        int initial = RealizedTileImages(grid);
        int initialItems = c.Browse.Items.Count;
        Click(view.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == "Load more")); Layout(window);
        Click(view.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == "Load more")); Layout(window);
        var result = new
        {
            itemsPanel = grid.ItemsPanelRoot?.GetType().FullName,
            itemsPanelIsVirtualizing = grid.ItemsPanelRoot is VirtualizingPanel,
            initialItems, initialRealizedTileImages = initial,
            itemsAfterTwoLoadMore = c.Browse.Items.Count,
            realizedAfterTwoLoadMore = RealizedTileImages(grid),
        };
        window.Close();
        return result;
    }

    private static object P1b_ScrollAwayCancelsRealHttp()
    {
        using var server = new HangingServer();
        var c = Connect(total: 600, collections: false, covers: true, server: new Uri($"http://127.0.0.1:{server.Port}/"));
        var view = new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show(); Layout(window);
        WaitFor(() => server.Accepted >= 4, 3000); Pump(300);
        var grid = Named<ListBox>(window, "RomMGameGrid");
        int realizedBefore = RealizedTileImages(grid);
        int acceptedBefore = server.Accepted, closedBefore = server.ClosedByClient;
        var firstRequests = server.Paths.Take(acceptedBefore).ToArray();
        grid.ScrollIntoView(59); Layout(window);
        WaitFor(() => server.ClosedByClient >= acceptedBefore, 3000); Pump(400);
        var result = new
        {
            realizedBefore, acceptedBefore, closedBefore, firstRequestPaths = firstRequests,
            realizedAfterScroll = RealizedTileImages(grid),
            closedByClientAfterScroll = server.ClosedByClient,
            acceptedAfterScroll = server.Accepted,
            laterRequestPaths = server.Paths.Skip(acceptedBefore).Take(6).ToArray(),
            lastIndexRealized = grid.ContainerFromIndex(59) is not null,
        };
        window.Close();
        return result;
    }

    private static object P2_TabReturnAndWindowClose()
    {
        var c = Connect(total: 60, collections: false, covers: false);
        var view = new LibraryView(c.Vm);
        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "Library", Content = view });
        tabs.Items.Add(new TabItem { Header = "Other", Content = new TextBlock { Text = "other" } });
        var window = new Window { Width = 1000, Height = 600, Content = tabs };
        window.Show(); Layout(window);
        var grid = Named<ListBox>(window, "RomMGameGrid");
        grid.SelectedIndex = 3; Layout(window);
        object? loaderBefore = GetField(view, "_coverLoader");
        var before = new { selected = grid.SelectedIndex, group = c.Browse.SelectedGroup?.Name, canAttach = c.Browse.CanAttach };
        tabs.SelectedIndex = 1; Layout(window);
        bool detached = TopLevel.GetTopLevel(view) is null;
        bool loaderAliveWhileAway = loaderBefore is not null && !LoaderDisposed(loaderBefore);
        tabs.SelectedIndex = 0; Layout(window);
        var after = new { selected = grid.SelectedIndex, group = c.Browse.SelectedGroup?.Name, canAttach = c.Browse.CanAttach,
            sameLoader = ReferenceEquals(loaderBefore, GetField(view, "_coverLoader")), detailName = ((TextBlock)GetField(view, "_detailName")!).Text };
        window.Close(); Pump(100);
        return new { before, detachedOnOtherTab = detached, loaderAliveWhileAway, after,
            loaderDisposedAfterWindowClose = loaderBefore is not null && LoaderDisposed(loaderBefore), viewLoaderNullAfterClose = GetField(view, "_coverLoader") is null };
    }

    private static object P2c_Reconnect()
    {
        var c = Connect(total: 60, collections: false, covers: false);
        var view = new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show(); Layout(window);
        object? loaderBefore = GetField(view, "_coverLoader");
        var newBrowse = NewBrowse(c.Gateway, Path.Combine(Path.GetTempPath(), "vsreview-probe-r2-vm-" + Guid.NewGuid().ToString("N")));
        SetProp(c.Vm, "ConnectedServerUrl", new Uri("http://127.0.0.2:9/"));
        SetProp(c.Vm, "Browse", newBrowse); Layout(window);
        object? loaderAfter = GetField(view, "_coverLoader");
        var client = (System.Net.Http.HttpClient)GetField(loaderAfter!, "_authenticated")!;
        var result = new { replaced = !ReferenceEquals(loaderBefore, loaderAfter), oldDisposed = loaderBefore is not null && LoaderDisposed(loaderBefore),
            newBase = client.BaseAddress?.ToString(), gridBoundToNewItems = ReferenceEquals(Named<ListBox>(window, "RomMGameGrid").ItemsSource, newBrowse.Items) };
        window.Close();
        return result;
    }

    private static object P3_ListsDeleteTarget()
    {
        var c = Connect(total: 10, collections: true, covers: false);
        var library = new LibraryView(c.Vm);
        var lists = new ListsView(c.Vm);
        var tabs = new TabControl();
        tabs.Items.Add(new TabItem { Header = "Library", Content = library });
        tabs.Items.Add(new TabItem { Header = "Lists", Content = lists });
        var window = new Window { Width = 1100, Height = 650, Content = tabs };
        window.Show(); Layout(window); Pump(300);
        tabs.SelectedIndex = 1; Layout(window); Pump(300);
        var listsRail = (ListBox)GetField(lists, "_collectionsList")!;
        var railItems = ((System.Collections.IEnumerable?)listsRail.ItemsSource)?.Cast<LibraryCollection>().ToList() ?? new();
        listsRail.SelectedItem = railItems.First(x => x.Id == 2); Pump(400);
        tabs.SelectedIndex = 0; Layout(window);
        var libraryRail = (ListBox)GetField(library, "_collectionsList")!;
        libraryRail.SelectedItem = c.Vm.Collections!.Collections.First(x => x.Id == 1); Pump(700); Layout(window);
        var afterNavigation = new { selectedTab = tabs.SelectedIndex, listsRailHighlightedId = (listsRail.SelectedItem as LibraryCollection)?.Id,
            collectionsSelectedId = c.Vm.Collections!.SelectedCollection?.Id, members = c.Vm.ListMemberTiles.Select(t => t.Id).ToArray(),
            libraryRailSelectionCleared = libraryRail.SelectedItem is null };
        // Forced mismatch: shared selection disagrees with the visible row -> Delete must refuse.
        c.Vm.Collections!.SelectedCollection = c.Vm.Collections.Collections.First(x => x.Id == 2);
        var delete = lists.GetVisualDescendants().OfType<Button>().First(b => (b.Content as string) == "Delete");
        Click(delete); Pump(400);
        int[] afterMismatch = c.Collections.Deleted.ToArray();
        // Re-select through the visible rail and delete the highlighted collection.
        listsRail.SelectedItem = null; Pump(200);
        listsRail.SelectedItem = ((System.Collections.IEnumerable)listsRail.ItemsSource!).Cast<LibraryCollection>().First(x => x.Id == 1); Pump(400);
        Click(delete); Pump(600);
        var result = new { afterNavigation, deletesAfterForcedMismatch = afterMismatch, deletesAfterConsistentDelete = c.Collections.Deleted.ToArray(),
            listsRailAfterDelete = (listsRail.SelectedItem as LibraryCollection)?.Id, sharedAfterDelete = c.Vm.Collections.SelectedCollection?.Id };
        window.Close();
        return result;
    }

    private static object P6_KeyboardAfterScrollAway(bool baseline)
    {
        var c = Connect(total: 600, collections: false, covers: false, assignBrowse: !baseline);
        Control view = baseline ? new ReviewProbe.Baseline.BaselineLibraryView(c.Vm) : new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show(); Layout(window);
        if (baseline) { SetProp(c.Vm, "Browse", c.Browse); Layout(window); }
        var grid = baseline ? (ListBox)GetField(view, "_list")! : Named<ListBox>(window, "RomMGameGrid");
        grid.SelectedIndex = 3; Layout(window);
        var container = grid.ContainerFromIndex(3);
        container?.Focus(); Layout(window);
        var fm = TopLevel.GetTopLevel(window)!.FocusManager!;
        string focusedBefore = fm.GetFocusedElement()?.GetType().Name ?? "null";
        var sv = grid.GetVisualDescendants().OfType<ScrollViewer>().First();
        sv.Offset = new Vector(0, Math.Max(0, sv.Extent.Height - sv.Viewport.Height)); Layout(window);
        bool selectedContainerRealized = grid.ContainerFromIndex(3) is not null;
        var focused = fm.GetFocusedElement() as Control;
        string focusedAfterScroll = focused?.GetType().Name ?? "null";
        Key key = baseline ? Key.Down : Key.Right;
        var target = (focused is not null && focused.GetSelfAndVisualAncestors().Contains(grid)) ? focused : (Control)grid;
        target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = target }); Layout(window);
        var result = new { view = baseline ? "main fa259f2 LibraryView (VirtualizingStackPanel, vertical list)" : "remediated LibraryView (LibraryTilePanel grid)",
            key = key.ToString(), focusedBefore, selectedContainerRealizedAfterScroll = selectedContainerRealized, focusedAfterScroll,
            keyRaisedOn = target == grid ? "ListBox" : target.GetType().Name, selectedIndexAfterKey = grid.SelectedIndex, expectedIfRelativeToSelection = 4 };
        window.Close();
        return result;
    }

    // Real input routing through the headless platform keyboard (FocusManager decides the target).
    private static object P6r_RoutedKeysAfterScrollAway(bool baseline)
    {
        var c = Connect(total: 600, collections: false, covers: false, assignBrowse: !baseline);
        Control view = baseline ? new ReviewProbe.Baseline.BaselineLibraryView(c.Vm) : new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show(); Layout(window);
        if (baseline) { SetProp(c.Vm, "Browse", c.Browse); Layout(window); }
        var grid = baseline ? (ListBox)GetField(view, "_list")! : Named<ListBox>(window, "RomMGameGrid");
        grid.SelectedIndex = 3; Layout(window);
        (grid.ContainerFromIndex(3) as Control)?.Focus(); Layout(window);
        var fm = TopLevel.GetTopLevel(window)!.FocusManager!;
        Key key = baseline ? Key.Down : Key.Right;
        PhysicalKey pk = baseline ? PhysicalKey.ArrowDown : PhysicalKey.ArrowRight;
        // Control: same key without scrolling away first.
        window.KeyPress(key, RawInputModifiers.None, pk, null); window.KeyRelease(key, RawInputModifiers.None, pk, null); Layout(window);
        int controlSelected = grid.SelectedIndex;
        grid.SelectedIndex = 3; Layout(window);
        (grid.ContainerFromIndex(3) as Control)?.Focus(); Layout(window);
        var sv = grid.GetVisualDescendants().OfType<ScrollViewer>().First();
        sv.Offset = new Vector(0, Math.Max(0, sv.Extent.Height - sv.Viewport.Height)); Layout(window);
        double offsetAfterScroll = sv.Offset.Y;
        string focusedAfterScroll = (fm.GetFocusedElement() as Control)?.GetType().Name ?? "null";
        window.KeyPress(key, RawInputModifiers.None, pk, null); window.KeyRelease(key, RawInputModifiers.None, pk, null); Layout(window);
        var result = new { view = baseline ? "main fa259f2 LibraryView" : "remediated LibraryView", key = key.ToString(),
            controlNoScroll_selectedAfterKey = controlSelected, offsetAfterScroll, focusedAfterScroll,
            afterScroll_selectedAfterRoutedKey = grid.SelectedIndex, afterScroll_offsetAfterKey = sv.Offset.Y,
            focusedAfterKey = (fm.GetFocusedElement() as Control)?.GetType().Name ?? "null" };
        window.Close();
        return result;
    }

    private static object P7_TileSize()
    {
        var c = Connect(total: 60, collections: false, covers: false);
        var view = new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show(); Layout(window);
        var grid = Named<ListBox>(window, "RomMGameGrid");
        var item = (Control)grid.ContainerFromIndex(0)!;
        var slot = item.Bounds;
        item.Measure(Size.Infinity);
        var natural = item.DesiredSize;
        item.InvalidateMeasure(); Layout(window);
        var result = new { slotWidth = slot.Width, slotHeight = slot.Height, naturalWidth = natural.Width, naturalHeight = natural.Height,
            fitsWidth = natural.Width <= slot.Width + 0.5, fitsHeight = natural.Height <= slot.Height + 0.5,
            itemPadding = (item as ContentControl)?.Padding.ToString() };
        window.Close();
        return result;
    }

    private static object P9_SelectionVisualAfterScrollBack()
    {
        var c = Connect(total: 600, collections: false, covers: false);
        var view = new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show(); Layout(window);
        var grid = Named<ListBox>(window, "RomMGameGrid");
        grid.SelectedIndex = 3; Layout(window);
        var sv = grid.GetVisualDescendants().OfType<ScrollViewer>().First();
        sv.Offset = new Vector(0, Math.Max(0, sv.Extent.Height - sv.Viewport.Height)); Layout(window);
        bool realizedWhileAway = grid.ContainerFromIndex(3) is not null;
        sv.Offset = new Vector(0, 0); Layout(window);
        var item = grid.ContainerFromIndex(3) as ListBoxItem;
        int selectedVisuals = grid.GetRealizedContainers().OfType<ListBoxItem>().Count(i => i.IsSelected);
        var result = new { realizedWhileAway, reRealized = item is not null, isSelected = item?.IsSelected, selectedIndex = grid.SelectedIndex, selectedVisuals };
        window.Close();
        return result;
    }

    private static object P10_ResizeColumns()
    {
        var c = Connect(total: 60, collections: false, covers: false);
        var view = new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show(); Layout(window);
        var grid = Named<ListBox>(window, "RomMGameGrid");
        int ColumnCount() => (int)GetField(grid.ItemsPanelRoot!, "_columns")!;
        int at1000 = ColumnCount(); int realized1000 = grid.GetRealizedContainers().Count();
        window.Width = 760; Layout(window); int at760 = ColumnCount(); int realized760 = grid.GetRealizedContainers().Count();
        window.Width = 1500; Layout(window); int at1500 = ColumnCount(); int realized1500 = grid.GetRealizedContainers().Count();
        var lastRow = grid.GetRealizedContainers().Select(x => x.Bounds).ToArray();
        bool overlaps = lastRow.SelectMany((a, i) => lastRow.Skip(i + 1).Select(b => a.Intersects(b) && a.Intersect(b).Width > 0.5 && a.Intersect(b).Height > 0.5)).Any(x => x);
        var result = new { at760, realized760, at1000, realized1000, at1500, realized1500, containersOverlap = overlaps };
        window.Close();
        return result;
    }

    private static object P8_VerticalNavigation()
    {
        var c = Connect(total: 60, collections: false, covers: false);
        var view = new LibraryView(c.Vm);
        var window = new Window { Width = 1000, Height = 600, Content = view };
        window.Show(); Layout(window);
        var grid = Named<ListBox>(window, "RomMGameGrid");
        grid.SelectedIndex = 0; Layout(window);
        (grid.ContainerFromIndex(0) as Control)?.Focus(); Layout(window);
        int columns = (int)GetField(grid.ItemsPanelRoot!, "_columns")!;
        var focused = TopLevel.GetTopLevel(window)!.FocusManager!.GetFocusedElement() as Control ?? grid;
        focused.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down, Source = focused }); Layout(window);
        int afterDown = grid.SelectedIndex;
        var result = new { columns, selectedAfterDown = afterDown, expected = columns };
        window.Close();
        return result;
    }
}

public sealed class HangingServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _cts = new();
    private readonly object _gate = new();
    private int _accepted, _closed;
    public List<string> Paths { get; } = new();
    public HangingServer() { _listener.Start(); _ = Task.Run(AcceptLoopAsync); }
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
    public int Accepted { get { lock (_gate) return _accepted; } }
    public int ClosedByClient { get { lock (_gate) return _closed; } }
    private async Task AcceptLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                Socket socket = await _listener.AcceptSocketAsync(_cts.Token).ConfigureAwait(false);
                lock (_gate) _accepted++;
                _ = Task.Run(() => ReadUntilClosedAsync(socket));
            }
        }
        catch { }
    }
    private async Task ReadUntilClosedAsync(Socket socket)
    {
        var buffer = new byte[4096];
        bool recorded = false;
        try
        {
            while (true)
            {
                int n = await socket.ReceiveAsync(buffer, SocketFlags.None, _cts.Token).ConfigureAwait(false);
                if (n == 0) break;
                if (!recorded)
                {
                    string head = System.Text.Encoding.ASCII.GetString(buffer, 0, n);
                    string line = head.Split("\r\n")[0];
                    lock (_gate) Paths.Add(line);
                    recorded = true;
                }
            }
            lock (_gate) _closed++;
        }
        catch (Exception) when (!_cts.IsCancellationRequested) { lock (_gate) _closed++; }
        catch { }
        finally { socket.Dispose(); }
    }
    public void Dispose() { _cts.Cancel(); _listener.Stop(); }
}

public sealed class FakeLibraryGateway(int total, bool covers) : IRomMLibraryGateway
{
    public Task<int> ResolvePlatformIdAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(19);
    public Task<LibraryPage> BrowseAsync(LibraryQuery query, CancellationToken cancellationToken = default)
    {
        var page = Enumerable.Range(1, total).Skip(query.Offset).Take(query.Limit)
            .Select(i => new RomTile(i, $"Game {i:D4}", $"game{i:D4}.d64", "c64", 174848, covers ? new CoverRef(null, $"/covers/{i}.png") : null, true)).ToList();
        return Task.FromResult(new LibraryPage(page, total, query.Offset, new Dictionary<string, int>()));
    }
    public Task<RomDetail> GetRomAsync(int romId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RomDetail(romId, $"Game {romId:D4}", "summary", "c64", null, Array.Empty<RomFile>(), Array.Empty<int>()));
    public Task<AcquiredGame> DownloadAsync(int romId, string fileName, long expectedSizeBytes, string cacheDir,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No download in the probe.");
}

public sealed class FakeCollectionsGateway : IRomMCollectionsGateway
{
    private readonly List<LibraryCollection> _items = [new(1, "X list", 2, false, new[] { 1, 2 }), new(2, "Y list", 1, false, new[] { 3 })];
    public List<int> Deleted { get; } = new();
    public Task<IReadOnlyList<LibraryCollection>> GetCollectionsAsync(bool includeSmartVirtual, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<LibraryCollection>>(_items.ToList());
    public Task<LibraryCollection> CreateCollectionAsync(string name, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    public Task RenameCollectionAsync(int id, string newName, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    public Task DeleteCollectionAsync(int id, CancellationToken cancellationToken = default) { Deleted.Add(id); _items.RemoveAll(x => x.Id == id); return Task.CompletedTask; }
    public Task AddRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RemoveRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class EmptyRecents : IRecentsStore
{
    public Task<IReadOnlyList<RecentGame>> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RecentGame>>(Array.Empty<RecentGame>());
    public Task RecordAsync(RecentGame game, int capacity = RecentGame.DefaultCapacity, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class NoLauncher : IGameLauncher
{
    public Task<LaunchOutcome> LaunchAsync(AcquiredGame game, MediaSlot slot, bool autostart, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
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
