[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference='Stop'
$parseTokens=$null;$parseErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis=@(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning,Error -WhatIf:$false)
if ($analysis.Count) { throw ($analysis | Out-String) }
Write-Output 'Parser errors=0; analyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess('Vic20HalfCycleComparatorTests','Run only the authorized managed filter with existing Release binary')) { return }
$project='tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj'
$testArgs=@('test',$project,'-c','Release','--no-build','--no-restore','--nologo','--filter','FullyQualifiedName~Vic20HalfCycleComparatorTests','--logger','trx;LogFileName=comparator.trx','--results-directory',$PSScriptRoot,'--diag',(Join-Path $PSScriptRoot 'comparator-vstest.diag.log'))
[ordered]@{timestampLocal=[DateTime]::Now.ToString('s');command='dotnet';arguments=$testArgs;binarySha256=(Get-FileHash -LiteralPath tests/ViceSharp.TestHarness/bin/Release/net10.0/ViceSharp.TestHarness.dll -Algorithm SHA256).Hash} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'comparator-command.json') -Encoding utf8
$output=(& dotnet @testArgs 2>&1 | Out-String)
$exitCode=$LASTEXITCODE
$output | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'comparator-output.txt') -Encoding utf8
Write-Output $output
[ordered]@{timestampLocal=[DateTime]::Now.ToString('s');exitCode=$exitCode;configuration='Release';filter='FullyQualifiedName~Vic20HalfCycleComparatorTests'} | ConvertTo-Json -Compress | Write-Output
