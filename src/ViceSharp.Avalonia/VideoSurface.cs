using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;

namespace ViceSharp.Avalonia;

public sealed class VideoSurface : Control
{
    private readonly WriteableBitmap _bitmap;
    private byte[]? _scratch;

    // VICE PAL dimensions: 384x272 visible area, 4:3 aspect ratio
    public const int SourceWidth = 384;
    public const int SourceHeight = 272;

    /// <summary>
    /// FIX-XASPECT-002: the ACTIVE machine's composite pixel aspect ratio (display width per
    /// pixel width; VICE vicii.c vicii_get_pixel_aspect: PAL 0.93650794, NTSC 0.75). The shell
    /// re-feeds it whenever the machine profile changes, so a PAL -> NTSC model switch changes
    /// the rendered proportions. 1.0 = square pixels until set.
    /// </summary>
    public double PixelAspect { get; set; } = 1.0;

    /// <summary>
    /// The display aspect mode from settings ("Square pixels" | "VICE pixel aspect" |
    /// "Force 4:3"). Previously the setting existed but the surface ignored it and always
    /// rendered square pixels; <see cref="Render"/> now honors it via
    /// <see cref="ComputeDisplayAspect"/>.
    /// </summary>
    public string AspectMode { get; set; } = "VICE pixel aspect";

    /// <summary>
    /// Computes the display aspect (width/height) of the emulator frame for the given aspect
    /// mode: "Square pixels" ignores the pixel aspect, "Force 4:3" pins the classic CRT frame,
    /// anything else (the "VICE pixel aspect" default) multiplies the frame width by the
    /// standard's composite pixel aspect. Non-positive pixel aspects degrade to square pixels.
    /// </summary>
    /// <param name="aspectMode">The display aspect mode label from settings.</param>
    /// <param name="pixelAspect">The active standard's composite pixel aspect ratio.</param>
    /// <returns>The frame's display aspect ratio (width over height).</returns>
    public static double ComputeDisplayAspect(string? aspectMode, double pixelAspect)
    {
        if (string.Equals(aspectMode, "Square pixels", StringComparison.OrdinalIgnoreCase))
            return (double)SourceWidth / SourceHeight;

        if (string.Equals(aspectMode, "Force 4:3", StringComparison.OrdinalIgnoreCase))
            return 4.0 / 3.0;

        var aspect = pixelAspect > 0 ? pixelAspect : 1.0;
        return SourceWidth * aspect / SourceHeight;
    }

    public VideoSurface()
    {
        Focusable = true;

        // VICE-style: Use VICE's pixel density (96 DPI = 384 pixels / 4 inches)
        _bitmap = new WriteableBitmap(
            new PixelSize(SourceWidth, SourceHeight),
            new Vector(96, 96),  // VICE uses square-ish pixels at 96 DPI
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        FillWithBlank();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        base.OnPointerPressed(e);
    }

    private void FillWithBlank()
    {
        using var fb = _bitmap.Lock();
        unsafe
        {
            var dst = (uint*)fb.Address;
            var count = SourceWidth * SourceHeight;
            for (int i = 0; i < count; i++)
            {
                dst[i] = 0xFF000000;
            }
        }
    }

    /// <summary>
    /// In-process zero-allocation render path (BUG-THROTTLE-001 / FR-1132): pull the
    /// emulation thread's latest published frame straight into this control's
    /// WriteableBitmap via a lock-free copy. No per-frame allocation and no emulation
    /// lock, so the UI render tick cannot stall the emulation worker thread.
    /// </summary>
    public bool UpdateFrom(ILocalVideoFrameSource source, string sessionId)
    {
        const int widthBytes = SourceWidth * 4;
        try
        {
            using var fb = _bitmap.Lock();
            unsafe
            {
                if (fb.RowBytes == widthBytes)
                {
                    // Contiguous: copy the published frame directly into the bitmap.
                    var dest = new Span<byte>((void*)fb.Address, widthBytes * SourceHeight);
                    if (!source.TryCopyFrameInto(sessionId, dest, out _, out _, out _))
                        return false;
                }
                else
                {
                    // Padded rows: copy into a reused scratch buffer, then blit per row.
                    _scratch ??= new byte[widthBytes * SourceHeight];
                    if (!source.TryCopyFrameInto(sessionId, _scratch, out _, out _, out _))
                        return false;

                    fixed (byte* pSrc = _scratch)
                    {
                        var dst = (byte*)fb.Address;
                        for (var y = 0; y < SourceHeight; y++)
                            Buffer.MemoryCopy(pSrc + (y * widthBytes), dst + (y * fb.RowBytes), widthBytes, widthBytes);
                    }
                }
            }

            InvalidateVisual();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SetFrame(VideoFrameDto? frame)
    {
        if (frame is null ||
            frame.Width != SourceWidth ||
            frame.Height != SourceHeight ||
            frame.Bgra.Length < SourceWidth * SourceHeight * 4)
        {
            return;
        }

        try
        {
            using var fb = _bitmap.Lock();
            unsafe
            {
                var dst = (byte*)fb.Address;
                var size = SourceWidth * SourceHeight * 4;

                fixed (byte* pSrc = frame.Bgra)
                {
                    Buffer.MemoryCopy(pSrc, dst, size, size);
                }
            }

            this.InvalidateVisual();
        }
        catch
        {
            // Ignore errors
        }
    }

    public override void Render(DrawingContext context)
    {
        // VICE-style aspect ratio handling: each VIC standard has a different composite pixel
        // aspect (FIX-XASPECT-002). Previously this used SourceWidth/SourceHeight directly,
        // which is SQUARE pixels: the "VICE pixel aspect" setting was a no-op and a PAL -> NTSC
        // model switch changed nothing on screen.
        double windowWidth = Bounds.Width;
        double windowHeight = Bounds.Height;

        if (windowWidth <= 0 || windowHeight <= 0)
            return;

        double displayAspect = ComputeDisplayAspect(AspectMode, PixelAspect);
        
        double windowAspect = windowWidth / windowHeight;
        
        double drawWidth, drawHeight;
        
        if (windowAspect > displayAspect)
        {
            // Window is wider than display, fit to height
            drawHeight = windowHeight;
            drawWidth = windowHeight * displayAspect;
        }
        else
        {
            // Window is taller than display, fit to width
            drawWidth = windowWidth;
            drawHeight = windowWidth / displayAspect;
        }
        
        double x = (windowWidth - drawWidth) / 2;
        double y = (windowHeight - drawHeight) / 2;
        
        var destRect = new Rect(x, y, drawWidth, drawHeight);
        
        context.DrawImage(_bitmap, new Rect(0, 0, SourceWidth, SourceHeight), destRect);
    }
}
