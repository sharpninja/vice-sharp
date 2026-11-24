#Requires -Version 7.0
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.7.0' }

# PLAN-NATIVERESTORE-001 D1 (round 1 F09): the two canonical native scripts support -WhatIf through
# SupportsShouldProcess, and a dry run creates no directory and writes no file. Tag MockGate: a temporary
# repository root with an empty materialization source stands in for the real tree; no native tool runs.

BeforeAll {
    $script:Native = (Resolve-Path -LiteralPath (Join-Path -Path $PSScriptRoot -ChildPath '..')).Path
    $script:Builder = Join-Path $script:Native 'build-vice-observer.ps1'
    $script:Runner = (Resolve-Path -LiteralPath (Join-Path -Path $script:Native -ChildPath '../tools/immutable-vice/Invoke-ObserverEquivalence.ps1')).Path
    $script:Sha = 'cc67418cba58e71d8e70fd353e9de47199ad2075'
}

Describe 'build-vice-observer.ps1 dry run' -Tag MockGate {
    BeforeEach {
        $script:Work = Join-Path ([IO.Path]::GetTempPath()) ('bdry-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path (Join-Path $script:Work "artifacts/immutable-vice/$script:Sha/source/vice") -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $script:Work 'tools/immutable-vice') -Force | Out-Null
    }
    AfterEach { if (Test-Path -LiteralPath $script:Work) { Remove-Item -LiteralPath $script:Work -Recurse -Force } }

    It 'declares SupportsShouldProcess' {
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($script:Builder, [ref]$null, [ref]$null)
        $attr = $ast.ParamBlock.Attributes | Where-Object { $_.TypeName.Name -eq 'CmdletBinding' }
        @($attr.NamedArguments | Where-Object ArgumentName -eq 'SupportsShouldProcess').Count | Should -Be 1
    }

    It 'creates no directory and no file under -WhatIf for every stage' {
        foreach ($stage in 'All', 'Copy', 'Make', 'Link') {
            $before = @(Get-ChildItem -LiteralPath $script:Work -Recurse -Force | ForEach-Object { $_.FullName })
            & $script:Builder -Stage $stage -RepoRoot $script:Work -Sha $script:Sha -WhatIf *> $null
            $after = @(Get-ChildItem -LiteralPath $script:Work -Recurse -Force | ForEach-Object { $_.FullName })
            $after | Should -Be $before
        }
        Test-Path -LiteralPath (Join-Path $script:Work "artifacts/immutable-vice/$script:Sha/build-linux") | Should -BeFalse
        Test-Path -LiteralPath (Join-Path $script:Work "artifacts/immutable-vice/$script:Sha/receipts-linux") | Should -BeFalse
    }
}

Describe 'Invoke-ObserverEquivalence.ps1 dry run' -Tag MockGate {
    BeforeEach {
        $script:Work = Join-Path ([IO.Path]::GetTempPath()) ('rdry-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:Work -Force | Out-Null
    }
    AfterEach { if (Test-Path -LiteralPath $script:Work) { Remove-Item -LiteralPath $script:Work -Recurse -Force } }

    It 'declares SupportsShouldProcess' {
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($script:Runner, [ref]$null, [ref]$null)
        $attr = $ast.ParamBlock.Attributes | Where-Object { $_.TypeName.Name -eq 'CmdletBinding' }
        @($attr.NamedArguments | Where-Object ArgumentName -eq 'SupportsShouldProcess').Count | Should -Be 1
    }

    It 'creates no output directory and no file under -WhatIf' {
        $out = Join-Path $script:Work 'equivalence-out'
        & $script:Runner -Machine x64sc -Cycles 100 -RepoRoot $script:Work -OutDir $out -WhatIf *> $null
        Test-Path -LiteralPath $out | Should -BeFalse
        @(Get-ChildItem -LiteralPath $script:Work -Recurse -Force).Count | Should -Be 0
    }
}
