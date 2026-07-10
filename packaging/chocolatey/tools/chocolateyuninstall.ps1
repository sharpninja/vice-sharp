$ErrorActionPreference = 'Stop'

# Uninstall by locating the ViceSharp product in the Windows uninstall registry
# and running its MSI uninstall silently.
$packageName = 'vice-sharp'
[array]$key = Get-UninstallRegistryKey -SoftwareName 'ViceSharp*'

if ($key.Count -eq 1) {
    $key | ForEach-Object {
        $packageArgs = @{
            packageName    = $packageName
            fileType       = 'msi'
            # $_.PSChildName is the MSI product code {GUID}.
            silentArgs     = "$($_.PSChildName) /qn /norestart"
            validExitCodes = @(0, 3010, 1605, 1614, 1641)
            file           = ''
        }
        Uninstall-ChocolateyPackage @packageArgs
    }
}
elseif ($key.Count -eq 0) {
    Write-Warning "$packageName is not installed (no matching uninstall registry key)."
}
else {
    Write-Warning "Found $($key.Count) matches for 'ViceSharp*'; skipping automatic uninstall to avoid removing the wrong product."
}
