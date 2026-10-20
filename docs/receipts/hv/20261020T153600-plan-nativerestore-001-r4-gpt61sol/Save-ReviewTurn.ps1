[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Write','Verify','Complete','Prove')][string]$Operation)
$ErrorActionPreference='Stop'
$parseTokens=$null;$parseErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis=@(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning,Error -WhatIf:$false)
if ($analysis.Count) { throw ($analysis | Out-String) }
Write-Output 'Parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess($Operation,'Persist or read back the complete verdict through the supported plugin')) { return }
. '/home/sharpninja/github/mcpserver-codex-plugin/lib/yaml-object-mutation.ps1'
Import-McpYamlSerializer
$identity=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'reviewer-identity.json') -Raw | ConvertFrom-Json
$manifestPath=Join-Path $PSScriptRoot 'verdict-chunks.manifest.json'
function Get-ReviewHash {
    param([string]$Text)
    [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Text)))
}
function Invoke-ReviewMethod {
    param([string]$Method,[object]$Payload,[string]$Label,[switch]$RequirePrimary)
    $paramsPath=Join-Path $PSScriptRoot ($Label+'.params.json')
    $Payload | ConvertTo-Json -Depth 90 | Set-Content -LiteralPath $paramsPath -Encoding utf8
    & (Join-Path $PSScriptRoot 'Invoke-ReviewPlugin.ps1') -Operation Invoke -Method $Method -PayloadPath $paramsPath -Label $Label -Quiet | Out-Host
    $documents=@(ConvertFrom-Yaml -Yaml (Get-Content -LiteralPath (Join-Path $PSScriptRoot ($Label+'.txt')) -Raw) -AllDocuments)
    $results=@($documents | Where-Object { $_.type -eq 'result' })
    if ($results.Count -ne 1) { throw ('Missing unique result: '+$Label) }
    $result=$results[0].payload.result
    if ($RequirePrimary -and (-not $result.persisted -or $result.queued -or $result.degraded)) { throw ('Not primary persisted: '+$Label) }
    return $result
}
function Get-ReviewTurn {
    param([string]$Label)
    $history=Invoke-ReviewMethod -Method client.SessionLog.QueryAsync -Payload ([ordered]@{agent='Codex';text='PLAN-NATIVERESTORE-001 revision 4';limit=10;offset=0}) -Label $Label
    $history | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ($Label+'.parsed.json')) -Encoding utf8
    $sessions=@($history.items | Where-Object { $_.sessionId -eq $identity.reviewerSessionId })
    if ($sessions.Count -ne 1) { throw 'Own session not uniquely found in history.' }
    $turns=@($sessions[0].turns | Where-Object { $_.requestId -eq $identity.reviewerRequestId })
    if ($turns.Count -ne 1) { throw 'Own request not uniquely found in history.' }
    return $turns[0]
}
switch ($Operation) {
    'Write' {
        if (Test-Path -LiteralPath $manifestPath) {
            $manifest=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            if ($manifest.sessionId -ne $identity.reviewerSessionId -or $manifest.requestId -ne $identity.reviewerRequestId) { throw 'Wrong content generation owner.' }
            foreach ($entry in $manifest.files) { if ((Get-ReviewHash ([IO.File]::ReadAllText((Join-Path $PSScriptRoot $entry.name)))) -ne $entry.sha256) { throw 'Source content changed; no continuation allowed.' } }
            $turn=Get-ReviewTurn -Label resume-content-history
            $remoteParts=@($turn.processingDialog | Where-Object { ([string]$_.content).StartsWith('HV_FULL_CONTENT|'+$manifest.generation+'|') })
            if ($remoteParts.Count -ne $manifest.confirmedChunks.Count) { throw 'Uncertain remote chunk count; no replay allowed.' }
            foreach ($confirmed in $manifest.confirmedChunks) {
                $header='HV_FULL_CONTENT|'+$manifest.generation+'|'+$confirmed.name+'|'+$confirmed.index+'|'
                $part=@($remoteParts | Where-Object { ([string]$_.content).StartsWith($header) })
                if ($part.Count -ne 1) { throw 'Confirmed remote chunk missing/duplicated.' }
                $body=([string]$part[0].content) -replace '^HV_FULL_CONTENT[^\n]*\r?\n',''
                if ((Get-ReviewHash $body) -ne $confirmed.sha256) { throw 'Confirmed remote chunk hash mismatch.' }
            }
        } else {
            $generation=[Guid]::NewGuid().ToString('N')
            $chunkSize=4500
            $entries=@(foreach ($name in @('verdict.md','verdict.json')) {
                $text=[IO.File]::ReadAllText((Join-Path $PSScriptRoot $name))
                [pscustomobject][ordered]@{name=$name;sha256=(Get-ReviewHash $text);characters=$text.Length;chunkCount=[int][Math]::Ceiling($text.Length/[double]$chunkSize)}
            })
            $manifest=[pscustomobject][ordered]@{timestampLocal=[DateTime]::Now.ToString('s');generation=$generation;sessionId=$identity.reviewerSessionId;requestId=$identity.reviewerRequestId;chunkSize=$chunkSize;files=$entries;confirmedChunks=@()}
            $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        }
        $generation=$manifest.generation;$chunkSize=$manifest.chunkSize;$entries=$manifest.files
        $total=0
        foreach ($entry in $entries) { $total += [int]$entry.chunkCount }
        $written=$manifest.confirmedChunks.Count
        foreach ($entry in $entries) {
            $text=[IO.File]::ReadAllText((Join-Path $PSScriptRoot $entry.name))
            for ($index=0;$index -lt $entry.chunkCount;$index++) {
                if (@($manifest.confirmedChunks | Where-Object { $_.name -eq $entry.name -and $_.index -eq $index+1 }).Count) { continue }
                $offset=$index*$chunkSize
                $chunk=$text.Substring($offset,[Math]::Min($chunkSize,$text.Length-$offset))
                $header='HV_FULL_CONTENT|'+$generation+'|'+$entry.name+'|'+($index+1)+'|'+$entry.chunkCount+"`n"
                $label='content-'+$entry.name.Replace('.','-')+'-'+($index+1).ToString('D3')
                $payload=[ordered]@{requestId=$identity.reviewerRequestId;dialogItems=@([ordered]@{timestamp=[DateTime]::Now.ToString('s');order=$written+1;role='model';category='observation';content=$header+$chunk})}
                $null=Invoke-ReviewMethod -Method workflow.sessionlog.appendDialog -Payload $payload -Label $label -RequirePrimary
                $manifest.confirmedChunks += [ordered]@{name=$entry.name;index=$index+1;sha256=(Get-ReviewHash $chunk);receipt=$label+'.txt';timestampLocal=[DateTime]::Now.ToString('s')}
                $manifest | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $manifestPath -Encoding utf8
                $written++
                [ordered]@{timestampLocal=[DateTime]::Now.ToString('s');completionPercent=[int](76+[Math]::Floor(10*$written/[double]$total));etaSeconds=[int](120+4*($total-$written));persistedChunks=$written;totalChunks=$total} | ConvertTo-Json -Compress | Write-Output
            }
        }
    }
    'Complete' {
        $verification=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict-readback.json') -Raw | ConvertFrom-Json
        if (-not $verification.allExact) { throw 'Exact complete-content readback is required first.' }
        $prose=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'verdict.md'))
        $json=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'verdict.json'))
        $bundle=$prose+"`nVERDICT_JSON`n"+$json
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verdict.bundle.txt'),$bundle,[Text.UTF8Encoding]::new($false))
        $turn=Get-ReviewTurn -Label metadata-readback
        $pins=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $contexts=@(@($turn.contextList)+@($pins.path)+@((Join-Path $PSScriptRoot 'verdict.md'),(Join-Path $PSScriptRoot 'verdict.json'),(Join-Path $PSScriptRoot 'verdict.persistence.json'),(Join-Path $PSScriptRoot 'final-integrity.json')) | Sort-Object -Unique)
        $payload=[ordered]@{requestId=$identity.reviewerRequestId;queryTitle='Hostile review of native restoration revision 4';interpretation='Class 1 independent pre-execution review, all four mandatory surfaces. DISAGREE 96/93: 34 PASS, 3 FAIL, 0 UNKNOWN, 4 ADD-AC. Exact reviewer gpt-6.1-sol-max@Codex; generic wrapper model codex is disclosed. Execution remains blocked.';response=$bundle;contextList=$contexts;tags=@('PLAN-NATIVERESTORE-001','revision-4','hostile-validation','DISAGREE','FR-VICE-001','TR-VSFLOCKSTEP-RESUME-001','TR-CYCLE-001','FR-SNP-005','TEST-SNP-002',('parentSessionId='+(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json).parentSessionId),('parentRequestId='+(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json).parentRequestId));planFile='docs/plans/PLAN-NATIVERESTORE-001.md';todoId='PLAN-VICEORACLE-001'}
        $null=Invoke-ReviewMethod -Method workflow.sessionlog.updateTurn -Payload $payload -Label verdict-context-update -RequirePrimary
        $actionBase=[int][DateTimeOffset]::Now.ToUnixTimeSeconds()
        $actions=@(
            [ordered]@{order=$actionBase;type='review_verdict';status='completed';filePath=(Join-Path $PSScriptRoot 'verdict.json');description='Full independent verdict DISAGREE: accuracy 96, completeness 93; 34 PASS, 3 FAIL (F05,N08,N09), 0 UNKNOWN, 4 ADD-AC (N10-N13). All 35 round 3b IDs independently dispositioned. Full prose/JSON primary-persisted in ordered chunks and hash-verified on readback.'},
            [ordered]@{order=$actionBase+1;type='validation';status='completed';filePath=(Join-Path $PSScriptRoot 'comparator.trx');description='Authorized managed Release comparator filter with --no-build --no-restore: exit 1, 1 failed, 0 passed, 0 skipped, missing HalfCycleState at source line 23. Baseline diagnostic; no other test/native/full build run.'},
            [ordered]@{order=$actionBase+2;type='validation';status='completed';filePath=(Join-Path $PSScriptRoot 'final-integrity.json');description='Final pinned input/source/native-inventory and git diff --check evidence retained. No native mutation, full-solution build, native execution/build, nested reviewer, commit or push.'}
        )
        $null=Invoke-ReviewMethod -Method workflow.sessionlog.completeTurn -Payload ([ordered]@{requestId=$identity.reviewerRequestId;queryTitle='Hostile review of native restoration revision 4';response=$bundle;actions=$actions}) -Label verdict-complete -RequirePrimary
        [ordered]@{timestampLocal=[DateTime]::Now.ToString('s');sessionId=$identity.reviewerSessionId;requestId=$identity.reviewerRequestId;bundleSha256=(Get-ReviewHash $bundle);completed=$true} | ConvertTo-Json -Compress | Write-Output
    }
    { $_ -in @('Verify','Prove') } {
        $manifest=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $label=if ($Operation -eq 'Prove') { 'completed-history' } else { 'content-readback' }
        $turn=Get-ReviewTurn -Label $label
        $comparisons=@(foreach ($entry in $manifest.files) {
            $parts=@(foreach ($dialog in $turn.processingDialog) {
                $content=[string]$dialog.content
                if ($content -match '^HV_FULL_CONTENT\|([^|]+)\|([^|]+)\|(\d+)\|(\d+)\r?\n') {
                    $generation=$Matches[1];$name=$Matches[2];$index=[int]$Matches[3];$count=[int]$Matches[4];$prefix=$Matches[0].Length
                    if ($generation -eq $manifest.generation -and $name -eq $entry.name) { [pscustomobject]@{index=$index;count=$count;text=$content.Substring($prefix)} }
                }
            })
            $ordered=@($parts | Sort-Object index)
            if ($parts.Count -ne $entry.chunkCount) { throw ('Missing/duplicated chunks: '+$entry.name) }
            for ($index=0;$index -lt $ordered.Count;$index++) { if ($ordered[$index].index -ne $index+1 -or $ordered[$index].count -ne $entry.chunkCount) { throw 'Invalid chunk order.' } }
            $text=$ordered.text -join ''
            $source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot $entry.name))
            $sourceHash=Get-ReviewHash $source
            $readHash=Get-ReviewHash $text
            $normalizedSource=$source.Replace("`r`n","`n").Replace("`r","`n")
            $normalizedRead=$text.Replace("`r`n","`n").Replace("`r","`n")
            [IO.File]::WriteAllText((Join-Path $PSScriptRoot ($entry.name+'.readback')),$text,[Text.UTF8Encoding]::new($false))
            [pscustomobject][ordered]@{name=$entry.name;expectedChunks=$entry.chunkCount;readChunks=$parts.Count;sourceSha256=$sourceHash;readbackSha256=$readHash;exactMatch=$sourceHash -eq $readHash;normalizedMatch=(Get-ReviewHash $normalizedSource) -eq (Get-ReviewHash $normalizedRead);normalization=if ($sourceHash -eq $readHash) { 'none; exact UTF-8/LF content' } else { 'CRLF/CR compared after normalization to LF' };sourceCharacters=$source.Length;readbackCharacters=$text.Length}
        })
        $allExact=@($comparisons | Where-Object { -not $_.exactMatch }).Count -eq 0
        $proof=[ordered]@{timestampLocal=[DateTime]::Now.ToString('s');mode='primary';persisted=$true;queued=$false;reviewerAgent='Codex';reviewerModel='gpt-6.1-sol';reviewerEffort='max';reviewerSessionId=$identity.reviewerSessionId;reviewerRequestId=$identity.reviewerRequestId;allExact=$allExact;contents=$comparisons;turnStatus=$turn.status;historyMethod='client.SessionLog.QueryAsync';historyReceipt=$label+'.txt';normalizationDisclosure='Per-file exact and LF-normalized hashes retained; none discarded.'}
        if ($Operation -eq 'Prove') {
            $bundle=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'verdict.bundle.txt'))
            $proof['responseSourceSha256']=Get-ReviewHash $bundle
            $proof['responseReadbackSha256']=Get-ReviewHash ([string]$turn.response)
            $proof['responseExactMatch']=$proof.responseSourceSha256 -eq $proof.responseReadbackSha256
            $proof['turnCompleted']=$turn.status -eq 'completed'
            $proof['historyCompleted']=$turn.status -eq 'completed'
            $proof['completionReceipt']='verdict-complete.txt'
            $proof['preexistingQueueDisclosure']='Two historical queued records reported before bootstrap; drain disabled; no replay or cancellation. This review is primary, not queued.'
            $proof['wrapperModelHeader']='codex (generic wrapper header; exact launch/model/effort retained in verdict)'
            $proof['requestPath']=Join-Path $PSScriptRoot 'request.jsonl'
            $proof['responsePath']=Join-Path $PSScriptRoot 'response.jsonl'
            $proof | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict.persistence.json') -Encoding utf8
            if (-not $proof.turnCompleted -or -not $proof.responseExactMatch) { throw 'Completed-state/full-response history proof failed.' }
        } else { $proof | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verdict-readback.json') -Encoding utf8 }
        $proof | ConvertTo-Json -Depth 40 | Write-Output
        if (-not $allExact) { throw 'Exact complete-content comparison failed.' }
    }
}
