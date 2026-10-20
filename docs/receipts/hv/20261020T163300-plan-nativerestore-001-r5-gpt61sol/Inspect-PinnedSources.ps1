[CmdletBinding(SupportsShouldProcess)]
param([string]$ReceiptSuffix = '')
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess('Read-only materialized source and immutable Git blobs', 'Hash verification')) { return }
$sourceRoot = 'artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source/vice/src'
$files = @('interrupt.h', 'interrupt.c', 'attach.c', '6510core.c', '6510dtvcore.c', 'machine.h', 'autostart.c', 'c64/c64-snapshot.c', 'vic20/vic20-snapshot.c', 'mainc64cpu.c', 'mainviccpu.c', 'mos6510.h', 'monitor/monitor.c', 'monitor/mon_parse.y', 'monitor/mon_lex.l', 'initcmdline.c', 'drive/drive-snapshot.c', 'drive/rotation.c', 'drive/drive-resources.c', 'iecbus/iecbus.c', 'resources.h', 'c64/c64.c', 'vic20/vic20.c')
$records = foreach ($relative in $files) {
    $materialPath = Join-Path $sourceRoot $relative
    $expected = git -C native/vice rev-parse ('cc67418cba58e71d8e70fd353e9de47199ad2075:vice/src/' + $relative)
    $actual = git -C native/vice hash-object -- (Join-Path (Get-Location) $materialPath)
    [ordered]@{ path = $relative; expectedBlob = $expected; actualBlob = $actual; matches = ($expected -eq $actual); sha256 = (Get-FileHash -LiteralPath $materialPath).Hash }
}
$records | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('pinned-sources.verified' + $ReceiptSuffix + '.json'))
$records | ConvertTo-Json -Depth 5
