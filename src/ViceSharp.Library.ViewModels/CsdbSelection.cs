namespace ViceSharp.Library.ViewModels;

/// <summary>FR-CSDB-001. A CSDb entry chosen for ingest.</summary>
/// <param name="CsdbId">The CSDb entry id.</param>
/// <param name="Kind">The entry kind (governs how it is ingested).</param>
public sealed record CsdbSelection(int CsdbId, CsdbKind Kind);
