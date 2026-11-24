#Requires -Version 7.4
<#
.SYNOPSIS
    PLAN-NUKETESTS-001 step 6 on PAYTON-LEGION2 (the requirement writer): capture plan section 3 in the
    requirement store through the mcpserver-claude-code-plugin wrapper.
      - TR-BUILD-001: new criterion ac-12 (exact plan text, unsatisfied); description gains BuildTest in the
        Technical Specification section 1 target list, a BuildTest bullet in section 4 (wording composed by
        the agent from ac-12; the plan gives intent only), item 12 in the description's Acceptance Criteria
        list, and the exact Verification Method sentence from the plan. Every other line is unchanged.
      - TEST-BUILD-001 (verified absent first): "Nuke build tests", the plan's four criteria verbatim,
        unsatisfied, with the plan's traceability in the description.
    Before snapshot, write, full readback and comparison for each; journal in OutDir. -WhatIf writes only
    the planned payloads.
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'WorkspacePath', Justification = 'Used by nested function Invoke-Plugin through script scope; the analyzer does not trace nested-function use.')]
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$OutDir,
    [string]$WorkspacePath = 'F:\GitHub\vice-sharp',
    [string]$PluginRoot = (Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver\1.119.0')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module powershell-yaml -MinimumVersion 0.4.12 -ErrorAction Stop
New-Item -ItemType Directory -Force -Path $OutDir -WhatIf:$false | Out-Null
$journal = Join-Path $OutDir 'journal.jsonl'
$wrapper = Join-Path $PluginRoot 'lib\Invoke-McpPlugin.ps1'

function Write-Journal { param([hashtable]$Entry) $Entry['at'] = Get-Date -Format s; ($Entry | ConvertTo-Json -Depth 20 -Compress) | Add-Content -LiteralPath $journal -Encoding utf8NoBOM -WhatIf:$false }

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [switch]$AllowError)
    $pp = Join-Path $OutDir ('params-{0}.yaml' -f [guid]::NewGuid().ToString('N'))
    ConvertTo-Yaml -Data $Params | Out-File -LiteralPath $pp -Encoding utf8NoBOM -WhatIf:$false
    $env:CLAUDE_PLUGIN_ROOT = $PluginRoot; $env:PLUGIN_AGENT_NAME = 'Claude'
    try { $raw = & pwsh.exe -NoProfile -NonInteractive -File $wrapper -Command Invoke -Method $Method -ParamsPath $pp -WorkspacePath $WorkspacePath 2>&1; $exit = $LASTEXITCODE }
    finally { Remove-Item Env:CLAUDE_PLUGIN_ROOT, Env:PLUGIN_AGENT_NAME -ErrorAction SilentlyContinue; Remove-Item -LiteralPath $pp -ErrorAction SilentlyContinue -WhatIf:$false }
    $text = $raw | Out-String
    $doc = $null
    try { $doc = ConvertFrom-Yaml -Yaml ($text -replace '(?m)^---\s*$', '') -Ordered } catch { $doc = $null }
    if ($AllowError) { return [pscustomobject]@{ Exit = $exit; Doc = $doc; Text = $text } }
    if ($exit -ne 0 -or $null -eq $doc -or $doc.type -ne 'result') { throw "$Method failed (exit $exit)`n$text" }
    return $doc.payload.result
}

function ConvertTo-Plain { param($Value) return ($Value | ConvertTo-Json -Depth 30 | ConvertFrom-Json) }

$ac12 = 'Tests of Nuke build code live in `build.tests/ViceSharp.Build.Tests.csproj` beside `build/`, outside `ViceSharp.slnx`, and run through `nuke BuildTest`. No product test project compiles or tests files from `build/`. `BuildTest` fails when it selects zero tests or any selected test does not pass, and host-specific cases are excluded by host filter, never skipped.'
$targetListOld = '`GitCommit`, `Test`, `CiTest`, `DeterminismTest`,'
$targetListNew = '`GitCommit`, `Test`, `CiTest`, `BuildTest`, `DeterminismTest`,'
$section4Anchor = '   - `CiTest`: Runs the same category filter and stages hash-pinned CI ROMs (`EnsureCiRomRoot`).'
$section4Bullet = '   - `BuildTest`: Runs the Nuke build tests in `build.tests/ViceSharp.Build.Tests.csproj` (outside `ViceSharp.slnx`, so `Test` and `CiTest` never run them) and writes a TRX under `TestResults/build-tests/<yyyyMMddTHHmmss>/`. Host-specific cases are excluded by host filter (`Host!=Linux` on non-Linux hosts), never skipped. Fails when zero tests are selected or any selected test does not pass.'
$acListAnchor = '11. A failed update cannot activate an incomplete or non-executable payload. Previous entrypoints and the selected release remain usable after a pre-activation failure. Unsafe paths and unmanaged destinations fail with actionable errors instead of being overwritten.'
$verifyAnchor = '- User-local deployment: run `pwsh ./tests/packaging/Verify-DeployYay.ps1`.'
$verifyLine = '- `./build.sh BuildTest` / `./build.ps1 BuildTest` TRX on each host, with the host-excluded count.'

# TR-BUILD-001 plan.
$tr = ConvertTo-Plain (Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-BUILD-001' })
Write-Journal @{ phase = 'before'; id = 'TR-BUILD-001'; record = $tr }
if (@($tr.acceptanceCriteria | Where-Object id -eq 'ac-12').Count) { throw 'TR-BUILD-001 already has ac-12; stop (capture already done or conflicting writer).' }
$desc = [string]$tr.description
foreach ($anchor in $targetListOld, $section4Anchor, $acListAnchor, $verifyAnchor) {
    $hits = ([regex]::Matches($desc, [regex]::Escape($anchor))).Count
    if ($hits -ne 1) { throw "TR-BUILD-001 description anchor found $hits times (want 1): $anchor" }
}
$newDesc = $desc.Replace($targetListOld, $targetListNew)
$newDesc = $newDesc.Replace($section4Anchor, $section4Anchor + "`n" + $section4Bullet)
$newDesc = $newDesc.Replace($acListAnchor, $acListAnchor + "`n12. " + $ac12)
$lines = $newDesc -split "`n"
$vi = [Array]::FindIndex($lines, [Predicate[string]] { param($l) $l.StartsWith($verifyAnchor, [StringComparison]::Ordinal) })
$newDesc = (@($lines[0..$vi]) + @($verifyLine) + @($lines[($vi + 1)..($lines.Count - 1)])) -join "`n"
$newCriteria = @($tr.acceptanceCriteria | ForEach-Object { $o = [ordered]@{}; foreach ($p in $_.PSObject.Properties) { $o[$p.Name] = $p.Value }; $o }) + @([ordered]@{ id = 'ac-12'; text = $ac12; isSatisfied = $false })
$trUpdate = [ordered]@{ id = 'TR-BUILD-001'; description = $newDesc; acceptanceCriteria = $newCriteria }

# TEST-BUILD-001 plan.
$probe = Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-BUILD-001' } -AllowError
$exists = ($probe.Exit -eq 0 -and $null -ne $probe.Doc -and $probe.Doc.type -eq 'result' -and $null -ne $probe.Doc.payload.result -and [string]$probe.Doc.payload.result.id -eq 'TEST-BUILD-001')
Write-Journal @{ phase = 'probe'; id = 'TEST-BUILD-001'; exists = $exists; exit = $probe.Exit; response = $probe.Text }
if ($exists) { throw 'TEST-BUILD-001 already exists; choose the next free id.' }
$testCreate = [ordered]@{
    id                 = 'TEST-BUILD-001'
    title              = 'Nuke build tests'
    description        = 'The Nuke build tests in `build.tests/ViceSharp.Build.Tests.csproj` run through `BuildTest` on every host (PLAN-NUKETESTS-001). Traceability: TR-BUILD-001 ac-10, ac-11, ac-12; TR-PLAT-001 ac-8.'
    priority           = 'high'
    area               = 'BUILD'
    acceptanceCriteria = @(
        [ordered]@{ id = 'ac-1'; text = 'The 13 user-local deployment cases pass on Linux through `BuildTest` and prove TR-BUILD-001 ac-10 and ac-11.'; isSatisfied = $false }
        [ordered]@{ id = 'ac-2'; text = 'The completion-manifest cases pass on every host through `BuildTest`.'; isSatisfied = $false }
        [ordered]@{ id = 'ac-3'; text = 'The placement guards of PLAN-NUKETESTS-001 step 2 pass.'; isSatisfied = $false }
        [ordered]@{ id = 'ac-4'; text = 'The Windows `BuildTest` run selects no Linux-host case and reports the excluded count.'; isSatisfied = $false }
    )
}
[ordered]@{ trUpdate = $trUpdate; testCreate = $testCreate } | ConvertTo-Json -Depth 20 | Out-File -LiteralPath (Join-Path $OutDir 'plan.json') -Encoding utf8NoBOM -WhatIf:$false
Write-Information ('TR-BUILD-001: criteria {0} -> {1}; description {2} -> {3} chars' -f @($tr.acceptanceCriteria).Count, $newCriteria.Count, $desc.Length, $newDesc.Length) -InformationAction Continue

if (-not $PSCmdlet.ShouldProcess('TR-BUILD-001 and TEST-BUILD-001', 'Update TR and create TEST')) { return }

# Write and verify TR.
$null = Invoke-Plugin -Method 'workflow.requirements.updateTr' -Params ([hashtable]$trUpdate)
$trAfter = ConvertTo-Plain (Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-BUILD-001' })
Write-Journal @{ phase = 'after'; id = 'TR-BUILD-001'; record = $trAfter }
$problems = [System.Collections.Generic.List[string]]::new()
if (([string]$trAfter.description -replace "`r`n", "`n") -cne $newDesc) { $problems.Add('description') }
if ((ConvertTo-Json @($trAfter.acceptanceCriteria) -Depth 10 -Compress) -cne (ConvertTo-Json @(ConvertTo-Plain $newCriteria) -Depth 10 -Compress)) { $problems.Add('acceptanceCriteria') }
foreach ($f in 'title', 'status', 'priority', 'area', 'subarea', 'notes', 'scopeStartLayerKey') {
    $b = if ($tr.PSObject.Properties[$f]) { ConvertTo-Json $tr.$f -Compress } else { '<absent>' }
    $a = if ($trAfter.PSObject.Properties[$f]) { ConvertTo-Json $trAfter.$f -Compress } else { '<absent>' }
    if ($a -cne $b) { $problems.Add("stable $f") }
}
Write-Journal @{ phase = 'verify'; id = 'TR-BUILD-001'; ok = ($problems.Count -eq 0); problems = @($problems) }
if ($problems.Count) { throw "TR-BUILD-001 readback mismatch: $($problems -join ', ')" }

# Create and verify TEST.
$null = Invoke-Plugin -Method 'workflow.requirements.createTest' -Params ([hashtable]$testCreate)
$testAfter = ConvertTo-Plain (Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-BUILD-001' })
Write-Journal @{ phase = 'after'; id = 'TEST-BUILD-001'; record = $testAfter }
$tp = [System.Collections.Generic.List[string]]::new()
foreach ($f in 'id', 'title', 'description', 'priority', 'area') { if ([string]$testAfter.$f -cne [string]$testCreate[$f]) { $tp.Add($f) } }
if ((ConvertTo-Json @($testAfter.acceptanceCriteria) -Depth 10 -Compress) -cne (ConvertTo-Json @(ConvertTo-Plain $testCreate.acceptanceCriteria) -Depth 10 -Compress)) { $tp.Add('acceptanceCriteria') }
if ($testAfter.status -in 'completed', 'closed') { $tp.Add('status done') }
Write-Journal @{ phase = 'verify'; id = 'TEST-BUILD-001'; ok = ($tp.Count -eq 0); problems = @($tp); status = $testAfter.status }
if ($tp.Count) { throw "TEST-BUILD-001 readback mismatch: $($tp -join ', ')" }
Write-Information ('verified: TR-BUILD-001 {0} criteria, TEST-BUILD-001 status {1}, 4 criteria unsatisfied' -f @($trAfter.acceptanceCriteria).Count, $testAfter.status) -InformationAction Continue
