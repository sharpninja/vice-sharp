[CmdletBinding(SupportsShouldProcess)]
param()
$ErrorActionPreference = 'Stop'
$requests = @(
    @{ name = 'fr-vice'; method = 'workflow.requirements.getFr'; payload = @{ id = 'FR-VICE-001' } },
    @{ name = 'fr-snapshot'; method = 'workflow.requirements.getFr'; payload = @{ id = 'FR-SNP-005' } },
    @{ name = 'tr-oracle'; method = 'workflow.requirements.getTr'; payload = @{ id = 'TR-VICE-ORACLE-001' } },
    @{ name = 'tr-resume'; method = 'workflow.requirements.getTr'; payload = @{ id = 'TR-VSFLOCKSTEP-RESUME-001' } },
    @{ name = 'tr-cycle'; method = 'workflow.requirements.getTr'; payload = @{ id = 'TR-CYCLE-001' } },
    @{ name = 'test-vice'; method = 'workflow.requirements.getTest'; payload = @{ id = 'TEST-VICE-001' } },
    @{ name = 'test-snapshot'; method = 'workflow.requirements.getTest'; payload = @{ id = 'TEST-SNP-002' } },
    @{ name = 'vice-mappings'; method = 'workflow.requirements.listMappings'; payload = @{ frId = 'FR-VICE-001' } },
    @{ name = 'snapshot-mappings'; method = 'workflow.requirements.listMappings'; payload = @{ frId = 'FR-SNP-005' } },
    @{ name = 'oracle-todo'; method = 'workflow.todo.get'; payload = @{ id = 'PLAN-VICEORACLE-001' } }
)
foreach ($request in $requests) {
    if ($PSCmdlet.ShouldProcess($request.method, 'Query authoritative MCP state through supported wrapper')) {
        $payloadPath = Join-Path $PSScriptRoot ($request.name + '.params.json')
        $request.payload | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $payloadPath -Encoding utf8
        $receiptName = $request.name + '.response.yaml'
        $outputText = & (Join-Path $PSScriptRoot 'review-mcp.ps1') -Operation Invoke -Method $request.method -PayloadPath $payloadPath -ReceiptName $receiptName
        [pscustomobject]@{ timestampLocal = (Get-Date).ToString('s'); method = $request.method; receipt = $receiptName; characters = ($outputText | Out-String).Length } | ConvertTo-Json -Compress
    }
}
