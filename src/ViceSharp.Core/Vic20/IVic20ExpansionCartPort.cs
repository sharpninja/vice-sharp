using ViceSharp.Abstractions;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// Attach/detach surface for VIC-20 expansion carts (FE3, Ultimem, Mega-Cart).
/// </summary>
/// <remarks>FR-VIC20-005. Aligns with VICE cart attach for xvic.</remarks>
public interface IVic20ExpansionCartPort : IDevice
{
    Vic20ExpansionCartKind AttachedKind { get; }

    bool WriteBack { get; set; }

    string ConfigPresetId { get; set; }

    /// <summary>Attach Final Expansion 3 flash image (512K typical).</summary>
    void AttachFinalExpansion3(ReadOnlySpan<byte> flashImage);

    /// <summary>Attach Ultimem image.</summary>
    void AttachUltimem(ReadOnlySpan<byte> image);

    /// <summary>Attach Mega-Cart ROM image (and optional NVRAM seed).</summary>
    void AttachMegaCart(ReadOnlySpan<byte> romImage, ReadOnlySpan<byte> nvram = default);

    void Eject();

    /// <summary>Apply a named configuration preset (cart-specific).</summary>
    void ApplyConfigPreset(string presetId);

    /// <summary>Flush write-back image when dirty; null if none.</summary>
    byte[]? FlushImage();

    /// <summary>Flush Mega-Cart NVRAM when dirty; null if none.</summary>
    byte[]? FlushNvram();

    /// <summary>IO3/IO2 peek for tests (address in $9C00-$9FFF or cart windows).</summary>
    byte PeekIo(ushort address);

    void PokeIo(ushort address, byte value);

    /// <summary>Read expansion window after cart mapping (BLK/RAM123).</summary>
    bool TryReadMapped(ushort address, out byte value);

    bool TryWriteMapped(ushort address, byte value);
}
