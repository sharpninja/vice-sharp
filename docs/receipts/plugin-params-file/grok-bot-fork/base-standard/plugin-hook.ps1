#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('session-start', 'session-end', 'pre-compact', 'post-compact', 'user-prompt-submit', 'stop-gate', 'code-verify', 'plan-approved', 'plan-modified', 'cache-flush', 'health-check', 'subagent-import')]
    [string]$HookName,

    [string]$HostName = $(if ($env:MCP_PLUGIN_HOST) { $env:MCP_PLUGIN_HOST } else { 'codex' }),

    [ValidateSet('flat', 'scoped')]
    [string]$CacheMode = 'scoped',

    [string]$WorkspacePath,

    [string]$Params,

    [string]$ParamsPath,

    [Parameter(ValueFromPipeline = $true)]
    [object]$InputObject
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
# TR-MCP-PLUGIN-HEADER-001: Read-HookInput is memoized because reading stdin
# consumes it and the session bootstrap now also needs the payload. These MUST be
# initialized here: Set-StrictMode -Version Latest makes referencing an unset
# variable a terminating error.
$script:HookInputCached = $false
$script:CachedHookInput = ''
$env:MCP_PLUGIN_HOST = $HostName
. (Join-Path $script:ScriptDir 'plugin-env.ps1')
. (Join-Path $script:ScriptDir 'resolve-cache-dir.ps1')
. (Join-Path $script:ScriptDir 'marker-resolver.ps1')
. (Join-Path $script:ScriptDir 'yaml-object-mutation.ps1')
. (Join-Path $script:ScriptDir 'plan-todo-map.ps1')
. (Join-Path $script:ScriptDir 'agent-runtime-header.ps1')
Import-McpYamlSerializer

function Get-PluginHookDeadline {
    <#
    .SYNOPSIS
        TR-MCP-REPL-012 AC-001 (D1, triage-report-124a374c): this hook's deadline in Unix milliseconds: the hook
        process's start time plus the timeout the plugin's generated hooks.json registers for the hook's script,
        less a three-second margin for writing the hook's result (measured 2026-10-14: up to 2.2 s from the
        deadline to process exit under load). $null when hooks.json is missing or unreadable or registers no
        timeout for the hook.
    #>
    param(
        [Parameter(Mandatory)][string]$HooksJsonPath,
        [Parameter(Mandatory)][string]$HookName,
        [Parameter(Mandatory)][datetime]$ProcessStartUtc
    )

    if (-not (Test-Path -LiteralPath $HooksJsonPath -PathType Leaf)) { return $null }
    try {
        $document = [System.IO.File]::ReadAllText($HooksJsonPath) | ConvertFrom-Json -Depth 20 -ErrorAction Stop
    } catch {
        return $null
    }
    $hooksProperty = if ($null -ne $document) { $document.PSObject.Properties['hooks'] } else { $null }
    if (-not $hooksProperty -or $null -eq $hooksProperty.Value) { return $null }

    # Property lookups through PSObject keep this safe under Set-StrictMode -Version Latest.
    $scriptPattern = '[\\/]hooks[\\/]scripts[\\/]' + [regex]::Escape($HookName) + '\.ps1'
    foreach ($hookEvent in $hooksProperty.Value.PSObject.Properties) {
        foreach ($group in @($hookEvent.Value)) {
            $groupHooks = if ($null -ne $group) { $group.PSObject.Properties['hooks'] } else { $null }
            if (-not $groupHooks) { continue }
            foreach ($hook in @($groupHooks.Value)) {
                if ($null -eq $hook) { continue }
                $command = $hook.PSObject.Properties['command']
                $timeoutProperty = $hook.PSObject.Properties['timeout']
                $timeout = 0
                if ($command -and $timeoutProperty -and [string]$command.Value -match $scriptPattern -and
                    [int]::TryParse([string]$timeoutProperty.Value, [ref]$timeout) -and $timeout -gt 0) {
                    return ([DateTimeOffset]$ProcessStartUtc.ToUniversalTime()).ToUnixTimeMilliseconds() + ([long]$timeout * 1000) - 3000
                }
            }
        }
    }
    return $null
}

function Test-PluginHookOwnsProcess {
    <#
    .SYNOPSIS
        TR-MCP-REPL-012 AC-001 (D1; operator decision 2026-10-15 "Scope D1 to the hook process"): whether this process
        was started for the hook, that is, one of its command-line arguments is the hook's entry script under the plugin
        root: hooks/scripts/<HookName>.ps1, lib/<HookName>.ps1 (hosts that keep their entries in lib/), or
        lib/plugin-hook.ps1 started directly; or names the running plugin-hook.ps1 itself (HookScriptPath), which can
        live outside the plugin root, as the plugin core's own does. A hook script run inside a process started for
        something else, such as a test runner, does not own that process, so no host timeout applies to it.
    #>
    param(
        [Parameter(Mandatory)][string]$PluginRoot,
        [Parameter(Mandatory)][string]$HookName,
        [string[]]$CommandLineArgs = [Environment]::GetCommandLineArgs(),
        [string]$HookScriptPath
    )

    $comparison = if ($IsWindows) { [System.StringComparison]::OrdinalIgnoreCase } else { [System.StringComparison]::Ordinal }
    $entries = @(
        [System.IO.Path]::Combine($PluginRoot, 'hooks', 'scripts', "$HookName.ps1"),
        [System.IO.Path]::Combine($PluginRoot, 'lib', "$HookName.ps1"),
        [System.IO.Path]::Combine($PluginRoot, 'lib', 'plugin-hook.ps1')
    ) + @($HookScriptPath | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) | ForEach-Object { [System.IO.Path]::GetFullPath($_) }
    foreach ($argument in @($CommandLineArgs)) {
        if ([string]::IsNullOrWhiteSpace($argument) -or $argument.StartsWith('-')) { continue }
        $fullPath = $null
        try { $fullPath = [System.IO.Path]::GetFullPath($argument) } catch { continue }
        foreach ($entry in $entries) {
            if ([string]::Equals($fullPath, $entry, $comparison)) { return $true }
        }
    }
    return $false
}

function Register-PluginHookDeadline {
    <#
    .SYNOPSIS
        TR-MCP-REPL-012 AC-001 (D1): publishes this hook's deadline in MCP_HOOK_DEADLINE_UTC_MS, measured from this
        process's start, for repl-invoke.ps1 and the child processes the hook starts, when this process was started for
        the hook (Test-PluginHookOwnsProcess); a hook script run inside another process publishes nothing. An earlier
        deadline already published by an enclosing hook is kept while it is still ahead; a deadline that has passed is
        stale and is replaced. Without a registered timeout nothing is published. The generated hook entry restores the
        variable when it returns.
    #>
    param([Parameter(Mandatory)][string]$HookName, [string]$HookScriptPath)

    $pluginRoot = [string]$env:MCP_PLUGIN_ROOT
    if ([string]::IsNullOrWhiteSpace($pluginRoot)) { return }
    if (-not (Test-PluginHookOwnsProcess -PluginRoot $pluginRoot -HookName $HookName -HookScriptPath $HookScriptPath)) { return }
    $processStartUtc = [System.Diagnostics.Process]::GetCurrentProcess().StartTime.ToUniversalTime()
    $deadline = Get-PluginHookDeadline -HooksJsonPath (Join-Path (Join-Path $pluginRoot 'hooks') 'hooks.json') -HookName $HookName -ProcessStartUtc $processStartUtc
    if ($null -eq $deadline) { return }
    $existing = 0L
    if ([long]::TryParse([string]$env:MCP_HOOK_DEADLINE_UTC_MS, [ref]$existing) -and
        $existing -gt [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() -and $existing -lt $deadline) { return }
    $env:MCP_HOOK_DEADLINE_UTC_MS = [string]$deadline
}

# TR-MCP-REPL-012 AC-001 (D1): every server call this hook makes ends before the hook's registered budget runs out.
Register-PluginHookDeadline -HookName $HookName -HookScriptPath $PSCommandPath

function ConvertTo-PluginParamsYaml {
    param([Parameter(Mandatory)]$Params)

    return (ConvertTo-Yaml -Data $Params -Options WithIndentedSequences)
}

function New-PluginSessionId {
    param([Parameter(Mandatory)][string]$AgentName)

    if ($env:MCP_SESSION_ID) { return $env:MCP_SESSION_ID }

    $timestamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $suffix = if ($env:MCP_SESSION_SUFFIX) { $env:MCP_SESSION_SUFFIX } else { 'plugin-session' }
    $suffix = ($suffix.ToLowerInvariant() -replace '[^a-z0-9]+', '-').Trim('-')
    if (-not $suffix) { $suffix = 'plugin-session' }

    return '{0}-{1}-{2}' -f $AgentName, $timestamp, $suffix
}

function Write-PluginJson {
    param([Parameter(Mandatory)]$Value)
    $Value | ConvertTo-Json -Depth 20 -Compress
}

function Stop-PluginUnavailable {
    $agent = if ($env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME } else { 'Agent' }
    Write-Output "MCP_PLUGIN_UNAVAILABLE:$agent"
    exit 0
}

function Confirm-PowerShellMcpRuntime {
    if ($env:MCP_PLUGIN_REFUSE_POWERSHELL -eq '1') {
        return $false
    }

    try {
        if (-not (Get-Module -ListAvailable -Name PowerShell.MCP)) {
            if (Get-Command Install-PSResource -ErrorAction SilentlyContinue) {
                Install-PSResource -Name PowerShell.MCP -Scope CurrentUser -TrustRepository -Quiet -ErrorAction Stop
            } else {
                Install-Module -Name PowerShell.MCP -Repository PSGallery -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
            }
        }

        Import-Module PowerShell.MCP -ErrorAction Stop
        return $true
    } catch {
        return $false
    }
}

function Get-PluginCacheDir {
    param([string]$StartPath)

    $cacheDir = Resolve-McpCacheDir -StartPath $StartPath
    if (-not (Test-Path -LiteralPath $cacheDir)) {
        [void][System.IO.Directory]::CreateDirectory($cacheDir)
    }
    return $cacheDir
}

function Get-PluginStartPath {
    param([string]$PreferredPath)

    if ($PreferredPath) { return $PreferredPath }
    if ($WorkspacePath) { return $WorkspacePath }

    $currentPath = (Get-Location).ProviderPath
    if ($currentPath -and (Get-Command Find-MarkerFile -ErrorAction SilentlyContinue)) {
        try {
            if (Find-MarkerFile -StartDir $currentPath) { return $currentPath }
        } catch {
        }
    }

    if ($env:MCP_WORKSPACE_START_DIR) { return $env:MCP_WORKSPACE_START_DIR }
    if ($env:CLAUDE_PROJECT_DIR) { return $env:CLAUDE_PROJECT_DIR }
    if ($env:CODEX_CWD) { return $env:CODEX_CWD }
    if ($env:CODEX_WORKSPACE_PATH) { return $env:CODEX_WORKSPACE_PATH }
    if ($env:CODEX_PROJECT_DIR) { return $env:CODEX_PROJECT_DIR }
    if ($env:COWORK_WORKSPACE_PATH) { return $env:COWORK_WORKSPACE_PATH }
    if ($env:COPILOT_WORKSPACE_PATH) { return $env:COPILOT_WORKSPACE_PATH }
    if ($env:CLINE_WORKSPACE_PATH) { return $env:CLINE_WORKSPACE_PATH }
    if ($env:OPENCODE_WORKSPACE_PATH) { return $env:OPENCODE_WORKSPACE_PATH }
    if ($env:MCPSERVER_WORKSPACE_PATH) { return $env:MCPSERVER_WORKSPACE_PATH }
    if ($env:MCP_WORKSPACE_PATH) { return $env:MCP_WORKSPACE_PATH }
    return $currentPath
}

function Set-PluginWorkspaceIdentity {
    param([string]$ResolvedPath)
    if ([string]::IsNullOrWhiteSpace($ResolvedPath)) { return }
    if (-not (Test-Path -LiteralPath $ResolvedPath -PathType Container)) { return }
    $resolved = (Resolve-Path -LiteralPath $ResolvedPath).ProviderPath
    $env:MCP_WORKSPACE_PATH = $resolved
    $env:MCPSERVER_WORKSPACE_PATH = $resolved
    $env:MCP_WORKSPACE_START_DIR = $resolved
    try { Set-Location -LiteralPath $resolved } catch { }
}

function Get-YamlScalar {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Key
    )

    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $document = Read-McpYamlObject -Path $Path
    if ($document -isnot [System.Collections.IDictionary]) { return $null }
    if (-not $document.Contains($Key) -or $null -eq $document[$Key]) { return $null }
    return ([string]$document[$Key]).Trim().Trim('"')
}

function Set-YamlScalar {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Key,
        [Parameter(Mandatory)][string]$Value
    )

    if (-not (Test-Path -LiteralPath $Path)) { return }
    Set-McpYamlObjectValue -Path $Path -KeyPath @($Key) -Value $Value | Out-Null
}

function Read-HookInput {
    if ($script:HookInputCached) {
        return $script:CachedHookInput
    }

    $value = ''
    if ($Params) {
        $value = $Params
    } elseif ($ParamsPath) {
        if (-not (Test-Path -LiteralPath $ParamsPath)) {
            throw "Hook params file was not found: $ParamsPath"
        }

        $value = [System.IO.File]::ReadAllText($ParamsPath)
    } elseif ($null -ne $InputObject) {
        if ($InputObject -is [string]) {
            $value = [string]$InputObject
        } else {
            $value = ($InputObject | ConvertTo-Json -Depth 20 -Compress)
        }
    } elseif ([Console]::IsInputRedirected) {
        $value = [Console]::In.ReadToEnd()
    }

    $script:CachedHookInput = $value
    $script:HookInputCached = $true
    return $value
}

function Get-HookPayloadValue {
    param(
        [string]$Payload,
        [string]$Name
    )

    if (-not $Payload) { return '' }
    try {
        $json = $Payload | ConvertFrom-Json -Depth 20 -ErrorAction Stop
        return [string]$json.$Name
    } catch {
        return ''
    }
}

function Invoke-PluginRepl {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$ParamsYaml = ''
    )

    $script:LastPluginReplExitCode = 0
    if ($env:MCP_PLUGIN_REPL_LOG) {
        Write-McpReplLogEntry -Path $env:MCP_PLUGIN_REPL_LOG -Method $Method -ParamsYaml $ParamsYaml
        if ($env:MCP_PLUGIN_REPL_RESPONSE) {
            Write-Output $env:MCP_PLUGIN_REPL_RESPONSE
        }
        return
    }

    & (Join-Path $script:ScriptDir 'repl-invoke.ps1') -Method $Method -ParamsYaml $ParamsYaml
    $exitCodeVariable = Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue
    $script:LastPluginReplExitCode = if ($null -ne $exitCodeVariable -and $null -ne $exitCodeVariable.Value) { [int]$exitCodeVariable.Value } else { 0 }
}

function Invoke-PluginOpenSession {
    param([Parameter(Mandatory)][string]$ParamsYaml)

    if ($env:MCP_PLUGIN_REPL_LOG) {
        Invoke-PluginRepl -Method 'client.SessionLog.OpenSessionAsync' -ParamsYaml $ParamsYaml | Out-Null
        return ($script:LastPluginReplExitCode -eq 0)
    }

    $pwsh = (Get-Command pwsh -ErrorAction Stop).Source
    $repl = Join-Path $script:ScriptDir 'repl-invoke.ps1'
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $pwsh
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $workspace = @(
        $env:MCP_WORKSPACE_PATH,
        $env:MCPSERVER_WORKSPACE_PATH,
        (Get-Location).ProviderPath
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
    if ($workspace) { $psi.WorkingDirectory = $workspace }
    foreach ($argument in @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $repl, '-Method', 'client.SessionLog.OpenSessionAsync', '-ParamsYaml', $ParamsYaml)) {
        $psi.ArgumentList.Add($argument)
    }
    $process = [System.Diagnostics.Process]::Start($psi)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    try { $null = $stdoutTask.GetAwaiter().GetResult() } catch { }
    try { $null = $stderrTask.GetAwaiter().GetResult() } catch { }
    $script:LastPluginReplExitCode = [int]$process.ExitCode
    return ($process.ExitCode -eq 0)
}

function Start-PluginSession {
    param([string]$StartPath)

    $start = Get-PluginStartPath -PreferredPath $StartPath
    if (-not [string]::IsNullOrWhiteSpace($start)) {
        $tempAlign = Set-McpPluginSameVolumeTemp -TargetPath $start
        if ($tempAlign -and -not $tempAlign.Succeeded -and $tempAlign.Error) {
            [Console]::Error.WriteLine([string]$tempAlign.Error)
        }
    }

    $cacheDir = Get-PluginCacheDir -StartPath $start
    $sessionFile = Join-Path $cacheDir 'session-state.yaml'
    $markerSnapshot = $null
    try {
        $markerSnapshot = Get-MarkerFileSnapshot -StartDir $start
    } catch {
        $markerSnapshot = $null
    }

    $verified = $false
    try {
        $verified = Invoke-FullBootstrap -StartDir $start
    } catch {
        $verified = $false
    }
    if (-not $verified) {
        $untrustedState = [ordered]@{
            status = 'MCP_UNTRUSTED'
            lastUpdated = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        if ($markerSnapshot) {
            $untrustedState['markerFilePath'] = $markerSnapshot.markerFilePath
            $untrustedState['markerLastWriteUtc'] = $markerSnapshot.markerLastWriteUtc
        }
        Write-McpYamlObject -Path $sessionFile -Document $untrustedState
        Write-PluginJson ([ordered]@{})
        return
    }

    if (-not $markerSnapshot) {
        $markerSnapshot = Get-MarkerFileSnapshot -StartDir $start
    }

    $sessionId = New-PluginSessionId -AgentName $env:MCP_AGENT_NAME
    # TR-MCP-PLUGIN-HEADER-001: capture the host's real session id and transcript
    # path from the hook payload so the header records observed values instead of
    # the MCP session id and a fabricated cache path.
    $bootstrapPayload = ''
    try { $bootstrapPayload = Read-HookInput } catch { $bootstrapPayload = '' }
    $providerSessionId = Get-HookPayloadValue -Payload $bootstrapPayload -Name 'session_id'
    $providerTranscript = Get-HookPayloadValue -Payload $bootstrapPayload -Name 'transcript_path'
    $agentHeaders = Resolve-McpPluginAgentHeaderFields -SessionId $sessionId -CacheDir $cacheDir -AgentName $env:MCP_AGENT_NAME -HostName $env:MCP_PLUGIN_HOST -ProviderSessionId $providerSessionId -TranscriptPath $providerTranscript
    $env:MCP_AGENT_SESSION_ID = [string]$agentHeaders.agentSessionId
    $env:MCP_AGENT_SESSION_TRANSCRIPT_FILE = [string]$agentHeaders.agentSessionTranscriptFile
    $env:MCP_AGENT_EXECUTABLE_PATH = [string]$agentHeaders.agentExecutablePath
    $env:MCP_AGENT_EXECUTABLE_VERSION = [string]$agentHeaders.agentExecutableVersion

    $agentName = if ($env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME } else { 'Agent' }
    $openParams = ConvertTo-PluginParamsYaml -Params ([ordered]@{
        agent = $agentName
        sessionId = $sessionId
        title = 'plugin-session'
        model = $(if ($env:MCP_MODEL) { $env:MCP_MODEL } else { 'plugin' })
    })
    $opened = Invoke-PluginOpenSession -ParamsYaml $openParams

    $now = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    $sessionState = [ordered]@{
        status = if ($opened) { 'verified' } else { 'persist-failed' }
        sessionId = $sessionId
        agent = $env:MCP_AGENT_NAME
        started = $now
        lastUpdated = $now
        markerFilePath = $markerSnapshot.markerFilePath
        markerLastWriteUtc = $markerSnapshot.markerLastWriteUtc
        agentSessionId = [string]$agentHeaders.agentSessionId
        agentSessionTranscriptFile = [string]$agentHeaders.agentSessionTranscriptFile
        agentExecutablePath = [string]$agentHeaders.agentExecutablePath
        agentExecutableVersion = [string]$agentHeaders.agentExecutableVersion
    }
    Write-McpYamlObject -Path $sessionFile -Document $sessionState
    Write-PluginJson ([ordered]@{})
}

function ConvertTo-PluginAgentKey {
    param([string]$AgentName)

    if ([string]::IsNullOrWhiteSpace($AgentName)) { return '' }

    switch ($AgentName.Trim().ToLowerInvariant()) {
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

    return (($AgentName.Trim() -replace '[^A-Za-z0-9]+', '-').Trim('-').ToLowerInvariant())
}

function Get-PluginExpectedAgentKey {
    $agent = @(
        $env:MCP_AGENT_NAME,
        $env:PLUGIN_AGENT_DEFAULT,
        $env:MCP_PLUGIN_HOST
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1

    return (ConvertTo-PluginAgentKey -AgentName $agent)
}

function Test-PluginSessionStateValid {
    param($State)

    if ($State -isnot [System.Collections.IDictionary]) { return $false }
    if ([string]$State['status'] -ne 'verified') { return $false }

    $sessionId = if ($State.Contains('sessionId')) { [string]$State['sessionId'] } else { '' }
    if ([string]::IsNullOrWhiteSpace($sessionId.Trim().Trim('''').Trim('"'))) { return $false }

    $expectedAgentKey = Get-PluginExpectedAgentKey
    if ($expectedAgentKey) {
        $stateAgent = if ($State.Contains('agent')) { [string]$State['agent'] } else { '' }
        if ((ConvertTo-PluginAgentKey -AgentName $stateAgent) -ne $expectedAgentKey) { return $false }
    }

    return $true
}

function Get-PluginNoSessionRecoveryPath {
    param([Parameter(Mandatory)][string]$CacheDir)

    return (Join-Path $CacheDir 'no-session-recovery.yaml')
}

function Read-PluginNoSessionRecoveryState {
    param([Parameter(Mandatory)][string]$CacheDir)

    $path = Get-PluginNoSessionRecoveryPath -CacheDir $CacheDir
    if (-not (Test-Path -LiteralPath $path)) { return $null }

    try {
        return Read-McpYamlObject -Path $path
    } catch {
        return $null
    }
}

function Get-PluginNoSessionRootId {
    param($Snapshot)

    if ($Snapshot) {
        return 'no-session:{0}:{1}' -f $Snapshot.markerFilePath, $Snapshot.markerLastWriteUtc
    }

    return 'no-session:unknown-marker'
}

function ConvertTo-PluginSafeFileStem {
    param([Parameter(Mandatory)][string]$Value)

    $hashBytes = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($Value))
    $hash = ([Convert]::ToHexString($hashBytes)).Substring(0, 12).ToLowerInvariant()
    $stem = (($Value -replace '[^A-Za-z0-9._-]+', '-').Trim('-'))
    if (-not $stem) { $stem = 'root' }
    if ($stem.Length -gt 80) { $stem = $stem.Substring(0, 80).Trim('-') }
    return ('{0}-{1}' -f $stem, $hash)
}

function Write-PluginRecoveryFailsafe {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$RootId,
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)]$Payload
    )

    try {
        $dir = Join-Path $CacheDir 'failsafe\pending'
        [void][System.IO.Directory]::CreateDirectory($dir)
        $fileName = '{0}-{1}.yaml' -f $Label, (ConvertTo-PluginSafeFileStem -Value $RootId)
        $path = Join-Path $dir $fileName
        $record = [ordered]@{
            method = $Method
            label = $Label
            rootId = $RootId
            timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
            payload = $Payload
        }
        Write-McpYamlObject -Path $path -Document $record
        return $path
    } catch {
        return ''
    }
}

function Clear-PluginRecoveryFailsafe {
    param([string]$Path)

    if ($Path -and (Test-Path -LiteralPath $Path)) {
        Remove-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
    }
}

function Test-PluginNoSessionRetrySuppressed {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        $Snapshot
    )

    $state = Read-PluginNoSessionRecoveryState -CacheDir $CacheDir
    if ($state -isnot [System.Collections.IDictionary]) { return $false }
    if ([string]$state['status'] -ne 'session-create-failed') { return $false }
    if (-not $Snapshot) { return $true }

    return ([string]$state['markerFilePath'] -eq [string]$Snapshot.markerFilePath -and
        [string]$state['markerLastWriteUtc'] -eq [string]$Snapshot.markerLastWriteUtc)
}

function Submit-PluginNoSessionTriage {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$StartPath,
        $Snapshot,
        [Parameter(Mandatory)][string]$Reason
    )

    $agent = if ($env:MCP_AGENT_NAME) { $env:MCP_AGENT_NAME } elseif ($env:PLUGIN_AGENT_DEFAULT) { $env:PLUGIN_AGENT_DEFAULT } else { $HostName }
    $rootId = 'triage:{0}' -f (Get-PluginNoSessionRootId -Snapshot $Snapshot)
    $params = [ordered]@{
        title = 'Plugin session recovery could not create verified session state'
        summary = 'The plugin health check succeeded, but session-state.yaml could not be recreated. Work should continue with degraded failsafe session logging until the marker timestamp changes.'
        component = 'mcpserver-plugin-core'
        affectedPaths = @('plugins/core/lib-ps/plugin-hook.ps1', 'plugins/core/lib-ps/repl-invoke.ps1')
        affectedSymbols = @('Open-PluginTurn', 'Start-PluginSession', 'Ensure-PluginMarkerFresh')
        errorSignature = 'plugin-no-session-create-failed'
        dedupeKey = $rootId
        evidence = $Reason
        reporterAgent = $agent
        workspacePath = $StartPath
        tags = @('BUG-TRIAGE-047', 'plugin', 'session-recovery')
        idempotencyKey = $rootId
    }
    if ($Snapshot) {
        $params['reproductionHints'] = @(
            ('markerFilePath={0}' -f $Snapshot.markerFilePath),
            ('markerLastWriteUtc={0}' -f $Snapshot.markerLastWriteUtc)
        )
    }

    $failsafePath = Write-PluginRecoveryFailsafe -CacheDir $CacheDir -Label 'no-session-triage' -RootId $rootId -Method 'workflow.triage.report' -Payload $params
    $paramsYaml = ConvertTo-PluginParamsYaml $params
    $output = Invoke-PluginRepl -Method 'workflow.triage.report' -ParamsYaml $paramsYaml
    if ($script:LastPluginReplExitCode -eq 0) {
        Clear-PluginRecoveryFailsafe -Path $failsafePath
        return [ordered]@{ submitted = $true; failsafePath = $null; output = (($output | Out-String).Trim()) }
    }

    return [ordered]@{ submitted = $false; failsafePath = $failsafePath; output = (($output | Out-String).Trim()) }
}

function Invoke-PluginNoSessionRecovery {
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [Parameter(Mandatory)][string]$SessionFile,
        [Parameter(Mandatory)][string]$StartPath
    )

    $snapshot = $null
    try {
        $snapshot = Get-MarkerFileSnapshot -StartDir $StartPath
    } catch {
        $snapshot = $null
    }

    $rootId = Get-PluginNoSessionRootId -Snapshot $snapshot
    $healthOk = $false
    try {
        $healthOk = [bool](Invoke-FullBootstrap -StartDir $StartPath)
    } catch {
        $healthOk = $false
    }

    if (-not $healthOk) {
        $state = [ordered]@{
            status = 'health-failed'
            healthStatus = 'failed'
            lastCheckedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        if ($snapshot) {
            $state['markerFilePath'] = $snapshot.markerFilePath
            $state['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
        }
        Write-McpYamlObject -Path (Get-PluginNoSessionRecoveryPath -CacheDir $CacheDir) -Document $state
        return $state
    }

    if (Test-PluginNoSessionRetrySuppressed -CacheDir $CacheDir -Snapshot $snapshot) {
        $state = Read-PluginNoSessionRecoveryState -CacheDir $CacheDir
        $state['healthStatus'] = 'healthy'
        $state['lastCheckedUtc'] = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        $state['message'] = 'Session creation retry suppressed until AGENTS-README-FIRST.yaml marker timestamp changes.'
        $failsafePayload = [ordered]@{
            workspacePath = $StartPath
            markerFilePath = if ($snapshot) { $snapshot.markerFilePath } else { '' }
            markerLastWriteUtc = if ($snapshot) { $snapshot.markerLastWriteUtc } else { '' }
            status = 'session-create-failed'
        }
        $state['failsafePath'] = Write-PluginRecoveryFailsafe -CacheDir $CacheDir -Label 'no-session-recovery' -RootId $rootId -Method 'workflow.sessionlog.recovery' -Payload $failsafePayload
        Write-McpYamlObject -Path (Get-PluginNoSessionRecoveryPath -CacheDir $CacheDir) -Document $state
        return $state
    }

    Start-PluginSession -StartPath $StartPath | Out-Null
    $stateAfterCreate = if (Test-Path -LiteralPath $SessionFile) { Read-McpYamlObject -Path $SessionFile } else { $null }
    if (Test-PluginSessionStateValid -State $stateAfterCreate) {
        $recoveryPath = Get-PluginNoSessionRecoveryPath -CacheDir $CacheDir
        if (Test-Path -LiteralPath $recoveryPath) { Remove-Item -LiteralPath $recoveryPath -Force -ErrorAction SilentlyContinue }
        return [ordered]@{ status = 'recovered'; healthStatus = 'healthy' }
    }

    $reason = 'health check succeeded, but session-state.yaml remained missing or unverified after Start-PluginSession.'
    $triage = Submit-PluginNoSessionTriage -CacheDir $CacheDir -StartPath $StartPath -Snapshot $snapshot -Reason $reason
    $failsafePayload = [ordered]@{
        workspacePath = $StartPath
        markerFilePath = if ($snapshot) { $snapshot.markerFilePath } else { '' }
        markerLastWriteUtc = if ($snapshot) { $snapshot.markerLastWriteUtc } else { '' }
        reason = $reason
        triageSubmitted = [bool]$triage['submitted']
    }
    $failsafePath = Write-PluginRecoveryFailsafe -CacheDir $CacheDir -Label 'no-session-recovery' -RootId $rootId -Method 'workflow.sessionlog.recovery' -Payload $failsafePayload
    $failureState = [ordered]@{
        status = 'session-create-failed'
        healthStatus = 'healthy'
        lastCheckedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        markerFilePath = if ($snapshot) { $snapshot.markerFilePath } else { '' }
        markerLastWriteUtc = if ($snapshot) { $snapshot.markerLastWriteUtc } else { '' }
        triageSubmitted = [bool]$triage['submitted']
        triageFailsafePath = [string]$triage['failsafePath']
        failsafePath = $failsafePath
        message = 'Session creation failed after a healthy marker/bootstrap check; retry suppressed until the marker timestamp changes.'
    }
    Write-McpYamlObject -Path (Get-PluginNoSessionRecoveryPath -CacheDir $CacheDir) -Document $failureState
    return $failureState
}

function Ensure-PluginMarkerFresh {
    param([string]$StartPath)

    $start = Get-PluginStartPath -PreferredPath $StartPath
    $cacheDir = Get-PluginCacheDir -StartPath $start
    $sessionFile = Join-Path $cacheDir 'session-state.yaml'

    try {
        $snapshotForRetry = $null
        try {
            $snapshotForRetry = Get-MarkerFileSnapshot -StartDir $start
        } catch {
            $snapshotForRetry = $null
        }

        if (-not (Test-Path -LiteralPath $sessionFile)) {
            if (Test-PluginNoSessionRetrySuppressed -CacheDir $cacheDir -Snapshot $snapshotForRetry) {
                return $false
            }
            Start-PluginSession -StartPath $start | Out-Null
        }

        if (-not (Test-Path -LiteralPath $sessionFile)) {
            return $false
        }

        $state = Read-McpYamlObject -Path $sessionFile
        if (-not (Test-PluginSessionStateValid -State $state)) {
            if (Test-PluginNoSessionRetrySuppressed -CacheDir $cacheDir -Snapshot $snapshotForRetry) {
                return $false
            }
            Start-PluginSession -StartPath $start | Out-Null
            if (-not (Test-Path -LiteralPath $sessionFile)) {
                return $false
            }

            $state = Read-McpYamlObject -Path $sessionFile
            if (-not (Test-PluginSessionStateValid -State $state)) {
                return $false
            }
        }
        $snapshot = if ($snapshotForRetry) { $snapshotForRetry } else { Get-MarkerFileSnapshot -StartDir $start }
        $cachedPath = [string]$state['markerFilePath']
        $cachedWriteUtc = [string]$state['markerLastWriteUtc']

        if ($cachedPath -ne $snapshot.markerFilePath -or $cachedWriteUtc -ne $snapshot.markerLastWriteUtc) {
            Start-PluginSession -StartPath $start | Out-Null
            $state = Read-McpYamlObject -Path $sessionFile
        }

        return (Test-PluginSessionStateValid -State $state)
    } catch {
        $untrustedState = [ordered]@{
            status = 'MCP_UNTRUSTED'
            lastUpdated = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        try {
            $snapshot = Get-MarkerFileSnapshot -StartDir $start
            $untrustedState['markerFilePath'] = $snapshot.markerFilePath
            $untrustedState['markerLastWriteUtc'] = $snapshot.markerLastWriteUtc
        } catch {
        }
        Write-McpYamlObject -Path $sessionFile -Document $untrustedState
        return $false
    }
}

function Test-PluginPromptIsHostEvent {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-003 (D4, triage-report-5c7278cd): true when a UserPromptSubmit prompt is made only of host events, Claude
        Code task notifications (<task-notification>...</task-notification>) and subagent hand-backs (<agent-message ...>...</agent-message>),
        with nothing but whitespace around them. Such a prompt is not an operator request. Any other text, an unclosed block, or an empty
        prompt makes it a request.
    #>
    param([AllowEmptyString()][string]$Prompt)

    if ([string]::IsNullOrWhiteSpace($Prompt)) { return $false }
    $rest = [regex]::Replace($Prompt, '(?s)<task-notification>.*?</task-notification>|<agent-message(?:\s[^>]*)?>.*?</agent-message>', '')
    return ($rest.Length -lt $Prompt.Length) -and [string]::IsNullOrWhiteSpace($rest)
}

function Test-PluginPromptIsBackgroundAgent {
    <#
    .SYNOPSIS
        FR-MCP-TRIAGEPLUGIN-001: true when UserPromptSubmit is a background or hostile agent brief.
    #>
    param(
        [string]$Prompt,
        [string]$Payload
    )

    if (-not [string]::IsNullOrWhiteSpace($env:MCP_SUBAGENT_ID) -or
        -not [string]::IsNullOrWhiteSpace($env:GROK_SUBAGENT_ID)) {
        return $true
    }

    $text = [string]$Prompt
    if ([string]::IsNullOrWhiteSpace($text) -and -not [string]::IsNullOrWhiteSpace($Payload)) {
        if (Get-Command Get-HookPayloadValue -ErrorAction SilentlyContinue) {
            $text = Get-HookPayloadValue -Payload $Payload -Name 'prompt'
            foreach ($name in @('subagentId', 'subagent_id', 'agentType', 'agent_type')) {
                $v = Get-HookPayloadValue -Payload $Payload -Name $name
                if ($v -match 'subagent|background|hostile') { return $true }
            }
        }
    }

    if ($text -match 'You are the HOSTILE VALIDATOR') { return $true }
    if ($text -match 'ValidatorIdentity:\s*GrokSubagentHostile') { return $true }
    if ($text -match 'FIRST ACTION \(mandatory, before any validation\)') { return $true }
    return $false
}

function Resolve-PluginBackgroundScope {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (PLAN-SESSIONTRIAGEFIX-001 D3, triage-report-061f30e9): the background scope of the host this hook runs
        for, its key (Get-McpBackgroundScopeKey over the payload's session_id and the agentSessionId the root's session-state.yaml records)
        and its cache folder under the root cache; $null for the root host. FR-MCP-TRIAGEPLUGIN-001: the root turn stays the root's.
    #>
    param(
        [Parameter(Mandatory)][string]$CacheDir,
        [string]$Payload
    )

    $sessionFile = Join-Path $CacheDir 'session-state.yaml'
    $rootAgentSessionId = if (Test-Path -LiteralPath $sessionFile) { [string](Get-YamlScalar -Path $sessionFile -Key 'agentSessionId') } else { '' }
    $key = Get-McpBackgroundScopeKey -PayloadSessionId (Get-HookPayloadValue -Payload $Payload -Name 'session_id') -RootAgentSessionId $rootAgentSessionId
    if (-not $key) { return $null }
    return [pscustomobject]@{ Key = $key; Dir = (Join-McpBackgroundScopePath -CacheDir $CacheDir -Key $key) }
}

function Initialize-PluginBackgroundScope {
    <#
    .SYNOPSIS
        TR-MCP-PLUGIN-013 AC-005 (D3): prepares a background host's cache folder for a turn in the root's session. Its session-state.yaml
        becomes a copy of the root's (the same session, marker proof and title, so the turn lands in the root's session and never retitles
        it) plus the scope key. Returns the root's session id and the id of the turn the root's current-turn.yaml names, open or completed.
    #>
    param(
        [Parameter(Mandatory)][string]$RootCacheDir,
        [Parameter(Mandatory)]$Scope
    )

    [void][System.IO.Directory]::CreateDirectory($Scope.Dir)
    $rootState = Read-McpYamlObject -Path (Join-Path $RootCacheDir 'session-state.yaml')
    $scopeState = [ordered]@{}
    foreach ($name in @($rootState.Keys)) { $scopeState[$name] = $rootState[$name] }
    if (-not $scopeState.Contains('title') -or [string]::IsNullOrWhiteSpace([string]$scopeState['title'])) {
        $scopeState['title'] = "background scope of $([string]$rootState['sessionId'])"
    }
    $scopeState['backgroundScopeKey'] = [string]$Scope.Key
    Write-McpYamlObject -Path (Join-Path $Scope.Dir 'session-state.yaml') -Document $scopeState
    $rootTurnFile = Join-Path $RootCacheDir 'current-turn.yaml'
    $rootTurnId = if (Test-Path -LiteralPath $rootTurnFile) { [string](Get-YamlScalar -Path $rootTurnFile -Key 'turnRequestId') } else { '' }
    return [pscustomobject]@{ SessionId = [string]$rootState['sessionId']; RootTurnId = $rootTurnId }
}

function Open-PluginTurn {
    $startPath = Get-PluginStartPath -PreferredPath $WorkspacePath
    Set-PluginWorkspaceIdentity -ResolvedPath $startPath
    $cacheDir = Get-PluginCacheDir -StartPath $startPath

    # TR-MCP-PLUGIN-013 AC-003 (D4, triage-report-5c7278cd): a task notification or subagent hand-back is not an operator request. It
    # opens no turn and supersedes nothing, so the operator's open turn stays current; this runs before the marker refresh so an event
    # costs no server call. Read-HookInput is memoized, so the turn path below reads the same payload.
    $eventPrompt = Get-HookPayloadValue -Payload (Read-HookInput) -Name 'prompt'
    if (Test-PluginPromptIsHostEvent -Prompt $eventPrompt) {
        $currentTurnFile = Join-Path $cacheDir 'current-turn.yaml'
        $currentRequestId = if (Test-Path -LiteralPath $currentTurnFile) { [string](Get-YamlScalar -Path $currentTurnFile -Key 'turnRequestId') } else { '' }
        Write-PluginJson ([ordered]@{
            hookSpecificOutput = [ordered]@{
                hookEventName = 'UserPromptSubmit'
                status = 'system-event'
                turnRequestId = $currentRequestId
                additionalContext = if ($currentRequestId) {
                    "Host event (task notification or subagent hand-back): no session log turn was opened; turn $currentRequestId is unchanged."
                } else {
                    'Host event (task notification or subagent hand-back): no session log turn was opened.'
                }
            }
        })
        return
    }

    $sessionFile = Join-Path $cacheDir 'session-state.yaml'
    Ensure-PluginMarkerFresh -StartPath $startPath | Out-Null
    if (Test-Path -LiteralPath $sessionFile) {
        $timestampText = Get-YamlScalar -Path $sessionFile -Key 'timestamp'
        if (-not $timestampText) { $timestampText = Get-YamlScalar -Path $sessionFile -Key 'lastUpdated' }
        if ($timestampText) {
            $parsedTimestamp = [datetime]::MinValue
            if ([datetime]::TryParse($timestampText.Trim('"'), [ref]$parsedTimestamp) -and $parsedTimestamp.ToUniversalTime() -lt (Get-Date).ToUniversalTime().AddHours(-24)) {
                Set-YamlScalar -Path $sessionFile -Key 'lastUpdated' -Value $((Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'))
            }
        }
    }

    $openSessionState = if (Test-Path -LiteralPath $sessionFile) { Read-McpYamlObject -Path $sessionFile } else { $null }
    if (-not (Test-PluginSessionStateValid -State $openSessionState)) {
        $recovery = Invoke-PluginNoSessionRecovery -CacheDir $cacheDir -SessionFile $sessionFile -StartPath $startPath
        $openSessionState = if (Test-Path -LiteralPath $sessionFile) { Read-McpYamlObject -Path $sessionFile } else { $null }
        if (-not (Test-PluginSessionStateValid -State $openSessionState)) {
            Write-PluginJson ([ordered]@{
                hookSpecificOutput = [ordered]@{
                    hookEventName = 'UserPromptSubmit'
                    status = 'no-session'
                    recoveryStatus = [string]$recovery['status']
                    healthStatus = [string]$recovery['healthStatus']
                    failsafePath = [string]$recovery['failsafePath']
                    message = 'MCP session unavailable after health recovery. Continue user work with degraded failsafe session logging; retry session creation after AGENTS-README-FIRST.yaml marker timestamp changes.'
                }
            })
            return
        }
    }

    $payload = Read-HookInput
    $prompt = Get-HookPayloadValue -Payload $payload -Name 'prompt'
    if (-not $prompt) { $prompt = 'Continuation or hook-triggered turn.' }
    $title = (($prompt -split "`r?`n")[0]).Trim()
    if (-not $title) { $title = 'Continuation turn' }
    if ($title.Length -gt 60) { $title = $title.Substring(0, 60) }

    # TR-MCP-PLUGIN-013 AC-005 (PLAN-SESSIONTRIAGEFIX-001 D3, triage-report-061f30e9): a background or hostile-agent brief from a host whose
    # identity differs from the root's opens and logs its own turn in the root's session, kept in its own cache folder, so the root's
    # current-turn.yaml and turn are never reused, skipped, superseded or rewritten. A brief with the root's own identity is the root's prompt.
    $backgroundScope = $null
    $backgroundContext = $null
    if (Test-PluginPromptIsBackgroundAgent -Prompt $prompt -Payload $payload) {
        $backgroundScope = Resolve-PluginBackgroundScope -CacheDir $cacheDir -Payload $payload
    }
    if ($backgroundScope) {
        $backgroundContext = Initialize-PluginBackgroundScope -RootCacheDir $cacheDir -Scope $backgroundScope
        $cacheDir = $backgroundScope.Dir
        $sessionFile = Join-Path $cacheDir 'session-state.yaml'
    }

    # Double hook registration (plugin hooks.json plus the settings.json bridge) delivers the
    # same prompt twice per user message; reuse the turn the first delivery opened instead of
    # opening a duplicate session-log turn (triage-report-7c84e6437f7b42d0a67fbe32679a686a).
    $existingTurnFile = Join-Path $cacheDir 'current-turn.yaml'
    if (Test-Path -LiteralPath $existingTurnFile) {
        $openTurn = Read-McpYamlObject -Path $existingTurnFile
        if ($openTurn -is [System.Collections.IDictionary]) {
            $openStatus = if ($openTurn.Contains('status')) { [string]$openTurn['status'] } else { '' }
            $openQuery = if ($openTurn.Contains('queryText')) { [string]$openTurn['queryText'] } else { '' }
            $openRequestId = if ($openTurn.Contains('turnRequestId')) { [string]$openTurn['turnRequestId'] } else { '' }
            $openedAtText = if ($openTurn.Contains('openedAt')) { [string]$openTurn['openedAt'] } else { '' }
            $openedRecently = $false
            $openedAtValue = [datetime]::MinValue
            if ($openedAtText -and [datetime]::TryParse($openedAtText.Trim('"'), [ref]$openedAtValue)) {
                $openedRecently = ($openedAtValue.ToUniversalTime() -gt (Get-Date).ToUniversalTime().AddMinutes(-2))
            }
            if ($openStatus -eq 'in_progress' -and
                -not [string]::IsNullOrWhiteSpace($openRequestId) -and
                $openedRecently -and
                $openQuery.Trim() -eq $prompt.Trim()) {
                $dupDegraded = if ($openTurn.Contains('degraded')) { [string]$openTurn['degraded'] } else { '' }
                $dupFailsafe = if ($openTurn.Contains('failsafePath')) { [string]$openTurn['failsafePath'] } else { '' }
                $dupOut = [ordered]@{
                    hookEventName = 'UserPromptSubmit'
                    status = 'turn-already-open'
                    turnRequestId = $openRequestId
                    additionalContext = "session log turn $openRequestId is now active. Continue the current task after any incidental triage submission."
                }
                if ($dupDegraded -match '^(?i:true|1)$') {
                    $dupOut['degraded'] = $true
                    if (-not [string]::IsNullOrWhiteSpace($dupFailsafe)) {
                        $dupOut['recoveryArtifactPath'] = $dupFailsafe
                        $dupOut['additionalContext'] = "$($dupOut.additionalContext) Retained recovery artifact: $dupFailsafe"
                    }
                }
                Write-PluginJson ([ordered]@{
                    hookSpecificOutput = $dupOut
                })
                return
            }

        }
    }

    $markerSnapshot = $null
    try {
        $markerSnapshot = Get-MarkerFileSnapshot -StartDir $startPath
    } catch {
    }
    $sessionState = Read-McpYamlObject -Path $sessionFile -Create
    $sessionId = if ($sessionState.Contains('sessionId')) { [string]$sessionState['sessionId'] } else { '' }

    $turnRequestId = 'req-{0}-prompt-{1:x4}' -f (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ'), (Get-Random -Maximum 0xffff)
    $turnContext = Resolve-PluginTurnPlanContext
    $beginParams = [ordered]@{
        requestId = $turnRequestId
        queryTitle = $title
        queryText = $prompt
        planFile = $turnContext.planFile
        todoId = $turnContext.todoId
    }
    if ($backgroundContext) {
        # TR-MCP-PLUGIN-013 AC-005 (D3): the background turn's first write names its root session and root turn.
        $rootIds = @("root session: $($backgroundContext.SessionId)")
        if ($backgroundContext.RootTurnId) { $rootIds += "root turn: $($backgroundContext.RootTurnId)" }
        $beginParams['contextList'] = $rootIds
        $beginParams['tags'] = @('background-agent')
    }
    $paramsYaml = ConvertTo-PluginParamsYaml $beginParams
    $priorCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
    if ($backgroundScope) { $env:MCP_CACHE_DIR_OVERRIDE = $backgroundScope.Dir }
    try {
        $beginOutput = Invoke-PluginRepl -Method 'workflow.sessionlog.beginTurn' -ParamsYaml $paramsYaml
    } finally {
        if ($backgroundScope) { $env:MCP_CACHE_DIR_OVERRIDE = $priorCacheOverride }
    }
    if ($script:LastPluginReplExitCode -ne 0) {
        Write-PluginJson ([ordered]@{
            hookSpecificOutput = [ordered]@{
                hookEventName = 'UserPromptSubmit'
                status = 'turn-open-failed'
                turnRequestId = $turnRequestId
                reason = 'workflow.sessionlog.beginTurn failed'
                output = (($beginOutput | Out-String).Trim())
            }
        })
        return
    }

    $turnFile = Join-Path $cacheDir 'current-turn.yaml'
    if (-not (Test-Path -LiteralPath $turnFile)) {
        Write-PluginJson ([ordered]@{
            hookSpecificOutput = [ordered]@{
                hookEventName = 'UserPromptSubmit'
                status = 'turn-open-failed'
                turnRequestId = $turnRequestId
                reason = 'workflow.sessionlog.beginTurn did not create current-turn.yaml'
            }
        })
        return
    }
    $openedRequestId = Get-YamlScalar -Path $turnFile -Key 'turnRequestId'
    if ($openedRequestId -ne $turnRequestId) {
        Write-PluginJson ([ordered]@{
            hookSpecificOutput = [ordered]@{
                hookEventName = 'UserPromptSubmit'
                status = 'turn-open-failed'
                turnRequestId = $turnRequestId
                reason = "workflow.sessionlog.beginTurn opened '$openedRequestId' instead of '$turnRequestId'"
            }
        })
        return
    }

    $turnDoc = Read-McpYamlObject -Path $turnFile -Create
    $degradedFlag = if ($turnDoc.Contains('degraded')) { [string]$turnDoc['degraded'] } else { '' }
    $recoveryArtifactPath = if ($turnDoc.Contains('failsafePath')) { [string]$turnDoc['failsafePath'] } else { '' }
    $openStatus = if ($degradedFlag -eq 'true') { 'turn-opened-degraded' } else { 'turn-opened' }
    $additionalContext = if ($env:MCP_CODEX_INTERNAL_TODO -eq '1' -or $env:MCPSERVER_CODEX_INTERNAL_TODO -eq '1' -or $env:CODEX_MCP_TODO -eq '1') {
        "session log turn $turnRequestId is now active. MCP-backed internal TODO tracking is enabled. Continue the current task after any incidental triage submission."
    } else {
        "session log turn $turnRequestId is now active. Continue the current task after any incidental triage submission."
    }
    if ($openStatus -eq 'turn-opened-degraded' -and -not [string]::IsNullOrWhiteSpace($recoveryArtifactPath)) {
        $additionalContext = "$additionalContext Retained recovery artifact: $recoveryArtifactPath"
    }
    $hookOutput = [ordered]@{
        hookEventName = 'UserPromptSubmit'
        status = $openStatus
        turnRequestId = $turnRequestId
        additionalContext = $additionalContext
    }
    if ($backgroundScope) {
        $rootTurnText = if ($backgroundContext.RootTurnId) { "root turn $($backgroundContext.RootTurnId) is unchanged" } else { 'the root has no current turn' }
        $hookOutput['backgroundScope'] = [string]$backgroundScope.Key
        $hookOutput['additionalContext'] = "$additionalContext Background agent turn in the root session; $rootTurnText."
    }
    if ($openStatus -eq 'turn-opened-degraded') {
        $hookOutput['recoveryArtifactPath'] = [string]$recoveryArtifactPath
    }
    Write-PluginJson ([ordered]@{
        hookSpecificOutput = $hookOutput
    })
}

function Close-PluginTurnIfNeeded {
    $startPath = Get-PluginStartPath -PreferredPath $WorkspacePath
    Set-PluginWorkspaceIdentity -ResolvedPath $startPath
    $cacheDir = Get-PluginCacheDir -StartPath $startPath
    # TR-MCP-PLUGIN-013 AC-005 (PLAN-SESSIONTRIAGEFIX-001 D3): a background host's Stop completes its own turn, kept in its own cache
    # folder, and never the root's.
    $backgroundScope = Resolve-PluginBackgroundScope -CacheDir $cacheDir -Payload (Read-HookInput)
    if ($backgroundScope -and -not (Test-Path -LiteralPath (Join-Path $backgroundScope.Dir 'current-turn.yaml'))) { $backgroundScope = $null }
    if ($backgroundScope) { $cacheDir = $backgroundScope.Dir }
    $turnFile = Join-Path $cacheDir 'current-turn.yaml'
    if ($env:CLAUDE_STOP_HOOK_ACTIVE -eq 'true') {
        Write-PluginJson ([ordered]@{})
        return
    }

    $sessionFile = Join-Path $cacheDir 'session-state.yaml'
    $sessionAgeStale = $false
    if (Test-Path -LiteralPath $sessionFile) {
        $cutoff = (Get-Date).ToUniversalTime().AddHours(-24)
        $timestampText = Get-YamlScalar -Path $sessionFile -Key 'timestamp'
        $updatedText = Get-YamlScalar -Path $sessionFile -Key 'lastUpdated'
        $timestampStale = $false
        $updatedStale = $true
        $parsedTimestamp = [datetime]::MinValue
        if ($timestampText -and [datetime]::TryParse($timestampText.Trim('"'), [ref]$parsedTimestamp)) {
            $timestampStale = $parsedTimestamp.ToUniversalTime() -lt $cutoff
        }
        $parsedUpdated = [datetime]::MinValue
        if ($updatedText -and [datetime]::TryParse($updatedText.Trim('"'), [ref]$parsedUpdated)) {
            $updatedStale = $parsedUpdated.ToUniversalTime() -lt $cutoff
        }
        # A fresh lastUpdated does not clear a stale timestamp pin.
        if ($timestampText) {
            $sessionAgeStale = $timestampStale
        } else {
            $sessionAgeStale = $updatedStale
        }
    }

    if (-not (Test-Path -LiteralPath $turnFile)) {
        if ($sessionAgeStale -and (Test-Path -LiteralPath $sessionFile)) {
            Set-YamlScalar -Path $sessionFile -Key 'lastUpdated' -Value ((Get-Date).ToUniversalTime().ToString('o'))
        }
        Write-PluginJson ([ordered]@{})
        return
    }

    $status = Get-YamlScalar -Path $turnFile -Key 'status'
    if ($sessionAgeStale -and $status -eq 'in_progress') {
        $requestId = Get-YamlScalar -Path $turnFile -Key 'turnRequestId'
        Write-PluginJson ([ordered]@{ decision = 'block'; reason = "stale cached session cannot be reused for $requestId" })
        return
    }
    if ($sessionAgeStale -and (Test-Path -LiteralPath $sessionFile)) {
        Set-YamlScalar -Path $sessionFile -Key 'lastUpdated' -Value ((Get-Date).ToUniversalTime().ToString('o'))
    }

    $status = Get-YamlScalar -Path $turnFile -Key 'status'
    if ($status -eq 'in_progress') {
        if ($env:MCP_STOP_GATE_COMPLETE_TIMEOUT_SECONDS -and $env:MCP_STOP_GATE_FORCE_TIMEOUT -eq '1') {
            Write-PluginJson ([ordered]@{ decision = 'block'; reason = "in_progress turn could not be auto-closed within $($env:MCP_STOP_GATE_COMPLETE_TIMEOUT_SECONDS)s" })
            return
        }

        # TR-MCP-PLUGIN-011: no-op (do not block) for phantom/empty turns - system-event prompts
        # (<task-notification>, <user_query>, <command-*>, <local-command-*>) or turns with no
        # recorded work at all. There is nothing to complete under them and the server does not gate
        # standard agents, so blocking is pure Stop-hook noise (BUG-TRIAGE-082/083).
        $stopEdits = [int]((Get-YamlScalar -Path $turnFile -Key 'codeEdits') ?? '0')
        $stopAuditTotal = [int]((Get-YamlScalar -Path $turnFile -Key 'auditActions') ?? '0') +
                          [int]((Get-YamlScalar -Path $turnFile -Key 'auditFiles') ?? '0') +
                          [int]((Get-YamlScalar -Path $turnFile -Key 'auditDialog') ?? '0') +
                          [int]((Get-YamlScalar -Path $turnFile -Key 'auditDecisions') ?? '0') +
                          [int]((Get-YamlScalar -Path $turnFile -Key 'auditCommits') ?? '0')
        $stopTitle = (Get-YamlScalar -Path $turnFile -Key 'queryTitle') ?? ''
        $stopText = (Get-YamlScalar -Path $turnFile -Key 'queryText') ?? ''
        $stopFirstLine = @($stopTitle, $stopText) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
        $stopIsSystemEvent = $stopFirstLine -match '^\s*<(task-notification|user_query|command-name|command-message|command-args|local-command-[a-z-]+)>'
        # A background agent's turn is a real request (D3), so its Stop closes it even when no work was recorded in it.
        if (-not $backgroundScope -and ($stopIsSystemEvent -or ($stopEdits -le 0 -and $stopAuditTotal -le 0))) {
            Write-PluginJson ([ordered]@{})
            return
        }

        $response = 'Auto-closed by PowerShell stop gate.'
        $paramsYaml = ConvertTo-PluginParamsYaml ([ordered]@{
            response = $response
        })
        $priorCacheOverride = $env:MCP_CACHE_DIR_OVERRIDE
        if ($backgroundScope) { $env:MCP_CACHE_DIR_OVERRIDE = $backgroundScope.Dir }
        try {
            $completeOutput = Invoke-PluginRepl -Method 'workflow.sessionlog.completeTurn' -ParamsYaml $paramsYaml
        } finally {
            if ($backgroundScope) { $env:MCP_CACHE_DIR_OVERRIDE = $priorCacheOverride }
        }
        if ($script:LastPluginReplExitCode -ne 0) {
            Write-PluginJson ([ordered]@{ decision = 'block'; reason = 'workflow.sessionlog.completeTurn failed; current turn remains in_progress'; output = (($completeOutput | Out-String).Trim()) })
            return
        }
        $status = Get-YamlScalar -Path $turnFile -Key 'status'
        if ($status -ne 'completed') {
            Write-PluginJson ([ordered]@{ decision = 'block'; reason = 'workflow.sessionlog.completeTurn did not mark the current turn completed' })
            return
        }
    }

    $edits = [int]((Get-YamlScalar -Path $turnFile -Key 'codeEdits') ?? '0')
    $buildStatus = Get-YamlScalar -Path $turnFile -Key 'lastBuildStatus'
    if ($edits -gt 0 -and $buildStatus -eq 'failed') {
        $acceptFailure = Join-Path $cacheDir 'turn-accept-failure.marker'
        if (Test-Path -LiteralPath $acceptFailure) {
            Remove-Item -LiteralPath $acceptFailure -Force
            Write-PluginJson ([ordered]@{})
            return
        }

        Write-PluginJson ([ordered]@{ decision = 'block'; reason = "Last build in this turn failed after $edits code edit(s)." })
        return
    }

    $auditActions = Get-YamlScalar -Path $turnFile -Key 'auditActions'
    $auditFiles = Get-YamlScalar -Path $turnFile -Key 'auditFiles'
    $auditDialog = Get-YamlScalar -Path $turnFile -Key 'auditDialog'
    $auditDecisions = Get-YamlScalar -Path $turnFile -Key 'auditDecisions'
    # TR-MCP-PLUGIN-011: auditCommits is a real audit signal and must count toward completeness so a
    # completed code-edit turn recorded only via commits is not falsely blocked (BUG-TRIAGE-071).
    $auditCommits = Get-YamlScalar -Path $turnFile -Key 'auditCommits'
    $hasAuditSchema = $null -ne $auditActions -or $null -ne $auditFiles -or $null -ne $auditDialog -or $null -ne $auditDecisions -or $null -ne $auditCommits
    if ($edits -gt 0 -and $hasAuditSchema) {
        $auditTotal = [int](($auditActions ?? '0')) + [int](($auditFiles ?? '0')) + [int](($auditDialog ?? '0')) + [int](($auditDecisions ?? '0')) + [int](($auditCommits ?? '0'))
        if ($auditTotal -le 0) {
            $acceptAudit = Join-Path $cacheDir 'turn-accept-incomplete-audit.marker'
            if (Test-Path -LiteralPath $acceptAudit) {
                Remove-Item -LiteralPath $acceptAudit -Force
                Write-PluginJson ([ordered]@{})
                return
            }

            Write-PluginJson ([ordered]@{ decision = 'block'; reason = 'audit is incomplete for a completed code-edit turn' })
            return
        }
    }

    Write-PluginJson ([ordered]@{})
}

function Test-PluginDiskFullException {
    <#
    .SYNOPSIS
        TR-MCP-VERIFYWRAP-001: true when the exception is a disk-capacity IOException.
    #>
    param($Exception)

    $ex = $Exception
    while ($null -ne $ex) {
        if ($ex -is [System.IO.IOException]) {
            $message = [string]$ex.Message
            if ($message -match 'not enough space|disk full|There is not enough space on the disk') {
                return $true
            }
            # ERROR_DISK_FULL (0x70) as HRESULT 0x80070070.
            if ($ex.HResult -eq -2147024784) {
                return $true
            }
        }
        $ex = $ex.InnerException
    }

    return $false
}

function Invoke-PluginCodeVerifyHandleDiskFull {
    <#
    .SYNOPSIS
        TR-MCP-VERIFYWRAP-001: typed disk_full that does not mutate current-turn audit.
    #>
    param(
        [string]$TurnFile = '',
        $Exception
    )

    if (-not (Test-PluginDiskFullException -Exception $Exception)) {
        throw $Exception
    }

    return (Get-PluginCodeVerifyFailureStatus -Exception $Exception)
}

function Get-PluginCodeVerifyFailureStatus {
    <#
    .SYNOPSIS
        Maps a code-verify exception to a typed JSON status object.
    #>
    param($Exception)

    if (Test-PluginDiskFullException -Exception $Exception) {
        return [ordered]@{
            status = 'failed'
            code = 'disk_full'
            message = [string]$Exception.Message
        }
    }

    return [ordered]@{
        status = 'failed'
        code = 'io_error'
        message = [string]$Exception.Message
    }
}

function Get-PluginCodeVerifyTimeoutSeconds {
    <#
    .SYNOPSIS
        Documented code-verify timeout. MCP_CODE_VERIFY_TIMEOUT_SECONDS or 60.
    #>
    $parsed = 60
    if ($env:MCP_CODE_VERIFY_TIMEOUT_SECONDS) {
        $value = 0
        if ([int]::TryParse([string]$env:MCP_CODE_VERIFY_TIMEOUT_SECONDS, [ref]$value) -and $value -gt 0) {
            $parsed = $value
        }
    }
    return $parsed
}

function Invoke-PluginBoundedProcess {
    <#
    .SYNOPSIS
        Starts a child process and kills the process tree when the timeout elapses.
    #>
    param(
        [Parameter(Mandatory)][string]$FileName,
        [string[]]$Arguments = @(),
        [int]$TimeoutSeconds = 60,
        [string]$WorkingDirectory = ''
    )

    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = $FileName
    foreach ($argument in $Arguments) {
        $psi.ArgumentList.Add($argument)
    }
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        $psi.WorkingDirectory = $WorkingDirectory
    }

    $process = [System.Diagnostics.Process]::Start($psi)
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $timedOut = $false
    $waitMs = [Math]::Max(1, $TimeoutSeconds) * 1000
    if (-not $process.WaitForExit($waitMs)) {
        $timedOut = $true
        try { $process.Kill($true) } catch { }
        try { [void]$process.WaitForExit(2000) } catch { }
    }

    $output = ''
    try { $output = [string]$stdoutTask.Result } catch { }
    try { $output += [string]$stderrTask.Result } catch { }

    return [pscustomobject]@{
        exitCode = $(if ($timedOut) { -1 } else { $process.ExitCode })
        output = $output
        timedOut = $timedOut
    }
}

function Invoke-CodeVerify {
    $startPath = Get-PluginStartPath -PreferredPath $WorkspacePath
    Set-PluginWorkspaceIdentity -ResolvedPath $startPath
    $cacheDir = $null
    try {
        $cacheDir = Get-PluginCacheDir -StartPath $startPath
    } catch {
        Write-PluginJson ([ordered]@{ status = 'skipped'; reason = 'no-cache' })
        return
    }
    $turnFile = Join-Path $cacheDir 'current-turn.yaml'
    $payload = Read-HookInput
    $filePath = Get-HookPayloadValue -Payload $payload -Name 'file_path'
    if (-not $filePath) {
        try {
            $json = $payload | ConvertFrom-Json -Depth 20 -ErrorAction Stop
            $filePath = [string]$json.tool_input.file_path
        } catch {
        }
    }

    if (-not $filePath -or -not (Test-Path -LiteralPath $filePath)) {
        Write-PluginJson ([ordered]@{ status = 'skipped'; reason = 'no-file' })
        return
    }

    $dir = Split-Path -Parent (Resolve-Path -LiteralPath $filePath).ProviderPath
    $project = $null
    while ($dir) {
        $project = Get-ChildItem -LiteralPath $dir -Filter '*.csproj' -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($project) { break }
        $parent = Split-Path -Parent $dir
        if ($parent -eq $dir) { break }
        $dir = $parent
    }

    $buildStatus = 'succeeded'
    try {
        if ($project) {
            $buildLog = Join-Path $cacheDir 'last-build.log'
            $bounded = Invoke-PluginBoundedProcess `
                -FileName 'dotnet' `
                -Arguments @('build', $project.FullName, '--nologo', '-clp:NoSummary') `
                -TimeoutSeconds (Get-PluginCodeVerifyTimeoutSeconds) `
                -WorkingDirectory $project.DirectoryName
            $output = $bounded.output
            try {
                [System.IO.File]::WriteAllText($buildLog, $output)
            } catch {
                if (Test-PluginDiskFullException -Exception $_.Exception) {
                    Write-PluginJson (Invoke-PluginCodeVerifyHandleDiskFull -TurnFile $turnFile -Exception $_.Exception)
                    return
                }
                throw
            }
            if ($bounded.timedOut) {
                $buildStatus = 'failed'
            } elseif ($bounded.exitCode -ne 0) {
                $buildStatus = 'failed'
            }
        }

        if (Test-Path -LiteralPath $turnFile) {
            Set-YamlScalar -Path $turnFile -Key 'lastBuildStatus' -Value $buildStatus
        }

        Write-PluginJson ([ordered]@{ status = $buildStatus })
    } catch {
        if (Test-PluginDiskFullException -Exception $_.Exception) {
            Write-PluginJson (Invoke-PluginCodeVerifyHandleDiskFull -TurnFile $turnFile -Exception $_.Exception)
            return
        }
        throw
    }
}

function Invoke-CacheFlushHook {
    $startPath = Get-PluginStartPath -PreferredPath $WorkspacePath
    $identified = $false
    try {
        if (-not [string]::IsNullOrWhiteSpace($startPath)) {
            Set-PluginWorkspaceIdentity -ResolvedPath $startPath
            $null = Resolve-McpCacheDir -StartPath $startPath
            $identified = $true
        }
    } catch {
        $identified = $false
    }

    if (-not $identified) {
        Write-PluginJson ([ordered]@{})
        return
    }

    try {
        $result = & (Join-Path $script:ScriptDir 'cache-manager.ps1') -Action flush
        $text = [string]($result | Out-String)
        $flushed = 0
        $failed = 0
        $pending = 0
        if ($text -match 'flushed=(\d+)') { $flushed = [int]$Matches[1] }
        if ($text -match 'failed=(\d+)') { $failed = [int]$Matches[1] }
        if ($text -match 'pending=(\d+)') { $pending = [int]$Matches[1] }
        if ($failed -gt 0) {
            Write-PluginJson ([ordered]@{
                status = 'flush-failed'
                flushed = $flushed
                failed = $failed
                pending = $pending
            })
            if ($HookName -eq 'session-end' -or $HookName -eq 'pre-compact') {
                exit 1
            }
            return
        }
        if ($HookName -eq 'cache-flush') {
            Write-Output $result
        } else {
            Write-PluginJson ([ordered]@{})
        }
    } catch {
        if ($HookName -eq 'session-end' -or $HookName -eq 'pre-compact') {
            Write-PluginJson ([ordered]@{
                status = 'flush-failed'
                message = [string]$_.Exception.Message
            })
            exit 1
        }
        throw
    }
}

function Get-PlanFilePathFromInput {
    $payload = Read-HookInput
    $filePath = Get-HookPayloadValue -Payload $payload -Name 'file_path'
    if (-not $filePath) {
        try {
            $json = $payload | ConvertFrom-Json -Depth 20 -ErrorAction Stop
            $filePath = [string]$json.tool_input.file_path
            if (-not $filePath) { $filePath = [string]$json.tool_input.path }
        } catch {
        }
    }
    if (-not $filePath -and $env:TOOL_INPUT) { $filePath = $env:TOOL_INPUT }
    return $filePath
}

function Write-PostToolUseOutput {
    param([string]$Status = 'skipped')

    Write-PluginJson ([ordered]@{
        hookSpecificOutput = [ordered]@{
            hookEventName = 'PostToolUse'
            status = $Status
        }
    })
}

function Get-PlanTitle {
    param([Parameter(Mandatory)][string]$PlanFile)

    if (-not (Test-Path -LiteralPath $PlanFile -PathType Leaf)) { return $null }
    foreach ($line in [System.IO.File]::ReadLines($PlanFile)) {
        if ($line -match '^#\s+(.+)$') {
            return $Matches[1].Trim()
        }
    }

    return [System.IO.Path]::GetFileNameWithoutExtension($PlanFile)
}

function Get-PlanTodoMapPath {
    Join-Path (Get-PluginCacheDir) 'plan-todo-map.yaml'
}

function Get-LastPlanTodoMapEntry {
    return (Get-LastPlanTodoMapEntryFromFile -MapPath (Get-PlanTodoMapPath))
}

function Resolve-PluginTurnPlanContext {
    $planFile = Get-PlanFilePathFromInput
    if (-not $planFile -or -not (Test-Path -LiteralPath $planFile -PathType Leaf)) {
        $mapped = Get-LastPlanTodoMapEntry
        if ($mapped -and $mapped.planFile -and (Test-Path -LiteralPath $mapped.planFile -PathType Leaf)) {
            $planFile = [string]$mapped.planFile
        } else {
            $planFile = $null
        }
    }

    if (-not $planFile) {
        return [ordered]@{ planFile = 'None'; todoId = 'None' }
    }

    $todoId = Find-PlanTodoId -PlanFile $planFile
    if ([string]::IsNullOrWhiteSpace($todoId)) { $todoId = 'None' }
    return [ordered]@{ planFile = $planFile; todoId = $todoId }
}

function Get-TodoIdFromReplOutput {
    param([string[]]$Output)

    $text = ($Output -join "`n")
    if ($text -match '(?m)^\s*id:\s*(?<id>\S+)') { return $Matches['id'] }
    if ($text -match '(?m)^\s*todoId:\s*(?<id>\S+)') { return $Matches['id'] }
    return $null
}

function New-PlanTodoId {
    param([Parameter(Mandatory)][string]$Title)

    $slug = ($Title.ToUpperInvariant() -replace '[^A-Z0-9]', '')
    if (-not $slug) { $slug = 'PLAN' }
    if ($slug.Length -gt 40) { $slug = $slug.Substring(0, 40) }
    return "PLAN-$slug-001"
}

function Invoke-PlanApprovedHook {
    $planFile = Get-PlanFilePathFromInput
    if (-not $planFile -or -not (Test-Path -LiteralPath $planFile -PathType Leaf)) {
        Write-PostToolUseOutput -Status 'skipped'
        return
    }

    $title = Get-PlanTitle -PlanFile $planFile
    # client.Todo.CreateAsync(TodoCreateRequest request): the server requires a
    # non-empty params.id, so derive a canonical PLAN-<slug>-001 id from the title.
    $paramsYaml = ConvertTo-PluginParamsYaml -Params ([ordered]@{
        request = [ordered]@{
            id          = New-PlanTodoId -Title $title
            title       = $title
            section     = 'Planning'
            priority    = 'medium'
            description = @("Plan approved from $planFile")
        }
    })
    $output = @(Invoke-PluginRepl -Method 'client.Todo.CreateAsync' -ParamsYaml $paramsYaml)
    $todoId = Get-TodoIdFromReplOutput -Output $output
    if ($todoId) {
        [void](Write-PlanTodoMapEntry -MapPath (Get-PlanTodoMapPath) -PlanFile $planFile -TodoId $todoId)
    }

    Write-PostToolUseOutput -Status 'created'
}

function Find-PlanTodoId {
    param([Parameter(Mandatory)][string]$PlanFile)

    return (Find-PlanTodoIdInMap -MapPath (Get-PlanTodoMapPath) -PlanFile $PlanFile)
}

function Invoke-PlanModifiedHook {
    $planFile = Get-PlanFilePathFromInput
    if (-not $planFile) {
        Write-PostToolUseOutput -Status 'skipped'
        return
    }

    $todoId = Find-PlanTodoId -PlanFile $planFile
    if (-not $todoId) {
        Write-PostToolUseOutput -Status 'skipped'
        return
    }

    # client.Todo.UpdateAsync(string id, TodoUpdateRequest request)
    $paramsYaml = ConvertTo-PluginParamsYaml -Params ([ordered]@{
        id      = $todoId
        request = [ordered]@{ doneSummary = "Plan modified: $planFile" }
    })
    Invoke-PluginRepl -Method 'client.Todo.UpdateAsync' -ParamsYaml $paramsYaml | Out-Null
    Write-PostToolUseOutput -Status 'updated'
}

$pluginTempTarget = Get-PluginStartPath -PreferredPath $WorkspacePath
if (-not [string]::IsNullOrWhiteSpace($pluginTempTarget)) {
    $pluginTempAlign = Set-McpPluginSameVolumeTemp -TargetPath $pluginTempTarget
    if ($pluginTempAlign -and -not $pluginTempAlign.Succeeded -and $pluginTempAlign.Error) {
        [Console]::Error.WriteLine([string]$pluginTempAlign.Error)
    }
}

if (-not (Confirm-PowerShellMcpRuntime)) {
    Stop-PluginUnavailable
}

switch ($HookName) {
    'session-start' { Start-PluginSession -StartPath $WorkspacePath }
    'session-end' { Invoke-CacheFlushHook }
    'pre-compact' { Invoke-CacheFlushHook }
    # TR-MCP-PLUGIN-012 AC-003 (D2, triage-report-d9bd6fc3): PostCompact starts no session. The SessionStart hook that follows a
    # compaction (source compact) starts the one session; a second start here minted an empty twin session at every compaction.
    'post-compact' { Write-PluginJson ([ordered]@{}) }
    'user-prompt-submit' { Open-PluginTurn }
    'stop-gate' { Close-PluginTurnIfNeeded }
    'code-verify' { Invoke-CodeVerify }
    'plan-approved' { Invoke-PlanApprovedHook }
    'plan-modified' { Invoke-PlanModifiedHook }
    'cache-flush' { Invoke-CacheFlushHook }
    'health-check' { Invoke-PluginRepl -Method 'client.Health.GetAsync' }
    'subagent-import' { Write-PluginJson ([ordered]@{ status = 'skipped' }) }
}
