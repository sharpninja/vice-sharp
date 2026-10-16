$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\vice-sharp\validation-output\wolf64-frame-diff-20260915'
$x = 'C:\Users\kingd\AppData\Local\Microsoft\WinGet\Packages\VICE-Team.VICE.GTK3_Microsoft.Winget.Source_8wekyb3d8bbwe\GTK3VICE-3.10-win64\bin\x64sc.exe'
$disk = Join-Path $out 'wolf64.d64'
$ram = Join-Path $out 'x64sc-ram.bin'
$log = Join-Path $out 'x64sc-input.log'
$trace = Join-Path $out 'x64sc-input-mon.txt'

Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class ViceWin {
  public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc lp, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lp, int n);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr extra);
  public static IntPtr FindVice() {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => {
      var sb = new StringBuilder(256);
      GetWindowText(h, sb, 256);
      var t = sb.ToString();
      if (t.IndexOf("VICE", StringComparison.OrdinalIgnoreCase) >= 0 &&
          t.IndexOf("C64", StringComparison.OrdinalIgnoreCase) >= 0) { found = h; return false; }
      return true;
    }, IntPtr.Zero);
    return found;
  }
  public static void TapReturn() {
    keybd_event(0x0D, 0x1C, 0, UIntPtr.Zero);
    System.Threading.Thread.Sleep(50);
    keybd_event(0x0D, 0x1C, 2, UIntPtr.Zero);
  }
}
"@

if (Test-Path $ram) { Remove-Item $ram -Force }
$p = Start-Process -FilePath $x -ArgumentList @(
    '-default', '+sound', '+autostart-delay-random', '-autostart-warp',
    '-remotemonitor', '-remotemonitoraddress', 'ip4://127.0.0.1:6510',
    '-windowxpos', '80', '-windowypos', '80', '-windowwidth', '768', '-windowheight', '544',
    '-autostart', $disk
) -WorkingDirectory $out -PassThru -RedirectStandardOutput $log

function Invoke-ViceMonitor([string]$Command, [int]$ReadMs = 800) {
    $c = New-Object System.Net.Sockets.TcpClient
    $c.ReceiveTimeout = 10000
    $c.Connect('127.0.0.1', 6510)
    $s = $c.GetStream()
    $buf = New-Object byte[] 16384
    Start-Sleep -Milliseconds 120
    while ($s.DataAvailable) { [void]$s.Read($buf, 0, $buf.Length) }
    $bytes = [Text.Encoding]::ASCII.GetBytes($Command + "`n")
    $s.Write($bytes, 0, $bytes.Length)
    $s.Flush()
    Start-Sleep -Milliseconds $ReadMs
    $resp = ''
    $deadline = [DateTime]::UtcNow.AddMilliseconds(1400)
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($s.DataAvailable) {
            $n = $s.Read($buf, 0, $buf.Length)
            $resp += [Text.Encoding]::ASCII.GetString($buf, 0, $n)
        } else { Start-Sleep -Milliseconds 40 }
    }
    $c.Close()
    $resp
}

$lines = New-Object System.Collections.Generic.List[string]
try {
    Start-Sleep -Seconds 22
    $hwnd = [ViceWin]::FindVice()
    $lines.Add("hwnd=$hwnd pid=$($p.Id) main=$($p.MainWindowHandle)")
    if ($hwnd -ne [IntPtr]::Zero) {
        [ViceWin]::ShowWindow($hwnd, 9) | Out-Null
        [ViceWin]::SetForegroundWindow($hwnd) | Out-Null
    }
    Start-Sleep -Milliseconds 500
    1..5 | ForEach-Object {
        if ($hwnd -ne [IntPtr]::Zero) { [ViceWin]::SetForegroundWindow($hwnd) | Out-Null }
        [ViceWin]::TapReturn()
        $lines.Add("tap RETURN $_")
        Start-Sleep -Seconds 5
    }
    Start-Sleep -Seconds 12
    $lines.Add((Invoke-ViceMonitor 'm d015' 700))
    $lines.Add((Invoke-ViceMonitor 'm d011' 700))
    $lines.Add((Invoke-ViceMonitor 'm d018' 700))
    $lines.Add((Invoke-ViceMonitor 'm dd00' 700))
    $save = 'save "' + $ram.Replace('\', '/') + '" 0 0000 ffff'
    $lines.Add((Invoke-ViceMonitor $save 2500))
    $lines.Add((Invoke-ViceMonitor 'quit' 400))
}
catch { $lines.Add("ERROR $_") }
finally {
    $lines | Out-File $trace -Encoding utf8
    if ($p -and -not $p.HasExited) {
        Start-Sleep -Seconds 1
        if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force }
    }
}
Write-Output "RAM=$(Test-Path $ram) LEN=$((Get-Item $ram -EA SilentlyContinue).Length)"
Get-Content $trace
