using System.Runtime.CompilerServices;

namespace ViceSharp.Chips.VicIi;

/// <summary>
/// Per-cycle graphics pixel sequencer for MOS 6569 VIC-II.
/// Ports <c>draw_graphics()</c> and <c>draw_graphics8()</c> from
/// <c>native/vice/vice/src/viciisc/vicii-draw-cycle.c</c> lines 144-295.
///
/// FR-VIC-DRAW-GFX: emits 8 render_buffer/pri_buffer pixels per cycle via a
/// gbuf shift register and a 2-stage (pipe0/pipe1) vbuf/cbuf pipeline, exactly
/// as VICE does. FR-VIC-XSCROLL: <c>xscroll_pipe</c> is sampled at the end of
/// each display cycle and consumed as the latch offset for gbuf/vbuf/cbuf at
/// the start of draw_graphics(i) when i==xscroll_pipe.
///
/// <c>LineIndices</c> is the per-line output: 63*8=504 bytes indexed by
/// <c>RasterX*8+i</c>. VideoRenderer reads the visible window from this buffer
/// by adding <c>VideoRenderer.FirstVisibleRasterX*8</c> to the frame-pixel
/// coordinate (identical to the <c>dbuf_offset</c> arithmetic in VICE).
/// </summary>
internal sealed class PixelSequencer
{
    // ---------------------------------------------------------------
    // Symbolic color codes from vicii-draw-cycle.c lines 41-61
    // ---------------------------------------------------------------
    private const byte ColNone    = 0x10;
    private const byte ColVbufL   = 0x11;
    private const byte ColVbufH   = 0x12;
    private const byte ColCbuf    = 0x13;
    private const byte ColCbufMc  = 0x14;
    private const byte ColD02xExt = 0x15;

    // VIC register byte offsets within the 64-byte _registers array.
    private const int D011 = 0x11;
    private const int D016 = 0x16;
    private const int D021 = 0x21; // background color 0

    // colors[] table from vicii-draw-cycle.c lines 133-142.
    // Index = vmode | px, where:
    //   vmode = (Vmode11Pipe | Vmode16Pipe) with Vmode11Pipe = (ECM<<4 | BMM<<3),
    //           Vmode16Pipe = (MCM<<2). Only bits 5-2 matter, collapsed to 4 bits.
    // px = 0-3 (pixel value from gbuf shift register).
    // 32-entry table: 8 mode combinations * 4 pixel values.
    private static ReadOnlySpan<byte> Colors =>
    [
        // ECM=0 BMM=0 MCM=0  vmode=0x00, px=0..3
        0x21, 0x21, ColCbuf, ColCbuf,
        // ECM=0 BMM=0 MCM=1  vmode=0x04, px=0..3
        0x21, 0x22, 0x23, ColCbufMc,
        // ECM=0 BMM=1 MCM=0  vmode=0x08, px=0..3
        ColVbufL, ColVbufL, ColVbufH, ColVbufH,
        // ECM=0 BMM=1 MCM=1  vmode=0x0C, px=0..3
        0x21, ColVbufH, ColVbufL, ColCbuf,
        // ECM=1 BMM=0 MCM=0  vmode=0x10, px=0..3
        ColD02xExt, ColD02xExt, ColCbuf, ColCbuf,
        // ECM=1 BMM=0 MCM=1  vmode=0x14 (invalid), px=0..3
        ColNone, ColNone, ColNone, ColNone,
        // ECM=1 BMM=1 MCM=0  vmode=0x18 (invalid), px=0..3
        ColNone, ColNone, ColNone, ColNone,
        // ECM=1 BMM=1 MCM=1  vmode=0x1C (invalid), px=0..3
        ColNone, ColNone, ColNone, ColNone,
    ];

    // ---------------------------------------------------------------
    // Pipeline registers - vicii-draw-cycle.c lines 65-116
    // ---------------------------------------------------------------

    // Stage-0 pipeline (loaded each vis_en cycle from gbuf/vbuf/cbuf).
    internal byte GbufPipe0Reg;
    internal byte CbufPipe0Reg;
    internal byte VbufPipe0Reg;

    // Stage-1 pipeline (promoted from pipe0 at the end of each DrawGraphics8 call).
    internal byte GbufPipe1Reg;
    internal byte CbufPipe1Reg;
    internal byte VbufPipe1Reg;

    // Mode and scroll latches.
    internal byte XscrollPipe;    // $D016 bits 0-2, sampled each vis_en cycle
    internal byte Vmode11Pipe;    // ($D011 & 0x60) >> 2 (ECM=bit4, BMM=bit3)
    internal byte Vmode16Pipe;    // ($D016 & 0x10) >> 2 (MCM=bit2)
    internal byte Vmode16Pipe2;   // previous-cycle Vmode16Pipe (kludge detector)

    // Shift register and pixel state.
    internal byte GbufReg;        // 8-bit shift register; MSB-first out
    internal byte GbufMcFlop;     // multicolor pixel-pair clock flop (0/1)
    internal byte GbufPixelReg;   // current 2-bit pixel value (hires 0/3 or MC 0-3)

    // Latched at i==xscroll_pipe each cycle from pipe1.
    internal byte CbufReg;        // color buffer for current cell
    internal byte VbufReg;        // video (screen code) buffer for current cell

    // Display-matrix line index (advances 1 per vis_en, non-idle cycle).
    internal byte Dmli;

    // ---------------------------------------------------------------
    // Per-cycle output buffers (8 entries, refreshed each DrawGraphics8).
    // ---------------------------------------------------------------

    /// <summary>
    /// 8-entry palette-index array for the most recent <see cref="DrawGraphics8"/> call.
    /// </summary>
    internal readonly byte[] RenderBuffer = new byte[8];

    /// <summary>
    /// 8-entry foreground-priority flags (0=background, 2=foreground).
    /// Matches <see cref="RenderBuffer"/>.
    /// </summary>
    internal readonly byte[] PriBuffer = new byte[8];

    // ---------------------------------------------------------------
    // Per-line output buffers (504 entries = 63 cycles * 8 pixels).
    // ---------------------------------------------------------------

    /// <summary>
    /// 504-byte palette-index line buffer.
    /// Index = RasterX * 8 + pixel_i (0..7).
    /// VideoRenderer reads frame pixel X as
    /// <c>LineIndices[X + FirstVisibleRasterX * 8]</c>.
    /// </summary>
    internal readonly byte[] LineIndices = new byte[63 * 8];

    /// <summary>
    /// 504-byte priority-flag line buffer; same indexing as <see cref="LineIndices"/>.
    /// 0 = background pixel, 2 = foreground pixel (sprite priority gate).
    /// </summary>
    internal readonly byte[] LinePriority = new byte[63 * 8];

    // ---------------------------------------------------------------
    // References to shared VIC state (zero-allocation, no copying).
    // ---------------------------------------------------------------
    private readonly byte[] _regs;   // Mos6569._registers[64]
    private readonly byte[] _vbuf;   // Mos6569._videoBuffer[40]
    private readonly byte[] _cbuf;   // Mos6569._colorBuffer[40]
    private readonly Mos6569 _vic;   // for ColorLatency

    internal PixelSequencer(byte[] registers, byte[] videoBuffer, byte[] colorBuffer, Mos6569 vic)
    {
        _regs = registers;
        _vbuf = videoBuffer;
        _cbuf = colorBuffer;
        _vic  = vic;
    }

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    /// <summary>Reset all pipeline state to power-on values.</summary>
    internal void Reset()
    {
        GbufPipe0Reg  = GbufPipe1Reg  = 0;
        CbufPipe0Reg  = CbufPipe1Reg  = 0;
        VbufPipe0Reg  = VbufPipe1Reg  = 0;
        XscrollPipe   = 0;
        Vmode11Pipe   = 0;
        Vmode16Pipe   = 0;
        Vmode16Pipe2  = 0;
        GbufReg       = 0;
        GbufMcFlop    = 0;
        GbufPixelReg  = 0;
        CbufReg       = 0;
        VbufReg       = 0;
        Dmli          = 0;
        Array.Clear(LineIndices,  0, LineIndices.Length);
        Array.Clear(LinePriority, 0, LinePriority.Length);
    }

    /// <summary>
    /// Called at the start of each new raster line (after the renderer has
    /// consumed the previous line's <see cref="LineIndices"/>).
    /// Clears the line buffers and resets <see cref="Dmli"/> to 0.
    /// </summary>
    internal void BeginLine()
    {
        Array.Clear(LineIndices,  0, LineIndices.Length);
        Array.Clear(LinePriority, 0, LinePriority.Length);
        Dmli = 0;
    }

    // ---------------------------------------------------------------
    // Per-pixel render (draw_graphics from vicii-draw-cycle.c:144-225)
    // ---------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte DrawGraphics(int i)
    {
        // Latch vbuf/cbuf/gbuf at offset == xscroll_pipe (lines 152-158).
        if (i == XscrollPipe)
        {
            VbufReg    = VbufPipe1Reg;
            CbufReg    = CbufPipe1Reg;
            GbufReg    = GbufPipe1Reg;
            GbufMcFlop = 1;
        }

        // Determine current pixel value from shift register (lines 164-187).
        if (Vmode16Pipe2 != 0)
        {
            // MCM was active at pixel 7 of the previous cycle.
            if ((Vmode11Pipe & 0x08) != 0 || (CbufReg & 0x08) != 0)
            {
                // MC pixels: sample MSBs every other pixel via GbufMcFlop.
                if (GbufMcFlop != 0)
                    GbufPixelReg = (byte)(GbufReg >> 6);
            }
            else
            {
                // Hires-under-MCM: foreground = 3, background = 0.
                GbufPixelReg = (GbufReg & 0x80) != 0 ? (byte)3 : (byte)0;
            }
        }
        else
        {
            // Normal or MCM-0-to-1 kludge ($D023 kludge, vicii-draw-cycle.c:178-186).
            if ((Vmode11Pipe & 0x08) != 0 || (CbufReg & 0x08) != 0)
                GbufPixelReg = (GbufReg & 0x80) != 0 ? (byte)2 : (byte)0;
            else
                GbufPixelReg = (GbufReg & 0x80) != 0 ? (byte)3 : (byte)0;
        }

        byte px = GbufPixelReg;

        // Shift the graphics buffer MSB-first (lines 191-192).
        GbufReg    <<= 1;
        GbufMcFlop ^= 1;

        // Determine priority and symbolic color (lines 195-197).
        byte vmode    = (byte)(Vmode11Pipe | Vmode16Pipe);
        byte pixelPri = (byte)(px & 0x2);
        byte cc       = Colors[vmode | px];

        // Resolve symbolic color codes to palette indices (lines 200-221).
        switch (cc)
        {
            case ColNone:
                cc = 0;
                break;
            case ColVbufL:
                cc = (byte)(VbufReg & 0x0F);
                break;
            case ColVbufH:
                cc = (byte)(VbufReg >> 4);
                break;
            case ColCbuf:
                cc = (byte)(CbufReg & 0x0F);
                break;
            case ColCbufMc:
                cc = (byte)(CbufReg & 0x07);
                break;
            case ColD02xExt:
                // ECM: select background register by screen-code bits 7-6.
                cc = (byte)(_regs[D021 + ((VbufReg >> 6) & 0x03)] & 0x0F);
                break;
            default:
                // Direct register codes 0x21-0x2E map to $D021-$D02E.
                if (cc >= 0x21 && cc <= 0x2E)
                    cc = (byte)(_regs[cc] & 0x0F);
                break;
        }

        RenderBuffer[i] = cc;
        PriBuffer[i]    = pixelPri;
        return pixelPri;
    }

    // ---------------------------------------------------------------
    // Per-cycle render (draw_graphics8 from vicii-draw-cycle.c:227-295)
    // ---------------------------------------------------------------

    /// <summary>
    /// Renders 8 pixels for one VIC-II cycle (equivalent to VICE <c>draw_graphics8</c>).
    /// Must be called every cycle from <see cref="Mos6569.Tick"/> AFTER
    /// <c>LastReadPhi1</c> is set (phi-1 / g-access data is ready).
    ///
    /// <list type="bullet">
    /// <item><paramref name="rasterX"/> - current cycle index (0-62 PAL)</item>
    /// <item><paramref name="visEn"/> - true when the cycle is inside the 40-column
    ///   display window and the vertical border is closed (FR-VIC-XSCROLL AC-01)</item>
    /// <item><paramref name="vborder"/> - vertical border active</item>
    /// <item><paramref name="idleState"/> - VIC-II idle (no display matrix data)</item>
    /// <item><paramref name="gbuf"/> - g-access result for this cycle
    ///   (= <c>LastReadPhi1</c> from <see cref="Mos6569"/>)</item>
    /// </list>
    /// </summary>
    internal void DrawGraphics8(int rasterX, bool visEn, bool vborder, bool idleState, byte gbuf)
    {
        // --- Pixel 0 ---
        DrawGraphics(0);
        // --- Pixel 1 ---
        DrawGraphics(1);
        // --- Pixel 2 ---
        DrawGraphics(2);
        // --- Pixel 3 ---
        DrawGraphics(3);

        // After pixel 3, before pixel 4: sample MCM and ECM+BMM edges
        // (vicii-draw-cycle.c:243-247).
        Vmode16Pipe = (byte)((_regs[D016] & 0x10) >> 2);
        if (_vic.ColorLatencyEnabled)
        {
            // 6569: rising edge - OR in ECM/BMM bits (color_latency=1).
            Vmode11Pipe |= (byte)((_regs[D011] & 0x60) >> 2);
        }

        // --- Pixel 4 ---
        DrawGraphics(4);
        // --- Pixel 5 ---
        DrawGraphics(5);

        // After pixel 5, before pixel 6: falling edge for color-latency chips
        // (vicii-draw-cycle.c:252-255).
        if (_vic.ColorLatencyEnabled)
        {
            Vmode11Pipe &= (byte)((_regs[D011] & 0x60) >> 2);
        }

        // --- Pixel 6 ---
        DrawGraphics(6);

        // After pixel 6, before pixel 7: Vmode16Pipe2 transition (lines 258-261).
        if (Vmode16Pipe != 0 && Vmode16Pipe2 == 0)
            GbufMcFlop = 0;
        Vmode16Pipe2 = Vmode16Pipe;

        // --- Pixel 7 ---
        DrawGraphics(7);

        // Without color latency (8565/8562): update Vmode11Pipe from live register
        // (vicii-draw-cycle.c:264-266).
        if (!_vic.ColorLatencyEnabled)
        {
            Vmode11Pipe = (byte)((_regs[D011] & 0x60) >> 2);
        }

        // Advance the 2-stage pipeline: pipe1 <- pipe0 (lines 269-271).
        VbufPipe1Reg = VbufPipe0Reg;
        CbufPipe1Reg = CbufPipe0Reg;
        GbufPipe1Reg = GbufPipe0Reg;

        // Load pipe0 from this cycle's data (lines 274-294).
        // FR-VIC-DRAW-GFX AC-15: gbuf is forced 0 outside the visible area.
        if (visEn && !vborder)
        {
            GbufPipe0Reg = gbuf;
            // FR-VIC-XSCROLL AC-01: xscroll_pipe sampled each display cycle.
            XscrollPipe = (byte)(_regs[D016] & 0x07);
        }
        else
        {
            GbufPipe0Reg = 0;
        }

        if (visEn && !vborder)
        {
            if (!idleState)
            {
                // FR-VIC-DRAW-GFX AC-15: advance dmli and latch vbuf/cbuf from display matrix.
                VbufPipe0Reg = (Dmli < 40) ? _vbuf[Dmli] : (byte)0;
                CbufPipe0Reg = (Dmli < 40) ? _cbuf[Dmli] : (byte)0;
                if (Dmli < 40) Dmli++;
            }
            else
            {
                // Idle state: vbuf/cbuf pipe0 forced to 0.
                VbufPipe0Reg = 0;
                CbufPipe0Reg = 0;
            }
        }
        else
        {
            // Outside visible area: reset dmli to 0 (lines 293-294).
            Dmli = 0;
        }

        // Write resolved palette indices to the 504-byte line buffer.
        // FR-VIC-DRAW-GFX AC-01: 8 pixels per cycle, indexed by RasterX*8+i.
        if ((uint)rasterX < 63u)
        {
            int offset = rasterX * 8;
            for (int i = 0; i < 8; i++)
            {
                LineIndices[offset + i]  = RenderBuffer[i];
                LinePriority[offset + i] = PriBuffer[i];
            }
        }
    }
}
