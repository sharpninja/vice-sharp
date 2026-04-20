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
    private readonly object _lock = new();

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
                var size = fb.Size.Bytes;

                if (src.Length >= size)
                {
                    fixed (byte* pSrc = src)
                    {
                        Buffer.MemoryCopy(pSrc, dst, size, size);
                    }
                }
            }

            Dispatcher.UIThread.Post(InvalidateVisual);
        }
        catch
        {
            // Ignore errors during frame copy
        }
    }

    public override void Render(DrawingContext context)
    {
        context.DrawImage(_bitmap, new Rect(Bounds.Size));
    }
}
