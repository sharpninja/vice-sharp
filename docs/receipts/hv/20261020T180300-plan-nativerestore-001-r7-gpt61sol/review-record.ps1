[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)][string]$Text,
    [ValidateSet('observation','decision','reasoning')][string]$Category = 'observation'
)
$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess('Reviewer session-log turn', 'Record review checkpoint')) { return }
$eventId = (Get-Date).ToString('yyyyMMddTHHmmssfff')
$payloadPath = Join-Path $PSScriptRoot ('dialog-' + $eventId + '.json')
$payload = [ordered]@{ dialogItems = @([ordered]@{ timestamp = (Get-Date).ToString('o'); role = 'model'; content = $Text; category = $Category }) }
$payload | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $payloadPath -Encoding utf8
& (Join-Path $PSScriptRoot 'review-mcp.ps1') -Operation Invoke -Method workflow.sessionlog.appendDialog -PayloadPath $payloadPath -ReceiptName ('dialog-' + $eventId + '.receipt.txt')
$actionPath = Join-Path $PSScriptRoot ('action-' + $eventId + '.json')
$actionOrder = [int](@(Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter 'action-*.json').Count + 1)
$actionPayload = [ordered]@{ actions = @([ordered]@{ order = $actionOrder; description = $Text; type = $(if ($Category -eq 'decision') { 'design_decision' } else { 'validation' }); status = 'completed'; filePath = $PSScriptRoot }) }
$actionPayload | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $actionPath -Encoding utf8
& (Join-Path $PSScriptRoot 'review-mcp.ps1') -Operation Invoke -Method workflow.sessionlog.appendActions -PayloadPath $actionPath -ReceiptName ('action-' + $eventId + '.receipt.txt')
