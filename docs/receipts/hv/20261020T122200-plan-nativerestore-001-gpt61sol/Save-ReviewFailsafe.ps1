[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Initialize', 'Checkpoint', 'Final')][string]$Stage = 'Initialize', [string]$CheckpointText = '')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess($PSScriptRoot, "Retain authorized supported-plugin failsafe: $Stage")) { return }
$pluginRoot = '/home/sharpninja/github/mcpserver-codex-plugin'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:CODEX_PLUGIN_ROOT = $pluginRoot
$env:MCP_PLUGIN_HOST = 'codex'
$env:MCP_AGENT_NAME = 'Codex'
$env:PLUGIN_AGENT_NAME = 'Codex'
$env:MCP_CACHE_DIR_OVERRIDE = Join-Path $PSScriptRoot 'plugin-cache'
$env:MCP_FAILSAFE_DIR = Join-Path $env:MCP_CACHE_DIR_OVERRIDE 'failsafe'
$env:MCPSERVER_FAILSAFE_DIR = $env:MCP_FAILSAFE_DIR
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'
$env:MCP_SESSION_SUFFIX = 'hv-plan-nativerestore-001'
$env:MCP_SESSION_ID = $null
$env:MCP_WORKSPACE_PATH = '/home/sharpninja/github/vice-sharp'
. (Join-Path $pluginRoot 'lib/repl-invoke.ps1')
$identityPath = Join-Path $PSScriptRoot 'reviewer-identity.json'
if ($Stage -eq 'Initialize') {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'mcp-triage-failure.json'))) { throw 'Failed primary triage receipt required before failsafe authorization applies.' }
    if (Test-Path -LiteralPath $identityPath) { throw 'Reviewer identity already allocated.' }
    $identity = [ordered]@{
        reviewerAgent = 'Codex'
        reviewerModel = 'gpt-6.1-sol'
        reviewerEffort = 'max'
        reviewerSessionId = New-ReplPluginSessionId
        reviewerRequestId = New-ReplBeginTurnRequestId
        allocatedAtLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
        identityDisposition = 'Allocated by supported plugin helpers for the offline queue. No primary session or turn exists.'
        primaryReadback = $false
    }
    [IO.File]::WriteAllText($identityPath, ($identity | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    $triageParams = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'triage-params.json') -Raw | ConvertFrom-Json -AsHashtable
    $triageYaml = ConvertTo-Yaml -Data $triageParams -Options WithIndentedSequences
    $triagePath = Write-ReplFailsafe -Method 'workflow.triage.report' -ParamsYaml $triageYaml -Label 'review_triage_report'
    if (-not $triagePath) { throw 'Supported triage failsafe write failed.' }
    Write-Output ('Queued triage: ' + $triagePath)
} else {
    $identity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json -AsHashtable
}
$request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
$now = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
$prose = if ($Stage -eq 'Final') { [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'verdict.md')) } else { $CheckpointText }
$json = if ($Stage -eq 'Final') { [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'verdict.json')) } else { '' }
$dialog = @()
foreach ($document in @(@{ Kind = 'Markdown'; Text = $prose }, @{ Kind = 'JSON'; Text = $json })) {
    for ($offset = 0; $offset -lt $document.Text.Length; $offset += 6000) {
        $length = [Math]::Min(6000, $document.Text.Length - $offset)
        $dialog += [ordered]@{ order = [int]($dialog.Count + 1); timestamp = $now; role = 'model'; category = 'observation'; content = $document.Text.Substring($offset, $length); documentKind = $document.Kind }
    }
}
$decisions = @(
    'This is a class-1 plan review, not authorization to execute Phase D. R1 B and D3 amend only their specified gates. No done-state change is permitted by this review.',
    'Primary bootstrap and triage failed with health Permission denied. Retain full content through the explicitly authorized supported-plugin failsafe; never label it persisted or completed on the server.'
)
foreach ($decision in $decisions) { $dialog += [ordered]@{ order = [int]($dialog.Count + 1); timestamp = $now; role = 'model'; category = 'decision'; content = $decision; documentKind = 'Decision' } }
$turnArgs = @{
    Agent = 'Codex'; SessionId = $identity.reviewerSessionId; RequestId = $identity.reviewerRequestId
    Timestamp = $now; QueryText = $request.prompt; Title = 'Hostile review of native restoration Phase D plan'
    Status = $(if ($Stage -eq 'Final') { 'completed' } else { 'in_progress' }); Model = 'gpt-6.1-sol'; ResponseText = ($prose + "`n" + $json)
    Interpretation = 'Independent hostile plan review before execution; complete full-content offline fallback only after genuine primary and triage failures.'
    ContextList = @('parentSessionId=' + $request.parentSessionId; 'parentRequestId=' + $request.parentRequestId; (Join-Path $PSScriptRoot 'request.jsonl'); $request.responsePath)
    Tags = @('hostile-review', 'class-1', 'effort-max', 'PLAN-NATIVERESTORE-001', 'queued-not-persisted')
    FilesModified = @((Join-Path $PSScriptRoot 'verdict.md'), (Join-Path $PSScriptRoot 'verdict.json'))
    ProcessingDialog = $dialog
    Actions = @(
        @{ order = 1; type = 'design_decision'; status = 'completed'; description = $decisions[0]; filePath = 'docs/plans/PLAN-NATIVERESTORE-001.md' },
        @{ order = 2; type = 'design_decision'; status = 'completed'; description = $decisions[1]; filePath = (Join-Path $PSScriptRoot 'mcp-triage-failure.json') }
    )
    PlanFile = 'docs/plans/PLAN-NATIVERESTORE-001.md'; TodoId = 'PLAN-NATIVERESTORE-001'
}
$turnDto = New-McpPluginTurnUpsertRequest @turnArgs
$sessionLog = [ordered]@{ sourceType = 'Codex'; sessionId = $identity.reviewerSessionId; model = 'gpt-6.1-sol'; title = $turnArgs.Title; started = $identity.allocatedAtLocal; lastUpdated = $now; status = $turnArgs.Status; turnCount = 1; turns = @($turnDto.Turn) }
$payload = [ordered]@{ sessionLog = $sessionLog }
$payloadYaml = ConvertTo-Yaml -Data $payload -Options WithIndentedSequences
$queuePath = Write-ReplFailsafe -Method 'client.SessionLog.SubmitAsync' -ParamsYaml $payloadYaml -Label ('review_' + $Stage.ToLowerInvariant())
if (-not $queuePath) { throw 'Supported full-content failsafe write failed.' }
$retained = Read-McpYamlObject -Path $queuePath
$readDialog = @($retained.params.sessionLog.turns[0].processingDialog)
$readProse = (@($readDialog | Where-Object { $_.documentKind -eq 'Markdown' } | ForEach-Object { $_.content }) -join '')
$readJson = (@($readDialog | Where-Object { $_.documentKind -eq 'JSON' } | ForEach-Object { $_.content }) -join '')
if ($readProse -cne $prose -or $readJson -cne $json) { throw 'Failsafe readback differs from complete source documents.' }
$receipt = [ordered]@{ stage = $Stage; queuePath = $queuePath; queueSha256 = (Get-FileHash -LiteralPath $queuePath -Algorithm SHA256).Hash; queued = $true; persisted = $false; primaryReadback = $false; localReadbackMatches = $true; crlfLfNormalizationApplied = $false; reviewerSessionId = $identity.reviewerSessionId; reviewerRequestId = $identity.reviewerRequestId; timestampLocal = $now; markdownChunkCount = @($readDialog | Where-Object { $_.documentKind -eq 'Markdown' }).Count; jsonChunkCount = @($readDialog | Where-Object { $_.documentKind -eq 'JSON' }).Count; sourceMarkdownSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($prose))); readbackMarkdownSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($readProse))); sourceJsonSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($json))); readbackJsonSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($readJson))) }
[IO.File]::WriteAllText((Join-Path $PSScriptRoot ('failsafe-' + $Stage.ToLowerInvariant() + '.json')), ($receipt | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
$receipt | ConvertTo-Json -Compress
