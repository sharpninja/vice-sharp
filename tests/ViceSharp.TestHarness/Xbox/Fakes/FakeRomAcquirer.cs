namespace ViceSharp.TestHarness.Xbox.Fakes;

using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S28 (IMPL-XBOXUWP-028), area XROM. Off-console test double for
/// <see cref="IRomAcquirer"/>: it stands in for the head's verified-HTTPS
/// <c>RomProvider.DownloadRom</c> acquisition. On a success run it writes the three
/// deterministic valid core-ROM files (via <see cref="RomProvisionTestData"/>) into the
/// target directory, exactly as the real acquirer would land the SHA256-verified dumps,
/// so a subsequent re-evaluation reports <c>Complete</c>. It records the call count so a
/// test can assert the confirm gate never invoked it.
/// </summary>
internal sealed class FakeRomAcquirer : IRomAcquirer
{
    private readonly bool _succeed;

    /// <summary>Creates the fake acquirer.</summary>
    /// <param name="succeed">Whether <see cref="DownloadCoreSetAsync"/> reports success and writes the set.</param>
    public FakeRomAcquirer(bool succeed = true)
    {
        _succeed = succeed;
    }

    /// <summary>Number of <see cref="DownloadCoreSetAsync"/> calls received.</summary>
    public int CallCount { get; private set; }

    /// <inheritdoc />
    public Task<RomDownloadResult> DownloadCoreSetAsync(string c64Directory, CancellationToken ct = default)
    {
        CallCount++;

        if (!_succeed)
        {
            return Task.FromResult(new RomDownloadResult(false, "Download failed."));
        }

        RomProvisionTestData.WriteValidSet(c64Directory);
        return Task.FromResult(new RomDownloadResult(true, "Downloaded 3 core ROMs."));
    }
}
