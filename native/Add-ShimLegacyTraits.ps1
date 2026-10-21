#Requires -Version 7.0
<#
.SYNOPSIS
PLAN-NATIVERESTORE-001 D2 (R6): adds Trait("Category", "ShimLegacy") to exactly the test methods named by the D0
semantic shim-consumer inventory (Get-ShimConsumerInventory.ps1 output).

.DESCRIPTION
For every consumer entry (file, class, method) the script locates the method declaration, finds the contiguous
attribute block above it and inserts the trait as the first attribute with the declaration's indentation, using the
short form when the file has "using Xunit;" and the qualified Xunit.Trait form otherwise. Methods already carrying the
trait are reported as alreadyTagged and left alone, so the script is idempotent. A method that cannot be found, or
that matches more than one declaration, is reported and makes the script fail without writing that file. Nothing
outside the inventory is touched: a managed fake, a string-only mention or a plain test in the same file stays as it
is. Line endings and byte order marks are preserved. Each tagged method is classified to the phase that rebinds it:
Phase F for VIC-20 (xvic) consumers, Phase E for every other (C64, x64sc) consumer. Supports -WhatIf.

.OUTPUTS
A report object (also written as JSON to -ReportPath when given) with tagged, alreadyTagged, notFound and ambiguous
lists and their counts.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string] $InventoryPath,
    [Parameter(Mandatory)][string] $TestRoot,
    [string] $ReportPath
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$traitText = 'Trait("Category", "ShimLegacy")'
$inventory = Get-Content -LiteralPath $InventoryPath -Raw | ConvertFrom-Json
$consumers = @($inventory.consumers)

function Get-RebindPhase {
    param([string] $File, [string] $ClassName)
    if ($File -match '(^|/)Vic20/' -or $ClassName -match 'Vic20|Xvic|VicI\b|Vic1\b') { return 'Phase F (VIC-20 xvic observer rebind, repair pair 4)' }
    return 'Phase E (C64 x64sc observer rebind)'
}

function Find-MethodDeclaration {
    param([string[]] $Lines, [string] $Method)
    $pattern = '^\s*(?:(?:public|internal|private|protected|static|async|unsafe|virtual|override|new)\s+)*[\w<>\[\],\.\?\s]+?\s+' + [regex]::Escape($Method) + '\s*(?:<[^>]+>)?\s*\('
    $hits = @()
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match $pattern) { $hits += $i }
    }
    return ,$hits
}

function Get-AttributeBlockStart {
    param([string[]] $Lines, [int] $DeclarationIndex)
    $start = $DeclarationIndex
    for ($i = $DeclarationIndex - 1; $i -ge 0; $i--) {
        $t = $Lines[$i].Trim()
        if ($t.Length -eq 0) { break }
        if ($t.StartsWith('//') -or $t.StartsWith('#')) { break }
        if ($t.EndsWith('{') -or $t.EndsWith('}') -or $t.EndsWith(';')) { break }
        $start = $i
    }
    return $start
}

$tagged = [Collections.Generic.List[object]]::new()
$already = [Collections.Generic.List[object]]::new()
$notFound = [Collections.Generic.List[object]]::new()
$ambiguous = [Collections.Generic.List[object]]::new()
$filesWritten = 0

foreach ($group in ($consumers | Group-Object -Property file)) {
    $relative = $group.Name
    $path = [IO.Path]::Combine($TestRoot, $relative)
    if (-not (Test-Path -LiteralPath $path)) {
        foreach ($c in $group.Group) { $notFound.Add([pscustomobject]@{ file = $relative; class = $c.class; method = $c.method; reason = 'file missing' }) }
        continue
    }
    $bytes = [IO.File]::ReadAllBytes($path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $raw = [Text.Encoding]::UTF8.GetString($bytes, ($hasBom ? 3 : 0), $bytes.Length - ($hasBom ? 3 : 0))
    $newline = if ($raw.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = [Collections.Generic.List[string]]::new([string[]]($raw -split "`r?`n"))
    $usesXunit = @($lines | Where-Object { $_ -match '^\s*(global\s+)?using\s+Xunit\s*;' }).Count -gt 0
    $attributeLine = if ($usesXunit) { '[' + $traitText + ']' } else { '[Xunit.' + $traitText + ']' }
    $fileChanged = $false
    $fileFailed = $false

    # Process methods bottom-up so earlier insertions never shift later indices.
    $ordered = @()
    foreach ($c in $group.Group) {
        $hits = Find-MethodDeclaration -Lines $lines.ToArray() -Method $c.method
        if ($hits.Count -eq 0) { $notFound.Add([pscustomobject]@{ file = $relative; class = $c.class; method = $c.method; reason = 'declaration not found' }); $fileFailed = $true; continue }
        if ($hits.Count -gt 1) { $ambiguous.Add([pscustomobject]@{ file = $relative; class = $c.class; method = $c.method; lines = @($hits | ForEach-Object { $_ + 1 }) }); $fileFailed = $true; continue }
        $ordered += [pscustomobject]@{ consumer = $c; index = $hits[0] }
    }
    if ($fileFailed) { continue }

    foreach ($entry in ($ordered | Sort-Object -Property index -Descending)) {
        $c = $entry.consumer
        $decl = $entry.index
        $blockStart = Get-AttributeBlockStart -Lines $lines.ToArray() -DeclarationIndex $decl
        $block = @($lines.GetRange($blockStart, $decl - $blockStart))
        $record = [pscustomobject]@{
            file = $relative; class = $c.class; method = $c.method; attribute = $c.attribute; reason = $c.reason; via = $c.via
            line = $blockStart + 1; rebind = Get-RebindPhase -File $relative -ClassName $c.class
        }
        if (@($block | Where-Object { $_ -match [regex]::Escape($traitText) }).Count -gt 0) { $already.Add($record); continue }
        $indent = ([regex]::Match($lines[$decl], '^\s*')).Value
        $lines.Insert($blockStart, $indent + $attributeLine)
        $tagged.Add($record)
        $fileChanged = $true
    }

    if ($fileChanged -and $PSCmdlet.ShouldProcess($relative, 'insert ShimLegacy traits')) {
        $text = [string]::Join($newline, $lines.ToArray())
        [IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($hasBom))
        $filesWritten++
    }
}

$report = [pscustomobject]@{
    inventoryPath = $InventoryPath
    inventoryConsumers = $consumers.Count
    counts = [pscustomobject]@{ tagged = $tagged.Count; alreadyTagged = $already.Count; notFound = $notFound.Count; ambiguous = $ambiguous.Count; filesWritten = $filesWritten }
    tagged = @($tagged)
    alreadyTagged = @($already)
    notFound = @($notFound)
    ambiguous = @($ambiguous)
}
if ($ReportPath -and $PSCmdlet.ShouldProcess($ReportPath, 'write report')) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $ReportPath) -Force | Out-Null
    $report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $ReportPath -Encoding utf8
}
Write-Output $report
if ($notFound.Count -gt 0 -or $ambiguous.Count -gt 0) {
    throw ('ShimLegacy tagging incomplete: notFound ' + $notFound.Count + ', ambiguous ' + $ambiguous.Count)
}
