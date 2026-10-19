#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Status', 'Invoke', 'CompleteTurn')]
    [string]$Command = 'Status',

    [string]$Method,

    [string]$Params,

    [string]$ParamsPath,

    [object]$ParamsObject,

    [string]$Response,

    [string]$ResponsePath,

    [string]$WorkspacePath,

    [string]$PluginRoot = $(if ($env:MCP_PLUGIN_ROOT) { $env:MCP_PLUGIN_ROOT } elseif ($env:CLAUDE_PLUGIN_ROOT) { $env:CLAUDE_PLUGIN_ROOT } else { Split-Path -Parent $PSScriptRoot }),

    [string]$CacheRoot,

    [int]$TimeoutSeconds = $(if ($env:MCP_PLUGIN_TIMEOUT_SECONDS) { [int]$env:MCP_PLUGIN_TIMEOUT_SECONDS } else { 90 })
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$shimModule = Join-Path $PSScriptRoot 'McpPluginShim.psm1'
Import-Module $shimModule -ErrorAction Stop
. (Join-Path $PSScriptRoot 'yaml-object-mutation.ps1')

$script:McpPluginParamsObjectBound = $PSBoundParameters.ContainsKey('ParamsObject')
if ($script:McpPluginParamsObjectBound -and ($PSBoundParameters.ContainsKey('Params') -or $PSBoundParameters.ContainsKey('ParamsPath'))) {
    throw '-ParamsObject cannot be combined with -Params or -ParamsPath.'
}

$script:McpPluginParamsFromObject = ''
if ($script:McpPluginParamsObjectBound) {
    Import-McpYamlSerializer
    $script:McpPluginParamsFromObject = ConvertTo-Yaml -Data $ParamsObject -Options WithIndentedSequences
}

function Resolve-McpPluginDefaultWorkspacePath {
    $current = (Get-Location).ProviderPath
    if ($current) {
        $dir = $current
        for ($i = 0; $i -lt 20 -and $dir; $i++) {
            $marker = Join-Path $dir 'AGENTS-README-FIRST.yaml'
            if (Test-Path -LiteralPath $marker -PathType Leaf) {
                return (Resolve-Path -LiteralPath $dir).ProviderPath
            }

            $parent = Split-Path -Parent $dir
            if ($parent -eq $dir) { break }
            $dir = $parent
        }
    }

    $configured = @(
        $env:MCP_WORKSPACE_PATH,
        $env:MCPSERVER_WORKSPACE_PATH,
        $env:CODEX_WORKSPACE_PATH,
        $env:CODEX_PROJECT_DIR,
        $env:CLAUDE_PROJECT_DIR
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path -LiteralPath $_ -PathType Container) } | Select-Object -First 1

    if ($configured) { return (Resolve-Path -LiteralPath $configured).ProviderPath }
    return $current
}

$script:McpPluginWorkspacePathExplicit = $PSBoundParameters.ContainsKey('WorkspacePath')
if (-not $script:McpPluginWorkspacePathExplicit -or [string]::IsNullOrWhiteSpace($WorkspacePath)) {
    $WorkspacePath = Resolve-McpPluginDefaultWorkspacePath
}

$script:McpPluginInvocation = New-McpPluginInvocationOptions `
    -Command $Command `
    -Method ($Method ?? '') `
    -Params $(if ($script:McpPluginParamsObjectBound) { $script:McpPluginParamsFromObject } else { $Params ?? '' }) `
    -ParamsPath ($ParamsPath ?? '') `
    -ParamsObject $(if ($script:McpPluginParamsObjectBound) { $ParamsObject } else { $null }) `
    -Response ($Response ?? '') `
    -ResponsePath ($ResponsePath ?? '') `
    -WorkspacePath $WorkspacePath `
    -PluginRoot $PluginRoot `
    -CacheRoot ($CacheRoot ?? '') `
    -TimeoutSeconds $TimeoutSeconds

function Resolve-FullPath {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction Stop
    return $resolved.ProviderPath
}

function Resolve-OptionalDirectory {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        [void][System.IO.Directory]::CreateDirectory($Path)
    }

    return (Resolve-FullPath $Path)
}

function Read-RedirectedInput {
    if ([Console]::IsInputRedirected) {
        return [Console]::In.ReadToEnd()
    }

    return ''
}

function Read-OptionalText {
    param(
        [string]$Inline,
        [bool]$HasInline,
        [string]$Path,
        [switch]$AllowRedirectedInput
    )

    if ($Path) {
        return [System.IO.File]::ReadAllText((Resolve-FullPath $Path))
    }

    if ($HasInline) {
        return $Inline
    }

    if ($AllowRedirectedInput) {
        return (Read-RedirectedInput)
    }

    return ''
}

function Resolve-PluginPowerShellScript {
    param(
        [Parameter(Mandatory)][string]$Root,
        [Parameter(Mandatory)][string]$Name
    )

    foreach ($libName in @('lib', 'lib-ps')) {
        $candidate = Join-Path (Join-Path $Root $libName) $Name
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Unable to find plugin PowerShell script '$Name' under '$Root'."
}

function Resolve-McpPluginHostName {
    param([Parameter(Mandatory)][string]$Root)

    $normalizedRoot = $Root.Replace('\\', '/').ToLowerInvariant()
    switch -Regex ($normalizedRoot) {
        'mcpserver-claude-cowork-plugin|claude-cowork' { return 'claude-cowork' }
        'mcpserver-claude-code-plugin|claude-code' { return 'claude-code' }
        'mcpserver-codex-plugin|/codex(?:/|$)' { return 'codex' }
        'mcpserver-copilot-plugin|/copilot(?:/|$)' { return 'copilot' }
        'mcpserver-grok-plugin|/grok(?:/|$)' { return 'grok' }
        'mcpserver-cline-v2-plugin|cline-v2' { return 'cline-v2' }
        'mcpserver-cline-plugin|/cline(?:/|$)' { return 'cline' }
        'mcpserver-opencode-plugin|open-code|opencode' { return 'opencode' }
    }

    return ''
}

function Get-McpPluginHostDefaults {
    param([Parameter(Mandatory)][string]$HostName)

    switch ($HostName.Trim().ToLowerInvariant()) {
        { $_ -in 'claude', 'claude-code' } {
            return [ordered]@{ Host = 'claude-code'; Agent = 'ClaudeCode'; Model = 'claude'; Tag = 'claude-code' }
        }
        { $_ -in 'cowork', 'claude-cowork' } {
            return [ordered]@{ Host = 'claude-cowork'; Agent = 'ClaudeCowork'; Model = 'claude'; Tag = 'claude-cowork' }
        }
        'codex' {
            return [ordered]@{ Host = 'codex'; Agent = 'Codex'; Model = 'codex'; Tag = 'codex' }
        }
        'copilot' {
            return [ordered]@{ Host = 'copilot'; Agent = 'Copilot'; Model = 'copilot'; Tag = 'copilot' }
        }
        'grok' {
            return [ordered]@{ Host = 'grok'; Agent = 'GrokCode'; Model = 'grok'; Tag = 'grok' }
        }
        'cline' {
            return [ordered]@{ Host = 'cline'; Agent = 'Cline'; Model = 'cline'; Tag = 'cline' }
        }
        'cline-v2' {
            return [ordered]@{ Host = 'cline-v2'; Agent = 'Cline'; Model = 'cline'; Tag = 'cline-v2' }
        }
        { $_ -in 'opencode', 'open-code' } {
            return [ordered]@{ Host = 'opencode'; Agent = 'OpenCode'; Model = 'opencode'; Tag = 'opencode' }
        }
    }

    return $null
}

function Invoke-PluginPowerShellScript {
    param(
        [Parameter(Mandatory)][string]$ScriptPath,
        [string[]]$Arguments = @(),
        [string]$StandardInput = ''
    )

    $pluginRootFull = Resolve-FullPath $script:McpPluginInvocation.PluginRoot
    $workspaceFull = Resolve-OptionalDirectory $script:McpPluginInvocation.WorkspacePath
    $cacheOverrideFull = if ($script:McpPluginInvocation.CacheRoot) {
        Resolve-OptionalDirectory $script:McpPluginInvocation.CacheRoot
    } elseif (-not $script:McpPluginWorkspacePathExplicit -and $env:MCP_CACHE_DIR_OVERRIDE) {
        Resolve-OptionalDirectory $env:MCP_CACHE_DIR_OVERRIDE
    } else {
        $null
    }
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = (Get-Command pwsh -ErrorAction Stop).Source
    $startInfo.WorkingDirectory = $workspaceFull
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.ArgumentList.Add('-NoLogo')
    $startInfo.ArgumentList.Add('-NoProfile')
    $startInfo.ArgumentList.Add('-NonInteractive')
    $startInfo.ArgumentList.Add('-File')
    $startInfo.ArgumentList.Add((Resolve-FullPath $ScriptPath))
    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }
    $startInfo.Environment['MCP_PLUGIN_ROOT'] = $pluginRootFull
    $startInfo.Environment['CLAUDE_PLUGIN_ROOT'] = $pluginRootFull
    $resolvedHost = Resolve-McpPluginHostName -Root $pluginRootFull
    $hostDefaults = if ($resolvedHost) { Get-McpPluginHostDefaults -HostName $resolvedHost } else { $null }
    if ($hostDefaults) {
        $startInfo.Environment['MCP_PLUGIN_HOST'] = [string]$hostDefaults.Host
        $startInfo.Environment['PLUGIN_AGENT_DEFAULT'] = [string]$hostDefaults.Agent
        $startInfo.Environment['PLUGIN_MODEL_DEFAULT'] = [string]$hostDefaults.Model
        $startInfo.Environment['PLUGIN_TAG'] = [string]$hostDefaults.Tag
        $startInfo.Environment['MCP_AGENT_NAME'] = [string]$hostDefaults.Agent
        $startInfo.Environment['MCP_AGENT_ID'] = [string]$hostDefaults.Agent
        $startInfo.Environment['MCP_SESSION_AGENT'] = [string]$hostDefaults.Agent
        $startInfo.Environment['MCP_SESSION_MODEL'] = [string]$hostDefaults.Model
        $startInfo.Environment['MCP_SESSION_TITLE'] = "$($hostDefaults.Agent) plugin session"
        $startInfo.Environment['CT2R_SOURCE_TYPE'] = [string]$hostDefaults.Agent
        $startInfo.Environment['CT2R_MODEL'] = [string]$hostDefaults.Model
        $startInfo.Environment['CT2R_TITLE'] = "$($hostDefaults.Agent) turn"
        $startInfo.Environment['CT2R_TAGS'] = [string]$hostDefaults.Tag
        if ($hostDefaults.Host -ne 'codex') {
            foreach ($codexName in @('CODEX_PLUGIN_ROOT', 'CODEX_WORKSPACE_PATH', 'CODEX_PROJECT_DIR', 'CODEX_CWD')) {
                [void]$startInfo.Environment.Remove($codexName)
            }
        }
    }
    [void]$startInfo.Environment.Remove('MCP_CACHE_DIR_OVERRIDE')
    [void]$startInfo.Environment.Remove('PLUGIN_ROOT_OVERRIDE')
    if ($cacheOverrideFull) {
        $startInfo.Environment['MCP_CACHE_DIR_OVERRIDE'] = $cacheOverrideFull
    }
    $startInfo.Environment['MCP_WORKSPACE_PATH'] = $workspaceFull
    $startInfo.Environment['MCPSERVER_WORKSPACE_PATH'] = $workspaceFull
    $startInfo.Environment['MCP_WORKSPACE_START_DIR'] = $workspaceFull
    $startInfo.Environment['CLAUDE_PROJECT_DIR'] = $workspaceFull

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    [void]$process.Start()

    if ($StandardInput.Length -gt 0) {
        $process.StandardInput.Write($StandardInput)
    }
    $process.StandardInput.Close()

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $boundedTimeout = [Math]::Max(1, $script:McpPluginInvocation.TimeoutSeconds)
    if (-not $process.WaitForExit($boundedTimeout * 1000)) {
        try {
            $process.Kill($true)
        } catch {
        }
        try { [void]$process.WaitForExit(2000) } catch { }
        # TR-MCP-TRIAGEPLUGIN-004 / BUG-TRIAGE-120: never throw an unclassified
        # timeout. Session-log callers treat command_timeout as retryable
        # degraded/queued and keep failsafe on disk. The envelope is a native
        # object serialized through the YAML serializer, never handwritten text.
        $envelope = (ConvertTo-McpPluginYaml -InputObject (New-McpPluginTimeoutEnvelope -TimeoutSeconds $boundedTimeout)).TrimEnd("`r", "`n")
        Write-Output $envelope
        [Console]::Error.WriteLine($envelope)
        return
    }

    $stdout = $stdoutTask.Result
    $stderr = $stderrTask.Result

    if ($stderr.Length -gt 0) {
        [Console]::Error.Write($stderr)
    }

    if ($stdout.Length -gt 0) {
        Write-Output ($stdout.TrimEnd("`r", "`n"))
    }

    if ($process.ExitCode -ne 0) {
        $msg = "Plugin command failed with exit code $($process.ExitCode)."
        if ($stdout) { $msg += "`n" + $stdout }
        throw $msg
    }
}

$pluginRootFull = Resolve-FullPath $script:McpPluginInvocation.PluginRoot

switch ($script:McpPluginInvocation.Command) {
    'Status' {
        Invoke-PluginPowerShellScript -ScriptPath (Resolve-PluginPowerShellScript -Root $pluginRootFull -Name 'mcp-status.ps1')
    }
    'Invoke' {
        if (-not $script:McpPluginInvocation.Method) {
            throw '-Method is required when -Command Invoke is used.'
        }

        $paramsText = if ($script:McpPluginParamsObjectBound) {
            $script:McpPluginInvocation.Params
        } else {
            Read-OptionalText -Inline $script:McpPluginInvocation.Params -HasInline:$($PSBoundParameters.ContainsKey('Params')) -Path $script:McpPluginInvocation.ParamsPath -AllowRedirectedInput
        }
        Invoke-PluginPowerShellScript -ScriptPath (Resolve-PluginPowerShellScript -Root $pluginRootFull -Name 'repl-invoke.ps1') -Arguments @('-Method', $script:McpPluginInvocation.Method, '-ParamsYaml', ($paramsText ?? ''))
    }
    'CompleteTurn' {
        $responseText = Read-OptionalText -Inline $script:McpPluginInvocation.Response -HasInline:$($PSBoundParameters.ContainsKey('Response')) -Path $script:McpPluginInvocation.ResponsePath -AllowRedirectedInput
        if (-not $responseText) {
            $responseText = 'Turn completed.'
        }

        Invoke-PluginPowerShellScript -ScriptPath (Resolve-PluginPowerShellScript -Root $pluginRootFull -Name 'final-response.ps1') -StandardInput $responseText
    }
}
