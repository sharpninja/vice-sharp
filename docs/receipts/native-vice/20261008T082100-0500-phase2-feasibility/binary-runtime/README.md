# Stock VICE binary snapshot acknowledgements

Recorded 2026-10-08T09:56:27-05:00. This is limited Phase 2 feasibility evidence,
not phase acceptance, full native resume, managed parity, or a completed FR.
The pinned target is `cc67418cba58e71d8e70fd353e9de47199ad2075`.

## What ran

`xvic-01/launch.json` and `x64sc-01/launch.json` retain executable hashes, exact
arguments, working directories and the production codec hash. These are stock
executables from the approved upstream build in the separate source checkout.
Neither the original source bytes nor `native/vice` was modified by the probes.
`integrity-result.json` records zero native mismatches and zero changes among
3847 original stock source files.

Both emulators stopped at BASIC ready with sound enabled on the dummy backend.
The runner checked that each loopback monitor listener belonged to the launched
PID. Socket read/write timeouts were 15000 ms. This is a diagnostic transport
timeout, not an HV reviewer limit. The codec was loaded from bytes to avoid
locking subsequent .NET build output.

Each `exchange.jsonl` contains complete raw request and response packets as hex,
with local timestamps. The runner captured the received header and body before
parsing the same bytes with `BinaryMonitorProtocol.ReadResponse`. It retained
unsolicited packets, correlated request IDs, and rejected unexpected IDs. It
does not turn a timeout, process exit, event, or native error into success.

`diagnostic-functions.clixml` preserves the exact executed receive/send helper
bodies exported from the persistent console, not a reconstructed protocol
implementation. `packet-replay-result.json` re-parses the retained responses
with the production codec: 17 xvic responses and 18 x64sc responses, with
8 unsolicited events and exactly one native missing-file error in each stream.
Every retained frame is consumed exactly. This replay is packet evidence only.

For each machine, request 1 pinged, 2 saved `fresh.vsf`, 3 stepped one instruction,
4 saved `after-step.vsf`, 5 loaded `fresh.vsf`, 6 stepped one instruction, and 7
saved `after-resume-step.vsf`. `acknowledgements.jsonl` records all six successful
snapshot/step acknowledgements, including exact response type, ID, error and body.
Load returned PC `$e5e8` on xvic and `$e5d4` on x64sc.

Request 8 attempted a nonexistent file. Both native responses had type 0,
error 143 (`0x8f`) and an empty body. The actual production `RequireSuccess`
guard rejected each with `InvalidDataException`; see `missing-snapshot-result.json`.
This supplies the explicit failure acknowledgement missing from the earlier
text-monitor probes, whose `undump` command discarded the native return value.

x64sc request 9 loaded the real adapter's normalized historical MAINCPU 1.2
fixture. `legacy-load-result.json` records type `0x42`, error 0, PC 22377 (`$5769`),
and a verified retained copy with SHA256
`CB4EB08D47A9C1F4BA7F6CD30A1ED5CAFEF0B8C50AAE57E9153E4B1831E8805E`.
It does not establish every device field, synthetic 1.3 runtime compatibility,
or production snapshot-path integration. Both quit commands were acknowledged
and both processes exited 0, as recorded in each `exit.json`.

## Snapshot differences retained, not masked

Each `snapshot-comparison.json` retains whole-file hashes, all module hashes and
every changed byte between uninterrupted and resumed one-instruction results.
Both have `fullStateEqual: false` and `fullParityProven: false`.

- xvic: VIA1, VIA2 and 1541VIA1D0 payload offset 22 changed `e0` to `c0`;
  VIA2D0 changed `60` to `40`; DRIVE8 offset 27 changed 2 to 0;
  DRIVECPU0 offset 43 changed 66 to 68.
- x64sc: SIDEXTENDED offsets 28 and 32 changed 0 to 116; DRIVE8 offset 27
  changed 2 to 0; DRIVECPU0 offset 43 changed 42 to 44; 1541VIA1D0 and VIA2D0
  have the same respective VIA changes above. MAINCPU modules did not differ.

Source anchors below refer to the separate pinned source checkout, not the dirty
`native/vice` checkout. They explain fields and possible mechanisms, not permission
to alter native behavior or omit differences from the comparator.

- **Direct encoding evidence:** `vice/src/core/viacore.c:1985` writes CABSTATE
  masks `80,40,40,20,20`; reader line 2162 decodes `80,40,20,10,08`. Re-encoding
  the decoded `e0` yields `c0`, and decoded `60` yields `40`, matching the observed
  bytes. The writer aliases distinct state bits. This is a stock serialization
  asymmetry; the probe does not establish all downstream behavioral effects.
- **Field mapping, branch not observed:** `vice/src/drive/drive-snapshot.c:184`
  writes `GCR_head_offset` at payload offset 27. Load reads it and later calls
  `drive_set_half_track` (line 570). `vice/src/drive/drive.c:724` rescales the
  offset for the selected track or sets it to zero when the current track size
  is zero. This is consistent with the no-disk probe, but the actual branch and
  track sizes have not been captured.
- **Field mapping, cause still open:** `vice/src/drive/drivecpu.c:572` serializes
  `stop_clk` at payload offset 43. Binary command dispatch synchronizes drives
  at `vice/src/monitor/monitor_binary.c:1774`; `drivecpu.c:374` computes cycles
  and increases `stop_clk`. The exact reason for the observed +2 is unproven.
- **Field mapping and candidate mechanism:** `vice/src/sid/sid-snapshot.c:663`
  writes 32 SID registers then `bus_value`, so offsets 28/32 are ENV3/readback
  and bus value. `vice/src/resid/sid.cc:353` reads registers through `read`, which
  changes bus state (line 176). `EnvelopeGenerator::readENV` returns cached `env3`
  (`envelope.cc:273`); single-cycle clock updates it (`envelope.h:118`). Native
  `SID::write_state` restores envelope counters but does not assign `env3`.
  This is a source-supported candidate for the differing readback, not a proven
  runtime causal trace. Do not report the SID difference as resolved.

Whole-file native save/load/save identity is a diagnostic here, not a newly
invented requirement. The external adapter must preserve specified bytes, and
ViceSharp must follow actual immutable VICE behavior. No managed divergence can
be inferred solely from a native-to-native difference. No comparison exclusions
or assertion weakening were added.

## Test chronology and remaining gates

See `tests/ViceSharp.VsfCompatibility.Tests/BINARY-MONITOR-SLICE.md` for the
preimplementation contract. Sibling receipt directories preserve every run:

- `binary-mock-01`: 125/125 at 2026-10-08T09:28:37-05:00, including 48 new
  protocol mock cases plus 77 prior cases. It preceded the final expanded
  mock gate and was omitted from the original written chronology.
- Thirteen cases were then added before production to cover selected nonzero
  errors `01,02,80,81,82,83,ff`, reserved expected ID, null acknowledgement body,
  null reader stream, the inclusive reader size limit and two encoder boundary
  rows (zero length/ID and maximum length/nonreserved ID). The retained TRX
  name comparison shows exactly these 13 additions and no removed cases.
- `binary-mock-03`: 138/138, including 61 new protocol mock cases plus 77 prior
  cases; `binary-prior-mock-02`: 82/82, both before production codec code.
- `binary-implementation-02`: 199/199, with the same 61 cases bound to the real
  codec; `binary-prior-implementation-01`: 82/82. Both exit 0 with zero failed,
  skipped, aborted, error or timeout counts and unchanged native inventories.
- Retain `binary-mock-02` (one test-name parser failure, fixed before production)
  and `binary-implementation-01` (build failure caused by the parent's loaded DLL
  lock, no test run). Neither is passing evidence.

Chronology correction recorded 2026-10-08T13:47:42-05:00 for F-06. The retained
`binary-mock-02` counters are 137 passed / 1 failed at 09:30:20; its failure is
`FormatException` in the fixture's `AssertAck` name parsing. `binary-mock-03`
passed 138/138 at 09:31:04 after correction, before production binding. Raw
results/TRX were not rewritten. Fresh counters, hashes and the 13 exact added
names are in `../../20261008T134300-0500-hv-evidence-corrections/historical-evidence-readback.json`.

All 281 current tests are native-safe unit, byte and protocol tests. They do not
replace real-emulator acceptance gates. Reset-boundary xvic load stall, full
snapshot-resume behavior, deterministic cycle control, complete bus observations,
native-safe TestHarness integration and 100000-half-cycle parity remain open.
Noon local Opus HV must review all deferred work, including failures and these
limitations. No plan phase, TODO, requirement or goal is marked done.

The subsequent hardware-debugger control probes did not stop as requested.
Their failure logs and unchanged-source proof are retained separately in
`../debugger-runtime/README.md`, not merged into successful snapshot evidence.
