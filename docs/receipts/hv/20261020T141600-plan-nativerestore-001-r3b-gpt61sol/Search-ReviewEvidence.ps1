[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$Pattern,
    [Parameter(Mandatory)][string[]]$Path,
    [int]$Context = 0,
    [switch]$FilesOnly
)
$ErrorActionPreference = 'Stop'
$parseTokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning, Error -WhatIf:$false)
if ($analysis.Count -gt 0) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'
foreach ($searchPath in $Path) {
    if ($searchPath -match '(?i)AGENTS-README-FIRST|(?:^|[/\\])\.creds(?:[/\\]|$)|api[-_]?key|credentials|secrets') { throw 'Forbidden search path.' }
}
if ($PSCmdlet.ShouldProcess(($Path -join ', '), 'Search read-only evidence')) {
    $rgArguments = @('--line-number','--hidden','--glob','!AGENTS-README-FIRST.yaml','--glob','!**/.git/**','--glob','!**/.creds/**','--glob','!**/plugin-cache/**','--glob','!**/response.jsonl','--glob','!**/request.jsonl','--context', [string]$Context)
    if ($FilesOnly) { $rgArguments += '--files-with-matches' }
    & rg @rgArguments -- $Pattern @Path
    if ($LASTEXITCODE -gt 1) { throw ('rg failed: ' + $LASTEXITCODE) }
}
