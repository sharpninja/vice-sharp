[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference='Stop'
$parseTokens=$null;$parseErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis=@(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning,Error -WhatIf:$false)
if ($analysis.Count) { throw ($analysis | Out-String) }
Write-Output 'Parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess('Own review session','Run supported stop gate with isolated cache and drain disabled')) { return }
$cacheDir=Join-Path $PSScriptRoot 'plugin-cache'
$env:MCP_FAILSAFE_DRAIN_DISABLED='1'
$env:MCP_CACHE_DIR_OVERRIDE=$cacheDir
$env:MCP_WORKSPACE_PATH='/home/sharpninja/github/vice-sharp'
$env:MCP_SESSION_AGENT='Codex'
$env:MCP_SESSION_MODEL='codex'
$env:TMPDIR=$cacheDir;$env:TMP=$cacheDir;$env:TEMP=$cacheDir
$gatePath='/home/sharpninja/github/mcpserver-codex-plugin/lib/stop-gate.ps1'
$output=(& pwsh -NoProfile -NonInteractive -File $gatePath 2>&1 | Out-String)
$exitCode=$LASTEXITCODE
if ($output -match '(?im)(?:X-Api-Key\s*[:=]\s*|apiKey\s*[:=]\s*)[\x22\x27]?[A-Za-z0-9+/=_-]{20,}|Bearer\s+[A-Za-z0-9+/=_-]{20,}') { throw 'Credential-shaped output suppressed.' }
$output | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'stop-gate.txt') -Encoding utf8
Write-Output $output
if ($exitCode -ne 0 -or $output -match '"decision"\s*:\s*"block"') { throw 'Supported stop gate did not pass.' }
