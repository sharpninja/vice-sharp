# Shared agent runtime header resolution for plugin-created session logs.

function Get-McpPluginFirstText {
    param([AllowNull()][object[]]$Values)

    foreach ($value in $Values) {
        if ($null -ne $value -and -not [string]::IsNullOrWhiteSpace([string]$value)) {
            return [string]$value
        }
    }

    return ''
}

function Get-McpPluginAgentExecutableCandidates {
    param(
        [string]$AgentName,
        [string]$HostName)

    $key = Get-McpPluginFirstText @($HostName, $AgentName)
    $key = ($key.Trim().ToLowerInvariant() -replace '[^a-z0-9]', '')

    $candidates = [System.Collections.Generic.List[string]]::new()
    foreach ($candidate in @(
        $env:MCP_AGENT_EXECUTABLE_PATH,
        $env:CODEX_EXECUTABLE_PATH,
        $env:CLAUDE_EXECUTABLE_PATH,
        $env:GROK_EXECUTABLE_PATH,
        $env:COPILOT_EXECUTABLE_PATH,
        $env:CLINE_EXECUTABLE_PATH,
        $env:OPENCODE_EXECUTABLE_PATH)) {
        if (-not [string]::IsNullOrWhiteSpace($candidate)) { $candidates.Add([string]$candidate) }
    }

    switch ($key) {
        { $_ -in @('codex') } { $candidates.Add('codex'); $candidates.Add('codex.cmd') }
        { $_ -in @('claude', 'claudecode') } { $candidates.Add('claude'); $candidates.Add('claude.cmd') }
        { $_ -in @('claudecowork') } { $candidates.Add('claude'); $candidates.Add('claude.cmd') }
        { $_ -in @('grok', 'grokcode') } { $candidates.Add('grok'); $candidates.Add('grok.cmd') }
        { $_ -in @('copilot') } { $candidates.Add('github-copilot'); $candidates.Add('copilot') }
        { $_ -in @('cline', 'clinev2') } { $candidates.Add('cline'); $candidates.Add('cline.cmd') }
        { $_ -in @('opencode') } { $candidates.Add('opencode'); $candidates.Add('opencode.cmd') }
    }

    return @($candidates | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
}

function Resolve-McpPluginAgentExecutablePath {
    param(
        [string]$AgentName,
        [string]$HostName,
        [string[]]$ExecutableCandidates = @())

    $candidates = @($ExecutableCandidates) + @(Get-McpPluginAgentExecutableCandidates -AgentName $AgentName -HostName $HostName)
    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        try {
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                return (Resolve-Path -LiteralPath $candidate).ProviderPath
            }

            $command = Get-Command $candidate -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
                return [string]$command.Source
            }
        } catch {
            continue
        }
    }

    $pluginWrapper = ''
    if (-not [string]::IsNullOrWhiteSpace($env:MCP_PLUGIN_ROOT)) {
        $candidateWrapper = Join-Path $env:MCP_PLUGIN_ROOT 'Invoke-McpPlugin.ps1'
        if (Test-Path -LiteralPath $candidateWrapper -PathType Leaf) { $pluginWrapper = $candidateWrapper }
    }

    return Get-McpPluginFirstText @($env:MCP_AGENT_EXECUTABLE_PATH, $pluginWrapper)
}

function Resolve-McpPluginAgentExecutableVersion {
    param(
        [string]$ExecutablePath,
        [string]$AgentName,
        [string]$HostName)

    $explicit = Get-McpPluginFirstText @(
        $env:MCP_AGENT_EXECUTABLE_VERSION,
        $env:CODEX_EXECUTABLE_VERSION,
        $env:CLAUDE_EXECUTABLE_VERSION,
        $env:GROK_EXECUTABLE_VERSION,
        $env:COPILOT_EXECUTABLE_VERSION,
        $env:CLINE_EXECUTABLE_VERSION,
        $env:OPENCODE_EXECUTABLE_VERSION)
    if ($explicit) { return $explicit }

    if (-not [string]::IsNullOrWhiteSpace($ExecutablePath) -and (Test-Path -LiteralPath $ExecutablePath -PathType Leaf)) {
        try {
            $psi = [System.Diagnostics.ProcessStartInfo]::new()
            $psi.FileName = $ExecutablePath
            $psi.ArgumentList.Add('--version')
            $psi.UseShellExecute = $false
            $psi.RedirectStandardOutput = $true
            $psi.RedirectStandardError = $true
            $process = [System.Diagnostics.Process]::Start($psi)
            if ($process.WaitForExit(3000)) {
                $version = ($process.StandardOutput.ReadToEnd() + "`n" + $process.StandardError.ReadToEnd()).Trim()
                if ($version) { return ($version -split "\r?\n" | Where-Object { $_ } | Select-Object -First 1) }
            } else {
                $process.Kill($true)
            }
        } catch {
        }
    }

    # TR-MCP-PLUGIN-HEADER-001: the plugin version is NOT the agent executable
    # version. When live discovery fails the honest answer is 'unknown'; reporting
    # MCP_PLUGIN_VERSION here put a false version on every session header.
    return 'unknown'
}

function Get-McpPluginFirstExistingFile {
    # TR-MCP-PLUGIN-HEADER-001: return the first candidate that is an existing file.
    # A transcript path is only accurate if the file is actually there.
    param([AllowNull()][object[]]$Values)

    foreach ($value in $Values) {
        if ($null -eq $value) { continue }
        $candidate = [string]$value
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        try {
            if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                return (Resolve-Path -LiteralPath $candidate).ProviderPath
            }
        } catch {
            continue
        }
    }

    return ''
}

function Resolve-McpPluginAgentHeaderFields {
    param(
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][string]$CacheDir,
        [string]$AgentName,
        [string]$HostName,
        [string]$ProviderSessionId,
        [string]$TranscriptPath,
        [string[]]$ExecutableCandidates = @())

    # TR-MCP-PLUGIN-HEADER-001: this field is defined as the PROVIDER-NATIVE session
    # id, so it carries only an observed provider value (hook payload or host env).
    # It is deliberately left empty when the provider id is unknown: echoing the MCP
    # session id here made the field never-empty but frequently wrong.
    # Do not read another host's session id (a Grok conversation id must not populate
    # a Codex header).
    $hostKey = @($HostName, $AgentName) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
    $hostKey = if ($hostKey) { $hostKey.Trim().ToLowerInvariant() } else { '' }
    $sessionIdCandidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ProviderSessionId)) { [void]$sessionIdCandidates.Add($ProviderSessionId) }
    if (-not [string]::IsNullOrWhiteSpace($env:MCP_AGENT_SESSION_ID)) { [void]$sessionIdCandidates.Add($env:MCP_AGENT_SESSION_ID) }
    switch -Regex ($hostKey) {
        '^(codex)$' { if ($env:CODEX_SESSION_ID) { [void]$sessionIdCandidates.Add($env:CODEX_SESSION_ID) } }
        '^(claude|claude-code|claudecode)$' { if ($env:CLAUDE_SESSION_ID) { [void]$sessionIdCandidates.Add($env:CLAUDE_SESSION_ID) } }
        '^(grok|grokcode|grok-code)$' { if ($env:GROK_SESSION_ID) { [void]$sessionIdCandidates.Add($env:GROK_SESSION_ID) } }
        default {
            if ($env:CODEX_SESSION_ID) { [void]$sessionIdCandidates.Add($env:CODEX_SESSION_ID) }
            if ($env:CLAUDE_SESSION_ID) { [void]$sessionIdCandidates.Add($env:CLAUDE_SESSION_ID) }
            if ($env:GROK_SESSION_ID) { [void]$sessionIdCandidates.Add($env:GROK_SESSION_ID) }
        }
    }
    $agentSessionId = Get-McpPluginFirstText @($sessionIdCandidates)

    # TR-MCP-PLUGIN-HEADER-001: only ever report a transcript file that exists on
    # disk. Previously this fell through to <CacheDir>/session.jsonl unconditionally,
    # writing a path to a file the plugin never creates.
    $transcriptCandidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($TranscriptPath)) { [void]$transcriptCandidates.Add($TranscriptPath) }
    if (-not [string]::IsNullOrWhiteSpace($env:MCP_AGENT_SESSION_TRANSCRIPT_FILE)) { [void]$transcriptCandidates.Add($env:MCP_AGENT_SESSION_TRANSCRIPT_FILE) }
    switch -Regex ($hostKey) {
        '^(codex)$' {
            if ($env:CODEX_SESSION_FILE) { [void]$transcriptCandidates.Add($env:CODEX_SESSION_FILE) }
            if ($env:CODEX_ROLLOUT_FILE) { [void]$transcriptCandidates.Add($env:CODEX_ROLLOUT_FILE) }
        }
        '^(claude|claude-code|claudecode)$' {
            if ($env:CLAUDE_TRANSCRIPT_PATH) { [void]$transcriptCandidates.Add($env:CLAUDE_TRANSCRIPT_PATH) }
            if ($env:CLAUDE_SESSION_FILE) { [void]$transcriptCandidates.Add($env:CLAUDE_SESSION_FILE) }
        }
        '^(grok|grokcode|grok-code)$' {
            if ($env:GROK_TRANSCRIPT_PATH) { [void]$transcriptCandidates.Add($env:GROK_TRANSCRIPT_PATH) }
            if ($env:GROK_SESSION_FILE) { [void]$transcriptCandidates.Add($env:GROK_SESSION_FILE) }
        }
        default {
            if ($env:CODEX_SESSION_FILE) { [void]$transcriptCandidates.Add($env:CODEX_SESSION_FILE) }
            if ($env:CODEX_ROLLOUT_FILE) { [void]$transcriptCandidates.Add($env:CODEX_ROLLOUT_FILE) }
            if ($env:CLAUDE_TRANSCRIPT_PATH) { [void]$transcriptCandidates.Add($env:CLAUDE_TRANSCRIPT_PATH) }
            if ($env:CLAUDE_SESSION_FILE) { [void]$transcriptCandidates.Add($env:CLAUDE_SESSION_FILE) }
            if ($env:GROK_TRANSCRIPT_PATH) { [void]$transcriptCandidates.Add($env:GROK_TRANSCRIPT_PATH) }
            if ($env:GROK_SESSION_FILE) { [void]$transcriptCandidates.Add($env:GROK_SESSION_FILE) }
        }
    }
    [void]$transcriptCandidates.Add((Join-Path $CacheDir 'session.jsonl'))
    $transcriptFile = Get-McpPluginFirstExistingFile @($transcriptCandidates)
    $executablePath = Resolve-McpPluginAgentExecutablePath -AgentName $AgentName -HostName $HostName -ExecutableCandidates $ExecutableCandidates
    $executableVersion = Resolve-McpPluginAgentExecutableVersion -ExecutablePath $executablePath -AgentName $AgentName -HostName $HostName

    return [ordered]@{
        agentSessionId = $agentSessionId
        agentSessionTranscriptFile = $transcriptFile
        agentExecutablePath = $executablePath
        agentExecutableVersion = $executableVersion
    }
}