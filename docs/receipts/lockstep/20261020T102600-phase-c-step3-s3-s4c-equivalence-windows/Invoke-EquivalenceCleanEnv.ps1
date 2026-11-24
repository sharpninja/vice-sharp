#Requires -Version 7.4
<#
.SYNOPSIS
    Runs tools/immutable-vice/Invoke-ObserverEquivalence.ps1 on PAYTON-LEGION2 in a fresh pwsh -NoProfile whose PATH
    is the registry PATH (machine, then user) with exactly one declared addition, C:/msys64/mingw64/bin, so the
    MinGW-built emulators find their runtime DLLs (libcurl-4, libpng16-16, libstdc++-6, zlib1). The parent console's
    PATH, which held a leftover mingw64/bin entry, is not inherited. Running the built binaries is not compiling;
    nothing here builds. Output goes to the given OutDir (the equivalence script's own evidence) and console.log.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = 'F:/GitHub/vice-sharp',
    [int[]]$Cycles = @(100000),
    [ValidateSet('x64sc', 'xvic', 'both')][string]$Machine = 'both',
    [Parameter(Mandatory)][string]$OutDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$registryPath = ([Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User'))
$childPath = 'C:/msys64/mingw64/bin;' + $registryPath
$script = "$RepoRoot/tools/immutable-vice/Invoke-ObserverEquivalence.ps1"
$psi = [System.Diagnostics.ProcessStartInfo]::new((Get-Process -Id $PID).Path)
foreach ($a in @('-NoProfile', '-NonInteractive', '-File', $script, '-Machine', $Machine, '-OutDir', $OutDir, '-Cycles') + @($Cycles | ForEach-Object { "$_" })) { [void]$psi.ArgumentList.Add($a) }
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.WorkingDirectory = $RepoRoot
$psi.Environment['Path'] = $childPath
foreach ($v in @('MCP_PLUGIN_ROOT', 'MCP_CACHE_DIR_OVERRIDE', 'VICESHARP_FAST_PATH', 'VICESHARP_BUS_RAW', 'VICESHARP_OBS_TRACE_FILES')) { [void]$psi.Environment.Remove($v) }
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$p = [System.Diagnostics.Process]::Start($psi)
$out = $p.StandardOutput.ReadToEndAsync()
$err = $p.StandardError.ReadToEndAsync()
$p.WaitForExit()
$sw.Stop()
$text = "STARTED-BY: Invoke-EquivalenceCleanEnv.ps1`nCHILD PATH first entry: C:/msys64/mingw64/bin (then registry machine+user PATH)`nARGS: $($psi.ArgumentList -join ' ')`nEXIT: $($p.ExitCode)`nSECONDS: $([math]::Round($sw.Elapsed.TotalSeconds,1))`n--- STDOUT`n$($out.Result)`n--- STDERR`n$($err.Result)"
[System.IO.File]::WriteAllText("$OutDir/console.log", $text, [System.Text.UTF8Encoding]::new($false))
$text
exit $p.ExitCode
