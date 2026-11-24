[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
$workspaceRoot = '/home/sharpninja/github/vice-sharp'
$nativeRoot = Join-Path $workspaceRoot 'native/vice'
$pinSha = 'cc67418cba58e71d8e70fd353e9de47199ad2075'
$materialRoot = Join-Path $workspaceRoot ('artifacts/immutable-vice/' + $pinSha + '/source/vice/src')
$relativePaths = @('interrupt.h','interrupt.c','6510core.c','6510dtvcore.c','machine.h','autostart.c','c64/c64-snapshot.c','vic20/vic20-snapshot.c','c64/c64.c','vic20/vic20.c','c64/c64cpusc.c','vic20/vic20cpu.c','mainc64cpu.c','mainviccpu.c','mos6510.h','monitor/monitor.c','monitor/mon_parse.y','monitor/mon_lex.l','initcmdline.c','drive/drive-snapshot.c','drive/rotation.c','drive/drive-resources.c','iecbus/iecbus.c','resources.h')
$results = foreach ($relativePath in $relativePaths) {
    $fullPath = Join-Path $materialRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) { throw ('Missing expected source: ' + $relativePath) }
    $blob = [string](& git -C $nativeRoot rev-parse ($pinSha + ':vice/src/' + $relativePath))
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve pinned blob' }
    $actual = [string](& git -C $nativeRoot hash-object -- $fullPath)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to hash read-only source' }
    [pscustomobject]@{ path = $relativePath; pinnedBlob = $blob; actualBlob = $actual; matches = ($actual -ceq $blob); sha256 = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash }
}
$result = [pscustomobject]@{ timestampLocal = (Get-Date).ToString('s'); pin = $pinSha; matches = @($results | Where-Object { $_.matches }).Count; mismatches = @($results | Where-Object { -not $_.matches }).Count; files = @($results) }
if ($PSCmdlet.ShouldProcess($PSScriptRoot, 'Save read-only source pin comparison')) { $result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'source-pins.json') -Encoding utf8 }
$result | Select-Object timestampLocal,pin,matches,mismatches | ConvertTo-Json
if ($result.mismatches -ne 0) { throw 'Source pin drift' }
