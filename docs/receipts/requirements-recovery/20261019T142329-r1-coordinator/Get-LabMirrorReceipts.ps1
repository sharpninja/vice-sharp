#Requires -Version 7.4
<#
.SYNOPSIS
    Read-only: lists the R0 mirror receipts that vice-sharp-b1 wrote on LAB-OMARCHY (~/codex-dispatch/r0-mirror-20261019)
    with byte length and SHA256, over Posh-SSH. The password comes from C:\Users\kingd\.creds\lab-omarchy.yaml as a
    PSCredential and is never printed or put in a command argument. Optional -Cat names receipt files (relative) whose
    text is returned. Writes nothing on either machine.
#>
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingPlainTextForPassword', 'CredPath', Justification = 'CredPath is a file path, not a password.')]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '', Justification = 'The saved credential file stores the password as text; it goes straight into a PSCredential.')]
[CmdletBinding()]
param(
    [string]$RemoteDir = '~/codex-dispatch/r0-mirror-20261019',
    [string[]]$Cat = @(),
    # Relative receipt paths to download (SFTP) into FetchTo, a new empty directory.
    [string[]]$Fetch = @(),
    [string]$FetchTo,
    # Optional read-only PowerShell command run on LAB-OMARCHY after the listing (caller's responsibility to keep read-only).
    [string]$Run,
    [string]$CredPath = (Join-Path $env:USERPROFILE '.creds\lab-omarchy.yaml')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module Posh-SSH
Import-Module powershell-yaml
$y = Get-Content -LiteralPath $CredPath -Raw | ConvertFrom-Yaml
$user = @($y['username'], $y['user'] | Where-Object { $_ })[0]
$pass = @($y['password'] | Where-Object { $_ })[0]
if (-not $user -or -not $pass) { throw "credential file lacks username or password keys (keys: $(@($y.Keys) -join ', '))" }
$cred = [pscredential]::new($user, (ConvertTo-SecureString -String $pass -AsPlainText -Force))
$pass = $null
$ip = @(Resolve-DnsName LAB-OMARCHY.lab -Server 10.42.0.1 -Type A | Where-Object IPAddress | Select-Object -ExpandProperty IPAddress)[0]
$s = New-SSHSession -ComputerName $ip -Port 22 -Credential $cred -AcceptKey:$false -ConnectionTimeout 15
try {
    $list = "Get-ChildItem -LiteralPath (Resolve-Path '$RemoteDir') -Recurse -File | ForEach-Object { '{0} bytes={1} sha256={2} mtime={3:s}' -f `$_.FullName, `$_.Length, (Get-FileHash `$_.FullName).Hash, `$_.LastWriteTime }"
    $r = Invoke-SSHCommand -SSHSession $s -Command $list -TimeOut 60
    "host=$ip exit=$($r.ExitStatus)"
    $r.Output
    foreach ($f in $Cat) {
        $c = Invoke-SSHCommand -SSHSession $s -Command ("Get-Content -Raw -LiteralPath (Join-Path (Resolve-Path '$RemoteDir') '$f')") -TimeOut 60
        "===== $f (exit $($c.ExitStatus))"
        $c.Output
    }
    if ($Run) {
        $x = Invoke-SSHCommand -SSHSession $s -Command $Run -TimeOut 120
        "===== run (exit $($x.ExitStatus))"
        $x.Output
    }
    if ($Fetch.Count) {
        $home_ = (Invoke-SSHCommand -SSHSession $s -Command "(Resolve-Path '$RemoteDir').Path" -TimeOut 30).Output[0].Trim()
        $f = New-SFTPSession -ComputerName $ip -Port 22 -Credential $cred -AcceptKey:$false -ConnectionTimeout 15
        try {
            foreach ($rel in $Fetch) {
                $dest = Join-Path $FetchTo (Split-Path $rel -Parent)
                New-Item -ItemType Directory -Force -Path $dest | Out-Null
                Get-SFTPItem -SFTPSession $f -Path "$home_/$rel" -Destination $dest -Force
                $local = Join-Path $dest (Split-Path $rel -Leaf)
                'fetched {0} bytes={1} sha256={2}' -f $rel, (Get-Item -LiteralPath $local).Length, (Get-FileHash -LiteralPath $local).Hash
            }
        }
        finally { Remove-SFTPSession -SFTPSession $f | Out-Null }
    }
}
finally { Remove-SSHSession -SSHSession $s | Out-Null }
