namespace ViceSharp.Xbox.ViewModels;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. FR-XROM-002, TR-XPATH-001. The seam the
/// first-run wizard drives to acquire the three core C64 ROMs.
/// </summary>
/// <remarks>
/// The head implements this over the RomFetch <c>RomProvider.DownloadRom</c> path: a
/// verified-HTTPS download of <c>basic-901226-01.bin</c>, <c>kernal-901227-03.bin</c> and
/// <c>chargen-901225-01.bin</c> from the VICE GitHub mirror (VICE-Team/svn-mirror,
/// <c>vice/data/C64/</c>), each SHA256-checked against the pins at
/// <c>RomProvider.cs:127-129</c> before the file is written (requires the
/// <c>internetClient</c> capability). Keeping it behind this seam lets the portable
/// ViewModels stay free of any network or RomFetch reference (TR-MVVM-001).
/// </remarks>
public interface IRomAcquirer
{
    /// <summary>
    /// Downloads and verifies the three core C64 ROMs into <paramref name="c64Directory"/>.
    /// </summary>
    /// <param name="c64Directory">The writable C64 ROM directory the verified files land in.</param>
    /// <param name="ct">A token to cancel the download.</param>
    /// <returns>The outcome of the acquisition.</returns>
    Task<RomDownloadResult> DownloadCoreSetAsync(string c64Directory, CancellationToken ct = default);
}
