[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
$parseTokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning, Error -WhatIf:$false)
if ($analysis.Count -gt 0) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess('Effective requirements, mappings and TODO', 'Read through supported plugin and retain receipt')) { return }
$calls = @(
    @{label='store-layers';method='workflow.requirements.listLayers';payload=@{}},
    @{label='store-effective';method='workflow.requirements.effective';payload=@{}},
    @{label='store-mappings';method='workflow.requirements.listMappings';payload=@{frId='FR-VICE-001'}},
    @{label='store-todo';method='workflow.todo.get';payload=@{id='PLAN-VICEORACLE-001'}}
)
foreach ($call in $calls) {
    $payloadPath = Join-Path $PSScriptRoot ($call.label + '.params.json')
    $call.payload | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $payloadPath -Encoding utf8
    & (Join-Path $PSScriptRoot 'Invoke-ReviewPlugin.ps1') -Operation Invoke -Method $call.method -PayloadPath $payloadPath -Label $call.label -Quiet
}
. '/home/sharpninja/github/mcpserver-codex-plugin/lib/yaml-object-mutation.ps1'
Import-McpYamlSerializer
$effective = ConvertFrom-Yaml -Yaml (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'store-effective.txt') -Raw) -AllDocuments
$effective | ConvertTo-Json -Depth 80 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'store-effective.parsed.json') -Encoding utf8
$ids = @('FR-VICE-001','TR-VICE-ORACLE-001','TEST-VICE-001','TR-VSFLOCKSTEP-RESUME-001','TR-CYCLE-001')
function Select-RequiredRecord {
    param([object]$Value)
    if ($Value -is [System.Collections.IDictionary]) {
        if ($Value.Contains('id') -and [string]$Value['id'] -in $ids) { Write-Output $Value; return }
        foreach ($key in $Value.Keys) { Select-RequiredRecord -Value $Value[$key] }
    } elseif ($Value -is [System.Collections.IEnumerable] -and $Value -isnot [string]) {
        foreach ($entry in $Value) { Select-RequiredRecord -Value $entry }
    }
}
$selected = @(Select-RequiredRecord -Value $effective)
$selected | ConvertTo-Json -Depth 80 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'store-effective.selected.json') -Encoding utf8
Write-Output ('Selected effective records: ' + $selected.Count)
$selected | ConvertTo-Json -Depth 80 | Write-Output
foreach ($label in @('store-layers','store-mappings','store-todo')) {
    Write-Output ('WRAPPER READBACK: ' + $label)
    Get-Content -LiteralPath (Join-Path $PSScriptRoot ($label + '.txt')) -Raw | Write-Output
}
