$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\vice-sharp\validation-output\wolf64-frame-diff-20260915'
$ram = Join-Path $out 'x64sc-play-ram.bin'
$ram64 = Join-Path $out 'x64sc-play-ram-64k.bin'
$trace = Join-Path $out 'x64sc-attach-d015.log'
$proc = Get-Process x64sc -ErrorAction Stop | Select-Object -First 1
$procId = [uint32]$proc.Id

Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class ViceAttach {
  public const int WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101, WM_CHAR = 0x0102;
  public const uint KEYEVENTF_KEYUP = 2, KEYEVENTF_SCANCODE = 8, KEYEVENTF_EXTENDEDKEY = 1, INPUT_KEYBOARD = 1, INPUT_MOUSE = 0;
  public const uint MOUSEEVENTF_ABSOLUTE = 0x8000, MOUSEEVENTF_MOVE = 1, MOUSEEVENTF_LEFTDOWN = 2, MOUSEEVENTF_LEFTUP = 4;
  public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc lp, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr h, EnumProc lp, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder lp, int n);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
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
  [StructLayout(LayoutKind.Sequential)] public struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public UIntPtr dwExtraInfo; }
  [StructLayout(LayoutKind.Sequential)] public struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public UIntPtr dwExtraInfo; }
  public static IntPtr FindVice(uint targetPid) {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => {
      uint p; GetWindowThreadProcessId(h, out p);
      if (p != targetPid) return true;
      var sb = new StringBuilder(256); GetWindowText(h, sb, 256);
      if (sb.ToString().IndexOf("VICE", StringComparison.OrdinalIgnoreCase) >= 0) { found = h; return false; }
      return true;
    }, IntPtr.Zero);
    return found;
  }
  public static System.Collections.Generic.List<IntPtr> WindowsFor(uint targetPid) {
    var list = new System.Collections.Generic.List<IntPtr>();
    EnumWindows((h, l) => {
      uint p; GetWindowThreadProcessId(h, out p);
      if (p == targetPid && IsWindowVisible(h)) {
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
  public static void PostKey(IntPtr h, byte vk, ushort scan) {
    PostMessage(h, WM_KEYDOWN, (IntPtr)vk, KeyLParam(scan, false));
    PostMessage(h, WM_CHAR, (IntPtr)vk, KeyLParam(scan, false));
    PostMessage(h, WM_KEYUP, (IntPtr)vk, KeyLParam(scan, true));
  }
  public static void SendScan(ushort scan, bool up, bool ext) {
    var inp = new INPUT();
    inp.type = INPUT_KEYBOARD;
    inp.u.ki.wScan = scan;
    uint f = KEYEVENTF_SCANCODE;
    if (up) f |= KEYEVENTF_KEYUP;
    if (ext) f |= KEYEVENTF_EXTENDEDKEY;
    inp.u.ki.dwFlags = f;
    SendInput(1, new INPUT[] { inp }, Marshal.SizeOf(typeof(INPUT)));
  }
  public static void ClickAbs(int x, int y) {
    int sx = Math.Max(1, GetSystemMetrics(0));
    int sy = Math.Max(1, GetSystemMetrics(1));
    var inp = new INPUT();
    inp.type = INPUT_MOUSE;
    inp.u.mi.dx = (x * 65535) / sx;
    inp.u.mi.dy = (y * 65535) / sy;
    inp.u.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;
    SendInput(1, new INPUT[] { inp }, Marshal.SizeOf(typeof(INPUT)));
    inp.u.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_LEFTDOWN;
    SendInput(1, new INPUT[] { inp }, Marshal.SizeOf(typeof(INPUT)));
    inp.u.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_LEFTUP;
    SendInput(1, new INPUT[] { inp }, Marshal.SizeOf(typeof(INPUT)));
  }
  public static void ForceForeground(IntPtr h) {
    uint unused;
    IntPtr fg = GetForegroundWindow();
    uint fgTid = GetWindowThreadProcessId(fg, out unused);
    uint cur = GetCurrentThreadId();
    uint viceTid = GetWindowThreadProcessId(h, out unused);
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
    Add-Content -Path $trace -Value $line
    Write-Output $line
}

function Invoke-ViceMonitor {
    param([string[]]$Commands, [int]$ReadMs = 400, [switch]$NoResume)
    $client = New-Object System.Net.Sockets.TcpClient
    $client.ReceiveTimeout = 8000
    $client.Connect('127.0.0.1', 6510)
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
        $until = [DateTime]::UtcNow.AddMilliseconds(700)
        while ([DateTime]::UtcNow -lt $until) {
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
        Start-Sleep -Milliseconds 80
    }
    $client.Close()
    $resp
}

function Get-HexByte([string]$text, [string]$addr) {
    $m = [regex]::Match($text, '(?i)>C:' + [regex]::Escape($addr) + '\s+([0-9A-Fa-f]{2})')
    if ($m.Success) { return $m.Groups[1].Value.ToUpperInvariant() }
    return $null
}

function Send-CiaKeys([uint32]$targetPid) {
    $hwnd = [ViceAttach]::FindVice($targetPid)
    if ($hwnd -eq [IntPtr]::Zero) { return 'hwnd=0' }
    [ViceAttach]::ShowWindow($hwnd, 6) | Out-Null
    Start-Sleep -Milliseconds 60
    [ViceAttach]::ShowWindow($hwnd, 9) | Out-Null
    [ViceAttach]::ForceForeground($hwnd)
    $r = New-Object ViceAttach+RECT
    [void][ViceAttach]::GetWindowRect($hwnd, [ref]$r)
    $cx = [int](($r.L + $r.R) / 2)
    $cy = [int](($r.T + $r.B) / 2)
    [ViceAttach]::ClickAbs($cx, $cy)
    Start-Sleep -Milliseconds 60
    $wins = [ViceAttach]::WindowsFor($targetPid)
    foreach ($h in $wins) {
        [ViceAttach]::PostKey($h, 0x0D, 0x1C)
        [ViceAttach]::PostKey($h, 0x20, 0x39)
        [ViceAttach]::PostKey($h, 0x60, 0x52)
    }
    foreach ($scan in @(0x1C, 0x39, 0x52)) {
        [ViceAttach]::SendScan($scan, $false, $false)
        Start-Sleep -Milliseconds 60
        [ViceAttach]::SendScan($scan, $true, $false)
        Start-Sleep -Milliseconds 30
    }
    return "hwnd=$hwnd kids=$($wins.Count) click=$cx,$cy"
}

if (Test-Path $trace) { Remove-Item $trace -Force }
Write-Trace "attach procId=$procId title=$($proc.MainWindowTitle)"

# Resume if still in monitor, then let autostart finish.
try { [void](Invoke-ViceMonitor -Commands @('x') -ReadMs 120) } catch { Write-Trace "resume-note $_" }
Write-Trace 'waiting 18s for autostart after unpause'
Start-Sleep -Seconds 18

$hit = $false
$last = '??'
$deadline = [DateTime]::UtcNow.AddSeconds(90)
$n = 0
while ([DateTime]::UtcNow -lt $deadline) {
    $n++
    $alive = Get-Process -Id $procId -ErrorAction SilentlyContinue
    if (-not $alive) { Write-Trace 'x64sc exited'; break }
    $inj = Send-CiaKeys $procId
    Write-Trace "inject $n $inj"
    try {
        [void](Invoke-ViceMonitor -Commands @('keybuf "\n"') -ReadMs 150)
    } catch {
        Write-Trace "keybuf-fail $n $_"
    }
    Start-Sleep -Seconds 2
    try {
        $peek = Invoke-ViceMonitor -Commands @('m 0001', 'm d011', 'm d015', 'm d018', 'm dd00') -ReadMs 180
        $d015 = Get-HexByte $peek 'd015'
        $d018 = Get-HexByte $peek 'd018'
        $d011 = Get-HexByte $peek 'd011'
        $dd00 = Get-HexByte $peek 'dd00'
        $cpu01 = Get-HexByte $peek '0001'
        $last = $d015
        Write-Trace "peek $n 01=$cpu01 d011=$d011 d015=$d015 d018=$d018 dd00=$dd00"
        if ($d015 -eq '20') {
            if (Test-Path $ram) { Remove-Item $ram -Force }
            $saveCmd = 'save "' + $ram.Replace('\', '/') + '" 0 0000 ffff'
            $saved = Invoke-ViceMonitor -Commands @($saveCmd) -ReadMs 2500 -NoResume
            Write-Trace "SAVE $saved"
            $hit = $true
            break
        }
    } catch {
        Write-Trace "peek-fail $n $_"
    }
}

if ($hit -and (Test-Path $ram)) {
    $bytes = [IO.File]::ReadAllBytes($ram)
    Write-Trace "ram-len=$($bytes.Length)"
    if ($bytes.Length -ge 65538) {
        $slice = New-Object byte[] 65536
        [Array]::Copy($bytes, 2, $slice, 0, 65536)
        [IO.File]::WriteAllBytes($ram64, $slice)
        Write-Trace ("stripped d015={0:X2} d018={1:X2} cpu01={2:X2}" -f $slice[0xD015], $slice[0xD018], $slice[1])
    }
}

Write-Trace "DONE hit=$hit lastD015=$last ram=$(Test-Path $ram)"
Get-Content $trace | Select-Object -Last 30
