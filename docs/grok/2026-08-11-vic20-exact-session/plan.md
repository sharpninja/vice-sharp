# Plan: Bit-exact whole VIC-20 / full pixel FB / full cart-and-sound parity vs xvic

**Date:** 2026-08-11 (revised BDPv4)  
**Process:** Byrd Development Process v4 (`F:\GitHub\McpServer\docs\Development-Process-draft-v4.md`)  
**Oracle:** VICE **xvic** only. No invent. Screenshots are not acceptance.  
**Hostile validator:** required on every Exact claim and every phase exit that asserts Exact/done/green.  
**Umbrella MCP TODO (on approval):** `PLAN-VIC20-EXACT-001`  

### BDPv4 compliance (this plan)

| BDPv4 artifact | This plan |
|----------------|-----------|
| Functional Requirements + AC | §3 FR-* with numbered AC |
| Technical Requirements + AC | §4 TR-* with numbered AC |
| Testing Requirements | §5 TEST-* with 100% AC coverage |
| FR→TR→TEST mapping | §6 matrix (every FR AC maps ≥1 TEST; every FR maps ≥1 TR) |
| Iterative phases | §7 phases A–G |
| TDD: red → green → refactor | §8 cycle rules; each AC has named test first |
| Mocks-first then real (Byrd augmentation) | §8.2 unit tests with fakes before native/xvic integration where applicable |
| Phase exit: full prior+current suite green | §8.3 0 fail / 0 skip in scoped gate |
| Public interfaces before impl | §9 interface contracts |

### Prior plan closeout

| Prior plan | Disposition |
|------------|-------------|
| Flash cart image builder (2026-08-08) | **Closed / shipped** on `main`. Residual FE3 erase latency → Phase C under FR-VIC20-CART-001. |

---

## 1. Problem and V²

**Problem:** Managed VIC-20 is not claimable as bit-exact with xvic for whole machine, full pixel framebuffer, full cart suite, and sound.  
**Users:** Emulator developers and operators who require VICE-faithful VIC-20 for lockstep validation and software compatibility.  
**Viable:** xvic oracle + existing 10 s CPU lockstep + video state lockstep + partial carts provide a proven path.  
**Valuable:** Completes Iteration 2 exit criteria and removes invent/Partial labels that block Exact claims.

**Explicit exclude from whole-machine claim (document as Explicit Missing, non-blocking):** IEEE-488, rsuser, printer. Xbox/Store cancelled. Zip media is separate TODO.

---

## 2. Completion definition (proof)

Plan is **complete** only when:

1. All FR/TR AC below are **green** under named TEST IDs (0 fail, 0 skip in each phase gate and final umbrella gate).  
2. Audit matrix (`docs/audit-vic20-vs-vice-*.md`) lists Exact for FR scopes; only IEEE/rsuser/printer remain Explicit Missing if still excluded.  
3. Hostile validator **AGREE** receipts exist for whole-machine claim language and each FR Exact family.  
4. MCP requirements FR/TR/TEST + mappings persisted; `PLAN-VIC20-EXACT-001` done with evidence.

---

## 3. Functional Requirements (FR) and Acceptance Criteria (AC)

Each FR is work the product must do for the user/developer. Every AC is binary, testable, and owns ≥1 TEST.

### FR-VIC20-CPU-001 — CPU every-cycle lockstep vs xvic

**Statement:** Managed main CPU registers match xvic every cycle for idle READY and non-idle workloads (PAL and NTSC).

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-CPU-01 | After identical reset, every cycle for **10 s PAL** (~11_084_050 cycles) A/X/Y/S/P/PC match xvic (0 diverge). |
| AC-CPU-02 | After identical reset, every cycle for **10 s NTSC** (~10_227_270 cycles) A/X/Y/S/P/PC match xvic (0 diverge). |
| AC-CPU-03 | Non-idle workload PRG (fixed fixture) runs ≥2 s PAL every-cycle A/X/Y/S/P/PC match (0 diverge). |
| AC-CPU-04 | Same non-idle workload ≥2 s NTSC every-cycle match (0 diverge). |
| AC-CPU-05 | Failed NTSC and PAL runs never share one process without isolation (documented + harness enforces or tests run isolated). |

**Baseline:** AC-CPU-01/02 green today (`Vic20DivergeProbe`). AC-CPU-03/04 red until workload fixtures land.

### FR-VIC20-VIDSTATE-001 — VIC-I pipeline state lockstep vs xvic

**Statement:** Managed VIC-I pipeline state matches xvic every cycle for fields exported in `Vic20VideoLockstepState`.

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-VS-01 | Every-cycle match of RasterLine, RasterCycle, Area, FetchState, TextCols/Lines, YCounter, RowCounter, Blank flags, Memptr/Inc, Regs[16], Cbuf, Gbuf for **2000** cycles PAL after reset. |
| AC-VS-02 | Same for **500_000** cycles PAL after reset. |
| AC-VS-03 | When CPU multi-second lockstep runs with video compare enabled, video state match holds for full CPU budget (or first diverge is reported with ring context). |
| AC-VS-04 | NTSC model: AC-VS-01 and AC-VS-02 budgets green (or documented equal-cycle NTSC budget). |

**Baseline:** AC-VS-01/02 largely green (`Vic20VideoLockstep`). AC-VS-03/04 to ratchet.

### FR-VIC20-PIXEL-001 — Full pixel framebuffer parity vs xvic

**Statement:** Managed visible framebuffer matches xvic capture for normal-border canvas (indices and BGRA).

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-PX-01 | After READY-stable boot, managed and xvic report identical canvas width/height (PAL normal **448×284**; NTSC normal from `vic-timing.h`). |
| AC-PX-02 | Palette-index buffers SequenceEqual managed vs xvic for READY idle PAL (one full frame after sync point). |
| AC-PX-03 | Index SequenceEqual READY idle NTSC one full frame. |
| AC-PX-04 | Index Sequenceequal for one **busy** screen fixture PAL (fixed PRG or scripted reg writes). |
| AC-PX-05 | BGRA buffers SequenceEqual managed vs xvic for same cases as AC-PX-02 and AC-PX-04 (after palette alignment). |
| AC-PX-06 | Capture is not sentinel/all-black after READY; alpha 0xFF on BGRA path. |

### FR-VIC20-CART-001 — Full cartridge parity vs xvic cart model

**Statement:** Managed cart behavior matches VICE VIC-20 cart implementations for the inventoried cart set (standard + FE3 + Ultimem + Mega-Cart + any other types enabled in the shim’s xvic build).

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-CT-01 | Inventory document lists every `vic20/cart` type in the shim build and maps each to managed type or Explicit Missing (operator sign-off required for any Missing). |
| AC-CT-02 | Standard 8K/16K/BLK map: attach + read/write windows match VICE for documented bank cases (unit + attach tests). |
| AC-CT-03 | FE3: REGA/REGB modes and bank map match VICE tables for START/FLASH/SUPER_ROM/ROM_RAM/RAM1/SUPER_RAM/RAM2 (unit tests per mode). |
| AC-CT-04 | FE3 flash040: unlock AA/55, byte program AND, autoselect IDs match VICE TYPE_B (already partial; keep green). |
| AC-CT-05 | FE3 flash040: chip/sector erase **latency and status bit timing** match VICE alarms (not instant-only). |
| AC-CT-06 | Ultimem: control registers and bank windows match VICE for documented cases. |
| AC-CT-07 | Mega-Cart: ROM bank + NVRAM windows match VICE for documented cases. |
| AC-CT-08 | Each cart family marked Exact has hostile AGREE receipt. |

### FR-VIC20-SOUND-001 — Full VIC-I sound parity vs xvic

**Statement:** Managed VIC-I sound produces the same deterministic sample stream as xvic for the same registers and cycles.

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-SD-01 | Native xvic exposes a documented audio capture API (samples + rate/clock metadata). |
| AC-SD-02 | `$900A-$900E` stores update sound model with VICE side effects (not register-only). |
| AC-SD-03 | Silence (default power-on / muted path) sample buffer matches xvic for fixed cycle budget. |
| AC-SD-04 | Single-channel tone fixture sample buffer SequenceEqual vs xvic for fixed budget. |
| AC-SD-05 | Multi-register fixture sample buffer SequenceEqual vs xvic for fixed budget. |
| AC-SD-06 | Hostile AGREE for sound Exact claim. |

### FR-VIC20-BUS-001 — Bus, VIA, and open-bus fidelity required by lockstep

**Statement:** Bus and VIA behaviors required for CPU/video lockstep and standard I/O match xvic (no invent).

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-BS-01 | Color RAM store/read/peek rules remain Exact (regression). |
| AC-BS-02 | BASIC/KERNAL open-bus last-data rules remain Exact (regression). |
| AC-BS-03 | VIA Read bus-visible T1/T2 and dual VIA peeks match xvic at lockstep checkpoints. |
| AC-BS-04 | Any Via6522 SR/handshake gap that causes lockstep diverge is closed with a named test. |
| AC-BS-05 | Keyboard matrix paths used by KERNAL and test PRGs match xvic sampled peeks. |

### FR-VIC20-SNAP-001 — VIC-20 snapshot round-trip and resume

**Statement:** Snapshot save/load preserves state such that post-load lockstep with xvic holds from cycle 0 for supported modules.

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-SN-01 | Inventory of xvic .vsf VIC-20 modules documented. |
| AC-SN-02 | Unexpanded machine: save managed → load managed → register/RAM sample match pre-save. |
| AC-SN-03 | Unexpanded: load xvic snapshot into managed (or dual native/managed) → cycle-0 lockstep green for ≥1024 cycles. |
| AC-SN-04 | One cart config (FE3 or Ultimem) snapshot round-trip + short lockstep green. |

### FR-VIC20-WHOLE-001 — Whole-machine Exact claim

**Statement:** Product may claim bit-exact VIC-20 vs xvic under the audited matrix (exclusions only IEEE/rsuser/printer if still excluded).

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-WH-01 | Umbrella gate runs CPU 10 s PAL+NTSC + video state + pixel READY PAL+NTSC + sound silence+tone + cart smoke matrix with 0 fail / 0 skip. |
| AC-WH-02 | Audit document claims Exact only where hostile AGREE exists; no invent labels open. |
| AC-WH-03 | README/Iteration-Roadmap/HANDOFF state “whole VIC-20 Exact vs xvic (scoped exclusions)” with evidence links. |
| AC-WH-04 | Hostile AGREE on whole-machine claim wording. |

---

## 4. Technical Requirements (TR) and Acceptance Criteria (AC)

### TR-VIC20-ORACLE-001 — Native xvic oracle surface

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-TO-01 | `vice_xvic.dll` built via documented MINGW64 recipe; tests resolve library. |
| AC-TO-02 | Exports: step, reset, peek bus, CPU regs, `vice_vic20_get_video_state`, frame indices, visible BGRA remain stable. |
| AC-TO-03 | New export for audio capture exists and is documented in shim header. |

### TR-VIC20-PIXEL-001 — Pixel compare path

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-TP-01 | Managed exposes index buffer and BGRA buffer with identical geometry to xvic normal-border canvas. |
| AC-TP-02 | Compare sync point is defined (frame boundary / cycle count) and used by all pixel tests. |
| AC-TP-03 | Index compare is primary Exact path; BGRA depends on shared palette table bytes. |

### TR-VIC20-SOUND-001 — Sound engine and host seam

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-TS-01 | Sound implementation cites `vic20sound.c` functions; no invented envelope model. |
| AC-TS-02 | Zero-allocation hot path preserved on CPU/VIC tick (sound may allocate only off hot path / buffer fill). |
| AC-TS-03 | Deterministic: same inputs → bit-identical samples. |

### TR-VIC20-CART-001 — Cart architecture

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-TC-01 | Carts implement `IAddressSpace` / port attach without machine-specific invent in chips layer. |
| AC-TC-02 | FE3 flash040 uses shared `Flash040Core` with VICE TYPE_B constants and alarm-timed erase. |
| AC-TC-03 | Flash erase advances with system clock (or documented cycle counter) matching VICE cycle counts within AC-CT-05 tests. |

### TR-VIC20-LOCKSTEP-001 — Lockstep harness

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-TL-01 | Harness reports first diverge cycle + register ring (existing pattern). |
| AC-TL-02 | Env flags: `VICESHARP_LOCKSTEP_10S`, `VICESHARP_LOCKSTEP_2S`, new `VICESHARP_LOCKSTEP_PIXEL`, `VICESHARP_LOCKSTEP_SOUND` documented. |
| AC-TL-03 | `[Collection("NativeVice")]` + process isolation rules for multi-second tests documented in test XML docs. |

### TR-VIC20-SNAP-001 — Snapshot modules

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-TN-01 | Snapshot format modules named; load/save APIs on managed VIC-20 machine. |
| AC-TN-02 | No silent drop of modules required by AC-SN-*. |

### TR-VIC20-EXACT-POLICY-001 — Exact claim discipline

| AC ID | Acceptance criterion |
|-------|----------------------|
| AC-TX-01 | Exact requires VICE file+function citation + matching control flow + hostile AGREE. |
| AC-TX-02 | Audit header/body stay consistent (no stale “stub” when wired). |

---

## 5. Testing Requirements (TEST) — 100% AC coverage

**Rule:** Every AC in §3 and §4 maps to ≥1 TEST. No AC without a test. Skipped tests **do not** count as coverage.

### 5.1 TEST inventory (IDs and AC covered)

| TEST ID | Type | AC covered | Test class / name (to create or extend) | Red-first |
|---------|------|------------|----------------------------------------|-----------|
| TEST-VIC20-CPU-01 | Integration/native | AC-CPU-01 | `Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TenSecondPal` | Exists green |
| TEST-VIC20-CPU-02 | Integration/native | AC-CPU-02 | `Vic20DivergeProbe.EveryCycle_CpuRegs_Match_TenSecondNtsc` | Exists green |
| TEST-VIC20-CPU-03 | Integration/native | AC-CPU-03 | `Vic20DivergeProbe.EveryCycle_Workload_Match_TwoSecondPal` | **New** |
| TEST-VIC20-CPU-04 | Integration/native | AC-CPU-04 | `Vic20DivergeProbe.EveryCycle_Workload_Match_TwoSecondNtsc` | **New** |
| TEST-VIC20-CPU-05 | Unit/doc | AC-CPU-05 | `Vic20LockstepIsolationTests` or collection docs + CI note | **New** |
| TEST-VIC20-VS-01 | Integration/native | AC-VS-01 | `Vic20VideoLockstep.EveryCycle_VicI_VideoState_Match_2k` | Exists |
| TEST-VIC20-VS-02 | Integration/native | AC-VS-02 | `Vic20VideoLockstep.EveryCycle_VicI_VideoState_Match_FocusedWindow` | Exists |
| TEST-VIC20-VS-03 | Integration/native | AC-VS-03 | `Vic20DivergeProbe` with video compare forced on 10s | **Extend** |
| TEST-VIC20-VS-04 | Integration/native | AC-VS-04 | NTSC video lockstep 2k + 500k | **New/extend** |
| TEST-VIC20-PX-01 | Integration/native | AC-PX-01, AC-PX-06 | `Vic20PixelFrameTests` geometry + non-black | Exists partial |
| TEST-VIC20-PX-02 | Integration/native | AC-PX-02 | `Vic20PixelLockstep.Index_ReadyPal_SequenceEqual` | **New** |
| TEST-VIC20-PX-03 | Integration/native | AC-PX-03 | `Vic20PixelLockstep.Index_ReadyNtsc_SequenceEqual` | **New** |
| TEST-VIC20-PX-04 | Integration/native | AC-PX-04 | `Vic20PixelLockstep.Index_BusyPal_SequenceEqual` | **New** |
| TEST-VIC20-PX-05 | Integration/native | AC-PX-05 | `Vic20PixelLockstep.Bgra_ReadyPal_SequenceEqual` + busy | **New** |
| TEST-VIC20-CT-01 | Doc/unit | AC-CT-01 | `Vic20CartInventoryTests` or audit checksum test | **New** |
| TEST-VIC20-CT-02 | Unit | AC-CT-02 | extend `Vic20ExpansionCartTests` | Extend |
| TEST-VIC20-CT-03 | Unit | AC-CT-03 | `Fe3ModeMapTests` | **New** |
| TEST-VIC20-CT-04 | Unit | AC-CT-04 | `Flash040CoreTests` / `Fe3Flash040Tests` | Exists |
| TEST-VIC20-CT-05 | Unit+clock | AC-CT-05 | `Fe3Flash040EraseLatencyTests` | **New** |
| TEST-VIC20-CT-06 | Unit | AC-CT-06 | `UltimemParityTests` | **New** |
| TEST-VIC20-CT-07 | Unit | AC-CT-07 | `MegaCartParityTests` | **New** |
| TEST-VIC20-CT-08 | Process | AC-CT-08 | hostile receipts required in phase exit checklist | Process |
| TEST-VIC20-SD-01 | Native/unit | AC-SD-01 | `Vic20NativeAudioExportTests` | **New** |
| TEST-VIC20-SD-02 | Unit (mock bus) | AC-SD-02 | `Vic20SoundRegisterSideEffectTests` mocks-first | **New** |
| TEST-VIC20-SD-03 | Integration/native | AC-SD-03 | `Vic20SoundLockstep.Silence_MatchesXvic` | **New** |
| TEST-VIC20-SD-04 | Integration/native | AC-SD-04 | `Vic20SoundLockstep.Tone_MatchesXvic` | **New** |
| TEST-VIC20-SD-05 | Integration/native | AC-SD-05 | `Vic20SoundLockstep.MultiReg_MatchesXvic` | **New** |
| TEST-VIC20-SD-06 | Process | AC-SD-06 | hostile receipt | Process |
| TEST-VIC20-BS-01 | Unit | AC-BS-01 | existing color RAM tests | Exists |
| TEST-VIC20-BS-02 | Unit | AC-BS-02 | existing open-bus tests | Exists |
| TEST-VIC20-BS-03 | Integration | AC-BS-03 | VIA peek checkpoints in lockstep suite | Exists/extend |
| TEST-VIC20-BS-04 | Unit | AC-BS-04 | `Via6522SrHandshakeTests` per diverge | **New as needed** |
| TEST-VIC20-BS-05 | Unit/integration | AC-BS-05 | `Vic20KeyboardTests` extend | Extend |
| TEST-VIC20-SN-01 | Doc | AC-SN-01 | inventory in audit + test lists modules | **New** |
| TEST-VIC20-SN-02 | Unit/integration | AC-SN-02 | `Vic20SnapshotRoundTripTests` | **New** |
| TEST-VIC20-SN-03 | Integration/native | AC-SN-03 | `Vic20SnapshotLockstepResumeTests` | **New** |
| TEST-VIC20-SN-04 | Integration | AC-SN-04 | cart snapshot round-trip | **New** |
| TEST-VIC20-WH-01 | Integration umbrella | AC-WH-01 | `Vic20ExactUmbrellaGate` | **New** |
| TEST-VIC20-WH-02 | Doc | AC-WH-02 | audit consistency test or checklist | Process |
| TEST-VIC20-WH-03 | Doc | AC-WH-03 | README/HANDOFF updated | Process |
| TEST-VIC20-WH-04 | Process | AC-WH-04 | hostile AGREE | Process |
| TEST-VIC20-TO-01 | Build | AC-TO-01 | native available check in tests | Exists |
| TEST-VIC20-TO-02 | Integration | AC-TO-02 | export smoke tests | Extend |
| TEST-VIC20-TO-03 | Integration | AC-TO-03 | audio export present | **New** |
| TEST-VIC20-TP-01 | Unit | AC-TP-01 | geometry constants tests | Exists/extend |
| TEST-VIC20-TP-02 | Unit | AC-TP-02 | sync point documented in test | **New** |
| TEST-VIC20-TP-03 | Unit | AC-TP-03 | index-primary policy in pixel tests | **New** |
| TEST-VIC20-TS-01 | Code review+test | AC-TS-01 | citation in XML docs + unit | **New** |
| TEST-VIC20-TS-02 | Unit/bench | AC-TS-02 | no alloc on hot tick when sound silent | **New** |
| TEST-VIC20-TS-03 | Unit | AC-TS-03 | determinism two-run equal | **New** |
| TEST-VIC20-TC-01 | Unit | AC-TC-01 | cart types on port | Exists |
| TEST-VIC20-TC-02 | Unit | AC-TC-02 | Flash040Core TYPE_B constants | Exists |
| TEST-VIC20-TC-03 | Unit | AC-TC-03 | erase advances with clock | **New** |
| TEST-VIC20-TL-01 | Unit | AC-TL-01 | diverge report format | Exists |
| TEST-VIC20-TL-02 | Doc/unit | AC-TL-02 | env flag tests or docs | **New** |
| TEST-VIC20-TL-03 | Doc | AC-TL-03 | collection attribute present | Exists |
| TEST-VIC20-TN-01 | Unit | AC-TN-01 | snapshot API surface | **New** |
| TEST-VIC20-TN-02 | Unit | AC-TN-02 | no module drop | **New** |
| TEST-VIC20-TX-01 | Process | AC-TX-01 | phase checklist | Process |
| TEST-VIC20-TX-02 | Unit/doc | AC-TX-02 | audit header not stale | **New** |

**Coverage check:** Every AC-CPU-*, AC-VS-*, AC-PX-*, AC-CT-*, AC-SD-*, AC-BS-*, AC-SN-*, AC-WH-*, AC-TO-*, AC-TP-*, AC-TS-*, AC-TC-*, AC-TL-*, AC-TN-*, AC-TX-* appears in the table above.

---

## 6. FR → TR → TEST mapping

| FR | Requires TR | Primary TEST IDs |
|----|-------------|------------------|
| FR-VIC20-CPU-001 | TR-VIC20-ORACLE-001, TR-VIC20-LOCKSTEP-001 | TEST-VIC20-CPU-01..05 |
| FR-VIC20-VIDSTATE-001 | TR-VIC20-ORACLE-001, TR-VIC20-LOCKSTEP-001 | TEST-VIC20-VS-01..04 |
| FR-VIC20-PIXEL-001 | TR-VIC20-PIXEL-001, TR-VIC20-ORACLE-001 | TEST-VIC20-PX-01..05 |
| FR-VIC20-CART-001 | TR-VIC20-CART-001 | TEST-VIC20-CT-01..08 |
| FR-VIC20-SOUND-001 | TR-VIC20-SOUND-001, TR-VIC20-ORACLE-001 | TEST-VIC20-SD-01..06 |
| FR-VIC20-BUS-001 | TR-VIC20-LOCKSTEP-001 | TEST-VIC20-BS-01..05 |
| FR-VIC20-SNAP-001 | TR-VIC20-SNAP-001 | TEST-VIC20-SN-01..04 |
| FR-VIC20-WHOLE-001 | TR-VIC20-EXACT-POLICY-001 + all above | TEST-VIC20-WH-01..04 |

On implementation start: persist FR/TR/TEST + mappings in MCP requirements store before writing production code for each phase.

---

## 7. Iterative phases (implementation order)

Each phase: **write/extend tests for AC → mocks-first green where unit → real green → refactor → phase gate (current + prior TEST IDs) → hostile if Exact → update audit**.

| Phase | FR AC focus | Exit gate (TEST filter / suite) |
|-------|-------------|----------------------------------|
| **A** Pixel | AC-PX-*, AC-TP-* | FullyQualifiedName~Vic20Pixel |
| **B** VIC-I residual | AC-VS-*, AC-BS-* as needed | Vic20VideoLockstep + Vic20Pixel + prior |
| **C** Carts | AC-CT-*, AC-TC-* | Flash040 + Fe3 + Ultimem + MegaCart + Cart |
| **D** Sound | AC-SD-*, AC-TS-*, AC-TO-03 | Vic20Sound* |
| **E** Bus/VIA/input | AC-BS-*, AC-CPU-03/04 | Vic20Diverge + VIA + Keyboard |
| **F** Snapshots | AC-SN-*, AC-TN-* | Vic20Snapshot* |
| **G** Whole claim | AC-WH-*, AC-TX-* | Vic20ExactUmbrella + full Vic20 filter 0/0/0 |

**Parallelism:** After A green, C and D may fork; B should complete before heavy FB stress expansions.

---

## 8. TDD cycle rules (BDPv4)

### 8.1 Fowler cycle per AC increment
1. Pick **one** AC (or smallest AC group).  
2. Write **failing** test(s) named in §5 (red).  
3. Implement minimum code to pass (green).  
4. Refactor tests and production; keep green.  
5. Do not skip tests; do not weaken AC to pass.

### 8.2 Mocks-first (Byrd augmentation)
- Unit tests for sound registers, flash040 timing units, cart maps: fake `IBus` / clock before native.  
- Integration tests vs xvic only after unit mocks green for that increment.

### 8.3 Phase exit
- All TEST IDs for current phase + all prior phases: **0 failed, 0 skipped**.  
- Record command, filter, counts in `docs/receipts/`.  
- Hostile AGREE when claiming Exact for that phase’s FR.

---

## 9. Public interfaces (before production code)

Document in XML docs / Abstractions before Phase A–D impl:

| Interface / API | Phase |
|-----------------|-------|
| Index frame capture on managed VIC-I (if not already public) | A |
| Sync-point helper for pixel compare | A |
| `Flash040Core` clocked erase advance API | C |
| xvic `vice_vic20_capture_audio` (shim) | D |
| Managed sound device / VIC-I sound port | D |
| VIC-20 snapshot save/load surface | F |

---

## 10. Risk and non-goals

| Risk | Mitigation |
|------|------------|
| BGRA fails due to CRT/palette | AC-PX: index Exact first; BGRA after palette table match |
| Cart inventory larger than three | AC-CT-01 inventory + operator sign-off for Missing |
| Sound analog | Digital VICE path only |
| 10s flaky native | Isolation; never claim hang as green |
| IEEE/printer pressure | Explicit exclude until amendment |

**Non-goals:** Xbox/Store, invent carts, zip media, C128+.

---

## 11. First slice after approval (Phase A kickoff)

1. Persist MCP FR/TR/TEST for FR-VIC20-PIXEL-001 + TR-VIC20-PIXEL-001 + TEST-VIC20-PX-02 (minimum set).  
2. Write **red** `Vic20PixelLockstep.Index_ReadyPal_SequenceEqual` (TEST-VIC20-PX-02 / AC-PX-02).  
3. Mocks not applicable (native compare); implement managed/native index path until green.  
4. Fix audit header stale capture-stub line (AC-TX-02).  
5. Phase A continues through AC-PX-05 then hostile AGREE.

---

## 12. Validation commands

```pwsh
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20Pixel"
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20Sound"
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Flash040|FullyQualifiedName~Fe3|FullyQualifiedName~Ultimem|FullyQualifiedName~MegaCart|FullyQualifiedName~Vic20Expansion"
$env:VICESHARP_LOCKSTEP_10S=1
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20DivergeProbe"
# Final
dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20"
```

---

**Plan status:** BDPv4 decision-complete: FR/TR with AC, TEST IDs with 100% AC coverage, mappings, TDD/mocks rules, phases. Ready for operator approval to implement starting Phase A.
