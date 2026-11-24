#Requires -Version 7.0
<#
.SYNOPSIS
PLAN-NATIVERESTORE-001 D1, AC BuilderRerun_OnUpToDateTree_ReusesVerifiedLinkEvidence (round 3b A06): the Make
stage's link-line decision, factored out of native/build-vice-observer.ps1 so it can be proven with mocks first.

.DESCRIPTION
When make emits no link line for a target because the tree is already up to date, the retained
stock-link-lines.json may be reused only after every object file the retained line names still hashes exactly as
recorded in stock-link-objects.json at capture time; otherwise the stage stops with a named prerequisite before any
prior evidence is replaced. Freshly captured lines always win and refresh both files.
#>
Set-StrictMode -Version Latest

function Get-LinkLineObjectPath {
    <#
    .SYNOPSIS
    The object and archive tokens (.o, .a) of one link command line, in order, quotes stripped.
    #>
    [CmdletBinding()]
    [OutputType([System.Object[]])]
    param([Parameter(Mandatory)][string] $LinkLine)
    $tokens = [regex]::Matches($LinkLine, '"[^"]+"|\S+') | ForEach-Object { $_.Value.Trim('"') }
    return @($tokens | Where-Object { $_ -match '\.(o|a)$' })
}

function Resolve-StockLinkLine {
    <#
    .SYNOPSIS
    Decides the link lines for the Link stage from the lines captured in this make run and the retained receipts.

    .OUTPUTS
    A hashtable with Lines (target to link line), Objects (target to list of {Path, Sha256}), Source ('captured' or
    'reused' per target) and Written (whether the receipt files must be rewritten).
    #>
    [CmdletBinding()]
    [OutputType([hashtable])]
    param(
        [Parameter(Mandatory)][hashtable] $Captured,
        [Parameter(Mandatory)][string[]] $Targets,
        [Parameter(Mandatory)][string] $ReceiptsDirectory,
        # Returns the SHA256 of an object path relative to the build's src directory, or $null when the file is absent.
        [Parameter(Mandatory)][scriptblock] $ObjectHashProvider
    )
    $linesPath = Join-Path $ReceiptsDirectory 'stock-link-lines.json'
    $objectsPath = Join-Path $ReceiptsDirectory 'stock-link-objects.json'
    $retainedLines = @{}
    $retainedObjects = @{}
    if (Test-Path -LiteralPath $linesPath) {
        $parsed = Get-Content -LiteralPath $linesPath -Raw | ConvertFrom-Json
        foreach ($p in $parsed.PSObject.Properties) { $retainedLines[$p.Name] = [string]$p.Value }
    }
    if (Test-Path -LiteralPath $objectsPath) {
        $parsed = Get-Content -LiteralPath $objectsPath -Raw | ConvertFrom-Json
        foreach ($p in $parsed.PSObject.Properties) { $retainedObjects[$p.Name] = @($p.Value) }
    }
    $lines = @{}
    $objects = @{}
    $source = @{}
    $written = $false
    foreach ($t in $Targets) {
        if ($Captured.ContainsKey($t) -and $Captured[$t]) {
            $lines[$t] = [string]$Captured[$t]
            $objects[$t] = @(Get-LinkLineObjectPath -LinkLine $lines[$t] | ForEach-Object { [pscustomobject]@{ Path = $_; Sha256 = (& $ObjectHashProvider $_) } })
            $source[$t] = 'captured'
            $written = $true
            continue
        }
        if (-not $retainedLines.ContainsKey($t)) {
            throw "No link line captured for $t and no retained stock-link-lines.json entry to reuse; prerequisite: run the Make stage on a tree that relinks $t (for example after removing its program binary under the build's src directory), never by editing the materialization."
        }
        if (-not $retainedObjects.ContainsKey($t) -or @($retainedObjects[$t]).Count -eq 0) {
            throw "A retained link line exists for $t but no object hash list (stock-link-objects.json) proves its inputs; prerequisite: run the Make stage once on a tree that relinks $t so the hashes are recorded."
        }
        $mismatches = @()
        foreach ($entry in $retainedObjects[$t]) {
            $live = & $ObjectHashProvider ([string]$entry.Path)
            if ($null -eq $live -or $live -cne [string]$entry.Sha256) { $mismatches += [string]$entry.Path }
        }
        if ($mismatches.Count -gt 0) {
            throw ("The retained link line for {0} names {1} object(s) whose hash no longer matches the capture ({2}); prerequisite: run the Make stage on a tree that relinks {0}. No prior evidence was replaced." -f $t, $mismatches.Count, ($mismatches -join ', '))
        }
        $lines[$t] = $retainedLines[$t]
        $objects[$t] = @($retainedObjects[$t] | ForEach-Object { [pscustomobject]@{ Path = [string]$_.Path; Sha256 = [string]$_.Sha256 } })
        $source[$t] = 'reused'
    }
    return @{ Lines = $lines; Objects = $objects; Source = $source; Written = $written; LinesPath = $linesPath; ObjectsPath = $objectsPath }
}

Export-ModuleMember -Function Get-LinkLineObjectPath, Resolve-StockLinkLine
