[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Inventory','Fixture','Sources','Hunks','Consumers')][string]$Operation,
    [string]$Label = 'initial'
)
$ErrorActionPreference = 'Stop'
$parseTokens = $null
$parseErrors = $null
$null = [System.Management.Automation.Language.Parser]::ParseFile($PSCommandPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis = @(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning, Error -WhatIf:$false)
if ($analysis.Count -gt 0) { throw ($analysis | Out-String) }
Write-Output 'Script validation: parser errors=0; PSScriptAnalyzer warnings/errors=0.'
if (-not $PSCmdlet.ShouldProcess($Operation, 'Inspect live sources read-only and retain receipt')) { return }
$env:GIT_OPTIONAL_LOCKS = '0'
$workspace = '/home/sharpninja/github/vice-sharp'
$nativeRoot = Join-Path $workspace 'native/vice'
$materializedRoot = Join-Path $workspace 'artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source'
$report = $null
switch ($Operation) {
    'Inventory' {
        $status = @(& git -C $nativeRoot status --porcelain=v1 -uall)
        $paths = foreach ($entry in $status) {
            $relative = $entry.Substring(3)
            $itemPath = Join-Path $nativeRoot $relative
            [ordered]@{status=$entry.Substring(0,2);path=$relative;sha256=(Get-FileHash -LiteralPath $itemPath -Algorithm SHA256).Hash;bytes=(Get-Item -LiteralPath $itemPath -Force).Length}
        }
        $report = [ordered]@{timestampLocal=(Get-Date -Format s);head=(& git rev-parse HEAD);nativeHead=(& git -C $nativeRoot rev-parse HEAD);gitlink=(& git ls-tree HEAD native/vice);nativeIndexDiff=@(& git -C $nativeRoot diff --cached --stat);paths=@($paths)}
    }
    'Sources' {
        $relativePaths = @('vice/src/autostart.c','vice/src/monitor/monitor.c','vice/src/monitor/mon_parse.y','vice/src/drive/drive-snapshot.c','vice/src/drive/drive-resources.c','vice/src/mainc64cpu.c','vice/src/mainviccpu.c','vice/src/c64/c64-snapshot.c','vice/src/vic20/vic20-snapshot.c','vice/src/c64/c64cpusc.c','vice/src/6510dtvcore.c','vice/src/mos6510.h','vice/src/drive/rotation.c','vice/src/drive/drive.c','vice/src/drive/drive-check.c','vice/src/drive/drive-cmdline-options.c','vice/src/iecbus/iecbus.c','vice/src/maincpu.c')
        $report = foreach ($relative in $relativePaths) {
            $sourcePath = Join-Path $materializedRoot $relative
            if (-not (Test-Path -LiteralPath $sourcePath)) { [ordered]@{path=$relative;missing=$true}; continue }
            $actualBlob = & git hash-object -- $sourcePath
            $pinnedBlob = & git -C $nativeRoot rev-parse ('cc67418cba58e71d8e70fd353e9de47199ad2075:' + $relative)
            [ordered]@{path=$relative;actualBlob=$actualBlob;pinnedBlob=$pinnedBlob;matches=($actualBlob -eq $pinnedBlob);sha256=(Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash}
        }
    }
    'Fixture' {
        $fixturePath = Join-Path $workspace 'tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf'
        $bytes = [System.IO.File]::ReadAllBytes($fixturePath)
        $offset = 58
        $modules = [System.Collections.Generic.List[object]]::new()
        while ($offset -lt $bytes.Length) {
            if (($offset + 22) -gt $bytes.Length) { throw 'Truncated module header.' }
            $name = [System.Text.Encoding]::ASCII.GetString($bytes,$offset,16).TrimEnd([char]0)
            $length = [BitConverter]::ToUInt32($bytes,$offset+18)
            if ($length -lt 22 -or ($offset + $length) -gt $bytes.Length) { throw ('Invalid module: ' + $name) }
            $module = [ordered]@{name=$name;offset=$offset;length=$length;major=$bytes[$offset+16];minor=$bytes[$offset+17]}
            if ($name -eq 'MAINCPU') {
                $module.clock = [BitConverter]::ToUInt64($bytes,$offset+22)
                $module.A = [int]$bytes[$offset+30]; $module.X = [int]$bytes[$offset+31]; $module.Y = [int]$bytes[$offset+32]; $module.SP = [int]$bytes[$offset+33]
                $module.PC = [BitConverter]::ToUInt16($bytes,$offset+34); $module.P = [int]$bytes[$offset+36]
                $module.payloadHex = [Convert]::ToHexString($bytes[($offset+22)..($offset+$length-1)])
            }
            if ($name -match '^DRIVE(8|9|10|11)$') {
                $module.hasTde = [int]$bytes[$offset+22];$module.hasDrives=[int]$bytes[$offset+23]
            }
            $modules.Add($module)
            $offset += $length
        }
        $report = [ordered]@{path=$fixturePath;bytes=$bytes.Length;sha256=(Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash;parsedEnd=$offset;modules=@($modules.ToArray())}
    }
    'Hunks' {
        $patchPath = Join-Path $workspace 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-tracked.patch'
        $currentFile = ''; $lineNumber = 0
        $report = foreach ($line in Get-Content -LiteralPath $patchPath) {
            $lineNumber++
            if ($line -match '^diff --git a/(.+) b/') { $currentFile = $Matches[1] }
            if ($line -match '^@@') { [ordered]@{file=$currentFile;patchLine=$lineNumber;header=$line} }
        }
    }
    'Consumers' {
        $pattern = 'ViceNative\.|ViceNativeXvic|ViceNativeBridge|IViceNative|XvicOraclePrerequisites'
        $consumerPaths = @(& rg --files-with-matches --glob '*.cs' -- $pattern (Join-Path $workspace 'tests'))
        $tokenMatches = @(& rg --files-with-matches --glob '*.cs' -- 'ViceNativeBridge|\[ViceFact|\[ViceTheory' (Join-Path $workspace 'tests'))
        $files = foreach ($matchPath in $consumerPaths) {
            $content = Get-Content -LiteralPath $matchPath -Raw
            [ordered]@{path=$matchPath.Substring($workspace.Length+1);plainFact=($content -match '\[Fact(?:\]|\()');viceAttribute=($content -match '\[Vice(?:Fact|Theory)');category=($content -match 'Trait\("Category"');tokenSetIncludes=($matchPath -in $tokenMatches)}
        }
        $report = [ordered]@{rawSemanticPattern=$pattern;rawSemanticFileCount=$consumerPaths.Count;rawTokenFileCount=$tokenMatches.Count;rawTokenWithCategoryCount=@($tokenMatches | Where-Object { (Get-Content -LiteralPath $_ -Raw) -match 'Trait\("Category"' }).Count;files=@($files)}
    }
}
$outputPath = Join-Path $PSScriptRoot ($Operation.ToLowerInvariant() + '-' + $Label + '.json')
$report | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $outputPath -Encoding utf8
if ($Operation -eq 'Inventory') {
    [ordered]@{report=$outputPath;pathCount=@($report.paths).Count;tracked=@($report.paths | Where-Object { $_.status -ne '??' }).Count;untracked=@($report.paths | Where-Object { $_.status -eq '??' }).Count;nativeIndexDiff=$report.nativeIndexDiff;gitlink=$report.gitlink;head=$report.head} | ConvertTo-Json -Depth 10 | Write-Output
} else { $report | ConvertTo-Json -Depth 30 | Write-Output }
