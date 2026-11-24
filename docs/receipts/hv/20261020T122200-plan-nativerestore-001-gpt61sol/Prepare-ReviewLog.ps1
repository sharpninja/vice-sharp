[CmdletBinding(SupportsShouldProcess)]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess($PSScriptRoot, 'Serialize reviewer MCP request parameters')) { return }
$request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
$begin = [ordered]@{
    queryTitle = 'Hostile review of native restoration Phase D plan'
    queryText = $request.prompt
    planFile = 'docs/plans/PLAN-NATIVERESTORE-001.md'
    todoId = 'PLAN-NATIVERESTORE-001'
    contextList = @('parentSessionId=' + $request.parentSessionId; 'parentRequestId=' + $request.parentRequestId; $request.promptPath; $request.responsePath)
    tags = @('hostile-review', 'class-1', 'gpt-6.1-sol', 'effort-max', 'parentSessionId:' + $request.parentSessionId, 'parentRequestId:' + $request.parentRequestId)
}
$triage = [ordered]@{
    title = 'Codex review bootstrap cannot establish health nonce in restricted review process'
    summary = 'Supported plugin session-start returned MCP_UNTRUSTED and Status hasSession=false. Local plugin Test-MarkerSignature returns true. Health nonce could not be established. No settings, hooks, queues, historical sessions, or environment repaired.'
    observedBehavior = 'No own session or review turn opened; initial overlong process-local temp path caused Unix socket length errors, subsequent default-temp launch reports Permission denied. Network restriction is a possible cause, not verified.'
    expectedBehavior = 'Supported plugin verifies trust, opens an isolated reviewer session, and persists a parent-linked review turn.'
    severity = 'high'
    component = 'mcpserver-codex-plugin/bootstrap'
    workspacePath = $request.workspacePath
    dedupeKey = 'hv-nativerestore-codex-restricted-bootstrap-20261020'
    affectedPaths = @('/home/sharpninja/github/mcpserver-codex-plugin/lib/session-start.ps1', '/home/sharpninja/github/mcpserver-codex-plugin/lib/marker-resolver.ps1')
    evidence = @('mcp-bootstrap.txt', 'mcp-bootstrap-second.txt', 'mcp-trust-diagnosis.json')
    reproductionHints = 'Use the exact requested supported wrapper/root in this reviewer sandbox with an isolated CacheRoot and MCP_FAILSAFE_DRAIN_DISABLED=1.'
    tags = @('Codex', 'hostile-review', 'bootstrap', 'PLAN-NATIVERESTORE-001')
    reporterAgent = 'Codex'
    parentSessionId = $request.parentSessionId
    parentRequestId = $request.parentRequestId
}
foreach ($record in @(@{ Name = 'begin-params.json'; Value = $begin }, @{ Name = 'triage-params.json'; Value = $triage })) {
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot $record.Name), ($record.Value | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
}
