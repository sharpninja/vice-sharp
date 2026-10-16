# Requires a test under tests/ for every requirement assigned to the phase
# being exited and to every earlier phase. Iteration is the phase number.
# Priority (P0, P1, P2) is severity and is not a phase.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [int]$ThroughPhase
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$requirementsRoot = Join-Path $repoRoot 'docs/requirements'
$testsRoot = Join-Path $repoRoot 'tests'
$idHeading = [regex]'^##\s+((?:FR|TR|TEST)-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3})\b'
$iterationLine = [regex]'\*\*Iteration:\*\*\s+(\d+)\b'
$acceptanceHeading = [regex]'^#{2,4}\s+Acceptance Criteria\s*$'
$criterionLine = [regex]'^\s*(?:\d+\.|[-*])\s+\S'

$inScope = [System.Collections.Generic.List[object]]::new()

Get-ChildItem -LiteralPath $requirementsRoot -Recurse -File -Filter '*.md' | ForEach-Object {
    $lines = [System.IO.File]::ReadAllLines($_.FullName)
    $sections = [System.Collections.Generic.List[object]]::new()
    $current = $null
    foreach ($line in $lines) {
        $heading = $idHeading.Match($line)
        if ($heading.Success) {
            if ($null -ne $current) {
                $sections.Add($current)
            }

            $current = [pscustomobject]@{
                Id = $heading.Groups[1].Value
                Iteration = $null
                AcceptanceCount = 0
                InAcceptance = $false
            }
            continue
        }

        if ($null -eq $current) {
            continue
        }

        $iteration = $iterationLine.Match($line)
        if ($iteration.Success) {
            $current.Iteration = [int]$iteration.Groups[1].Value
        }

        if ($acceptanceHeading.Match($line).Success) {
            $current.InAcceptance = $true
            continue
        }

        if ($current.InAcceptance -and $line -match '^#{1,4}\s+') {
            $current.InAcceptance = $false
        }

        if ($current.InAcceptance -and $criterionLine.Match($line).Success) {
            $current.AcceptanceCount++
        }
    }

    if ($null -ne $current) {
        $sections.Add($current)
    }

    foreach ($section in $sections) {
        if ($null -eq $section.Iteration) {
            continue
        }

        if ($section.Iteration -gt $ThroughPhase -or $section.AcceptanceCount -lt 1) {
            continue
        }

        $inScope.Add([pscustomobject]@{
            Id = $section.Id
            Iteration = $section.Iteration
            AcceptanceCount = $section.AcceptanceCount
        })
    }
}

$namedInTests = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$testId = [regex]'\b(?:FR|TR|TEST)-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}\b'
Get-ChildItem -LiteralPath $testsRoot -Recurse -File | Where-Object {
    $_.Extension -in @('.cs', '.md') -and $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]'
} | ForEach-Object {
    foreach ($match in $testId.Matches([System.IO.File]::ReadAllText($_.FullName))) {
        [void]$namedInTests.Add($match.Value)
    }
}

$missing = @($inScope | Where-Object { -not $namedInTests.Contains($_.Id) } | Sort-Object Iteration, Id)

Write-Output "Phase acceptance coverage"
Write-Output "Repository: $repoRoot"
Write-Output "Through phase: $ThroughPhase"
Write-Output "In-scope requirements with acceptance criteria: $($inScope.Count)"
Write-Output "Missing from tests/: $($missing.Count)"

foreach ($phase in ($missing | Select-Object -ExpandProperty Iteration -Unique | Sort-Object)) {
    $rows = @($missing | Where-Object { $_.Iteration -eq $phase })
    $criteria = ($rows | Measure-Object -Property AcceptanceCount -Sum).Sum
    Write-Output "Phase ${phase}: $($rows.Count) requirements, $criteria criteria"
    $rows | ForEach-Object { Write-Output "  $($_.Id)" }
}

if ($missing.Count -gt 0) {
    exit 1
}
