using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;

namespace ViceSharp.Library.Tests.Connection;

/// <summary>
/// FR-ROMM-RECENTS-001. Use case: the local Recents list keeps the last N games loaded from RomM
/// (MRU), drops older entries, and reloads from disk after restart.
/// </summary>
[Trait("Category", "Library")]
public sealed class FileRecentsStoreTests
{
    private static RecentGame Game(int id, string name = "G") =>
        new(id, name, $"{id}.d64", "c64", 1000, null, true, DateTimeOffset.UtcNow);

    /// <summary>Record moves an existing id to the front and trims to capacity.</summary>
    [Fact]
    [Trait("AC", "AC-RECENTS-01")]
    public async Task Record_MruAndCapacity()
    {
        var ct = TestContext.Current.CancellationToken;
        DirectoryInfo dir = Directory.CreateTempSubdirectory("vs-romm-recents");
        try
        {
            var store = new FileRecentsStore(Path.Combine(dir.FullName, "recents.json"));

            await store.RecordAsync(Game(1), capacity: 3, ct);
            await store.RecordAsync(Game(2), capacity: 3, ct);
            await store.RecordAsync(Game(3), capacity: 3, ct);
            await store.RecordAsync(Game(4), capacity: 3, ct);
            await store.RecordAsync(Game(2), capacity: 3, ct); // bump 2 to front

            IReadOnlyList<RecentGame> list = await store.LoadAsync(ct);
            list.Select(g => g.Id).Should().Equal(2, 4, 3);
            list.Should().HaveCount(3);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    /// <summary>Load after a new store instance sees the same order (persisted).</summary>
    [Fact]
    [Trait("AC", "AC-RECENTS-01")]
    public async Task Load_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        DirectoryInfo dir = Directory.CreateTempSubdirectory("vs-romm-recents2");
        try
        {
            string path = Path.Combine(dir.FullName, "recents.json");
            var store = new FileRecentsStore(path);
            await store.RecordAsync(Game(9, "Nine"), capacity: 25, ct);

            var reopened = new FileRecentsStore(path);
            IReadOnlyList<RecentGame> list = await reopened.LoadAsync(ct);
            list.Should().ContainSingle();
            list[0].Id.Should().Be(9);
            list[0].Name.Should().Be("Nine");
            list[0].FileName.Should().Be("9.d64");
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
