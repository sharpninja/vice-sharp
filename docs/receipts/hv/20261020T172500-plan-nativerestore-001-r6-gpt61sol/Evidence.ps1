#Requires -Version 7.0
[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Native','Fixtures','Counts','Projection','Store','SelectStore','Check')][string]$Mode='Counts',[switch]$Final)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$workspaceRoot='/home/sharpninja/github/vice-sharp'
$nativeRoot=Join-Path $workspaceRoot 'native/vice'
$materialRoot=Join-Path $workspaceRoot 'artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075/source/vice/src'
$receiptRoot=$PSScriptRoot
if (-not $PSCmdlet.ShouldProcess($receiptRoot,('Retain read-only evidence: '+$Mode))) {return}
switch ($Mode) {
    'Projection' {
        $buildText=Get-Content -LiteralPath (Join-Path $workspaceRoot 'build/Build.cs') -Raw
        $filters=@([regex]::Matches($buildText,'\.SetFilter\("(?<filter>Category!=Determinism[^"\r\n]+)"\)')|ForEach-Object{$_.Groups['filter'].Value})
        $testFilter=$filters[0]+'&Category!=ShimLegacy'
        $ciFilter=$filters[1]
        $projection=@(foreach($category in @('VsfSlice1Contract','VsfSlice1Real','ShimLegacy','Ordinary')){
            [ordered]@{category=$category;projectedTestSelected=($category -notin @(($testFilter -split '&') -replace '^Category!=',''));unchangedCiTestSelected=($category -notin @(($ciFilter -split '&') -replace '^Category!=',''))}
        })
        [ordered]@{timestampLocal=(Get-Date).ToString('s');basis='Build.cs current filter plus only the edit explicitly named in D2; a static projection, not execution of future code';projectedTestFilter=$testFilter;unchangedCiTestFilter=$ciFilter;cases=$projection}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $receiptRoot 'filter-projection.json')
        $projection|ConvertTo-Json -Depth 5
    }
    'Native' {
        $status=@(& git -C $nativeRoot status --porcelain=v1 -uall)
        $tracked=@(& git -C $nativeRoot diff --name-only)
        $untracked=@(& git -C $nativeRoot ls-files --others --exclude-standard)
        $inventory=@(foreach($relative in @($tracked)+@($untracked)) {$item=Get-Item -LiteralPath (Join-Path $nativeRoot $relative) -Force;[ordered]@{path=$relative;length=$item.Length;sha256=(Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash}})
        $sourcePaths=@('interrupt.h','interrupt.c','6510core.c','6510dtvcore.c','machine.h','autostart.c','c64/c64-snapshot.c','vic20/vic20-snapshot.c','mainc64cpu.c','mainviccpu.c','mos6510.h','monitor/monitor.c','monitor/mon_parse.y','monitor/mon_lex.l','initcmdline.c','drive/drive-snapshot.c','drive/rotation.c','drive/drive-resources.c','iecbus/iecbus.c','resources.h')
        $sourceHashes=@(foreach($relative in $sourcePaths){$materialPath=Join-Path $materialRoot $relative;$actual= & git -C $nativeRoot hash-object --no-filters $materialPath;$expected= & git -C $nativeRoot rev-parse ('cc67418cba58e71d8e70fd353e9de47199ad2075:vice/src/'+$relative);[ordered]@{path=$relative;actual=$actual;expected=$expected;match=($actual -eq $expected);sha256=(Get-FileHash -LiteralPath $materialPath).Hash}})
        $state=[ordered]@{timestampLocal=(Get-Date).ToString('s');branch=(& git -C $workspaceRoot branch --show-current);head=(& git -C $workspaceRoot rev-parse HEAD);nativeHead=(& git -C $nativeRoot rev-parse HEAD);gitlink=(& git -C $workspaceRoot ls-tree HEAD native/vice);nativeDiffStat=@(& git -C $nativeRoot diff --stat);nativeIndexDiff=@(& git -C $nativeRoot diff --cached --stat);porcelain=$status;trackedCount=$tracked.Count;untrackedCount=$untracked.Count;inventory=$inventory;pinnedSourceHashes=$sourceHashes}
        $name=if($Final){'native-final.json'}else{'native-initial.json'}
        $state | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $receiptRoot $name)
        if($Final){$before=Get-Content -LiteralPath (Join-Path $receiptRoot 'native-initial.json') -Raw|ConvertFrom-Json;$old=$before.inventory|ConvertTo-Json -Depth 6 -Compress;$new=$inventory|ConvertTo-Json -Depth 6 -Compress;Write-Output ('NATIVE_INVENTORY_UNCHANGED='+($old -eq $new))}
        [pscustomobject]$state|Select-Object timestampLocal,branch,head,nativeHead,trackedCount,untrackedCount|ConvertTo-Json
        Write-Output ('SOURCE_HASH_MISMATCHES='+@($sourceHashes|Where-Object{-not $_.match}).Count)
    }
    'Fixtures' {
        $fixturePaths=@(& rg --files (Join-Path $workspaceRoot 'tests') -g '*.vsf')
        $fixtureFacts=@(foreach($fixturePath in $fixturePaths){
            $bytes=[System.IO.File]::ReadAllBytes($fixturePath)
            $moduleFacts=[System.Collections.Generic.List[object]]::new()
            $offset=37
            if($bytes.Length -ge 58 -and [System.Text.Encoding]::ASCII.GetString($bytes,37,13) -eq "VICE Version`u{1a}"){$offset=58}
            $header=$offset
            while($offset -lt $bytes.Length){
                if($bytes.Length-$offset -lt 22){throw 'Truncated module header'}
                $moduleName=[System.Text.Encoding]::ASCII.GetString($bytes,$offset,16).TrimEnd([char]0)
                $length=[BitConverter]::ToUInt32($bytes,$offset+18)
                if($length -lt 22 -or $length -gt $bytes.Length-$offset){throw 'Invalid module length'}
                $module=[ordered]@{name=$moduleName;major=$bytes[$offset+16];minor=$bytes[$offset+17];inclusiveLength=$length;offset=$offset}
                if($moduleName -eq 'MAINCPU'){$module.clock=[BitConverter]::ToUInt64($bytes,$offset+22);$module.a=$bytes[$offset+30];$module.x=$bytes[$offset+31];$module.y=$bytes[$offset+32];$module.sp=$bytes[$offset+33];$module.pc=[BitConverter]::ToUInt16($bytes,$offset+34);$module.p=$bytes[$offset+36]}
                if($moduleName -match '^DRIVE(?:8|9|10|11)$'){$module.payloadLength=$length-22;$module.hasTde=if($length -ge 23){$bytes[$offset+22]}else{$null};$module.hasDrives=if($length -ge 24){$bytes[$offset+23]}else{$null}}
                $moduleFacts.Add($module);$offset+=$length
            }
            [ordered]@{path=$fixturePath;sha256=(Get-FileHash -LiteralPath $fixturePath).Hash;bytes=$bytes.Length;headerBytes=$header;machine=[System.Text.Encoding]::ASCII.GetString($bytes,21,16).TrimEnd([char]0);modules=@($moduleFacts);exactEof=($offset -eq $bytes.Length)}
        })
        $fixtureFacts|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $receiptRoot 'fixture-inspection.json')
        foreach($fixture in $fixtureFacts){[ordered]@{path=$fixture.path;bytes=$fixture.bytes;cpu=@($fixture.modules|Where-Object name -eq 'MAINCPU');drives=@($fixture.modules|Where-Object name -match '^DRIVE(?:8|9|10|11)$')}|ConvertTo-Json -Depth 8 -Compress}
    }
    'Counts' {
        $historicalPaths=@(Get-Content -LiteralPath (Join-Path $workspaceRoot 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-untracked-paths.txt'))
        $patch=Get-Content -LiteralPath (Join-Path $workspaceRoot 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-tracked.patch') -Raw
        $testRoot=Join-Path $workspaceRoot 'tests/ViceSharp.TestHarness'
        $token=@(& rg -l --glob '*.cs' 'ViceNativeBridge|\[ViceFact|\[ViceTheory' $testRoot)
        $semantic=@(& rg -l --glob '*.cs' 'ViceNative\.|ViceNativeXvic|ViceNativeBridge|IViceNative|XvicOraclePrerequisites' $testRoot)
        $facts=[ordered]@{timestampLocal=(Get-Date).ToString('s');historicalInventoryLines=$historicalPaths.Count;historicalExtensions=@($historicalPaths|Group-Object {[System.IO.Path]::GetExtension($_)}|Select-Object Name,Count);historicalMakefiles=@($historicalPaths|Where-Object{[System.IO.Path]::GetFileName($_) -eq 'Makefile'}).Count;patchHunks=[regex]::Matches($patch,'(?m)^@@').Count;patchFiles=[regex]::Matches($patch,'(?m)^diff --git').Count;tokenCandidateCount=$token.Count;tokenCandidates=$token;semanticCandidateCount=$semantic.Count;semanticCandidates=$semantic;generatorExists=(Test-Path -LiteralPath (Join-Path $workspaceRoot 'tools/ViceSharp.SnapshotFixtureGenerator'));nativeProjectCallers=@(& rg -n --glob '*.csproj' --glob '*.proj' --glob '*.cs' 'Vice\.Native\.proj|ViceNativeProject' (Join-Path $workspaceRoot 'tests') (Join-Path $workspaceRoot 'build') (Join-Path $workspaceRoot 'src'))}
        $facts|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $receiptRoot 'counts.json')
        [pscustomobject]$facts|Select-Object timestampLocal,historicalInventoryLines,historicalMakefiles,patchHunks,patchFiles,tokenCandidateCount,semanticCandidateCount,generatorExists,nativeProjectCallers|ConvertTo-Json -Depth 5
    }
    'Store' {
        $env:MCP_FAILSAFE_DRAIN_DISABLED='1'
        $wrapperPath='/home/sharpninja/github/mcpserver-codex-plugin/Invoke-CodexMcpPlugin.ps1'
        $options=@{WorkspacePath=$workspaceRoot;CacheRoot=(Join-Path $receiptRoot 'plugin-cache')}
        foreach($query in @(@{name='todo';method='workflow.todo.get';data=@{id='PLAN-VICEORACLE-001'}},@{name='vice-mapping';method='workflow.requirements.listMappings';data=@{frId='FR-VICE-001'}},@{name='snp-mapping';method='workflow.requirements.listMappings';data=@{frId='FR-SNP-005'}},@{name='own';method='client.SessionLog.QueryAsync';data=@{agent='Codex';limit=1;offset=0}})){
            $result=& $wrapperPath -Command Invoke -Method $query.method -ParamsObject $query.data @options
            $result|Set-Content -LiteralPath (Join-Path $receiptRoot ('store-'+$query.name+'.txt'))
        }
        . '/home/sharpninja/github/mcpserver-codex-plugin/lib/yaml-object-mutation.ps1'
        Import-McpYamlSerializer
        foreach($label in @('effective','todo','vice-mapping','snp-mapping','own')){
            $raw=Get-Content -LiteralPath (Join-Path $receiptRoot ('store-'+$label+'.txt')) -Raw
            $parsed=ConvertFrom-Yaml -Yaml $raw
            Write-Output ($label+': root keys '+($parsed.Keys -join ','))
            if($parsed.ContainsKey('payload')){Write-Output ($label+': payload keys '+($parsed.payload.Keys -join ','))}
        }
    }
    'SelectStore' {
        Import-Module powershell-yaml
        $effective=(ConvertFrom-Yaml -Yaml (Get-Content -LiteralPath (Join-Path $receiptRoot 'store-effective.txt') -Raw)).payload.result
        $selectedIds=@('FR-VICE-001','TR-VICE-ORACLE-001','TEST-VICE-001','TR-VSFLOCKSTEP-RESUME-001','TR-CYCLE-001','FR-SNP-005','TEST-SNP-002')
        $selected=@(foreach($kind in @('functional','technical','testing')){foreach($record in $effective[$kind]){if($record.id -in $selectedIds){$record}}})
        $selected|ConvertTo-Json -Depth 25|Set-Content -LiteralPath (Join-Path $receiptRoot 'store-effective.selected.json')
        $selected|ConvertTo-Json -Depth 25
        $todo=(ConvertFrom-Yaml -Yaml (Get-Content -LiteralPath (Join-Path $receiptRoot 'store-todo.txt') -Raw)).payload.result
        $todo|ConvertTo-Json -Depth 20|Set-Content -LiteralPath (Join-Path $receiptRoot 'store-todo.json')
        $own=(ConvertFrom-Yaml -Yaml (Get-Content -LiteralPath (Join-Path $receiptRoot 'store-own.txt') -Raw)).payload.result
        $own.items|ForEach-Object{[pscustomobject]@{sessionId=$_.sessionId;sourceType=$_.sourceType;model=$_.model;turns=@($_.turns|Select-Object requestId,queryTitle,status)}}|ConvertTo-Json -Depth 6
    }
    'Check' {
        & git -C $workspaceRoot diff --check
        Write-Output ('DIFF_CHECK_EXIT_CODE='+$LASTEXITCODE)
        & git -C $workspaceRoot status --porcelain=v1 -uno
    }
}
