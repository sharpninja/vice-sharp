$ErrorActionPreference = 'Stop'

# The PublishChocolatey Nuke target replaces __URL__ and __SHA256__ with the
# GitHub release MSI asset URL and its SHA256 before `choco pack`. The MSI is
# downloaded at install time (not embedded) so the .nupkg stays small.
$packageName = 'vice-sharp'
$url64       = '__URL__'
$checksum64  = '__SHA256__'

$packageArgs = @{
    packageName    = $packageName
    fileType       = 'msi'
    url64bit       = $url64
    checksum64     = $checksum64
    checksumType64 = 'sha256'
    softwareName   = 'ViceSharp*'
    # Per-machine MSI: silent, no reboot. 3010 = success, reboot required.
    silentArgs     = '/qn /norestart'
    validExitCodes = @(0, 3010)
}

Install-ChocolateyPackage @packageArgs
