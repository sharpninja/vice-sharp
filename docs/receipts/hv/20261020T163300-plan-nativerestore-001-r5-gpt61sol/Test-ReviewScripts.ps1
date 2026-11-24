[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess($PSScriptRoot, 'Parse and analyze review scripts')) { return }
$scripts = @(Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter '*.ps1')
$parseFindings = [Collections.Generic.List[object]]::new()
foreach ($scriptFile in $scripts) {
    $tokens = $null
    $errors = $null
    [void][Management.Automation.Language.Parser]::ParseFile($scriptFile.FullName, [ref]$tokens, [ref]$errors)
    foreach ($parseError in @($errors)) {
        if ($null -ne $parseError) { $parseFindings.Add([pscustomobject]@{ file = $scriptFile.Name; message = $parseError.Message }) }
    }
}
$analysis = @(Invoke-ScriptAnalyzer -Path $PSScriptRoot -Recurse -Severity Warning, Error)
$report = [ordered]@{ timestampLocal = (Get-Date -Format s); scripts = @($scripts.Name); parserErrorCount = $parseFindings.Count; analyzerFindingCount = $analysis.Count; parserErrors = @($parseFindings.ToArray()); analyzerFindings = @($analysis | Select-Object RuleName, Message, ScriptName, Line, Severity) }
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'script-validation.json')
$report | ConvertTo-Json -Depth 10
if ($parseFindings.Count -or $analysis.Count) { throw 'Script validation failed' }
