[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Status','Bootstrap','Open','Begin','Triage','History')][string]$Operation = 'Status')
$ErrorActionPreference = 'Stop'
$pluginRoot = '/home/sharpninja/github/mcpserver-codex-plugin'
$repoRoot = '/home/sharpninja/github/vice-sharp'
$request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
$identityPath = Join-Path $PSScriptRoot 'review-identity.json'
if (Test-Path -LiteralPath $identityPath) {
    $identity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
} else {
    $measuredNow = Get-Date
    $identity = [ordered]@{ timestampLocal = $measuredNow.ToString('s'); reviewerAgent = 'Codex'; reviewerModel = $request.reviewer.model; reviewerEffort = $request.reviewer.effort; reviewerSessionId = ('Codex-' + $measuredNow.ToUniversalTime().ToString('yyyyMMddTHHmmssZ') + '-nativerestore-r3'); reviewerRequestId = ('req-' + $measuredNow.ToUniversalTime().ToString('yyyyMMddTHHmmssZ') + '-nativerestore-r3'); parentSessionId = $request.parentSessionId; parentRequestId = $request.parentRequestId }
    if ($PSCmdlet.ShouldProcess($identityPath, 'Save reviewer identity')) { $identity | ConvertTo-Json | Set-Content -LiteralPath $identityPath -Encoding utf8NoBOM }
}
$method = ''
$methodParams = @{}
switch ($Operation) {
    'Bootstrap' { $method = 'workflow.sessionlog.bootstrap' }
    'Open' { $method = 'workflow.sessionlog.openSession'; $methodParams = @{ agent = 'Codex'; model = $identity.reviewerModel; sessionId = $identity.reviewerSessionId; title = 'Hostile review of native restoration plan revision 3' } }
    'Begin' { $method = 'workflow.sessionlog.beginTurn'; $methodParams = @{ requestId = $identity.reviewerRequestId; queryTitle = 'Review Phase D native restoration plan revision 3'; queryText = $request.prompt; parentSessionId = $request.parentSessionId; parentRequestId = $request.parentRequestId; tags = @('PLAN-NATIVERESTORE-001', 'hostile-review', ('parentSessionId:' + $request.parentSessionId), ('parentRequestId:' + $request.parentRequestId)) } }
    'Triage' { $method = 'workflow.triage.report'; $methodParams = @{ title = 'Codex plugin review bootstrap remains untrusted on LAB-OMARCHY'; summary = 'Round 3 independent reviewer cannot establish supported MCP logging. Prior incident triage-report-1a233c0d6d834f5f9655508c5a2b5f8b. No repair or queue replay authorized.'; component = 'mcpserver-codex-plugin'; reporterAgent = 'Codex'; errorSignature = 'MCP_UNTRUSTED'; dedupeKey = 'codex-plugin-lab-omarchy-mcp-untrusted'; evidence = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'plugin-Bootstrap.json')); affectedPaths = @('lib/repl-invoke.ps1') } }
    'History' { $method = 'workflow.sessionlog.queryHistory'; $methodParams = @{ agent = 'Codex'; sessionId = $identity.reviewerSessionId; requestId = $identity.reviewerRequestId; limit = 10; offset = 0 } }
}
if (-not $PSCmdlet.ShouldProcess($Operation, 'Invoke supported Codex plugin wrapper')) { return }
$invocation = @{ Command = $(if ($Operation -eq 'Status') { 'Status' } else { 'Invoke' }); WorkspacePath = $repoRoot; PluginRoot = $pluginRoot; CacheRoot = $PSScriptRoot }
if ($method) { $invocation.Method = $method; $invocation.ParamsObject = $methodParams }
$wrapperOutput = ''
$wrapperError = $null
try { $wrapperOutput = (& (Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1') @invocation *>&1 | Out-String) } catch { $wrapperError = $_.Exception.Message }
if (($wrapperOutput + $wrapperError) -match '(?i)(?:Bearer\s+[a-z0-9._-]{16,}|sk-[a-z0-9_-]{16,})') { throw 'Possible credential in plugin output; output refused.' }
$result = [ordered]@{ timestampLocal = (Get-Date -Format 's'); operation = $Operation; method = $method; output = $wrapperOutput; error = $wrapperError; reviewerSessionId = $identity.reviewerSessionId; reviewerRequestId = $identity.reviewerRequestId }
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('plugin-' + $Operation + '.json')) -Encoding utf8NoBOM
$result | ConvertTo-Json -Depth 8
