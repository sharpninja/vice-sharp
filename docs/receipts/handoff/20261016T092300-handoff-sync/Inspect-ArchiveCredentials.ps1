[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkspacePath,
    [Parameter(Mandatory)][string[]]$SourcePaths,
    [Parameter(Mandatory)][string[]]$CandidatePaths
)
$ErrorActionPreference = 'Stop'
$secrets = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$pattern = '(?im)(?:api[_-]?key|X-Api-Key)[\\"''\s:=]+(?<value>[A-Za-z0-9_+/=.!@#$%&*?-]{30,})'
foreach ($relative in $SourcePaths) {
    $body = [IO.File]::ReadAllText((Join-Path $WorkspacePath $relative))
    foreach ($match in [regex]::Matches($body, $pattern)) {
        $value = $match.Groups['value'].Value
        if ($value.Length -eq 43 -and $value -notmatch '^[A-Za-z_-]+$') { [void]$secrets.Add($value) }
    }
}
if ($secrets.Count -eq 0) { throw 'No old workspace credential signatures found; cannot assert archive screening.' }
function Test-SecretStream {
    param([IO.Stream]$Stream)
    $reader = [IO.StreamReader]::new($Stream, [Text.Encoding]::UTF8, $true, 65536, $true)
    try {
        $buffer = [char[]]::new(65536)
        $tail = ''
        while (($count = $reader.Read($buffer, 0, $buffer.Length)) -gt 0) {
            $chunk = $tail + [string]::new($buffer, 0, $count)
            foreach ($secret in $secrets) {
                if ($chunk.Contains($secret, [StringComparison]::Ordinal)) { return $true }
            }
            $tail = $chunk.Substring([Math]::Max(0, $chunk.Length - 128))
        }
        return $false
    } finally { $reader.Dispose() }
}
foreach ($relative in ($CandidatePaths | Sort-Object -Unique)) {
    $path = Join-Path $WorkspacePath $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $hit = $false
    $extension = [IO.Path]::GetExtension($path)
    if ($extension -eq '.zip') {
        $archive = [IO.Compression.ZipFile]::OpenRead($path)
        try {
            foreach ($entry in $archive.Entries) {
                $stream = $entry.Open()
                try { if (Test-SecretStream $stream) { $hit = $true; break } } finally { $stream.Dispose() }
            }
        } finally { $archive.Dispose() }
    } else {
        $file = [IO.File]::OpenRead($path)
        try {
            if ($extension -eq '.gz') {
                $gzip = [IO.Compression.GZipStream]::new($file, [IO.Compression.CompressionMode]::Decompress, $true)
                try { $hit = Test-SecretStream $gzip } finally { $gzip.Dispose() }
            } else { $hit = Test-SecretStream $file }
        } finally { $file.Dispose() }
    }
    if ($hit) { [pscustomobject]@{path=$relative; reason='Exact historical workspace credential; value suppressed'; sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash} }
}
