[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][ValidateSet('State','Inspect','Prior','Test','Source','Seams','Final')][string]$Mode,
    [string]$PriorPath = '',
    [int]$Start = 0,
    [int]$Count = 15,
    [string]$Label = 'before'
)
$ErrorActionPreference = 'Stop'
if ($WhatIfPreference) { Write-Output ('WhatIf: evidence ' + $Mode); return }
if (-not $PSCmdlet.ShouldProcess($PSScriptRoot, $Mode)) { return }
$root = '/home/sharpninja/github/vice-sharp'
$nativeRoot = Join-Path $root 'native/vice'
$sourceRoot = Join-Path $root 'artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source/vice/src'
function Read-VsfModule {
    param([string]$Path)
    $bytes = [IO.File]::ReadAllBytes($Path)
    $offset = 58
    $modules = @()
    while ($offset + 22 -le $bytes.Length) {
        $name = [Text.Encoding]::ASCII.GetString($bytes, $offset, 16).TrimEnd([char]0)
        $length = [BitConverter]::ToUInt32($bytes, $offset + 18)
        if ($length -lt 22 -or $offset + $length -gt $bytes.Length) { throw ('Invalid module at ' + $offset) }
        $entry = [ordered]@{name=$name;major=[int]$bytes[$offset+16];minor=[int]$bytes[$offset+17];offset=$offset;length=$length;payloadLength=($length-22)}
        if ($name -match '^DRIVE(8|9|10|11)$') { $entry.payloadFirstBytes=@($bytes[($offset+22)..([Math]::Min($offset+25,$offset+$length-1))]) }
        if ($name -eq 'MAINCPU') {
            $entry.clock=[BitConverter]::ToUInt64($bytes,$offset+22)
            $entry.A=[int]$bytes[$offset+30];$entry.X=[int]$bytes[$offset+31];$entry.Y=[int]$bytes[$offset+32];$entry.SP=[int]$bytes[$offset+33];$entry.PC=[BitConverter]::ToUInt16($bytes,$offset+34);$entry.P=[int]$bytes[$offset+36]
        }
        $modules += $entry
        $offset += $length
    }
    [ordered]@{path=$Path;sha256=(Get-FileHash -LiteralPath $Path).Hash;length=$bytes.Length;modules=$modules;parsedTo=$offset}
}
switch ($Mode) {
    'State' {
        $env:GIT_OPTIONAL_LOCKS = '0'
        $status = @(& git -C $nativeRoot status --porcelain=v1 -uall)
        $entries = @(foreach ($line in $status) {
            $path = $line.Substring(3)
            $absolute = Join-Path $nativeRoot $path
            [ordered]@{status=$line.Substring(0,2);path=$path;sha256=(Get-FileHash -LiteralPath $absolute).Hash;length=(Get-Item -Force -LiteralPath $absolute).Length}
        })
        $state = [ordered]@{timestampLocal=(Get-Date -Format s);branch=(& git branch --show-current);head=(& git rev-parse HEAD);gitlink=(& git ls-tree HEAD native/vice);nativeHead=(& git -C $nativeRoot rev-parse HEAD);submodule=(& git submodule status);nativeDiffStat=@(& git -C $nativeRoot diff --stat);nativeDiffNumstat=@(& git -C $nativeRoot diff --numstat);nativeIndex=@(& git -C $nativeRoot diff --cached --stat);status=$status;entries=$entries;rootStatus=@(& git status --porcelain=v1 -uall)}
        $state | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('state-' + $Label + '.json'))
        [ordered]@{timestampLocal=$state.timestampLocal;branch=$state.branch;head=$state.head;gitlink=$state.gitlink;nativeHead=$state.nativeHead;submodule=$state.submodule;entryCount=$entries.Count;tracked=@($entries | Where-Object status -ne '??').Count;untracked=@($entries | Where-Object status -eq '??').Count;index=$state.nativeIndex;diffStat=$state.nativeDiffStat} | ConvertTo-Json -Depth 5
    }
    'Inspect' {
        $patchPath = Join-Path $root 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-tracked.patch'
        $inventoryPath = Join-Path $root 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-untracked-paths.txt'
        $inventory = @(Get-Content -LiteralPath $inventoryPath)
        $patch = @(Get-Content -LiteralPath $patchPath)
        $inspection = [ordered]@{timestampLocal=(Get-Date -Format s);patchLines=$patch.Count;hunks=@($patch | Where-Object { $_ -match '^@@' });files=@($patch | Where-Object { $_ -match '^diff --git' });inventoryCount=$inventory.Count;extensionCounts=@($inventory | Group-Object { [IO.Path]::GetExtension($_) } | Sort-Object Count -Descending | Select-Object Name,Count);makefiles=@($inventory | Where-Object { [IO.Path]::GetFileName($_) -eq 'Makefile' }).Count;fixtures=@((Read-VsfModule (Join-Path $root 'tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf')),(Read-VsfModule (Join-Path $root 'tests/ViceSharp.VsfCompatibility.Tests/Fixtures/native-maincpu-1.4.vsf')))}
        $inspection | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'inspection.json')
        [ordered]@{timestampLocal=$inspection.timestampLocal;patchLines=$inspection.patchLines;hunkCount=$inspection.hunks.Count;fileCount=$inspection.files.Count;inventoryCount=$inspection.inventoryCount;extensionCounts=$inspection.extensionCounts;makefiles=$inspection.makefiles;fixtures=@($inspection.fixtures | ForEach-Object { [ordered]@{path=$_.path;sha256=$_.sha256;length=$_.length;modules=@($_.modules | Where-Object { $_.name -eq 'MAINCPU' -or $_.name -match '^DRIVE(8|9|10|11)$' });parsedTo=$_.parsedTo} })} | ConvertTo-Json -Depth 12
        & rg -l --glob '*.cs' -- 'ViceNative\.|ViceNativeXvic|ViceNativeBridge|IViceNative|XvicOraclePrerequisites' (Join-Path $root 'tests')
        & rg -n --glob '*.csproj' --glob '*.proj' --glob '*.cs' --glob '*.ps1' --glob '*.props' --glob '*.targets' -- 'Vice\.Native\.proj|ViceNativeProject|BuildViceNative|GenerateNativeSnapshotFixtures|NativeSnapshotCompatibilityTest' (Join-Path $root 'build') (Join-Path $root 'native') (Join-Path $root 'src') (Join-Path $root 'tests')
        Write-Output ('Generator exists: ' + (Test-Path -LiteralPath (Join-Path $root 'tools/ViceSharp.SnapshotFixtureGenerator')))
    }
    'Prior' {
        $prior = Get-Content -LiteralPath $PriorPath -Raw | ConvertFrom-Json
        Write-Output ('PRIOR: ' + $PriorPath)
        if ($Start -eq 0) { $prior | Select-Object OverallVerdict,accuracyPercent,completenessPercent,passCount,failCount,unknownCount,reviewerSessionId,reviewerRequestId | ConvertTo-Json -Depth 4 }
        @($prior.findings) | Select-Object -Skip $Start -First $Count | ConvertTo-Json -Depth 15
    }
    'Source' {
        $sourceFiles = @('interrupt.h','6510core.c','6510dtvcore.c','machine.h','autostart.c','c64/c64-snapshot.c','vic20/vic20-snapshot.c','mainc64cpu.c','mainviccpu.c','mos6510.h','monitor/monitor.c','monitor/mon_parse.y','monitor/mon_lex.l','initcmdline.c','drive/drive-snapshot.c','drive/rotation.c','drive/drive-resources.c','iecbus/iecbus.c','resources.h','interrupt.c','event.c','c64/c64.c','vic20/vic20.c','c64/c64cpusc.c','snapshot.h')
        $results = @(foreach ($sourceFile in $sourceFiles) {
            $path = Join-Path $sourceRoot $sourceFile
            $relative = 'vice/src/' + $sourceFile
            $expected = (& git -C $nativeRoot rev-parse ('cc67418cba58e71d8e70fd353e9de47199ad2075:' + $relative))
            $actual = (& git -C $nativeRoot hash-object -- $path)
            [ordered]@{path=$path;blobExpected=$expected;blobActual=$actual;matches=($expected -eq $actual);sha256=(Get-FileHash -LiteralPath $path).Hash}
        })
        $results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'source-pins.json')
        $results | ConvertTo-Json -Depth 5
    }
    'Test' {
        $resultsPath = Join-Path $PSScriptRoot 'TestResults'
        $null = New-Item -ItemType Directory -Path $resultsPath -Force
        $argsList = @('test',(Join-Path $root 'tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj'),'-c','Release','--filter','FullyQualifiedName~Vic20HalfCycleComparatorTests','--results-directory',$resultsPath,'--logger','trx;LogFileName=vic20-comparator.trx','--diag',(Join-Path $resultsPath 'vstest.diag.log'),'--blame-hang','--blame-hang-timeout','5m')
        Write-Output ('COMMAND: dotnet ' + ($argsList -join ' '))
        & dotnet @argsList 2>&1 | Tee-Object -FilePath (Join-Path $resultsPath 'stdout-stderr.log')
        $resultCode = $LASTEXITCODE
        [ordered]@{timestampLocal=(Get-Date -Format s);command='dotnet';arguments=$argsList;exitCode=$resultCode} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $resultsPath 'run.json')
        Write-Output ('Exit code: ' + $resultCode)
    }
    'Seams' {
        $specifications = @(
            @{file='interrupt.h';ranges='299:309'}
            @{file='6510core.c';ranges='474:490'}
            @{file='machine.h';ranges='140:151'}
            @{file='autostart.c';ranges='877:900;1255:1282;1510:1580'}
            @{file='c64/c64-snapshot.c';ranges='1:159'}
            @{file='vic20/vic20-snapshot.c';ranges='47:159'}
            @{file='mainc64cpu.c';ranges='350:370;505:520;795:813'}
            @{file='mainviccpu.c';ranges='620:640;711:815'}
            @{file='mos6510.h';ranges='90:142'}
            @{file='monitor/monitor.c';ranges='2225:2260;2825:2838'}
            @{file='monitor/mon_parse.y';ranges='255:270'}
            @{file='monitor/mon_lex.l';ranges='175:190;215:225'}
            @{file='initcmdline.c';ranges='275:289'}
            @{file='drive/drive-snapshot.c';ranges='125:170;300:442;525:558'}
            @{file='drive/rotation.c';ranges='180:211'}
            @{file='drive/drive-resources.c';ranges='100:180;1045:1130'}
            @{file='iecbus/iecbus.c';ranges='490:550'}
            @{file='resources.h';ranges='95:125'}
            @{file='interrupt.c';ranges='315:380'}
            @{file='event.c';ranges='680:705;765:800'}
            @{file='6510dtvcore.c';ranges='1005:1045;1727:1810;1839:1852'}
            @{file='c64/c64.c';ranges='1064:1076;1365:1395'}
            @{file='vic20/vic20.c';ranges='945:958;1200:1238'}
        )
        foreach ($specification in ($specifications | Select-Object -Skip $Start -First $Count)) {
            $path = Join-Path $sourceRoot $specification.file
            $content = @(Get-Content -LiteralPath $path)
            Write-Output ('SOURCE: ' + $specification.file)
            foreach ($rangeText in $specification.ranges.Split(';')) {
                $range = @($rangeText.Split(':') | ForEach-Object { [int]$_ })
                for ($index=$range[0]-1;$index -lt [Math]::Min($range[1],$content.Count);$index++) { Write-Output ('{0}: {1}' -f ($index+1),$content[$index]) }
            }
        }
    }
    'Final' {
        $before = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'state-before.json') -Raw | ConvertFrom-Json
        $after = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'state-after.json') -Raw | ConvertFrom-Json
        $beforeCanonical = $before.entries | ConvertTo-Json -Depth 6 -Compress
        $afterCanonical = $after.entries | ConvertTo-Json -Depth 6 -Compress
        & git diff --check
        $diffCheck = $LASTEXITCODE
        $pins = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'input-pins.json') -Raw | ConvertFrom-Json
        $pinResults = @(foreach ($pin in $pins) { [ordered]@{path=$pin.relativePath;matches=((Get-FileHash -LiteralPath $pin.path).Hash -eq $pin.sha256)} })
        $sourcePins = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source-pins.json') -Raw | ConvertFrom-Json
        $sourceChecks = @(foreach ($pin in $sourcePins) { [ordered]@{path=$pin.path;matches=((Get-FileHash -LiteralPath $pin.path).Hash -ceq $pin.sha256);pinnedBlobMatch=$pin.matches} })
        $final = [ordered]@{timestampLocal=(Get-Date -Format s);nativeInventoryUnchanged=($beforeCanonical -ceq $afterCanonical);nativeEntryCountBefore=$before.entries.Count;nativeEntryCountAfter=$after.entries.Count;headUnchanged=($before.head -eq $after.head);pins=$pinResults;driftCount=@($pinResults | Where-Object { -not $_.matches }).Count;sourceChecks=$sourceChecks;sourceDriftCount=@($sourceChecks | Where-Object { -not $_.matches -or -not $_.pinnedBlobMatch }).Count;diffCheckExit=$diffCheck}
        $final | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'final-integrity.json')
        $final | ConvertTo-Json -Depth 8
        if (-not $final.nativeInventoryUnchanged -or -not $final.headUnchanged -or $final.driftCount -or $final.sourceDriftCount -or $diffCheck) { throw 'Final integrity gate failed.' }
    }
}
