namespace ViceSharp.Abstractions;

/// <summary>
/// FR-MON-005. ROM image bytes that stay visible when the CPU PLA maps RAM
/// over the same addresses.
/// </summary>
public interface IFixedRomImage
{
    /// <summary>
    /// Reads the ROM image at <paramref name="address"/>.
    /// </summary>
    /// <param name="address">CPU address.</param>
    /// <param name="value">ROM byte when this image covers the address.</param>
    /// <returns><c>true</c> when a ROM image covers <paramref name="address"/>.</returns>
    bool TryPeekRom(ushort address, out byte value);
}
