[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidateSet('Status','Bootstrap','Open','Begin','Call','Checkpoint','Catalog','StopGate')][string]$Mode,
    [string]$Method,
    [string]$PayloadPath,
    [string]$Label = 'call',
    [string]$Note = '',
    [int]$Start = 0,
    [int]$Count = 4
)
$ErrorActionPreference = 'Stop'
if ($WhatIfPreference) { Write-Output ('WhatIf: plugin ' + $Mode); return }
if (-not $PSCmdlet.ShouldProcess('isolated review session', $Mode)) { return }
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'
$env:MCP_WORKSPACE_PATH = '/home/sharpninja/github/vice-sharp'
$env:MCP_CACHE_DIR_OVERRIDE = Join-Path $PSScriptRoot 'plugin-cache'
$env:MCPSERVER_FAILSAFE_DIR = Join-Path $PSScriptRoot 'plugin-failsafe'
$env:TEMP = Join-Path $PSScriptRoot 'plugin-temp'
$env:TMP = $env:TEMP
$env:MCP_PLUGIN_ROOT = '/home/sharpninja/github/mcpserver-codex-plugin'
$env:CODEX_PLUGIN_ROOT = $env:MCP_PLUGIN_ROOT
$env:MCP_AGENT_NAME = 'Codex'
$env:MCP_SESSION_MODEL = 'gpt-6.1-sol'
$env:CT2R_MODEL = 'gpt-6.1-sol'
$env:MCP_PLUGIN_HOST = 'codex'
$null = New-Item -ItemType Directory -Path $env:MCP_CACHE_DIR_OVERRIDE -Force
$null = New-Item -ItemType Directory -Path $env:MCPSERVER_FAILSAFE_DIR -Force
$null = New-Item -ItemType Directory -Path $env:TEMP -Force
$wrapper = Join-Path $env:MCP_PLUGIN_ROOT 'Invoke-CodexMcpPlugin.ps1'
$request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
$identityPath = Join-Path $PSScriptRoot 'review-identity.json'
if (-not (Test-Path -LiteralPath $identityPath)) {
    $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
    $identity = [ordered]@{reviewerAgent='Codex';reviewerModel='gpt-6.1-sol';reviewerEffort='max';reviewerSessionId=('Codex-' + $stamp + '-nativerestore-r8');reviewerRequestId=('req-' + $stamp + '-nativerestore-r8');parentSessionId=$request.parentSessionId;parentRequestId=$request.parentRequestId;runtimeThreadId=$env:CODEX_THREAD_ID;timestampLocal=(Get-Date -Format s)}
    $identity | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $identityPath
}
$identity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
$beginReceiptPath = Join-Path $PSScriptRoot 'plugin-begin.txt'
if (Test-Path -LiteralPath $beginReceiptPath) {
    $beginReceipt = Get-Content -LiteralPath $beginReceiptPath -Raw
    if ($beginReceipt -match 'persisted: true' -and $beginReceipt -match '(?m)^\s*sessionId:\s*(\S+)') {
        $identity.reviewerSessionId = $Matches[1]
        $identity | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $identityPath
    }
}
if ($Mode -eq 'StopGate') {
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = (Get-Command pwsh -ErrorAction Stop).Source
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardInput = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.WorkingDirectory = $env:MCP_WORKSPACE_PATH
    foreach ($argument in @('-NoProfile','-NonInteractive','-File',(Join-Path $env:MCP_PLUGIN_ROOT 'lib/stop-gate.ps1'))) { $startInfo.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($startInfo)
    $process.StandardInput.Close()
    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit()
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    if (($stdout + $stderr) -match '(?im)(X-Api-Key\s*:\s*\S+|apiKey\s*:\s*[A-Za-z0-9._-]{20,}|Bearer\s+[A-Za-z0-9._-]{20,})') { throw 'Credential output blocked.' }
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'plugin-stop-gate.stdout.txt'),$stdout)
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'plugin-stop-gate.stderr.txt'),$stderr)
    $parsed = $stdout | ConvertFrom-Json
    $result = [ordered]@{timestampLocal=(Get-Date -Format s);exitCode=$process.ExitCode;stdout=$stdout;stderr=$stderr;blocked=($parsed.decision -eq 'block')}
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'plugin-stop-gate.proof.json')
    $result | ConvertTo-Json -Depth 8
    if ($process.ExitCode -ne 0 -or $result.blocked) { throw 'Supported stop gate did not permit completion.' }
    return
}
$paramsObject = [ordered]@{}
if ($Mode -eq 'Catalog') {
    $queries = @(
        @{method='workflow.requirements.getFr';params=@{id='FR-VICE-001'}}
        @{method='workflow.requirements.getTr';params=@{id='TR-VICE-ORACLE-001'}}
        @{method='workflow.requirements.getTest';params=@{id='TEST-VICE-001'}}
        @{method='workflow.requirements.getTr';params=@{id='TR-VSFLOCKSTEP-RESUME-001'}}
        @{method='workflow.requirements.getTr';params=@{id='TR-CYCLE-001'}}
        @{method='workflow.requirements.getFr';params=@{id='FR-SNP-005'}}
        @{method='workflow.requirements.getTest';params=@{id='TEST-SNP-002'}}
        @{method='workflow.requirements.listMappings';params=@{frId='FR-VICE-001'}}
        @{method='workflow.requirements.listMappings';params=@{frId='FR-SNP-005'}}
        @{method='workflow.todo.get';params=@{id='PLAN-VICEORACLE-001'}}
    )
    foreach ($query in ($queries | Select-Object -Skip $Start -First $Count)) {
        $result = (& $wrapper -Command Invoke -Method $query.method -ParamsObject $query.params -WorkspacePath $env:MCP_WORKSPACE_PATH -PluginRoot $env:MCP_PLUGIN_ROOT -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE 2>&1 | Out-String)
        if ($result -match '(?im)(X-Api-Key\s*:\s*\S+|Bearer\s+[A-Za-z0-9._-]{20,}|apiKey\s*:\s*[A-Za-z0-9._-]{20,})') { throw 'Credential output blocked.' }
        $recordId = if ($query.params.id) { $query.params.id } else { 'mapping-' + $query.params.frId }
        $result | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('store-' + $recordId + '.txt'))
        Write-Output ($query.method + ': ' + $recordId)
        Write-Output $result
    }
    return
}
switch ($Mode) {
    'Status' { $arguments = @{Command='Status'} }
    'Bootstrap' { $arguments = @{Command='Invoke';Method='workflow.sessionlog.bootstrap';ParamsObject=$paramsObject} }
    'Open' { $arguments = @{Command='Invoke';Method='workflow.sessionlog.openSession';ParamsObject=[ordered]@{agent='Codex';sessionId=$identity.reviewerSessionId;title='Hostile review of native restoration revision 8';model='gpt-6.1-sol'}} }
    'Begin' { $arguments = @{Command='Invoke';Method='workflow.sessionlog.beginTurn';ParamsObject=[ordered]@{requestId=$identity.reviewerRequestId;queryTitle='Hostile review of native restoration revision 8';queryText=$request.prompt}} }
    'Call' {
        if ($PayloadPath) { $paramsObject = Get-Content -LiteralPath $PayloadPath -Raw | ConvertFrom-Json -AsHashtable }
        $arguments = @{Command='Invoke';Method=$Method;ParamsObject=$paramsObject}
    }
    'Checkpoint' {
        $arguments = @{Command='Invoke';Method='workflow.sessionlog.updateTurn';ParamsObject=[ordered]@{
            requestId=$identity.reviewerRequestId
            interpretation='Class-1 independent plan review of revision 8 before execution; stale revision 4 phrases in the request do not change the live subject. No nested review and no native mutation.'
            response=$Note
            tags=@('PLAN-NATIVERESTORE-001','hostile-review','revision-8',('parentSessionId:' + $identity.parentSessionId),('parentRequestId:' + $identity.parentRequestId))
            contextList=@($request.inputPinsPath,$request.responsePath,('parentSessionId=' + $identity.parentSessionId),('parentRequestId=' + $identity.parentRequestId),'reviewerModel=gpt-6.1-sol;reviewerEffort=max')
        }}
    }
}
$arguments.WorkspacePath = $env:MCP_WORKSPACE_PATH
$arguments.PluginRoot = $env:MCP_PLUGIN_ROOT
$arguments.CacheRoot = $env:MCP_CACHE_DIR_OVERRIDE
$captured = try { (& $wrapper @arguments 2>&1 | Out-String) } catch { $_ | Out-String }
if ($captured -match '(?im)(X-Api-Key\s*:\s*\S+|Bearer\s+[A-Za-z0-9._-]{20,})') { throw 'Credential output blocked.' }
$captured | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('plugin-' + $Label + '.txt'))
if ($Mode -eq 'Begin' -and $captured -match 'persisted: true' -and $captured -match '(?m)^\s*sessionId:\s*(\S+)') {
    $identity.reviewerSessionId = $Matches[1]
    $identity | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $identityPath
}
Write-Output $captured
Write-Output ('Identity: ' + $identity.reviewerSessionId + ' / ' + $identity.reviewerRequestId)
