using System.Net;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Avalonia.Views;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using Xunit;

namespace ViceSharp.Heads.Tests;

public sealed partial class RommDesktopContractTests
{
    private readonly ITestOutputHelper _output;
    public RommDesktopContractTests(ITestOutputHelper output) => _output = output;

    // FR-ROMM-AVUI-001 AC-AUI-01/03/04 (changed desktop behaviors only),
    // FR-ROMM-COVER-001 AC-COVER-01/03/04; FR-ROMM-COLLECT-001 AC-COLLECT-02/04.
    // R1 F01-F04 regression contracts. Scripted observations are independent, contract-derived
    // fixtures. The same assertions and omission/corruption controls bind to actual UI observations.
    // Neither these tests nor the fixture claim real-server/emulator E2E or all-project completion.
    public static TheoryData<string> DesktopCases => new()
    {
        "grid", "tab", "credentials", "tiles", "collections", "repeat-navigation",
        "collection-failure", "detail-guard", "auto-connect", "loader", "loader-invalid",
        "list-race", "grid-resize", "grid-keyboard", "delete-guard", "collection-create", "tile-page-boundary",
        "grid-scroll-focus", "tile-layout",
    };

    private static Dictionary<string, string> ScriptedObservation(string scenario) => scenario switch
    {
        "grid" => new() { ["initial"] = "6", ["paged"] = "6", ["items"] = "180", ["oldPending"] = "0", ["last"] = "179" },
        "grid-resize" => new() { ["narrow"] = "4", ["wide"] = "12", ["empty"] = "0", ["last"] = "59" },
        "grid-keyboard" => new() { ["right"] = "1", ["end"] = "59", ["home"] = "0" },
        "grid-scroll-focus" => new() { ["unrealized"] = "True", ["focusRetained"] = "True", ["right"] = "4", ["visible"] = "True" },
        "tile-layout" => new() { ["normalFits"] = "True", ["longFits"] = "True" },
        "tab" => new() { ["selected"] = "3", ["primary"] = "4", ["attach"] = "True", ["sameLoader"] = "True", ["closedDisposed"] = "True" },
        "credentials" => new() { ["base"] = "http://127.0.0.1:9/", ["token"] = "contract-token", ["sameLoader"] = "True", ["replaced"] = "True", ["oldDisposed"] = "True" },
        "tiles" => new() { ["title"] = "Grouped game", ["subtitle"] = "2 variants", ["primary"] = "2" },
        "collections" => new() { ["tab"] = "1", ["visible"] = "1", ["selected"] = "1", ["members"] = "1,2", ["deleted"] = "1" },
        "repeat-navigation" => new() { ["tab"] = "1", ["visible"] = "1" },
        "collection-failure" => new() { ["status"] = "Library operation failed: synthetic create failure", ["enabled"] = "True", ["createCalls"] = "1" },
        "detail-guard" => new() { ["name"] = "", ["addCalls"] = "0" },
        "auto-connect" => new() { ["before"] = "0", ["after"] = "1", ["return"] = "1" },
        "loader" => new() { ["auth"] = "http://server.invalid/root/cover.png|Bearer contract-token", ["anon"] = "http://cdn.invalid/cover.png|", ["requests"] = "2", ["disposed"] = "2", ["malformed"] = "null", ["canceled"] = "null" },
        "loader-invalid" => new() { ["rejected"] = "3", ["absolutePathRequests"] = "0" },
        "list-race" => new() { ["members"] = "3", ["selectedTile"] = "null" },
        "delete-guard" => new() { ["mismatchDeletes"] = "0", ["missingDeletes"] = "0" },
        "collection-create" => new() { ["text"] = "", ["enabled"] = "True", ["newName"] = "New collection", ["createCalls"] = "1" },
        "tile-page-boundary" => new() { ["before"] = "60 variants", ["after"] = "120 variants", ["selected"] = "0" },
        _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
    };

    private static void AssertDesktopObservation(string scenario, Dictionary<string, string> actual)
    {
        var expected = ScriptedObservation(scenario);
        actual.Keys.Should().BeEquivalentTo(expected.Keys);
        foreach (var (key, value) in expected)
        {
            if ((scenario == "grid" && key is "initial" or "paged") ||
                (scenario == "grid-resize" && key is "narrow" or "wide"))
                int.Parse(actual[key]).Should().BeInRange(1, 30, "realization is bounded by the viewport, not total item count");
            else actual[key].Should().Be(value, $"{scenario}/{key} must match the contract");
        }
    }

    [Theory, MemberData(nameof(DesktopCases))]
    public void BehaviorAssertions_RejectEveryCorruptOrMissingObservation(string scenario)
    {
        var correct = ScriptedObservation(scenario);
        AssertDesktopObservation(scenario, correct);
        foreach (string key in correct.Keys)
        {
            var corrupt = new Dictionary<string, string>(correct) { [key] = "incorrect" };
            Action check = () => AssertDesktopObservation(scenario, corrupt);
            check.Should().Throw<Exception>();
            var missing = new Dictionary<string, string>(correct);
            missing.Remove(key);
            check = () => AssertDesktopObservation(scenario, missing);
            check.Should().Throw<Exception>();
        }
        if (scenario == "grid")
        {
            var nonVirtualized = new Dictionary<string, string>(correct) { ["initial"] = "60", ["paged"] = "180" };
            Action check = () => AssertDesktopObservation(scenario, nonVirtualized);
            check.Should().Throw<Exception>();
        }
    }

    [AvaloniaTheory, MemberData(nameof(DesktopCases))]
    public async Task DesktopBehavior_MatchesContract(string scenario)
    {
        var actual = MockGate ? ScriptedObservation(scenario) : await ObserveDesktopAsync(scenario);
        _output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { scenario, provider = MockGate ? "scripted" : "production", observations = actual }));
        AssertDesktopObservation(scenario, actual);
    }

    private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static object? Field(object instance, string name) => instance.GetType().GetField(name, InstanceMembers)!.GetValue(instance);
    private static void Field(object instance, string name, object? value) => instance.GetType().GetField(name, InstanceMembers)!.SetValue(instance, value);
    private static void Property(object instance, string name, object? value) => instance.GetType().GetProperty(name, InstanceMembers)!.SetValue(instance, value);
    private static T ControlNamed<T>(Visual root, string name) where T : Control => root.GetVisualDescendants().OfType<T>().Single(x => x.Name == name);
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Layout(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static bool Disposed(object loader)
    {
        try { ((HttpClient)Field(loader, "_authenticated")!).CancelPendingRequests(); return false; }
        catch (ObjectDisposedException) { return true; }
    }

    private static async Task<Dictionary<string, string>> ObserveDesktopAsync(string scenario)
    {
        if (scenario.StartsWith("loader", StringComparison.Ordinal)) return await ObserveLoaderAsync(scenario);
        using var f = new DesktopFixture();
        await f.InitializeAsync();
        if (scenario == "auto-connect")
        {
            Property(f.Vm, "Browse", null);
            Field(f.Vm, "_autoConnectTried", false);
            var store = new CountingConnectionStore();
            Field(f.Vm, "_connectionStore", store);
            var detached = new LibraryView(f.Vm);
            int before = store.Loads;
            f.Window.Content = detached;
            f.Window.Show(); Layout(f.Window);
            int after = store.Loads;
            f.Window.Content = null; f.Window.Content = detached; Layout(f.Window);
            return new() { ["before"] = before.ToString(), ["after"] = after.ToString(), ["return"] = store.Loads.ToString() };
        }
        f.Show();
        var grid = ControlNamed<ListBox>(f.View, "RomMGameGrid");
        if (scenario == "grid-scroll-focus")
        {
            grid.SelectedIndex = 3; Layout(f.Window);
            grid.ContainerFromIndex(3)!.Focus(); Layout(f.Window);
            grid.ScrollIntoView(59); Layout(f.Window);
            bool unrealized = grid.ContainerFromIndex(3) is null;
            bool focusRetained = grid.IsKeyboardFocusWithin;
            // Send physical input to the window's actual focus target, not directly to the grid.
            f.Window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);
            f.Window.KeyRelease(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null); Layout(f.Window);
            return new() { ["unrealized"] = unrealized.ToString(), ["focusRetained"] = focusRetained.ToString(),
                ["right"] = grid.SelectedIndex.ToString(), ["visible"] = (grid.ContainerFromIndex(4) is not null).ToString() };
        }
        if (scenario == "tile-layout")
        {
            bool Fits()
            {
                var tile = grid.ContainerFromIndex(0)!;
                var slot = tile.Bounds.Size;
                tile.Measure(Size.Infinity);
                return tile.DesiredSize.Width <= slot.Width && tile.DesiredSize.Height <= slot.Height;
            }
            bool normalFits = Fits();
            f.Browse.Items.Clear();
            f.Browse.Items.Add(new GameGroup("A long title that must stay inside one tile", [
                new(1, "A long title that must stay inside one tile", new string('x', 200) + ".d64", "c64", null, null, true)]));
            Layout(f.Window);
            return new() { ["normalFits"] = normalFits.ToString(), ["longFits"] = Fits().ToString() };
        }
        if (scenario == "tile-page-boundary")
        {
            f.Gateway.BoundaryVariants = true; await f.ReplaceBrowseAsync(); Layout(f.Window);
            grid.SelectedIndex = 0;
            string Subtitle() => grid.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text?.EndsWith(" variants", StringComparison.Ordinal) == true).Text!;
            string before = Subtitle();
            Click(f.View.GetVisualDescendants().OfType<Button>().Single(b => b.Content?.ToString() == "Load more")); Layout(f.Window);
            return new() { ["before"] = before, ["after"] = Subtitle(), ["selected"] = grid.SelectedIndex.ToString() };
        }
        if (scenario == "delete-guard")
        {
            f.Tabs.SelectedIndex = 1; Layout(f.Window);
            await f.Lists.SelectCollectionAsync(1); Layout(f.Window);
            f.Vm.Collections!.SelectedCollection = f.Vm.Collections.Collections.Single(c => c.Id == 2);
            var delete = f.Lists.GetVisualDescendants().OfType<Button>().Single(b => b.Content?.ToString() == "Delete");
            Click(delete); Layout(f.Window); int mismatch = f.Collections.Deleted.Count;
            await f.Lists.SelectCollectionAsync(999); Layout(f.Window); Click(delete); Layout(f.Window);
            return new() { ["mismatchDeletes"] = mismatch.ToString(), ["missingDeletes"] = f.Collections.Deleted.Count.ToString() };
        }
        if (scenario == "collection-create")
        {
            var input = ControlNamed<TextBox>(f.View, "RomMNewList"); input.Text = " New collection ";
            input.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter }); Layout(f.Window);
            return new() { ["text"] = input.Text ?? "", ["enabled"] = input.IsEnabled.ToString(), ["newName"] = f.Vm.Collections!.Collections.Last().Name, ["createCalls"] = f.Collections.Creates.ToString() };
        }
        if (scenario == "grid")
        {
            int initial = grid.GetVisualDescendants().OfType<LibraryCoverImage>().Count();
            await f.Vm.LoadMoreAsync(); await f.Vm.LoadMoreAsync(); Layout(f.Window);
            var images = grid.GetVisualDescendants().OfType<LibraryCoverImage>().ToArray();
            var pending = new TaskCompletionSource<Bitmap?>();
            var tokens = new List<CancellationToken>();
            var loads = images.Select(i => i.LoadAsync(ct => { tokens.Add(ct); return pending.Task; })).ToArray();
            grid.ScrollIntoView(179); Layout(f.Window);
            int uncanceled = tokens.Count(t => !t.IsCancellationRequested);
            pending.SetResult(null); await Task.WhenAll(loads);
            return new() { ["initial"] = initial.ToString(), ["paged"] = images.Length.ToString(), ["items"] = grid.ItemCount.ToString(),
                ["oldPending"] = uncanceled.ToString(), ["last"] = grid.IndexFromContainer(grid.ContainerFromIndex(179)!).ToString() };
        }
        if (scenario == "grid-resize")
        {
            f.Window.Width = 760; Layout(f.Window);
            int narrow = grid.GetRealizedContainers().Count();
            f.Window.Width = 1500; Layout(f.Window);
            int wide = grid.GetRealizedContainers().Count();
            grid.ScrollIntoView(59); Layout(f.Window);
            int last = grid.IndexFromContainer(grid.ContainerFromIndex(59)!);
            f.Browse.Items.Clear(); Layout(f.Window);
            return new() { ["narrow"] = narrow.ToString(), ["wide"] = wide.ToString(), ["last"] = last.ToString(), ["empty"] = grid.GetRealizedContainers().Count().ToString() };
        }
        if (scenario == "grid-keyboard")
        {
            grid.SelectedIndex = 0; grid.Focus();
            void KeyPress(Key key) { grid.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key }); Layout(f.Window); }
            KeyPress(Key.Right); int right = grid.SelectedIndex;
            KeyPress(Key.End); int end = grid.SelectedIndex;
            KeyPress(Key.Home);
            return new() { ["right"] = right.ToString(), ["end"] = end.ToString(), ["home"] = grid.SelectedIndex.ToString() };
        }
        if (scenario == "tab")
        {
            grid.SelectedIndex = 3; Layout(f.Window);
            var loader = Field(f.View, "_coverLoader")!;
            f.Tabs.SelectedIndex = 1; Layout(f.Window);
            f.Tabs.SelectedIndex = 0; Layout(f.Window);
            var result = new Dictionary<string, string> { ["selected"] = grid.SelectedIndex.ToString(), ["primary"] = f.Browse.SelectedGroup?.Primary.Id.ToString() ?? "null",
                ["attach"] = f.Browse.CanAttach.ToString(), ["sameLoader"] = ReferenceEquals(loader, Field(f.View, "_coverLoader")).ToString() };
            f.Window.Close(); result["closedDisposed"] = Disposed(loader).ToString();
            return result;
        }
        if (scenario == "credentials")
        {
            var loader = Field(f.View, "_coverLoader")!;
            f.Vm.BaseUrl = "http://wrong.invalid/"; f.Vm.Token = "wrong";
            var client = (HttpClient)Field(loader, "_authenticated")!;
            var result = new Dictionary<string, string> { ["base"] = client.BaseAddress!.ToString(), ["token"] = client.DefaultRequestHeaders.Authorization!.Parameter!,
                ["sameLoader"] = ReferenceEquals(loader, Field(f.View, "_coverLoader")).ToString() };
            await f.ReplaceBrowseAsync(); Layout(f.Window);
            result["replaced"] = (!ReferenceEquals(loader, Field(f.View, "_coverLoader"))).ToString();
            result["oldDisposed"] = Disposed(loader).ToString(); return result;
        }
        if (scenario == "tiles")
        {
            f.Browse.Items.Clear();
            f.Browse.Items.Add(new GameGroup("Grouped game", [new(1, "Grouped game", "bad.txt", "c64", null, null, false), new(2, "Grouped game", "good.d64", "c64", null, null, true)]));
            Layout(f.Window); grid.SelectedIndex = 0; Layout(f.Window);
            string[] texts = grid.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();
            return new() { ["title"] = texts.FirstOrDefault(t => t == "Grouped game") ?? "missing", ["subtitle"] = texts.FirstOrDefault(t => t == "2 variants") ?? "missing", ["primary"] = f.Browse.SelectedGroup?.Primary.Id.ToString() ?? "null" };
        }
        if (scenario is "collections" or "repeat-navigation")
        {
            f.Tabs.SelectedIndex = 1; Layout(f.Window);
            var listsRail = (ListBox)Field(f.Lists, "_collectionsList")!;
            listsRail.SelectedItem = listsRail.Items.Cast<LibraryCollection>().Single(c => c.Id == 2); Layout(f.Window);
            f.Tabs.SelectedIndex = 0; Layout(f.Window);
            var libraryRail = (ListBox)Field(f.View, "_collectionsList")!;
            libraryRail.SelectedItem = f.Vm.Collections!.Collections.Single(c => c.Id == 1); Layout(f.Window);
            if (scenario == "repeat-navigation")
            {
                f.Tabs.SelectedIndex = 0; Layout(f.Window);
                libraryRail.SelectedItem = f.Vm.Collections.Collections.Single(c => c.Id == 1); Layout(f.Window);
                return new() { ["tab"] = f.Tabs.SelectedIndex.ToString(), ["visible"] = (listsRail.SelectedItem as LibraryCollection)?.Id.ToString() ?? "null" };
            }
            var result = new Dictionary<string, string> { ["tab"] = f.Tabs.SelectedIndex.ToString(), ["visible"] = (listsRail.SelectedItem as LibraryCollection)?.Id.ToString() ?? "null",
                ["selected"] = f.Vm.Collections.SelectedCollection?.Id.ToString() ?? "null", ["members"] = string.Join(',', f.Vm.ListMemberTiles.Select(t => t.Id)) };
            Click(f.Lists.GetVisualDescendants().OfType<Button>().Single(b => b.Content?.ToString() == "Delete")); Layout(f.Window);
            // If the UI offers a confirmation, explicitly accept the named collection.
            var confirm = f.Lists.GetVisualDescendants().OfType<Button>().SingleOrDefault(b => b.Name == "RomMConfirmDelete");
            if (confirm?.IsVisible == true) { Click(confirm); Layout(f.Window); }
            result["deleted"] = string.Join(',', f.Collections.Deleted); return result;
        }
        if (scenario == "collection-failure")
        {
            var input = ControlNamed<TextBox>(f.View, "RomMNewList"); input.Text = "fail";
            input.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter }); Layout(f.Window);
            return new() { ["status"] = ControlNamed<TextBlock>(f.View, "RomMStatus").Text!, ["enabled"] = input.IsEnabled.ToString(), ["createCalls"] = f.Collections.Creates.ToString() };
        }
        if (scenario == "detail-guard")
        {
            grid.SelectedIndex = 0; Layout(f.Window);
            Property(f.Vm, "SelectedDetail", new RomDetailViewModel(new RomDetail(999, "wrong", null, "c64", null, [], []), f.Collections));
            ((ComboBox)Field(f.View, "_listPicker")!).SelectedItem = f.Vm.Collections!.Collections[0];
            Click((Button)Field(f.View, "_addToList")!); Layout(f.Window);
            return new() { ["name"] = ((TextBlock)Field(f.View, "_detailName")!).Text!, ["addCalls"] = f.Collections.Adds.ToString() };
        }
        if (scenario == "list-race")
        {
            var deferred = new TaskCompletionSource<RomDetail>(); f.Gateway.Deferred = deferred;
            Task first = f.Vm.LoadListMembersAsync(f.Vm.Collections!.Collections[0]);
            Task second = f.Vm.LoadListMembersAsync(f.Vm.Collections.Collections[1]); await second;
            deferred.SetResult(new RomDetail(1, "old", null, "c64", null, [], [])); await first;
            return new() { ["members"] = string.Join(',', f.Vm.ListMemberTiles.Select(t => t.Id)), ["selectedTile"] = f.Vm.SelectedListTile?.Id.ToString() ?? "null" };
        }
        throw new ArgumentOutOfRangeException(nameof(scenario));
    }

    private static async Task<Dictionary<string, string>> ObserveLoaderAsync(string scenario)
    {
        var auth = new RecordingHandler(); var anonymous = new RecordingHandler();
        using var loader = (AvaloniaCoverImageLoader)Activator.CreateInstance(typeof(AvaloniaCoverImageLoader), InstanceMembers, null,
            [new Uri("http://server.invalid/root/"), "contract-token", auth, anonymous], null)!;
        if (scenario == "loader-invalid")
        {
            int rejected = 0;
            foreach (Uri invalid in new[] { new Uri("relative", UriKind.Relative), new Uri("file:///tmp/a"), new Uri("ftp://server.invalid/") })
            { try { using var unexpected = new AvaloniaCoverImageLoader(invalid, null); } catch (ArgumentException) { rejected++; } }
            using var result = await loader.LoadCoverAsync(new CoverRef(null, "https://other.invalid/private.png"));
            return new() { ["rejected"] = rejected.ToString(), ["absolutePathRequests"] = auth.Requests.Count.ToString() };
        }
        using var first = await loader.LoadCoverAsync(new CoverRef(null, "cover.png"));
        using var second = await loader.LoadCoverAsync(new CoverRef("http://cdn.invalid/cover.png", null));
        using var canceled = await loader.LoadCoverAsync(new CoverRef(null, "cancel.png"), new CancellationToken(true));
        loader.Dispose();
        return new() { ["auth"] = auth.Requests.Single(), ["anon"] = anonymous.Requests.Single(), ["requests"] = (auth.Requests.Count + anonymous.Requests.Count).ToString(),
            ["disposed"] = (auth.Disposals + anonymous.Disposals).ToString(), ["malformed"] = first is null && second is null ? "null" : "bitmap", ["canceled"] = canceled is null ? "null" : "bitmap" };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];
        public int Disposals { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri + "|" + request.Headers.Authorization);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        }
        protected override void Dispose(bool disposing) { if (disposing) Disposals++; base.Dispose(disposing); }
    }

    private sealed class CountingConnectionStore : IRomMConnectionStore
    {
        public int Loads { get; private set; }
        public Task<RomMConnection?> LoadAsync(CancellationToken cancellationToken = default) { Loads++; throw new IOException("synthetic connection store failure; no LAN access"); }
        public Task SaveAsync(RomMConnection connection, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task ClearAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    }

    private sealed class DesktopFixture : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "ViceSharp-desktop-behavior-" + Guid.NewGuid());
        public RomMLibraryViewModel Vm { get; }
        public LibraryBrowseViewModel Browse { get; private set; } = null!;
        public BehaviorGateway Gateway { get; } = new();
        public BehaviorCollections Collections { get; } = new();
        public Window Window { get; } = new() { Width = 1000, Height = 600 };
        public TabControl Tabs { get; } = new();
        public LibraryView View { get; private set; } = null!;
        public ListsView Lists { get; private set; } = null!;
        public DesktopFixture()
        {
            Vm = new RomMLibraryViewModel(new NoNetworkTarget(), Path.Combine(_directory, "cache"));
            Field(Vm, "_autoConnectTried", true); Field(Vm, "_gateway", Gateway); Field(Vm, "_collectionsGateway", Collections);
            Property(Vm, "ConnectedServerUrl", new Uri("http://127.0.0.1:9/")); Property(Vm, "ConnectedToken", "contract-token");
        }
        public async Task InitializeAsync()
        {
            await ReplaceBrowseAsync();
            var collections = new CollectionsViewModel(Collections); await collections.RefreshAsync(); Property(Vm, "Collections", collections);
            View = new LibraryView(Vm); Lists = new ListsView(Vm);
            Tabs.Items.Add(new TabItem { Header = "Library", Content = View }); Tabs.Items.Add(new TabItem { Header = "Lists", Content = Lists });
            Window.Content = Tabs;
        }
        public async Task ReplaceBrowseAsync()
        {
            Browse = new LibraryBrowseViewModel(Gateway, new NeverLauncher(), new DesktopMachine(), _directory);
            await Browse.InitializeAsync(); Property(Vm, "Browse", Browse);
        }
        public void Show() { Window.Show(); Layout(Window); }
        public void Dispose() { Window.Close(); if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    }
    private sealed class BehaviorGateway : IRomMLibraryGateway
    {
        public TaskCompletionSource<RomDetail>? Deferred { get; set; }
        public bool BoundaryVariants { get; set; }
        public Task<int> ResolvePlatformIdAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(19);
        public Task<LibraryPage> BrowseAsync(LibraryQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new LibraryPage(
            Enumerable.Range(1, 600).Skip(query.Offset).Take(query.Limit).Select(i => new RomTile(i, BoundaryVariants ? "Same title" : $"Game {i:D4}", $"game{i:D4}.d64", "c64", 174848, null, true)).ToList(), 600, query.Offset, new Dictionary<string, int>()));
        public Task<RomDetail> GetRomAsync(int romId, CancellationToken cancellationToken = default) => romId == 1 && Deferred is not null ? Deferred.Task :
            Task.FromResult(new RomDetail(romId, $"Game {romId:D4}", "summary", "c64", null, [], []));
        public Task<AcquiredGame> DownloadAsync(int romId, string fileName, long expectedSizeBytes, string cacheDir, IProgress<double>? progress = null, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    }
    private sealed class BehaviorCollections : IRomMCollectionsGateway
    {
        private readonly List<LibraryCollection> _items = [new(1, "X", 2, false, [1, 2]), new(2, "Y", 1, false, [3])];
        public List<int> Deleted { get; } = [];
        public int Creates { get; private set; }
        public int Adds { get; private set; }
        public Task<IReadOnlyList<LibraryCollection>> GetCollectionsAsync(bool includeSmartVirtual, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LibraryCollection>>(_items.ToArray());
        public Task<LibraryCollection> CreateCollectionAsync(string name, CancellationToken cancellationToken = default)
        {
            Creates++;
            if (name == "fail") throw new IOException("synthetic create failure");
            var created = new LibraryCollection(3, name, 0, false, []); _items.Add(created); return Task.FromResult(created);
        }
        public Task RenameCollectionAsync(int id, string newName, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task DeleteCollectionAsync(int id, CancellationToken cancellationToken = default) { Deleted.Add(id); _items.RemoveAll(x => x.Id == id); return Task.CompletedTask; }
        public Task AddRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default) { Adds++; return Task.CompletedTask; }
        public Task RemoveRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class NeverLauncher : IGameLauncher
    {
        public Task<LaunchOutcome> LaunchAsync(AcquiredGame game, MediaSlot slot, bool autostart, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    }
    private sealed class DesktopMachine : ICurrentMachineProvider
    {
        public string GetActivePlatformSlug() => "c64";
        public event EventHandler PlatformChanged { add { } remove { } }
    }
}
