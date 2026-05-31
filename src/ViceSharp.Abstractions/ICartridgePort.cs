namespace ViceSharp.Abstractions;

/// <summary>
/// Cartridge expansion port exposed by cartridge-capable machines.
/// </summary>
public interface ICartridgePort : IDevice
{
    /// <summary>Default mapping mode selected by the machine profile.</summary>
    CartridgeMappingMode DefaultMappingMode { get; }

    /// <summary>Currently attached cartridge mapping mode, or null when empty.</summary>
    CartridgeMappingMode? AttachedMappingMode { get; }

    /// <summary>True when a cartridge image is currently attached.</summary>
    bool IsCartridgeAttached { get; }

    /// <summary>Attach a raw cartridge payload using the requested mapping mode.</summary>
    void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode);

    /// <summary>Eject the currently attached cartridge, if any.</summary>
    void EjectCartridge();

    /// <summary>
    /// Bind a cart-port InterSystemBus endpoint as the live GAME/EXROM pin
    /// source. When set + a cartridge is attached, the active mapping mode
    /// is derived from the pin state instead of the static
    /// <see cref="AttachedMappingMode"/>. Pass null to revert to static.
    /// </summary>
    void SetCartPortPinSource(IBusEndpoint? endpoint);
}

/// <summary>
/// Raw cartridge mapping modes understood by the C64 cartridge port.
/// </summary>
public enum CartridgeMappingMode
{
    /// <summary>Resolve from the machine profile and payload size.</summary>
    Auto = 0,

    /// <summary>Generic 8K cartridge mapped at ROML, $8000-$9FFF.</summary>
    Standard8K = 1,

    /// <summary>Generic 16K cartridge mapped at ROML/ROMH, $8000-$BFFF.</summary>
    Standard16K = 2,

    /// <summary>Ultimax/MAX cartridge mapped at ROML and ROMH, $8000-$9FFF and $E000-$FFFF.</summary>
    Ultimax = 3,

    /// <summary>C64 Games System cartridge mapped as 64 switchable 8K ROML banks.</summary>
    GameSystem = 4,

    /// <summary>
    /// Magic Desk / Domark / HES Australia (CRT type 19). 32K/64K/128K image
    /// split into 8K ROML banks. Writes to $DE00 select bank: data bits 0-6
    /// = bank index, bit 7 = cart-disable (releases ROML when high).
    /// </summary>
    MagicDesk = 5
}
