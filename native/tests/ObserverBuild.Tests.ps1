#Requires -Version 7.0
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.7.0' }

# PLAN-NATIVERESTORE-001 D1, AC BuilderRerun_OnUpToDateTree_ReusesVerifiedLinkEvidence (round 3b A06): the Make
# stage's link-line decision (native/lib/ObserverBuild.psm1) proven with mocks before the builder uses it.
# Tag MockGate: captured lines win and refresh; an up-to-date make (no link line) reuses retained lines only when
# every recorded object still hashes the same; a changed or missing object, a missing hash list, or no retained
# line stops with a named prerequisite and replaces nothing.

BeforeAll {
    Import-Module (Join-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..') -ChildPath 'lib/ObserverBuild.psm1') -Force
    $script:Line = 'gcc -g -O2 -o x64sc c64/libc64sc.a "core/libcore.a" main.o -lm'
}

Describe 'Resolve-StockLinkLine' -Tag MockGate {
    BeforeEach {
        $script:Work = Join-Path ([IO.Path]::GetTempPath()) ('obld-' + [Guid]::NewGuid().ToString('N'))
        New-Item -ItemType Directory -Path $script:Work -Force | Out-Null
        $script:Hashes = @{ 'c64/libc64sc.a' = 'A'; 'core/libcore.a' = 'B'; 'main.o' = 'C' }
        $script:Provider = { param($p) $script:Hashes[$p] }
    }
    AfterEach { if (Test-Path -LiteralPath $script:Work) { Remove-Item -LiteralPath $script:Work -Recurse -Force } }

    It 'extracts object and archive tokens from a link line in order' {
        Get-LinkLineObjectPath -LinkLine $script:Line | Should -Be @('c64/libc64sc.a', 'core/libcore.a', 'main.o')
    }

    It 'uses a captured line, records its object hashes and marks the receipts for writing' {
        $r = Resolve-StockLinkLine -Captured @{ x64sc = $script:Line } -Targets @('x64sc') -ReceiptsDirectory $script:Work -ObjectHashProvider $script:Provider
        $r.Lines.x64sc | Should -Be $script:Line
        $r.Source.x64sc | Should -Be 'captured'
        $r.Written | Should -BeTrue
        @($r.Objects.x64sc).Count | Should -Be 3
        ($r.Objects.x64sc | Where-Object Path -eq 'main.o').Sha256 | Should -Be 'C'
    }

    It 'reuses a retained line on an up-to-date tree when every recorded object hash still matches' {
        @{ x64sc = $script:Line } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $script:Work 'stock-link-lines.json')
        @{ x64sc = @(@{ Path = 'c64/libc64sc.a'; Sha256 = 'A' }, @{ Path = 'core/libcore.a'; Sha256 = 'B' }, @{ Path = 'main.o'; Sha256 = 'C' }) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $script:Work 'stock-link-objects.json')
        $r = Resolve-StockLinkLine -Captured @{} -Targets @('x64sc') -ReceiptsDirectory $script:Work -ObjectHashProvider $script:Provider
        $r.Lines.x64sc | Should -Be $script:Line
        $r.Source.x64sc | Should -Be 'reused'
        $r.Written | Should -BeFalse
    }

    It 'stops with a named prerequisite when a recorded object changed, and replaces nothing' {
        @{ x64sc = $script:Line } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $script:Work 'stock-link-lines.json')
        @{ x64sc = @(@{ Path = 'main.o'; Sha256 = 'OLD' }) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $script:Work 'stock-link-objects.json')
        $before = Get-Content -LiteralPath (Join-Path $script:Work 'stock-link-lines.json') -Raw
        { Resolve-StockLinkLine -Captured @{} -Targets @('x64sc') -ReceiptsDirectory $script:Work -ObjectHashProvider $script:Provider } | Should -Throw '*prerequisite*'
        Get-Content -LiteralPath (Join-Path $script:Work 'stock-link-lines.json') -Raw | Should -Be $before
    }

    It 'stops when a recorded object is missing from the tree' {
        @{ x64sc = $script:Line } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $script:Work 'stock-link-lines.json')
        @{ x64sc = @(@{ Path = 'gone.o'; Sha256 = 'Z' }) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $script:Work 'stock-link-objects.json')
        { Resolve-StockLinkLine -Captured @{} -Targets @('x64sc') -ReceiptsDirectory $script:Work -ObjectHashProvider $script:Provider } | Should -Throw '*prerequisite*'
    }

    It 'stops when a retained line has no object hash list' {
        @{ x64sc = $script:Line } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $script:Work 'stock-link-lines.json')
        { Resolve-StockLinkLine -Captured @{} -Targets @('x64sc') -ReceiptsDirectory $script:Work -ObjectHashProvider $script:Provider } | Should -Throw '*prerequisite*'
    }

    It 'stops when nothing was captured and nothing is retained' {
        { Resolve-StockLinkLine -Captured @{} -Targets @('x64sc') -ReceiptsDirectory $script:Work -ObjectHashProvider $script:Provider } | Should -Throw '*prerequisite*'
    }

    It 'handles a mixed run: one target captured, the other reused' {
        @{ xvic = 'gcc -o xvic vic20/libvic20.a vmain.o' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $script:Work 'stock-link-lines.json')
        @{ xvic = @(@{ Path = 'vic20/libvic20.a'; Sha256 = 'V' }, @{ Path = 'vmain.o'; Sha256 = 'W' }) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $script:Work 'stock-link-objects.json')
        $script:Hashes['vic20/libvic20.a'] = 'V'; $script:Hashes['vmain.o'] = 'W'
        $r = Resolve-StockLinkLine -Captured @{ x64sc = $script:Line } -Targets @('x64sc', 'xvic') -ReceiptsDirectory $script:Work -ObjectHashProvider $script:Provider
        $r.Source.x64sc | Should -Be 'captured'
        $r.Source.xvic | Should -Be 'reused'
        $r.Written | Should -BeTrue
    }
}
