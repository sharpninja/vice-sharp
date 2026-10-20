[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
$workspaceRoot = '/home/sharpninja/github/vice-sharp'
$patchPath = Join-Path $workspaceRoot 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-tracked.patch'
$inventoryPath = Join-Path $workspaceRoot 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-untracked-paths.txt'
$patchLines = [System.IO.File]::ReadAllLines($patchPath)
$inventory = [System.IO.File]::ReadAllLines($inventoryPath)
$hunks = @()
$currentFile = ''
for ($line = 0; $line -lt $patchLines.Length; $line++) {
    if ($patchLines[$line].StartsWith('+++ b/')) { $currentFile = $patchLines[$line].Substring(6) }
    if ($patchLines[$line].StartsWith('@@')) { $hunks += [pscustomobject]@{ file = $currentFile; line = $line + 1; header = $patchLines[$line] } }
}
$fixtures = foreach ($fixturePath in @(rg --files (Join-Path $workspaceRoot 'tests') -g '*.vsf')) {
    $bytes = [System.IO.File]::ReadAllBytes($fixturePath)
    $offset = 37
    if ($bytes.Length -ge 50 -and [System.Text.Encoding]::ASCII.GetString($bytes,37,13) -eq "VICE Version$([char]26)") { $offset = 58 }
    $modules = @()
    while ($offset -lt $bytes.Length) {
        if ($bytes.Length - $offset -lt 22) { throw 'Truncated module header' }
        $name = [System.Text.Encoding]::ASCII.GetString($bytes,$offset,16).TrimEnd([char]0)
        $size = [System.BitConverter]::ToUInt32($bytes,$offset + 18)
        if ($size -lt 22 -or $size -gt ($bytes.Length - $offset)) { throw 'Invalid module size' }
        $payloadOffset = $offset + 22
        $module = [ordered]@{ name = $name; major = $bytes[$offset+16]; minor = $bytes[$offset+17]; offset = $offset; size = $size; payloadBytes = $size - 22 }
        if ($name -eq 'MAINCPU') {
            $module.clock = [System.BitConverter]::ToUInt64($bytes,$payloadOffset)
            $module.a = $bytes[$payloadOffset+8]
            $module.x = $bytes[$payloadOffset+9]
            $module.y = $bytes[$payloadOffset+10]
            $module.sp = $bytes[$payloadOffset+11]
            $module.pc = [System.BitConverter]::ToUInt16($bytes,$payloadOffset+12)
            $module.p = $bytes[$payloadOffset+14]
        }
        if ($name -match '^DRIVE(8|9|10|11)$' -and $size -ge 24) { $module.has_tde = $bytes[$payloadOffset]; $module.has_drives = $bytes[$payloadOffset+1] }
        $modules += [pscustomobject]$module
        $offset += $size
    }
    [pscustomobject]@{ path = $fixturePath; sha256 = (Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash; bytes = $bytes.Length; machine = [System.Text.Encoding]::ASCII.GetString($bytes,21,16).TrimEnd([char]0); modules = $modules }
}
$semanticCandidates = @(rg -l -- 'ViceNative\.|ViceNativeXvic|ViceNativeBridge|IViceNative|XvicOraclePrerequisites' (Join-Path $workspaceRoot 'tests'))
$tokenCandidates = @(rg -l -- 'ViceNativeBridge|\[ViceFact|\[ViceTheory' (Join-Path $workspaceRoot 'tests'))
$tokenWithCategory = @($tokenCandidates | Where-Object { [System.IO.File]::ReadAllText($_) -match 'Trait\("Category"' })
$result = [ordered]@{
    timestampLocal = (Get-Date).ToString('s')
    patchLineCount = $patchLines.Length
    hunkCount = $hunks.Count
    hunks = $hunks
    inventoryCount = $inventory.Length
    inventoryExtensions = @($inventory | Group-Object { [System.IO.Path]::GetExtension($_) } | Sort-Object Count -Descending | Select-Object Name,Count)
    makefileCount = @($inventory | Where-Object { [System.IO.Path]::GetFileName($_) -eq 'Makefile' }).Count
    fixtures = @($fixtures)
    semanticCandidateCount = $semanticCandidates.Count
    semanticCandidates = $semanticCandidates
    tokenCandidateCount = $tokenCandidates.Count
    tokenWithCategoryCount = $tokenWithCategory.Count
    nativeProjectCallers = @(rg -n --glob '*.csproj' --glob '*.proj' --glob '*.targets' --glob '*.props' --glob '*.cs' -- 'Vice\.Native\.proj|ViceNativeProject' (Join-Path $workspaceRoot 'src') (Join-Path $workspaceRoot 'tests') (Join-Path $workspaceRoot 'build') (Join-Path $workspaceRoot 'native'))
    proposedToolExists = Test-Path -LiteralPath (Join-Path $workspaceRoot 'tools/ViceSharp.SnapshotFixtureGenerator')
    nukeTargetDefinitions = @(rg -n --glob '*.cs' -- 'Target (BuildViceNative|GenerateNativeSnapshotFixtures|NativeSnapshotCompatibilityTest)' (Join-Path $workspaceRoot 'build'))
}
if ($PSCmdlet.ShouldProcess($PSScriptRoot, 'Save complete binary and inventory inspection')) {
    $result | ConvertTo-Json -Depth 18 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'inspection.json') -Encoding utf8
}
[pscustomobject]$result | Select-Object timestampLocal,patchLineCount,hunkCount,inventoryCount,inventoryExtensions,makefileCount,semanticCandidateCount,tokenCandidateCount,tokenWithCategoryCount,nativeProjectCallers,proposedToolExists,nukeTargetDefinitions | ConvertTo-Json -Depth 12
$fixtures | ForEach-Object { [pscustomobject]@{ path = $_.path; bytes = $_.bytes; machine = $_.machine; cpu = @($_.modules | Where-Object name -EQ 'MAINCPU'); drives = @($_.modules | Where-Object name -Match '^DRIVE(8|9|10|11)$') } } | ConvertTo-Json -Depth 12
