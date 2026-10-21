namespace ViceSharp.ViceCompatibility.SnapshotFixtures;

/// <summary>One ROM file of a launch checked against the allowlist.</summary>
/// <param name="RelativePath">Path relative to the VICE data root, with forward slashes (the allowlist key).</param>
/// <param name="Sha256">Upper-case SHA-256 of the file, or null when the file is missing.</param>
/// <param name="Expected">The allowlisted hash for that path, or null when the path is not allowlisted.</param>
/// <param name="Ok">True only when the file exists, the path is allowlisted and the hashes are equal.</param>
/// <param name="Reason">Why the check failed, or "ok".</param>
public sealed record RomCheck(string RelativePath, string? Sha256, string? Expected, bool Ok, string Reason);
