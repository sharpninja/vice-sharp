using System.Buffers;
using ViceSharp.Abstractions;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// PAL/NTSC Video Renderer for MOS 6569 VIC-II
/// </summary>
public sealed class VideoRenderer
{
    public const int ScreenWidth = 384;
    public const int ScreenHeight = 272;
    public const int PalCyclesPerLine = 63;
    public const int PalTotalLines = 312;
    public const int PalVisibleLines = 272;
    
    /// <summary>
    /// Pixel aspect ratios by video standard (from VICE)
    /// These are the horizontal stretch factors - multiply width to get correct display aspect
    /// </summary>
    public static float GetPixelAspectRatio(Mos6569.TvSystem system) => system switch
    {
        Mos6569.TvSystem.PAL => 0.93650794f,   // PAL pixels are slightly taller
        Mos6569.TvSystem.PALN => 0.90769231f, // PAL-N pixels are taller
        Mos6569.TvSystem.NTSC => 0.75000000f,  // NTSC pixels are much taller
        _ => 1.0f
    };

    public readonly byte[] FrameBuffer = new byte[ScreenWidth * ScreenHeight * 4];

    private readonly Mos6569 _vic;
    private readonly IBus _bus;
    private int _currentFrame;

    // C64 palette in BGRA format - built from VicPalette colors
    // Format: 0xAABBGGRR (Alpha, Blue, Green, Red)
    private static readonly uint[] Palette = new uint[16];

    static VideoRenderer()
    {
        // Initialize palette from VicPalette to BGRA format
        // FrameBuffer stores pixels as BGRA: [offset 0=B, offset 1=G, offset 2=R, offset 3=A]
        // BitConverter.ToUInt32 reads bytes as: [0]=bits0-7, [1]=bits8-15, [2]=bits16-23, [3]=bits24-31
        // So we need: B at bits0-7, G at bits8-15, R at bits16-23, A at bits24-31
        for (int i = 0; i < 16; i++)
        {
            var c = VicPalette.Colors[i];
            Palette[i] = 0xFF000000u | ((uint)c.B) | ((uint)c.G << 8) | ((uint)c.R << 16);
        }
    }

    public VideoRenderer(Mos6569 vic, IBus bus)
    {
        _vic = vic;
        _bus = bus;
    }

    /// <summary>
    /// Step video pipeline one cycle - called from VIC Tick()
    /// Renders a full scanline when cycle hits end of line
    /// </summary>
    public void Tick()
    {
        // Check if we've completed a line (63 cycles for PAL)
        if (_vic.RasterX == 0 && _vic.CurrentRasterLine > 0)
        {
            RenderScanline(_vic.CurrentRasterLine - 1);
        }

        // Check for frame complete
        if (_vic.CurrentRasterLine == 0 && _currentFrame > 0)
        {
            FrameCompleted?.Invoke(this, EventArgs.Empty);
        }
        _currentFrame++;
    }

    private void RenderScanline(int lineNumber)
    {
        if (lineNumber < 0 || lineNumber >= PalVisibleLines)
            return;

        int y = lineNumber;
        Span<byte> line = FrameBuffer.AsSpan(y * ScreenWidth * 4, ScreenWidth * 4);
        
        // Get border and background colors from VIC registers
        byte borderColor = _vic.BorderColor;
        byte backgroundColor = _vic.BackgroundColor;
        
        uint borderPixel = Palette[borderColor & 0x0F];
        uint bgPixel = Palette[backgroundColor & 0x0F];

        // Calculate which raster line within a character cell (0-7)
        int charRow = lineNumber % 8;
        
        // Calculate if we're in visible vertical area (lines 51-250 are visible)
        int visLine = lineNumber - 51;
        if (visLine < 0 || visLine >= 200)
        {
            // Outside visible area - draw border
            DrawBorder(line, borderPixel);
            return;
        }

        for (int x = 0; x < ScreenWidth; x++)
        {
            int offset = x * 4;
            uint pixel;
            
            // Check if we're in the side border (left 24 pixels, right 24 pixels)
            if (x < 24 || x >= 360)
            {
                // Side border
                pixel = borderPixel;
            }
            else
            {
                // Inside the screen area (320 pixels wide)
                int screenX = x - 24;
                
                // Check if in main screen area (40 chars * 8 pixels)
                if (screenX < 320)
                {
                    // Main screen area - render characters
                    int col = screenX / 8;
                    int charX = screenX % 8;
                    
                    // Get screen memory index (40 chars per row)
                    int screenIndex = visLine / 8 * 40 + col;
                    
                    // Read character code from screen RAM at $0400
                    ushort screenAddr = (ushort)(0x0400 + screenIndex);
                    byte charCode = _bus.Read(screenAddr);
                    
                    // Read color from color RAM at $D800
                    ushort colorAddr = (ushort)(0xD800 + screenIndex);
                    byte colorCode = _bus.Read(colorAddr);
                    
                    // Character generator base is at $D000, each char is 8 bytes
                    ushort charAddr = (ushort)(0xD000 + charCode * 8 + charRow);
                    byte charData = _bus.Read(charAddr);
                    
                    // Get bit position (bit 7 is leftmost pixel)
                    int bitPos = 7 - charX;
                    byte bit = (byte)((charData >> bitPos) & 0x01);
                    
                    // Foreground color (4-bit, convert to 0-15)
                    byte fgColor = (byte)(colorCode & 0x0F);
                    
                    // Select pixel: foreground or background
                    uint fgPixel = Palette[fgColor];
                    pixel = bit != 0 ? fgPixel : bgPixel;
                }
                else
                {
                    // Right extended border
                    pixel = borderPixel;
                }
            }
            
            line[offset] = (byte)(pixel >> 0);
            line[offset + 1] = (byte)(pixel >> 8);
            line[offset + 2] = (byte)(pixel >> 16);
            line[offset + 3] = 0xFF;
        }
    }

    private void DrawBorder(Span<byte> line, uint borderPixel)
    {
        for (int x = 0; x < ScreenWidth; x++)
        {
            int offset = x * 4;
            line[offset] = (byte)(borderPixel >> 0);
            line[offset + 1] = (byte)(borderPixel >> 8);
            line[offset + 2] = (byte)(borderPixel >> 16);
            line[offset + 3] = 0xFF;
        }
    }

    /// <summary>
    /// Force a full frame render (for initial display)
    /// </summary>
    public void RenderFullFrame()
    {
        for (int y = 0; y < PalVisibleLines; y++)
        {
            RenderScanline(y);
        }
    }

    public event EventHandler? FrameCompleted;

    public void Reset()
    {
        _currentFrame = 0;
        Array.Clear(FrameBuffer);
    }
}
