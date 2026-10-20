#Requires -Version 7.0
<#
.SYNOPSIS
Launches the exact GPT-6.1-Sol max review recorded in request.jsonl through the Codex CLI with full stream capture.
Refuses to run unless the pre-review checklist, the pins, the reviewer identity, the CLI binary and the prompt all
match the request (profile 7.5 hard gate). Review time is unbounded (profile 7.1): the process is started and left
running; the caller waits on it with read-only checks.
#>
[CmdletBinding(SupportsShouldProcess)]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json -AsHashtable
$selfReview = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SELF-REVIEW.md') -Raw
foreach ($heading in @('Holistic plan', 'What-if per changed criterion', 'Cross-rule failures', 'Propagation', 'Phase dependencies', 'Search-before-adding', 'No new mechanism', 'Mapping lint', 'Standing rules')) {
    if (-not $selfReview.Contains($heading)) { throw "Missing checklist evidence: $heading" }
}
if ($request.reviewer.model -cne 'gpt-6.1-sol' -or $request.reviewer.effort -cne 'max') { throw 'Reviewer mismatch.' }
foreach ($pin in (Get-Content -LiteralPath $request.inputPinsPath -Raw | ConvertFrom-Json)) {
    if ((Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash -cne $pin.sha256) { throw "Changed input: $($pin.path)" }
}
if ((Get-FileHash -LiteralPath $request.executable -Algorithm SHA256).Hash -cne $request.executableSha256) { throw 'CLI binary changed.' }
$prompt = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'prompt.md') -Raw
if ($prompt -cne $request.prompt) { throw 'Request prompt differs.' }
if (Test-Path -LiteralPath $request.responsePath) { throw 'Existing response; refuse duplicate.' }
if (-not $PSCmdlet.ShouldProcess($request.gate, 'Launch GPT-6.1-Sol max review through Codex CLI with full stream capture')) { return }
$start = [Diagnostics.ProcessStartInfo]::new()
$start.FileName = $request.executable
$start.WorkingDirectory = $request.workspacePath
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.RedirectStandardInput = $true
$start.StandardInputEncoding = [Text.UTF8Encoding]::new($false)
foreach ($argument in $request.arguments) { $start.ArgumentList.Add($argument) }
foreach ($key in @($start.Environment.Keys)) {
    if ($key -match '^(MCP_|MCPSERVER_|PLUGIN_|GROK_)|_PLUGIN_ROOT$') { [void]$start.Environment.Remove($key) }
}
foreach ($key in $request.environment.Keys) { $start.Environment[$key] = [string]$request.environment[$key] }
$stdout = [IO.FileStream]::new($request.responsePath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
$stderr = [IO.FileStream]::new($request.stderrPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
$process = [Diagnostics.Process]::new()
$process.StartInfo = $start
try {
    if (-not $process.Start()) { throw 'Process start failed.' }
    $outTask = $process.StandardOutput.BaseStream.CopyToAsync($stdout)
    $errTask = $process.StandardError.BaseStream.CopyToAsync($stderr)
    $process.StandardInput.Write($prompt)
    $process.StandardInput.Close()
    $launch = [ordered]@{ startedAtLocal = (Get-Date -Format s); pid = $process.Id; model = $request.reviewer.model; effort = $request.reviewer.effort; captureStarted = $true; terminal = $false }
    $launch | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'launch-result.json') -Encoding utf8
    Write-Output ("review started: pid {0} at {1}" -f $process.Id, $launch.startedAtLocal)
    $process.WaitForExit()
    [void]$outTask.Wait(); [void]$errTask.Wait()
    $terminal = [ordered]@{ endedAtLocal = (Get-Date -Format s); pid = $process.Id; exitCode = $process.ExitCode; responseBytes = (Get-Item -LiteralPath $request.responsePath).Length; stderrBytes = (Get-Item -LiteralPath $request.stderrPath).Length }
    $terminal | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'terminal-result.json') -Encoding utf8
    Write-Output ("review ended: exit {0} at {1}, response {2} bytes" -f $process.ExitCode, $terminal.endedAtLocal, $terminal.responseBytes)
}
finally { $stdout.Dispose(); $stderr.Dispose() }
