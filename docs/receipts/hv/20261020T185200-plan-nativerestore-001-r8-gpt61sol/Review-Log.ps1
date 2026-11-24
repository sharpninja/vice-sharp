[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidateSet('Decision','Stage','Send','Verify','Complete','Finish')][string]$Mode,
    [int]$Start = 0,
    [int]$Count = 4
)
$ErrorActionPreference = 'Stop'
if ($WhatIfPreference) { Write-Output ('WhatIf: review logging ' + $Mode); return }
if (-not $PSCmdlet.ShouldProcess('Own reviewer turn and this receipt directory', $Mode)) { return }
Import-Module powershell-yaml -ErrorAction Stop
$identity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'review-identity.json') -Raw | ConvertFrom-Json
$manifestPath = Join-Path $PSScriptRoot 'verdict-chunks.manifest.json'
function Get-TextSha256 {
    param([string]$Text)
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text)))
}
function Invoke-OwnReviewCall {
    param([string]$Method,[object]$Payload,[string]$Label,[switch]$Mutation)
    $payloadPath = Join-Path $PSScriptRoot ($Label + '.params.json')
    $Payload | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $payloadPath -Encoding utf8
    $null = & (Join-Path $PSScriptRoot 'Review-Plugin.ps1') -Mode Call -Method $Method -PayloadPath $payloadPath -Label $Label
    $raw = Get-Content -LiteralPath (Join-Path $PSScriptRoot ('plugin-' + $Label + '.txt')) -Raw
    $parsed = $raw | ConvertFrom-Yaml
    if ($Mutation -and (-not $parsed.payload.result.persisted -or $parsed.payload.result.queued -or $parsed.payload.result.degraded)) { throw ('Primary mutation not confirmed: ' + $Label) }
    return $parsed
}
function Get-OwnTurn {
    param([string]$Label)
    $response = Invoke-OwnReviewCall -Method client.SessionLog.QueryAsync -Payload ([ordered]@{agent='Codex';sessionId=$identity.reviewerSessionId;limit=1;offset=0}) -Label $Label
    $sessions = @($response.payload.result.items | Where-Object sessionId -CEQ $identity.reviewerSessionId)
    if ($sessions.Count -ne 1) { throw 'History did not return exactly the own session.' }
    $turns = @($sessions[0].turns | Where-Object requestId -CEQ $identity.reviewerRequestId)
    if ($turns.Count -ne 1) { throw 'History did not return exactly the own request.' }
    return [pscustomobject]@{session=$sessions[0];turn=$turns[0]}
}
function Test-CompleteBody {
    param([string]$Label,[switch]$RequireCompleted)
    $record = Get-OwnTurn -Label ('history-' + $Label)
    if ($RequireCompleted -and $record.turn.status -cne 'completed') { throw 'Own turn is not completed.' }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $parts = @(foreach ($dialog in $record.turn.processingDialog) {
        $body = [string]$dialog.content
        if ($body.StartsWith('{') -and $body.Contains('hv-full-content-r8')) {
            $item = $body | ConvertFrom-Json
            if ($item.kind -ceq 'hv-full-content-r8') { $item }
        }
    })
    $proofs = @(foreach ($artifact in $manifest.artifacts) {
        $rows = @($parts | Where-Object artifact -CEQ $artifact.name | Sort-Object sequence)
        if ($rows.Count -ne $artifact.chunkCount) { throw ('Missing or duplicate chunks: ' + $artifact.name) }
        for ($i=0;$i -lt $rows.Count;$i++) { if ($rows[$i].sequence -ne ($i+1) -or $rows[$i].total -ne $artifact.chunkCount -or $rows[$i].artifactSha256 -cne $artifact.sha256) { throw 'Chunk ordering or identity mismatch.' } }
        $assembled = [string]::Concat([string[]]@($rows.content))
        $local = [IO.File]::ReadAllText((Join-Path $PSScriptRoot $artifact.name))
        $rawHash = Get-TextSha256 $assembled
        $localHash = Get-TextSha256 $local
        $normalizedHash = Get-TextSha256 ($assembled.Replace("`r`n","`n").Replace("`r","`n"))
        $localNormalizedHash = Get-TextSha256 ($local.Replace("`r`n","`n").Replace("`r","`n"))
        if ($localHash -cne $artifact.sha256 -or $normalizedHash -cne $localNormalizedHash) { throw ('Complete content mismatch: ' + $artifact.name) }
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot ('readback-' + $Label + '-' + $artifact.name)),$assembled,[Text.UTF8Encoding]::new($false))
        [ordered]@{artifact=$artifact.name;expectedSha256=$artifact.sha256;rawReadbackSha256=$rawHash;rawExactMatch=($rawHash -ceq $localHash);normalizedLfSha256=$normalizedHash;normalizedLfExactMatch=($normalizedHash -ceq $localNormalizedHash);normalization=if($rawHash -ceq $localHash){'none; raw UTF-8 bytes match exactly'}else{'CRLF/CR normalized to LF for comparison; raw hashes retained'};chunks=$rows.Count;sequences=@($rows.sequence);localBytes=[Text.Encoding]::UTF8.GetByteCount($local);readbackBytes=[Text.Encoding]::UTF8.GetByteCount($assembled)}
    })
    $verdict = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.json') -Raw | ConvertFrom-Json
    $proof = [ordered]@{timestampLocal=(Get-Date -Format s);sessionId=$identity.reviewerSessionId;requestId=$identity.reviewerRequestId;sourceType=$record.session.sourceType;status=$record.turn.status;title=$record.turn.queryTitle;historyReceipt=('plugin-history-' + $Label + '.txt');artifacts=$proofs;fullFindingCount=$verdict.findings.Count;passCount=$verdict.passCount;failCount=$verdict.failCount;unknownCount=$verdict.unknownCount;addAcCount=$verdict.addAcCount;accuracyPercent=$verdict.accuracyPercent;completenessPercent=$verdict.completenessPercent;OverallVerdict=$verdict.OverallVerdict;primaryPersisted=$true;queued=$false;fullContentVerified=$true;processingDialogCount=@($record.turn.processingDialog).Count;actionCount=@($record.turn.actions).Count}
    $proof | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('readback-' + $Label + '.proof.json')) -Encoding utf8
    return $proof
}
switch ($Mode) {
    'Decision' {
        $decisions = @(
            'Judge revision 8 as a class-1 plan before execution. The request retains stale revision-4 labels; all 41 content pins match the live revision-8 subject. Preserve actual revision-4 dispositions separately from the original round-4 receipt rather than relabeling historical findings.'
            'The original N19 defect was global export timing exclusivity. R12 now uses the actual chosen trap boundary and write clock; BRK remains an internal post-fetch export, and event.c independently proves snapshot writing from a trap callback. Treat surviving section-1 universal checkpoint wording and section-7 metadata overstatement as a low-severity editorial ADD-AC, without inventing a checkpoint safety gap.'
            'Honor R12 literally: only trigger 100000 and actual write/MAINCPU clock 100000 on both hosts can accept xvic fixtures. The named nonpassing BLOCKED exit holds Phase E and all done-states while independent D cases continue with separate denominators. Do not treat that plan contract as an executed native acceptance.'
            'R15 explicitly amends real AC1 inputs to 1.2 and 1.4 in D1 through the wrapper. Keep the synthetic 1.3 matrix, AC2 integrity semantics, and managed TR-CYCLE-001 AC4/task-5 acceptance pending.'
            'Add a nonblocking D1/D5 control for every nonzero native snapshot-writer result, including positive 1 from the xvic IEEE488 failure path. Existing write-failure/publication isolation gates already fail closed; this sharpens their mocks rather than authorizing any immutable-source change.'
        )
        $dialogItems = @(for ($i=0;$i -lt $decisions.Count;$i++) { [ordered]@{order=$i+1;timestamp=(Get-Date -Format o);role='model';category='decision';content=$decisions[$i]} })
        $null = Invoke-OwnReviewCall -Method workflow.sessionlog.appendDialog -Payload ([ordered]@{requestId=$identity.reviewerRequestId;dialogItems=$dialogItems}) -Label judgment-dialog -Mutation
        $actions = @(for ($i=0;$i -lt $decisions.Count;$i++) { [ordered]@{order=$i+1;type='design_decision';status='completed';description=$decisions[$i]} })
        $actions += [ordered]@{order=6;type='validation';status='completed';filePath=(Join-Path $PSScriptRoot 'TestResults/vic20-comparator.trx');description='Allowed Release TestHarness filter FullyQualifiedName~Vic20HalfCycleComparatorTests completed: total/executed 1, passed 0, failed 1, skipped 0, exit 1; assertion at fixture line 23 proves HalfCycleState absent. Retained TRX, VSTest diagnostics and stdout/stderr. No Phase D implementation or native build ran.'}
        $actions += [ordered]@{order=7;type='create';status='completed';filePath=(Join-Path $PSScriptRoot 'finding-history.json');description='Authored review-only PowerShell helpers and evidence exclusively in this receipt directory. Recounted 43 token candidates, 5 categorized, 56 semantic candidates; inspected all 20 historical patch hunks and 2527 preserved paths; 25 materialized source blobs match pinned Git objects.'}
        $null = Invoke-OwnReviewCall -Method workflow.sessionlog.appendActions -Payload ([ordered]@{requestId=$identity.reviewerRequestId;actions=$actions}) -Label judgment-actions -Mutation
        $record = Get-OwnTurn -Label history-checkpoint
        [ordered]@{timestampLocal=(Get-Date -Format s);sessionId=$identity.reviewerSessionId;requestId=$identity.reviewerRequestId;status=$record.turn.status;dialogCount=@($record.turn.processingDialog).Count;actionCount=@($record.turn.actions).Count;primaryPersisted=$true;queued=$false} | ConvertTo-Json -Depth 6
    }
    'Stage' {
        $artifacts = @(foreach ($name in @('verdict.md','verdict.json')) {
            $path = Join-Path $PSScriptRoot $name
            $body = [IO.File]::ReadAllText($path)
            $hash = (Get-FileHash -LiteralPath $path).Hash
            $total = [int][Math]::Ceiling($body.Length / 4000.0)
            for ($index=0;$index -lt $total;$index++) {
                $chunk = [ordered]@{kind='hv-full-content-r8';artifact=$name;sequence=$index+1;total=$total;artifactSha256=$hash;content=$body.Substring($index*4000,[Math]::Min(4000,$body.Length-$index*4000))}
                $chunk | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('chunk-' + $name + '-' + ($index+1).ToString('D3') + '.json')) -Encoding utf8
            }
            [ordered]@{name=$name;sha256=$hash;bytes=(Get-Item -LiteralPath $path).Length;chars=$body.Length;chunkCount=$total}
        })
        [ordered]@{timestampLocal=(Get-Date -Format s);sessionId=$identity.reviewerSessionId;requestId=$identity.reviewerRequestId;chunkSizeChars=4000;artifacts=$artifacts} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        $artifacts | ConvertTo-Json -Depth 8
    }
    'Send' {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $ordinal = 0
        $rows = @(foreach ($artifact in $manifest.artifacts) { for ($number=1;$number -le $artifact.chunkCount;$number++) { $ordinal++; [pscustomobject]@{artifact=$artifact;number=$number;ordinal=$ordinal;stem=('chunk-' + $artifact.name + '-' + $number.ToString('D3'))} } })
        foreach ($row in ($rows | Select-Object -Skip $Start -First $Count)) {
            if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $row.artifact.name)).Hash -cne $row.artifact.sha256) { throw 'Artifact changed after staging.' }
            $receiptPath = Join-Path $PSScriptRoot ('plugin-' + $row.stem + '.txt')
            if (Test-Path -LiteralPath $receiptPath) {
                $old = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Yaml
                if (-not $old.payload.result.persisted -or $old.payload.result.queued) { throw 'Existing chunk receipt is not primary; inspect before retry.' }
            } else {
                $chunk = Get-Content -LiteralPath (Join-Path $PSScriptRoot ($row.stem + '.json')) -Raw | ConvertFrom-Json
                $payload = [ordered]@{requestId=$identity.reviewerRequestId;dialogItems=@([ordered]@{order=100+$row.ordinal;timestamp=(Get-Date -Format o);role='model';category='observation';content=($chunk | ConvertTo-Json -Depth 8 -Compress)})}
                $null = Invoke-OwnReviewCall -Method workflow.sessionlog.appendDialog -Payload $payload -Label $row.stem -Mutation
            }
            [ordered]@{timestampLocal=(Get-Date -Format s);artifact=$row.artifact.name;sequence=$row.number;total=$row.artifact.chunkCount;persisted=$true;queued=$false} | ConvertTo-Json -Compress
        }
    }
    'Verify' { Test-CompleteBody -Label precomplete | ConvertTo-Json -Depth 16 }
    'Complete' {
        $proof = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'readback-precomplete.proof.json') -Raw | ConvertFrom-Json
        if (-not $proof.fullContentVerified) { throw 'Full-content readback is not verified.' }
        $verdict = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.json') -Raw | ConvertFrom-Json
        $metadata = [ordered]@{filesModified=@('Review-*.ps1','verdict.md','verdict.json','verdict.persistence.json','Supporting evidence and plugin receipts, all inside this receipt directory');designDecisions=@('N19 PASS: the global export-timing error is removed; N27 is a nonblocking wording/metadata refinement.', 'Literal R12 and named nonpassing BLOCKED exit are preserved; no native checkpoint was executed in this review.', 'R15 is explicit AC1 semantic amendment; synthetic 1.3 and AC2 semantics remain.', 'N28 adds a positive nonzero native-write failure control to existing fail-closed gates.');requirementsDiscovered=@('N27 D1 ReviewSummary_MatchesChosenCheckpointScope','N28 D1/D5 SnapshotWriter_AnyNonzeroResultRejectsPublication');blockers=@();validation=@('41 input pins and 25 pinned source blobs checked; native before/after inventory and diff checks retained in final-integrity.json.', 'Focused comparator: 0 passed, 1 failed, 0 skipped; expected current gap allocated to D2.', 'Full verdict prose and JSON were primary persisted in ordered dialog chunks, reassembled and hash checked before completion.')}
        $null = Invoke-OwnReviewCall -Method workflow.sessionlog.appendDialog -Payload ([ordered]@{requestId=$identity.reviewerRequestId;dialogItems=@([ordered]@{order=1000;timestamp=(Get-Date -Format o);role='model';category='observation';content=($metadata | ConvertTo-Json -Depth 12 -Compress)})}) -Label final-metadata -Mutation
        $summary = '{0}; accuracy {1}; completeness {2}; PASS {3}; FAIL {4}; UNKNOWN {5}; ADD-AC {6}. Complete verdict.md and verdict.json are stored as ordered hv-full-content-r8 dialog chunks and primary-readback hash verified. No Phase D step executed or implementation release granted.' -f $verdict.OverallVerdict,$verdict.accuracyPercent,$verdict.completenessPercent,$verdict.passCount,$verdict.failCount,$verdict.unknownCount,$verdict.addAcCount
        $update = [ordered]@{requestId=$identity.reviewerRequestId;queryTitle='Hostile review of Phase D restoration plan revision 8';interpretation='Independent class-1 pre-execution review of revision 8, all 54 round-7 IDs, all 35 historical round-3b/revision-4 dispositions, live source and four mandatory attack surfaces.';response=$summary;contextList=@($verdict.requestPath,$verdict.responsePath,(Join-Path $PSScriptRoot 'verdict.md'),(Join-Path $PSScriptRoot 'verdict.json'),(Join-Path $PSScriptRoot 'verdict.persistence.json'),('parentSessionId=' + $identity.parentSessionId),('parentRequestId=' + $identity.parentRequestId),'reviewerAgent=Codex;reviewerModel=gpt-6.1-sol;reviewerEffort=max','TR-VSFLOCKSTEP-RESUME-001','TEST-VICE-001','TR-CYCLE-001','FR-SNP-005','TEST-SNP-002','PLAN-VICEORACLE-001')}
        $null = Invoke-OwnReviewCall -Method workflow.sessionlog.updateTurn -Payload $update -Label final-update -Mutation
        $null = Invoke-OwnReviewCall -Method workflow.sessionlog.appendActions -Payload ([ordered]@{requestId=$identity.reviewerRequestId;actions=@([ordered]@{order=8;type='validation';status='completed';filePath=(Join-Path $PSScriptRoot 'verdict.json');description=$summary},[ordered]@{order=9;type='create';status='completed';filePath=(Join-Path $PSScriptRoot 'verdict.md');description='Complete hostile-review prose and JSON persisted in ordered chunks, read back and reassembled; both raw and LF-normalized SHA256 comparisons retained. No commit or push.'})}) -Label final-actions -Mutation
        $null = Invoke-OwnReviewCall -Method workflow.sessionlog.completeTurn -Payload ([ordered]@{requestId=$identity.reviewerRequestId;queryTitle=$update.queryTitle;response=$summary}) -Label final-complete -Mutation
        Write-Output 'Primary completeTurn persisted; completed-state history proof follows.'
    }
    'Finish' {
        $proof = Test-CompleteBody -Label completed -RequireCompleted
        $null = & (Join-Path $PSScriptRoot 'Review-Plugin.ps1') -Mode Status -Label final-status
        $proof.requestPath = Join-Path $PSScriptRoot 'request.jsonl'
        $proof.responsePath = Join-Path $PSScriptRoot 'response.jsonl'
        $proof.parentSessionId = $identity.parentSessionId
        $proof.parentRequestId = $identity.parentRequestId
        $proof.completeMutationReceipt = 'plugin-final-complete.txt'
        $proof.precompleteReadbackReceipt = 'readback-precomplete.proof.json'
        $proof.failSafeDrainDisabled = $true
        $proof.historicalQueues = 'Two preexisting default-root queued records left untouched; no replay or historical cancellation. All own review writes were primary persisted.'
        $proof | ConvertTo-Json -Depth 18 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.persistence.json') -Encoding utf8
        $proof | ConvertTo-Json -Depth 18
    }
}
