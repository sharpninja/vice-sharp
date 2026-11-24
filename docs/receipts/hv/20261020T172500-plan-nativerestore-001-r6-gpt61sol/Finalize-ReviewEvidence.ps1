#Requires -Version 7.0
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Integrity','PersistenceHelper')][string]$Mode='Integrity')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$workspaceRoot='/home/sharpninja/github/vice-sharp'
if (-not $PSCmdlet.ShouldProcess($PSScriptRoot,('Retain final review support: '+$Mode))) {return}
switch ($Mode) {
    'PersistenceHelper' {
        $originalPath=Join-Path $workspaceRoot 'docs/receipts/hv/20261020T163300-plan-nativerestore-001-r5-gpt61sol/Save-ReviewPersistence.ps1'
        $copyPath=Join-Path $PSScriptRoot 'Save-ReviewPersistence.ps1'
        if (Test-Path -LiteralPath $copyPath) {throw 'Refusing to overwrite an existing persistence helper'}
        Copy-Item -LiteralPath $originalPath -Destination $copyPath
        $originalHash=(Get-FileHash -LiteralPath $originalPath).Hash
        $copyHash=(Get-FileHash -LiteralPath $copyPath).Hash
        if ($originalHash -ne $copyHash) {throw 'Copy verification failed'}
        [ordered]@{timestampLocal=(Get-Date).ToString('s');source=$originalPath;copy=$copyPath;originalSha256=$originalHash;copySha256=$copyHash;originalRetained=(Test-Path -LiteralPath $originalPath)} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'persistence-helper-copy-proof.json')
        $body=[IO.File]::ReadAllText($copyPath)
        $changes=[ordered]@{
            'Codex-20261020T213608Z-plugin-session'='Codex-20261020T222731Z-plugin-session'
            'req-20261020T213610Z-nativerestore-r5'='req-20261020T222756Z-nativerestore-r6'
            'BEGIN-HV-R5'='BEGIN-HV-R6'
            'END-HV-R5'='END-HV-R6'
            'round 5'='round 6'
            'revision 5'='revision 6'
            'revision-5'='revision-6'
            'all 41 prior IDs plus six new findings'='all 47 prior IDs plus four new findings'
            'all 47 findings'='all 51 findings'
            'All 41 round-4 IDs'='All 47 round-5 IDs'
            '97 accuracy / 96 completeness'='97 accuracy / 97 completeness'
            'accuracy 97%, completeness 96%'='accuracy 97%, completeness 97%'
            'accuracy:97'='accuracy:97'
            'completeness:96'='completeness:97'
            'PASS 40'='PASS 45'
            'ADD-AC 4'='ADD-AC 3'
            'Fails N12,N14,N19.'='Fails R02,N19,N20.'
            'FAIL 3 (N12,N14,N19)'='FAIL 3 (R02,N19,N20)'
            '37/37 final pins'='41/41 final pins'
            'review-integrity.json'='review-integrity-final.json'
            'fixture-inspection-corrected.json'='fixture-inspection.json'
            'pinned-sources.verified-final.json'='native-final.json'
            "(Join-Path `$PSScriptRoot 'review-identity.json')"="(Join-Path `$PSScriptRoot 'persistence-identity.json')"
        }
        foreach ($entry in $changes.GetEnumerator()) {$body=$body.Replace($entry.Key,$entry.Value)}
        [IO.File]::WriteAllText($copyPath,$body,[Text.UTF8Encoding]::new($false))
        [ordered]@{timestampLocal=(Get-Date).ToString('s');helper=$copyPath;sha256=(Get-FileHash -LiteralPath $copyPath).Hash;originalRetained=(Test-Path -LiteralPath $originalPath)} | ConvertTo-Json
    }
    'Integrity' {
        $pins=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $checks=@(foreach($pin in $pins){if($pin.path -match 'AGENTS-README-FIRST|\.creds'){throw 'Prohibited pin'};$hash=(Get-FileHash -LiteralPath $pin.path).Hash;$bytes=(Get-Item -LiteralPath $pin.path -Force).Length;[ordered]@{path=$pin.relativePath;expectedSha256=$pin.sha256;actualSha256=$hash;expectedBytes=$pin.bytes;actualBytes=$bytes;match=($hash -eq $pin.sha256 -and $bytes -eq $pin.bytes)}})
        $scriptChecks=@(foreach($name in @('Review.ps1','Evidence.ps1','Write-HvVerdict.ps1','Finalize-ReviewEvidence.ps1','Save-ReviewPersistence.ps1')){
            $scriptPath=Join-Path $PSScriptRoot $name
            $parseTokens=$null;$parseErrors=$null
            [Management.Automation.Language.Parser]::ParseFile($scriptPath,[ref]$parseTokens,[ref]$parseErrors)|Out-Null
            $issues=@(Invoke-ScriptAnalyzer -Path $scriptPath -Severity Warning,Error)
            [ordered]@{path=$scriptPath;parserErrors=@($parseErrors).Count;analyzerWarningsErrors=$issues.Count;findings=@($issues | Select-Object RuleName,Severity,Line,Message);sha256=(Get-FileHash -LiteralPath $scriptPath).Hash}
        })
        $scriptChecks | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'script-validation.json')
        if (@($checks | Where-Object {-not $_.match}).Count -or @($scriptChecks | Where-Object {$_.parserErrors -or $_.analyzerWarningsErrors}).Count) {throw 'Pin drift or script validation failure'}
        & (Join-Path $PSScriptRoot 'Evidence.ps1') -Mode Native -Final
        $finalNative=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'native-final.json') -Raw | ConvertFrom-Json
        $initialNative=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'native-initial.json') -Raw | ConvertFrom-Json
        $inventoryEqual=(($initialNative.inventory | ConvertTo-Json -Depth 8 -Compress) -eq ($finalNative.inventory | ConvertTo-Json -Depth 8 -Compress))
        $checkoutEqual=($initialNative.head -eq $finalNative.head -and $initialNative.branch -eq $finalNative.branch -and $initialNative.nativeHead -eq $finalNative.nativeHead -and $initialNative.gitlink -eq $finalNative.gitlink)
        $nativeStatusEqual=(($initialNative.porcelain -join "`n") -eq ($finalNative.porcelain -join "`n") -and ($initialNative.nativeIndexDiff -join "`n") -eq ($finalNative.nativeIndexDiff -join "`n"))
        & git -C $workspaceRoot diff --check
        $diffCheckCode=$LASTEXITCODE
        $trackedStatus=@(& git -C $workspaceRoot status --porcelain=v1 -uno)
        $proof=[ordered]@{timestampLocal=(Get-Date).ToString('s');pins=$checks;pinCount=$checks.Count;driftCount=@($checks | Where-Object {-not $_.match}).Count;nativeInventoryUnchanged=$inventoryEqual;checkoutUnchanged=$checkoutEqual;nativeStatusUnchanged=$nativeStatusEqual;nativeInventoryCount=@($finalNative.inventory).Count;materializedSourceCount=@($finalNative.pinnedSourceHashes).Count;materializedSourceMismatches=@($finalNative.pinnedSourceHashes | Where-Object {-not $_.match}).Count;gitDiffCheckExitCode=$diffCheckCode;outsideReceiptTrackedStatus=$trackedStatus;branch=$finalNative.branch;head=$finalNative.head;scriptValidation='script-validation.json';nativeBaselineCaveat='163 changed/untracked content hashes first measured after the allowed comparator run. Initial 41 input pins were rehashed before analysis; final source/native verification brackets subsequent read-only review.';writes='Only receipt-local helper/evidence/verdict/cache writes and the explicitly permitted managed comparator test outputs. No native/full-solution build, native execution, moves, deletes, commit or push.'}
        $proof | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'review-integrity-final.json')
        if (-not $inventoryEqual -or -not $checkoutEqual -or -not $nativeStatusEqual -or $diffCheckCode -ne 0 -or $trackedStatus.Count -or $proof.materializedSourceMismatches) {throw 'Final integrity mismatch'}
        [pscustomobject]$proof | Select-Object timestampLocal,pinCount,driftCount,nativeInventoryUnchanged,nativeInventoryCount,materializedSourceCount,materializedSourceMismatches,gitDiffCheckExitCode,branch,head | ConvertTo-Json
    }
}
