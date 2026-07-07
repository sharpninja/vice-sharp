namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// PLAN-VICEPARITY-001 slice V6 / TR-PARITY-GATE-001: DIVERGENT (red-now
/// remediation target) parity tests for FR-VIC-SPRITE-RENDER (AC-01..04,
/// 09..13), FR-VIC-SPRITE-COLLISION (AC-01, 06..09), and
/// FR-VIC-SPRITE-PRIORITY (AC-05) from
/// artifacts/vice-parity-requirements/requirements.yaml.
///
/// These tests verify the VICE per-pixel sprite render path
/// (native/vice/vice/src/viciisc/vicii-draw-cycle.c: draw_sprites8 lines
/// 304-531, draw_sprites lines 363-420, trigger_sprites lines 318-340,
/// get_trigger_candidates lines 304-316, update_sprite_data lines 451-457,
/// update_sprite_xpos lines 459-465, update_sprite_mc_bits_6569 lines
/// 433-439, update_sprite_mc_bits_8565 lines 442-448) against the managed
/// implementation in PixelSequencer.cs and Mos6569.cs.
///
/// VicDrawSpriteFaithfulParityTests regression-locks SPRITE-RENDER
/// AC-05..08, PRIORITY AC-01..04/06, COLLISION AC-02..05/10/11; this file
/// covers only the remaining DIVERGENT ACs.
///
/// Cycle numbering: VICE is 1-based; managed RasterX = VICE cycle - 1.
/// xpos formula (managed): phi1_xpos = (0x194 + 8 * RasterX) % 0x1F8.
/// - RasterX 15: phi1_xpos = 0x14 = 20 -> trigger for sprite at X=20.
/// PAL sprite 0 DMA: RasterX 54 first DMA check; RasterX 57 ChkSprDisp;
/// RasterX 58 SprDma1+SprDma2 (VICE cycles 55/58/59).
///
/// VICE sources: native/vice/vice/src/viciisc/vicii-draw-cycle.c,
/// native/vice/vice/src/viciisc/vicii-chip-model.c (PAL cycle table lines
/// 111-238 with ChkSprDisp at Phi1(58)=RasterX 57).
/// </summary>
public sealed class VicSpriteRenderDivergentParityTests
{
    // VIC register offsets relative to base $D000.
    private const ushort SpriteX0Lo   = 0xD000;
    private const ushort SpriteX1Lo   = 0xD002;
    private const ushort SpriteY0     = 0xD001;
    private const ushort SpriteY1     = 0xD003;
    private const ushort SpriteX0Hi   = 0xD010;
    private const ushort SpriteEnable = 0xD015;
    private const ushort SpritePriReg = 0xD01B;
    private const ushort SpriteMcReg  = 0xD01C;
    private const ushort SpriteSSColl = 0xD01E;
    private const ushort SpriteSBColl = 0xD01F;
    private const ushort InterruptReg = 0xD019;

    // Sprite at X=20: phi1_xpos = (0x194 + 8*15) % 0x1F8 = 0x14 = 20 -> triggers at RasterX 15.
    private const byte SpriteTestX = 20;
    // Y=100 starts DMA on line 100; sprite renders on lines 100..120.
    private const byte SpriteTestY = 100;
    // Line we advance TO for render-path tests (line 101 passes through trigger RasterX 15).
    private const ushort TriggerLine = 101;
    // RasterX where phi1_xpos == SpriteTestX == 20.
    private const byte TriggerCycle = 15;

    // ----------------------------------------------------------------
    // FR-VIC-SPRITE-RENDER: per-pixel sbuf shift-register render path
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-01.
    /// Use case: VICE get_trigger_candidates (vicii-draw-cycle.c:304-316) performs
    /// a coarse 8-pixel window check (xpos and 0x1F8) == (sprite_x_pipe and 0x1F8)
    /// before trigger_sprites does the exact-X match. The managed VideoRenderer had
    /// no candidate or trigger stage at all (finding 38).
    /// Acceptance: after sprite 0 display bit is set, sbuf loaded, and the raster
    /// reaches RasterX 15 (phi1_xpos=20=sprite_x_pipe[0]), GetSpriteActiveBits()
    /// bit 0 must be 1 (sprite triggered and rendering).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-01", ParityTag.Divergent, pending: false)]
    public void SpriteRender_TriggerCandidates_SpriteActiveAtExactXpos()
    {
        var vic = BuildVic();
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, SpriteTestY, 57);
        LatchOpaqueData(vic, 0);

        AdvanceTo(vic, TriggerLine, TriggerCycle);

        // VICE trigger_sprites fires at exact xpos match: sprite_active_bits |= 1.
        Assert.NotEqual(0, vic.GetSpriteActiveBits() & 0x01);
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-02.
    /// Use case: VICE trigger_sprites (vicii-draw-cycle.c:318-340) activates sprite
    /// rendering at the SINGLE cycle where xpos == sprite_x_pipe; one cycle before
    /// the match the sprite is not yet active. The managed bounding-box test
    /// activated sprites across their entire rendered width (finding 38).
    /// Acceptance: at RasterX 14 (phi1_xpos=0x0C != 20), GetSpriteActiveBits() bit 0
    /// is 0; at RasterX 15 (phi1_xpos=0x14=20) it is 1.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-02", ParityTag.Divergent, pending: false)]
    public void SpriteRender_ExactXActivation_NotActiveOneCycleBefore()
    {
        var vic = BuildVic();
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, SpriteTestY, 57);
        LatchOpaqueData(vic, 0);

        // One cycle before trigger: phi1_xpos = (0x194 + 8*14) % 0x1F8 = 0x0C = 12 != 20.
        AdvanceTo(vic, TriggerLine, (byte)(TriggerCycle - 1));
        Assert.Equal(0, vic.GetSpriteActiveBits() & 0x01);

        // Trigger cycle: phi1_xpos = 20 == sprite_x_pipe[0].
        AdvanceTo(vic, TriggerLine, TriggerCycle);
        Assert.NotEqual(0, vic.GetSpriteActiveBits() & 0x01);
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-03.
    /// Use case: VICE renders while sbuf_reg or sbuf_pixel_reg is non-zero (24-bit
    /// drain; 24 pixels hires or 48 X-expanded), stopping when both are empty. The
    /// managed code rendered a fixed-width window from memory rather than a shift
    /// register (finding 38).
    /// Acceptance: sbuf after trigger (data=0xFFFFFF) is non-zero at RasterX 15 and
    /// 16, and exactly 0 at RasterX 19 (all 32 bits shifted out: 4 at RasterX 15
    /// (trigger at pixel 4) + 8 each at RasterX 16, 17, 18, 19 = 36 total; sbuf
    /// becomes 0 when all 32 bits of the uint32 shift register drain out).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-03", ParityTag.Divergent, pending: false)]
    public void SpriteRender_SbufDrains24BitsNonExpanded()
    {
        var vic = BuildVic();
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, SpriteTestY, 57);
        LatchOpaqueData(vic, 0); // 0xFFFFFF

        AdvanceTo(vic, TriggerLine, TriggerCycle);
        Assert.NotEqual(0u, vic.GetSbufReg(0)); // sbuf loaded, non-zero at trigger

        AdvanceTo(vic, TriggerLine, (byte)(TriggerCycle + 1));
        Assert.NotEqual(0u, vic.GetSbufReg(0)); // still draining

        // After trigger at pixel 4 of RasterX 15 (4 shifts) plus RasterX 16..19
        // (8 shifts each = 32 more), total 36 shifts empties the 32-bit register.
        AdvanceTo(vic, TriggerLine, (byte)(TriggerCycle + 4));
        Assert.Equal(0u, vic.GetSbufReg(0));
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-04.
    /// Use case: VICE hires sprite pixel formula is px = ((sbuf >> 23) and 1) and lt; and lt; 1
    /// (vicii-draw-cycle.c:371-372), yielding px=2 for MSB set (opaque) or px=0
    /// (transparent). The managed path used an opacity test on the geometry path
    /// instead (finding 38).
    /// Acceptance: load sbuf=0x100000 (bit 20 set); the sprite triggers at pixel 4
    /// of RasterX 15, so 4 shifts occur in that cycle. After shift 3 sbuf has bit
    /// 23 set (0x100000 shifted 3 places = 0x800000); that is the LAST pixel of the
    /// batch (pixel 7), giving GetSbufPixelReg(0)==2. At RasterX 16, sbuf shifts 8
    /// more times (total 12), emptying all bits, so GetSbufPixelReg(0)==0 and
    /// GetSbufReg(0)==0.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-04", ParityTag.Divergent, pending: false)]
    public void SpriteRender_HiresPixelMsbFirst_PxFormula()
    {
        var vic = BuildVic();
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteEnable, 0x01);

        // Sprite data = 0x100000: bit 20 set. Trigger at pixel 4 of RasterX 15.
        // Pixel 4: sbuf=0x100000, shift -> 0x200000; pixel_reg=0 (bit 23=0).
        // Pixel 5: sbuf=0x200000, shift -> 0x400000; pixel_reg=0.
        // Pixel 6: sbuf=0x400000, shift -> 0x800000; pixel_reg=0.
        // Pixel 7: sbuf=0x800000 BEFORE shift -> bit 23=1 -> pixel_reg=2 (opaque hires).
        // After shift: sbuf=0x01000000. GetSbufPixelReg(0)==2 at end of RasterX 15.
        AdvanceTo(vic, SpriteTestY, 57);
        vic.LatchSpriteData(0, 0, 0x10); // hi=0x10 -> data = 0x100000
        vic.LatchSpriteData(0, 1, 0x00);
        vic.LatchSpriteData(0, 2, 0x00);

        AdvanceTo(vic, TriggerLine, TriggerCycle);
        // Last pixel (pixel 7) of batch has bit 23 set -> pixel_reg==2 (opaque hires).
        Assert.Equal(2, vic.GetSbufPixelReg(0));

        // After all pixels shifted: sbuf = 0 -> pixel reg = 0 at next cycle.
        // 0x01000000 shifted 8 more times (RasterX 16) = 0x00000000.
        AdvanceTo(vic, TriggerLine, (byte)(TriggerCycle + 1));
        Assert.Equal(0, vic.GetSbufPixelReg(0));
        Assert.Equal(0u, vic.GetSbufReg(0));
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-09.
    /// Use case: VICE update_sprite_xpos (vicii-draw-cycle.c:459-465) latches
    /// sprite_x_pipe from vicii.sprite[s].x at the end of each draw_sprites8 call
    /// (after pixel 7). The managed VideoRenderer read the live sprite X register
    /// per pixel rather than a one-cycle-lagged pipe (finding 38).
    /// Acceptance: immediately after writing sprite 0 X=100, GetSpriteXPipe(0) is
    /// still 0 (no latch fired yet). After one Tick(), GetSpriteXPipe(0)==100
    /// (latched by update_sprite_xpos at pixel 7).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-09", ParityTag.Divergent, pending: false)]
    public void SpriteRender_XpipeLatched_EndOfCycle()
    {
        var vic = BuildVic();
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, 0); // initial X = 0

        // Pipe holds previous value (0) before any Tick.
        Assert.Equal(0, vic.GetSpriteXPipe(0));

        // Write new X = 100.
        vic.Write(SpriteX0Lo, 100);

        // Pipe must NOT yet reflect the write (update_sprite_xpos fires at pixel 7,
        // which has not run yet).
        Assert.Equal(0, vic.GetSpriteXPipe(0));

        // One full Tick advances through pixel 7 -> update_sprite_xpos latches X=100.
        vic.Tick();
        Assert.Equal(100, vic.GetSpriteXPipe(0));
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-10.
    /// Use case: VICE update_sprite_data (vicii-draw-cycle.c:451-457) copies
    /// vicii.sprite[s].data into sbuf_reg at the DMA1/DMA2 cycle for sprite s
    /// (SprDma1/SprDma2 in the PAL cycle table, RasterX 58 for sprite 0). The
    /// managed renderer read sprite bytes from video RAM per pixel on the geometry
    /// path (finding 38).
    /// Acceptance: after LatchSpriteData loads sprite 0 data=0xA1B2C3, advancing
    /// to RasterX 58 (VICE cycle 59: Phi1=SprDma1(0), Phi2=SprDma2(0)) causes
    /// GetSbufReg(0)==0xA1B2C3.
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-10", ParityTag.Divergent, pending: false)]
    public void SpriteRender_SbufReloadedFromDataAtDmaCycle()
    {
        var vic = BuildVic();
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteEnable, 0x01);

        // DMA activates at RasterX 54 on line 100.
        AdvanceTo(vic, SpriteTestY, 54);

        // Latch sprite data that update_sprite_data will copy into sbuf_reg at DMA1/DMA2.
        vic.LatchSpriteData(0, 0, 0xA1);
        vic.LatchSpriteData(0, 1, 0xB2);
        vic.LatchSpriteData(0, 2, 0xC3); // sprite.Data = 0xA1B2C3

        // The SprDma1(0) flags ride RasterX 58 (VICE Phi1(59)), but draw_sprites8
        // consumes them one cycle later via cycle_flags_pipe
        // (vicii-draw-cycle.c:679-687, audit H1), so update_sprite_data copies
        // sprite[0].data into sbuf_reg during the RasterX 59 draw.
        AdvanceTo(vic, SpriteTestY, 59);
        Assert.Equal(0xA1B2C3u, vic.GetSbufReg(0));
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-11.
    /// Use case: VICE update_sprite_mc_bits_6569 (vicii-draw-cycle.c:433-439) runs
    /// at pixel 7 on color-latency chips (6569) and clears sbuf_mc_flops for any
    /// bit that TOGGLED in $D01C: sbuf_mc_flops and= ~toggled. The managed code
    /// read the live $D01C register per pixel (finding 38).
    /// Acceptance: with sprite 0 MC active (sbuf_mc_flops[0]=1 set at trigger),
    /// clearing $D01C bit 0 (toggled=1) before the next batch causes
    /// GetSbufMcFlops() bit 0 to be 0 after that batch's pixel 7 (cleared by
    /// update_sprite_mc_bits_6569 on 6569).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-11", ParityTag.Divergent, pending: false)]
    public void SpriteRender_6569_D01cClearsSbufMcFlopsAtPixel7()
    {
        var vic = BuildVic(); // default 6569 (ColorLatency=1)
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteEnable, 0x01);
        vic.Write(SpriteMcReg, 0x01); // sprite 0 multicolor

        AdvanceTo(vic, SpriteTestY, 57);
        LatchOpaqueData(vic, 0);

        // At trigger: sbuf_mc_flops |= 1 (set in trigger_sprites).
        AdvanceTo(vic, TriggerLine, TriggerCycle);
        Assert.NotEqual(0, vic.GetSbufMcFlops() & 0x01);

        // Clear $D01C bit 0: toggled = 0 ^ 1 = 1.
        // update_sprite_mc_bits_6569 at pixel 7 clears sbuf_mc_flops[0].
        vic.Write(SpriteMcReg, 0x00);
        AdvanceTo(vic, TriggerLine, (byte)(TriggerCycle + 1));
        Assert.Equal(0, vic.GetSbufMcFlops() & 0x01);
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-12.
    /// Use case: VICE update_sprite_mc_bits_8565 (vicii-draw-cycle.c:442-448) runs
    /// at pixel 6 on non-color-latency chips (8565) and XORs sbuf_mc_flops with
    /// (toggled and ~sbuf_expx_flops): sbuf_mc_flops ^= (toggled and ~expx). The
    /// managed code read the live $D01C per pixel (finding 38).
    /// Acceptance: on a Mos8565 with sbuf_mc_flops[0]=1 and sbuf_expx_flops[0]=1
    /// (set at trigger), setting $D01C bit 0 (toggled=1; ~expx=0) leaves
    /// sbuf_mc_flops[0]=1 unchanged (XOR mask is 0 due to expx masking).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-12", ParityTag.Divergent, pending: false)]
    public void SpriteRender_8565_D01cXorsSbufMcFlopsAtPixel6MaskedByExpx()
    {
        // Mos8565 has ColorLatency=false (color_latency=0, 8565 variant).
        var vic = Build8565Vic();
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteEnable, 0x01);

        AdvanceTo(vic, SpriteTestY, 57);
        LatchOpaqueData(vic, 0);

        // At trigger: sbuf_expx_flops[0] = 1 set in trigger_sprites; MC=0 initially.
        AdvanceTo(vic, TriggerLine, TriggerCycle);
        // sbuf_mc_flops[0] is set at trigger because the sprite is single-cycle activated.

        // Set $D01C bit 0 (toggled = 0 XOR 1 = 1; but ~expx_flop[0] = 0 -> XOR mask = 0).
        vic.Write(SpriteMcReg, 0x01);
        AdvanceTo(vic, TriggerLine, (byte)(TriggerCycle + 1));
        // sbuf_mc_flops[0] should be UNCHANGED (expx masked the toggle).
        Assert.NotEqual(0, vic.GetSbufMcFlops() & 0x01);
    }

    /// <summary>
    /// FR-VIC-SPRITE-RENDER AC-13.
    /// Use case: VICE latches sprite_pri_bits and sprite_expx_bits from $D01B/$D01D
    /// at pixel 6 of each draw_sprites8 call (vicii-draw-cycle.c:518-519). The
    /// managed code read live register values on every pixel instead (finding 38).
    /// Acceptance: initially sprite_pri_bits=0; write $D01B=0x01; after one complete
    /// Tick() that includes pixel 6, GetSpritePriBits()==0x01 (latched at pixel 6).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-RENDER-13", ParityTag.Divergent, pending: false)]
    public void SpriteRender_PriBitsLatchedAtPixel6()
    {
        var vic = BuildVic();

        // Initially sprite_pri_bits = 0.
        Assert.Equal(0, vic.GetSpritePriBits());

        // Write $D01B = 0x01 (sprite 0 BEHIND).
        vic.Write(SpritePriReg, 0x01);

        // Before pixel 6 of the first draw_sprites8 batch, pri_bits is still 0.
        // After one full Tick() (which includes pixel 6), GetSpritePriBits() == 0x01.
        vic.Tick();
        Assert.Equal(0x01, vic.GetSpritePriBits());
    }

    // ----------------------------------------------------------------
    // FR-VIC-SPRITE-COLLISION: per-pixel collision tracking
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-01.
    /// Use case: VICE draw_sprites (vicii-draw-cycle.c:391-394) accumulates
    /// sprite-sprite collision bits per pixel during rendering. The managed code
    /// ran a geometric bounding-box raster ONCE per scanline at line wrap
    /// (finding 40).
    /// Acceptance: two sprites at the same X/Y with opaque data. After exactly the
    /// one Tick() that contains the first overlapping pixel, SpriteSpriteCollision
    /// must already be non-zero (mid-line detection, before end-of-line wrap).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-01", ParityTag.Divergent, pending: false)]
    public void SpriteCollision_PerPixelCollision_DetectedMidLine()
    {
        var vic = BuildVicWithSprites();

        // Set up sprites 0 and 1 at the same position (guaranteed pixel overlap).
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteX1Lo, SpriteTestX);
        vic.Write(SpriteY1, SpriteTestY);
        vic.Write(SpriteEnable, 0x03); // sprites 0 and 1

        AdvanceTo(vic, SpriteTestY, 57);

        // Advance to trigger cycle (xpos=SpriteTestX, sprites overlap).
        AdvanceTo(vic, TriggerLine, TriggerCycle);

        // VICE per-pixel detection: latch non-zero BEFORE end-of-line, not at wrap.
        Assert.NotEqual(0, vic.SpriteSpriteCollision);
    }

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-06.
    /// Use case: VICE clear_collisions (vicii-cycle.c:413-418) defers the $D01E
    /// zero-clear to the NEXT machine cycle after the CPU read. The managed code
    /// was clearing the latch in the same read (finding 46).
    /// Acceptance: after a sprite-sprite collision populates the latch, a CPU Read
    /// of $D01E returns the mask; a subsequent Peek of $D01E (before the next Tick)
    /// still returns the same mask (clear is deferred, not immediate).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-06", ParityTag.Divergent, pending: false)]
    public void SpriteCollision_D01eReadDefersClear()
    {
        var vic = BuildVicWithSprites();

        // Two sprites at same position with opaque data.
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteX1Lo, SpriteTestX);
        vic.Write(SpriteY1, SpriteTestY);
        vic.Write(SpriteEnable, 0x03);

        // Advance to the trigger cycle so the per-pixel SS collision fires.
        // DrawSprites8 at RasterX=TriggerCycle detects pixel overlap between sprites 0 and 1.
        AdvanceTo(vic, TriggerLine, TriggerCycle);

        // Precondition: latch is non-zero (per-pixel DrawSprites8 populated it).
        byte latch = vic.SpriteSpriteCollision;
        Assert.NotEqual(0, latch);

        // CPU Read: returns the collision mask, schedules deferred clear.
        byte readResult = vic.Read(SpriteSSColl);
        Assert.Equal(latch, readResult);

        // Deferred: latch still non-zero before the next Tick.
        Assert.NotEqual(0, vic.SpriteSpriteCollision);
    }

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-07.
    /// Use case: VICE defers the $D01F (sprite-background) zero-clear to the NEXT
    /// machine cycle after the CPU read (vicii-cycle.c:419-423). The managed code
    /// cleared in-read (finding 46).
    /// Acceptance: after a sprite-background collision, Read($D01F) returns the mask;
    /// Peek before next Tick still returns the mask (deferred clear).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-07", ParityTag.Divergent, pending: false)]
    public void SpriteCollision_D01fReadDefersClear()
    {
        var vic = BuildVicWithSprites();
        vic.Phi1MemoryReader = _ => 0xFF; // foreground pixels at sprite position

        // Sprite 0 over a foreground pixel at (SpriteTestX, SpriteTestY).
        vic.Write(0xD011, 0x1B); // DEN|RSEL|YSCROLL=3
        vic.Write(0xD016, 0x08); // CSEL=1 (40 columns, LeftBorderPixel=24)
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteEnable, 0x01);

        // Advance to TriggerCycle+2: the pipe0 load is gated by the PREVIOUS
        // cycle's VISIBLE flag (audit H1, cycle_flags_pipe,
        // vicii-draw-cycle.c:679/:687), so the first g-access (cycle 15) loads
        // at end of cycle 15 and its foreground pixels first appear in
        // PriBuffer at cycle 17 (TriggerCycle+2). The sprite triggered at
        // pixel 4 of TriggerCycle=15 and is 24px wide, so it is still active
        // at cycle 17 and the SB collision fires there (still mid-line).
        AdvanceTo(vic, TriggerLine, (byte)(TriggerCycle + 2));

        byte latch = vic.SpriteBackgroundCollision;
        Assert.NotEqual(0, latch); // per-pixel SB collision must have fired

        byte readResult = vic.Read(SpriteSBColl);
        Assert.Equal(latch, readResult);

        // Deferred: latch still non-zero before the next Tick.
        Assert.NotEqual(0, vic.SpriteBackgroundCollision);
    }

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-08.
    /// Use case: VICE fires the sprite-sprite IRQ ($D019 bit 2) on the first pixel
    /// where a NEW sprite-sprite collision is detected (first-appearance edge,
    /// vicii-cycle.c:427-430). The managed code fired it at end-of-line geometric
    /// raster (finding 40).
    /// Acceptance: after the one Tick() containing the first overlapping pixel,
    /// $D019 bit 2 is already set (fired mid-line, before end-of-line wrap).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-08", ParityTag.Divergent, pending: false)]
    public void SpriteCollision_SpriteSpriteIrq_FirstAppearanceMidLine()
    {
        var vic = BuildVicWithSprites();

        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteX1Lo, SpriteTestX);
        vic.Write(SpriteY1, SpriteTestY);
        vic.Write(SpriteEnable, 0x03);

        // IRQ enable for sprite-sprite ($D01A bit 2) must be set.
        vic.Write(0xD01A, 0x04);

        AdvanceTo(vic, SpriteTestY, 57);

        // Advance to trigger cycle (first overlap pixel).
        AdvanceTo(vic, TriggerLine, TriggerCycle);

        // VICE first-appearance: $D019 bit 2 set at the FIRST pixel of overlap.
        Assert.NotEqual(0, vic.Read(InterruptReg) & 0x04);
    }

    /// <summary>
    /// FR-VIC-SPRITE-COLLISION AC-09.
    /// Use case: VICE fires the sprite-background IRQ ($D019 bit 1) on first
    /// appearance of a sprite-background collision (vicii-cycle.c:430-433). The
    /// managed code fired it at end-of-line geometric raster (finding 40).
    /// Acceptance: after the one Tick() containing the first sprite-background
    /// overlap pixel, $D019 bit 1 is set (mid-line, before end-of-line wrap).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-COLLISION-09", ParityTag.Divergent, pending: false)]
    public void SpriteCollision_SpriteBgIrq_FirstAppearanceMidLine()
    {
        var vic = BuildVicWithSprites();
        vic.Phi1MemoryReader = _ => 0xFF; // all foreground pixels

        vic.Write(0xD011, 0x1B); // DEN|RSEL|YSCROLL=3
        vic.Write(0xD016, 0x08); // CSEL=1
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, SpriteTestX);
        vic.Write(SpriteY0, SpriteTestY);
        vic.Write(SpriteEnable, 0x01);
        vic.Write(0xD01A, 0x02); // IRQ enable for sprite-bg ($D01A bit 1)

        AdvanceTo(vic, SpriteTestY, 57);

        // Advance to TriggerCycle+2: the pipe0 load is gated by the PREVIOUS
        // cycle's VISIBLE flag (audit H1, cycle_flags_pipe,
        // vicii-draw-cycle.c:679/:687), so the first g-access (cycle 15) loads
        // at end of cycle 15 and its foreground PriBuffer pixels appear at
        // cycle 17. Sprite triggered at pixel 4 of cycle 15 and is 24px wide,
        // so the SB IRQ fires at cycle 17 (still mid-line, before wrap).
        AdvanceTo(vic, TriggerLine, (byte)(TriggerCycle + 2));

        // VICE first-appearance: $D019 bit 1 set at the FIRST bg collision pixel.
        Assert.NotEqual(0, vic.Read(InterruptReg) & 0x02);
    }

    // ----------------------------------------------------------------
    // FR-VIC-SPRITE-PRIORITY: winner-first behind stop (not fall-through)
    // ----------------------------------------------------------------

    /// <summary>
    /// FR-VIC-SPRITE-PRIORITY AC-05.
    /// Use case: VICE draw_sprites (vicii-draw-cycle.c:401-419) applies the priority
    /// test only to the WINNER (lowest-numbered opaque sprite) and does NOT write
    /// render_buffer[i] when the winner is behind AND the graphics pixel is
    /// foreground. The managed VideoRenderer used "continue" rather than "return
    /// false", so a higher-numbered in-front sprite was rendered instead (finding 41).
    /// Acceptance: with sprite 0 BEHIND ($D01B bit 0=1), sprite 1 IN-FRONT, both
    /// opaque at the same pixel, and the underlying graphics pixel being foreground:
    /// the rendered pixel is BACKGROUND color (not sprite 1).
    /// </summary>
    [Fact]
    [ParityAc("TEST-VIC-SPRITE-PRIORITY-05", ParityTag.Divergent, pending: false)]
    public void SpritePriority_BehindWinnerOverFg_ShowsBackground_NotSprite1()
    {
        var vic = BuildVicWithSprites();
        vic.Phi1MemoryReader = _ => 0xFF; // all foreground pixels

        vic.Write(0xD011, 0x1B); // DEN|RSEL|YSCROLL=3 (display enabled)
        vic.Write(0xD016, 0x08); // CSEL=1 (LeftBorderPixel=24)

        // Sprite X=50, Y=100: inside display window (50 >= LeftBorderPixel=24).
        vic.Write(SpriteX0Hi, 0);
        vic.Write(SpriteX0Lo, 50);  // sprite 0 X
        vic.Write(SpriteY0, 100);
        vic.Write(SpriteX1Lo, 50);  // sprite 1 X (same position)
        vic.Write(SpriteY1, 100);
        vic.Write(SpriteEnable, 0x03);        // sprites 0 and 1
        vic.Write(SpritePriReg, 0x01);         // sprite 0 BEHIND, sprite 1 in-front
        vic.Write(0xD027, 0x02);               // sprite 0 color = 2 (red)
        vic.Write(0xD028, 0x05);               // sprite 1 color = 5 (green)

        // Advance to start of line 101 so NotifyLineCompleted(100) fires.
        // At that point FrameBuffer has line 100 rendered with PixelSequencer foreground data.
        AdvanceTo(vic, 101, 0);

        // Frame pixel for raster line 100 (VICE window starts at raster line
        // 16, VICII_PAL_NORMAL_FIRST_DISPLAYED_LINE, vicii-timing.h:68). The
        // frame x coordinate equals the sprite/beam x coordinate: frame x 0 is
        // dbuf[104] = beam xpos 0 (vicii-draw.c:71 DBUF_OFFSET with the PAL
        // 0x20 left border width).
        int frameY = 100 - VideoRenderer.PalFirstVisibleRasterLine;
        int offset = ((frameY * VideoRenderer.ScreenWidth) + 50) * 4;
        uint pixel = System.BitConverter.ToUInt32(vic.FrameBuffer, offset);

        // Background color register $D021 defaults to 0 (black) -> BGRA 0xFF000000.
        const uint BgColor = 0xFF000000u;

        // With the AC-05 bug (continue), sprite 1 (in-front, green) would render.
        // With the fix (return false when winner is behind+foreground), background renders.
        Assert.Equal(BgColor, pixel);
    }

    // ----------------------------------------------------------------
    // Test helpers
    // ----------------------------------------------------------------

    /// <summary>
    /// Minimal VIC-II with no bus backing (all VideoMemoryReader reads return 0,
    /// Phi1MemoryReader returns 0). Use for tests that only check PixelSequencer
    /// internal state via stub getters.
    /// </summary>
    private static Mos6569 BuildVic()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        return vic;
    }

    /// <summary>
    /// VIC-II with VideoMemoryReader wired to a 16KB array that contains opaque
    /// sprite data for sprites 0 and 1 at pointers 0 and 1 respectively.
    /// ScreenMemoryBase defaults to 0 after Reset (registers cleared, $D018=0).
    /// Sprite pointer table at ScreenMemoryBase+$3F8 = $03F8 and $03F9.
    /// Sprite 0: pointer=0 -> data at 0x0000..0x003F (all 0xFF = opaque).
    /// Sprite 1: pointer=1 -> data at 0x0040..0x007F (all 0xFF = opaque).
    /// </summary>
    private static Mos6569 BuildVicWithSprites()
    {
        var vic = new Mos6569(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        var mem = new byte[0x4000];
        // Fill sprite 0 pattern (pointer 0 -> address 0*64=0): all opaque.
        System.Array.Fill(mem, (byte)0xFF, 0, 64);
        // Fill sprite 1 pattern (pointer 1 -> address 1*64=64): all opaque.
        System.Array.Fill(mem, (byte)0xFF, 64, 64);
        // Sprite pointers: ScreenMemoryBase(0) + 0x3F8 + sprite.
        // mem[0x3F8]=0 (sprite 0 -> pointer 0), mem[0x3F9]=1 (sprite 1 -> pointer 1).
        mem[0x3F8] = 0;
        mem[0x3F9] = 1;
        vic.VideoMemoryReader = addr => addr < mem.Length ? mem[addr] : (byte)0;
        return vic;
    }

    /// <summary>
    /// VIC-II wired as a Mos8565 (ColorLatency=false, color_latency=0).
    /// Used for AC-12 which tests the 8565-specific MC flop XOR path.
    /// </summary>
    private static Mos8565 Build8565Vic()
    {
        var vic = new Mos8565(new BasicBus(), new InterruptLine(InterruptType.Irq));
        vic.Reset();
        return vic;
    }

    /// <summary>
    /// Latch all-ones data (0xFFFFFF) into the given sprite via three s-accesses.
    /// Called after the DMA trigger line so mc does not advance past 63.
    /// </summary>
    private static void LatchOpaqueData(Mos6569 vic, int sprite)
    {
        vic.LatchSpriteData(sprite, 0, 0xFF);
        vic.LatchSpriteData(sprite, 1, 0xFF);
        vic.LatchSpriteData(sprite, 2, 0xFF);
    }

    private static void AdvanceTo(Mos6569 vic, ushort rasterLine, byte rasterCycle)
    {
        int maxCycles = vic.TotalLines * vic.CyclesPerLine * 3;
        for (int cycle = 0; cycle < maxCycles; cycle++)
        {
            if (vic.CurrentRasterLine == rasterLine && vic.RasterX == rasterCycle)
                return;
            vic.Tick();
        }
        throw new InvalidOperationException(
            $"VIC did not reach line {rasterLine}, cycle {rasterCycle}.");
    }
}
