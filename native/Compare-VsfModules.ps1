#Requires -Version 7.0
<#
.SYNOPSIS
Compares two VICE snapshot files module by module using the pinned snapshot.c framing (19-byte magic, two version
bytes, 16-byte machine name, optional 21-byte VICE version block, then 22-byte module headers with an inclusive
little-endian length). Prints, per module, equal or the first differing byte offset inside the module payload
and the count of differing bytes. Read only.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Left,
    [Parameter(Mandatory)][string] $Right
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Read-Modules {
    param([Parameter(Mandatory)][string] $Path)
    $bytes = [IO.File]::ReadAllBytes($Path)
    $offset = 37
    if ($bytes.Length -ge 50 -and ([Text.Encoding]::ASCII.GetString($bytes, 37, 12)) -eq 'VICE Version') { $offset = 58 }
    $modules = [ordered]@{}
    while ($offset -lt $bytes.Length) {
        $name = ([Text.Encoding]::ASCII.GetString($bytes, $offset, 16)).TrimEnd([char]0)
        $size = [BitConverter]::ToUInt32($bytes, $offset + 18)
        $modules[$name] = [pscustomobject]@{ name = $name; version = ('{0}.{1}' -f $bytes[$offset + 16], $bytes[$offset + 17]); offset = $offset; length = $size; bytes = $bytes[$offset..($offset + $size - 1)] }
        $offset += $size
    }
    return [pscustomobject]@{ header = $bytes[0..($offset - 1)]; headerBytes = $offset; modules = $modules; length = $bytes.Length }
}

$l = Read-Modules -Path $Left
$r = Read-Modules -Path $Right
Write-Output ('left  ' + $Left + ' (' + $l.length + ' bytes, ' + $l.modules.Count + ' modules)')
Write-Output ('right ' + $Right + ' (' + $r.length + ' bytes, ' + $r.modules.Count + ' modules)')
$names = @($l.modules.Keys) + @($r.modules.Keys | Where-Object { -not $l.modules.Contains($_) })
foreach ($n in $names) {
    if (-not $l.modules.Contains($n)) { Write-Output ('  ' + $n.PadRight(16) + ' only in right'); continue }
    if (-not $r.modules.Contains($n)) { Write-Output ('  ' + $n.PadRight(16) + ' only in left'); continue }
    $a = $l.modules[$n]; $b = $r.modules[$n]
    if ($a.length -ne $b.length) { Write-Output ('  ' + $n.PadRight(16) + ' v' + $a.version + ' length differs: ' + $a.length + ' vs ' + $b.length); continue }
    $first = -1; $count = 0
    for ($i = 22; $i -lt $a.length; $i++) { if ($a.bytes[$i] -ne $b.bytes[$i]) { $count++; if ($first -lt 0) { $first = $i } } }
    if ($count -eq 0) { Write-Output ('  ' + $n.PadRight(16) + ' v' + $a.version + ' equal (' + $a.length + ' bytes)') }
    else { Write-Output ('  ' + $n.PadRight(16) + ' v' + $a.version + ' DIFFERS: ' + $count + ' bytes, first at payload offset ' + ($first - 22) + ' (module byte ' + $first + '), left ' + $a.bytes[$first] + ' right ' + $b.bytes[$first]) }
}
