#Requires -Version 7.0
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.7.0' }

# PLAN-NATIVERESTORE-001 step D0 fixture scan (N01, N05) and the R10 launch precondition: contract of
# native/Get-VsfDriveModules.ps1. Tag MockGate uses synthetic containers; Tag Binding reads the approved fixture
# tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf and must reproduce the round 2 byte
# inspection (MAINCPU 1.2, clock 109163946, PC 22377; DRIVE8 at offset 66204, length 24, has_tde 0, has_drives 1).

BeforeAll {
    $script:ScriptPath = (Resolve-Path -LiteralPath (Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..') -ChildPath 'Get-VsfDriveModules.ps1')).Path
    $script:RepoRoot = (Resolve-Path -LiteralPath (Join-Path -Path $PSScriptRoot -ChildPath '../..')).Path

    function ConvertTo-VsfByteArray {
        param([object[]]$Modules, [string]$Machine = 'C64SC', [switch]$BadMagic)
        $ms = [IO.MemoryStream]::new()
        function Write-ByteBlock([byte[]]$b) { $ms.Write($b, 0, $b.Length) }
        [byte[]]$magic = [Text.Encoding]::ASCII.GetBytes('VICE Snapshot File') + @([byte]0x1a)
        if ($BadMagic) { $magic[0] = [byte][char]'X' }
        Write-ByteBlock $magic
        Write-ByteBlock ([byte[]]@(2, 0))
        [byte[]]$name = [byte[]]::new(16); [Text.Encoding]::ASCII.GetBytes($Machine).CopyTo($name, 0); Write-ByteBlock $name
        Write-ByteBlock ([byte[]]([Text.Encoding]::ASCII.GetBytes('VICE Version') + @([byte]0x1a)))
        Write-ByteBlock ([byte[]]@(3, 9, 0, 0)); Write-ByteBlock ([BitConverter]::GetBytes([uint32]45000))
        foreach ($m in $Modules) {
            [byte[]]$n = [byte[]]::new(16); [Text.Encoding]::ASCII.GetBytes([string]$m.name).CopyTo($n, 0); Write-ByteBlock $n
            Write-ByteBlock ([byte[]]@([byte]$m.major, [byte]$m.minor))
            [byte[]]$payload = [byte[]]$m.payload
            Write-ByteBlock ([BitConverter]::GetBytes([uint32](22 + $payload.Length)))
            Write-ByteBlock $payload
        }
        return [byte[]]$ms.ToArray()
    }

    function Get-MainCpuPayload {
        param([uint64]$Clock, [uint16]$Pc, [byte]$P)
        [byte[]]$out = [BitConverter]::GetBytes($Clock) + @([byte]0x11, [byte]0x22, [byte]0x33, [byte]0xF8) + [BitConverter]::GetBytes($Pc) + @($P) + [BitConverter]::GetBytes([uint32]0)
        return $out
    }
}
Describe 'Get-VsfDriveModules synthetic containers' -Tag MockGate {
    BeforeEach {
        $script:Work = Join-Path ([IO.Path]::GetTempPath()) ('vsf-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:Work -Force | Out-Null
    }
    AfterEach { if (Test-Path -LiteralPath $script:Work) { Remove-Item -LiteralPath $script:Work -Recurse -Force } }

    It 'lists modules with offsets and lengths, reads MAINCPU fields and DRIVE payload bytes, and computes the R10 precondition' {
        $bytes = ConvertTo-VsfByteArray -Modules @(
            @{ name = 'MAINCPU'; major = 1; minor = 2; payload = (Get-MainCpuPayload -Clock 109163946 -Pc 22377 -P 0xA0) },
            @{ name = 'DRIVE8'; major = 2; minor = 0; payload = [byte[]]@(0, 1) },
            @{ name = 'DRIVE9'; major = 2; minor = 0; payload = [byte[]]@(1, 1, 7, 7, 7) },
            @{ name = 'DRIVE11'; major = 2; minor = 0; payload = [byte[]]@(1, 2, 9) }
        )
        $path = Join-Path $script:Work 'a.vsf'; [IO.File]::WriteAllBytes($path, $bytes)
        $r = & $script:ScriptPath -Path $path
        $r.machine | Should -Be 'C64SC'
        $r.modules.Count | Should -Be 4
        $r.mainCpu.major | Should -Be 1
        $r.mainCpu.minor | Should -Be 2
        $r.mainCpu.clock | Should -Be 109163946
        $r.mainCpu.pc | Should -Be 22377
        $r.mainCpu.p | Should -Be 0xA0
        $r.mainCpu.offset | Should -Be 58
        ($r.drives | Where-Object unit -eq 8).hasTde | Should -Be 0
        ($r.drives | Where-Object unit -eq 8).hasDrives | Should -Be 1
        ($r.drives | Where-Object unit -eq 8).present | Should -BeTrue
        ($r.drives | Where-Object unit -eq 9).hasTde | Should -Be 1
        ($r.drives | Where-Object unit -eq 10).present | Should -BeFalse
        ($r.drives | Where-Object unit -eq 11).hasTde | Should -Be 1
        $r.precondition | Should -Be @('-drive8type', '0', '-drive10type', '0')
        $r.invalid.Count | Should -Be 0
    }

    It 'flags a DRIVE module whose payload is shorter than two bytes as invalid and withholds the precondition' {
        $bytes = ConvertTo-VsfByteArray -Modules @(
            @{ name = 'MAINCPU'; major = 1; minor = 4; payload = (Get-MainCpuPayload -Clock 5 -Pc 0x0400 -P 0x24) },
            @{ name = 'DRIVE8'; major = 2; minor = 0; payload = [byte[]]@(1) }
        )
        $path = Join-Path $script:Work 'b.vsf'; [IO.File]::WriteAllBytes($path, $bytes)
        $r = & $script:ScriptPath -Path $path
        $r.invalid.Count | Should -Be 1
        $r.invalid[0].unit | Should -Be 8
        $r.precondition | Should -BeNullOrEmpty
        $r.valid | Should -BeFalse
    }

    It 'rejects a container with the wrong magic' {
        $bytes = ConvertTo-VsfByteArray -BadMagic -Modules @(@{ name = 'MAINCPU'; major = 1; minor = 4; payload = (Get-MainCpuPayload -Clock 1 -Pc 1 -P 0) })
        $path = Join-Path $script:Work 'c.vsf'; [IO.File]::WriteAllBytes($path, $bytes)
        { & $script:ScriptPath -Path $path } | Should -Throw '*magic*'
    }

    It 'rejects a module whose declared size runs past the end of the file' {
        $bytes = ConvertTo-VsfByteArray -Modules @(@{ name = 'MAINCPU'; major = 1; minor = 4; payload = (Get-MainCpuPayload -Clock 1 -Pc 1 -P 0) })
        $bytes[58 + 18] = 0xFF
        $path = Join-Path $script:Work 'd.vsf'; [IO.File]::WriteAllBytes($path, $bytes)
        { & $script:ScriptPath -Path $path } | Should -Throw '*past the end*'
    }
}

Describe 'Get-VsfDriveModules on the approved fixture' -Tag Binding {
    It 'reproduces the round 2 byte inspection of ready-c64sc-truedrive.vsf' {
        $fixture = Join-Path $script:RepoRoot 'tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf'
        (Get-FileHash -LiteralPath $fixture -Algorithm SHA256).Hash | Should -Be '5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19'
        $r = & $script:ScriptPath -Path $fixture
        $r.mainCpu.major | Should -Be 1
        $r.mainCpu.minor | Should -Be 2
        $r.mainCpu.clock | Should -Be 109163946
        $r.mainCpu.pc | Should -Be 22377
        $r.mainCpu.p | Should -Be 0xA0
        $d8 = $r.drives | Where-Object unit -eq 8
        $d8.offset | Should -Be 66204
        $d8.length | Should -Be 24
        $d8.hasTde | Should -Be 0
        $d8.hasDrives | Should -Be 1
        foreach ($u in 9, 10, 11) { ($r.drives | Where-Object unit -eq $u).hasTde | Should -Be 1 }
        $r.precondition | Should -Be @('-drive8type', '0')
        $r.valid | Should -BeTrue
    }
}
