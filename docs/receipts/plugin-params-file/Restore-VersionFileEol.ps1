#Requires -Version 7.4
<#
.SYNOPSIS
    The version bump rewrote plugin JSON files and .version with LF line endings; where the committed file used CRLF,
    restore CRLF so the diff shows only the value change. Content is untouched apart from line endings.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Root = 'F:\GitHub',
    [string]$WorktreeName = 'params-file-20261019'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repos = @('mcpserver-claude-code-plugin', 'mcpserver-codex-plugin', 'mcpserver-cline-plugin', 'mcpserver-cline-v2-plugin', 'mcpserver-copilot-plugin', 'mcpserver-grok-plugin', 'mcpserver-opencode-plugin', 'mcpserver-grok-bot-plugin', 'mcpserver-claude-cowork-plugin')
foreach ($r in $repos) {
    $w = Join-Path $Root "$r\.worktrees\$WorktreeName"
    foreach ($f in @(git -C $w diff --name-only -- '*.json' '.version')) {
        # Read the committed blob as raw bytes (a PowerShell pipeline would drop the line endings).
        $blob = git -C $w rev-parse "HEAD:$f"
        $headHasCrlf = $false
        $tmp = [System.IO.Path]::GetTempFileName()
        try {
            $psi = [System.Diagnostics.ProcessStartInfo]::new('git')
            foreach ($a in @('-C', $w, 'cat-file', 'blob', $blob)) { $psi.ArgumentList.Add($a) }
            $psi.RedirectStandardOutput = $true; $psi.UseShellExecute = $false
            $p = [System.Diagnostics.Process]::Start($psi)
            $fs = [System.IO.File]::Create($tmp); try { $p.StandardOutput.BaseStream.CopyTo($fs) } finally { $fs.Dispose() }
            $p.WaitForExit()
            $headHasCrlf = [System.IO.File]::ReadAllText($tmp).Contains("`r`n")
        }
        finally { Remove-Item -LiteralPath $tmp -ErrorAction SilentlyContinue }
        $path = Join-Path $w $f
        $text = [System.IO.File]::ReadAllText($path)
        $lf = $text -replace "`r`n", "`n"
        $want = if ($headHasCrlf) { $lf -replace "`n", "`r`n" } else { $lf }
        if ($want -cne $text) {
            if ($PSCmdlet.ShouldProcess($path, "match committed line endings ($(if ($headHasCrlf) { 'CRLF' } else { 'LF' }))")) {
                [System.IO.File]::WriteAllText($path, $want, [System.Text.UTF8Encoding]::new($false))
            }
            Write-Information ('{0} {1}: line endings set to {2}' -f $r, $f, $(if ($headHasCrlf) { 'CRLF' } else { 'LF' })) -InformationAction Continue
        }
    }
}
