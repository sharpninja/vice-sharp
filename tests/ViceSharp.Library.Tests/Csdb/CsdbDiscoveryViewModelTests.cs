using FluentAssertions;
using ViceSharp.Library.ViewModels;
using Xunit;

namespace ViceSharp.Library.Tests.Csdb;

/// <summary>
/// FR-CSDB-001 (AC-CSDB-01/02/05). Use case: the discovery ViewModel searches CSDb, caps the ingest
/// selection, and signals the library to refresh after ingest.
/// </summary>
[Trait("Category", "Library")]
public sealed class CsdbDiscoveryViewModelTests
{
    /// <summary>AC-CSDB-01: search populates the results.</summary>
    [Fact]
    [Trait("AC", "AC-CSDB-01")]
    public async Task Search_ReturnsHits()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeCsdbGateway
        {
            Hits =
            {
                new CsdbHit(1, "Comic Bakery", CsdbKind.Sid, "SID", "csdb", null),
                new CsdbHit(2, "Cybernoid", CsdbKind.Crack, "Crack", "live", null),
            },
        };
        var vm = new CsdbDiscoveryViewModel(gateway) { Query = "c" };

        await vm.SearchAsync(50, ct);

        vm.Results.Should().HaveCount(2);
        vm.Results[0].Title.Should().Be("Comic Bakery");
    }

    /// <summary>AC-CSDB-02: ingest caps the selection at the maximum.</summary>
    [Fact]
    [Trait("AC", "AC-CSDB-02")]
    public async Task Ingest_CapsAt20()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeCsdbGateway();
        var vm = new CsdbDiscoveryViewModel(gateway);

        var selections = Enumerable.Range(1, 25).Select(i => new CsdbSelection(i, CsdbKind.Demo)).ToList();
        await vm.IngestAsync(selections, force: false, ct);

        gateway.IngestCalls.Should().ContainSingle();
        gateway.IngestCalls[0].Should().HaveCount(CsdbDiscoveryViewModel.MaxIngestSelection);
    }

    /// <summary>AC-CSDB-05: a successful ingest raises LibraryRefreshRequested.</summary>
    [Fact]
    [Trait("AC", "AC-CSDB-05")]
    public async Task Ingest_RaisesRefresh()
    {
        var ct = TestContext.Current.CancellationToken;
        var gateway = new FakeCsdbGateway();
        var vm = new CsdbDiscoveryViewModel(gateway);

        int raised = 0;
        vm.LibraryRefreshRequested += (_, _) => raised++;

        await vm.IngestAsync(new[] { new CsdbSelection(1, CsdbKind.Demo) }, force: false, ct);

        raised.Should().Be(1);
    }
}

/// <summary>An in-memory <see cref="ICsdbGateway"/> that records ingest calls.</summary>
internal sealed class FakeCsdbGateway : ICsdbGateway
{
    public List<CsdbHit> Hits { get; } = new();

    public List<IReadOnlyList<CsdbSelection>> IngestCalls { get; } = new();

    public Task<IReadOnlyList<CsdbHit>> SearchAsync(
        string query,
        IReadOnlyList<CsdbKind>? kinds,
        int limit,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CsdbHit>>(Hits.ToList());

    public Task<CsdbIngestResult> IngestAndScanAsync(
        IReadOnlyList<CsdbSelection> selections,
        bool force,
        CancellationToken cancellationToken = default)
    {
        IngestCalls.Add(selections);
        return Task.FromResult(new CsdbIngestResult(selections.Count, 0, 0, Scanned: true));
    }
}
