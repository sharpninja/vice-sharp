[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][ValidateSet('Stage','Send','Verify','Complete','Finish')][string]$Operation)
$ErrorActionPreference = 'Stop'
$identity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'review-identity.json') -Raw | ConvertFrom-Json
$manifestPath = Join-Path $PSScriptRoot 'verdict-chunks.manifest.json'
function Get-ReviewTextHash {
    param([string]$Text)
    [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($Text)))
}
function Invoke-ReviewPersistedCall {
    param([string]$Method,[string]$PayloadPath,[string]$ReceiptName)
    $outputText = & (Join-Path $PSScriptRoot 'review-mcp.ps1') -Operation Invoke -Method $Method -PayloadPath $PayloadPath -ReceiptName $ReceiptName
    $result = ($outputText | Out-String) | ConvertFrom-Yaml
    $typed = $result.payload.result
    if (-not $typed.persisted -or $typed.queued -or $typed.degraded) { throw ('Primary persistence not confirmed: ' + $ReceiptName) }
}
function Test-ReviewBodyReadback {
    param([string]$Label,[switch]$RequireCompleted)
    $null = & (Join-Path $PSScriptRoot 'review-history.ps1') -Label $Label
    $history = Get-Content -LiteralPath (Join-Path $PSScriptRoot ('history-' + $Label + '.yaml')) -Raw | ConvertFrom-Yaml
    $sessions = @($history.payload.result.items | Where-Object { $_.sessionId -ceq $identity.reviewerSessionId })
    $turns = @($sessions[0].turns | Where-Object { $_.requestId -ceq $identity.reviewerRequestId })
    if ($sessions.Count -ne 1 -or $turns.Count -ne 1) { throw 'History identity mismatch' }
    $turn = $turns[0]
    if ($RequireCompleted -and $turn.status -cne 'completed') { throw 'Own turn is not completed' }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $chunks = foreach ($dialog in $turn.processingDialog) {
        $contentText = [string]$dialog.content
        if ($contentText.StartsWith('{') -and $contentText.Contains('hv-full-content-r7')) {
            $chunk = $contentText | ConvertFrom-Json
            if ($chunk.kind -ceq 'hv-full-content-r7') { $chunk }
        }
    }
    $proofs = foreach ($artifact in $manifest.artifacts) {
        $rows = @($chunks | Where-Object artifact -CEQ $artifact.name | Sort-Object sequence)
        if ($rows.Count -ne $artifact.chunkCount) { throw ('Missing or duplicate artifact chunks: ' + $artifact.name) }
        for ($i = 0; $i -lt $rows.Count; $i++) {
            if ($rows[$i].sequence -ne ($i + 1) -or $rows[$i].total -ne $artifact.chunkCount -or $rows[$i].artifactSha256 -cne $artifact.sha256) { throw 'Chunk order/identity mismatch' }
        }
        $assembled = [string]::Concat([string[]]@($rows.content))
        $local = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot $artifact.name))
        $rawHash = Get-ReviewTextHash $assembled
        $localHash = Get-ReviewTextHash $local
        $normalizedHash = Get-ReviewTextHash ($assembled.Replace("`r`n","`n").Replace("`r","`n"))
        $localNormalizedHash = Get-ReviewTextHash ($local.Replace("`r`n","`n").Replace("`r","`n"))
        if ($localHash -cne $artifact.sha256 -or $normalizedHash -cne $localNormalizedHash) { throw ('Full-content readback mismatch: ' + $artifact.name) }
        [System.IO.File]::WriteAllText((Join-Path $PSScriptRoot ('readback-' + $Label + '-' + $artifact.name)), $assembled, [System.Text.UTF8Encoding]::new($false))
        [pscustomobject]@{ artifact = $artifact.name; expectedSha256 = $artifact.sha256; rawReadbackSha256 = $rawHash; rawExactMatch = ($rawHash -ceq $localHash); normalizedLfSha256 = $normalizedHash; normalizedLfExactMatch = ($normalizedHash -ceq $localNormalizedHash); normalization = $(if ($rawHash -ceq $localHash) { 'none; raw UTF-8 bytes match exactly' } else { 'CRLF/CR converted to LF for comparison; raw hashes retained' }); chunks = $rows.Count; sequences = @($rows.sequence); localBytes = (Get-Item -LiteralPath (Join-Path $PSScriptRoot $artifact.name)).Length; readbackBytes = [System.Text.Encoding]::UTF8.GetByteCount($assembled) }
    }
    $proof = [ordered]@{ timestampLocal = (Get-Date).ToString('s'); sessionId = $identity.reviewerSessionId; requestId = $identity.reviewerRequestId; sourceType = $sessions[0].sourceType; status = $turn.status; title = $turn.queryTitle; historyReceipt = ('history-' + $Label + '.yaml'); artifacts = @($proofs); fullFindingCount = 54; passCount = 50; failCount = 1; unknownCount = 0; addAcCount = 3; accuracyPercent = 97; completenessPercent = 98; primaryPersisted = $true; queued = $false; fullContentVerified = $true; processingDialogCount = @($turn.processingDialog).Count; actionCount = @($turn.actions).Count }
    $proof | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('readback-' + $Label + '.proof.json')) -Encoding utf8
    return $proof
}
if (-not $PSCmdlet.ShouldProcess('Own reviewer turn and this receipt directory', ('Execute complete-content persistence stage ' + $Operation))) { return }
Import-Module powershell-yaml -ErrorAction Stop
switch ($Operation) {
    'Stage' {
        $artifacts = foreach ($name in @('verdict.md','verdict.json')) {
            $path = Join-Path $PSScriptRoot $name
            $body = [System.IO.File]::ReadAllText($path)
            $hash = (Get-FileHash -LiteralPath $path).Hash
            $total = [int][Math]::Ceiling($body.Length / 4000.0)
            for ($index = 0; $index -lt $total; $index++) {
                $part = $body.Substring($index * 4000, [Math]::Min(4000,$body.Length - $index * 4000))
                $chunk = [ordered]@{ kind = 'hv-full-content-r7'; artifact = $name; sequence = ($index + 1); total = $total; artifactSha256 = $hash; content = $part }
                $payload = [ordered]@{ dialogItems = @([ordered]@{ timestamp = (Get-Date).ToString('o'); role = 'model'; category = 'observation'; content = ($chunk | ConvertTo-Json -Depth 8 -Compress) }) }
                $payload | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('chunk-' + $name + '-' + ($index + 1).ToString('D3') + '.params.json')) -Encoding utf8
            }
            [pscustomobject]@{ name = $name; sha256 = $hash; bytes = (Get-Item -LiteralPath $path).Length; chars = $body.Length; chunkCount = $total }
        }
        [ordered]@{ timestampLocal = (Get-Date).ToString('s'); sessionId = $identity.reviewerSessionId; requestId = $identity.reviewerRequestId; chunkSizeChars = 4000; artifacts = @($artifacts) } | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        $artifacts | ConvertTo-Json -Depth 8
    }
    'Send' {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        foreach ($artifact in $manifest.artifacts) {
            if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $artifact.name)).Hash -cne $artifact.sha256) { throw 'Artifact changed after chunk staging' }
            for ($number = 1; $number -le $artifact.chunkCount; $number++) {
                $stem = 'chunk-' + $artifact.name + '-' + $number.ToString('D3')
                $receiptPath = Join-Path $PSScriptRoot ($stem + '.receipt.yaml')
                if (Test-Path -LiteralPath $receiptPath) {
                    $oldReceipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Yaml
                    if (-not $oldReceipt.payload.result.persisted) { throw 'Previous chunk attempt was not persisted; inspect before retry' }
                } else {
                    Invoke-ReviewPersistedCall -Method workflow.sessionlog.appendDialog -PayloadPath (Join-Path $PSScriptRoot ($stem + '.params.json')) -ReceiptName ($stem + '.receipt.yaml')
                }
                [pscustomobject]@{ timestampLocal = (Get-Date).ToString('s'); artifact = $artifact.name; sequence = $number; total = $artifact.chunkCount; persisted = $true; queued = $false } | ConvertTo-Json -Compress
            }
        }
    }
    'Verify' {
        [pscustomobject](Test-ReviewBodyReadback -Label precomplete) | ConvertTo-Json -Depth 14
    }
    'Complete' {
        $proof = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'readback-precomplete.proof.json') -Raw | ConvertFrom-Json
        if (-not $proof.fullContentVerified) { throw 'Complete content not verified' }
        $metadata = [ordered]@{ filesModified = @('verdict.md','verdict.json','verdict.persistence.json','review-*.ps1 and supporting read-only evidence, only inside this receipt directory'); designDecisions = @('N19 is the only FAIL: narrow global export exclusivity to the chosen public trap seam; source feasibility does not prove arbitrary-cycle access.', 'N20-N23 PASS as future contracts, not executed Phase D gates.', 'N24-N26 are ADD-AC because existing fail-closed gates prevent unsafe restoration; they do not independently cause DISAGREE.', 'R15 is an explicit AC1 semantic amendment; synthetic 1.3 and AC2 integrity semantics remain.', 'Literal R12 permits only 100000 on both hosts or a nonpassing named BLOCKED exit; no approximate acceptance.'); requirementsDiscovered = @('N24 D2/D5 successful ordinary-discovery control','N25 D1/D2/D5 absent-candidate evidence control','N26 D1 R15 propagation control'); blockers = @('Plan approval held by N19 and accuracy 97; Phase D not executed.'); validation = @('41 pins match; native 163 content/status entries match; empty native index; git diff --check exit 0.', 'Allowed comparator Release test: 0 passed, 1 failed, 0 skipped, allocated to D2.', 'Complete MD/JSON primary chunk reassembly verified before turn completion.') }
        $payload = [ordered]@{ dialogItems = @([ordered]@{ timestamp = (Get-Date).ToString('o'); role = 'model'; category = 'decision'; content = ($metadata | ConvertTo-Json -Depth 12 -Compress) }) }
        $metaPath = Join-Path $PSScriptRoot 'turn-final-metadata.params.json'
        $payload | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $metaPath -Encoding utf8
        Invoke-ReviewPersistedCall -Method workflow.sessionlog.appendDialog -PayloadPath $metaPath -ReceiptName 'turn-final-metadata.receipt.yaml'
        $update = [ordered]@{ requestId = $identity.reviewerRequestId; queryTitle = 'Hostile review of Phase D restoration plan revision 7'; interpretation = 'Independent class-1 pre-execution hostile review of revision 7, every round-6 finding, historical round-3b/revision-4 dispositions and all mandatory source/rule/requirement/what-if surfaces.'; response = 'DISAGREE; accuracy 97; completeness 98; PASS 50; FAIL 1 (N19); UNKNOWN 0; ADD-AC 3 (N24-N26). Complete verdict.md and verdict.json are stored as ordered hv-full-content-r7 dialog chunks and primary-readback hash verified. No Phase D step executed or released.'; contextList = @((Join-Path $PSScriptRoot 'request.jsonl'),(Join-Path $PSScriptRoot 'response.jsonl'),(Join-Path $PSScriptRoot 'verdict.md'),(Join-Path $PSScriptRoot 'verdict.json'),(Join-Path $PSScriptRoot 'verdict.persistence.json'),('parentSessionId=' + $identity.parentSessionId),('parentRequestId=' + $identity.parentRequestId),'PLAN-NATIVERESTORE-001 revision 7','TR-VSFLOCKSTEP-RESUME-001','TEST-VICE-001','TR-CYCLE-001','FR-SNP-005','TEST-SNP-002','PLAN-VICEORACLE-001') }
        $updatePath = Join-Path $PSScriptRoot 'turn-final-update.params.json'
        $update | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath $updatePath -Encoding utf8
        Invoke-ReviewPersistedCall -Method workflow.sessionlog.updateTurn -PayloadPath $updatePath -ReceiptName 'turn-final-update.receipt.yaml'
        $complete = [ordered]@{ requestId = $identity.reviewerRequestId; queryTitle = $update.queryTitle; response = $update.response }
        $completePath = Join-Path $PSScriptRoot 'turn-final-complete.params.json'
        $complete | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $completePath -Encoding utf8
        Invoke-ReviewPersistedCall -Method workflow.sessionlog.completeTurn -PayloadPath $completePath -ReceiptName 'turn-final-complete.receipt.yaml'
        Write-Output 'Primary completeTurn persisted; completed history proof is still required.'
    }
    'Finish' {
        $proof = Test-ReviewBodyReadback -Label completed -RequireCompleted
        $null = & (Join-Path $PSScriptRoot 'review-mcp.ps1') -Operation Status -ReceiptName 'plugin-final-status.yaml'
        $proof.requestPath = Join-Path $PSScriptRoot 'request.jsonl'
        $proof.responsePath = Join-Path $PSScriptRoot 'response.jsonl'
        $proof.parentSessionId = $identity.parentSessionId
        $proof.parentRequestId = $identity.parentRequestId
        $proof.completeMutationReceipt = 'turn-final-complete.receipt.yaml'
        $proof.precompleteReadbackReceipt = 'readback-precomplete.proof.json'
        $proof.failSafeDrainDisabled = $true
        $proof.historicalQueues = 'Two preexisting queued records retained; no queue replay or historical cancellation. Final supported Status is plugin-final-status.yaml.'
        $proof | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.persistence.json') -Encoding utf8
        [pscustomobject]$proof | ConvertTo-Json -Depth 16
    }
}
