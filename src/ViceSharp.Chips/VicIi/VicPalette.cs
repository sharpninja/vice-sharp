namespace ViceSharp.Chips.VicIi;

/// <summary>
/// VIC-II color palette generated through VICE's color pipeline
/// (PLAN-VICEPARITY-001 audit M15/L7/L8/L9).
///
/// VICE stores per-model YUV tables (TOBIAS_COLORS, viciisc/vicii-color.c:50,
/// :363-585) and derives RGB at runtime: with the SEPERATE_ODD_EVEN_COLORS
/// build (:53) the internal non-CRT palette feeds the EVEN tables through
/// video_convert_cbm_to_ycbcr (video/video-color.c:318-354: basesat =
/// saturation / 1.75, UV from the chroma angle, UV to CbCr via / 0.493111 and
/// / 0.877283 on PAL, YIQ components on NTSC, direction 0 = grey and
/// direction &lt; 0 = inverted vector) and video_calc_palette /
/// video_convert_renderer_to_rgb_gamma (:404-452, :827-864) using the
/// BT.601 matrix on PAL or the Sony YIQ matrix on NTSC (:249-278). All five
/// color resources default to 1000 (video/video-resources.c:595-608), which
/// makes saturation/contrast neutral, brightness 0, tint 0 and gamma 1
/// (factor = 255^0 = 1), so the neutral pipeline below is the exact default
/// VICE output.
///
/// Model to palette-group mapping (vicii_color_update_palette,
/// vicii-color.c:630-648): 6567R56A and 6569R1 use 6569r1; 6567, 6572 and
/// 6569 use 6569r5; 8562 and 8565 use 8565r2.
/// </summary>
public static class VicPalette
{
    /// <summary>
    /// VIC-II color entry (RGB)
    /// </summary>
    public readonly record struct Color(byte R, byte G, byte B);

    /// <summary>
    /// VICE TOBIAS_COLORS palette groups (vicii-color.c:630-648).
    /// </summary>
    public enum Group
    {
        /// <summary>vicii_palette_6569r1: 6569R1 and 6567R56A (5-luma "old").</summary>
        Mos6569R1,
        /// <summary>vicii_palette_6569r5: 6569, 6567 and 6572 (9-luma "old").</summary>
        Mos6569R5,
        /// <summary>vicii_palette_8565r2: 8565 and 8562 (9-luma "new").</summary>
        Mos8565R2,
    }

    // video_cbm_color_t: luma (0..256), chroma angle (degrees), saturation
    // (0..256), direction (0 = grey, -1 = inverted chroma vector).
    private readonly record struct CbmColor(double Luma, double Angle, double Saturation, int Direction);

    // vicii_colors_6569r1_even (vicii-color.c:385-403).
    private static readonly CbmColor[] Table6569R1Even =
    [
        new(0.000 * 256.0, 0.00, 0.000 * 256.0, 0),           // Black
        new(1.000 * 256.0, 0.00, 0.000 * 256.0, 0),           // White
        new(0.237 * 256.0, 89.00, 0.202 * 256.0, 1),          // Red
        new(0.763 * 256.0, 269.25 - 180.0, 0.191 * 256.0, -1),// Cyan
        new(0.500 * 256.0, 48.50 - 180.0, 0.226 * 256.0, -1), // Purple
        new(0.500 * 256.0, 235.45, 0.222 * 256.0, 1),         // Green
        new(0.237 * 256.0, -12.40 - 360.0, 0.234 * 256.0, 1), // Blue
        new(0.763 * 256.0, 168.60 - 180.0, 0.231 * 256.0, -1),// Yellow
        new(0.500 * 256.0, 122.00 - 180.0, 0.213 * 256.0, -1),// Orange
        new(0.237 * 256.0, 140.00, 0.226 * 256.0, 1),         // Brown
        new(0.500 * 256.0, 89.00, 0.202 * 256.0, 1),          // Light Red
        new(0.237 * 256.0, 0.00, 0.000 * 256.0, 0),           // Dark Grey
        new(0.500 * 256.0, 0.00, 0.000 * 256.0, 0),           // Medium Grey
        new(0.763 * 256.0, 235.45 - 360.0, 0.222 * 256.0, 1), // Light Green
        new(0.500 * 256.0, -12.40 - 360.0, 0.234 * 256.0, 1), // Light Blue
        new(0.763 * 256.0, 0.00, 0.000 * 256.0, 0),           // Light Grey
    ];

    // vicii_colors_6569r5_even (vicii-color.c:459-477).
    private static readonly CbmColor[] Table6569R5Even =
    [
        new(0.000 * 256.0, 0.00, 0.000 * 256.0, 0),           // Black
        new(1.000 * 256.0, 0.00, 0.000 * 256.0, 0),           // White
        new(0.306 * 256.0, 89.00, 0.202 * 256.0, 1),          // Red
        new(0.639 * 256.0, 269.25 - 180.0, 0.191 * 256.0, -1),// Cyan
        new(0.363 * 256.0, 48.50 - 180.0, 0.226 * 256.0, -1), // Purple
        new(0.500 * 256.0, 235.45, 0.222 * 256.0, 1),         // Green
        new(0.237 * 256.0, -12.40 - 360.0, 0.234 * 256.0, 1), // Blue
        new(0.763 * 256.0, 168.60 - 180.0, 0.231 * 256.0, -1),// Yellow
        new(0.363 * 256.0, 122.00 - 180.0, 0.213 * 256.0, -1),// Orange
        new(0.237 * 256.0, 140.00, 0.226 * 256.0, 1),         // Brown
        new(0.500 * 256.0, 89.00, 0.202 * 256.0, 1),          // Light Red
        new(0.306 * 256.0, 0.00, 0.000 * 256.0, 0),           // Dark Grey
        new(0.461 * 256.0, 0.00, 0.000 * 256.0, 0),           // Medium Grey
        new(0.763 * 256.0, 235.45 - 360.0, 0.222 * 256.0, 1), // Light Green
        new(0.461 * 256.0, -12.40 - 360.0, 0.234 * 256.0, 1), // Light Blue
        new(0.639 * 256.0, 0.00, 0.000 * 256.0, 0),           // Light Grey
    ];

    // vicii_colors_8565r2_even (vicii-color.c:533-551).
    private static readonly CbmColor[] Table8565R2Even =
    [
        new(0.000 * 256.0, 0.00, 0.000 * 256.0, 0),           // Black
        new(1.000 * 256.0, 0.00, 0.000 * 256.0, 0),           // White
        new(0.306 * 256.0, 93.50, 0.212 * 256.0, 1),          // Red
        new(0.639 * 256.0, 273.00 - 180.0, 0.215 * 256.0, -1),// Cyan
        new(0.363 * 256.0, 43.00 - 180.0, 0.214 * 256.0, -1), // Purple
        new(0.500 * 256.0, 231.70, 0.216 * 256.0, 1),         // Green
        new(0.237 * 256.0, -24.40 - 360.0, 0.215 * 256.0, 1), // Blue
        new(0.763 * 256.0, 169.60 - 180.0, 0.215 * 256.0, -1),// Yellow
        new(0.363 * 256.0, 120.00 - 180.0, 0.211 * 256.0, -1),// Orange
        new(0.237 * 256.0, 146.50, 0.212 * 256.0, 1),         // Brown
        new(0.500 * 256.0, 93.50, 0.212 * 256.0, 1),          // Light Red
        new(0.306 * 256.0, 0.00, 0.000 * 256.0, 0),           // Dark Grey
        new(0.461 * 256.0, 0.00, 0.000 * 256.0, 0),           // Medium Grey
        new(0.763 * 256.0, 231.70 - 360.0, 0.216 * 256.0, 1), // Light Green
        new(0.461 * 256.0, -24.40 - 360.0, 0.215 * 256.0, 1), // Light Blue
        new(0.639 * 256.0, 0.00, 0.000 * 256.0, 0),           // Light Grey
    ];

    // Cached generated palettes: [group * 2 + (ntsc ? 1 : 0)].
    private static readonly Color[][] GeneratedPalettes = BuildAll();

    /// <summary>
    /// Default palette: PAL 6569 (group 6569r5, BT.601 path) - the managed
    /// default C64 machine's VIC model.
    /// </summary>
    public static readonly Color[] Colors = ForGroup(Group.Mos6569R5, ntsc: false);

    /// <summary>
    /// Returns the generated 16-entry palette for a VICE palette group and
    /// video standard (PAL = BT.601, NTSC = Sony YIQ matrix).
    /// </summary>
    public static Color[] ForGroup(Group group, bool ntsc)
        => GeneratedPalettes[((int)group * 2) + (ntsc ? 1 : 0)];

    private static Color[][] BuildAll()
    {
        var tables = new[] { Table6569R1Even, Table6569R5Even, Table8565R2Even };
        var result = new Color[6][];
        for (int g = 0; g < 3; g++)
        {
            result[g * 2] = Generate(tables[g], ntsc: false);
            result[(g * 2) + 1] = Generate(tables[g], ntsc: true);
        }

        return result;
    }

    /// <summary>
    /// The neutral-resource VICE color pipeline:
    /// video_convert_cbm_to_ycbcr (video-color.c:318-354) followed by
    /// video_convert_renderer_to_rgb_gamma (:404-452) with saturation 1,
    /// brightness 0, contrast 1, gamma 1 (identity) and tint 0. Float casts
    /// mirror the C stages.
    /// </summary>
    private static Color[] Generate(CbmColor[] table, bool ntsc)
    {
        var palette = new Color[16];
        for (int i = 0; i < 16; i++)
        {
            var c = table[i];
            double basesat = c.Saturation / 1.75; // INT_SAT_ADJ
            float y = (float)c.Luma;              // INT_LUMA_ADJ = 1.0
            float cb;
            float cr;
            if (!ntsc)
            {
                // PAL: UV from the angle, then UV -> CbCr
                // (video_convert_yuv_to_ycbcr, video-color.c:218-223).
                float u = (float)(basesat * Math.Cos(c.Angle * (Math.PI / 180.0)));
                float v = (float)(basesat * Math.Sin(c.Angle * (Math.PI / 180.0)));
                cb = u / 0.493111f;
                cr = v / 0.877283f;
            }
            else
            {
                // NTSC: I/Q with the -100/3 degree offset (video-color.c:340-341).
                cb = (float)(basesat * Math.Sin((c.Angle - (100.0 / 3.0)) * (Math.PI / 180.0)));
                cr = (float)(basesat * Math.Cos((c.Angle - (100.0 / 3.0)) * (Math.PI / 180.0)));
            }

            if (c.Direction == 0)
            {
                cb = 0f;
                cr = 0f;
            }
            else if (c.Direction < 0)
            {
                cb = -cb;
                cr = -cr;
            }

            float rf;
            float gf;
            float bf;
            if (!ntsc)
            {
                // BT.601 (video_convert_ycbcr_to_rgb, video-color.c:256-261).
                rf = y + (1.402f * cr);
                gf = y - (0.344136f * cb) - (0.714136f * cr);
                bf = y + (1.772f * cb);
            }
            else
            {
                // Sony matrix (video_convert_yiq_to_rgb, video-color.c:271-274).
                rf = y + (1.630f * cb) + (0.317f * cr);
                gf = y - (0.378f * cb) - (0.466f * cr);
                bf = y - (1.089f * cb) + (1.677f * cr);
            }

            // Round + clamp (video-color.c:262-264); the neutral gamma stage
            // (factor 255^0 = 1, value^1) is the identity, then the final
            // 255 max-clamp (:448-450).
            palette[i] = new Color(ClampByte(rf), ClampByte(gf), ClampByte(bf));
        }

        return palette;
    }

    private static byte ClampByte(float value)
    {
        int v = (int)(value + 0.5f);
        return (byte)Math.Clamp(v, 0, 255);
    }

    /// <summary>
    /// Get RGB color from palette index
    /// </summary>
    public static Color GetColor(int index) => Colors[index & 0x0F];

    /// <summary>
    /// Convert palette index to RGB tuple
    /// </summary>
    public static (byte R, byte G, byte B) ToRgb(int index) =>
        (Colors[index & 0x0F].R, Colors[index & 0x0F].G, Colors[index & 0x0F].B);

    /// <summary>
    /// Get 16-bit RGB565 color from palette index
    /// </summary>
    public static ushort ToRgb565(int index)
    {
        var c = Colors[index & 0x0F];
        return (ushort)(((c.R >> 3) << 11) | ((c.G >> 2) << 5) | (c.B >> 3));
    }
}
