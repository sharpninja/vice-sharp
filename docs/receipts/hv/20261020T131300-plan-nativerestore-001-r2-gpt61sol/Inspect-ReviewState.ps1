[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('State','Consumers','Tooling')][string] $Mode = 'State')
$ErrorActionPreference = 'Stop'
switch ($Mode) {
    'Tooling' {
        Get-Command Invoke-ScriptAnalyzer,Invoke-Pester,pwsh.exe | Select-Object Name,CommandType,Source,Version
    }
    'State' {
        $nativeStatus = @(& git -C native/vice status --porcelain=v1 -uall)
        $untrackedPaths = @($nativeStatus | Where-Object { $_.StartsWith('?? ') } | ForEach-Object { $_.Substring(3) })
        $modifiedPaths = @(& git -C native/vice diff --name-only)
        $inventory = foreach ($relativePath in @($untrackedPaths) + @($modifiedPaths)) {
            $filePath = Join-Path 'native/vice' $relativePath
            [pscustomobject]@{path=$relativePath; length=(Get-Item -Force -LiteralPath $filePath).Length; sha256=(Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash}
        }
        $state = [ordered]@{timestampLocal=(Get-Date -Format 's');branch=(& git branch --show-current);head=(& git rev-parse HEAD);gitLog=@(& git log -5 --format='%h %s');gitlink=(& git ls-tree HEAD native/vice);nativeHead=(& git -C native/vice rev-parse HEAD);nativeStatus=$nativeStatus;nativeDiffStat=@(& git -C native/vice diff --stat);nativeCachedDiff=@(& git -C native/vice diff --cached);trackedCount=$modifiedPaths.Count;untrackedCount=$untrackedPaths.Count;inventory=$inventory;superprojectStatus=@(& git status --porcelain=v1 -uall)}
        if ($PSCmdlet.ShouldProcess('state-before.json', 'Record read-only state evidence')) { $state | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'state-before.json') -Encoding utf8 }
        $state | Select-Object timestampLocal,branch,head,gitLog,gitlink,nativeHead,trackedCount,untrackedCount,nativeDiffStat,nativeCachedDiff | ConvertTo-Json -Depth 8
        & git diff --stat 4c5cb166..HEAD
    }
    'Consumers' {
        $seedPaths = @(& rg -l -g '*.cs' 'ViceNativeBridge|\[ViceFact|\[ViceTheory' tests)
        $consumerInventory = foreach ($consumerPath in $seedPaths) {
            $text = Get-Content -Raw -LiteralPath $consumerPath
            [pscustomobject]@{path=$consumerPath; category=($text -match 'Trait\("Category"'); viceFactCount=([regex]::Matches($text,'\[ViceFact')).Count; viceTheoryCount=([regex]::Matches($text,'\[ViceTheory')).Count}
        }
        $otherPaths = @(& rg -l -g '*.cs' 'ViceNative\.|ViceXvicNative|ViceXvicNativeBridge|DllImport|NativeLibrary\.(Load|TryLoad)' tests | Where-Object { $_ -notin $seedPaths })
        $result = [ordered]@{timestampLocal=(Get-Date -Format 's');seedCount=$seedPaths.Count;withCategory=@($consumerInventory | Where-Object category).Count;withoutCategory=@($consumerInventory | Where-Object { -not $_.category }).Count;consumerInventory=$consumerInventory;otherPotentialNativePaths=$otherPaths}
        if ($PSCmdlet.ShouldProcess('consumer-inventory.json', 'Record consumer search evidence')) { $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'consumer-inventory.json') -Encoding utf8 }
        $result | ConvertTo-Json -Depth 8
        & rg -n -g '*.csproj' -g '*.props' -g '*.targets' -g '*.proj' -g '*.slnx' 'Vice\.Native\.proj|BuildViceNative|BuildNative' src tests native build ViceSharp.slnx
    }
}
Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffzzz'
