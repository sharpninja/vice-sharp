# Flash Cart Image Builder

Portable builder for VIC-20 expansion flash images (and a C64 EasyFlash layout profile for reuse). UI-free Core types; Avalonia and Xbox host the same view model.

## Profiles

| Profile id | Display | Primary size | Bank size | Secondary |
|------------|---------|--------------|-----------|-----------|
| `fe3` | Final Expansion 3 | 512 KiB (`FinalExpansion3Cartridge.FlashSize`) | 8 KiB | none |
| `ultimem` | Ultimem (1MB image) | 1 MiB (`UltimemCartridge.DefaultImageSize`) | 8 KiB | none |
| `megacart` | Mega-Cart | 1 MiB low ROM (`MegaCartCartridge.LowRomSize`) | 8 KiB | 8 KiB NVRAM (`NvramSize`) |
| `easyflash` | EasyFlash (C64 layout) | 1 MiB (128 x 8K banks) | 8 KiB | none |

Primary UI lists FE3 / Ultimem / Mega-Cart. EasyFlash remains on `FlashCartProfiles.All` for structure reuse; C64 EasyFlash attach UI is out of band.

## Core types

- `src/ViceSharp.Core/FlashCarts/IFlashCartProfile.cs` - profile contract
- `FlashCartProfiles.cs` - shipped profile instances
- `FlashImageBuilder.cs` - pack primary (+ optional secondary) streams into a raw image
- `FlashCartImageBuilderViewModel.cs` - portable VM (bank slots, load file, build, status text)
- `IFlashBuilderFileIo.cs` - host file I/O seam for tests and UI

## UI entry points

### Avalonia desktop

Settings (VIC-20 machine selected) → **VIC-20 expansion cart** → **Build cart image.** opens `FlashCartBuilderView` dialog bound to `FlashCartImageBuilderViewModel`.

BLK0/1/2/3/5 RAM toggles use a horizontal `WrapPanel` so they wrap on narrow Settings panes.

### Xbox UWP

Settings (VIC-20 selected) → expansion cart section:

- Cart kind / preset combo boxes (FE3 presets include flash / super-rom / rom-ram / ram2 style options)
- Attach image / Eject image
- **Build cart image** navigates to `FlashCartBuilderPage` (`NavigationDestination.FlashCartBuilder`)

BLK toggles use `VariableSizedWrapGrid` (UWP has no `WrapPanel`).

## Attach path

Built or external images attach through the media/cartridge slot with size-based detection for FE3 / Ultimem / Mega-Cart. Expansion cart state is exposed via protocol (`ExpansionCartManageState`) and settings host APIs on Xbox (`XboxSettingsViewModel`) and Avalonia settings surfaces.

## Accuracy notes

- Image sizes and bank geometry for the three VIC-20 profiles are Exact against the matching cartridge type constants (hostile validator receipt `docs/receipts/hostile-validator-20260808T103211Z.md`).
- FE3 runtime: MODE_FLASH stores go through managed `Flash040Core` (AM29F040B unlock, byte program AND, chip/sector erase, autoselect IDs). Erase **latency** is instant (Partial vs VICE multi-second `erase_alarm`).
- Mega-Cart NVRAM secondary blob size is Exact; full Mega-Cart mapper runtime depth remains Partial / Stub per `docs/audit-vic20-vs-vice-2026-08-07.md`.

## Tests

```pwsh
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~FlashCart"
```

Coverage: profile sizes, builder packing, portable VM bank load/build, FE3 preset enum surface.
