[CmdletBinding()]
param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$nativeRoot = Join-Path $repoRoot 'native'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot 'artifacts\native\vice-sharp-native-win-x64.zip'
}

$requiredFiles = @(
    'vice_x64.dll',
    'libiconv-2.dll',
    'zlib1.dll'
)

$missing = @()
foreach ($fileName in $requiredFiles) {
    $path = Join-Path $nativeRoot $fileName
    if (-not (Test-Path -LiteralPath $path)) {
        $missing += $fileName
    }
}

if ($missing.Count -gt 0) {
    $list = $missing -join ', '
    throw "Missing native binary file(s): $list. Run native/build-vice-shim.ps1 on Windows with MSYS2 installed, then retry packaging."
}

$resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutput
$stagingRoot = Join-Path $outputDirectory 'vice-sharp-native-win-x64'
$stagingNative = Join-Path $stagingRoot 'native'

if (Test-Path -LiteralPath $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingNative -Force | Out-Null

foreach ($fileName in $requiredFiles) {
    Copy-Item -LiteralPath (Join-Path $nativeRoot $fileName) -Destination (Join-Path $stagingNative $fileName)
}

if (Test-Path -LiteralPath $resolvedOutput) {
    Remove-Item -LiteralPath $resolvedOutput -Force
}

Compress-Archive -Path (Join-Path $stagingRoot '*') -DestinationPath $resolvedOutput -CompressionLevel Optimal
Get-Item -LiteralPath $resolvedOutput
