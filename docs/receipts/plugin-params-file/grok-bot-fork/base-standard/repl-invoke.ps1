<#
.SYNOPSIS
    Sends a YAML request envelope through the PowerShell MCP runtime.
.DESCRIPTION
    Constructs a YAML envelope and routes it through the configured
    PowerShell MCP invocation path.

    Translation shim: workflow.sessionlog.* methods are not server routes
    — the dispatcher rejects them as method_not_found. They are plugin-
    local verbs that update cache/current-turn.yaml so the Stop hook can
    verify completion, and persist a session-log turn via the real
    client.SessionLog.SubmitAsync route.

    Two usage modes:
      1. Script entry: pwsh -File repl-invoke.ps1 -Method <m> [-ParamsYaml <y>]
      2. Dot-source for Invoke-ReplMethod cmdlet:
             . .\repl-invoke.ps1
             Invoke-ReplMethod -Method workflow.sessionlog.completeTurn ...
#>
[CmdletBinding()]
param(
    [string]$Method,
    [string]$ParamsYaml = ''
)

$ErrorActionPreference = 'Stop'

$shimModule = Join-Path $PSScriptRoot 'McpPluginShim.psm1'
Import-Module $shimModule -Force -ErrorAction Stop
$shimCommand = Get-Command New-McpPluginTurnUpsertRequest -ErrorAction Stop
if (-not $shimCommand.Parameters.ContainsKey('ProcessingDialog')) {
    Remove-Module McpPluginShim -Force -ErrorAction SilentlyContinue
    Import-Module $shimModule -Force -ErrorAction Stop
    $shimCommand = Get-Command New-McpPluginTurnUpsertRequest -ErrorAction Stop
    if (-not $shimCommand.Parameters.ContainsKey('ProcessingDialog')) {
        throw 'McpPluginShim.psm1 is stale or invalid because New-McpPluginTurnUpsertRequest lacks ProcessingDialog.'
    }
}
. (Join-Path $PSScriptRoot 'yaml-object-mutation.ps1')
. (Join-Path $PSScriptRoot 'agent-runtime-header.ps1')
. (Join-Path $PSScriptRoot 'classified-error.ps1')
Import-McpYamlSerializer
. (Join-Path $PSScriptRoot 'marker-resolver.ps1')

$script:ReplInvokePluginRoot = if ($env:MCP_PLUGIN_ROOT) {
    $env:MCP_PLUGIN_ROOT
} else {
    Split-Path -Parent $PSScriptRoot
}

# Agent for per-agent REPL cache and isolation. Must be passed to every qbrain-ai-repl call.
# Keep this precedence aligned with the shell resolve-cache-dir counterpart and MarkerFileClientOptionsResolver.ResolveAgentKey.
$script:AgentName = if ($env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME }
                   elseif ($env:PLUGIN_AGENT_NAME) { $env:PLUGIN_AGENT_NAME }
                   elseif ($env:PLUGIN_AGENT_DEFAULT) { $env:PLUGIN_AGENT_DEFAULT }
                   elseif ($env:MCP_PLUGIN_HOST) { $env:MCP_PLUGIN_HOST }
                   else { 'default' }

# FR-MCP-SESSIONLIFE-002: Codex keeps its audit identity when a host process
# inherits Grok agent variables. Other hosts keep the existing precedence.
if ($env:MCP_PLUGIN_HOST -match '^(?i:codex)$' -and $script:AgentName -match '(?i)grok') {
    $script:AgentName = 'Codex'
}

function Get-ReplCanonicalAgentName {
    # TR-MCP-REPL-011: map the resolved agent (which can fall back to lowercase 'default' or a
    # lowercase host key like 'claude-code') to a PascalCase source type so composed session ids
    # satisfy the server regex ^[A-Z][A-Za-z0-9]*-... (BUG-TRIAGE-085).
    param([string]$AgentName)

    if ([string]::IsNullOrWhiteSpace($AgentName)) { return 'ClaudeCode' }

    switch (($AgentName.Trim().ToLowerInvariant() -replace '[^a-z0-9]', '')) {
        'claude'       { return 'ClaudeCode' }
        'claudecode'   { return 'ClaudeCode' }
        'claudecowork' { return 'ClaudeCowork' }
        'codex'        { return 'Codex' }
        'copilot'      { return 'Copilot' }
        'grok'         { return 'GrokCode' }
        'grokcode'     { return 'GrokCode' }
        'cline'        { return 'Cline' }
        'clinev2'      { return 'Cline' }
        'opencode'     { return 'OpenCode' }
    }

    $parts = [regex]::Split($AgentName.Trim(), '[^A-Za-z0-9]+') | Where-Object { $_ }
    if (-not $parts) { return 'ClaudeCode' }
    return (-join ($parts | ForEach-Object { $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1) }))
}

if (-not (Get-Command Resolve-McpCacheDir -ErrorAction SilentlyContinue) -or
    -not (Get-Command Get-McpFailsafeDir -ErrorAction SilentlyContinue)) {
    . (Join-Path $PSScriptRoot 'resolve-cache-dir.ps1')
}

# Resolved lazily so per-call context (workspace / env) governs path. TR-MCP-PLUGIN-013 AC-005 (PLAN-SESSIONTRIAGEFIX-001 D3): a
# subagent's own session-log verbs (MCP_SUBAGENT_ID or GROK_SUBAGENT_ID set, no explicit override) reach its own background folder.
function script:Get-ReplInvokeCacheDir {
    $cacheDir = Resolve-McpCacheDir
    if (Get-Command Resolve-McpBackgroundScopeCacheDir -ErrorAction SilentlyContinue) {
        return (Resolve-McpBackgroundScopeCacheDir -CacheDir $cacheDir)
    }
    return $cacheDir
}

function New-ReplPluginSessionId {
    if ($env:MCP_SESSION_ID) { return $env:MCP_SESSION_ID }

    $timestamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $suffix = if ($env:MCP_SESSION_SUFFIX) { $env:MCP_SESSION_SUFFIX } else { 'plugin-session' }
    $suffix = ($suffix.ToLowerInvariant() -replace '[^a-z0-9]+', '-').Trim('-')
    if (-not $suffix) { $suffix = 'plugin-session' }

    return '{0}-{1}-{2}' -f (Get-ReplCanonicalAgentName $script:AgentName), $timestamp, $suffix
}

function Write-ReplStickySessionState {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [string]$RootSessionId = '',
        [Parameter(Mandatory)][string]$SessionId,
        [switch]$Child
    )
    $sessionDir = Get-ReplOpenSessionStatePath -CacheDir $CacheDir -RootSessionId $RootSessionId -SessionId $SessionId -Child:$Child
    $isChild = $sessionDir -ne $CacheDir
    if ($isChild) {
        New-Item -ItemType Directory -Path $sessionDir -Force | Out-Null
    }
    $sessionFile = Join-Path $sessionDir 'session-state.yaml'
    $state = [ordered]@{
        status = 'verified'
        sessionId = $SessionId
        lastUpdated = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    }
    Write-McpYamlObject -Path $sessionFile -Document $state
    return $sessionFile
}

function Complete-ReplBeginTurnAfterPersist {
    param(
        [bool]$Persisted,
        [bool]$Degraded,
        [string]$FailsafePath = '',
        [string]$CurrentTurnFile = '',
        [hashtable]$TurnState = $null
    )
    if (Test-ReplBeginTurnDegradedQueued -Persisted $Persisted -Degraded $Degraded) {
        # FR-MCP-SESSIONLIFE-001: keep the turn file already written. Record degraded
        # on that object. Do not replace queryText, planFile, todoId, or audit fields.
        # AGENTS.md rule 12 / HV08: mutate via YAML object helpers so paths with
        # spaces or '#' survive round-trip (HV04).
        if ($CurrentTurnFile) {
            if (Test-Path -LiteralPath $CurrentTurnFile) {
                $doc = Read-McpYamlObject -Path $CurrentTurnFile -Create
                if ($doc -isnot [System.Collections.IDictionary]) { $doc = [ordered]@{} }
                $doc['degraded'] = $true
                if (-not [string]::IsNullOrWhiteSpace($FailsafePath)) {
                    $doc['failsafePath'] = $FailsafePath
                }
                Write-McpYamlObject -Path $CurrentTurnFile -Document $doc
            } elseif ($TurnState) {
                $doc = [ordered]@{
                    turnRequestId = $TurnState.turnRequestId
                    sessionId = $TurnState.sessionId
                    status = 'in_progress'
                    degraded = $true
                }
                if (-not [string]::IsNullOrWhiteSpace($FailsafePath)) {
                    $doc['failsafePath'] = $FailsafePath
                }
                Write-McpYamlObject -Path $CurrentTurnFile -Document $doc
            }
        }
        return @{ ok = $true; degraded = $true; failsafeRetained = (-not [string]::IsNullOrWhiteSpace($FailsafePath) -and (Test-Path -LiteralPath $FailsafePath)) }
    }
    if (-not $Persisted) {
        return @{ ok = $false; degraded = $false; failsafeRetained = (Test-Path -LiteralPath $FailsafePath) }
    }
    if ($FailsafePath -and (Test-Path -LiteralPath $FailsafePath)) {
        Remove-Item -LiteralPath $FailsafePath -Force -ErrorAction SilentlyContinue
    }
    return @{ ok = $true; degraded = $false; failsafeRetained = $false }
}

function Get-ReplOpenSessionStatePath {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [string]$RootSessionId = '',
        [Parameter(Mandatory)][string]$SessionId,
        [switch]$Child
    )
    $isChild = [bool]$Child
    if (-not $isChild -and -not [string]::IsNullOrWhiteSpace($RootSessionId) -and $RootSessionId -ne $SessionId) {
        $isChild = $true
    }
    if ($isChild) {
        return (Join-Path (Join-Path $CacheDir 'sessions') $SessionId)
    }
    return $CacheDir
}

function Get-ReplCompleteTurnPersistSessionId {
    # Do not overwrite it with the rotated active session id. A cached turn
    # session id is the persist target. The active id is only a fallback.
    param(
        [string]$CurrentTurnSessionId,
        [string]$ActiveSessionId
    )
    if (-not [string]::IsNullOrWhiteSpace($CurrentTurnSessionId)) {
        return $CurrentTurnSessionId
    }
    return $ActiveSessionId
}

function Get-ReplSessionIdSourceTypePrefix {
    # FR-MCP-XAGENT-001: session ids are <Agent>-<yyyyMMddTHHmmssZ>-<suffix>.
    param([string]$SessionId)

    if ([string]::IsNullOrWhiteSpace($SessionId)) { return '' }
    if ($SessionId -match '^(?<prefix>[A-Za-z][A-Za-z0-9]*)-\d{8}T') {
        return $Matches['prefix']
    }
    return ($SessionId -split '-', 2)[0]
}

function Test-ReplSessionSourceTypeCompatible {
    param(
        [string]$Left,
        [string]$Right
    )

    $leftPrefix = Get-ReplSessionIdSourceTypePrefix -SessionId $Left
    $rightPrefix = Get-ReplSessionIdSourceTypePrefix -SessionId $Right
    if ([string]::IsNullOrWhiteSpace($leftPrefix) -or [string]::IsNullOrWhiteSpace($rightPrefix)) {
        return $true
    }
    return $leftPrefix -eq $rightPrefix
}

function Test-ReplBeginTurnDegradedQueued {
    param([bool]$Persisted, [bool]$Degraded)
    return (-not $Persisted) -and $Degraded
}

function Invoke-WorkflowOpenSession {
    # TR-MCP-REPL-011: persist an explicit valid sessionId into session-state.yaml instead of the
    # historical no-op, so an explicit openSession can recover a bad/rotated local session id
    # (BUG-TRIAGE-085; previously documented as a gap in GAPS.md).
    param(
        [string]$ParamsYaml,
        [string]$CacheDir = '',
        [string]$SessionId = '',
        [string]$RootSessionId = ''
    )
    if (-not [string]::IsNullOrWhiteSpace($CacheDir) -and -not [string]::IsNullOrWhiteSpace($SessionId)) {
        Write-ReplStickySessionState -CacheDir $CacheDir -RootSessionId $RootSessionId -SessionId $SessionId | Out-Null
        return $true
    }

    $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    $sessionId = ''
    if ($params -is [System.Collections.IDictionary] -and $params.Contains('sessionId')) {
        $sessionId = [string]$params['sessionId']
    } elseif ($params -and $params.PSObject.Properties['sessionId']) {
        $sessionId = [string]$params.PSObject.Properties['sessionId'].Value
    }
    if ([string]::IsNullOrWhiteSpace($sessionId)) { return $false }

    $sessionId = ConvertTo-ReplCanonicalSessionId -SessionId $sessionId.Trim()
    $cacheDir = Get-ReplInvokeCacheDir
    $rootSessionFile = Join-Path $cacheDir 'session-state.yaml'
    $rootState = if (Test-Path -LiteralPath $rootSessionFile) { Read-McpYamlObject -Path $rootSessionFile } else { $null }
    $rootSessionId = ''
    if ($rootState -is [System.Collections.IDictionary] -and $rootState.Contains('sessionId')) {
        $rootSessionId = [string]$rootState['sessionId']
    }
    $isChild = $false
    if ($params -is [System.Collections.IDictionary]) {
        $isChild = [bool]($params['background'] -or $params['child'] -or $params['sessionKey'])
    }
    if (-not $isChild -and -not [string]::IsNullOrWhiteSpace($rootSessionId) -and $rootSessionId -ne $sessionId) {
        $isChild = $true
    }
    $sessionFile = Write-ReplStickySessionState -CacheDir $cacheDir -RootSessionId $rootSessionId -SessionId $sessionId -Child:$isChild
    $state = Read-McpYamlObject -Path $sessionFile -Create
    if ($state -isnot [System.Collections.IDictionary]) { $state = [ordered]@{} }
    $state['status'] = 'verified'
    $state['sessionId'] = $sessionId
    if (-not $state.Contains('agent') -or [string]::IsNullOrWhiteSpace([string]$state['agent'])) {
        $state['agent'] = Get-ReplCanonicalAgentName $script:AgentName
    }
    $state['lastUpdated'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Write-McpYamlObject -Path $sessionFile -Document $state
    return $true
}


function Resolve-ReplWorkspaceDirectory {
    # A marker-bearing current directory outranks the workspace env vars: ambient
    # MCP_WORKSPACE_PATH values leak across workspaces through host processes and
    # persistent consoles, and an inherited value must not re-bind marker trust,
    # API keys, and session logs to another workspace while the cache directory
    # stays local (triage-report-7c84e6437f7b42d0a67fbe32679a686a). This matches
    # the plugin hook's Get-PluginStartPath precedence.
    $providerLocation = $null
    try {
        $location = Get-Location
        if ($location.Provider.Name -eq 'FileSystem') {
            $providerLocation = $location.ProviderPath
        }
    } catch {
        # Fall through to the env and .NET current directory fallbacks.
    }

    # Only the PowerShell provider location is trusted for the marker check: the
    # process-wide [Environment]::CurrentDirectory can be stale in shared hosts.
    if (-not [string]::IsNullOrWhiteSpace($providerLocation) -and
        (Test-Path -LiteralPath $providerLocation -PathType Container)) {
        try {
            if ((Get-Command Find-MarkerFile -ErrorAction SilentlyContinue) -and
                (Find-MarkerFile -StartDir $providerLocation)) {
                return (Resolve-Path -LiteralPath $providerLocation).ProviderPath
            }
        } catch {
            # No marker above the current directory; consult the env fallbacks below.
        }
    }

    $candidates = @(
        $env:MCP_WORKSPACE_PATH,
        $env:MCPSERVER_WORKSPACE_PATH,
        $env:MCP_WORKSPACE_START_DIR,
        $env:CLAUDE_PROJECT_DIR,
        $providerLocation,
        [Environment]::CurrentDirectory
    )

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        if (Test-Path -LiteralPath $candidate -PathType Container) {
            return (Resolve-Path -LiteralPath $candidate).ProviderPath
        }
    }

    return (Get-Location).ProviderPath
}

function Assert-ReplMarkerFresh {
    $workspace = Resolve-ReplWorkspaceDirectory
    $sessionFile = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'

    try {
        $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
        $state = Read-McpYamlObject -Path $sessionFile -Create
        if ($state -isnot [System.Collections.IDictionary]) {
            $state = [ordered]@{}
        }

        $status = if ($state.Contains('status')) { [string]$state['status'] } else { '' }
        $cachedPath = if ($state.Contains('markerFilePath')) { [string]$state['markerFilePath'] } else { '' }
        $cachedWriteUtc = if ($state.Contains('markerLastWriteUtc')) { [string]$state['markerLastWriteUtc'] } else { '' }
        $cachedSessionId = if ($state.Contains('sessionId')) { [string]$state['sessionId'] } else { '' }

        if ($status -eq 'verified' -and
            $cachedPath -eq $snapshot.markerFilePath -and
            $cachedWriteUtc -eq $snapshot.markerLastWriteUtc -and
            -not [string]::IsNullOrWhiteSpace($cachedSessionId)) {
            return $true
        }

        if (-not (Invoke-FullBootstrap -StartDir $workspace)) {
            throw 'marker bootstrap failed'
        }
        $snapshot = Get-MarkerFileSnapshot -StartDir $workspace

        $state['status'] = 'verified'
        if (-not $state.Contains('agent')) {
            $state['agent'] = $script:AgentName
        }
        if (-not $state.Contains('sessionId') -or [string]::IsNullOrWhiteSpace([string]$state['sessionId'])) {
            $state['sessionId'] = New-ReplPluginSessionId
        }
        $state['lastUpdated'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        $state['markerFilePath'] = $snapshot.markerFilePath
        $state['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
        Write-McpYamlObject -Path $sessionFile -Document $state
        return $true
    } catch {
        $untrustedState = [ordered]@{
            status = 'MCP_UNTRUSTED'
            lastUpdated = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        try {
            $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
            $untrustedState['markerFilePath'] = $snapshot.markerFilePath
            $untrustedState['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
        } catch {
        }
        Write-McpYamlObject -Path $sessionFile -Document $untrustedState
        [Console]::Error.WriteLine("MCP_UNTRUSTED: marker refresh failed before REPL request: $_")
        return $false
    }
}

function Set-ReplProcessWorkspace {
    param([Parameter(Mandatory)][System.Diagnostics.ProcessStartInfo]$StartInfo)

    $workspace = Resolve-ReplWorkspaceDirectory
    $StartInfo.WorkingDirectory = $workspace
    $StartInfo.Environment['MCP_WORKSPACE_PATH'] = $workspace
    $StartInfo.Environment['MCPSERVER_WORKSPACE_PATH'] = $workspace
    $StartInfo.Environment['MCP_WORKSPACE_START_DIR'] = $workspace
    $StartInfo.Environment['CLAUDE_PROJECT_DIR'] = $workspace
}

function Convert-ReplParamsYamlToObject {
    param([string]$ParamsYaml)

    if (-not $ParamsYaml) { return $null }

    $normalized = $ParamsYaml -replace "`r`n", "`n" -replace "`r", ""
    if ($normalized.TrimStart() -match '^[\{\[]') {
        return ($normalized | ConvertFrom-Json -Depth 100 -ErrorAction Stop)
    }

    if (Get-Command ConvertFrom-Yaml -ErrorAction SilentlyContinue) {
        try {
            return ($normalized | ConvertFrom-Yaml -ErrorAction Stop)
        } catch {
            # Fall through to the local subset parser so plugin runtime remains
            # self-contained when the optional YAML module cannot parse input.
        }
    }

    return (ConvertFrom-ReplYamlSubset -Text $normalized)
}

function ConvertFrom-ReplYamlScalar {
    param([string]$Value)

    $trimmed = $Value.Trim()
    if ($trimmed -eq '') { return '' }
    if ($trimmed -eq '{}') { return [ordered]@{} }
    if ($trimmed -eq '[]') { return @() }
    if ($trimmed -match '^(true|false)$') { return [bool]::Parse($trimmed) }
    if ($trimmed -match '^-?\d+$') { return [int64]$trimmed }
    if (($trimmed.StartsWith('"') -and $trimmed.EndsWith('"')) -or ($trimmed.StartsWith("'") -and $trimmed.EndsWith("'"))) {
        return $trimmed.Substring(1, $trimmed.Length - 2)
    }

    return $trimmed
}

function Get-ReplYamlIndent {
    param([string]$Line)

    return ([regex]::Match($Line, '^\s*').Value.Length)
}

function ConvertFrom-ReplYamlSubset {
    param([string]$Text)

    $lines = @($Text -split "`n")
    $index = 0
    return (Read-ReplYamlBlock -Lines $lines -Index ([ref]$index) -Indent 0)
}

function Read-ReplYamlBlock {
    param(
        [string[]]$Lines,
        [ref]$Index,
        [int]$Indent
    )

    while ($Index.Value -lt $Lines.Count -and $Lines[$Index.Value].Trim() -eq '') {
        $Index.Value++
    }

    if ($Index.Value -ge $Lines.Count) { return [ordered]@{} }

    $first = $Lines[$Index.Value]
    $isList = (Get-ReplYamlIndent $first) -eq $Indent -and $first.Substring($Indent).TrimStart().StartsWith('- ')
    if ($isList) {
        $items = [System.Collections.Generic.List[object]]::new()
        while ($Index.Value -lt $Lines.Count) {
            $line = $Lines[$Index.Value]
            if ($line.Trim() -eq '') { $Index.Value++; continue }
            $currentIndent = Get-ReplYamlIndent $line
            if ($currentIndent -lt $Indent) { break }
            if ($currentIndent -ne $Indent) { break }
            $content = $line.Substring($Indent).TrimStart()
            if (-not $content.StartsWith('- ')) { break }

            $itemText = $content.Substring(2).Trim()
            $Index.Value++
            if ($itemText -match '^([^:]+):\s*(.*)$') {
                $item = [ordered]@{}
                $key = $Matches[1].Trim()
                $value = $Matches[2]
                if ($value -eq '') {
                    $item[$key] = Read-ReplYamlBlock -Lines $Lines -Index $Index -Indent ($Indent + 2)
                } else {
                    $item[$key] = ConvertFrom-ReplYamlScalar $value
                }

                while ($Index.Value -lt $Lines.Count) {
                    $nextLine = $Lines[$Index.Value]
                    if ($nextLine.Trim() -eq '') { $Index.Value++; continue }
                    $nextIndent = Get-ReplYamlIndent $nextLine
                    if ($nextIndent -le $Indent) { break }
                    $nextContent = $nextLine.Substring($nextIndent)
                    if ($nextContent -notmatch '^([^:]+):\s*(.*)$') { break }
                    $nextKey = $Matches[1].Trim()
                    $nextValue = $Matches[2]
                    $Index.Value++
                    if ($nextValue -eq '|') {
                        $item[$nextKey] = Read-ReplYamlLiteralBlock -Lines $Lines -Index $Index -Indent ($nextIndent + 2)
                    } elseif ($nextValue -eq '') {
                        $item[$nextKey] = Read-ReplYamlBlock -Lines $Lines -Index $Index -Indent ($nextIndent + 2)
                    } else {
                        $item[$nextKey] = ConvertFrom-ReplYamlScalar $nextValue
                    }
                }

                $items.Add([pscustomobject]$item)
            } else {
                $items.Add((ConvertFrom-ReplYamlScalar $itemText))
            }
        }

        return $items.ToArray()
    }

    $map = [ordered]@{}
    while ($Index.Value -lt $Lines.Count) {
        $line = $Lines[$Index.Value]
        if ($line.Trim() -eq '') { $Index.Value++; continue }
        $currentIndent = Get-ReplYamlIndent $line
        if ($currentIndent -lt $Indent) { break }
        if ($currentIndent -gt $Indent) { break }

        $content = $line.Substring($Indent)
        if ($content -notmatch '^([^:]+):\s*(.*)$') { $Index.Value++; continue }
        $key = $Matches[1].Trim()
        $value = $Matches[2]
        $Index.Value++

        if ($value -eq '|') {
            $map[$key] = Read-ReplYamlLiteralBlock -Lines $Lines -Index $Index -Indent ($Indent + 2)
        } elseif ($value -eq '') {
            $map[$key] = Read-ReplYamlBlock -Lines $Lines -Index $Index -Indent ($Indent + 2)
        } else {
            $map[$key] = ConvertFrom-ReplYamlScalar $value
        }
    }

    return [pscustomobject]$map
}

function Read-ReplYamlLiteralBlock {
    param(
        [string[]]$Lines,
        [ref]$Index,
        [int]$Indent
    )

    $items = [System.Collections.Generic.List[string]]::new()
    while ($Index.Value -lt $Lines.Count) {
        $line = $Lines[$Index.Value]
        if ($line.Trim() -ne '' -and (Get-ReplYamlIndent $line) -lt $Indent) { break }
        if ($line.Length -ge $Indent) {
            $items.Add($line.Substring($Indent))
        } else {
            $items.Add('')
        }
        $Index.Value++
    }

    return ($items -join "`n").TrimEnd()
}

function ConvertTo-ReplCanonicalSessionId {
    param([Parameter(Mandatory)][string]$SessionId)

    if ($SessionId -match '^[A-Za-z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$') {
        return $SessionId
    }

    if ($SessionId -match '^(?<agent>[A-Za-z][A-Za-z0-9]*(?:-[A-Za-z0-9]+)*)-(?<stamp>\d{8}T\d{6}Z)$') {
        return '{0}-{1}-plugin-session' -f $Matches['agent'], $Matches['stamp']
    }

    return $SessionId
}

function Get-ReplSessionMeta {
    $f = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    if (-not (Test-Path $f)) { return $null }
    $line = Select-String -Path $f -Pattern '^sessionId:' -SimpleMatch:$false |
        Select-Object -First 1
    if (-not $line) { return $null }
    $sid = ($line.Line -replace '^sessionId:\s*', '').Trim()
    $sid = Get-ReplCompleteTurnPersistSessionId -CurrentTurnSessionId (Get-ReplCurrentTurnValue -Key 'sessionId') -ActiveSessionId $sid
    if (-not $sid) { return $null }
    $canonicalSessionId = ConvertTo-ReplCanonicalSessionId -SessionId $sid
    if ($canonicalSessionId -ne $sid) {
        $state = Read-McpYamlObject -Path $f
        $state['sessionId'] = $canonicalSessionId
        if (-not $state.Contains('lastUpdated')) {
            $state['lastUpdated'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        Write-McpYamlObject -Path $f -Document $state
        $sid = $canonicalSessionId
    }
    $prefix = ($sid -split '-', 2)[0]
    New-McpPluginSessionMeta -SourceType $prefix -SessionId $sid
}

function Get-ReplMethodTimeoutSeconds {
    # TR-MCP-REPL-012: per-method timeout. Long-running requirement/agent methods (which invoke
    # external CLIs and can take minutes) get an extended, env-configurable budget; everything else
    # keeps the short default so sessionlog calls fail fast (BUG-TRIAGE-072).
    param([Parameter(Mandatory)][string]$Method)

    $default = if ($env:REPL_TIMEOUT) { [int]$env:REPL_TIMEOUT } else { 30 }
    $long = if ($env:REPL_LONG_TIMEOUT) { [int]$env:REPL_LONG_TIMEOUT } else { 300 }
    $helper = if ($env:REPL_HELPER_TIMEOUT) { [int]$env:REPL_HELPER_TIMEOUT } else { 120 }

    if ($script:ReplFailsafeDraining -and $Method -eq 'client.SessionLog.SubmitAsync') {
        # TR-MCP-REPL-012 AC3 / TEST-MCP-195 AC5-7: drain SubmitAsync uses
        # REPL_FAILSAFE_DRAIN_TIMEOUT (default 120s), or REPL_TIMEOUT when greater.
        # Sessionlog workflow methods keep $default (30s). Nested drain stays deferred.
        $drain = if ($env:REPL_FAILSAFE_DRAIN_TIMEOUT) { [int]$env:REPL_FAILSAFE_DRAIN_TIMEOUT } else { 120 }
        $seconds = if ($default -gt $drain) { $default } else { $drain }
    } else {
        $seconds = switch -Wildcard ($Method) {
            'workflow.todo.analyzeRequirements'       { $long; break }
            'workflow.requirements.generateDocument'  { $long; break }
            'workflow.requirements.ingestDocument'    { $long; break }
            'workflow.requirements.analyze*'          { $long; break }
            'client.Requirements.Analyze*'            { $long; break }
            'workflow.agenthelp.submitTurn'           { $helper; break }
            default                                   { $default }
        }
    }

    # TR-MCP-REPL-012 AC-001 (D1, triage-report-124a374c): inside a hook no call
    # may wait past the hook's deadline; outside a hook the timeout is unchanged.
    return (Get-ReplCappedTimeout -TimeoutSeconds $seconds -RemainingMs (Get-ReplHookTimeLeft))
}

function Get-ReplHookTimeLeft {
    # TR-MCP-REPL-012 AC-001 (D1): milliseconds left before the deadline that
    # plugin-hook.ps1 publishes in MCP_HOOK_DEADLINE_UTC_MS (Unix milliseconds);
    # $null outside a hook. May be negative once the deadline has passed.
    $deadline = 0L
    if (-not [long]::TryParse([string]$env:MCP_HOOK_DEADLINE_UTC_MS, [ref]$deadline) -or $deadline -le 0) { return $null }
    return ($deadline - [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds())
}

function Get-ReplCappedTimeout {
    # TR-MCP-REPL-012 AC-001 (D1): the lesser of the per-method timeout and the
    # whole seconds left before the hook deadline, so a slow or silent server ends
    # the call in time and the persist falls back to its write-ahead failsafe
    # record. 0 when less than one second is left: the call is then not started
    # (Invoke-ReplRawCore), because a call begun after the deadline would carry the
    # hook past its budget. Without a deadline the timeout is unchanged.
    param(
        [Parameter(Mandatory)][int]$TimeoutSeconds,
        [AllowNull()][Nullable[long]]$RemainingMs
    )
    if ($null -eq $RemainingMs) { return $TimeoutSeconds }
    return [int][Math]::Max(0, [Math]::Min([double]$TimeoutSeconds, [Math]::Floor([double]$RemainingMs / 1000)))
}

function Invoke-ReplRaw {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )
    # TR-MCP-REPL-016 AC-001 (D1): a drain deferred by an earlier successful call no
    # longer runs here, ahead of this call: it ran before beginTurn's new-turn persist
    # and never ran at all in an invocation with one backend call. The script entry
    # runs it once the invoked method has completed.
    $script:ReplRawInFlight = $true
    try {
        return (Invoke-ReplRawCore -Method $Method -ParamsYaml $ParamsYaml)
    }
    finally {
        $script:ReplRawInFlight = $false
    }
}

function Invoke-ReplRawCore {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )
    if (-not (Assert-ReplMarkerFresh)) {
        return (New-McpPluginReplResult -Success $false -Output '' -Error 'MCP_UNTRUSTED: marker refresh failed before REPL request')
    }

    $replCommand = Get-Command qbrain-ai-repl -ErrorAction SilentlyContinue
    if (-not $replCommand) {
        $replCommand = Get-Command mcpserver-repl -ErrorAction SilentlyContinue
    }
    $replExe = $null
    if ($env:MCP_REPL_EXECUTABLE -and (Test-Path -LiteralPath $env:MCP_REPL_EXECUTABLE)) {
        $replExe = $env:MCP_REPL_EXECUTABLE
    } elseif ($replCommand) {
        $replExe = [string]$replCommand.Source
    }
    if ([string]::IsNullOrWhiteSpace($replExe)) {
        return (New-McpPluginReplResult -Success $false -Output '' -Error 'qbrain-ai-repl or mcpserver-repl not found on PATH')
    }

    $requestId = "req-$(Get-Date -AsUTC -Format 'yyyyMMddTHHmmssZ')-$((Get-Random -Maximum 0xFFFF).ToString('x4'))"
    $timeout = Get-ReplMethodTimeoutSeconds -Method $Method
    if ($timeout -le 0) {
        # TR-MCP-REPL-012 AC-001 (D1): under one second is left before the hook deadline, so the call is not
        # started; it fails as timed out and a persist keeps its write-ahead failsafe record as the fallback.
        return (New-McpPluginReplResult -Success $false -Output '' -Error "qbrain-ai-repl timed out after 0s: the hook deadline left no time to start $Method")
    }

    # Build as an object and serialize to JSON so request envelopes keep a
    # single canonical shape across plugin hosts.
    $paramsObject = $null
    if ($ParamsYaml) {
        $paramsObject = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    }
    $request = New-McpPluginReplRequest -RequestId $requestId -Method $Method -Params $paramsObject
    $envelope = ConvertTo-McpPluginJson -InputObject $request -Depth 20 -Compress

    try {
        $psi = [System.Diagnostics.ProcessStartInfo]::new()
        if ($replExe -match '\.(cmd|bat)$') {
            $psi.FileName = $env:ComSpec
            $psi.ArgumentList.Add('/c')
            $psi.ArgumentList.Add($replExe)
        } else {
            $psi.FileName = $replExe
        }
        $psi.ArgumentList.Add('--agent-stdio')
        $psi.ArgumentList.Add('--agent')
        $psi.ArgumentList.Add($script:AgentName)
        $psi.RedirectStandardInput = $true
        $psi.RedirectStandardOutput = $true
        # Do NOT redirect stderr: qbrain-ai-repl logs verbose 'info:' lines
        # to stderr, and an unread redirected stream blocks the child once
        # its pipe buffer fills (Windows ~4 KB), causing WaitForExit to hang.
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true
        Set-ReplProcessWorkspace -StartInfo $psi
        # qbrain-ai-repl writes UTF-8 (with BOM). Without explicit encoding,
        # PowerShell decodes as cp437 and BOM bytes (EF BB BF) become box-
        # drawing glyphs that break the '^type: error' regex anchor.
        $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8

        $proc = [System.Diagnostics.Process]::Start($psi)
        $budget = [System.Diagnostics.Stopwatch]::StartNew()
        $envFile = Join-Path (Get-ReplInvokeCacheDir) "envelope-$requestId.tmp"
        [System.IO.File]::WriteAllText($envFile, $envelope, [System.Text.Encoding]::UTF8)
        $copyStream = [System.IO.File]::OpenRead($envFile)
        $copyTask = $copyStream.CopyToAsync($proc.StandardInput.BaseStream)
        $copyBudget = [int][Math]::Max(1, ($timeout * 1000) - $budget.ElapsedMilliseconds)
        if (-not $copyTask.Wait($copyBudget)) {
            try { $copyStream.Dispose() } catch { }
            try { $proc.Kill($true) } catch { }
            try { [void]$proc.WaitForExit(2000) } catch { }
            Remove-Item $envFile -ErrorAction SilentlyContinue
            return (New-McpPluginReplResult -Success $false -Output '' -Error "qbrain-ai-repl timed out after ${timeout}s")
        }
        $copyStream.Dispose()
        $proc.StandardInput.Close()
        Remove-Item $envFile -ErrorAction SilentlyContinue

        # Drain stdout BEFORE waiting for exit. With a redirected pipe, the
        # child blocks on stdout writes once the pipe buffer (~4 KB on
        # Windows) fills, and WaitForExit then deadlocks. ReadToEndAsync
        # streams the buffer concurrently and resolves when the child closes
        # stdout (which happens at process exit).
        $readTask = $proc.StandardOutput.ReadToEndAsync()
        $readBudget = [int][Math]::Max(1, ($timeout * 1000) - $budget.ElapsedMilliseconds)
        if (-not $readTask.Wait($readBudget)) {
            try { $proc.Kill($true) } catch { }
            try { [void]$proc.WaitForExit(2000) } catch { }
            return (New-McpPluginReplResult -Success $false -Output '' -Error "qbrain-ai-repl timed out after ${timeout}s")
        }
        $output = $readTask.Result
        $exitBudget = [int][Math]::Max(1, ($timeout * 1000) - $budget.ElapsedMilliseconds)
        if (-not $proc.WaitForExit($exitBudget)) {
            try { $proc.Kill($true) } catch { }
            try { [void]$proc.WaitForExit(2000) } catch { }
            return (New-McpPluginReplResult -Success $false -Output '' -Error "qbrain-ai-repl timed out after ${timeout}s")
        }

        # qbrain-ai-repl writes a UTF-8 BOM before the YAML doc and may
        # interleave logger 'info:' lines on stdout — strip BOM and ignore
        # leading log noise so the regex anchor matches the real header.
        $output = $output -replace "[\uFEFF]", ''
        $isError = $output -match '(?m)^type:\s*error\b'
        if ($proc.ExitCode -ne 0 -or $isError) {
            return (New-McpPluginReplResult -Success $false -Output $output -ExitCode $proc.ExitCode)
        }
        # TR-MCP-REPL-016: this is the first proof in the process that the backend
        # answers, so it is the safe moment to replay anything the failsafe queue
        # captured while it was unreachable. Guarded to run at most once.
        Invoke-ReplFailsafeDrainOnFirstSuccess
        return (New-McpPluginReplResult -Success $true -Output $output -ExitCode $proc.ExitCode)
    }
    catch {
        $caughtOutput = ''
        if (Get-Variable -Name output -Scope Local -ErrorAction SilentlyContinue) {
            $caughtOutput = [string]$output
        }
        $classified = ConvertTo-McpPluginClassifiedError -Output $caughtOutput -ErrorText $_.ToString()
        if ($classified.preserved) {
            return (New-McpPluginReplResult -Success $false -Output $caughtOutput -Error $classified.message)
        }
        return (New-McpPluginReplResult -Success $false -Output '' -Error $_.ToString())
    }
}

function Get-ReplSessionStateValue {
    param([Parameter(Mandatory)][string]$Key)
    $f = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    if (-not (Test-Path $f)) { return '' }
    $state = Read-McpYamlObject -Path $f
    if (-not $state -or -not $state.Contains($Key) -or $null -eq $state[$Key]) { return '' }
    return [string]$state[$Key]
}

function Set-ReplSessionStateValue {
    # TR-MCP-REPL-014/015: object-first read-modify-write of a single session-state
    # key (e.g. the stable session 'title'). Never edits YAML as text.
    param(
        [Parameter(Mandatory)][string]$Key,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Value)
    $f = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    $state = Read-McpYamlObject -Path $f -Create
    $state[$Key] = $Value
    Write-McpYamlObject -Path $f -Document $state
    return $true
}

function Get-ReplCurrentTurnValue {
    param([Parameter(Mandatory)][string]$Key)
    $state = Read-ReplCurrentTurnState
    if (-not $state -or -not $state.Contains($Key) -or $null -eq $state[$Key]) { return '' }
    return [string]$state[$Key]
}

function Get-ReplCurrentTurnFile {
    return (Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml')
}

function Get-ReplRecoveryGuidance {
    return 'Run the active agent prompt hook; it will health-check the marker, recreate session state when AGENTS-README-FIRST.yaml changes, submit triage if create fails after healthy bootstrap, and continue through failsafe session logging while degraded.'
}

function Deny-ReplMissingCurrentTurn {
    param([Parameter(Mandatory)][string]$Method)

    $cacheDir = Get-ReplInvokeCacheDir
    $message = "$Method requires current-turn.yaml in '$cacheDir'. $(Get-ReplRecoveryGuidance) Set MCP_CACHE_DIR_OVERRIDE only when intentionally targeting a different active-turn cache."
    if (Get-Command Publish-ReplSessionVerbReceipt -ErrorAction SilentlyContinue) {
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition lost -Method $Method -Message $message -ChildStderr $message))
    }
    [Console]::Error.WriteLine($message)
    return $false
}

function Read-ReplCurrentTurnState {
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    if (-not (Test-Path $turnFile)) { return $null }
    try {
        return Read-McpYamlObject -Path $turnFile
    } catch {
        return $null
    }
}

function Write-ReplCurrentTurnState {
    param([Parameter(Mandatory)]$State)
    $turnFile = Join-Path (Get-ReplInvokeCacheDir) 'current-turn.yaml'
    Write-McpYamlObject -Path $turnFile -Document $State
}

function Get-ReplCurrentTurnQueryText {
    $turnState = Read-ReplCurrentTurnState
    if ($turnState -and $turnState.Contains('queryText')) {
        return [string]$turnState['queryText']
    }
    return ''
}

function Get-ReplFailsafeDir {
    # TR-MCP-REPL-016: the queue location is resolved by the shared helper so the
    # writer, the drain, and mcp-status.ps1 can never disagree about which
    # directory holds the pending records.
    return (Get-McpFailsafeDir)
}

function Get-ReplFailsafeQuarantineDir {
    # TR-MCP-REPL-017: unreplayable records are parked here instead of being
    # deleted, so a bad record is recoverable by hand and never blocks the queue.
    return (Get-McpFailsafeQuarantineDir)
}

# TR-MCP-REPL-016: records written by the submit currently in flight. The drain
# must not replay (and must not delete) a record whose original submit has not
# resolved yet, otherwise a bootstrap drain would double-submit the live turn.
$script:ReplFailsafeInFlight = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
# Re-entrancy guard: the drain calls Invoke-ReplRaw, which is the same place the
# drain is triggered from.
$script:ReplFailsafeDraining = $false
# One drain attempt per process. A second pass in the same process would only
# re-walk records the first pass already decided about.
$script:ReplFailsafeDrainCompleted = $false
# TR-MCP-PERSIST-003: while Invoke-ReplRaw is in flight, drain must not
# block the successful caller (getFr/getTr) for a 30s SubmitAsync timeout.
$script:ReplRawInFlight = $false
$script:ReplFailsafeDrainDeferred = $false
$script:ReplPersistVerbMethod = $null

function Write-ReplFailsafe {
    # Capture the serialized request before the remote call so a crash cannot lose
    # the turn. The YAML document is written through the shared object serializer.
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$ParamsYaml,
        [Parameter(Mandatory)][string]$Label,
        [string]$PayloadFingerprint = ''
    )
    try {
        $dir = Get-ReplFailsafeDir
        [void][System.IO.Directory]::CreateDirectory($dir)
        $stamp = Get-Date -AsUTC -Format 'yyyyMMddTHHmmssZ'
        $file = Join-Path $dir ("{0}-{1}-{2:x4}.yaml" -f $stamp, $Label, (Get-Random -Maximum 0xFFFF))
        $paramsObject = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
        $record = [ordered]@{
            method = $Method
            label = $Label
            timestamp = $stamp
            params = $paramsObject
        }
        if (-not [string]::IsNullOrWhiteSpace($PayloadFingerprint)) {
            $record.payloadFingerprint = $PayloadFingerprint
        }
        Write-McpYamlObject -Path $file -Document $record
        [void]$script:ReplFailsafeInFlight.Add($file)
        return $file
    }
    catch {
        return ''
    }
}
function Clear-ReplFailsafe {
    param([string]$Path)
    if ($Path) {
        [void]$script:ReplFailsafeInFlight.Remove($Path)
        if (Test-Path $Path) {
            Remove-Item -Path $Path -Force -ErrorAction SilentlyContinue
        }
    }
}

function Test-ReplFailsafeBackendUnreachable {
    # TR-MCP-REPL-016: distinguish "the backend never answered" from "the backend
    # answered and rejected this record". Only the first aborts the drain; the
    # second must let the walk continue so one bad record cannot dam the queue.
    param([AllowEmptyString()][string]$Detail)

    if ([string]::IsNullOrWhiteSpace($Detail)) { return $true }

    $markers = @(
        'MCP_UNTRUSTED',
        'not found on PATH',
        'timed out',
        'invocation failed',
        'No connection could be made',
        'actively refused',
        'connection refused',
        'Connection refused',
        'Unable to resolve the active workspace cache',
        'backend_unavailable',
        'HTTP 503',
        'http 503',
        'database is locked',
        'SQLITE_BUSY',
        'SQLite Error 5'
    )
    foreach ($marker in $markers) {
        if ($Detail -like "*$marker*") { return $true }
    }
    return $false
}

function Move-ReplFailsafeToQuarantine {
    # TR-MCP-REPL-017: park a record that cannot be replayed, with the reason next
    # to it. Never delete: a malformed record still holds the only copy of a turn.
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Reason
    )

    try {
        $quarantineDir = Get-ReplFailsafeQuarantineDir
        [void][System.IO.Directory]::CreateDirectory($quarantineDir)
        $target = Join-Path $quarantineDir ([System.IO.Path]::GetFileName($Path))
        if (Test-Path -LiteralPath $target) {
            $target = Join-Path $quarantineDir ("{0}-{1:x4}{2}" -f
                [System.IO.Path]::GetFileNameWithoutExtension($Path),
                (Get-Random -Maximum 0xFFFF),
                [System.IO.Path]::GetExtension($Path))
        }
        [System.IO.File]::Move($Path, $target)
        $reasonText = "quarantinedAtUtc: $((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))" +
            [Environment]::NewLine + "originalPath: $Path" +
            [Environment]::NewLine + "reason: $Reason" + [Environment]::NewLine
        [System.IO.File]::WriteAllText(($target + '.reason.txt'), $reasonText)
        return $target
    }
    catch {
        [Console]::Error.WriteLine("Failsafe quarantine failed for '$Path': $($_.Exception.Message)")
        return ''
    }
}

function Repair-ReplFailsafeQueuedRecord {
    param([Parameter(Mandatory)]$Document)

    $script:ReplFailsafeRepaired = $false
    if ($Document -isnot [System.Collections.IDictionary]) { return }
    if (-not $Document.Contains('params') -or $Document['params'] -isnot [System.Collections.IDictionary]) { return }
    $params = $Document['params']
    if (-not $params.Contains('turns')) { return }
    $turns = $params['turns']
    if ($turns -is [System.Collections.IDictionary]) {
        $ordered = [System.Collections.Generic.List[object]]::new()
        foreach ($key in @($turns.Keys | Sort-Object { [int]$_ })) {
            $turn = $turns[$key]
            if ($turn -is [System.Collections.IDictionary]) {
                if (-not $turn.Contains('planFile') -or [string]::IsNullOrWhiteSpace([string]$turn['planFile'])) { $turn['planFile'] = 'None' }
                if (-not $turn.Contains('todoId') -or [string]::IsNullOrWhiteSpace([string]$turn['todoId'])) { $turn['todoId'] = 'None' }
            }
            $ordered.Add($turn)
        }
        $params['turns'] = $ordered
        $script:ReplFailsafeRepaired = $true
    } elseif ($turns -is [System.Collections.IEnumerable] -and $turns -isnot [string]) {
        foreach ($turn in @($turns)) {
            if ($turn -isnot [System.Collections.IDictionary]) { continue }
            if (-not $turn.Contains('planFile') -or [string]::IsNullOrWhiteSpace([string]$turn['planFile'])) { $turn['planFile'] = 'None'; $script:ReplFailsafeRepaired = $true }
            if (-not $turn.Contains('todoId') -or [string]::IsNullOrWhiteSpace([string]$turn['todoId'])) { $turn['todoId'] = 'None'; $script:ReplFailsafeRepaired = $true }
        }
    }
}

function Invoke-ReplFailsafeDrain {
    <#
    .SYNOPSIS
        TR-MCP-REPL-016/017: replay queued failsafe records against a reachable backend.
    .DESCRIPTION
        Walks the failsafe queue oldest-first (the file name is a UTC stamp) and
        re-issues each captured request. A record is deleted only after its submit
        succeeds, so a failure never loses data. client.SessionLog.SubmitAsync is an
        upsert keyed by sessionId plus requestId, which makes a replay idempotent.

        Safety rules:
          - A record rejected by the backend stays on disk, its attempt counter is
            incremented, and the walk continues with the newer records behind it.
          - A record that cannot be parsed, or that has burned MaxAttempts, is moved
            to the quarantine directory with a reason file instead of being retried
            forever or deleted.
          - A transport failure aborts the whole pass without consuming attempts,
            because the backend, not the record, is the problem.
          - The record for the submit currently in flight is skipped.
    .PARAMETER MaxRecords
        Maximum records to consider in one pass. 0 means the whole queue.
    .PARAMETER MaxAttempts
        Attempt budget before a repeatedly rejected record is quarantined.
    #>
    [CmdletBinding()]
    param(
        [int]$MaxRecords = 0,
        [int]$MaxAttempts = 5
    )

    $summary = [ordered]@{
        failsafeDir = ''
        scanned = 0
        replayed = 0
        failed = 0
        quarantined = 0
        skipped = 0
        aborted = $false
        abortReason = ''
        stoppedAtDeadline = $false
    }

    if ($script:ReplFailsafeDraining) {
        $summary.aborted = $true
        $summary.abortReason = 'a drain is already running in this process'
        return $summary
    }

    try {
        $dir = Get-ReplFailsafeDir
    } catch {
        $summary.aborted = $true
        $summary.abortReason = "failsafe directory could not be resolved: $($_.Exception.Message)"
        return $summary
    }

    $summary.failsafeDir = $dir
    if (-not (Test-Path -LiteralPath $dir -PathType Container)) { return $summary }

    $script:ReplFailsafeDraining = $true
    try {
        # The file name starts with the capture stamp, so a name sort is an
        # oldest-first order and stays stable across passes.
        $records = @(Get-ChildItem -LiteralPath $dir -Filter '*.yaml' -File -ErrorAction SilentlyContinue |
            Sort-Object -Property Name)

        foreach ($record in $records) {
            if ($MaxRecords -gt 0 -and $summary.scanned -ge $MaxRecords) { break }
            # TR-MCP-REPL-016 AC-002 (D1): inside a hook, start a record only while at
            # least one second is left before the hook deadline; the records not reached
            # stay queued with their attempt counts unchanged for a later invocation.
            $remainingMs = Get-ReplHookTimeLeft
            if ($null -ne $remainingMs -and $remainingMs -lt 1000) {
                $summary.stoppedAtDeadline = $true
                break
            }
            if ($script:ReplFailsafeInFlight.Contains($record.FullName)) {
                $summary.skipped++
                continue
            }

            $summary.scanned++

            $document = $null
            $parseError = ''
            try {
                $document = Read-McpYamlObject -Path $record.FullName
            } catch {
                $parseError = "record is not readable YAML: $($_.Exception.Message)"
            }

            $method = ''
            $recordParams = $null
            if (-not $parseError) {
                if ($document -isnot [System.Collections.IDictionary]) {
                    $parseError = 'record root is not a YAML mapping'
                } else {
                    Repair-ReplFailsafeQueuedRecord -Document $document
                    if ($document.Contains('method')) { $method = [string]$document['method'] }
                    if ($document.Contains('params')) { $recordParams = $document['params'] }
                    if ([string]::IsNullOrWhiteSpace($method)) {
                        $parseError = 'record has no method'
                    } elseif ($null -eq $recordParams) {
                        $parseError = 'record has no params'
                    }
                }
            }

            if ($parseError) {
                if (Move-ReplFailsafeToQuarantine -Path $record.FullName -Reason $parseError) {
                    $summary.quarantined++
                }
                continue
            }

            $attempts = 0
            if ($script:ReplFailsafeRepaired) {
                $attempts = 0
            } elseif ($document.Contains('drainAttempts')) {
                try { $attempts = [int]$document['drainAttempts'] } catch { $attempts = 0 }
            }
            if ($attempts -ge $MaxAttempts) {
                $reason = "exceeded the drain attempt budget of $MaxAttempts"
                if ($document.Contains('lastDrainError')) {
                    $reason += ". Last error: $([string]$document['lastDrainError'])"
                }
                if (Move-ReplFailsafeToQuarantine -Path $record.FullName -Reason $reason) {
                    $summary.quarantined++
                }
                continue
            }

            $paramsYaml = ConvertTo-Yaml -Data $recordParams -Options WithIndentedSequences
            try {
                $result = Invoke-ReplRaw -Method $method -ParamsYaml $paramsYaml
            } catch {
                $result = New-McpPluginReplResult -Success $false -Output '' -Error $_.ToString() -ExitCode 1
            }
            if ($result.Success) {
                Clear-ReplFailsafe -Path $record.FullName
                $summary.replayed++
                continue
            }

            $summary.failed++
            $detail = "$($result.Error) $($result.Output)".Trim()
            if (Test-ReplFailsafeBackendUnreachable -Detail $detail) {
                # Leave the attempt counter alone: the backend, not the record,
                # failed, and burning attempts here would quarantine good turns.
                $summary.aborted = $true
                $summary.abortReason = "backend unreachable: $detail"
                break
            }

            try {
                $document['drainAttempts'] = $attempts + 1
                $document['lastDrainError'] = if ($detail.Length -gt 500) { $detail.Substring(0, 500) } else { $detail }
                Write-McpYamlObject -Path $record.FullName -Document $document
            } catch {
                [Console]::Error.WriteLine("Failsafe attempt counter update failed for '$($record.FullName)': $($_.Exception.Message)")
            }
        }
    }
    finally {
        $script:ReplFailsafeDraining = $false
    }

    return $summary
}

function Invoke-ReplFailsafeDrainOnFirstSuccess {
    <#
    .SYNOPSIS
        TR-MCP-REPL-016: run one queue drain after the first confirmed backend call.
    .DESCRIPTION
        The drain is wired here rather than to bootstrap on purpose. Bootstrap
        (Assert-ReplMarkerFresh) only proves the marker file is fresh, it does not
        prove the backend answers, and it runs inside Invoke-ReplRaw, so draining
        there would recurse. A successful Invoke-ReplRaw is the first point where
        reachability is proven, which is exactly the precondition for replay, and it
        covers every entry path (hooks, workflow verbs, direct client calls) with a
        single hook. Set MCP_FAILSAFE_DRAIN_DISABLED=1 to opt out.
    #>
    [CmdletBinding()]
    param()

    if ($script:ReplFailsafeDrainCompleted) { return }
    if ($script:ReplFailsafeDraining) { return }
    if ($env:MCP_FAILSAFE_DRAIN_DISABLED -eq '1') { return }

    # TR-MCP-PERSIST-003: do not block getFr/getTr on a nested SubmitAsync drain.
    if ($script:ReplRawInFlight) {
        $script:ReplFailsafeDrainDeferred = $true
        return
    }

    Invoke-ReplFailsafeDrainOnFirstSuccessCore
}

function Invoke-ReplFailsafeDrainOnFirstSuccessCore {
    # TR-MCP-FAILSAFE-001: do not latch completed on a backend-down abort.
    # A 503/backend_unavailable pass must be retryable in this process after
    # storage answers again.
    try {
        $summary = Invoke-ReplFailsafeDrain
        if ($summary.aborted) {
            return
        }
        $script:ReplFailsafeDrainCompleted = $true
        if ($summary.replayed -gt 0 -or $summary.quarantined -gt 0 -or $summary.failed -gt 0) {
            [Console]::Error.WriteLine(
                "Failsafe queue drain: replayed=$($summary.replayed) failed=$($summary.failed) quarantined=$($summary.quarantined) skipped=$($summary.skipped) dir='$($summary.failsafeDir)'.")
        }
    } catch {
        $detail = $_.Exception.Message
        if (Test-ReplFailsafeBackendUnreachable -Detail $detail) {
            return
        }
        [Console]::Error.WriteLine("Failsafe queue drain failed: $detail")
    }
}

function Invoke-ReplTurnUpsertParams {
    # Build as object, will be serialized to JSON in the caller (no text YAML).
    # This eliminates indentation and block-scalar errors.
    param(
        [Parameter(Mandatory)][string]$SourceType,
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][string]$RequestId,
        # TR-MCP-REPL-018: '' is a deliberate value meaning "omit the title so
        # the server preserves it" (TR-MCP-REPL-015). Mandatory alone rejects an
        # empty string at bind time, which killed every title-omitting persist
        # (appendDialog/appendActions/completeTurn/supersede) with
        # ParameterBindingValidationException (BUG-TRIAGE-087/089/091/098).
        [Parameter(Mandatory)][AllowEmptyString()][string]$Title,
        [Parameter(Mandatory)][string]$Status,
        [string]$ResponseText = '',
        [string]$ActionsYaml = '',
        [object[]]$ProcessingDialog = @(),
        [string]$Interpretation = '',
        [int]$TokenCount = 0,
        [string[]]$Tags = @(),
        [string[]]$ContextList = @(),
        [string]$PlanFile = '',
        [string]$TodoId = ''
    )

    $queryText = Get-ReplCurrentTurnQueryText
    if ([string]::IsNullOrWhiteSpace($queryText)) {
        # FR-MCP-SESSIONLIFE-001: complete/supersede recover a missing query.
        # Update, append, and fail omit it so the server QueryText stays.
        if ($Status -eq 'completed' -or $Status -eq 'canceled' -or $Status -eq 'cancelled') {
            $queryText = Get-ReplCurrentTurnValue -Key 'queryTitle'
            if ([string]::IsNullOrWhiteSpace($queryText)) {
                $queryText = 'Recovered session-log turn'
            }
        } else {
            $queryText = ''
        }
    }
    # FR-MCP-SESSIONLIFE-001: cancelled is accepted and stored as canceled.
    if ($Status -eq 'cancelled') { $Status = 'canceled' }
    $timestamp = Get-ReplCurrentTurnValue -Key 'openedAt'
    if (-not $timestamp) { $timestamp = (Get-Date -AsUTC -Format "yyyy-MM-ddTHH:mm:ssZ") }
    $model = Get-ReplSessionStateValue -Key 'model'
    if (-not $model) {
        $model = if ($env:MCP_SESSION_MODEL) { $env:MCP_SESSION_MODEL }
                 elseif ($env:PLUGIN_MODEL_DEFAULT) { $env:PLUGIN_MODEL_DEFAULT }
                 else { 'codex' }
    }

    $actions = @()
    if ($ActionsYaml) {
        $actionParams = Convert-ReplParamsYamlToObject -ParamsYaml ("actions:`n$ActionsYaml")
        if ($actionParams -and $actionParams.actions) {
            foreach ($action in @($actionParams.actions)) {
                $actions += (New-McpPluginActionRecord -Values $action).ToMap()
            }
        }
    }

    $filePaths = @($actions | ForEach-Object {
        if ($_ -is [hashtable] -and $_.ContainsKey('filePath') -and $_.filePath) {
            $_.filePath
        } elseif ($_.PSObject.Properties.Name -contains 'filePath' -and $_.filePath) {
            $_.filePath
        }
    } | Where-Object { $_ })

    $request = New-McpPluginTurnUpsertRequest `
        -Agent $SourceType `
        -SessionId $SessionId `
        -RequestId $RequestId `
        -Timestamp $timestamp `
        -QueryText $queryText `
        -Title $Title `
        -Status $Status `
        -ResponseText $ResponseText `
        -Model $model `
        -TokenCount $TokenCount `
        -Interpretation $Interpretation `
        -Tags $Tags `
        -ContextList $ContextList `
        -FilesModified $filePaths `
        -Actions $actions `
        -ProcessingDialog $ProcessingDialog `
        -PlanFile $PlanFile `
        -TodoId $TodoId

    return $request.ToParamsObject()
}

function Get-ReplStableFingerprint {
    # Canonical hash of one logical payload. Timestamps are excluded by the caller.
    param($Value)
    $json = ConvertTo-Json -InputObject $Value -Compress -Depth 20
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes([string]$json))
        return (([System.BitConverter]::ToString($bytes)) -replace '-', '')
    } finally {
        $sha.Dispose()
    }
}

function Find-ReplFailsafeByFingerprint {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$Fingerprint
    )
    if ([string]::IsNullOrWhiteSpace($Fingerprint)) { return '' }
    $dir = ''
    try { $dir = Get-ReplFailsafeDir } catch { return '' }
    if (-not (Test-Path -LiteralPath $dir -PathType Container)) { return '' }
    foreach ($file in @(Get-ChildItem -LiteralPath $dir -Filter '*.yaml' -File -ErrorAction SilentlyContinue)) {
        try {
            $doc = Read-McpYamlObject -Path $file.FullName
        } catch {
            continue
        }
        if (-not $doc) { continue }
        $storedMethod = [string](Get-ReplObjectValue -InputObject $doc -Name 'method')
        $storedFingerprint = [string](Get-ReplObjectValue -InputObject $doc -Name 'payloadFingerprint')
        if ($storedMethod -eq $Method -and $storedFingerprint -eq $Fingerprint) {
            return $file.FullName
        }
    }
    return ''
}

function New-ReplSessionVerbReceipt {
    # FR-MCP-SESSIONLIFE-003: one typed outcome for primary, confirmed queued, and lost writes.
    param(
        [Parameter(Mandatory)][ValidateSet('primary', 'queued', 'lost', 'unchanged', 'rejected')][string]$Disposition,
        [Parameter(Mandatory)][string]$Method,
        [AllowEmptyString()][string]$RequestId = '',
        [AllowEmptyString()][string]$FailsafePath = '',
        [AllowEmptyString()][string]$Message = '',
        [AllowEmptyString()][string]$ChildStderr = ''
    )

    $code = switch ($Disposition) {
        'primary' { 'persisted' }
        'queued' { 'queued' }
        'lost' { 'lost' }
        'unchanged' { 'unchanged' }
        'rejected' { 'rejected' }
    }
    $sessionId = ''
    $agent = ''
    try {
        $meta = Get-ReplSessionMeta
        if ($meta) {
            $sessionId = [string]$meta.SessionId
            $agent = [string]$meta.SourceType
        }
    } catch { }
    return [ordered]@{
        code = $code
        retryable = ($Disposition -eq 'queued')
        persisted = ($Disposition -eq 'primary')
        degraded = ($Disposition -eq 'queued')
        queued = ($Disposition -eq 'queued')
        method = $Method
        agent = $agent
        sessionId = $sessionId
        requestId = $RequestId
        failsafePath = $FailsafePath
        message = $Message
        childStderr = $ChildStderr
    }
}

function Publish-ReplSessionVerbReceipt {
    param([Parameter(Mandatory)]$Receipt)
    $script:LastReplPersistenceDetails = $Receipt
    try {
        $cache = Get-ReplInvokeCacheDir
        if ($cache) {
            Write-McpYamlObject -Path (Join-Path $cache 'session-verb-outcome.yaml') -Document $Receipt
        }
    } catch {
    }
    $code = [string](Get-ReplObjectValue -InputObject $Receipt -Name 'code')
    $persisted = Get-ReplObjectValue -InputObject $Receipt -Name 'persisted'
    $queued = Get-ReplObjectValue -InputObject $Receipt -Name 'queued'
    return ($persisted -eq $true -or $queued -eq $true -or $code -eq 'unchanged')
}

function Test-ReplSessionVerbQueued {
    if (-not $script:LastReplPersistenceDetails) { return $false }
    $queued = Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'queued'
    return ($queued -eq $true)
}

function Test-ReplExplicitParam {
    param(
        [string]$ParamsYaml,
        [Parameter(Mandatory)][string]$Name
    )
    if (-not $ParamsYaml) { return $false }
    $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    if (-not $params) { return $false }
    if ($params -is [System.Collections.IDictionary]) {
        return $params.Contains($Name)
    }
    return $null -ne $params.PSObject.Properties[$Name]
}

function Resolve-ReplPersistPlanTodo {
    param([string]$ParamsYaml = '')
    $planExplicit = Test-ReplExplicitParam -ParamsYaml $ParamsYaml -Name 'planFile'
    $todoExplicit = Test-ReplExplicitParam -ParamsYaml $ParamsYaml -Name 'todoId'
    $planFile = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'planFile'
    $todoId = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'todoId'
    # HV10: on an already-persisted turn, omitted plan/todo must stay omitted in the
    # submit payload (empty => New-McpPluginTurnUpsertRequest leaves the property off).
    # Materializing cached values or 'None' would replace server metadata.
    $persistedRaw = Get-ReplTurnCacheField -Field 'persisted'
    $isDurable = $persistedRaw -match '^(?i:true|1)$'
    if ($planExplicit) {
        if ([string]::IsNullOrWhiteSpace($planFile)) { $planFile = 'None' }
    } elseif ($isDurable) {
        $planFile = ''
    } else {
        $cachedPlan = Get-ReplTurnCacheField -Field 'planFile'
        $planFile = if (-not [string]::IsNullOrWhiteSpace($cachedPlan)) { $cachedPlan } else { 'None' }
    }
    if ($todoExplicit) {
        if ([string]::IsNullOrWhiteSpace($todoId)) { $todoId = 'None' }
    } elseif ($isDurable) {
        $todoId = ''
    } else {
        $cachedTodo = Get-ReplTurnCacheField -Field 'todoId'
        $todoId = if (-not [string]::IsNullOrWhiteSpace($cachedTodo)) { $cachedTodo } else { 'None' }
    }
    return [ordered]@{
        PlanFile = $planFile
        TodoId = $todoId
        PlanExplicit = $planExplicit
        TodoExplicit = $todoExplicit
        OmitPlan = (-not $planExplicit -and $isDurable)
        OmitTodo = (-not $todoExplicit -and $isDurable)
    }
}

function Set-ReplPersistPlanTodoArgs {
    # HV10/HV11: only bind PlanFile/TodoId when not omitted; write explicit values to cache.
    param(
        [Parameter(Mandatory)][System.Collections.IDictionary]$PersistArgs,
        [Parameter(Mandatory)]$MetaPT,
        [switch]$UpdateCache
    )
    $omitPlan = [bool](Get-ReplObjectValue -InputObject $MetaPT -Name 'OmitPlan')
    $omitTodo = [bool](Get-ReplObjectValue -InputObject $MetaPT -Name 'OmitTodo')
    $planExplicit = [bool](Get-ReplObjectValue -InputObject $MetaPT -Name 'PlanExplicit')
    $todoExplicit = [bool](Get-ReplObjectValue -InputObject $MetaPT -Name 'TodoExplicit')
    $planFile = [string](Get-ReplObjectValue -InputObject $MetaPT -Name 'PlanFile')
    $todoId = [string](Get-ReplObjectValue -InputObject $MetaPT -Name 'TodoId')
    if (-not $omitPlan) {
        $PersistArgs['PlanFile'] = $planFile
    }
    if (-not $omitTodo) {
        $PersistArgs['TodoId'] = $todoId
    }
    if ($UpdateCache) {
        # HV11: explicit metadata must land in current-turn.yaml before later verbs.
        $state = Read-ReplCurrentTurnState
        if ($state) {
            if ($planExplicit) { $state['planFile'] = $planFile }
            if ($todoExplicit) { $state['todoId'] = $todoId }
            if ($planExplicit -or $todoExplicit) {
                Write-ReplCurrentTurnState -State $state
            }
        }
    }
}

function Test-ReplTypedSessionMutationResult {
    # HV14: transport Success alone is not primary. Require typed identity match
    # (and retitled=true when the contract emits that flag).
    [CmdletBinding(DefaultParameterSetName = 'Turn')]
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][AllowEmptyString()][string]$Output,
        [Parameter(Mandatory)][string]$ExpectedSessionId,
        [Parameter(Mandatory, ParameterSetName = 'Turn')][string]$ExpectedRequestId,
        [Parameter(Mandatory, ParameterSetName = 'Session')][switch]$SessionOnly,
        [switch]$RequireRetitled
    )
    $response = $null
    try {
        $response = Convert-ReplParamsYamlToObject -ParamsYaml $Output
    } catch {
        $response = $null
    }
    if (-not $response) {
        try {
            $response = $Output | ConvertFrom-Json -ErrorAction Stop
        } catch {
            $response = $null
        }
    }
    if (-not $response) {
        return [ordered]@{ Ok = $false; Message = "$Method typed result missing or unparseable." }
    }
    $payload = Get-ReplObjectValue -InputObject $response -Name 'payload'
    $details = if ($payload) { Get-ReplObjectValue -InputObject $payload -Name 'result' } else { $null }
    if (-not $details) { $details = $response }
    $responseSession = [string](Get-ReplObjectValue -InputObject $details -Name 'sessionId')
    $responseRequest = [string](Get-ReplObjectValue -InputObject $details -Name 'requestId')
    # HV14: absent/blank/whitespace typed identity is not primary - require present matching ids.
    if ([string]::IsNullOrWhiteSpace($responseSession)) {
        return [ordered]@{ Ok = $false; Message = "$Method typed result sessionId is missing or blank." }
    }
    # HV17: ordinal case-sensitive identity (PowerShell -eq/-ne is case-insensitive by default).
    if (-not [string]::Equals($responseSession, $ExpectedSessionId, [System.StringComparison]::Ordinal)) {
        return [ordered]@{ Ok = $false; Message = "$Method typed result sessionId '$responseSession' does not match '$ExpectedSessionId'." }
    }
    if (-not $SessionOnly -and [string]::IsNullOrWhiteSpace($responseRequest)) {
        return [ordered]@{ Ok = $false; Message = "$Method typed result requestId is missing or blank." }
    }
    if (-not $SessionOnly -and -not [string]::Equals($responseRequest, $ExpectedRequestId, [System.StringComparison]::Ordinal)) {
        return [ordered]@{ Ok = $false; Message = "$Method typed result requestId '$responseRequest' does not match '$ExpectedRequestId'." }
    }
    if ($RequireRetitled) {
        $retitledRaw = Get-ReplObjectValue -InputObject $details -Name 'retitled'
        $retitled = $false
        if ($retitledRaw -is [bool]) { $retitled = [bool]$retitledRaw }
        elseif ($null -ne $retitledRaw) { $retitled = ([string]$retitledRaw) -match '^(?i:true|1)$' }
        if (-not $retitled) {
            return [ordered]@{ Ok = $false; Message = "$Method typed result did not confirm retitled=true." }
        }
    }
    return [ordered]@{ Ok = $true; Message = '' }
}

function Assert-ReplCallerRequestMatches {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = '',
        [string]$CachedRequestId = ''
    )
    $explicitRequest = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'requestId'
    if ([string]::IsNullOrWhiteSpace($explicitRequest)) { return $true }
    if ([string]::IsNullOrWhiteSpace($CachedRequestId)) { $CachedRequestId = Get-ReplTurnCacheField -Field 'turnRequestId' }
    # HV16: ordinal case-sensitive request equality (PowerShell -eq is case-insensitive by default).
    if ([string]::Equals($explicitRequest, $CachedRequestId, [System.StringComparison]::Ordinal)) { return $true }
    $reject = "$Method refused requestId '$explicitRequest' because the current turn is '$CachedRequestId'."
    [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method $Method -RequestId $CachedRequestId -Message $reject -ChildStderr $reject))
    [Console]::Error.WriteLine($reject)
    return $false
}

function Clear-ReplTurnDegradedMarkers {
    $state = Read-ReplCurrentTurnState
    if (-not $state) { return $false }
    $changed = $false
    if ($state.Contains('degraded')) { $state.Remove('degraded'); $changed = $true }
    if ($state.Contains('failsafePath')) { $state.Remove('failsafePath'); $changed = $true }
    if ($changed) { Write-ReplCurrentTurnState -State $state }
    return $true
}

function Invoke-ReplPersistTurn {
    # Build the complete session payload once, save it before the remote call, and
    # remove the local copy only after MCP confirms durable persistence.
    param(
        [Parameter(Mandatory)][string]$RequestId,
        [AllowEmptyString()][string]$Title = '',
        [switch]$IncludeSessionTitle,
        [Parameter(Mandatory)][string]$Status,
        [string]$ResponseText = '',
        [string]$ActionsYaml = '',
        [object[]]$ProcessingDialog = @(),
        [string]$Interpretation = '',
        [int]$TokenCount = 0,
        [string[]]$Tags = @(),
        [string[]]$ContextList = @(),
        [string]$PlanFile = '',
        [string]$TodoId = ''
    )
    $script:LastReplPersistenceDetails = $null
    $verbMethod = if ([string]::IsNullOrWhiteSpace([string]$script:ReplPersistVerbMethod)) { 'client.SessionLog.SubmitAsync' } else { [string]$script:ReplPersistVerbMethod }
    $script:ReplPersistVerbMethod = $null
    if ($env:MCP_PLUGIN_PERSIST_LOG) {
        $persistRecord = [ordered]@{
            requestId = $RequestId
            planFile = $PlanFile
            todoId = $TodoId
            status = $Status
            boundPlanFile = [bool]$PSBoundParameters.ContainsKey('PlanFile')
            boundTodoId = [bool]$PSBoundParameters.ContainsKey('TodoId')
            tags = @($Tags)
            contextList = @($ContextList)
        }
        Add-Content -LiteralPath $env:MCP_PLUGIN_PERSIST_LOG -Value ($persistRecord | ConvertTo-Json -Compress)
        # Test seam mimics a confirmed durable write: set persisted so beginTurn
        # durable-reopen (F3) omits planFile/todoId the same way production does.
        Set-ReplTurnCacheField -Field 'persisted' -Value 'true' | Out-Null
        Clear-ReplTurnDegradedMarkers | Out-Null
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition primary -Method $verbMethod -RequestId $RequestId -Message 'Test transport confirmed persistence.'))
        return $true
    }

    $meta = Get-ReplSessionMeta
    if (-not $meta) { throw 'Session log persistence failed because no session metadata is cached.' }

    $turnObj = Invoke-ReplTurnUpsertParams -SourceType $meta.SourceType -SessionId $meta.SessionId -RequestId $RequestId -Title $Title -Status $Status -ResponseText $ResponseText -ActionsYaml $ActionsYaml -ProcessingDialog $ProcessingDialog -Interpretation $Interpretation -TokenCount $TokenCount -Tags $Tags -ContextList $ContextList -PlanFile $PlanFile -TodoId $TodoId

    $workspaceFingerprint = ''
    try { $workspaceFingerprint = [string](Resolve-ReplWorkspaceDirectory) } catch { $workspaceFingerprint = '' }
    $logical = [ordered]@{
        method = 'client.SessionLog.SubmitAsync'
        sourceType = [string]$meta.SourceType
        sessionId = [string]$meta.SessionId
        workspace = $workspaceFingerprint
        requestId = $RequestId
        status = $Status
        title = $Title
        response = $ResponseText
        interpretation = $Interpretation
        tokenCount = $TokenCount
        tags = @($Tags)
        contextList = @($ContextList)
        actions = [string]$ActionsYaml
        processingDialog = @($ProcessingDialog)
        planFile = [string]$PlanFile
        todoId = [string]$TodoId
        queryText = [string](Get-ReplObjectValue -InputObject $turnObj.turn -Name 'queryText')
    }
    $fingerprint = Get-ReplStableFingerprint -Value $logical
    $cachedFingerprint = Get-ReplTurnCacheField -Field 'lastPersistFingerprint'
    if (-not [string]::IsNullOrWhiteSpace($cachedFingerprint) -and $cachedFingerprint -eq $fingerprint) {
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition unchanged -Method $verbMethod -RequestId $RequestId -Message "unchanged session payload did not create duplicate recovery work requestId=$RequestId"))
        return $true
    }
    $duplicateFailsafe = Find-ReplFailsafeByFingerprint -Method 'client.SessionLog.SubmitAsync' -Fingerprint $fingerprint
    # HV05: identical degraded retry must still attempt primary SubmitAsync rather
    # than returning queued solely because a matching failsafe already exists.
    $script:ReplPersistReuseFailsafePath = if ($duplicateFailsafe) { [string]$duplicateFailsafe } else { [string]::Empty }

    # TR-MCP-REPL-015: send the session title only when the caller explicitly seeds
    # or sets it (IncludeSessionTitle). Otherwise omit it so an incidental re-submit
    # never retitles the session; the server preserves the omitted field.
    $sessionTitle = Get-ReplSessionStateValue -Key 'title'
    $sessionStarted = Get-ReplSessionStateValue -Key 'started'
    if (-not $sessionStarted) { $sessionStarted = [string]$turnObj.turn.timestamp }
    $sessionLog = [ordered]@{
        sourceType = $meta.SourceType
        sessionId = $meta.SessionId
        model = [string]$turnObj.turn.model
        started = $sessionStarted
        lastUpdated = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        status = 'in_progress'
        turnCount = 1
        turns = @($turnObj.turn)
    }
    if ($IncludeSessionTitle -and -not [string]::IsNullOrWhiteSpace($sessionTitle)) {
        $sessionLog.title = $sessionTitle
    }

    $resolvedAgentHeaders = Resolve-McpPluginAgentHeaderFields -SessionId $meta.SessionId -CacheDir (Get-ReplInvokeCacheDir) -AgentName $meta.SourceType -HostName $env:MCP_PLUGIN_HOST
    $agentHeaderFields = [ordered]@{
        agentSessionId = Get-McpPluginFirstText @($env:MCP_AGENT_SESSION_ID, (Get-ReplSessionStateValue -Key 'agentSessionId'), $resolvedAgentHeaders.agentSessionId)
        # TR-MCP-PLUGIN-HEADER-001: existence-validated. A pre-fix session-state cache
        # or env var can still hold a fabricated <cache>/session.jsonl path; that value
        # must never be re-submitted just because it was cached earlier.
        agentSessionTranscriptFile = Get-McpPluginFirstExistingFile @($env:MCP_AGENT_SESSION_TRANSCRIPT_FILE, (Get-ReplSessionStateValue -Key 'agentSessionTranscriptFile'), $resolvedAgentHeaders.agentSessionTranscriptFile)
        agentExecutablePath = Get-McpPluginFirstText @($env:MCP_AGENT_EXECUTABLE_PATH, (Get-ReplSessionStateValue -Key 'agentExecutablePath'), $resolvedAgentHeaders.agentExecutablePath)
        agentExecutableVersion = Get-McpPluginFirstText @($env:MCP_AGENT_EXECUTABLE_VERSION, (Get-ReplSessionStateValue -Key 'agentExecutableVersion'), $resolvedAgentHeaders.agentExecutableVersion)
    }
    # TR-MCP-PLUGIN-HEADER-001: agentSessionId is the PROVIDER-NATIVE id. A pre-fix
    # cache may echo the MCP session id there; drop it rather than submit a value
    # that is mislabeled by definition.
    if ([string]$agentHeaderFields.agentSessionId -eq [string]$meta.SessionId) {
        $agentHeaderFields.agentSessionId = ''
    }
    foreach ($entry in $agentHeaderFields.GetEnumerator()) {
        if (-not [string]::IsNullOrWhiteSpace([string]$entry.Value)) {
            $sessionLog[$entry.Key] = [string]$entry.Value
        }
    }

    $payloadObject = [ordered]@{
        sessionLog = $sessionLog
    }
    $paramsYaml = ConvertTo-Yaml -Data $payloadObject -Options WithIndentedSequences
    $failsafePath = if (-not [string]::IsNullOrWhiteSpace([string]$script:ReplPersistReuseFailsafePath)) {
        [string]$script:ReplPersistReuseFailsafePath
    } else {
        Write-ReplFailsafe -Method 'client.SessionLog.SubmitAsync' -ParamsYaml $paramsYaml -Label 'session_submit' -PayloadFingerprint $fingerprint
    }
    $script:ReplPersistReuseFailsafePath = $null
    if (-not $failsafePath) {
        $lostMessage = "Session log persistence failed because the failsafe payload could not be saved for request '$RequestId'."
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition lost -Method $verbMethod -RequestId $RequestId -Message $lostMessage -ChildStderr $lostMessage))
        throw $lostMessage
    }

    $result = Invoke-ReplRaw -Method 'client.SessionLog.SubmitAsync' -ParamsYaml $paramsYaml
    if (-not $result.Success) {
        $combined = "$($result.Output) $($result.Error)"
        if ($combined -match 'timeout|timed out|command_timeout|backend_unavailable|HTTP 503|http 503') {
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition queued -Method $verbMethod -RequestId $RequestId -FailsafePath $failsafePath -Message 'Session log persist timed out or returned HTTP 503 backend_unavailable; failsafe retained and current-turn stays active.' -ChildStderr $combined))
            return $false
        }
        $rejectedMessage = "Session log persistence failed for request '$RequestId'. FailsafePath='$failsafePath'."
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method $verbMethod -RequestId $RequestId -FailsafePath $failsafePath -Message $rejectedMessage -ChildStderr $combined))
        throw "$rejectedMessage Output=$($result.Output) Error=$($result.Error)"
    }

    $response = Convert-ReplParamsYamlToObject -ParamsYaml $result.Output
    $payload = Get-ReplObjectValue -InputObject $response -Name 'payload'
    $details = Get-ReplObjectValue -InputObject $payload -Name 'result'
    if (-not $details) {
        $unconfirmed = "Session log persistence did not confirm a durable write for request '$RequestId' because the success envelope lacked result details. FailsafePath='$failsafePath'."
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method $verbMethod -RequestId $RequestId -FailsafePath $failsafePath -Message $unconfirmed -ChildStderr $unconfirmed))
        throw $unconfirmed
    }

    $persistedRaw = Get-ReplObjectValue -InputObject $details -Name 'persisted'
    $persisted = $false
    if ($persistedRaw -is [bool]) { $persisted = [bool]$persistedRaw }
    elseif ($null -ne $persistedRaw) { $persisted = ([string]$persistedRaw) -match '^(?i:true|1)$' }
    $degradedRaw = Get-ReplObjectValue -InputObject $details -Name 'degraded'
    $responseDegraded = $false
    if ($degradedRaw -is [bool]) { $responseDegraded = [bool]$degradedRaw }
    elseif ($null -ne $degradedRaw) { $responseDegraded = ([string]$degradedRaw) -match '^(?i:true|1)$' }
    $responseSession = [string](Get-ReplObjectValue -InputObject $details -Name 'sessionId')
    $responseRequest = [string](Get-ReplObjectValue -InputObject $details -Name 'requestId')
    $identityMismatch = $false
    # HV19: ordinal case-sensitive identity (PowerShell -ne is case-insensitive by default).
    if (-not [string]::IsNullOrWhiteSpace($responseSession) -and -not [string]::Equals($responseSession, [string]$meta.SessionId, [System.StringComparison]::Ordinal)) { $identityMismatch = $true }
    if (-not [string]::IsNullOrWhiteSpace($responseRequest) -and -not [string]::Equals($responseRequest, $RequestId, [System.StringComparison]::Ordinal)) { $identityMismatch = $true }
    if (-not $persisted -or $responseDegraded -or $identityMismatch) {
        $unconfirmed = "Session log persistence did not confirm a durable write for request '$RequestId'. FailsafePath='$failsafePath'."
        if ($responseDegraded) { $unconfirmed = "Session log persistence returned contradictory persisted+degraded for request '$RequestId'. FailsafePath='$failsafePath'." }
        if ($identityMismatch) { $unconfirmed = "Session log persistence returned mismatched identity for request '$RequestId'. FailsafePath='$failsafePath'." }
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method $verbMethod -RequestId $RequestId -FailsafePath $failsafePath -Message $unconfirmed -ChildStderr $unconfirmed))
        throw $unconfirmed
    }

    Clear-ReplFailsafe -Path $failsafePath
    Set-ReplTurnCacheField -Field 'lastPersistFingerprint' -Value $fingerprint | Out-Null
    Set-ReplTurnCacheField -Field 'persisted' -Value 'true' | Out-Null
    Clear-ReplTurnDegradedMarkers | Out-Null
    [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition primary -Method $verbMethod -RequestId $RequestId -Message "Session log persist confirmed a durable write for request '$RequestId'."))
    return $true
}
function Update-ReplTurnCacheStatus {
    param([Parameter(Mandatory)][string]$NewStatus)
    $state = Read-ReplCurrentTurnState
    if (-not $state) { return $false }
    $state['status'] = $NewStatus
    Write-ReplCurrentTurnState -State $state
    return $true
}

function Update-ReplTurnCacheEdits {
    param([Parameter(Mandatory)][int]$Increment)
    $state = Read-ReplCurrentTurnState
    if (-not $state) { return $false }
    $current = if ($state.Contains('codeEdits')) { [int]$state['codeEdits'] } else { 0 }
    $state['codeEdits'] = $current + $Increment
    Write-ReplCurrentTurnState -State $state
    return $true
}

function Get-ReplTurnCacheField {
    param([Parameter(Mandatory)][string]$Field)
    $state = Read-ReplCurrentTurnState
    if (-not $state -or -not $state.Contains($Field) -or $null -eq $state[$Field]) { return '' }
    return [string]$state[$Field]
}

function Set-ReplTurnCacheField {
    param(
        [Parameter(Mandatory)][string]$Field,
        [Parameter(Mandatory)][string]$Value
    )
    $state = Read-ReplCurrentTurnState
    if (-not $state) { return $false }
    $state[$Field] = $Value
    Write-ReplCurrentTurnState -State $state
    return $true
}

function Get-ReplNormalizedActionsBlock {
    # Returns bare list content under the 'actions:' key (common indent stripped).
    # Twin of _repl_normalized_actions_block + _repl_list_block_get.
    # Safe for map-style turn docs (preserves nesting) and top-level actions: lists.
    param([string]$ParamsYaml)
    if (-not $ParamsYaml) { return '' }
    $text = $ParamsYaml -replace "`r`n", "`n" -replace "`r", ""
    $lines = $text -split "`n"
    $capture = $false
    $keyIndent = -1
    $stripIndent = -1
    $result = @()
    foreach ($line in $lines) {
        if (-not $capture) {
            if ($line -match '^\s*actions:\s*$') {
                $capture = $true
                $m = [regex]::Match($line, '^(\s*)')
                $keyIndent = $m.Groups[1].Value.Length
                continue
            }
            continue
        }
        if ($line -match '^\s*$') {
            $result += $line
            continue
        }
        $m = [regex]::Match($line, '^(\s*)')
        $lineIndent = $m.Groups[1].Value.Length
        if ($lineIndent -le $keyIndent -and $line -notmatch '^\s*-') {
            break
        }
        if ($stripIndent -lt 0) { $stripIndent = $lineIndent }
        if ($stripIndent -gt 0 -and $line.Length -ge $stripIndent) {
            $result += $line.Substring($stripIndent)
        } else {
            $result += $line
        }
    }
    return ($result -join "`n").TrimEnd()
}

function Update-ReplTurnAudit {
    param([Parameter(Mandatory)][string]$Field, [int]$Increment = 0)
    if ($Increment -le 0) { return $false }
    $state = Read-ReplCurrentTurnState
    if (-not $state) { return $false }
    $current = if ($state.Contains($Field)) { [int]$state[$Field] } else { 0 }
    $state[$Field] = $current + $Increment
    Write-ReplCurrentTurnState -State $state
    return $true
}

function Get-ReplObjectValue {
    param(
        $InputObject,
        [Parameter(Mandatory)][string]$Name
    )

    if ($null -eq $InputObject) { return $null }
    if ($InputObject -is [System.Collections.IDictionary]) {
        if ($InputObject.Contains($Name)) { return $InputObject[$Name] }
        return $null
    }

    $property = $InputObject.PSObject.Properties[$Name]
    if ($property) { return $property.Value }
    return $null
}

function Get-ReplParamString {
    param(
        [string]$ParamsYaml,
        [Parameter(Mandatory)][string]$Name
    )
    if (-not $ParamsYaml) { return '' }
    $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    if (-not $params) { return '' }
    $value = Get-ReplObjectValue -InputObject $params -Name $Name
    if ($null -eq $value) { return '' }
    return [string]$value
}

function Update-ReplTurnTitleFromParams {
    param([string]$ParamsYaml)
    $queryTitle = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'queryTitle'
    if ([string]::IsNullOrWhiteSpace($queryTitle)) { return $false }
    return Set-ReplTurnCacheField -Field 'queryTitle' -Value $queryTitle
}

function Assert-ReplCurrentTurnFresh {
    param([Parameter(Mandatory)][string]$Method)

    $turnFile = Get-ReplCurrentTurnFile
    $turnState = Read-ReplCurrentTurnState
    if (-not $turnState) { return $false }

    $sessionFile = Join-Path (Get-ReplInvokeCacheDir) 'session-state.yaml'
    $sessionState = Read-McpYamlObject -Path $sessionFile -Create
    $workspace = Resolve-ReplWorkspaceDirectory
    $snapshot = $null
    try {
        $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
    } catch {
        $snapshot = $null
    }

    $staleReasons = @()
    $turnSessionId = if ($turnState.Contains('sessionId')) { [string]$turnState['sessionId'] } else { '' }
    $activeSessionId = if ($sessionState.Contains('sessionId')) { [string]$sessionState['sessionId'] } else { '' }
    $turnMarkerPath = if ($turnState.Contains('markerFilePath')) { [string]$turnState['markerFilePath'] } else { '' }
    $turnMarkerWriteUtc = if ($turnState.Contains('markerLastWriteUtc')) { [string]$turnState['markerLastWriteUtc'] } else { '' }

    if ($turnSessionId -and $activeSessionId -and $turnSessionId -ne $activeSessionId) {
        $staleReasons += 'sessionId'
    }
    if ($snapshot -and $turnMarkerPath -and $turnMarkerPath -ne $snapshot.markerFilePath) {
        $staleReasons += 'markerFilePath'
    }
    if ($snapshot -and $turnMarkerWriteUtc -and $turnMarkerWriteUtc -ne $snapshot.markerLastWriteUtc) {
        $staleReasons += 'markerLastWriteUtc'
    }

    # FR-MCP-SESSIONLIFE-002: a different marker path is a wrong workspace.
    # Reject it without rewriting the bound session id. Same-path timestamp
    # drift still refreshes below.
    if ($snapshot -and -not [string]::IsNullOrWhiteSpace($turnMarkerPath) -and $turnMarkerPath -ne $snapshot.markerFilePath) {
        [Console]::Error.WriteLine("$Method rejected wrong-workspace marker '$turnFile'. turnMarker='$turnMarkerPath' activeMarker='$($snapshot.markerFilePath)' sessionId='$turnSessionId'.")
        return $false
    }

    # TR-MCP-PLUGIN-012: a sessionId-only mismatch means the session rotated (Start-PluginSession
    # minted a new id) while the turn cache still carries the old one. Re-bind the turn to the active
    # session (below) instead of hard-rejecting every subsequent completeTurn (BUG-TRIAGE-071/075).
    # Marker drift (wrong-workspace) is still rejected separately.
    if ($staleReasons -contains 'sessionId') {
        if (-not (Test-ReplSessionSourceTypeCompatible -Left $turnSessionId -Right $activeSessionId)) {
            $turnPrefix = Get-ReplSessionIdSourceTypePrefix -SessionId $turnSessionId
            $activePrefix = Get-ReplSessionIdSourceTypePrefix -SessionId $activeSessionId
            [Console]::Error.WriteLine("$Method refused current-turn cache '$turnFile' because sourceType prefix '$turnPrefix' differs from active '$activePrefix'.")
            return $false
        }
        [Console]::Error.WriteLine("$Method re-binding current-turn cache '$turnFile' from rotated sessionId '$turnSessionId' to active sessionId '$activeSessionId'.")
    }

    $markerDriftReasons = @($staleReasons | Where-Object { $_ -eq 'markerFilePath' -or $_ -eq 'markerLastWriteUtc' })
    if ($markerDriftReasons.Count -gt 0) {
        if (-not (Assert-ReplMarkerFresh)) {
            $markerPath = if ($snapshot) { $snapshot.markerFilePath } else { '' }
            $markerLastWriteUtc = if ($snapshot) { $snapshot.markerLastWriteUtc } else { '' }
            [Console]::Error.WriteLine("$Method rejected stale current-turn cache '$turnFile'. staleSessionId='$turnSessionId' activeSessionId='$activeSessionId' markerFilePath='$markerPath' markerLastWriteUtc='$markerLastWriteUtc' staleReasons='$($markerDriftReasons -join ',')'. $(Get-ReplRecoveryGuidance)")
            return $false
        }

        $sessionState = Read-McpYamlObject -Path $sessionFile -Create
        $activeSessionId = if ($sessionState.Contains('sessionId')) { [string]$sessionState['sessionId'] } else { '' }

        try {
            $snapshot = Get-MarkerFileSnapshot -StartDir $workspace
        } catch {
            $snapshot = $null
        }
    }

    # HV12: persisted turns require workspace identity proof and must not manufacture
    # markers. Non-persisted / markerless-workspace probes still proceed without
    # synthesizing proof (so mismatch-missing-marker hashes stay stable).
    $persistedRaw = if ($turnState.Contains('persisted')) { [string]$turnState['persisted'] } else { '' }
    $isPersistedTurn = $persistedRaw -match '^(?i:true|1)$'
    if ($isPersistedTurn -and ([string]::IsNullOrWhiteSpace($turnMarkerPath) -or [string]::IsNullOrWhiteSpace($turnMarkerWriteUtc))) {
        [Console]::Error.WriteLine("$Method rejected current-turn cache '$turnFile' because workspace identity proof is missing.")
        return $false
    }

    # TR-MCP-PLUGIN-012 AC1: session rotation rewrites current-turn.yaml sessionId
    # to the active session. Persist still uses Get-ReplCompleteTurnPersistSessionId
    # on the (now rebound) turn value. Fill an empty turn sessionId from active too.
    # FR-MCP-SESSIONLIFE-002: fill only an empty turn session id. Do not rebind
    # a cached session onto a post-restart active session.
    if ($activeSessionId -and -not $turnSessionId) {
        $turnState['sessionId'] = $activeSessionId
    }
    # Refresh same-path marker timestamp only when proof already exists; never
    # manufacture markers for markerless/non-persisted turns (HV12/HV13).
    if ($snapshot -and -not [string]::IsNullOrWhiteSpace($turnMarkerPath) -and $turnMarkerPath -eq $snapshot.markerFilePath) {
        $turnState['markerFilePath'] = $snapshot.markerFilePath
        $turnState['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
    }
    Write-ReplCurrentTurnState -State $turnState

    return $true
}

function New-ReplBeginTurnRequestId {
    return ('req-{0}-turn-{1:x4}' -f (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'), (Get-Random -Maximum 0xffff))
}

function Get-ReplServerDialogCount {
    # TEST-MCP-SESSIONLIFE-005: count processingDialog items stored for one turn.
    # Returns -1 when the query cannot see that turn, so complete does not invent a count.
    param([Parameter(Mandatory)][string]$RequestId)

    $meta = Get-ReplSessionMeta
    if (-not $meta) { return -1 }

    try {
        $callParams = [ordered]@{
            agent = $meta.SourceType
            limit = 25
        }
        $callYaml = ConvertTo-Yaml -Data $callParams -Options WithIndentedSequences
        $result = Invoke-ReplRaw -Method 'client.SessionLog.QueryAsync' -ParamsYaml $callYaml
        if (-not $result.Success) { return -1 }

        $response = Convert-ReplParamsYamlToObject -ParamsYaml $result.Output
        $payload = Get-ReplObjectValue -InputObject $response -Name 'payload'
        $details = Get-ReplObjectValue -InputObject $payload -Name 'result'
        if ($null -eq $details) { $details = $payload }
        $items = Get-ReplObjectValue -InputObject $details -Name 'items'
        foreach ($session in @($items)) {
            if ([string](Get-ReplObjectValue -InputObject $session -Name 'sessionId') -ne $meta.SessionId) { continue }
            foreach ($turn in @((Get-ReplObjectValue -InputObject $session -Name 'turns'))) {
                if ([string](Get-ReplObjectValue -InputObject $turn -Name 'requestId') -ne $RequestId) { continue }
                $dialog = Get-ReplObjectValue -InputObject $turn -Name 'processingDialog'
                if ($null -eq $dialog) { return 0 }
                return @($dialog).Count
            }
        }
    } catch {
        return -1
    }

    return -1
}

function Resolve-ReplSupersedeTitle {
    # TR-MCP-REPL-019 AC-001: choose the title persisted with a superseded turn
    # from the local cache alone (BUG-TRIAGE-086; D1, triage-report-124a374c):
    #   - A locally refined title (non-empty and different from the hook's raw
    #     default: the prompt first line or the literal 'User prompt') is sent.
    #   - An empty or raw-default local title is omitted (''): the turn upsert then
    #     carries no queryTitle and the server keeps the title it stores, refined
    #     or not, under the additive turn merge (FR-SUPPORT-015). No server read is
    #     made; the whole-session query it took outran the UserPromptSubmit budget.
    #   - Raw prompt text is never sent as a title (TR-MCP-REPL-015).
    param([Parameter(Mandatory)]$State)

    $localTitle = if ($State.Contains('queryTitle')) { [string]$State['queryTitle'] } else { '' }
    $queryText = if ($State.Contains('queryText')) { [string]$State['queryText'] } else { '' }
    $rawDefaults = @('User prompt')
    $promptFirstLine = @(($queryText -replace "`r`n", "`n" -replace "`r", "") -split "`n")[0].Trim()
    if ($promptFirstLine) { $rawDefaults += $promptFirstLine }

    $trimmedLocal = $localTitle.Trim()
    if ($trimmedLocal -and ($rawDefaults -notcontains $trimmedLocal)) {
        return $localTitle
    }
    return ''
}

function Invoke-ReplSupersedeCurrentTurnIfInProgress {
    param([Parameter(Mandatory)][string]$NextRequestId)

    $state = Read-ReplCurrentTurnState
    if (-not $state) { return }
    $status = if ($state.Contains('status')) { [string]$state['status'] } else { '' }
    if ($status -ne 'in_progress') { return }
    $oldRequestId = if ($state.Contains('turnRequestId')) { [string]$state['turnRequestId'] } else { '' }
    if (-not $oldRequestId -or $oldRequestId -eq $NextRequestId) { return }
    try {
        # TR-MCP-REPL-019 AC-001: persist the superseded turn with a title that can
        # never clobber a refined title: a locally refined title is kept, and a raw
        # or empty local title is omitted so the server keeps its stored title.
        $title = Resolve-ReplSupersedeTitle -State $state
        # Durable turns omit unbound links; first writes preserve cached links before None.
        $script:ReplPersistVerbMethod = 'workflow.sessionlog.beginTurn'
        $persistArgs = @{
            RequestId = $oldRequestId
            Title = $title
            Status = 'canceled'
            ResponseText = "Superseded by $NextRequestId before it was completed."
        }
        Set-ReplPersistPlanTodoArgs -PersistArgs $persistArgs -MetaPT (Resolve-ReplPersistPlanTodo)
        [void](Invoke-ReplPersistTurn @persistArgs)
    } catch {
        [Console]::Error.WriteLine("workflow.sessionlog.beginTurn could not persist superseded turn '$oldRequestId': $_")
    }
}

function Invoke-WorkflowBeginTurn {
    param([string]$ParamsYaml)

    $sessionId = Get-ReplSessionStateValue -Key 'sessionId'
    if ([string]::IsNullOrWhiteSpace($sessionId)) {
        [Console]::Error.WriteLine("workflow.sessionlog.beginTurn requires session-state.yaml with sessionId. $(Get-ReplRecoveryGuidance)")
        return $false
    }

    $requestId = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'requestId'
    if ([string]::IsNullOrWhiteSpace($requestId)) { $requestId = New-ReplBeginTurnRequestId }
    $queryTitle = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'queryTitle'
    if ([string]::IsNullOrWhiteSpace($queryTitle)) { $queryTitle = 'User prompt' }
    $queryText = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'queryText'
    if ([string]::IsNullOrWhiteSpace($queryText)) { $queryText = $queryTitle }
    $planFile = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'planFile'
    $todoId = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'todoId'
    $planExplicit = Test-ReplExplicitParam -ParamsYaml $ParamsYaml -Name 'planFile'
    $todoExplicit = Test-ReplExplicitParam -ParamsYaml $ParamsYaml -Name 'todoId'
    # TR-MCP-PLUGIN-013 AC-005 (PLAN-SESSIONTRIAGEFIX-001 D3): optional contextList and tags for a new turn's first write, so a background
    # turn names its root session and root turn from the start (completeTurn does not send them).
    $beginContextList = [string[]]@()
    $beginTags = [string[]]@()
    $beginParams = if ($ParamsYaml) { Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml } else { $null }
    if ($beginParams) {
        $contextValue = Get-ReplObjectValue -InputObject $beginParams -Name 'contextList'
        if ($null -ne $contextValue) { $beginContextList = ConvertTo-McpPluginStringList -Value $contextValue }
        $tagValue = Get-ReplObjectValue -InputObject $beginParams -Name 'tags'
        if ($null -ne $tagValue) { $beginTags = ConvertTo-McpPluginStringList -Value $tagValue }
    }
    $currentTurnId = Get-ReplCurrentTurnValue -Key 'turnRequestId'
    $cachedPlan = Get-ReplCurrentTurnValue -Key 'planFile'
    $cachedTodo = Get-ReplCurrentTurnValue -Key 'todoId'
    $cachedDegraded = Get-ReplCurrentTurnValue -Key 'degraded'
    $cachedPersisted = Get-ReplCurrentTurnValue -Key 'persisted'
    $cachedSessionId = Get-ReplCurrentTurnValue -Key 'sessionId'
    # HV20: ordinal case-sensitive reopen/session match (PowerShell -eq is case-insensitive by default).
    $isReopen = (-not [string]::IsNullOrWhiteSpace($currentTurnId) -and [string]::Equals($currentTurnId, $requestId, [System.StringComparison]::Ordinal))
    $isDegradedTurn = $cachedDegraded -match '^(?i:true|1)$'
    # HV02: empty cached sessionId is NOT a match. Durable omission requires a
    # non-empty matching sessionId plus persisted proof and workspace freshness.
    $sessionMatches = (-not [string]::IsNullOrWhiteSpace($cachedSessionId)) -and [string]::Equals($cachedSessionId, $sessionId, [System.StringComparison]::Ordinal)
    # HV20: case-different requestId is not a new turn and must not overwrite a bound turn.
    if (-not [string]::IsNullOrWhiteSpace($currentTurnId) -and -not [string]::IsNullOrWhiteSpace($requestId) -and -not $isReopen -and [string]::Equals($currentTurnId, $requestId, [System.StringComparison]::OrdinalIgnoreCase)) {
        $reject = "workflow.sessionlog.beginTurn refused case-different requestId '$requestId' because the current turn is '$currentTurnId'."
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.beginTurn' -RequestId $currentTurnId -Message $reject -ChildStderr $reject))
        [Console]::Error.WriteLine($reject)
        return $false
    }
    $hasDurableProof = $cachedPersisted -match '^(?i:true|1)$'
    # HV12: reopen must not migrate or manufacture workspace identity. Reject wrong
    # markers (including degraded) and incomplete marker proof on persisted turns.
    if ($isReopen) {
        $reopenMarkerPath = Get-ReplCurrentTurnValue -Key 'markerFilePath'
        $reopenMarkerWriteUtc = Get-ReplCurrentTurnValue -Key 'markerLastWriteUtc'
        $reopenSnapshot = $null
        try { $reopenSnapshot = Get-MarkerFileSnapshot -StartDir (Resolve-ReplWorkspaceDirectory) } catch { $reopenSnapshot = $null }
        if ($reopenSnapshot -and -not [string]::IsNullOrWhiteSpace($reopenMarkerPath) -and $reopenMarkerPath -ne $reopenSnapshot.markerFilePath) {
            $reject = "workflow.sessionlog.beginTurn rejected wrong-workspace marker for requestId=$requestId. turnMarker='$reopenMarkerPath' activeMarker='$($reopenSnapshot.markerFilePath)'."
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.beginTurn' -RequestId $requestId -Message $reject -ChildStderr $reject))
            [Console]::Error.WriteLine($reject)
            return $false
        }
        if ($hasDurableProof -and ([string]::IsNullOrWhiteSpace($reopenMarkerPath) -or [string]::IsNullOrWhiteSpace($reopenMarkerWriteUtc))) {
            $reject = "workflow.sessionlog.beginTurn refused durable reopen for requestId=$requestId because workspace identity proof is missing."
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.beginTurn' -RequestId $requestId -Message $reject -ChildStderr $reject))
            [Console]::Error.WriteLine($reject)
            return $false
        }
    }
    $workspaceFresh = $true
    if ($isReopen -and $sessionMatches -and $hasDurableProof -and -not $isDegradedTurn) {
        $workspaceFresh = [bool](Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.beginTurn')
        if (-not $workspaceFresh) {
            $reject = "workflow.sessionlog.beginTurn refused durable reopen for requestId=$requestId because workspace/session identity proof failed."
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.beginTurn' -RequestId $requestId -Message $reject -ChildStderr $reject))
            [Console]::Error.WriteLine($reject)
            return $false
        }
    }
    $isDurableReopen = $isReopen -and $sessionMatches -and $hasDurableProof -and -not $isDegradedTurn -and $workspaceFresh
    if ($isReopen -and -not $sessionMatches) {
        $reject = "workflow.sessionlog.beginTurn refused to rebind session '$cachedSessionId' to '$sessionId' for requestId=$requestId"
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.beginTurn' -RequestId $requestId -Message $reject -ChildStderr $reject))
        [Console]::Error.WriteLine($reject)
        return $false
    }
    if (-not $isDurableReopen -and (($planExplicit -and [string]::IsNullOrWhiteSpace($planFile)) -or ($todoExplicit -and [string]::IsNullOrWhiteSpace($todoId)))) {
        $reject = "workflow.sessionlog.beginTurn rejected whitespace metadata for ordinary or degraded first persistence requestId=$requestId"
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.beginTurn' -RequestId $requestId -Message $reject -ChildStderr $reject))
        [Console]::Error.WriteLine($reject)
        return $false
    }
    if (-not $isDurableReopen) {
        if ([string]::IsNullOrWhiteSpace($planFile)) { $planFile = $(if ($isReopen -and -not [string]::IsNullOrWhiteSpace($cachedPlan)) { $cachedPlan } else { 'None' }) }
        if ([string]::IsNullOrWhiteSpace($todoId)) { $todoId = $(if ($isReopen -and -not [string]::IsNullOrWhiteSpace($cachedTodo)) { $cachedTodo } else { 'None' }) }
    }

    $existingTurn = Read-ReplCurrentTurnState
    $preserveDegraded = ($isReopen -and $isDegradedTurn -and $existingTurn)
    $preserveDurable = ($isDurableReopen -and $existingTurn)
    if ($preserveDegraded -or $preserveDurable) {
        $turnState = $existingTurn
        $turnState['turnRequestId'] = $requestId
        $turnState['sessionId'] = $sessionId
        $turnState['status'] = 'in_progress'
        if ($preserveDurable) {
            $turnState['queryTitle'] = $queryTitle
            $turnState['queryText'] = $queryText
            if ($planExplicit -and -not [string]::IsNullOrWhiteSpace($planFile)) { $turnState['planFile'] = $planFile }
            if ($todoExplicit -and -not [string]::IsNullOrWhiteSpace($todoId)) { $turnState['todoId'] = $todoId }
        } elseif ($planExplicit -or $todoExplicit) {
            if ($planExplicit -and -not [string]::IsNullOrWhiteSpace($planFile)) { $turnState['planFile'] = $planFile }
            if ($todoExplicit -and -not [string]::IsNullOrWhiteSpace($todoId)) { $turnState['todoId'] = $todoId }
        }
    } else {
        $openedAt = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        $turnState = [ordered]@{
            turnRequestId = $requestId
            queryTitle = $queryTitle
            openedAt = $openedAt
            status = 'in_progress'
            sessionId = $sessionId
            codeEdits = 0
            lastBuildStatus = 'unknown'
            auditActions = 0
            auditDialog = 0
            auditDecisions = 0
            auditFiles = 0
            auditCommits = 0
            queryText = $queryText
        }
        if (-not $isDurableReopen) {
            $turnState['planFile'] = $planFile
            $turnState['todoId'] = $todoId
        }
        if ($beginContextList.Count -gt 0) { $turnState['contextList'] = @($beginContextList) }
        if ($beginTags.Count -gt 0) { $turnState['tags'] = @($beginTags) }
    }

    # HV02: durable reopen must keep the bound marker proof; do not overwrite
    # identity fields from a different workspace snapshot.
    if (-not $isDurableReopen) {
        try {
            $snapshot = Get-MarkerFileSnapshot -StartDir (Resolve-ReplWorkspaceDirectory)
            $turnState['markerFilePath'] = $snapshot.markerFilePath
            $turnState['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
        } catch {
        }
    }

    try {
        Invoke-ReplSupersedeCurrentTurnIfInProgress -NextRequestId $requestId
        Write-ReplCurrentTurnState -State $turnState
        # TR-MCP-REPL-015: seed the session title from the first turn only. Once
        # session-state has a title, later turns omit it (so the session is not
        # retitled to each new prompt); setSessionTitle changes it explicitly.
        $existingSessionTitle = Get-ReplSessionStateValue -Key 'title'
        $seedSessionTitle = ([string]::IsNullOrWhiteSpace($existingSessionTitle) -and -not [string]::IsNullOrWhiteSpace($queryTitle))
        if ($seedSessionTitle) {
            Set-ReplSessionStateValue -Key 'title' -Value $queryTitle | Out-Null
        }
        if ($isDurableReopen) {
            $script:ReplPersistVerbMethod = 'workflow.sessionlog.beginTurn'
            $persistArgs = @{
                RequestId = $requestId
                Title = $queryTitle
                Status = 'in_progress'
                ResponseText = '(turn opened)'
            }
            if ($planExplicit -and -not [string]::IsNullOrWhiteSpace($planFile)) { $persistArgs.PlanFile = $planFile }
            if ($todoExplicit -and -not [string]::IsNullOrWhiteSpace($todoId)) { $persistArgs.TodoId = $todoId }
            $persisted = [bool](Invoke-ReplPersistTurn @persistArgs -IncludeSessionTitle:$seedSessionTitle)
        } else {
            $script:ReplPersistVerbMethod = 'workflow.sessionlog.beginTurn'
            $persistArgs = @{
                RequestId = $requestId
                Title = $queryTitle
                Status = 'in_progress'
                ResponseText = '(turn opened)'
                IncludeSessionTitle = $seedSessionTitle
                PlanFile = $planFile
                TodoId = $todoId
            }
            # TR-MCP-PLUGIN-013 AC-005 (D3): only a turn opened with contextList or tags sends them, so every other turn's first write
            # keeps its exact earlier shape.
            if ($beginTags.Count -gt 0) { $persistArgs.Tags = $beginTags }
            if ($beginContextList.Count -gt 0) { $persistArgs.ContextList = $beginContextList }
            $persisted = [bool](Invoke-ReplPersistTurn @persistArgs)
        }
        if (-not $persisted) {
            $degraded = $false
            if ($script:LastReplPersistenceDetails) {
                $degraded = [bool](Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'degraded')
            }
            $failsafePath = ''
            if ($script:LastReplPersistenceDetails) {
                $failsafePath = [string](Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'failsafePath')
            }
            $turnFile = Get-ReplCurrentTurnFile
            $beginResult = Complete-ReplBeginTurnAfterPersist -Persisted $persisted -Degraded $degraded -FailsafePath $failsafePath -CurrentTurnFile $turnFile -TurnState @{
                turnRequestId = $requestId
                sessionId = $sessionId
            }
            if ($beginResult.degraded) {
                [Console]::Error.WriteLine("workflow.sessionlog.beginTurn queued/degraded for '$requestId'. Failsafe retained; current-turn remains active.")
                return $true
            }
            [Console]::Error.WriteLine("workflow.sessionlog.beginTurn did not confirm durable persistence for '$requestId'.")
            return $false
        }
        return $true
    } catch {
        [Console]::Error.WriteLine("workflow.sessionlog.beginTurn failed for '$requestId': $_")
        return $false
    }
}

function Invoke-WorkflowAppendActions {
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.appendActions')
    }
    # HV13: request guard before freshness so a mismatch cannot mutate marker fields.
    if (-not (Assert-ReplCallerRequestMatches -Method 'workflow.sessionlog.appendActions' -ParamsYaml $ParamsYaml)) {
        return $false
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.appendActions')) {
        return $false
    }

    $added = 0
    $actionsBlock = ''
    if ($ParamsYaml) {
        $p = $ParamsYaml -replace "`r`n", "`n" -replace "`r", ""
        $actionsBlock = Get-ReplNormalizedActionsBlock -ParamsYaml $p
        # Count only real filePath: fields (with value) for codeEdits. Substring matches in
        # descriptions must be ignored. Non-file actions (design_decision etc.) must persist.
        $added = ([regex]::Matches($p, '(?m)^\s*(?:-\s*)?filePath:\s*\S')).Count
    }

    if ($ParamsYaml -and $ParamsYaml.Trim()) {
        $explicitTitle = [bool](Update-ReplTurnTitleFromParams -ParamsYaml $ParamsYaml)
        if ($added -gt 0) {
            Update-ReplTurnCacheEdits -Increment $added | Out-Null
        }
        $actionC = ([regex]::Matches($ParamsYaml, '(?m)^\s*(?:-\s*)?type:')).Count
        $decC = ([regex]::Matches($ParamsYaml, '(?m)^\s*(?:-\s*)?type:\s*design_decision\b')).Count
        $comC = ([regex]::Matches($ParamsYaml, '(?m)^\s*(?:-\s*)?type:\s*commit\b')).Count
        Update-ReplTurnAudit -Field 'auditActions' -Increment $actionC | Out-Null
        Update-ReplTurnAudit -Field 'auditFiles' -Increment $added | Out-Null
        Update-ReplTurnAudit -Field 'auditDecisions' -Increment $decC | Out-Null
        Update-ReplTurnAudit -Field 'auditCommits' -Increment $comC | Out-Null

        $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
        # TR-MCP-REPL-015: send the turn title only when explicitly set this call;
        # otherwise omit so a stale cache title cannot clobber the server value.
        $title = if ($explicitTitle) { Get-ReplTurnCacheField -Field 'queryTitle' } else { '' }
        $script:ReplPersistVerbMethod = 'workflow.sessionlog.appendActions'
        $metaPT = Resolve-ReplPersistPlanTodo -ParamsYaml $ParamsYaml
        # HV11: explicit append metadata must update the turn cache before later verbs.
        $persistArgs = @{
            RequestId = $reqId
            Title = $title
            Status = 'in_progress'
            ResponseText = 'Actions appended.'
            ActionsYaml = $actionsBlock
        }
        Set-ReplPersistPlanTodoArgs -PersistArgs $persistArgs -MetaPT $metaPT -UpdateCache
        $persisted = [bool](Invoke-ReplPersistTurn @persistArgs)
        if (-not $persisted) {
            $queued = $false
            if ($script:LastReplPersistenceDetails) {
                $queued = [bool](Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'queued')
            }
            if ($queued) {
                [Console]::Error.WriteLine("workflow.sessionlog.appendActions queued for '$reqId'. Failsafe retained; primary persistence was not claimed.")
                return $true
            }
            [Console]::Error.WriteLine("workflow.sessionlog.appendActions did not persist '$reqId' and no failsafe was retained.")
            return $false
        }
    }
    return $true
}

function Get-ReplDialogItemsFromParams {
    param([string]$ParamsYaml)

    if (-not $ParamsYaml) { return @() }
    $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    if (-not $params) { return @() }

    $rawItems = @()
    $rawValue = Get-ReplObjectValue -InputObject $params -Name 'dialogItems'
    if ($null -eq $rawValue) { $rawValue = Get-ReplObjectValue -InputObject $params -Name 'dialog' }
    if ($null -ne $rawValue) { $rawItems = @($rawValue) }

    $items = @()
    foreach ($rawItem in $rawItems) {
        if (-not $rawItem) { continue }
        $item = [ordered]@{}
        if ($rawItem -is [System.Collections.IDictionary]) {
            foreach ($key in $rawItem.Keys) {
                $item[[string]$key] = $rawItem[$key]
            }
        } else {
            foreach ($property in $rawItem.PSObject.Properties) {
                $item[$property.Name] = $property.Value
            }
        }
        if ($item.Count -gt 0) {
            $items += $item
        }
    }

    return @($items)
}

function Invoke-WorkflowAppendDialog {
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.appendDialog')
    }
    # HV13: request guard before freshness so a mismatch cannot mutate marker fields.
    if (-not (Assert-ReplCallerRequestMatches -Method 'workflow.sessionlog.appendDialog' -ParamsYaml $ParamsYaml)) {
        return $false
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.appendDialog')) {
        return $false
    }

    $dialogItems = @(Get-ReplDialogItemsFromParams -ParamsYaml $ParamsYaml)
    if ($dialogItems.Count -eq 0) {
        [Console]::Error.WriteLine('workflow.sessionlog.appendDialog requires at least one valid dialogItems or dialog entry.')
        return $false
    }

    $null = [bool](Update-ReplTurnTitleFromParams -ParamsYaml $ParamsYaml)
    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    $meta = Get-ReplSessionMeta
    if (-not $meta -or [string]::IsNullOrWhiteSpace($reqId)) {
        [Console]::Error.WriteLine("workflow.sessionlog.appendDialog requires cached session metadata and an active turn. $(Get-ReplRecoveryGuidance)")
        return $false
    }

    # FR-MCP-170 / TR-MCP-PERSIST-001: incremental dialog POST, not a full-session SubmitAsync upsert.
    $callParams = [ordered]@{
        agent     = $meta.SourceType
        sessionId = $meta.SessionId
        requestId = $reqId
        items     = @($dialogItems)
    }
    $callYaml = ConvertTo-Yaml -Data $callParams -Options WithIndentedSequences
    $dialogFingerprint = Get-ReplStableFingerprint -Value ([ordered]@{
        method = 'client.SessionLog.AppendDialogAsync'
        requestId = $reqId
        sessionId = $meta.SessionId
        items = @($dialogItems)
    })
    $result = Invoke-ReplRaw -Method 'client.SessionLog.AppendDialogAsync' -ParamsYaml $callYaml
    if ($result.Success) {
        $typed = Test-ReplTypedSessionMutationResult -Method 'workflow.sessionlog.appendDialog' -Output ([string]$result.Output) -ExpectedSessionId ([string]$meta.SessionId) -ExpectedRequestId $reqId
        if (-not [bool]$typed.Ok) {
            $rejectTyped = [string]$typed.Message
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.appendDialog' -RequestId $reqId -Message $rejectTyped -ChildStderr $rejectTyped))
            [Console]::Error.WriteLine($rejectTyped)
            return $false
        }
        # BUG-TRIAGE-165 / FR-MCP-PLUGINCORE-004: increment only after the server accepts the items.
        Update-ReplTurnAudit -Field 'auditDialog' -Increment $dialogItems.Count | Out-Null
        Set-ReplTurnCacheField -Field 'lastDialogFingerprint' -Value $dialogFingerprint | Out-Null
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition primary -Method 'workflow.sessionlog.appendDialog' -RequestId $reqId -Message "appendDialog persisted requestId=$reqId"))
        return $true
    }

    $combined = "$($result.Output) $($result.Error)"
    $queueDialog = $false
    $queueMessage = ''
    if ($combined -match 'not_found|not found|HTTP 404|http 404') {
        $degradedTurn = Get-ReplTurnCacheField -Field 'degraded'
        if ($degradedTurn -eq 'true' -or $degradedTurn -eq $true) {
            # FR-MCP-SESSIONLIFE-001-AC004: one recovery SubmitAsync attempt before dialog failsafe.
            $recoveryOk = $false
            try {
                $script:ReplPersistVerbMethod = 'workflow.sessionlog.appendDialog'
                $recoveryTitle = Get-ReplTurnCacheField -Field 'queryTitle'
                $metaPT = Resolve-ReplPersistPlanTodo -ParamsYaml $ParamsYaml
                $recoveryArgs = @{
                    RequestId = $reqId
                    Title = [string]$recoveryTitle
                    Status = 'in_progress'
                    ResponseText = '(recovery before dialog append)'
                }
                Set-ReplPersistPlanTodoArgs -PersistArgs $recoveryArgs -MetaPT $metaPT
                $recoveryOk = [bool](Invoke-ReplPersistTurn @recoveryArgs)
            } catch {
                $recoveryOk = $false
            }
            if ($recoveryOk) {
                $result = Invoke-ReplRaw -Method 'client.SessionLog.AppendDialogAsync' -ParamsYaml $callYaml
                if ($result.Success) {
                    $typed = Test-ReplTypedSessionMutationResult -Method 'workflow.sessionlog.appendDialog' -Output ([string]$result.Output) -ExpectedSessionId ([string]$meta.SessionId) -ExpectedRequestId $reqId
                    if (-not [bool]$typed.Ok) {
                        $rejectTyped = [string]$typed.Message
                        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.appendDialog' -RequestId $reqId -Message $rejectTyped -ChildStderr $rejectTyped))
                        [Console]::Error.WriteLine($rejectTyped)
                        return $false
                    }
                    Update-ReplTurnAudit -Field 'auditDialog' -Increment $dialogItems.Count | Out-Null
                    Set-ReplTurnCacheField -Field 'lastDialogFingerprint' -Value $dialogFingerprint | Out-Null
                    [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition primary -Method 'workflow.sessionlog.appendDialog' -RequestId $reqId -Message "appendDialog persisted after recovery requestId=$reqId"))
                    return $true
                }
                $combined = "$($result.Output) $($result.Error)"
            }
            $queueDialog = $true
            $queueMessage = "appendDialog degraded turn not stored; session_dialog failsafe retained requestId=$reqId"
        } else {
            $missing = "workflow.sessionlog.appendDialog turn not found (retryable false); failsafe not used. $($result.Error)$($result.Output)"
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.appendDialog' -RequestId $reqId -Message $missing -ChildStderr $combined))
            [Console]::Error.WriteLine($missing)
            return $false
        }
    } elseif ($combined -match 'timeout|timed out|command_timeout|backend_unavailable|HTTP 503|http 503') {
        $queueDialog = $true
        $queueMessage = "appendDialog persist timed out or returned HTTP 503; dialog failsafe retained requestId=$reqId"
    }
    if ($queueDialog) {
        $existingDialog = Find-ReplFailsafeByFingerprint -Method 'client.SessionLog.AppendDialogAsync' -Fingerprint $dialogFingerprint
        if ($existingDialog) {
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition queued -Method 'workflow.sessionlog.appendDialog' -RequestId $reqId -FailsafePath $existingDialog -Message "appendDialog unchanged payload reused the retained failsafe requestId=$reqId" -ChildStderr $combined))
            [Console]::Error.WriteLine($queueMessage)
            return $true
        }
        $failsafe = Write-ReplFailsafe -Method 'client.SessionLog.AppendDialogAsync' -ParamsYaml $callYaml -Label 'session_dialog' -PayloadFingerprint $dialogFingerprint
        if (-not $failsafe) {
            $lost = "appendDialog failed because neither the server nor the failsafe received the write requestId=$reqId"
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition lost -Method 'workflow.sessionlog.appendDialog' -RequestId $reqId -Message $lost -ChildStderr $combined))
            [Console]::Error.WriteLine($lost)
            return $false
        }
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition queued -Method 'workflow.sessionlog.appendDialog' -RequestId $reqId -FailsafePath $failsafe -Message $queueMessage -ChildStderr $combined))
        [Console]::Error.WriteLine($queueMessage)
        return $true
    }

    $failed = "workflow.sessionlog.appendDialog server call failed: $($result.Error)$($result.Output)"
    [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.appendDialog' -RequestId $reqId -Message $failed -ChildStderr $combined))
    [Console]::Error.WriteLine($failed)
    return $false
}

function Invoke-WorkflowUpdateTurn {
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.updateTurn')
    }
    # HV13: request guard before freshness so a mismatch cannot mutate marker fields.
    if (-not (Assert-ReplCallerRequestMatches -Method 'workflow.sessionlog.updateTurn' -ParamsYaml $ParamsYaml)) {
        return $false
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.updateTurn')) {
        return $false
    }

    $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
    $explicitTitle = [bool](Update-ReplTurnTitleFromParams -ParamsYaml $ParamsYaml)

    $state = Read-ReplCurrentTurnState
    $responseText = if ($state -and $state.Contains('response')) { [string]$state['response'] } else { '' }
    $interpretation = if ($state -and $state.Contains('interpretation')) { [string]$state['interpretation'] } else { '' }
    $tokenCount = if ($state -and $state.Contains('tokenCount')) { [int]$state['tokenCount'] } else { 0 }
    $tags = [string[]]@()
    $contextList = [string[]]@()
    if ($state -and $state.Contains('tags')) {
        $tags = ConvertTo-McpPluginStringList -Value $state['tags']
    }
    if ($state -and $state.Contains('contextList')) {
        $contextList = ConvertTo-McpPluginStringList -Value $state['contextList']
    }

    if ($params) {
        $responseValue = Get-ReplObjectValue -InputObject $params -Name 'response'
        if ($null -ne $responseValue) { $responseText = [string]$responseValue }

        $interpretationValue = Get-ReplObjectValue -InputObject $params -Name 'interpretation'
        if ($null -ne $interpretationValue) { $interpretation = [string]$interpretationValue }

        $tokenValue = Get-ReplObjectValue -InputObject $params -Name 'tokenCount'
        if ($null -ne $tokenValue) { [void][int]::TryParse([string]$tokenValue, [ref]$tokenCount) }

        $tagValue = Get-ReplObjectValue -InputObject $params -Name 'tags'
        if ($null -ne $tagValue) { $tags = ConvertTo-McpPluginStringList -Value $tagValue }

        $contextValue = Get-ReplObjectValue -InputObject $params -Name 'contextList'
        if ($null -ne $contextValue) { $contextList = ConvertTo-McpPluginStringList -Value $contextValue }

        if ($state) {
            if (-not [string]::IsNullOrWhiteSpace($responseText)) { $state['response'] = $responseText }
            if (-not [string]::IsNullOrWhiteSpace($interpretation)) { $state['interpretation'] = $interpretation }
            if ($tokenCount -gt 0) { $state['tokenCount'] = $tokenCount }
            if ($tags.Count -gt 0) { $state['tags'] = @($tags) }
            if ($contextList.Count -gt 0) { $state['contextList'] = @($contextList) }
            Write-ReplCurrentTurnState -State $state
        }
    }

    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    # TR-MCP-REPL-015: send the turn title only when explicitly set this call.
    $title = if ($explicitTitle) { Get-ReplTurnCacheField -Field 'queryTitle' } else { '' }
    try {
        $script:ReplPersistVerbMethod = 'workflow.sessionlog.updateTurn'
        $metaPT = Resolve-ReplPersistPlanTodo -ParamsYaml $ParamsYaml
        # HV10: do not write omitted metadata into cache as None/cached replacements.
        # HV11: explicit values update cache.
        if ($state) {
            if ([bool]$metaPT.PlanExplicit) { $state['planFile'] = [string]$metaPT.PlanFile }
            if ([bool]$metaPT.TodoExplicit) { $state['todoId'] = [string]$metaPT.TodoId }
            if ([bool]$metaPT.PlanExplicit -or [bool]$metaPT.TodoExplicit) {
                Write-ReplCurrentTurnState -State $state
            }
        }
        $persistArgs = @{
            RequestId = $reqId
            Title = $title
            Status = 'in_progress'
            ResponseText = $responseText
            Interpretation = $interpretation
            TokenCount = $tokenCount
            Tags = $tags
            ContextList = $contextList
        }
        Set-ReplPersistPlanTodoArgs -PersistArgs $persistArgs -MetaPT $metaPT
        $persisted = [bool](Invoke-ReplPersistTurn @persistArgs)
    } catch {
        [Console]::Error.WriteLine("workflow.sessionlog.updateTurn failed for '$reqId': $_")
        return $false
    }
    if ($persisted) { return $true }
    $queued = $false
    if ($script:LastReplPersistenceDetails) {
        $queued = [bool](Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'queued')
    }
    if ($queued) {
        [Console]::Error.WriteLine("workflow.sessionlog.updateTurn queued for '$reqId'. Failsafe retained; primary persistence was not claimed.")
        return $true
    }
    [Console]::Error.WriteLine("workflow.sessionlog.updateTurn did not persist '$reqId' and no failsafe was retained.")
    return $false
}

function Invoke-WorkflowCompleteTurn {
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.completeTurn')
    }
    # HV13: request guard before freshness so a mismatch cannot mutate marker fields.
    if (-not (Assert-ReplCallerRequestMatches -Method 'workflow.sessionlog.completeTurn' -ParamsYaml $ParamsYaml)) {
        return $false
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.completeTurn')) {
        return $false
    }

    $responseText = '(no response provided)'
    if ($ParamsYaml) {
        $params = Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml
        $responseValue = Get-ReplObjectValue -InputObject $params -Name 'response'
        if ($null -ne $responseValue) {
            $responseText = [string]$responseValue
        }
    }
    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    $explicitTitle = [bool](Update-ReplTurnTitleFromParams -ParamsYaml $ParamsYaml)

    $actionsBlock = ''
    if ($ParamsYaml -and ($ParamsYaml -match '(?m)^\s*actions:' -or $ParamsYaml -match '(?m)^\s*actions:\s*\S')) {
        $actionsBlock = Get-ReplNormalizedActionsBlock -ParamsYaml ($ParamsYaml -replace "`r`n", "`n" -replace "`r", "")
    }

    # TR-MCP-REPL-015: send the turn title only when explicitly set this call.
    $title = if ($explicitTitle) { Get-ReplTurnCacheField -Field 'queryTitle' } else { '' }
    $metaPT = Resolve-ReplPersistPlanTodo -ParamsYaml $ParamsYaml
    $persisted = $false
    try {
        $script:ReplPersistVerbMethod = 'workflow.sessionlog.completeTurn'
        $persistArgs = @{
            RequestId = $reqId
            Title = $title
            Status = 'completed'
            ResponseText = $responseText
            ActionsYaml = $actionsBlock
        }
        Set-ReplPersistPlanTodoArgs -PersistArgs $persistArgs -MetaPT $metaPT -UpdateCache
        $persisted = [bool](Invoke-ReplPersistTurn @persistArgs)
    } catch {
        [Console]::Error.WriteLine("workflow.sessionlog.completeTurn failed for '$reqId': $_")
        return $false
    }
    if (-not $persisted) {
        if (Test-ReplSessionVerbQueued) {
            [Console]::Error.WriteLine("workflow.sessionlog.completeTurn queued for '$reqId'. Failsafe retained; primary persistence was not claimed.")
            return $true
        }
        return $false
    }

    $serverDialogCount = Get-ReplServerDialogCount -RequestId $reqId
    if ($serverDialogCount -ge 0) {
        Set-ReplTurnCacheField -Field 'auditDialog' -Value ([string]$serverDialogCount) | Out-Null
    }

    Update-ReplTurnCacheStatus -NewStatus 'completed' | Out-Null

    if ($script:LastReplPersistenceDetails) {
        $degraded = Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'degraded'
        if ($degraded -eq $true) {
            $message = [string](Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'message')
            $failsafePath = [string](Get-ReplObjectValue -InputObject $script:LastReplPersistenceDetails -Name 'failsafePath')
            if ([string]::IsNullOrWhiteSpace($message)) {
                $message = 'MCP Session Log persistence is degraded.'
            }
            [Console]::Error.WriteLine("$message FailsafePath='$failsafePath'.")
        }
    }
    return $true

}

function Invoke-WorkflowFailTurn {
    # TR-MCP-REPL-020: close the active turn as failed from plugin cache state
    # (BUG-TRIAGE-099). workflow.sessionlog.failTurn cannot be dispatched to the
    # REPL for plugin-shim turns: the in-process SessionLogWorkflow throws
    # 'No active session exists' because the PowerShell shim's beginTurn never
    # creates REPL-native state (that in-process contract is correct for
    # REPL-native sessions and stays untouched). Following the
    # appendDialog/appendActions pattern, the session and turn are resolved from
    # session-state.yaml + current-turn.yaml, the turn is persisted with status
    # 'failed' and the failure note, and current-turn.yaml is cleared so the
    # Stop hook sees a closed turn.
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.failTurn')
    }
    # HV13: request guard before freshness so a mismatch cannot mutate marker fields.
    if (-not (Assert-ReplCallerRequestMatches -Method 'workflow.sessionlog.failTurn' -ParamsYaml $ParamsYaml)) {
        return $false
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.failTurn')) {
        return $false
    }

    # errorMessage is the canonical REPL contract parameter (IFailTurnParams);
    # failureNote is accepted as an alias for symmetry with the persisted field.
    $errorMessage = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'errorMessage'
    if ([string]::IsNullOrWhiteSpace($errorMessage)) {
        $errorMessage = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'failureNote'
    }
    if ([string]::IsNullOrWhiteSpace($errorMessage)) {
        [Console]::Error.WriteLine('workflow.sessionlog.failTurn requires a non-empty errorMessage (or failureNote).')
        return $false
    }
    $errorCode = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'errorCode'
    $failureNote = if ([string]::IsNullOrWhiteSpace($errorCode)) { $errorMessage } else { "$errorMessage (errorCode: $errorCode)" }

    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    $persisted = $false
    try {
        # TR-MCP-REPL-015: omit the title so failing a turn never retitles it.
        $script:ReplPersistVerbMethod = 'workflow.sessionlog.failTurn'
        $metaPT = Resolve-ReplPersistPlanTodo -ParamsYaml $ParamsYaml
        $persisted = [bool](Invoke-ReplPersistTurn -RequestId $reqId -Title '' `
            -Status 'failed' -ResponseText $failureNote -PlanFile ([string]$metaPT.PlanFile) -TodoId ([string]$metaPT.TodoId))
    } catch {
        [Console]::Error.WriteLine("workflow.sessionlog.failTurn failed for '$reqId': $_")
        return $false
    }
    if (-not $persisted) {
        if (Test-ReplSessionVerbQueued) {
            [Console]::Error.WriteLine("workflow.sessionlog.failTurn queued for '$reqId'. Failsafe retained; primary persistence was not claimed.")
            return $true
        }
        return $false
    }

    Remove-Item -LiteralPath $turnFile -Force -ErrorAction SilentlyContinue
    return $true
}

function Invoke-WorkflowFailsafeDrain {
    # TR-MCP-REPL-016: operator-facing drain. Runs a full pass regardless of the
    # once-per-process latch used by the automatic trigger, and prints the summary
    # as YAML so a human or a script can read the outcome.
    param([string]$ParamsYaml)

    $maxRecords = 0
    $maxAttempts = 5
    $params = if ($ParamsYaml) { Convert-ReplParamsYamlToObject -ParamsYaml $ParamsYaml } else { $null }
    if ($params -is [System.Collections.IDictionary]) {
        if ($params.Contains('maxRecords')) {
            try { $maxRecords = [int]$params['maxRecords'] } catch { $maxRecords = 0 }
        }
        if ($params.Contains('maxAttempts')) {
            try { $maxAttempts = [int]$params['maxAttempts'] } catch { $maxAttempts = 5 }
        }
    }

    $summary = Invoke-ReplFailsafeDrain -MaxRecords $maxRecords -MaxAttempts $maxAttempts
    if (-not $summary.aborted) {
        $script:ReplFailsafeDrainCompleted = $true
    }
    return [pscustomobject]@{
        Success = (-not $summary.aborted)
        Output = (ConvertTo-Yaml -Data $summary -Options WithIndentedSequences)
    }
}

function Invoke-WorkflowFailsafeStatus {
    # TR-MCP-REPL-017: read-only queue depth for operators, matching what
    # mcp-status.ps1 reports, without replaying anything.
    param([string]$ParamsYaml)

    $dir = Get-ReplFailsafeDir
    $quarantineDir = Get-ReplFailsafeQuarantineDir
    $status = [ordered]@{
        failsafeDir = $dir
        quarantineDir = $quarantineDir
        pendingCount = if (Test-Path -LiteralPath $dir -PathType Container) {
            @(Get-ChildItem -LiteralPath $dir -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count
        } else { 0 }
        quarantineCount = if (Test-Path -LiteralPath $quarantineDir -PathType Container) {
            @(Get-ChildItem -LiteralPath $quarantineDir -Filter '*.yaml' -File -ErrorAction SilentlyContinue).Count
        } else { 0 }
    }
    return [pscustomobject]@{
        Success = $true
        Output = (ConvertTo-Yaml -Data $status -Options WithIndentedSequences)
    }
}

function Invoke-WorkflowSetTurnTitle {
    # TR-MCP-REPL-014: dedicated turn retitle. Updates the local cache queryTitle
    # and calls the server SetTurnTitle path, so the title is durable even though
    # incidental re-submits now omit the title (TR-MCP-REPL-015).
    param([string]$ParamsYaml)
    $turnFile = Get-ReplCurrentTurnFile
    if (-not (Test-Path $turnFile)) {
        return (Deny-ReplMissingCurrentTurn -Method 'workflow.sessionlog.setTurnTitle')
    }
    # HV13: request guard before freshness so a mismatch cannot mutate marker fields.
    if (-not (Assert-ReplCallerRequestMatches -Method 'workflow.sessionlog.setTurnTitle' -ParamsYaml $ParamsYaml)) {
        return $false
    }
    if (-not (Assert-ReplCurrentTurnFresh -Method 'workflow.sessionlog.setTurnTitle')) {
        return $false
    }

    $title = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'queryTitle'
    if ([string]::IsNullOrWhiteSpace($title)) {
        [Console]::Error.WriteLine('workflow.sessionlog.setTurnTitle requires a non-empty queryTitle.')
        return $false
    }

    $meta = Get-ReplSessionMeta
    if (-not $meta) {
        [Console]::Error.WriteLine("workflow.sessionlog.setTurnTitle requires cached session metadata. $(Get-ReplRecoveryGuidance)")
        return $false
    }
    $reqId = Get-ReplTurnCacheField -Field 'turnRequestId'
    if ([string]::IsNullOrWhiteSpace($reqId)) {
        [Console]::Error.WriteLine('workflow.sessionlog.setTurnTitle requires an active turn requestId.')
        return $false
    }

    Set-ReplTurnCacheField -Field 'queryTitle' -Value $title | Out-Null

    $callParams = [ordered]@{
        agent     = $meta.SourceType
        sessionId = $meta.SessionId
        requestId = $reqId
        title     = $title
    }
    $callYaml = ConvertTo-Yaml -Data $callParams -Options WithIndentedSequences
    $titleFingerprint = Get-ReplStableFingerprint -Value ([ordered]@{
        method = 'client.SessionLog.SetTurnTitleAsync'
        requestId = $reqId
        title = $title
    })
    $existingTitle = Find-ReplFailsafeByFingerprint -Method 'client.SessionLog.SetTurnTitleAsync' -Fingerprint $titleFingerprint
    if ($existingTitle) {
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition queued -Method 'workflow.sessionlog.setTurnTitle' -RequestId $reqId -FailsafePath $existingTitle -Message "setTurnTitle unchanged payload reused the retained failsafe requestId=$reqId"))
        return $true
    }
    $failsafePath = Write-ReplFailsafe -Method 'client.SessionLog.SetTurnTitleAsync' -ParamsYaml $callYaml -Label 'session_setTurnTitle' -PayloadFingerprint $titleFingerprint
    if (-not $failsafePath) {
        $lost = "workflow.sessionlog.setTurnTitle failed because the failsafe payload could not be saved for request '$reqId'."
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition lost -Method 'workflow.sessionlog.setTurnTitle' -RequestId $reqId -Message $lost -ChildStderr $lost))
        [Console]::Error.WriteLine($lost)
        return $false
    }
    $result = Invoke-ReplRaw -Method 'client.SessionLog.SetTurnTitleAsync' -ParamsYaml $callYaml
    if (-not $result.Success) {
        $combined = "$($result.Output) $($result.Error)"
        if ($combined -match 'timeout|timed out|command_timeout|backend_unavailable|HTTP 503|http 503') {
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition queued -Method 'workflow.sessionlog.setTurnTitle' -RequestId $reqId -FailsafePath $failsafePath -Message "setTurnTitle queued after confirmed failsafe write requestId=$reqId" -ChildStderr $combined))
            [Console]::Error.WriteLine("workflow.sessionlog.setTurnTitle queued for '$reqId'. Failsafe retained; primary persistence was not claimed.")
            return $true
        }
        $rejected = "workflow.sessionlog.setTurnTitle server call failed: $($result.Error)$($result.Output)"
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.setTurnTitle' -RequestId $reqId -FailsafePath $failsafePath -Message $rejected -ChildStderr $combined))
        [Console]::Error.WriteLine($rejected)
        return $false
    }
    $typed = Test-ReplTypedSessionMutationResult -Method 'workflow.sessionlog.setTurnTitle' -Output ([string]$result.Output) -ExpectedSessionId ([string]$meta.SessionId) -ExpectedRequestId $reqId -RequireRetitled
    if (-not [bool]$typed.Ok) {
        $rejectTyped = [string]$typed.Message
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.setTurnTitle' -RequestId $reqId -FailsafePath $failsafePath -Message $rejectTyped -ChildStderr $rejectTyped))
        [Console]::Error.WriteLine($rejectTyped)
        return $false
    }
    Clear-ReplFailsafe -Path $failsafePath
    [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition primary -Method 'workflow.sessionlog.setTurnTitle' -RequestId $reqId -Message "setTurnTitle persisted requestId=$reqId"))
    return $true
}

function Invoke-WorkflowSetSessionTitle {
    # TR-MCP-REPL-014: dedicated session retitle. Writes the stable session-state
    # title and calls the server SetSessionTitle path.
    param([string]$ParamsYaml)
    $title = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'title'
    if ([string]::IsNullOrWhiteSpace($title)) {
        $title = Get-ReplParamString -ParamsYaml $ParamsYaml -Name 'sessionTitle'
    }
    if ([string]::IsNullOrWhiteSpace($title)) {
        [Console]::Error.WriteLine('workflow.sessionlog.setSessionTitle requires a non-empty title.')
        return $false
    }

    $meta = Get-ReplSessionMeta
    if (-not $meta) {
        [Console]::Error.WriteLine("workflow.sessionlog.setSessionTitle requires cached session metadata. $(Get-ReplRecoveryGuidance)")
        return $false
    }

    Set-ReplSessionStateValue -Key 'title' -Value $title | Out-Null

    $callParams = [ordered]@{
        agent     = $meta.SourceType
        sessionId = $meta.SessionId
        title     = $title
    }
    $callYaml = ConvertTo-Yaml -Data $callParams -Options WithIndentedSequences
    $sessionFingerprint = Get-ReplStableFingerprint -Value ([ordered]@{
        method = 'client.SessionLog.SetSessionTitleAsync'
        sessionId = $meta.SessionId
        title = $title
    })
    $existingSessionTitle = Find-ReplFailsafeByFingerprint -Method 'client.SessionLog.SetSessionTitleAsync' -Fingerprint $sessionFingerprint
    if ($existingSessionTitle) {
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition queued -Method 'workflow.sessionlog.setSessionTitle' -FailsafePath $existingSessionTitle -Message "setSessionTitle unchanged payload reused the retained failsafe"))
        return $true
    }
    $failsafePath = Write-ReplFailsafe -Method 'client.SessionLog.SetSessionTitleAsync' -ParamsYaml $callYaml -Label 'session_setSessionTitle' -PayloadFingerprint $sessionFingerprint
    if (-not $failsafePath) {
        $lost = "workflow.sessionlog.setSessionTitle failed because the failsafe payload could not be saved."
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition lost -Method 'workflow.sessionlog.setSessionTitle' -Message $lost -ChildStderr $lost))
        [Console]::Error.WriteLine($lost)
        return $false
    }
    $result = Invoke-ReplRaw -Method 'client.SessionLog.SetSessionTitleAsync' -ParamsYaml $callYaml
    if (-not $result.Success) {
        $combined = "$($result.Output) $($result.Error)"
        if ($combined -match 'timeout|timed out|command_timeout|backend_unavailable|HTTP 503|http 503') {
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition queued -Method 'workflow.sessionlog.setSessionTitle' -FailsafePath $failsafePath -Message 'setSessionTitle queued after confirmed failsafe write' -ChildStderr $combined))
            [Console]::Error.WriteLine('workflow.sessionlog.setSessionTitle queued. Failsafe retained; primary persistence was not claimed.')
            return $true
        }
        $rejected = "workflow.sessionlog.setSessionTitle server call failed: $($result.Error)$($result.Output)"
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.setSessionTitle' -FailsafePath $failsafePath -Message $rejected -ChildStderr $combined))
        [Console]::Error.WriteLine($rejected)
        return $false
    }
    $typed = Test-ReplTypedSessionMutationResult -Method 'workflow.sessionlog.setSessionTitle' -Output ([string]$result.Output) -ExpectedSessionId ([string]$meta.SessionId) -SessionOnly -RequireRetitled
    if (-not [bool]$typed.Ok) {
        $rejectTyped = [string]$typed.Message
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method 'workflow.sessionlog.setSessionTitle' -FailsafePath $failsafePath -Message $rejectTyped -ChildStderr $rejectTyped))
        [Console]::Error.WriteLine($rejectTyped)
        return $false
    }
    Clear-ReplFailsafe -Path $failsafePath
    [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition primary -Method 'workflow.sessionlog.setSessionTitle' -Message 'setSessionTitle persisted'))
    return $true
}

function Invoke-ReplMethodCore {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )

    # Local plugin-shim verbs: record the boolean outcome on the script-scoped
    # success flag (so the script-entry exit code is truthful) and return without
    # emitting the boolean to stdout. Emitting it leaked "True" lines, and leaving
    # the flag unset made the script-entry exit 1 even on a successful persist.
    switch -Wildcard ($Method) {
        'workflow.sessionlog.beginTurn'       { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowBeginTurn -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.openSession'     { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowOpenSession -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.updateTurn'      { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowUpdateTurn -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.appendActions'   { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowAppendActions -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.appendDialog'    { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowAppendDialog -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.completeTurn'    { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowCompleteTurn -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.failTurn'        { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowFailTurn -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.setTurnTitle'    { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowSetTurnTitle -ParamsYaml $ParamsYaml); return }
        'workflow.sessionlog.setSessionTitle' { $script:LastInvokeReplMethodSuccess = [bool](Invoke-WorkflowSetSessionTitle -ParamsYaml $ParamsYaml); return }
        'workflow.failsafe.drain' {
            # These two verbs report a YAML document to stdout as well as a boolean
            # outcome, so they return a result object instead of a bare boolean.
            $drainResult = Invoke-WorkflowFailsafeDrain -ParamsYaml $ParamsYaml
            $script:LastInvokeReplMethodSuccess = [bool]$drainResult.Success
            $drainResult.Output
            return
        }
        'workflow.failsafe.status' {
            $failsafeStatusResult = Invoke-WorkflowFailsafeStatus -ParamsYaml $ParamsYaml
            $script:LastInvokeReplMethodSuccess = [bool]$failsafeStatusResult.Success
            $failsafeStatusResult.Output
            return
        }
    }

    $r = Invoke-ReplRaw -Method $Method -ParamsYaml $ParamsYaml
    if ($r.Output) {
        $script:LastInvokeReplMethodSuccess = [bool]$r.Success
        $r.Output
        return
    }

    # TR-MCP-REPL-001 AC-001 (PLAN-SESSIONTRIAGEFIX-001 F1, triage-report-eb56d4eb): without a response envelope the
    # outcome lives only in the success flag, never in a bare True or False on stdout. A failure answers with one
    # error envelope naming why, with its code and retryable from the plugin's error classification
    # (TR-MCP-TRIAGEERR-001); a success without output writes nothing.
    $script:LastInvokeReplMethodSuccess = [bool]$r.Success
    if ($r.Success) { return }
    $failureText = [string]$r.Error
    if ([string]::IsNullOrWhiteSpace($failureText)) {
        $failureText = if ($null -ne $r.ExitCode) { "qbrain-ai-repl exited with code $($r.ExitCode) without a response to $Method" } else { "$Method failed without a response" }
    }
    $classified = ConvertTo-McpPluginClassifiedError -ErrorText $failureText
    ConvertTo-Yaml -Data ([ordered]@{
            type = 'error'
            payload = [ordered]@{
                code = [string]$classified.code
                message = [string]$classified.message
                retryable = [bool]$classified.retryable
                details = [ordered]@{ method = $Method }
            }
        }) -Options WithIndentedSequences
}

function Invoke-ReplMethod {
    <#
    .SYNOPSIS
    Dispatches a method and emits one typed envelope for local session mutations.
    .DESCRIPTION
    Keeps boolean control flow internal. Diagnostics remain on stderr, while stdout
    carries the same persisted/queued/rejected/lost/unchanged receipt as the cache.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )
    $localMethods = @(
        'workflow.sessionlog.beginTurn', 'workflow.sessionlog.openSession',
        'workflow.sessionlog.updateTurn', 'workflow.sessionlog.appendActions',
        'workflow.sessionlog.appendDialog', 'workflow.sessionlog.completeTurn',
        'workflow.sessionlog.failTurn', 'workflow.sessionlog.setTurnTitle',
        'workflow.sessionlog.setSessionTitle'
    )
    if ($Method -notin $localMethods) {
        Invoke-ReplMethodCore -Method $Method -ParamsYaml $ParamsYaml
        return
    }

    $script:LastReplPersistenceDetails = $null
    $script:LastInvokeReplMethodSuccess = $false
    try {
        Invoke-ReplMethodCore -Method $Method -ParamsYaml $ParamsYaml | Out-Null
    } catch {
        $script:LastInvokeReplMethodSuccess = $false
        $message = "$Method failed: $_"
        if (-not $script:LastReplPersistenceDetails -or $script:LastReplPersistenceDetails.code -notin @('rejected', 'lost')) {
            [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method $Method -Message $message -ChildStderr $message))
        }
        [Console]::Error.WriteLine($message)
    }
    if (-not $script:LastReplPersistenceDetails) {
        $disposition = if ($script:LastInvokeReplMethodSuccess) { 'unchanged' } else { 'rejected' }
        $requestId = if ($Method -in @('workflow.sessionlog.openSession', 'workflow.sessionlog.setSessionTitle')) { '' } else { Get-ReplTurnCacheField -Field 'turnRequestId' }
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition $disposition -Method $Method -RequestId $requestId -Message "$Method completed without a remote persistence receipt."))
    }
    if (-not $script:LastInvokeReplMethodSuccess -and $script:LastReplPersistenceDetails.code -in @('persisted', 'queued', 'unchanged')) {
        $prior = $script:LastReplPersistenceDetails
        [void](Publish-ReplSessionVerbReceipt -Receipt (New-ReplSessionVerbReceipt -Disposition rejected -Method $Method -RequestId ([string]$prior.requestId) -FailsafePath ([string]$prior.failsafePath) -Message "$Method failed after an earlier substep; the overall operation was not confirmed."))
    }
    $receipt = $script:LastReplPersistenceDetails
    $receipt['method'] = $Method
    $script:LastInvokeReplMethodSuccess = ($receipt.code -in @('persisted', 'queued', 'unchanged'))
    $envelope = [ordered]@{ type = 'result'; payload = [ordered]@{ result = $receipt } }
    ConvertTo-Yaml -Data $envelope -Options WithIndentedSequences
}

# Script-entry: only when invoked directly with -Method (not when dot-sourced).
if ($Method -and $MyInvocation.InvocationName -ne '.') {
    $script:LastInvokeReplMethodSuccess = $false
    Invoke-ReplMethod -Method $Method -ParamsYaml $ParamsYaml
    # TR-MCP-REPL-016 AC-001 (D1, triage-report-124a374c): the drain the first
    # successful backend call asked for runs here, after the method has completed
    # and written its output, so it never delays one of the method's own backend
    # calls; inside a hook it stops at the hook deadline (AC-002). It does not
    # change this invocation's exit code.
    if ($script:ReplFailsafeDrainDeferred) {
        $script:ReplFailsafeDrainDeferred = $false
        Invoke-ReplFailsafeDrainOnFirstSuccessCore
    }
    if (-not $script:LastInvokeReplMethodSuccess) { exit 1 }
    exit 0
}
