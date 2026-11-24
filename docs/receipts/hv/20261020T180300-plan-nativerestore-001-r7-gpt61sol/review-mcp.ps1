[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidateSet('Status','Open','Begin','Invoke')][string]$Operation,
    [string]$Method = '',
    [string]$PayloadPath = '',
    [string]$ReceiptName = ''
)
$ErrorActionPreference = 'Stop'
$pluginRoot = '/home/sharpninja/github/mcpserver-codex-plugin'
$workspaceRoot = '/home/sharpninja/github/vice-sharp'
$cacheRoot = Join-Path $PSScriptRoot 'review-plugin-cache'
$identityPath = Join-Path $PSScriptRoot 'review-identity.json'
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'
if (-not $PSCmdlet.ShouldProcess($Operation, 'Invoke supported Codex MCP plugin for this review')) { return }
if (-not (Test-Path -LiteralPath $cacheRoot)) { $null = New-Item -ItemType Directory -Path $cacheRoot }
$wrapper = Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1'
if ($Operation -eq 'Status') {
    $outputText = (& $wrapper -Command Status -WorkspacePath $workspaceRoot -PluginRoot $pluginRoot -CacheRoot $cacheRoot | Out-String)
} else {
    if ($Operation -eq 'Open') {
        $request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
        $identity = [ordered]@{
            reviewerAgent = 'Codex'
            reviewerModel = 'gpt-6.1-sol'
            reviewerEffort = 'max'
            reviewerSessionId = ('Codex-{0}-hv-nativerestore-r7' -f (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'))
            reviewerRequestId = ('req-{0}-hv-nativerestore-r7' -f (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'))
            parentSessionId = $request.parentSessionId
            parentRequestId = $request.parentRequestId
            timestampLocal = (Get-Date).ToString('s')
        }
        $identity | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $identityPath -Encoding utf8
        $payload = [ordered]@{ agent = $identity.reviewerAgent; sessionId = $identity.reviewerSessionId; title = 'Hostile review of Phase D restoration plan revision 7'; model = $identity.reviewerModel }
        $Method = 'workflow.sessionlog.openSession'
    } elseif ($Operation -eq 'Begin') {
        $identity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
        $request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
        $payload = [ordered]@{
            requestId = $identity.reviewerRequestId
            queryTitle = 'Hostile review of Phase D restoration plan revision 7'
            queryText = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'prompt.md'))
            planFile = 'docs/plans/PLAN-NATIVERESTORE-001.md'
            contextList = @('parentSessionId=' + $request.parentSessionId; 'parentRequestId=' + $request.parentRequestId; (Join-Path $PSScriptRoot 'request.jsonl'); (Join-Path $PSScriptRoot 'response.jsonl'))
            tags = @('hostile-review','PLAN-NATIVERESTORE-001','revision-7','class-1','gpt-6.1-sol','effort-max',('parent-session:' + $request.parentSessionId),('parent-request:' + $request.parentRequestId))
        }
        $Method = 'workflow.sessionlog.beginTurn'
    } else {
        if (-not $PayloadPath) { throw 'Invoke requires a serialized object payload file.' }
        $payload = Get-Content -LiteralPath $PayloadPath -Raw | ConvertFrom-Json -AsHashtable
    }
    $outputText = (& $wrapper -Command Invoke -Method $Method -ParamsObject $payload -WorkspacePath $workspaceRoot -PluginRoot $pluginRoot -CacheRoot $cacheRoot | Out-String)
}
if ($outputText -match '(?im)(X-Api-Key\s*:\s*\S+|apiKey\s*:\s*[A-Za-z0-9_-]{20,}|Bearer\s+[A-Za-z0-9._-]+)') { throw 'Output refused because it may contain a credential.' }
if ($ReceiptName) { [System.IO.File]::WriteAllText((Join-Path $PSScriptRoot $ReceiptName), $outputText) }
Write-Output $outputText
