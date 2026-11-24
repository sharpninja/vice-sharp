#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only (OMAR-VICE# request 2026-10-19T20:26): hash list of the PAYTON-LEGION2 materialization of VICE cc67418c,
    in OMAR-VICE#'s format: sorted lines "<sha256>|<bytes>|<relative path with forward slashes>" relative to the source
    root, then SHA256 of that list file (UTF-8 without BOM, LF line endings, trailing LF).
    Two lists, because this materialization was built in place on Oct 8 and also holds generated files:
      tracked-files.txt: every file git tracks at HEAD (git ls-files), hashed from disk;
      all-files.txt: every file on disk except the .git folder.
    Also records whether any tracked file differs from HEAD (git status) and the .gitattributes eol rules.
#>
[CmdletBinding()]
param(
    [string]$Source = 'F:\GitHub\vice-sharp\artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source',
    [string]$OutDir = $PSScriptRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath($Source).TrimEnd('\') + '\'
$utf8 = [System.Text.UTF8Encoding]::new($false)

function Write-HashList([string[]]$Rel, [string]$Name) {
    $lines = foreach ($r in $Rel) {
        $full = Join-Path $root ($r -replace '/', '\')
        '{0}|{1}|{2}' -f (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant(), (Get-Item -LiteralPath $full -Force).Length, $r
    }
    $sorted = @($lines | Sort-Object -CaseSensitive)
    $path = Join-Path $OutDir $Name
    [System.IO.File]::WriteAllText($path, (($sorted -join "`n") + "`n"), $utf8)
    [pscustomobject]@{ List = $Name; Files = $sorted.Count; Bytes = ($sorted | ForEach-Object { [long]($_.Split('|')[1]) } | Measure-Object -Sum).Sum; ListSha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
}

$head = (git -c safe.directory=* -C $Source rev-parse HEAD).Trim()
$tracked = @(git -c safe.directory=* -c core.quotepath=off -C $Source ls-files | Where-Object { Test-Path -LiteralPath (Join-Path $root ($_ -replace '/', '\')) -PathType Leaf })
$trackedListed = @(git -c safe.directory=* -c core.quotepath=off -C $Source ls-files).Count
$dirty = @(git -c safe.directory=* -c color.ui=never -C $Source status --porcelain=v1 --untracked-files=no)
$all = @(Get-ChildItem -LiteralPath $Source -Recurse -File -Force | Where-Object { $_.FullName -notmatch '\\\.git(\\|$)' } | ForEach-Object { $_.FullName.Substring($root.Length) -replace '\\', '/' })
$eolRules = @(Get-ChildItem -LiteralPath $Source -Recurse -File -Force -Filter '.gitattributes' | Where-Object { $_.FullName -notmatch '\\\.git\\' } | ForEach-Object {
        $rel = $_.FullName.Substring($root.Length) -replace '\\', '/'
        foreach ($l in [System.IO.File]::ReadAllLines($_.FullName)) { if ($l -match '(?i)\b(eol|text|crlf|export-ignore)\b') { "$rel : $l" } }
    })

$t = Write-HashList $tracked 'tracked-files.txt'
$a = Write-HashList $all 'all-files.txt'
$summary = [ordered]@{
    at = (Get-Date -Format s); source = $Source; head = $head; coreAutocrlf = [string](git -c safe.directory=* -C $Source config --get core.autocrlf)
    trackedListedByGit = $trackedListed; trackedPresentOnDisk = $tracked.Count; trackedDifferingFromHead = $dirty.Count
    tracked = $t; allFiles = $a; generatedOrUntracked = $all.Count - $tracked.Count; gitattributesRules = $eolRules
}
$summary | ConvertTo-Json -Depth 4 | Out-File -LiteralPath (Join-Path $OutDir 'summary.json') -Encoding utf8NoBOM
$summary | ConvertTo-Json -Depth 4
