// PLAN-XBOXUWP S34 / FIX-XWIN2D-001: the always-present video surface.
//
// The whole type is #if HAS_UWP-guarded (UWP-only), so it compiles to nothing on the
// workload-free net10.0 fallback. Video is rendered through a raw Direct3D 11 composition
// swap chain hosted by a XAML SwapChainPanel - NO Win2D. Win2D (Win2D.uwp) was dropped
// because its Microsoft.Graphics.Canvas.winmd is a Windows Metadata component that modern
// .NET (UseUwp) cannot reference, which failed the UWP build with NETSDK1130. The DX11 path
// removes that dependency entirely.
//
// The DirectX interop follows the same AOT-/trim-clean convention as
// ViceSharp.Host.Audio.XAudio2SourceVoiceDevice: only [LibraryImport] for the two DLL entry
// points (D3D11CreateDevice, CreateDXGIFactory2) plus blittable
// delegate* unmanaged[Stdcall] COM vtable calls - no runtime-marshalled COM interfaces, no
// reflection - so it is Native-AOT safe and needs no bundled native library (d3d11.dll /
// dxgi.dll are part of Windows 10+). Every native call is guarded: a machine with no usable
// GPU/DXGI degrades to a silent no-blit surface rather than throwing.
#if HAS_UWP
namespace ViceSharp.Xbox.Controls;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.System;
using Windows.UI.Xaml.Controls;
using WinRT;
using Microsoft.Extensions.Logging;
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// The C64 video surface: a XAML <see cref="SwapChainPanel"/> backed by a raw Direct3D 11
/// composition swap chain, driven by a <see cref="DispatcherQueueTimer"/> at ~50 Hz. Each
/// tick pulls the latest committed frame through the pure <see cref="VideoFramePullViewModel"/>
/// and nearest-neighbor upscales the 384x272 BGRA8888 source into the centered, window-filling
/// draw rectangle (letterboxed with the active standard's TRUE composite pixel aspect,
/// FIX-XASPECT-001) of a CPU-write staging texture, which is copied to
/// the swap-chain back buffer and presented. The pull is a pure sink (it never advances the
/// emulator), so the render loop can never perturb determinism or stall the worker.
/// </summary>
public sealed unsafe partial class VideoSurfaceHost : Grid
{
    // ~50 Hz PAL frame cadence (matches XboxInputContext.FrameDurationMs).
    private const double RenderIntervalMs = 20.0;

    // DXGI / D3D11 enum values (only the small set this surface uses).
    private const uint DxgiFormatB8G8R8A8Unorm = 87;
    private const uint DxgiUsageRenderTargetOutput = 0x20;
    private const uint DxgiScalingStretch = 0;
    private const uint DxgiSwapEffectFlipSequential = 3;
    private const uint DxgiAlphaModeIgnore = 3;
    private const uint D3D11SdkVersion = 7;
    private const int D3DDriverTypeHardware = 1;
    private const int D3DDriverTypeWarp = 5;
    private const uint D3D11CreateDeviceBgraSupport = 0x20;
    private const uint D3D11UsageStaging = 3;
    private const uint D3D11CpuAccessWrite = 0x10000;
    private const uint D3D11MapWrite = 2;

    // IUnknown vtable slot (all COM objects below inherit IUnknown).
    private const int SlotRelease = 2;

    // ISwapChainPanelNative : IUnknown.
    private const int SlotSetSwapChain = 3;

    // ID3D11Device : IUnknown.
    private const int SlotCreateTexture2D = 5;

    // IDXGISwapChain1 (IDXGISwapChain base): Present 8, GetBuffer 9, ResizeBuffers 13.
    private const int SlotPresent = 8;
    private const int SlotGetBuffer = 9;
    private const int SlotResizeBuffers = 13;

    // IDXGISwapChain2 (IDXGISwapChain1 base): SetMatrixTransform is slot 34 (Windows SDK
    // 10.0.26100 dxgi1_3.h C-style vtbl: ...GetFrameLatencyWaitableObject 33, SetMatrixTransform 34).
    private const int SlotSwapChain2SetMatrixTransform = 34;

    // ID3D11DeviceContext (ID3D11DeviceChild base): Map 14, Unmap 15, CopyResource 47.
    private const int SlotContextMap = 14;
    private const int SlotContextUnmap = 15;
    private const int SlotContextCopyResource = 47;

    // IDXGIFactory2 (IDXGIFactory1/IDXGIFactory/IDXGIObject/IUnknown base):
    // CreateSwapChainForComposition is slot 24.
    private const int SlotCreateSwapChainForComposition = 24;

    private static readonly Guid IidDxgiFactory2 = new("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
    private static readonly Guid IidDxgiSwapChain2 = new("a8be2ac4-199f-4946-b331-79599fb98de7");
    private static readonly Guid IidD3D11Texture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
    // FIX-XRENDERCRASH-001: this MUST be the SYSTEM-XAML (UWP, WinUI2-era) ISwapChainPanelNative
    // from windows.ui.xaml.media.dxinterop.h (Windows SDK 10.0.26100, line 854:
    // MIDL_INTERFACE("F92F19D2-3ADE-45A6-A20C-F6F1EA90554B")). The WinUI 3 header
    // (microsoft.ui.xaml.media.dxinterop.h) declares a DIFFERENT ISwapChainPanelNative
    // (63aad0b8-...; full value banned from this file by TEST-XVIDEO-IID-001b); UWP does not
    // support WinUI 3, so QI'ing a
    // Windows.UI.Xaml SwapChainPanel with the WinUI3 IID fails E_NOINTERFACE (0x80004002),
    // which shipped as a permanently black emulator surface (vicesharp.log, 2026-07-14).
    private static readonly Guid IidSwapChainPanelNative = new("f92f19d2-3ade-45a6-a20c-f6f1ea90554b");

    private readonly DispatcherQueueTimer _timer;
    private readonly SwapChainPanel _panel = new();
    private VideoFramePullViewModel? _pull;

    // FIX-XASPECT-001: the active video standard's composite pixel aspect ratio (display width
    // per pixel width; VICE vicii_get_pixel_aspect). 1.0 = square pixels until the head applies
    // the session's standard via SetPixelAspect.
    private float _pixelAspect = 1f;

    // FEAT-XPERFHUD-001: the letterbox performance HUD's rate aggregator (portable math);
    // null until the head attaches it. Samples are recorded on the render-timer thread only.
    private VideoPerfStatsViewModel? _stats;

    // FIX-XNTSCFILL-001: the active standard's written content rows (246 NTSC / 272 PAL);
    // 0 = use the full source frame.
    private int _sourceContentHeight;

    // FIX-XNTSCFPS-001: geometry-cached blit state. The coordinate maps and the border
    // clear are recomputed ONLY when the paint geometry changes; the steady-state hot path
    // is row stretches + row copies (no per-pixel division, no full-target clear, and no
    // allocation). _clearPending forces one full clear after any geometry change.
    private int _geoTargetWidth;
    private int _geoTargetHeight;
    private int _geoSourceWidth;
    private int _geoVisibleHeight;
    private float _geoPixelAspect;
    private int _geoDrawX;
    private int _geoDrawY;
    private int _geoDrawWidth;
    private int _geoDrawHeight;
    private int[] _xMap = Array.Empty<int>();
    private int[] _yMap = Array.Empty<int>();
    private uint[] _stretchedRow = Array.Empty<uint>();
    private bool _clearPending = true;

    // FIX-XNTSCFPS-001: every Nth HUD compute (~5 s) is mirrored into the log so cadence
    // fixes are receipt-verifiable from LocalState\vicesharp.log without eyes on the HUD.
    private int _hudComputeCount;

    private IntPtr _device;       // ID3D11Device*
    private IntPtr _context;      // ID3D11DeviceContext* (immediate)
    private IntPtr _factory;      // IDXGIFactory2*
    private IntPtr _swapChain;    // IDXGISwapChain1*
    private IntPtr _panelNative;  // ISwapChainPanelNative*
    private IntPtr _staging;      // ID3D11Texture2D* (CPU-write, target-sized)

    private int _targetWidth;
    private int _targetHeight;
    private float _appliedScaleX;
    private float _appliedScaleY;
    private bool _deviceReady;
    private bool _deviceFailed;

    // Dev-PC render diagnostics: the DX11 present path fails silently by design, so a black surface
    // is otherwise undiagnosable. These trace the first frames + first present + failures to the
    // Output window (prefixed "[ViceSharp.Xbox.Video]" for grep). Cheap: gated to the first ticks.
    private int _renderTicks;
    private bool _presentedOnce;

    /// <summary>Creates the surface and its repeating render timer (not yet started).</summary>
    public VideoSurfaceHost()
    {
        Children.Add(_panel);

        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(RenderIntervalMs);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => RenderTick();

        // Release native resources when the surface leaves the tree (process exit / teardown).
        Unloaded += (_, _) =>
        {
            _timer.Stop();
            ReleaseNative();
        };
    }

    /// <summary>Binds the pure video-pull adapter this surface renders.</summary>
    /// <param name="pull">The ~50 Hz frame-pull adapter.</param>
    public void Attach(VideoFramePullViewModel pull)
        => _pull = pull ?? throw new ArgumentNullException(nameof(pull));

    /// <summary>
    /// Sets the composite pixel aspect ratio of the active video standard (FIX-XASPECT-001,
    /// VICE vicii_get_pixel_aspect: PAL 0.93650794, NTSC 0.75). Non-positive values degrade
    /// to square pixels. Takes effect on the next render tick.
    /// </summary>
    /// <param name="pixelAspect">Display width per pixel width.</param>
    public void SetPixelAspect(float pixelAspect)
        => _pixelAspect = pixelAspect > 0f ? pixelAspect : 1f;

    /// <summary>
    /// Sets the number of source-frame rows that carry CONTENT for the active standard
    /// (FIX-XNTSCFILL-001: the VIC frame is a fixed 384x272 for every standard, but NTSC only
    /// writes rows 0..245, leaving an in-frame black band). The paint path crops to this many
    /// top-anchored rows and scales THEM to the window, so NTSC grows to fill and switching
    /// back to PAL shrinks to fit. Values outside 1..frame-height mean "use the full frame".
    /// </summary>
    /// <param name="contentHeight">The written content rows (246 NTSC, 272 PAL), or 0 = full.</param>
    public void SetSourceContentHeight(int contentHeight)
        => _sourceContentHeight = contentHeight;

    /// <summary>
    /// Sets the render cadence to the ACTIVE machine's refresh rate (FIX-XNTSCFPS-001:
    /// NTSC ~59.826 Hz, PAL ~50.125 Hz; the fixed 20 ms interval capped NTSC at ~50 fps
    /// before tick cost, which the operator's HUD surfaced as FPS 22.3 at SPD 98.6%).
    /// Applied at boot and re-applied after a model-change session rebuild; takes effect
    /// immediately on the running repeating timer.
    /// </summary>
    /// <param name="refreshHz">The machine refresh rate in Hz; non-positive = 20 ms default.</param>
    public void SetTargetRefreshRate(double refreshHz)
        => _timer.Interval = TimeSpan.FromMilliseconds(VideoCadence.IntervalMsFor(refreshHz));

    /// <summary>
    /// Raised (~2 Hz, on the render-timer/dispatcher thread) with the freshly formatted
    /// letterbox performance-HUD text (FEAT-XPERFHUD-001).
    /// </summary>
    public event Action<string>? StatsTextUpdated;

    /// <summary>
    /// Attaches the performance-HUD rate aggregator this surface feeds per tick
    /// (FEAT-XPERFHUD-001): one sample per present, one per newly committed frame.
    /// </summary>
    /// <param name="stats">The portable HUD rate math.</param>
    public void AttachStats(VideoPerfStatsViewModel stats)
        => _stats = stats ?? throw new ArgumentNullException(nameof(stats));

    /// <summary>Starts the render loop.</summary>
    public void Start() => _timer.Start();

    /// <summary>Stops the render loop.</summary>
    public void Stop() => _timer.Stop();

    private void RenderTick()
    {
        try
        {
            RenderTickCore();
        }
        catch (Exception ex)
        {
            // ROOT CAUSE of the deployed code-1 crash: every native COM/pointer call in the render
            // path (Slot vtable derefs, GetBuffer, Map, the nearest-neighbor span writes, Present,
            // the CsWinRT panel-native QueryInterface) can throw. The DispatcherQueueTimer fires
            // this ~50 Hz; for the first several seconds it bails early (pull.Tick()=false / panel
            // not sized), but once the first C64 frame is committed AND the panel is sized it enters
            // the real DX11 path and an uncaught throw here propagates to the XAML dispatcher and
            // terminates the app with exit code 1. A render-loop fault must NEVER kill the app: log
            // the full stack, disable the surface (degrade to silent no-blit), and keep the process
            // and emulator alive.
            _deviceFailed = true;
            ReleaseNative();
            App.CreateLogger("Video").LogError(
                ex,
                "RenderTick threw (tick {Tick}); disabling video surface -> silent no-blit",
                _renderTicks);
        }
    }

    private void RenderTickCore()
    {
        _renderTicks++;
        var trace = _renderTicks <= 15;

        // FEAT-XPERFHUD-001: poll the HUD aggregator every tick (including bail paths, so the
        // HUD keeps refreshing when paused/idle); it throttles itself to ~2 Hz internally.
        if (_stats is not null
            && _stats.TryComputeText(Stopwatch.GetTimestamp(), Stopwatch.Frequency, out var hudText))
        {
            StatsTextUpdated?.Invoke(hudText);

            // FIX-XNTSCFPS-001: mirror every 10th HUD compute (~5 s) into the log so the
            // cadence receipts are readable from LocalState\vicesharp.log headlessly.
            if (++_hudComputeCount % 10 == 0)
            {
                App.CreateLogger("Video").LogInformation(
                    "perf: {Hud} (interval {IntervalMs}ms)",
                    hudText.Replace("\r", string.Empty).Replace('\n', ' '),
                    _timer.Interval.TotalMilliseconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        var pull = _pull;
        if (pull is null)
        {
            if (trace) Diag("bail: no pull adapter attached");
            return;
        }

        if (_deviceFailed)
        {
            if (trace) Diag("bail: DX11 device previously failed -> silent no-blit");
            return;
        }

        if (!pull.Tick())
        {
            if (trace) Diag("bail: pull.Tick()=false (no committed C64 frame yet)");
            return;
        }

        // FEAT-XPERFHUD-001: one emulated-frame sample per pull (the aggregator dedupes
        // repeated cycle stamps, so unchanged committed frames never count).
        _stats?.RecordFrame(pull.Cycle, Stopwatch.GetTimestamp());

        // Physical pixel size of the panel (logical size * composition scale).
        var targetWidth = (int)Math.Round(_panel.ActualWidth * _panel.CompositionScaleX);
        var targetHeight = (int)Math.Round(_panel.ActualHeight * _panel.CompositionScaleY);
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            if (trace)
                Diag($"bail: panel not sized (ActualW={_panel.ActualWidth} ActualH={_panel.ActualHeight} scaleX={_panel.CompositionScaleX} scaleY={_panel.CompositionScaleY})");
            return;
        }

        if (!EnsureDevice()
            || !EnsureSwapChain(targetWidth, targetHeight, _panel.CompositionScaleX, _panel.CompositionScaleY))
        {
            return;
        }

        var frame = pull.CurrentFrame;
        if (frame.Length == 0)
        {
            if (trace) Diag($"bail: empty frame (src {pull.Width}x{pull.Height})");
            return;
        }

        if (!RenderFrame(pull.Width, pull.Height, frame))
            return;

        // FEAT-XPERFHUD-001: one present sample per ACTUALLY presented frame (RenderFrame
        // reports its internal GetBuffer/Map bails, which must not count as presents).
        _stats?.RecordPresent(Stopwatch.GetTimestamp());

        if (!_presentedOnce)
        {
            _presentedOnce = true;
            Diag($"OK: first frame presented (target {targetWidth}x{targetHeight}, src {pull.Width}x{pull.Height}, {frame.Length} bytes)");
        }
    }

    private void Diag(string message)
    {
        // Keep the original Debug trace AND route through ILogger so the one-shot render-pipeline
        // traces land in the readable LocalState\vicesharp.log (Debug.WriteLine is invisible in a
        // packaged UWP app). App.CreateLogger never returns null, so this is always safe.
        var line = $"[ViceSharp.Xbox.Video] tick {_renderTicks}: {message}";
        System.Diagnostics.Debug.WriteLine(line);
        App.CreateLogger("Video").LogInformation("tick {Tick}: {Message}", _renderTicks, message);
    }

    /// <summary>
    /// Blits one frame into the staging texture, copies it to the back buffer, and presents.
    /// Returns <c>false</c> on the internal GetBuffer/Map bails so the caller (and the HUD's
    /// present counter, FEAT-XPERFHUD-001) knows nothing reached the screen this tick.
    /// </summary>
    private bool RenderFrame(int sourceWidth, int sourceHeight, ReadOnlySpan<byte> source)
    {
        // Acquire the current back buffer (flip model rotates buffers; always GetBuffer(0)).
        var getBuffer =
            (delegate* unmanaged[Stdcall]<IntPtr, uint, Guid*, IntPtr*, int>)
            Slot(_swapChain, SlotGetBuffer);
        var texIid = IidD3D11Texture2D;
        IntPtr backBuffer;
        if (getBuffer(_swapChain, 0, &texIid, &backBuffer) < 0 || backBuffer == IntPtr.Zero)
            return false;

        try
        {
            var map =
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, uint, uint, D3D11MappedSubresource*, int>)
                Slot(_context, SlotContextMap);
            D3D11MappedSubresource mapped;
            if (map(_context, _staging, 0, D3D11MapWrite, 0, &mapped) < 0 || mapped.PData == IntPtr.Zero)
                return false;

            PaintNearestNeighbor(sourceWidth, sourceHeight, source, (byte*)mapped.PData, (int)mapped.RowPitch);

            var unmap =
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, void>)
                Slot(_context, SlotContextUnmap);
            unmap(_context, _staging, 0);

            var copy =
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, void>)
                Slot(_context, SlotContextCopyResource);
            copy(_context, backBuffer, _staging);

            var present =
                (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, int>)
                Slot(_swapChain, SlotPresent);
            present(_swapChain, 1, 0);
            return true;
        }
        finally
        {
            Release(ref backBuffer);
        }
    }

    /// <summary>
    /// Nearest-neighbor upscales the BGRA source into the centered, window-filling draw
    /// rectangle computed by <see cref="VideoDisplayGeometry.ComputeDrawRect"/>
    /// (FIX-XASPECT-001: letterboxed with the TRUE composite pixel aspect). FIX-XNTSCFPS-001
    /// hot-path shape: the coordinate maps come from <see cref="NearestNeighborMap"/> and are
    /// rebuilt only on geometry change (as is the one full black clear for the letterbox
    /// borders, which are static between geometry changes); each painted row is stretched
    /// ONCE into a cached row buffer and repeated rows are bulk-copied. Alpha is IGNORE'd by
    /// the composition swap chain, so cleared borders read as opaque black.
    /// </summary>
    private void PaintNearestNeighbor(
        int sourceWidth, int sourceHeight, ReadOnlySpan<byte> source, byte* destination, int destinationRowPitch)
    {
        int targetWidth = _targetWidth;
        int targetHeight = _targetHeight;

        if (sourceWidth <= 0 || sourceHeight <= 0 || source.Length < sourceWidth * sourceHeight * 4)
        {
            ClearTarget(destination, destinationRowPitch, targetWidth, targetHeight);
            return;
        }

        // FIX-XNTSCFILL-001: crop to the standard's WRITTEN rows (top-anchored: the renderer
        // maps its first visible raster line to frame row 0), so NTSC's 246 content rows fill
        // the window instead of dragging the frame's black band along.
        var visibleHeight = _sourceContentHeight > 0 && _sourceContentHeight < sourceHeight
            ? _sourceContentHeight
            : sourceHeight;

        if (targetWidth != _geoTargetWidth || targetHeight != _geoTargetHeight
            || sourceWidth != _geoSourceWidth || visibleHeight != _geoVisibleHeight
            || _pixelAspect != _geoPixelAspect)
        {
            RebuildPaintGeometry(targetWidth, targetHeight, sourceWidth, visibleHeight);
        }

        if (_clearPending)
        {
            ClearTarget(destination, destinationRowPitch, targetWidth, targetHeight);
            _clearPending = false;
        }

        if (_geoDrawWidth <= 0 || _geoDrawHeight <= 0)
            return;

        int sourceStride = sourceWidth * 4;
        int drawX = _geoDrawX;
        int drawY = _geoDrawY;
        int drawWidth = _geoDrawWidth;
        int drawHeight = _geoDrawHeight;
        int lastStretchedSy = -1;

        fixed (byte* sourceBase = source)
        fixed (int* xMap = _xMap)
        fixed (uint* stretched = _stretchedRow)
        {
            for (int dy = 0; dy < drawHeight; dy++)
            {
                int sy = _yMap[dy];

                // Stretch each SOURCE row once; duplicate destination rows reuse the buffer.
                if (sy != lastStretchedSy)
                {
                    uint* sourceRow = (uint*)(sourceBase + (long)sy * sourceStride);
                    for (int dx = 0; dx < drawWidth; dx++)
                        stretched[dx] = sourceRow[xMap[dx]];
                    lastStretchedSy = sy;
                }

                Buffer.MemoryCopy(
                    stretched,
                    (uint*)(destination + (long)(drawY + dy) * destinationRowPitch) + drawX,
                    (long)drawWidth * 4,
                    (long)drawWidth * 4);
            }
        }
    }

    /// <summary>
    /// Recomputes the draw rectangle, the precomputed nearest-neighbor coordinate maps, and
    /// the stretched-row buffer for a new paint geometry, and schedules the one-time border
    /// clear (FIX-XNTSCFPS-001). Allocation happens ONLY here, never on the steady-state path.
    /// </summary>
    private void RebuildPaintGeometry(int targetWidth, int targetHeight, int sourceWidth, int visibleHeight)
    {
        _geoTargetWidth = targetWidth;
        _geoTargetHeight = targetHeight;
        _geoSourceWidth = sourceWidth;
        _geoVisibleHeight = visibleHeight;
        _geoPixelAspect = _pixelAspect;

        (_geoDrawX, _geoDrawY, _geoDrawWidth, _geoDrawHeight) = VideoDisplayGeometry.ComputeDrawRect(
            targetWidth, targetHeight, sourceWidth, visibleHeight, _pixelAspect);

        _xMap = NearestNeighborMap.Build(sourceWidth, _geoDrawWidth);
        _yMap = NearestNeighborMap.Build(visibleHeight, _geoDrawHeight);
        _stretchedRow = _geoDrawWidth > 0 ? new uint[_geoDrawWidth] : Array.Empty<uint>();
        _clearPending = true;

        Diag($"paint geometry: target {targetWidth}x{targetHeight}, src {sourceWidth}x{visibleHeight}, draw {_geoDrawWidth}x{_geoDrawHeight}+{_geoDrawX}+{_geoDrawY}, PAR {_pixelAspect}");
    }

    private static void ClearTarget(byte* destination, int destinationRowPitch, int targetWidth, int targetHeight)
    {
        for (int y = 0; y < targetHeight; y++)
            new Span<byte>(destination + (long)y * destinationRowPitch, targetWidth * 4).Clear();
    }

    private bool EnsureDevice()
    {
        if (_deviceReady)
            return true;
        if (_deviceFailed)
            return false;

        try
        {
            const uint flags = D3D11CreateDeviceBgraSupport;

            int hr = D3D11CreateDevice(
                IntPtr.Zero, D3DDriverTypeHardware, IntPtr.Zero, flags,
                IntPtr.Zero, 0, D3D11SdkVersion, out _device, out _, out _context);
            Diag($"EnsureDevice: D3D11CreateDevice(HW) hr=0x{hr:X8} device=0x{_device:X} context=0x{_context:X}");

            if (hr < 0 || _device == IntPtr.Zero || _context == IntPtr.Zero)
            {
                // Retry with the WARP software rasterizer (no/failed GPU).
                Release(ref _context);
                Release(ref _device);
                hr = D3D11CreateDevice(
                    IntPtr.Zero, D3DDriverTypeWarp, IntPtr.Zero, flags,
                    IntPtr.Zero, 0, D3D11SdkVersion, out _device, out _, out _context);
                Diag($"EnsureDevice: D3D11CreateDevice(WARP) hr=0x{hr:X8} device=0x{_device:X} context=0x{_context:X}");
            }

            if (hr < 0 || _device == IntPtr.Zero || _context == IntPtr.Zero)
            {
                Diag("EnsureDevice: no D3D11 device after HW+WARP -> FailDevice");
                return FailDevice();
            }

            int factoryHr = CreateDXGIFactory2(0, IidDxgiFactory2, out _factory);
            Diag($"EnsureDevice: CreateDXGIFactory2 hr=0x{factoryHr:X8} factory=0x{_factory:X}");
            if (factoryHr < 0 || _factory == IntPtr.Zero)
                return FailDevice();

            if (!EnsurePanelNative())
            {
                Diag("EnsureDevice: EnsurePanelNative failed -> FailDevice");
                return FailDevice();
            }

            _deviceReady = true;
            Diag("EnsureDevice: OK (device + context + factory + panel-native all ready)");
            return true;
        }
        catch (Exception ex)
        {
            Diag($"EnsureDevice threw: {ex}");
            return FailDevice();
        }
    }

    private bool EnsurePanelNative()
    {
        if (_panelNative != IntPtr.Zero)
            return true;

        try
        {
            // Borrow the panel's native IInspectable and QueryInterface ISwapChainPanelNative
            // through CsWinRT (AOT-safe: no reflection). TryAs returns an AddRef'd pointer we own.
            IObjectReference? native = ((IWinRTObject)_panel).NativeObject;
            if (native is null)
            {
                Diag("EnsurePanelNative: ((IWinRTObject)_panel).NativeObject is null");
                return false;
            }

            int hr = native.TryAs(IidSwapChainPanelNative, out _panelNative);
            Diag($"EnsurePanelNative: TryAs(ISwapChainPanelNative) hr=0x{hr:X8} ptr=0x{_panelNative:X}");
            return hr >= 0 && _panelNative != IntPtr.Zero;
        }
        catch (Exception ex)
        {
            Diag($"EnsurePanelNative threw: {ex}");
            return false;
        }
    }

    private bool EnsureSwapChain(int width, int height, float scaleX, float scaleY)
    {
        if (width <= 0 || height <= 0)
            return false;

        if (_swapChain == IntPtr.Zero)
        {
            var description = new DxgiSwapChainDesc1
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = DxgiFormatB8G8R8A8Unorm,
                Stereo = 0,
                SampleDesc = new DxgiSampleDesc { Count = 1, Quality = 0 },
                BufferUsage = DxgiUsageRenderTargetOutput,
                BufferCount = 2,
                Scaling = DxgiScalingStretch,
                SwapEffect = DxgiSwapEffectFlipSequential,
                AlphaMode = DxgiAlphaModeIgnore,
                Flags = 0,
            };

            var createSwapChain =
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, DxgiSwapChainDesc1*, IntPtr, IntPtr*, int>)
                Slot(_factory, SlotCreateSwapChainForComposition);
            IntPtr swapChain;
            if (createSwapChain(_factory, _device, &description, IntPtr.Zero, &swapChain) < 0
                || swapChain == IntPtr.Zero)
            {
                Diag("EnsureSwapChain: IDXGIFactory2.CreateSwapChainForComposition failed");
                return false;
            }
            _swapChain = swapChain;

            var setSwapChain =
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)
                Slot(_panelNative, SlotSetSwapChain);
            if (setSwapChain(_panelNative, _swapChain) < 0)
            {
                Diag("EnsureSwapChain: ISwapChainPanelNative.SetSwapChain failed");
                return false;
            }

            _targetWidth = width;
            _targetHeight = height;
            ApplyInverseCompositionScale(scaleX, scaleY);
            Diag($"EnsureSwapChain: created + bound {width}x{height}");
            return EnsureStaging(width, height);
        }

        if (_targetWidth != width || _targetHeight != height)
        {
            var resize =
                (delegate* unmanaged[Stdcall]<IntPtr, uint, uint, uint, uint, uint, int>)
                Slot(_swapChain, SlotResizeBuffers);
            if (resize(_swapChain, 2, (uint)width, (uint)height, DxgiFormatB8G8R8A8Unorm, 0) < 0)
                return false;

            _targetWidth = width;
            _targetHeight = height;
            ApplyInverseCompositionScale(scaleX, scaleY);
            return EnsureStaging(width, height);
        }

        // Same dimensions but a changed composition scale (e.g. the user moved the window to a
        // monitor with a different DPI while the physical size happened to match): re-apply the
        // inverse transform so the content is not composition-scaled again.
        if (_appliedScaleX != scaleX || _appliedScaleY != scaleY)
            ApplyInverseCompositionScale(scaleX, scaleY);

        // Swap chain exists and dimensions are unchanged. Retry staging if a prior EnsureStaging
        // attempt failed (a one-off CreateTexture2D failure must not leave _staging NULL while we
        // report the surface ready, or RenderFrame would Map a NULL ID3D11Resource* -> AV).
        return _staging != IntPtr.Zero || EnsureStaging(width, height);
    }

    /// <summary>
    /// FIX-XCOMPSCALE-001: the swap chain is sized in PHYSICAL pixels (panel logical size x
    /// CompositionScaleX/Y), but XAML composition maps swap-chain content in LOGICAL pixels and
    /// scales it by the composition scale. Without compensation the buffer renders composition-
    /// scale-times too large (on a scale-2.5 dev PC only the top-left ~40% of the C64 frame was
    /// visible, 2.5x oversized). The documented fix (DirectX-and-XAML interop; every UWP
    /// SwapChainPanel sample) is IDXGISwapChain2::SetMatrixTransform with the INVERSE scale
    /// (DXGI_MATRIX_3X2_F _11=1/scaleX, _22=1/scaleY). Failure is non-fatal: content stays
    /// oversized rather than absent, and the hr lands in the on-device trace.
    /// </summary>
    private void ApplyInverseCompositionScale(float scaleX, float scaleY)
    {
        // QI the IDXGISwapChain1 for IDXGISwapChain2 (IUnknown::QueryInterface, slot 0).
        var queryInterface =
            (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)Slot(_swapChain, 0);
        var iid = IidDxgiSwapChain2;
        IntPtr swapChain2;
        int hr = queryInterface(_swapChain, &iid, &swapChain2);
        if (hr < 0 || swapChain2 == IntPtr.Zero)
        {
            Diag($"ApplyInverseCompositionScale: QI(IDXGISwapChain2) hr=0x{hr:X8} -> content stays composition-scaled");
            return;
        }

        try
        {
            var matrix = new DxgiMatrix3x2F
            {
                M11 = scaleX > 0f ? 1f / scaleX : 1f,
                M22 = scaleY > 0f ? 1f / scaleY : 1f,
            };

            var setMatrixTransform =
                (delegate* unmanaged[Stdcall]<IntPtr, DxgiMatrix3x2F*, int>)
                Slot(swapChain2, SlotSwapChain2SetMatrixTransform);
            hr = setMatrixTransform(swapChain2, &matrix);

            _appliedScaleX = scaleX;
            _appliedScaleY = scaleY;
            Diag($"ApplyInverseCompositionScale: SetMatrixTransform(1/{scaleX}, 1/{scaleY}) hr=0x{hr:X8}");
        }
        finally
        {
            Release(ref swapChain2);
        }
    }

    private bool EnsureStaging(int width, int height)
    {
        Release(ref _staging);

        var description = new D3D11Texture2DDesc
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = DxgiFormatB8G8R8A8Unorm,
            SampleDesc = new DxgiSampleDesc { Count = 1, Quality = 0 },
            Usage = D3D11UsageStaging,
            BindFlags = 0,
            CpuAccessFlags = D3D11CpuAccessWrite,
            MiscFlags = 0,
        };

        var createTexture =
            (delegate* unmanaged[Stdcall]<IntPtr, D3D11Texture2DDesc*, IntPtr, IntPtr*, int>)
            Slot(_device, SlotCreateTexture2D);
        IntPtr texture;
        if (createTexture(_device, &description, IntPtr.Zero, &texture) < 0 || texture == IntPtr.Zero)
            return false;

        _staging = texture;
        return true;
    }

    private bool FailDevice()
    {
        _deviceFailed = true;
        Diag("FailDevice: DX11 unavailable (D3D11CreateDevice HW+WARP, the DXGI factory, or the panel-native QueryInterface failed) -> silent no-blit");
        ReleaseNative();
        return false;
    }

    private void ReleaseNative()
    {
        Release(ref _staging);
        Release(ref _swapChain);
        Release(ref _panelNative);
        Release(ref _factory);
        Release(ref _context);
        Release(ref _device);
        _deviceReady = false;
        _targetWidth = 0;
        _targetHeight = 0;
        _appliedScaleX = 0f;
        _appliedScaleY = 0f;
    }

    /// <summary>Reads vtable slot <paramref name="index"/> of the COM object at <paramref name="comObject"/>.</summary>
    private static void* Slot(IntPtr comObject, int index)
    {
        var vtbl = *(void***)(void*)comObject;
        return vtbl[index];
    }

    /// <summary>Releases the COM object at <paramref name="comObject"/> (IUnknown::Release) and clears it.</summary>
    private static void Release(ref IntPtr comObject)
    {
        if (comObject == IntPtr.Zero)
            return;

        var release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)Slot(comObject, SlotRelease);
        release(comObject);
        comObject = IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DxgiSampleDesc
    {
        public uint Count;
        public uint Quality;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DxgiSwapChainDesc1
    {
        public uint Width;
        public uint Height;
        public uint Format;
        public int Stereo;
        public DxgiSampleDesc SampleDesc;
        public uint BufferUsage;
        public uint BufferCount;
        public uint Scaling;
        public uint SwapEffect;
        public uint AlphaMode;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11Texture2DDesc
    {
        public uint Width;
        public uint Height;
        public uint MipLevels;
        public uint ArraySize;
        public uint Format;
        public DxgiSampleDesc SampleDesc;
        public uint Usage;
        public uint BindFlags;
        public uint CpuAccessFlags;
        public uint MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct D3D11MappedSubresource
    {
        public IntPtr PData;
        public uint RowPitch;
        public uint DepthPitch;
    }

    /// <summary>DXGI_MATRIX_3X2_F (dxgi1_3.h): six floats _11 _12 _21 _22 _31 _32.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct DxgiMatrix3x2F
    {
        public float M11;
        public float M12;
        public float M21;
        public float M22;
        public float M31;
        public float M32;
    }

    [LibraryImport("d3d11.dll")]
    private static partial int D3D11CreateDevice(
        IntPtr pAdapter,
        int driverType,
        IntPtr software,
        uint flags,
        IntPtr pFeatureLevels,
        uint featureLevels,
        uint sdkVersion,
        out IntPtr ppDevice,
        out uint pFeatureLevel,
        out IntPtr ppImmediateContext);

    [LibraryImport("dxgi.dll")]
    private static partial int CreateDXGIFactory2(uint flags, in Guid riid, out IntPtr ppFactory);
}
#endif
