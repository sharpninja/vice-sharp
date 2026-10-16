using FluentAssertions;
using ViceSharp.Library.ViewModels;
using ViceSharp.RomM;
using Xunit;
using CsdbPkg = RomM.Client.Csdb;

namespace ViceSharp.Library.Tests.Csdb;

/// <summary>
/// FR-CSDB-001 (AC-CSDB-03). Use case: the co-located gateway ingests the selection through the
/// CSDb-to-RomM workflow (which writes the files then triggers a scan).
/// </summary>
[Trait("Category", "Library")]
public sealed class LocalCsdbGatewayTests
{
    /// <summary>AC-CSDB-03: ingest maps the selection and requests the write-then-scan workflow.</summary>
    [Fact]
    [Trait("AC", "AC-CSDB-03")]
    public async Task Ingest_WritesThenScans()
    {
        var ct = TestContext.Current.CancellationToken;
        var workflow = new FakeCsdbWorkflow();
        var gateway = new LocalCsdbGateway(new FakeCsdbClient(), workflow);

        CsdbIngestResult result = await gateway.IngestAndScanAsync(
            new[] { new CsdbSelection(101, CsdbKind.Demo), new CsdbSelection(202, CsdbKind.Sid) },
            force: true,
            ct);

        workflow.LastScanAfter.Should().BeTrue();
        workflow.LastOptions!.Force.Should().BeTrue();
        workflow.LastSelections!.Select(s => s.CsdbId).Should().Equal(101, 202);
        workflow.LastSelections!.Select(s => s.Kind).Should().Equal(CsdbPkg.CsdbKind.Demo, CsdbPkg.CsdbKind.Sid);
        result.Ingested.Should().Be(2);
        result.Scanned.Should().BeTrue();
    }
}

/// <summary>A CSDb search client that returns nothing (search is not exercised by this test).</summary>
internal sealed class FakeCsdbClient : CsdbPkg.ICsdbClient
{
    public Task<IReadOnlyList<CsdbPkg.CsdbSearchHit>> SearchAsync(CsdbPkg.CsdbSearchRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CsdbPkg.CsdbSearchHit>>(Array.Empty<CsdbPkg.CsdbSearchHit>());

    public Task<CsdbPkg.CsdbReleaseDetail> GetReleaseAsync(int csdbId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<CsdbPkg.CsdbSidDetail> GetSidAsync(int csdbId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<(byte[] Data, string? FileName)> DownloadBytesAsync(string url, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

/// <summary>A CSDb-to-RomM workflow that records the ingest call and returns an all-ok result.</summary>
internal sealed class FakeCsdbWorkflow : CsdbPkg.ICsdbRomMWorkflow
{
    public IReadOnlyList<CsdbPkg.CsdbSelection>? LastSelections { get; private set; }

    public bool LastScanAfter { get; private set; }

    public CsdbPkg.CsdbIngestOptions? LastOptions { get; private set; }

    public Task<CsdbPkg.CsdbIngestAndScanResult> IngestSelectedAsync(
        IReadOnlyList<CsdbPkg.CsdbSelection> selections,
        bool scanAfterIngest = true,
        CsdbPkg.CsdbIngestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        LastSelections = selections;
        LastScanAfter = scanAfterIngest;
        LastOptions = options;

        var items = selections
            .Select(s => new CsdbPkg.CsdbIngestItemResult(s.CsdbId, s.Kind, "title", "ok", Array.Empty<string>()))
            .ToList();
        var ingest = new CsdbPkg.CsdbIngestResult("job1", selections.Count, items);
        return Task.FromResult(new CsdbPkg.CsdbIngestAndScanResult(ingest, scanAfterIngest, scanAfterIngest));
    }
}
