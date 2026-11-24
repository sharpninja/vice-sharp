<#
.SYNOPSIS
    plugin-env.ps1 - host knob defaults for the shared PowerShell runtime.
.DESCRIPTION
    Maps host-specific environment variables onto the neutral knob surface
    consumed by the core libraries. Dot-source after setting
    $env:MCP_PLUGIN_HOST (claude-code | cowork | codex | copilot | grok).
    Keep host defaults in this file so generated wrappers stay minimal.
#>

$script:PluginEnvScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Get-PluginCacheVersionDriftMessage {
    param([string]$ConfiguredRoot)
    return "MCP plugin version-drift: configured plugin root '$ConfiguredRoot' is missing and no replacement cache was found."
}

function Resolve-PluginCacheOrVersionDrift {
    param(
        [string]$ConfiguredRoot,
        [string]$ReplacementRoot
    )
    if ($ConfiguredRoot -and (Test-Path -LiteralPath $ConfiguredRoot -PathType Container)) {
        return $ConfiguredRoot
    }
    if ($ReplacementRoot -and (Test-Path -LiteralPath $ReplacementRoot -PathType Container)) {
        return $ReplacementRoot
    }
    throw (Get-PluginCacheVersionDriftMessage -ConfiguredRoot $ConfiguredRoot)
}
$host_ = if ($env:MCP_PLUGIN_HOST) { $env:MCP_PLUGIN_HOST } else { 'claude-code' }
$env:MCP_PLUGIN_HOST = $host_

if ((Get-Variable -Name McpPluginEnvLoadedForHost -Scope Script -ErrorAction SilentlyContinue) -and ($script:McpPluginEnvLoadedForHost -eq $host_)) { return }
$script:McpPluginEnvLoadedForHost = $host_
$env:MCP_PLUGIN_ENV_LOADED = '1'

switch ($host_) {
    { $_ -in 'claude', 'claude-code' } {
        $agent = 'ClaudeCode'; $model = 'claude'; $tag = 'claude-code'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:CLAUDE_PLUGIN_ROOT)
        $startChain = @($env:CLAUDE_PROJECT_DIR)
    }
    { $_ -in 'cowork', 'claude-cowork' } {
        $agent = 'ClaudeCowork'; $model = 'claude'; $tag = 'claude-cowork'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:CLAUDE_PLUGIN_ROOT)
        $startChain = @($env:COWORK_WORKSPACE_PATH, $env:CLAUDE_COWORK_WORKSPACE_PATH, $env:CLAUDE_PROJECT_DIR, $env:MCPSERVER_WORKSPACE_PATH, $env:MCP_WORKSPACE_PATH)
    }
    'codex' {
        $agent = 'Codex'; $model = 'codex'; $tag = 'codex'; $outputMode = 'cli'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:CODEX_PLUGIN_ROOT)
        $startChain = @($env:CODEX_CWD, $env:CODEX_WORKSPACE_PATH, $env:CODEX_PROJECT_DIR, $env:MCPSERVER_WORKSPACE_PATH, $env:MCP_WORKSPACE_PATH)
    }
    'copilot' {
        $agent = 'Copilot'; $model = 'copilot'; $tag = 'copilot'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:PLUGIN_ROOT, $env:CLAUDE_PLUGIN_ROOT)
        $startChain = @($env:COPILOT_WORKSPACE_PATH, $env:COPILOT_PROJECT_DIR, $env:MCPSERVER_WORKSPACE_PATH, $env:MCP_WORKSPACE_PATH)
    }
    { $_ -in 'grok', 'grok-bot', 'grokbot' } {
        $agent = 'GrokBot'; $model = 'grok'; $tag = 'grok'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:GROK_PLUGIN_ROOT, $env:PLUGIN_ROOT, $env:CLAUDE_PLUGIN_ROOT)
        $startChain = @($env:GROK_WORKSPACE_PATH, $env:GROK_PROJECT_DIR, $env:CLAUDE_PROJECT_DIR, $env:MCPSERVER_WORKSPACE_PATH, $env:MCP_WORKSPACE_PATH)
    }
    'cline' {
        $agent = 'Cline'; $model = 'cline'; $tag = 'cline'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:CLINE_PLUGIN_ROOT, $env:PLUGIN_ROOT)
        $startChain = @($env:CLINE_WORKSPACE_PATH, $env:MCPSERVER_WORKSPACE_PATH, $env:MCP_WORKSPACE_PATH)
    }
    'cline-v2' {
        $agent = 'Cline'; $model = 'cline'; $tag = 'cline-v2'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:CLINE_PLUGIN_ROOT, $env:PLUGIN_ROOT)
        $startChain = @($env:CLINE_WORKSPACE_PATH, $env:MCPSERVER_WORKSPACE_PATH, $env:MCP_WORKSPACE_PATH)
    }
    'opencode' {
        $agent = 'OpenCode'; $model = 'opencode'; $tag = 'opencode'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT, $env:OPENCODE_PLUGIN_ROOT, $env:PLUGIN_ROOT)
        $startChain = @($env:OPENCODE_WORKSPACE_PATH, $env:MCPSERVER_WORKSPACE_PATH, $env:MCP_WORKSPACE_PATH)
    }
    default {
        $agent = 'Codex'; $model = 'codex'; $tag = 'mcpserver'; $outputMode = 'hook'
        $rootChain = @($env:MCP_PLUGIN_ROOT)
        $startChain = @($env:MCPSERVER_WORKSPACE_PATH, $env:MCP_WORKSPACE_PATH)
    }
}

$env:PLUGIN_AGENT_DEFAULT = $agent
$env:PLUGIN_MODEL_DEFAULT = $model
$env:PLUGIN_TAG = $tag
$env:MCP_HOOK_OUTPUT_MODE = $outputMode

if (-not $env:MCP_PLUGIN_ROOT -or -not (Test-Path -LiteralPath $env:MCP_PLUGIN_ROOT -PathType Container)) {
    $resolvedRoot = $rootChain | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Container) } | Select-Object -First 1
    if (-not $resolvedRoot) {
        $profileRoot = if ($env:USERPROFILE) { $env:USERPROFILE } elseif ($env:HOME) { $env:HOME } else { [Environment]::GetFolderPath('UserProfile') }
        $cacheHome = Join-Path $profileRoot '.claude\plugins\cache'
        if (Test-Path -LiteralPath $cacheHome -PathType Container) {
            $resolvedRoot = Get-ChildItem -LiteralPath $cacheHome -Directory -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -like 'mcpserver*' -or $_.Name -like 'f--github-mcpserver*' } |
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 1 -ExpandProperty FullName
        }
    }
    if (-not $resolvedRoot) { $resolvedRoot = Split-Path -Parent $script:PluginEnvScriptDir }
    $env:MCP_PLUGIN_ROOT = Resolve-PluginCacheOrVersionDrift -ConfiguredRoot $env:MCP_PLUGIN_ROOT -ReplacementRoot $resolvedRoot
}

if (-not $env:MCP_WORKSPACE_START_DIR) {
    $resolvedStart = $startChain | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Container) } | Select-Object -First 1
    if ($resolvedStart) { $env:MCP_WORKSPACE_START_DIR = $resolvedStart }
}

# Unified identity trio + model/title defaults.
$env:MCP_AGENT_NAME = $agent
$env:MCP_AGENT_ID = $agent
$env:MCP_SESSION_AGENT = $agent
$env:MCP_SESSION_MODEL = $model
$env:MCP_SESSION_TITLE = "$agent plugin session"

# Turn recovery host identity.
$env:CT2R_SOURCE_TYPE = $agent
$env:CT2R_MODEL = $model
$env:CT2R_TITLE = "$agent turn"
$env:CT2R_TAGS = $tag
