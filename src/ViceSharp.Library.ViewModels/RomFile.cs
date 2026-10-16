namespace ViceSharp.Library.ViewModels;

/// <summary>FR-ROMM-DETAIL-001. One downloadable file that belongs to a ROM.</summary>
/// <param name="FileName">The file name (RomM <c>file_name</c> / <c>fs_name</c>).</param>
/// <param name="SizeBytes">The file size in bytes (0 when unknown).</param>
/// <param name="Kind">The media nature resolved from the extension.</param>
/// <param name="Launchable">Whether this file can be attached and booted.</param>
public sealed record RomFile(string FileName, long SizeBytes, MediaKind Kind, bool Launchable);
