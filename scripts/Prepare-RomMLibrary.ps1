<#
.SYNOPSIS
  Prepare attached RomM library storage: platforms, HVSC root, screenshots root,
  and optional GB64 game import.

.DESCRIPTION
  Attached library root: runtime/library → /romm/library

    runtime/library/roms/{c64,c128,c-plus-4,vic-20,...}   Structure A games
    runtime/library/hvsc/                                 NFO SID: resolution root
    runtime/library/screenshots/                          NFO Screenshot: resolution root
      screenshots/A/Alfabug.png  <=  Screenshot: A\Alfabug.png
    runtime/library/bios/                                 optional firmware

  Source ./gb64 remains the import source only (Games ZIPs, original Screenshots).
  Screenshots are staged into library/screenshots so runtime path resolution does
  not depend on mounting the whole gb64 tree.

  - Never overwrites non-empty runtime/config/config.yml
  - GB64 game package import only when roms/c64 library media is missing
  - Screenshots sync when library/screenshots is empty and gb64/Screenshots exists
  - HVSC is not downloaded here (use Download-Hvsc.ps1 into library/hvsc)

.PARAMETER RepoRoot
  Deploy / repo root.

.PARAMETER ForceLibraryBuild
  Force game import and screenshot re-sync.
#>
[CmdletBinding()]
param(
    [string]$ToolRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$LibraryRoot = '',
    [switch]$ForceLibraryBuild
)

$ErrorActionPreference = 'Stop'

function Test-HasGameMedia([string]$Dir) {
    if (-not (Test-Path -LiteralPath $Dir -PathType Container)) { return $false }
    $ext = @('*.d64', '*.g64', '*.t64', '*.tap', '*.crt', '*.prg', '*.p00', '*.sid', '*.zip', '*.7z', '*.rar')
    foreach ($e in $ext) {
        $hit = Get-ChildItem -LiteralPath $Dir -Recurse -File -Filter $e -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($hit) { return $true }
    }
    $any = Get-ChildItem -LiteralPath $Dir -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Length -gt 0 -and $_.Name -notmatch '^\.' -and $_.Name -notmatch '^\.gb64' } |
        Select-Object -First 1
    return [bool]$any
}

function Test-HasScreenshots([string]$Dir) {
    if (-not (Test-Path -LiteralPath $Dir -PathType Container)) { return $false }
    $hit = Get-ChildItem -LiteralPath $Dir -Recurse -File -Include *.png, *.jpg, *.gif, *.bmp -ErrorAction SilentlyContinue |
        Select-Object -First 1
    return [bool]$hit
}

function Test-HasHvsc([string]$Dir) {
    if (-not (Test-Path -LiteralPath $Dir -PathType Container)) { return $false }
    return (Test-Path -LiteralPath (Join-Path $Dir 'MUSICIANS')) -or
        [bool](Get-ChildItem -LiteralPath $Dir -Recurse -File -Filter '*.sid' -ErrorAction SilentlyContinue | Select-Object -First 1)
}

if (-not $LibraryRoot) {
    if ($env:ROMM_ROOT) {
        $LibraryRoot = $env:ROMM_ROOT
    }
    else {
        $sibling = Join-Path (Split-Path $ToolRoot -Parent) 'RomM'
        if (Test-Path -LiteralPath (Join-Path $sibling 'gb64\Games') -PathType Container) {
            $LibraryRoot = $sibling
        }
        else {
            $LibraryRoot = $ToolRoot
        }
    }
}

$RepoRoot = $LibraryRoot
$libraryRoot = Join-Path $RepoRoot 'runtime\library'
$romsRoot = Join-Path $libraryRoot 'roms'
$hvscRoot = Join-Path $libraryRoot 'hvsc'
$screenshotsRoot = Join-Path $libraryRoot 'screenshots'
$biosRoot = Join-Path $libraryRoot 'bios'
$configDir = Join-Path $RepoRoot 'runtime\config'
$configFile = Join-Path $configDir 'config.yml'
$gb64Games = Join-Path $RepoRoot 'gb64\Games'
$gb64Screenshots = Join-Path $RepoRoot 'gb64\Screenshots'
$c64Root = Join-Path $romsRoot 'c64'
$gameMarker = Join-Path $romsRoot '.gb64-library-built'
$shotMarker = Join-Path $screenshotsRoot '.screenshots-synced'

Write-Host "Prepare-RomMLibrary tools=$ToolRoot library=$RepoRoot"
Write-Host "Library (attached) root=$libraryRoot"

# --- Library skeleton (always) ---
$requiredPlatforms = @('c64', 'c128', 'c-plus-4', 'vic-20')
$optionalPlatforms = @('c16', 'cpet', 'commodore-cdtv')
foreach ($p in ($requiredPlatforms + $optionalPlatforms)) {
    $d = Join-Path $romsRoot $p
    if (-not (Test-Path -LiteralPath $d)) {
        New-Item -ItemType Directory -Path $d -Force | Out-Null
        Write-Host "Created platform dir: roms/$p"
    }
}
foreach ($d in @($hvscRoot, $screenshotsRoot, $biosRoot)) {
    if (-not (Test-Path -LiteralPath $d)) {
        New-Item -ItemType Directory -Path $d -Force | Out-Null
        Write-Host "Created library dir: $d"
    }
}

# --- Config: never clobber ---
if (-not (Test-Path -LiteralPath $configDir)) {
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
}
if (-not (Test-Path -LiteralPath $configFile)) {
    New-Item -ItemType File -Path $configFile -Force | Out-Null
    Write-Host "Created empty config.yml (first run)"
}
else {
    Write-Host "Preserving existing config.yml ($((Get-Item -LiteralPath $configFile).Length) bytes)"
}

# --- Screenshots: stage into library so Screenshot: metadata paths resolve ---
# NFO: Screenshot: A\Alfabug.png  →  library/screenshots/A/Alfabug.png
$hasShotSource = Test-Path -LiteralPath $gb64Screenshots -PathType Container
$hasShotLibrary = (Test-HasScreenshots $screenshotsRoot) -or (Test-Path -LiteralPath $shotMarker)

if (-not $hasShotSource) {
    Write-Host "SKIP screenshots sync: source missing at $gb64Screenshots"
}
elseif ($hasShotLibrary -and -not $ForceLibraryBuild) {
    Write-Host "SKIP screenshots sync: library/screenshots already has data (or marker)"
}
else {
    Write-Host "RUN screenshots sync: $gb64Screenshots -> $screenshotsRoot"
    # Prefer hardlink/junction-friendly robocopy; /E copy subtree
    $rc = Start-Process -FilePath 'robocopy.exe' -ArgumentList @(
        "`"$gb64Screenshots`"", "`"$screenshotsRoot`"", '/E', '/NFL', '/NDL', '/NJH', '/NJS', '/nc', '/ns', '/np'
    ) -Wait -PassThru -NoNewWindow
    # robocopy exit 0-7 = success-ish
    if ($rc.ExitCode -ge 8) {
        throw "robocopy screenshots failed exit $($rc.ExitCode)"
    }
    @(
        "syncedUtc=$((Get-Date).ToUniversalTime().ToString('o'))"
        "source=$gb64Screenshots"
        "note=NFO Screenshot: Letter\file.png resolves under this root"
    ) | Set-Content -LiteralPath $shotMarker -Encoding utf8
    Write-Host "Screenshots ready for metadata path resolution (SCREENSHOTS_ROOT)"
}

# --- HVSC: must already live under library/hvsc (attached); do not download here ---
if (Test-HasHvsc $hvscRoot) {
    Write-Host "HVSC present at $hvscRoot (NFO SID: paths resolve here)"
}
else {
    Write-Host "WARN: HVSC missing under $hvscRoot"
    Write-Host "  Run: $ToolRoot\scripts\Download-Hvsc.ps1   # writes library/hvsc"
    Write-Host "  SID: MUSICIANS\...\file.sid  =>  /romm/library/hvsc/MUSICIANS/.../file.sid"
}

# --- GB64 games import (only if library media missing) ---
$hasGameSource = Test-Path -LiteralPath $gb64Games -PathType Container
$hasGameLibrary = (Test-HasGameMedia $c64Root) -or (Test-Path -LiteralPath $gameMarker)
$importProj = Join-Path $ToolRoot 'tools\Gb64Import\Gb64Import.csproj'

if (-not $hasGameSource) {
    Write-Host "SKIP game import: GB64 Games missing at $gb64Games"
}
elseif ($hasGameLibrary -and -not $ForceLibraryBuild) {
    Write-Host "SKIP game import: roms/c64 already has library media (or marker)"
}
elseif (-not (Test-Path -LiteralPath $importProj)) {
    Write-Host "ERROR: GB64 importer project missing at $importProj"
    throw "Gb64Import project not found"
}
else {
    Write-Host "RUN game import: source=$gb64Games target=$c64Root"
    $gameZipCount = @(Get-ChildItem -LiteralPath $gb64Games -Recurse -Filter '*.zip' -File -ErrorAction SilentlyContinue).Count
    Write-Host "GB64 game zip count (approx): $gameZipCount"

    $importArgs = @(
        'run', '--project', $importProj, '-c', 'Release', '--',
        '--games', $gb64Games,
        '--out', $c64Root,
        '--hvsc', $hvscRoot,
        '--screenshots', $screenshotsRoot
    )
    if ($ForceLibraryBuild) {
        $importArgs += '--force'
    }

    & dotnet @importArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Gb64Import failed with exit $LASTEXITCODE"
    }
    if (-not (Test-Path -LiteralPath $gameMarker)) {
        throw "Gb64Import finished but marker missing: $gameMarker"
    }
    Write-Host "Game import complete (marker: $gameMarker)"
}

Write-Host "Prepare complete. Attached library layout:"
Write-Host "  roms/         -> /romm/library/roms"
Write-Host "  hvsc/         -> /romm/library/hvsc          (SID:)"
Write-Host "  screenshots/  -> /romm/library/screenshots   (Screenshot:)"
exit 0
