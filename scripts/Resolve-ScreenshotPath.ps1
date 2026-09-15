<#
.SYNOPSIS
  Resolve a GameBase64 VERSION.NFO Screenshot: path against library screenshots.

.PARAMETER ScreenshotPath
  Relative path from NFO, e.g. A\Alfabug.png

.PARAMETER ScreenshotsRoot
  Screenshots root (default: env SCREENSHOTS_ROOT, then runtime/library/screenshots,
  then legacy ./gb64/Screenshots)
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$ScreenshotPath,

    [Parameter()]
    [string]$ScreenshotsRoot = $(
        if ($env:SCREENSHOTS_ROOT) { $env:SCREENSHOTS_ROOT }
        else {
            $repo = Split-Path $PSScriptRoot -Parent
            $lib = Join-Path $repo 'runtime\library\screenshots'
            $legacy = Join-Path $repo 'gb64\Screenshots'
            if (Test-Path $lib) { $lib }
            elseif (Test-Path $legacy) { $legacy }
            else { '/romm/library/screenshots' }
        }
    )
)

$ErrorActionPreference = 'Stop'
$rel = ($ScreenshotPath -replace '\\', '/').TrimStart('/')
$candidate = Join-Path $ScreenshotsRoot ($rel -replace '/', [IO.Path]::DirectorySeparatorChar)

if (Test-Path -LiteralPath $candidate -PathType Leaf) {
    Write-Output (Resolve-Path -LiteralPath $candidate).Path
    exit 0
}

$dir = Split-Path $candidate -Parent
$base = Split-Path $candidate -Leaf
if (Test-Path -LiteralPath $dir) {
    $hit = Get-ChildItem -LiteralPath $dir -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -ieq $base } |
        Select-Object -First 1
    if ($hit) {
        Write-Output $hit.FullName
        exit 0
    }
}

Write-Error "Screenshot not found under $ScreenshotsRoot : $rel"
exit 1
