# Read-only publication screening for the operator-requested checkpoint.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkspacePath,
    [string[]]$ExcludePaths = @()
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $WorkspacePath).ProviderPath
$gitPaths = @(& git -C $root -c color.ui=false -c core.quotepath=false ls-files --modified --others --exclude-standard | Sort-Object -Unique)
if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate checkpoint candidates.' }
$excluded = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $ExcludePaths) { [void]$excluded.Add($entry.Replace('\', '/')) }
$marker = Get-Content -LiteralPath (Join-Path $root 'AGENTS-README-FIRST.yaml') -Raw | ConvertFrom-Yaml
$secret = [string]$marker.apiKey
if ([string]::IsNullOrEmpty($secret)) { throw 'Marker secret unavailable for value-free scan.' }
$findings = [Collections.Generic.List[object]]::new()
$inventory = [Collections.Generic.List[object]]::new()
$pattern = '(?im)(?:api[_-]?key|X-Api-Key|access[_-]?token|client[_-]?secret|password)[\\"''\s:=]+(?<value>[A-Za-z0-9_+/=.!@#$%&*?-]{24,})'
foreach ($relative in $gitPaths) {
    if ($relative -like 'native/vice/*' -or $relative -eq 'native/vice' -or $excluded.Contains($relative)) { continue }
    $path = Join-Path $root $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $file = Get-Item -LiteralPath $path
    $entry = [ordered]@{ path = $relative; bytes = $file.Length; sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
    $inventory.Add([pscustomobject]$entry)
    if ($file.Length -ge 100MB) { $findings.Add([pscustomobject]@{path=$relative; reason='At least 100 MiB'}) }
    if ($file.Extension -match '^\.(jpg|jpeg|png|gif|webp|zip|gz|dll|exe|pdb|vsf|bin|wav)$') { continue }
    $body = [IO.File]::ReadAllText($path)
    if ($body.Contains($secret, [StringComparison]::Ordinal)) { $findings.Add([pscustomobject]@{path=$relative; reason='Exact current marker credential'}) }
    if ($body -match '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----') { $findings.Add([pscustomobject]@{path=$relative; reason='Private-key header'}) }
    foreach ($match in [regex]::Matches($body, $pattern)) {
        $value = $match.Groups['value'].Value
        if ($value -match '^(?:REDACTED|PLACEHOLDER|EXAMPLE|[xX]{10}|[0]{10})' -or $value -match '^[A-Za-z_-]+$') { continue }
        $findings.Add([pscustomobject]@{path=$relative; reason='Potential credential assignment; value suppressed'})
        break
    }
}
[pscustomobject]@{
    measuredAtLocal = [TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTimeOffset]::UtcNow, 'America/Chicago').ToString('s')
    scannedFiles = $inventory.Count
    bytes = ($inventory | Measure-Object bytes -Sum).Sum
    findings = @($findings)
    inventory = @($inventory)
    limitations = 'Text screening only. Binary/archive payloads are not classified by this scanner; no claim of exhaustive secret detection.'
}
