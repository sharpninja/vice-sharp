namespace ViceSharp.Core.Media;

using System;
using System.Collections.Generic;

/// <summary>
/// FR-MEDOVL-001: draws a compact statistics overlay (emulation rate, fps, emulated-vs-wall
/// time, cycle) directly into a BGRA frame buffer before it is teed to a video capture, so a
/// recorded clip carries the live numbers needed to debug pacing/recorder issues. Uses a tiny
/// embedded 5x7 font (uppercase + digits + a few symbols) and a translucent dark backing strip
/// so the text stays legible over any video content. Pure pixel manipulation - no GDI/Skia dep.
/// </summary>
public static class StatsOverlayRenderer
{
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;
    private const int GlyphSpacing = 1;

    /// <summary>
    /// Blit the overlay <paramref name="lines"/> into the top-left of a BGRA8888 frame
    /// (row-major, top-down, length = width*height*4). Unknown characters render as blank cells.
    /// </summary>
    public static void Draw(Span<byte> bgra, int width, int height, IReadOnlyList<string> lines, int scale = 2)
    {
        if (lines.Count == 0 || width <= 0 || height <= 0 || scale <= 0)
            return;
        if (bgra.Length < width * height * 4)
            return;

        var cellW = (GlyphWidth + GlyphSpacing) * scale;
        var lineH = (GlyphHeight + 2) * scale;

        var maxChars = 0;
        foreach (var line in lines)
            maxChars = Math.Max(maxChars, line.Length);

        const int pad = 2;
        var boxW = Math.Min(width, maxChars * cellW + pad * 2);
        var boxH = Math.Min(height, lines.Count * lineH + pad * 2);

        // Translucent dark backing so white text stays readable over bright frames.
        FillBox(bgra, width, height, 0, 0, boxW, boxH, blendAlpha: 160);

        for (var li = 0; li < lines.Count; li++)
        {
            var line = lines[li];
            var y0 = pad + li * lineH;
            for (var ci = 0; ci < line.Length; ci++)
            {
                var x0 = pad + ci * cellW;
                DrawGlyph(bgra, width, height, line[ci], x0, y0, scale);
            }
        }
    }

    private static void FillBox(Span<byte> bgra, int width, int height, int bx, int by, int bw, int bh, int blendAlpha)
    {
        var x1 = Math.Min(width, bx + bw);
        var y1 = Math.Min(height, by + bh);
        for (var y = by; y < y1; y++)
        {
            for (var x = bx; x < x1; x++)
            {
                var i = (y * width + x) * 4;
                // Blend toward black by blendAlpha/255.
                bgra[i] = (byte)(bgra[i] * (255 - blendAlpha) / 255);
                bgra[i + 1] = (byte)(bgra[i + 1] * (255 - blendAlpha) / 255);
                bgra[i + 2] = (byte)(bgra[i + 2] * (255 - blendAlpha) / 255);
                bgra[i + 3] = 255;
            }
        }
    }

    private static void DrawGlyph(Span<byte> bgra, int width, int height, char c, int x0, int y0, int scale)
    {
        if (!Font.TryGetValue(char.ToUpperInvariant(c), out var rows))
            return;

        for (var ry = 0; ry < GlyphHeight; ry++)
        {
            var bits = rows[ry];
            for (var rx = 0; rx < GlyphWidth; rx++)
            {
                // bit (GlyphWidth-1 - rx) is the leftmost pixel.
                if ((bits & (1 << (GlyphWidth - 1 - rx))) == 0)
                    continue;

                for (var sy = 0; sy < scale; sy++)
                {
                    var py = y0 + ry * scale + sy;
                    if (py < 0 || py >= height)
                        continue;
                    for (var sx = 0; sx < scale; sx++)
                    {
                        var px = x0 + rx * scale + sx;
                        if (px < 0 || px >= width)
                            continue;
                        var i = (py * width + px) * 4;
                        bgra[i] = 255;     // B
                        bgra[i + 1] = 255; // G
                        bgra[i + 2] = 255; // R
                        bgra[i + 3] = 255; // A
                    }
                }
            }
        }
    }

    // 5x7 font: each glyph is 7 rows; the low 5 bits of each row are the pixels (bit4 = left).
    // Only the characters used by the capture overlay are defined.
    private static readonly IReadOnlyDictionary<char, byte[]> Font = new Dictionary<char, byte[]>
    {
        [' '] = new byte[] { 0, 0, 0, 0, 0, 0, 0 },
        ['0'] = new byte[] { 0b01110, 0b10001, 0b10011, 0b10101, 0b11001, 0b10001, 0b01110 },
        ['1'] = new byte[] { 0b00100, 0b01100, 0b00100, 0b00100, 0b00100, 0b00100, 0b01110 },
        ['2'] = new byte[] { 0b01110, 0b10001, 0b00001, 0b00010, 0b00100, 0b01000, 0b11111 },
        ['3'] = new byte[] { 0b11111, 0b00010, 0b00100, 0b00010, 0b00001, 0b10001, 0b01110 },
        ['4'] = new byte[] { 0b00010, 0b00110, 0b01010, 0b10010, 0b11111, 0b00010, 0b00010 },
        ['5'] = new byte[] { 0b11111, 0b10000, 0b11110, 0b00001, 0b00001, 0b10001, 0b01110 },
        ['6'] = new byte[] { 0b00110, 0b01000, 0b10000, 0b11110, 0b10001, 0b10001, 0b01110 },
        ['7'] = new byte[] { 0b11111, 0b00001, 0b00010, 0b00100, 0b01000, 0b01000, 0b01000 },
        ['8'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110 },
        ['9'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b01111, 0b00001, 0b00010, 0b01100 },
        ['.'] = new byte[] { 0, 0, 0, 0, 0, 0b00100, 0b00100 },
        [':'] = new byte[] { 0, 0b00100, 0b00100, 0, 0b00100, 0b00100, 0 },
        ['%'] = new byte[] { 0b11001, 0b11010, 0b00100, 0b01011, 0b10011, 0, 0 },
        ['A'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 },
        ['C'] = new byte[] { 0b01110, 0b10001, 0b10000, 0b10000, 0b10000, 0b10001, 0b01110 },
        ['D'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b11110 },
        ['E'] = new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111 },
        ['O'] = new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
        ['F'] = new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b10000 },
        ['L'] = new byte[] { 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111 },
        ['M'] = new byte[] { 0b10001, 0b11011, 0b10101, 0b10001, 0b10001, 0b10001, 0b10001 },
        ['P'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000 },
        ['R'] = new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001 },
        ['S'] = new byte[] { 0b01111, 0b10000, 0b10000, 0b01110, 0b00001, 0b00001, 0b11110 },
        ['T'] = new byte[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 },
        ['U'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
        ['W'] = new byte[] { 0b10001, 0b10001, 0b10001, 0b10101, 0b10101, 0b11011, 0b10001 },
        ['Y'] = new byte[] { 0b10001, 0b10001, 0b01010, 0b00100, 0b00100, 0b00100, 0b00100 },
    };
}
