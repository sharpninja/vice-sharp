namespace ViceSharp.RomM;

/// <summary>FR-CSDB-001 (AC-CSDB-06). Which CSDb gateway a head should use.</summary>
public enum CsdbGatewayMode
{
    /// <summary>Co-located ingest (the RomM roms root is locally writable).</summary>
    Local = 0,

    /// <summary>The csdb-bridge sidecar (roms root not locally writable; e.g. Xbox).</summary>
    Bridge = 1,
}

/// <summary>
/// FR-CSDB-001 (AC-CSDB-06). Selects the CSDb gateway from configuration: use the co-located
/// <see cref="LocalCsdbGateway"/> when the RomM roms root exists and is writable, otherwise the
/// <see cref="BridgeCsdbGateway"/>.
/// </summary>
public static class CsdbGatewaySelection
{
    /// <summary>Picks the gateway mode for the given local RomM roms root.</summary>
    /// <param name="localRomsRoot">The local RomM roms root path, or <c>null</c> when unavailable.</param>
    public static CsdbGatewayMode Select(string? localRomsRoot)
    {
        if (string.IsNullOrWhiteSpace(localRomsRoot) || !Directory.Exists(localRomsRoot))
        {
            return CsdbGatewayMode.Bridge;
        }

        return CanWrite(localRomsRoot) ? CsdbGatewayMode.Local : CsdbGatewayMode.Bridge;
    }

    private static bool CanWrite(string directory)
    {
        try
        {
            string probe = Path.Combine(directory, ".vs-romm-write-probe");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
        }
    }
}
