$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\vice-sharp\validation-output\wolf64-frame-diff-20260915'
$x64sc = 'C:\Users\kingd\AppData\Local\Microsoft\WinGet\Packages\VICE-Team.VICE.GTK3_Microsoft.Winget.Source_8wekyb3d8bbwe\GTK3VICE-3.10-win64\bin\x64sc.exe'
$disk = Join-Path $outDir 'wolf64.d64'
$ram = Join-Path $outDir 'x64sc-play-ram.bin'
$ram64 = Join-Path $outDir 'x64sc-play-ram-64k.bin'
$trace = Join-Path $outDir 'x64sc-drive-d015.log'
$stdoutLog = Join-Path $outDir 'x64sc-drive-stdout.log'
$stderrLog = Join-Path $outDir 'x64sc-drive-stderr.log'
$vsRam = Join-Path $outDir 'vicesharp-ram.bin'
$compareOut = Join-Path $outDir 'RAM-COMPARE-play.txt'

Add-Type @"
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class ViceDrive2 {
  public const int WM_KEYDOWN = 0x0100;
  public const int WM_KEYUP = 0x0101;
  public const int WM_CHAR = 0x0102;
  public const uint KEYEVENTF_KEYUP = 0x0002;
  public const uint KEYEVENTF_SCANCODE = 0x0008;
  public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
  public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
  public const uint MOUSEEVENTF_MOVE = 0x0001;
  public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
  public const uint MOUSEEVENTF_LEFTUP = 0x0004;
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
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
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
      uint p;
      GetWindowThreadProcessId(h, out p);
      if (p != procId) return true;
      var sb = new StringBuilder(256);
      GetWindowText(h, sb, 256);
      var t = sb.ToString();
      if (t.IndexOf("VICE", StringComparison.OrdinalIgnoreCase) >= 0) { found = h; return false; }
      return true;
    }, IntPtr.Zero);
    return found;
  }
  public static List<IntPtr> WindowsForProc(uint procId) {
    var list = new List<IntPtr>();
    EnumWindows((h, l) => {
      uint p;
      GetWindowThreadProcessId(h, out p);
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
  public static void PostKey(IntPtr h, byte vk, ushort scan) {
    PostMessage(h, WM_KEYDOWN, (IntPtr)vk, KeyLParam(scan, false));
    PostMessage(h, WM_CHAR, (IntPtr)vk, KeyLParam(scan, false));
    PostMessage(h, WM_KEYUP, (IntPtr)vk, KeyLParam(scan, true));
  }
  public static void SendScan(ushort scan, bool up, bool ext) {
    var inp = new INPUT();
    inp.type = 1;
    inp.u.ki.wScan = scan;
    uint f = KEYEVENTF_SCANCODE;
    if (up) f |= KEYEVENTF_KEYUP;
    if (ext) f |= KEYEVENTF_EXTENDEDKEY;
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
    inp.u.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE;
    SendInput(1, new INPUT[] { inp }, Marshal.SizeOf(typeof(INPUT)));
    inp.u.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_LEFTDOWN;
    SendInput(1, new INPUT[] { inp }, Marshal.SizeOf(typeof(INPUT)));
    inp.u.mi.dwFlags = MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_LEFTUP;
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
    param(
        [string[]]$Commands,
        [int]$ReadMs = 500,
        [switch]$NoResume
    )
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
        $deadline = [DateTime]::UtcNow.AddMilliseconds(800)
        while ([DateTime]::UtcNow -lt $deadline) {
            if ($stream.DataAvailable) {
                $n = $stream.Read($buf, 0, $buf.Length)
                $resp += [Text.Encoding]::ASCII.GetString($buf, 0, $n)
            } else {
                Start-Sleep -Milliseconds 25
            }
        }
    }
    if (-not $NoResume) {
        $xb = [Text.Encoding]::ASCII.GetBytes("x`n")
        $stream.Write($xb, 0, $xb.Length)
        $stream.Flush()
        Start-Sleep -Milliseconds 600
    }
    $client.Close()
    $resp
}

function Get-HexAt([string]$text, [string]$addr) {
    $m = [regex]::Match($text, '(?i)C:' + [regex]::Escape($addr) + '\s+([0-9A-F]{2})')
    if ($m.Success) { return $m.Groups[1].Value.ToUpperInvariant() }
    return $null
}

function Send-CiaKeys([uint32]$processId) {
    $hwnd = [ViceDrive2]::FindVice($processId)
    if ($hwnd -eq [IntPtr]::Zero) { return 'hwnd=0' }
    [ViceDrive2]::ShowWindow($hwnd, 6) | Out-Null
    Start-Sleep -Milliseconds 50
    [ViceDrive2]::ShowWindow($hwnd, 9) | Out-Null
    [ViceDrive2]::ForceForeground($hwnd)
    $r = New-Object ViceDrive2+RECT
    [void][ViceDrive2]::GetWindowRect($hwnd, [ref]$r)
    $cx = [int](($r.L + $r.R) / 2)
    $cy = [int](($r.T + $r.B) / 2)
    [ViceDrive2]::ClickAbs($cx, $cy)
    Start-Sleep -Milliseconds 60
    $wins = [ViceDrive2]::WindowsForProc($processId)
    foreach ($h in $wins) {
        [ViceDrive2]::PostKey($h, 0x0D, 0x1C)
        [ViceDrive2]::PostKey($h, 0x20, 0x39)
        [ViceDrive2]::PostKey($h, 0x60, 0x52)
    }
    foreach ($scan in @(0x1C, 0x39, 0x52)) {
        [ViceDrive2]::SendScan($scan, $false, $false)
        Start-Sleep -Milliseconds 60
        [ViceDrive2]::SendScan($scan, $true, $false)
        Start-Sleep -Milliseconds 30
    }
    return "hwnd=$hwnd kids=$($wins.Count) click=$cx,$cy"
}

function Compare-PlayRam([string]$vicePath, [string]$vsPath, [string]$reportPath) {
    $v = [IO.File]::ReadAllBytes($vicePath)
    $s = [IO.File]::ReadAllBytes($vsPath)
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("vsLen=$($s.Length) viceLen=$($v.Length)")
    $lines.Add(('vs d015={0:X2} d018={1:X2} 01={2:X2}' -f $s[0xD015], $s[0xD018], $s[1]))
    $lines.Add(('vice d015={0:X2} d018={1:X2} 01={2:X2}' -f $v[0xD015], $v[0xD018], $v[1]))
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

if (-not (Test-Path -LiteralPath $disk)) { throw "missing disk $disk" }
Get-Process x64sc -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400
foreach ($f in @($trace, $ram, $ram64, $stdoutLog, $stderrLog, $compareOut)) {
    if (Test-Path -LiteralPath $f) { Remove-Item -LiteralPath $f -Force }
}

$proc = Start-Process -FilePath $x64sc -ArgumentList @(
    '-default', '+sound', '+autostart-delay-random', '-autostart-warp', '-warp',
    '+drive8truedrive', '-trapdevice1',
    '-remotemonitor', '-remotemonitoraddress', 'ip4://127.0.0.1:6510',
    '-controlport1device', '1', '-controlport2device', '1',
    '-joydev1', '1', '-joydev2', '1',
    '-joystick1autofire', '-joystick1autofiremode', '1', '-joystick1autofirespeed', '8',
    '-joystick2autofire', '-joystick2autofiremode', '1', '-joystick2autofirespeed', '8',
    '-windowxpos', '80', '-windowypos', '80', '-windowwidth', '768', '-windowheight', '544',
    '-autostart', $disk
) -WorkingDirectory $outDir -PassThru -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog

Write-Trace "started processId=$($proc.Id) free-run 28s (do not touch monitor; a TCP connect pauses x64sc)"
Start-Sleep -Seconds 28
try {
    $boot = Invoke-ViceMonitor -Commands @('m 0001', 'm d011', 'm d015', 'm d018', 'm dd00') -ReadMs 220
    Write-Trace ("boot 01={0} d011={1} d015={2} d018={3} dd00={4}" -f (Get-HexAt $boot '0001'), (Get-HexAt $boot 'd011'), (Get-HexAt $boot 'd015'), (Get-HexAt $boot 'd018'), (Get-HexAt $boot 'dd00'))
} catch {
    Write-Trace "boot-peek-fail $_"
}

$deadline = [DateTime]::UtcNow.AddSeconds(100)
$hit = $false
$n = 0
$lastD015 = '??'
try {
    while ([DateTime]::UtcNow -lt $deadline -and -not $proc.HasExited) {
        $n++
        $inj = Send-CiaKeys ([uint32]$proc.Id)
        Write-Trace "inject $n $inj"
        Start-Sleep -Seconds 2
        $peek = Invoke-ViceMonitor -Commands @('m 0001', 'm d011', 'm d015', 'm d018', 'm dd00') -ReadMs 200
        $cpu01 = Get-HexAt $peek '0001'
        $d011 = Get-HexAt $peek 'd011'
        $d015 = Get-HexAt $peek 'd015'
        $d018 = Get-HexAt $peek 'd018'
        $dd00 = Get-HexAt $peek 'dd00'
        $lastD015 = $d015
        Write-Trace "peek $n 01=$cpu01 d011=$d011 d015=$d015 d018=$d018 dd00=$dd00"
        if ($d015 -eq '20') {
            $saveCmd = 'save "' + $ram.Replace('\', '/') + '" 0 0000 ffff'
            $saved = Invoke-ViceMonitor -Commands @($saveCmd) -ReadMs 2500 -NoResume
            Write-Trace "SAVE $saved"
            $hit = $true
            break
        }
    }
} catch {
    Write-Trace "LOOP-ERROR $_"
}

if ($proc.HasExited) { Write-Trace "x64sc exited code=$($proc.ExitCode)" }

if ($hit -and (Test-Path -LiteralPath $ram)) {
    $bytes = [IO.File]::ReadAllBytes($ram)
    Write-Trace "ram-len=$($bytes.Length)"
    $slice = $null
    if ($bytes.Length -ge 65538) {
        $slice = New-Object byte[] 65536
        [Array]::Copy($bytes, 2, $slice, 0, 65536)
    } elseif ($bytes.Length -eq 65536) {
        $slice = $bytes
    }
    if ($slice) {
        [IO.File]::WriteAllBytes($ram64, $slice)
        Write-Trace ('stripped d015={0:X2} d018={1:X2} 01={2:X2}' -f $slice[0xD015], $slice[0xD018], $slice[1])
        if (Test-Path -LiteralPath $vsRam) {
            $cmp = Compare-PlayRam $ram64 $vsRam $compareOut
            $cmp | ForEach-Object { Write-Trace $_ }
        }
    }
}

Write-Trace "DONE hit=$hit lastD015=$lastD015 ram=$(Test-Path -LiteralPath $ram) processId=$($proc.Id) exited=$($proc.HasExited)"
if (-not $hit -and -not $proc.HasExited) {
    try {
        $saveCmd = 'save "' + $ram.Replace('\', '/') + '" 0 0000 ffff'
        $saved = Invoke-ViceMonitor -Commands @('m d015', $saveCmd) -ReadMs 2500 -NoResume
        Write-Trace "FAILSAVE $saved"
    } catch {
        Write-Trace "FAILSAVE-ERROR $_"
    }
}
