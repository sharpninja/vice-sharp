namespace ViceSharp.Library.ViewModels;

/// <summary>FR-CSDB-001. The outcome of a CSDb ingest-and-scan.</summary>
/// <param name="Ingested">The number of entries successfully written into the RomM library.</param>
/// <param name="Skipped">The number of entries skipped (already present).</param>
/// <param name="Failed">The number of entries that failed to ingest.</param>
/// <param name="Scanned">Whether a RomM library scan completed after the ingest.</param>
public sealed record CsdbIngestResult(int Ingested, int Skipped, int Failed, bool Scanned);
