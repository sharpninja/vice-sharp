# VIC-II + SID VICE-Fidelity Requirements (consolidated)

Status: authored 2026-07-01 while the MCP server was DOWN. This is a working plan
artifact, NOT the MCP requirements store and NOT code. On MCP reconnect the full
FR/AC/TR/TEST set below is created in the MCP requirements workflow in one clean pass
(see "Create on reconnect"). Linked TODOs: PLAN-VICEPARITY-001 (audit findings, 47),
PLAN-REQAUDIT-001 (complete requirement audit + VICE-verified ACs + TDD).

Mandate: VICE is the specification. reSID (native/vice/vice/src/resid) is the SID spec;
viciisc (native/vice/vice/src/viciisc) is the VIC-II spec. The managed C# must duplicate
VICE's algorithm EXACTLY. Because these are deterministic, every acceptance criterion is
bit-exact EQUALITY vs the VICE oracle, never tolerance. Each AC is tagged FAITHFUL
(managed already matches; its test locks a regression, green now) or DIVERGENT (managed
differs; its test starts red and is a remediation target).

## Tally
- ~38 FRs, ~466 fine-grained ACs (one per distinct VICE behavior), ~466 TESTs (one per
  AC), ~38+ TRs.
- SID: 23 FRs, ~284 ACs. VIC-II: 15 FRs, ~182 ACs.
- Full per-AC detail (VICE cite, managed cite, tag, test, red/green-now) is preserved in
  agent-outputs/ (nine files, one per algorithm area).

## Cross-cutting prerequisites (Phase 0, before any remediation slice)
1. Single-cycle reSID oracle. native/vice-shim.c drives the SID through the BATCHED
   clock(delta_t), which drops the envelope/waveform pipelines. Exact ENV3/waveform
   equality is impossible until a single-cycle clock() entry point is added to the shim.
   This is why SidEngineParityTests runs at |delta|<=12 instead of 0.
2. InternalsVisibleTo. ViceSharp.Chips does not expose internals to ViceSharp.TestHarness
   (only ViceSharp.Host does). Add InternalsVisibleTo so fine-grained internal-state ACs
   can be asserted directly instead of only through ENV3/OSC3.
3. Supersede existing records. docs/requirements FR-SID-004/005 and the current SID
   filter/combined-waveform tests describe the approximation as correct; they must be
   superseded (via PLAN-REQAUDIT-001), not duplicated.
4. Finding-ID reconciliation. Agents cited PLAN-VICEPARITY-001 finding numbers from the
   prompt; reconcile against the persisted TODO when MCP is back.
5. SID wiring inconsistency. Commodore64.cs:48 builds a 6581 with no audio backend
   (sample emission inert); ArchitectureBuilder.cs:408-421 builds an 8580 with the backend.
   Reconcile which path is canonical; every OUTPUT/resample/8580 test must pin the path.

## New findings surfaced during authoring (beyond the original 47)
- SID accumulator not masked to 24 bits (stored 32-bit).
- Power-on seeds wrong: accumulator should be 0x555555; envelope 0xaa; noise LFSR 0x7ffffe.
- reset() should PRESERVE envelope_counter and the accumulator (managed zeroes them).
- Noise LFSR is shared across voices (should be per-voice) and has a wrong zero-reseed
  (reSID never un-locks a zeroed register); missing 2-cycle shift pipeline.
- Combined-waveform shift-register corruption / lock-up not modeled.
- Hard-sync same-cycle special case missing; ring gated on triangle-selected vs saw-deselected.

## SID requirement inventory (spec: native/vice/vice/src/resid; managed: src/ViceSharp.Chips/Audio)
- FR-SID-ENV (54 ACs; 51 faithful, 3 divergent): envelope state machine. Divergent AC-07
  reset-preserve-counter, AC-08 power-up 0xaa, AC-50 output model_dac. Detail: sid-envelope.
- FR-SID-WAVE-ACC (6): 24-bit accumulator; mask/seed/reset divergences. Detail: sid-waveform-1.
- FR-SID-WAVE-SAWTRI (4): sawtooth/triangle 12-bit. sid-waveform-1.
- FR-SID-WAVE-PULSE (7): pulse comparator [finding 05]. sid-waveform-1.
- FR-SID-WAVE-SYNC (6): hard sync rising edge [07]. sid-waveform-1.
- FR-SID-WAVE-RING (4): ring fold-MSB substitution [06]. sid-waveform-1.
- FR-SID-WAVE-TESTBIT (8): test-bit state machine [09]. sid-waveform-1.
- FR-SID-WAVE-NOISE (19): 23-bit per-voice LFSR, 2-cycle pipeline, corruption [08]. sid-waveform-2.
- FR-SID-WAVE-COMBINED (17): measured ROM tables, not AND [04]. sid-waveform-2.
- FR-SID-OSC3ENV3 (11): OSC3 selected-waveform readback [10]. sid-waveform-2.
- FR-SID-WAVE-DACRES (11): 12-bit path + model_dac R-2R [11]. sid-waveform-2.
- FR-SID-FILTER-6581 (30): two-integrator EKV/VCR/opamp model [12]. sid-filter.
- FR-SID-FILTER-8580 (14): solve_integrate_8580 / resGain [17]. sid-filter.
- FR-SID-EXTFILT (7): 16kHz LP + 16Hz HP output stage [15]. sid-filter.
- FR-SID-CUTOFFDAC (8): build_dac_table R-2R + f0_dac [13]. sid-filter.
- FR-SID-FILTER-CLOCK (5): per-phi2 filter clocking [16]. sid-filter.
- FR-SID-VOICE (11): (wave-wave_zero)*env multiplying DAC [18]. sid-top.
- FR-SID-MIXVOL (13): set_sum_mix 3OFF + nonlinear gain[vol] [18,19]. sid-top.
- FR-SID-OUTPUT (13): extfilt.output, amplify/scaleFactor, resample [20,23]. sid-top.
- FR-SID-CLOCK (11): per-phi2 chain env/osc/sync/filter/extfilt/bus [13]. sid-top.
- FR-SID-DATABUS (10): bus_value TTL fade, write-only readback [21]. sid-top.
- FR-SID-POT (4): readPOT 0xff idle [22]. sid-top.
- FR-SID-8580 (11): write pipeline, scaleFactor, databus TTL, variant [23]. sid-top.

## VIC-II requirement inventory (spec: native/vice/vice/src/viciisc; managed: src/ViceSharp.Chips/VicIi + src/ViceSharp.Core/C64MemoryMap.cs)
- FR-VIC-CYCLE (19): VC/RC/VMLI/idle/badline; VC-not-reset [33], badline 0xF7 [34], DEN recheck [35]. Detail: vic-cycle-fetch.
- FR-VIC-FETCH (15): c/g-access split, prefetch_cycles, reg11_delay [36,37]. vic-cycle-fetch.
- FR-VIC-MATRIX-ADDR (10): v_fetch_addr from VC, 40/row stride [31,32]. vic-cycle-fetch.
- FR-VIC-DRAW-GFX (15): 8px/cycle shift-register draw [27]. vic-draw.
- FR-VIC-DRAW-COLOR (10): cregs pipeline, color-latency, grey-dot [28]. vic-draw.
- FR-VIC-XSCROLL (4): $D016 fine scroll pipe [29]. vic-draw.
- FR-VIC-DISPLAYMODE (9): mid-line mode edges at px 4/6 [30]. vic-draw.
- FR-VIC-SPRITE-RENDER (13): sbuf shift regs, xpos trigger, flops [38]. vic-sprites.
- FR-VIC-SPRITE-DMA (14): mc/mcbase/exp_flop, display-bit latch [39]. vic-sprites.
- FR-VIC-SPRITE-COLLISION (11): per-pixel collisions, first-appearance IRQ [40]. vic-sprites.
- FR-VIC-SPRITE-PRIORITY (6): winner-first behind test [41]. vic-sprites.
- FR-VIC-BORDER (14): per-cycle draw_border8, CSEL pixel-7 edge, FF cycle base [42,43]. vic-border-irq.
- FR-VIC-RASTER-IRQ (13): fire at cycle 0, triggered latch, $D012 guard [44,45]. vic-border-irq.
- FR-VIC-REGISTERS (15): read masks, deferred collision clear [46]. vic-border-irq.
- FR-VIC-LIGHTPEN (14): $D013 xpos formula, guards, retrigger [47]. vic-border-irq.

## Remediation plan (Byrd gates; each slice = an FR or small FR group)
- Phase 0 (foundation, no algorithm change): single-cycle reSID shim oracle;
  InternalsVisibleTo for ViceSharp.Chips; retire the false-confidence tests
  (SidEngineParityTests sawtooth-only, SidCombinedWaveformTests asserting the AND).
- SID slices (reSID): S1 localized waveform logic (PULSE/SYNC/RING/TESTBIT + envelope
  power-up/reset); S2 12-bit waveform + combined ROM tables + noise LFSR + voice DAC +
  DACRES; S3 filter/extfilt/DAC full port + per-phi2 CLOCK + OUTPUT/resample + MIXVOL/3OFF
  + DATABUS + POT + 8580 pipeline.
- VIC-II slices (viciisc): V1 localized fixes (CYCLE VC-reset/badline/DEN, MATRIX-ADDR
  38-col stride, BORDER cycle-base, RASTER-IRQ timing/guard, REGISTERS deferred clear,
  LIGHTPEN formula); V2 per-cycle FETCH pipeline (gbuf/vbuf/cbuf, prefetch, reg11_delay);
  V3 per-cycle DRAW (graphics/color/xscroll/mode + BORDER draw_border8 + SPRITES pipeline
  + per-pixel collisions + winner-first priority).
- Gate per slice: 100 percent of that slice's AC tests green (DIVERGENT flip red-to-green,
  FAITHFUL stay green), plus the existing determinism/checkpoint suites.
- SID and VIC-II are independent tracks; S* and V* can proceed in parallel after Phase 0.

## Create on reconnect
1. Verify MCP trust (marker signature + /health nonce).
2. Through the plugin requirements workflow, create each FR (with its ACs), each TR, and
   each TEST from this index + agent-outputs/, and the FR->TR->TEST mappings.
3. Supersede docs/requirements FR-SID-004/005 (do not duplicate); reconcile finding IDs
   against PLAN-VICEPARITY-001.
4. Record the created requirement IDs in PLAN-REQAUDIT-001 / PLAN-VICEPARITY-001 and the
   session log.

## Full detail
The full per-AC detail (each AC with its VICE cite, managed cite, FAITHFUL/DIVERGENT tag,
its TEST, and red/green-now state) was authored by nine per-area agents and is recorded in
this work session's transcript. The agents' on-disk output files were empty (the harness
delivered results by notification, not to file), so they are NOT a durable source.

If the session transcript is lost before MCP reconnect, the full set is reproducible:
re-run the nine authoring agents (areas: sid-envelope, sid-waveform-1, sid-waveform-2,
sid-filter, sid-top, vic-cycle-fetch, vic-draw, vic-sprites, vic-border-irq) using this
README's FR/finding inventory plus the VICE source (native/vice/vice/src/{resid,viciisc})
and the managed files as inputs. The FR list, AC counts, findings, prerequisites, and
remediation gates above are the durable scaffold for that regeneration and for MCP creation.
