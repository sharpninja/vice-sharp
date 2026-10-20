[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
$baseCategories = @('Determinism','AiReview','ParityPending','ParityLegacy','Integration')
$ordinaryExcluded = @($baseCategories + 'ShimLegacy' + 'VsfSlice1Real')
$cases = @(
    @{ id = 'ordinary-managed'; categories = @() },
    @{ id = 'pure-managed-fake'; categories = @() },
    @{ id = 'contract'; categories = @('VsfSlice1Contract') },
    @{ id = 'legacy-direct'; categories = @('ShimLegacy') },
    @{ id = 'legacy-provider'; categories = @('ShimLegacy') },
    @{ id = 'real-c64'; categories = @('VsfSlice1Real') },
    @{ id = 'real-xvic-blocked'; categories = @('VsfSlice1Real') },
    @{ id = 'contract-real-double-trait'; categories = @('VsfSlice1Contract','VsfSlice1Real') }
)
$projected = foreach ($case in $cases) {
    $ordinary = @($case.categories | Where-Object { $ordinaryExcluded -contains $_ }).Count -eq 0
    [pscustomobject]@{ id = $case.id; categories = $case.categories; Test = $ordinary; CiTest = $ordinary; Cumulative = ($case.categories -contains 'VsfSlice1Real') }
}
$result = [pscustomobject]@{ timestampLocal = (Get-Date).ToString('s'); kind = 'Model of the revision-7 selector contract; not execution of unimplemented Nuke targets'; ordinaryFilter = 'Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy&Category!=Integration&Category!=ShimLegacy&Category!=VsfSlice1Real'; cumulativeFilter = 'Category=VsfSlice1Real'; cases = @($projected) }
if ($PSCmdlet.ShouldProcess($PSScriptRoot, 'Save selector contract projection')) { $result | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'selector-projection.json') -Encoding utf8 }
$result | ConvertTo-Json -Depth 10
