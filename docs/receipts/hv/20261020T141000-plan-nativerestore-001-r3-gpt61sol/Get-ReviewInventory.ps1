[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string[]]$Path)
$ErrorActionPreference = 'Stop'
foreach ($inventoryPath in $Path) {
    if ($PSCmdlet.ShouldProcess($inventoryPath, 'List review input names')) {
        Get-ChildItem -LiteralPath $inventoryPath -Force | Select-Object Name, Length, Mode | Format-Table -AutoSize
    }
}
