<#
.SYNOPSIS
  Resolve a GameBase64 VERSION.NFO SID: path against the library HVSC tree.

.PARAMETER SidPath
  Relative path from NFO, e.g. MUSICIANS\W\Whittaker_David\180.sid

.PARAMETER HvscRoot
  HVSC root (default: env HVSC_ROOT, then runtime/library/hvsc, then legacy ./hvsc)
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$SidPath,

    [Parameter()]
    [string]$HvscRoot = $(
        if ($env:HVSC_ROOT) { $env:HVSC_ROOT }
        else {
            $repo = Split-Path $PSScriptRoot -Parent
            $lib = Join-Path $repo 'runtime\library\hvsc'
            $legacy = Join-Path $repo 'hvsc'
            if (Test-Path $lib) { $lib }
            elseif (Test-Path $legacy) { $legacy }
            else { '/romm/library/hvsc' }
        }
    )
)

$ErrorActionPreference = 'Stop'
$rel = ($SidPath -replace '\\', '/').TrimStart('/')
$candidate = Join-Path $HvscRoot ($rel -replace '/', [IO.Path]::DirectorySeparatorChar)

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

Write-Error "SID not found under $HvscRoot : $rel"
exit 1
