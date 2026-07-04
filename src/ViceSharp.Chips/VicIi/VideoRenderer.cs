using System.Runtime.InteropServices;
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
    public const int PalFirstVisibleRasterLine = 15;
    
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

    // VIC-II palette in BGRA format - built from VicPalette colors
    // Format: 0xAABBGGRR (Alpha, Blue, Green, Red)
    private static readonly uint[] Palette = new uint[16];

    // PLAN-VICEPARITY-001 V3: true only during NotifyLineCompleted (clock-driven live
    // rendering). False during RenderFullFrame (synthetic path). Controls whether
    // RenderStandardTextLineNoSprites and RenderBackgroundPixel read from
    // PixelSequencer.LineIndices (cycle-accurate) or the geometric fallback path.
    private bool _isLiveRender;

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

    public VideoRenderer(Mos6569 vic)
    {
        _vic = vic;
    }

    public VideoRenderer(Mos6569 vic, IBus _)
        : this(vic)
    {
    }

    // PERF-RENDER-001: called once per completed scanline from Mos6569.Tick() at line-wrap,
    // eliminating 19,344 no-op per-cycle Tick() calls per PAL frame.
    // BACKFILL-VIDEO-001 / FR-VIC-002 / FR-VIC-003 / FR-VIC-008 / TEST-VIC-001.
    internal void NotifyLineCompleted(int completedLine)
    {
        _isLiveRender = true;
        RenderRasterLine(completedLine);
        _isLiveRender = false;
    }

    // PERF-RENDER-001: called once per frame at frame-wrap from Mos6569.Tick().
    internal void NotifyFrameCompleted()
    {
        FrameCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void RenderRasterLine(int lineNumber)
    {
        if (!TryMapRasterLineToFrameY(lineNumber, out var y))
            return;

        Span<byte> line = FrameBuffer.AsSpan(y * ScreenWidth * 4, ScreenWidth * 4);
        
        // Get border and background colors from VIC registers
        byte borderColor = _vic.BorderColor;
        byte backgroundColor = _vic.BackgroundColor;
        
        uint borderPixel = Palette[borderColor & 0x0F];
        uint bgPixel = Palette[backgroundColor & 0x0F];

        if (_vic.IsRasterLineVerticalBorderActive(lineNumber) ||
            !_vic.IsRasterLineHorizontalDisplayOpen(lineNumber))
        {
            DrawBorder(line, borderPixel);
            return;
        }

        // PLAN-VICRENDER-001: precompute the per-pixel background colour for this line so mid-line
        // $D021 changes (the demo's background bars) render in the display area, not one colour/line.
        PopulateBackgroundLine();

        int leftBorderPixel = _vic.LeftBorderPixel;
        int rightBorderPixel = _vic.RightBorderEndPixel;
        bool leftBorderOpen = _vic.IsRasterLineLeftBorderOpen(lineNumber);
        bool rightBorderOpen = _vic.IsRasterLineRightBorderOpen(lineNumber);
        int columns = _vic.Columns == Mos6569.ColumnMode.Wide40 ? 40 : 38;
        int screenWidth = columns * 8;
        bool hasDisplayCell = _vic.TryMapRasterLineToDisplayCell(lineNumber, out var screenRow, out var charRow);

        // PERF-RENDER-002: cache DisplayModeSelection once per line rather than
        // evaluating the 3-register switch expression for every pixel (384x per line).
        var displayMode = _vic.DisplayModeSelection;

        if (displayMode == Mos6569.VicIIDisplayMode.StandardText && _vic.Peek(0xD015) == 0)
        {
            RenderStandardTextLineNoSprites(
                line,
                leftBorderPixel,
                rightBorderPixel,
                leftBorderOpen,
                rightBorderOpen,
                screenWidth,
                hasDisplayCell,
                screenRow,
                charRow,
                columns,
                bgPixel,
                borderPixel);
            return;
        }

        var pixels = MemoryMarshal.Cast<byte, uint>(line);
        for (int x = 0; x < ScreenWidth; x++)
        {
            var background = RenderBackgroundPixel(
                x,
                lineNumber,
                leftBorderPixel,
                rightBorderPixel,
                leftBorderOpen,
                rightBorderOpen,
                screenWidth,
                hasDisplayCell,
                screenRow,
                charRow,
                columns,
                bgPixel,
                borderPixel,
                displayMode);
            uint pixel = background.Pixel;
            
            if (TryGetSpritePixel(x, lineNumber, background.IsForeground, out var spritePixel))
            {
                pixel = spritePixel;
            }

            pixels[x] = pixel;
        }
    }

    private void RenderStandardTextLineNoSprites(
        Span<byte> line,
        int leftBorderPixel,
        int rightBorderPixel,
        bool leftBorderOpen,
        bool rightBorderOpen,
        int screenWidth,
        bool hasDisplayCell,
        int screenRow,
        int charRow,
        int columns,
        uint bgPixel,
        uint borderPixel)
    {
        var pixels = MemoryMarshal.Cast<byte, uint>(line);
        // PLAN-VICRENDER-001: segmented border fill (mid-line $D020 changes) instead of one colour.
        FillBorderSegmented(pixels);

        if (leftBorderOpen)
        {
            FillBackground(pixels, 0, leftBorderPixel);
        }

        var displayStart = leftBorderPixel;
        var displayEnd = Math.Min(rightBorderPixel, leftBorderPixel + screenWidth);
        if (displayEnd > displayStart && _isLiveRender)
        {
            // PLAN-VICEPARITY-001 V3 FR-VIC-DRAW-GFX / FR-VIC-XSCROLL: live clock-driven
            // rendering uses PixelSequencer.LineIndices (ports draw_graphics8 from
            // vicii-draw-cycle.c). Correct xscroll_pipe 2-cycle pipeline delay: col0 at
            // frame pixel 32 (xscroll=0). Synthetic path (RenderFullFrame) falls through
            // to the geometric branches below which read VIC state on-demand.
            var lineIndices = _vic.PixelSequencer.LineIndices;
            int bufOffset = FirstVisibleRasterX * 8; // = 96
            for (int x = displayStart; x < displayEnd; x++)
            {
                int idx = x + bufOffset;
                pixels[x] = Palette[lineIndices[idx]];
            }
        }
        else if (displayEnd > displayStart && !hasDisplayCell)
        {
            // Lower fine-scroll overflow has no matrix row to render; display window shows background.
            FillBackground(pixels, displayStart, displayEnd - displayStart);
        }
        else if (displayEnd > displayStart)
        {
            for (var col = 0; col < columns; col++)
            {
                var x = displayStart + col * 8;
                if (x >= displayEnd)
                    break;
                var screenIndex = screenRow * columns + col;
                ReadMatrixCell(screenRow, col, screenIndex, out var charCode, out var colorCode);
                byte charData = ReadCharacterRow(charCode, charRow);
                uint fgPixel = Palette[colorCode & 0x0F];
                var pixelsInCell = Math.Min(8, displayEnd - x);
                for (var charX = 0; charX < pixelsInCell; charX++)
                {
                    pixels[x + charX] = ((charData >> (7 - charX)) & 0x01) != 0
                        ? fgPixel
                        : _backgroundLine[x + charX];
                }
            }
        }

        if (rightBorderOpen)
        {
            FillBackground(pixels, rightBorderPixel, ScreenWidth - rightBorderPixel);
        }
    }

    private PixelSample RenderBackgroundPixel(
        int x,
        int rasterLine,
        int leftBorderPixel,
        int rightBorderPixel,
        bool leftBorderOpen,
        bool rightBorderOpen,
        int screenWidth,
        bool hasDisplayCell,
        int screenRow,
        int charRow,
        int columns,
        uint bgPixel,
        uint borderPixel,
        Mos6569.VicIIDisplayMode displayMode)
    {
        bool inLeftBorder = x < leftBorderPixel;
        bool inRightBorder = x >= rightBorderPixel;

        // PLAN-VICRENDER-001: per-pixel background colour (mid-line $D021 bars) instead of one/line.
        uint bgPixelAtX = _backgroundLine[x];

        if ((inLeftBorder && !leftBorderOpen) || (inRightBorder && !rightBorderOpen))
        {
            return new PixelSample(borderPixel, false);
        }

        if (inLeftBorder || inRightBorder)
        {
            return new PixelSample(bgPixelAtX, false);
        }

        int screenX = x - leftBorderPixel;
        if (screenX >= screenWidth)
        {
            return new PixelSample(borderPixel, false);
        }

        // PLAN-VICEPARITY-001 V3 FR-VIC-DRAW-GFX / FR-VIC-XSCROLL: during live clock-driven
        // rendering (NotifyLineCompleted) use PixelSequencer.LineIndices for cycle-accurate
        // color and priority. During synthetic renders (RenderFullFrame), fall through to the
        // geometric path so RenderFullFrame-based tests continue to read VIC state on-demand.
        if (_isLiveRender)
        {
            int idx = x + FirstVisibleRasterX * 8;
            byte paletteIndex = _vic.PixelSequencer.LineIndices[idx];
            bool isForeground  = _vic.PixelSequencer.LinePriority[idx] != 0;
            return new PixelSample(Palette[paletteIndex], isForeground);
        }

        if (!hasDisplayCell)
        {
            return new PixelSample(bgPixelAtX, false);
        }

        int col = screenX / 8;
        int charX = screenX % 8;
        int screenIndex = screenRow * columns + col;

        ReadMatrixCell(screenRow, col, screenIndex, out var charCode, out var colorCode);

        // BACKFILL-VIDEO-001 / FR-VIC-002 / FR-VIC-003 / FR-VIC-008 /
        // TEST-VIC-001: follow the VICE vicii-draw-cycle.c mode color table.
        // PERF-RENDER-002: displayMode cached once per line by RenderRasterLine.
        return displayMode switch
        {
            Mos6569.VicIIDisplayMode.StandardText => RenderStandardTextPixel(charCode, colorCode, charRow, charX, bgPixelAtX),
            Mos6569.VicIIDisplayMode.MulticolorText => RenderMulticolorTextPixel(charCode, colorCode, charRow, charX, bgPixelAtX),
            Mos6569.VicIIDisplayMode.ExtendedColor => RenderExtendedColorPixel(charCode, colorCode, charRow, charX),
            Mos6569.VicIIDisplayMode.StandardBitmap => RenderStandardBitmapPixel(screenIndex, colorCode: charCode, charRow, charX),
            Mos6569.VicIIDisplayMode.MulticolorBitmap => RenderMulticolorBitmapPixel(screenIndex, screenCode: charCode, colorCode, charRow, charX, bgPixelAtX),
            _ => new PixelSample(Palette[0], _vic.IsGraphicsPixelForegroundForSpritePriority(x, rasterLine)),
        };
    }

    private void ReadMatrixCell(int row, int col, int screenIndex, out byte screenCode, out byte colorCode)
    {
        if (_vic.TryReadRenderMatrixCell(row, col, out screenCode, out colorCode))
            return;

        if (_vic.TryReadVideoMatrixLatch(col, out screenCode, out colorCode))
            return;

        screenCode = _vic.ReadVideoMemory((ushort)(_vic.ScreenMemoryBase + screenIndex));
        colorCode = _vic.ReadVideoMemory((ushort)(0xD800 + screenIndex));
    }

    private PixelSample RenderStandardTextPixel(byte charCode, byte colorCode, int charRow, int charX, uint bgPixel)
    {
        byte charData = ReadCharacterRow(charCode, charRow);
        bool isForeground = ((charData >> (7 - charX)) & 0x01) != 0;
        return isForeground
            ? new PixelSample(Palette[colorCode & 0x0F], true)
            : new PixelSample(bgPixel, false);
    }

    private PixelSample RenderMulticolorTextPixel(byte charCode, byte colorCode, int charRow, int charX, uint bgPixel)
    {
        byte charData = ReadCharacterRow(charCode, charRow);
        if ((colorCode & 0x08) == 0)
        {
            bool isForeground = ((charData >> (7 - charX)) & 0x01) != 0;
            return isForeground
                ? new PixelSample(Palette[colorCode & 0x07], true)
                : new PixelSample(bgPixel, false);
        }

        int pair = ReadMulticolorPair(charData, charX);
        return pair switch
        {
            0 => new PixelSample(bgPixel, false),
            1 => new PixelSample(Palette[_vic.AuxiliaryColor & 0x0F], false),
            2 => new PixelSample(Palette[_vic.Peek(0xD023) & 0x0F], true),
            _ => new PixelSample(Palette[colorCode & 0x07], true),
        };
    }

    private PixelSample RenderExtendedColorPixel(byte screenCode, byte colorCode, int charRow, int charX)
    {
        byte charData = ReadCharacterRow((byte)(screenCode & 0x3F), charRow);
        bool isForeground = ((charData >> (7 - charX)) & 0x01) != 0;
        if (isForeground)
        {
            return new PixelSample(Palette[colorCode & 0x0F], true);
        }

        int backgroundRegister = (screenCode >> 6) & 0x03;
        byte backgroundColor = backgroundRegister switch
        {
            0 => _vic.BackgroundColor,
            1 => _vic.AuxiliaryColor,
            2 => (byte)(_vic.Peek(0xD023) & 0x0F),
            _ => (byte)(_vic.Peek(0xD024) & 0x0F),
        };
        return new PixelSample(Palette[backgroundColor & 0x0F], false);
    }

    private PixelSample RenderStandardBitmapPixel(int screenIndex, byte colorCode, int charRow, int charX)
    {
        byte bitmapData = ReadBitmapRow(screenIndex, charRow);
        bool isForeground = ((bitmapData >> (7 - charX)) & 0x01) != 0;
        byte paletteIndex = isForeground
            ? (byte)(colorCode >> 4)
            : (byte)(colorCode & 0x0F);
        return new PixelSample(Palette[paletteIndex & 0x0F], isForeground);
    }

    private PixelSample RenderMulticolorBitmapPixel(int screenIndex, byte screenCode, byte colorCode, int charRow, int charX, uint bgPixel)
    {
        byte bitmapData = ReadBitmapRow(screenIndex, charRow);
        int pair = ReadMulticolorPair(bitmapData, charX);
        return pair switch
        {
            0 => new PixelSample(bgPixel, false),
            1 => new PixelSample(Palette[(screenCode >> 4) & 0x0F], false),
            2 => new PixelSample(Palette[screenCode & 0x0F], true),
            _ => new PixelSample(Palette[colorCode & 0x0F], true),
        };
    }

    private byte ReadCharacterRow(byte charCode, int charRow)
    {
        ushort charAddr = (ushort)(_vic.CharacterBase + charCode * 8 + charRow);
        return _vic.ReadVideoMemory(charAddr);
    }

    private byte ReadBitmapRow(int screenIndex, int charRow)
    {
        ushort bitmapAddr = (ushort)(_vic.BitmapPointerBase + screenIndex * 8 + charRow);
        return _vic.ReadVideoMemory(bitmapAddr);
    }

    private static int ReadMulticolorPair(byte source, int charX)
    {
        int pairShift = 6 - ((charX / 2) * 2);
        return (source >> pairShift) & 0x03;
    }

    // BACKFILL-VIDEO-001 / FR-VIC-004 / FR-VIC-007 / TEST-VIC-001:
    // sprite composition must respect sprite priority and current border
    // visibility state before a pixel reaches the framebuffer.
    private bool TryGetSpritePixel(int x, int rasterLine, bool backgroundIsForeground, out uint pixel)
    {
        pixel = 0;

        if (!_vic.CanRenderSpritePixelAt(x, rasterLine))
        {
            return false;
        }

        byte enabled = _vic.Peek(0xD015);
        if (enabled == 0)
        {
            return false;
        }

        for (int sprite = 0; sprite < 8; sprite++)
        {
            if ((enabled & (1 << sprite)) == 0)
            {
                continue;
            }

            if (!TryGetSpritePaletteIndex(sprite, x, rasterLine, out var paletteIndex))
            {
                continue;
            }

            if (_vic.GetSpritePriority(sprite) == Mos6569.SpritePriority.Behind && backgroundIsForeground)
            {
                // PLAN-VICEPARITY-001 FR-VIC-SPRITE-PRIORITY AC-05: winner is behind
                // AND graphics pixel is foreground -> output background (return false),
                // not fall through to a lower-priority in-front sprite. VICE
                // vicii-draw-cycle.c:401-419 applies the priority gate to the winner
                // only and does not write render_buffer[i] on this path.
                return false;
            }

            pixel = Palette[paletteIndex & 0x0F];
            return true;
        }

        return false;
    }

    private bool TryGetSpritePaletteIndex(int sprite, int x, int rasterLine, out byte paletteIndex)
    {
        paletteIndex = 0;

        int spriteX = _vic.GetSpriteX(sprite);
        int spriteY = _vic.GetSpriteY(sprite);
        bool expandedX = _vic.GetSpriteExpansionX(sprite) == Mos6569.SpriteExpansion.Double;
        bool expandedY = _vic.GetSpriteExpansionY(sprite) == Mos6569.SpriteExpansion.Double;
        bool multicolor = _vic.GetSpriteColorMode(sprite) == Mos6569.SpriteColorMode.Multi;
        int width = expandedX ? 48 : 24;
        int height = expandedY ? 42 : 21;

        int localX = x - spriteX;
        int localY = rasterLine - spriteY;
        if (localX < 0 || localX >= width || localY < 0 || localY >= height)
        {
            return false;
        }

        int sourceX = expandedX ? localX / 2 : localX;
        int sourceY = expandedY ? localY / 2 : localY;
        byte pointer = _vic.ReadVideoMemory((ushort)(_vic.ScreenMemoryBase + 0x03F8 + sprite));
        ushort baseAddress = (ushort)(pointer * 64 + sourceY * 3);
        byte row0 = _vic.ReadVideoMemory(baseAddress);
        byte row1 = _vic.ReadVideoMemory((ushort)(baseAddress + 1));
        byte row2 = _vic.ReadVideoMemory((ushort)(baseAddress + 2));

        if (multicolor)
        {
            int evenSource = sourceX & ~1;
            int byteIndex = evenSource / 8;
            int bitIndex = 7 - (evenSource % 8);
            byte source = byteIndex switch
            {
                0 => row0,
                1 => row1,
                _ => row2,
            };
            int pair = (source >> (bitIndex - 1)) & 0x03;
            if (pair == 0)
            {
                return false;
            }

            paletteIndex = pair switch
            {
                1 => (byte)(_vic.Peek(0xD025) & 0x0F),
                2 => _vic.GetSpriteColor(sprite),
                _ => (byte)(_vic.Peek(0xD026) & 0x0F),
            };
            return true;
        }

        int rowByteIndex = sourceX / 8;
        int rowBitIndex = 7 - (sourceX % 8);
        byte rowByte = rowByteIndex switch
        {
            0 => row0,
            1 => row1,
            _ => row2,
        };
        if (((rowByte >> rowBitIndex) & 0x01) == 0)
        {
            return false;
        }

        paletteIndex = _vic.GetSpriteColor(sprite);
        return true;
    }

    private readonly record struct PixelSample(uint Pixel, bool IsForeground);

    private void DrawBorder(Span<byte> line, uint borderPixel)
    {
        // PLAN-VICRENDER-001: segmented border fill (mid-line $D020 changes) instead of one colour.
        FillBorderSegmented(MemoryMarshal.Cast<byte, uint>(line));
    }

    private static void FillPixels(Span<uint> pixels, int start, int length, uint pixel)
    {
        if (length <= 0)
        {
            return;
        }

        pixels.Slice(start, length).Fill(pixel);
    }

    // PLAN-VICRENDER-001: frame pixel 0 corresponds to ~RasterX 12 (the display window starts at
    // frame pixel 24 = LeftBorderPixel, which is the first g-access at ~RasterX 15; 8 pixels per
    // cycle). A $D020 write at in-line cycle C therefore takes effect from this frame pixel; a
    // late-in-line write (RasterX ~60) maps past the right edge and so carries into the next line,
    // which is exactly what places the raster bar on the correct scanline.
    private const int FirstVisibleRasterX = 12;

    private static int RasterXToFramePixel(int rasterX)
    {
        int px = (rasterX - FirstVisibleRasterX) * 8;
        if (px < 0)
        {
            return 0;
        }

        return px > ScreenWidth ? ScreenWidth : px;
    }

    // PLAN-VICRENDER-001: fill the whole scanline with the border colour(s), honouring mid-line
    // $D020 changes so cycle-stable raster bars render on the correct lines. Fast path (a single
    // Fill) when the line had no mid-line change, which is the overwhelmingly common case.
    private void FillBorderSegmented(Span<uint> pixels)
    {
        int count = _vic.BorderChangeCount;
        if (count == 0)
        {
            // Fast path (no mid-line change): the whole line is the current border colour,
            // identical to the pre-PLAN-VICRENDER-001 single-fill behaviour (zero change).
            pixels.Fill(Palette[_vic.BorderColor & 0x0F]);
            return;
        }

        uint colour = Palette[_vic.BorderEntryColour & 0x0F];
        int x = 0;
        for (int i = 0; i < count; i++)
        {
            _vic.GetBorderChange(i, out int rasterX, out byte c);
            int px = RasterXToFramePixel(rasterX);
            if (px > x)
            {
                FillPixels(pixels, x, px - x, colour);
                x = px;
            }

            colour = Palette[c & 0x0F];
        }

        if (x < pixels.Length)
        {
            FillPixels(pixels, x, pixels.Length - x, colour);
        }
    }

    // PLAN-VICRENDER-001: per-pixel background colour for the current scanline, honouring mid-line
    // $D021 changes (the demo writes $D020 AND $D021 per bar, so the display-area background bands
    // too). Precomputed once per line into _backgroundLine; the display render reads
    // _backgroundLine[x] instead of a single colour. Fast path (uniform fill) when unchanged.
    private readonly uint[] _backgroundLine = new uint[ScreenWidth];

    private void PopulateBackgroundLine()
    {
        var span = _backgroundLine.AsSpan();
        int count = _vic.BackgroundChangeCount;
        if (count == 0)
        {
            span.Fill(Palette[_vic.BackgroundColor & 0x0F]);
            return;
        }

        uint colour = Palette[_vic.BackgroundEntryColour & 0x0F];
        int x = 0;
        for (int i = 0; i < count; i++)
        {
            _vic.GetBackgroundChange(i, out int rasterX, out byte c);
            int px = RasterXToFramePixel(rasterX);
            if (px > x)
            {
                span.Slice(x, px - x).Fill(colour);
                x = px;
            }

            colour = Palette[c & 0x0F];
        }

        if (x < span.Length)
        {
            span.Slice(x, span.Length - x).Fill(colour);
        }
    }

    // PLAN-VICRENDER-001: copy the precomputed per-pixel background into a pixel range (replaces
    // FillPixels(..., bgPixel) so background bars are honoured).
    private void FillBackground(Span<uint> pixels, int start, int length)
    {
        if (length <= 0)
        {
            return;
        }

        _backgroundLine.AsSpan(start, length).CopyTo(pixels.Slice(start, length));
    }

    /// <summary>
    /// Force a full frame render (for initial display)
    /// </summary>
    public void RenderFullFrame()
    {
        for (int y = 0; y < PalTotalLines; y++)
        {
            RenderRasterLine(y);
        }
    }

    public static bool TryMapRasterLineToFrameY(int rasterLine, out int y)
    {
        y = rasterLine - PalFirstVisibleRasterLine;
        return y >= 0 && y < ScreenHeight;
    }

    public static int RasterLineToFrameY(int rasterLine) => rasterLine - PalFirstVisibleRasterLine;

    public event EventHandler? FrameCompleted;

    public void Reset()
    {
        Array.Clear(FrameBuffer);
    }
}
