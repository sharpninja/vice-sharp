#Requires -Version 7.4
<#
.SYNOPSIS
    Payton, 2026-10-19: "Make it so" (Cline v2 reinstall) and "Why would you need permission to do a build?".
    Reinstalls the Cline v2 mcpserver plugin on PAYTON-LEGION2 at ee63577a (1.121.0).

    The installed layout (verified against the 1.118.0 copy) is the repo's tracked files minus tests/, plus the built
    dist/, under <install>\package, with Cline's own package.json wrapper one level up (left as is).
      1. Stage: git archive of BuildDir HEAD, tests/ removed, plus BuildDir\dist (built by tsc; jest 27/27 green).
      2. Back up: tar of the whole install folder with SHA256 and an entry-count check; the tar is the retained
         original.
      3. Replace <install>\package with the staged tree, then verify the file list equals the stage and the lib hashes
         equal the published fix.
    Writes the receipt to ReceiptPath. -WhatIf stops after staging.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$BuildDir = $PSScriptRoot,
    [string]$Install = (Join-Path $env:USERPROFILE '.cline\plugins\_installed\local\mcpserver-cline-v2-plugin-3c1e3e547a6c'),
    [string]$ExpectedHead = 'ee63577a',
    [string]$ExpectedInvokeSha = 'AF6D1E5B686E88DE53C280EB1D7A9B10711201362614AF08C58B49C9118A0830',
    [string]$ExpectedReplSha = '93AE956EEECB13D2561621A69C88D86A1CDE32026F81445761D75536BA4EE90E',
    [string]$BackupDir = (Join-Path $env:USERPROFILE '.cline\plugin-backups'),
    [Parameter(Mandatory)][string]$ReceiptPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$stamp = Get-Date -Format 'yyyyMMddTHHmmss'
$receipt = [ordered]@{ startedAt = (Get-Date -Format s) }
if ((git -C $BuildDir rev-parse --short=8 HEAD).Trim() -ne $ExpectedHead) { throw "build dir is not at $ExpectedHead" }
if (@(Get-ChildItem -LiteralPath (Join-Path $BuildDir 'dist') -Recurse -File).Count -eq 0) { throw 'dist is empty; build first' }
$pkg = Join-Path $Install 'package'
if (-not (Test-Path -LiteralPath $pkg)) { throw "no installed package at $pkg" }

# 1. Stage.
$stage = Join-Path $env:TEMP "cline-v2-stage-$stamp"
New-Item -ItemType Directory -Path $stage -WhatIf:$false | Out-Null
$zip = Join-Path $env:TEMP "cline-v2-$stamp.tar"
git -C $BuildDir archive --format=tar -o $zip HEAD
if ($LASTEXITCODE -ne 0) { throw 'git archive failed' }
tar -xf $zip -C $stage
if ($LASTEXITCODE -ne 0) { throw 'extract of the archive failed' }
Remove-Item -LiteralPath $zip -WhatIf:$false
if (Test-Path -LiteralPath (Join-Path $stage 'tests')) { Remove-Item -LiteralPath (Join-Path $stage 'tests') -Recurse -Force -WhatIf:$false }
Copy-Item -LiteralPath (Join-Path $BuildDir 'dist') -Destination $stage -Recurse -WhatIf:$false
$stageFiles = @(Get-ChildItem -LiteralPath $stage -Recurse -File -Force | ForEach-Object { $_.FullName.Substring($stage.Length + 1) } | Sort-Object)
$receipt.stage = $stage
$receipt.stageFiles = $stageFiles.Count
$receipt.version = (Get-Content -LiteralPath (Join-Path $stage 'package.json') -Raw | ConvertFrom-Json).version
Write-Information ('staged {0} files (version {1}) at {2}' -f $stageFiles.Count, $receipt.version, $stage) -InformationAction Continue
if (-not $PSCmdlet.ShouldProcess($pkg, "back up $Install and replace package with the staged $ExpectedHead build")) { return }

# 2. Back up.
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null
$tar = Join-Path $BackupDir ('{0}-{1}.tar' -f (Split-Path $Install -Leaf), $stamp)
tar -cf $tar -C (Split-Path $Install -Parent) (Split-Path $Install -Leaf)
if ($LASTEXITCODE -ne 0) { throw 'tar backup failed' }
$src = @(Get-ChildItem -LiteralPath $Install -Recurse -Force).Count
$entries = @(tar -tf $tar).Count - 1
if ($entries -ne $src) { throw "backup entry count $entries does not match source item count $src" }
$receipt.backup = [ordered]@{ tar = $tar; sha256 = (Get-FileHash -LiteralPath $tar).Hash; entries = $entries; oldVersion = (Get-Content -LiteralPath (Join-Path $pkg '.version') -Raw).Trim() }

# 3. Replace and verify.
Remove-Item -LiteralPath $pkg -Recurse -Force
Copy-Item -LiteralPath $stage -Destination $pkg -Recurse
$newFiles = @(Get-ChildItem -LiteralPath $pkg -Recurse -File -Force | ForEach-Object { $_.FullName.Substring($pkg.Length + 1) } | Sort-Object)
$receipt.fileListMatchesStage = (($newFiles -join '|') -ceq ($stageFiles -join '|'))
$receipt.invokeSha = (Get-FileHash -LiteralPath (Join-Path $pkg 'lib\Invoke-McpPlugin.ps1')).Hash
$receipt.replSha = (Get-FileHash -LiteralPath (Join-Path $pkg 'lib\repl-invoke.ps1')).Hash
$receipt.hashesMatch = ($receipt.invokeSha -eq $ExpectedInvokeSha -and $receipt.replSha -eq $ExpectedReplSha)
$receipt.wrapperPackageJsonUnchanged = Test-Path -LiteralPath (Join-Path $Install 'package.json')
$receipt.finishedAt = Get-Date -Format s
$receipt | ConvertTo-Json -Depth 4 | Out-File -LiteralPath $ReceiptPath -Encoding utf8NoBOM
$receipt | ConvertTo-Json -Depth 4
