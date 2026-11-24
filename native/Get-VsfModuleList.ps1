#Requires -Version 7.0
<#
.SYNOPSIS
Lists the ordered module entries (name, major, minor, inclusive length, offset) of one or more VICE snapshot files
using the pinned snapshot.c framing (19-byte magic, two version bytes, 16-byte machine name, optional 21-byte VICE
version block, then 22-byte module headers with a little-endian inclusive length). Read only; prints and optionally
writes JSON.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]] $Path,
    [string] $OutPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Path = @($Path | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$results = @()
foreach ($p in $Path) {
    $bytes = [IO.File]::ReadAllBytes($p)
    $magic = [Text.Encoding]::ASCII.GetString($bytes, 0, 18)
    if ($magic -ne 'VICE Snapshot File') { throw "not a VSF: $p" }
    $major = $bytes[19]; $minor = $bytes[20]
    $machine = ([Text.Encoding]::ASCII.GetString($bytes, 21, 16)).TrimEnd([char]0)
    $offset = 37
    if ($bytes.Length -ge 50 -and ([Text.Encoding]::ASCII.GetString($bytes, 37, 12)) -eq 'VICE Version') { $offset = 58 }
    $modules = @()
    $index = 0
    $headerBytes = $offset
    while ($offset -lt $bytes.Length) {
        $name = ([Text.Encoding]::ASCII.GetString($bytes, $offset, 16)).TrimEnd([char]0)
        $mMajor = $bytes[$offset + 16]; $mMinor = $bytes[$offset + 17]
        $size = [BitConverter]::ToUInt32($bytes, $offset + 18)
        $modules += [pscustomobject]@{ index = $index; name = $name; major = $mMajor; minor = $mMinor; length = $size; offset = $offset }
        $offset += $size; $index++
    }
    $results += [pscustomobject]@{ path = $p; containerMajor = $major; containerMinor = $minor; machine = $machine; headerBytes = $headerBytes; modules = $modules }
    Write-Output ('== ' + $p + ' (' + $machine + ' ' + $major + '.' + $minor + ', ' + $modules.Count + ' modules)')
    foreach ($m in $modules) { Write-Output ('  ' + $m.index.ToString().PadLeft(2) + ' ' + $m.name.PadRight(16) + ' v' + $m.major + '.' + $m.minor + ' len ' + $m.length) }
}
if ($OutPath) { $results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $OutPath -Encoding utf8 }
