[CmdletBinding(SupportsShouldProcess)]
param([switch]$RunFocusedTest, [string]$ReceiptSuffix = '')
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess('Read-only workspace and allowed managed comparator test', 'Inspect')) { return }
$env:GIT_OPTIONAL_LOCKS = '0'
if ($RunFocusedTest) {
    $testArgs = @('test', 'tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj', '-c', 'Release', '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~Vic20HalfCycleComparatorTests', '--results-directory', (Join-Path $PSScriptRoot 'TestResults'), '--logger', 'trx;LogFileName=vic20-comparator.trx', '--diag', (Join-Path $PSScriptRoot 'comparator.vstest.log'))
    ('dotnet ' + ($testArgs -join ' ')) | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'comparator-command.txt')
    & dotnet @testArgs 2>&1 | Tee-Object -FilePath (Join-Path $PSScriptRoot 'comparator-output.txt')
    [ordered]@{ timestampLocal = (Get-Date -Format s); exitCode = $LASTEXITCODE } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'comparator-exit.json')
    return
}
$nativeStatus = @(git -C native/vice status --porcelain=v1 -uall)
$nativeDiff = @(git -C native/vice diff --stat)
$receipt = [ordered]@{
    timestampLocal = (Get-Date -Format s)
    branch = (git branch --show-current)
    head = (git rev-parse HEAD)
    gitlink = (git ls-tree HEAD native/vice)
    submoduleStatus = @(git submodule status)
    nativeStatusCount = $nativeStatus.Count
    nativeTrackedCount = @($nativeStatus | Where-Object { $_ -notmatch '^\?\?' }).Count
    nativeUntrackedCount = @($nativeStatus | Where-Object { $_ -match '^\?\?' }).Count
    nativeStatus = $nativeStatus
    nativeDiffStat = $nativeDiff
    nativeCachedDiff = @(git -C native/vice diff --cached --stat)
    superprojectStatus = @(git status --porcelain=v1 -uall)
    missingTargets = @(rg -n 'Target\s+(GenerateNativeSnapshotFixtures|NativeSnapshotCompatibilityTest|BuildViceNative)\b' build -g '*.cs')
    generatorExists = (Test-Path -LiteralPath tools/ViceSharp.SnapshotFixtureGenerator)
    nativeProjectCallers = @(rg -n 'ViceNativeProject|Vice[.]Native[.]proj|BuildNative' tests build native/Vice.Native.proj ViceSharp.slnx -g '*.cs' -g '*.csproj' -g '*.proj' -g '*.slnx')
    semanticCandidates = @(rg -l 'ViceNative\.|ViceNativeXvic|ViceNativeBridge|IViceNative|XvicOraclePrerequisites' tests/ViceSharp.TestHarness -g '*.cs')
}
$receipt | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('live-state' + $ReceiptSuffix + '.json'))
$receipt | ConvertTo-Json -Depth 8
$bytes = [System.IO.File]::ReadAllBytes((Join-Path (Get-Location) 'tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf'))
$fixture = [ordered]@{ bytes = $bytes.Length; magic = [Text.Encoding]::ASCII.GetString($bytes, 0, 19); modules = @() }
$offset = 58
while ($offset -lt $bytes.Length) {
    $name = [Text.Encoding]::ASCII.GetString($bytes, $offset, 16).TrimEnd([char]0)
    $size = [BitConverter]::ToUInt32($bytes, $offset + 18)
    if ($size -lt 22 -or $offset + $size -gt $bytes.Length) { throw ('Invalid module at ' + $offset) }
    $module = [ordered]@{ name = $name; version = ('{0}.{1}' -f $bytes[$offset + 16], $bytes[$offset + 17]); offset = $offset; size = $size; payloadFirstTwo = @($bytes[$offset + 22], $bytes[$offset + 23]) }
    if ($name -eq 'MAINCPU') {
        $module.clock = [BitConverter]::ToUInt64($bytes, $offset + 22)
        $module.registerBytes = @($bytes[($offset + 30)..($offset + 36)])
        $module.pc = [BitConverter]::ToUInt16($bytes, $offset + 34)
        $module.status = $bytes[$offset + 36]
    }
    $fixture.modules += $module
    $offset += $size
}
$fixture | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('fixture-inspection' + $ReceiptSuffix + '.json'))
$fixture | ConvertTo-Json -Depth 8
