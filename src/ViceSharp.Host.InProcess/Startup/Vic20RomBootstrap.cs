using ViceSharp.Architectures.Vic20;
using ViceSharp.RomFetch;

namespace ViceSharp.Host.Startup;

/// <summary>
/// Ensures VIC-20 ROMs exist under a VICE data root so session create / profile
/// restart can build a Vic20 machine (Iteration 2).
/// </summary>
public static class Vic20RomBootstrap
{
    /// <summary>
    /// Files required for the default PAL Vic20 profile (and enough for NTSC kernal too).
    /// </summary>
    public static IReadOnlyList<string> RequiredRomFiles { get; } =
    [
        Vic20ViceRomNames.Basic,
        Vic20ViceRomNames.KernalPal,
        Vic20ViceRomNames.KernalNtsc,
        Vic20ViceRomNames.Character,
    ];

    /// <summary>
    /// Returns true when the default Vic20 ROM set is complete at <paramref name="dataRoot"/>.
    /// </summary>
    public static bool IsComplete(string dataRoot)
    {
        if (string.IsNullOrWhiteSpace(dataRoot) || !Directory.Exists(dataRoot))
            return false;
        return new Vic20RomSet().IsComplete(new RomProvider(dataRoot, []));
    }

    /// <summary>
    /// Create <c>VIC20/</c> under the data root and download any missing ROMs via
    /// <see cref="RomProvider.DownloadRom"/> (SHA256 verified). Idempotent.
    /// </summary>
    public static async Task EnsureAsync(string dataRoot, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        var vicDir = Path.Combine(dataRoot, Vic20ViceRomNames.ArchitectureKey);
        Directory.CreateDirectory(vicDir);

        var provider = new RomProvider(dataRoot, []);
        foreach (var file in RequiredRomFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (provider.IsAvailable(file, Vic20ViceRomNames.ArchitectureKey))
                continue;
            await provider.DownloadRom(file, Vic20ViceRomNames.ArchitectureKey, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Synchronous ensure for composition paths that cannot await (best-effort).
    /// </summary>
    public static void Ensure(string dataRoot)
    {
        try
        {
            EnsureAsync(dataRoot).GetAwaiter().GetResult();
        }
        catch
        {
            // Caller re-checks IsComplete and surfaces a clear failure on machine build.
        }
    }
}
