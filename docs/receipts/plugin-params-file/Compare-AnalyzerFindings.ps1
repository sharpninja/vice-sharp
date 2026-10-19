#Requires -Version 7.4
<#
.SYNOPSIS
    Runs Invoke-ScriptAnalyzer (Warning, Error) on each changed file and on its HEAD version, and reports only
    findings that are new (rule plus message, ignoring line numbers). New files are compared against nothing.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Repo,
    [Parameter(Mandatory)][string[]]$Files
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ('pssa-base-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
try {
    foreach ($f in $Files) {
        $now = @(Invoke-ScriptAnalyzer -Path (Join-Path $Repo $f) -Severity Warning, Error)
        $baseKeys = @()
        $blob = git -C $Repo rev-parse --verify --quiet "HEAD:$f" 2>$null
        if ($blob) {
            $basePath = Join-Path $tmp ([IO.Path]::GetFileName($f))
            $bytes = [System.Text.Encoding]::UTF8.GetBytes(((git -C $Repo show "HEAD:$f") -join "`n"))
            [System.IO.File]::WriteAllBytes($basePath, $bytes)
            $baseKeys = @(Invoke-ScriptAnalyzer -Path $basePath -Severity Warning, Error | ForEach-Object { "$($_.RuleName)|$($_.Message)" })
        }
        $new = @($now | Where-Object { $baseKeys -notcontains "$($_.RuleName)|$($_.Message)" })
        Write-Information ('{0}: now {1}, base {2}, new {3}' -f $f, $now.Count, $baseKeys.Count, $new.Count) -InformationAction Continue
        foreach ($n in $new) { Write-Information ('    NEW {0} L{1}: {2}' -f $n.RuleName, $n.Line, $n.Message) -InformationAction Continue }
    }
}
finally { Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue }
