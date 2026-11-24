[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Status','Bootstrap','Open','Begin','Invoke')][string]$Operation = 'Status',
    [string]$Method, [string]$PayloadPath, [string]$Label = 'plugin', [switch]$Quiet
)
$ErrorActionPreference = 'Stop'
$parseTokens = $null; $parseErrors = $null
$null = [Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning,Error -WhatIf:$false)
if ($analysis.Count) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess($Operation,'Invoke supported plugin with receipt-local cache; drain disabled')) { return }
$workspace = '/home/sharpninja/github/vice-sharp'
$pluginRoot = '/home/sharpninja/github/mcpserver-codex-plugin'
$cacheDir = Join-Path $PSScriptRoot 'plugin-cache'
$identityPath = Join-Path $PSScriptRoot 'reviewer-identity.json'
$null = New-Item -ItemType Directory -Path $cacheDir -Force
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'
$env:TMPDIR = $cacheDir; $env:TMP = $cacheDir; $env:TEMP = $cacheDir
$invokeArgs = @{WorkspacePath=$workspace;PluginRoot=$pluginRoot;CacheRoot=$cacheDir}
$payload = [ordered]@{}
switch ($Operation) {
    'Status' { $invokeArgs.Command = 'Status' }
    'Bootstrap' { $invokeArgs.Command='Invoke';$invokeArgs.Method='workflow.sessionlog.bootstrap' }
    'Open' {
        $identity = [ordered]@{reviewerAgent='Codex';reviewerModel='gpt-6.1-sol';reviewerEffort='max';reviewerSessionId=('Codex-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')+'-hv-nativerestore-r4');reviewerRequestId=('req-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')+'-hv-nativerestore-r4')}
        $identity | ConvertTo-Json | Set-Content -LiteralPath $identityPath -Encoding utf8
        $invokeArgs.Command='Invoke';$invokeArgs.Method='workflow.sessionlog.openSession'
        $payload=[ordered]@{agent='Codex';sessionId=$identity.reviewerSessionId;title='Hostile review of native restoration revision 4';model='gpt-6.1-sol'}
    }
    'Begin' {
        $identity=Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
        $request=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
        $invokeArgs.Command='Invoke';$invokeArgs.Method='workflow.sessionlog.beginTurn'
        $payload=[ordered]@{requestId=$identity.reviewerRequestId;sessionId=$identity.reviewerSessionId;queryTitle='Hostile review of native restoration revision 4';queryText=$request.prompt;interpretation='Class 1 independent pre-execution plan review, round 4. Read-only outside this receipt directory; only explicitly permitted managed tests. No native mutations or builds.';model='gpt-6.1-sol';agent='Codex';sourceType='Codex';parentSessionId=$request.parentSessionId;parentRequestId=$request.parentRequestId;tags=@('PLAN-NATIVERESTORE-001','revision-4','hostile-validation',('parentSessionId='+$request.parentSessionId),('parentRequestId='+$request.parentRequestId));contextList=@($request.inputPinsPath,$request.promptPath,$request.responsePath)}
    }
    'Invoke' {
        if (-not $Method) { throw 'Method required.' }
        $invokeArgs.Command='Invoke';$invokeArgs.Method=$Method
        if ($PayloadPath) { $payload=Get-Content -LiteralPath $PayloadPath -Raw | ConvertFrom-Json -AsHashtable }
    }
}
if ($Operation -ne 'Status') { $invokeArgs.ParamsObject=$payload }
try { $resultText=(& (Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1') @invokeArgs 2>&1 | Out-String) } catch { $resultText=($_ | Out-String) }
if ($resultText -match '(?im)(?:X-Api-Key\s*[:=]\s*|apiKey\s*[:=]\s*)[\x22\x27]?[A-Za-z0-9+/=_-]{20,}|Bearer\s+[A-Za-z0-9+/=_-]{20,}') { throw 'Credential-shaped output suppressed.' }
if ($Operation -eq 'Begin' -and $resultText -match '(?m)^\s+sessionId:\s*([^\r\n]+)') { $identity.reviewerSessionId=$Matches[1].Trim().Trim('"');$identity | ConvertTo-Json | Set-Content -LiteralPath $identityPath -Encoding utf8 }
$resultText | Set-Content -LiteralPath (Join-Path $PSScriptRoot ($Label+'.txt')) -Encoding utf8
if ($Quiet) { Write-Output ('Wrapper output retained: '+$Label+'.txt; characters='+$resultText.Length) } else { Write-Output $resultText }
