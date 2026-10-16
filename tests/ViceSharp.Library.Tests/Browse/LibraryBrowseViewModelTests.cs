using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using ViceSharp.Library.Tests.Adapter;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Browse;

/// <summary>
/// FR-ROMM-BROWSE/LAUNCH-001. Use case: the browser pages the library, collapses same-name variants,
/// jumps by letter, debounces search, re-scopes when the machine changes, and downloads + launches
/// the selection with a two-phase status.
/// </summary>
[Trait("Category", "Library")]
public sealed class LibraryBrowseViewModelTests
{
    private static readonly string CacheDir = Path.Combine(Path.GetTempPath(), "vs-romm-vm");

    private static RomTile Tile(int id, string file, bool launchable = true, long size = 1000, string? name = null) =>
        new(id, name ?? $"Game {id}", file, "c64", size, null, launchable);

    private static List<RomTile> Backing(int count) =>
        Enumerable.Range(1, count).Select(i => Tile(i, $"g{i}.d64")).ToList();

    /// <summary>AC-BROWSE-04: LoadMore appends the next page; HasMore is false once all ROMs are loaded.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-04")]
    public async Task Paging_AppendsToEnd()
    {
        var ct = TestContext.Current.CancellationToken;
        var gw = new FakeLibraryGateway(Backing(5));
        var vm = new LibraryBrowseViewModel(gw, new FakeGameLauncher(), new FakeMachineProvider(), CacheDir, pageSize: 2);

        await vm.InitializeAsync(ct);
        vm.Items.Should().HaveCount(2);
        vm.LoadedRomCount.Should().Be(2);
        vm.Total.Should().Be(5);
        vm.HasMore.Should().BeTrue();

        await vm.LoadMoreAsync(ct);
        vm.Items.Should().HaveCount(4);
        vm.LoadedRomCount.Should().Be(4);
        vm.HasMore.Should().BeTrue();

        await vm.LoadMoreAsync(ct);
        vm.Items.Should().HaveCount(5);
        vm.LoadedRomCount.Should().Be(5);
        vm.HasMore.Should().BeFalse();

        await vm.LoadMoreAsync(ct); // no-op once exhausted
        vm.Items.Should().HaveCount(5);
        vm.Items.Select(g => g.Primary.Id).Should().Equal(1, 2, 3, 4, 5);
    }

    /// <summary>Same-name ROMs on one page collapse to a single group with all variants.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-04")]
    public async Task Paging_GroupsSameNameVariants()
    {
        var ct = TestContext.Current.CancellationToken;
        List<RomTile> backing =
        [
            Tile(1, "a.t64", name: "64 Breakout"),
            Tile(2, "b.t64", name: "64 Breakout"),
            Tile(3, "c.t64", name: "64 Breakout"),
            Tile(4, "d.d64", name: "Other"),
            Tile(5, "e.d64", name: "Other"),
        ];
        var gw = new FakeLibraryGateway(backing);
        var vm = new LibraryBrowseViewModel(gw, new FakeGameLauncher(), new FakeMachineProvider(), CacheDir, pageSize: 10);

        await vm.InitializeAsync(ct);

        vm.Items.Should().HaveCount(2);
        vm.LoadedRomCount.Should().Be(5);
        vm.Items[0].Name.Should().Be("64 Breakout");
        vm.Items[0].VariantCount.Should().Be(3);
        vm.Items[0].Subtitle.Should().Be("3 variants");
        vm.Items[1].Name.Should().Be("Other");
        vm.Items[1].VariantCount.Should().Be(2);
    }

    /// <summary>Page-boundary siblings merge into the previous group on LoadMore.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-04")]
    public async Task Paging_MergesVariantsAcrossPages()
    {
        var ct = TestContext.Current.CancellationToken;
        List<RomTile> backing =
        [
            Tile(1, "a.t64", name: "Same"),
            Tile(2, "b.t64", name: "Same"),
            Tile(3, "c.t64", name: "Same"),
            Tile(4, "d.d64", name: "Next"),
        ];
        var gw = new FakeLibraryGateway(backing);
        var vm = new LibraryBrowseViewModel(gw, new FakeGameLauncher(), new FakeMachineProvider(), CacheDir, pageSize: 2);

        await vm.InitializeAsync(ct);
        vm.Items.Should().HaveCount(1);
        vm.Items[0].VariantCount.Should().Be(2);
        vm.LoadedRomCount.Should().Be(2);

        // Second page continues "Same" then starts "Next" - boundary merges into the first group.
        await vm.LoadMoreAsync(ct);
        vm.LoadedRomCount.Should().Be(4);
        vm.Items.Should().HaveCount(2);
        vm.Items[0].Name.Should().Be("Same");
        vm.Items[0].VariantCount.Should().Be(3);
        vm.Items[1].Name.Should().Be("Next");
        vm.Items[1].VariantCount.Should().Be(1);
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
        vm.Items[0].Primary.Id.Should().Be(3);
    }

    /// <summary>
    /// AC-BROWSE-05: after an A-Z jump, LoadMore must continue from jumpOffset+pageSize, not from 0.
    /// Regression: pagination restarted at the library head, so H + LoadMore mixed in A/3-D titles.
    /// </summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-05")]
    public async Task Jump_ThenLoadMore_ContinuesFromJumpOffset()
    {
        var ct = TestContext.Current.CancellationToken;
        // 10 uniquely named tiles; M starts at index 4.
        List<RomTile> backing = Enumerable.Range(0, 10)
            .Select(i => Tile(i + 1, $"g{i}.d64", name: $"Game{i}"))
            .ToList();
        var charIndex = new Dictionary<string, int> { ["G"] = 0, ["M"] = 4 };
        // Rename so char-index M is meaningful: keep backing order, jump at offset 4.
        var gw = new FakeLibraryGateway(backing, charIndex);
        var vm = new LibraryBrowseViewModel(gw, new FakeGameLauncher(), new FakeMachineProvider(), CacheDir, pageSize: 2);

        await vm.InitializeAsync(ct);
        gw.BrowseCalls.Clear();

        await vm.JumpToLetterAsync('m', ct);
        gw.BrowseCalls.Should().ContainSingle();
        gw.BrowseCalls[0].Offset.Should().Be(4);
        vm.Items.Select(g => g.Primary.Id).Should().Equal(5, 6);

        gw.BrowseCalls.Clear();
        await vm.LoadMoreAsync(ct);

        gw.BrowseCalls.Should().ContainSingle();
        gw.BrowseCalls[0].Offset.Should().Be(6, "LoadMore after jump must use jump offset + already loaded ROM count");
        vm.Items.Select(g => g.Primary.Id).Should().Equal(5, 6, 7, 8);
    }

    /// <summary>
    /// AC-BROWSE-05: RomM 5.x returns lowercase char_index keys (a, m, ...). Jump must still resolve.
    /// </summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-05")]
    public async Task Jump_UsesLowercaseCharIndexKeys()
    {
        var ct = TestContext.Current.CancellationToken;
        var charIndex = new Dictionary<string, int> { ["a"] = 0, ["m"] = 2, ["t"] = 4, ["#"] = 0 };
        var gw = new FakeLibraryGateway(Backing(5), charIndex);
        var vm = new LibraryBrowseViewModel(gw, new FakeGameLauncher(), new FakeMachineProvider(), CacheDir, pageSize: 2);

        await vm.InitializeAsync(ct);
        await vm.JumpToLetterAsync('M', ct);

        vm.Items.Should().HaveCount(2);
        vm.Items[0].Primary.Id.Should().Be(3);
    }

    /// <summary>AC-BROWSE-05: the # strip entry jumps using the # char-index key.</summary>
    [Fact]
    [Trait("AC", "AC-BROWSE-05")]
    public async Task Jump_HashUsesHashKey()
    {
        var ct = TestContext.Current.CancellationToken;
        var charIndex = new Dictionary<string, int> { ["#"] = 1, ["a"] = 3 };
        var gw = new FakeLibraryGateway(Backing(5), charIndex);
        var vm = new LibraryBrowseViewModel(gw, new FakeGameLauncher(), new FakeMachineProvider(), CacheDir, pageSize: 2);

        await vm.InitializeAsync(ct);
        await vm.JumpToLetterAsync('#', ct);

        vm.Items.Should().HaveCount(2);
        vm.Items[0].Primary.Id.Should().Be(2);
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

    /// <summary>AC-LAUNCH-03: unsupported disks stay browsable but cannot download or launch.</summary>
    [Theory]
    [InlineData("g64", false)]
    [InlineData("d71", false)]
    [InlineData("d81", false)]
    [InlineData("G64", true)]
    [InlineData("D71", true)]
    [InlineData("D81", true)]
    [Trait("AC", "AC-LAUNCH-03")]
    public async Task UnsupportedDisk_AttachDisabled(string extension, bool autostart)
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new FakeRomMHandler(request => request.RequestUri!.AbsolutePath == "/api/roms"
            ? FakeRomMHandler.Json(RomMFixtures.RomsPageJson.Replace("boulderdash.d64", $"boulderdash.{extension}"))
            : RomMFixtures.DefaultRouter(request));
        await using var client = RomMFixtures.Client(handler);
        var launcher = new FakeGameLauncher();
        using var vm = new LibraryBrowseViewModel(
            new RomMLibraryGateway(client), launcher, new FakeMachineProvider(), CacheDir);
        await vm.InitializeAsync(ct);
        vm.SelectedTile = vm.Items.Single(group => group.Primary.Id == 101).Primary;

        vm.SelectedTile.Launchable.Should().BeFalse();
        vm.CanAttach.Should().BeFalse();
        LaunchOutcome outcome = await vm.AttachAsync(autostart, ct);

        outcome.Success.Should().BeFalse();
        launcher.Calls.Should().BeEmpty();
        handler.CountPathPrefix("/api/roms/101/content/").Should().Be(0);
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

    /// <summary>FR-ROMM-RECENTS-001: a successful attach is recorded in the Recents store.</summary>
    [Fact]
    [Trait("AC", "AC-RECENTS-01")]
    public async Task Attach_RecordsRecent()
    {
        var ct = TestContext.Current.CancellationToken;
        var recents = new MemoryRecentsStore();
        var vm = new LibraryBrowseViewModel(
            new FakeLibraryGateway(),
            new FakeGameLauncher(),
            new FakeMachineProvider(),
            CacheDir,
            recents: recents)
        {
            SelectedTile = Tile(2, "b.d64", launchable: true, size: 1000),
        };

        LaunchOutcome outcome = await vm.AttachAsync(autostart: false, ct);

        outcome.Success.Should().BeTrue();
        IReadOnlyList<RecentGame> list = await recents.LoadAsync(ct);
        list.Should().ContainSingle();
        list[0].Id.Should().Be(2);
        list[0].FileName.Should().Be("b.d64");
    }

    /// <summary>FR-ROMM-RECENTS-001: ShowRecents replaces the grid with stored entries and disables paging.</summary>
    [Fact]
    [Trait("AC", "AC-RECENTS-01")]
    public async Task ShowRecents_LoadsLocalList()
    {
        var ct = TestContext.Current.CancellationToken;
        var recents = new MemoryRecentsStore();
        await recents.RecordAsync(new RecentGame(7, "Seven", "7.d64", "c64", 10, null, true, DateTimeOffset.UtcNow), 25, ct);
        await recents.RecordAsync(new RecentGame(8, "Eight", "8.d64", "c64", 10, null, true, DateTimeOffset.UtcNow), 25, ct);

        var gw = new FakeLibraryGateway(Backing(5));
        var vm = new LibraryBrowseViewModel(gw, new FakeGameLauncher(), new FakeMachineProvider(), CacheDir, recents: recents);
        await vm.InitializeAsync(ct);
        // Backing has 5 unique names; default pageSize 60 loads all 5.
        vm.Items.Should().HaveCount(5);

        await vm.ShowRecentsAsync(ct);

        vm.IsShowingRecents.Should().BeTrue();
        vm.HasMore.Should().BeFalse();
        vm.Items.Select(g => g.Primary.Id).Should().Equal(8, 7);
    }

    private sealed class MemoryRecentsStore : IRecentsStore
    {
        private readonly List<RecentGame> _items = new();

        public Task<IReadOnlyList<RecentGame>> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RecentGame>>(_items.ToList());

        public Task RecordAsync(
            RecentGame game,
            int capacity = RecentGame.DefaultCapacity,
            CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(g => g.Id == game.Id);
            _items.Insert(0, game);
            if (_items.Count > capacity)
            {
                _items.RemoveRange(capacity, _items.Count - capacity);
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _items.Clear();
            return Task.CompletedTask;
        }
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

    /// <summary>
    /// FR-ROMM-LAUNCH-001, AC-LAUNCH-01/04/07: a T64 attach failure after download or cache reuse
    /// becomes an outcome and visible status; retry reuses the intact file and records only success.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Trait("AC", "AC-LAUNCH-01")]
    [Trait("AC", "AC-LAUNCH-04")]
    [Trait("AC", "AC-LAUNCH-07")]
    public async Task AttachT64_LaunchFailure_ReportsOutcomeAndRetries(bool cached, bool throws)
    {
        var ct = TestContext.Current.CancellationToken;
        byte[] payload = CreateSyntheticT64();
        var handler = new FakeRomMHandler(_ => FakeRomMHandler.Bytes(payload));
        await using var client = RomMFixtures.Client(handler);
        DirectoryInfo cache = Directory.CreateTempSubdirectory("vs-romm-t64-launch-");
        string cachedPath = Path.Combine(cache.FullName, "101", "synthetic.t64");
        const string failure = "True drive is unavailable.";
        int launches = 0;
        var launcher = new CallbackGameLauncher((game, slot, autostart, _) =>
        {
            game.LocalPath.Should().Be(cachedPath);
            game.Kind.Should().Be(MediaKind.Tape);
            slot.Should().Be(MediaSlot.Drive8);
            autostart.Should().BeTrue();
            File.ReadAllBytes(game.LocalPath).Should().Equal(payload);
            if (++launches == 1)
            {
                return throws
                    ? Task.FromException<LaunchOutcome>(new InvalidOperationException(failure))
                    : Task.FromResult(new LaunchOutcome(false, failure));
            }

            return Task.FromResult(new LaunchOutcome(true, "Ready"));
        });
        var recents = new MemoryRecentsStore();
        using var vm = new LibraryBrowseViewModel(
            new RomMLibraryGateway(client), launcher, new FakeMachineProvider(), cache.FullName, recents: recents)
        {
            SelectedTile = Tile(101, "synthetic.t64", size: payload.Length),
        };

        try
        {
            if (cached)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachedPath)!);
                await File.WriteAllBytesAsync(cachedPath, payload, ct);
            }

            LaunchOutcome failed = await vm.AttachAsync(autostart: true, ct);

            failed.Success.Should().BeFalse();
            failed.Message.Should().Contain(failure);
            vm.StatusMessage.Should().Be(failed.Message);
            vm.CanAttach.Should().BeTrue("a failed launch must remain retryable");
            (await recents.LoadAsync(ct)).Should().BeEmpty();
            File.ReadAllBytes(cachedPath).Should().Equal(payload);
            handler.CountPathPrefix("/api/roms/101/content/").Should().Be(cached ? 0 : 1);

            LaunchOutcome retried = await vm.AttachAsync(autostart: true, ct);

            retried.Success.Should().BeTrue();
            vm.StatusMessage.Should().Be("Ready");
            launches.Should().Be(2);
            handler.CountPathPrefix("/api/roms/101/content/").Should().Be(cached ? 0 : 1);
            (await recents.LoadAsync(ct)).Should().ContainSingle().Which.Id.Should().Be(101);
        }
        finally
        {
            cache.Delete(recursive: true);
        }
    }

    /// <summary>
    /// FR-ROMM-LAUNCH-001, AC-LAUNCH-01/07: an incomplete T64 download reports failure without
    /// invoking the launcher; a later complete download can still attach successfully.
    /// </summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-01")]
    [Trait("AC", "AC-LAUNCH-07")]
    public async Task AttachT64_DownloadFailure_ReportsOutcomeAndRetries()
    {
        var ct = TestContext.Current.CancellationToken;
        byte[] payload = CreateSyntheticT64();
        bool truncated = true;
        var handler = new FakeRomMHandler(_ => FakeRomMHandler.Bytes(truncated ? payload[..^1] : payload));
        await using var client = RomMFixtures.Client(handler);
        var launcher = new FakeGameLauncher();
        DirectoryInfo cache = Directory.CreateTempSubdirectory("vs-romm-t64-download-");
        using var vm = new LibraryBrowseViewModel(
            new RomMLibraryGateway(client), launcher, new FakeMachineProvider(), cache.FullName)
        {
            SelectedTile = Tile(101, "synthetic.t64", size: payload.Length),
        };

        try
        {
            LaunchOutcome failed = await vm.AttachAsync(autostart: true, ct);

            failed.Success.Should().BeFalse();
            vm.StatusMessage.Should().Be(failed.Message);
            failed.Message.Should().Contain("Download failed");
            launcher.Calls.Should().BeEmpty();
            File.Exists(Path.Combine(cache.FullName, "101", "synthetic.t64")).Should().BeFalse();

            truncated = false;
            LaunchOutcome retried = await vm.AttachAsync(autostart: true, ct);

            retried.Success.Should().BeTrue();
            vm.StatusMessage.Should().Be("Ready");
            launcher.Calls.Should().ContainSingle();
            File.ReadAllBytes(launcher.Calls[0].Game.LocalPath).Should().Equal(payload);
            handler.CountPathPrefix("/api/roms/101/content/").Should().Be(2);
        }
        finally
        {
            cache.Delete(recursive: true);
        }
    }

    /// <summary>AC-LAUNCH-07: caller cancellation is a visible failed outcome, safe for UI handlers.</summary>
    [Fact]
    [Trait("AC", "AC-LAUNCH-07")]
    public async Task Attach_CanceledLaunch_ReturnsCanceledOutcome()
    {
        using var cancellation = new CancellationTokenSource();
        var launcher = new CallbackGameLauncher((_, _, _, _) =>
        {
            cancellation.Cancel();
            return Task.FromCanceled<LaunchOutcome>(cancellation.Token);
        });
        using var vm = new LibraryBrowseViewModel(
            new FakeLibraryGateway(), launcher, new FakeMachineProvider(), CacheDir)
        {
            SelectedTile = Tile(101, "synthetic.t64"),
        };

        LaunchOutcome outcome = await vm.AttachAsync(autostart: true, cancellation.Token);

        outcome.Success.Should().BeFalse();
        outcome.Message.Should().Be("Launch canceled.");
        vm.StatusMessage.Should().Be(outcome.Message);
    }

    private static byte[] CreateSyntheticT64()
    {
        // One PRG directory entry containing only the two-byte BASIC end marker; no game data.
        byte[] image = new byte[98];
        Encoding.ASCII.GetBytes("C64 tape image file").CopyTo(image, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(32), 0x0101);
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(34), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(36), 1);
        image[64] = 1;
        image[65] = 0x82;
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(66), 0x0801);
        BinaryPrimitives.WriteUInt16LittleEndian(image.AsSpan(68), 0x0803);
        BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(72), 96);
        Encoding.ASCII.GetBytes("SYNTHETIC").CopyTo(image, 80);
        return image;
    }

    private sealed class CallbackGameLauncher(
        Func<AcquiredGame, MediaSlot, bool, CancellationToken, Task<LaunchOutcome>> launch) : IGameLauncher
    {
        public Task<LaunchOutcome> LaunchAsync(
            AcquiredGame game,
            MediaSlot slot,
            bool autostart,
            CancellationToken cancellationToken = default) => launch(game, slot, autostart, cancellationToken);
    }
}
