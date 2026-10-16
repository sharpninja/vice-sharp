[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$Destination
)

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$backupRoot = [IO.Path]::GetFullPath($Destination)
$allowedRoot = Join-Path $repository '.local-preservation'
if (!$backupRoot.StartsWith($allowedRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Destination must be a new directory beneath .local-preservation.'
}
if (Test-Path -LiteralPath $backupRoot) { throw 'Destination already exists; refusing overwrite.' }
$roots = @('F:\GitHub\vice-sharp-romm', 'F:\GitHub\vice-sharp-romless')
$inventories = @{}
foreach ($root in $roots) {
    $resolved = (Resolve-Path -LiteralPath $root).Path
    if ($resolved -ne $root) { throw "Unexpected source resolution: $root" }
    $entries = @(Get-ChildItem -LiteralPath $root -Recurse -Force)
    if (@($entries | Where-Object Attributes -Match 'ReparsePoint').Count) {
        throw "Reparse points require an explicit preservation policy: $root"
    }
    $inventories[$root] = $entries
}
if (!$PSCmdlet.ShouldProcess($backupRoot, 'Copy and SHA256-verify both linked worktrees, retaining originals')) { return }
$null = New-Item -ItemType Directory -Path $backupRoot
foreach ($root in $roots) {
    $name = Split-Path $root -Leaf
    $snapshot = Join-Path $backupRoot $name
    $null = New-Item -ItemType Directory -Path $snapshot
    $manifest = [Collections.Generic.List[object]]::new()
    foreach ($entry in $inventories[$root]) {
        $relative = [IO.Path]::GetRelativePath($root, $entry.FullName)
        $target = Join-Path $snapshot $relative
        if ($entry.PSIsContainer) {
            $null = New-Item -ItemType Directory -Path $target -Force
            continue
        }
        $null = New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force
        $before = (Get-FileHash -LiteralPath $entry.FullName -Algorithm SHA256).Hash
        Copy-Item -LiteralPath $entry.FullName -Destination $target
        $copied = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        $after = (Get-FileHash -LiteralPath $entry.FullName -Algorithm SHA256).Hash
        if ($before -ne $copied -or $before -ne $after) { throw "Copy/source changed: $relative" }
        $manifest.Add([ordered]@{ path = $relative; bytes = $entry.Length; sha256 = $copied })
    }
    $head = & git -C $root rev-parse HEAD
    if ($LASTEXITCODE) { throw 'Could not read source HEAD.' }
    $branch = & git -C $root branch --show-current
    if ($LASTEXITCODE) { throw 'Could not read source branch.' }
    $status = @(& git -C $root status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE) { throw 'Could not read source status.' }
    $bundle = Join-Path $backupRoot "$name.bundle"
    & git -C $repository bundle create $bundle $branch '^main'
    if ($LASTEXITCODE) { throw 'Branch preservation bundle failed.' }
    & git -C $repository bundle verify $bundle
    if ($LASTEXITCODE) { throw 'Branch preservation bundle verification failed.' }
    $receipt = [ordered]@{
        capturedAt = [TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTimeOffset]::UtcNow, 'America/Chicago').ToString('s')
        source = $root; snapshot = $snapshot; head = $head; branch = $branch
        status = $status; files = $manifest.ToArray(); bundle = $bundle
        bundleSha256 = (Get-FileHash -LiteralPath $bundle -Algorithm SHA256).Hash
        prerequisites = 'Existing main history; branch refs are retained. Bundle is incremental.'
    }
    $receipt | ConvertTo-Json -Depth 12 | Out-File -LiteralPath (Join-Path $backupRoot "$name.manifest.json") -Encoding utf8NoBOM
    [pscustomobject]@{ Source = $root; Snapshot = $snapshot; Files = $manifest.Count; HashMismatches = 0; Branch = $branch; Head = $head }
}
