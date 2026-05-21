[CmdletBinding()]
param(
    [string]$SourceRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'native/vice/vice/src'),
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'docs/requirements/backfill/Classic-VICE-Edge-Case-Inventory.md')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

$sourceExtensions = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($extension in @('.c', '.h', '.cc', '.cpp', '.cxx', '.hpp', '.inc')) {
    [void]$sourceExtensions.Add($extension)
}

$patterns = @(
    [pscustomobject]@{
        Name = 'Commented Compatibility Warning'
        Regex = [regex]'(?i)\b(HACK|TODO|FIXME|KLUDGE|XXX|workaround)\b'
    },
    [pscustomobject]@{
        Name = 'Illegal or Invalid Mode'
        Regex = [regex]'(?i)\b(illegal|invalid|undefined|undocumented|forbidden)\b'
    },
    [pscustomobject]@{
        Name = 'Timing Latch or Delay'
        Regex = [regex]'(?i)\b(delay|delayed|latency|latch|underflow|overflow|timeout|alarm|pipeline|race)\b'
    },
    [pscustomobject]@{
        Name = 'Video Edge Timing'
        Regex = [regex]'(?i)\b(badline|border|sprite_dma|sprite|COL_NONE|open[ -]?border|VSP|FLI|AFLI|raster|CSEL|RSEL|DMA)\b'
    },
    [pscustomobject]@{
        Name = 'CPU Bus or Interrupt Edge'
        Regex = [regex]'(?i)\b(BA|AEC|NMI|IRQ|BRK|SEI|dummy|idle|open[ -]?bus|openbus|read_modify_write|DMA)\b'
    },
    [pscustomobject]@{
        Name = 'Media Protocol Edge'
        Regex = [regex]'(?i)\b(syncmark|weak|GCR|sector|BAM|REL|EOF|EOI|ATN|IEC|TAP|pulse|long[ -]?pulse|datasette)\b'
    },
    [pscustomobject]@{
        Name = 'Machine Model Constant'
        Regex = [regex]'(?i)\b(PAL|NTSC|6569|8565|VICII|C128|DTV|VIC20|PLUS4|PET|SCPU64)\b'
    },
    [pscustomobject]@{
        Name = 'Named Cycle Constant'
        Regex = [regex]'(?i)\b(CYCLE|LINE|CLOCK|PHASE|TICK|TIMEOUT|BORDER|SPRITE|DMA|IRQ|NMI)_[A-Z0-9_]+\b'
    }
)

function Get-Subsystem {
    param([string]$RelativePath)

    $path = $RelativePath -replace '\\', '/'
    if ($path -match '^(viciisc|vicii|raster|video)/') { return 'VIC/VIC-II/video' }
    if ($path -match '^vdc/') { return 'VDC/video' }
    if ($path -match '^(sid|resid|resid-dtv)/') { return 'SID/audio' }
    if ($path -match '^(serial|vdrive|lib/p64)/') { return 'serial/IEC/vdrive/media' }
    if ($path -match '^(tape|tapeport)/') { return 'tape/datasette' }
    if ($path -match '^(pet|plus4|vic20|scpu64)/') { return 'machine-specific deferred' }
    if ($path -match '^monitor/') { return 'monitor/debug' }
    if ($path -match '^(parallel|printerdrv|rs232drv|samplerdrv|userport)/') { return 'peripheral/host I/O' }
    if ($path -match '(^|/)(maincpu|mainc64cpu|ram|mem|interrupt|alarm)') { return 'CPU/bus/memory' }
    return 'core/shared'
}

function New-NormalizedSignature {
    param([string]$Text)

    return (($Text.Trim().ToLowerInvariant() `
        -replace '0x[0-9a-f]+', '0x#' `
        -replace '\b\d+\b', '#' `
        -replace '\s+', ' ') `
        -replace '[{}();,]+$', '')
}

$files = Get-ChildItem -LiteralPath $resolvedSourceRoot -Recurse -File |
    Where-Object { $sourceExtensions.Contains($_.Extension) } |
    Sort-Object FullName

$findings = [System.Collections.Generic.List[object]]::new()
$signatureCounts = [System.Collections.Generic.Dictionary[string, int]]::new([StringComparer]::Ordinal)

foreach ($file in $files) {
    $relativePath = [System.IO.Path]::GetRelativePath($resolvedSourceRoot, $file.FullName)
    $subsystem = Get-Subsystem -RelativePath $relativePath
    $lineNumber = 0

    foreach ($line in [System.IO.File]::ReadLines($file.FullName)) {
        $lineNumber++
        $matchedPatternNames = [System.Collections.Generic.List[string]]::new()

        foreach ($pattern in $patterns) {
            if ($pattern.Regex.IsMatch($line)) {
                [void]$matchedPatternNames.Add($pattern.Name)
            }
        }

        if ($matchedPatternNames.Count -eq 0) {
            continue
        }

        $snippet = $line.Trim()
        if ($snippet.Length -gt 180) {
            $snippet = $snippet.Substring(0, 177) + '...'
        }

        $signature = New-NormalizedSignature -Text $snippet
        if ($signatureCounts.ContainsKey($signature)) {
            $signatureCounts[$signature]++
        } else {
            $signatureCounts[$signature] = 1
        }

        $normalizedPath = $relativePath -replace '\\', '/'
        $findings.Add([pscustomobject]@{
            Subsystem = $subsystem
            Path = $normalizedPath
            Line = $lineNumber
            Patterns = ($matchedPatternNames -join ', ')
            Signature = $signature
            Snippet = $snippet
        })
    }
}

$duplicateSignatures = @($signatureCounts.GetEnumerator() | Where-Object { $_.Value -gt 1 } | Sort-Object Value -Descending)
$generatedUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$relativeSourceRoot = [System.IO.Path]::GetRelativePath($repoRoot, $resolvedSourceRoot) -replace '\\', '/'

$builder = [System.Text.StringBuilder]::new()
[void]$builder.AppendLine('# Classic VICE Edge-Case Candidate Inventory')
[void]$builder.AppendLine()
[void]$builder.AppendLine('## Document Information')
[void]$builder.AppendLine()
[void]$builder.AppendLine('| Field | Value |')
[void]$builder.AppendLine('|-------|-------|')
[void]$builder.AppendLine(('| Source Root | `{0}` |' -f $relativeSourceRoot))
[void]$builder.AppendLine("| Generated UTC | $generatedUtc |")
[void]$builder.AppendLine("| Source Files Scanned | $($files.Count) |")
[void]$builder.AppendLine("| Candidate Lines | $($findings.Count) |")
[void]$builder.AppendLine("| Duplicate Normalized Signatures | $($duplicateSignatures.Count) |")
[void]$builder.AppendLine()
[void]$builder.AppendLine('## Review Rules')
[void]$builder.AppendLine()
[void]$builder.AppendLine('- This inventory is a scanner output, not a requirements list.')
[void]$builder.AppendLine('- Promote only observable compatibility behavior into TR records.')
[void]$builder.AppendLine('- Do not promote internal VICE implementation choices unless they encode behavior ViceSharp must match.')
[void]$builder.AppendLine('- Keep one TR per distinct observable behavior, not one TR per source line.')
[void]$builder.AppendLine()
[void]$builder.AppendLine('## Pattern Groups')
[void]$builder.AppendLine()
foreach ($pattern in $patterns) {
    [void]$builder.AppendLine(('- {0}: `{1}`' -f $pattern.Name, $pattern.Regex.ToString()))
}
[void]$builder.AppendLine()

foreach ($group in ($findings | Group-Object Subsystem | Sort-Object Name)) {
    [void]$builder.AppendLine("## $($group.Name)")
    [void]$builder.AppendLine()
    [void]$builder.AppendLine("| Source | Patterns | Snippet |")
    [void]$builder.AppendLine("|--------|----------|---------|")

    foreach ($finding in ($group.Group | Sort-Object Path, Line)) {
        $safeSnippet = $finding.Snippet.Replace('|', '\|')
        [void]$builder.AppendLine(('| `{0}:{1}` | {2} | `{3}` |' -f $finding.Path, $finding.Line, $finding.Patterns, $safeSnippet))
    }

    [void]$builder.AppendLine()
}

if ($duplicateSignatures.Count -gt 0) {
    [void]$builder.AppendLine('## Duplicate Signature Summary')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('| Count | Normalized Signature |')
    [void]$builder.AppendLine('|-------|----------------------|')

    foreach ($duplicate in ($duplicateSignatures | Select-Object -First 100)) {
        $safeSignature = $duplicate.Key.Replace('|', '\|')
        [void]$builder.AppendLine(('| {0} | `{1}` |' -f $duplicate.Value, $safeSignature))
    }

    if ($duplicateSignatures.Count -gt 100) {
        [void]$builder.AppendLine()
        [void]$builder.AppendLine("Additional duplicate signatures omitted from this summary: $($duplicateSignatures.Count - 100)")
    }
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

[System.IO.File]::WriteAllText($resolvedOutputPath, $builder.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Output "Scanned $($files.Count) source files under $relativeSourceRoot."
Write-Output "Wrote $($findings.Count) candidate lines to $resolvedOutputPath."
