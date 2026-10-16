using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAssertions;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Avalonia.Views;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(ViceSharp.Heads.Tests.DesktopContractApp))]

namespace ViceSharp.Heads.Tests;

public sealed class DesktopContractApp : Application
{
    public override void Initialize() => Styles.Add(new global::Avalonia.Themes.Fluent.FluentTheme());
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopContractApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>
/// FR-ROMM-AVUI-001 AC-AUI-01 (control presence), FR-ROMM-COVER-001 AC-COVER-03/04,
/// TEST-ROMM-AVUI-001. Mock gate runs the same assertions before
/// real controls exist. Negative controls deliberately corrupt the observed contract.
/// These tests do not claim a live RomM/emulator E2E or the entirety of PLAN-ROMM-001.
/// </summary>
public sealed partial class RommDesktopContractTests
{
    private static bool MockGate => Environment.GetEnvironmentVariable("VICESHARP_ROMM_CONTRACT_MOCKS") == "1";
    private static readonly string[] RequiredControls =
    ["RomMLibraryRail", "RomMGameGrid", "RomMStatus", "RomMSearch", "RomMServers", "RomMDetailCover", "RomMNewList"];
    private static readonly string[] RequiredActions =
    ["Connect", "Scan LAN", "Recents", "Load more", "Attach + play", "Attach", "All games", "CSDb discovery"];

    [AvaloniaFact]
    public void Layout_PreservesNavigationAndConnectionActions()
    {
        using var fixture = new ViewFixture();
        var controls = Descendants(fixture.View).ToArray();
        AssertLayout(controls.Select(c => c.Name ?? string.Empty).ToArray(),
            controls.OfType<Button>().Select(b => b.Content?.ToString() ?? string.Empty).ToArray());
    }

    [Fact]
    public void LayoutAssertions_RejectEveryMissingControlAndAction()
    {
        foreach (string missing in RequiredControls)
        {
            Action check = () => AssertLayout(RequiredControls.Where(x => x != missing).ToArray(), RequiredActions);
            check.Should().Throw<Exception>();
        }
        foreach (string missing in RequiredActions)
        {
            Action check = () => AssertLayout(RequiredControls, RequiredActions.Where(x => x != missing).ToArray());
            check.Should().Throw<Exception>();
        }
    }

    [AvaloniaFact]
    public async Task Cover_LastSelectionWins_EvenIfOldLoadIgnoresCancellation()
    {
        using var slot = CreateSlot();
        var old = new TaskCompletionSource<Bitmap?>();
        var newest = new TaskCompletionSource<Bitmap?>();
        CancellationToken oldToken = default;
        Task first = slot.Set(async ct => { oldToken = ct; return await old.Task; });
        Task second = slot.Set(_ => newest.Task);
        Bitmap winner = MakeBitmap(3);
        newest.SetResult(winner);
        await second;
        old.SetResult(MakeBitmap(2));
        await first;
        AssertCover(slot.Image.Source, winner, oldToken.IsCancellationRequested);
    }

    [AvaloniaFact]
    public async Task Cover_ClearCancelsPending_AndStaleResultCannotReappear()
    {
        using var slot = CreateSlot();
        var pending = new TaskCompletionSource<Bitmap?>();
        CancellationToken token = default;
        Task load = slot.Set(async ct => { token = ct; return await pending.Task; });
        slot.Clear();
        pending.SetResult(MakeBitmap(2));
        await load;
        AssertCover(slot.Image.Source, null, token.IsCancellationRequested);
    }

    [AvaloniaFact]
    public async Task Cover_DetachingVisualCancelsPendingLoad()
    {
        using var slot = CreateSlot();
        var window = new Window { Content = slot.Image };
        window.Show();
        var pending = new TaskCompletionSource<Bitmap?>();
        CancellationToken token = default;
        Task load = slot.Set(async ct => { token = ct; return await pending.Task; });
        window.Content = null;
        if (MockGate) slot.Clear(); // Script the platform detach event at the mock boundary.
        pending.SetResult(MakeBitmap(2));
        await load;
        try { AssertCover(slot.Image.Source, null, token.IsCancellationRequested); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public async Task Cover_FailureShowsFallback_AndNextRequestWorks()
    {
        using var slot = CreateSlot();
        await slot.Set(_ => Task.FromException<Bitmap?>(new IOException("synthetic image failure")));
        slot.Image.Source.Should().BeNull();
        Bitmap recovered = MakeBitmap(4);
        await slot.Set(_ => Task.FromResult<Bitmap?>(recovered));
        slot.Image.Source.Should().BeSameAs(recovered);
        await slot.Set(_ => Task.FromResult<Bitmap?>(null));
        slot.Image.Source.Should().BeNull();
    }

    [AvaloniaFact]
    public void CoverAssertions_RejectStaleImageMissingCancellationAndUnexpectedImage()
    {
        using Bitmap expected = MakeBitmap(3);
        using Bitmap stale = MakeBitmap(2);
        Action wrongImage = () => AssertCover(stale, expected, true);
        Action noCancellation = () => AssertCover(expected, expected, false);
        Action noClear = () => AssertCover(stale, null, true);
        wrongImage.Should().Throw<Exception>();
        noCancellation.Should().Throw<Exception>();
        noClear.Should().Throw<Exception>();
    }

    [Fact]
    public async Task Detail_LastSelectionWins_WhenRequestsCompleteOutOfOrder()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ViceSharp-detail-contract-" + Guid.NewGuid());
        var first = new TaskCompletionSource<RomDetail>();
        var second = new TaskCompletionSource<RomDetail>();
        try
        {
            var gateway = new DetailGateway(first.Task, second.Task);
            var vm = new RomMLibraryViewModel(new NoNetworkTarget(), directory);
            typeof(RomMLibraryViewModel).GetField("_gateway", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(vm, gateway);
            typeof(RomMLibraryViewModel).GetField("_collectionsGateway", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(vm, DispatchProxy.Create<IRomMCollectionsGateway, UnusedCollectionsProxy>());
            int? mockSelection = null;
            int generation = 0;
            async Task Show(int id)
            {
                if (!MockGate) { await vm.ShowDetailAsync(id); return; }
                int request = ++generation;
                var detail = await gateway.GetRomAsync(id);
                if (generation == request) mockSelection = detail.Id;
            }
            Task a = Show(1);
            Task b = Show(2);
            second.SetResult(new RomDetail(2, "second", null, "c64", null, [], []));
            await b;
            first.SetResult(new RomDetail(1, "first", null, "c64", null, [], []));
            await a;
            (MockGate ? mockSelection : vm.SelectedDetail?.Detail.Id).Should().Be(2);
            Action staleControl = () => ((int?)1).Should().Be(2);
            staleControl.Should().Throw<Exception>();
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    public class UnusedCollectionsProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw new InvalidOperationException("No collection operation expected.");
    }

    private sealed class DetailGateway(Task<RomDetail> first, Task<RomDetail> second) : IRomMLibraryGateway
    {
        public Task<RomDetail> GetRomAsync(int romId, CancellationToken cancellationToken = default) => romId == 1 ? first : second;
        public Task<int> ResolvePlatformIdAsync(string slug, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task<LibraryPage> BrowseAsync(LibraryQuery query, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task<AcquiredGame> DownloadAsync(int romId, string fileName, long expectedSizeBytes, string cacheDir,
            IProgress<double>? progress = null, CancellationToken cancellationToken = default) => throw new InvalidOperationException();
    }

    private static void AssertLayout(string[] controls, string[] actions)
    {
        controls.Should().Contain(RequiredControls);
        actions.Should().Contain(RequiredActions);
    }

    private static void AssertCover(object? actual, object? expected, bool canceled)
    {
        actual.Should().BeSameAs(expected);
        canceled.Should().BeTrue();
    }

    private static Bitmap MakeBitmap(int size) => new WriteableBitmap(new PixelSize(size, size),
        new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

    private static IEnumerable<Control> Descendants(Control root)
    {
        yield return root;
        IEnumerable<Control> children = root switch
        {
            Panel panel => panel.Children,
            Decorator decorator when decorator.Child is { } child => [child],
            ContentControl content when content.Content is Control child => [child],
            _ => [],
        };
        foreach (Control child in children)
            foreach (Control descendant in Descendants(child))
                yield return descendant;
    }

    private interface ISlot : IDisposable
    {
        Image Image { get; }
        Task Set(Func<CancellationToken, Task<Bitmap?>> loader);
        void Clear();
    }

    private static ISlot CreateSlot() => MockGate ? new ScriptedSlot() : new RealSlot();

    // Independent model of the observable latest-request contract, never calls production.
    private sealed class ScriptedSlot : ISlot
    {
        public Image Image { get; } = new();
        private CancellationTokenSource? _current;
        public async Task Set(Func<CancellationToken, Task<Bitmap?>> loader)
        {
            Clear();
            var request = _current = new CancellationTokenSource();
            Bitmap? result;
            try { result = await loader(request.Token); }
            catch (IOException) { return; }
            if (request != _current) { result?.Dispose(); return; }
            Image.Source = result;
        }
        public void Clear()
        {
            _current?.Cancel();
            _current = null;
            (Image.Source as IDisposable)?.Dispose();
            Image.Source = null;
        }
        public void Dispose() => Clear();
    }

    private sealed class RealSlot : ISlot
    {
        private readonly object _slot = Activator.CreateInstance(typeof(LibraryView).Assembly
            .GetType("ViceSharp.Avalonia.Views.LibraryCoverImage", throwOnError: true)!)!;
        public Image Image => (Image)_slot;
        public Task Set(Func<CancellationToken, Task<Bitmap?>> loader) =>
            (Task)_slot.GetType().GetMethod("LoadAsync")!.Invoke(_slot, [loader])!;
        public void Clear() => _slot.GetType().GetMethod("Clear")!.Invoke(_slot, null);
        public void Dispose() => Clear();
    }

    private sealed class ViewFixture : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "ViceSharp-RomM-contract-" + Guid.NewGuid());
        public Control View { get; }
        public ViewFixture()
        {
            if (MockGate)
            {
                var panel = new StackPanel();
                foreach (string name in RequiredControls) panel.Children.Add(new Border { Name = name });
                foreach (string action in RequiredActions) panel.Children.Add(new Button { Content = action });
                View = panel;
            }
            else
            {
                var vm = new RomMLibraryViewModel(new NoNetworkTarget(), _directory);
                // Keep the view detached: no network discovery is authorized by this unit test.
                View = new LibraryView(vm);
            }
        }
        public void Dispose()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class NoNetworkTarget : IGameLaunchTarget
    {
        public Task<RpcStatus> DropAndStartFileAsync(string filePath, CancellationToken ct = default) => throw new InvalidOperationException();
        public Task<RpcStatus> AttachFileAsync(MediaSlot slot, string filePath, CancellationToken ct = default) => throw new InvalidOperationException();
    }
}
