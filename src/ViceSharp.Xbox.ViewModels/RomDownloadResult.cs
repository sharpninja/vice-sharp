namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-002, TR-XPATH-001. The outcome of
/// a core-ROM-set acquisition (verified HTTPS download) reported by an
/// <see cref="IRomAcquirer"/>.
/// </summary>
/// <param name="IsSuccess">Whether the acquisition completed and landed the verified files.</param>
/// <param name="Message">A short human-readable description of the outcome (success or failure reason).</param>
public sealed record RomDownloadResult(bool IsSuccess, string Message);
