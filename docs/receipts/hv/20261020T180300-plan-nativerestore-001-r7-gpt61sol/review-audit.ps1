[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Historical','Integrity','Analyze','Summary')][string]$Operation = 'Summary')
$ErrorActionPreference = 'Stop'
$workspace = '/home/sharpninja/github/vice-sharp'
switch ($Operation) {
    'Historical' {
        $r3 = Join-Path $workspace 'docs/receipts/hv/20261020T141600-plan-nativerestore-001-r3b-gpt61sol/verdict.json'
        $r4 = Join-Path $workspace 'docs/receipts/hv/20261020T153600-plan-nativerestore-001-r4-gpt61sol/verdict.json'
        $r6 = Join-Path $workspace 'docs/receipts/hv/20261020T172500-plan-nativerestore-001-r6-gpt61sol/verdict.json'
        $a = Get-Content -LiteralPath $r3 -Raw | ConvertFrom-Json
        $b = Get-Content -LiteralPath $r4 -Raw | ConvertFrom-Json
        $c = Get-Content -LiteralPath $r6 -Raw | ConvertFrom-Json
        $historical = foreach ($f in $a.findings) {
            $match = @($b.findings | Where-Object id -CEQ $f.id)
            if ($match.Count -ne 1) { throw ('Missing or duplicate historical id: ' + $f.id) }
            [pscustomobject]@{ id = $f.id; round3bStatus = $f.status; revision4Disposition = $match[0].status }
        }
        $receipt = [ordered]@{ timestampLocal = (Get-Date).ToString('s'); round3bPath = $r3; round3bSha256 = (Get-FileHash -LiteralPath $r3).Hash; round4Path = $r4; round4Sha256 = (Get-FileHash -LiteralPath $r4).Hash; round6Path = $r6; round6Sha256 = (Get-FileHash -LiteralPath $r6).Hash; historical = @($historical); round6Ids = @($c.findings.id) }
        if ($PSCmdlet.ShouldProcess($PSScriptRoot, 'Save independently parsed historical finding ledger')) {
            $receipt | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'historical-ledger.json') -Encoding utf8
        }
        $historical | ConvertTo-Json -Depth 8
    }
    'Integrity' {
        if (-not $PSCmdlet.ShouldProcess($PSScriptRoot, 'Save final read-only integrity evidence')) { return }
        & (Join-Path $PSScriptRoot 'review-state.ps1') -Stage After
        $before = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'state-before.json') -Raw | ConvertFrom-Json
        $after = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'state-after.json') -Raw | ConvertFrom-Json
        $changed = @(Compare-Object -ReferenceObject @($before.nativeFiles) -DifferenceObject @($after.nativeFiles) -Property path,status,sha256,bytes)
        $pins = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $pinRows = @($pins | ForEach-Object { [pscustomobject]@{ path = $_.relativePath; matches = ((Get-FileHash -LiteralPath $_.path).Hash -ceq $_.sha256); bytes = (Get-Item -LiteralPath $_.path -Force).Length } })
        $diffOutput = @(& git -C $workspace diff --check)
        $diffExit = $LASTEXITCODE
        $indexOutput = @(& git -C (Join-Path $workspace 'native/vice') diff --cached --name-only)
        $indexExit = $LASTEXITCODE
        $receipt = [ordered]@{ timestampLocal = (Get-Date).ToString('s'); initialBranch = $before.branch; finalBranch = $after.branch; initialHead = $before.commit; finalHead = $after.commit; initialNativeHead = $before.nativeHead; finalNativeHead = $after.nativeHead; nativeFileCountBefore = @($before.nativeFiles).Count; nativeFileCountAfter = @($after.nativeFiles).Count; nativeDifferences = $changed; nativeStatusEqual = ((@($before.nativeStatus) -join "`n") -ceq (@($after.nativeStatus) -join "`n")); submoduleStatusEqual = ((@($before.submoduleStatus) -join "`n") -ceq (@($after.submoduleStatus) -join "`n")); nativeIndexPaths = $indexOutput; nativeIndexCommandExit = $indexExit; pins = $pinRows; pinCount = $pinRows.Count; pinDriftCount = @($pinRows | Where-Object { -not $_.matches }).Count; gitDiffCheckExit = $diffExit; gitDiffCheckOutput = $diffOutput }
        $receipt | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'final-integrity.json') -Encoding utf8
        [pscustomobject]$receipt | Select-Object * -ExcludeProperty pins | ConvertTo-Json -Depth 10
        if ($changed.Count -or $receipt.pinDriftCount -or $diffExit -or $indexExit -or $indexOutput.Count -or -not $receipt.nativeStatusEqual -or $before.commit -cne $after.commit) { throw 'Final integrity gate did not match the starting state' }
    }
    'Analyze' {
        $results = foreach ($script in Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter '*.ps1') {
            $parseTokens = $null
            $parseErrors = $null
            $null = [System.Management.Automation.Language.Parser]::ParseFile($script.FullName,[ref]$parseTokens,[ref]$parseErrors)
            $findings = @(Invoke-ScriptAnalyzer -Path $script.FullName -Severity Warning,Error)
            [pscustomobject]@{ script = $script.Name; parseErrors = @($parseErrors).Count; analyzerFindings = $findings.Count; details = @($findings | Select-Object RuleName,Line,Message) }
        }
        $results | ConvertTo-Json -Depth 10
        if (@($results | Where-Object { $_.parseErrors -or $_.analyzerFindings }).Count) { throw 'Receipt helper analyzer gate failed' }
    }
    'Summary' {
        $i = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'inspection.json') -Raw | ConvertFrom-Json
        [pscustomobject]@{ patchLines = $i.patchLineCount; hunks = $i.hunkCount; inventoryCount = $i.inventoryCount; semanticCandidates = $i.semanticCandidateCount; tokenCandidates = $i.tokenCandidateCount; tokenWithCategoryCount = $i.tokenWithCategoryCount; fixtures = @($i.fixtures | ForEach-Object { $m = @($_.modules | Where-Object name -CEQ 'MAINCPU')[0]; [pscustomobject]@{ path = $_.path; sha256 = $_.sha256; bytes = $_.bytes; minor = $m.minor; clock = $m.clock; registers = @($m.a,$m.x,$m.y,$m.sp,$m.pc,$m.p) } }) } | ConvertTo-Json -Depth 10
    }
}
