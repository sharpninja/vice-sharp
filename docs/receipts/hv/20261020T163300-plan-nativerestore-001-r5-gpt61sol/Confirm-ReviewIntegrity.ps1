[CmdletBinding(SupportsShouldProcess)]
param([string]$ReceiptSuffix = '')
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess('Pinned inputs and read-only native content', 'Verify review integrity and diff whitespace')) { return }
$env:GIT_OPTIONAL_LOCKS = '0'
$pins = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json)
$verified = foreach ($pin in $pins) {
    if ($pin.path -match 'AGENTS-README-FIRST|[.]creds|api.?key|plugin-cache') { throw 'Credential-path guard stopped pin inspection' }
    $item = Get-Item -LiteralPath $pin.path -Force
    $actualHash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
    [ordered]@{ relativePath = $pin.relativePath; actualSha256 = $actualHash; expectedSha256 = $pin.sha256; actualBytes = $item.Length; expectedBytes = $pin.bytes; matches = ($actualHash -eq $pin.sha256 -and $item.Length -eq $pin.bytes) }
}
$initial = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'live-state.json') -Raw | ConvertFrom-Json
$baseline = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'native-content-bracket.json') -Raw | ConvertFrom-Json
$status = @(git -C native/vice status --porcelain=v1 -uall)
$entries = foreach ($line in $status) {
    $relative = $line.Substring(3)
    $file = Join-Path 'native/vice' $relative
    $info = Get-Item -LiteralPath $file -Force
    [ordered]@{ status = $line.Substring(0, 2); path = $relative; length = $info.Length; sha256 = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash }
}
$nativeChanges = foreach ($entry in $entries) {
    $old = @($baseline.entries | Where-Object path -CEQ $entry.path)
    if ($old.Count -ne 1 -or $old[0].status -cne $entry.status -or $old[0].length -ne $entry.length -or $old[0].sha256 -ne $entry.sha256) { $entry }
}
$deleted = @($baseline.entries | Where-Object path -CNotIn @($entries.path))
$nativeStatusUnchanged = (($initial.nativeStatus -join [Environment]::NewLine) -ceq ($status -join [Environment]::NewLine))
$head = git rev-parse HEAD
$nativeHead = git -C native/vice rev-parse HEAD
$diff = @(git -C native/vice diff --stat)
$cached = @(git -C native/vice diff --cached --stat)
$statusBefore = @($initial.superprojectStatus | Where-Object { $_ -notlike '*20261020T163300-plan-nativerestore-001-r5-gpt61sol/*' })
$statusNow = @(git status --porcelain=v1 -uall | Where-Object { $_ -notlike '*20261020T163300-plan-nativerestore-001-r5-gpt61sol/*' })
$outsideUnchanged = (($statusBefore -join [Environment]::NewLine) -ceq ($statusNow -join [Environment]::NewLine))
$semanticHarness = @(rg -l 'ViceNative\.|ViceNativeXvic|ViceNativeBridge|IViceNative|XvicOraclePrerequisites' tests/ViceSharp.TestHarness -g '*.cs')
$semanticAllTests = @(rg -l 'ViceNative.|ViceNativeXvic|ViceNativeBridge|IViceNative|XvicOraclePrerequisites' tests -g '*.cs')
$whitespace = @(git diff --check 2>&1)
$diffExit = $LASTEXITCODE
$report = [ordered]@{
    timestampLocal = (Get-Date -Format s)
    pinCount = $pins.Count
    pinMatches = @($verified | Where-Object matches).Count
    pins = @($verified)
    checkoutHead = $head
    checkoutUnchanged = ($head -eq $initial.head)
    nativeHead = $nativeHead
    nativeStatusUnchanged = $nativeStatusUnchanged
    nativeStatusCount = $status.Count
    nativeDiffStat = $diff
    nativeIndexEmpty = ($cached.Count -eq 0)
    nativeComparedEntries = $entries.Count
    nativeContentBaselineMeasuredAt = $baseline.timestampLocal
    changedNativeEntries = @($nativeChanges)
    deletedNativeEntries = @($deleted)
    statusOutsideReceiptUnchanged = $outsideUnchanged
    semanticHarnessEscapedDotCandidateCount = $semanticHarness.Count
    semanticAllTestsUnescapedDotCandidateCount = $semanticAllTests.Count
    semanticAllTestsCandidates = $semanticAllTests
    gitDiffCheckExitCode = $diffExit
    gitDiffCheckOutput = $whitespace
}
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('review-integrity' + $ReceiptSuffix + '.json'))
[ordered]@{ timestampLocal = $report.timestampLocal; pinsMatch = ($report.pinMatches -eq $report.pinCount); pinCount = $report.pinCount; headUnchanged = $report.checkoutUnchanged; nativeStatusUnchanged = $nativeStatusUnchanged; nativeContentCompared = $entries.Count; nativeContentChanges = @($nativeChanges).Count; nativeDeleted = $deleted.Count; outsideStatusUnchanged = $outsideUnchanged; semanticHarnessCandidates = $semanticHarness.Count; semanticAllTestsCandidates = $semanticAllTests.Count; gitDiffCheckExitCode = $diffExit } | ConvertTo-Json -Compress
if ($report.pinMatches -ne $pins.Count -or -not $nativeStatusUnchanged -or @($nativeChanges).Count -or $deleted.Count -or -not $outsideUnchanged -or $diffExit -ne 0 -or $head -ne $initial.head) { throw 'Review integrity mismatch; inspect retained report' }
