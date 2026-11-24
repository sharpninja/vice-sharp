[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string] $Stage,
    [Parameter(Mandatory)][string] $Message
)
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess($Stage, 'Save authorized queued review audit')) { return }
$identity = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'reviewer-identity.json') | ConvertFrom-Json
$entry = [ordered]@{timestampLocal=(Get-Date -Format 's');stage=$Stage;content=$Message;sourceType='Codex';model='gpt-6.1-sol';effort='max';sessionId=$identity.reviewerSessionId;requestId=$identity.reviewerRequestId;parentSessionId=$identity.parentSessionId;parentRequestId=$identity.parentRequestId;primaryPersisted=$false;disposition='QUEUED_ONLY_AUTHORIZED_FAILSAFE'}
$entry | ConvertTo-Json -Depth 12 -Compress | Add-Content -LiteralPath (Join-Path $PSScriptRoot 'review-audit.jsonl') -Encoding utf8
. '/home/sharpninja/github/mcpserver-codex-plugin/lib/yaml-object-mutation.ps1'
$queuePath = Join-Path $PSScriptRoot 'authorized-failsafe'
if (-not (Test-Path -LiteralPath $queuePath)) { New-Item -ItemType Directory -Path $queuePath | Out-Null }
$record = [ordered]@{method='review.audit';label=$Stage;timestampLocal=$entry.timestampLocal;primaryPersisted=$false;params=$entry}
$recordPath = Join-Path $queuePath ((Get-Date -Format 'yyyyMMddTHHmmss') + '-' + $Stage + '.yaml')
Write-McpYamlObject -Path $recordPath -Document $record
[pscustomobject]@{timestampLocal=$entry.timestampLocal;path=$recordPath;sha256=(Get-FileHash -LiteralPath $recordPath -Algorithm SHA256).Hash;primaryPersisted=$false} | ConvertTo-Json
