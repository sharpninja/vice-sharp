[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
Set-Location -LiteralPath $root
Import-Module powershell-yaml
$manifest = Get-Content -LiteralPath 'docs/wiki.yaml' -Raw | ConvertFrom-Yaml
$navigation = @($manifest.navigation.children.document)
$problems = [Collections.Generic.List[string]]::new()
if ($manifest.schema -ne 'mcp-wiki-export/v1') { $problems.Add('Manifest schema mismatch') }
foreach ($doc in $manifest.documents) {
    if ($doc.id -notin $navigation) { $problems.Add("Navigation missing: $($doc.id)") }
    if ($doc.source -notlike 'generated:*' -and !(Test-Path -LiteralPath $doc.source)) {
        $problems.Add("Source missing: $($doc.source)")
    }
    if (@($doc.platforms).Count -ne 1 -or $doc.platforms[0] -ne 'github') {
        $problems.Add("Unexpected platform: $($doc.id)")
    }
    if (!(Test-Path -LiteralPath (Join-Path 'docs/Project/wiki/github' $doc.target))) {
        $problems.Add("Export missing: $($doc.target)")
    }
}
foreach ($property in @('id', 'target')) {
    if (@($manifest.documents | Group-Object -Property $property | Where-Object Count -GT 1).Count) {
        $problems.Add("Duplicate manifest $property")
    }
}
$sources = @('README.md', 'docs/README.md', 'docs/USER-GUIDE.md',
    'src/ViceSharp.RomM/README.md', 'src/ViceSharp.Avalonia/README.md',
    'docs/testing/RomM-Desktop.md')
$localLinks = 0
foreach ($source in $sources) {
    $body = Get-Content -LiteralPath $source -Raw
    foreach ($match in [regex]::Matches($body, '\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value.Split('#')[0]
        if (!$target -or $target -match '^[a-z][a-z0-9+.-]*:') { continue }
        $localLinks++
        $resolved = Join-Path (Split-Path (Join-Path $root $source) -Parent) $target
        if (!(Test-Path -LiteralPath $resolved)) { $problems.Add("Broken source link: $source -> $target") }
    }
}
$zipPath = Join-Path $root 'docs/requirements/requirements-wiki-documents.zip'
$archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
$zipEntries = $archive.Entries.Count
try {
    foreach ($entry in $archive.Entries) {
        $relative = $entry.FullName.Replace('/', [IO.Path]::DirectorySeparatorChar)
        $destination = [IO.Path]::GetFullPath((Join-Path $root "docs/Project/wiki/$relative"))
        if (!$destination.StartsWith((Join-Path $root 'docs/Project/wiki') + [IO.Path]::DirectorySeparatorChar,
                [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe archive entry' }
        if (!(Test-Path -LiteralPath $destination)) { $problems.Add("ZIP file missing on disk: $relative"); continue }
        $stream = $entry.Open()
        try { $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) }
        finally { $stream.Dispose() }
        if ($hash -ne (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash) {
            $problems.Add("ZIP/disk mismatch: $relative")
        }
    }
}
finally { $archive.Dispose() }
$requirementBodies = 0
foreach ($file in @('Functional-Requirements.md', 'Technical-Requirements.md', 'Testing-Requirements.md',
        'TR-per-FR-Mapping.md', 'Requirements-Matrix.md')) {
    $path = "docs/Project/wiki/github/$file"
    $before = @(git show "HEAD:$path")
    if ($LASTEXITCODE -ne 0) { throw "git show failed: $path" }
    $after = @(Get-Content -LiteralPath $path)
    if ((($before | Select-Object -Skip 1) -join "`n") -cne (($after | Select-Object -Skip 1) -join "`n")) {
        $problems.Add("Requirement body changed beyond generated title: $file")
    }
    $requirementBodies++
}
$baselinePath = Join-Path $root '.local-preservation/worktrees-20261016T101333/native-before.json'
$baseline = Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
$nativeDifferences = 0
foreach ($file in $baseline) {
    $nativePath = Join-Path (Join-Path $root 'native/vice') $file.Path
    if (!(Test-Path -LiteralPath $nativePath) -or
        (Get-FileHash -LiteralPath $nativePath -Algorithm SHA256).Hash -ne $file.Sha256) { $nativeDifferences++ }
}
if ($nativeDifferences) { $problems.Add("Native baseline differences: $nativeDifferences") }
git diff --check
if ($LASTEXITCODE -ne 0) { $problems.Add('git diff --check failed') }
[ordered]@{
    checkedAtLocal = Get-Date -Format s
    documents = $manifest.documents.Count
    localSourceLinksChecked = $localLinks
    zipEntriesCompared = $zipEntries
    zipSha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    requirementBodiesUnchanged = $requirementBodies
    nativeBaselineFiles = $baseline.Count
    nativeHashDifferences = $nativeDifferences
    errors = @($problems)
    wikiRelativeLinkLimitation = 'triage-report-9af6dae387364ba296240585a2cd3110'
} | ConvertTo-Json -Depth 5
if ($problems.Count) { throw "$($problems.Count) documentation validation failure(s)" }
