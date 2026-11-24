[CmdletBinding(SupportsShouldProcess)]
param([ValidateSet('Store', 'Inventory', 'Integrity')][string]$Mode = 'Store')
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess('Receipt-local evidence processing', $Mode)) { return }
switch ($Mode) {
    'Store' {
        Import-Module powershell-yaml
        $effective = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'store-effective.txt') -Raw | ConvertFrom-Yaml -AllDocuments)[0].payload.result
        $selectedIds = @('FR-VICE-001', 'TR-VICE-ORACLE-001', 'TEST-VICE-001', 'TR-VSFLOCKSTEP-RESUME-001', 'TR-CYCLE-001', 'FR-SNP-005', 'TEST-SNP-002')
        $selected = foreach ($key in @('functional', 'technical', 'testing')) {
            foreach ($record in $effective[$key]) {
                if ($record.id -in $selectedIds) { $record }
            }
        }
        $selected | ConvertTo-Json -Depth 25 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'store-effective.selected.json')
        $selected | ConvertTo-Json -Depth 25
    }
    'Inventory' {
        $paths = @(Get-Content -LiteralPath 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-untracked-paths.txt')
        $patch = Get-Content -LiteralPath 'docs/receipts/native-vice/20261004T090000-0500-preexisting-delta/native-vice-tracked.patch'
        $hunks = for ($index = 0; $index -lt $patch.Count; $index++) { if ($patch[$index].StartsWith('@@')) { @{ line = ($index + 1); header = $patch[$index] } } }
        $historical = [ordered]@{
            entryCount = $paths.Count
            extensionCounts = @($paths | Group-Object { [IO.Path]::GetExtension($_) } -CaseSensitive | ForEach-Object { @{ extension = $_.Name; count = $_.Count } })
            injectedPaths = @($paths | Where-Object { $_ -match 'vice-shim|shim-configure' })
            hunkCount = @($hunks).Count
            hunks = @($hunks)
        }
        $historical | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'historical-inventory.json')
        $historical | ConvertTo-Json -Depth 6
    }
    'Integrity' {
        $nativeStatus = @(git -C native/vice status --porcelain=v1 -uall)
        $entries = foreach ($line in $nativeStatus) {
            $relative = $line.Substring(3)
            $fullPath = Join-Path 'native/vice' $relative
            [ordered]@{ status = $line.Substring(0, 2); path = $relative; length = (Get-Item -LiteralPath $fullPath -Force).Length; sha256 = (Get-FileHash -LiteralPath $fullPath).Hash }
        }
        [ordered]@{ timestampLocal = (Get-Date -Format s); entries = @($entries) } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'native-content-bracket.json')
        Write-Output ('Retained native content inventory: ' + $entries.Count + ' paths')
    }
}
