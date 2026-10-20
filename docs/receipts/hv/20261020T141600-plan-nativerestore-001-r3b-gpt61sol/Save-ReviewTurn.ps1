[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Write','Verify','Complete','Prove')][string]$Operation,[string]$Label='verdict')
$ErrorActionPreference = 'Stop'
$parseTokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning, Error -WhatIf:$false)
if ($analysis.Count -gt 0) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess($Operation, 'Persist or verify the complete verdict through the supported plugin')) { return }
. '/home/sharpninja/github/mcpserver-codex-plugin/lib/yaml-object-mutation.ps1'
Import-McpYamlSerializer
$identity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'reviewer-identity.json') -Raw | ConvertFrom-Json
$manifestPath = Join-Path $PSScriptRoot 'verdict-chunks.manifest.json'
function Get-ReviewHash {
    param([string]$Text)
    [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($Text)))
}
function Invoke-ReviewMethod {
    param([string]$Method,[object]$Payload,[string]$ReceiptLabel,[switch]$RequirePrimary)
    $paramsPath = Join-Path $PSScriptRoot ($ReceiptLabel + '.params.json')
    $Payload | ConvertTo-Json -Depth 60 | Set-Content -LiteralPath $paramsPath -Encoding utf8
    & (Join-Path $PSScriptRoot 'Invoke-ReviewPlugin.ps1') -Operation Invoke -Method $Method -PayloadPath $paramsPath -Label $ReceiptLabel -Quiet
    $raw = Get-Content -LiteralPath (Join-Path $PSScriptRoot ($ReceiptLabel + '.txt')) -Raw
    $documents = @(ConvertFrom-Yaml -Yaml $raw -AllDocuments)
    $result = @($documents | Where-Object { $_.type -eq 'result' })[-1].payload.result
    if ($RequirePrimary -and (-not $result.persisted -or $result.queued -or $result.degraded)) { throw ('Primary write was not confirmed: ' + $ReceiptLabel) }
    return $result
}
function Get-OwnReviewTurn {
    param([string]$ReceiptLabel)
    & (Join-Path $PSScriptRoot 'Read-ReviewTurn.ps1') -Label $ReceiptLabel | Out-Host
    $docs = Get-Content -LiteralPath (Join-Path $PSScriptRoot ($ReceiptLabel + '.parsed.json')) -Raw | ConvertFrom-Json -AsHashtable
    $sessions = @(foreach ($doc in $docs) { if ($doc.payload.result.items) { $doc.payload.result.items | Where-Object { $_.sessionId -eq $identity.reviewerSessionId } } })
    if ($sessions.Count -ne 1) { throw ('Expected exactly one own session in ' + $ReceiptLabel) }
    $turns = @($sessions[0].turns | Where-Object { $_.requestId -eq $identity.reviewerRequestId })
    if ($turns.Count -ne 1) { throw ('Expected exactly one own turn in ' + $ReceiptLabel) }
    return $turns[0]
}
switch ($Operation) {
    'Write' {
        if (Test-Path -LiteralPath $manifestPath) { throw 'An existing content generation must be verified, not replayed.' }
        $runId = [Guid]::NewGuid().ToString('N')
        $entries = [System.Collections.Generic.List[object]]::new()
        $chunkSize = 4500
        foreach ($name in @('verdict.md','verdict.json')) {
            $text = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot $name))
            $count = [int][Math]::Ceiling($text.Length / [double]$chunkSize)
            $entries.Add([pscustomobject][ordered]@{name=$name;sha256=(Get-ReviewHash $text);characters=$text.Length;chunkCount=$count})
        }
        $manifest = [ordered]@{timestampLocal=(Get-Date -Format s);runId=$runId;sessionId=$identity.reviewerSessionId;requestId=$identity.reviewerRequestId;chunkSize=$chunkSize;files=@($entries.ToArray());confirmedChunks=@()}
        $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        $written = 0
        $total = [int](($entries | Measure-Object -Property chunkCount -Sum).Sum)
        foreach ($entry in $entries) {
            $text = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot $entry.name))
            for ($i=0; $i -lt $entry.chunkCount; $i++) {
                $offset = $i * $chunkSize
                $chunk = $text.Substring($offset,[Math]::Min($chunkSize,$text.Length-$offset))
                $header = 'HV_FULL_CONTENT|' + $runId + '|' + $entry.name + '|' + ($i+1) + '|' + $entry.chunkCount + "`n"
                $receiptLabel = 'content-' + $entry.name.Replace('.','-') + '-' + ($i+1).ToString('D3')
                $payload = [ordered]@{requestId=$identity.reviewerRequestId;dialogItems=@([ordered]@{timestamp=(Get-Date -Format s);role='model';category='review_verdict';content=($header+$chunk)})}
                $null = Invoke-ReviewMethod -Method 'workflow.sessionlog.appendDialog' -Payload $payload -ReceiptLabel $receiptLabel -RequirePrimary
                $manifest.confirmedChunks += [ordered]@{name=$entry.name;index=($i+1);sha256=(Get-ReviewHash $chunk);receipt=($receiptLabel+'.txt');timestampLocal=(Get-Date -Format s)}
                $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8
                $written++
                [ordered]@{timestampLocal=(Get-Date -Format s);completionPercent=[int](78+[Math]::Floor(10*$written/[double]$total));etaSeconds=[int](120+5*($total-$written));persistedChunks=$written;totalChunks=$total} | ConvertTo-Json -Compress | Write-Output
            }
        }
    }
    'Complete' {
        $verification = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict-readback.json') -Raw | ConvertFrom-Json
        if (-not $verification.allExact) { throw 'Complete content has not been verified exactly.' }
        $prose = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'verdict.md'))
        $json = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'verdict.json'))
        $response = $prose + "`nVERDICT_JSON`n" + $json
        [System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verdict.bundle.txt'),$response,[System.Text.UTF8Encoding]::new($false))
        $previousTurn = Get-OwnReviewTurn -ReceiptLabel 'verdict-metadata-readback'
        $pins = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $context = @(@($previousTurn.contextList) + @($pins.path) + @((Join-Path $PSScriptRoot 'verdict.md'),(Join-Path $PSScriptRoot 'verdict.json'),(Join-Path $PSScriptRoot 'final-integrity.json'),(Join-Path $PSScriptRoot 'verdict.persistence.json')) | Sort-Object -Unique)
        $tags = @(@($previousTurn.tags) + @('FR-VICE-001','TR-VICE-ORACLE-001','TEST-VICE-001','TR-VSFLOCKSTEP-RESUME-001','TR-CYCLE-001','PLAN-VICEORACLE-001','DISAGREE','gpt-6.1-sol','max') | Sort-Object -Unique)
        $update = [ordered]@{requestId=$identity.reviewerRequestId;queryTitle='Hostile review of native restoration revision 3';interpretation='Class 1 independent pre-execution plan review, all four mandatory surfaces; no implementation authorization. Five FAIL and one UNKNOWN prevent execution. Exact reviewer identity is gpt-6.1-sol-max@Codex; the wrapper model header is generic codex.';response=$response;contextList=$context;tags=$tags;planFile='docs/plans/PLAN-NATIVERESTORE-001.md';todoId='PLAN-VICEORACLE-001'}
        $null = Invoke-ReviewMethod -Method 'workflow.sessionlog.updateTurn' -Payload $update -ReceiptLabel 'verdict-context-update' -RequirePrimary
        $actionBase = [int][DateTimeOffset]::Now.ToUnixTimeSeconds()
        $payload = [ordered]@{
            requestId=$identity.reviewerRequestId;queryTitle='Hostile review of native restoration revision 3';response=$response
            actions=@(
                [ordered]@{order=$actionBase;type='review_verdict';status='completed';filePath=(Join-Path $PSScriptRoot 'verdict.json');description='Complete DISAGREE verdict: accuracy 94, completeness 89; 25 PASS, 5 FAIL, 1 UNKNOWN, 4 ADD-AC. Every prior ID disposition and complete prose/JSON persisted and exactly read back through the supported client route. Implementation remains blocked.'},
                [ordered]@{order=($actionBase+1);type='file_update';status='completed';filePath=(Join-Path $PSScriptRoot 'verdict.md');description='Complete prose verdict, every finding and both scores retained and exactly reconstructed from primary dialog chunks.'},
                [ordered]@{order=($actionBase+2);type='validation';status='completed';filePath=(Join-Path $PSScriptRoot 'final-integrity.json');description='All 33 pins unchanged, native 163-path inventory unchanged, 18 materialized source hashes pinned/unchanged, HEAD unchanged, git diff --check exit 0. No native/test build or test execution.'}
            )
        }
        $null = Invoke-ReviewMethod -Method 'workflow.sessionlog.completeTurn' -Payload $payload -ReceiptLabel 'verdict-complete' -RequirePrimary
        [ordered]@{timestampLocal=(Get-Date -Format s);sessionId=$identity.reviewerSessionId;requestId=$identity.reviewerRequestId;completionPrimaryReceipt='verdict-complete.txt';bundleSha256=(Get-ReviewHash $response)} | ConvertTo-Json | Write-Output
    }
    { $_ -in @('Verify','Prove') } {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $persistencePath = Join-Path $PSScriptRoot 'verdict.persistence.json'
        if ($Operation -eq 'Prove' -and (Test-Path -LiteralPath $persistencePath)) {
            $previousProof = Get-Content -LiteralPath $persistencePath -Raw | ConvertFrom-Json
            $backupPath = Join-Path $PSScriptRoot 'verdict.persistence.initial-session-check.json'
            if ($previousProof.turnCompleted -and -not $previousProof.historyCompleted -and -not (Test-Path -LiteralPath $backupPath)) {
                Copy-Item -LiteralPath $persistencePath -Destination $backupPath
                if ((Get-FileHash -LiteralPath $persistencePath -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash) { throw 'Initial proof backup hash mismatch.' }
                $correction = [ordered]@{requestId=$identity.reviewerRequestId;dialogItems=@([ordered]@{timestamp=(Get-Date -Format s);role='model';category='observation';content='Reviewer proof-check correction: my first verifier incorrectly required the containing session status to be completed. Supported client.SessionLog.QueryAsync already proved this exact turn completed, all 16 full-content chunks exact and the complete response hash exact. The containing session remains in_progress, a distinct lifecycle state. The failed local predicate and original proof are retained as verdict.persistence.initial-session-check.json. No primary persistence failure, trust failure, replay, cancellation or session repair occurred. The corrected proof uses the actual completed turn in the supported history query and retains the active session status separately.'})}
                $null = Invoke-ReviewMethod -Method 'workflow.sessionlog.appendDialog' -Payload $correction -ReceiptLabel 'verdict-proof-check-correction' -RequirePrimary
            }
        }
        $turn = Get-OwnReviewTurn -ReceiptLabel ($Label+'-full-readback')
        $comparisons = [System.Collections.Generic.List[object]]::new()
        foreach ($entry in $manifest.files) {
            $parts = [System.Collections.Generic.List[object]]::new()
            foreach ($dialog in $turn.processingDialog) {
                $content = [string]$dialog.content
                if ($content -match '^HV_FULL_CONTENT\|([^|]+)\|([^|]+)\|(\d+)\|(\d+)\r?\n') {
                    $generation = $Matches[1];$fileName=$Matches[2];$index=[int]$Matches[3];$count=[int]$Matches[4];$prefixLength=$Matches[0].Length
                    if ($generation -eq $manifest.runId -and $fileName -eq $entry.name) { $parts.Add([pscustomobject][ordered]@{index=$index;count=$count;text=$content.Substring($prefixLength)}) }
                }
            }
            if ($parts.Count -ne $entry.chunkCount -or @($parts | Group-Object -Property index | Where-Object { $_.Count -ne 1 }).Count -ne 0) { throw ('Missing or duplicated content chunks: ' + $entry.name) }
            $orderedParts = @($parts | Sort-Object -Property index)
            for ($i=0; $i -lt $orderedParts.Count; $i++) { if ($orderedParts[$i].index -ne ($i+1) -or $orderedParts[$i].count -ne $entry.chunkCount) { throw 'Invalid chunk ordering metadata.' } }
            $reassembled = ($orderedParts.text -join '')
            $sourceText = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot $entry.name))
            $sourceHash = Get-ReviewHash $sourceText
            $readHash = Get-ReviewHash $reassembled
            $normalizedSource = $sourceText.Replace("`r`n","`n").Replace("`r","`n")
            $normalizedRead = $reassembled.Replace("`r`n","`n").Replace("`r","`n")
            $comparison = [ordered]@{name=$entry.name;expectedChunks=$entry.chunkCount;readChunks=$parts.Count;sourceSha256=$sourceHash;readbackSha256=$readHash;exactMatch=($sourceHash -eq $readHash);normalizedMatch=((Get-ReviewHash $normalizedSource) -eq (Get-ReviewHash $normalizedRead));normalization=($(if ($sourceHash -eq $readHash) { 'none; exact UTF-8 bytes including LF' } else { 'CRLF/CR compared after normalization to LF' }));sourceCharacters=$sourceText.Length;readbackCharacters=$reassembled.Length}
            $comparisons.Add([pscustomobject]$comparison)
            [System.IO.File]::WriteAllText((Join-Path $PSScriptRoot ($entry.name+'.readback')),$reassembled,[System.Text.UTF8Encoding]::new($false))
        }
        $allExact = @($comparisons | Where-Object { -not $_.exactMatch }).Count -eq 0
        $proof = [ordered]@{timestampLocal=(Get-Date -Format s);reviewerSessionId=$identity.reviewerSessionId;reviewerRequestId=$identity.reviewerRequestId;mode='primary';persisted=$true;queued=$false;allExact=$allExact;contents=@($comparisons.ToArray());turnStatus=$turn.status;fullReadbackReceipt=($Label+'-full-readback.txt')}
        if ($Operation -eq 'Prove') {
            $history = Invoke-ReviewMethod -Method 'workflow.sessionlog.queryHistory' -Payload ([ordered]@{agent='Codex';text='PLAN-NATIVERESTORE-001 revision 3';limit=10;offset=0}) -ReceiptLabel 'verdict-completed-history'
            $ownHistory = @($history | Where-Object { $_.sessionId -eq $identity.reviewerSessionId })
            $bundle = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'verdict.bundle.txt'))
            $proof['responseSourceSha256'] = Get-ReviewHash $bundle
            $proof['responseReadbackSha256'] = Get-ReviewHash ([string]$turn.response)
            $proof['responseExactMatch'] = $proof.responseSourceSha256 -eq $proof.responseReadbackSha256
            $proof['historyReceipt'] = $Label+'-full-readback.txt'
            $proof['historyMethod'] = 'client.SessionLog.QueryAsync (complete sessions and turns, including exact own requestId)'
            $proof['sessionHistoryReceipt'] = 'verdict-completed-history.txt'
            $proof['historySessionCount'] = $ownHistory.Count
            $proof['sessionStatus'] = if ($ownHistory.Count -eq 1) { $ownHistory[0].status } else { '' }
            $proof['historyTurnStatus'] = $turn.status
            $proof['turnCompleted'] = $turn.status -eq 'completed'
            $proof['historyCompleted'] = $ownHistory.Count -eq 1 -and $turn.status -eq 'completed'
            $proof['lifecycleDisclosure'] = 'The review turn is completed; the containing session remains in_progress. These are separate states. The first local proof predicate incorrectly required session completion; the initial proof and the logged correction are retained.'
            $proof['proofCorrectionReceipt'] = 'verdict-proof-check-correction.txt'
            $proof['completionReceipt'] = 'verdict-complete.txt'
            $proof['preexistingQueuedRecords'] = 'Two historical records reported by Status; not replayed or canceled; this verdict is primary, not queued.'
            $proof['normalizationDisclosure'] = 'Exact UTF-8/LF content and complete response hashes compared; per-file differences, if any, appear in contents.'
            $proof['sourceType'] = 'Codex'
            $proof['reviewerModel'] = 'gpt-6.1-sol'
            $proof['reviewerEffort'] = 'max'
            $proof['wrapperModelHeader'] = 'codex (wrapper default; exact runtime model/effort recorded in launch receipts and verdict content)'
            $proof['requestPath'] = Join-Path $PSScriptRoot 'request.jsonl'
            $proof['responsePath'] = Join-Path $PSScriptRoot 'response.jsonl'
            $proof | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.persistence.json') -Encoding utf8
            if (-not $proof.turnCompleted -or -not $proof.historyCompleted -or -not $proof.responseExactMatch) { throw 'Completed-state or full-response proof failed.' }
        } else {
            $proof | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict-readback.json') -Encoding utf8
        }
        $proof | ConvertTo-Json -Depth 30 | Write-Output
        if (-not $allExact) { throw 'Complete content did not survive exact readback.' }
    }
}
