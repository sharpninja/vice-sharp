# Phase C step 3, PLAN-OBSERVER-001 slice S4: stolen cycles, VICE_OBS composition and real observation traces (LAB-OMARCHY), 2026-10-20T09:07 to 10:25

Follows S3 (`afeb9652`). Rulings at the start of the slice (09:07): D15 "Not applicable: constant 0" for the VIC-20 Ba/Aec, and stderr confirmed as the observation channel (confirm item 3). Commits: `df6bd7ca` (plan), `ad94269f` (S4a), `fe8836d6` (S4b emitter), `2f99f621` (S4b adapter, glue, decoder), `a075fee1` (validator), and the S4c runner commit. No VICE source, object or build target is modified; the materialization is re-hashed unchanged at every Link stage (3,841 original files; the plugin-written `.mcpServer` files remain the only additions, see the S3 receipt).

## S4a: VIC-II stolen cycles

Finding (observation, 09:12): `vicii_steal_cycles` (`viciisc/vicii-cycle.c` 628 to 636) loops `maincpu_clk++; ba_low = vicii_cycle();` inside its own object, so stolen cycles reached neither the per-cycle hook nor the gate; the 1M reset workload had written `$D011` zero times (grep of the withheld trace), so no badline had occurred and the gap went unnoticed. Fix, mocks first (`StealCyclesContractTests`, four fixtures, implementation run failing on the missing wrap): `clock-observer-x64sc.c` wraps `vicii_steal_cycles` (called cross-object from `mainc64cpu.c` 118 in `c64cpusc.o`), executes the same three statements and calls `vicii_cycle` cross-object, so every stolen cycle runs through `__wrap_vicii_cycle` with `vice_sharp_stolen_cycle` set; the real function is never called (the test double exits 9 if reached). This is the one place the wrappers restate VICE statements instead of forwarding them.

Proof (09:17 to 09:31, x64sc 4,000,000 cycles, display on): stock, withheld and keep exit screenshots identical (`308BEE06...`, different from the 1M image, so the screen had changed); `VICE_GATE` lines 3,999,998 in both gated runs with zero forward gaps in the clock sequence; 3,891,785 accesses observed withheld (108,213 cycles without a CPU access, the stolen cycles) versus 1,479,503 kept; zero keep accesses absent from the withheld run; runner `equivalent: every check passed`.

## S4b: VICE_OBS composition

- `obs-emitter.c` (pure; `ObsEmitterContractTests`, seven fixtures, every column corruption rejected, five lenient variants rejected): per CPU cycle `PHI1` then `PHI2`, half-cycle `2n` and `2n+1`; PHI1 idle with address 0 and the exposed PHI1 bus value (amendment A1); PHI2 with the cycle's access or idle with an empty data column (fails closed as `Missing:Data`); AEC per machine rule (amendment A2 for x64sc, D15 for xvic).
- `bus-adapter.c` collects the cycle's first access (further accesses before the hook become `VICE_BUS,extra,...`, observed at reset: VICE's non-cycle vector read and opcode fetch), composes the pair through the machine glue, writes it to stderr with the fail-closed flush check, then runs the unmodified gate adapter; raw per-access lines only with `VICESHARP_BUS_RAW=1`. `BusAdapterFixtures` (mocks first) expect the pair between the cycle line and the gate record.
- Glue: `obs-fields-x64sc.c` (interrupt mask through the real `interrupt.h`; BA from the flags `vicii_cycle` returned; stolen from S4a; PHI1 data from `vicii_read_phi1()`), `obs-fields-xvic.c` (D15 constants; mask through `interrupt.h`; PHI1 data from `vic.vbuf` through `victypes.h`); both compiled with `-Wall -Wextra -Werror` against the real headers of the work tree and the configured `config.h`.
- `ObservationLineDecoder`, the real step 1 decoder, bound to the identical 53 fixture cases (valid, missing, malformed, framing) the mocks proved; `ObservationTraceValidationTests` validates whole traces (no malformed or framing fault, no missing field other than idle PHI2 data, consecutive half-cycles alternating PHI1 and PHI2, PHI1 always idle, one PHI1 per PHI2), writing a summary beside each trace.

## S4c: real traces (composing binaries relinked 09:32; x64sc `09B40513...`, xvic `CECE95ED...`)

100,000 cycles, both machines (09:40:50 to 09:42:33, run `equivalence-linux/20261020T094050/`): screenshots identical across stock, withheld and keep; gate sequences identical, no clock gaps; zero keep accesses absent; `VICE_OBS` lines 199,996 (xvic) and 199,998 (x64sc) per gated run, exactly twice the gate lines; withheld traces decode with 0 errors, 0 sequence faults and 0 idle-missing (one CPU access every cycle); keep traces 0 errors, 0 sequence faults, 52,185 (xvic) and 56,497 (x64sc) idle PHI2 records with the fail-closed empty data (the fetches the kept fast path hides); runner `equivalent: every check passed`, validation 6 passed.

First real records (observation): x64sc `VICE_OBS,1,0,PHI1,0,1,idle,0,0,0,0,7` and `VICE_OBS,1,1,PHI2,1,1,read,65532,226,0,0,7` (reset vector low byte, `$FCE2`), extras `read,65533,252` and `read,64738,162`; xvic `VICE_OBS,1,0,PHI1,1,0,idle,0,0,4,0,7` (interrupt mask 4 = `IK_RESET` pending at start) and `VICE_OBS,1,1,PHI2,1,0,read,65532,34,4,0,7` (`$FD22`); x64sc at clock 17,214 a record with mask 66 (`IK_IRQ` | `IK_IRQPEND`), the first CIA interrupt.

1,000,000 cycles both machines (09:42:51 to 09:56, run `20261020T094251/`): every check passed; xvic 1,999,994 and x64sc 1,999,998 `VICE_OBS` lines per gated run (twice the gate lines), withheld traces 0 errors, 0 sequence faults, 0 idle-missing; keep traces 0 errors, 0 sequence faults; screenshots identical (`DD3221CF...`, `5283DAEF...`); zero keep accesses absent; validation 6 passed.

4,000,000 cycles x64sc, display on (09:56:37 to 10:25, run `20261020T095637/`): every check passed; 7,999,996 `VICE_OBS` lines per gated run; withheld trace 7,891,781 records decoded with 0 errors and 0 sequence faults plus 108,215 idle PHI2 records with the fail-closed empty data, exactly the cycles without a CPU access, and 216,430 records carrying `stolen` 1 (two half-cycles for each of the 108,215 stolen cycles; the badline and sprite-fetch cycles the S4a wrap made visible); keep trace 0 errors, 2,520,496 idle PHI2 records (the hidden fetches plus the stolen cycles), same 216,430 stolen records; screenshots identical (`308BEE06...`); gate sequences identical with zero clock gaps; zero keep accesses absent; validation 4 passed (18 s over 16 million lines).

Observation, not inference: on x64sc the `stolen` count is identical between the withheld and keep runs, and the idle PHI2 count of the withheld run equals the stolen-cycle count, so every cycle in which the CPU was not on the bus is accounted for as a stolen cycle and no CPU access went unobserved.

## Open

- Payton: amendments A1 and A2 (`OBSERVATION-RECORD-SLICE.md`).
- Windows: S2b build and all S3, S4 runs (LEGION2, after Payton's clearance; its session unreachable since 08:37).
- S5 legacy adapter.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 95%, accuracy 97%, completeness 92%
