# Phase C step 3 on PAYTON-LEGION2: named blocker, CPU bus access fields not reachable by link-time wrapping

Goal GOAL-LOCKSTEP-REMEDIATION-001, task "Step 8", Phase C step 3 (OMAR-VICE# brief 2026-10-19T19:52: "If an inlined or optimized path hides a required field from link-time wrapping, stop at that field with a named blocker and the probe evidence; do not lower the field set"). Stopped before any wrapper or C code was written. Nothing under native/vice or the materialization was changed; the probe only reads object files.

## Probe

`Get-CpuObservabilityProbe.ps1` (read-only, analyzer 0) runs GNU nm 2.45.1 on the CPU and cycle objects of the materialization `artifacts/immutable-vice/cc67418cba58e71d8e70fd353e9de47199ad2075` and writes `probe/` (undefined and defined symbol lists per object, hashes, `classification.txt`). Undefined symbols are the calls and data references that leave an object; they are the only targets `ld --wrap` can intercept.

- `vic20/vic20cpu.o` (xvic CPU core) SHA256 6F54B7CC158F437B..., `c64/c64cpusc.o` (x64sc CPU core) SHA256 AA7F8318266FB55C..., `vic20/vic-cycle.o`, `viciisc/vicii-cycle.o`.
- xvic.exe SHA256 C01442BC1F15DD8C7006FABE0D35DFA9707664A3120DD1FA014A51EEB481B92C (same as the Oct 8 pin); x64sc.exe SHA256 B3ED5F3AC6B2DC05D084E63CF1406B516EB3EEF456B59154B9AD287123F87AEF.

## Field by field

Reachable by link-time wrapping (evidence: `probe/classification.txt`):

- Per-cycle hook: the CPU cores call `vic_cycle` (xvic) and `vicii_cycle` (x64sc) across the object boundary, so a wrapper runs once per CPU cycle.
- Clock: `maincpu_clk` is a global in the CPU object, readable at the hook.
- Interrupt mask: `maincpu_int_status` is a global (VICE's interrupt status, holding the `enum cpu_int` pending mask), readable at the hook.
- BA, x64sc only: `maincpu_ba_low_flags` is a global; `vicii_steal_cycles` is a call from the CPU core.

Not reachable (the blocker): the address, data and access kind (read, write, idle, dummy) of each CPU bus access, on both machines.

1. Reads and writes dispatch through function-pointer tables: `_mem_read_tab_ptr`, `_mem_write_tab_ptr`, `_mem_read_tab_ptr_dummy`, `_mem_write_tab_ptr_dummy` are type `B` data in `vic20mem.o` and `c64memsc.o` (`read_func_ptr_t *_mem_read_tab_ptr;`, vic20mem.c line 99). The CPU calls through them indirectly, which `--wrap` cannot intercept.
2. `memmap_mem_read` and `memmap_mem_store` are local to the CPU object.
3. Instruction fetch uses the inline `bank_base` fast path, a raw host load (Oct 8 probe: xvic RVA 0x50ce4, `mov eax,dword ptr[rdx+rax]`, `docs/receipts/native-vice/20261008T082100-0500-phase2-feasibility/bus-runtime/README.md`).

Without address and access kind, the Data field cannot be attributed to a bus access either. The field set is not lowered.

## A second, separate item: BA and AEC on the VIC-20

The xvic CPU core has no BA state (`maincpu_ba_low_flags` and `vicii_steal_cycles` are absent from `vic20cpu.o`); the VIC-20 VIC chip does not stop the CPU through a BA line. The step 1 contract requires `Ba` and `Aec` on every record and fails closed when they are missing. Recording a constant for the VIC-20 would be an inference, so this needs a contract ruling (for example a per-machine field applicability table derived from the hardware documentation and VICE source), not a choice made here.

## Options for the operator (none lowers the field set; none edits VICE source)

- A. VICE's own watchpoint path. VICE already reroutes every table access: vic20mem.c line 539, `_mem_read_tab_ptr = _mem_read_tab_watch;`, enabled through `mem_toggle_watchpoints` (called from the CPU core) and driven by the monitor interface. This is a public mechanism, first in the plan's mechanism order. Needs proof that it covers the `bank_base` fast fetch and the dummy accesses, and it costs a monitor round trip per access.
- B. Runtime table interposition from the external wrapper. A wrapped startup call replaces the four table pointers with forwarding tables that record and then call the original entries, and a wrapped `mem_mmu_translate` (a call from the CPU core) withholds the `bank_base` fast path so fetches take the table route. No source change, but it changes which VICE code path executes, so it needs an equivalence proof against the stock binary before any observation counts.
- C. Debugger hardware observation (the Oct 8 method). Already shown to work after startup, but it attaches at an arbitrary clock, not deterministically at reset, and is slow.

## Self-assessment

- Accuracy 98: every symbol claim is in `probe/` with object hashes; the fast-fetch claim cites the retained Oct 8 receipt.
- Completeness: not applicable as a pass; step 3 is blocked. What is unproven: whether option A covers fast fetch, and whether option B is behavior-equivalent.

PLAN-VICEORACLE-001 task 3 stays open.
