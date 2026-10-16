# CPU-port capacitor correction and proposed cases

Prepared by Codex on PAYTON-LEGION2 from source inspected 2026-10-11. This is an unapplied proposal, not a test result or accepted requirement. The exact before/after record is in `proposed-field-delta.json`, with fresh typed records in `typed-record-preimages.json`.

## Why this correction is necessary

Live `FR-CPU-004/ac-06` says "approximately 350 microseconds to decay". The same sentence occurs in its description. The description also says capacitor timing affects reads of output pins. Both conflict with the executed, pinned C64/SX-64 paths: the retained charge substitutes for input-pin reads, and expiration is measured in CPU cycles, with random delay.

The proposed ac-06 preserves the decay obligation and existing ID while replacing the wrong timing and making its boundaries testable. The description changes only the embedded ac-06 text and "reads of output pins" to "reads of floating input pins", in addition to the preceding draft's separately disclosed wiring corrections. No live record is changed. All seven CPU AC IDs remain unsatisfied.

## Source evidence

Immutable commit: `cc67418cba58e71d8e70fd353e9de47199ad2075`. Six files are pinned by Git blob and SHA256 in the JSON. They were inspected in `F:\GitHub\vice-sharp\artifacts\immutable-vice\cc67418cba58e71d8e70fd353e9de47199ad2075\source\vice`, not edited under `native/vice`.

- `src/c64/c64.h:74-89`: bits 6/7 use 350000 cycles; SX-64 bits 3/4/5 use 1500000. The comment says 350 milliseconds, not microseconds. The separate 8500 constant is defined but a search of the pinned `src` tree found no use beyond its definition. Do not substitute that unused constant for the executed path.
- `src/c64/c64mem.c:366-495` and `src/c64/c64memsc.c:366-495`: delay jitter is base/5. DDR output-to-input transitions capture the old data bit and set the deadline. DATA writes while output do so even when the value is unchanged; the `pport.data != value` check occurs afterward. Writing zero still sets the falloff flag and deadline.
- `src/c64/c64mem.c:262-336` and `src/c64/c64memsc.c:262-338`: a DATA read clears charge/flag only for `deadline < maincpu_clk`. Equality retains charge. Expiration is checked regardless of current DDR; charge overlays the returned value only when the pin is input. A DDR read does not run that expiration path.
- `src/lib.c:944-1002,1029-1034`: shared PCG state, seeded then advanced once; integer range projection includes both endpoints. Record seed and the complete preceding random-call history. A distribution-only comparison cannot prove lockstep equality.
- `src/c64/c64pla.c:106-123`: reset clears data, direction, charge and falloff flags. This function does not reset the saved deadline fields. Do not invent that side effect.
- `src/types.h:61-65`: `CLOCK` is unsigned 64-bit. Source-defined addition wraps; expiration uses unsigned comparison, not a wrap-aware signed-duration replacement.

These are source-inspection facts, not runtime observations. The x64 and x64sc surrounding memory paths differ. Each accepted native binary must be compared against its own pinned path, not assumed trace-identical to the other.

## Fixture contract

Each future fixture names the requirement/AC, group below, exact binary/source/resource/ROM hashes, board, configured model, PAL/NTSC selection, complete process/VSF/prelude history, seed and prior random calls, initial clock, DDR/data/charge/flag/deadline state, ordered writes/reads/resets, and independently derived expected observations. Native VSF remains canonical; its existence is not proof that it serializes every RNG/static field. Unknown state or unavailable accepted external observation blocks the case. Do not alter VICE or manufacture its internal observations.

Assertions compare read values, direction/data values, affected and unaffected pins, per-pin charge/flag/deadline when observable, and exact random-call count/order. Use independent scripted mocks first, with deliberate corruptions caught as expected negative controls inside a green harness. Then bind the same cases and assertions to real providers. Names below are not implemented tests.

## Eight case groups

1. `C64_Capacitor_DeadlineInclusiveJitter`: for clock 10, bits 6/7 use deadline 350010 with draw 0 and 420010 with draw 70000. SX bits 3/4/5 use 1500010 with draw 0 and 1800010 with draw 300000. Include interior draws and each bit separately. Reject microsecond conversion, an exclusive upper bound, wrong base, or one shared deadline for unequal draws. Endpoint fixtures use independent mocked draws; native cases must record an actual seeded/prelude-derived draw, not assert that a selected seed reaches an endpoint without evidence.
2. `C64_Capacitor_DdrTransitionMatrix`: cover 0->0, 0->1, 1->0 and 1->1 for each eligible pin and data 0/1. Only 1->0 captures old data and consumes a random draw. Preserve an already-held charge/deadline through the other transitions, subject to a subsequent DATA read's expiration. Reject capture from data_read instead of data, reseeding on every DDR write, or forgetting zero-valued captures.
3. `C64_Capacitor_RepeatedDataWrite`: write high twice while output at distinct clocks, write low, then write the same low again. Each write consumes its eligible draws and moves deadlines even without a data-value change. Repeat while input: no capacitor update or draw. Reject same-value suppression, zero-value suppression, or charging an input pin.
4. `C64_Capacitor_ReadBoundaryAndLazyExpiry`: independent fixtures at D-1, D and D+1 show retained, retained and cleared charge. Also perform repeated reads; DDR-only reads after D; no-read gaps followed by a DATA read; and DATA reads while output. Output reads still execute expiry but do not overlay charge. Reject `<=`, host-time expiry, DDR-read discharge, output overlay, or a synthetic read inserted into the prelude.
5. `C64_Capacitor_BoardPinMatrix`: normal C64 bits 6/7 and SX-64 bits 3..7 use the appropriate paths. Ordinary C64 bits 3/4/5 keep their native tape/pull-up behavior, not SX capacitor substitution. Include mixed eligible/ineligible DDR masks and preserved unrelated return bits. Reject applying SX behavior to C64 or treating all SX inputs as zero.
6. `C64_Capacitor_RngConsumptionOrder`: with scripted distinct draws 1,2,3,4,5 and all eligible pins, SX DDR transitions consume in order 3,4,5,6,7; SX DATA writes consume 7,6,5,4,3. C64 DDR uses 6,7 and DATA uses 7,6. Test sparse masks and earlier global draws. Reject sorting both paths alike, drawing for an ineligible pin, restarting the RNG per pin, or sharing one draw across pins.
7. `C64_Capacitor_ResetAndClockWrap`: reset after charge must clear charges/flags and leave the observed deadline fields as native does. A later input read must not resurrect charge. With clock `UINT64_MAX-10`, base350000 and draw0, deadline wraps to349989; an immediate DATA read at that high clock sees deadline<clock and expires. Reject saturating/signed arithmetic, invented wrap-aware expiration, or preserving active flags on reset. Full native reset/prelude effects remain part of the fixture, not inferred from this function alone.
8. `C64_Capacitor_PalNtscAndConfiguredModel`: repeat board/pin cases for accepted PAL/NTSC and configured C64/SX model variants. Constants remain in CPU-cycle units; wall-clock duration can differ with clock rate. In these pinned paths a configured 8500 model does not select the unused 8500 constant. Reject host-frequency scaling of deadlines, a hardware-intent replacement, or omission of a required board/model/resource combination. Pin the actually supported model/resource values before building executable fixtures.

The read, direction, reset and random histories are a matrix, not eight one-line success checks. Exact native fixture assets and observation adapters remain R2 prerequisites. No mock suite, real-emulator case or HV gate ran for this document.
