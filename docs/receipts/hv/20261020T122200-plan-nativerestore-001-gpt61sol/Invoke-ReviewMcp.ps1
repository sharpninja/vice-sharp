[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Start', 'Status', 'Invoke', 'Complete', 'StopGate', 'Trust')]
    [string]$Action,
    [string]$Method,
    [string]$ParametersPath,
    [string]$ResponsePath,
    [string]$OutputName = 'mcp-last-output.txt'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$pluginRoot = '/home/sharpninja/github/mcpserver-codex-plugin'
$workspaceRoot = '/home/sharpninja/github/vice-sharp'
$reviewCache = Join-Path $PSScriptRoot 'plugin-cache'
if (-not $PSCmdlet.ShouldProcess($workspaceRoot, "MCP reviewer operation: $Action $Method")) { return }

# Process-local isolation for this reviewer. No host settings or historical queues change.
$env:CODEX_PLUGIN_ROOT = $pluginRoot
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:MCP_PLUGIN_HOST = 'codex'
$env:PLUGIN_AGENT_NAME = 'Codex'
$env:MCP_AGENT_NAME = 'Codex'
$env:MCP_MODEL = 'gpt-6.1-sol'
$env:MCP_MODEL_EFFORT = 'max'
$env:MCP_WORKSPACE_PATH = $workspaceRoot
$env:MCPSERVER_WORKSPACE_PATH = $workspaceRoot
$env:MCP_CACHE_DIR_OVERRIDE = $reviewCache
$env:MCP_FAILSAFE_DIR = Join-Path $reviewCache 'failsafe'
$env:MCPSERVER_FAILSAFE_DIR = $env:MCP_FAILSAFE_DIR
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'
$wrapper = Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1'
$wrapperOptions = @{ PluginRoot = $pluginRoot; WorkspacePath = $workspaceRoot; CacheRoot = $reviewCache }

try {
$outputText = switch ($Action) {
    'Trust' {
        . (Join-Path $pluginRoot 'lib/marker-resolver.ps1')
        [ordered]@{ markerSignatureValid = (Test-MarkerSignature -MarkerFile (Join-Path $workspaceRoot 'AGENTS-README-FIRST.yaml')); healthNonceVerified = $false; note = 'Local signature diagnosis only. No endpoint called.' } | ConvertTo-Json -Compress
    }
    'Start' {
        $env:MCP_SESSION_SUFFIX = 'hv-plan-nativerestore-001'
        $env:MCP_SESSION_ID = $null
        & pwsh -NoProfile -NonInteractive -File (Join-Path $pluginRoot 'lib/session-start.ps1') $workspaceRoot
        if ($LASTEXITCODE -ne 0) { throw "Plugin session-start returned $LASTEXITCODE" }
        & $wrapper @wrapperOptions -Command Status
    }
    'Status' { & $wrapper @wrapperOptions -Command Status }
    'Invoke' {
        $parameters = if ($ParametersPath) { Get-Content -LiteralPath $ParametersPath -Raw | ConvertFrom-Json -AsHashtable } else { @{} }
        & $wrapper @wrapperOptions -Command Invoke -Method $Method -ParamsObject $parameters
    }
    'Complete' { & $wrapper @wrapperOptions -Command CompleteTurn -ResponsePath $ResponsePath }
    'StopGate' { & pwsh -NoProfile -NonInteractive -File (Join-Path $pluginRoot 'lib/stop-gate.ps1') }
}
$text = $outputText -join [Environment]::NewLine
[IO.File]::WriteAllText((Join-Path $PSScriptRoot $OutputName), $text, [Text.UTF8Encoding]::new($false))
Write-Output $text
} catch {
    $errorRecord = [ordered]@{ action = $Action; method = $Method; error = $_.Exception.Message; timestampLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz'); persisted = $false }
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot $OutputName), ($errorRecord | ConvertTo-Json -Depth 10), [Text.UTF8Encoding]::new($false))
    throw
}
