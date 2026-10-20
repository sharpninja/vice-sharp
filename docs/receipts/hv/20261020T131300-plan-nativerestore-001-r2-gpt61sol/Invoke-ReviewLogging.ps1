[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Status','Open','Triage','History','Invoke')][string] $Operation = 'Status',
    [string] $Method,
    [string] $ParamsFile
)
$ErrorActionPreference = 'Stop'
$workspacePath = '/home/sharpninja/github/vice-sharp'
$pluginRoot = '/home/sharpninja/github/mcpserver-codex-plugin'
$wrapper = Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1'
$cachePath = Join-Path $PSScriptRoot 'plugin-cache'
$identityPath = Join-Path $PSScriptRoot 'reviewer-identity.json'
if (-not $PSCmdlet.ShouldProcess($Operation, 'Invoke supported MCP logging wrapper')) { return }
if (-not (Test-Path -LiteralPath $cachePath)) { New-Item -ItemType Directory -Path $cachePath | Out-Null }
function Invoke-ReviewWrapper {
    param([string] $WrapperCommand = 'Invoke', [string] $WorkflowMethod, [hashtable] $WorkflowParams = @{})
    $capture = @()
    try {
        if ($WrapperCommand -eq 'Status') {
            $capture = @(& $wrapper -Command Status -PluginRoot $pluginRoot -WorkspacePath $workspacePath -CacheRoot $cachePath *>&1)
        } else {
            $capture = @(& $wrapper -Command Invoke -Method $WorkflowMethod -ParamsObject $WorkflowParams -PluginRoot $pluginRoot -WorkspacePath $workspacePath -CacheRoot $cachePath *>&1)
        }
    } catch { $capture += $_.Exception.Message }
    $safeOutput = ($capture | ForEach-Object { $_.ToString() }) -join "`n"
    if ($safeOutput -match '(?i)([?&](api[-_]?key|token|key|code)=|Bearer\s+[A-Za-z0-9]|"apiKey"\s*:\s*"[^"\s]+)') { throw 'Credential-shaped wrapper output blocked before display or storage.' }
    $record = [ordered]@{timestampLocal=(Get-Date -Format 's'); operation=$Operation; method=$WorkflowMethod; output=$safeOutput}
    $record | ConvertTo-Json -Depth 12 -Compress | Add-Content -LiteralPath (Join-Path $PSScriptRoot 'logging-attempts.jsonl') -Encoding utf8
    Write-Output $safeOutput
}
switch ($Operation) {
    'Status' { Invoke-ReviewWrapper -WrapperCommand Status }
    'Open' {
        $request = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') | ConvertFrom-Json
        $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
        $identity = [ordered]@{reviewerAgent='Codex';reviewerModel='gpt-6.1-sol';reviewerEffort='max';reviewerSessionId=('Codex-' + $stamp + '-hv-nativerestore-r2');reviewerRequestId=('req-' + $stamp + '-hv-nativerestore-r2');parentSessionId=$request.parentSessionId;parentRequestId=$request.parentRequestId}
        $identity | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $identityPath -Encoding utf8
        Invoke-ReviewWrapper -WorkflowMethod 'workflow.sessionlog.bootstrap'
        Invoke-ReviewWrapper -WorkflowMethod 'workflow.sessionlog.openSession' -WorkflowParams @{agent=$identity.reviewerAgent;model=$identity.reviewerModel;sessionId=$identity.reviewerSessionId;title='Hostile review of native restoration plan revision 2'}
        Invoke-ReviewWrapper -WorkflowMethod 'workflow.sessionlog.beginTurn' -WorkflowParams @{requestId=$identity.reviewerRequestId;queryTitle='Hostile review of native restoration plan revision 2';queryText=$request.prompt;parentSessionId=$request.parentSessionId;parentRequestId=$request.parentRequestId;contextList=@($request.promptPath,$request.inputPinsPath)}
    }
    'Triage' {
        $identity = Get-Content -Raw -LiteralPath $identityPath | ConvertFrom-Json
        Invoke-ReviewWrapper -WorkflowMethod 'workflow.triage.report' -WorkflowParams @{title='Codex native-restoration review blocked by supported plugin trust failure';summary='Round 2 review on LAB-OMARCHY. Supported wrapper failures are retained in logging-attempts.jsonl in this review receipt. Prior report triage-report-1a233c0d6d834f5f9655508c5a2b5f8b. No trust repair, queue replay, or historical turn cancellation authorized.';component='mcpserver-codex-plugin';errorSignature='MCP_UNTRUSTED';reporterAgent='Codex';dedupeKey='codex-lab-omarchy-marker-refresh-untrusted';affectedPaths=@($wrapper);evidence=('Reviewer session ' + $identity.reviewerSessionId + ', request ' + $identity.reviewerRequestId + '. See this receipt logging-attempts.jsonl.')}
    }
    'History' { Invoke-ReviewWrapper -WorkflowMethod 'workflow.sessionlog.queryHistory' -WorkflowParams @{agent='Codex';limit=100;offset=0} }
    'Invoke' {
        if (-not $Method -or -not $ParamsFile) { throw 'Method and ParamsFile are required.' }
        $workflowParams = Get-Content -Raw -LiteralPath $ParamsFile | ConvertFrom-Json -AsHashtable
        Invoke-ReviewWrapper -WorkflowMethod $Method -WorkflowParams $workflowParams
    }
}
Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffzzz'
