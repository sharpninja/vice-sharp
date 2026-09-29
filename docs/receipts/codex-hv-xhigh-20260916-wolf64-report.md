## Hostile validation

2026-09-16, Codex / GPT-6 Astra xhigh. Reviewed HEAD `687b03a75c1971bdb4bcbe07f2b5ea2d94f47b0d` plus the working-tree changes. **OverallVerdict: DISAGREE.**

- **C1 — PASS.** Independently read the dumps: both DDRs are `$2F`; processor-port values are `$24/$34`, selecting config 4 without a cartridge. Managed `$D800–$DBE7` contains only `$00/$FF`; native has 143 distinct values. Differences are 919/1,000 there and 19/8,000 in the bitmap. VICE initializes those pages to `ram_store`, and `c64meminit_io_config[4]=0` prevents installing `colorram_store` (`c64meminit.c:96–100,166–187`; `c64memsc.c:519–522,597–600,816–818`). The HEAD version’s unconditional color-RAM block diverted banked-out stores and returned before updating RAM.

- **C2 — PASS.** The current `C64MemoryMap.cs:467–478` routes banked-out stores to full-byte RAM; `WriteIo:1019–1022` retains the four-bit color store. Both named regression tests passed after rebuilding. The managed test explicitly verifies retained color RAM after restoring I/O. **The diagnosis and scoped store-path solution are VICE-faithful.** These tests do not establish corrected Wolf64 playfield pixels.

- **C3 — FAIL as written.** The register/PC trace is reproduced: sample 2,029,945 has managed `$EA11`, native `$EA0F`, and matching `A=20 X=08 Y=10 S=F9 P=25`; the preceding sample advances 44 native cycles from VIC x=11 to x=55. ROM bytes at `$EA0A` are `A9 20 91 D1 88 10 F6 60`, confirming DEY followed by taken BPL to `$EA07`. **However, LOAD had not been typed:** live automation was `phase=WaitingForReady, keyIndex=0/12, readyWaitFrames=103, pressed=''`. Both actual machine counters were **2,031,359**; `Run():202–223` reports its iteration index as `FirstMismatchCycle`. This is a startup mismatch with a disk attached and LOAD queued, before command entry. Both sprite-enable registers are zero. Native raw D018 is `$14`, which reads as `$15` because VICE forces bit 0 high (`viciisc/vicii-mem.c:639–642`). The visible-PC localization is correct; the overall diagnosis is **incomplete**.

- **C4 — PASS.** A fresh sample-209 probe shows `opcode=D0, previousOpcode=C8, instructionPC=FD5D, cycle=0, stagedFallthrough=false`, while both visible PCs are `FD5F`. Since the helper includes INY (`Mos6502.cs:2456`), the proposed condition necessarily exports the wrong `FD5D` in both startup gates. I verified this counterexample without restoring the broad patch. The initially present stale DLL contains the DEY-only comparison against `$88` followed by `_visiblePC=_instructionPC`; executing it reproduced sample 2,003,339, managed `FD2D` versus native `FD2F`. Previous-opcode classification cannot determine the correct export phase.

- **C5 — PASS for the current source tree.** `git diff HEAD -- src/ViceSharp.Chips/Cpu/Mos6502.cs` is empty. Lines 2263–2271 retain fall-through for the unstaged taken path; neither rejected condition is there. Initial build output was stale and still contained the DEY variant, so source reversion alone did not establish binary reversion. After the supported rebuild, the CPU PDB checksum matches the reverted source. No deployed package was assessed.

- **C6 — PASS.** The rebuilt Wolf64 test fails before READY/LOAD entry with D015 zero. Neither first-game-scene lockstep nor a fixed BPL mismatch is established.

- **C7 — PASS.** The independently observed sample-209 state proves the proposed implied-opcode condition would regress the existing startup gates. **Reject it; do not restore it.**

**Explicit FAIL list:** C3 — LOAD was queued but no key was typed; 2,029,945 is a comparison-sample index, not the actual machine cycle.

**VICE-faithful next fix, not implemented:** model fetch/branch export phases and BA/RDY stalls explicitly. VICE’s FETCH clocks export the opcode PC; BRANCH then increments PC by two, performs the dummy read/clock with fall-through visible, and finally jumps (`c64cpusc.c:149–174`, `6510dtvcore.c:986–1005`). Hosted `CLK_INC` exports registers after VIC advancement (`c64cpusc.c:58–68`); stolen VIC clocks advance time separately (`viciisc/vicii-cycle.c:628–635`). Preserve the CPU phase across steals and align managed advancement with those boundaries. Validate stalled and unstalled DEY/BPL and INY/BNE paths. Also report sample index, actual cycles, and input-automation state distinctly in the harness.

Validation: supported Release build succeeded with **0 warnings, 0 errors**. Five reviewed managed files match their PDB source checksums. Current runs executed **9 tests: 8 passed, 1 failed, 0 skipped**. The failure is the reproduced startup mismatch, not a passing Wolf64 gate. All nine requested source files retained their pre-review SHA-256 hashes. Whole-tree `git diff --check` passed. No product source was modified.

Commands used:

```powershell
dotnet build .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-restore
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter 'FullyQualifiedName~C64MemoryMapPageDispatchTests|FullyQualifiedName~RamOnlyPla_D800Store_MatchesNativeRamPeek|FullyQualifiedName~LockstepValidationTests.First10000CyclesMatch|FullyQualifiedName~LockstepValidationTests.First100000CyclesMatch'
$env:VICESHARP_WOLF64_LOCKSTEP='1'
dotnet test .\tests\ViceSharp.TestHarness\ViceSharp.TestHarness.csproj -c Release --no-build --filter 'FullyQualifiedName~C64ColorRamPlaNativeTests.Wolf64Disk_ReportsFirstMismatchCycle'
```

Test invocations additionally retained TRX, normal console output, VSTest diagnostics, and a two-minute blame-hang limit. Evidence directory: [TestResults/codex-hv-wolf64-20260916T132200Z](F:/GitHub/vice-sharp/TestResults/codex-hv-wolf64-20260916T132200Z). Authoritative files are `current-focused.trx`, `current-wolf64.trx`, `sample209.stdout.log`, and `wolf64-state-verified.stdout.log`; unprefixed initial test receipts used stale binaries.

SHA-256 receipts:

- Current focused TRX: `D365B5810A6D63DDCCC3A5AF31E454EA08D4D3A407B08E859EC9CDE1C830515F`.
- Current Wolf64 TRX: `5187B61CC73A3F84A080E627C16ED5C1AA674A42C0DF95F7D9DFACA242580B11`.
- Actual loaded native DLL: `049F0B1B19EF51B91B2368B65D13E954A13DD6D08F6B9B19CB546B1A2B9946AC`.

Report: [codex-hv-xhigh-20260916-wolf64-report.md](F:/GitHub/vice-sharp/docs/receipts/codex-hv-xhigh-20260916-wolf64-report.md).

=== VERDICT JSON ===
{"verdict":"DISAGREE","fail_list":["C3: LOAD had not been typed; 2029945 is the sample index, while both actual cycle counters were 2031359."],"color_ram":"AGREE","bpl_diagnosis":"INCOMPLETE","implied_opcode_patch":"REJECT"}

<oai-mem-citation>
<citation_entries>
MEMORY.md:157-162|note=[Review-only scope and baseline separation; findings independently verified]
</citation_entries>
<rollout_ids>
01a00ce5-d30d-7451-ab45-fa3e4fbb825f
</rollout_ids>
</oai-mem-citation>
