using ViceSharp.Library.ViewModels;
using ViceSharp.Protocol;

namespace ViceSharp.Library.Tests.Browse;

/// <summary>An in-memory <see cref="IRomMLibraryGateway"/> that pages a backing list and records calls.</summary>
internal sealed class FakeLibraryGateway : IRomMLibraryGateway
{
    private readonly List<RomTile> _backing;
    private readonly IReadOnlyDictionary<string, int> _charIndex;
    private readonly IReadOnlyDictionary<string, int> _platformIds;

    public FakeLibraryGateway(
        IEnumerable<RomTile>? backing = null,
        IReadOnlyDictionary<string, int>? charIndex = null,
        IReadOnlyDictionary<string, int>? platformIds = null)
    {
        _backing = backing?.ToList() ?? new List<RomTile>();
        _charIndex = charIndex ?? new Dictionary<string, int>();
        _platformIds = platformIds ?? new Dictionary<string, int> { ["c64"] = 15, ["c128"] = 20 };
    }

    public List<string> ResolveCalls { get; } = new();

    public List<LibraryQuery> BrowseCalls { get; } = new();

    public Task<int> ResolvePlatformIdAsync(string slug, CancellationToken cancellationToken = default)
    {
        ResolveCalls.Add(slug);
        return Task.FromResult(_platformIds.TryGetValue(slug, out int id) ? id : 15);
    }

    public Task<LibraryPage> BrowseAsync(LibraryQuery query, CancellationToken cancellationToken = default)
    {
        BrowseCalls.Add(query);
        var items = _backing.Skip(query.Offset).Take(query.Limit).ToList();
        return Task.FromResult(new LibraryPage(items, _backing.Count, query.Offset, _charIndex));
    }

    public Task<RomDetail> GetRomAsync(int romId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RomDetail(romId, "n", null, "c64", null, Array.Empty<RomFile>(), Array.Empty<int>()));

    public Task<AcquiredGame> DownloadAsync(
        int romId,
        string fileName,
        long expectedSizeBytes,
        string cacheDir,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(0.5);
        progress?.Report(1.0);
        MediaKind kind = MediaExtensionMap.Resolve(fileName)?.Kind ?? MediaKind.Program;
        return Task.FromResult(new AcquiredGame(Path.Combine(cacheDir, romId.ToString(), fileName), fileName, kind));
    }
}

/// <summary>An <see cref="IGameLauncher"/> that records each launch.</summary>
internal sealed class FakeGameLauncher : IGameLauncher
{
    public sealed record Call(AcquiredGame Game, MediaSlot Slot, bool Autostart);

    public List<Call> Calls { get; } = new();

    public Task<LaunchOutcome> LaunchAsync(
        AcquiredGame game,
        MediaSlot slot,
        bool autostart,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(new Call(game, slot, autostart));
        return Task.FromResult(new LaunchOutcome(true, "Ready"));
    }
}

/// <summary>A settable <see cref="ICurrentMachineProvider"/> that can raise its change event.</summary>
internal sealed class FakeMachineProvider : ICurrentMachineProvider
{
    public string Slug { get; set; } = "c64";

    public string GetActivePlatformSlug() => Slug;

    public event EventHandler? PlatformChanged;

    public void Raise() => PlatformChanged?.Invoke(this, EventArgs.Empty);
}
