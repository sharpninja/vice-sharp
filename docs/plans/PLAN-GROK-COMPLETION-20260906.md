# Complete Grok’s ViceSharp work

2026-09-06 20:27:16 -05:00

## Summary

Finish the C64/VIC-20 Settings, Warp, RemoteControl, CLI, and video work; repair the baseline failures; then commit and push with complete validation evidence.

**Astra’s hostile plan review: READY**, with the corrections below incorporated. This approves the plan, not the current implementation.

The current baseline remains incomplete: **879 passed, 3 failed, 2 skipped, then aborted on a native hang**. Focused runs also reproduced snapshot divergence and the Avalonia dependency violation. The historical performance tests and both investigated prefetch tests currently pass.

- **Implementation:** GPT-5.6 Sol, High.
- **Independent hostile validation:** GPT-6 Astra, Extra High (`xhigh`).
- **Grok:** no dependency on its availability before the September 11 reset; retain these assignments throughout this work.
- **Environment:** Windows, using PowerShell.MCP and the repository’s supported tooling.
- **Completion:** all required suites, including remote services and paid AI reviews, pass before final push.

## Gated implementation slices

Each slice follows requirements and acceptance criteria → failing regression evidence → implementation → focused green tests → independent Astra review. Resolve blocking findings before advancing dependent work.

### 0. Establish the completion contract

- Inventory the dirty working tree and native submodule. Identify the changes belonging to this work; preserve unrelated files and probes.
- Consolidate Grok’s two plans into a repository-owned completion plan. Update the canonical [HANDOFF.md](F:/GitHub/vice-sharp/HANDOFF.md), which currently misses the latest Settings work.
- Reconcile MCP TODOs and canonical requirements. Preserve the existing picker, visibility, Warp, and RemoteControl IDs, including `FR-MACHINEPICKER-001`, `FR-UISETVIS-001`, `TR-UIAXAML-PICKER-001`, `TEST-UISET-003`, `FR-WARP-001`, `TR-WARP-STATUS-001`, `TEST-UISET-002`, and `TEST-REMOTECTRL-001`.
- Add missing FR/TR/TEST coverage for transactional settings, native lifecycle failures, portable CLI behavior, and the aiUnit dependency change. Connect every changed acceptance criterion to implementation and executable evidence.
- Create an exhaustive test manifest and a Nuke `CompletionTest` target. Account for every test project and every category omitted by ordinary Nuke `Test`.

### 1. Repair native audio state and lifecycle safety

- Separate sound-oracle state from the live VIC-20 mixer. Oracle reset must not zero playback timing or clocks.
- Preserve the existing limits: **create/step 5 seconds; stop 2 seconds**. Enforce absolute monotonic deadlines, remove infinite waits, and propagate failures to managed callers.
- Make disposal idempotent. Never free memory while its worker remains alive. Failed shutdown poisons the shared native-library state, and subsequent operations fail immediately.
- Apply consistent shutdown handling to reset, destroy, and snapshot operations.
- Capture required dirty native-source changes in the supported patch. Prove that the pinned clean native source accepts the patch and produces the tested DLL; retain its hash.
- Require exact audio sample counts and complete PCM equality. Remove comparisons that conceal truncated output.
- Run normal audio → dual-VIA → reset → snapshot → disposal sequences repeatedly in one process, covering PAL and NTSC. Run deliberate shutdown-failure tests in owned child processes.
- Make required native tests fail when prerequisites are missing. Enable the required workload flags, record actual cycle budgets, and restore environment variables after each test.

### 2. Restore baseline correctness

- Correct the two BasicBus expectations to preserve the intentional open-bus latch behavior.
- Add the missing `NativeVice` collection declaration.
- Fix the reproduced snapshot divergence at cycle 2078 using a minimal instruction-sequence regression and the pinned VICE source as the timing reference. Cover taken, untaken, and page-crossing branches on both machine families.
- Keep the existing 5,000-cycle snapshot fixture unchanged. Require it to pass independently and after native lifecycle tests.
- Preserve the currently passing performance and prefetch behavior. Remove obsolete prefetch quarantine metadata only after tracing its acceptance coverage.
- Replace obsolete renderer tests with equivalent cycle-aware acceptance tests before retiring them.
- Move intentionally failing manual diagnostics into an explicit diagnostic-tool surface. Do not count them as passing tests or hide product failures behind exclusions.

### 3. Repair the Avalonia architecture boundary

- Introduce Abstractions-owned flash-cart builder interfaces and a Host composition factory.
- Make Avalonia depend on those interfaces instead of Core flash-cart types. Preserve existing Core and Xbox APIs.
- Preserve observable collection and property-change behavior through the adapter.
- Extend source-boundary validation to relevant C# and AXAML sources.
- Test profile selection, bank updates, import, build, save, and failure reporting through the abstraction using controlled file I/O.

### 4. Make Settings truthful and transactional

Introduce three explicit states:

- **Active:** settings actually used by the runtime.
- **Accepted:** the complete last accepted target, including changes awaiting restart.
- **Draft:** unsaved client edits.

Add `SettingsStateDto(Active, Accepted)` and append optional `State` to both settings responses. Preserve existing fields and protobuf tags; use `GetSettingsResponse.state = 3` and `UpdateSettingsResponse.state = 4`.

Implement these rules:

- Dirty state compares normalized Draft with Accepted.
- Restart state compares restart-relevant Draft fields with Active.
- Plain Apply applies live fields and accepts restart-only changes. It clears dirty state while retaining a pending restart.
- Revert restores Accepted and discards only unaccepted edits.
- Apply + Restart prepares and publishes the replacement runtime, then makes Active and Accepted equal.
- Failed operations preserve prior runtime, accepted state, and draft.

Prepare and validate the complete operation before mutation: profile, family, RAM, pacing, resources, cart image/preset compatibility, and IEC root/unit conflicts. Prepare replacement sessions with media restored before publication. Commit under existing synchronization, defer notifications until success, and roll back failures without transient success events.

Additional corrections:

- Honor Warp through the existing limiter DTO in the same transaction; remove the second Warp RPC from Apply.
- Stop converting attachment exceptions into overall success.
- Derive active cart kind from the actual device. Selecting a nonempty kind requires a compatible image; selecting `none` detaches it.
- Preserve attached images, bank state, presets, writeback behavior, and uIEC attachments across applicable restarts.
- Serialize refresh and Apply, reject stale responses, and preserve edited draft fields while refreshing untouched fields.
- Older hosts lacking `State` remain readable, but state-aware Apply controls show a clear upgrade requirement.
- Expand snapshots and persistence to include RAM, cartridge, and uIEC fields. Keep save-on-exit preferences outside host Apply/Revert semantics.

### 5. Finish the machine selectors and visibility

- Build Computer and Model selectors from the authoritative host catalog: `x64sc` and `xvic`.
- Preserve existing selection APIs. Use stable model collections within each family; switch collections only when the family changes.
- Prefer `c64` and `vic20` defaults, otherwise the first available model. Compare IDs case-insensitively.
- Reject unavailable, out-of-family, and transient invalid selections. An empty or unusable catalog disables Apply with a visible explanation.
- Use one synchronization path for initialization, refresh, Apply responses, Revert, and persisted settings.
- Use canonical automation IDs `Settings.Computer`, `Settings.MachineVariant`, and `Settings.FlashCartBuilder`.
- Classify every Settings control as shared or VIC-20-specific. C64 hides VIC-20 RAM blocks and expansion controls while retaining shared controls.
- Preserve VIC-20 drafts across family switches without attaching VIC-20 hardware to C64.
- Cover all 14 C64 catalog profiles automatically.

### 6. Make the RemoteControl CLI portable and verifiable

- Replace the sibling-project dependency with the published RemoteControl Protocol package matching Server `0.7.4`.
- Use its generated gRPC client with owned channel lifetime, bearer metadata, cancellation, and bounded operations.
- Retain `--transport grpc` and its environment default. Reject unsupported transports explicitly.
- Add the CLI and its tests to the solution and completion gates. Verify a standalone checkout without sibling repositories.
- Add machine-readable capability/tree output for retained validation evidence.
- Prefer exact automation-ID matches; reject ambiguous matches.
- Calculate effective visibility through ancestors and reject malformed trees.
- Add real in-process gRPC contract tests for authentication, default-disabled behavior, action permissions, mutation failures, timeouts, invalid frames, and exit codes.

### 7. Enable the required AI and remote-service gates

**aiUnit dependency**

- Add reasoning effort end-to-end through strategy configuration, resolution, inline specifications, Codex invocation, and result metadata.
- Emit Codex’s `model_reasoning_effort` setting through its supported command arguments.
- Test strategy precedence and exact model/effort routing without paid calls.
- Release through aiUnit’s supported Nuke packaging and Azure pipeline using pool `Default`; consume the verified published version in ViceSharp.
- Route **both review theories to Astra/xhigh**. Remove forced Grok attributes and Grok-specific prompt instructions.
- Require deterministic routing tests and independent review-artifact validation. A green “NeverFails” xUnit result is insufficient.

**Existing remote services**

- RomM: `http://192.168.0.148:8080/`.
- CSDb bridge: `http://192.168.0.148:8090/`.
- Use the existing per-user bridge-token flow with a dedicated test identity. Keep administrative credentials on the service.
- Support an optional bridge API key through the fixture’s caller-owned HTTP client, including connection and search requests.
- Confirm authenticated access and at least one downloadable C64 fixture before running the suite.
- Require **seven integration passes, zero failures, zero skips**, including collection cleanup.
- Missing credentials, disabled token provisioning, or missing fixtures block completion; they do not justify substituting the stopped local stack.

### 8. Prove the final application behavior

Run two fresh validation passes against final binaries.

- Prove RemoteControl is disabled by default and fails closed; then prove the explicitly enabled configuration supports the required actions.
- Drive every applicable Settings control, including Apply, Revert, Apply + Restart, validation, speed cycling, individual RAM blocks, persistence preferences, and builder open/close.
- Verify effective visibility, fresh host state, and actual runtime effects. UI values alone are insufficient.
- Read emulator settings through the existing debug-attach endpoint belonging to the launched process. Refresh session identifiers and tree nodes after restarts.
- Boot representative `c64`, `c64c`, `ntsc`, VIC-20 PAL, and VIC-20 NTSC profiles. Retain READY screenshots, geometry, clock, pacing, and Warp evidence.
- Verify the pinned-ROM VIC-20 “all RAM” result is exactly **28,159 BASIC bytes free**.
- Test cartridge image/bank/preset/writeback persistence and real uIEC behavior.
- Stop only validation processes started by this work.

## Validation and completion

The final gate includes:

- Release solution build.
- Exhaustive managed, native, determinism, parity, CLI, and integration manifest coverage.
- Required workload and fixture execution with no silent prerequisite returns.
- Both Astra AI reviews, with valid completed artifacts, correct model/effort metadata, and durable review logs.
- Fresh live RemoteControl evidence.
- Requirement traceability with no unexplained gaps for changed acceptance criteria.
- `git diff --check`.

Retain exact commands, configuration, filters, counts, TRX, stdout/stderr, diagnostic logs, and relevant dumps. Failed, skipped, aborted, or incomplete required runs prevent completion.

Astra reviews each slice and the final combined diff and evidence. Fix blocking findings and rerun affected gates.

Finalize tracked requirements, the plan, handoff, and receipts **before committing**. Fetch origin, reconcile any advanced `main` without force, rerun affected validation, commit the explicit work manifest, and push. Verify local and remote commit IDs match, then append the commit receipt to MCP. Report preserved unrelated dirty files accurately.

## Defaults and remaining prerequisites

- The other open backlog items remain outside this work unless they block the agreed completion gates.
- No ViceSharp MSI deployment is included.
- Remote service health is verified; authentication and seed availability remain execution prerequisites.
- No source or configuration changes were made during this planning phase.
- Phone access remains pending desktop pairing through **Settings → Connections → Control this PC**, followed by scanning the QR code and selecting this chat in ChatGPT’s Remote view. [Official setup instructions](https://learn.chatgpt.com/docs/remote-connections)

## Approved amendments and execution ledger

- 2026-09-06 21:37:31 -05:00: operator directed "Forget about the leaked keys. Just get back to the real work." All credential-remediation work is removed from this completion plan, including the historical-token cleanup bullet from Slice 0 and the subsequently proposed MCP workspace-key rotation/restart. This supersedes those earlier cleanup instructions.
- Implementation remains GPT-5.6 Sol High; independent hostile validation remains GPT-6 Astra xhigh. Every delegated run loads add-profile and states its operating implications before work.
- The operator's plan lock remains in effect: if the approved design proves flawed, stop task work, present the concrete proposed amendment, and wait for explicit approval. Goal/timer controls must be used only within their supported contract; do not misreport an active goal as paused.
- No implementation gate is complete at this initial persistence. Existing baseline evidence is historical; final completion requires fresh receipts for all gates.

### Gate ledger

- [ ] Slice 0: completion contract, requirements, ownership and exhaustive test manifest.
- [ ] Slice 1: native audio/lifecycle red, green and hostile agreement.
- [ ] Slice 2: baseline regressions red, green and hostile agreement.
- [ ] Slice 3: Avalonia boundary red, green and hostile agreement.
- [ ] Slice 4: transactional Settings red, green and hostile agreement.
- [ ] Slice 5: machine selectors and visibility red, green and hostile agreement.
- [ ] Slice 6: portable RemoteControl CLI red, green and hostile agreement.
- [ ] Slice 7: published aiUnit reasoning-effort support, both Astra reviews and seven remote integration passes.
- [ ] Slice 8: two fresh live application passes and complete final validation.
- [ ] Final combined hostile agreement, explicit-manifest commit/push and matching local/origin SHA.

## Phase receipts

- 2026-09-06 22:21:06 -05:00: Slice 0 requirements/AC readiness is AGREE in [Astra requirements-phase review](../../validation-output/codex-grok-completion-20260906/astra-requirements-phase-review.md#final-requirements-phase-verdict-agree). Four review surfaces pass; zero blocking failures; four findings closed. One historical evidence limit remains: no pre-prepend HANDOFF byte hash exists. This does not waive a product gate.
- The reviewed package contains 32 scoped records, 134 pending acceptance criteria, 19 verified FR mappings and the corrected exhaustive test design. Astra's own completed review turn was independently read back from MCP.
- Sol High is authorized to execute the validator red-test phase, using only a compilation contract scaffold before genuine failing behavioral tests. Production validator/Nuke implementation follows the red-test hostile gate. Full Slice 0 and all subsequent ledger entries remain open.

