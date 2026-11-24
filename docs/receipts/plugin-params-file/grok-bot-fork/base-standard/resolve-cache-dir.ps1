<#
.SYNOPSIS
    Resolve the workspace-scoped PowerShell plugin cache directory.
.DESCRIPTION
    Runtime state belongs to the active workspace, never to the installed
    plugin checkout. The returned path is <workspace>/.mcpServer/<agent>.

    Precedence:
      1. MCP_CACHE_DIR_OVERRIDE for an explicit test or recovery override.
      2. The workspace marker found from the explicit or active start path.
      3. MCP/host workspace environment variables as a markerless fallback.
 #>

$script:ResolveCacheDirScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Get-McpCacheAgentKey {
    [CmdletBinding()]
    param()

    # FR-MCP-SESSIONLIFE-002: inherited Grok variables must not move a Codex
    # plugin into the Grok cache namespace.
    $inheritedAgent = @($env:PLUGIN_AGENT_NAME, $env:MCP_AGENT_NAME) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
    if ($env:MCP_PLUGIN_HOST -match '^(?i:codex)$' -and $inheritedAgent -match '(?i)grok') {
        return 'codex'
    }

    $agent = @(
        $env:MCP_AGENT_NAME,
        $env:PLUGIN_AGENT_NAME,
        $env:PLUGIN_AGENT_DEFAULT,
        $env:MCP_PLUGIN_HOST
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1

    if (-not $agent) { return 'default' }

    switch ($agent.Trim().ToLowerInvariant()) {
        'claude' { return 'claude' }
        'claudecode' { return 'claude' }
        'claude-code' { return 'claude' }
        'claudecowork' { return 'cowork' }
        'claude-cowork' { return 'cowork' }
        'codex' { return 'codex' }
        'copilot' { return 'copilot' }
        'grok' { return 'grok' }
        'grokcode' { return 'grok' }
        'grok-code' { return 'grok' }
        'cline' { return 'cline' }
        'cline-v2' { return 'cline' }
        'opencode' { return 'opencode' }
        'open-code' { return 'opencode' }
    }

    $key = ($agent.Trim() -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant()
    if (-not $key) { return 'default' }
    return $key
}

function Get-McpPluginRoot {
    if ($env:MCP_PLUGIN_ROOT) { return $env:MCP_PLUGIN_ROOT }
    if ($env:CLAUDE_PLUGIN_ROOT) { return $env:CLAUDE_PLUGIN_ROOT }
    return (Split-Path -Parent $script:ResolveCacheDirScriptDir)
}

function Join-McpWorkspaceCachePath {
    param([Parameter(Mandatory)][string]$WorkspacePath)

    return (Join-Path (Join-Path $WorkspacePath '.mcpServer') (Get-McpCacheAgentKey))
}

function Get-McpBackgroundScopeKey {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (PLAN-SESSIONTRIAGEFIX-001 D3): the identity of a background host: MCP_SUBAGENT_ID, else GROK_SUBAGENT_ID,
        else the hook payload's session_id. None when there is none or when it equals the agentSessionId recorded for the root, because
        then the prompt is the root host's own.
    #>
    [CmdletBinding()]
    param(
        [string]$PayloadSessionId,
        [string]$RootAgentSessionId
    )

    $key = @($env:MCP_SUBAGENT_ID, $env:GROK_SUBAGENT_ID, $PayloadSessionId) |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($key)) { return $null }
    $key = $key.Trim()
    if (-not [string]::IsNullOrWhiteSpace($RootAgentSessionId) -and
        [string]::Equals($key, $RootAgentSessionId.Trim(), [System.StringComparison]::Ordinal)) {
        return $null
    }
    return $key
}

function Join-McpBackgroundScopePath {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (D3): the cache folder of one background host, <cache>/background/<key>. A key with characters other than
        letters, digits, dot, underscore and hyphen, or longer than 96 characters, is made file-safe and suffixed with a hash of the
        original, so two keys never share a folder.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$Key
    )

    $safe = ($Key -replace '[^A-Za-z0-9._-]+', '-').Trim('-', '.')
    if ($safe -cne $Key -or $safe.Length -gt 96) {
        $hash = [System.Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($Key))).Substring(0, 12).ToLowerInvariant()
        if ($safe.Length -gt 80) { $safe = $safe.Substring(0, 80) }
        $safe = if ($safe) { "$safe-$hash" } else { $hash }
    }
    return (Join-Path (Join-Path $CacheDir 'background') $safe)
}

function Resolve-McpBackgroundScopeCacheDir {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (D3): the cache a session-log verb the agent calls itself uses. With no MCP_CACHE_DIR_OVERRIDE and with
        MCP_SUBAGENT_ID or GROK_SUBAGENT_ID set, that agent's background folder when it holds a current-turn.yaml; otherwise CacheDir.
        Those two are the only host identities an agent's own calls carry.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$CacheDir)

    if ($env:MCP_CACHE_DIR_OVERRIDE) { return $CacheDir }
    $key = @($env:MCP_SUBAGENT_ID, $env:GROK_SUBAGENT_ID) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($key)) { return $CacheDir }
    $scope = Join-McpBackgroundScopePath -CacheDir $CacheDir -Key $key.Trim()
    if (Test-Path -LiteralPath (Join-Path $scope 'current-turn.yaml') -PathType Leaf) { return $scope }
    return $CacheDir
}

function Get-McpWorkspaceFromEnvironment {
    $configured = @(
        $env:MCPSERVER_WORKSPACE_PATH,
        $env:MCP_WORKSPACE_PATH,
        $env:CLAUDE_PROJECT_DIR,
        $env:CODEX_WORKSPACE_PATH,
        $env:CODEX_PROJECT_DIR,
        $env:COWORK_WORKSPACE_PATH,
        $env:CLINE_WORKSPACE_PATH,
        $env:OPENCODE_WORKSPACE_PATH
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1

    if ($configured -and (Test-Path -LiteralPath $configured -PathType Container)) {
        return (Resolve-Path -LiteralPath $configured).ProviderPath
    }

    return $null
}

function Resolve-McpCacheDir {
    [CmdletBinding()]
    param([string]$StartPath)

    if ($env:MCP_CACHE_DIR_OVERRIDE) {
        return $env:MCP_CACHE_DIR_OVERRIDE
    }

    if (-not (Get-Command Find-MarkerFile -ErrorAction SilentlyContinue)) {
        $resolver = Join-Path $script:ResolveCacheDirScriptDir 'marker-resolver.ps1'
        if (Test-Path -LiteralPath $resolver) {
            . $resolver
        }
    }

    $explicitWorkspace = @(
        $env:MCP_WORKSPACE_PATH,
        $env:MCPSERVER_WORKSPACE_PATH,
        $env:CLAUDE_PROJECT_DIR
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path -LiteralPath $_ -PathType Container) } | Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($StartPath) -and $explicitWorkspace) {
        return (Join-McpWorkspaceCachePath -WorkspacePath ((Resolve-Path -LiteralPath $explicitWorkspace).ProviderPath))
    }

    $startCandidates = if (-not [string]::IsNullOrWhiteSpace($StartPath)) {
        @($StartPath)
    } else {
        @(
            $env:CODEX_CWD,
            (Get-Location).Path
        ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique
    }

    if (Get-Command Find-MarkerFile -ErrorAction SilentlyContinue) {
        foreach ($startDir in $startCandidates) {
            try {
                $markerFile = Find-MarkerFile -StartDir $startDir
                if ($markerFile) {
                    return (Join-McpWorkspaceCachePath -WorkspacePath (Split-Path -Parent $markerFile))
                }
            } catch {
                # Try the next active start candidate, then configured env.
            }
        }
    }

    $configuredWorkspace = Get-McpWorkspaceFromEnvironment
    if ($configuredWorkspace) {
        return (Join-McpWorkspaceCachePath -WorkspacePath $configuredWorkspace)
    }

    throw "Unable to resolve the active workspace cache. Set MCP_WORKSPACE_PATH or MCP_CACHE_DIR_OVERRIDE; plugin install paths are not workspace caches."
}

function Get-McpFailsafeFileSha256Hex {
    <#
    .SYNOPSIS
        SHA-256 of a file as uppercase hex, used for failsafe queue migration.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $stream = [System.IO.File]::OpenRead($Path)
        try {
            $hash = $sha.ComputeHash($stream)
            return (([BitConverter]::ToString($hash) -replace '-', ''))
        } finally {
            $stream.Dispose()
        }
    } finally {
        $sha.Dispose()
    }
}

function Get-McpFailsafeAgentSegment {
    <#
    .SYNOPSIS
        Agent path segment matching FilesystemSessionLogPersistenceStrategy.SanitizePathSegment.
    #>
    [CmdletBinding()]
    param()

    $inheritedAgent = @($env:PLUGIN_AGENT_NAME, $env:MCP_AGENT_NAME) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
    if ($env:MCP_PLUGIN_HOST -match '^(?i:codex)$' -and $inheritedAgent -match 'grok') {
        $agent = 'Codex'
    } else {
        $agent = @(
            $env:PLUGIN_AGENT_NAME,
            $env:MCP_AGENT_NAME,
            $env:PLUGIN_AGENT_DEFAULT,
            $env:MCP_PLUGIN_HOST
        ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
    }

    if (-not $agent) {
        return (Get-McpCacheAgentKey)
    }

    $builder = New-Object System.Text.StringBuilder
    foreach ($character in $agent.ToCharArray()) {
        if ([char]::IsLetterOrDigit($character) -or $character -eq '-' -or $character -eq '_' -or $character -eq '.') {
            [void]$builder.Append($character)
        } else {
            [void]$builder.Append('_')
        }
    }

    $result = $builder.ToString().Trim('.')
    if ([string]::IsNullOrWhiteSpace($result)) { return 'unknown' }
    return $result
}

function Get-McpFailsafeWorkspaceKey {
    <#
    .SYNOPSIS
        Workspace key matching FilesystemSessionLogPersistenceStrategy (base64url of UTF-8 full path).
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$WorkspacePath)

    $full = [System.IO.Path]::GetFullPath($WorkspacePath)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($full)
    $b64 = [Convert]::ToBase64String($bytes)
    return (($b64 -replace '\+', '-') -replace '/', '_').TrimEnd('=')
}

function Resolve-McpFailsafeWorkspacePath {
    <#
    .SYNOPSIS
        Resolves the workspace root used for the V4 failsafe tree.
    #>
    [CmdletBinding()]
    param([string]$StartPath)

    if (-not [string]::IsNullOrWhiteSpace($StartPath) -and (Test-Path -LiteralPath $StartPath -PathType Container)) {
        return [System.IO.Path]::GetFullPath($StartPath)
    }

    $configured = Get-McpWorkspaceFromEnvironment
    if ($configured) { return $configured }

    if (Get-Command Find-MarkerFile -ErrorAction SilentlyContinue) {
        $startCandidates = if (-not [string]::IsNullOrWhiteSpace($StartPath)) { @($StartPath) } else {
            @(
                $env:MCP_WORKSPACE_START_DIR,
                $env:MCP_WORKSPACE_PATH,
                $env:MCPSERVER_WORKSPACE_PATH,
                (Get-Location).Path
            ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique
        }
        foreach ($startDir in $startCandidates) {
            try {
                $markerFile = Find-MarkerFile -StartDir $startDir
                if ($markerFile) {
                    return [System.IO.Path]::GetFullPath((Split-Path -Parent $markerFile))
                }
            } catch {
            }
        }
    }

    throw "Unable to resolve the workspace root for the V4 failsafe queue. Set MCP_WORKSPACE_PATH or MCPSERVER_FAILSAFE_DIR."
}

function Invoke-McpFailsafeLegacyQueueMigration {
    <#
    .SYNOPSIS
        Copy-then-delete legacy plugin failsafe YAML into the V4 pending directory after SHA-256 match.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$WorkspacePath,
        [Parameter(Mandatory)][string]$PendingDir
    )

    $mcpRoot = Join-Path $WorkspacePath '.mcpServer'
    if (-not (Test-Path -LiteralPath $mcpRoot -PathType Container)) {
        return
    }

    [void][System.IO.Directory]::CreateDirectory($PendingDir)

    $legacyDirs = New-Object System.Collections.Generic.List[string]
    foreach ($child in [System.IO.Directory]::GetDirectories($mcpRoot)) {
        $name = [System.IO.Path]::GetFileName($child)
        if ([string]::Equals($name, 'failsafe', [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        $legacy = Join-Path $child 'failsafe'
        if (Test-Path -LiteralPath $legacy -PathType Container) {
            $legacyDirs.Add($legacy)
        }
    }

    foreach ($legacy in $legacyDirs) {
        $files = @(Get-ChildItem -LiteralPath $legacy -File -Filter '*.yaml' -ErrorAction SilentlyContinue)
        foreach ($file in $files) {
            $dest = Join-Path $PendingDir $file.Name
            if (-not (Test-Path -LiteralPath $dest)) {
                Copy-Item -LiteralPath $file.FullName -Destination $dest -Force
            }

            if (Test-Path -LiteralPath $dest) {
                $sourceHash = Get-McpFailsafeFileSha256Hex -Path $file.FullName
                $destHash = Get-McpFailsafeFileSha256Hex -Path $dest
                if ($sourceHash -eq $destHash) {
                    Remove-Item -LiteralPath $file.FullName -Force
                }
            }
        }
    }
}

function Get-McpFailsafeDir {
    <#
    .SYNOPSIS
        TR-MCP-REPL-016: resolves the V4 failsafe pending directory.
    .DESCRIPTION
        Single source of truth for the queue location so the writer (repl-invoke),
        the drain, and the status reporter all agree with
        FilesystemSessionLogPersistenceStrategy:
        {workspace}/.mcpServer/failsafe/{agent}/workspaces/{workspace-key}/pending/.
        MCPSERVER_FAILSAFE_DIR and MCP_FAILSAFE_DIR override for tests and recovery.
        Legacy {workspace}/.mcpServer/{agent}/failsafe YAML is migrated by checksum
        match then source delete.
    #>
    [CmdletBinding()]
    param([string]$StartPath)

    if ($env:MCPSERVER_FAILSAFE_DIR) { return $env:MCPSERVER_FAILSAFE_DIR }
    if ($env:MCP_FAILSAFE_DIR) { return $env:MCP_FAILSAFE_DIR }

    # FR-MCP-SESSIONLIFE-003: a cache override is the supported markerless
    # recovery target. Keep the write-ahead queue beside it when no workspace
    # root can be resolved, so title and submit calls are not reported lost
    # before the server is attempted. An explicit failsafe override above
    # still wins, including a non-directory used to prove a lost write.
    try {
        $workspace = Resolve-McpFailsafeWorkspacePath -StartPath $StartPath
    } catch {
        $override = $env:MCP_CACHE_DIR_OVERRIDE
        if ($override -and (Test-Path -LiteralPath $override -PathType Container)) {
            $overridePending = Join-Path $override 'failsafe-pending'
            [void][System.IO.Directory]::CreateDirectory($overridePending)
            return $overridePending
        }

        throw
    }
    $agent = Get-McpFailsafeAgentSegment
    $key = Get-McpFailsafeWorkspaceKey -WorkspacePath $workspace
    $pending = Join-Path $workspace (Join-Path '.mcpServer' (Join-Path 'failsafe' (Join-Path $agent (Join-Path 'workspaces' (Join-Path $key 'pending')))))
    [void][System.IO.Directory]::CreateDirectory($pending)
    Invoke-McpFailsafeLegacyQueueMigration -WorkspacePath $workspace -PendingDir $pending
    return $pending
}

function Get-McpFailsafeQuarantineDir {
    <#
    .SYNOPSIS
        TR-MCP-REPL-017: resolves the quarantine directory under the failsafe queue.
    .DESCRIPTION
        Records that cannot be replayed are moved here instead of being deleted or
        retried forever, so nothing is lost and the live queue keeps draining.
    #>
    [CmdletBinding()]
    param([string]$StartPath)

    return (Join-Path (Get-McpFailsafeDir -StartPath $StartPath) 'quarantine')
}

function Get-McpPathVolumeRoot {
    <#
    .SYNOPSIS
        FR-MCP-TEMPVOL-001: returns the volume root of a filesystem path.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }

    try {
        $full = [System.IO.Path]::GetFullPath($Path)
        $root = [System.IO.Path]::GetPathRoot($full)
        if ([string]::IsNullOrWhiteSpace($root)) { return $null }
        return $root
    } catch {
        return $null
    }
}

function New-McpPluginTempAlignmentResult {
    <#
    .SYNOPSIS
        FR-MCP-TEMPVOL-001: builds the TEMP alignment result object.
    #>
    [CmdletBinding()]
    param(
        [bool]$Succeeded,
        [bool]$Changed,
        [string]$TempPath,
        [string]$ErrorMessage
    )

    return [pscustomobject]@{
        Succeeded = [bool]$Succeeded
        Changed = [bool]$Changed
        TempPath = $TempPath
        Error = $ErrorMessage
    }
}

function Get-McpPluginWorkspaceTempDirectory {
    <#
    .SYNOPSIS
        FR-MCP-TEMPVOL-001: prefers workspace .mcpServer/tmp on the target volume.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$TargetPath)

    $dir = $TargetPath
    $isLeaf = $false
    try {
        $isLeaf = Test-Path -LiteralPath $TargetPath -PathType Leaf
    } catch {
        $isLeaf = $false
    }
    if ($isLeaf) {
        $dir = Split-Path -Parent $TargetPath
    }

    $cursor = $dir
    for ($i = 0; $i -lt 20 -and -not [string]::IsNullOrWhiteSpace($cursor); $i++) {
        $marker = Join-Path $cursor 'AGENTS-README-FIRST.yaml'
        $markerExists = $false
        try {
            $markerExists = Test-Path -LiteralPath $marker -PathType Leaf
        } catch {
            $markerExists = $false
        }
        if ($markerExists) {
            return (Join-Path (Join-Path $cursor '.mcpServer') 'tmp')
        }

        $parent = Split-Path -Parent $cursor
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -eq $cursor) { break }
        $cursor = $parent
    }

    return (Join-Path (Join-Path $dir '.mcpServer') 'tmp')
}

function Set-McpPluginSameVolumeTemp {
    <#
    .SYNOPSIS
        FR-MCP-TEMPVOL-001 / TR-MCP-TEMPVOL-001: align process TEMP and TMP to the target volume.
    .DESCRIPTION
        When TEMP/TMP are on a different volume than TargetPath, sets both to a writable
        directory on the target volume. Same-volume TEMP is left unchanged. A failed
        directory create does not mutate TEMP/TMP and returns Succeeded false with Error.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$TargetPath)

    $currentTemp = if (-not [string]::IsNullOrWhiteSpace($env:TEMP)) {
        $env:TEMP
    } elseif (-not [string]::IsNullOrWhiteSpace($env:TMP)) {
        $env:TMP
    } else {
        [System.IO.Path]::GetTempPath()
    }

    $targetRoot = Get-McpPathVolumeRoot -Path $TargetPath
    $tempRoot = Get-McpPathVolumeRoot -Path $currentTemp
    if ($targetRoot -and $tempRoot -and $targetRoot.Equals($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return (New-McpPluginTempAlignmentResult -Succeeded $true -Changed $false -TempPath $currentTemp)
    }

    try {
        $alignedDir = Get-McpPluginWorkspaceTempDirectory -TargetPath $TargetPath
        [void][System.IO.Directory]::CreateDirectory($alignedDir)
        $probe = Join-Path $alignedDir ('.write-probe-' + [guid]::NewGuid().ToString('N'))
        [System.IO.File]::WriteAllText($probe, 'ok')
        [System.IO.File]::Delete($probe)
    } catch {
        return (New-McpPluginTempAlignmentResult -Succeeded $false -Changed $false -TempPath $currentTemp -ErrorMessage ('Unable to create writable TEMP on the workspace volume: ' + $_.Exception.Message))
    }

    $resolved = [System.IO.Path]::GetFullPath($alignedDir)
    $env:TEMP = $resolved
    $env:TMP = $resolved
    [Environment]::SetEnvironmentVariable('TEMP', $resolved, 'Process')
    [Environment]::SetEnvironmentVariable('TMP', $resolved, 'Process')
    return (New-McpPluginTempAlignmentResult -Succeeded $true -Changed $true -TempPath $resolved)
}

function Invoke-McpPluginReplacementMove {
    <#
    .SYNOPSIS
        FR-MCP-TEMPVOL-001: replacement move that never reports success when the destination is unchanged.
    .DESCRIPTION
        Cross-volume File.Move is refused with a visible Error. Exceptions and missing
        destinations also return Succeeded false. Preview-without-apply cannot look like success.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SourcePath,
        [Parameter(Mandatory)][string]$DestinationPath
    )

    $failed = {
        param([string]$Message)
        return [pscustomobject]@{
            Succeeded = $false
            Error = $Message
            DestinationUnchanged = $true
        }
    }

    $srcRoot = $null
    $dstRoot = $null
    try { $srcRoot = Get-McpPathVolumeRoot -Path $SourcePath } catch { $srcRoot = $null }
    try { $dstRoot = Get-McpPathVolumeRoot -Path $DestinationPath } catch { $dstRoot = $null }

    if ([string]::IsNullOrWhiteSpace($srcRoot) -or [string]::IsNullOrWhiteSpace($dstRoot)) {
        return (& $failed 'replacement move refused: source or destination volume could not be resolved')
    }

    if (-not $srcRoot.Equals($dstRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return (& $failed 'cross-volume replacement move refused: TEMP/TMP must be on the destination volume')
    }

    try {
        if (-not (Test-Path -LiteralPath $SourcePath -PathType Leaf)) {
            return (& $failed 'replacement move failed: source file is missing')
        }

        $destDir = Split-Path -Parent $DestinationPath
        if ($destDir -and -not (Test-Path -LiteralPath $destDir -PathType Container)) {
            return (& $failed 'replacement move failed: destination directory is missing')
        }

        [System.IO.File]::Move($SourcePath, $DestinationPath, $true)
    } catch {
        return (& $failed ('replacement move failed: ' + $_.Exception.Message))
    }

    if (-not (Test-Path -LiteralPath $DestinationPath -PathType Leaf)) {
        return (& $failed 'replacement move failed: destination is missing after move')
    }

    return [pscustomobject]@{
        Succeeded = $true
        Error = $null
        DestinationUnchanged = $false
    }
}
