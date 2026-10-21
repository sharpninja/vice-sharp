#Requires -Version 7.0
<#
.SYNOPSIS
PLAN-VSFLOCKSTEP-001 Slice 1, PLAN-NATIVERESTORE-001 D5: writes docs/requirements/oracles/vice-vsf-state-transfer-manifest.json
(schema 1) from native objects: the pinned VICE commit, the container framing, the ROM allowlist, the supplied x64sc
fixture entry (with its ordered module list read from the file), the blocked xvic fixture entries from the committed
candidate receipts, and one entry per reader unit of the pinned machine read orders (c64-snapshot.c 121 to 136,
vic20-snapshot.c 129 to 140), each with the VSF modules the unit consumes (name and version from the pinned
writers), state categories, native export and import functions, managed record and adapter (the Slice 1 adapter
where one exists, otherwise the owning later slice named explicitly), mandatory or optional classification, source
anchor and test ids. Supports -WhatIf.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [string] $OutPath = '',
    [string] $CandidateReceipts = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $OutPath) { $OutPath = [IO.Path]::Combine($RepoRoot, 'docs', 'requirements', 'oracles', 'vice-vsf-state-transfer-manifest.json') }
$sha = 'cc67418cba58e71d8e70fd353e9de47199ad2075'

$allowlist = [ordered]@{
    'C64/basic-901226-01.bin' = '89878CEA0A268734696DE11C4BAE593EAAA506465D2029D619C0E0CBCCDFA62D'
    'C64/kernal-901227-03.bin' = '83C60D47047D7BEAB8E5B7BF6F67F80DAA088B7A6A27DE0D7E016F6484042721'
    'C64/chargen-901225-01.bin' = 'FD0D53B8480E86163AC98998976C72CC58D5DD8EB824ED7B829774E74213B420'
    'DRIVES/dos1541-325302-01+901229-05.bin' = 'D1D45AFB46FD4E2B48D93CA367B889D75654A3B7ACF73044E51F6D880C09369E'
    'VIC20/basic-901486-01.bin' = '8BE2D882E0EEB1C6923A43660236EA69D43CA8A3BD0613B1B6C910601584FBED'
    'VIC20/kernal.901486-07.bin' = 'FF6D1416B91A71F5240A5BB8ADC9FDC1B955374C48322CA47B3E436334F168FB'
    'VIC20/kernal.901486-06.bin' = '8FC94BD895F488AC0AE1A52ABDC32988CD63C777770B89A9D415513098535CC2'
    'VIC20/chargen-901460-03.bin' = 'BDA280726CFE069CA70EF5E2F2D95A29F69D3950C859A12E6979464963FCF502'
    'DRIVES/dos1540-325302+3-01.bin' = 'A966E3FF6FD2DB7B8F6D57E4D105C589A92E88A6705201215604AC864CC3C27F'
}

function Get-ModuleList {
    param([Parameter(Mandatory)][string] $Path)
    $bytes = [IO.File]::ReadAllBytes($Path)
    $offset = 37
    $variant = 'legacy-37'
    if ($bytes.Length -ge 50 -and ([Text.Encoding]::ASCII.GetString($bytes, 37, 12)) -eq 'VICE Version') { $offset = 58; $variant = 'current-58' }
    $list = @()
    while ($offset -lt $bytes.Length) {
        $name = ([Text.Encoding]::ASCII.GetString($bytes, $offset, 16)).TrimEnd([char]0)
        $size = [BitConverter]::ToUInt32($bytes, $offset + 18)
        $list += ('{0}:{1}.{2}:{3}' -f $name, $bytes[$offset + 16], $bytes[$offset + 17], $size)
        $offset += $size
    }
    return [pscustomobject]@{ variant = $variant; modules = $list }
}

# Row layout: name, version, restoreOrder, snapshotModules, categories, nativeExport, nativeImport, managedRecord, managedAdapter, mandatory, sourceAnchor, testIds.
function ConvertTo-UnitEntries {
    param([Parameter(Mandatory)][string] $Machine, [Parameter(Mandatory)][object[]] $Rows)
    $entries = @()
    foreach ($r in $Rows) {
        $entries += [ordered]@{
            machine = $Machine; name = $r[0]; version = $r[1]; restoreOrder = $r[2]; snapshotModules = @($r[3]); categories = @($r[4])
            nativeExport = $r[5]; nativeImport = $r[6]; managedRecord = $r[7]; managedAdapter = $r[8]
            mandatory = $r[9]; sourceAnchor = $r[10]; testIds = @($r[11])
        }
    }
    return $entries
}

$slice2 = 'pending: PLAN-VSFLOCKSTEP-001 Slice 2 (C64 adapter, IViceSnapshotMachineAdapter) owns the managed record'
$slice2Adapter = 'pending: PLAN-VSFLOCKSTEP-001 Slice 2 (C64 adapter)'
$slice4 = 'pending: PLAN-VSFLOCKSTEP-001 Slice 4 (VIC-20 adapter) owns the managed record'
$slice4Adapter = 'pending: PLAN-VSFLOCKSTEP-001 Slice 4 (VIC-20 adapter)'
$manifestTest = 'ViceSnapshotStateTransferManifestTests.Manifest_CoversEveryDeterministicModule'
$resumeTest = 'NativeViceSnapshotResumeTests.SnapshotResume_ObserverClockAndPcMatchVsfMainCpu'
$mainCpuRecord = 'ViceSharp.ViceCompatibility.X64ScNativeLaunch.MainCpuFields (clock, A, X, Y, SP, PC, status)'
$mainCpuAdapter = 'ViceSharp.ViceCompatibility.X64ScSnapshotCompatibility.Normalize (v1.2 and v1.3 to the pinned v1.4 layout)'
$driveRecord = 'ViceSharp.ViceCompatibility.X64ScNativeLaunch.DriveUnit (present, has_tde, has_drives)'
$driveAdapter = 'ViceSharp.ViceCompatibility.X64ScNativeLaunch.ComputePrecondition (R10 A launch precondition)'
$driveCats = @('drive type', 'true drive emulation', 'attach and detach clocks', 'drive cpu, via and image modules when enabled')
$driveExport = 'drive_snapshot_write_module (drive-snapshot.c 115; per unit 139 to 162, cpu and via modules 245 to 249)'
$driveImport = 'drive_snapshot_read_module (drive-snapshot.c 309; per unit 333, cpu and via modules 494 to 498, vdrive images 586)'
$driveAnchor = 'drive-snapshot.c 112 to 113, 115, 139 to 162, 245 to 249, 309, 333, 494 to 498, 586'
$viaCats = @('via ports', 'timers', 'shift register', 'interrupt flags')
$viaExport = 'viacore_snapshot_write_module (viacore.c 1948)'
$viaImport = 'viacore_snapshot_read_module (viacore.c 2018)'
$ciaCats = @('cia ports', 'timers', 'tod', 'serial shift register', 'interrupt flags')
$joyportExport = 'joyport_snapshot_write_module (joyport.c 2077, 2082; joystick.c 2740 to 2743)'
$joyportImport = 'joyport_snapshot_read_module (joyport.c 2116, 2126; joystick.c 2762)'
$joyportAnchor = 'joyport.c 2074 to 2075, 2077, 2082, 2116, 2126; joystick.c 2732 to 2733, 2740 to 2743, 2762'
$tapeExport = 'tapeport_snapshot_write_module (tapeport.c 599; datasette.c 1371)'
$tapeImport = 'tapeport_snapshot_read_module (tapeport.c 646, 653; datasette.c 1417)'
$tapeAnchor = 'tapeport.c 595 to 597, 599, 646, 653; datasette.c 1363 to 1364, 1371, 1417'
$drive8Modules = @('DRIVE8:2.0', 'VDRIVEIMAGE8:2.0 (image attached)', 'DRIVECPU0:1.3 (true drive emulation)', '1541VIA1D0:2.2 (true drive emulation)', 'VIA2D0:2.2 (true drive emulation)')
$drive9Modules = @('DRIVE9:2.0', 'VDRIVEIMAGE9:2.0 (image attached)', 'DRIVECPU1:1.3 (true drive emulation)', '1541VIA1D1:2.2 (true drive emulation)', 'VIA2D1:2.2 (true drive emulation)')
$drive10Modules = @('DRIVE10:2.0', 'VDRIVEIMAGE10:2.0 (image attached)', 'DRIVECPU2:1.3 (true drive emulation)', '1541VIA1D2:2.2 (true drive emulation)', 'VIA2D2:2.2 (true drive emulation)')
$drive11Modules = @('DRIVE11:2.0', 'VDRIVEIMAGE11:2.0 (image attached)', 'DRIVECPU3:1.3 (true drive emulation)', '1541VIA1D3:2.2 (true drive emulation)', 'VIA2D3:2.2 (true drive emulation)')

$x64scRows = @(
    , @('MAINCPU', '1.4', 1, @('MAINCPU:1.4'), @('cpu registers', 'clock', 'interrupt status', 'last opcode info', 'ba low flags'), 'maincpu_snapshot_write_module (mainc64cpu.c 887)', 'maincpu_snapshot_read_module (mainc64cpu.c 934)', $mainCpuRecord, $mainCpuAdapter, $true, 'c64-snapshot.c 121; mainc64cpu.c 883 to 885, 887, 934', @('NativeViceSnapshotLegacyMainCpuCompatibilityTests (LegacyMainCpuContractTests, LegacyMainCpuImplementationTests)', $resumeTest, $manifestTest))
    , @('C64', '0.1', 2, @('C64MEM:0.1', 'C64ROM:0.0 (save_roms only)', 'C64CART:0.1'), @('ram', 'processor port', 'memory configuration', 'rom images when saved', 'cartridge port state and type'), 'c64_snapshot_write_module (c64memsnapshot.c 204, cartridge 243)', 'c64_snapshot_read_module (c64memsnapshot.c 246, 254, cartridge 310)', $slice2, $slice2Adapter, $true, 'c64-snapshot.c 122; c64memsnapshot.c 66 to 68, 78, 130, 200 to 202, 204, 243, 246, 254, 310; c64carthooks.c 3285 to 3287, 3289, 3917', @($manifestTest))
    , @('CIA1', '2.5', 3, @('CIA1:2.5'), $ciaCats, 'ciacore_snapshot_write_module (ciacore.c 2221)', 'ciacore_snapshot_read_module (ciacore.c 2357)', $slice2, $slice2Adapter, $true, 'c64-snapshot.c 123; ciacore.c 2146 to 2147, 2221, 2357', @($manifestTest))
    , @('CIA2', '2.5', 4, @('CIA2:2.5'), $ciaCats, 'ciacore_snapshot_write_module (ciacore.c 2221)', 'ciacore_snapshot_read_module (ciacore.c 2357)', $slice2, $slice2Adapter, $true, 'c64-snapshot.c 124; ciacore.c 2146 to 2147, 2221, 2357', @($manifestTest))
    , @('SID', '1.5', 5, @('SID:1.5', 'SIDEXTENDED:1.4'), @('sid registers', 'engine state', 'resid internal state'), 'sid_snapshot_write_module (sid-snapshot.c 1152, 1172, 1176)', 'sid_snapshot_read_module (sid-snapshot.c 1184, 1189, 1193)', $slice2, $slice2Adapter, $true, 'c64-snapshot.c 125; sid-snapshot.c 132, 141 to 142, 935, 939 to 940, 1152, 1184', @($manifestTest))
    , @('DRIVE8', '2.0', 6, $drive8Modules, $driveCats, $driveExport, $driveImport, $driveRecord, $driveAdapter, $true, ('c64-snapshot.c 126; ' + $driveAnchor + ', 540 to 550'), @('NativeLaunchPreconditionContractTests', 'NativeLaunchPreconditionImplementationTests', $manifestTest))
    , @('DRIVE9', '2.0', 7, $drive9Modules, $driveCats, $driveExport, $driveImport, $driveRecord, $driveAdapter, $true, ('c64-snapshot.c 126; ' + $driveAnchor), @('NativeLaunchPreconditionImplementationTests', $manifestTest))
    , @('DRIVE10', '2.0', 8, $drive10Modules, $driveCats, $driveExport, $driveImport, $driveRecord, $driveAdapter, $true, ('c64-snapshot.c 126; ' + $driveAnchor), @('NativeLaunchPreconditionImplementationTests', $manifestTest))
    , @('DRIVE11', '2.0', 9, $drive11Modules, $driveCats, $driveExport, $driveImport, $driveRecord, $driveAdapter, $true, ('c64-snapshot.c 126; ' + $driveAnchor), @('NativeLaunchPreconditionImplementationTests', $manifestTest))
    , @('FSDRIVE', '0.0', 10, @('FSDRIVE:0.0'), @('file system drive state'), 'fsdrive_snapshot_write_module (fsdrive-snapshot.c 52)', 'fsdrive_snapshot_read_module (fsdrive-snapshot.c 79)', $slice2, $slice2Adapter, $true, 'c64-snapshot.c 127; fsdrive-snapshot.c 48 to 50, 52, 79', @($manifestTest))
    , @('VIC-II', '1.3', 11, @('VIC-II:1.3'), @('vic-ii registers', 'raster state', 'bad line and ba state', 'sprite state', 'color ram'), 'vicii_snapshot_write_module (viciisc/vicii-snapshot.c 68)', 'vicii_snapshot_read_module (viciisc/vicii-snapshot.c 166)', $slice2, $slice2Adapter, $true, 'c64-snapshot.c 128; viciisc/vicii-snapshot.c 64 to 66, 68, 166', @($manifestTest))
    , @('C64GLUE', '1.0', 12, @('GLUE:1.0'), @('glue logic state'), 'c64_glue_snapshot_write_module (c64gluelogic.c 182)', 'c64_glue_snapshot_read_module (c64gluelogic.c 206)', $slice2, $slice2Adapter, $true, 'c64-snapshot.c 129; c64gluelogic.c 178 to 180, 182, 206', @($manifestTest))
    , @('EVENT', '0.1', 13, @('EVENT:0.1 (event mode only)'), @('event recording state (event mode only)'), 'event_snapshot_write_module (event.c 1138, 1147)', 'event_snapshot_read_module (event.c 1028)', $slice2, $slice2Adapter, $false, 'c64-snapshot.c 130; event.c 1028, 1138, 1147', @($manifestTest))
    , @('MEMHACKS', '0.0', 14, @('C64MEMHACKS:0.0'), @('memory expansion hack state'), 'memhacks_snapshot_write_modules (c64-memory-hacks.c 172)', 'memhacks_snapshot_read_modules (c64-memory-hacks.c 211)', $slice2, $slice2Adapter, $true, 'c64-snapshot.c 131; c64-memory-hacks.c 168 to 170, 172, 211', @($manifestTest))
    , @('TAPEPORT', '1.0', 15, @('TAPEPORT:1.0', 'DATASETTE:1.5'), @('tape port device selection', 'datasette motor, counter and tape position'), $tapeExport, $tapeImport, $slice2, $slice2Adapter, $true, ('c64-snapshot.c 132; ' + $tapeAnchor), @($manifestTest))
    , @('KEYBOARD', '1.1', 16, @('KEYBOARD:1.1'), @('keyboard matrix'), 'keyboard_snapshot_write_module (keyboard.c 1262)', 'keyboard_snapshot_read_module (keyboard.c 1282)', $slice2, $slice2Adapter, $true, 'c64-snapshot.c 133; keyboard.c 1258 to 1260, 1262, 1282', @($manifestTest))
    , @('JOYPORT1', '0.0', 17, @('JOYPORT0:0.0', 'JOYSTICK0:1.2'), @('joyport 1 device', 'joystick 1 state'), $joyportExport, $joyportImport, $slice2, $slice2Adapter, $true, ('c64-snapshot.c 134; ' + $joyportAnchor), @($manifestTest))
    , @('JOYPORT2', '0.0', 18, @('JOYPORT1:0.0', 'JOYSTICK1:1.2'), @('joyport 2 device', 'joystick 2 state'), $joyportExport, $joyportImport, $slice2, $slice2Adapter, $true, ('c64-snapshot.c 135; ' + $joyportAnchor), @($manifestTest))
    , @('USERPORT', '1.0', 19, @('USERPORT:1.0'), @('userport device selection'), 'userport_snapshot_write_module (userport.c 1090)', 'userport_snapshot_read_module (userport.c 1126)', $slice2, $slice2Adapter, $true, 'c64-snapshot.c 136; userport.c 1086 to 1088, 1090, 1126', @($manifestTest))
)

$xvicRows = @(
    , @('MAINCPU', '1.4', 1, @('MAINCPU:1.4'), @('cpu registers', 'clock', 'interrupt status', 'last opcode info'), 'maincpu_snapshot_write_module (mainviccpu.c 714)', 'maincpu_snapshot_read_module (mainviccpu.c 756)', $slice4, $slice4Adapter, $true, 'vic20-snapshot.c 129; mainviccpu.c 710 to 712, 714, 756', @($manifestTest))
    , @('VIC20', '1.1', 2, @('VIC20MEM:1.1', 'VIC20ROM:1.1 (save_roms only)', 'VIC20CART:2.1'), @('ram', 'memory configuration', 'rom images when saved', 'cartridge port state and type'), 'vic20_snapshot_write_module (vic20memsnapshot.c 322, 97, cartridge 326)', 'vic20_snapshot_read_module (vic20memsnapshot.c 332, 140, cartridge 336)', $slice4, $slice4Adapter, $true, 'vic20-snapshot.c 130; vic20memsnapshot.c 61 to 63, 97, 140, 192, 227, 281, 322, 326, 332, 336; vic20cart.c 1312 to 1314, 1316, 1339, 1499', @($manifestTest))
    , @('VIC-I', '0.4', 3, @('VIC-I:0.4'), @('vic-i registers', 'raster and fetch state', 'video pipeline buffers'), 'vic_snapshot_write_module (vic-snapshot.c 50)', 'vic_snapshot_read_module (vic-snapshot.c 113)', $slice4, $slice4Adapter, $true, 'vic20-snapshot.c 131; vic-snapshot.c 45 to 47, 50, 113', @($manifestTest))
    , @('VIA1', '2.2', 4, @('VIA1:2.2'), $viaCats, $viaExport, $viaImport, $slice4, $slice4Adapter, $true, 'vic20-snapshot.c 132; viacore.c 1908 to 1909, 1948, 2018', @($manifestTest))
    , @('VIA2', '2.2', 5, @('VIA2:2.2'), $viaCats, $viaExport, $viaImport, $slice4, $slice4Adapter, $true, 'vic20-snapshot.c 133; viacore.c 1908 to 1909, 1948, 2018', @($manifestTest))
    , @('DRIVE8', '2.0', 6, $drive8Modules, $driveCats, $driveExport, $driveImport, $driveRecord, $driveAdapter, $true, ('vic20-snapshot.c 134; ' + $driveAnchor), @($manifestTest))
    , @('DRIVE9', '2.0', 7, $drive9Modules, $driveCats, $driveExport, $driveImport, $driveRecord, $driveAdapter, $true, ('vic20-snapshot.c 134; ' + $driveAnchor), @($manifestTest))
    , @('DRIVE10', '2.0', 8, $drive10Modules, $driveCats, $driveExport, $driveImport, $driveRecord, $driveAdapter, $true, ('vic20-snapshot.c 134; ' + $driveAnchor), @($manifestTest))
    , @('DRIVE11', '2.0', 9, $drive11Modules, $driveCats, $driveExport, $driveImport, $driveRecord, $driveAdapter, $true, ('vic20-snapshot.c 134; ' + $driveAnchor), @($manifestTest))
    , @('FSDRIVE', '0.0', 10, @('FSDRIVE:0.0'), @('file system drive state'), 'fsdrive_snapshot_write_module (fsdrive-snapshot.c 52)', 'fsdrive_snapshot_read_module (fsdrive-snapshot.c 79)', $slice4, $slice4Adapter, $true, 'vic20-snapshot.c 135; fsdrive-snapshot.c 48 to 50, 52, 79', @($manifestTest))
    , @('EVENT', '0.1', 11, @('EVENT:0.1 (event mode only)'), @('event recording state (event mode only)'), 'event_snapshot_write_module (event.c 1138, 1147)', 'event_snapshot_read_module (event.c 1028)', $slice4, $slice4Adapter, $false, 'vic20-snapshot.c 136; event.c 1028, 1138, 1147', @($manifestTest))
    , @('TAPEPORT', '1.0', 12, @('TAPEPORT:1.0', 'DATASETTE:1.5'), @('tape port device selection', 'datasette motor, counter and tape position'), $tapeExport, $tapeImport, $slice4, $slice4Adapter, $true, ('vic20-snapshot.c 137; ' + $tapeAnchor), @($manifestTest))
    , @('KEYBOARD', '1.1', 13, @('KEYBOARD:1.1'), @('keyboard matrix'), 'keyboard_snapshot_write_module (keyboard.c 1262)', 'keyboard_snapshot_read_module (keyboard.c 1282)', $slice4, $slice4Adapter, $true, 'vic20-snapshot.c 138; keyboard.c 1258 to 1260, 1262, 1282', @($manifestTest))
    , @('JOYPORT1', '0.0', 14, @('JOYPORT0:0.0', 'JOYSTICK0:1.2'), @('joyport 1 device', 'joystick 1 state'), $joyportExport, $joyportImport, $slice4, $slice4Adapter, $true, ('vic20-snapshot.c 139; ' + $joyportAnchor), @($manifestTest))
    , @('USERPORT', '1.0', 15, @('USERPORT:1.0'), @('userport device selection'), 'userport_snapshot_write_module (userport.c 1090)', 'userport_snapshot_read_module (userport.c 1126)', $slice4, $slice4Adapter, $true, 'vic20-snapshot.c 140; userport.c 1086 to 1088, 1090, 1126', @($manifestTest))
)
$modules = @(ConvertTo-UnitEntries -Machine 'x64sc' -Rows $x64scRows) + @(ConvertTo-UnitEntries -Machine 'xvic' -Rows $xvicRows)

$suppliedRelative = 'tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf'
$suppliedPath = [IO.Path]::Combine($RepoRoot, 'tests', 'ViceSharp.TestHarness', 'Fixtures', 'Vsf', 'ready-c64sc-truedrive.vsf')
$supplied = Get-ModuleList -Path $suppliedPath
$suppliedBytes = [IO.File]::ReadAllBytes($suppliedPath)
# MAINCPU v1.2 payload: the clock is the first 8 bytes after the 22-byte module header (mainc64cpu.c 889 to 890, SMW_CLOCK first).
$suppliedClock = [BitConverter]::ToUInt64($suppliedBytes, 58 + 22)
$fixtures = @(
    [ordered]@{
        path = $suppliedRelative
        sha256 = (Get-FileHash -LiteralPath $suppliedPath -Algorithm SHA256).Hash
        length = (Get-Item -LiteralPath $suppliedPath).Length
        machine = 'x64sc'
        model = 'c64sc PAL (C64C, true drive on units 9 to 11, unit 8 has_tde 0)'
        command = 'supplied Gate 0 fixture (docs/receipts/hv/20261003T100623-0500-vsf-gate0-local-evidence.md), MAINCPU v1.2; not generated by this plan, so trigger and write clock are both the MAINCPU clock stored in the file'
        triggerClock = $suppliedClock
        writeClock = $suppliedClock
        romHashes = @($allowlist['C64/basic-901226-01.bin'], $allowlist['C64/kernal-901227-03.bin'], $allowlist['C64/chargen-901225-01.bin'], $allowlist['DRIVES/dos1541-325302-01+901229-05.bin'])
        romHashesSource = 'allowlist entries of the machine (RomAllowlist.BaseFixtureSubset); the producing ROM set is the vendored data root of the Gate 0 receipt'
        mediaHashes = @()
        hosts = @()
        status = 'accepted'
        headerVariant = $supplied.variant
        modules = $supplied.modules
    }
)
if ($CandidateReceipts) {
    $byFixture = [ordered]@{}
    foreach ($f in @(Get-ChildItem -LiteralPath $CandidateReceipts -File -Filter '*.candidate.json' | Sort-Object Name)) {
        $c = Get-Content -LiteralPath $f.FullName -Raw | ConvertFrom-Json
        if (-not $byFixture.Contains([string]$c.fixtureId)) { $byFixture[[string]$c.fixtureId] = @() }
        $byFixture[[string]$c.fixtureId] += $c
    }
    foreach ($id in $byFixture.Keys) {
        $runs = @($byFixture[$id])
        $first = $runs[0]
        $candidateModules = @()
        $variant = $null
        $candidateVsf = [IO.Path]::Combine($CandidateReceipts, [IO.Path]::GetFileName([string]$first.candidatePath))
        if (Test-Path -LiteralPath $candidateVsf) { $ml = Get-ModuleList -Path $candidateVsf; $candidateModules = $ml.modules; $variant = $ml.variant }
        $fixtures += [ordered]@{
            path = ('fixtures/' + $id + '.vsf (not published: ' + ([string]$first.outcome) + ')')
            sha256 = [string]$first.candidateSha256
            length = [long]$first.candidateLength
            machine = [string]$first.machine
            model = [string]$first.model
            command = [string]$first.command
            triggerClock = [uint64]$first.trigger
            writeClock = [uint64]$first.writeClock
            romHashes = @($first.roms | ForEach-Object { [string]$_.sha256 })
            mediaHashes = @()
            hosts = @($runs | ForEach-Object { [ordered]@{ host = [string]$_.host; writeClock = [uint64]$_.writeClock; writerResult = [int]$_.writerResult; candidateSha256 = [string]$_.candidateSha256; outcome = [string]$_.outcome; reason = [string]$_.reason; receipt = (Split-Path -Leaf $CandidateReceipts) + '/' + [IO.Path]::GetFileName([string]$_.candidatePath).Replace('.vsf', '.candidate.json') } })
            status = [string]$first.outcome
            observerSha256 = [string]$first.observerSha256
            headerVariant = $variant
            modules = $candidateModules
        }
    }
}

$manifest = [ordered]@{
    schemaVersion = 1
    viceCommit = $sha
    fixtureGeneratorVersion = '1.0.0'
    framing = [ordered]@{ magicBytes = 19; containerVersionBytes = 2; machineFieldBytes = 16; currentHeaderBytes = 58; legacyHeaderBytes = 37; moduleHeaderBytes = 22; integerEncoding = 'little-endian'; moduleLengthRule = 'inclusive of the 22-byte module header' }
    romAllowlist = @($allowlist.Values | Sort-Object)
    fixtures = $fixtures
    readOrderSources = [ordered]@{ x64sc = 'c64-snapshot.c 121 to 136 (read), 76 to 91 (write); container 2.0'; xvic = 'vic20-snapshot.c 129 to 140 (read), 79 to 90 (write); container 3.1; the IEEE488 VIA modules of lines 98 to 99 and 144 to 145 are written only with the IEEE488 interface and are outside the Slice 1 base fixtures' }
    modules = $modules
}
$json = $manifest | ConvertTo-Json -Depth 8
if ($PSCmdlet.ShouldProcess($OutPath, 'write the schema 1 state-transfer manifest')) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $OutPath) -Force | Out-Null
    [IO.File]::WriteAllText($OutPath, $json + "`n", [Text.UTF8Encoding]::new($false))
}
Write-Output ('manifest: ' + $OutPath + ' units ' + $modules.Count + ', fixtures ' + $fixtures.Count + ', supplied modules ' + $supplied.modules.Count + ' (' + $supplied.variant + ', clock ' + $suppliedClock + ')')
