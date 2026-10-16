<#
.SYNOPSIS
Validates a PackYay artifact and the adjacent package recipe and payload.
.DESCRIPTION
Checks the installable archive rather than the publish directory: package identity,
self-contained .NET runtime, executable permissions, desktop integration, licensing,
and source checksums. Covers the Linux packaging acceptance of TR-BUILD-001 and
TR-PLAT-001. Does not install or launch the desktop application.
.EXAMPLE
pwsh tests/packaging/Verify-ArchPackage.ps1 -PackagePath artifacts/yay/1.3.3/x86_64/vicesharp-1.3.3-1-x86_64.pkg.tar.zst
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $PackagePath,

    [string] $ExpectedVersion,

    [ValidateSet('x86_64', 'aarch64')]
    [string] $ExpectedArchitecture
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

function Assert-Package {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) {
        throw "Arch package validation failed: $Message"
    }
}

Assert-Package (Test-Path -LiteralPath $PackagePath -PathType Leaf) "Package does not exist: $PackagePath"
$package = (Get-Item -LiteralPath $PackagePath).FullName
$packageDirectory = [IO.Path]::GetDirectoryName($package)
$tar = (Get-Command bsdtar -CommandType Application -ErrorAction Stop).Source

function Invoke-Archive {
    param([string[]] $Arguments)
    $output = @(& $tar @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    Assert-Package ($exitCode -eq 0) "bsdtar exited $exitCode`: $($output -join [Environment]::NewLine)"
    return ($output | ForEach-Object { $_.ToString() })
}

function Read-Metadata {
    param([string] $Text)
    $metadata = @{}
    foreach ($line in ($Text -split '\r?\n')) {
        if ($line -match '^\s*([^#\s=]+)\s*=\s*(.*?)\s*$') {
            $key = $Matches[1]
            $metadata[$key] = @($metadata[$key]) + $Matches[2]
        }
    }
    return $metadata
}

function Get-OneValue {
    param([hashtable] $Metadata, [string] $Key, [string] $Source)
    $values = @($Metadata[$Key] | Where-Object { $null -ne $_ })
    Assert-Package ($values.Count -eq 1) "$Source must contain one '$Key' value."
    return $values[0]
}

$entries = @{}
$archiveEntries = @(Invoke-Archive @('-tf', $package))
foreach ($entry in $archiveEntries) {
    $normalized = $entry -replace '^\./', ''
    Assert-Package ($normalized -notmatch '^/|(^|/)\.\.(/|$)') "Unsafe archive path: $entry"
    Assert-Package (-not $entries.ContainsKey($normalized)) "Duplicate archive path: $entry"
    $entries[$normalized] = $entry
}

function Read-Entry {
    param([string] $Path)
    Assert-Package ($entries.ContainsKey($Path)) "Missing archive entry: $Path"
    return (@(Invoke-Archive @('-xOf', $package, '--', $entries[$Path])) -join "`n")
}

$info = Read-Metadata (Read-Entry '.PKGINFO')
$name = Get-OneValue $info 'pkgname' '.PKGINFO'
$versionAndRelease = Get-OneValue $info 'pkgver' '.PKGINFO'
$architecture = Get-OneValue $info 'arch' '.PKGINFO'
Assert-Package ($name -ceq 'vicesharp') "Expected pkgname vicesharp, found $name."
Assert-Package ($architecture -in @('x86_64', 'aarch64')) "Unsupported architecture: $architecture"
$dependencies = @($info['depend'] | ForEach-Object { $_ -replace '[<>=].*$', '' })
foreach ($dependency in @('glibc', 'icu', 'openssl', 'libx11', 'libxcursor', 'fontconfig')) {
    Assert-Package ($dependency -in $dependencies) "Missing system runtime dependency: $dependency"
}
foreach ($dependency in @('libgcc', 'libstdc++')) {
    Assert-Package ($dependency -in $dependencies -or 'gcc-libs' -in $dependencies) "Missing system runtime dependency: $dependency"
}
Assert-Package (@($dependencies | Where-Object { $_ -match '(?i)^(?:dotnet|aspnet)' }).Count -eq 0) 'Self-contained package must not depend on a system .NET SDK or runtime.'
Assert-Package ($versionAndRelease -match '^(.+)-(\d+)$') "Invalid package version: $versionAndRelease"
$version = $Matches[1]
$release = $Matches[2]
if ($ExpectedVersion) {
    Assert-Package ($version -ceq $ExpectedVersion) "Expected version $ExpectedVersion, found $version."
}
if ($ExpectedArchitecture) {
    Assert-Package ($architecture -ceq $ExpectedArchitecture) "Expected architecture $ExpectedArchitecture, found $architecture."
}

$requiredFiles = @(
    'usr/bin/vicesharp'
    'usr/lib/vicesharp/ViceSharp.Avalonia'
    'usr/lib/vicesharp/ViceSharp.Avalonia.dll'
    'usr/lib/vicesharp/ViceSharp.Avalonia.deps.json'
    'usr/lib/vicesharp/ViceSharp.Avalonia.runtimeconfig.json'
    'usr/lib/vicesharp/libcoreclr.so'
    'usr/lib/vicesharp/libhostfxr.so'
    'usr/lib/vicesharp/libhostpolicy.so'
    'usr/lib/vicesharp/libSkiaSharp.so'
    'usr/lib/vicesharp/libHarfBuzzSharp.so'
    'usr/lib/vicesharp/System.Private.CoreLib.dll'
    'usr/lib/vicesharp/System.Runtime.dll'
    'usr/lib/vicesharp/Microsoft.AspNetCore.dll'
    'usr/share/applications/vicesharp.desktop'
    'usr/share/icons/hicolor/scalable/apps/vicesharp.svg'
    'usr/share/licenses/vicesharp/COPYING'
    'usr/share/licenses/vicesharp/THIRD_PARTY_NOTICES.md'
)
foreach ($file in $requiredFiles) {
    Assert-Package ($entries.ContainsKey($file)) "Missing installed file: $file"
}

# FR-INP-006 / TEST-INPUT-001: all redistributable C64 maps must reach the
# installed payload, not just exist in the source tree or build output.
$keymapSource = Join-Path $PSScriptRoot '../../src/ViceSharp.Xbox/Assets/vice-data/C64'
$keymaps = @(Get-ChildItem -LiteralPath $keymapSource -Filter '*.vkm' -File | Sort-Object Name)
Assert-Package ($keymaps.Count -gt 1) 'Vendored C64 keymap set is missing.'
foreach ($keymap in $keymaps) {
    $installedPath = "usr/lib/vicesharp/data/C64/$($keymap.Name)"
    Assert-Package ($entries.ContainsKey($installedPath)) "Missing installed keymap: $installedPath"
    $actual = (Read-Entry $installedPath).Replace("`r`n", "`n").TrimEnd("`n")
    $expected = [IO.File]::ReadAllText($keymap.FullName).Replace("`r`n", "`n").TrimEnd("`n")
    Assert-Package ($actual -ceq $expected) "Installed keymap differs from vendored VICE data: $($keymap.Name)"
}

$permissions = @{}
$listing = @(Invoke-Archive @('-tvf', $package, '--numeric-owner'))
Assert-Package ($listing.Count -eq $archiveEntries.Count) 'Verbose archive listing does not match the entry list.'
for ($index = 0; $index -lt $archiveEntries.Count; $index++) {
    # Both listings follow archive order. Match the known full path instead of
    # splitting whitespace, which would corrupt paths containing spaces.
    $entry = $archiveEntries[$index]
    $detail = $listing[$index]
    Assert-Package ($detail -match '^[bcdhlps-][rwxstST-]{9}[+@.]?\s') "Cannot parse archive permissions: $detail"
    $pathPattern = '\s' + [regex]::Escape($entry) + '(?: -> .+| link to .+)?$'
    Assert-Package ($detail -cmatch $pathPattern) "Verbose archive entry does not match $entry."
    $path = $entry -replace '^\./', ''
    $mode = $detail.Substring(0, 10)
    $permissions[$path] = $mode
    if ($path -match '^usr/lib/vicesharp(?:/|$)') {
        if ($mode[0] -eq '-') {
            Assert-Package ($mode -match '^-r..r..r..$') "Installed runtime file must be readable by all users: $path ($mode)"
        }
        elseif ($mode[0] -eq 'd') {
            Assert-Package ($mode -match '^d..[xs]..[xs]..[xt]$') "Installed runtime directory must be traversable by all users: $path ($mode)"
        }
    }
}
foreach ($file in @('usr/bin/vicesharp', 'usr/lib/vicesharp/ViceSharp.Avalonia')) {
    Assert-Package ($permissions[$file] -match '^-[r-][w-]x[r-][w-]x[r-][w-]x$') "Installed entry must be a regular file executable by all users: $file"
}

$roms = @($entries.Keys | Where-Object {
    $_ -match '(?i)\.(bin|rom)$|(?:^|/)(?:kernal|basic|chargen|dos1541|dos1571|dos1581)(?:[._-]|$)'
})
Assert-Package ($roms.Count -eq 0) "ROM dumps must not be packaged: $($roms -join ', ')"

$runtime = (Read-Entry 'usr/lib/vicesharp/ViceSharp.Avalonia.runtimeconfig.json' | ConvertFrom-Json -AsHashtable).runtimeOptions
Assert-Package ($runtime -is [hashtable]) 'Missing runtimeOptions in runtime configuration.'
Assert-Package (-not $runtime.ContainsKey('framework') -and -not $runtime.ContainsKey('frameworks')) 'Application requires a shared .NET runtime.'
foreach ($framework in @('Microsoft.NETCore.App', 'Microsoft.AspNetCore.App')) {
    $frameworks = @($runtime['includedFrameworks'] | Where-Object { $_.name -eq $framework })
    Assert-Package ($frameworks.Count -eq 1) "Self-contained runtime must include $framework."
    Assert-Package ([version]$frameworks[0].version -ge [version]'10.0') "Packaged $framework runtime must be .NET 10 or newer."
}
$deps = Read-Entry 'usr/lib/vicesharp/ViceSharp.Avalonia.deps.json' | ConvertFrom-Json -AsHashtable
$rid = if ($architecture -eq 'x86_64') { 'linux-x64' } else { 'linux-arm64' }
Assert-Package ($deps.runtimeTarget.name.EndsWith("/$rid", [StringComparison]::Ordinal)) "Application dependencies do not target $rid."

$wrapper = Read-Entry 'usr/bin/vicesharp'
Assert-Package ($wrapper -match '\A#!/(?:usr/)?bin/(?:env\s+)?(?:ba)?sh\r?\n') 'Launcher must have a shell shebang.'
# A direct exec with quoted "$@" retains spaces, empty arguments and signals.
Assert-Package ($wrapper -match '(?m)^\s*exec\s+(?:"/usr/lib/vicesharp/ViceSharp\.Avalonia"|/usr/lib/vicesharp/ViceSharp\.Avalonia)\s+"\$@"\s*$') 'Launcher must exec the installed application and preserve arguments with quoted "$@".'

$desktop = Read-Entry 'usr/share/applications/vicesharp.desktop'
Assert-Package ($desktop -match '(?m)^\[Desktop Entry\]\s*$') 'Desktop entry group is missing.'
Assert-Package ($desktop -match '(?m)^Type=Application\s*$') 'Desktop entry must be an application.'
Assert-Package ($desktop -match '(?m)^Exec=(?:/usr/bin/)?vicesharp(?:\s+%[fFuU])?\s*$') 'Desktop entry must launch vicesharp.'
Assert-Package ($desktop -match '(?m)^Icon=vicesharp\s*$') 'Desktop entry must reference the installed icon.'
Assert-Package ($desktop -match '(?m)^Terminal=false\s*$') 'Desktop application must not require a terminal.'
$icon = Read-Entry 'usr/share/icons/hicolor/scalable/apps/vicesharp.svg'
Assert-Package ($icon -match '<svg(?:\s|>)') 'Installed icon is not an SVG document.'
Assert-Package ((Read-Entry 'usr/share/licenses/vicesharp/COPYING') -match 'GNU GENERAL PUBLIC LICENSE') 'Installed COPYING is not the GPL license.'
Assert-Package (-not [string]::IsNullOrWhiteSpace((Read-Entry 'usr/share/licenses/vicesharp/THIRD_PARTY_NOTICES.md'))) 'Third-party notices must not be empty.'

$recipePath = Join-Path $packageDirectory 'PKGBUILD'
$sourceInfoPath = Join-Path $packageDirectory '.SRCINFO'
Assert-Package (Test-Path -LiteralPath $recipePath -PathType Leaf) 'Adjacent PKGBUILD is missing.'
Assert-Package (Test-Path -LiteralPath $sourceInfoPath -PathType Leaf) 'Adjacent .SRCINFO is missing.'
$sourceInfo = Read-Metadata (Get-Content -LiteralPath $sourceInfoPath -Raw)
$expectedMetadata = @{ pkgname = $name; pkgver = $version; pkgrel = $release; arch = $architecture }
foreach ($item in $expectedMetadata.GetEnumerator()) {
    $actual = Get-OneValue $sourceInfo $item.Key '.SRCINFO'
    Assert-Package ($actual -ceq $item.Value) ".SRCINFO $($item.Key) '$actual' does not match package '$($item.Value)'."
}

$payloadName = "vicesharp-$version-$architecture.tar.gz"
$sourceKey = if ($sourceInfo.ContainsKey("source_$architecture")) { "source_$architecture" } else { 'source' }
$checksumKey = if ($sourceInfo.ContainsKey("sha256sums_$architecture")) { "sha256sums_$architecture" } else { 'sha256sums' }
$source = Get-OneValue $sourceInfo $sourceKey '.SRCINFO'
$expectedHash = Get-OneValue $sourceInfo $checksumKey '.SRCINFO'
Assert-Package ($source -ceq $payloadName) ".SRCINFO must reference adjacent payload $payloadName."
Assert-Package ($expectedHash -match '^[0-9a-fA-F]{64}$') '.SRCINFO must contain a SHA-256 checksum; SKIP is not accepted.'
$payload = Join-Path $packageDirectory $payloadName
Assert-Package (Test-Path -LiteralPath $payload -PathType Leaf) "Adjacent source payload is missing: $payloadName"
$actualHash = (Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash
Assert-Package ($actualHash -ieq $expectedHash) 'Source payload SHA-256 does not match .SRCINFO.'
$recipe = Get-Content -LiteralPath $recipePath -Raw
$checksumPattern = '(?ms)^\s*sha256sums(?:_' + [regex]::Escape($architecture) + ')?\s*=\s*\(\s*[''"\s]*' + [regex]::Escape($actualHash) + '[''"\s]*\)'
Assert-Package ($recipe -imatch $checksumPattern) 'PKGBUILD checksum does not match the adjacent source payload.'

Write-Output "PASS: $name $versionAndRelease ($architecture); $($requiredFiles.Count) required installed files and $($keymaps.Count) C64 keymaps; self-contained runtime, launchers, ROM exclusion, metadata and payload SHA-256 verified."
