[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][ValidateSet('Analyze','Fresh','History','Export')][string]$Mode)
$ErrorActionPreference = 'Stop'
if ($WhatIfPreference) { Write-Output ('WhatIf: audit ' + $Mode); return }
if (-not $PSCmdlet.ShouldProcess($PSScriptRoot, $Mode)) { return }
$root = '/home/sharpninja/github/vice-sharp'
switch ($Mode) {
    'Analyze' {
        $results = @(foreach ($script in (Get-ChildItem -LiteralPath $PSScriptRoot -Filter 'Review-*.ps1' -File)) {
            $tokens = $null
            $parseErrors = $null
            $null = [Management.Automation.Language.Parser]::ParseFile($script.FullName,[ref]$tokens,[ref]$parseErrors)
            $findings = @(Invoke-ScriptAnalyzer -Path $script.FullName -Severity Warning,Error)
            [ordered]@{path=$script.FullName;parseErrors=@($parseErrors | ForEach-Object Message);findings=@($findings | Select-Object RuleName,Severity,Line,Message)}
        })
        [ordered]@{timestampLocal=(Get-Date -Format s);scripts=$results} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'script-analysis.json')
        $results | ConvertTo-Json -Depth 10
        if (@($results | Where-Object { $_.parseErrors.Count -or $_.findings.Count }).Count) { throw 'Review helper analysis failed.' }
    }
    'Fresh' {
        $tokenPaths = @(& rg -l --glob '*.cs' -- 'ViceNativeBridge|\[ViceFact|\[ViceTheory' (Join-Path $root 'tests'))
        $categoryPaths = @($tokenPaths | Where-Object { @(Select-String -LiteralPath $_ -Pattern 'Trait\("Category"').Count -gt 0 })
        $semanticPaths = @(& rg -l --glob '*.cs' -- 'ViceNative\.|ViceNativeXvic|ViceNativeBridge|IViceNative|XvicOraclePrerequisites' (Join-Path $root 'tests'))
        [xml]$trx = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'TestResults/vic20-comparator.trx') -Raw
        $counters = $trx.SelectSingleNode('//*[local-name()="Counters"]')
        $run = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'TestResults/run.json') -Raw | ConvertFrom-Json
        $result = [ordered]@{timestampLocal=(Get-Date -Format s);tokenCount=$tokenPaths.Count;categoryCount=$categoryPaths.Count;semanticCount=$semanticPaths.Count;tokenPaths=$tokenPaths;categoryPaths=$categoryPaths;semanticPaths=$semanticPaths;testCommand=$run;testCounters=[ordered]@{total=[int]$counters.GetAttribute('total');executed=[int]$counters.GetAttribute('executed');passed=[int]$counters.GetAttribute('passed');failed=[int]$counters.GetAttribute('failed');notExecuted=[int]$counters.GetAttribute('notExecuted');aborted=[int]$counters.GetAttribute('aborted');skipped=0};testResults=@($trx.TestRun.Results.UnitTestResult | ForEach-Object { [ordered]@{testName=$_.testName;outcome=$_.outcome;message=$_.Output.ErrorInfo.Message;stackTrace=$_.Output.ErrorInfo.StackTrace} });nativeSafeInSolution=@(& rg -n -- 'ViceSharp\.(OracleContract|VsfCompatibility|NativeClockProbe)\.Tests' (Join-Path $root 'ViceSharp.slnx'));newTargetsOrTool=@(& rg -n -- 'GenerateNativeSnapshotFixtures|NativeSnapshotCompatibilityTest' (Join-Path $root 'build/Build.cs'));generatorExists=(Test-Path -LiteralPath (Join-Path $root 'tools/ViceSharp.SnapshotFixtureGenerator'));desktopProductChanges=@(& git diff --name-only 2c8d6028 HEAD -- src build native/build-vice-observer.ps1)}
        $result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'fresh-checks.json')
        [pscustomobject]$result | Select-Object timestampLocal,tokenCount,categoryCount,semanticCount,testCommand,testCounters,testResults,nativeSafeInSolution,newTargetsOrTool,generatorExists,desktopProductChanges | ConvertTo-Json -Depth 12
    }
    'History' {
        $r3b = Get-Content -LiteralPath (Join-Path $root 'docs/receipts/hv/20261020T141600-plan-nativerestore-001-r3b-gpt61sol/verdict.json') -Raw | ConvertFrom-Json
        $r4 = Get-Content -LiteralPath (Join-Path $root 'docs/receipts/hv/20261020T153600-plan-nativerestore-001-r4-gpt61sol/verdict.json') -Raw | ConvertFrom-Json
        $r7 = Get-Content -LiteralPath (Join-Path $root 'docs/receipts/hv/20261020T180300-plan-nativerestore-001-r7-gpt61sol/verdict.json') -Raw | ConvertFrom-Json
        $ledger = @(foreach ($finding in $r7.findings) {
            $old = @($r3b.findings | Where-Object id -EQ $finding.id)
            $revision4 = @($r4.findings | Where-Object id -EQ $finding.id)
            [ordered]@{id=$finding.id;round7Status=$finding.status;round7Section=$finding.section;round3bStatus=if($old.Count){$old[0].status}else{$null};revision4Disposition=if($old.Count -and $revision4.Count){$revision4[0].status}else{$null};revision4Evidence=if($old.Count -and $revision4.Count){$revision4[0].evidence}else{$null}}
        })
        [ordered]@{timestampLocal=(Get-Date -Format s);round3bCount=$r3b.findings.Count;round7Count=$r7.findings.Count;ledger=$ledger} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'finding-history.json')
        $ledger | ForEach-Object { [pscustomobject]$_ } | Select-Object id,round7Status,round7Section,round3bStatus,revision4Disposition | ConvertTo-Json -Depth 6
    }
    'Export' {
        $planPath = Join-Path $root 'docs/plans/PLAN-NATIVERESTORE-001.md'
        $lines = @(Get-Content -LiteralPath $planPath)
        $wordingMatches = @(for ($i=0;$i -lt $lines.Count;$i++) { if ($lines[$i] -match '(?i)export|exclusiv|only checkpoint|no external seam|sole') { [ordered]@{line=$i+1;text=$lines[$i]} } })
        $core = Join-Path $root 'artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source/vice/src/6510dtvcore.c'
        $sourceLines = @(& rg -n -- 'EXPORT_REGISTERS\(' $core)
        [ordered]@{timestampLocal=(Get-Date -Format s);planPath=$planPath;matches=$wordingMatches;activeCoreExports=$sourceLines} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'export-wording-audit.json')
        $wordingMatches | ForEach-Object { Write-Output ('Plan line ' + $_.line + ': ' + $_.text.Substring(0,[Math]::Min($_.text.Length,160))) }
        $sourceLines
    }
}
