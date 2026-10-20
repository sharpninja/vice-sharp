[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
if (-not $IsLinux) { throw 'This review permits this managed TestHarness run on Linux only.' }
$testProject = '/home/sharpninja/github/vice-sharp/tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj'
$resultsRoot = Join-Path $PSScriptRoot 'TestResults'
$testArgs = @('test', $testProject, '-c', 'Release', '--filter', 'FullyQualifiedName~Vic20HalfCycleComparatorTests', '--results-directory', $resultsRoot, '--logger', 'trx;LogFileName=vic20-comparator.trx', '--diag', (Join-Path $resultsRoot 'vstest.diag.log'), '--blame-hang', '--blame-hang-timeout', '5m')
if (-not $PSCmdlet.ShouldProcess(($testArgs -join ' '), 'Run the explicitly permitted managed comparator test')) { return }
$null = New-Item -ItemType Directory -Path $resultsRoot -Force
$testArgs | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'comparator-command.json') -Encoding utf8
& dotnet @testArgs 2>&1 | Tee-Object -FilePath (Join-Path $PSScriptRoot 'comparator-test.log')
$testExit = $LASTEXITCODE
[ordered]@{ timestampLocal = (Get-Date).ToString('s'); exitCode = $testExit; command = 'dotnet'; arguments = $testArgs } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'comparator-test-result.json') -Encoding utf8
Write-Output ('Comparator test exit code: ' + $testExit)
