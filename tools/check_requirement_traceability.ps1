[CmdletBinding()]
param(
    [switch]$FailOnMissing,
    [switch]$FailOnNonCanonical
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$requirementsRoot = Join-Path $repoRoot 'docs/requirements'
$sourceRoots = @(
    (Join-Path $repoRoot 'src'),
    (Join-Path $repoRoot 'tests')
) | Where-Object { Test-Path -LiteralPath $_ }

$canonicalPattern = '\*\*ID:\*\*\s+(?<id>(?:FR|TR|TEST)-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3})\b'
$referencePattern = '\b(?:FR|TR|TEST)-[A-Za-z0-9]+(?:-[A-Za-z0-9]+)*\b'

$canonical = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$referenced = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$nonCanonical = [System.Collections.Generic.SortedDictionary[string, System.Collections.Generic.List[string]]]::new([StringComparer]::Ordinal)

Get-ChildItem -LiteralPath $requirementsRoot -Recurse -File -Filter '*.md' | ForEach-Object {
    $text = Get-Content -LiteralPath $_.FullName -Raw
    foreach ($match in [regex]::Matches($text, $canonicalPattern)) {
        [void]$canonical.Add($match.Groups['id'].Value)
    }
}

foreach ($root in $sourceRoots) {
    Get-ChildItem -LiteralPath $root -Recurse -File | Where-Object {
        $_.Extension -in @('.cs', '.md')
    } | ForEach-Object {
        $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $_.FullName)
        $text = Get-Content -LiteralPath $_.FullName -Raw
        foreach ($match in [regex]::Matches($text, $referencePattern)) {
            $id = $match.Value
            if ($canonical.Contains($id)) {
                [void]$referenced.Add($id)
                continue
            }

            if (-not $nonCanonical.ContainsKey($id)) {
                $nonCanonical[$id] = [System.Collections.Generic.List[string]]::new()
            }

            if (-not $nonCanonical[$id].Contains($relativePath)) {
                $nonCanonical[$id].Add($relativePath)
            }
        }
    }
}

$missing = $canonical | Where-Object { -not $referenced.Contains($_) } | Sort-Object

Write-Output "Requirement traceability audit"
Write-Output "Repository: $repoRoot"
Write-Output "Canonical IDs: $($canonical.Count)"
Write-Output "Referenced canonical IDs in src/tests: $($referenced.Count)"
Write-Output "Canonical IDs not referenced in src/tests: $($missing.Count)"
Write-Output "Noncanonical IDs in src/tests: $($nonCanonical.Count)"

foreach ($prefix in @('FR', 'TR', 'TEST')) {
    $canonicalForPrefix = @($canonical | Where-Object { $_.StartsWith("$prefix-", [StringComparison]::Ordinal) })
    $referencedForPrefix = @($referenced | Where-Object { $_.StartsWith("$prefix-", [StringComparison]::Ordinal) })
    $missingForPrefix = @($missing | Where-Object { $_.StartsWith("$prefix-", [StringComparison]::Ordinal) })
    Write-Output "$prefix canonical/referenced/missing: $($canonicalForPrefix.Count)/$($referencedForPrefix.Count)/$($missingForPrefix.Count)"
}

if ($missing.Count -gt 0) {
    Write-Output ''
    Write-Output 'Canonical IDs not referenced in src/tests:'
    $missing | ForEach-Object { Write-Output "  $_" }
}

if ($nonCanonical.Count -gt 0) {
    Write-Output ''
    Write-Output 'Noncanonical IDs in src/tests:'
    foreach ($entry in $nonCanonical.GetEnumerator()) {
        $paths = $entry.Value | Sort-Object
        Write-Output "  $($entry.Key): $($paths -join ', ')"
    }
}

if (($FailOnMissing -and $missing.Count -gt 0) -or ($FailOnNonCanonical -and $nonCanonical.Count -gt 0)) {
    exit 1
}
