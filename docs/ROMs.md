# ROM Handling

> Starting fresh? [USER-GUIDE.md](USER-GUIDE.md) is the higher-level entry point: install, first run, CLI launcher, and machine YAML topologies. This file is the deep reference for ROM sourcing and layout.

## Overview

ViceSharp requires Commodore ROM images to boot emulated machines. ROMs are copyrighted by Commodore/Tulip and are NOT included in the ViceSharp distribution.

## Legal ROM Options

### 1. VICE ROM Distribution

The VICE project distributes ROM images with its binary packages. If you have VICE installed, point ViceSharp at the VICE data root that contains `C64/`, `DRIVES/`, and the other machine resource folders.

Typical VICE data-root locations:
- **Windows:** `C:\Program Files\WinVICE-*` or a Chocolatey/UniGetUI package root such as `...\GTK3VICE-3.8-win64`
- **Linux:** `/usr/share/vice` or `/usr/lib/vice`
- **macOS:** `/Applications/vice-*`

### 2. Cloanto C64 Forever

[C64 Forever](https://www.oxyron.de/) by Cloanto provides legally licensed ROM images. The "Plus" and "Premium" editions include complete ROM sets for all Commodore machines.

### 3. Open-Source Alternatives

- **Open ROMs** — open-source KERNAL and BASIC replacements (limited compatibility)
- **JiffyDOS** — aftermarket replacement KERNAL (requires purchase)

### 4. Physical Extraction

If you own original Commodore hardware, you may legally extract ROM contents using hardware tools (e.g., EPROM reader).

## Environment Variable

Set `VICESHARP_ROM_PATH` to point to your VICE data root. You can also use `VICE_DATA_PATH` or `VICE_HOME`, and on Windows the test/runtime resolver can derive the data root from `x64sc.exe` on `PATH`.

```bash
# Linux/macOS
export VICESHARP_ROM_PATH="/usr/share/vice"

# Windows
set VICESHARP_ROM_PATH=C:\path\to\GTK3VICE-3.8-win64

# PowerShell
$env:VICESHARP_ROM_PATH = "C:\path\to\GTK3VICE-3.8-win64"
```

## ROM Directory Structure

ViceSharp resolves the native VICE data layout by machine/resource folder. If `VICESHARP_ROM_PATH` points directly at `C64/`, the resolver normalizes it to the parent data root.

```
$VICESHARP_ROM_PATH/
    C64/
        basic-901226-01.bin
        chargen-901225-01.bin
        kernal-901227-03.bin
        gtk3_pos.vkm
    DRIVES/
        dos1541-325302-01+901229-05.bin
        dos1541ii-251968-03.bin
    C128/
    VIC20/
    PET/
    PLUS4/
```

## ROM Validation

ViceSharp validates ROM files at load time using SHA256 checksums for known-good images. Unknown checksums produce a warning but do not prevent loading (to support modified/patched ROMs).

### Known C64 ROM Checksums

| File | Size | SHA256 (first 8 hex) | Description |
|------|------|---------------------|-------------|
| `kernal-901227-03.bin` | 8,192 | `83c60d47...` | Standard C64 KERNAL (901227-03) |
| `basic-901226-01.bin` | 8,192 | `89878cea...` | BASIC V2 (901226-01) |
| `chargen-901225-01.bin` | 4,096 | `fd0d53b8...` | Character ROM (901225-01) |
| `dos1541-325302-01+901229-05.bin` | 16,384 | n/a | 1541 DOS (325302-01/901229-05) |

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
- `VICESHARP_ROM_PATH` / `VICE_DATA_PATH` unset and no `x64sc.exe` found on `PATH` → error with setup instructions
- ROM directory exists but files missing → error listing missing files
- ROM loads but checksum unknown → warning (proceeds with caution)
- ROM loads and checksum matches → normal operation

ViceSharp will NOT attempt to download ROMs automatically without explicit user action via the RomFetch tool.
