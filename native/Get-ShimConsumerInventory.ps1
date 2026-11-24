#Requires -Version 7.0
<#
.SYNOPSIS
PLAN-NATIVERESTORE-001 D0: semantic per-method inventory of the tests that consume the retired native shim (R6).

.DESCRIPTION
Enumerates every C# file under the test root (bin and obj excluded), strips comments and string literals so that
mentions in documentation or literals never count, and reports every test method ([Fact], [Theory], [ViceFact],
[ViceTheory]) that reaches the shim: by attribute (ViceFact or ViceTheory, which query ViceNative at construction),
directly (the method body references ViceNative., ViceNativeXvic, ViceNativeBridge or XvicOraclePrerequisites), or
transitively (the body references a helper type of the same project whose own body reaches the shim, computed to a
fixpoint). Helpers are scoped per project (first path segment under the root) because type names are not unique
across projects. Reflection on a type (typeof, nameof) does not execute it and is ignored. A managed fake that
merely implements IViceNative is not a consumer. Read only.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $TestRoot
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$rootPattern = '\bViceNative\s*\.|\bViceNativeXvic\b|\bViceNativeBridge\b|\bXvicOraclePrerequisites\b|\[\s*ViceFact\b|\[\s*ViceTheory\b'

function Clear-CommentAndStringText {
    param([string] $Text)
    $sb = [Text.StringBuilder]::new()
    $i = 0; $n = $Text.Length
    while ($i -lt $n) {
        $c = $Text[$i]
        $next = if ($i + 1 -lt $n) { $Text[$i + 1] } else { [char]0 }
        if ($c -eq '/' -and $next -eq '/') {
            while ($i -lt $n -and $Text[$i] -ne "`n") { $i++ }
            continue
        }
        if ($c -eq '/' -and $next -eq '*') {
            $i += 2
            while ($i + 1 -lt $n -and -not ($Text[$i] -eq '*' -and $Text[$i + 1] -eq '/')) { $i++ }
            $i += 2
            continue
        }
        if ($c -eq '"' -and $i + 2 -lt $n -and $Text[$i + 1] -eq '"' -and $Text[$i + 2] -eq '"') {
            $i += 3
            while ($i + 2 -lt $n -and -not ($Text[$i] -eq '"' -and $Text[$i + 1] -eq '"' -and $Text[$i + 2] -eq '"')) { $i++ }
            $i += 3
            [void]$sb.Append('""')
            continue
        }
        if (($c -eq '@' -and $next -eq '"') -or ($c -eq '$' -and $next -eq '@' -and $i + 2 -lt $n -and $Text[$i + 2] -eq '"')) {
            $i += if ($c -eq '@') { 2 } else { 3 }
            while ($i -lt $n) {
                if ($Text[$i] -eq '"') {
                    if ($i + 1 -lt $n -and $Text[$i + 1] -eq '"') { $i += 2; continue }
                    break
                }
                $i++
            }
            $i++
            [void]$sb.Append('""')
            continue
        }
        if ($c -eq '"') {
            $i++
            while ($i -lt $n -and $Text[$i] -ne '"') {
                if ($Text[$i] -eq '\') { $i++ }
                $i++
            }
            $i++
            [void]$sb.Append('""')
            continue
        }
        if ($c -eq "'") {
            $i++
            while ($i -lt $n -and $Text[$i] -ne "'") {
                if ($Text[$i] -eq '\') { $i++ }
                $i++
            }
            $i++
            [void]$sb.Append("''")
            continue
        }
        [void]$sb.Append($c)
        $i++
    }
    return $sb.ToString()
}

function Find-MatchingBrace {
    param([string] $Text, [int] $OpenIndex)
    $depth = 0
    for ($k = $OpenIndex; $k -lt $Text.Length; $k++) {
        if ($Text[$k] -eq '{') { $depth++ }
        elseif ($Text[$k] -eq '}') { $depth--; if ($depth -eq 0) { return $k } }
    }
    return $Text.Length - 1
}

# Reflection on a type (typeof, nameof) does not execute it and never loads a native library; strip before matching.
function Clear-ReflectionReference {
    param([string] $Text)
    return [regex]::Replace($Text, '\b(typeof|nameof)\s*\(\s*[\w\.<>\[\],\s]+\)', '')
}

$files = @(Get-ChildItem -LiteralPath $TestRoot -Recurse -File -Filter '*.cs' | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
$rootFull = (Resolve-Path -LiteralPath $TestRoot).Path

# Pass 1: every type with its stripped body, scoped to its project.
$types = [System.Collections.Generic.List[object]]::new()
foreach ($f in $files) {
    $stripped = Clear-CommentAndStringText -Text ([IO.File]::ReadAllText($f.FullName))
    $relative = [IO.Path]::GetRelativePath($rootFull, $f.FullName)
    $project = ($relative -split '[\\/]')[0]
    foreach ($m in [regex]::Matches($stripped, '\b(class|struct|record)\s+([A-Za-z_]\w*)')) {
        $open = $stripped.IndexOf('{', $m.Index)
        if ($open -lt 0) { continue }
        $close = Find-MatchingBrace -Text $stripped -OpenIndex $open
        $body = $stripped.Substring($open, $close - $open + 1)
        $types.Add([ordered]@{ type = $m.Groups[2].Value; project = $project; file = $f.FullName; relative = $relative; body = $body; matchBody = (Clear-ReflectionReference -Text $body) })
    }
}

# Pass 2: helper closure per project (types whose body reaches the shim directly or through another helper of the
# same project).
$helpers = @{}
foreach ($t in $types) { if ($t.matchBody -match $rootPattern) { $helpers[$t.project + '/' + $t.type] = 'direct' } }
$changed = $true
while ($changed) {
    $changed = $false
    foreach ($t in $types) {
        $key = $t.project + '/' + $t.type
        if ($helpers.ContainsKey($key)) { continue }
        foreach ($hk in @($helpers.Keys)) {
            $hp, $hn = $hk -split '/', 2
            if ($hp -ne $t.project -or $hn -eq $t.type) { continue }
            if ($t.matchBody -match ('\b' + [regex]::Escape($hn) + '\b')) { $helpers[$key] = ('via ' + $hn); $changed = $true; break }
        }
    }
}

# Pass 3: test methods.
$consumers = [System.Collections.Generic.List[object]]::new()
$testMethods = 0
$methodPattern = '\[\s*(?<attr>Fact|Theory|ViceFact|ViceTheory)\b[^\]]*\](?:\s*\[[^\]]*\])*\s*(?:(?:public|internal|private|protected|static|async|unsafe|override|virtual)\s+)*[\w<>\[\],\.\s\?]+?\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*\('
foreach ($t in $types) {
    foreach ($m in [regex]::Matches($t.body, $methodPattern)) {
        $testMethods++
        $attr = $m.Groups['attr'].Value
        $name = $m.Groups['name'].Value
        $sigEnd = $t.body.IndexOf(')', $m.Index + $m.Length)
        $bodyStart = $t.body.IndexOf('{', $sigEnd)
        $arrow = $t.body.IndexOf('=>', $sigEnd)
        if ($arrow -ge 0 -and ($bodyStart -lt 0 -or $arrow -lt $bodyStart)) {
            $semi = $t.body.IndexOf(';', $arrow)
            $body = $t.body.Substring($arrow, [Math]::Max(0, $semi - $arrow))
        } elseif ($bodyStart -ge 0) {
            $bodyEnd = Find-MatchingBrace -Text $t.body -OpenIndex $bodyStart
            $body = $t.body.Substring($bodyStart, $bodyEnd - $bodyStart + 1)
        } else { $body = '' }
        $matchBody = Clear-ReflectionReference -Text $body
        $reason = $null; $via = $null
        if ($attr -in @('ViceFact', 'ViceTheory')) { $reason = 'attribute' }
        elseif ($matchBody -match $rootPattern) { $reason = 'direct' }
        else {
            foreach ($hk in @($helpers.Keys | Sort-Object)) {
                $hp, $hn = $hk -split '/', 2
                if ($hp -ne $t.project -or $hn -eq $t.type) { continue }
                if ($matchBody -match ('\b' + [regex]::Escape($hn) + '\b')) { $reason = 'transitive'; $via = $hn; break }
            }
        }
        if ($reason) {
            $consumers.Add([pscustomobject]@{ file = $t.relative; project = $t.project; class = $t.type; method = $name; attribute = $attr; reason = $reason; via = $via })
        }
    }
}

$helperRows = @($helpers.Keys | Sort-Object | ForEach-Object {
    $hp, $hn = $_ -split '/', 2
    $decl = $types | Where-Object { $_.project -eq $hp -and $_.type -eq $hn } | Select-Object -First 1
    [pscustomobject]@{ type = $hn; project = $hp; reason = $helpers[$_]; file = $decl.relative }
})

[pscustomobject]@{
    testRoot = $rootFull
    counts = [pscustomobject]@{ files = $files.Count; types = $types.Count; testMethods = $testMethods; consumers = $consumers.Count; helpers = $helpers.Count }
    helpers = $helperRows
    consumers = @($consumers | Sort-Object -Property file, class, method)
}
