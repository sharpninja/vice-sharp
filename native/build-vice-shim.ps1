param()

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$resolvedScriptDir = (Resolve-Path $scriptDir).Path
$msysScriptDir = '/' + $resolvedScriptDir.Substring(0, 1).ToLowerInvariant() + $resolvedScriptDir.Substring(2).Replace('\', '/')

if (-not (Test-Path 'C:\msys64\usr\bin\bash.exe')) {
    throw 'MSYS2 bash was not found at C:\msys64\usr\bin\bash.exe.'
}

$env:MSYSTEM = 'MINGW64'
& 'C:\msys64\usr\bin\bash.exe' -lc "cd '$msysScriptDir' && bash ./build-vice-shim.sh"
