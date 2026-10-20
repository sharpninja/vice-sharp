#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only: expands the stock link inputs of x64sc and xvic from the generated VICE Makefile of the LEGION2
    materialization (cc67418cba58e71d8e70fd353e9de47199ad2075, src/Makefile): the base objects (am__objects_1),
    midi objects, <machine>_LDADD (libraries, emu_extlibs, resources) and LIBS. Makefile variables are expanded
    recursively ($(name) and ${name}; $(OBJEXT) is o, $(EXEEXT) is .exe, $(top_builddir) is ..). As a self-check
    the xvic expansion is compared with the Oct 8 recorded gated xvic link
    (20261008T082100-0500-phase2-feasibility/gate-runtime/link.launch.json). Writes vice-link-inputs.json.
#>
[CmdletBinding()]
param(
    [string]$Makefile = 'F:\GitHub\vice-sharp\artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source\vice\src\Makefile',
    [string]$RecordedXvicLink = 'F:\GitHub\vice-sharp\docs\receipts\native-vice\20261008T082100-0500-phase2-feasibility\gate-runtime\link.launch.json',
    [string]$OutPath = (Join-Path $PSScriptRoot 'vice-link-inputs.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$text = [System.IO.File]::ReadAllText($Makefile) -replace "\\\r?\n", ' '
$vars = @{ OBJEXT = 'o'; EXEEXT = '.exe'; top_builddir = '..'; srcdir = '.' }
foreach ($m in [regex]::Matches($text, '(?m)^([A-Za-z_][A-Za-z0-9_]*)\s*=[ \t]*(.*)$')) {
    if (-not $vars.ContainsKey($m.Groups[1].Value)) { $vars[$m.Groups[1].Value] = $m.Groups[2].Value }
}
function Expand-Value([string]$Value, [int]$Depth = 0) {
    if ($Depth -gt 30) { throw "expansion too deep: $Value" }
    $out = [regex]::Replace($Value, '\$[({]([A-Za-z_][A-Za-z0-9_]*)[)}]', {
            param($x)
            $n = $x.Groups[1].Value
            if ($vars.ContainsKey($n)) { Expand-Value $vars[$n] ($Depth + 1) } else { '' }
        })
    ($out -replace '\s+', ' ').Trim()
}
function Split-Token([string]$Value) { @($Value -split ' ' | Where-Object { $_ -and $_ -notmatch '^@.*@$' }) }

$result = [ordered]@{ makefile = $Makefile; makefileSha256 = (Get-FileHash -LiteralPath $Makefile).Hash; at = (Get-Date -Format s) }
$baseObjects = @(Split-Token (Expand-Value '$(am__objects_1)'))
$result.baseObjects = $baseObjects
foreach ($machine in 'x64sc', 'xvic') {
    $midi = @(Split-Token (Expand-Value ('$(am__objects_2)')))
    $result["${machine}_ldadd"] = @(Split-Token (Expand-Value "`$(${machine}_LDADD)"))
    $result["${machine}_midiObjects"] = @($midi)
}
$result.LIBS = @(Split-Token (Expand-Value '$(LIBS)'))
$result.LDFLAGS = Expand-Value '$(LDFLAGS)'
$result.CC = Expand-Value '$(CC)'
$result.CXX = Expand-Value '$(CXX)'
$result.CFLAGS = Expand-Value '$(CFLAGS)'
$result.CXXFLAGS = Expand-Value '$(CXXFLAGS)'

# Self-check against the Oct 8 recorded gated xvic link (stock inputs after the -o output).
$rec = (Get-Content -LiteralPath $RecordedXvicLink -Raw | ConvertFrom-Json).arguments
$afterOut = @($rec[([array]::IndexOf($rec, '-o') + 2)..($rec.Count - 1)])
$expected = @($baseObjects) + @($result.xvic_midiObjects) + @($result.xvic_ldadd) + @($result.LIBS)
$result.xvicSelfCheck = [ordered]@{
    recordedCount = $afterOut.Count
    expandedCount = $expected.Count
    identicalSequence = (($afterOut -join ' ') -ceq ($expected -join ' '))
    onlyRecorded = @($afterOut | Where-Object { $_ -notin $expected })
    onlyExpanded = @($expected | Where-Object { $_ -notin $afterOut })
}
$result | ConvertTo-Json -Depth 5 | Out-File -LiteralPath $OutPath -Encoding utf8NoBOM
'xvic self-check: recorded {0} vs expanded {1}, identical sequence {2}, only recorded [{3}], only expanded [{4}]' -f $afterOut.Count, $expected.Count, $result.xvicSelfCheck.identicalSequence, ($result.xvicSelfCheck.onlyRecorded -join ' '), ($result.xvicSelfCheck.onlyExpanded -join ' ')
'x64sc: {0} base objects, {1} midi objects, {2} LDADD tokens, LIBS {3}' -f $baseObjects.Count, $result.x64sc_midiObjects.Count, $result.x64sc_ldadd.Count, ($result.LIBS -join ' ')
'x64sc LDADD: ' + ($result.x64sc_ldadd -join ' ')
'CXX={0} | LDFLAGS={1}' -f $result.CXX, $result.LDFLAGS
