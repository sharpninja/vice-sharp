using ViceSharp.Library.ViewModels;

namespace ViceSharp.Library.Tests.Collections;

/// <summary>An in-memory <see cref="IRomMCollectionsGateway"/> that records mutations and refreshes.</summary>
internal sealed class FakeCollectionsGateway : IRomMCollectionsGateway
{
    private readonly List<LibraryCollection> _state;

    public FakeCollectionsGateway(IEnumerable<LibraryCollection>? state = null) =>
        _state = state?.ToList() ?? new List<LibraryCollection>();

    public int GetCalls { get; private set; }

    public List<string> Created { get; } = new();

    public List<(int Id, IReadOnlyList<int> Roms)> Added { get; } = new();

    public List<(int Id, IReadOnlyList<int> Roms)> Removed { get; } = new();

    public Task<IReadOnlyList<LibraryCollection>> GetCollectionsAsync(bool includeSmartVirtual, CancellationToken cancellationToken = default)
    {
        GetCalls++;
        return Task.FromResult<IReadOnlyList<LibraryCollection>>(_state.ToList());
    }

    public Task<LibraryCollection> CreateCollectionAsync(string name, CancellationToken cancellationToken = default)
    {
        Created.Add(name);
        var collection = new LibraryCollection(_state.Count + 1, name, 0, false, Array.Empty<int>());
        _state.Add(collection);
        return Task.FromResult(collection);
    }

    public Task RenameCollectionAsync(int id, string newName, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task DeleteCollectionAsync(int id, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task AddRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default)
    {
        Added.Add((id, romIds));
        return Task.CompletedTask;
    }

    public Task RemoveRomsAsync(int id, IReadOnlyList<int> romIds, CancellationToken cancellationToken = default)
    {
        Removed.Add((id, romIds));
        return Task.CompletedTask;
    }
}
