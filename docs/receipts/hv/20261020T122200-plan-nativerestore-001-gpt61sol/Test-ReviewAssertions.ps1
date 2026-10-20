[CmdletBinding(SupportsShouldProcess)]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess($PSScriptRoot, 'Write source-derived review assertions without running builds')) { return }
$workspaceRoot = '/home/sharpninja/github/vice-sharp'
$env:GIT_OPTIONAL_LOCKS = '0'
$plan = [IO.File]::ReadAllText((Join-Path $workspaceRoot 'docs/plans/PLAN-NATIVERESTORE-001.md'))
$nativeProject = [xml][IO.File]::ReadAllText((Join-Path $workspaceRoot 'native/Vice.Native.proj'))
$harness = [xml][IO.File]::ReadAllText((Join-Path $workspaceRoot 'tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj'))
$solution = [IO.File]::ReadAllText((Join-Path $workspaceRoot 'ViceSharp.slnx'))
$nativeStatus = @(& git -C (Join-Path $workspaceRoot 'native/vice') status --porcelain=v1 -uall)
$bridgeFiles = @(& rg -l 'ViceNativeBridge' (Join-Path $workspaceRoot 'tests') -g '*.cs')
$consumerEvidence = foreach ($file in $bridgeFiles) {
    $content = [IO.File]::ReadAllText($file)
    [ordered]@{ path = [IO.Path]::GetRelativePath($workspaceRoot, $file); categories = @([regex]::Matches($content, '\[Trait\("Category",\s*"([^"\r\n]+)"\)\]') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique); viceFactsOrTheories = [regex]::Matches($content, '\[(ViceFact|ViceTheory)\b').Count }
}
$scripts = foreach ($file in Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter '*.ps1') {
    $tokens = $null; $parseErrors = $null
    $null = [Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$parseErrors)
    $issues = @(Invoke-ScriptAnalyzer -Path $file.FullName -Severity Warning,Error)
    [ordered]@{ path = $file.Name; parseErrorCount = @($parseErrors).Count; warningErrorCount = $issues.Count; issues = @($issues | ForEach-Object { $_.Message }); sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash }
}
$record = [ordered]@{
    timestampLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
    nativeTrackedChanges = @($nativeStatus | Where-Object { $_ -notmatch '^\?\? ' }).Count
    nativeUntrackedEntries = @($nativeStatus | Where-Object { $_ -match '^\?\? ' }).Count
    planClaims135Untracked = $plan.Contains('135 untracked entries')
    nativeProjectBuildNativeTargetPresent = @($nativeProject.Project.Target | Where-Object { $_.Name -eq 'BuildNative' }).Count -eq 1
    harnessInvokesBuildNativeTarget = @($harness.Project.Target | Where-Object { $_.Name -eq 'BuildViceNative' } | ForEach-Object { $_.MSBuild.Targets }) -contains 'BuildNative'
    nativeSafeSolutionMembership = @('NativeClockProbe', 'OracleContract', 'VsfCompatibility' | ForEach-Object { [ordered]@{ project = 'ViceSharp.' + $_ + '.Tests'; present = $solution.Contains('ViceSharp.' + $_ + '.Tests.csproj') } })
    consumerFileCount = $bridgeFiles.Count
    consumers = @($consumerEvidence)
    builderSupportsWhatIf = [IO.File]::ReadAllText((Join-Path $workspaceRoot 'native/build-vice-observer.ps1')).Contains('SupportsShouldProcess')
    equivalenceRunnerSupportsWhatIf = [IO.File]::ReadAllText((Join-Path $workspaceRoot 'tools/immutable-vice/Invoke-ObserverEquivalence.ps1')).Contains('SupportsShouldProcess')
    planNamesRealPhi2AecGate = $plan.Contains('REAL-C64-PHI2-AEC-001')
    oldInjectedHeaderHash = (Get-FileHash -LiteralPath (Join-Path $workspaceRoot 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/vice-shim-runtime.h.preserved') -Algorithm SHA256).Hash
    oldUntrackedInventoryCount = @([IO.File]::ReadAllLines((Join-Path $workspaceRoot 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-untracked-paths.txt')) | Where-Object { $_ }).Count
    scripts = @($scripts)
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'source-assertions.json'), ($record | ConvertTo-Json -Depth 15), [Text.UTF8Encoding]::new($false))
$summary = [ordered]@{ nativeTrackedChanges = $record.nativeTrackedChanges; nativeUntrackedEntries = $record.nativeUntrackedEntries; consumerFileCount = $record.consumerFileCount; harnessInvokesBuildNativeTarget = $record.harnessInvokesBuildNativeTarget; oldUntrackedInventoryCount = $record.oldUntrackedInventoryCount; builderSupportsWhatIf = $record.builderSupportsWhatIf; equivalenceRunnerSupportsWhatIf = $record.equivalenceRunnerSupportsWhatIf; reviewerScriptFindings = @($scripts | Where-Object { $_.parseErrorCount -or $_.warningErrorCount }).Count }
$summary | ConvertTo-Json -Compress
