$ErrorActionPreference = 'SilentlyContinue'
$log = Join-Path $PSScriptRoot 'wolf64.stdout.log'
$diag = Join-Path $PSScriptRoot 'watch-wolf64.diag.log'
function Write-Diag([string]$msg) {
    Add-Content -LiteralPath $diag -Value ("{0:o} {1}" -f [DateTime]::UtcNow, $msg)
}
Write-Diag 'watch start'
while ($true) {
    Start-Sleep -Seconds 30
    if (-not (Test-Path -LiteralPath $log)) {
        Write-Diag 'log missing'
        continue
    }
    $text = [IO.File]::ReadAllText($log)
    Write-Diag ("logLen={0}" -f $text.Length)
    if ($text -match 'Failed!') {
        Write-Output 'FAILED'
        exit 1
    }
    if ($text -match 'Passed!' -and $text -match 'EXIT=0') {
        Write-Output 'DONE'
        exit 0
    }
    if ($text -match 'EXIT=(\d+)' -and $Matches[1] -ne '0') {
        Write-Output 'FAILED'
        exit 1
    }
}
