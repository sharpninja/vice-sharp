<#
.SYNOPSIS
Publish ViceSharp requirements wiki to Azure DevOps and (optionally) GitHub.

.DESCRIPTION
REPO-MAINT-001 wiki publishing automation. Both targets are git-backed wiki
repos. This script:
  1. Refreshes the MCP-generated wiki source set under docs/Project/wiki/{target}/.
  2. Clones each target wiki repo into a temp dir.
  3. Mirrors the source dir into the clone (preserving _Sidebar/_Footer/.order).
  4. Commits + pushes when there are any actual changes.

Auth:
  Azure DevOps : $env:ADO_PAT (PAT with Wiki Read/Write scope on McpServer/VICE-Sharp).
  GitHub       : $env:GITHUB_TOKEN (PAT with public_repo scope on sharpninja/vice-sharp).

If a token is missing the corresponding target is skipped with a warning so the
script is safe to run unattended in CI.

.PARAMETER Target
"azure", "github", or "both" (default).

.PARAMETER DryRun
Stage all changes in the clone but do not push or commit. Use to inspect diffs.

.PARAMETER RegenerateSource
Re-export the requirements wiki ZIP from the MCP server before publishing
(REPO-MAINT-001 step). Requires MCP server reachable and $env:MCP_API_KEY OR
ApiKey/BaseUrl args.

.PARAMETER ApiKey
Optional explicit MCP API key (overrides $env:MCP_API_KEY).

.PARAMETER BaseUrl
Optional explicit MCP base URL (default http://PAYTON-LEGION2:7147).

.EXAMPLE
pwsh -File tools/Publish-Wiki.ps1 -Target both -DryRun

.EXAMPLE
$env:ADO_PAT='xyz'; pwsh -File tools/Publish-Wiki.ps1 -Target azure
#>
param(
    [ValidateSet("azure","github","both")]
    [string]$Target = "both",
    [switch]$DryRun,
    [switch]$RegenerateSource,
    [string]$ApiKey = $env:MCP_API_KEY,
    [string]$BaseUrl = "http://PAYTON-LEGION2:7147"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$wikiRoot = Join-Path $repoRoot "docs/Project/wiki"

function Write-Step($msg) { Write-Host "[wiki] $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "[wiki] WARN $msg" -ForegroundColor Yellow }
function Write-OK($msg)   { Write-Host "[wiki] OK   $msg" -ForegroundColor Green }

function Invoke-RegenerateSource {
    param([string]$apiKey, [string]$baseUrl, [string]$wikiRoot)
    if (-not $apiKey) {
        Write-Warn "RegenerateSource requested but no MCP_API_KEY / -ApiKey provided. Skipping."
        return
    }
    Write-Step "Regenerating wiki source from $baseUrl"
    $zipPath = Join-Path $repoRoot "docs/requirements/requirements-wiki-documents.zip"
    $headers = @{ "X-Api-Key" = $apiKey }
    $r = Invoke-WebRequest -Method Get -Uri "$baseUrl/mcpserver/requirements/generate?format=wiki&docType=all" -Headers $headers
    [System.IO.File]::WriteAllBytes($zipPath, $r.Content)
    Write-OK "Wrote $zipPath ($($r.Content.Length) bytes)"

    # Expand into docs/Project/wiki/{azure,github}/, preserving _Sidebar/_Footer/.order.
    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("vicewiki-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $tmp | Out-Null
    Expand-Archive -Path $zipPath -DestinationPath $tmp -Force
    foreach ($flavor in @("azure","github")) {
        $src = Join-Path $tmp $flavor
        $dst = Join-Path $wikiRoot $flavor
        if (-not (Test-Path $src)) { continue }
        if (-not (Test-Path $dst)) { New-Item -ItemType Directory -Force -Path $dst | Out-Null }
        Get-ChildItem -Path $src -File | ForEach-Object {
            Copy-Item -Path $_.FullName -Destination (Join-Path $dst $_.Name) -Force
        }
    }
    Remove-Item -Recurse -Force $tmp
    Write-OK "Refreshed wiki source under $wikiRoot"
}

function Publish-AzureWiki {
    param([string]$wikiRoot, [bool]$dryRun)
    if (-not $env:ADO_PAT) {
        Write-Warn "ADO_PAT not set; skipping Azure DevOps wiki publish."
        return
    }
    $src = Join-Path $wikiRoot "azure"
    if (-not (Test-Path $src)) {
        Write-Warn "No source at $src; skipping Azure wiki publish."
        return
    }

    $wikiUrl = "https://dev.azure.com/McpServer/VICE-Sharp/_git/VICE-Sharp.wiki"
    $authUrl = $wikiUrl -replace "^https://","https://anything:$env:ADO_PAT@"
    $clone = Join-Path ([System.IO.Path]::GetTempPath()) ("vicewiki-azure-" + [Guid]::NewGuid().ToString("N"))

    Write-Step "Cloning Azure DevOps wiki into $clone"
    & git clone --depth 1 $authUrl $clone 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "git clone failed for ADO wiki." }

    try {
        Get-ChildItem -Path $src -File | ForEach-Object {
            Copy-Item -Path $_.FullName -Destination (Join-Path $clone $_.Name) -Force
        }

        & git -C $clone add -A | Out-Null
        $diff = & git -C $clone diff --cached --stat
        if (-not $diff) { Write-OK "Azure wiki already in sync."; return }
        Write-Host $diff

        if ($dryRun) {
            Write-OK "[dry-run] Azure wiki staged; not pushing."
            return
        }
        & git -C $clone -c user.name="vicesharp-ci" -c user.email="ci@vicesharp.local" commit -m "chore(wiki): sync requirements pages" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "git commit failed for ADO wiki." }
        & git -C $clone push origin HEAD:wikiMaster 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "git push failed for ADO wiki." }
        Write-OK "Azure DevOps wiki published."
    }
    finally {
        Remove-Item -Recurse -Force $clone -ErrorAction SilentlyContinue
    }
}

function Publish-GitHubWiki {
    param([string]$wikiRoot, [bool]$dryRun)
    if (-not $env:GITHUB_TOKEN) {
        Write-Warn "GITHUB_TOKEN not set; skipping GitHub wiki publish."
        return
    }
    $src = Join-Path $wikiRoot "github"
    if (-not (Test-Path $src)) {
        Write-Warn "No source at $src; skipping GitHub wiki publish."
        return
    }

    $wikiUrl = "https://github.com/sharpninja/vice-sharp.wiki.git"
    $authUrl = $wikiUrl -replace "^https://","https://$env:GITHUB_TOKEN@"
    $clone = Join-Path ([System.IO.Path]::GetTempPath()) ("vicewiki-github-" + [Guid]::NewGuid().ToString("N"))

    Write-Step "Cloning GitHub wiki into $clone"
    & git clone --depth 1 $authUrl $clone 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "git clone failed for GitHub wiki." }

    try {
        Get-ChildItem -Path $src -File | ForEach-Object {
            Copy-Item -Path $_.FullName -Destination (Join-Path $clone $_.Name) -Force
        }

        & git -C $clone add -A | Out-Null
        $diff = & git -C $clone diff --cached --stat
        if (-not $diff) { Write-OK "GitHub wiki already in sync."; return }
        Write-Host $diff

        if ($dryRun) {
            Write-OK "[dry-run] GitHub wiki staged; not pushing."
            return
        }
        & git -C $clone -c user.name="vicesharp-ci" -c user.email="ci@vicesharp.local" commit -m "chore(wiki): sync requirements pages" | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "git commit failed for GitHub wiki." }
        & git -C $clone push origin HEAD:master 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "git push failed for GitHub wiki." }
        Write-OK "GitHub wiki published."
    }
    finally {
        Remove-Item -Recurse -Force $clone -ErrorAction SilentlyContinue
    }
}

# Main
Write-Step "ViceSharp wiki publisher - target=$Target dryRun=$DryRun"

if ($RegenerateSource) {
    Invoke-RegenerateSource -apiKey $ApiKey -baseUrl $BaseUrl -wikiRoot $wikiRoot
}

if ($Target -eq "azure" -or $Target -eq "both") {
    Publish-AzureWiki -wikiRoot $wikiRoot -dryRun $DryRun.IsPresent
}
if ($Target -eq "github" -or $Target -eq "both") {
    Publish-GitHubWiki -wikiRoot $wikiRoot -dryRun $DryRun.IsPresent
}

Write-OK "Done."
