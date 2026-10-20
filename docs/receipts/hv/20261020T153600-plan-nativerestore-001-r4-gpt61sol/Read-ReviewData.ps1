[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Inventory','Fixture','Sources','Contracts')][string]$Operation,[string]$Label='initial')
$ErrorActionPreference='Stop'
$parseTokens=$null;$parseErrors=$null
$null=[Management.Automation.Language.Parser]::ParseFile($PSCommandPath,[ref]$parseTokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
Import-Module PSScriptAnalyzer -ErrorAction Stop
$analysis=@(Invoke-ScriptAnalyzer -Path $PSCommandPath -Severity Warning,Error -WhatIf:$false)
if ($analysis.Count) { throw ($analysis | Out-String) }
if (-not $PSCmdlet.ShouldProcess($Operation,'Read and hash review evidence; save only receipt-local metadata')) { return }
switch ($Operation) {
    'Inventory' {
        $entries=@(foreach ($line in (& git -C native/vice status --porcelain=v1 -uall)) {
            $relative=$line.Substring(3)
            $full=Join-Path 'native/vice' $relative
            [ordered]@{status=$line.Substring(0,2);path=$relative;length=(Get-Item -LiteralPath $full -Force).Length;sha256=(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash}
        })
        $oldPaths=[IO.File]::ReadAllLines((Join-Path (Get-Location).Path 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-untracked-paths.txt'))
        $patch=[IO.File]::ReadAllLines((Join-Path (Get-Location).Path 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-tracked.patch'))
        $hunks=@(for ($index=0;$index -lt $patch.Count;$index++) { if ($patch[$index].StartsWith('@@')) { [ordered]@{line=$index+1;header=$patch[$index]} } })
        $record=[ordered]@{timestampLocal=[DateTime]::Now.ToString('s');nativeEntries=$entries;nativeCount=$entries.Count;trackedCount=@($entries | Where-Object status -NE '??').Count;untrackedCount=@($entries | Where-Object status -EQ '??').Count;historicalCount=$oldPaths.Count;historicalExtensionCounts=@($oldPaths | Group-Object { [IO.Path]::GetExtension($_) } | Sort-Object Count -Descending | Select-Object Name,Count);historicalInjected=@($oldPaths | Where-Object { $_ -match 'vice-shim|runtime' });hunkCount=$hunks.Count;hunks=$hunks}
        $record | ConvertTo-Json -Depth 15 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ($Label+'-inventory.json')) -Encoding utf8
        $record.nativeEntries=$null
        $record | ConvertTo-Json -Depth 15 | Write-Output
    }
    'Fixture' {
        $fixturePath='tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf'
        $bytes=[IO.File]::ReadAllBytes((Join-Path (Get-Location).Path $fixturePath))
        $offset=if ([Text.Encoding]::ASCII.GetString($bytes,37,13) -eq "VICE Version$([char]26)") { 58 } else { 37 }
        $modules=@(while ($offset -lt $bytes.Length) {
            if ($bytes.Length-$offset -lt 22) { throw 'Truncated module header.' }
            $size=[BitConverter]::ToUInt32($bytes,$offset+18)
            if ($size -lt 22 -or $size -gt $bytes.Length-$offset) { throw 'Invalid module size.' }
            $name=[Text.Encoding]::ASCII.GetString($bytes,$offset,16).TrimEnd([char]0)
            $record=[ordered]@{name=$name;major=$bytes[$offset+16];minor=$bytes[$offset+17];offset=$offset;totalSize=$size;payloadLength=$size-22}
            if ($name -eq 'MAINCPU') { $record.clock=[BitConverter]::ToUInt64($bytes,$offset+22);$record.pc=[BitConverter]::ToUInt16($bytes,$offset+34);$record.p=$bytes[$offset+36];$record.a=$bytes[$offset+30];$record.x=$bytes[$offset+31];$record.y=$bytes[$offset+32];$record.sp=$bytes[$offset+33] }
            if ($name -match '^DRIVE(8|9|10|11)$') { $record.hasTde=$bytes[$offset+22];$record.hasDrives=$bytes[$offset+23] }
            if ($name -eq 'VIC-II') { $record.viciiModel=$bytes[$offset+22] }
            $record
            $offset+=$size
        })
        $result=[ordered]@{timestampLocal=[DateTime]::Now.ToString('s');path=$fixturePath;bytes=$bytes.Length;sha256=(Get-FileHash -LiteralPath $fixturePath -Algorithm SHA256).Hash;moduleCount=$modules.Count;end=$offset;modules=$modules}
        $result | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'fixture-inspection.json') -Encoding utf8
        $result | ConvertTo-Json -Depth 20 | Write-Output
    }
    'Sources' {
        $pin='cc67418cba58e71d8e70fd353e9de47199ad2075'
        $root=Join-Path (Get-Location).Path ('artifacts/immutable-vice/'+$pin+'/source/vice/src')
        $names=@('interrupt.h','interrupt.c','6510core.c','6510dtvcore.c','machine.h','autostart.c','c64/c64-snapshot.c','vic20/vic20-snapshot.c','mainc64cpu.c','mainviccpu.c','mos6510.h','monitor/monitor.c','monitor/mon_parse.y','monitor/mon_lex.l','initcmdline.c','drive/drive-snapshot.c','drive/rotation.c','drive/drive-resources.c','iecbus/iecbus.c','resources.h','vsync.c','c64/c64cpusc.c','vic20/vic20cpu.c','c64/c64.c','vic20/vic20.c','viciisc/vicii-snapshot.c','viciisc/vicii-resources.c','vicii.h')
        $checks=@(foreach ($name in $names) {
            $full=Join-Path $root $name
            $expected=(& git -C native/vice rev-parse ($pin+':vice/src/'+$name))
            $actual=(& git -C native/vice hash-object --no-filters -- $full)
            [ordered]@{file=$name;expectedBlob=$expected;actualBlob=$actual;match=($actual -ceq $expected);sha256=(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash}
        })
        [ordered]@{timestampLocal=[DateTime]::Now.ToString('s');pin=$pin;files=$checks} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ($Label+'-source-pins.json')) -Encoding utf8
        $checks | ConvertTo-Json -Depth 10 | Write-Output
    }
    'Contracts' {
        $old=Get-Content -LiteralPath docs/receipts/hv/20261020T141600-plan-nativerestore-001-r3b-gpt61sol/verdict.json -Raw | ConvertFrom-Json
        $old.findings | Select-Object id,status,section | ConvertTo-Json | Write-Output
    }
}
