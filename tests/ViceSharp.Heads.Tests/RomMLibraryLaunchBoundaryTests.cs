using System.Reflection;
using FluentAssertions;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;
using Xunit;

namespace ViceSharp.Heads.Tests;

/// <summary>
/// FR-ROMM-LAUNCH-001 / AC-LAUNCH-06: desktop attach-button operations report failures through
/// observable status and remain usable. Synthetic collaborators never contact RomM or load game files.
/// </summary>
[Trait("Category", "Heads")]
public sealed class RomMLibraryLaunchBoundaryTests
{
    /// <summary>Failed attachment or autostart updates UI status and does not prevent the next launch.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("AC", "AC-LAUNCH-06")]
    public async Task AttachFailure_UpdatesObservableStatus_AndAllowsNextGame(bool autostart)
    {
        using var fixture = new LaunchFixture();
        fixture.Target.Failure = new InvalidOperationException("Synthetic host unavailable.");
        var statuses = new List<string>();
        fixture.ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RomMLibraryViewModel.Status))
                statuses.Add(fixture.ViewModel.Status);
        };

        LaunchOutcome failed = await fixture.ViewModel.AttachAsync(autostart, TestContext.Current.CancellationToken);

        failed.Success.Should().BeFalse();
        fixture.ViewModel.Status.Should().Contain("first.t64").And.Contain("Synthetic host unavailable.");
        statuses.Should().Contain(fixture.ViewModel.Status);
        fixture.Recents.LoadCalls.Should().Be(0);

        fixture.Target.Failure = null;
        fixture.Browse.SelectedTile = fixture.Second;
        LaunchOutcome retry = await fixture.ViewModel.AttachAsync(autostart, TestContext.Current.CancellationToken);

        retry.Success.Should().BeTrue();
        fixture.Target.LastPath.Should().EndWith("second.t64");
        fixture.ViewModel.Status.Should().Be("Synthetic launch complete.");
        fixture.Recents.LoadCalls.Should().Be(1);
    }

    /// <summary>
    /// FR-ROMM-RECENTS-001: a failed postlaunch Recents refresh warns in status while preserving
    /// the successful launch, rather than escaping the attach button's async event handler.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("AC", "AC-LAUNCH-06")]
    public async Task RecentsRefreshFailure_PreservesLaunchSuccess_AndAllowsNextGame(bool autostart)
    {
        using var fixture = new LaunchFixture();
        fixture.Recents.Failure = new IOException("Synthetic recents read failed.");

        LaunchOutcome launched = await fixture.ViewModel.AttachAsync(autostart, TestContext.Current.CancellationToken);

        launched.Success.Should().BeTrue();
        launched.Message.Should().Be("Synthetic launch complete.");
        fixture.ViewModel.Status.Should().Contain("Recent games could not be refreshed")
            .And.Contain("Synthetic recents read failed.");

        fixture.Recents.Failure = null;
        fixture.Browse.SelectedTile = fixture.Second;
        (await fixture.ViewModel.AttachAsync(autostart, TestContext.Current.CancellationToken))
            .Success.Should().BeTrue();
        fixture.Target.LastPath.Should().EndWith("second.t64");
        fixture.ViewModel.Status.Should().Be("Synthetic launch complete.");
    }

    private sealed class LaunchFixture : IDisposable
    {
        private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("vs-romm-launch-boundary-");

        public LaunchFixture()
        {
            string cache = Path.Combine(_directory.FullName, "cache");
            ViewModel = new RomMLibraryViewModel(Target, cache);
            Browse = new LibraryBrowseViewModel(new FakeGateway(), new AvaloniaGameLauncher(Target),
                new FakeMachine(), cache);
            Browse.SelectedTile = new RomTile(1, "First", "first.t64", "c64", 64, null, true);

            // The production constructor still owns platform persistence. Replace only private
            // collaborators for this network-free desktop-boundary regression.
            typeof(RomMLibraryViewModel).GetProperty(nameof(RomMLibraryViewModel.Browse))!
                .SetValue(ViewModel, Browse);
            typeof(RomMLibraryViewModel).GetField("_recentsStore", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(ViewModel, Recents);
        }

        public FakeTarget Target { get; } = new();
        public FakeRecents Recents { get; } = new();
        public RomMLibraryViewModel ViewModel { get; }
        public LibraryBrowseViewModel Browse { get; }
        public RomTile Second { get; } = new(2, "Second", "second.t64", "c64", 64, null, true);

        public void Dispose()
        {
            Browse.Dispose();
            _directory.Delete(recursive: true);
        }
    }

    private sealed class FakeTarget : IGameLaunchTarget
    {
        public Exception? Failure { get; set; }
        public string? LastPath { get; private set; }

        public Task<RpcStatus> DropAndStartFileAsync(string filePath, CancellationToken ct = default) =>
            AttachFileAsync(MediaSlot.Tape, filePath, ct);

        public Task<RpcStatus> AttachFileAsync(MediaSlot slot, string filePath, CancellationToken ct = default)
        {
            LastPath = filePath;
            return Failure is { } error
                ? Task.FromException<RpcStatus>(error)
                : Task.FromResult(new RpcStatus(RpcStatusCode.Ok, "Synthetic launch complete."));
        }
    }

    private sealed class FakeRecents : IRecentsStore
    {
        public Exception? Failure { get; set; }
        public int LoadCalls { get; private set; }

        public Task<IReadOnlyList<RecentGame>> LoadAsync(CancellationToken cancellationToken = default)
        {
            LoadCalls++;
            return Failure is { } error
                ? Task.FromException<IReadOnlyList<RecentGame>>(error)
                : Task.FromResult<IReadOnlyList<RecentGame>>(Array.Empty<RecentGame>());
        }

        public Task RecordAsync(RecentGame game, int capacity = RecentGame.DefaultCapacity,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMachine : ICurrentMachineProvider
    {
        public string GetActivePlatformSlug() => "c64";
        public event EventHandler PlatformChanged { add { } remove { } }
    }

    private sealed class FakeGateway : IRomMLibraryGateway
    {
        public Task<int> ResolvePlatformIdAsync(string slug, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<LibraryPage> BrowseAsync(LibraryQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<RomDetail> GetRomAsync(int romId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AcquiredGame> DownloadAsync(int romId, string fileName, long expectedSizeBytes,
            string cacheDir, IProgress<double>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AcquiredGame(Path.Combine(cacheDir, fileName), fileName, MediaKind.Tape));
    }
}
