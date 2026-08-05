<#
.SYNOPSIS
Stamps the Microsoft Store (Partner Center) identity into the UWP head's
Package.appxmanifest (FEAT-XSTOREPIPE-001).

.DESCRIPTION
The repository carries the DEV identity (local F5 deploys); a Store submission must
carry the identity values reserved in Partner Center. The pipeline rewrites the
manifest in place just before packaging. The manifest is edited as NATIVE XML and
saved, never string-templated.

.PARAMETER ManifestPath
Path to Package.appxmanifest.

.PARAMETER IdentityName
Partner Center Package/Identity/Name (e.g. 12345PublisherName.ViceSharp).

.PARAMETER Publisher
Partner Center Package/Identity/Publisher (the CN=GUID form).

.PARAMETER PublisherDisplayName
Partner Center publisher display name.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ManifestPath,
    [Parameter(Mandatory)] [string] $IdentityName,
    [Parameter(Mandatory)] [string] $Publisher,
    [Parameter(Mandatory)] [string] $PublisherDisplayName
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $ManifestPath)) {
    throw "Manifest not found: $ManifestPath"
}

$manifest = [xml](Get-Content -LiteralPath $ManifestPath -Raw)

$identity = $manifest.Package.Identity
if (-not $identity) {
    throw 'The manifest has no Package/Identity element.'
}

$identity.SetAttribute('Name', $IdentityName)
$identity.SetAttribute('Publisher', $Publisher)

$properties = $manifest.Package.Properties
if (-not $properties) {
    throw 'The manifest has no Package/Properties element.'
}

$properties.PublisherDisplayName = $PublisherDisplayName

$manifest.Save((Resolve-Path -LiteralPath $ManifestPath).Path)

Write-Host "Stamped Store identity: Name='$IdentityName' Publisher='$Publisher' PublisherDisplayName='$PublisherDisplayName' into $ManifestPath"
