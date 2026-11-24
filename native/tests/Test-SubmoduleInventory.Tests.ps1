#Requires -Version 7.0
<#
PLAN-NATIVERESTORE-001 D2 (F08 negative control on a mock tree): Test-SubmoduleInventory.ps1 reports Equal for an
unchanged tree and detects a changed existing untracked file, an added file, a deleted file, a modified tracked
file and a moved HEAD.
#>
BeforeAll {
    $script:Script = [IO.Path]::Combine($PSScriptRoot, '..', 'Test-SubmoduleInventory.ps1')

    function Initialize-MockRepo {
        param([string] $Root)
        New-Item -ItemType Directory -Path $Root -Force | Out-Null
        & git -C $Root init -q
        & git -C $Root config user.email 'pester@example.invalid'
        & git -C $Root config user.name 'Pester'
        [IO.File]::WriteAllText([IO.Path]::Combine($Root, 'tracked.c'), "int x;`n")
        & git -C $Root add -A
        & git -C $Root commit -q -m 'base'
        [IO.File]::WriteAllText([IO.Path]::Combine($Root, 'untracked.h'), "#define U 1`n")
        [IO.File]::WriteAllText([IO.Path]::Combine($Root, '.hidden-flags'), "flags`n")
    }
}

Describe 'Test-SubmoduleInventory' {
    BeforeEach {
        $script:Root = [IO.Path]::Combine([IO.Path]::GetTempPath(), ('subinv-' + [guid]::NewGuid().ToString('N')))
        Initialize-MockRepo -Root $script:Root
        $script:Baseline = [IO.Path]::Combine($script:Root, '..', ('subinv-baseline-' + [guid]::NewGuid().ToString('N') + '.json'))
        & $script:Script -SubmodulePath $script:Root -WriteBaselinePath $script:Baseline | Out-Null
    }
    AfterEach {
        Remove-Item -LiteralPath $script:Root -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $script:Baseline -Force -ErrorAction SilentlyContinue
    }

    It 'writes a baseline with every untracked path including hidden files' {
        $b = Get-Content -LiteralPath $script:Baseline -Raw | ConvertFrom-Json
        @($b.entries).Count | Should -Be 2
        @($b.entries | ForEach-Object { $_.path }) | Should -Contain '.hidden-flags'
        @($b.entries | ForEach-Object { $_.path }) | Should -Contain 'untracked.h'
        $b.checkedOutSha | Should -Match '^[0-9a-f]{40}$'
    }

    It 'reports Equal for an unchanged tree' {
        $out = & $script:Script -SubmodulePath $script:Root -BaselineManifest $script:Baseline
        ($out -join "`n") | Should -Match 'inventory Equal'
    }

    It 'detects a changed existing untracked file' {
        [IO.File]::WriteAllText([IO.Path]::Combine($script:Root, 'untracked.h'), "#define U 2`n")
        { & $script:Script -SubmodulePath $script:Root -BaselineManifest $script:Baseline -ErrorAction Stop | Out-Null } | Should -Throw -ExpectedMessage '*differs*'
    }

    It 'detects an added file' {
        [IO.File]::WriteAllText([IO.Path]::Combine($script:Root, 'new.c'), "int y;`n")
        $report = [IO.Path]::Combine($script:Root, '..', 'subinv-report.json')
        { & $script:Script -SubmodulePath $script:Root -BaselineManifest $script:Baseline -ReportPath $report -ErrorAction Stop | Out-Null } | Should -Throw
        $r = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        @($r.added) | Should -Contain 'new.c'
        Remove-Item -LiteralPath $report -Force
    }

    It 'detects a deleted file and a modified tracked file' {
        Remove-Item -LiteralPath ([IO.Path]::Combine($script:Root, 'untracked.h')) -Force
        [IO.File]::WriteAllText([IO.Path]::Combine($script:Root, 'tracked.c'), "int x = 2;`n")
        $report = [IO.Path]::Combine($script:Root, '..', 'subinv-report2.json')
        { & $script:Script -SubmodulePath $script:Root -BaselineManifest $script:Baseline -ReportPath $report -ErrorAction Stop | Out-Null } | Should -Throw
        $r = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        @($r.deleted) | Should -Contain 'untracked.h'
        @($r.added) | Should -Contain 'tracked.c'
        Remove-Item -LiteralPath $report -Force
    }

    It 'detects a moved HEAD' {
        [IO.File]::WriteAllText([IO.Path]::Combine($script:Root, 'tracked.c'), "int x = 3;`n")
        & git -C $script:Root commit -q -am 'move'
        { & $script:Script -SubmodulePath $script:Root -BaselineManifest $script:Baseline -ErrorAction Stop | Out-Null } | Should -Throw
    }
}
