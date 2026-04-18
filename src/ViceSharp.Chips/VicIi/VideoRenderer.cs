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
    private int _currentLine;
    private int _cycleInLine;

    public VideoRenderer(Mos6569 vic)
    {
        _vic = vic;
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
            // Render single scanline
            int y = _currentLine;
            Span<byte> line = FrameBuffer.AsSpan(y * ScreenWidth * 4, ScreenWidth * 4);
            RenderLine(line, y);
        }
    }

    private void RenderLine(Span<byte> lineBuffer, int lineNumber)
    {
        // Border color
        byte borderColor = 6;

        for (int x = 0; x < ScreenWidth; x++)
        {
            int offset = x * 4;
            lineBuffer[offset + 0] = C64Palette.Rgb[borderColor, 2];
            lineBuffer[offset + 1] = C64Palette.Rgb[borderColor, 1];
            lineBuffer[offset + 2] = C64Palette.Rgb[borderColor, 0];
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

/// <summary>
/// Standard VIC-II color palette
/// </summary>
public static class C64Palette
{
    // Index, Red, Green, Blue
    public static readonly byte[,] Rgb = new byte[16, 3]
    {
        { 0x00, 0x00, 0x00 }, // 0: Black
        { 0xFF, 0xFF, 0xFF }, // 1: White
        { 0x81, 0x33, 0x38 }, // 2: Red
        { 0x75, 0xCE, 0xC8 }, // 3: Cyan
        { 0x8E, 0x3C, 0x97 }, // 4: Purple
        { 0x56, 0xAC, 0x4D }, // 5: Green
        { 0x2E, 0x2C, 0x9B }, // 6: Blue
        { 0xED, 0xF1, 0x71 }, // 7: Yellow
        { 0x8E, 0x50, 0x29 }, // 8: Orange
        { 0x55, 0x38, 0x00 }, // 9: Brown
        { 0xC4, 0x6C, 0x71 }, // A: Light Red
        { 0x4A, 0x4A, 0x4A }, // B: Dark Grey
        { 0x7B, 0x7B, 0x7B }, // C: Medium Grey
        { 0xA9, 0xFF, 0x9F }, // D: Light Green
        { 0x70, 0x6D, 0xEB }, // E: Light Blue
        { 0xB2, 0xB2, 0xB2 }  // F: Light Grey
    };
}