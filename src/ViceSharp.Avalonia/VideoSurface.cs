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
    private readonly IMachine _machine;

    public VideoSurface(IMachine machine)
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

                // Get machine state
                var state = _machine.GetState();

                // Render based on current state
                // Until VIC-II provides framebuffer, show test pattern
                RenderTestPattern(pixelPtr, state);
            }
        }

        context.DrawImage(_bitmap, new Rect(Bounds.Size));
    }

    private static unsafe void RenderTestPattern(uint* pixelPtr, MachineState state)
    {
        // Simple test pattern: cycling colors based on cycle count
        int colorIndex = (int)(state.Cycle / 1000) % 16;
        uint color = C64Palette.Colors[colorIndex];

        // Fill with alternating pattern
        for (int y = 0; y < 272; y++)
        {
            for (int x = 0; x < 384; x++)
            {
                int idx = y * 384 + x;
                // Checkerboard pattern based on position
                bool checker = ((x / 8) + (y / 8)) % 2 == 0;
                pixelPtr[idx] = checker ? color : C64Palette.Colors[0];
            }
        }
    }
}

/// <summary>
/// C64 color palette (16 colors).
/// </summary>
public static class C64Palette
{
    public static readonly uint[] Colors = new uint[16]
    {
        0x00000000, // 0: Black
        0x00FFFFFF, // 1: White
        0x00D52B2B, // 2: Red
        0x003ED8E8, // 3: Cyan
        0x008B2FBE, // 4: Purple
        0x0038D92B, // 5: Green
        0x002720C8, // 6: Blue
        0x00E5E52B, // 7: Yellow
        0x00D86B2B, // 8: Orange
        0x004B4B4B, // 9: Brown
        0x00A16969, // 10: Light Red
        0x004B4B4B, // 11: Dark Gray
        0x006B6B6B, // 12: Medium Gray
        0x005FD85F, // 13: Light Green
        0x005F5FD8, // 14: Light Blue
        0x009A9A9A, // 15: Light Gray
    };
}
