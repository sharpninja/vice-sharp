[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Prepare','Verify')][string] $Stage = 'Prepare')
$ErrorActionPreference = 'Stop'
function Get-TextSha256 {
    param([Parameter(Mandatory)][AllowEmptyString()][string] $Text)
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text)))
}
$prosePath = Join-Path $PSScriptRoot 'verdict.md'
$jsonPath = Join-Path $PSScriptRoot 'verdict.json'
$prose = [IO.File]::ReadAllText($prosePath)
$jsonText = [IO.File]::ReadAllText($jsonPath)
$verdict = $jsonText | ConvertFrom-Json
$identity = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'reviewer-identity.json') | ConvertFrom-Json
$fullResponse = $prose + "`nVERDICT_JSON`n" + $jsonText
$chunksPath = Join-Path $PSScriptRoot 'verdict-chunks.json'
$turnPath = Join-Path $PSScriptRoot 'review-turn.failsafe.json'
$yamlPath = Join-Path $PSScriptRoot 'authorized-failsafe/full-review-turn.yaml'
if ($Stage -eq 'Prepare') {
    if (-not $PSCmdlet.ShouldProcess('Complete review response and failsafe','Write full content package')) { return }
    $chunks = foreach ($document in @(@{name='prose';text=$prose},@{name='json';text=$jsonText})) {
        $part = 0
        for ($offset=0; $offset -lt $document.text.Length; $offset+=4096) {
            $part++
            $content = $document.text.Substring($offset,[Math]::Min(4096,$document.text.Length-$offset))
            [pscustomobject]@{document=$document.name;order=$part;content=$content;sha256=(Get-TextSha256 -Text $content)}
        }
    }
    $chunks | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $chunksPath -Encoding utf8
    $actions = @()
    $order = 0
    foreach ($finding in $verdict.findings) {
        $order++
        $actions += [ordered]@{order=$order;type='review_finding';status='completed';filePath=$finding.file;description=('{0} {1}: {2} Evidence: {3} Rationale: {4} Correction: {5}' -f $finding.id,$finding.status,$finding.section,$finding.evidence,$finding.rationale,$finding.correction)}
    }
    $decisions = @(
        'DISAGREE at accuracy 92 and completeness 84: unresolved consumer, hunk, snapshot and audit gaps prevent Phase D execution readiness.'
        'Honor R1 B and R9 exactly as approved; do not demand deletion or silently filter known failures.'
        'The supplied TDE-off DRIVE8 fixture makes H5 a current prerequisite failure, not a hypothetical later Phase E issue.'
        'Native observer clock/PC evidence is narrow; preserve full native resume/round-trip and register obligations.'
        'After supported primary and triage failure, retain full content locally and label it queued only; do not repair trust or replay historical queues.'
    )
    $dialogItems = @()
    foreach ($chunk in $chunks) { $dialogItems += [ordered]@{order=$chunk.order;document=$chunk.document;timestampLocal=(Get-Date -Format 's');role='model';category='observation';content=$chunk.content} }
    foreach ($decision in $decisions) {
        $order++
        $actions += [ordered]@{order=$order;type='design_decision';status='completed';filePath='docs/plans/PLAN-NATIVERESTORE-001.md';description=$decision}
        $dialogItems += [ordered]@{order=$order;timestampLocal=(Get-Date -Format 's');role='model';category='decision';content=$decision}
    }
    $turn = [ordered]@{
        timestampLocal=(Get-Date -Format 's')
        sourceType='Codex';agent='Codex';model='gpt-6.1-sol';effort='max'
        sessionId=$identity.reviewerSessionId;requestId=$identity.reviewerRequestId
        parentSessionId=$identity.parentSessionId;parentRequestId=$identity.parentRequestId
        queryTitle='Hostile review of native restoration plan revision 2'
        interpretation='Independently reverify every round 1 finding, attack all revised plan surfaces, retain complete verdict and audit evidence; no Phase D execution.'
        intendedStatus='completed';primaryPersisted=$false;primaryCompletedStateProved=$false
        disposition='QUEUED_ONLY_AUTHORIZED_FULL_CONTENT_FAILSAFE'
        response=$fullResponse;verdictProse=$prose;verdictJson=$jsonText
        proseSha256=(Get-TextSha256 -Text $prose);jsonSha256=(Get-TextSha256 -Text $jsonText)
        actions=$actions;dialogItems=$dialogItems;designDecisions=$decisions
        filesModified=@('review-findings.psd1','verdict.md','verdict.json','verdict.persistence.json','review audit helpers and evidence inside this receipt directory')
        contextList=@($verdict.requestPath,$verdict.responsePath,'input-pins.json','state-before.json','boundary-verification.json','source-pin-verification.json','vsf-fixture-inspection.json','consumer-inventory.json','patch-hunks.json','logging-attempts.jsonl')
        requirementsDiscovered=@('Existing resume and phase ordering conflict N01; existing Slice 1 obligation gap F05; no requirement records were mutated.')
        blockers=@('MCP_UNTRUSTED: Health check failed - Permission denied','F03','F04','F05','F10','N01','U01','U02','R02')
        processingDialog=@(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'review-audit.jsonl') | ForEach-Object { $_ | ConvertFrom-Json })
    }
    $turn | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $turnPath -Encoding utf8
    . '/home/sharpninja/github/mcpserver-codex-plugin/lib/yaml-object-mutation.ps1'
    Write-McpYamlObject -Path $yamlPath -Document ([ordered]@{method='workflow.sessionlog.completeTurn';label='hv-native-restore-r2-full-content';timestampLocal=(Get-Date -Format 's');sessionId=$identity.reviewerSessionId;requestId=$identity.reviewerRequestId;parentSessionId=$identity.parentSessionId;parentRequestId=$identity.parentRequestId;primaryPersisted=$false;doNotReplayAutomatically=$true;params=@{response=$fullResponse};fullContentTurn=$turn})
    @{response=$fullResponse;interpretation=$turn.interpretation;contextList=$turn.contextList;tags=@('hostile-validation','PLAN-NATIVERESTORE-001','revision-2','queued-only')} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'update-turn.params.json') -Encoding utf8
    @{response=$fullResponse} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'complete-turn.params.json') -Encoding utf8
    [pscustomobject]@{timestampLocal=(Get-Date -Format 's');chunks=@($chunks).Count;proseSha256=$turn.proseSha256;jsonSha256=$turn.jsonSha256;primaryPersisted=$false} | ConvertTo-Json
    return
}
$chunks = Get-Content -Raw -LiteralPath $chunksPath | ConvertFrom-Json
$chunkErrors = @($chunks | Where-Object { (Get-TextSha256 -Text $_.content) -ne $_.sha256 })
$rebuiltProse = (@($chunks | Where-Object document -eq 'prose' | Sort-Object order).content) -join ''
$rebuiltJson = (@($chunks | Where-Object document -eq 'json' | Sort-Object order).content) -join ''
$turnReadback = Get-Content -Raw -LiteralPath $turnPath | ConvertFrom-Json
. '/home/sharpninja/github/mcpserver-codex-plugin/lib/yaml-object-mutation.ps1'
$yamlReadback = Read-McpYamlObject -Path $yamlPath
$checks = [ordered]@{
    chunkHashesMatch=($chunkErrors.Count -eq 0)
    proseChunksExact=($rebuiltProse -ceq $prose)
    jsonChunksExact=($rebuiltJson -ceq $jsonText)
    turnProseExact=($turnReadback.verdictProse -ceq $prose)
    turnJsonExact=($turnReadback.verdictJson -ceq $jsonText)
    yamlProseExact=($yamlReadback.fullContentTurn.verdictProse -ceq $prose)
    yamlJsonExact=($yamlReadback.fullContentTurn.verdictJson -ceq $jsonText)
    yamlResponseExact=($yamlReadback.params.response -ceq $fullResponse)
}
if (@($checks.Values | Where-Object { -not $_ }).Count) { $checks | ConvertTo-Json; throw 'Full-content fallback readback mismatch.' }
$attempts = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'logging-attempts.jsonl') | ForEach-Object { $_ | ConvertFrom-Json })
$proof = [ordered]@{
    timestampLocal=(Get-Date -Format 's')
    reviewerSessionId=$identity.reviewerSessionId;reviewerRequestId=$identity.reviewerRequestId
    parentSessionId=$identity.parentSessionId;parentRequestId=$identity.parentRequestId
    primaryPersisted=$false;PrimaryReadback=$false;primaryCompletedStateProved=$false
    persistenceDisposition='QUEUED ONLY. The supported primary and triage routes failed; local complete content is retained. No server-completed state is claimed.'
    errorSignature='MCP_UNTRUSTED: marker refresh failed before REPL request: MCP_UNTRUSTED: Health check failed - Permission denied'
    triageAttemptCount=@($attempts | Where-Object method -eq 'workflow.triage.report').Count
    completionAttempt=@($attempts | Where-Object method -eq 'workflow.sessionlog.completeTurn')
    historyAttempts=@($attempts | Where-Object method -eq 'workflow.sessionlog.queryHistory')
    fullContentReadback=$checks
    normalization='verdict.md and verdict.json are UTF-8 without BOM with LF. JSON chunks, rich JSON turn and supported-helper YAML readback reproduce the original content exactly. No CRLF/LF content normalization was needed; serialized container line endings are not included in document-content hashes.'
    proseFile=@{path=$prosePath;sha256=(Get-FileHash -LiteralPath $prosePath -Algorithm SHA256).Hash;reassembledSha256=(Get-TextSha256 -Text $rebuiltProse);bytes=(Get-Item -LiteralPath $prosePath).Length}
    jsonFile=@{path=$jsonPath;sha256=(Get-FileHash -LiteralPath $jsonPath -Algorithm SHA256).Hash;reassembledSha256=(Get-TextSha256 -Text $rebuiltJson);bytes=(Get-Item -LiteralPath $jsonPath).Length}
    fallbackFiles=@(@{path=$turnPath;sha256=(Get-FileHash -LiteralPath $turnPath -Algorithm SHA256).Hash},@{path=$yamlPath;sha256=(Get-FileHash -LiteralPath $yamlPath -Algorithm SHA256).Hash},@{path=$chunksPath;sha256=(Get-FileHash -LiteralPath $chunksPath -Algorithm SHA256).Hash})
    findingCount=($rebuiltJson | ConvertFrom-Json).findings.Count
    scores=@{accuracyPercent=$verdict.accuracyPercent;completenessPercent=$verdict.completenessPercent}
    authorization='Current request authorizes full-content failsafe only after genuine primary failure including failed supported triage; both are retained in logging-attempts.jsonl.'
    noTrustRepair=$true;historicalQueueReplayCount=0;historicalTurnCancellationCount=0
    nextAction='Later authorized supported persistence and primary full-content/completed-state readback, or an explicit operator waiver. These local packages must not be replayed automatically.'
}
if ($PSCmdlet.ShouldProcess('verdict.persistence.json','Write persistence failure and complete local readback proof')) { $proof | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.persistence.json') -Encoding utf8 }
[pscustomobject]@{timestampLocal=$proof.timestampLocal;checks=$checks;findingCount=$proof.findingCount;triageAttemptCount=$proof.triageAttemptCount;primaryPersisted=$false;primaryCompletedStateProved=$false;prose=$proof.proseFile;json=$proof.jsonFile} | ConvertTo-Json -Depth 12
