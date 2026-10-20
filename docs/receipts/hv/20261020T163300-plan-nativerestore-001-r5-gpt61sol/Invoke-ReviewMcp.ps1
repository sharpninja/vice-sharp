[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Status', 'Bootstrap', 'Begin', 'Invoke', 'History', 'Checkpoint', 'Store', 'QueryOwn', 'Decision')]
    [string]$Mode = 'Status',
    [string]$Method,
    [string]$PayloadPath,
    [string]$ReceiptName = 'mcp-operation.txt'
    , [string]$Note
)
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess('Own isolated review session via supported Codex MCP wrapper', $Mode)) { return }
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'
$pluginRoot = '/home/sharpninja/github/mcpserver-codex-plugin'
$wrapper = Join-Path $pluginRoot 'Invoke-CodexMcpPlugin.ps1'
$workspacePath = '/home/sharpninja/github/vice-sharp'
$cacheRoot = Join-Path $PSScriptRoot 'plugin-cache'
[void](New-Item -ItemType Directory -Path $cacheRoot -Force)
$script:reviewReceiptPath = Join-Path $PSScriptRoot $ReceiptName
$options = @{ WorkspacePath = $workspacePath; CacheRoot = $cacheRoot }
function Invoke-ReviewPlugin {
    param([string]$PluginMethod, [object]$Parameters)
    $result = & $wrapper -Command Invoke -Method $PluginMethod -ParamsObject $Parameters @options
    $result | Add-Content -LiteralPath $script:reviewReceiptPath
    $result
}
switch ($Mode) {
    'Decision' {
        Invoke-ReviewPlugin -PluginMethod 'workflow.sessionlog.appendDialog' -Parameters @{
            dialogItems = @(@{ timestamp = (Get-Date -Format o); role = 'model'; category = 'decision'; content = $Note })
        }
        Invoke-ReviewPlugin -PluginMethod 'workflow.sessionlog.appendActions' -Parameters @{
            actions = @(@{ order = [int]([DateTimeOffset]::Now.ToUnixTimeSeconds() % 1000000); type = 'design_decision'; status = 'completed'; description = $Note; filePath = 'docs/plans/PLAN-NATIVERESTORE-001.md' })
        }
    }
    'QueryOwn' {
        $ownResult = & $wrapper -Command Invoke -Method 'client.SessionLog.QueryAsync' -ParamsObject @{ agent = 'Codex'; limit = 1; offset = 0 } @options
        $ownResult | Set-Content -LiteralPath (Join-Path $PSScriptRoot $ReceiptName)
        Write-Output ('Own-session query retained in ' + $ReceiptName)
    }
    'Store' {
        foreach ($read in @(
            @{ method = 'workflow.requirements.listLayers'; data = @{}; name = 'store-layers.txt' },
            @{ method = 'workflow.requirements.effective'; data = @{}; name = 'store-effective.txt' },
            @{ method = 'workflow.requirements.listMappings'; data = @{ frId = 'FR-VICE-001' }; name = 'store-vice-mapping.txt' },
            @{ method = 'workflow.requirements.listMappings'; data = @{ frId = 'FR-SNP-005' }; name = 'store-snp-mapping.txt' },
            @{ method = 'workflow.todo.get'; data = @{ id = 'PLAN-VICEORACLE-001' }; name = 'store-todo.txt' }
        )) {
            $readResult = & $wrapper -Command Invoke -Method $read.method -ParamsObject $read.data @options
            $readResult | Set-Content -LiteralPath (Join-Path $PSScriptRoot $read.name)
            Write-Output ($read.name + ': retained ' + ($readResult | Out-String).Length + ' characters')
        }
    }
    'History' {
        Invoke-ReviewPlugin -PluginMethod 'workflow.sessionlog.queryHistory' -Parameters @{ agent = 'Codex'; limit = 5; offset = 0 }
    }
    'Checkpoint' {
        Invoke-ReviewPlugin -PluginMethod 'workflow.sessionlog.appendDialog' -Parameters @{
            dialogItems = @(@{ timestamp = (Get-Date -Format o); role = 'model'; category = 'observation'; content = $Note })
        }
        Invoke-ReviewPlugin -PluginMethod 'workflow.sessionlog.appendActions' -Parameters @{
            actions = @(@{ order = [int]([DateTimeOffset]::Now.ToUnixTimeSeconds() % 1000000); type = 'validation'; status = 'completed'; description = $Note; filePath = $PSScriptRoot })
        }
    }
    'Status' {
        $result = & $wrapper -Command Status @options
        $result | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'mcp-status.txt')
        $result
    }
    'Bootstrap' {
        Invoke-ReviewPlugin -PluginMethod 'workflow.sessionlog.bootstrap' -Parameters @{}
        $identity = [ordered]@{
            sessionId = ('Codex-' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssZ') + '-nativerestore-r5')
            requestId = ('req-' + (Get-Date -AsUTC -Format 'yyyyMMddTHHmmssZ') + '-nativerestore-r5')
            model = 'gpt-6.1-sol'
            effort = 'max'
            sourceType = 'Codex'
        }
        $identity | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'review-identity.json')
        Invoke-ReviewPlugin -PluginMethod 'workflow.sessionlog.openSession' -Parameters @{
            agent = 'Codex'; sessionId = $identity.sessionId; title = 'Hostile review of native restoration revision 5'; model = $identity.model
        }
    }
    'Begin' {
        $identity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'review-identity.json') -Raw | ConvertFrom-Json
        $request = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'request.jsonl') -Raw | ConvertFrom-Json
        Invoke-ReviewPlugin -PluginMethod 'workflow.sessionlog.beginTurn' -Parameters @{
            requestId = $identity.requestId; queryTitle = 'Hostile review of PLAN-NATIVERESTORE-001 revision 5'
            queryText = $request.prompt; parentSessionId = $request.parentSessionId; parentRequestId = $request.parentRequestId
        }
        Invoke-ReviewPlugin -PluginMethod 'workflow.sessionlog.updateTurn' -Parameters @{
            interpretation = 'Class 1 plan review before implementation. Review revision 5 against live pinned sources, all round 4 IDs, requirements, operator rulings, what-if cases, and whole-plan dependencies. No implementation or native changes authorized.'
            tags = @('hostile-validation', 'PLAN-NATIVERESTORE-001', 'revision-5', 'gpt-6.1-sol', 'max', ('parentSessionId:' + $request.parentSessionId), ('parentRequestId:' + $request.parentRequestId))
            contextList = @($request.inputPinsPath, $request.responsePath, (Join-Path $PSScriptRoot 'input-pins.verified.json'))
        }
    }
    'Invoke' {
        $payload = Get-Content -LiteralPath $PayloadPath -Raw | ConvertFrom-Json -AsHashtable
        Invoke-ReviewPlugin -PluginMethod $Method -Parameters $payload
    }
}
