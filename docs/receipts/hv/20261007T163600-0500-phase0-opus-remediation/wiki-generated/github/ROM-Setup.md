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

[C64 Forever](https://www.c64forever.com/) by Cloanto provides legally licensed ROM images. The "Plus" and "Premium" editions include complete ROM sets for all Commodore machines.

### 3. Open-Source Alternatives

- **Open ROMs** - open-source KERNAL and BASIC replacements (limited compatibility)
- **JiffyDOS** - aftermarket replacement KERNAL (requires purchase)

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

ViceSharp validates ROM files at load time using MD5 checksums for known-good images (`C64RomLoader.LoadRom` in [src/ViceSharp.RomFetch/C64RomLoader.cs](../src/ViceSharp.RomFetch/C64RomLoader.cs)). Validation is strict: if a file matches a known ROM name but its MD5 does not match the descriptor, the load fails (no warning, no fallback). ROM file names the loader does not recognize skip checksum validation entirely and load as-is (this is what supports modified/patched ROMs). Each descriptor also stores a SHA1 hash for reference, but only MD5 is checked at load time. SHA256 is used elsewhere: it pins RomFetch downloads (`RomProvider`) and the CI ROM staging in `build/Build.cs` (`EnsureCiRomRoot`).

### Known C64 ROM Checksums

Copied from the descriptors in `C64RomLoader.cs`:

| File | Size | MD5 (validated at load) | Description |
|------|------|-------------------------|-------------|
| `kernal-901227-03.bin` | 8,192 | `39065497630802346bce17963f13c092` | Standard C64 KERNAL (901227-03) |
| `kernal-901227-02.bin` | 8,192 | `7360b296d64e18b88f6cf52289fd99a1` | KERNAL rev 2 (901227-02) |
| `kernal-901227-01.bin` | 8,192 | `1ae0ea224f2b291dafa2c20b990bb7d4` | KERNAL rev 1 (901227-01) |
| `kernal-251104-04.bin` | 8,192 | `187b8c713b51931e070872bd390b472a` | SX-64 KERNAL (251104-04) |
| `kernal-901246-01.bin` | 8,192 | `da92801e3a03b005b746a4dd0b639c7c` | PET64 KERNAL (901246-01) |
| `kernal-906145-02.bin` | 8,192 | `479553fd53346ec84054f0b1c6237397` | Japanese C64 KERNAL (906145-02) |
| `kernal-390852-01.bin` | 8,192 | `ddee89b0fed19572da5245ea68ff11b5` | C64GS KERNAL (390852-01) |
| `basic-901226-01.bin` | 8,192 | `57af4ae21d4b705c2991d98ed5c1f7b8` | BASIC V2 (901226-01) |
| `chargen-901225-01.bin` | 4,096 | `12a4202f5331d45af846af6c58fba946` | Character ROM (901225-01) |
| `chargen-906143-02.bin` | 4,096 | `cf32a93c0a693ed359a4f483ef6db53d` | Japanese character ROM (906143-02) |
| `dos1541-325302-01+901229-05.bin` | 16,384 | n/a (no descriptor; loads unvalidated) | 1541 DOS (325302-01/901229-05) |

## ViceSharp.RomFetch Tool

`ViceSharp.RomFetch` is currently a class library, not a command-line tool: the project has no executable entry point, so there is no `dotnet run --project src/ViceSharp.RomFetch` invocation today (the Nuke `RomFetch` target logs "tool not yet implemented"). A standalone CLI remains planned work.

What the library provides today:

- `C64RomLoader` - loads C64 ROM images into the bus with strict MD5 validation (see [ROM Validation](#rom-validation) above).
- `RomProvider` - resolves ROM files from one or more base paths and can download known ROMs from a user-supplied URL database, verifying each download against a pinned SHA256 hash before writing it to disk.
- `ViceDataPathResolver` - locates the VICE data root from `VICESHARP_ROM_PATH`, `VICE_DATA_PATH`, `VICE_HOME`, or (on Windows) `x64sc.exe` on `PATH`, and normalizes a `C64/` subdirectory to its parent root.

CI uses the same hash-pinning approach: the `CiTest` Nuke target stages the required ROM dumps via `EnsureCiRomRoot` in `build/Build.cs`, downloading them SHA256-pinned when they are not already present on the agent.

## Fallback Behavior

When ROMs are missing or invalid:
- `VICESHARP_ROM_PATH` / `VICE_DATA_PATH` unset and no `x64sc.exe` found on `PATH` → error with setup instructions
- ROM directory exists but files missing → error listing missing files
- Known ROM name but MD5 checksum mismatch → load fails
- Unrecognized ROM file name → checksum validation skipped, loads as-is
- ROM loads and checksum matches → normal operation

ViceSharp will NOT attempt to download ROMs automatically without explicit user action via the RomFetch APIs.
