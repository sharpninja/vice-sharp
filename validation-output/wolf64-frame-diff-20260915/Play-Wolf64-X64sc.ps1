$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\vice-sharp\validation-output\wolf64-frame-diff-20260915'
$x64sc = 'C:\Users\kingd\AppData\Local\Microsoft\WinGet\Packages\VICE-Team.VICE.GTK3_Microsoft.Winget.Source_8wekyb3d8bbwe\GTK3VICE-3.10-win64\bin\x64sc.exe'
$disk = Join-Path $outDir 'wolf64.d64'
$ram = Join-Path $outDir 'x64sc-play-ram.bin'
$ram64 = Join-Path $outDir 'x64sc-play-ram-64k.bin'
$trace = Join-Path $outDir 'x64sc-play-drive.log'
$stdoutLog = Join-Path $outDir 'x64sc-play-stdout.log'
$stderrLog = Join-Path $outDir 'x64sc-play-stderr.log'
$vsRam = Join-Path $outDir 'vicesharp-ram.bin'
$compareOut = Join-Path $outDir 'RAM-COMPARE-play.txt'

Add-Type @"
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class WolfKeys {
  public const int WM_KEYDOWN = 0x0100;
  public const int WM_KEYUP = 0x0101;
  public const int WM_CHAR = 0x0102;
  public const uint KEYEVENTF_KEYUP = 0x0002;
  public const uint KEYEVENTF_SCANCODE = 0x0008;
  public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc lp, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr h, EnumProc lp, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lp, int n);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint procId);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool SetActiveWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool f);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern uint SendInput(uint n, INPUT[] i, int size);
  [DllImport("user32.dll")] public static extern int GetSystemMetrics(int n);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct INPUT { public uint type; public INPUTUNION u; }
  [StructLayout(LayoutKind.Explicit)] public struct INPUTUNION {
    [FieldOffset(0)] public MOUSEINPUT mi;
    [FieldOffset(0)] public KEYBDINPUT ki;
  }
  [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT {
    public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo;
  }
  [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT {
    public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo;
  }
  public static IntPtr FindVice(uint procId) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => {
      uint p; GetWindowThreadProcessId(h, out p);
      if (p != procId) return true;
      var sb = new StringBuilder(256);
      GetWindowText(h, sb, 256);
      if (sb.ToString().IndexOf("VICE", StringComparison.OrdinalIgnoreCase) >= 0) { found = h; return false; }
      return true;
    }, IntPtr.Zero);
    return found;
  }
  public static List<IntPtr> WindowsForProc(uint procId) {
    var list = new List<IntPtr>();
    EnumWindows((h, l) => {
      uint p; GetWindowThreadProcessId(h, out p);
      if (p == procId && IsWindowVisible(h)) {
        list.Add(h);
        EnumChildWindows(h, (c, l2) => { list.Add(c); return true; }, IntPtr.Zero);
      }
      return true;
    }, IntPtr.Zero);
    return list;
  }
  static IntPtr KeyLParam(ushort scan, bool up) {
    int lp = 1 | (scan << 16);
    if (up) lp |= unchecked((int)0xC0000000);
    return (IntPtr)lp;
  }
  public static void PostReturnDown(IntPtr h) {
    PostMessage(h, WM_KEYDOWN, (IntPtr)0x0D, KeyLParam(0x1C, false));
  }
  public static void PostReturnUp(IntPtr h) {
    PostMessage(h, WM_CHAR, (IntPtr)0x0D, KeyLParam(0x1C, false));
    PostMessage(h, WM_KEYUP, (IntPtr)0x0D, KeyLParam(0x1C, true));
  }
  public static void SendReturnScan(bool up) {
    var inp = new INPUT();
    inp.type = 1;
    inp.u.ki.wScan = 0x1C;
    uint f = KEYEVENTF_SCANCODE;
    if (up) f |= KEYEVENTF_KEYUP;
    inp.u.ki.dwFlags = f;
    SendInput(1, new INPUT[] { inp }, Marshal.SizeOf(typeof(INPUT)));
  }
  public static void ClickAbs(int x, int y) {
    int sx = GetSystemMetrics(0); if (sx < 1) sx = 1;
    int sy = GetSystemMetrics(1); if (sy < 1) sy = 1;
    var inp = new INPUT();
    inp.type = 0;
    inp.u.mi.dx = (x * 65535) / sx;
    inp.u.mi.dy = (y * 65535) / sy;
    inp.u.mi.dwFlags = 0x8000 | 0x0001;
    SendInput(1, new INPUT[] { inp }, Marshal.SizeOf(typeof(INPUT)));
    inp.u.mi.dwFlags = 0x8000 | 0x0002;
    SendInput(1, new INPUT[] { inp }, Marshal.SizeOf(typeof(INPUT)));
    inp.u.mi.dwFlags = 0x8000 | 0x0004;
    SendInput(1, new INPUT[] { inp }, Marshal.SizeOf(typeof(INPUT)));
  }
  public static void ForceForeground(IntPtr h) {
    uint dummy;
    IntPtr fg = GetForegroundWindow();
    uint fgTid = GetWindowThreadProcessId(fg, out dummy);
    uint cur = GetCurrentThreadId();
    uint viceTid = GetWindowThreadProcessId(h, out dummy);
    AttachThreadInput(cur, fgTid, true);
    AttachThreadInput(cur, viceTid, true);
    ShowWindow(h, 9);
    BringWindowToTop(h);
    SetForegroundWindow(h);
    SetActiveWindow(h);
    AttachThreadInput(cur, viceTid, false);
    AttachThreadInput(cur, fgTid, false);
  }
}
"@

function Write-Trace([string]$msg) {
    $line = '{0} {1}' -f ([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss.fff zzz')), $msg
    Add-Content -LiteralPath $trace -Value $line
    Write-Output $line
}

function Invoke-ViceMonitor {
    param([string[]]$Commands, [int]$ReadMs = 400, [switch]$NoResume)
    $client = New-Object System.Net.Sockets.TcpClient
    $client.ReceiveTimeout = 8000
    $client.SendTimeout = 8000
    $client.Connect([Net.IPAddress]::Loopback, 6510)
    $stream = $client.GetStream()
    $buf = New-Object byte[] 32768
    Start-Sleep -Milliseconds 80
    while ($stream.DataAvailable) { [void]$stream.Read($buf, 0, $buf.Length) }
    $resp = ''
    foreach ($cmd in $Commands) {
        $bytes = [Text.Encoding]::ASCII.GetBytes($cmd + "`n")
        $stream.Write($bytes, 0, $bytes.Length)
        $stream.Flush()
        Start-Sleep -Milliseconds $ReadMs
        $deadline = [DateTime]::UtcNow.AddMilliseconds(700)
        while ([DateTime]::UtcNow -lt $deadline) {
            if ($stream.DataAvailable) {
                $n = $stream.Read($buf, 0, $buf.Length)
                $resp += [Text.Encoding]::ASCII.GetString($buf, 0, $n)
            } else { Start-Sleep -Milliseconds 25 }
        }
    }
    if (-not $NoResume) {
        $xb = [Text.Encoding]::ASCII.GetBytes("x`n")
        $stream.Write($xb, 0, $xb.Length)
        $stream.Flush()
        Start-Sleep -Milliseconds 700
    }
    $client.Close()
    $resp
}

function Get-HexAt([string]$text, [string]$addr) {
    $m = [regex]::Match($text, '(?i)C:' + [regex]::Escape($addr) + '\s+([0-9A-F]{2})')
    if ($m.Success) { return $m.Groups[1].Value.ToUpperInvariant() }
    return $null
}

function Get-VicState([uint32]$processId) {
    $peek = Invoke-ViceMonitor -Commands @('m 0001', 'm d011', 'm d015', 'm d018', 'm dd00') -ReadMs 180
    [pscustomobject]@{
        Cpu01 = Get-HexAt $peek '0001'
        D011  = Get-HexAt $peek 'd011'
        D015  = Get-HexAt $peek 'd015'
        D018  = Get-HexAt $peek 'd018'
        Dd00  = Get-HexAt $peek 'dd00'
        Raw   = $peek
        ProcessId = $processId
    }
}

function Test-Wolf64Menu($st) {
    if ($st.D015 -eq '20') { return $true }
    if ($st.D018 -eq '09') { return $true }
    if ($st.D015 -eq '7F') { return $true }
    if ($st.D011 -eq '3B') { return $true }
    return $false
}

function Send-ReturnHold([uint32]$processId) {
    $hwnd = [WolfKeys]::FindVice($processId)
    if ($hwnd -eq [IntPtr]::Zero) { return 'hwnd=0' }
    [WolfKeys]::ShowWindow($hwnd, 9) | Out-Null
    [WolfKeys]::ForceForeground($hwnd)
    $r = New-Object WolfKeys+RECT
    [void][WolfKeys]::GetWindowRect($hwnd, [ref]$r)
    $cx = [int](($r.L + $r.R) / 2)
    $cy = [int](($r.T + $r.B) / 2)
    [WolfKeys]::ClickAbs($cx, $cy)
    Start-Sleep -Milliseconds 80
    $wins = [WolfKeys]::WindowsForProc($processId)
    foreach ($h in $wins) { [WolfKeys]::PostReturnDown($h) }
    [WolfKeys]::SendReturnScan($false)
    Start-Sleep -Milliseconds 400
    foreach ($h in $wins) { [WolfKeys]::PostReturnUp($h) }
    [WolfKeys]::SendReturnScan($true)
    return "hwnd=$hwnd kids=$($wins.Count) click=$cx,$cy holdMs=400"
}

function Compare-PlayRam([string]$vicePath, [string]$vsPath, [string]$reportPath) {
    $v = [IO.File]::ReadAllBytes($vicePath)
    $s = [IO.File]::ReadAllBytes($vsPath)
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("vsLen=$($s.Length) viceLen=$($v.Length)")
    $lines.Add(('vs   01={0:X2} d015={1:X2} d018={2:X2}' -f $s[1], $s[0xD015], $s[0xD018]))
    $lines.Add(('vice 01={0:X2} d015={1:X2} d018={2:X2}' -f $v[1], $v[0xD015], $v[0xD018]))
    function Count-Diff([byte[]]$a, [byte[]]$b, [int]$start, [int]$len) {
        $n = 0
        for ($i = 0; $i -lt $len; $i++) { if ($a[$start + $i] -ne $b[$start + $i]) { $n++ } }
        return $n
    }
    $bm = Count-Diff $s $v 0xA000 0x1F40
    $cm = Count-Diff $s $v 0xD800 0x03E8
    $all = Count-Diff $s $v 0 0x10000
    $lines.Add("BITMAP A000-BF3F mismatch $bm/8000")
    $lines.Add("COLOR  D800-DBE7 mismatch $cm/1000")
    $lines.Add("ALL    0000-FFFF mismatch $all/65536")
    $lines | Set-Content -LiteralPath $reportPath -Encoding utf8
    $lines
}

function Save-PlayRam {
    $saveCmd = 'save "' + $ram.Replace('\', '/') + '" 0 0000 ffff'
    $saved = Invoke-ViceMonitor -Commands @($saveCmd) -ReadMs 2500 -NoResume
    Write-Trace "SAVE $saved"
    if (-not (Test-Path -LiteralPath $ram)) { throw 'save did not create ram file' }
    $bytes = [IO.File]::ReadAllBytes($ram)
    Write-Trace "ram-len=$($bytes.Length)"
    $slice = $null
    if ($bytes.Length -ge 65538) {
        $slice = New-Object byte[] 65536
        [Array]::Copy($bytes, 2, $slice, 0, 65536)
    } elseif ($bytes.Length -eq 65536) {
        $slice = $bytes
    } else { throw "unexpected ram length $($bytes.Length)" }
    [IO.File]::WriteAllBytes($ram64, $slice)
    Write-Trace ('stripped 01={0:X2} d015={1:X2} d018={2:X2} dd00={3:X2}' -f $slice[1], $slice[0xD015], $slice[0xD018], $slice[0xDD00])
    if (Test-Path -LiteralPath $vsRam) {
        Compare-PlayRam $ram64 $vsRam $compareOut | ForEach-Object { Write-Trace $_ }
    }
}

if (-not (Test-Path -LiteralPath $disk)) { throw "missing $disk" }
Get-Process x64sc -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400
foreach ($f in @($trace, $ram, $ram64, $stdoutLog, $stderrLog, $compareOut)) {
    if (Test-Path -LiteralPath $f) { Remove-Item -LiteralPath $f -Force }
}

$proc = Start-Process -FilePath $x64sc -ArgumentList @(
    '-default', '+sound', '+autostart-delay-random', '-autostart-warp',
    '-remotemonitor', '-remotemonitoraddress', 'ip4://127.0.0.1:6510',
    '-windowxpos', '80', '-windowypos', '80', '-windowwidth', '768', '-windowheight', '544',
    '-autostart', $disk
) -WorkingDirectory $outDir -PassThru -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog

Write-Trace "started processId=$($proc.Id) TDE default ON; first file via autostart traps, rest via 1541"
Write-Trace "poll stdout for AUTOSTART Done; do not open monitor during first load"
$autoDone = $false
$autoDeadline = [DateTime]::UtcNow.AddSeconds(40)
while ([DateTime]::UtcNow -lt $autoDeadline -and -not $proc.HasExited) {
    if (Test-Path -LiteralPath $stdoutLog) {
        $txt = Get-Content -LiteralPath $stdoutLog -Raw -ErrorAction SilentlyContinue
        if ($txt -and $txt.Contains('AUTOSTART:  Done.')) { $autoDone = $true; break }
    }
    Start-Sleep -Milliseconds 400
}
Write-Trace "autostart-done=$autoDone exited=$($proc.HasExited)"
if (-not $autoDone) { throw 'VICE autostart did not finish' }

Write-Trace "re-enable warp for the game disk loader, then leave it alone"
try {
    Invoke-ViceMonitor -Commands @('resourceset "WarpMode" "1"') -ReadMs 150 | Out-Null
    Write-Trace "WarpMode 1"
} catch {
    Write-Trace "warp-on-fail $_"
}

Write-Trace "free-run 55s for TDE second-stage load (no monitor, no keys)"
Start-Sleep -Seconds 55

$st = $null
$menu = $false
$deadline = [DateTime]::UtcNow.AddSeconds(20)
$waitN = 0
while ([DateTime]::UtcNow -lt $deadline -and -not $proc.HasExited) {
    $waitN++
    $st = Get-VicState ([uint32]$proc.Id)
    Write-Trace "wait $waitN 01=$($st.Cpu01) d011=$($st.D011) d015=$($st.D015) d018=$($st.D018) dd00=$($st.Dd00)"
    if ($st.D015 -eq '20') {
        Write-Trace "already in play before any Return"
        Save-PlayRam
        Write-Trace "DONE hit=True"
        return
    }
    if (Test-Wolf64Menu $st) {
        $menu = $true
        Write-Trace "menu/title gate passed (D018=$($st.D018) D015=$($st.D015) D011=$($st.D011))"
        break
    }
    Start-Sleep -Seconds 4
}

if (-not $menu) {
    Write-Trace "FAIL never left BASIC/load. Not sending Returns. last 01=$($st.Cpu01) d018=$($st.D018) d015=$($st.D015)"
    throw "Wolf64 menu not observed; refusing to send keys at BASIC"
}

try {
    Invoke-ViceMonitor -Commands @('resourceset "WarpMode" "0"') -ReadMs 150 | Out-Null
    Write-Trace "WarpMode 0"
} catch {
    Write-Trace "warp-off-fail $_"
}

for ($ret = 1; $ret -le 3; $ret++) {
    if ($proc.HasExited) { throw 'x64sc exited during menu' }
    $inj = Send-ReturnHold ([uint32]$proc.Id)
    Write-Trace "Return $ret/3 $inj"
    Start-Sleep -Seconds 6
    $st = Get-VicState ([uint32]$proc.Id)
    Write-Trace "after Return $ret 01=$($st.Cpu01) d011=$($st.D011) d015=$($st.D015) d018=$($st.D018) dd00=$($st.Dd00)"
    if ($st.D015 -eq '20') {
        Save-PlayRam
        Write-Trace "DONE hit=True after Return $ret"
        return
    }
}

$playDeadline = [DateTime]::UtcNow.AddSeconds(15)
$p = 0
while ([DateTime]::UtcNow -lt $playDeadline -and -not $proc.HasExited) {
    $p++
    Start-Sleep -Seconds 3
    $st = Get-VicState ([uint32]$proc.Id)
    Write-Trace "playwait $p 01=$($st.Cpu01) d011=$($st.D011) d015=$($st.D015) d018=$($st.D018) dd00=$($st.Dd00)"
    if ($st.D015 -eq '20') {
        Save-PlayRam
        Write-Trace "DONE hit=True after playwait"
        return
    }
}

Write-Trace "FAIL after three Returns still d015=$($st.D015) d018=$($st.D018)"
throw "did not reach D015=20"
