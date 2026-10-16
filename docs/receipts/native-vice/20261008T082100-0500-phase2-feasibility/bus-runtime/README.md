# External stock VICE access observations

Recorded 2026-10-08T11:49:07-05:00. These are partial diagnostic observations,
not a complete bus decoder, deterministic start, implementation acceptance or
lockstep parity proof. No production decoder was implemented in this slice.

## What ran and what it proves

- `Invoke-AttachHardwareProbe.ps1` waited for the separately linked gate probe's
  actual `VICE_GATE,7` output, attached CDB, armed an eight-byte hardware clock
  watchpoint and supplied one `c`. `after-init-watch.result.json` and its raw
  logs record one hit and clock 7 to 8. This establishes a working post-startup
  hardware-watchpoint diagnostic, not the cause of earlier startup failures.
- `Invoke-StockDebugProbe.ps1` launches the unchanged stock xvic, waits for its
  actual VSync startup message, then attaches CDB with local symbol paths. Its
  finite diagnostic waits are not limits on hostile reviewers. Each run retains
  launch arguments, PIDs, binary/script hashes, stdout, stderr and exits.
- `stock-fast-fetch-01/` observes the actual original native instruction at
  xvic RVA `0x50ce4`, bytes `8b0402`: `mov eax,dword ptr[rdx+rax]`. Before:
  RAX `e5ec`, RDX `00007ff63fbed020`, effective host address
  `00007ff63fbfb60c`. After one instruction: EAX `f002928d`. Clock is
  `00646b26` on both sides. No emulated-PC lookup or emulated-memory peek was
  used to manufacture the observed result.
- `stock-access-stream-01/` records 128 clock-write hits, 39 paired fast-fetch
  before/after observations and 20 normal-store entry observations. The dummy
  store breakpoint records zero hits in this sample. This is not evidence that
  dummy stores do not occur or are comprehensively covered.

The access stream's raw debugger stdout SHA256 is
`660921BD12E83A4A9FC918BC4D343D9E0F0B9D92A986473A3CF0AFA0B6709F4A`.
Ad hoc diagnostic extraction is retained in `observation-extraction.json` and
`fetch-pair-check.json` inside that run directory. There are 127 consecutive
clock deltas, all +1; all 39 fetch pairs have unchanged clock and host RIP +3.
The first observed store has ECX `cc`, EDX `0` at clock `0075fc26`.
These extraction scripts are not a tested production parser or bus comparator.

All three successful diagnostic runners report CDB exit 0, target exit
`-1073740972`, and parent `forcedTermination: false`. CDB `q` terminated the
debuggee. This is not normal native shutdown or a passing emulator run.

## Source mapping and missing coverage

The separately built target is stock commit
`cc67418cba58e71d8e70fd353e9de47199ad2075`, with xvic SHA256
`C01442BC1F15DD8C7006FABE0D35DFA9707664A3120DD1FA014A51EEB481B92C`.
`stock-maincpu-disassembly.txt` retains the source-annotated disassembly.
Its SHA256 is `63835631060373AD981C9A0F4953800D00B05013E9DB6D3B5F917D44EA380BDA`.
The compiled `mainviccpu.c` includes `6510dtvcore.c`, whose line 1804 maps to
this fetch. Do not substitute the generic CPU core as the evidence source.

A four-byte host load is not four emulated bus cycles. The native path masks
the packed result to 24 bits and clocks instruction fetch according to its
actual branches. A future adapter needs independently pinned contracts and
mock-first tests before deriving any per-cycle event from these observations.

Regular reads and inlined dummy reads are not captured in this stream. Neither
IRQ/NMI stage nor stolen-cycle evidence is captured. Attachment occurs after
startup at an arbitrary running clock, not a deterministic reset boundary.
The existing callback gate and these hardware observations are not yet one
complete synchronized oracle. All-device freeze, long-delay equivalence,
complete read/write/dummy classification, cycle alignment and full real
emulator gates remain mandatory. Missing observations must fail closed.

Memory-table interposition alone misses fast fetch. Index 256 in the native
257-entry tables also requires preserving actual table-entry identity rather
than blindly using an address-only forwarding rule. No such interposition was
implemented in this slice.

## Preservation and review

`integrity-result.json`, measured 2026-10-08T11:48:24-05:00, records zero changes
to all 3847 original stock source files, all 99 stock link inputs, the eight
native baseline fields and the stock xvic hash. `native-before.json` and
`native-after.json` retain full dirty-baseline inventories. The preexisting
dirty `native/vice` is preserved, not reclassified as a clean oracle.

See `../debugger-runtime/README.md` for unsuccessful fixture/startup probes.
Include every success, failure, script, raw log and extraction in the noon
Opus catch-up. Latest code suites remain 50+199+82 = 331 native-safe passing
tests, not a new bus-observation unit suite or full real-emulator acceptance.
No goal, requirement, TODO or plan completion is authorized by this receipt.
