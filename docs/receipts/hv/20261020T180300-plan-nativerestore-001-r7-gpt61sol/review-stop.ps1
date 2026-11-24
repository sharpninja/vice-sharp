[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess('Own completed review turn', 'Run supported plugin Phase 3 stop gate')) { return }
$pluginRoot = '/home/sharpninja/github/mcpserver-codex-plugin'
$startInfo = [System.Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = (Get-Command pwsh -ErrorAction Stop).Source
$startInfo.UseShellExecute = $false
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.RedirectStandardInput = $true
$startInfo.WorkingDirectory = '/home/sharpninja/github/vice-sharp'
foreach ($argument in @('-NoProfile','-NonInteractive','-File',(Join-Path $pluginRoot 'lib/stop-gate.ps1'))) { $startInfo.ArgumentList.Add($argument) }
$startInfo.Environment['MCP_CACHE_DIR_OVERRIDE'] = Join-Path $PSScriptRoot 'review-plugin-cache'
$startInfo.Environment['MCP_FAILSAFE_DRAIN_DISABLED'] = '1'
$startInfo.Environment['MCP_WORKSPACE_PATH'] = $startInfo.WorkingDirectory
$startInfo.Environment['MCPSERVER_WORKSPACE_PATH'] = $startInfo.WorkingDirectory
$startInfo.Environment['CODEX_WORKSPACE_PATH'] = $startInfo.WorkingDirectory
$startInfo.Environment['CODEX_PLUGIN_ROOT'] = $pluginRoot
$startInfo.Environment['MCP_PLUGIN_ROOT'] = $pluginRoot
$startInfo.Environment['MCP_PLUGIN_HOST'] = 'codex'
$startInfo.Environment['PLUGIN_AGENT_DEFAULT'] = 'Codex'
$startInfo.Environment['MCP_AGENT_NAME'] = 'Codex'
$startInfo.Environment['MCP_AGENT_ID'] = 'Codex'
$process = [System.Diagnostics.Process]::Start($startInfo)
$process.StandardInput.Close()
$stdout = $process.StandardOutput.ReadToEndAsync()
$stderr = $process.StandardError.ReadToEndAsync()
$process.WaitForExit()
$outputText = $stdout.GetAwaiter().GetResult()
$errorText = $stderr.GetAwaiter().GetResult()
if (($outputText + $errorText) -match '(?im)(X-Api-Key\s*:\s*\S+|apiKey\s*:\s*[A-Za-z0-9_-]{20,}|Bearer\s+[A-Za-z0-9._-]+)') { throw 'Potential credential output refused' }
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'plugin-stop-gate.stdout.txt'),$outputText)
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'plugin-stop-gate.stderr.txt'),$errorText)
[pscustomobject]@{ timestampLocal = (Get-Date).ToString('s'); exitCode = $process.ExitCode; stdout = $outputText; stderr = $errorText } | ConvertTo-Json -Depth 8
if ($process.ExitCode -ne 0) { throw 'Stop gate returned nonzero' }
