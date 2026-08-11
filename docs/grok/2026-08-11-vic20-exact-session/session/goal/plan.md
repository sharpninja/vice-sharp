# Plan: Finish bit-exact VIC-20 / xvic plan (Phases A–G) without stopping for user input

## Goal kind
code-change

## Acceptance criteria
1. **Pixel FB parity (Phase A):** Managed vs xvic normal-border canvas matches on palette-index SequenceEqual for READY idle PAL and NTSC and one busy PAL fixture, and on BGRA SequenceEqual for READY PAL (and the same busy case after palette alignment); dimensions match (PAL 448×284; NTSC normal); capture is not all-black/sentinel.
2. **Cart and sound parity (Phases C–D):** Inventoried VIC-20 cart set (standard + FE3 + Ultimem + Mega-Cart + any other types in the xvic shim build) has green parity tests including FE3 flash040 erase latency/status timing; VIC-I sound matches xvic deterministic sample capture for silence + single-channel + multi-register fixtures (native audio export present).
3. **Whole-machine lockstep residuals (Phases B, E–F):** VIC-I pipeline residual needed for multi-frame FB remains green; multi-second CPU lockstep stays green for existing 10s PAL/NTSC and for ≥2s non-idle workload PAL+NTSC; bus/VIA/keyboard gaps that cause diverge are closed with tests; unexpanded (+ one cart) snapshot round-trip / short resume lockstep green.
4. **Plan closeout (Phase G + process):** Focused Vic20 gates for the above run with 0 failed and 0 skipped; audit matrix and HANDOFF/README state the Exact claim only where evidence exists (IEEE/rsuser/printer stay Explicit Missing if still excluded); MCP `PLAN-VIC20-EXACT-001` and FR/TR/TEST AC for shipped scopes updated with receipts; no mid-work pause for user approval unless blocked by environment or missing oracle ROMs/native dll.

## Verification plan
1. **gating:** Run `dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --filter "FullyQualifiedName~Vic20Pixel"` (and any new pixel lockstep filter). Capture full stdout/stderr to `{SCRATCH}/vic20-pixel-gate.log`. Pass only if exit 0, Failed=0, Skipped=0, and logs show SequenceEqual (or equivalent assert) success for READY PAL index and the other Phase A pixel cases that land in-repo.
2. **gating:** Run focused cart+flash+sound filters covering FE3 flash040 (incl. erase latency), Ultimem, Mega-Cart, expansion cart, and sound lockstep (`FullyQualifiedName~Flash040|Fe3|Ultimem|MegaCart|Vic20Expansion|Vic20Sound` or the actual class names added). Capture to `{SCRATCH}/vic20-cart-sound-gate.log`. Pass only if exit 0, Failed=0, Skipped=0.
3. **gating:** Run video lockstep + diverge probes as available (`FullyQualifiedName~Vic20VideoLockstep|Vic20DivergeProbe` with env flags documented in tests for 10s/2s workloads). Capture to `{SCRATCH}/vic20-lockstep-gate.log`. Pass only if exit 0, Failed=0, Skipped=0 for the tests that exist and are non-skipped (10s may require `VICESHARP_LOCKSTEP_10S=1`; if env/native cannot run multi-second, capture that and require at least 2k/500k video + short CPU green).
4. **gating:** Run snapshot-related Vic20 tests if present (`FullyQualifiedName~Vic20Snapshot`). Capture to `{SCRATCH}/vic20-snapshot-gate.log`. Pass with 0 fail 0 skip when tests exist; if phase not yet landed, this step fails the goal (snapshots are in-plan).
5. **gating:** Run umbrella/full Vic20 filter `FullyQualifiedName~Vic20` once native and fixtures are ready. Capture to `{SCRATCH}/vic20-umbrella-gate.log`. Pass only if Failed=0 and Skipped=0 (or document only Explicit Missing skips with zero failures and operator-allowed skip policy—default is 0 skip).
6. **evidence:** Confirm on disk: updated audit under `docs/audit-vic20-vs-vice*.md`, receipts under `docs/receipts/`, MCP TODO/requirements status for `PLAN-VIC20-EXACT-001` / FR-VIC20-001 family, and hostile-validator AGREE artifacts if Exact claims are asserted. Copy key paths/hashes into `{SCRATCH}/vic20-exact-receipts.txt`.
7. **evidence:** Rebuild xvic shim when native exports change (`native/build-vice-shim-xvic.sh` under MINGW64); note dll timestamp in `{SCRATCH}/vic20-exact-receipts.txt`.

## Non-goals
- Microsoft Store / Xbox UWP / Dev-Mode sideload (cancelled).
- IEEE-488, rsuser, printer (Explicit Missing exclude unless plan amended).
- Zip virtual media (`PLAN-ZIPMEDIA-001`).
- C128/PET/Plus4 or inventing cart types not in VICE xvic.
- CRT/YUV host display filters beyond what xvic canvas export uses for compare.
- Stopping to ask the user for go/no-go between phases (only hard environment blockers).

## Assumed scope
- Session plan: bit-exact whole VIC-20 vs xvic (Phases A–G), BDPv4 FR/TR/TEST.
- Repo: `src/ViceSharp.Chips/Vic/Mos6561.cs`, `src/ViceSharp.Core/Vic20/*`, `FlashCarts/Flash040Core.cs`, `ViceNative.Xvic.cs`, `native/vice-shim-vic20.c`, `tests/ViceSharp.TestHarness/Vic20/*`, `docs/audit-vic20-vs-vice*.md`, `docs/receipts/*`, MCP FR/TR/TEST/`PLAN-VIC20-EXACT-001`.
- Oracle: `native/vice_xvic.dll`, VICE `vic20/` + `vic20sound.c` + `vic20/cart/*`, ROMs via `VICESHARP_ROM_PATH`.
- Baseline: `85396c8` — Phase A index scaffolding; `Index_ReadyPal_SequenceEqual` currently red (first mismatch n=0 m=3 at x=296,y=0).

## Implementation approach
Drive BDPv4 per AC: red test first, mocks-first for pure cart/sound units, then xvic integration. Keep pure draw/sound/cart logic separable from host I/O. Prefer index lockstep before BGRA. Add native audio capture only when sound phase needs it. Do not invent; realign to VICE file+function. Process-isolate multi-second PAL/NTSC runs. Update audit and receipts at each phase exit; mark MCP AC satisfied only with command evidence.

## Task checklist
- [ ] Green Phase A: fix READY PAL index SequenceEqual; add NTSC + busy index + BGRA tests; capture `{SCRATCH}/vic20-pixel-gate.log` with 0 fail 0 skip.
- [ ] Phase B: close VIC-I draw/cycle residuals that block multi-frame FB; keep video lockstep + pixel green.
- [ ] Phase C: cart inventory + FE3 erase latency + Ultimem/Mega full banking tests green.
- [ ] Phase D: native audio export + managed sound + silence/tone/multi sample SequenceEqual green.
- [ ] Phase E: non-idle ≥2s CPU lockstep PAL+NTSC; close VIA/keyboard diverges cited by probes.
- [ ] Phase F: snapshot inventory + unexpanded + one-cart round-trip/resume lockstep green.
- [ ] Phase G: umbrella Vic20 gate 0/0/0; audit/HANDOFF/README + MCP TODO/FR AC + receipts; hostile AGREE where Exact claimed.

## Risks / Contradictions
- Full Phases A–G is a large multi-day effort; a single harness turn may not finish all gates—implementer must continue autonomously across turns without waiting for user, and only fail the goal if blocked by missing ROMs/native build environment (capture that evidence).
- Multi-second lockstep and sound compare need working `vice_xvic.dll` + ROMs; if unavailable, capture failure and cannot claim green.
- Hostile AGREE may require a separate validator agent; treat missing AGREE as incomplete Exact claim, not as skip of functional tests.
