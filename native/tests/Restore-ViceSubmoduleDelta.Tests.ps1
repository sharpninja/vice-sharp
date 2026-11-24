#Requires -Version 7.0
<#
PLAN-NATIVERESTORE-001 D4 Pester controls on a mock superproject with a real git submodule: a changed live delta,
staged drift, a partial completion (resume), a missing receipt, a corrupt receipt, a wrong repository, a wrong host
and a changed gitlink each stop before any overwrite; the happy path and the resume path complete with every proof;
-WhatIf writes nothing. (LEGION2 adds the CRLF-converted mock on Windows.)
#>
BeforeAll {
    $script:Script = [IO.Path]::Combine($PSScriptRoot, '..', 'Restore-ViceSubmoduleDelta.ps1')

    function Invoke-Quiet { param([string] $Root, [string[]] $Arguments) & git -C $Root @Arguments 2>&1 | Out-Null; if ($LASTEXITCODE -ne 0) { throw ('git ' + ($Arguments -join ' ') + ' failed') } }

    function Initialize-MockSuperproject {
        param([string] $Root)
        $upstream = [IO.Path]::Combine($Root, 'upstream')
        New-Item -ItemType Directory -Path ([IO.Path]::Combine($upstream, 'vice', 'src')) -Force | Out-Null
        Invoke-Quiet -Root $upstream -Arguments @('init', '-q')
        Invoke-Quiet -Root $upstream -Arguments @('config', 'user.email', 'pester@example.invalid')
        Invoke-Quiet -Root $upstream -Arguments @('config', 'user.name', 'Pester')
        [IO.File]::WriteAllText([IO.Path]::Combine($upstream, 'vice', 'src', 'a.c'), "int a;`n")
        [IO.File]::WriteAllText([IO.Path]::Combine($upstream, 'vice', 'src', 'b.c'), "int b;`n")
        [IO.File]::WriteAllText([IO.Path]::Combine($upstream, 'vice', 'src', 'c.c'), "int c;`n")
        Invoke-Quiet -Root $upstream -Arguments @('add', '-A')
        Invoke-Quiet -Root $upstream -Arguments @('commit', '-q', '-m', 'pinned')
        $super = [IO.Path]::Combine($Root, 'super')
        New-Item -ItemType Directory -Path $super -Force | Out-Null
        Invoke-Quiet -Root $super -Arguments @('init', '-q')
        Invoke-Quiet -Root $super -Arguments @('config', 'user.email', 'pester@example.invalid')
        Invoke-Quiet -Root $super -Arguments @('config', 'user.name', 'Pester')
        Invoke-Quiet -Root $super -Arguments @('-c', 'protocol.file.allow=always', 'submodule', 'add', '-q', $upstream, 'native/vice')
        Invoke-Quiet -Root $super -Arguments @('config', '-f', '.gitmodules', 'submodule.native/vice.ignore', 'untracked')
        Invoke-Quiet -Root $super -Arguments @('add', '-A')
        Invoke-Quiet -Root $super -Arguments @('commit', '-q', '-m', 'super')
        $sub = [IO.Path]::Combine($super, 'native', 'vice')
        Invoke-Quiet -Root $sub -Arguments @('config', 'user.email', 'pester@example.invalid')
        Invoke-Quiet -Root $sub -Arguments @('config', 'user.name', 'Pester')
        # The delta: two modified tracked files, one untracked generated file, one hidden untracked file.
        [IO.File]::WriteAllText([IO.Path]::Combine($sub, 'vice', 'src', 'a.c'), "int a; /* shim */`n")
        [IO.File]::WriteAllText([IO.Path]::Combine($sub, 'vice', 'src', 'b.c'), "int b; /* shim */`n")
        [IO.File]::WriteAllText([IO.Path]::Combine($sub, 'vice', 'src', 'gen.h'), "#define G 1`n")
        [IO.File]::WriteAllText([IO.Path]::Combine($sub, 'vice', '.flags'), "flags`n")
        return $super
    }

    function Initialize-MockReceipt {
        param([string] $Super, [string] $Receipt, [string] $HostName = 'MOCK-HOST')
        $sub = [IO.Path]::Combine($Super, 'native', 'vice')
        New-Item -ItemType Directory -Path $Receipt -Force | Out-Null
        $gitlink = ((& git -C $Super ls-tree HEAD -- native/vice) -split ' ')[2].Split("`t")[0]
        $head = (& git -C $sub rev-parse HEAD).Trim()
        $entries = @(); $preserved = @()
        foreach ($row in @(& git -C $sub status --porcelain=v1 -uall --untracked-files=all | Where-Object { $_ -ne '' })) {
            $code = $row.Substring(0, 2); $path = $row.Substring(3)
            $full = [IO.Path]::Combine($sub, $path)
            $sha = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash
            $len = (Get-Item -LiteralPath $full -Force).Length
            $entries += [pscustomobject]@{ path = $path; status = $code.Trim(); class = ($code -eq '??' ? 'generated' : 'modified'); hidden = $path.Contains('/.'); sha256 = $sha; length = $len }
            if ($code.Trim() -eq 'M') {
                $copy = [IO.Path]::Combine($Receipt, 'preserved', $path)
                New-Item -ItemType Directory -Path (Split-Path -Parent $copy) -Force | Out-Null
                Copy-Item -LiteralPath $full -Destination $copy
                $preserved += [pscustomobject]@{ path = $path; copyPath = 'preserved/' + $path; sha256 = $sha; length = $len }
            }
        }
        $manifest = [pscustomobject]@{ schema = 'phase-d-preservation/1'; host = $HostName; submodulePath = $sub; gitlinkSha = $gitlink; checkedOutSha = $head; cachedDiffEmpty = $true; entries = $entries; preserved = $preserved }
        $manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath ([IO.Path]::Combine($Receipt, 'manifest.json')) -Encoding utf8
    }

    function Invoke-Restore {
        param([string] $Super, [string] $Receipt, [string] $HostName = 'MOCK-HOST', [switch] $DryRun)
        $params = @{ SuperprojectRoot = $Super; SubmoduleRelativePath = 'native/vice'; ReceiptDirectory = $Receipt; HostName = $HostName; ReportPath = [IO.Path]::Combine($Receipt, 'restore-report.json') }
        if ($DryRun) { $params['WhatIf'] = $true }
        & $script:Script @params
    }

    function Get-Report { param([string] $Receipt) Get-Content -LiteralPath ([IO.Path]::Combine($Receipt, 'restore-report.json')) -Raw | ConvertFrom-Json }
    function Get-Text { param([string] $Super, [string] $Relative) [IO.File]::ReadAllText([IO.Path]::Combine($Super, 'native', 'vice', $Relative)) }
}

Describe 'Restore-ViceSubmoduleDelta' {
    BeforeEach {
        $script:Root = [IO.Path]::Combine([IO.Path]::GetTempPath(), ('restore-' + [guid]::NewGuid().ToString('N')))
        New-Item -ItemType Directory -Path $script:Root | Out-Null
        $script:Super = Initialize-MockSuperproject -Root $script:Root
        $script:Receipt = [IO.Path]::Combine($script:Root, 'receipt')
        Initialize-MockReceipt -Super $script:Super -Receipt $script:Receipt
    }
    AfterEach { Remove-Item -LiteralPath $script:Root -Recurse -Force -ErrorAction SilentlyContinue }

    It 'happy path: restores every untouched path, proves the tree, keeps every untracked entry' {
        Invoke-Restore -Super $script:Super -Receipt $script:Receipt | Out-Null
        $r = Get-Report -Receipt $script:Receipt
        $r.outcome | Should -Be 'Complete'
        @($r.restored) | Should -Be @('vice/src/a.c', 'vice/src/b.c')
        $r.proofs.diffEmpty | Should -BeTrue
        $r.proofs.cachedDiffEmpty | Should -BeTrue
        $r.proofs.untrackedEqual | Should -BeTrue
        $r.proofs.superprojectSubmoduleClean | Should -BeTrue
        $r.proofs.gitlinkEqualsHead | Should -BeTrue
        $r.proofs.preservedStillMatch | Should -BeTrue
        $r.proofs.trackedEntriesAfter | Should -Be 0
        Get-Text -Super $script:Super -Relative 'vice/src/a.c' | Should -Be "int a;`n"
        Test-Path -LiteralPath ([IO.Path]::Combine($script:Super, 'native', 'vice', 'vice', 'src', 'gen.h')) | Should -BeTrue
        Test-Path -LiteralPath ([IO.Path]::Combine($script:Super, 'native', 'vice', 'vice', '.flags')) | Should -BeTrue
        Get-Text -Super $script:Super -Relative 'vice/src/c.c' | Should -Be "int c;`n"
    }

    It '-WhatIf classifies and writes nothing' {
        Invoke-Restore -Super $script:Super -Receipt $script:Receipt -DryRun | Out-Null
        Get-Text -Super $script:Super -Relative 'vice/src/a.c' | Should -Be "int a; /* shim */`n"
        Test-Path -LiteralPath ([IO.Path]::Combine($script:Receipt, 'restore-report.json')) | Should -BeFalse
    }

    It 'resume path: a path restored before the rerun is reported restored and the rest completes' {
        & git -C ([IO.Path]::Combine($script:Super, 'native', 'vice')) checkout -- vice/src/a.c 2>&1 | Out-Null
        Invoke-Restore -Super $script:Super -Receipt $script:Receipt | Out-Null
        $r = Get-Report -Receipt $script:Receipt
        $r.outcome | Should -Be 'Complete'
        @($r.paths | Where-Object { $_.path -eq 'vice/src/a.c' })[0].state | Should -Be 'restored'
        @($r.paths | Where-Object { $_.path -eq 'vice/src/b.c' })[0].state | Should -Be 'untouched'
        @($r.restored) | Should -Be @('vice/src/b.c')
        Invoke-Restore -Super $script:Super -Receipt $script:Receipt | Out-Null
        (Get-Report -Receipt $script:Receipt).restored.Count | Should -Be 0
    }

    It 'stops before any overwrite on a changed live delta' {
        [IO.File]::WriteAllText([IO.Path]::Combine($script:Super, 'native', 'vice', 'vice', 'src', 'b.c'), "int b; /* changed since D0 */`n")
        { Invoke-Restore -Super $script:Super -Receipt $script:Receipt -ErrorAction Stop | Out-Null } | Should -Throw -ExpectedMessage '*stopped*'
        $r = Get-Report -Receipt $script:Receipt
        $r.outcome | Should -Be 'Stopped'
        @($r.stops | Where-Object { $_ -like '*changed live delta*' }).Count | Should -Be 1
        Get-Text -Super $script:Super -Relative 'vice/src/a.c' | Should -Be "int a; /* shim */`n"
    }

    It 'stops on staged drift' {
        & git -C ([IO.Path]::Combine($script:Super, 'native', 'vice')) add -- vice/src/gen.h 2>&1 | Out-Null
        { Invoke-Restore -Super $script:Super -Receipt $script:Receipt -ErrorAction Stop | Out-Null } | Should -Throw
        @((Get-Report -Receipt $script:Receipt).stops | Where-Object { $_ -like '*staged drift*' }).Count | Should -Be 1
        Get-Text -Super $script:Super -Relative 'vice/src/a.c' | Should -Be "int a; /* shim */`n"
    }

    It 'stops on a missing receipt, a corrupt manifest and a corrupt preserved copy' {
        $missing = [IO.Path]::Combine($script:Root, 'nowhere')
        { Invoke-Restore -Super $script:Super -Receipt $missing -ErrorAction Stop | Out-Null } | Should -Throw
        $manifest = [IO.Path]::Combine($script:Receipt, 'manifest.json')
        $good = Get-Content -LiteralPath $manifest -Raw
        [IO.File]::WriteAllText($manifest, '{ not json')
        { Invoke-Restore -Super $script:Super -Receipt $script:Receipt -ErrorAction Stop | Out-Null } | Should -Throw
        [IO.File]::WriteAllText($manifest, $good)
        [IO.File]::WriteAllText([IO.Path]::Combine($script:Receipt, 'preserved', 'vice', 'src', 'a.c'), "tampered`n")
        { Invoke-Restore -Super $script:Super -Receipt $script:Receipt -ErrorAction Stop | Out-Null } | Should -Throw
        @((Get-Report -Receipt $script:Receipt).stops | Where-Object { $_ -like '*preserved copy does not match*' }).Count | Should -Be 1
        Get-Text -Super $script:Super -Relative 'vice/src/a.c' | Should -Be "int a; /* shim */`n"
    }

    It 'stops on the wrong repository and the wrong host' {
        $other = Initialize-MockSuperproject -Root ([IO.Path]::Combine($script:Root, 'other'))
        { Invoke-Restore -Super $other -Receipt $script:Receipt -ErrorAction Stop | Out-Null } | Should -Throw
        @((Get-Report -Receipt $script:Receipt).stops | Where-Object { $_ -like '*wrong repository*' }).Count | Should -Be 1
        { Invoke-Restore -Super $script:Super -Receipt $script:Receipt -HostName 'OTHER-HOST' -ErrorAction Stop | Out-Null } | Should -Throw
        @((Get-Report -Receipt $script:Receipt).stops | Where-Object { $_ -like '*not the running host*' }).Count | Should -Be 1
        Get-Text -Super $script:Super -Relative 'vice/src/a.c' | Should -Be "int a; /* shim */`n"
    }

    It 'stops on a changed gitlink or checkout' {
        $sub = [IO.Path]::Combine($script:Super, 'native', 'vice')
        & git -C $sub commit -q -am 'moved' 2>&1 | Out-Null
        { Invoke-Restore -Super $script:Super -Receipt $script:Receipt -ErrorAction Stop | Out-Null } | Should -Throw
        @((Get-Report -Receipt $script:Receipt).stops | Where-Object { $_ -like '*checked-out HEAD*' -or $_ -like '*gitlink*' }).Count | Should -BeGreaterThan 0
    }

    It 'stops when the untracked inventory already differs from the receipt' {
        [IO.File]::WriteAllText([IO.Path]::Combine($script:Super, 'native', 'vice', 'vice', 'src', 'extra.h'), "x`n")
        { Invoke-Restore -Super $script:Super -Receipt $script:Receipt -ErrorAction Stop | Out-Null } | Should -Throw
        @((Get-Report -Receipt $script:Receipt).stops | Where-Object { $_ -like '*untracked inventory differs*' }).Count | Should -Be 1
        Get-Text -Super $script:Super -Relative 'vice/src/a.c' | Should -Be "int a; /* shim */`n"
    }
}
