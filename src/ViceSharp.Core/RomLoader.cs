namespace ViceSharp.Core;

/// <summary>
/// ROM file loader
/// </summary>
public static class RomLoader
{
    /// <summary>
    /// Load ROM file from disk
    /// </summary>
    public static byte[] Load(string path, int expectedSize)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("ROM file not found", path);

        byte[] data = File.ReadAllBytes(path);

        if (data.Length != expectedSize)
            throw new InvalidDataException($"Invalid ROM size: expected {expectedSize}, got {data.Length}");

        return data;
    }

    /// <summary>
    /// Verify ROM checksum
    /// </summary>
    public static bool VerifyChecksum(byte[] data, uint expectedChecksum)
    {
        uint checksum = 0;
        foreach (byte b in data)
            checksum += b;
        return checksum == expectedChecksum;
    }
}