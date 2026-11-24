[CmdletBinding(SupportsShouldProcess)]
param([string] $FixtureRoot = '/home/sharpninja/github/vice-sharp/tests/ViceSharp.TestHarness/Fixtures/Vsf')
$ErrorActionPreference = 'Stop'
$results = foreach ($fixture in Get-ChildItem -LiteralPath $FixtureRoot -Filter '*.vsf' -File -Force -Recurse) {
    $bytes = [IO.File]::ReadAllBytes($fixture.FullName)
    if ($bytes.Length -lt 37 -or [Text.Encoding]::ASCII.GetString($bytes,0,19) -ne "VICE Snapshot File$([char]26)") { throw ('Bad VSF header: ' + $fixture.Name) }
    $offset = 37
    if ($bytes.Length -ge 58 -and [Text.Encoding]::ASCII.GetString($bytes,37,13) -eq "VICE Version$([char]26)") { $offset = 58 }
    $modules = @()
    while ($offset -lt $bytes.Length) {
        if ($bytes.Length - $offset -lt 22) { throw 'Truncated VSF module.' }
        $length = [BitConverter]::ToUInt32($bytes,$offset+18)
        if ($length -lt 22 -or $length -gt $bytes.Length - $offset) { throw 'Invalid VSF module length.' }
        $name = [Text.Encoding]::ASCII.GetString($bytes,$offset,16).TrimEnd([char]0)
        $module = [ordered]@{name=$name;major=$bytes[$offset+16];minor=$bytes[$offset+17];length=$length;offset=$offset}
        if ($name -eq 'MAINCPU' -and $length -ge 37) {
            $module.clock=[BitConverter]::ToUInt64($bytes,$offset+22)
            $module.pc=[BitConverter]::ToUInt16($bytes,$offset+34)
        }
        if ($name -match '^DRIVE\d+$' -and $length -ge 24) {
            $module.hasTde=[int]$bytes[$offset+22]
            $module.hasDrives=[int]$bytes[$offset+23]
        }
        if ($name -eq 'MAINCPU' -or $name -match '^DRIVE\d+$') { $modules += [pscustomobject]$module }
        $offset += [int]$length
    }
    [pscustomobject]@{path=$fixture.FullName;sha256=(Get-FileHash -LiteralPath $fixture.FullName -Algorithm SHA256).Hash;bytes=$bytes.Length;modules=$modules}
}
if ($PSCmdlet.ShouldProcess('vsf-fixture-inspection.json','Write read-only fixture inspection')) { $results | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'vsf-fixture-inspection.json') -Encoding utf8 }
$results | ConvertTo-Json -Depth 10
Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffzzz'
