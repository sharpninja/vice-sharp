$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$workspace = 'F:\GitHub\vice-sharp'
$source = Join-Path $workspace 'artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source'
$executable = Join-Path $workspace 'artifacts\immutable-vice\clock-hook\xvic-gate-probe.exe'
$debugger = 'C:\Program Files (x86)\Windows Kits\10\Debuggers\x64\cdb.exe'
$stdoutPath = Join-Path $root 'after-init-watch.stdout.log'
$stderrPath = Join-Path $root 'after-init-watch.stderr.log'
if (Test-Path -LiteralPath $stdoutPath) { throw 'Refusing to overwrite prior debugger evidence.' }
$start = [Diagnostics.ProcessStartInfo]::new($executable)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardInput = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.WorkingDirectory = Join-Path $source 'vice\src'
$start.Environment['PATH'] = 'C:\msys64\mingw64\bin;' + $start.Environment['PATH']
$arguments = @('-default','-console','-sound','-sounddev','dummy','-warp','-seed','1',
    '-directory',(Join-Path $source 'vice\data').Replace('\','/'))
foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
$target = $null
$cdb = $null
$output = $null
$errorStream = $null
$first = $null
$failure = $null
$forced = $false
$result = [ordered]@{startedLocal=(Get-Date).ToString('o');executable=$executable;
    executableSha256=(Get-FileHash -LiteralPath $executable).Hash;arguments=$arguments;
    purpose='Test hardware watchpoint after native startup using already-validated external callback pause. No software breakpoint or native source edit.'}
try {
    $target = [Diagnostics.Process]::Start($start)
    if ($null -eq $target) { throw 'Native probe did not start.' }
    $result.targetPid = $target.Id
    $output = $target.StandardOutput.ReadToEndAsync()
    $firstTask = $target.StandardError.ReadLineAsync()
    if (-not $firstTask.Wait(5000)) { throw 'First native callback was not observed.' }
    $first = $firstTask.GetAwaiter().GetResult()
    $result.firstCallback = $first
    if ($first -cne 'VICE_GATE,7') { throw "Unexpected first callback: $first" }
    $errorStream = $target.StandardError.ReadToEndAsync()
    $debugArguments = @('-y', ('"'+$root+'"'), '-cf', ('"'+(Join-Path $root 'after-init-watch.cdb')+'"'), '-p', [string]$target.Id)
    $result.debuggerArguments = $debugArguments
    $cdb = Start-Process -FilePath $debugger -ArgumentList $debugArguments -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdoutPath -RedirectStandardError $stderrPath -Environment @{'_NT_SYMBOL_PATH'='';'_NT_ALT_SYMBOL_PATH'='';'_NT_SOURCE_PATH'='';PATH=$start.Environment['PATH']}
    $result.debuggerPid = $cdb.Id
    $result | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $root 'after-init-watch.launch.json') -Encoding utf8
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $armed = $false
    while ($watch.ElapsedMilliseconds -lt 5000 -and -not $armed) {
        $armed = (Test-Path -LiteralPath $stdoutPath) -and [bool](Select-String -LiteralPath $stdoutPath -Pattern '^AFTER_INIT_WATCH_ARMED$')
        if (-not $armed) { [Threading.Thread]::Sleep(50) }
    }
    if (-not $armed) { throw 'Debugger did not acknowledge arming.' }
    $target.StandardInput.Write('c')
    $target.StandardInput.Flush()
    $result.permissionSentLocal = (Get-Date).ToString('o')
    $result.debuggerCompletedWithin5s = $cdb.WaitForExit(5000)
    $result.hardwareHitCount = @(Select-String -LiteralPath $stdoutPath -Pattern '^AFTER_INIT_WATCH_HIT$').Count
    if ($result.hardwareHitCount -ne 1) { throw 'No single hardware-watch hit was observed.' }
    if (-not $result.debuggerCompletedWithin5s) { throw 'Debugger did not exit after its hit command.' }
    $result.debuggerExitCode = $cdb.ExitCode
} catch {
    $failure = $_.Exception.ToString()
} finally {
    if ($null -ne $cdb -and -not $cdb.HasExited) { $cdb.Kill($true); $cdb.WaitForExit(5000) | Out-Null; $forced=$true }
    if ($null -ne $target) {
        if (-not $target.WaitForExit(1000)) { $target.Kill($true);$target.WaitForExit(5000)|Out-Null;$forced=$true }
        $result.targetExitCode = $target.ExitCode
        if ($null -ne $output) { $output.GetAwaiter().GetResult() | Out-File -LiteralPath (Join-Path $root 'after-init-native.stdout.log') -Encoding utf8 -NoNewline }
        $first | Out-File -LiteralPath (Join-Path $root 'after-init-native.stderr.log') -Encoding utf8
        if ($null -ne $errorStream) { $errorStream.GetAwaiter().GetResult() | Out-File -LiteralPath (Join-Path $root 'after-init-native.stderr.log') -Encoding utf8 -Append -NoNewline }
    }
    $result.completed = $null -eq $failure
    $result.failure = $failure
    $result.forcedTermination = $forced
    $result.endedLocal = (Get-Date).ToString('o')
    $result | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $root 'after-init-watch.result.json') -Encoding utf8
    if ($null -ne $target) { $target.Dispose() }
    if ($null -ne $cdb) { $cdb.Dispose() }
}
[pscustomobject]$result
