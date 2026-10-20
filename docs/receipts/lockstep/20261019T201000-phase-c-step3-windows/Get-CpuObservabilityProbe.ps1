#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only probe for Phase C step 3 (OMAR-VICE# brief 2026-10-19T19:52): which observation fields can an external
    link-time wrapper reach in the built xvic and x64sc CPU cores of the separate materialization? Records, per object,
    nm's undefined symbols (calls and data that leave the object: the only targets of ld --wrap) and defined symbols,
    with object and binary hashes, into probe/. Classifies each Phase C step 1 field by the symbol evidence.
    Writes nothing outside this receipt folder; touches neither native/vice nor the materialization.
#>
[CmdletBinding()]
param(
    [string]$Materialization = 'F:\GitHub\vice-sharp\artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source\vice\src',
    [string]$Nm = 'C:\msys64\mingw64\bin\nm.exe'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$probe = Join-Path $PSScriptRoot 'probe'
New-Item -ItemType Directory -Force -Path $probe | Out-Null
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("nm: $((& $Nm --version | Select-Object -First 1))")
$symbols = @{}
foreach ($o in 'vic20\vic20cpu.o', 'c64\c64cpusc.o', 'vic20\vic-cycle.o', 'viciisc\vicii-cycle.o') {
    $path = Join-Path $Materialization $o
    $leaf = ($o -replace '\\', '-') -replace '\.o$', ''
    $undef = @(& $Nm -u $path | ForEach-Object { ($_ -split '\s+')[-1] })
    $def = @(& $Nm --defined-only $path)
    [System.IO.File]::WriteAllLines((Join-Path $probe "$leaf.undefined.txt"), [string[]]$undef)
    [System.IO.File]::WriteAllLines((Join-Path $probe "$leaf.defined.txt"), [string[]]$def)
    $symbols[$o] = [pscustomobject]@{ Undefined = $undef; Defined = @($def | ForEach-Object { ($_ -split '\s+')[-1] }) }
    $lines.Add(('{0}: sha256 {1}, {2} undefined, {3} defined' -f $o, (Get-FileHash -LiteralPath $path).Hash, $undef.Count, $def.Count))
}
foreach ($b in 'xvic.exe', 'x64sc.exe') { $lines.Add(('{0}: sha256 {1}' -f $b, (Get-FileHash -LiteralPath (Join-Path $Materialization $b)).Hash)) }

function Test-Undef([string]$Obj, [string]$Name) { $symbols[$Obj].Undefined -contains $Name }
function Test-Def([string]$Obj, [string]$Name) { $symbols[$Obj].Defined -contains $Name }
$rows = foreach ($cpu in 'vic20\vic20cpu.o', 'c64\c64cpusc.o') {
    $hook = if ($cpu -like 'vic20*') { 'vic_cycle' } else { 'vicii_cycle' }
    [ordered]@{
        cpu = $cpu
        perCycleHookCalledFromCpu = "$hook=$(Test-Undef $cpu $hook)"
        clockGlobal = "maincpu_clk=$(Test-Def $cpu 'maincpu_clk')"
        interruptStatusGlobal = "maincpu_int_status=$(Test-Def $cpu 'maincpu_int_status')"
        baLowFlagsGlobal = "maincpu_ba_low_flags=$(Test-Def $cpu 'maincpu_ba_low_flags')"
        stealCyclesCall = "vicii_steal_cycles=$(Test-Undef $cpu 'vicii_steal_cycles')"
        memoryDispatchIsDataTables = '_mem_read_tab_ptr={0} _mem_write_tab_ptr={1} dummy-read={2} dummy-write={3}' -f (Test-Undef $cpu '_mem_read_tab_ptr'), (Test-Undef $cpu '_mem_write_tab_ptr'), (Test-Undef $cpu '_mem_read_tab_ptr_dummy'), (Test-Undef $cpu '_mem_write_tab_ptr_dummy')
        memmapAccessorsAreLocal = 'memmap_mem_read defined-in-object={0} memmap_mem_store defined-in-object={1}' -f (Test-Def $cpu 'memmap_mem_read'), (Test-Def $cpu 'memmap_mem_store')
        fastFetchBase = "bank_base defined-in-object=$(Test-Def $cpu 'bank_base'); mem_mmu_translate called=$(Test-Undef $cpu 'mem_mmu_translate')"
    }
}
$rows = @($rows)
foreach ($r in $rows) { $lines.Add(''); foreach ($k in $r.Keys) { $lines.Add("  $k : $($r[$k])") } }
[System.IO.File]::WriteAllLines((Join-Path $probe 'classification.txt'), [string[]]$lines)
$lines
