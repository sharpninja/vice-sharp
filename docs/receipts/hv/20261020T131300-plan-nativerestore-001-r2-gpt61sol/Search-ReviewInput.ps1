[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string] $Pattern,
    [Parameter(Mandatory)][string[]] $SearchPath,
    [switch] $FilesOnly,
    [int] $Context = 0
)
$ErrorActionPreference = 'Stop'
$rgArguments = @('--glob','!AGENTS-README-FIRST.yaml','--glob','!**/session-state.yaml','--glob','!**/*api-key*','--glob','!**/*apikey*','--glob','!**/settings.json','--glob','!**/config.toml','--glob','!**/response.jsonl')
if ($FilesOnly) { $rgArguments += '-l' } else { $rgArguments += @('-n','-C',$Context) }
$rgArguments += @('--',$Pattern)
$rgArguments += $SearchPath
& rg @rgArguments
if ($LASTEXITCODE -gt 1) { throw ('rg failed: ' + $LASTEXITCODE) }
Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffzzz'
