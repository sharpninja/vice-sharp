$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\vice-sharp\validation-output\wolf64-frame-diff-20260915'
$x = 'C:\Users\kingd\AppData\Local\Microsoft\WinGet\Packages\VICE-Team.VICE.GTK3_Microsoft.Winget.Source_8wekyb3d8bbwe\GTK3VICE-3.10-win64\bin\x64sc.exe'
$disk = Join-Path $out 'wolf64.d64'
$ram = Join-Path $out 'x64sc-ram.bin'
$log = Join-Path $out 'x64sc-play.log'
$trace = Join-Path $out 'x64sc-play-mon.txt'

if (Test-Path $ram) { Remove-Item $ram -Force }
$p = Start-Process -FilePath $x -ArgumentList @(
    '-default', '+sound', '+autostart-delay-random', '-autostart-warp',
    '-remotemonitor', '-remotemonitoraddress', 'ip4://127.0.0.1:6510',
    '-windowxpos', '80', '-windowypos', '80', '-windowwidth', '768', '-windowheight', '544',
    '-autostart', $disk
) -WorkingDirectory $out -PassThru -RedirectStandardOutput $log

function Invoke-ViceMonitor {
    param([string]$Command, [int]$ReadMs = 600)
    $c = New-Object System.Net.Sockets.TcpClient
    $c.ReceiveTimeout = 8000
    $c.SendTimeout = 8000
    $c.Connect('127.0.0.1', 6510)
    $s = $c.GetStream()
    $buf = New-Object byte[] 16384
    Start-Sleep -Milliseconds 200
    $pre = ''
    while ($s.DataAvailable) {
        $n = $s.Read($buf, 0, $buf.Length)
        $pre += [Text.Encoding]::ASCII.GetString($buf, 0, $n)
    }
    $bytes = [Text.Encoding]::ASCII.GetBytes($Command + "`n")
    $s.Write($bytes, 0, $bytes.Length)
    $s.Flush()
    Start-Sleep -Milliseconds $ReadMs
    $resp = $pre
    $deadline = [DateTime]::UtcNow.AddMilliseconds(1500)
    do {
        if ($s.DataAvailable) {
            $n = $s.Read($buf, 0, $buf.Length)
            $resp += [Text.Encoding]::ASCII.GetString($buf, 0, $n)
        } else {
            Start-Sleep -Milliseconds 50
        }
    } while ([DateTime]::UtcNow -lt $deadline)
    $c.Close()
    $resp
}

function Get-D015 {
    $r = Invoke-ViceMonitor 'm d015'
    if ($r -match 'd015\s+([0-9a-fA-F]{2})') { return $matches[1].ToUpperInvariant() }
    if ($r -match '>\w:d015\s+([0-9a-fA-F]{2})') { return $matches[1].ToUpperInvariant() }
    return $r
}

$logLines = New-Object System.Collections.Generic.List[string]
try {
    Start-Sleep -Seconds 18
    $logLines.Add('--- after wait ---')
    $logLines.Add((Invoke-ViceMonitor 'x' 200))
    Start-Sleep -Seconds 2

    foreach ($step in 1..3) {
        $logLines.Add("--- return $step ---")
        $logLines.Add((Invoke-ViceMonitor 'keybuf "\n"' 400))
        $logLines.Add((Invoke-ViceMonitor 'x' 200))
        Start-Sleep -Seconds 4
        $d015 = Get-D015
        $logLines.Add("D015=$d015")
        $logLines.Add((Invoke-ViceMonitor 'x' 200))
        if ($d015 -eq '20') { break }
        Start-Sleep -Seconds 2
    }

    Start-Sleep -Seconds 8
    $d015 = Get-D015
    $logLines.Add("pre-save D015=$d015")
    $logLines.Add((Invoke-ViceMonitor 'm d011' 400))
    $logLines.Add((Invoke-ViceMonitor 'm d018' 400))
    $logLines.Add((Invoke-ViceMonitor 'm dd00' 400))
    $save = 'save "' + ($ram.Replace('\', '/')) + '" 0 0000 ffff'
    $logLines.Add((Invoke-ViceMonitor $save 2500))
    $logLines.Add((Invoke-ViceMonitor 'quit' 400))
}
catch {
    $logLines.Add("ERROR $_")
}
finally {
    $logLines | Out-File $trace -Encoding utf8
    if ($p -and -not $p.HasExited) {
        Start-Sleep -Seconds 1
        if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force }
    }
}

Write-Output "RAM_EXISTS=$(Test-Path $ram) LEN=$((Get-Item $ram -EA SilentlyContinue).Length)"
Write-Output '--- mon ---'
Get-Content $trace
