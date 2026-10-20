[CmdletBinding(SupportsShouldProcess)]
param([string]$OutputName = 'state-before.json')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess($PSScriptRoot, 'Capture read-only git evidence to reviewer receipt')) { return }
$env:GIT_OPTIONAL_LOCKS = '0'
$workspaceRoot = '/home/sharpninja/github/vice-sharp'
$nativeRoot = Join-Path $workspaceRoot 'native/vice'
$commands = @(
    @{ Root = $workspaceRoot; Arguments = @('rev-parse', '--abbrev-ref', 'HEAD') },
    @{ Root = $workspaceRoot; Arguments = @('rev-parse', 'HEAD') },
    @{ Root = $workspaceRoot; Arguments = @('ls-tree', 'HEAD', 'native/vice') },
    @{ Root = $workspaceRoot; Arguments = @('submodule', 'status') },
    @{ Root = $workspaceRoot; Arguments = @('status', '--porcelain=v1', '-uall') },
    @{ Root = $nativeRoot; Arguments = @('rev-parse', 'HEAD') },
    @{ Root = $nativeRoot; Arguments = @('rev-parse', '--git-dir') },
    @{ Root = $nativeRoot; Arguments = @('rev-parse', '--git-path', 'info/exclude') },
    @{ Root = $nativeRoot; Arguments = @('status', '--short', '--untracked-files=no') },
    @{ Root = $nativeRoot; Arguments = @('diff', '--stat') },
    @{ Root = $nativeRoot; Arguments = @('diff', '--numstat') },
    @{ Root = $nativeRoot; Arguments = @('diff', '--no-ext-diff') },
    @{ Root = $nativeRoot; Arguments = @('status', '--porcelain=v1', '-uall') },
    @{ Root = $nativeRoot; Arguments = @('diff', '--cached', '--stat') },
    @{ Root = $workspaceRoot; Arguments = @('diff', '--check') }
)
$results = foreach ($command in $commands) {
    $outputLines = @(& git -C $command.Root @($command.Arguments) 2>&1 | ForEach-Object { [string]$_ })
    [ordered]@{ command = ('git -C ' + $command.Root + ' ' + ($command.Arguments -join ' ')); exitCode = $LASTEXITCODE; lines = $outputLines }
}
$changedFiles = @(& git -C $nativeRoot diff --name-only)
$hashes = foreach ($changedFile in $changedFiles) {
    [ordered]@{ path = $changedFile; sha256 = (Get-FileHash -LiteralPath (Join-Path $nativeRoot $changedFile) -Algorithm SHA256).Hash }
}
$record = [ordered]@{ timestampLocal = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ss.fffzzz'); commands = @($results); changedNativeFileHashes = @($hashes) }
[IO.File]::WriteAllText((Join-Path $PSScriptRoot $OutputName), ($record | ConvertTo-Json -Depth 20), [Text.UTF8Encoding]::new($false))
foreach ($result in $results) {
    if ($result.command -match 'diff --no-ext-diff|status --porcelain') {
        [ordered]@{ command = $result.command; exitCode = $result.exitCode; lineCount = @($result.lines).Count; untrackedCount = @($result.lines | Where-Object { $_ -match '^\?\? ' }).Count } | ConvertTo-Json -Compress
    } else {
        Write-Output $result.command
        Write-Output $result.lines
        Write-Output ('Exit code: ' + $result.exitCode)
    }
}
