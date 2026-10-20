#Requires -Version 7.0
[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Status','Bootstrap','Open','Link','Note','Decision','Prior','Store','Call','Pins','Read','Search','Progress','Inspect','Test')]
    [string]$Operation = 'Progress',
    [string]$Path,
    [string]$Method,
    [string]$Pattern,
    [string]$Message,
    [int]$Start = 1,
    [int]$End = 100000,
    [int]$Percent = 1,
    [Nullable[int]]$Eta = $null
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$receiptRoot = $PSScriptRoot
$workspaceRoot = '/home/sharpninja/github/vice-sharp'
$pluginRoot = '/home/sharpninja/github/mcpserver-codex-plugin'
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'
$env:CODEX_PLUGIN_ROOT = $pluginRoot
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:MCP_WORKSPACE_PATH = $workspaceRoot
$env:MCP_CACHE_DIR_OVERRIDE = Join-Path $receiptRoot 'plugin-cache'
switch ($Operation) {
    'Prior' {
        $previous = Get-Content -LiteralPath (Join-Path $workspaceRoot 'docs/receipts/hv/20261020T163300-plan-nativerestore-001-r5-gpt61sol/verdict.json') -Raw | ConvertFrom-Json
        foreach ($finding in $previous.findings) {
            [ordered]@{id=$finding.id;status=$finding.status;section=$finding.section;rationale=$finding.rationale} | ConvertTo-Json -Compress
        }
    }
    'Decision' {
        if ($PSCmdlet.ShouldProcess($receiptRoot,'Persist source-based review judgment and design-decision action')) {
            $wrapperPath = Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1'
            & $wrapperPath -Command Invoke -Method 'workflow.sessionlog.appendDialog' -ParamsObject @{dialogItems=@(@{timestamp=(Get-Date).ToString('o');role='model';category='decision';content=$Message})} -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
            & $wrapperPath -Command Invoke -Method 'workflow.sessionlog.appendActions' -ParamsObject @{actions=@(@{order=$Start;description=$Message;type='design_decision';status='completed';filePath=$Path})} -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
        }
    }
    'Note' {
        if ($PSCmdlet.ShouldProcess($receiptRoot,'Persist review observation and action immediately')) {
            $wrapperPath = Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1'
            & $wrapperPath -Command Invoke -Method 'workflow.sessionlog.appendDialog' -ParamsObject @{dialogItems=@(@{timestamp=(Get-Date).ToString('o');role='model';category='observation';content=$Message})} -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
            & $wrapperPath -Command Invoke -Method 'workflow.sessionlog.appendActions' -ParamsObject @{actions=@(@{order=$Start;description=$Message;type='validation';status='completed';filePath=$Path})} -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
        }
    }
    'Store' {
        if ($PSCmdlet.ShouldProcess($receiptRoot,'Read supported effective requirements and own history into receipts')) {
            $wrapperPath = Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1'
            foreach ($storeQuery in @(@{label='effective';method='workflow.requirements.effective';params=@{}},@{label='history';method='workflow.sessionlog.queryHistory';params=@{agent='Codex';limit=5;offset=0}})) {
                $queryResult = & $wrapperPath -Command Invoke -Method $storeQuery.method -ParamsObject $storeQuery.params -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
                $queryResult | Set-Content -LiteralPath (Join-Path $receiptRoot ('store-' + $storeQuery.label + '.txt'))
                Write-Output ($storeQuery.label + ': saved ' + @($queryResult).Count + ' output lines')
            }
        }
    }
    'Search' {
        if ($Path -match 'AGENTS-README-FIRST|\.creds|mcp\.db') { throw 'Prohibited search path.' }
        & rg -n --glob '*.cs' --glob '*.ps1' --glob '*.c' --glob '*.h' --glob '*.md' --glob '*.csproj' --glob '*.proj' --glob '*.slnx' -- $Pattern $Path
    }
    'Progress' { [ordered]@{ timestampLocal=(Get-Date).ToString('s'); completionPercent=$Percent; etaSeconds=$Eta } | ConvertTo-Json -Compress }
    'Read' {
        $resolvedPath = (Resolve-Path -LiteralPath $Path).Path
        if ($resolvedPath -match 'AGENTS-README-FIRST|session-state|credentials|\.creds|mcp\.db') { throw 'Credential-bearing or prohibited path rejected.' }
        $allLines = [System.IO.File]::ReadAllLines($resolvedPath)
        for ($index = [Math]::Max(0,$Start-1); $index -lt [Math]::Min($End,$allLines.Length); $index++) { '{0,5}: {1}' -f ($index+1), $allLines[$index] }
    }
    'Status' {
        if ($PSCmdlet.ShouldProcess($receiptRoot,'Run supported plugin status with isolated cache')) {
            New-Item -Path $env:MCP_CACHE_DIR_OVERRIDE -ItemType Directory -Force | Out-Null
            & (Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1') -Command Status -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
        }
    }
    'Bootstrap' {
        if ($PSCmdlet.ShouldProcess($receiptRoot,'Bootstrap supported plugin session')) {
            & (Join-Path $pluginRoot 'lib/session-start.ps1') $workspaceRoot
        }
    }
    'Call' {
        $callParameters = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable
        if ($PSCmdlet.ShouldProcess($Method,'Invoke supported wrapper')) {
            & (Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1') -Command Invoke -Method $Method -ParamsObject $callParameters -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
        }
    }
    'Open' {
        if ($PSCmdlet.ShouldProcess($receiptRoot,'Open linked independent review session and turn')) {
            $request = Get-Content -LiteralPath (Join-Path $receiptRoot 'request.jsonl') -Raw | ConvertFrom-Json
            $reviewIdentity = [ordered]@{reviewerAgent='Codex'; reviewerModel='gpt-6.1-sol'; reviewerEffort='max'; reviewerSessionId=('Codex-' + (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ') + '-nativerestore-r6'); reviewerRequestId=('req-' + (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ') + '-nativerestore-r6'); parentSessionId=$request.parentSessionId; parentRequestId=$request.parentRequestId}
            $reviewIdentity | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $receiptRoot 'review-identity.json')
            $wrapperPath = Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1'
            & $wrapperPath -Command Invoke -Method 'workflow.sessionlog.openSession' -ParamsObject @{agent='Codex';sessionId=$reviewIdentity.reviewerSessionId;title='Hostile review Phase D native restoration revision 6';model='gpt-6.1-sol'} -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
            & $wrapperPath -Command Invoke -Method 'workflow.sessionlog.beginTurn' -ParamsObject @{requestId=$reviewIdentity.reviewerRequestId;queryTitle='Hostile review native restoration plan revision 6';queryText=$request.prompt;parentSessionId=$request.parentSessionId;parentRequestId=$request.parentRequestId} -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
            & $wrapperPath -Command Invoke -Method 'workflow.sessionlog.updateTurn' -ParamsObject @{interpretation='Class-1 independent pre-execution plan review. No implementation authorized. Reverify all prior findings and whole revision 6 from live sources. Credential prohibition overrides marker-read instructions.';tags=@('PLAN-NATIVERESTORE-001','hostile-validation','revision-6','gpt-6.1-sol','effort-max',('parentSessionId:'+ $request.parentSessionId),('parentRequestId:'+ $request.parentRequestId));contextList=@((Join-Path $receiptRoot 'request.jsonl'),$request.promptPath,$request.inputPinsPath);response='Profile loaded, supported plugin checked with failsafe draining disabled. All 41 input pins rehashed with zero drift.'} -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
            $reviewIdentity | ConvertTo-Json
        }
    }
    'Link' {
        if ($PSCmdlet.ShouldProcess($receiptRoot,'Record actual supported plugin identity and parent links')) {
            $request = Get-Content -LiteralPath (Join-Path $receiptRoot 'request.jsonl') -Raw | ConvertFrom-Json
            $identityPath = Join-Path $receiptRoot 'review-identity.json'
            $identity = Get-Content -LiteralPath $identityPath -Raw | ConvertFrom-Json
            $identity.reviewerSessionId = 'Codex-20261020T222731Z-plugin-session'
            $identity | ConvertTo-Json | Set-Content -LiteralPath $identityPath
            & (Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1') -Command Invoke -Method 'workflow.sessionlog.updateTurn' -ParamsObject @{interpretation='Class-1 independent pre-execution review of PLAN-NATIVERESTORE-001 revision 6. Read-only except round 6 receipt. Parent links are explicit tags and context. No nested reviewer. Marker read prohibited. Add-profile fully read.';tags=@('PLAN-NATIVERESTORE-001','revision-6','hostile-validation','gpt-6.1-sol','effort-max',('parentSessionId:'+ $request.parentSessionId),('parentRequestId:'+ $request.parentRequestId));contextList=@((Join-Path $receiptRoot 'request.jsonl'),$request.promptPath,$request.inputPinsPath);response='Supported plugin is available. BeginTurn returned persisted:true. Zero drift across all 41 input pins. Existing two queued operations were not replayed.'} -WorkspacePath $workspaceRoot -CacheRoot $env:MCP_CACHE_DIR_OVERRIDE
        }
    }
    'Pins' {
        $pins = Get-Content -LiteralPath (Join-Path $receiptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $results = foreach ($pin in $pins) {
            if ($pin.path -match 'AGENTS-README-FIRST|\.creds') { throw 'Prohibited pin.' }
            $actualHash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
            $actualBytes = (Get-Item -LiteralPath $pin.path -Force).Length
            [ordered]@{path=$pin.relativePath; expected=$pin.sha256; actual=$actualHash; bytes=$actualBytes; match=($actualHash -eq $pin.sha256 -and $actualBytes -eq $pin.bytes)}
        }
        $pinProof = [ordered]@{timestampLocal=(Get-Date).ToString('s'); pinCount=@($results).Count; driftCount=@($results | Where-Object {-not $_.match}).Count; pins=@($results)}
        if ($PSCmdlet.ShouldProcess($receiptRoot,'Write input-pin rehash proof')) {
            $pinProof | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $receiptRoot 'pin-verification.json')
        }
        $pinProof | Select-Object timestampLocal,pinCount,driftCount | ConvertTo-Json
    }
    'Inspect' {
        Write-Output ('BRANCH ' + (& git -C $workspaceRoot branch --show-current))
        Write-Output ('HEAD ' + (& git -C $workspaceRoot rev-parse HEAD))
        & git -C $workspaceRoot status --porcelain=v1 -uno
        & git -C (Join-Path $workspaceRoot 'native/vice') status --porcelain=v1 -uall
        & git -C (Join-Path $workspaceRoot 'native/vice') diff --stat
        & git -C $workspaceRoot submodule status
    }
    'Test' {
        if ($PSCmdlet.ShouldProcess($receiptRoot,'Run allowed managed comparator filter with retained evidence')) {
            & dotnet test (Join-Path $workspaceRoot 'tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj') -c Release --filter 'FullyQualifiedName~Vic20HalfCycleComparatorTests' --results-directory (Join-Path $receiptRoot 'TestResults') --logger 'trx;LogFileName=vic20-comparator.trx' --diag (Join-Path $receiptRoot 'TestResults/vstest.diag.log') --blame-hang --blame-hang-timeout 5m 2>&1 | Tee-Object -FilePath (Join-Path $receiptRoot 'comparator-test.log')
            Write-Output ('DOTNET_EXIT_CODE=' + $LASTEXITCODE)
        }
    }
}
