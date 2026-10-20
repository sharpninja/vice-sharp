[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Prepare', 'SendBatch', 'Verify', 'Complete', 'Proof', 'StopGate')]
    [string]$Mode = 'Prepare',
    [int]$StartIndex = 1,
    [int]$BatchCount = 3
)
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess('Own supported MCP review turn and receipt-local proof', $Mode)) { return }
Import-Module powershell-yaml
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'
$workspace = '/home/sharpninja/github/vice-sharp'
$pluginRoot = '/home/sharpninja/github/mcpserver-codex-plugin'
$wrapper = Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1'
$cache = Join-Path $PSScriptRoot 'plugin-cache'
$options = @{ WorkspacePath = $workspace; CacheRoot = $cache }
$sessionId = 'Codex-20261020T222731Z-plugin-session'
$turnId = 'req-20261020T222756Z-nativerestore-r6'
$manifestPath = Join-Path $PSScriptRoot 'verdict.chunks.json'
$nl = [string][char]10
function Get-TextSha256 {
    param([string]$Value)
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Value)))
}
function ConvertTo-LfText {
    param([string]$Value)
    $Value -replace '\r\n?', ([string][char]10)
}
function Invoke-PersistencePlugin {
    param([string]$Method, [object]$Parameters, [string]$Name)
    $raw = & $wrapper -Command Invoke -Method $Method -ParamsObject $Parameters @options
    $text = ($raw | Out-String)
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot $Name), $text, [Text.UTF8Encoding]::new($false))
    $documents = @(ConvertFrom-Yaml -Yaml $text -AllDocuments)
    if ($documents.Count -lt 1 -or -not $documents[0].payload) { throw ('Invalid wrapper envelope for ' + $Method) }
    $result = $documents[0].payload.result
    if ($Method -ne 'client.SessionLog.QueryAsync') {
        if (-not $result.persisted -or $result.queued -or $result.degraded) { throw ('Primary persistence not confirmed for ' + $Method + '; inspect ' + $Name) }
        if ($result.sessionId -ne $sessionId -or $result.requestId -ne $turnId) { throw 'Wrapper wrote to an unexpected session or request' }
    }
    return $result
}
function Get-OwnHistoryTurn {
    param([string]$Name)
    $history = Invoke-PersistencePlugin -Method 'client.SessionLog.QueryAsync' -Parameters @{ agent = 'Codex'; limit = 1; offset = 0 } -Name $Name
    $sessions = @($history.items | Where-Object sessionId -EQ $sessionId)
    if ($sessions.Count -ne 1) { throw 'Own session absent from scoped history result' }
    $turns = @($sessions[0].turns | Where-Object requestId -EQ $turnId)
    if ($turns.Count -ne 1) { throw 'Own exact turn absent from scoped history result' }
    [pscustomobject]@{ session = $sessions[0]; turn = $turns[0] }
}
function Test-CompleteContent {
    param([object]$Turn)
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $proofs = foreach ($file in $manifest.files) {
        $readBack = [Text.StringBuilder]::new()
        $readBackLf = [Text.StringBuilder]::new()
        $partProofs = foreach ($chunk in @($manifest.chunks | Where-Object file -EQ $file.name | Sort-Object index)) {
            $chunkMatches = @($Turn.processingDialog | Where-Object { $_.content -and ((ConvertTo-LfText -Value $_.content).StartsWith($chunk.header + $nl)) })
            if ($chunkMatches.Count -ne 1) { throw ('Missing or duplicate primary chunk ' + $chunk.header) }
            $rawContent = [string]$chunkMatches[0].content
            $lfContent = ConvertTo-LfText -Value $rawContent
            $prefix = $chunk.header + $nl
            $suffix = $nl + 'END-HV-R6'
            if (-not $lfContent.EndsWith($suffix)) { throw ('Missing end marker in ' + $chunk.header) }
            $lfPart = $lfContent.Substring($prefix.Length, $lfContent.Length - $prefix.Length - $suffix.Length)
            $rawStart = $rawContent.IndexOf([char]10) + 1
            $rawEnd = $rawContent.LastIndexOf('END-HV-R6', [StringComparison]::Ordinal)
            $rawBody = $rawContent.Substring($rawStart, $rawEnd - $rawStart)
            if ($rawBody.EndsWith(([char]13).ToString() + [char]10)) { $rawPart = $rawBody.Substring(0, $rawBody.Length - 2) }
            elseif ($rawBody.EndsWith($nl)) { $rawPart = $rawBody.Substring(0, $rawBody.Length - 1) }
            else { throw ('Invalid raw end marker separator in ' + $chunk.header) }
            [void]$readBack.Append($rawPart)
            [void]$readBackLf.Append($lfPart)
            $actualLfHash = Get-TextSha256 -Value $lfPart
            if ($actualLfHash -ne $chunk.lfSha256) { throw ('Chunk content mismatch for ' + $chunk.header) }
            [ordered]@{ index = $chunk.index; chars = $rawPart.Length; rawSha256 = (Get-TextSha256 -Value $rawPart); lfSha256 = $actualLfHash; normalizedMatches = $true }
        }
        $original = [IO.File]::ReadAllText((Join-Path $PSScriptRoot $file.name))
        $rawReassembled = $readBack.ToString()
        $lfReassembled = $readBackLf.ToString()
        $rawHash = Get-TextSha256 -Value $rawReassembled
        $lfHash = Get-TextSha256 -Value $lfReassembled
        $originalHash = Get-TextSha256 -Value $original
        $originalLfHash = Get-TextSha256 -Value (ConvertTo-LfText -Value $original)
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot ($file.name + '.readback')), $rawReassembled, [Text.UTF8Encoding]::new($false))
        if ($lfHash -ne $originalLfHash) { throw ('Complete content mismatch for ' + $file.name) }
        [pscustomobject][ordered]@{ name = $file.name; chunks = @($partProofs); sourceBytes = [Text.Encoding]::UTF8.GetByteCount($original); readbackBytes = [Text.Encoding]::UTF8.GetByteCount($rawReassembled); sourceSha256 = $originalHash; readbackSha256 = $rawHash; rawBytesMatch = ($originalHash -eq $rawHash); sourceLfSha256 = $originalLfHash; readbackLfSha256 = $lfHash; normalizedMatches = ($originalLfHash -eq $lfHash); normalization = $(if ($originalHash -eq $rawHash) { 'none' } else { 'CRLF/CR to LF only; normalized content identical' }) }
    }
    return @($proofs)
}
switch ($Mode) {
    'Prepare' {
        $verdict = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.json') -Raw | ConvertFrom-Json
        $files = [Collections.Generic.List[object]]::new()
        $chunks = [Collections.Generic.List[object]]::new()
        $ordinal = 1
        foreach ($name in @('verdict.md', 'verdict.json')) {
            $body = [IO.File]::ReadAllText((Join-Path $PSScriptRoot $name))
            $total = [int][Math]::Ceiling($body.Length / 6000.0)
            $files.Add([ordered]@{ name = $name; chars = $body.Length; sha256 = (Get-TextSha256 -Value $body); lfSha256 = (Get-TextSha256 -Value (ConvertTo-LfText -Value $body)); totalChunks = $total })
            for ($i = 0; $i -lt $total; $i++) {
                $part = $body.Substring($i * 6000, [Math]::Min(6000, $body.Length - $i * 6000))
                $header = 'BEGIN-HV-R6|' + $name + '|' + ($i + 1).ToString('D4') + '|' + $total.ToString('D4')
                $chunks.Add([ordered]@{ ordinal = $ordinal; file = $name; index = ($i + 1); total = $total; chars = $part.Length; sha256 = (Get-TextSha256 -Value $part); lfSha256 = (Get-TextSha256 -Value (ConvertTo-LfText -Value $part)); header = $header; content = ($header + $nl + $part + $nl + 'END-HV-R6') })
                $ordinal++
            }
        }
        [ordered]@{ timestampLocal = (Get-Date -Format s); sessionId = $sessionId; requestId = $turnId; files = @($files.ToArray()); chunks = @($chunks.ToArray()) } | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM
        $identity = [ordered]@{ timestampLocal = (Get-Date -Format s); sessionId = $sessionId; requestId = $turnId; model = $verdict.reviewerModel; effort = $verdict.reviewerEffort; sourceType = 'Codex'; note = 'Actual supported-plugin returned identity; prior requested custom identity retained in response stream.' }
        $identity | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'persistence-identity.json')
        $actions = @(
            @{ order = 600001; type = 'create'; status = 'completed'; filePath = (Join-Path $PSScriptRoot 'verdict.md'); description = 'Complete independent round 6 prose: all 47 prior IDs plus four new findings, evidence, corrections, requirements and structured what-if pass. DISAGREE 97 accuracy / 97 completeness; PASS 45 FAIL 3 UNKNOWN 0 ADD-AC 3. Fails R02,N19,N20.' },
            @{ order = 600002; type = 'create'; status = 'completed'; filePath = (Join-Path $PSScriptRoot 'verdict.json'); description = 'Complete native-object JSON verdict with all 51 findings, both scores, exact reviewer and parent IDs and revision-4 dispositions for all 35 round-3b IDs.' },
            @{ order = 600003; type = 'validation'; status = 'completed'; filePath = (Join-Path $PSScriptRoot 'review-integrity-final.json'); description = '41/41 final pins match, checkout/native status unchanged, 163 changed/untracked native file hashes unchanged against measured midpoint inventory, outside-receipt tracked status clean, git diff --check exit 0. No full/native build or native execution; authorized comparator baseline failed 1 passed 0 skipped 0.' },
            @{ order = 600004; type = 'validation'; status = 'completed'; filePath = (Join-Path $PSScriptRoot 'script-validation.json'); description = 'Receipt-only PowerShell helpers parser/analyzer checked with zero findings and WhatIf dry runs; complete fixture payload inspection retained separately. No implementation, file moves, deletion, commit or push.' }
        )
        $null = Invoke-PersistencePlugin -Method 'workflow.sessionlog.appendActions' -Parameters @{ actions = $actions } -Name 'persistence-actions.txt'
        $context = @($verdict.requestPath, $verdict.responsePath, (Join-Path $PSScriptRoot 'verdict.md'), (Join-Path $PSScriptRoot 'verdict.json'), (Join-Path $PSScriptRoot 'verdict.persistence.json'), (Join-Path $PSScriptRoot 'review-integrity-final.json'), (Join-Path $PSScriptRoot 'input-pins.json'), (Join-Path $PSScriptRoot 'script-validation.json'), (Join-Path $PSScriptRoot 'fixture-inspection.json'), (Join-Path $PSScriptRoot 'native-final.json'))
        $pins = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $context += @($pins.path)
        $modified = @(Get-ChildItem -LiteralPath $PSScriptRoot -File | Where-Object { $_.Name -notin @('request.jsonl', 'input-pins.json', 'SELF-REVIEW.md', 'prompt.md', 'Launch-Review.ps1') } | Select-Object -ExpandProperty FullName)
        $null = Invoke-PersistencePlugin -Method 'workflow.sessionlog.updateTurn' -Parameters @{ interpretation = 'Class 1 independent plan review, revision 6, all prior IDs plus whole-plan/requirements/workspace attacks. Literal R12 preserved; no execution authorized. Full prose/JSON persisted in ordered chunks and verified before completion.'; tags = @('hostile-validation', 'PLAN-NATIVERESTORE-001', 'revision-6', 'DISAGREE', 'accuracy:97', 'completeness:97', 'gpt-6.1-sol', 'max', ('parentSessionId:' + $verdict.parentSessionId), ('parentRequestId:' + $verdict.parentRequestId)); contextList = $context; filesModified = $modified } -Name 'persistence-update.txt'
        [ordered]@{ timestampLocal = (Get-Date -Format s); preparedChunks = $chunks.Count; files = @($files.ToArray()) } | ConvertTo-Json -Depth 8
    }
    'SendBatch' {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $batch = @($manifest.chunks | Where-Object { $_.ordinal -ge $StartIndex -and $_.ordinal -lt ($StartIndex + $BatchCount) } | Sort-Object ordinal)
        if (-not $batch.Count) { throw 'Empty persistence batch' }
        $items = @($batch | ForEach-Object { @{ order = [int]$_.ordinal; timestamp = (Get-Date -Format o); role = 'model'; category = 'observation'; content = $_.content } })
        $name = 'persistence-batch-' + $StartIndex.ToString('D4') + '.txt'
        $result = Invoke-PersistencePlugin -Method 'workflow.sessionlog.appendDialog' -Parameters @{ dialogItems = $items } -Name $name
        [ordered]@{ timestampLocal = (Get-Date -Format s); firstChunk = $StartIndex; sentChunks = $batch.Count; lastChunk = $batch[-1].ordinal; persisted = $result.persisted; queued = $result.queued; degraded = $result.degraded } | ConvertTo-Json -Compress
    }
    'Verify' {
        $own = Get-OwnHistoryTurn -Name 'history-full-readback-before-completion.txt'
        $proof = @(Test-CompleteContent -Turn $own.turn)
        [ordered]@{ timestampLocal = (Get-Date -Format s); sessionId = $sessionId; requestId = $turnId; status = $own.turn.status; files = $proof } | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.content-proof.json')
        [ordered]@{ timestampLocal = (Get-Date -Format s); status = $own.turn.status; files = @($proof | Select-Object name, sourceBytes, readbackBytes, rawBytesMatch, normalizedMatches, normalization) } | ConvertTo-Json -Depth 8
    }
    'Complete' {
        $proof = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.content-proof.json') -Raw | ConvertFrom-Json
        if (@($proof.files | Where-Object { -not $_.normalizedMatches }).Count -or $proof.files.Count -ne 2) { throw 'Complete-content proof missing or invalid' }
        foreach ($fileProof in $proof.files) {
            if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $fileProof.name)).Hash -ne $fileProof.sourceSha256) { throw 'Verdict changed after readback proof' }
        }
        $summary = 'Independent round 6 review delivered: DISAGREE, accuracy 97%, completeness 97%; PASS 45, FAIL 3 (R02,N19,N20), UNKNOWN 0, ADD-AC 3. Complete prose and JSON are retained in ordered BEGIN-HV-R6 dialog chunks, read back through supported QueryAsync and reassembled with SHA256 equality (see verdict.content-proof.json). All 47 round-5 IDs are dispositioned. No Phase D implementation or native change occurred. Execution remains held.'
        $null = Invoke-PersistencePlugin -Method 'workflow.sessionlog.appendDialog' -Parameters @{ dialogItems = @(@{ timestamp = (Get-Date -Format o); role = 'model'; category = 'observation'; content = ('Complete-content readback verified before completion. ' + $summary) }) } -Name 'persistence-readback-observation.txt'
        $null = Invoke-PersistencePlugin -Method 'workflow.sessionlog.appendActions' -Parameters @{ actions = @(@{ order = 600005; type = 'validation'; status = 'completed'; filePath = (Join-Path $PSScriptRoot 'verdict.content-proof.json'); description = 'Complete prose and complete JSON reassembled from primary session-log processingDialog and SHA256 compared; per-file raw and LF-normalized results retained. Historical queues untouched. This completes review work only, not the rejected plan.' }) } -Name 'persistence-readback-action.txt'
        $result = Invoke-PersistencePlugin -Method 'workflow.sessionlog.completeTurn' -Parameters @{ response = $summary } -Name 'persistence-complete.txt'
        [ordered]@{ timestampLocal = (Get-Date -Format s); persisted = $result.persisted; queued = $result.queued; sessionId = $result.sessionId; requestId = $result.requestId } | ConvertTo-Json -Compress
    }
    'StopGate' {
        $env:MCP_CACHE_DIR_OVERRIDE = $cache
        $env:MCP_WORKSPACE_PATH = $workspace
        $raw = & (Join-Path $pluginRoot 'lib/stop-gate.ps1')
        $text = $raw | Out-String
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'persistence-stop-gate.txt'), $text, [Text.UTF8Encoding]::new($false))
        $text
    }
    'Proof' {
        $own = Get-OwnHistoryTurn -Name 'history-completed-turn.txt'
        if ($own.turn.status -ne 'completed') { throw ('Own turn is not completed: ' + $own.turn.status) }
        $content = @(Test-CompleteContent -Turn $own.turn)
        $verdict = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.json') -Raw | ConvertFrom-Json
        $proof = [ordered]@{
            timestampLocal = (Get-Date -Format s)
            persisted = $true
            queued = $false
            degraded = $false
            sourceType = $own.session.sourceType
            reviewerAgent = 'Codex'
            reviewerModel = $verdict.reviewerModel
            reviewerEffort = $verdict.reviewerEffort
            reviewerSessionId = $sessionId
            reviewerRequestId = $turnId
            parentSessionId = $verdict.parentSessionId
            parentRequestId = $verdict.parentRequestId
            agentSessionId = $own.session.agentSessionId
            agentExecutablePath = $own.session.agentExecutablePath
            agentExecutableVersion = $own.session.agentExecutableVersion
            serverModelField = $own.session.model
            exactModelEvidence = 'request.jsonl CLI launch model/effort; server header is generic codex'
            historyQuery = 'Invoke-CodexMcpPlugin.ps1 -Command Invoke -Method client.SessionLog.QueryAsync -ParamsObject @{agent=Codex;limit=1;offset=0}, through Save-ReviewPersistence.ps1'
            historyReceiptPath = (Join-Path $PSScriptRoot 'history-completed-turn.txt')
            historyReceiptSha256 = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'history-completed-turn.txt')).Hash
            turnTitle = $own.turn.queryTitle
            turnStatus = $own.turn.status
            sessionHeaderStatus = $own.session.status
            note = 'The exact review turn is completed; the session header may remain in_progress. Whole prose and JSON are in ordered readable dialog chunks, not just a digest or local failsafe.'
            content = $content
            allCompleteContentVerified = (@($content | Where-Object { -not $_.normalizedMatches }).Count -eq 0 -and $content.Count -eq 2)
            completeReceiptPath = (Join-Path $PSScriptRoot 'persistence-complete.txt')
            stopGateReceiptPath = (Join-Path $PSScriptRoot 'persistence-stop-gate.txt')
            stopGateRawResponse = ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'persistence-stop-gate.txt')).Trim())
            stopGateInterpretation = 'Empty object is the nonblocking return in supported plugin-hook.ps1 Close-PluginTurnIfNeeded line 1262; no status:passed field is fabricated.'
            finalIntegrityReceiptPath = (Join-Path $PSScriptRoot 'review-integrity-final.json')
            finalIntegrityReceiptSha256 = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'review-integrity-final.json')).Hash
            historicalQueuedRecordsAtBootstrap = 2
            historicalQueueHandling = 'Never replayed, drained or canceled; MCP_FAILSAFE_DRAIN_DISABLED=1 throughout own wrapper operations'
            requestPath = $verdict.requestPath
            responsePath = $verdict.responsePath
            responseHashNote = 'Response stream remains live until CLI exit; no misleading final-stream hash is claimed before exit'
            OverallVerdict = $verdict.OverallVerdict
            accuracyPercent = $verdict.accuracyPercent
            completenessPercent = $verdict.completenessPercent
            passCount = $verdict.passCount
            failCount = $verdict.failCount
            unknownCount = $verdict.unknownCount
            addAcCount = $verdict.addAcCount
        }
        $proof | ConvertTo-Json -Depth 25 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.persistence.json') -Encoding utf8NoBOM
        [ordered]@{ timestampLocal = $proof.timestampLocal; persisted = $true; queued = $false; sessionId = $sessionId; requestId = $turnId; turnStatus = $own.turn.status; completeContentVerified = $proof.allCompleteContentVerified; files = @($content | Select-Object name, rawBytesMatch, normalizedMatches, normalization) } | ConvertTo-Json -Depth 8
    }
}
