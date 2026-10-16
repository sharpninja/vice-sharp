param(
    [Parameter(Mandatory)][string]$Name,
    [ValidateRange(0, 256)][int]$Permissions = 16,
    [ValidateSet('quit', 'eof', 'invalid', 'uppercase', 'newline', 'nul')][string]$FinalCommand = 'quit'
)
$ErrorActionPreference = 'Stop'
$receiptRoot = $PSScriptRoot
$runRoot = Join-Path $receiptRoot $Name
if (Test-Path -LiteralPath $runRoot) { throw 'Refusing to overwrite an existing probe receipt.' }
New-Item -ItemType Directory -Path $runRoot | Out-Null
$workspace = 'F:\GitHub\vice-sharp'
$source = Join-Path $workspace 'artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source'
$executable = Join-Path $workspace 'artifacts\immutable-vice\clock-hook\xvic-gate-probe.exe'
$arguments = @('-default', '-console', '-sound', '-sounddev', 'dummy', '-warp', '-seed', '1',
    '-directory', (Join-Path $source 'vice\data').Replace('\', '/'))
$start = [Diagnostics.ProcessStartInfo]::new($executable)
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardInput = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.WorkingDirectory = Join-Path $source 'vice\src'
$start.Environment['PATH'] = 'C:\msys64\mingw64\bin;' + $start.Environment['PATH']
foreach ($argument in $arguments) { $start.ArgumentList.Add($argument) }
$events = [Collections.Generic.List[object]]::new()
$lines = [Collections.Generic.List[string]]::new()
$clocks = [Collections.Generic.List[ulong]]::new()
$process = $null
$pending = $null
$stdout = $null
$failure = $null
$forced = $false
$expectedExit = switch ($FinalCommand) { 'quit' { 67 } 'eof' { 66 } default { 68 } }
$receipt = [ordered]@{
    startedLocal = (Get-Date).ToString('o'); executable = $executable
    executableSha256 = (Get-FileHash -LiteralPath $executable).Hash
    arguments = $arguments; workingDirectory = $start.WorkingDirectory
    permissions = $Permissions; finalCommand = $FinalCommand; expectedExit = $expectedExit
}
$receipt | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $runRoot 'launch.json') -Encoding utf8
try {
    $process = [Diagnostics.Process]::Start($start)
    if ($null -eq $process) { throw 'Probe did not start.' }
    $receipt.pid = $process.Id
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $pending = $process.StandardError.ReadLineAsync()
    for ($index = 0; $index -le $Permissions; $index++) {
        if (-not $pending.Wait(5000)) { throw 'Clock record timed out; not a passing gate.' }
        $line = $pending.GetAwaiter().GetResult()
        $pending = $null
        if ($null -ne $line) { $lines.Add($line) }
        if ($line -notmatch '^VICE_GATE,([0-9]+)$') { throw "Unexpected clock record: $line" }
        $value = [ulong]::Parse($Matches[1], [Globalization.CultureInfo]::InvariantCulture)
        $clocks.Add($value)
        $events.Add([ordered]@{timestampLocal=(Get-Date).ToString('o');kind='observed';index=$index;clock=$value})
        $pending = $process.StandardError.ReadLineAsync()
        $watch = [Diagnostics.Stopwatch]::StartNew()
        if ($pending.Wait(250)) { throw 'Probe produced output or EOF without permission.' }
        $watch.Stop()
        if ($process.HasExited) { throw 'Probe exited while permission was withheld.' }
        $events.Add([ordered]@{timestampLocal=(Get-Date).ToString('o');kind='permission-withheld';index=$index;milliseconds=$watch.Elapsed.TotalMilliseconds})
        if ($index -lt $Permissions) {
            $process.StandardInput.Write('c')
            $process.StandardInput.Flush()
            $events.Add([ordered]@{timestampLocal=(Get-Date).ToString('o');kind='permit';index=$index})
        }
    }
    if ($FinalCommand -eq 'eof') { $process.StandardInput.Close() }
    else {
        $command = switch ($FinalCommand) { 'quit' { 'q' } 'invalid' { 'x' } 'uppercase' { 'C' } 'newline' { "`n" } 'nul' { [string][char]0 } }
        $process.StandardInput.Write($command)
        $process.StandardInput.Flush()
    }
    $events.Add([ordered]@{timestampLocal=(Get-Date).ToString('o');kind='terminal-command';command=$FinalCommand})
    if (-not $pending.Wait(5000)) { throw 'Terminal record timed out.' }
    $line = $pending.GetAwaiter().GetResult()
    $pending = $null
    if ($null -ne $line) { $lines.Add($line) }
    if ($line -cne "VICE_GATE_STOP,$expectedExit") { throw "Wrong terminal record: $line" }
    if (-not $process.WaitForExit(5000)) { throw 'Probe did not terminate after terminal command.' }
    if ($process.ExitCode -ne $expectedExit) { throw "Wrong process exit: $($process.ExitCode)" }
    $remainder = $process.StandardError.ReadToEnd()
    if ($remainder.Length -ne 0) { $lines.Add($remainder); throw 'Unexpected output after terminal record.' }
    if ($clocks.Count -ne $Permissions + 1) { throw 'Incorrect callback count.' }
} catch {
    $failure = $_.Exception.ToString()
} finally {
    if ($null -ne $process) {
        if (-not $process.HasExited) { $process.Kill($true); $forced = $true; $process.WaitForExit(5000) | Out-Null }
        if ($null -ne $pending -and $pending.Wait(5000)) {
            $line = $pending.GetAwaiter().GetResult()
            if ($null -ne $line) { $lines.Add($line) }
        }
        $remainder = $process.StandardError.ReadToEnd()
        if ($remainder.Length -ne 0) { $lines.Add($remainder) }
        $stdout.GetAwaiter().GetResult() | Out-File -LiteralPath (Join-Path $runRoot 'stdout.log') -Encoding utf8 -NoNewline
        $receipt.exitCode = $process.ExitCode
    }
    $lines | Out-File -LiteralPath (Join-Path $runRoot 'stderr.log') -Encoding utf8
    $receipt.endedLocal = (Get-Date).ToString('o')
    $receipt.completed = $null -eq $failure
    $receipt.failure = $failure
    $receipt.forcedTermination = $forced
    $receipt.clocks = $clocks.ToArray()
    $receipt.events = $events.ToArray()
    $receipt.scope = 'Post-vic_cycle callback permission only; not complete device freeze, bus trace or parity acceptance.'
    $receipt | ConvertTo-Json -Depth 8 | Out-File -LiteralPath (Join-Path $runRoot 'result.json') -Encoding utf8
    if ($null -ne $process) { $process.Dispose() }
}
[pscustomobject]$receipt | Select-Object completed,exitCode,expectedExit,forcedTermination,@{n='observations';e={$_.clocks.Count}},failure
if ($failure) { throw $failure }
