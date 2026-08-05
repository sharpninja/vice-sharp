using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using Xunit;

namespace ViceSharp.Library.Tests.Browse;

/// <summary>
/// FR-ROMM-BROWSE/LAUNCH-001. Use case: the browser pages the library, jumps by letter, debounces
/// search, re-scopes when the machine changes, and downloads + launches the selection with a two-phase
/// status.
/// </summary>
[Trait("Category", "Library")]
public sealed class LibraryBrowseViewModelTests
{
    private static readonly string CacheDir = Path.Combine(Path.GetTempPath(), "vs-romm-vm");

    private static RomTile Tile(int id, string file, bool launchable = true, long size = 1000) =>
        new(id, $"Game {id}", file, "c64", size, null, launchable);

    private static List<RomTile> Backing(int count) =>
        Enumerable.Range(1, count).Select(i => Tile(i, $"g{i}.d64")).ToList();

    /// <summary>AC-BROWSE-04: LoadMore appends the next page; HasMore is false once all are loaded.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-04")]
    public async Task Paging_AppendsToEnd()
    {
        var ct = TestContext.Current.CancellationToken;
        var gw = new FakeLibraryGateway(Backing(5));
        var vm = new LibraryBrowseViewModel(gw, new FakeGameLauncher(), new FakeMachineProvider(), CacheDir, pageSize: 2);

        await vm.InitializeAsync(ct);
        vm.Items.Should().HaveCount(2);
        vm.Total.Should().Be(5);
        vm.HasMore.Should().BeTrue();

        await vm.LoadMoreAsync(ct);
        vm.Items.Should().HaveCount(4);
        vm.HasMore.Should().BeTrue();

        await vm.LoadMoreAsync(ct);
        vm.Items.Should().HaveCount(5);
        vm.HasMore.Should().BeFalse();

        await vm.LoadMoreAsync(ct); // no-op once exhausted
        vm.Items.Should().HaveCount(5);
        vm.Items.Select(t => t.Id).Should().Equal(1, 2, 3, 4, 5);
    }

    /// <summary>AC-BROWSE-05: JumpToLetter loads the page at the char-index offset.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-05")]
    public async Task Jump_UsesCharIndex()
    {
        var ct = TestContext.Current.CancellationToken;
        var charIndex = new Dictionary<string, int> { ["A"] = 0, ["M"] = 2, ["T"] = 4 };
        var gw = new FakeLibraryGateway(Backing(5), charIndex);
        var vm = new LibraryBrowseViewModel(gw, new FakeGameLauncher(), new FakeMachineProvider(), CacheDir, pageSize: 2);

        await vm.InitializeAsync(ct);
        await vm.JumpToLetterAsync('m', ct);

        vm.Items.Should().HaveCount(2);
        vm.Items[0].Id.Should().Be(3);
    }

    /// <summary>AC-BROWSE-06: rapid search input coalesces to a single query with the final term.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-06")]
    public async Task Search_Debounced()
    {
        var ct = TestContext.Current.CancellationToken;
        var gw = new FakeLibraryGateway(Backing(5));
        var vm = new LibraryBrowseViewModel(
            gw, new FakeGameLauncher(), new FakeMachineProvider(), CacheDir, debounce: TimeSpan.FromMilliseconds(30));

        await vm.InitializeAsync(ct);
        gw.BrowseCalls.Clear();

        vm.SearchText = "b";
        vm.SearchText = "bo";
        vm.SearchText = "bou";
        await vm.PendingSearch;

        gw.BrowseCalls.Should().HaveCount(1);
        gw.BrowseCalls[0].SearchTerm.Should().Be("bou");
    }

    /// <summary>AC-BROWSE-07: a machine change re-scopes to the new platform and reloads from offset 0.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-07")]
    public async Task MachineChange_RescopesAndReloads()
    {
        var ct = TestContext.Current.CancellationToken;
        var machine = new FakeMachineProvider { Slug = "c64" };
        var gw = new FakeLibraryGateway(Backing(5));
        var vm = new LibraryBrowseViewModel(gw, new FakeGameLauncher(), machine, CacheDir);

        await vm.InitializeAsync(ct);
        gw.ResolveCalls.Clear();
        gw.BrowseCalls.Clear();

        machine.Slug = "c128";
        machine.Raise();
        await vm.PendingRescope;

        gw.ResolveCalls.Should().Contain("c128");
        gw.BrowseCalls.Should().NotBeEmpty();
        gw.BrowseCalls[^1].PlatformId.Should().Be(20);
        gw.BrowseCalls[^1].Offset.Should().Be(0);
    }

    /// <summary>AC-LAUNCH-03: a non-launchable selection (.prg) disables attach and no-ops AttachAsync.</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-03")]
    public async Task Prg_AttachDisabled()
    {
        var ct = TestContext.Current.CancellationToken;
        var launcher = new FakeGameLauncher();
        var vm = new LibraryBrowseViewModel(new FakeLibraryGateway(), launcher, new FakeMachineProvider(), CacheDir)
        {
            SelectedTile = Tile(9, "prog.prg", launchable: false),
        };

        vm.CanAttach.Should().BeFalse();

        await vm.AttachAsync(autostart: false, ct);
        launcher.Calls.Should().BeEmpty();
    }

    /// <summary>AC-LAUNCH-04: attach+start invokes the launcher with the resolved slot and autostart flag.</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-04")]
    public async Task AttachStart_InvokesLauncher()
    {
        var ct = TestContext.Current.CancellationToken;
        var launcher = new FakeGameLauncher();
        var vm = new LibraryBrowseViewModel(new FakeLibraryGateway(), launcher, new FakeMachineProvider(), CacheDir)
        {
            SelectedTile = Tile(2, "b.d64", launchable: true, size: 1000),
        };

        vm.CanAttach.Should().BeTrue();

        await vm.AttachAsync(autostart: true, ct);

        launcher.Calls.Should().ContainSingle();
        launcher.Calls[0].Slot.Should().Be(MediaSlot.Drive8);
        launcher.Calls[0].Autostart.Should().BeTrue();
        launcher.Calls[0].Game.FileName.Should().Be("b.d64");
    }

    /// <summary>AC-LAUNCH-07: the status goes through "Downloading N%" before "Starting".</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-07")]
    public async Task Launch_TwoPhaseStatus()
    {
        var ct = TestContext.Current.CancellationToken;
        var vm = new LibraryBrowseViewModel(new FakeLibraryGateway(), new FakeGameLauncher(), new FakeMachineProvider(), CacheDir);

        var statuses = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LibraryBrowseViewModel.StatusMessage))
            {
                statuses.Add(vm.StatusMessage);
            }
        };

        vm.SelectedTile = Tile(2, "b.d64", launchable: true, size: 1000);
        await vm.AttachAsync(autostart: true, ct);

        int downloadIndex = statuses.FindIndex(s => s.StartsWith("Downloading", StringComparison.Ordinal));
        int startIndex = statuses.IndexOf("Starting");

        downloadIndex.Should().BeGreaterThanOrEqualTo(0);
        startIndex.Should().BeGreaterThan(downloadIndex);
    }
}
