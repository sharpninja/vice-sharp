# ROM Handling

## Overview

ViceSharp requires Commodore ROM images to boot emulated machines. ROMs are copyrighted by Commodore/Tulip and are NOT included in the ViceSharp distribution.

## Legal ROM Options

### 1. VICE ROM Distribution

The VICE project distributes ROM images with its binary packages. If you have VICE installed, you can point ViceSharp to VICE's ROM directory.

Typical VICE ROM locations:
- **Windows:** `C:\Program Files\WinVICE-*\C64\`
- **Linux:** `/usr/share/vice/C64/` or `/usr/lib/vice/C64/`
- **macOS:** `/Applications/vice-*/C64/`

### 2. Cloanto C64 Forever

[C64 Forever](https://www.oxyron.de/) by Cloanto provides legally licensed ROM images. The "Plus" and "Premium" editions include complete ROM sets for all Commodore machines.

### 3. Open-Source Alternatives

- **Open ROMs** — open-source KERNAL and BASIC replacements (limited compatibility)
- **JiffyDOS** — aftermarket replacement KERNAL (requires purchase)

### 4. Physical Extraction

If you own original Commodore hardware, you may legally extract ROM contents using hardware tools (e.g., EPROM reader).

## Environment Variable

Set `VICESHARP_ROM_PATH` to point to your ROM directory:

```bash
# Linux/macOS
export VICESHARP_ROM_PATH="$HOME/.vicesharp/roms"

# Windows
set VICESHARP_ROM_PATH=%USERPROFILE%\.vicesharp\roms

# PowerShell
$env:VICESHARP_ROM_PATH = "$env:USERPROFILE\.vicesharp\roms"
```

## ROM Directory Structure

ViceSharp expects ROMs organized by machine:

```
$VICESHARP_ROM_PATH/
    C64/
        kernal          # C64 KERNAL ROM (8 KB)
        basic           # BASIC V2 ROM (8 KB)
        chargen         # Character generator ROM (4 KB)
        1541            # 1541 drive DOS ROM (16 KB)
    C128/
        kernal          # C128 KERNAL ROM
        basiclo         # BASIC 7.0 low ROM
        basichi         # BASIC 7.0 high ROM
        chargen         # C128 character ROM
        kernal64        # C64 mode KERNAL
        basic64         # C64 mode BASIC
        z80bios         # Z80 BIOS ROM
        1571            # 1571 drive ROM
    VIC20/
        kernal          # VIC-20 KERNAL ROM (8 KB)
        basic           # BASIC V2 ROM (8 KB)
        chargen         # Character ROM (4 KB)
    PET/
        kernal4         # PET 4.0 KERNAL
        basic4          # BASIC 4.0
        edit4b80        # Editor ROM (80 column)
        chargen         # PET character ROM
    PLUS4/
        kernal          # Plus/4 KERNAL ROM
        basic           # BASIC 3.5 ROM
        3plus1lo        # Built-in software (low)
        3plus1hi        # Built-in software (high)
    DRIVES/
        1541            # 1541 DOS (if shared across machines)
        1571            # 1571 DOS
        1581            # 1581 DOS
```

## ROM Validation

ViceSharp validates ROM files at load time using SHA256 checksums for known-good images. Unknown checksums produce a warning but do not prevent loading (to support modified/patched ROMs).

### Known C64 ROM Checksums

| File | Size | SHA256 (first 8 hex) | Description |
|------|------|---------------------|-------------|
| `kernal` | 8,192 | `39065497...` | Standard C64 KERNAL (901227-03) |
| `basic` | 8,192 | `79015323...` | BASIC V2 (901226-01) |
| `chargen` | 4,096 | `adc7c31b...` | Character ROM (901225-01) |
| `1541` | 16,384 | `d3b78c2e...` | 1541 DOS (325302-01/901229-05) |

## ViceSharp.RomFetch Tool

The `ViceSharp.RomFetch` tool automates ROM acquisition from legal sources:

```bash
# Fetch ROMs from an existing VICE installation
dotnet run --project src/ViceSharp.RomFetch -- --source vice --vice-path "C:\WinVICE"

# Fetch from a URL (user provides their own legal source)
dotnet run --project src/ViceSharp.RomFetch -- --source url --url "https://example.com/roms.zip"

# Validate existing ROM set
dotnet run --project src/ViceSharp.RomFetch -- --validate --rom-path "$VICESHARP_ROM_PATH"
```

The tool:
1. Locates or downloads ROM files
2. Organizes them into the expected directory structure
3. Validates checksums
4. Reports any missing or corrupted files

## Fallback Behavior

When ROMs are missing:
- `VICESHARP_ROM_PATH` not set → error with setup instructions
- ROM directory exists but files missing → error listing missing files
- ROM loads but checksum unknown → warning (proceeds with caution)
- ROM loads and checksum matches → normal operation

ViceSharp will NOT attempt to download ROMs automatically without explicit user action via the RomFetch tool.
