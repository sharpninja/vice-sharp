[CmdletBinding(SupportsShouldProcess)]
param([string[]]$Path)
$ErrorActionPreference = 'Stop'
Import-Module PSScriptAnalyzer -ErrorAction Stop
$reviewScripts = @($PSCommandPath) + @($Path)
foreach ($reviewScript in ($reviewScripts | Select-Object -Unique)) {
    $parseTokens = $null
    $parseErrors = $null
    $null = [System.Management.Automation.Language.Parser]::ParseFile($reviewScript, [ref]$parseTokens, [ref]$parseErrors)
    if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
    $analysisResults = @(Invoke-ScriptAnalyzer -Path $reviewScript -Severity Warning, Error)
    $analysisResults | Format-List
    if ($analysisResults.Count -gt 0) { throw ('Analyzer findings: ' + $reviewScript) }
    Write-Output ('ANALYZER PASS: ' + $reviewScript)
}
