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

    public VideoSurface(IMachine machine)
    {
        _vic = machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;

        if (_vic != null)
        {
            _vic.FrameCompleted += OnFrameCompleted;
        }

        _bitmap = new WriteableBitmap(
            new PixelSize(384, 272),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);
        
        // Fill with blue initially
        FillWithBlue();
    }

    private void FillWithBlue()
    {
        using var fb = _bitmap.Lock();
        unsafe
        {
            var dst = (uint*)fb.Address;
            var count = 384 * 272;
            for (int i = 0; i < count; i++)
            {
                dst[i] = 0xFFFF0000; // Initial fill color (blue-ish, will be replaced by actual frame)
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
                var size = 384 * 272 * 4;

                if (src.Length >= size)
                {
                    // Copy the framebuffer directly
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
        // Always draw - if no content yet, shows blue from initial fill
        context.DrawImage(_bitmap, new Rect(Bounds.Size));
    }
}
