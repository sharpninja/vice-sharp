# GB64 and HVSC importer

Moved from `F:\GitHub\RomM` into this repo. The Docker library data (host `gb64/`, `runtime/library`) still lives in the RomM stack repo unless you copy it here.

## Tools

- `tools/Gb64Import` - extract GameBase64 ZIPs into Structure A `roms/c64/`
- `tools/HvscFetch` - download and normalize HVSC
- `scripts/Prepare-RomMLibrary.ps1` - platform dirs, screenshot sync, gated game import
- `scripts/Download-Hvsc.ps1` - runs HvscFetch
- `scripts/Resolve-SidPath.ps1` / `Resolve-ScreenshotPath.ps1`

## Library root

Scripts default `LibraryRoot` to `$env:ROMM_ROOT`, else sibling `../RomM` when `gb64/Games` exists, else this repo.

```pwsh
.\scripts\Download-Hvsc.ps1
.\scripts\Prepare-RomMLibrary.ps1
# or:
.\scripts\Prepare-RomMLibrary.ps1 -LibraryRoot 'F:\GitHub\RomM'
dotnet test .\tools\Gb64Import.Tests\Gb64Import.Tests.csproj -c Release
```
