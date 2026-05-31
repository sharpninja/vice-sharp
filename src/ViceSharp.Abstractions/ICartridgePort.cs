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
    MagicDesk = 5,

    /// <summary>
    /// Ocean (CRT type 5). 32K to 512K image split into 8K ROML banks.
    /// Writes to $DE00 take bits 0-5 (mod bank-count) as the bank index.
    /// No disable bit; ROML is always mapped while attached.
    /// </summary>
    Ocean = 6,

    /// <summary>
    /// Final Cartridge III (CRT type 60). 64K image split into 4 banks of
    /// 16K each (ROML + ROMH). The bank register lives at $DFFF: bits 0-1
    /// select bank, bit 7 = hide (releases ROML and ROMH). Standard 16K
    /// mapping is assumed (no Ultimax flip).
    /// </summary>
    FinalCartridgeIII = 7,

    /// <summary>
    /// Action Replay V4/V5 (CRT type 1). 32K image = 4 banks of 8K. Bank
    /// register at $DE00: bits 0-3 = bank (mod 4), bit 7 = hide (release
    /// ROML). Bits 4-6 are reserved (LED + control). This slice implements
    /// the minimum-viable mapper (bank + hide); freeze ROM and EXROM/GAME
    /// pin manipulation are deferred to a follow-up Action Replay slice.
    /// </summary>
    ActionReplay = 8,

    /// <summary>
    /// EasyFlash (CRT type 32). Up to 1024K of flash split into 8K ROML
    /// banks. Bank register at $DE00 (bits 0-5 = bank, mod bank-count);
    /// control register at $DE02 (bits 0-2 = mode). This slice implements
    /// only bank switching; flash write emulation is deferred.
    /// </summary>
    EasyFlash = 9,

    /// <summary>
    /// Super Snapshot V5 (CRT type 4). 64K image = 4 banks of 16K (ROML +
    /// ROMH). Bank register at $DE00 (bits 2-4 = bank, mod 4); bit 1 = hide.
    /// This slice implements bank + hide; freeze button is deferred.
    /// </summary>
    SuperSnapshotV5 = 10,

    /// <summary>
    /// RR-Net (CRT type 25). 64K image = 4 banks of 16K (ROML + ROMH) plus
    /// an Ethernet I/O register window at $DE00-$DE0F. Bank register at
    /// $DE00 mirrors Action Replay (bits 0-3 = bank, bit 7 = hide). This
    /// slice implements bank + hide; the Ethernet register window is
    /// stubbed (reads as $FF, writes are ignored).
    /// </summary>
    RRNet = 11
}
