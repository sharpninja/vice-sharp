using ViceSharp.Library.ViewModels;
using Csdb = RomM.Client.Csdb;

namespace ViceSharp.RomM;

/// <summary>
/// FR-CSDB-001 (AC-CSDB-01/03). The co-located CSDb gateway: searches CSDb via <see cref="Csdb.ICsdbClient"/>
/// and ingests the selection through <see cref="Csdb.ICsdbRomMWorkflow"/> (which writes the files into the
/// locally-writable RomM roms root and then triggers a RomM scan). Used where the roms root is writable
/// (desktop); Xbox/remote use <see cref="BridgeCsdbGateway"/>.
/// </summary>
public sealed class LocalCsdbGateway : ICsdbGateway
{
    private readonly Csdb.ICsdbClient _client;
    private readonly Csdb.ICsdbRomMWorkflow _workflow;

    /// <summary>Creates the gateway.</summary>
    /// <param name="client">The CSDb search client.</param>
    /// <param name="workflow">The CSDb-to-RomM ingest workflow.</param>
    public LocalCsdbGateway(Csdb.ICsdbClient client, Csdb.ICsdbRomMWorkflow workflow)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CsdbHit>> SearchAsync(
        string query,
        IReadOnlyList<CsdbKind>? kinds,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var request = new Csdb.CsdbSearchRequest(
            query,
            kinds?.Select(ToPackageKind).ToList(),
            limit);

        IReadOnlyList<Csdb.CsdbSearchHit> hits =
            await _client.SearchAsync(request, cancellationToken).ConfigureAwait(false);

        return hits
            .Select(h => new CsdbHit(h.CsdbId, h.Title, ToLibraryKind(h.Kind), h.CsdbType, "csdb", h.Url))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<CsdbIngestResult> IngestAndScanAsync(
        IReadOnlyList<CsdbSelection> selections,
        bool force,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selections);

        var packageSelections = selections
            .Select(s => new Csdb.CsdbSelection(s.CsdbId, ToPackageKind(s.Kind)))
            .ToList();

        Csdb.CsdbIngestAndScanResult result = await _workflow
            .IngestSelectedAsync(packageSelections, scanAfterIngest: true, new Csdb.CsdbIngestOptions { Force = force }, cancellationToken)
            .ConfigureAwait(false);

        return MapResult(result);
    }

    internal static CsdbIngestResult MapResult(Csdb.CsdbIngestAndScanResult result)
    {
        int ingested = 0;
        int skipped = 0;
        int failed = 0;

        foreach (Csdb.CsdbIngestItemResult item in result.Ingest.Items)
        {
            switch (item.Status)
            {
                case "ok":
                    ingested++;
                    break;
                case "skipped":
                    skipped++;
                    break;
                default:
                    failed++;
                    break;
            }
        }

        return new CsdbIngestResult(ingested, skipped, failed, result.ScanCompleted);
    }

    private static Csdb.CsdbKind ToPackageKind(CsdbKind kind) => kind switch
    {
        CsdbKind.Demo => Csdb.CsdbKind.Demo,
        CsdbKind.Crack => Csdb.CsdbKind.Crack,
        CsdbKind.Sid => Csdb.CsdbKind.Sid,
        _ => Csdb.CsdbKind.Other,
    };

    private static CsdbKind ToLibraryKind(Csdb.CsdbKind kind) => kind switch
    {
        Csdb.CsdbKind.Demo => CsdbKind.Demo,
        Csdb.CsdbKind.Crack => CsdbKind.Crack,
        Csdb.CsdbKind.Sid => CsdbKind.Sid,
        _ => CsdbKind.Other,
    };
}
