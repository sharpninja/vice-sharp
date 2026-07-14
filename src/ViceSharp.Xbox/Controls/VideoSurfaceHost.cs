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
/// and nearest-neighbor upscales the 384x272 BGRA8888 source into the 90% TV-safe rectangle
/// (letterboxed to preserve aspect ratio) of a CPU-write staging texture, which is copied to
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

    // ID3D11DeviceContext (ID3D11DeviceChild base): Map 14, Unmap 15, CopyResource 47.
    private const int SlotContextMap = 14;
    private const int SlotContextUnmap = 15;
    private const int SlotContextCopyResource = 47;

    // IDXGIFactory2 (IDXGIFactory1/IDXGIFactory/IDXGIObject/IUnknown base):
    // CreateSwapChainForComposition is slot 24.
    private const int SlotCreateSwapChainForComposition = 24;

    private static readonly Guid IidDxgiFactory2 = new("50c83a1c-e072-4c48-87b0-3630fa36a6d0");
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

    private IntPtr _device;       // ID3D11Device*
    private IntPtr _context;      // ID3D11DeviceContext* (immediate)
    private IntPtr _factory;      // IDXGIFactory2*
    private IntPtr _swapChain;    // IDXGISwapChain1*
    private IntPtr _panelNative;  // ISwapChainPanelNative*
    private IntPtr _staging;      // ID3D11Texture2D* (CPU-write, target-sized)

    private int _targetWidth;
    private int _targetHeight;
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

        // Physical pixel size of the panel (logical size * composition scale).
        var targetWidth = (int)Math.Round(_panel.ActualWidth * _panel.CompositionScaleX);
        var targetHeight = (int)Math.Round(_panel.ActualHeight * _panel.CompositionScaleY);
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            if (trace)
                Diag($"bail: panel not sized (ActualW={_panel.ActualWidth} ActualH={_panel.ActualHeight} scaleX={_panel.CompositionScaleX} scaleY={_panel.CompositionScaleY})");
            return;
        }

        if (!EnsureDevice() || !EnsureSwapChain(targetWidth, targetHeight))
            return;

        var frame = pull.CurrentFrame;
        if (frame.Length == 0)
        {
            if (trace) Diag($"bail: empty frame (src {pull.Width}x{pull.Height})");
            return;
        }

        RenderFrame(pull.Width, pull.Height, frame);

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

    private void RenderFrame(int sourceWidth, int sourceHeight, ReadOnlySpan<byte> source)
    {
        // Acquire the current back buffer (flip model rotates buffers; always GetBuffer(0)).
        var getBuffer =
            (delegate* unmanaged[Stdcall]<IntPtr, uint, Guid*, IntPtr*, int>)
            Slot(_swapChain, SlotGetBuffer);
        var texIid = IidD3D11Texture2D;
        IntPtr backBuffer;
        if (getBuffer(_swapChain, 0, &texIid, &backBuffer) < 0 || backBuffer == IntPtr.Zero)
            return;

        try
        {
            var map =
                (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, uint, uint, D3D11MappedSubresource*, int>)
                Slot(_context, SlotContextMap);
            D3D11MappedSubresource mapped;
            if (map(_context, _staging, 0, D3D11MapWrite, 0, &mapped) < 0 || mapped.PData == IntPtr.Zero)
                return;

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
        }
        finally
        {
            Release(ref backBuffer);
        }
    }

    /// <summary>
    /// Paints the target black and nearest-neighbor upscales the BGRA source into the 90%
    /// TV-safe rectangle, letterboxed to preserve the source aspect ratio. Alpha is IGNORE'd
    /// by the composition swap chain, so the cleared borders read as opaque black.
    /// </summary>
    private void PaintNearestNeighbor(
        int sourceWidth, int sourceHeight, ReadOnlySpan<byte> source, byte* destination, int destinationRowPitch)
    {
        int targetWidth = _targetWidth;
        int targetHeight = _targetHeight;

        // Clear the whole target to black.
        for (int y = 0; y < targetHeight; y++)
            new Span<byte>(destination + (long)y * destinationRowPitch, targetWidth * 4).Clear();

        if (sourceWidth <= 0 || sourceHeight <= 0 || source.Length < sourceWidth * sourceHeight * 4)
            return;

        // 90% TV-safe rectangle; fit the source inside it preserving aspect ratio.
        int safeWidth = (int)(targetWidth * 0.9);
        int safeHeight = (int)(targetHeight * 0.9);
        if (safeWidth <= 0 || safeHeight <= 0)
            return;

        double scale = Math.Min((double)safeWidth / sourceWidth, (double)safeHeight / sourceHeight);
        int drawWidth = Math.Max(1, (int)(sourceWidth * scale));
        int drawHeight = Math.Max(1, (int)(sourceHeight * scale));
        int drawX = (targetWidth - drawWidth) / 2;
        int drawY = (targetHeight - drawHeight) / 2;

        int sourceStride = sourceWidth * 4;
        fixed (byte* sourceBase = source)
        {
            for (int dy = 0; dy < drawHeight; dy++)
            {
                int sy = dy * sourceHeight / drawHeight;
                if (sy >= sourceHeight)
                    sy = sourceHeight - 1;

                uint* sourceRow = (uint*)(sourceBase + (long)sy * sourceStride);
                uint* destinationRow = (uint*)(destination + (long)(drawY + dy) * destinationRowPitch) + drawX;

                for (int dx = 0; dx < drawWidth; dx++)
                {
                    int sx = dx * sourceWidth / drawWidth;
                    if (sx >= sourceWidth)
                        sx = sourceWidth - 1;
                    destinationRow[dx] = sourceRow[sx];
                }
            }
        }
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

    private bool EnsureSwapChain(int width, int height)
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
            return EnsureStaging(width, height);
        }

        // Swap chain exists and dimensions are unchanged. Retry staging if a prior EnsureStaging
        // attempt failed (a one-off CreateTexture2D failure must not leave _staging NULL while we
        // report the surface ready, or RenderFrame would Map a NULL ID3D11Resource* -> AV).
        return _staging != IntPtr.Zero || EnsureStaging(width, height);
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
