using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Host.Runtime;
using ViceSharp.RomFetch;

namespace ViceSharp.Host.Startup;

/// <summary>FR-CFG-002: provisions the default desktop C64 ROMs before host startup.</summary>
public static class C64RomBootstrap
{
    /// <summary>
    /// Reuses the runtime's existing ROM roots or downloads the verified default C64 set
    /// to the per-user cache. Desktop composition opts in; headless factories stay offline.
    /// Downloads have a shared 30-second deadline and failures prevent local host startup.
    /// </summary>
    public static async Task EnsureAsync(CancellationToken cancellationToken = default, string? architectureId = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Explicit non-C64 profiles retain their own provisioning path, including
        // offline VIC-20 installations and deliberate minimal protocol test sessions.
        if (!string.IsNullOrWhiteSpace(architectureId) && !C64MachineProfiles.TryResolve(architectureId, out _))
            return;

        if (DefaultEmulatorRuntimeFactory.TryFindC64RomBasePath(out _))
            return;

        var directory = ViceDataPathResolver.DefaultDownloadDirectory;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            // Never write into a configured VICE install (which may be read-only).
            // The resolver includes this cache after explicitly configured roots.
            var provider = new RomProvider(directory);
            await EnsureCoreSetAsync(provider, async (name, token) =>
            {
                await provider.DownloadRom(name, "C64", token).ConfigureAwait(false);
            }, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException
            or InvalidOperationException or OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Could not prepare C64 ROMs in '{directory}'. " +
                "Check your connection and retry, or set VICESHARP_ROM_PATH to an existing VICE data directory. " +
                (ex is OperationCanceledException ? "ROM download timed out after 30 seconds." : ex.Message), ex);
        }
    }

    internal static async Task EnsureCoreSetAsync(
        IRomProvider provider,
        Func<string, CancellationToken, Task> download,
        CancellationToken cancellationToken)
    {
        string[] required = [C64ViceRomNames.Basic, C64ViceRomNames.KernalRev3, C64ViceRomNames.Character];
        foreach (var name in required)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (provider.IsAvailable(name, "C64"))
                continue;

            await download(name, cancellationToken).ConfigureAwait(false);
            if (!provider.IsAvailable(name, "C64"))
                throw new InvalidOperationException($"C64 ROM '{name}' is still missing or invalid after download.");
        }
    }
}
