[CmdletBinding(SupportsShouldProcess)]
param(
    [string[]]$Path,
    [int]$Skip = 0,
    [int]$Take = 0,
    [string]$Message,
    [int]$CompletionPercent = -1,
    [Nullable[int]]$EtaSeconds = $null,
    [switch]$List
)

$ErrorActionPreference = 'Stop'
$parseTokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning, Error)
if ($analysis.Count -gt 0) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'

if ($Message) { Write-Output ((Get-Date -Format s) + ' - ' + $Message) }
if ($CompletionPercent -ge 0) {
    [ordered]@{timestampLocal=(Get-Date -Format s); completionPercent=$CompletionPercent; etaSeconds=$EtaSeconds} | ConvertTo-Json -Compress | Write-Output
}
foreach ($evidencePath in $Path) {
    if ($evidencePath -match '(?i)AGENTS-README-FIRST|(?:^|[/\\])\.creds(?:[/\\]|$)|api[-_]?key|credentials|secrets') {
        throw 'Refused a forbidden credential or marker path.'
    }
    if ($PSCmdlet.ShouldProcess($evidencePath, 'Read review evidence')) {
        if ($List) {
            Get-ChildItem -LiteralPath $evidencePath | Select-Object Name, Length, Mode | Format-List | Out-String | Write-Output
        } else {
            Write-Output ('FILE: ' + $evidencePath)
            $lineIndex = 0
            Get-Content -LiteralPath $evidencePath | ForEach-Object {
                $lineIndex++
                if ($lineIndex -gt $Skip -and ($Take -eq 0 -or $lineIndex -le ($Skip + $Take))) {
                    Write-Output ('{0,5}: {1}' -f $lineIndex, $_)
                }
            }
        }
    }
}
