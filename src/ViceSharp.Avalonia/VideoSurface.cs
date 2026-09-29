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
    private WriteableBitmap _bitmap;
    private byte[]? _packedScratch;
    private int _pixelWidth = SourceWidth;
    private int _pixelHeight = SourceHeight;
    private int _contentHeight = SourceHeight;

    // VICE PAL dimensions: 384x272 visible area; NTSC writes fewer rows into the same buffer.
    public const int SourceWidth = 384;
    public const int SourceHeight = 272;

    /// <summary>
    /// FIX-XNTSCFILL-001: rows of content NTSC actually writes (VisibleLines 262 minus first
    /// displayed raster 16). The bottom of the fixed 272-row buffer stays black and must not
    /// consume layout height.
    /// </summary>
    public const int NtscContentHeight = 246;

    /// <summary>
    /// FIX-XASPECT-002: the ACTIVE machine's composite pixel aspect ratio (display width per
    /// pixel width; VICE vicii.c vicii_get_pixel_aspect: PAL 0.93650794, NTSC 0.75). The shell
    /// re-feeds it whenever the machine profile changes, so a PAL to NTSC model switch changes
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
    /// FIX-XNTSCFILL-001: number of top-anchored source rows that carry picture for the active
    /// standard (246 NTSC / 272 PAL). A non-positive value means "use the live
    /// frame height" so VIC-20 canvases are not clamped to the C64 272-row default.
    /// </summary>
    public int ContentHeight
    {
        get => _contentHeight > 0 ? _contentHeight : _pixelHeight;
        set
        {
            var next = value > 0 ? value : 0;
            if (_contentHeight == next)
                return;
            _contentHeight = next;
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Computes the display aspect (width/height) of the emulator frame for the given aspect
    /// mode: "Square pixels" ignores the pixel aspect, "Force 4:3" pins the classic CRT frame,
    /// anything else (the "VICE pixel aspect" default) multiplies the frame width by the
    /// standard's composite pixel aspect. Non-positive pixel aspects degrade to square pixels.
    /// <paramref name="contentHeight"/> is the written content rows (FIX-XNTSCFILL-001).
    /// </summary>
    /// <param name="aspectMode">The display aspect mode label from settings.</param>
    /// <param name="pixelAspect">The active standard's composite pixel aspect ratio.</param>
    /// <param name="contentHeight">Written content rows (default full <see cref="SourceHeight"/>).</param>
    /// <param name="sourceWidth">Live canvas width in pixels (default C64 PAL 384).</param>
    /// <returns>The frame's display aspect ratio (width over height).</returns>
    public static double ComputeDisplayAspect(
        string? aspectMode,
        double pixelAspect,
        int contentHeight = SourceHeight,
        int sourceWidth = SourceWidth)
    {
        int height = contentHeight > 0 ? contentHeight : SourceHeight;
        int width = sourceWidth > 0 ? sourceWidth : SourceWidth;

        if (string.Equals(aspectMode, "Square pixels", StringComparison.OrdinalIgnoreCase))
            return (double)width / height;

        if (string.Equals(aspectMode, "Force 4:3", StringComparison.OrdinalIgnoreCase))
            return 4.0 / 3.0;

        var aspect = pixelAspect > 0 ? pixelAspect : 1.0;
        return width * aspect / height;
    }

    /// <summary>
    /// FR-HOST-003 / FR-VIC20-001: true when a protocol frame DTO has a positive
    /// size and a BGRA payload long enough for Width*Height pixels. C64 384x272 is
    /// the default, not the only legal canvas (VIC-20 NTSC is 400x234).
    /// </summary>
    public static bool IsValidFrame(VideoFrameDto? frame)
    {
        if (frame is null || frame.Width <= 0 || frame.Height <= 0 || frame.Bgra is null)
            return false;

        var needed = (long)frame.Width * frame.Height * 4;
        return frame.Bgra.Length >= needed;
    }

    /// <summary>
    /// Copies a packed BGRA canvas (stride = width*4) into a destination that may
    /// have padded rows. Using a destination stride of 384*4 for a 400-wide source
    /// shears the picture; callers must pass the live width.
    /// </summary>
    public static void BlitPackedBgra(
        ReadOnlySpan<byte> packed,
        int width,
        int height,
        Span<byte> dest,
        int destStrideBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(destStrideBytes, width * 4);

        var srcStride = width * 4;
        var neededSrc = srcStride * height;
        if (packed.Length < neededSrc)
            throw new ArgumentException("Packed BGRA buffer is shorter than width*height*4.", nameof(packed));

        var neededDest = destStrideBytes * (height - 1) + srcStride;
        if (dest.Length < neededDest)
            throw new ArgumentException("Destination buffer is shorter than height rows at destStrideBytes.", nameof(dest));

        if (destStrideBytes == srcStride)
        {
            packed[..neededSrc].CopyTo(dest);
            return;
        }

        for (var y = 0; y < height; y++)
            packed.Slice(y * srcStride, srcStride).CopyTo(dest.Slice(y * destStrideBytes, srcStride));
    }

    public VideoSurface()
    {
        Focusable = true;
        HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left;
        VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch;

        _bitmap = CreateBitmap(SourceWidth, SourceHeight);
        FillWithBlank();
    }

    private static WriteableBitmap CreateBitmap(int width, int height)
        => new(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

    private void EnsureBitmap(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;
        if (_pixelWidth == width && _pixelHeight == height)
            return;

        _bitmap.Dispose();
        _pixelWidth = width;
        _pixelHeight = height;
        _bitmap = CreateBitmap(width, height);
        FillWithBlank();
        InvalidateMeasure();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        base.OnPointerPressed(e);
    }

    /// <summary>
    /// Prefer filling the available height (and grow width by display aspect). When the slot is
    /// too narrow, fall back to width-limited sizing. Unconstrained measure uses content-pixel
    /// natural size.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        int contentH = ContentHeight;
        int contentW = _pixelWidth > 0 ? _pixelWidth : SourceWidth;
        double aspect = ComputeDisplayAspect(AspectMode, PixelAspect, contentH, contentW);
        if (aspect <= 0)
            aspect = (double)contentW / Math.Max(1, contentH);

        double naturalH = contentH;
        double naturalW = naturalH * aspect;

        bool finiteW = !double.IsInfinity(availableSize.Width) && !double.IsNaN(availableSize.Width);
        bool finiteH = !double.IsInfinity(availableSize.Height) && !double.IsNaN(availableSize.Height);

        if (!finiteW && !finiteH)
            return new Size(naturalW, naturalH);

        double maxW = finiteW ? Math.Max(0, availableSize.Width) : double.PositiveInfinity;
        double maxH = finiteH ? Math.Max(0, availableSize.Height) : double.PositiveInfinity;

        if (maxW <= 0 || maxH <= 0)
            return new Size(0, 0);

        // Fit aspect box into available: fill the limiting axis (same idea as Xbox geometry).
        if (maxW / maxH > aspect)
        {
            // Slot wider than needed: fill height, shrink width.
            double h = double.IsInfinity(maxH) ? naturalH : maxH;
            return new Size(h * aspect, h);
        }

        double w = double.IsInfinity(maxW) ? naturalW : maxW;
        return new Size(w, w / aspect);
    }

    private void FillWithBlank()
    {
        using var fb = _bitmap.Lock();
        unsafe
        {
            var dst = (byte*)fb.Address;
            var rowBytes = _pixelWidth * 4;
            for (var y = 0; y < _pixelHeight; y++)
            {
                var row = new Span<byte>(dst + (y * fb.RowBytes), rowBytes);
                for (var i = 0; i < rowBytes; i += 4)
                {
                    row[i] = 0;
                    row[i + 1] = 0;
                    row[i + 2] = 0;
                    row[i + 3] = 0xFF;
                }
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
        try
        {
            var packed = EnsurePackedScratch(_pixelWidth * _pixelHeight * 4);
            if (!source.TryCopyFrameInto(sessionId, packed, out var width, out var height, out _))
            {
                if (!source.TryGetFrameGeometry(sessionId, out _, out _, out var bytes) || bytes <= 0)
                    return false;
                packed = EnsurePackedScratch(bytes);
                if (!source.TryCopyFrameInto(sessionId, packed, out width, out height, out _))
                    return false;
            }

            if (width <= 0 || height <= 0)
                return false;

            EnsureBitmap(width, height);
            using var fb = _bitmap.Lock();
            unsafe
            {
                var destLen = fb.RowBytes * height;
                var dest = new Span<byte>((void*)fb.Address, destLen);
                BlitPackedBgra(packed.AsSpan(0, width * height * 4), width, height, dest, fb.RowBytes);
            }

            InvalidateVisual();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private byte[] EnsurePackedScratch(int byteLength)
    {
        if (byteLength <= 0)
            byteLength = SourceWidth * SourceHeight * 4;
        if (_packedScratch is null || _packedScratch.Length < byteLength)
            _packedScratch = new byte[byteLength];
        return _packedScratch;
    }

    public void SetFrame(VideoFrameDto? frame)
    {
        if (!IsValidFrame(frame))
            return;

        try
        {
            EnsureBitmap(frame!.Width, frame.Height);
            using var fb = _bitmap.Lock();
            unsafe
            {
                var destLen = fb.RowBytes * frame.Height;
                var dest = new Span<byte>((void*)fb.Address, destLen);
                BlitPackedBgra(frame.Bgra, frame.Width, frame.Height, dest, fb.RowBytes);
            }

            InvalidateVisual();
        }
        catch
        {
            // Ignore errors
        }
    }

    public override void Render(DrawingContext context)
    {
        // VICE-style aspect ratio handling: each VIC standard has a different composite pixel
        // aspect (FIX-XASPECT-002). FIX-XNTSCFILL-001: crop to written content rows so NTSC
        // does not letterbox its in-frame black band.
        double windowWidth = Bounds.Width;
        double windowHeight = Bounds.Height;

        if (windowWidth <= 0 || windowHeight <= 0)
            return;

        int contentH = ContentHeight;
        if (contentH <= 0 || contentH > _pixelHeight)
            contentH = _pixelHeight;
        double displayAspect = ComputeDisplayAspect(AspectMode, PixelAspect, contentH, _pixelWidth);

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
        var sourceRect = new Rect(0, 0, _pixelWidth, contentH);

        context.DrawImage(_bitmap, sourceRect, destRect);
    }
}
