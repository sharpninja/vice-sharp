using System.Numerics;
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
    // Symbolic code for $D020 (border colour). draw_border8 fills RenderBuffer
    // with this code; draw_colors8 resolves it to the live $D020 palette index
    // via Cregs[0x20]. vicii-draw-cycle.c line 47.
    private const byte ColD020 = 0x20;

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
    /// 520-byte palette-index line buffer (audit L1: VICE
    /// VICII_DRAW_BUFFER_SIZE = 65*8, viciitypes.h:60, sized for the 65-cycle
    /// NTSC line). Index = VICE dbuf indexing: cycle k's ring-delayed pixels
    /// land at 8*(k-1) after the raster-cycle-1 offset reset. VideoRenderer
    /// reads frame pixel X as <c>LineIndices[X + FirstVisibleRasterX * 8]</c>.
    /// </summary>
    internal readonly byte[] LineIndices = new byte[65 * 8];

    /// <summary>
    /// 520-byte priority-flag line buffer; same sizing rationale as
    /// <see cref="LineIndices"/> (audit L1). 0 = background pixel,
    /// 2 = foreground pixel (sprite priority gate).
    /// </summary>
    internal readonly byte[] LinePriority = new byte[65 * 8];

    // ---------------------------------------------------------------
    // V4: colour resolution pipeline state (draw_colors8).
    // vicii-draw-cycle.c:578-663 / vicii_draw_cycle_init :702-706.
    // ---------------------------------------------------------------

    /// <summary>
    /// Colour resolution register file (47 entries). Seeded at init:
    /// <c>cregs[0x00..0x0F]</c> = identity; <c>cregs[0x10..0x2E]</c> = 0.
    /// Symbolic codes 0x21-0x2E from <see cref="DrawGraphics"/> are resolved
    /// through this table by <see cref="DrawColors8"/> (not through live
    /// <c>_regs</c>). Updated lazily via <see cref="MonitorColorStore"/> or
    /// the pending-write path in <see cref="DrawColors8"/>.
    /// vicii-draw-cycle.c:702-706 (vicii_draw_cycle_init).
    /// </summary>
    internal readonly byte[] Cregs = new byte[0x2F];

    /// <summary>
    /// 8-entry per-pixel ring delay buffer used by the 6569 color_latency=1
    /// path (<c>draw_colors_6569</c>, vicii-draw-cycle.c:592-604).
    /// Each entry holds a symbolic code that is resolved via
    /// <see cref="Cregs"/> one pixel later.
    /// </summary>
    internal readonly byte[] PixelBuffer = new byte[8];

    /// <summary>
    /// Local last_color_reg (vicii-draw-cycle.c static variable). Transferred
    /// from <c>VicLastColorRegWrite</c> by <c>update_cregs</c> at cycle end.
    /// 0xFF = no pending colour update for the next cycle.
    /// </summary>
    internal byte LastColorReg = 0xFF;

    /// <summary>Last colour value paired with <see cref="LastColorReg"/>.</summary>
    internal byte LastColorValue;

    /// <summary>
    /// Running draw-buffer frame offset (vicii.dbuf_offset equivalent).
    /// Incremented by 8 each <see cref="DrawColors8"/> call; reset to 0 by
    /// <c>Mos6569.Tick</c> at raster cycle 1, exactly like VICE
    /// (vicii-draw-cycle.c:674-677), so cycle k's ring-delayed pixels land at
    /// <see cref="LineIndices"/>[8*(k-1)] = VICE dbuf[8*(k-1)].
    /// </summary>
    internal int DbufOffset;

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

    // PLAN-VICEPARITY-001 FR-VIC-SPRITE-RENDER / FR-VIC-SPRITE-COLLISION V6:
    // Sprite-render pipeline state (sbuf shift register, x_pipe, mc_flops, etc.)
    // from VICE vicii-draw-cycle.c lines 65-116.
    // All fields match VICE static locals in vicii-draw-cycle.c.

    private readonly int[]  _spriteXPipe      = new int[8];
    private readonly uint[] _sbufReg          = new uint[8];
    private readonly int[]  _sbufPixelReg     = new int[8];
    private byte             _sbufMcFlops;
    private byte             _sbufExpxFlops;
    private byte             _spriteActiveBits;
    private byte             _spritePendingBits;
    private byte             _spriteHaltBits;
    private byte             _spritePriBits;
    private byte             _spriteExpxBits;

    // sprite_mc_bits: last-committed $D01C value for toggled-bits calculation
    // (vicii.sprite_mc_bits, VICE vicii-draw-cycle.c). Separate from sbuf_mc_flops.
    private byte _spriteMcBits;

    // Per-cycle sprite collision output written by DrawSprites8, consumed by
    // Mos6569 for first-appearance IRQ gate (vicii-cycle.c:427-433).
    internal byte SpriteSsCollisionThisCycle;
    internal byte SpriteSbCollisionThisCycle;

    // V7: border_state (vicii-draw-cycle.c:105, static int border_state=0).
    // One-cycle-lagged copy of main_border; updated by DrawBorder8 each cycle.
    // Must be reset to 0 at line/frame start (vicii_draw_cycle_init).
    internal int BorderState;

    // PAL cycle DMA tables: index = managed RasterX (0-62).
    // _s_palDmaCycle0[rx] = sprite bitmask for Phi1(SprPtr) cycles (VICE dma_cycle_0).
    // _s_palDmaCycle2[rx] = sprite bitmask for Phi1(SprDma1) cycles (VICE dma_cycle_2,
    //   fires update_sprite_data and also halt-release at pixel 7).
    // Phi1(N) -> managed RasterX = N-1 (VICE uses 1-based cycles).
    private static readonly byte[] s_palDmaCycle0 = new byte[63];
    private static readonly byte[] s_palDmaCycle2 = new byte[63];

    // audit M11: NTSC-65 tables (cycle_tab_ntsc, vicii-chip-model.c:272-403).
    // Sprite 3's pair wraps the line: SprDma1(3) at rc0, SprPtr(3) at rc64.
    private static readonly byte[] s_ntscDmaCycle0 = new byte[65];
    private static readonly byte[] s_ntscDmaCycle2 = new byte[65];

    // audit M11: old-NTSC tables (cycle_tab_ntsc_old, vicii-chip-model.c:437-566).
    private static readonly byte[] s_ntscOldDmaCycle0 = new byte[64];
    private static readonly byte[] s_ntscOldDmaCycle2 = new byte[64];

    static PixelSequencer()
    {
        // SprPtr (Phi1 type) -> dma_cycle_0. vicii-chip-model.c PAL table lines 112-238.
        s_palDmaCycle0[57] = 0x01; // Phi1(58) SprPtr(0)
        s_palDmaCycle0[59] = 0x02; // Phi1(60) SprPtr(1)
        s_palDmaCycle0[61] = 0x04; // Phi1(62) SprPtr(2)
        s_palDmaCycle0[ 0] = 0x08; // Phi1(1)  SprPtr(3)
        s_palDmaCycle0[ 2] = 0x10; // Phi1(3)  SprPtr(4)
        s_palDmaCycle0[ 4] = 0x20; // Phi1(5)  SprPtr(5)
        s_palDmaCycle0[ 6] = 0x40; // Phi1(7)  SprPtr(6)
        s_palDmaCycle0[ 8] = 0x80; // Phi1(9)  SprPtr(7)
        // SprDma1 (Phi1 type) -> dma_cycle_2. vicii-chip-model.c PAL table lines 112-238.
        s_palDmaCycle2[58] = 0x01; // Phi1(59) SprDma1(0)
        s_palDmaCycle2[60] = 0x02; // Phi1(61) SprDma1(1)
        s_palDmaCycle2[62] = 0x04; // Phi1(63) SprDma1(2)
        s_palDmaCycle2[ 1] = 0x08; // Phi1(2)  SprDma1(3)
        s_palDmaCycle2[ 3] = 0x10; // Phi1(4)  SprDma1(4)
        s_palDmaCycle2[ 5] = 0x20; // Phi1(6)  SprDma1(5)
        s_palDmaCycle2[ 7] = 0x40; // Phi1(8)  SprDma1(6)
        s_palDmaCycle2[ 9] = 0x80; // Phi1(10) SprDma1(7)

        // NTSC-65 (vicii-chip-model.c:272-403).
        s_ntscDmaCycle0[ 1] = 0x10; // Phi1(2)  SprPtr(4)
        s_ntscDmaCycle0[ 3] = 0x20; // Phi1(4)  SprPtr(5)
        s_ntscDmaCycle0[ 5] = 0x40; // Phi1(6)  SprPtr(6)
        s_ntscDmaCycle0[ 7] = 0x80; // Phi1(8)  SprPtr(7)
        s_ntscDmaCycle0[58] = 0x01; // Phi1(59) SprPtr(0)
        s_ntscDmaCycle0[60] = 0x02; // Phi1(61) SprPtr(1)
        s_ntscDmaCycle0[62] = 0x04; // Phi1(63) SprPtr(2)
        s_ntscDmaCycle0[64] = 0x08; // Phi1(65) SprPtr(3)
        s_ntscDmaCycle2[ 0] = 0x08; // Phi1(1)  SprDma1(3)
        s_ntscDmaCycle2[ 2] = 0x10; // Phi1(3)  SprDma1(4)
        s_ntscDmaCycle2[ 4] = 0x20; // Phi1(5)  SprDma1(5)
        s_ntscDmaCycle2[ 6] = 0x40; // Phi1(7)  SprDma1(6)
        s_ntscDmaCycle2[ 8] = 0x80; // Phi1(9)  SprDma1(7)
        s_ntscDmaCycle2[59] = 0x01; // Phi1(60) SprDma1(0)
        s_ntscDmaCycle2[61] = 0x02; // Phi1(62) SprDma1(1)
        s_ntscDmaCycle2[63] = 0x04; // Phi1(64) SprDma1(2)

        // Old NTSC (vicii-chip-model.c:437-566).
        s_ntscOldDmaCycle0[ 0] = 0x08; // Phi1(1)  SprPtr(3)
        s_ntscOldDmaCycle0[ 2] = 0x10; // Phi1(3)  SprPtr(4)
        s_ntscOldDmaCycle0[ 4] = 0x20; // Phi1(5)  SprPtr(5)
        s_ntscOldDmaCycle0[ 6] = 0x40; // Phi1(7)  SprPtr(6)
        s_ntscOldDmaCycle0[ 8] = 0x80; // Phi1(9)  SprPtr(7)
        s_ntscOldDmaCycle0[58] = 0x01; // Phi1(59) SprPtr(0)
        s_ntscOldDmaCycle0[60] = 0x02; // Phi1(61) SprPtr(1)
        s_ntscOldDmaCycle0[62] = 0x04; // Phi1(63) SprPtr(2)
        s_ntscOldDmaCycle2[ 1] = 0x08; // Phi1(2)  SprDma1(3)
        s_ntscOldDmaCycle2[ 3] = 0x10; // Phi1(4)  SprDma1(4)
        s_ntscOldDmaCycle2[ 5] = 0x20; // Phi1(6)  SprDma1(5)
        s_ntscOldDmaCycle2[ 7] = 0x40; // Phi1(8)  SprDma1(6)
        s_ntscOldDmaCycle2[ 9] = 0x80; // Phi1(10) SprDma1(7)
        s_ntscOldDmaCycle2[59] = 0x01; // Phi1(60) SprDma1(0)
        s_ntscOldDmaCycle2[61] = 0x02; // Phi1(62) SprDma1(1)
        s_ntscOldDmaCycle2[63] = 0x04; // Phi1(64) SprDma1(2)
    }

    /// <summary>Test-only: sprite_x_pipe[n] (one-cycle-lagged X, VICE vicii.sprite_x_pipe).</summary>
    internal int  GetSpriteXPipe(int n)    => _spriteXPipe[n];
    /// <summary>Test-only: sbuf_reg[n] (24-bit shift register, VICE vicii.sbuf_reg).</summary>
    internal uint GetSbufReg(int n)        => _sbufReg[n];
    /// <summary>Test-only: sbuf_pixel_reg[n] (current pixel value, VICE vicii.sbuf_pixel_reg).</summary>
    internal int  GetSbufPixelReg(int n)   => _sbufPixelReg[n];
    /// <summary>Test-only: sbuf_mc_flops byte (VICE vicii.sbuf_mc_flops).</summary>
    internal byte GetSbufMcFlops()         => _sbufMcFlops;
    /// <summary>Test-only: sbuf_expx_flops byte (VICE vicii.sbuf_expx_flops).</summary>
    internal byte GetSbufExpxFlops()       => _sbufExpxFlops;
    /// <summary>Test-only: sprite_active_bits (VICE vicii.sprite_active_bits).</summary>
    internal byte GetSpriteActiveBits()    => _spriteActiveBits;
    /// <summary>Test-only: sprite_pending_bits (VICE vicii.sprite_pending_bits).</summary>
    internal byte GetSpritePendingBits()   => _spritePendingBits;
    /// <summary>Test-only: sprite_halt_bits (VICE vicii.sprite_halt_bits).</summary>
    internal byte GetSpriteHaltBits()      => _spriteHaltBits;
    /// <summary>Test-only: sprite_pri_bits latched at pixel 6 (VICE vicii.sprite_pri_bits).</summary>
    internal byte GetSpritePriBits()       => _spritePriBits;
    /// <summary>Test-only: sprite_expx_bits latched at pixel 6 (VICE vicii.sprite_expx_bits).</summary>
    internal byte GetSpriteExpxBits()      => _spriteExpxBits;

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
        // V4: initialise cregs identity table (vicii-draw-cycle.c:702-706).
        for (int i = 0; i < 0x10; i++) Cregs[i] = (byte)i;
        Array.Clear(Cregs, 0x10, Cregs.Length - 0x10);
        Array.Clear(PixelBuffer, 0, PixelBuffer.Length);
        LastColorReg  = 0xFF;
        LastColorValue = 0;
        DbufOffset    = 0;
        // V6: sprite-render pipeline state.
        Array.Clear(_spriteXPipe,   0, _spriteXPipe.Length);
        Array.Clear(_sbufReg,       0, _sbufReg.Length);
        Array.Clear(_sbufPixelReg,  0, _sbufPixelReg.Length);
        _sbufMcFlops               = 0;
        _sbufExpxFlops             = 0;
        _spriteActiveBits          = 0;
        _spritePendingBits         = 0;
        _spriteHaltBits            = 0;
        _spritePriBits             = 0;
        _spriteExpxBits            = 0;
        _spriteMcBits              = 0;
        SpriteSsCollisionThisCycle = 0;
        SpriteSbCollisionThisCycle = 0;
        BorderState                = 0;
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
        // PLAN-VICEPARITY-001 audit H2: DbufOffset is deliberately NOT reset
        // here. VICE resets vicii.dbuf_offset at raster_cycle == 1 inside
        // vicii_draw_cycle (vicii-draw-cycle.c:674-677), one cycle after the
        // line wrap; Mos6569.Tick performs that reset. Resetting at the wrap
        // shifted every resolved colour 8px right of VICE's dbuf placement.
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
                // V4: leave symbolic codes 0x21-0x2E unresolved in RenderBuffer.
                // draw_colors8 (DrawColors8) resolves them via Cregs[], allowing
                // the one-pixel ring delay (6569) and mid-line cregs updates.
                // vicii-draw-cycle.c draw_graphics lines 200-221: symbolic codes
                // 0x21-0x2E pass through to render_buffer unchanged; draw_colors8
                // resolves via cregs[]. Direct resolution via live _regs[] was V3.
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

        // Write priority flags to the line buffer (520 bytes, audit L1).
        // FR-VIC-DRAW-GFX AC-01: 8 pixels per cycle, indexed by RasterX*8+i.
        // V4: LineIndices is written by DrawColors8 (via DbufOffset) rather than
        // here, so that colour resolution flows through the Cregs pipeline with
        // the correct ring delay. LinePriority is still written here because
        // priority does not go through the colour pipeline.
        if ((uint)rasterX < 65u)
        {
            int offset = rasterX * 8;
            for (int i = 0; i < 8; i++)
            {
                LinePriority[offset + i] = PriBuffer[i];
            }
        }
    }

    // ---------------------------------------------------------------
    // V4: colour pipeline (draw_colors8 from vicii-draw-cycle.c:627-663)
    // ---------------------------------------------------------------

    /// <summary>
    /// Immediate colour-register update path used by the monitor and by
    /// <see cref="Mos6569.Write"/> for $D020-$D02E.
    /// Maps to <c>vicii_monitor_colreg_store</c> (vicii-draw-cycle.c:120-125):
    /// updates <see cref="Cregs"/>[reg] immediately and records the pending
    /// <see cref="LastColorReg"/>/<see cref="LastColorValue"/> pair so the
    /// CPU-write pipeline path remains consistent.
    /// </summary>
    internal void MonitorColorStore(byte reg, byte value)
    {
        // vicii_monitor_colreg_store (vicii-draw-cycle.c:120-125):
        // immediate Cregs update AND sets local last_color_reg for pipeline.
        Cregs[reg]    = value;
        LastColorReg  = reg;
        LastColorValue = value;
    }

    /// <summary>
    /// Seeds <see cref="Cregs"/> from the current register state after
    /// snapshot injection (<see cref="Mos6569.InjectSnapshotState"/>).
    /// Ensures the first rendered frame uses the correct colour values
    /// without requiring a full-machine CPU write replay.
    /// </summary>
    internal void SeedCregsFromRegisters()
    {
        // Copy colour registers $D020-$D02E (offsets 0x20-0x2E) into Cregs so
        // that snapshot-restored state produces correct colours from the first
        // rendered frame without a CPU write replay.
        for (int i = 0x20; i <= 0x2E; i++)
            Cregs[i] = _regs[i];
    }

    /// <summary>
    /// Per-cycle colour resolution pipeline (maps to <c>draw_colors8</c>,
    /// vicii-draw-cycle.c:627-663). Called each cycle after
    /// <see cref="DrawGraphics8"/>. Resolves the symbolic codes in
    /// <see cref="RenderBuffer"/> via <see cref="Cregs"/>[], applies the
    /// one-pixel ring delay for 6569 (<c>color_latency=1</c>) or direct
    /// lookup for 8565 (<c>color_latency=0</c>), and writes resolved palette
    /// indices into <see cref="LineIndices"/>.
    /// <para>
    /// Also applies any pending colour-register write from
    /// <c>Mos6569.VicLastColorRegWrite</c> at cycle start
    /// (vicii-draw-cycle.c:636-638), then calls <c>update_cregs</c> to
    /// transfer the chip-level pending into the local pipeline
    /// (vicii-draw-cycle.c:585-590).
    /// </para>
    /// </summary>
    internal void DrawColors8()
    {
        int offs = DbufOffset;
        // Guard: vicii-draw-cycle.c:631-633 with VICII_DRAW_BUFFER_SIZE = 520
        // (audit L1: the 65-cycle NTSC line writes offsets up to 504..511).
        if (offs > 520 - 8) return;

        // Apply chip-level pending colour-register write to Cregs immediately
        // so the current pixel loop uses the updated value (vicii-draw-cycle.c
        // combined update_cregs + apply step; managed collapses the two VICE
        // cycles into one for AC-03 compatibility without changing frame output:
        // pixel 0 of the 6569 path still uses the previous ring-buffer value).
        if (_vic.VicLastColorRegWrite != 0xFF)
        {
            Cregs[_vic.VicLastColorRegWrite] = _vic.VicLastColorValueWrite;
        }
        // Apply local LastColorReg (transferred from chip pending in the
        // PREVIOUS cycle's update_cregs; vicii-draw-cycle.c:636-638).
        if (LastColorReg != 0xFF)
        {
            Cregs[LastColorReg] = LastColorValue;
        }

        if (_vic.ColorLatencyEnabled)
        {
            // draw_colors_6569 (vicii-draw-cycle.c:592-604): one-pixel ring delay.
            // lookup_index=(i+1)&7 resolves NEXT pixel's code; outputs CURRENT
            // pixel's previously-resolved ring value; loads render_buffer[i].
            for (int i = 0; i < 8; i++)
            {
                int lookupIndex = (i + 1) & 7;
                PixelBuffer[lookupIndex] = Cregs[PixelBuffer[lookupIndex]];
                LineIndices[offs + i]    = PixelBuffer[i];
                PixelBuffer[i]           = RenderBuffer[i];
            }
        }
        else
        {
            // draw_colors_8565 (vicii-draw-cycle.c:606-624): no ring delay.
            // lookup_index=i; grey-dot at pixel 0 when pixel_buffer[0]==last_color_reg;
            // resolve immediately and output.
            for (int i = 0; i < 8; i++)
            {
                if (i == 0 && PixelBuffer[0] == LastColorReg)
                {
                    PixelBuffer[0] = 0x0F; // grey-dot (vicii-draw-cycle.c:614-615)
                }
                else
                {
                    PixelBuffer[i] = Cregs[PixelBuffer[i]];
                }
                LineIndices[offs + i] = PixelBuffer[i];
                PixelBuffer[i]        = RenderBuffer[i];
            }
        }

        DbufOffset += 8;

        // update_cregs (vicii-draw-cycle.c:585-590): transfer chip-level pending
        // to local last_color_reg (for next-cycle grey-dot check); reset chip pending.
        LastColorReg              = _vic.VicLastColorRegWrite;
        LastColorValue            = _vic.VicLastColorValueWrite;
        _vic.VicLastColorRegWrite = 0xFF;
    }

    // ---------------------------------------------------------------
    // V6: sprite render pipeline (draw_sprites8 vicii-draw-cycle.c:469-532)
    // ---------------------------------------------------------------

    /// <summary>
    /// Per-pixel sprite trigger: activates sprites whose x_pipe position matches
    /// xpos exactly. Corresponds to VICE trigger_sprites (vicii-draw-cycle.c:318-340).
    /// Sets sbuf_expx_flops, sbuf_mc_flops, sprite_active_bits unconditionally on match.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TriggerSprites(int xpos, byte candidateBits)
    {
        if (candidateBits == 0 || _spritePendingBits == 0) return;
        for (int s = 0; s < 8; s++)
        {
            byte m = (byte)(1 << s);
            if ((candidateBits & m) != 0
                && (_spritePendingBits & m) != 0
                && (_spriteActiveBits & m) == 0
                && (_spriteHaltBits   & m) == 0)
            {
                if (xpos == _spriteXPipe[s])
                {
                    _sbufExpxFlops    |= m;
                    _sbufMcFlops      |= m;
                    _spriteActiveBits |= m;
                }
            }
        }
    }

    /// <summary>
    /// Per-pixel sprite draw: advances sbuf shift registers, computes pixel values,
    /// writes sprite color symbolic codes to RenderBuffer, and accumulates collision
    /// masks. Corresponds to VICE draw_sprites (vicii-draw-cycle.c:342-430).
    ///
    /// Symbolic codes written to RenderBuffer:
    ///   0x25 = COL_D025 (sprite MC1, $D025)
    ///   0x26 = COL_D026 (sprite MC2, $D026)
    ///   0x27..0x2E = COL_D027 + sprite_num (sprite individual color)
    /// DrawColors8 resolves these via Cregs[] exactly as for background colors.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawSpritePixel(int i)
    {
        if (_spriteActiveBits == 0) return;

        int  activeSpriteNum = -1;
        byte collisionMask   = 0;

        for (int s = 7; s >= 0; s--)
        {
            byte m = (byte)(1 << s);
            if ((_spriteActiveBits & m) == 0) continue;

            if (_sbufReg[s] != 0 || _sbufPixelReg[s] != 0)
            {
                if ((_spriteHaltBits & m) == 0)
                {
                    if ((_sbufExpxFlops & m) != 0)
                    {
                        if ((_spriteMcBits & m) != 0)
                        {
                            // MC sprite: fetch 2-bit pair when mc_flop is set.
                            if ((_sbufMcFlops & m) != 0)
                                _sbufPixelReg[s] = (int)((_sbufReg[s] >> 22) & 0x03);
                            _sbufMcFlops ^= m;  // toggle MC clock every expx pixel
                        }
                        else
                        {
                            // Hires sprite: fetch 1 bit, shift to 0 or 2.
                            _sbufPixelReg[s] = (int)(((_sbufReg[s] >> 23) & 0x01) << 1);
                        }
                    }

                    // Shift sbuf only when expx_flop is set (VICE vicii-draw-cycle.c:377-379).
                    if ((_sbufExpxFlops & m) != 0)
                        _sbufReg[s] <<= 1;

                    // Update expx_flop: toggle for expanded, set for non-expanded.
                    if ((_spriteExpxBits & m) != 0)
                        _sbufExpxFlops ^= m;
                    else
                        _sbufExpxFlops |= m;
                }

                // Accumulate collision if this sprite has a non-transparent pixel.
                if (_sbufPixelReg[s] != 0)
                {
                    activeSpriteNum = s;
                    collisionMask  |= m;
                }
            }
            else
            {
                // sbuf drained: deactivate sprite (VICE vicii-draw-cycle.c:395-397).
                _spriteActiveBits &= (byte)~m;
            }
        }

        if (collisionMask != 0)
        {
            // Determine if graphics pixel at this position is foreground (pri_buffer[i]).
            byte pixelPri = PriBuffer[i];
            int  as_      = activeSpriteNum;  // winner = lowest-numbered opaque sprite
            bool spri     = (_spritePriBits & (1 << as_)) != 0;

            // Write sprite color to render_buffer unless winner is behind + foreground
            // graphics pixel (VICE vicii-draw-cycle.c:401-419).
            if (!(pixelPri != 0 && spri))
            {
                RenderBuffer[i] = _sbufPixelReg[as_] switch
                {
                    1 => 0x25,               // COL_D025: sprite MC1 color
                    2 => (byte)(0x27 + as_), // COL_D027+n: sprite individual color
                    3 => 0x26,               // COL_D026: sprite MC2 color
                    _ => RenderBuffer[i],    // transparent: leave background
                };
            }

            // Sprite-background collision: any foreground graphics pixel under any
            // opaque sprite pixel (vicii-draw-cycle.c:420-424).
            if (pixelPri != 0)
                SpriteSbCollisionThisCycle |= collisionMask;
        }

        // Sprite-sprite collision: two or more sprites opaque at this pixel
        // (vicii-draw-cycle.c:426-429).
        if ((collisionMask & (collisionMask - 1)) != 0)
            SpriteSsCollisionThisCycle |= collisionMask;
    }

    /// <summary>
    /// 6569 (color_latency=1) MC-bits update at pixel 7.
    /// Clears sbuf_mc_flops for bits that TOGGLED in $D01C since last update
    /// (VICE update_sprite_mc_bits_6569, vicii-draw-cycle.c:433-439).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSpriteMcBits6569()
    {
        byte nextMcBits = _regs[0x1C];
        byte toggled    = (byte)(nextMcBits ^ _spriteMcBits);
        _sbufMcFlops   &= (byte)~toggled;
        _spriteMcBits   = nextMcBits;
    }

    /// <summary>
    /// 8565 (color_latency=0) MC-bits update at pixel 6.
    /// XORs sbuf_mc_flops with (toggled AND NOT expx_flops)
    /// (VICE update_sprite_mc_bits_8565, vicii-draw-cycle.c:442-448).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateSpriteMcBits8565()
    {
        byte nextMcBits = _regs[0x1C];
        byte toggled    = (byte)(nextMcBits ^ _spriteMcBits);
        _sbufMcFlops   ^= (byte)(toggled & ~_sbufExpxFlops);
        _spriteMcBits   = nextMcBits;
    }

    /// <summary>
    /// Per-cycle sprite render pipeline (VICE draw_sprites8, vicii-draw-cycle.c:469-532).
    /// Called each cycle from Mos6569.Tick AFTER DrawGraphics8 (which fills PriBuffer)
    /// and BEFORE DrawColors8 (which resolves symbolic color codes from RenderBuffer).
    ///
    /// PLAN-VICEPARITY-001 audit H1: VICE calls draw_sprites8(cycle_flags_pipe)
    /// (vicii-draw-cycle.c:681), so <paramref name="flagsRasterX"/> is the
    /// PREVIOUS cycle's RasterX (Mos6569's cycle_flags_pipe equivalent). All
    /// cycle-derived inputs - xpos, ChkSprDisp, the SprPtr/SprDma1 pixel
    /// events - key off that piped cycle; register and sprite state stay live.
    ///
    /// Pixel sequence mirrors VICE exactly: trigger+draw per pixel, with DMA halt/reload
    /// at pixels 2-4, MC-bits update at pixel 6 (8565) or 7 (6569), and xpos pipe
    /// latch (update_sprite_xpos) at the end of pixel 7.
    ///
    /// Writes SpriteSsCollisionThisCycle and SpriteSbCollisionThisCycle for Mos6569
    /// first-appearance IRQ gate (vicii-cycle.c:427-433).
    /// </summary>
    internal void DrawSprites8(int flagsRasterX, byte spriteDisplayBits)
    {
        // xpos: cycle_get_xpos(cycle_flags) (vicii-chip-model.h:164-167)
        // returns the piped cycle's merged xpos, which the table build stores
        // as the PHI1 xpos floored to 8 (vicii-chip-model.c:767,
        // entry |= (xpos_phi[0] >> 3) << XPOS_B). audit M11: base/wrap follow
        // the model's cycle_tab: PAL Phi1(1)=0x194 wrapping at 0x1F8
        // (:112-238); both NTSC tables start at 0x19c wrapping at 0x200
        // (:273/:438), and NTSC-65 holds 0x184 for one extra cycle at rc62
        // (the Phi1(62)/Phi1(63) stall, :395-397). Beam anchor: sprite X=24
        // (the CSEL=1 display edge) triggers during the cycle-17 draw on
        // every model. A negative flagsRasterX models VICE's zero flags word
        // before the first draw (xpos 0).
        int cyclesPerLine = _vic.CyclesPerLine;
        int xpos = flagsRasterX >= 0 ? _vic.FlooredPhi1Xpos(flagsRasterX) : 0;

        // ChkSprDisp rides the rc57 flags on PAL and old NTSC (Phi1(58),
        // vicii-chip-model.c:226/:552) and rc58 on NTSC-65 (Phi1(59), :389;
        // audit M4), consumed one cycle later via the pipe.
        bool sprEn = flagsRasterX == (cyclesPerLine == Mos6569.NtscCyclesPerLine ? 58 : 57);

        // DMA tables: bitmask of sprite(s) whose SprPtr/SprDma1 flags ride the
        // piped cycle (vicii-draw-cycle.c:481-486 reading the passed flags);
        // audit M11: per-model tables matching the cycle_tab layouts.
        byte[] dma0Table = cyclesPerLine == Mos6569.NtscCyclesPerLine
            ? s_ntscDmaCycle0
            : cyclesPerLine == Mos6569.NtscOldCyclesPerLine ? s_ntscOldDmaCycle0 : s_palDmaCycle0;
        byte[] dma2Table = cyclesPerLine == Mos6569.NtscCyclesPerLine
            ? s_ntscDmaCycle2
            : cyclesPerLine == Mos6569.NtscOldCyclesPerLine ? s_ntscOldDmaCycle2 : s_palDmaCycle2;
        byte dmaCycle0 = (uint)flagsRasterX < (uint)dma0Table.Length ? dma0Table[flagsRasterX] : (byte)0;
        byte dmaCycle2 = (uint)flagsRasterX < (uint)dma2Table.Length ? dma2Table[flagsRasterX] : (byte)0;

        // get_trigger_candidates: coarse xpos window check (VICE vicii-draw-cycle.c:304-316).
        byte candidateBits = 0;
        for (int s = 0; s < 8; s++)
        {
            if ((xpos & 0x1F8) == (_spriteXPipe[s] & 0x1F8))
                candidateBits |= (byte)(1 << s);
        }

        SpriteSsCollisionThisCycle = 0;
        SpriteSbCollisionThisCycle = 0;

        // pixel 0
        TriggerSprites(xpos + 0, candidateBits);
        DrawSpritePixel(0);

        // pixel 1
        TriggerSprites(xpos + 1, candidateBits);
        DrawSpritePixel(1);

        // pixel 2: deactivate sprite under SprDma1 reload
        _spriteActiveBits &= (byte)~dmaCycle2;
        TriggerSprites(xpos + 2, candidateBits);
        DrawSpritePixel(2);

        // pixel 3: halt sprite under SprPtr
        _spriteHaltBits |= dmaCycle0;
        TriggerSprites(xpos + 3, candidateBits);
        DrawSpritePixel(3);

        // pixel 4: copy pending display bits when ChkSprDisp; reload sbuf from sprite data
        if (sprEn)
            _spritePendingBits = spriteDisplayBits;
        if (dmaCycle2 != 0)
        {
            int sn = BitOperations.TrailingZeroCount((uint)dmaCycle2);
            // VICE update_sprite_data (vicii-draw-cycle.c:451-457) gates on sprite_dma.
            // Without the guard, the DMA2 table fires for sprite N at every cycle 58
            // (even on lines where DMA is inactive), causing spurious fallback Mc increments.
            if (_vic.IsSpriteDmaActive(sn))
                _sbufReg[sn] = _vic.GetSpriteDataForRender(sn);
        }
        TriggerSprites(xpos + 4, candidateBits);
        DrawSpritePixel(4);

        // pixel 5
        TriggerSprites(xpos + 5, candidateBits);
        DrawSpritePixel(5);

        // pixel 6: update MC bits (8565 path); latch pri_bits and expx_bits
        if (!_vic.ColorLatencyEnabled)
            UpdateSpriteMcBits8565();
        _spritePriBits  = _regs[0x1B];
        _spriteExpxBits = _regs[0x1D];
        TriggerSprites(xpos + 6, candidateBits);
        DrawSpritePixel(6);

        // pixel 7: update MC bits (6569 path); release halt after SprDma1
        if (_vic.ColorLatencyEnabled)
            UpdateSpriteMcBits6569();
        _spriteHaltBits &= (byte)~dmaCycle2;
        TriggerSprites(xpos + 7, candidateBits);
        DrawSpritePixel(7);

        // update_sprite_xpos: latch x_pipe from live sprite X registers (vicii-draw-cycle.c:459-465).
        for (int s = 0; s < 8; s++)
            _spriteXPipe[s] = _vic.GetSpriteX(s);
    }

    // ---------------------------------------------------------------
    // V7: border render pipeline (draw_border8 vicii-draw-cycle.c:541-575)
    // ---------------------------------------------------------------

    /// <summary>
    /// Per-cycle border pixel sequencer. Exact port of <c>draw_border8</c> from
    /// <c>native/vice/vice/src/viciisc/vicii-draw-cycle.c</c> lines 541-575.
    ///
    /// <para>Called after <see cref="DrawSprites8"/> and before
    /// <see cref="DrawColors8"/> in the per-cycle pipeline so that border pixels
    /// overlay the graphics + sprite output before colour resolution runs.</para>
    ///
    /// <para>When active, fills <see cref="RenderBuffer"/> entries with
    /// <c>ColD020</c> (0x20) - the symbolic border-colour code that
    /// <see cref="DrawColors8"/> resolves via <c>Cregs[0x20]</c> to the live
    /// <c>$D020</c> palette index. This mirrors VICE's <c>COL_D020</c> sentinel +
    /// <c>cregs[]</c> resolution pipeline (vicii-draw-cycle.c:47, 592-604).</para>
    ///
    /// <para>FR-VIC-BORDER AC-01..07: draw_border8 semantics per vicii-draw-cycle.c.</para>
    /// </summary>
    /// <param name="mainBorder">Current value of <c>vicii.main_border</c>
    /// (combined horizontal + vertical flip-flop result for this cycle).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DrawBorder8(bool mainBorder)
    {
        // draw_border8 line 544: uint8_t csel = vicii.regs[0x16] & 0x8
        int csel = _regs[D016] & 0x8;
        // draw_border8 line 547: if (!border_state && !main_border) return (early exit)
        int mainInt = mainBorder ? 1 : 0;
        if ((BorderState | mainInt) == 0) return;
        // draw_border8 line 551: if (border_state && main_border) continuous border
        if ((BorderState & mainInt) != 0)
        {
            // memset(render_buffer, COL_D020, 8)
            for (int i = 0; i < 8; i++) RenderBuffer[i] = ColD020;
            return; // border_state NOT updated; already latched from prev cycle
        }
        // draw_border8 lines 556-574: csel-dependent transition logic
        if (csel != 0)
        {
            // CSEL=1: if border_state, memset 8; border_state = main_border
            if (BorderState != 0)
                for (int i = 0; i < 8; i++) RenderBuffer[i] = ColD020;
            BorderState = mainInt;
        }
        else
        {
            // CSEL=0: if border_state, memset 7; border_state = main_border;
            // if new border_state, set pixel 7
            if (BorderState != 0)
                for (int i = 0; i < 7; i++) RenderBuffer[i] = ColD020;
            BorderState = mainInt;
            if (BorderState != 0)
                RenderBuffer[7] = ColD020;
        }
    }
}
