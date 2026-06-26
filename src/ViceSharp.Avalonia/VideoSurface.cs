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
        // VICE-style aspect ratio handling
        // Each VIC chip has different pixel aspect ratios based on video standard
        double windowWidth = Bounds.Width;
        double windowHeight = Bounds.Height;
        
        if (windowWidth <= 0 || windowHeight <= 0)
            return;

        double displayAspect = (double)SourceWidth / SourceHeight;
        
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
