#Requires -Version 7.4
<#
.SYNOPSIS
    Moves the host-specific (not core-synced) repl-invoke.ps1 launch sites in the plugin worktrees from
    -ParamsYaml argument text to the core params-file transfer (lib/repl-params-transfer.ps1):
      hooks/scripts/memory-context.ps1 (eight repos) and the Codex / Copilot entry scripts.
    Every anchor must occur exactly once per file or the script stops without writing that file.
    Line endings of each file are preserved. -WhatIf lists the planned edits only.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Root = 'F:\GitHub',
    [string]$WorktreeName = 'params-file-20261019'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')

$memoryEdits = @(
    [pscustomobject]@{
        Old = "    `$repl = Join-Path `$root 'lib/repl-invoke.ps1'"
        New = "    `$repl = Join-Path `$root 'lib/repl-invoke.ps1'`n    . (Join-Path `$root 'lib/repl-params-transfer.ps1')"
    }
    [pscustomobject]@{
        Old = "    `$psi = [System.Diagnostics.ProcessStartInfo]::new()`n    `$psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source"
        New = "    # Params travel in a file, never on the child command line (32,767-character limit on Windows).`n    `$paramsTransfer = New-McpReplParamsTransfer -ParamsText `$ParamsYaml`n    try {`n    `$psi = [System.Diagnostics.ProcessStartInfo]::new()`n    `$psi.FileName = (Get-Command pwsh -ErrorAction Stop).Source"
    }
    [pscustomobject]@{
        Old = "'-Method', `$Method, '-ParamsYaml', `$ParamsYaml)) {"
        New = "'-Method', `$Method) + @(`$paramsTransfer.Arguments)) {"
    }
    [pscustomobject]@{
        Old = "    return `$stdoutText.Trim()`n}"
        New = "    return `$stdoutText.Trim()`n    }`n    finally {`n        Remove-McpReplParamsTransfer -Transfer `$paramsTransfer`n    }`n}"
    }
)
$entryEdit = [pscustomobject]@{
    Old = "        Invoke-PowerShellPluginScript -ScriptPath (Join-Path `$pluginRootFull 'lib\repl-invoke.ps1') -Arguments @('-Method', `$Method, '-ParamsYaml', (`$paramsText ?? ''))"
    New = @(
        "        # Params travel in a file, never on the child command line (32,767-character limit on Windows)."
        "        . (Join-Path `$pluginRootFull 'lib\repl-params-transfer.ps1')"
        "        `$paramsTransfer = New-McpReplParamsTransfer -ParamsText (`$paramsText ?? '')"
        "        try {"
        "            Invoke-PowerShellPluginScript -ScriptPath (Join-Path `$pluginRootFull 'lib\repl-invoke.ps1') -Arguments (@('-Method', `$Method) + @(`$paramsTransfer.Arguments))"
        "        }"
        "        finally {"
        "            Remove-McpReplParamsTransfer -Transfer `$paramsTransfer"
        "        }"
    ) -join "`n"
}

$targets = foreach ($name in $repos) {
    $wt = Join-Path $Root "$name\.worktrees\$WorktreeName"
    $mem = Join-Path $wt 'hooks\scripts\memory-context.ps1'
    if (Test-Path -LiteralPath $mem) { [pscustomobject]@{ Repo = $name; Path = $mem; Edits = $memoryEdits } }
    foreach ($entry in 'Invoke-CodexMcpPlugin.ps1', 'Invoke-CopilotMcpPlugin.ps1') {
        $p = Join-Path $wt $entry
        if (Test-Path -LiteralPath $p) { [pscustomobject]@{ Repo = $name; Path = $p; Edits = @($entryEdit) } }
    }
}

foreach ($t in $targets) {
    $text = [System.IO.File]::ReadAllText($t.Path)
    $eol = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $updated = $text
    foreach ($e in $t.Edits) {
        $old = $e.Old -replace "`n", $eol
        $hits = ([regex]::Matches($updated, [regex]::Escape($old))).Count
        if ($hits -ne 1) { throw "$($t.Repo) $([IO.Path]::GetFileName($t.Path)): anchor found $hits times: $($e.Old.Split("`n")[0])" }
        $updated = $updated.Replace($old, ($e.New -replace "`n", $eol))
    }
    if ($PSCmdlet.ShouldProcess($t.Path, "apply $(@($t.Edits).Count) edits")) {
        $bom = [System.IO.File]::ReadAllBytes($t.Path)
        $hasBom = ($bom.Length -ge 3 -and $bom[0] -eq 0xEF -and $bom[1] -eq 0xBB -and $bom[2] -eq 0xBF)
        [System.IO.File]::WriteAllText($t.Path, $updated, [System.Text.UTF8Encoding]::new($hasBom))
        $tok = $null; $err = $null
        [void][System.Management.Automation.Language.Parser]::ParseFile($t.Path, [ref]$tok, [ref]$err)
        if ($err.Count) { throw "parse errors after edit in $($t.Path): $($err[0].Message)" }
    }
    Write-Information ('{0} {1}: {2} edits, all anchors unique' -f $t.Repo, [IO.Path]::GetFileName($t.Path), @($t.Edits).Count) -InformationAction Continue
}
