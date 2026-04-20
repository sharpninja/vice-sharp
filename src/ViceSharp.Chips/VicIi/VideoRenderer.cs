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

    public readonly byte[] FrameBuffer = new byte[ScreenWidth * ScreenHeight * 4];

    private readonly Mos6569 _vic;
    private readonly IBus _bus;
    private int _currentLine;
    private int _cycleInLine;

    // C64 palette in BGRA format (byte order: B, G, R, A)
    private static readonly uint[] Palette = new uint[16]
    {
        0xFF000000, // 0: Black
        0xFFFFFFFF, // 1: White  
        0xFFD52B2B, // 2: Red
        0xFFD8CEE8, // 3: Cyan
        0xFF8E3CBE, // 4: Purple
        0xFF4DAC2B, // 5: Green
        0xFF282CC8, // 6: Blue
        0xFF2EF171, // 7: Yellow
        0xFF4B8E29, // 8: Orange
        0xFF6B6B00, // 9: Brown
        0xFF6B6BCD, // 10: Light Red
        0xFF4B4B4B, // 11: Dark Gray
        0xFF6B6B6B, // 12: Medium Gray
        0xFF5FD85F, // 13: Light Green
        0xFFD85F6B, // 14: Light Blue
        0xFF9A9A9A, // 15: Light Gray
    };

    public VideoRenderer(Mos6569 vic, IBus bus)
    {
        _vic = vic;
        _bus = bus;
    }

    /// <summary>
    /// Step video pipeline one cycle
    /// </summary>
    public void Tick()
    {
        _cycleInLine++;

        if (_cycleInLine >= PalCyclesPerLine)
        {
            _cycleInLine = 0;
            RenderScanline();
            _currentLine++;

            if (_currentLine >= PalTotalLines)
            {
                _currentLine = 0;
                FrameCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void RenderScanline()
    {
        if (_currentLine < PalVisibleLines)
        {
            int y = _currentLine;
            Span<byte> line = FrameBuffer.AsSpan(y * ScreenWidth * 4, ScreenWidth * 4);
            RenderLine(line, y);
        }
    }

    private void RenderLine(Span<byte> lineBuffer, int lineNumber)
    {
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
            for (int x = 0; x < ScreenWidth; x++)
            {
                int offset = x * 4;
                lineBuffer[offset] = (byte)(borderPixel >> 0);
                lineBuffer[offset + 1] = (byte)(borderPixel >> 8);
                lineBuffer[offset + 2] = (byte)(borderPixel >> 16);
                lineBuffer[offset + 3] = 0xFF;
            }
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
            
            lineBuffer[offset] = (byte)(pixel >> 0);
            lineBuffer[offset + 1] = (byte)(pixel >> 8);
            lineBuffer[offset + 2] = (byte)(pixel >> 16);
            lineBuffer[offset + 3] = 0xFF;
        }
    }

    public event EventHandler? FrameCompleted;

    public void Reset()
    {
        _currentLine = 0;
        _cycleInLine = 0;
        Array.Clear(FrameBuffer);
    }
}
