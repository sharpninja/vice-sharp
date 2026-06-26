#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$IgnoredArguments
)

$ErrorActionPreference = 'Stop'

function Write-ClaudeEnvelope {
    param(
        [string]$Result = '',
        [bool]$IsError = $false,
        [string]$ErrorDetail = ''
    )

    $body = [ordered]@{
        type = 'result'
        subtype = if ($IsError) { 'error' } else { 'success' }
        is_error = $IsError
        result = $Result
    }

    if ($ErrorDetail) {
        $body.error = $ErrorDetail
    }

    $body | ConvertTo-Json -Compress
}

$promptFile = $null
$stderrFile = $null
try {
    $repoRoot = if (-not [string]::IsNullOrWhiteSpace($env:AIUNIT_GROK_REVIEW_ROOT)) {
        Resolve-Path -LiteralPath $env:AIUNIT_GROK_REVIEW_ROOT
    } else {
        Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
    }
    $prompt = [Console]::In.ReadToEnd()

    $promptFile = New-TemporaryFile
    $stderrFile = New-TemporaryFile
    [System.IO.File]::WriteAllText($promptFile.FullName, $prompt, [System.Text.Encoding]::UTF8)

    $grokOutput = & grok `
        --model grok-build `
        --output-format plain `
        --no-alt-screen `
        --max-turns 100 `
        --permission-mode dontAsk `
        --cwd $repoRoot.ProviderPath `
        --prompt-file $promptFile.FullName `
        2> $stderrFile.FullName
    $exitCode = $LASTEXITCODE

    $result = ($grokOutput -join "`n").Trim()
    $stderr = if (Test-Path -LiteralPath $stderrFile.FullName) {
        [System.IO.File]::ReadAllText($stderrFile.FullName).Trim()
    } else {
        ''
    }

    if ($exitCode -ne 0) {
        Write-ClaudeEnvelope -Result $result -IsError $true -ErrorDetail $stderr
        exit $exitCode
    }

    if ([string]::IsNullOrWhiteSpace($result)) {
        Write-ClaudeEnvelope -Result '' -IsError $true -ErrorDetail $stderr
        exit 1
    }

    Write-ClaudeEnvelope -Result $result
    exit 0
}
catch {
    Write-ClaudeEnvelope -Result '' -IsError $true -ErrorDetail $_.Exception.Message
    exit 1
}
finally {
    if ($promptFile -ne $null -and (Test-Path -LiteralPath $promptFile.FullName)) {
        Remove-Item -LiteralPath $promptFile.FullName -Force
    }
    if ($stderrFile -ne $null -and (Test-Path -LiteralPath $stderrFile.FullName)) {
        Remove-Item -LiteralPath $stderrFile.FullName -Force
    }
}
