#Requires -Version 7.0
<#
.SYNOPSIS
PLAN-NATIVERESTORE-001 D0 fixture scan: lists the modules of a VICE snapshot (.vsf), reads the MAINCPU register
fields and every DRIVE8 to DRIVE11 payload header, and computes the R10 native-launch precondition.

.DESCRIPTION
Container layout of the pinned VICE (snapshot.c): 19-byte magic "VICE Snapshot File\x1a", one major and one minor
byte, a 16-byte machine name, then the 21-byte version block ("VICE Version\x1a", 4 version bytes, 4 revision bytes),
then modules, each with a 16-byte name, one major and one minor byte and a little-endian uint32 size that includes
the 22-byte module header. The MAINCPU payload starts with the 64-bit clock, then A, X, Y, SP, the 16-bit PC and the
status byte (mainc64cpu.c maincpu_snapshot_write_module). A DRIVE<n> payload starts with has_tde and has_drives
(drive-snapshot.c lines 139 to 162). The precondition is "-drive<n>type 0" for every unit whose module is missing
or has has_tde 0 (section 1 of the plan: the pinned reader would otherwise copy uninitialized clocks into that unit).
A DRIVE payload shorter than two bytes is invalid, the result is not valid, and no precondition is emitted (N05).
Read only.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Path
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$bytes = [IO.File]::ReadAllBytes($Path)
$magic = [Text.Encoding]::ASCII.GetString($bytes, 0, 18)
if ($bytes.Length -lt 58 -or $magic -cne 'VICE Snapshot File' -or $bytes[18] -ne 0x1a) { throw "Not a VICE snapshot (magic mismatch): $Path" }
$machine = [Text.Encoding]::ASCII.GetString($bytes, 21, 16).TrimEnd([char]0)
$versionMagic = [Text.Encoding]::ASCII.GetString($bytes, 37, 12)
if ($versionMagic -cne 'VICE Version' -or $bytes[49] -ne 0x1a) { throw "Not a VICE snapshot (version block magic mismatch): $Path" }

$modules = [System.Collections.Generic.List[object]]::new()
$offset = 58
while ($offset -lt $bytes.Length) {
    if ($offset + 22 -gt $bytes.Length) { throw "Module header at offset $offset runs past the end of the file." }
    $name = [Text.Encoding]::ASCII.GetString($bytes, $offset, 16).TrimEnd([char]0)
    $major = [int]$bytes[$offset + 16]
    $minor = [int]$bytes[$offset + 17]
    $size = [BitConverter]::ToUInt32($bytes, $offset + 18)
    if ($size -lt 22 -or $offset + $size -gt $bytes.Length) { throw "Module '$name' at offset $offset declares size $size which runs past the end of the file." }
    $modules.Add([ordered]@{ name = $name; major = $major; minor = $minor; offset = $offset; length = [int]$size; payloadOffset = $offset + 22; payloadLength = [int]$size - 22 })
    $offset += [int]$size
}

$mainCpu = $null
$mainModule = $modules | Where-Object name -eq 'MAINCPU' | Select-Object -First 1
if ($mainModule -and $mainModule.payloadLength -ge 15) {
    $p = $mainModule.payloadOffset
    $mainCpu = [ordered]@{
        major = $mainModule.major; minor = $mainModule.minor; offset = $mainModule.offset; length = $mainModule.length
        clock = [BitConverter]::ToUInt64($bytes, $p)
        a = [int]$bytes[$p + 8]; x = [int]$bytes[$p + 9]; y = [int]$bytes[$p + 10]; sp = [int]$bytes[$p + 11]
        pc = [int][BitConverter]::ToUInt16($bytes, $p + 12)
        p = [int]$bytes[$p + 14]
    }
}

$drives = [System.Collections.Generic.List[object]]::new()
$invalid = [System.Collections.Generic.List[object]]::new()
foreach ($unit in 8..11) {
    $m = $modules | Where-Object name -eq "DRIVE$unit" | Select-Object -First 1
    if (-not $m) {
        $drives.Add([ordered]@{ unit = $unit; present = $false; hasTde = $null; hasDrives = $null; offset = $null; length = 0; needsTypeNone = $true })
        continue
    }
    if ($m.payloadLength -lt 2) {
        $invalid.Add([ordered]@{ unit = $unit; offset = $m.offset; length = $m.length; reason = 'DRIVE payload shorter than two bytes' })
        $drives.Add([ordered]@{ unit = $unit; present = $true; hasTde = $null; hasDrives = $null; offset = $m.offset; length = $m.length; needsTypeNone = $null })
        continue
    }
    $hasTde = [int]$bytes[$m.payloadOffset]
    $hasDrives = [int]$bytes[$m.payloadOffset + 1]
    $drives.Add([ordered]@{ unit = $unit; present = $true; hasTde = $hasTde; hasDrives = $hasDrives; offset = $m.offset; length = $m.length; needsTypeNone = ($hasTde -eq 0) })
}

$valid = ($invalid.Count -eq 0) -and ($null -ne $mainCpu)
$precondition = @()
if ($valid) {
    foreach ($d in $drives) { if ($d.needsTypeNone) { $precondition += @("-drive$($d.unit)type", '0') } }
}

[pscustomobject]@{
    path = (Resolve-Path -LiteralPath $Path).Path
    sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    bytes = $bytes.Length
    machine = $machine
    modules = @($modules)
    mainCpu = $mainCpu
    drives = @($drives)
    invalid = @($invalid)
    valid = $valid
    precondition = $precondition
}
