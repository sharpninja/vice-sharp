[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidateSet('Read','Range','Pins','Inventory','Git','Search','Progress','Request','JsonFindings','Names')][string]$Operation,
    [string[]]$Paths = @(),
    [int]$Start = 1,
    [int]$End = 2147483647,
    [string]$Pattern = '',
    [int]$Percent = 0,
    [Nullable[int]]$EtaSeconds = $null
)
$ErrorActionPreference = 'Stop'
$receiptRoot = $PSScriptRoot
$workspaceRoot = '/home/sharpninja/github/vice-sharp'
function Assert-SafeReadPath {
    param([string]$ReadPath)
    if ($ReadPath -match '(?i)AGENTS-README-FIRST|[\\/]\.creds[\\/]|api[-_]?key|credentials|session-state\.yaml') {
        throw 'Credential-bearing or marker path refused.'
    }
}
switch ($Operation) {
    'Read' {
        foreach ($readPath in $Paths) {
            Assert-SafeReadPath $readPath
            Write-Output ('FILE: ' + $readPath)
            Get-Content -LiteralPath $readPath -Raw
        }
    }
    'Range' {
        foreach ($readPath in $Paths) {
            Assert-SafeReadPath $readPath
            Write-Output ('FILE: ' + $readPath)
            $lineNumber = 0
            Get-Content -LiteralPath $readPath | ForEach-Object {
                $lineNumber++
                if ($lineNumber -ge $Start -and $lineNumber -le $End) { '{0}: {1}' -f $lineNumber, $_ }
            }
        }
    }
    'Pins' {
        $pins = Get-Content -LiteralPath (Join-Path $receiptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $results = foreach ($pin in $pins) {
            Assert-SafeReadPath $pin.path
            $actualHash = (Get-FileHash -LiteralPath $pin.path -Algorithm SHA256).Hash
            [pscustomobject]@{ path = $pin.relativePath; expected = $pin.sha256; actual = $actualHash; matches = ($actualHash -eq $pin.sha256); bytes = (Get-Item -LiteralPath $pin.path -Force).Length }
        }
        $results | ConvertTo-Json -Depth 6
        if ($PSCmdlet.ShouldProcess((Join-Path $receiptRoot 'pin-check.json'), 'Save pin verification receipt')) {
            $results | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $receiptRoot 'pin-check.json') -Encoding utf8
        }
    }
    'Inventory' { rg --files @Paths -g $Pattern }
    'Git' {
        & git -C $workspaceRoot @Paths
        if ($LASTEXITCODE -ne 0) { throw "Git returned $LASTEXITCODE" }
    }
    'Search' {
        foreach ($readPath in $Paths) { Assert-SafeReadPath $readPath }
        rg -n -- $Pattern @Paths
        if ($LASTEXITCODE -gt 1) { throw "rg returned $LASTEXITCODE" }
    }
    'Progress' {
        [pscustomobject]@{ timestampLocal = (Get-Date).ToString('s'); completionPercent = $Percent; etaSeconds = $EtaSeconds } | ConvertTo-Json -Compress
    }
    'Request' {
        $request = Get-Content -LiteralPath (Join-Path $receiptRoot 'request.jsonl') -Raw | ConvertFrom-Json
        $request | Select-Object timestampLocal,workspacePath,reviewer,implementer,parentSessionId,parentRequestId,executable,executableSha256,arguments,environment | ConvertTo-Json -Depth 12
    }
    'Names' {
        $request = Get-Content -LiteralPath (Join-Path $receiptRoot 'request.jsonl') -Raw | ConvertFrom-Json
        $request.PSObject.Properties.Name
        Get-Command strace -ErrorAction SilentlyContinue | Select-Object Name,Source
    }
    'JsonFindings' {
        foreach ($readPath in $Paths) {
            Assert-SafeReadPath $readPath
            $verdict = Get-Content -LiteralPath $readPath -Raw | ConvertFrom-Json
            if ($Start -eq 1) { $verdict | Select-Object * -ExcludeProperty findings | ConvertTo-Json -Depth 15 }
            $verdict.findings | Select-Object -Skip ($Start - 1) -First ($End - $Start + 1) | ConvertTo-Json -Depth 18
        }
    }
}
