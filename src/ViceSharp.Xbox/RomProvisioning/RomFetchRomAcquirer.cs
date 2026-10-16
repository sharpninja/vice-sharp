namespace ViceSharp.Xbox.RomProvisioning;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.RomFetch;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// PLAN-XBOXUWP S40 (IMPL-XBOXUWP-040), area XROM. FR-XROM-002, TR-XPATH-001. The head's
/// concrete <see cref="IRomAcquirer"/>: a verified-HTTPS acquisition of the three core C64
/// ROMs over the RomFetch <see cref="RomProvider.DownloadRom"/> path.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RomProvider.DownloadRom"/> is keyed by the bare RomDatabase keys
/// (<c>basic</c>/<c>kernal</c>/<c>characters</c>) and writes the verified file under that
/// bare name. Neither <see cref="RomProvisionEvaluator"/> nor the C64 machine's
/// <c>C64RomSet</c> can see a bare-named file - both look up the canonical VICE names
/// (<c>basic-901226-01.bin</c> etc.) and RomFetch's alias map is one-directional. So this
/// acquirer uses <c>DownloadRom</c> ONLY for its SHA256-verified RETURNED bytes and writes
/// them itself under the canonical <see cref="RomSpec.FileName"/> from
/// <see cref="RomCatalog.C64"/> into <paramref name="c64Directory">the C64 directory</paramref>
/// - the exact name both the evaluator and the machine read.
/// </para>
/// <para>
/// The byte fetch is injected so the write + name mapping is testable without a network
/// round-trip (the default fetch runs <c>DownloadRom</c> against a throwaway staging dir, so
/// its own bare-named write never lands in <c>c64Directory</c>). This type uses no UWP API
/// (only System.IO + RomFetch + the portable contracts), so it compiles and unit-tests on the
/// workload-free net10.0 fallback; TR-MVVM-001 stays intact because the portable ViewModels
/// never reference RomFetch - only the head does.
/// </para>
/// </remarks>
public sealed class RomFetchRomAcquirer : IRomAcquirer
{
    /// <summary>The RomProvider architecture segment for the C64 core ROMs.</summary>
    public const string Architecture = "C64";

    /// <summary>
    /// One core ROM: its role, its bare RomProvider download key, and the canonical file name it
    /// must land under.
    /// </summary>
    /// <param name="Role">The provisioning role.</param>
    /// <param name="DownloadKey">The bare RomProvider/RomDatabase key.</param>
    /// <param name="CanonicalFileName">The canonical VICE file name the evaluator + machine read.</param>
    public sealed record CoreRom(RomRole Role, string DownloadKey, string CanonicalFileName);

    /// <summary>
    /// The three core ROMs. Canonical names are pinned to <see cref="RomCatalog.C64"/> so the
    /// files land EXACTLY where <see cref="RomProvisionEvaluator"/> and the C64 machine's
    /// <c>C64RomSet</c> read.
    /// </summary>
    public static IReadOnlyList<CoreRom> CoreRoms { get; } = new[]
    {
        new CoreRom(RomRole.Basic, "basic", RomCatalog.C64.GetSpec(RomRole.Basic).FileName),
        new CoreRom(RomRole.Kernal, "kernal", RomCatalog.C64.GetSpec(RomRole.Kernal).FileName),
        new CoreRom(RomRole.Chargen, "characters", RomCatalog.C64.GetSpec(RomRole.Chargen).FileName),
    };

    private readonly Func<string, string, CancellationToken, Task<ReadOnlyMemory<byte>>> _fetch;

    /// <summary>Creates the acquirer with the production RomFetch verified-byte fetch.</summary>
    public RomFetchRomAcquirer()
        : this(DefaultFetchAsync)
    {
    }

    /// <summary>Creates the acquirer over an injected verified-byte fetch (headless tests).</summary>
    /// <param name="fetch">(downloadKey, architecture, ct) -&gt; SHA256-verified bytes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fetch"/> is <c>null</c>.</exception>
    public RomFetchRomAcquirer(Func<string, string, CancellationToken, Task<ReadOnlyMemory<byte>>> fetch)
    {
        _fetch = fetch ?? throw new ArgumentNullException(nameof(fetch));
    }

    /// <inheritdoc />
    public async Task<RomDownloadResult> DownloadCoreSetAsync(string c64Directory, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(c64Directory);
        Directory.CreateDirectory(c64Directory);

        foreach (var rom in CoreRoms)
        {
            // The default fetch (or a failing injected one) throws on a network/hash failure;
            // XboxRomProvisioningViewModel.DownloadAsync catches it and degrades to the offline
            // path (StatusMessage + stays boot-blocked).
            var bytes = await _fetch(rom.DownloadKey, Architecture, ct).ConfigureAwait(false);
            var destination = Path.Combine(c64Directory, rom.CanonicalFileName);
            await File.WriteAllBytesAsync(destination, bytes.ToArray(), ct).ConfigureAwait(false);
        }

        return new RomDownloadResult(true, $"Downloaded {CoreRoms.Count} core ROMs.");
    }

    private static async Task<ReadOnlyMemory<byte>> DefaultFetchAsync(string key, string architecture, CancellationToken ct)
    {
        // Staging keeps DownloadRom's own bare-named write out of c64Directory; we only want its
        // verified bytes. The staging copy is deleted afterwards (best-effort).
        var staging = Path.Combine(Path.GetTempPath(), "vicesharp-romfetch-" + Guid.NewGuid().ToString("N"));
        try
        {
            var provider = new RomProvider(staging);
            return await provider.DownloadRom(key, architecture, ct).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, recursive: true);
            }
            catch
            {
                // Best-effort cleanup: a leaked temp dir is harmless; the verified bytes are
                // already re-written under the canonical name in c64Directory.
            }
        }
    }
}
