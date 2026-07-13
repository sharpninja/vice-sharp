// PLAN-XBOXUWP S34 (IMPL-XBOXUWP-034): the always-present video surface.
//
// The whole type is #if HAS_UWP-guarded (UWP-only), so it compiles to nothing on the
// workload-free net10.0 fallback. The Win2D rendering is further gated on the WIN2D symbol,
// which the csproj defines only when the guarded Win2D package is referenced. When WIN2D is
// off (the plan's Win2D->DX11 fallback / a Win2D-less validation build) the surface is a
// plain panel that still pulls frames but performs no GPU blit, so the XAML head compiles
// and runs without a Win2D dependency.
#if HAS_UWP
namespace ViceSharp.Xbox.Controls;

using System;
using Windows.System;
using Windows.UI.Xaml.Controls;
#if WIN2D
using Windows.Graphics.DirectX;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
#endif
using ViceSharp.Xbox.ViewModels;

/// <summary>
/// The C64 video surface: a Win2D <see cref="CanvasSwapChainPanel"/> driven by a
/// <see cref="DispatcherQueueTimer"/> at ~50 Hz. Each tick pulls the latest committed frame
/// through the pure <see cref="VideoFramePullViewModel"/> into a reused buffer and blits it
/// as a BGRA8888 bitmap. The pull is a pure sink (it never advances the emulator), so the
/// render loop can never perturb determinism or stall the worker.
/// </summary>
public sealed partial class VideoSurfaceHost : Grid
{
    // ~50 Hz PAL frame cadence (matches XboxInputContext.FrameDurationMs).
    private const double RenderIntervalMs = 20.0;

    private readonly DispatcherQueueTimer _timer;
    private VideoFramePullViewModel? _pull;

#if WIN2D
    private readonly CanvasSwapChainPanel _panel = new();
    private CanvasDevice? _device;
    private CanvasSwapChain? _swapChain;
    private byte[] _uploadBuffer = Array.Empty<byte>();
    private int _width;
    private int _height;
#endif

    /// <summary>Creates the surface and its repeating render timer (not yet started).</summary>
    public VideoSurfaceHost()
    {
#if WIN2D
        Children.Add(_panel);
#endif
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(RenderIntervalMs);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => RenderTick();
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
        var pull = _pull;
        if (pull is null || !pull.Tick())
            return;

#if WIN2D
        EnsureSwapChain(pull.Width, pull.Height);
        if (_swapChain is null || _device is null)
            return;

        var frame = pull.CurrentFrame;
        if (frame.Length == 0)
            return;

        if (_uploadBuffer.Length < frame.Length)
            _uploadBuffer = new byte[frame.Length];
        frame.CopyTo(_uploadBuffer);

        using var bitmap = CanvasBitmap.CreateFromBytes(
            _device,
            _uploadBuffer.AsSpan(0, frame.Length).ToArray(),
            pull.Width,
            pull.Height,
            DirectXPixelFormat.B8G8R8A8UIntNormalized);

        using var session = _swapChain.CreateDrawingSession(Colors.Black);
        session.DrawImage(bitmap);
        _swapChain.Present();
#endif
    }

#if WIN2D
    private void EnsureSwapChain(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        _device ??= CanvasDevice.GetSharedDevice();

        const float dpi = 96.0f;
        if (_swapChain is null)
        {
            _swapChain = new CanvasSwapChain(_device, width, height, dpi);
            _panel.SwapChain = _swapChain;
        }
        else if (_width != width || _height != height)
        {
            _swapChain.ResizeBuffers(width, height, dpi);
        }

        _width = width;
        _height = height;
    }
#endif
}
#endif
