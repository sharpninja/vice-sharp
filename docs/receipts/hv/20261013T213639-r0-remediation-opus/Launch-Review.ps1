[CmdletBinding(SupportsShouldProcess)]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$request=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json -AsHashtable
$selfReview=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SELF-REVIEW.md') -Raw
foreach($heading in @('Holistic plan','What-if per changed criterion','Cross-rule failures','Propagation','Phase dependencies','Search-before-adding','No new mechanism','Mapping lint','Standing rules')){
    if(-not $selfReview.Contains($heading)){throw "Missing pre-HV checklist item: $heading"}
}
$verification=Get-Content -LiteralPath $request.verificationPath -Raw | ConvertFrom-Json -AsHashtable
if($verification.failedCount -ne 0 -or $verification.checkCount -ne 177){throw 'Document verification gate failed'}
if((Get-FileHash -LiteralPath $verification.candidatePath -Algorithm SHA256).Hash -cne $verification.candidateSha256){throw 'Candidate changed after verification'}
if($request.reviewer.model -cne 'claude-opus-5-5' -or $request.reviewer.effort -cne 'max'){throw 'Reviewer model/effort mismatch'}
if($request.artificialTurnLimit -or $request.artificialTimeout -or $request.fallbackModel){throw 'Review limits or substitution forbidden'}
if($request.arguments -contains '--max-turns' -or $request.arguments -contains '--dangerously-skip-permissions'){throw 'Disallowed review option'}
$pins=Get-Content -LiteralPath $request.inputPinsPath -Raw | ConvertFrom-Json -AsHashtable
foreach($pin in $pins){
    if(-not (Test-Path -LiteralPath $pin.path -PathType Leaf)){throw "Missing input: $($pin.path)"}
    if((Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash -cne $pin.sha256){throw "Changed pinned input: $($pin.path)"}
}
if((Get-FileHash -LiteralPath $request.executable -Algorithm SHA256).Hash -cne $request.executableSha256){throw 'CLI binary changed'}
$prompt=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'prompt.md') -Raw
if($prompt -cne $request.prompt){throw 'Request does not contain exact prompt bytes decoded as UTF8'}
$same=@(Get-CimInstance Win32_Process -Filter "Name='claude.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Contains($request.cliSessionId) })
if($same.Count -or (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'verdict.json')) -or (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'response.jsonl'))){throw 'Existing reviewer/capture/verdict; refuse duplicate launch'}
if(-not $PSCmdlet.ShouldProcess(('Local ClaudeCode '+$request.cliSessionId),'Launch exact Opus5.5max read-only HV with complete stream capture')){return}
$start=[System.Diagnostics.ProcessStartInfo]::new()
$start.FileName=$request.executable
$start.WorkingDirectory=$request.workspacePath
$start.UseShellExecute=$false
$start.CreateNoWindow=$true
$start.RedirectStandardOutput=$true
$start.RedirectStandardError=$true
$start.RedirectStandardInput=$true
$start.StandardInputEncoding=[System.Text.UTF8Encoding]::new($false)
foreach($argument in $request.arguments){$start.ArgumentList.Add($argument)}
foreach($key in @($start.Environment.Keys)){
    if($key -match '^(MCP_|MCPSERVER_|PLUGIN_|GROK_)|_PLUGIN_ROOT$'){[void]$start.Environment.Remove($key)}
}
foreach($key in $request.environment.Keys){$start.Environment[$key]=[string]$request.environment[$key]}
$stdout=[System.IO.FileStream]::new($request.responsePath,[System.IO.FileMode]::CreateNew,[System.IO.FileAccess]::Write,[System.IO.FileShare]::Read)
$stderr=[System.IO.FileStream]::new($request.stderrPath,[System.IO.FileMode]::CreateNew,[System.IO.FileAccess]::Write,[System.IO.FileShare]::Read)
$process=[System.Diagnostics.Process]::new()
$process.StartInfo=$start
try{
    if(-not $process.Start()){throw 'Process.Start returned false'}
    $outTask=$process.StandardOutput.BaseStream.CopyToAsync($stdout)
    $errTask=$process.StandardError.BaseStream.CopyToAsync($stderr)
    $process.StandardInput.Write($prompt)
    $process.StandardInput.Close()
    [ordered]@{process=$process;stdout=$stdout;stderr=$stderr;outTask=$outTask;errTask=$errTask;request=$request;launch=[ordered]@{startedAtLocal=(Get-Date -Format s);pid=$process.Id;cliSessionId=$request.cliSessionId;model=$request.reviewer.model;effort=$request.reviewer.effort;requestPath=(Join-Path $PSScriptRoot 'request.jsonl');responsePath=$request.responsePath;stderrPath=$request.stderrPath;captureStarted=$true;terminal=$false}}
}catch{
    $stdout.Dispose()
    $stderr.Dispose()
    throw
}
