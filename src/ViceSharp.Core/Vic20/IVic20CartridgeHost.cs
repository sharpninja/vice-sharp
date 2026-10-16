namespace ViceSharp.Core.Vic20;

/// <summary>
/// Optional architecture descriptor contract for attaching a VIC-20 MVP cart
/// at machine build time.
/// </summary>
public interface IVic20CartridgeHost
{
    /// <summary>Cartridge image to map, or null when empty.</summary>
    Vic20Cartridge? Cartridge { get; }
}
