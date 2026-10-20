[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference='Stop'
$parseTokens=$null;$parseErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis=@(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning,Error -WhatIf:$false)
if ($analysis.Count) { throw ($analysis | Out-String) }
if (-not $PSCmdlet.ShouldProcess('Requirements and TODO','Read supported primary store through wrappers')) { return }
$calls=@(
    @{label='store-layers';method='workflow.requirements.listLayers';payload=@{}},
    @{label='store-effective';method='workflow.requirements.effective';payload=@{}},
    @{label='store-mappings';method='workflow.requirements.listMappings';payload=@{frId='FR-VICE-001'}},
    @{label='store-snapshot-mappings';method='workflow.requirements.listMappings';payload=@{frId='FR-SNP-005'}},
    @{label='store-todo';method='workflow.todo.get';payload=@{id='PLAN-VICEORACLE-001'}}
)
foreach ($call in $calls) {
    $paramsPath=Join-Path $PSScriptRoot ($call.label+'.params.json')
    $call.payload | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $paramsPath -Encoding utf8
    & (Join-Path $PSScriptRoot 'Invoke-ReviewPlugin.ps1') -Operation Invoke -Method $call.method -PayloadPath $paramsPath -Label $call.label -Quiet
}
. '/home/sharpninja/github/mcpserver-codex-plugin/lib/yaml-object-mutation.ps1'
Import-McpYamlSerializer
$effective=ConvertFrom-Yaml -Yaml (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'store-effective.txt') -Raw) -AllDocuments
$ids=@('FR-VICE-001','TR-VICE-ORACLE-001','TEST-VICE-001','TR-VSFLOCKSTEP-RESUME-001','TR-CYCLE-001','FR-SNP-005','TEST-SNP-002')
function Select-ReviewRecord {
    param([object]$Value)
    if ($Value -is [Collections.IDictionary]) {
        if ($Value.Contains('id') -and [string]$Value['id'] -in $ids) { Write-Output $Value;return }
        foreach ($key in $Value.Keys) { Select-ReviewRecord $Value[$key] }
    } elseif ($Value -is [Collections.IEnumerable] -and $Value -isnot [string]) { foreach ($entry in $Value) { Select-ReviewRecord $entry } }
}
$selected=@(Select-ReviewRecord $effective)
$selected | ConvertTo-Json -Depth 80 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'store-effective.selected.json') -Encoding utf8
$selected | ConvertTo-Json -Depth 80 | Write-Output
foreach ($label in @('store-layers','store-mappings','store-snapshot-mappings','store-todo')) { Write-Output $label;Get-Content -LiteralPath (Join-Path $PSScriptRoot ($label+'.txt')) -Raw | Write-Output }
