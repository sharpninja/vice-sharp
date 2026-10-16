<#
.SYNOPSIS
Validates user-space deployment and the DeployYay compatibility alias without installing.
.DESCRIPTION
TR-BUILD-001 / TR-PLAT-001: executes retained-payload, actual launcher/desktop,
unsafe-path, and rollback regression tests in isolated explicit userHome fixtures.
Compiles Nuke and checks its reported DeployYay -> DeployUser target graph.
Requires Linux, desktop-file-validate, and gio. No package manager is invoked.
The caller may serialize .NET work with the repository's shared build lock.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
if (-not $IsLinux) { throw 'User-space deployment validation requires Linux.' }
foreach ($command in @('desktop-file-validate', 'gio')) {
    if (-not (Get-Command $command -CommandType Application -ErrorAction SilentlyContinue)) {
        throw "User-space deployment validation requires $command."
    }
}
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$evidence = Join-Path $repository 'TestResults/user-local-deploy'
$null = [IO.Directory]::CreateDirectory($evidence)
$testLog = Join-Path $evidence 'installer-verified.log'
$graphLog = Join-Path $evidence 'nuke-target-graph.log'
$savedDesktopTools = [Environment]::GetEnvironmentVariable('VICESHARP_DEPLOY_TEST_DESKTOP_TOOLS')
Push-Location $repository
try {
    $env:VICESHARP_DEPLOY_TEST_DESKTOP_TOOLS = '1'
    'Command: dotnet test tests/ViceSharp.Library.Tests/ViceSharp.Library.Tests.csproj -c Release --filter FullyQualifiedName~UserLocalDeploymentTests --logger trx;LogFileName=installer-verified.trx --results-directory TestResults/user-local-deploy' |
        Set-Content -LiteralPath $testLog
    & dotnet test tests/ViceSharp.Library.Tests/ViceSharp.Library.Tests.csproj -c Release `
        --filter 'FullyQualifiedName~UserLocalDeploymentTests' `
        --logger 'trx;LogFileName=installer-verified.trx' --results-directory $evidence 2>&1 |
        Tee-Object -FilePath $testLog -Append
    if ($LASTEXITCODE -ne 0) { throw "Installer regression gate failed with exit $LASTEXITCODE." }
    [xml]$trx = Get-Content -LiteralPath (Join-Path $evidence 'installer-verified.trx') -Raw
    $counts = $trx.TestRun.ResultSummary.Counters
    if ([int]$counts.total -lt 13 -or [int]$counts.failed -ne 0 -or [int]$counts.notExecuted -ne 0 `
        -or [int]$counts.passed -ne [int]$counts.total) {
        throw 'Installer gate must execute at least 13 cases, all passing and none skipped.'
    }

    'Command: dotnet run --project build/_build.csproj -c Release -- --help --no-logo' |
        Set-Content -LiteralPath $graphLog
    & dotnet run --project build/_build.csproj -c Release -- --help --no-logo 2>&1 |
        Tee-Object -FilePath $graphLog -Append
    if ($LASTEXITCODE -ne 0) { throw "Nuke graph inspection failed with exit $LASTEXITCODE." }
    $graph = Get-Content -LiteralPath $graphLog -Raw
    if ($graph -notmatch '(?m)^\s+DeployYay\s+-> DeployUser\s*$' `
        -or $graph -notmatch '(?m)^\s+DeployUser\s*$') {
        throw 'DeployYay must depend only on DeployUser, and DeployUser must not depend on package targets.'
    }
    [ordered]@{
        agent = 'Codex'; utc = [DateTime]::UtcNow.ToString('O')
        configuration = 'Release'; filter = 'FullyQualifiedName~UserLocalDeploymentTests'
        passed = [int]$counts.passed; failed = [int]$counts.failed; skipped = [int]$counts.notExecuted
        actualDesktopTools = @('desktop-file-validate', 'gio launch')
        alias = 'DeployYay -> DeployUser'; actualDeployment = $false
        evidence = @($testLog, (Join-Path $evidence 'installer-verified.trx'), $graphLog)
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidence 'installer-validation.json')
    Write-Output "PASS: $($counts.passed) installer tests; zero failures/skips; actual desktop launch verified; DeployYay delegates only to DeployUser. No installation performed."
}
finally {
    [Environment]::SetEnvironmentVariable('VICESHARP_DEPLOY_TEST_DESKTOP_TOOLS', $savedDesktopTools)
    Pop-Location
}
