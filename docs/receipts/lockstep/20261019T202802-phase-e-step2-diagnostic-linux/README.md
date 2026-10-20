# Phase E step 2 diagnostic baseline, LAB-OMARCHY, 2026-10-19T20:28

Plan: `docs/receipts/handoff/HANDOFF.lockstep-plan.md` section 4 Phase E step 2 ("remediate the five preexisting CPU/NMI failures named in Phase A and the red acceptance criteria that touch the CPU, CIA, VIC-II and port; reconcile each to its VICE-derived AC first, never by weakening assertions"), amended by `HANDOFF.lockstep-plan.v2.md` section 4 (each failure reconciled to `FR-CPU-002` ac-05 and `TR-CYCLE-001` AC1 to AC3 before any assertion is touched; `DIAG-CPUINT-001` closes only through this slice) and section 10 (the 16 `CpuVisiblePcTimingTests` failures inherited by this step). Goal TODO `GOAL-LOCKSTEP-REMEDIATION-001`, task "Step 8", Phase E. Diagnostic only: no code, no requirement or TODO change, no done claim. Phase E has not entered (its entry is Phase D accepted); this baseline and the VICE references below are the preparation the step requires, produced while Phase C step 3 waits on an operator ruling.

Measured by Claude Code (claude-fable-5-1) on LAB-OMARCHY, `develop` at `feb1fd4`, dotnet 10.0.402, 20:28:02 to 20:28:50, by `~/codex-dispatch/scripts/Invoke-FocusedTest.ps1`:

`dotnet test tests/ViceSharp.TestHarness/ViceSharp.TestHarness.csproj -c Release --nologo --filter "FullyQualifiedName~CpuInterruptTests|FullyQualifiedName~Mos6510NmiTests|FullyQualifiedName~CpuVisiblePcTimingTests" --logger "trx;LogFileName=cpu-diagnostic.trx"`

Result: 162 total, 142 passed, 20 failed, 0 skipped (`cpu-diagnostic.trx`); the TestHarness build on Linux never invokes `Vice.Native.proj`; `native/vice` inventory 163 entries unchanged. The 20 failures equal the Phase A Linux baseline of 2026-10-18 (4 CPU/NMI plus 16 `CpuVisiblePcTimingTests`), so nothing merged since has changed this area.

## The 20 failures (test, assertion message)

CPU/NMI (the four of `DIAG-CPUINT-001` still red; the fifth, `Nmi_IsEdgeTriggered_HoldingLineDoesNotRefire`, passes):
1. `CpuInterruptTests.LdaImm_IrqOnLastClk_DoesNotDispatchBeforeNextFetch`: `Assert.Equal` expected 169 actual 0 (the loop of 8 steps never observed opcode $A9 at cycle 0 with A=$20; `DebugOpcode` stayed 0).
2. `CpuInterruptTests.RtsThenLdaImm_NextTick_FetchesFollowingOpcode`: expected 169 actual 0 (same observation after RTS, 24 steps).
3. `CpuInterruptTests.Rts_DelayNextFetchTick_DoesNotTakeIrqBeforeReturnFetch`: expected 169 actual 0 (same observation; the preceding PC=$8003 assertion passed).
4. `Mos6510NmiTests.Nmi_RtiReturnsCpuToPreNmiState`: "CPU did not reach PC=$0400 within 64 ticks (PC=$0000)" after `Nmi()` entered the handler (sanity asserts PC=$0800 and S=$FC passed) and RTI ran.

`CpuVisiblePcTimingTests` (16):
5. `TaxThenTakenBmi_Cycle1_ExportsFallthrough`: "Wolf64 2142610 BMI dummy INC_PC; PC=$800F cyc=1 trail=0 full=True nonOvlF=True nonOvlR=True".
6. `Jsr_FinalOperandRead_CanBeHeldByExternalBusOwner`: expected 1 actual 0.
7. `WaitLoopIteration_IrqPending_CountsThirteenViceClocks`: "wait-loop irq_delay delta=12 (want 11: 9 host CLK + 2 collapsed zp FETCH)".
8. `Jsr_ReturnAddressPushTimingMatchesHostedVice`: expected 248 actual 247.
9. `CpyImmAfterTakenBcsIny_LastClk_ExportsNextPcAndFlags`: expected 32778 actual 32776.
10. `AbsoluteStoreThenImmediateLoad_SecondFetchCompletesLoad`: expected 2 actual 1.
11. `Rts_DebugCycleTwo_HasCompletedLowBytePull`: expected 248 actual 249.
12. `LdaIndYAfterTakenBcsClc_DataRead_ShowsLoadedA`: expected 76 actual 31.
13. `InyAfterFusedCpyImm_LastClk_ExportsNextPcAndY`: expected 44 actual 43.
14. `SbcAbsYAfterSec_NoBranchLag_LastClk_FusesAluAndNextPc`: expected 7 actual 76.
15. `TakenBneThenLdaImm_SecondFetch_HoldsPreOpAAndOpcodePc`: expected 2 actual 1.
16. `CmpAbsAfterTakenBccLdaAbs_LastClk_ExportsNextPcAndFlags`: expected 32780 actual 32777.
17. `InyAfterPla_LastClk_ExportsNextPcAndY`: expected 43 actual 42.
18. `TaxAfterNotTakenBne_LastClk_ExportsNextPcAndX`: expected 255 actual 0.
19. `TakenBne_StolenFetch_Cycle1_ExportsFallThrough`: expected 2 actual 1.
20. `StaAbsAfterTakenBcsRolA_WriteSample_ExportsNextPc`: expected 32779 actual 32776.

## VICE references for the reconciliation (read in the Linux materialization of cc67418c, `artifacts/immutable-vice/.../source/vice/src`)

- `interrupt.h` line 39: `#define INTERRUPT_DELAY 2`.
- `interrupt.h` lines 141 to 196 (`interrupt_set_irq`): on the first active IRQ source `global_pending_int |= IK_IRQ | IK_IRQPEND`, `irq_pending_clk = CLOCK_MAX`, `irq_delay_cycles = 0`, `irq_clk = cpu_clk` (or fixed up after stolen cycles); on removal of the last source `irq_pending_clk = cpu_clk + 3` (an acknowledged IRQ may still trigger for three cycles).
- `interrupt.h` lines 199 to 250 (`interrupt_set_nmi`): edge semantics, `nmi_delay_cycles = 0`, `nmi_clk = cpu_clk`; `interrupt_ack_nmi` clears `IK_NMI`; `interrupt_ack_irq` clears `IK_IRQPEND` and resets `irq_pending_clk`.
- `6510core.c` lines 436 to 481 (`DO_INTERRUPT`): dispatch only when `interrupt_check_nmi_delay` or (`I` clear or `OPINFO_DISABLES_IRQ`) and `interrupt_check_irq_delay` hold; the 7-cycle sequence (two dummy fetches when `NMI_CYCLES == 7`, PCH, PCL, status with B clear, I set, vector read, jump); NMI takes over IRQ when `CLK >= nmi_clk + INTERRUPT_DELAY`.
- `6510core.c` lines 2306 to 2345 (main loop head): `CPU_DELAY_CLK`, `PROCESS_ALARMS`, the jammed-CPU acknowledgment, the `irq_pending_clk <= CLK` acknowledgment, then `DO_INTERRUPT(pending_interrupt)` before the opcode fetch.
- `6510core.c` lines 226 to 254: `OPCODE_DELAYS_INTERRUPT`, `OPCODE_DISABLES_IRQ`, `OPCODE_ENABLES_IRQ` (last-opcode info that gates dispatch for CLI/SEI/PLP/RTI and the taken-branch delay).
- Managed counterparts (grep): `src/ViceSharp.Chips/Cpu/Mos6502.cs` `_lastOpcodeDelaysInterrupt` (lines 54, 577, 1227, 3593 to 3631, 4472) and the staged dispatch sequence commented at lines 126, 361, 406; the interrupt gating used by these tests runs through `SystemClock` with `InterruptLine`.

## Observations and inferences (marked)

- Observation: failures 1 to 3 share one symptom. Each test steps the clock until it observes opcode $A9 at `DebugCycle` 0 with A=$20 and never does within its step budget (`DebugOpcode` 0). Failure 4 reaches the RTI handler and returns to PC $0000, which is what RTI pulls when the three pushed bytes are not where it pulls from.
- Inference: the common factor is the instruction-boundary export (`DebugOpcode`, `DebugCycle`, and the stack state visible to the test bus) under the one-cycle-lag convention after the PR7 merge, not the IRQ delay arithmetic itself; the reconciliation step must first prove what the export shows at each tick against the Wolf64 samples the tests cite (2093250 to 2093252, 2208208) before touching delay logic. This is the first question of the slice, not a conclusion.
- Observation: the 16 timing failures are all one-off cycle or PC exports (expected minus actual is 1 cycle, 1 to 3 PC bytes, or one register value) on branch, JSR, RTS and fused-ALU cases that cite hosted-VICE per-cycle exports.
- Inference: they belong to the per-cycle phase model (locked decision 7 of the goal plan, Phase E step 4), and a per-test reconciliation against `6510core.c` opcode macros is required before deciding whether the hybrid phase flags can satisfy them.

## What this is and is not

A measured baseline and the located VICE anchors for the Phase E step 2 slice plan. Not a slice, not a fix, not a status change. The slice plan (mock-first per the operator's BDPv4 statement, one test family per slice, Wolf64 sample citations verified against the materialized source) is written when Phase E is entered.

Signed: claude-fable-5-1-high@ClaudeCode | Confidence: correctness 98%, accuracy 99%, completeness 95%
