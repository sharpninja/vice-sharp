[CmdletBinding(SupportsShouldProcess)]
param([int]$CompletionPercent = 3, [Nullable[int]]$EtaSeconds = $null, [switch]$VerifyPins, [switch]$NativeState)
$ErrorActionPreference = 'Stop'
$receiptRoot = $PSScriptRoot
$repoRoot = '/home/sharpninja/github/vice-sharp'
if ($VerifyPins) {
    $pins = Get-Content -LiteralPath (Join-Path $receiptRoot 'input-pins.json') -Raw | ConvertFrom-Json
    $pinResults = foreach ($pin in $pins) {
        if ([IO.Path]::GetFileName($pin.path) -eq 'AGENTS-README-FIRST.yaml') { throw 'Credential marker pin refused.' }
        $actualHash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
        [ordered]@{ path = $pin.relativePath; expectedSha256 = $pin.sha256; actualSha256 = $actualHash; expectedBytes = $pin.bytes; actualBytes = (Get-Item -LiteralPath $pin.path -Force).Length; matches = ($actualHash -eq $pin.sha256) }
    }
    if ($PSCmdlet.ShouldProcess($receiptRoot, 'Save pin verification')) {
        [ordered]@{ timestampLocal = (Get-Date -Format 's'); results = @($pinResults) } | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $receiptRoot 'pin-verification.json') -Encoding utf8NoBOM
    }
    $pinResults | ConvertTo-Json -Depth 8
}
if ($NativeState) {
    $env:GIT_OPTIONAL_LOCKS = '0'
    $nativeStatus = @(& git -C (Join-Path $repoRoot 'native/vice') status --porcelain=v1 -uall)
    $nativeStat = @(& git -C (Join-Path $repoRoot 'native/vice') diff --stat)
    $state = [ordered]@{ timestampLocal = (Get-Date -Format 's'); branch = (& git -C $repoRoot branch --show-current); head = (& git -C $repoRoot rev-parse HEAD); superprojectStatus = @(& git -C $repoRoot status --porcelain=v1 -uall); submoduleStatus = @(& git -C $repoRoot submodule status); nativeStatus = $nativeStatus; nativeDiffStat = $nativeStat; nativeStatusCount = $nativeStatus.Count; nativeTrackedCount = @($nativeStatus | Where-Object { -not $_.StartsWith('??') }).Count; nativeUntrackedCount = @($nativeStatus | Where-Object { $_.StartsWith('??') }).Count }
    if ($PSCmdlet.ShouldProcess($receiptRoot, 'Save native read-only state')) { $state | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $receiptRoot 'live-state.json') -Encoding utf8NoBOM }
    [ordered]@{ branch = $state.branch; head = $state.head; submoduleStatus = $state.submoduleStatus; nativeStatusCount = $state.nativeStatusCount; nativeTrackedCount = $state.nativeTrackedCount; nativeUntrackedCount = $state.nativeUntrackedCount; nativeDiffStat = $nativeStat } | ConvertTo-Json -Depth 8
}
[ordered]@{ timestampLocal = (Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffzzz'); completionPercent = $CompletionPercent; etaSeconds = $EtaSeconds } | ConvertTo-Json -Compress
