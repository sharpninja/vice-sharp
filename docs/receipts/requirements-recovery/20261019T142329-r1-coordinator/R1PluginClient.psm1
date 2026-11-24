#Requires -Version 7.4
<#
.SYNOPSIS
    Production client for Invoke-R1Coordinator: the same interface as R1FakeClient (GetRecord, UpdateBatch,
    GetMapping, UpsertMapping, Export, Now, State.Calls), bound to the mcpserver-claude-code-plugin wrapper.
    Call shapes follow 20261011T102600-0500-requirements-api-guard/README.md:
      typed reads   workflow.requirements.getFr | getTr | getTest  (id)
      record write  workflow.requirements.updateBatch               (records: kind, id, approved fields)
      mapping read  client.Requirements.GetMappingAsync             (frId)
      mapping write client.Requirements.UpsertMappingAsync          (frId, request: trIds, testIds; full union)
      export        workflow.requirements.generateDocument
    Params are serialized from native objects (ConvertTo-Yaml) into a params file; no handwritten YAML.
#>
Set-StrictMode -Version Latest

function New-R1PluginClient {
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'Constructs a client object; writes happen only when the coordinator invokes its members.')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$WorkDir,
        [string]$WorkspacePath = 'F:\GitHub\vice-sharp',
        [string]$PluginRoot = (Join-Path $env:USERPROFILE '.claude\plugins\cache\mcpserver-local\mcpserver\1.119.0')
    )
    Import-Module powershell-yaml -MinimumVersion 0.4.12 -ErrorAction Stop -Global
    New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null
    $state = [pscustomobject]@{ Calls = [System.Collections.Generic.List[object]]::new(); WorkDir = $WorkDir; WorkspacePath = $WorkspacePath; PluginRoot = $PluginRoot }

    $invoke = {
        param([string]$Method, [hashtable]$Params, [switch]$AllowMissing)
        $s = $state
        $pp = Join-Path $s.WorkDir ('params-{0}.yaml' -f [guid]::NewGuid().ToString('N'))
        ConvertTo-Yaml -Data $Params | Out-File -LiteralPath $pp -Encoding utf8NoBOM
        $env:CLAUDE_PLUGIN_ROOT = $s.PluginRoot; $env:PLUGIN_AGENT_NAME = 'Claude'
        try {
            $raw = & pwsh.exe -NoProfile -NonInteractive -File (Join-Path $s.PluginRoot 'lib\Invoke-McpPlugin.ps1') -Command Invoke -Method $Method -ParamsPath $pp -WorkspacePath $s.WorkspacePath 2>&1
            $exit = $LASTEXITCODE
        }
        finally { Remove-Item Env:CLAUDE_PLUGIN_ROOT, Env:PLUGIN_AGENT_NAME -ErrorAction SilentlyContinue }
        $text = $raw | Out-String
        $respPath = $pp -replace '\.yaml$', '.response.txt'
        $text | Out-File -LiteralPath $respPath -Encoding utf8NoBOM
        $doc = $null
        try { $doc = ConvertFrom-Yaml -Yaml ($text -replace '(?m)^---\s*$', '') -Ordered } catch { $doc = $null }
        if ($exit -ne 0 -or $null -eq $doc -or $doc.type -ne 'result') {
            if ($AllowMissing -and $text -match '(?i)not\s*found|404') { return $null }
            throw "$Method failed (exit $exit); response saved to $respPath"
        }
        return ($doc.payload.result | ConvertTo-Json -Depth 30 | ConvertFrom-Json)
    }.GetNewClosure()

    $client = @{ State = $state }
    $client.GetRecord = {
        param([string]$Kind, [string]$Id)
        $state.Calls.Add([pscustomobject]@{ Method = 'GetRecord'; Kind = $Kind; Id = $Id })
        $m = @{ fr = 'workflow.requirements.getFr'; tr = 'workflow.requirements.getTr'; test = 'workflow.requirements.getTest' }[$Kind]
        $r = & $invoke $m @{ id = $Id } -AllowMissing
        if ($null -eq $r) { return , @() }
        return , @($r)
    }.GetNewClosure()
    $client.UpdateBatch = {
        param([object[]]$Records)
        $state.Calls.Add([pscustomobject]@{ Method = 'UpdateBatch'; Records = ($Records | ConvertTo-Json -Depth 30 | ConvertFrom-Json) })
        $recs = @($Records | ForEach-Object { $o = [ordered]@{}; foreach ($p in $_.PSObject.Properties) { $o[$p.Name] = ($p.Value | ConvertTo-Json -Depth 30 | ConvertFrom-Json -AsHashtable) }; $o })
        return (& $invoke 'workflow.requirements.updateBatch' @{ records = $recs })
    }.GetNewClosure()
    $client.GetMapping = {
        param([string]$FrId)
        $state.Calls.Add([pscustomobject]@{ Method = 'GetMapping'; FrId = $FrId })
        return (& $invoke 'client.Requirements.GetMappingAsync' @{ frId = $FrId } -AllowMissing)
    }.GetNewClosure()
    $client.UpsertMapping = {
        param([string]$FrId, [string[]]$TrIds, [string[]]$TestIds)
        $state.Calls.Add([pscustomobject]@{ Method = 'UpsertMapping'; FrId = $FrId; TrIds = @($TrIds); TestIds = @($TestIds) })
        return (& $invoke 'client.Requirements.UpsertMappingAsync' @{ frId = $FrId; request = [ordered]@{ trIds = @($TrIds); testIds = @($TestIds) } })
    }.GetNewClosure()
    $client.Export = {
        $state.Calls.Add([pscustomobject]@{ Method = 'Export' })
        try { $r = & $invoke 'workflow.requirements.generateDocument' @{}; return [pscustomobject]@{ ExitCode = 0; Output = ($r | ConvertTo-Json -Depth 10 -Compress) } }
        catch { return [pscustomobject]@{ ExitCode = 1; Output = "$_" } }
    }.GetNewClosure()
    $client.Now = { Get-Date }
    return $client
}

Export-ModuleMember -Function New-R1PluginClient
