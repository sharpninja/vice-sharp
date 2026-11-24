[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Read', 'Pins', 'Git', 'Search', 'Progress', 'List')]
    [string]$Operation = 'Read',
    [string[]]$Paths,
    [int]$StartLine = 1,
    [int]$LineCount = 2147483647,
    [string[]]$NativeArguments,
    [string]$WorkingPath = '/home/sharpninja/github/vice-sharp',
    [int]$CompletionPercent = 0,
    [Nullable[int]]$EtaSeconds = $null
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess($WorkingPath, "Read-only review: $Operation")) { return }
switch ($Operation) {
    'Progress' {
        [ordered]@{
            timestampLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz')
            completionPercent = $CompletionPercent
            etaSeconds = $EtaSeconds
        } | ConvertTo-Json -Compress
    }
    'Read' {
        foreach ($reviewPath in $Paths) {
            $reviewLines = [IO.File]::ReadAllLines($reviewPath)
            Write-Output ("FILE: {0} ({1} lines)" -f $reviewPath, $reviewLines.Length)
            $endLine = [Math]::Min([long]$reviewLines.Length, [long]$StartLine - 1 + $LineCount)
            for ($i = $StartLine - 1; $i -lt $endLine; $i++) {
                '{0}: {1}' -f ($i + 1), $reviewLines[$i]
            }
        }
    }
    'Pins' {
        $pins = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        foreach ($pin in $pins) {
            $liveHash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
            $liveBytes = (Get-Item -LiteralPath $pin.path -Force).Length
            [ordered]@{
                relativePath = $pin.relativePath
                expectedSha256 = $pin.sha256
                actualSha256 = $liveHash
                expectedBytes = $pin.bytes
                actualBytes = $liveBytes
                matches = ($liveHash -eq $pin.sha256 -and $liveBytes -eq $pin.bytes)
            } | ConvertTo-Json -Compress
        }
    }
    'Git' {
        $env:GIT_OPTIONAL_LOCKS = '0'
        & git -C $WorkingPath @NativeArguments
        if ($LASTEXITCODE -ne 0) { throw "git returned $LASTEXITCODE" }
    }
    'Search' {
        & rg @NativeArguments
        if ($LASTEXITCODE -gt 1) { throw "rg returned $LASTEXITCODE" }
    }
    'List' {
        foreach ($reviewPath in $Paths) {
            Get-ChildItem -LiteralPath $reviewPath -Force | Select-Object Mode, Name, Length
        }
    }
}
