namespace ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-002, TR-XPATH-001. The
/// specification of one core C64 ROM: the file name it lands under, its exact expected size,
/// and both pinned digests.
/// </summary>
/// <remarks>
/// Two digests are carried because the two head acquisition paths validate differently, and
/// this ViewModels layer must not reference <c>ViceSharp.RomFetch</c> (TR-MVVM-001):
/// <list type="bullet">
///   <item><description><see cref="ExpectedSha256"/> is the download-verification digest,
///   parity with <c>RomProvider.cs:127-129</c> (the head's verified-HTTPS <c>DownloadRom</c>
///   SHA256 pins). The evaluator classifies presence off this digest.</description></item>
///   <item><description><see cref="ExpectedMd5"/> is the storage-import digest, parity with
///   <c>C64RomLoader.cs:13-80</c> (the size + MD5 the loader validates). The wizard's import
///   path validates a picked file off this digest.</description></item>
/// </list>
/// Both are stored as upper- or lower-case hex; all comparisons are case-insensitive.
/// </remarks>
/// <param name="Role">The logical role this file satisfies.</param>
/// <param name="FileName">The canonical VICE file name (e.g. <c>basic-901226-01.bin</c>).</param>
/// <param name="ExpectedSize">The exact expected file size in bytes.</param>
/// <param name="ExpectedSha256">The hex-encoded SHA256 digest (download-verification parity).</param>
/// <param name="ExpectedMd5">The hex-encoded MD5 digest (storage-import parity).</param>
public sealed record RomSpec(
    RomRole Role,
    string FileName,
    int ExpectedSize,
    string ExpectedSha256,
    string ExpectedMd5);
