namespace ViceSharp.Library.ViewModels;

/// <summary>FR-CSDB-001. A single CSDb search result.</summary>
/// <param name="CsdbId">The CSDb entry id.</param>
/// <param name="Title">The release/SID title.</param>
/// <param name="Kind">The classified kind.</param>
/// <param name="TypeLabel">The raw CSDb type label (e.g. "One-File Demo", "SID"), when known.</param>
/// <param name="Source">Where the hit came from ("csdb", "live", "index"), when known.</param>
/// <param name="Url">The CSDb page URL, when known.</param>
public sealed record CsdbHit(
    int CsdbId,
    string Title,
    CsdbKind Kind,
    string? TypeLabel,
    string? Source,
    string? Url);
