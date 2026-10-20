[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$Pattern, [Parameter(Mandatory)][string[]]$Path, [string[]]$Glob = @(), [switch]$FilesOnly)
$ErrorActionPreference = 'Stop'
if ($Path -match 'AGENTS-README-FIRST.yaml|/\.creds/') { throw 'Credential-bearing path refused.' }
$rgArguments = @('--glob', '!AGENTS-README-FIRST.yaml', '--glob', '!**/.creds/**')
foreach ($reviewGlob in $Glob) { $rgArguments += @('--glob', $reviewGlob) }
$rgArguments += $(if ($FilesOnly) { '-l' } else { '-n' })
$rgArguments += @('--', $Pattern)
$rgArguments += $Path
if ($PSCmdlet.ShouldProcess(($Path -join ', '), 'Search scoped review evidence')) {
    $searchOutput = @(& rg @rgArguments)
    if ($LASTEXITCODE -gt 1) { throw ('rg exit: ' + $LASTEXITCODE) }
    if (($searchOutput -join "`n") -match '(?i)(?:Bearer\s+[a-z0-9._-]{16,}|sk-[a-z0-9_-]{16,})') { throw 'Possible credential output refused.' }
    $searchOutput
}
