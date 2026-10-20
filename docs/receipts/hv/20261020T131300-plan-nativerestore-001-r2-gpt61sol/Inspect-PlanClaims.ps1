[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('SourcePins','CurrentHunks','CommitDrift','Historical')][string] $Mode = 'SourcePins')
$ErrorActionPreference = 'Stop'
switch ($Mode) {
    'SourcePins' {
        $nativeSha = 'cc67418cba58e71d8e70fd353e9de47199ad2075'
        $sourceRoot = Join-Path '/home/sharpninja/github/vice-sharp/artifacts/immutable-vice' ($nativeSha + '/source')
        $paths = @('vice/src/autostart.c','vice/src/mainc64cpu.c','vice/src/c64/c64cpusc.c','vice/src/drive/drive-snapshot.c','vice/src/6510dtvcore.c','vice/src/c64/c64-snapshot.c')
        $rows = foreach ($relativePath in $paths) {
            $filePath = Join-Path $sourceRoot $relativePath
            $expectedBlob = & git -C native/vice rev-parse "${nativeSha}:$relativePath"
            $actualBlob = & git -C native/vice hash-object -- $filePath
            [pscustomobject]@{path=$relativePath;expectedGitBlob=$expectedBlob;actualGitBlob=$actualBlob;match=($expectedBlob -eq $actualBlob);sha256=(Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash}
        }
        if ($PSCmdlet.ShouldProcess('source-pin-verification.json','Record pinned source verification')) { $rows | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'source-pin-verification.json') -Encoding utf8 }
        $rows | ConvertTo-Json -Depth 8
    }
    'CurrentHunks' {
        & git -C native/vice diff -- vice/src/mainc64cpu.c
        & git -C native/vice diff --numstat
        & git -C native/vice diff --cached --stat
    }
    'CommitDrift' { & git diff --name-only 4c5cb166..HEAD }
    'Historical' {
        $preservationPath = 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta'
        $paths = @(Get-Content -LiteralPath (Join-Path $preservationPath 'native-vice-untracked-paths.txt'))
        [pscustomobject]@{untrackedCount=$paths.Count;extensionCounts=@($paths | Group-Object { [IO.Path]::GetExtension($_) } | Sort-Object Count -Descending | Select-Object Count,Name);headerSha256=(Get-FileHash -LiteralPath (Join-Path $preservationPath 'vice-shim-runtime.h.preserved') -Algorithm SHA256).Hash} | ConvertTo-Json -Depth 8
        $lines = @(Get-Content -LiteralPath (Join-Path $preservationPath 'native-vice-tracked.patch'))
        $currentFile = ''
        $hunks = for ($lineIndex = 0; $lineIndex -lt $lines.Count; $lineIndex++) {
            if ($lines[$lineIndex] -match '^diff --git a/(.*) b/') { $currentFile = $Matches[1] }
            if ($lines[$lineIndex].StartsWith('@@')) { [pscustomobject]@{path=$currentFile;patchLine=$lineIndex+1;header=$lines[$lineIndex]} }
        }
        if ($PSCmdlet.ShouldProcess('patch-hunks.json','Record complete patch hunk inventory')) { $hunks | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'patch-hunks.json') -Encoding utf8 }
        $hunks | ConvertTo-Json -Depth 8
    }
}
Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffzzz'
