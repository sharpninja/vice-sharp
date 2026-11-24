[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference='Stop'
$parseTokens=$null;$parseErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis=@(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning,Error -WhatIf:$false)
if ($analysis.Count) { throw ($analysis | Out-String) }
if (-not $PSCmdlet.ShouldProcess('Final integrity','Read-only rehash and receipt-local verification')) { return }
$initial=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'initial-inventory.json') -Raw | ConvertFrom-Json
$final=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'final-inventory.json') -Raw | ConvertFrom-Json
$before=$initial.nativeEntries | ConvertTo-Json -Depth 10 -Compress
$after=$final.nativeEntries | ConvertTo-Json -Depth 10 -Compress
$pins=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
$pinChecks=@(foreach ($pin in $pins) { [pscustomobject]@{path=$pin.relativePath;match=((Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash -ceq $pin.sha256);lengthMatch=(Get-Item -LiteralPath $pin.path -Force).Length -eq $pin.bytes} })
$sourcesBefore=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'expanded-source-pins.json') -Raw | ConvertFrom-Json
$sourcesAfter=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'final-source-pins.json') -Raw | ConvertFrom-Json
$sourceMatch=($sourcesBefore.files | ConvertTo-Json -Depth 10 -Compress) -ceq ($sourcesAfter.files | ConvertTo-Json -Depth 10 -Compress)
$diffOutput=(& git diff --check 2>&1 | Out-String)
$diffExit=$LASTEXITCODE
$head=(& git rev-parse HEAD)
$nativeHead=(& git -C native/vice rev-parse HEAD)
$indexOutput=(& git -C native/vice diff --cached --stat | Out-String)
$outsideChanges=@(& git status --porcelain=v1 -uall | Where-Object { $_.Substring(3).Trim('"') -notlike 'docs/receipts/hv/20261020T153600-plan-nativerestore-001-r4-gpt61sol/*' })
$initialGit=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'initial-git.json') -Raw | ConvertFrom-Json
$initialOutside=@($initialGit.status | Where-Object { $_.Substring(3).Trim('"') -notlike 'docs/receipts/hv/20261020T153600-plan-nativerestore-001-r4-gpt61sol/*' })
$outsideEqual=($outsideChanges -join "`n") -ceq ($initialOutside -join "`n")
$result=[ordered]@{timestampLocal=[DateTime]::Now.ToString('s');inputPinCount=$pinChecks.Count;pinChecks=$pinChecks;pinDriftCount=@($pinChecks | Where-Object { -not $_.match -or -not $_.lengthMatch }).Count;nativeContentInventoryUnchanged=$before -ceq $after;nativePathCount=$final.nativeCount;nativeHead=$nativeHead;nativeIndexClean=[string]::IsNullOrWhiteSpace($indexOutput);materializedSourceCount=$sourcesAfter.files.Count;materializedSourcesMatchPinnedBlobs=@($sourcesAfter.files | Where-Object { -not $_.match }).Count -eq 0;materializedSourceReadbackUnchanged=$sourceMatch;head=$head;headUnchanged=$head -ceq $initialGit.head;gitDiffCheckExit=$diffExit;gitDiffCheckOutput=$diffOutput;outsideReceiptGitStatusUnchanged=$outsideEqual;outsideReceiptStatus=$outsideChanges}
$result | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'final-integrity.json') -Encoding utf8
$result | ConvertTo-Json -Depth 20 | Write-Output
if ($result.pinDriftCount -or -not $result.nativeContentInventoryUnchanged -or -not $result.materializedSourceReadbackUnchanged -or -not $result.materializedSourcesMatchPinnedBlobs -or -not $result.headUnchanged -or $diffExit -ne 0 -or -not $outsideEqual) { throw 'Review integrity drift detected.' }
