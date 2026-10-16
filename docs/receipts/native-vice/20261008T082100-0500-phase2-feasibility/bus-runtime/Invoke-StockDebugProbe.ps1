param(
    [Parameter(Mandatory)][string]$Name,
    [Parameter(Mandatory)][string]$CommandFile,
    [Parameter(Mandatory)][string]$SuccessMarker
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$runRoot = Join-Path $root $Name
if (Test-Path -LiteralPath $runRoot) { throw 'Refusing to overwrite prior probe evidence.' }
New-Item -ItemType Directory -Path $runRoot | Out-Null
$source = 'F:\GitHub\vice-sharp\artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source'
$executable = Join-Path $source 'vice\src\xvic.exe'
$debugger = 'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe'
$environment = @{PATH='C:\msys64\mingw64\bin;'+$env:PATH;'_NT_SYMBOL_PATH'='';'_NT_ALT_SYMBOL_PATH'='';'_NT_SOURCE_PATH'=''}
$arguments = @('-default','-console','-sound','-sounddev','dummy','-warp','-seed','1',
    '-directory',(Join-Path $source 'vice\data').Replace('\','/'))
$nativeOut = Join-Path $runRoot 'native.stdout.log'
$target = $null
$cdb = $null
$failure = $null
$forced = $false
$result = [ordered]@{startedLocal=(Get-Date).ToString('o');executable=$executable;
    executableSha256=(Get-FileHash -LiteralPath $executable).Hash;arguments=$arguments;
    commandFile=$CommandFile;commandFileSha256=(Get-FileHash -LiteralPath $CommandFile).Hash;
    successMarker=$SuccessMarker;scope='Diagnostic external stock-process observation; no parity acceptance.'}
try {
    $target = Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory (Join-Path $source 'vice\src') -Environment $environment -WindowStyle Hidden -PassThru -RedirectStandardOutput $nativeOut -RedirectStandardError (Join-Path $runRoot 'native.stderr.log')
    $result.targetPid = $target.Id
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $ready = $false
    while ($watch.ElapsedMilliseconds -lt 5000 -and -not $ready) {
        $ready = (Test-Path -LiteralPath $nativeOut) -and [bool](Select-String -LiteralPath $nativeOut -Pattern 'VSync:.*Sync reset')
        if ($target.HasExited) { throw 'Native process exited before debugger attachment.' }
        if (-not $ready) { [Threading.Thread]::Sleep(50) }
    }
    if (-not $ready) { throw 'Native startup marker was not observed.' }
    $debugArguments = @('-y', ('"'+$root+'"'), '-cf', ('"'+$CommandFile+'"'), '-p', [string]$target.Id)
    $result.debuggerArguments = $debugArguments
    $debugOut = Join-Path $runRoot 'debugger.stdout.log'
    $cdb = Start-Process -FilePath $debugger -ArgumentList $debugArguments -WorkingDirectory $root -Environment $environment -WindowStyle Hidden -PassThru -RedirectStandardOutput $debugOut -RedirectStandardError (Join-Path $runRoot 'debugger.stderr.log')
    $result.debuggerPid = $cdb.Id
    $result | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $runRoot 'launch.json') -Encoding utf8
    $result.debuggerCompletedWithin10s = $cdb.WaitForExit(10000)
    $result.successMarkerCount = @(Select-String -LiteralPath $debugOut -Pattern ('^'+[regex]::Escape($SuccessMarker)+'$')).Count
    if ($result.successMarkerCount -ne 1) { throw 'Expected diagnostic completion marker not observed exactly once.' }
    if (-not $result.debuggerCompletedWithin10s) { throw 'Debugger did not exit after diagnostic.' }
    $result.debuggerExitCode = $cdb.ExitCode
    if ($cdb.ExitCode -ne 0) { throw 'Debugger exited with error.' }
} catch {
    $failure = $_.Exception.ToString()
} finally {
    if ($null -ne $cdb -and -not $cdb.HasExited) { $cdb.Kill($true);$cdb.WaitForExit(5000)|Out-Null;$forced=$true }
    if ($null -ne $target) {
        if (-not $target.WaitForExit(1000)) { $target.Kill($true);$target.WaitForExit(5000)|Out-Null;$forced=$true }
        $result.targetExitCode = $target.ExitCode
    }
    $result.endedLocal = (Get-Date).ToString('o')
    $result.completed = $null -eq $failure
    $result.failure = $failure
    $result.forcedTermination = $forced
    $result | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $runRoot 'result.json') -Encoding utf8
    if ($null -ne $target) { $target.Dispose() }
    if ($null -ne $cdb) { $cdb.Dispose() }
}
[pscustomobject]$result
