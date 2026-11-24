[CmdletBinding(SupportsShouldProcess)]
param([string]$Label = 'active')
$ErrorActionPreference = 'Stop'
if ($Label -notmatch '^[a-z0-9-]+$') { throw 'Unsafe receipt label' }
if (-not $PSCmdlet.ShouldProcess('Own review turn', 'Query supported primary history')) { return }
$identity = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'review-identity.json') -Raw | ConvertFrom-Json
$payloadPath = Join-Path $PSScriptRoot ('history-' + $Label + '.params.json')
@{ agent = 'Codex'; sessionId = $identity.reviewerSessionId; limit = 1; offset = 0 } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $payloadPath -Encoding utf8
$receiptName = 'history-' + $Label + '.yaml'
$null = & (Join-Path $PSScriptRoot 'review-mcp.ps1') -Operation Invoke -Method client.SessionLog.QueryAsync -PayloadPath $payloadPath -ReceiptName $receiptName
Import-Module powershell-yaml -ErrorAction Stop
$history = Get-Content -LiteralPath (Join-Path $PSScriptRoot $receiptName) -Raw | ConvertFrom-Yaml
$sessions = @($history.payload.result.items)
$session = @($sessions | Where-Object { $_.sessionId -ceq $identity.reviewerSessionId })
if ($session.Count -ne 1) { throw 'Own session was not returned exactly once' }
$turn = @($session[0].turns | Where-Object { $_.requestId -ceq $identity.reviewerRequestId })
if ($turn.Count -ne 1) { throw 'Own turn was not returned exactly once' }
$result = [pscustomobject]@{ timestampLocal = (Get-Date).ToString('s'); sessionId = $session[0].sessionId; requestId = $turn[0].requestId; status = $turn[0].status; queryTitle = $turn[0].queryTitle; processingDialogCount = @($turn[0].processingDialog).Count; actionCount = @($turn[0].actions).Count; receipt = $receiptName }
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot ('history-' + $Label + '.proof.json')) -Encoding utf8
$result | ConvertTo-Json -Depth 8
