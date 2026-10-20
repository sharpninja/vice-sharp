# Phase C step 3, PLAN-OBSERVER-001 slice S3: equivalence proof of the observer binaries against the stock binaries (LAB-OMARCHY), 2026-10-20T08:24 to 08:55

Option B changes which VICE code path executes (every fetch takes the table route instead of the bank-base fast path) but not the emulated semantics by design. This receipt is the empirical half of the proof on the Linux headless build; the source half is in the plan (S3 proof items (a) to (c)). Runner: `tools/immutable-vice/Invoke-ObserverEquivalence.ps1` (`a0cb5f72` and the follow-up commit with the per-cycle comparison). Raw evidence (stdout, stderr, screenshots, `equivalence.json`) under `artifacts/immutable-vice/<sha>/equivalence-linux/<stamp>/` (gitignored, retained on LAB-OMARCHY).

## Workload and runs

ROM-only reset workload (no media; VICE data directory `work-linux/vice/data`, hash-verified copy of the materialization), headless, `-default +logcolorize -sounddev dummy -limitcycles N -exitscreenshot <png>`. Per machine and cycle count three runs: `stock` (the stock binary from the Make stage, no wrappers, ungated), `withheld` (observer binary, fast path withheld, gated with one permit byte per cycle), `keep` (observer binary with `VICESHARP_FAST_PATH=keep`, translate forwarded unchanged, gated). Binaries: `x64sc-observer` and `xvic-observer` relinked at 08:38 with the healed wrapper (SHA256 in the `*-link.json` receipt of that stage); stock `x64sc` and `xvic` from 08:16.

Why `+logcolorize`: the stock headless binaries segfault when stdout is not a terminal and log colors are on (`log.c` 767 to 775 hand a null `nocolortxt` to `log_archdep`, from the banner at `main.c` 141, gdb backtrace at 08:26). A command-line resource, no source change.

## Results (08:38:29 to 08:45, healed binaries)

- xvic 100,000 cycles: all three exit 1 (cycle limit); exit screenshots identical (`EC7D9617...`); `VICE_GATE` lines 99,998 in both gated runs; accesses 100,002 withheld versus 47,815 kept; translate events 9,465 versus 14; 0 accesses with kind missing; 0 missing or foreign reports; 0 STOP lines.
- x64sc 100,000 cycles: exit 1; screenshots identical (`5283DAEF...`); gate lines 99,999 both; accesses 100,001 versus 43,503; translate 2,164 versus 7; 0 kind missing (the earlier run at 08:33 with the pre-heal wrapper had 2, see below); 0 missing or foreign; 0 STOP.
- xvic 1,000,000 cycles: exit 1; screenshots identical (`DD3221CF...`); gate lines 999,997 both; accesses 1,000,001 versus 401,603; translate 72,523 versus 694; 0, 0, 0.
- x64sc 1,000,000 cycles: exit 1; screenshots identical (`5283DAEF...`); gate lines 999,999 both; accesses 1,000,001 versus 439,497; translate 20,154 versus 7; 0, 0, 0.

Observation: with the fast path withheld the observer sees one bus access per CPU cycle (accesses equal gate lines plus the reset-sequence accesses before the first hook), which is what a 6502 does; with the fast path kept, 52 to 57 percent of the accesses (the opcode and operand fetches) are invisible, which is the reason option B withholds it. The x64sc exit screenshot is the same image at 100,000 and 1,000,000 cycles (bytes identical), so for x64sc the screenshot is a weak digest; the per-cycle trace comparison below carries the claim there.

## Per-cycle trace comparison (added after the first matrix)

Rule: every `VICE_BUS,access` line of the keep run (kind, address, data, clock) must occur in the withheld run with at least the same multiplicity (the withheld run observes a superset: the fetches too), and the `VICE_GATE` sequences must be identical line for line.

First result (08:47 to 08:56, no seed pinned): gate sequences identical in all four pairs, but keep-run access lines absent from the withheld run: xvic 9 of 47,815 (100k) and 170 of 401,603 (1M); x64sc 99 of 43,503 and 1,036 of 439,497. Every absent line was a `dummy-read` of a stack or page-2 address whose counterpart in the withheld run had the same kind, address and clock and a different data byte (for example xvic clock 35, `dummy-read,509`: data 1 withheld versus 0 kept; x64sc clock 323, `dummy-read,518`: 0 versus 32). Cause, from source: VICE initializes RAM with random bits (`RAMInitRandomChance`, default `RAM_INIT_RANDOM_CHANCE_DEFAULT`, `ram.c` 321 via `lib_unsigned_rand`) seeded from `time(NULL)` (`lib.c` 1045) unless `-seed` is given (`main.c` 298), so two processes read different uninitialized bytes on dummy stack reads; the real reads and writes, the screens and the cycle sequences were unaffected. Not a wrapper defect; a workload parameter that was missing.

Second result (08:57 to 09:06, `-seed 1` added to every run; the seed is now part of the workload definition): xvic 100k: 47,815 keep accesses, 0 absent, gate identical; x64sc 100k: 43,503 keep accesses, 0 absent, gate identical; runner result `equivalent: every check passed`. 1,000,000 cycles (08:58:48 to 09:06): xvic 401,603 keep accesses, 0 absent, gate sequences identical (999,997 lines), screenshots identical (`DD3221CF...`); x64sc 439,497 keep accesses, 0 absent, gate identical (999,999 lines), screenshots identical (`5283DAEF...`); 0 missing, foreign or kind-missing reports in every gated run; runner result `equivalent: every check passed`, exit 0.

Consequence for Phase E: the oracle's RAM initialization is random by default and must be pinned by `-seed` (or `RAMInitRandomChance 0`) in every lockstep workload, and the managed core must reproduce the seeded pattern or both sides must disable the random bits; recorded in the handoff for the Phase E plan.

## Finding fixed inside this slice

The first x64sc matrix (08:33, pre-heal wrapper) reported exactly 2 accesses with kind missing in 100,000 cycles: the Kernal reset's `STX $01` (data `$E7`, clock 120) and `STX $00` (`$2F`, clock 125). The store itself runs `mem_pla_config_changed` (`c64memsc.c` 226 to 243), which re-aliases the dummy pointers intra-object before the trampoline had decided the kind. Fixed mocks first (`bank-by-store` fixture, the double's `$0001` store now reconfigures and re-points like VICE; implementation run failing before the change): the kind is decided at entry from the pointer state the access was dispatched through, and the dummy pointers are healed right after every access; `repatch,0,1` is emitted when a heal happened. After the fix: 0 kind-missing accesses in all four gated runs.

## Other findings

- The plugin's session hooks wrote `src/.mcpServer/claude/session-state.yaml`, `src/.mcpServer/claude/session-verb-outcome.yaml` and `.mcpServer/claude/session-state.yaml` into the materialization after the Bash working directory drifted into it (no original file changed; 3,841 originals verified at every Inventory stage). Triage `triage-report-7b1d571db6c54ddd8d0d81a890cce447` (filed twice by my mistake). Files left in place for Payton's disposition.
- `LEG-VICE#` became unreachable at 08:37 (not in the session list); the 08:37 update to it is queued.

## Gates

- `dotnet test tests/ViceSharp.NativeClockProbe.Tests -c Release` (no filter) at 08:36: 240 passed, 0 failed, 0 skipped.
- Equivalence runner: `equivalent: every check passed` for both machines at 100,000 and 1,000,000 cycles (08:38 to 08:45), exit 0.
- `git diff --check` clean; analyzer clean on both scripts.

## Not claimed

Windows (LEGION2) has not built or run the observer binaries yet (S2b awaits Payton's clearance). No workload with media, cartridges or the monitor has been run. The VIC-20 Ba/Aec applicability is still "decide later" (D15), asked again at S4.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 95%, accuracy 97%, completeness 92%
