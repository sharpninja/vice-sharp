using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ViceSharp.Abstractions;

namespace ViceSharp.Avalonia;

public sealed class VideoSurface : Control
{
    private readonly WriteableBitmap _bitmap;
    private readonly IVideoChip? _vic;
    private readonly byte[] _scaledBuffer;
    
    // VICE PAL dimensions: 384x272 visible area, 4:3 aspect ratio
    public const int SourceWidth = 384;
    public const int SourceHeight = 272;
    
    public VideoSurface(IMachine machine)
    {
        _vic = machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;

        if (_vic != null)
        {
            _vic.FrameCompleted += OnFrameCompleted;
        }

        // VICE-style: Use VICE's pixel density (96 DPI = 384 pixels / 4 inches)
        _bitmap = new WriteableBitmap(
            new PixelSize(SourceWidth, SourceHeight),
            new Vector(96, 96),  // VICE uses square-ish pixels at 96 DPI
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);
        
        _scaledBuffer = new byte[SourceWidth * SourceHeight * 4];
        
        // Fill with blue initially (border color)
        FillWithBlue();
    }

    private void FillWithBlue()
    {
        using var fb = _bitmap.Lock();
        unsafe
        {
            var dst = (uint*)fb.Address;
            var count = SourceWidth * SourceHeight;
            for (int i = 0; i < count; i++)
            {
                dst[i] = 0xFFFF0000; // Blue in BGRA
            }
        }
    }

    private void OnFrameCompleted(object? sender, EventArgs e)
    {
        if (_vic == null) return;

        try
        {
            using var fb = _bitmap.Lock();
            unsafe
            {
                var src = _vic.FrameBuffer;
                var dst = (byte*)fb.Address;
                var size = SourceWidth * SourceHeight * 4;

                if (src.Length >= size)
                {
                    fixed (byte* pSrc = src)
                    {
                        Buffer.MemoryCopy(pSrc, dst, size, size);
                    }
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
        // C64 display is 4:3, but window may be any size
        // Calculate centered rect maintaining 4:3 aspect ratio
        
        double targetAspect = 4.0 / 3.0;  // C64 standard aspect ratio
        double windowWidth = Bounds.Width;
        double windowHeight = Bounds.Height;
        
        if (windowWidth <= 0 || windowHeight <= 0)
            return;
            
        double windowAspect = windowWidth / windowHeight;
        
        double drawWidth, drawHeight;
        
        if (windowAspect > targetAspect)
        {
            // Window is wider than 4:3, fit to height
            drawHeight = windowHeight;
            drawWidth = windowHeight * targetAspect;
        }
        else
        {
            // Window is taller than 4:3, fit to width
            drawWidth = windowWidth;
            drawHeight = windowWidth / targetAspect;
        }
        
        double x = (windowWidth - drawWidth) / 2;
        double y = (windowHeight - drawHeight) / 2;
        
        var destRect = new Rect(x, y, drawWidth, drawHeight);
        
        context.DrawImage(_bitmap, new Rect(0, 0, SourceWidth, SourceHeight), destRect);
    }
}
