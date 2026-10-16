[CmdletBinding(SupportsShouldProcess)]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json -AsHashtable
$selfReview = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SELF-REVIEW.md') -Raw
foreach ($heading in @('Holistic plan','What-if per changed criterion','Cross-rule failures','Propagation','Phase dependencies','Search-before-adding','No new mechanism','Mapping lint','Standing rules')) {
    if (!$selfReview.Contains($heading)) { throw "Missing checklist evidence: $heading" }
}
if ($request.reviewer.model -cne 'claude-opus-5-5' -or $request.reviewer.effort -cne 'max') { throw 'Reviewer mismatch.' }
if ($request.arguments -contains '--max-turns' -or $request.arguments -contains '--dangerously-skip-permissions') { throw 'Forbidden review argument.' }
foreach ($pin in (Get-Content -LiteralPath $request.inputPinsPath -Raw | ConvertFrom-Json)) {
    if ((Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash -cne $pin.sha256) { throw "Changed input: $($pin.path)" }
}
foreach ($trx in $request.validationTrx) {
    $doc = [xml](Get-Content -LiteralPath $trx -Raw)
    $counts = $doc.TestRun.ResultSummary.Counters
    if ([int]$counts.total -le 0 -or [int]$counts.passed -ne [int]$counts.total) { throw "Incomplete validation: $trx" }
}
if ((Get-FileHash -LiteralPath $request.executable -Algorithm SHA256).Hash -cne $request.executableSha256) { throw 'CLI binary changed.' }
$prompt = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'prompt.md') -Raw
if ($prompt -cne $request.prompt) { throw 'Request prompt differs.' }
if (Test-Path -LiteralPath $request.responsePath) { throw 'Existing response; refuse duplicate.' }
$running = @(Get-CimInstance Win32_Process -Filter "Name='claude.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($request.cliSessionId) })
if ($running.Count) { throw 'Reviewer already running.' }
if (!$PSCmdlet.ShouldProcess($request.cliSessionId, 'Launch local exact Opus 5.5 max review with full stream capture')) { return }
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
    if (!$process.Start()) { throw 'Process start failed.' }
    $outTask = $process.StandardOutput.BaseStream.CopyToAsync($stdout)
    $errTask = $process.StandardError.BaseStream.CopyToAsync($stderr)
    $process.StandardInput.Write($prompt)
    $process.StandardInput.Close()
    [ordered]@{
        process = $process; stdout = $stdout; stderr = $stderr; outTask = $outTask; errTask = $errTask; request = $request
        launch = [ordered]@{ startedAtLocal = (Get-Date -Format s); pid = $process.Id; cliSessionId = $request.cliSessionId;
            model = $request.reviewer.model; effort = $request.reviewer.effort; captureStarted = $true; terminal = $false }
    }
}
catch { $stdout.Dispose(); $stderr.Dispose(); throw }
