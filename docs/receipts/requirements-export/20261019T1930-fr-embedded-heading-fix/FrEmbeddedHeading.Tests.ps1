#Requires -Version 7.4
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
<#
    Requirement (Payton, 2026-10-19 19:27, "Yes"): remove the embedded old section heading from the 10 FR records whose
    descriptions render a duplicate heading, then regenerate docs/Project. Acceptance criteria:
      AC1 the duplicate heading line "## <ID>: ..." (or "## <ID> ...") and the blank lines after it are removed;
      AC2 every other character of the description is kept exactly;
      AC3 a description without its own heading, with another id's heading, or with the heading not first is refused;
      AC4 a description that would become empty is refused;
      AC5 against the live before.json snapshot, all 10 records transform, each result has no "## <ID>" line, and
          each result equals the original minus exactly its first line and following blank lines.
#>
BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'FrEmbeddedHeading.psm1') -Force
    $script:before = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'before.json') -Raw | ConvertFrom-Json
}

Describe 'Remove-FrEmbeddedHeading' {
    It 'AC1/AC2: removes the colon-form heading and following blank lines, keeps the rest exactly' {
        $d = "## FR-X-001: Old Title`n`n**ID:** FR-X-001`n### Description`n`nBody  `n"
        Remove-FrEmbeddedHeading -Id 'FR-X-001' -Description $d | Should -BeExactly "**ID:** FR-X-001`n### Description`n`nBody  `n"
    }
    It 'AC1: removes the space-form heading' {
        Remove-FrEmbeddedHeading -Id 'FR-X-001' -Description "## FR-X-001 Old Title`nBody" | Should -BeExactly 'Body'
    }
    It 'AC2: keeps CRLF line endings in the remainder' {
        Remove-FrEmbeddedHeading -Id 'FR-X-001' -Description "## FR-X-001: T`r`n`r`nA`r`nB" | Should -BeExactly "A`r`nB"
    }
    It 'AC3 negative: refuses a description with no heading' {
        { Remove-FrEmbeddedHeading -Id 'FR-X-001' -Description 'Plain body' } | Should -Throw '*does not start*'
    }
    It 'AC3 negative: refuses another id heading' {
        { Remove-FrEmbeddedHeading -Id 'FR-X-001' -Description "## FR-X-002: Other`nBody" } | Should -Throw '*does not start*'
    }
    It 'AC3 negative: refuses an id that is only a prefix of the heading id' {
        { Remove-FrEmbeddedHeading -Id 'FR-X-00' -Description "## FR-X-001: T`nBody" } | Should -Throw '*does not start*'
    }
    It 'AC3 negative: refuses a heading that is not on the first line' {
        { Remove-FrEmbeddedHeading -Id 'FR-X-001' -Description "Intro`n## FR-X-001: T`nBody" } | Should -Throw '*does not start*'
    }
    It 'AC3 negative: refuses a level-3 heading' {
        { Remove-FrEmbeddedHeading -Id 'FR-X-001' -Description "### FR-X-001: T`nBody" } | Should -Throw '*does not start*'
    }
    It 'AC4 negative: refuses a description that would become empty' {
        { Remove-FrEmbeddedHeading -Id 'FR-X-001' -Description "## FR-X-001: T`n`n" } | Should -Throw '*empty*'
    }
}

Describe 'AC5: live snapshot (before.json, 10 records)' {
    It 'has exactly the 10 expected ids' {
        @($script:before.id | Sort-Object) -join ',' | Should -BeExactly 'FR-CFG-002,FR-INP-006,FR-MED-002,FR-MED-003,FR-ROMM-LAUNCH-001,FR-SID-013,FR-SNP-001,FR-SNP-002,FR-UI-003,FR-VIC20-002'
    }
    It '<_> transforms, loses only its first line and blank lines, and keeps no own heading' -ForEach @('FR-CFG-002', 'FR-INP-006', 'FR-MED-002', 'FR-MED-003', 'FR-ROMM-LAUNCH-001', 'FR-SID-013', 'FR-SNP-001', 'FR-SNP-002', 'FR-UI-003', 'FR-VIC20-002') {
        $id = $_
        $r = $script:before | Where-Object id -EQ $id
        $new = Remove-FrEmbeddedHeading -Id $id -Description $r.description
        $new | Should -Not -Match ('(?m)^##\s+' + [regex]::Escape($id) + '\b')
        $first = ([string]$r.description -split "`r?`n")[0]
        $first | Should -Match ('^##\s+' + [regex]::Escape($id))
        $r.description.EndsWith($new) | Should -BeTrue
        $r.description.Substring(0, $r.description.Length - $new.Length).Trim() | Should -BeExactly $first.Trim()
    }
}
