# Requirements Implementation Audit - 2026-05-20

## Finding

The imported VICE requirements are present, but they are not yet enforced
strongly enough to guarantee that Phase 1 design and code are requirement-led.
This is a project-wide traceability deficiency, not just a VIC-II renderer
issue.

## Evidence

- VICE requirement import is script-driven through
  `tools/port_vice_requirements.py`, and the source manifest records classic
  VICE documentation as the FR corpus.
- The latest `tools/check_requirement_traceability.ps1` audit after the
  2026-05-21 edge-TR and side-border work found 163 canonical IDs in
  `docs/requirements`, with 80 referenced from `src` or `tests`, 83 not
  referenced from source/test files, and 53 noncanonical IDs still present in
  source/test files.
- The FR-only breakdown is 108 canonical FRs, 67 referenced from `src` or
  `tests`, and 41 not referenced from source/test files.
- Source and tests still contain broad or noncanonical labels such as `FR-VIC`,
  `FR-CIA-TIMER`, and `FR-INPUT-KEYBOARD`. Those labels do not prove that a
  specific imported requirement drove the implementation.
- Some requirement traceability entries name planned suites that do not match
  the executable test class names now present in `tests`.
- `FR-VIC-007` described open-border behavior but did not explicitly require
  closed-border sprite masking before the renderer bug was found.
- `FR-VIC-010` stated sprite 0 was earliest and sprite 7 latest. VICE PAL
  x64sc source instead schedules sprites 3-7 at one-based table cycles 1/2,
  3/4, 5/6, 7/8, and 9/10, then sprites 0-2 at 58/59, 60/61, and 62/63.
  VICE maps PAL cycle `c` to internal cycle `c - 1`, so vice-sharp
  `CurrentCycle` expects sprites 3-7 at 0/1, 2/3, 4/5, 6/7, and 8/9, then
  sprites 0-2 at 57/58, 59/60, and 61/62.

## Corrective Gate

Every Phase 1 slice must now pass this gate before implementation continues:

1. Name canonical `FR-*`, `TR-*`, `TEST-*`, and owning `BACKFILL-*` IDs.
2. Cite the VICE documentation or source files that define the behavior.
3. Correct stale or incomplete requirements before code changes are treated as
   complete.
4. Add or update executable tests that cite canonical IDs.
5. Run `tools/check_requirement_traceability.ps1` and record the output in the
   handoff/session notes.

## Immediate Corrections

- `FR-VIC-007` now states closed vertical/side borders mask sprite pixels and
  opened borders permit sprite pixels according to the border flip-flop state.
- `FR-VIC-010` now points at model-specific VICE sprite DMA tables and records
  the PAL x64sc sprite access order.
- `TEST-VIC-001` now explicitly includes closed-border sprite masking,
  open-border sprite visibility, sprite priority, and per-model sprite DMA
  timing.
- `docs/plan.md` now includes Slice 0.5 as a mandatory requirements
  traceability gate before further Phase 1 implementation.
- `docs/requirements/backfill/Classic-VICE-Edge-Case-TR-Backfill.md` now
  records 19 VICE-source-derived observable edge TRs, and `docs/plan.md`
  integrates those TRs into the Phase 1 slice order.
- `TR-VIC-EDGE-001` and `TR-VIC-EDGE-002` now have executable managed coverage
  for invalid ECM priority/collision behavior and continuous side-border
  behavior respectively. Native visible-frame/checkpoint validation remains
  open under `BACKFILL-VIDEO-001`.

## Follow-Up Work

- Replace broad `FR-VIC` and other noncanonical labels in touched files with
  canonical imported IDs.
- Align traceability map test-suite names with executable test classes.
- Decide whether Phase 1 should formally promote the x64sc-required portions of
  `FR-VIC-007`, `FR-VIC-008`, and `FR-VIC-010`, which are currently imported as
  Iteration 2 requirements.
- Consider making `tools/check_requirement_traceability.ps1 -FailOnNonCanonical`
  a CI or pre-commit gate after existing broad labels are triaged.
