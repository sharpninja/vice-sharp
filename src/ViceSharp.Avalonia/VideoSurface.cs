using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ViceSharp.Architectures;

namespace ViceSharp.Avalonia;

public sealed class VideoSurface : Control
{
    private readonly WriteableBitmap _bitmap;
    private readonly C64Machine _machine;

    public VideoSurface(C64Machine machine)
    {
        _machine = machine;
        _bitmap = new WriteableBitmap(
            new PixelSize(384, 272),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);
    }

    public override void Render(DrawingContext context)
    {
        using (var framebuffer = _bitmap.Lock())
        {
            unsafe
            {
                uint* pixelPtr = (uint*)framebuffer.Address;
                
                // Read actual framebuffer from C64 machine
                ReadOnlySpan<byte> frame = _machine.ScreenBuffer;
                
                for (int i = 0; i < 384 * 272; i++)
                {
                    byte colorIndex = frame[i];
                    pixelPtr[i] = C64Palette.Colors[colorIndex];
                }
            }
        }

        context.DrawImage(_bitmap, new Rect(Bounds.Size));
    }
}
