#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only export of the workspace requirement store (FR, TR, TEST, mappings) through
    the mcpserver-claude-code-plugin wrapper. Writes the raw wrapper YAML per type plus
    a parsed JSON copy and a counts summary into OutDir. Never writes to the store.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OutDir,
    [string]$WorkspacePath = 'F:\GitHub\vice-sharp',
    [string]$PluginRoot = (Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver\1.119.0')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module powershell-yaml -MinimumVersion 0.4.12 -ErrorAction Stop

$wrapper = Join-Path $PluginRoot 'lib\Invoke-McpPlugin.ps1'
if (-not (Test-Path -LiteralPath $wrapper)) { throw "Plugin wrapper not found: $wrapper" }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$methods = [ordered]@{
    fr      = 'workflow.requirements.listFr'
    tr      = 'workflow.requirements.listTr'
    test    = 'workflow.requirements.listTest'
    mapping = 'workflow.requirements.listMappings'
}

$summary = [ordered]@{ exportedAt = (Get-Date -Format s); machine = $env:COMPUTERNAME; pluginRoot = $PluginRoot; types = [ordered]@{} }

$env:CLAUDE_PLUGIN_ROOT = $PluginRoot
$env:PLUGIN_AGENT_NAME = 'Claude'
try {
    foreach ($type in $methods.Keys) {
        $rawPath = Join-Path $OutDir "$type.raw.yaml"
        $raw = & pwsh.exe -NoProfile -NonInteractive -File $wrapper -Command Invoke -Method $methods[$type] -WorkspacePath $WorkspacePath 2>&1
        $exit = $LASTEXITCODE
        $raw | Out-File -LiteralPath $rawPath -Encoding utf8NoBOM
        if ($exit -ne 0) { throw "$($methods[$type]) failed with exit $exit; see $rawPath" }
        $doc = (ConvertFrom-Yaml -Yaml (($raw | Out-String) -replace '(?m)^---\s*$', '') -Ordered)
        $items = @($doc.payload.result.items)
        $items | ConvertTo-Json -Depth 20 | Out-File -LiteralPath (Join-Path $OutDir "$type.json") -Encoding utf8NoBOM
        $summary.types[$type] = [ordered]@{
            method     = $methods[$type]
            items      = $items.Count
            totalCount = $doc.payload.result.totalCount
            rawSha256  = (Get-FileHash -LiteralPath $rawPath -Algorithm SHA256).Hash
        }
    }
}
finally {
    Remove-Item Env:CLAUDE_PLUGIN_ROOT, Env:PLUGIN_AGENT_NAME -ErrorAction SilentlyContinue
}

$summary | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $OutDir 'export-summary.json') -Encoding utf8NoBOM
$summary | ConvertTo-Json -Depth 6
