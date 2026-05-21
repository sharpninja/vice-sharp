# Technical Requirements (MCP Server)

## TR-ADHOC-YAML-001

**Ad-hoc machine YAML loader (YamlDotNet, AOT-opt-in)** — AdhocMachineYamlLoader shall parse YAML via YamlDotNet 17.x and emit field-path-tagged AdhocMachineValidationException on schema mismatches. The loader is marked RequiresDynamicCode/RequiresUnreferencedCode and is invoked only behind the Console --machine-yaml flag with a scoped UnconditionalSuppressMessage; the AOT-clean default path is untouched. AdhocMachineBlueprint.BuildMachine(IArchitectureBuilder) materialises chips (Mos6502, Mos6526, Mos6569, Sid6581) and RAM/ROM regions on the supplied bus and clock. AdhocMachine implements IMachine.Reset (clocks reset, devices reset) and GetState (default MachineState until a designated CPU role is added to the schema).

## TR-ALLOC-001

**Zero Managed Allocations Per Emulation Cycle on Hot Path** — ## TR-ALLOC-001: Zero Managed Allocations Per Emulation Cycle on Hot Path

**ID:** TR-ALLOC-001
**Title:** Zero Managed Allocations Per Emulation Cycle on Hot Path
**Priority:** P0 -- Critical
**Category:** Performance

### Description

The emulation hot path (the per-cycle execution of CPU, VIC-II, SID, and CIA ticks) shall produce zero managed heap allocations during steady-state operation. This eliminates GC pauses that would cause frame drops and audio glitches. All per-cycle data structures shall use value types (structs), stack allocation, or pre-allocated buffers.

### Rationale

A single C64 frame at PAL timing requires approximately 19,656 CPU cycles (312 lines x 63 cycles). At 50fps, that is approximately 985,000 ticks per second. Even a 24-byte allocation per tick would generate approximately 23MB/s of GC pressure, causing Gen0 collections every few milliseconds and introducing unpredictable latency.

### Technical Specification

1. **Struct-Based Value Types:** Core data structures (CpuState, VicState, SidVoiceState, CiaState) are `readonly struct` or `ref struct` types.
2. **PayloadArena Bump Allocator:** Variable-size data within a frame (sprite line buffers, audio sample batches) uses a `PayloadArena<T>` bump allocator that resets per frame.
3. **Span/Memory Usage:** All byte-buffer operations use `Span<byte>` or `Memory<byte>` backed by pre-allocated arrays or native memory.
4. **No Boxing:** Interface dispatch on value types uses generic constraints (`where T : struct, IFoo`) to avoid boxing. Critical paths use concrete types directly.
5. **No String Concatenation:** Logging on the hot path uses structured logging with pre-allocated message templates, not string interpolation.
6. **No LINQ on Hot Path:** LINQ extension methods allocate enumerator objects; hot-path iteration uses `for`/`foreach` over arrays or spans.
7. **No Closures on Hot Path:** Lambda captures that close over local variables allocate a display class; hot-path callbacks use static lambdas or direct method references.

### Acceptance Criteria

1. A benchmark running 10 million emulation cycles reports zero `GC.GetAllocatedBytesForCurrentThread()` delta on the hot-path thread.
2. `dotnet-counters` monitoring during a 60-second emulation session shows zero Gen0 collections attributable to the emulation thread.
3. The `[NoAlloc]` custom attribute (checked by a Roslyn analyzer) is applied to all hot-path methods, and the analyzer reports zero violations.
4. The PayloadArena reset occurs exactly once per frame and completes in under 100ns.
5. All event/message payloads in the pub/sub system are value types (per TR-PUBSUB-001).
6. No `new` keyword appears in hot-path code paths except for stack-allocated `Span<T>` or `stackalloc`.

### Verification Method

- Allocation-tracking benchmark in the performance test suite.
- Roslyn analyzer that flags heap allocations in methods annotated with `[NoAlloc]`.
- `dotnet-trace` GC event analysis during integration test runs.
- Code review checklist item for all hot-path PRs.

### Related TRs

- TR-AOT-001 (Struct types are inherently AoT-friendly)
- TR-PUBSUB-001 (Zero allocations in the messaging system)
- TR-SIMD-001 (SIMD operations on stack-allocated vectors)

### Design Decisions

- CPU state is a `readonly record struct` passed by `ref` or `in` to avoid copies.
- The frame buffer is a pre-allocated `byte[]` (or native memory) reused every frame.
- Audio sample buffers are pre-allocated ring buffers sized for the maximum samples-per-frame.
- Event payloads use a tagged union (`readonly struct EventPayload`) with an inline fixed buffer for small data.

## TR-AOT-001

**Full NativeAOT Compatibility with Zero Reflection on Hot Path** — ## TR-AOT-001: NativeAOT Compatible, No Reflection on Hot Path

**ID:** TR-AOT-001
**Title:** Full NativeAOT Compatibility with Zero Reflection on Hot Path
**Priority:** P0 -- Critical
**Category:** Deployment / Performance

### Description

All ViceSharp assemblies shall be fully compatible with .NET NativeAOT publication. This means no use of runtime reflection, `System.Reflection.Emit`, or dynamic code generation on the emulation hot path. All assemblies shall pass the NativeAOT trim analysis without warnings. Source generators shall be used where compile-time code generation is needed.

### Rationale

NativeAOT provides sub-50ms startup time, reduced memory footprint, and ahead-of-time optimization of hot paths. Reflection-based patterns are incompatible with NativeAOT's static compilation model and would cause runtime failures or require expensive fallback paths.

### Technical Specification

1. **Trim Analysis:** Every assembly in the solution shall pass `dotnet publish` with `<PublishTrimmed>true</PublishTrimmed>` and `<TrimmerSingleWarn>false</TrimmerSingleWarn>` producing zero trim warnings.
2. **No Reflection Hot Path:** The emulation loop (clock tick, CPU decode/execute, VIC-II render, SID sample, CIA tick) shall not invoke any `System.Reflection` APIs.
3. **Source Generators:** Opcode dispatch tables, device registration, and configuration binding shall use Roslyn source generators instead of reflection-based discovery.
4. **DynamicDependency Annotations:** Where trim-sensitive patterns exist in non-hot code paths (configuration, plugin loading), `[DynamicDependency]` and `[DynamicallyAccessedMembers]` annotations shall preserve required metadata.
5. **No System.Linq.Expressions:** Expression trees compile via reflection emit and are not AoT-safe. Use direct delegates or source-generated alternatives.

### Acceptance Criteria

1. `dotnet publish -c Release -r win-x64 --self-contained /p:PublishAot=true` succeeds for all published assemblies with zero trim analysis warnings.
2. `dotnet publish -c Release -r linux-x64 --self-contained /p:PublishAot=true` succeeds with zero warnings.
3. `dotnet publish -c Release -r osx-arm64 --self-contained /p:PublishAot=true` succeeds with zero warnings.
4. The emulation hot loop contains zero calls to `System.Reflection` namespace types (verified by IL analysis).
5. Application startup time is under 100ms on reference hardware (NativeAOT-published binary).
6. All opcode dispatch uses compile-time generated jump tables (source generator output).
7. DI container registration does not use assembly scanning; all registrations are explicit or source-generated.

### Verification Method

- CI pipeline includes a NativeAOT publish step that fails on any trim warning.
- IL scanning tool (custom Roslyn analyzer) detects `System.Reflection` usage on annotated hot-path methods.
- Startup time benchmark included in the performance test suite.

### Related TRs

- TR-ALLOC-001 (Zero allocations -- struct types are inherently AoT-friendly)
- TR-SIMD-001 (SIMD intrinsics are AoT-compatible)
- TR-MEDIA-001 (FFmpeg P/Invoke must be AoT-compatible)

### Design Decisions

- Opcode decoder is a source-generated `switch` expression over all 256 opcodes, not a delegate array populated by reflection.
- Configuration binding uses a source generator that emits strongly-typed binders.
- Plugin/extension loading (if needed) uses `AssemblyLoadContext` with explicit type loading, not assembly scanning.

## TR-BUILD-001

**Nuke Build System with Dual CI/CD Pipelines** — ## TR-BUILD-001: Nuke Build with Dual CI/CD (Azure DevOps + GitHub Actions)

**ID:** TR-BUILD-001
**Title:** Nuke Build System with Dual CI/CD Pipelines
**Priority:** P0 -- Critical
**Category:** Build / CI/CD

### Description

ViceSharp shall use the Nuke build system for all build, test, package, and publish operations. The build definition is a C# project (`_build`) that defines targets as code. Two CI/CD pipelines are maintained: Azure DevOps (primary, source of truth) and GitHub Actions (mirror, community-facing).

### Rationale

Nuke provides a strongly-typed, IDE-debuggable build system written in C#, matching the project language. Dual CI/CD ensures the primary Azure DevOps pipeline is the deployment authority while the GitHub mirror provides community visibility and PR validation.

### Technical Specification

1. **Nuke Build Project:**
   - The `_build` project is a .NET console application using the Nuke framework.
   - Build targets include: `Clean`, `Restore`, `Compile`, `Test`, `Pack`, `Publish`, `PublishAot`, `IntegrationTest`, `BenchmarkRun`.
   - Target dependencies form a DAG (directed acyclic graph) with correct ordering.
   - The build can be executed locally via `nuke` CLI or `dotnet run --project _build`.

2. **Azure DevOps Pipeline (Primary):**
   - YAML pipeline (`azure-pipelines.yml`) triggers on `main` and `release/*` branches.
   - PR validation runs `Compile` + `Test` targets.
   - CI builds run `Compile` + `Test` + `Pack` + `PublishAot` targets.
   - Release builds additionally publish NuGet packages to the Azure DevOps Artifacts feed.
   - Multi-platform matrix: Windows x64, Ubuntu x64, macOS ARM64.

3. **GitHub Actions Pipeline (Mirror):**
   - Workflow file (`.github/workflows/ci.yml`) triggers on push/PR to `main`.
   - Runs the same `Compile` + `Test` targets as Azure DevOps for validation.
   - Does not publish packages (Azure DevOps is the single source for artifact publication).
   - Multi-platform matrix matching Azure DevOps targets.

4. **Build Targets:**
   - `Clean`: Removes all `bin`/`obj`/`artifacts` directories.
   - `Restore`: Runs `dotnet restore` with the global packages folder.
   - `Compile`: Builds the solution in the specified configuration (Debug/Release).
   - `Test`: Runs all unit tests with coverage collection (minimum 80% line coverage).
   - `IntegrationTest`: Runs integration tests (Lorenz suite, timing tests) with a longer timeout.
   - `BenchmarkRun`: Executes BenchmarkDotNet benchmarks and archives results.
   - `Pack`: Creates NuGet packages for `ViceSharp.Abstractions` and `ViceSharp.Core`.
   - `Publish`: Publishes framework-dependent binaries.
   - `PublishAot`: Publishes NativeAOT binaries for all target RIDs (per TR-AOT-001).

5. **Versioning:**
   - Version is derived from a `.version` file and Git tags (GitVersion or Nerdbank.GitVersioning).
   - Pre-release versions use the branch name and build counter as suffix.
   - Release versions are tagged on `main` (e.g., `v1.0.0`).

### Acceptance Criteria

1. `nuke Compile` succeeds locally on Windows, Linux, and macOS.
2. `nuke Test` runs all unit tests and reports results in a structured format (TRX or JUnit XML).
3. `nuke PublishAot` produces native binaries for at least `win-x64`, `linux-x64`, and `osx-arm64`.
4. The Azure DevOps pipeline completes a full CI build (Compile + Test + Pack + PublishAot) in under 15 minutes.
5. The GitHub Actions pipeline completes PR validation (Compile + Test) in under 10 minutes.
6. NuGet packages are versioned correctly with pre-release suffixes for non-release branches.
7. Test coverage reports are generated and uploaded as pipeline artifacts.
8. Build failures produce clear, actionable error messages with the failing target and step identified.

### Verification Method

- Local build execution on developer machines (all three OS).
- Pipeline execution logs reviewed for completeness and timing.
- Version string inspection on built packages.
- Coverage report review for minimum threshold compliance.

### Related TRs

- TR-AOT-001 (PublishAot target validates AoT compatibility)
- TR-PLAT-001 (Multi-platform build matrix)

### Design Decisions

- Nuke is preferred over MSBuild-only or FAKE because it provides C# build logic that is debuggable in the IDE.
- Azure DevOps is the primary CI/CD because it hosts the primary repository and artifact feed.
- GitHub Actions mirrors the build for community contributors who submit PRs to the GitHub mirror.
- The `_build` project uses the same .NET SDK version as the main solution (specified in `global.json`).

## TR-CPU-EDGE-001

**6510 Interrupt Latency Edge Cases** — Observable behavior: 6510 interrupt dispatch models BRK delaying NMI by one opcode, taken branches delaying IRQ/NMI by one cycle, and SEI-specific IRQ delay-counter handling.
Affected profile: C64/x64sc 6510 CPU core.
Sources: native/vice/vice/src/mainc64cpu.c:185-193,663-708; native/vice/vice/src/maincpu.c:454-501.
Acceptance: IRQ/NMI tests around BRK, taken branches, CLI/SEI, and pending interrupt lines match x64sc instruction and cycle traces.
Related: FR-CPU-002, FR-CPU-003, TEST-CPU-001.

## TR-CPU-EDGE-002

**6510 BA-Low Dummy Access and Bus Stall Semantics** — Observable behavior: CPU memory access helpers distinguish real and dummy reads/writes and apply BA-low checks to reads, stack accesses, and zero-page accesses so DMA bus steals stall the CPU with x64sc side effects.
Affected profile: C64/x64sc 6510 CPU and VIC-II shared bus.
Sources: native/vice/vice/src/mainc64cpu.c:270-288,320-330,362-371,395-448.
Acceptance: CPU/VIC bus arbitration tests prove dummy accesses, stack accesses, zero-page reads, and DMA stalls match x64sc traces.
Related: FR-CPU-002, FR-VIC-006, FR-VIC-010, TEST-CPU-001, TEST-VIC-001.

## TR-CYCLE-001

**Sub-Cycle Bus-Phase Accuracy Matching VICE x64sc Behavior** — ## TR-CYCLE-001: Sub-Cycle Bus-Phase Accuracy

**ID:** TR-CYCLE-001
**Title:** Sub-Cycle Bus-Phase Accuracy Matching VICE x64sc Behavior
**Priority:** P0 -- Critical
**Category:** Accuracy

### Description

The emulation core shall operate at sub-cycle granularity, modeling the two phases of the system clock (PHI1 and PHI2) and the bus ownership arbitration between the CPU and VIC-II. This level of accuracy is required to correctly emulate cycle-exact tricks used by C64 demos and games. The reference implementation for correctness is VICE x64sc (the cycle-exact VICE variant).

### Rationale

Many C64 programs rely on exact cycle timing for visual effects (raster bars, FLI, sprite multiplexing, open borders) and audio effects (digi playback). A cycle-accurate emulator that does not model bus phases correctly will fail these programs. VICE x64sc is the accepted gold standard for cycle-accurate C64 emulation.

### Technical Specification

1. **Clock Model:** The system clock is divided into PHI1 (VIC-II access phase) and PHI2 (CPU access phase). Each phase is one half-cycle.
2. **Bus Arbitration:** During PHI1, the VIC-II has bus access (for c-access, g-access, p-access, s-access). During PHI2, the CPU has bus access (unless the VIC-II has asserted BA to steal cycles).
3. **BA/AEC Signals:** The VIC-II asserts BA (Bus Available = low) 3 cycles before it needs to steal PHI2 cycles, giving the CPU time to complete its current access. AEC (Address Enable Control) gates the CPU's address bus.
4. **CPU Pipeline:** The CPU's internal pipeline (fetch, decode, execute sub-phases) is modeled per-cycle, not per-instruction.
5. **Tick Granularity:** The `IClockedDevice.Tick()` method is invoked once per half-cycle (PHI1 or PHI2), not once per full cycle.

### Acceptance Criteria

1. All devices (CPU, VIC-II, CIA, SID) are ticked at half-cycle (bus phase) granularity.
2. The VIC-II's DMA steal pattern (badlines, sprites) matches VICE x64sc cycle-by-cycle.
3. CIA timer countdown occurs at the correct bus phase (PHI2 falling edge).
4. CPU memory accesses occur only during PHI2 when AEC is asserted.
5. The Lorenz test suite passes at 100% (same pass rate as VICE x64sc).
6. The VICE cycle-exact test programs (e.g., those in the VICE test suite repository) produce identical output.
7. Raster effects that depend on sub-cycle timing (FLI, AGSP, sprite stretching) work correctly.

### Verification Method

- Automated test suite comparing ViceSharp output to VICE x64sc reference captures.
- Lorenz test suite full execution with pass/fail comparison.
- Visual comparison of known demo effects (Crest, Booze Design, Oxyron productions).

### Related FRs

- FR-CPU-002 (Cycle-accurate execution timing)
- FR-VIC-006 (Badline handling and DMA stealing)
- FR-VIC-010 (Sprite multiplexing DMA timing)

### Design Decisions

- Half-cycle ticking doubles the tick rate compared to full-cycle emulators but is essential for bus-phase accuracy.
- The clock driver is a single-threaded loop that alternates PHI1/PHI2 ticks to all devices in deterministic order.
- The tick order within a phase is: VIC-II, then CIA1, then CIA2, then SID, then CPU (matching the hardware's analog timing).

## TR-DET-001

**Bit-Exact Reproducibility Given Same Initial State and Inputs** — ## TR-DET-001: Bit-Exact Reproducibility

**ID:** TR-DET-001
**Title:** Bit-Exact Reproducibility Given Same Initial State and Inputs
**Priority:** P0 -- Critical
**Category:** Correctness

### Description

Given the same initial machine state (snapshot) and the same sequence of input events (keyboard, joystick, timing), the emulator shall produce bit-exact identical output (video frames, audio samples, final machine state) across all runs, all platforms, and all compilation modes (Debug, Release, NativeAOT).

### Rationale

Determinism is a foundational requirement for: (1) snapshot-based replay (FR-SNP-003), (2) automated testing against reference outputs, (3) TAS (tool-assisted speedrun) support, (4) networked multiplayer synchronization (future), and (5) bug reproduction.

### Technical Specification

1. **No Floating-Point in Emulation Core:** The emulation core (CPU, VIC-II, CIA, SID oscillator/envelope) shall use only integer arithmetic. Floating-point is permitted only in the audio output resampling stage (post-SID, not part of the emulated state).
2. **No Uninitialized State:** All state variables are explicitly initialized. RAM initialization follows the known C64 power-on pattern (alternating $00/$FF blocks).
3. **No Random Number Generation:** The emulation core does not use `System.Random` or any PRNG. All "random" behavior comes from the emulated hardware state (e.g., SID noise LFSR, uninitialized RAM patterns).
4. **Deterministic Tick Order:** Devices are ticked in a fixed, documented order within each clock phase. The order does not change based on runtime conditions.
5. **No Thread-Dependent State:** The emulation core runs on a single thread. There are no data races, lock-dependent ordering, or thread-pool scheduling dependencies.
6. **Platform-Independent Arithmetic:** All integer operations produce identical results on x86, x64, and ARM64. No reliance on platform-specific overflow behavior beyond what C# guarantees.

### Acceptance Criteria

1. A "replay determinism" test loads a snapshot, replays 10 million cycles of recorded input, and compares the final state hash: the hash must be identical across 100 consecutive runs.
2. The same test produces identical hashes on Windows x64, Linux x64, macOS ARM64, and Linux ARM64.
3. The same test produces identical hashes for Debug, Release, and NativeAOT builds.
4. Video frame checksums (CRC32 of raw pixel data) match for every frame between two runs of the same replay.
5. Audio sample checksums match for every audio buffer between two runs of the same replay.
6. The `[Deterministic]` custom attribute is applied to all emulation-core methods, and a Roslyn analyzer verifies no non-deterministic operations are used.

### Verification Method

- Cross-platform determinism test in CI (Windows, Linux, macOS runners).
- Replay regression test suite that compares frame/audio checksums against golden reference files.
- Static analysis (Roslyn analyzer) for floating-point usage and non-deterministic patterns in annotated code.

### Related FRs

- FR-SNP-003 (Deterministic replay depends on this TR)
- FR-SNP-001 / FR-SNP-002 (Snapshot save/load must be complete for determinism)

### Related TRs

- TR-CYCLE-001 (Cycle accuracy ensures the tick order matches hardware)
- TR-SIMD-001 (SIMD and scalar paths must produce identical results)
- TR-STATE-001 (ACID state transactions ensure consistent snapshots)

### Design Decisions

- SID filter computation uses fixed-point arithmetic (Q16.16) in the emulation core; float conversion happens only at the audio output boundary.
- The frame buffer stores raw pixel indices (palette indices), not RGB values, until the output stage.
- RAM initialization pattern is configurable but defaults to the documented C64 power-on pattern for deterministic cold starts.

## TR-DOC-DASHBOARD-001

**Completion Dashboard markdown structure in root README** — The Completion Dashboard section in README.md uses one table per group (Iteration 0, Iteration 1, Iterations 2-5, Tooling/Ecosystem). Columns: Feature, State (✅/🟢/🟡/⚪), %, Source. Source column links to the live MCP TODO id and to in-repo artifacts where present. The dashboard cites a refresh date and a snapshot link to /mcpserver/todo?done=false.

## TR-DRV-EDGE-001

**VDrive Directory BAM and REL Flush Quirks** — Observable behavior: virtual drive behavior includes DOS quirks for directory traversal, native partition block counts, zero inputs, status-channel continuity, and listen/unlisten REL flush events.
Affected profile: 1541/1571/1581-style virtual drives and host filesystem drive emulation.
Sources: native/vice/vice/src/vdrive/vdrive-dir.c:279-302,393-396,515,749; native/vice/vice/src/serial/fsdrive.c:67,228-267.
Acceptance: directory listing, BAM/block-count, zero-pattern matching, status-channel, and REL flush tests reproduce VICE virtual-drive behavior.
Related: FR-DRV-001, FR-DRV-003, FR-DRV-005, TEST-DRV-001.

## TR-GRPC-BOUNDARY-001

**Versioned gRPC Boundary Between Emulator Host and UI Clients** — ## TR-GRPC-BOUNDARY-001: Versioned gRPC Boundary Between Emulator Host and UI Clients

**ID:** TR-GRPC-BOUNDARY-001
**Supersedes Draft Label:** TR-GRPC-001
**Title:** Versioned gRPC Boundary Between Emulator Host and UI Clients
**Priority:** P0 -- Critical
**Category:** Architecture

### Description

ViceSharp shall separate emulator execution from host UI control through a versioned gRPC boundary. The host process owns the emulator core, devices, media state, persistence, and local render-source composition. Host UI control, media, session, input, snapshot, capture, and diagnostic operations consume generated gRPC clients or narrow gRPC-backed client abstractions and must not instantiate or mutate core emulator objects directly.

The in-process Avalonia renderer is the local rendering exception: a host-owned render surface may bind directly to a local emulator/frame source so frame presentation does not have to loop through gRPC. This exception is narrow and does not permit Avalonia ViewModels to reference runtime internals or concrete emulator devices. External or remote UIs still use the gRPC video service and stream APIs when direct in-process rendering is unavailable.

### Rationale

The boundary keeps UI control shells thin, testable, replaceable, and safe to reconnect while preserving the library-first emulator core. It also allows headless hosting, future remote clients, process isolation for crashes, and a single observable contract for lifecycle, input, media, snapshot, capture, and diagnostic operations. Local frame presentation remains a host-owned rendering concern when the UI is in-process with the emulator host.

### ID Note

`TR-GRPC-BOUNDARY-001` is the canonical requirement id. It supersedes the earlier draft label `TR-GRPC-001`; requirements-tool records must use the canonical id because the MCP requirements tool requires TR ids in `TR-AREA-SUBAREA-###` form.

### Technical Specification

1. **Ownership Boundary:** `ViceSharp.Hosting` owns emulator session composition, local render-source composition, and references concrete core assemblies. UI control assemblies reference host client abstractions and generated contracts only.
2. **Contract Source of Truth:** `.proto` files define the wire contract for control, remote output, input, media, state, diagnostics, and event services.
3. **Versioning:** Contract packages include a semantic version and support additive evolution. Breaking changes require a new package or service version.
4. **Command Shape:** Mutating commands are unary request/response calls with session id, command id, sequence, correlation id, and structured result.
5. **Streaming Shape:** Remote video, audio, lifecycle events, media events, and diagnostics use server streaming with explicit metadata and reconnect behavior.
6. **Input Shape:** Input events use normalized envelopes that preserve ordering and can carry frame/cycle stamps for deterministic replay.
7. **Artifact Shape:** Snapshots, screenshots, and media payloads use bounded byte payloads or host-owned artifact handles with checksums.
8. **Backpressure:** Remote video may drop stale committed frames; audio preserves order or reports explicit drops; neither policy can mutate emulation state.
9. **Error Model:** All services return structured error codes, messages, and recoverability hints without leaking implementation exceptions across the boundary.
10. **AoT Compatibility:** Generated clients, explicit service registration, and serializer configuration must be compatible with .NET 10 NativeAOT and trimming.
11. **Default Exposure:** The host listens on a local endpoint by default. Remote access requires explicit configuration.
12. **Local Renderer Boundary:** The in-process Avalonia host may bind a dedicated render surface directly to a local emulator/frame source. The binding is owned by the host/composition layer, not ViewModels, and is limited to frame presentation data.

### Acceptance Criteria

1. UI assemblies do not reference `ViceSharp.Core`, `ViceSharp.Chips`, or `ViceSharp.Architectures` directly.
2. Host services expose enough control, remote output, input, media, state, and diagnostic operations to satisfy FR-HOST-001 through FR-HOST-005 and FR-UI-001.
3. Contract compatibility checks detect deleted fields, reused field numbers, and incompatible service changes.
4. An integration test can start the host, connect a remote UI client, receive a frame stream, submit input, request a screenshot, and shut down cleanly.
5. Architecture tests enforce that only host/composition assemblies reference both concrete core assemblies and boundary service implementations.
6. Streaming tests verify that client disconnects, slow frame consumers, and reconnects do not mutate emulator state.
7. NativeAOT publish validation includes the host and at least one reference UI client.
8. An in-process Avalonia rendering test can bind the host-owned render surface to a local frame source without introducing `ViceSharp.Core` or runtime-internal references into ViewModels.

### Verification Method

- Architecture tests for assembly dependency rules.
- gRPC contract compatibility tests for `.proto` evolution.
- Host/UI integration smoke tests over a loopback gRPC channel for control and remote-output behavior.
- Local renderer boundary tests for the host-owned Avalonia render surface.
- Streaming backpressure and reconnect tests.
- NativeAOT publish validation for host and reference UI client.

### Related FRs

- FR-HOST-001 through FR-HOST-005
- FR-UI-001
- FR-INP-001 / FR-INP-002
- FR-DRV-001 / FR-TAP-002 / FR-CRT-001
- FR-SNP-001 / FR-SNP-002
- FR-MED-001

### Related TRs

- TR-LIB-001 (Library-first design keeps core embeddable inside the host)
- TR-MVVM-001 (UI ViewModels remain isolated from concrete core types)
- TR-AOT-001 (Generated contracts and host services must be AoT compatible)
- TR-PLAT-001 (The boundary must support desktop hosts across target platforms)

### Design Decisions

- The host process is the only owner of live emulator sessions.
- UI control clients use command, stream, and artifact contracts instead of direct object references.
- The latest committed video frame is replayable to reconnecting clients.
- The in-process Avalonia renderer may consume a local frame source directly, but only through a narrow host-owned rendering boundary.
- Input events are normalized before reaching emulator devices.
- Snapshots and screenshots cross the boundary as artifacts with metadata and checksums.
- Local-only binding is the default until remote access is explicitly configured.

## TR-HOST-STATUS-001

**Measured Emulator Runtime Telemetry** — ## TR-HOST-STATUS-001: Measured Emulator Runtime Telemetry

**ID:** TR-HOST-STATUS-001
**Title:** Measured Emulator Runtime Telemetry
**Priority:** P0 -- Critical
**Category:** Observability

### Description

ViceSharp host status shall distinguish requested throttle settings from measured emulation output and measured emulated clock speed.

### Technical Specification

1. The host computes effective clock speed as rolling emulated cycles per real second.
2. Effective clock percent is effective clock speed divided by the active machine profile nominal clock.
3. Requested limiter rate is reported separately from measured FPS and effective clock speed.
4. Cycle, frame, PC, power state, and run state are sampled from host-owned session state.
5. Status polling must not mutate emulator state.

### Acceptance Criteria

1. Status responses include nominal clock Hz, effective clock Hz, effective clock percent, limiter rate percent, measured FPS, frame count, cycle, PC, power state, and run state.
2. Tests can verify limiter target remains stable while effective clock and FPS vary with execution.
3. Paused sessions report stable cycle/frame counters and paused run state.

### Verification Method

- Host status unit tests.
- gRPC status contract tests.
- UI status bar ViewModel tests with fake host status clients.

### Architecture Sources

- `docs/Architecture.md`: host/UI boundary and clock/timing sections.
- `src/ViceSharp.Host`: host-owned session/status services.
- `src/ViceSharp.Protocol`: generated status contract types.

### Related FRs

- FR-HOST-006
- FR-UI-002
- FR-CFG-008

## TR-IEC-EDGE-001

**IEC ATN EOI ACK NACK and Bit Timeout Timing** — Observable behavior: IEC serial devices model ATN edge handling, listener/talker role changes, EOI signaling, frame acknowledge, NACK recovery, and microsecond-scale clock/data timing windows.
Affected profile: C64 IEC bus and 1541-style device interactions.
Sources: native/vice/vice/src/serial/serial-iec-device.c:279-291,388-430,487-534,555-622,633-721.
Acceptance: IEC protocol tests match x64sc device-line transitions and timing for ATN, EOI, byte acknowledge, NACK, and role reversal.
Related: FR-DRV-005, FR-DRV-006, TEST-DRV-001.

## TR-INPUT-VKM-001

**VICE VKM Parser and Selected Map Resolver** — ## TR-INPUT-VKM-001: VICE VKM Parser and Selected Map Resolver

**ID:** TR-INPUT-VKM-001
**Title:** VICE VKM Parser and Selected Map Resolver
**Priority:** P0 -- Critical
**Category:** Input

### Description

ViceSharp machine keyboard input shall resolve normalized host key events through a selected machine-specific VICE keymap before updating keyboard matrix state.

### Technical Specification

1. Keyboard map parsing is host-owned and session-scoped.
2. C64 support parses VICE VKM comments, `!CLEAR`, `!INCLUDE`, `!UNDEF`, modifier directives, row/column entries, and shift flags.
3. Custom uploaded keymaps are validated before becoming active.
4. The machine keyboard translator is abstracted so other machine profiles can provide different matrix/key handling.
5. Real-time key state changes update CIA keyboard matrix lines through machine input abstractions, not UI runtime references.

### Acceptance Criteria

1. Built-in and custom VKM maps produce deterministic key-to-matrix mappings.
2. Invalid maps return diagnostics without replacing the selected map.
3. Host input service tests prove selected map entries affect C64 keyboard matrix state.
4. The parser and resolver are usable without Avalonia dependencies.

### Verification Method

- VKM parser unit tests with VICE C64 maps.
- Input integration tests against C64 keyboard matrix/CIA behavior.
- gRPC input service tests for selected-map behavior.

### Architecture Sources

- `docs/Architecture.md`: host/UI boundary, input boundary, and library-first assembly rules.
- `src/ViceSharp.Abstractions`: keyboard map and machine keyboard abstractions.
- `src/ViceSharp.Chips/Input`: C64 VKM parser and keyboard matrix implementation.
- `src/ViceSharp.Host`: host input service.

### Related FRs

- FR-INP-001
- FR-INP-006
- FR-HOST-004

## TR-LIB-001

**Emulator Core as a Reusable Library with UI Shells as Thin Consumers** — ## TR-LIB-001: Emulator as Library, UI Shells as Thin Consumers

**ID:** TR-LIB-001
**Title:** Emulator Core as a Reusable Library with UI Shells as Thin Consumers
**Priority:** P0 -- Critical
**Category:** Architecture

### Description

The ViceSharp emulator core shall be packaged as a standalone .NET library (NuGet package) with no dependencies on any UI framework, windowing system, or platform-specific API. UI applications (desktop, web, headless test harness, embedded) consume the library and provide their own rendering, audio output, and input handling through well-defined abstractions.

### Rationale

A library-first design enables: (1) multiple UI frontends (Avalonia, MAUI, Blazor, headless), (2) embedding in other applications (game development tools, music production, testing harnesses), (3) clean separation of concerns, (4) independent versioning of core vs. UI, and (5) automated testing without any UI dependency.

### Technical Specification

1. **Core Library Assembly:** `ViceSharp.Core` contains the complete emulation engine with zero UI dependencies.
2. **Abstractions Assembly:** `ViceSharp.Abstractions` defines all interfaces and value types shared between core and consumers. The core depends on Abstractions; consumers depend on Abstractions.
3. **No UI Framework References:** The core library shall not reference `System.Windows`, `Avalonia`, `Microsoft.Maui`, `System.Drawing`, or any UI framework assembly.
4. **No Threading Assumptions:** The core library does not create threads. The host application controls the emulation thread and timing.
5. **Frame-Based API:**
   - `IEmulator.RunFrame()` advances the emulation by one video frame and returns the frame data (video buffer, audio buffer, state events).
   - `IEmulator.RunCycles(int count)` advances by a specific number of cycles (for debugging).
   - `IEmulator.Step()` advances by one CPU instruction.
6. **Output Buffers:**
   - Video: The core writes to a pre-allocated pixel buffer (`Span<byte>` or `Memory<byte>`) provided by the host.
   - Audio: The core writes to a pre-allocated sample buffer provided by the host.
   - The core does not perform any rendering, blitting, or audio playback.
7. **Input Injection:**
   - The host injects input events via `IKeyboardMatrix`, `IJoystickPort`, `IMousePort`.
   - Events are queued and consumed at the correct emulation time.
8. **Host Boundary:**
   - Out-of-process UI shells communicate with `ViceSharp.Hosting` through TR-GRPC-BOUNDARY-001.
   - The core remains embeddable and does not know whether its host is local, headless, or serving a UI client.

### Acceptance Criteria

1. `ViceSharp.Core` compiles with zero references to any UI framework (verified by dependency analysis).
2. `ViceSharp.Core` can be consumed from a headless console application that runs the emulation and writes frame checksums to stdout.
3. `ViceSharp.Core` can be consumed from a unit test project that validates emulation behavior without any display or audio.
4. The `IEmulator` API is sufficient to build a complete UI (demonstrated by at least one working UI shell).
5. The core library NuGet package size is under 5MB (excluding ROMs and test data).
6. The core library has zero transitive dependencies beyond the .NET BCL and `ViceSharp.Abstractions`.
7. A gRPC-hosted UI scenario can run without adding UI or transport dependencies to `ViceSharp.Core`.

### Verification Method

- Dependency analysis tool (e.g., `dotnet list package --include-transitive`) verifying no UI framework dependencies.
- Headless test harness in the CI pipeline that exercises the full emulation API without a UI.
- NuGet package size check in the build pipeline.

### Related TRs

- TR-PLAT-001 (Platform-agnostic core enables cross-platform UI shells)
- TR-AOT-001 (NativeAOT publishing of the library)
- TR-MVVM-001 (UI shells use MVVM pattern on top of the library)
- TR-GRPC-BOUNDARY-001 (Host/UI process boundary preserves core library ownership)

### Design Decisions

- The core library does not implement a "run loop" -- the host application calls `RunFrame()` at the appropriate cadence (vsync, timer, or free-running).
- The core library exposes synchronous APIs only; the host is responsible for threading and async patterns.
- ROM images are not bundled with the library; the host provides ROM data via `IEmulator.LoadRom()`.
- `ViceSharp.Hosting` may expose the core through gRPC, but that transport remains outside the core library.

## TR-MEDIA-001

**FFmpeg Integration via P/Invoke with NativeAOT Compatibility** — ## TR-MEDIA-001: FFmpeg P/Invoke, AoT Compatible, Multiple Format Support

**ID:** TR-MEDIA-001
**Title:** FFmpeg Integration via P/Invoke with NativeAOT Compatibility
**Priority:** P1 -- Important
**Category:** Integration

### Description

Video and audio encoding shall use FFmpeg libraries (libavcodec, libavformat, libavutil, libswscale, libswresample) accessed via P/Invoke. The FFmpeg bindings shall be NativeAOT-compatible (no reflection, no dynamic assembly loading). Multiple output formats shall be supported through FFmpeg's codec infrastructure.

### Rationale

FFmpeg is the industry standard for media encoding/decoding, supports all required formats (H.264, FLAC, WAV, MP4, AVI), and is available on all target platforms. P/Invoke is the most AoT-friendly interop mechanism in .NET.

### Technical Specification

1. **P/Invoke Bindings:**
   - All FFmpeg function calls use `[DllImport]` or `[LibraryImport]` (source-generated, preferred for AoT) attributes.
   - Bindings are auto-generated from FFmpeg C headers using a code generator tool.
   - Pointer-heavy FFmpeg APIs are wrapped in safe C# abstractions that manage lifetime.

2. **NativeAOT Compatibility:**
   - All P/Invoke declarations use `[LibraryImport]` (compile-time marshaling) instead of `[DllImport]` where possible.
   - No `Marshal.GetDelegateForFunctionPointer()` usage (incompatible with AoT).
   - Callback functions passed to FFmpeg use `[UnmanagedCallersOnly]` static methods.

3. **Library Loading:**
   - FFmpeg shared libraries are loaded via `NativeLibrary.SetDllImportResolver()` to support platform-specific paths.
   - Missing FFmpeg libraries result in graceful degradation (media capture features report as unavailable, per FR-MED-005).
   - Supported FFmpeg versions: 6.x and 7.x (API compatibility layer handles minor version differences).

4. **Supported Formats:**
   - Video codecs: H.264 (libx264), H.265/HEVC (libx265), VP9 (libvpx-vp9), MJPEG.
   - Audio codecs: PCM (WAV), FLAC, MP3 (libmp3lame), AAC.
   - Container formats: MP4, AVI, MKV, WAV, FLAC.
   - Image formats (single-frame): PNG, BMP, JPEG (via libavcodec).

5. **Memory Management:**
   - FFmpeg's internal allocations are managed by FFmpeg's own allocator.
   - Frame data passed from the emulator to FFmpeg uses pinned buffers or native memory to avoid GC relocation.
   - All FFmpeg resources (AVFormatContext, AVCodecContext, AVFrame, AVPacket) are wrapped in `IDisposable` wrappers with deterministic cleanup.

### Acceptance Criteria

1. `[LibraryImport]` source-generated P/Invoke compiles and runs under NativeAOT on all target platforms.
2. Video recording to H.264/MP4 produces valid output (playable by VLC and browser HTML5 video).
3. Audio recording to WAV and FLAC produces valid output (playable by standard audio players).
4. Synchronized A/V recording produces an MP4 with correct A/V sync (per FR-MED-004).
5. When FFmpeg libraries are not present, `IMediaCapture.GetSupportedFormats()` returns an empty set and capture methods throw `MediaNotAvailableException`.
6. No FFmpeg-related memory leaks: a 10-minute recording session followed by disposal shows no growth in native memory.
7. The FFmpeg bindings pass trim analysis with zero warnings.

### Verification Method

- NativeAOT publish and execution test with FFmpeg recording.
- Media file validation tests using FFprobe to verify codec, container, and sync.
- Memory leak test using native memory profiling during extended recording.
- Graceful degradation test with FFmpeg libraries removed from the search path.

### Related FRs

- FR-MED-001 through FR-MED-005 (all media capture features)

### Related TRs

- TR-AOT-001 (P/Invoke bindings must be AoT-compatible)
- TR-ALLOC-001 (Frame data transfer uses pinned/native buffers, not managed arrays)
- TR-PLAT-001 (FFmpeg libraries are platform-specific binaries)

### Design Decisions

- `[LibraryImport]` (source-generated) is strongly preferred over `[DllImport]` (runtime-generated) for AoT compatibility.
- FFmpeg bindings are isolated in a separate assembly (`ViceSharp.Media.FFmpeg`) so the core library has no FFmpeg dependency.
- The media encoding pipeline runs on a dedicated thread (producer-consumer pattern with the emulation thread), not on the emulation hot path.
- Frame data is copied to a native-memory staging buffer before handoff to FFmpeg, decoupling the emulation frame buffer from the encoder.

## TR-MEDIA-EDGE-001

**P64 GCR Pulse Stream Weak-Bit and Syncmark Handling** — Observable behavior: P64/GCR conversion treats weak regions as special pulse values and preserves syncmark-border alignment considerations when converting between pulse streams and GCR bytes.
Affected profile: disk image and low-level GCR media behavior for C64 drives.
Sources: native/vice/vice/src/lib/p64/p64.c:663-673,686-724,724-763.
Acceptance: P64/GCR tests preserve weak-bit regions and syncmark-aligned GCR byte reconstruction with VICE-compatible pulse semantics.
Related: FR-DRV-004, TEST-DRV-001.

## TR-MVVM-001

**Strict MVVM Separation -- ViewModels Reference Abstractions Only, Views Contain Zero Logic** — ## TR-MVVM-001: ViewModels Reference Only Abstractions, Views Contain Zero Logic

**ID:** TR-MVVM-001
**Title:** Strict MVVM Separation -- ViewModels Reference Abstractions Only, Views Contain Zero Logic
**Priority:** P1 -- Important
**Category:** Architecture

### Description

All UI shells shall follow a strict Model-View-ViewModel (MVVM) pattern. ViewModels shall depend only on `ViceSharp.Abstractions` (never on `ViceSharp.Core` directly or any UI framework). Views shall contain zero business logic -- they bind to ViewModel properties and commands exclusively. This ensures that ViewModels are testable without a UI framework and that the UI layer is trivially replaceable.

### Rationale

Strict MVVM enables: (1) unit testing of all UI logic without instantiating UI controls, (2) swapping UI frameworks (Avalonia to MAUI) by replacing only the View layer, (3) headless operation using ViewModels directly, and (4) clean dependency graph enforcement.

### Technical Specification

1. **ViewModel Layer:**
   - ViewModels are in a separate assembly (e.g., `ViceSharp.ViewModels`) that references only `ViceSharp.Abstractions` and standard .NET libraries.
   - ViewModels expose observable properties (implementing `INotifyPropertyChanged`) and commands (implementing `ICommand` or equivalent).
   - ViewModels receive emulator services via constructor injection of abstraction interfaces.
   - ViewModels do not reference any UI framework types (`Avalonia.Controls`, `Microsoft.Maui.Controls`, etc.).

2. **View Layer:**
   - Views are XAML/markup files with code-behind that contains only: DataContext assignment, platform-specific initialization (e.g., render surface setup), and direct event-to-command wiring where data binding is insufficient.
   - Views do not contain conditional logic, string formatting, arithmetic, or state management.
   - All value conversions use `IValueConverter` implementations, not code-behind logic.

3. **Model Layer:**
   - The "Model" is the `ViceSharp.Core` emulation engine, accessed exclusively through `ViceSharp.Abstractions` interfaces.
   - ViewModels never instantiate core types directly; they receive them from the DI container.

4. **Dependency Rules (strict, enforced by architecture test):**
   - `ViceSharp.Abstractions` -> (no dependencies)
   - `ViceSharp.Core` -> `ViceSharp.Abstractions`
   - `ViceSharp.ViewModels` -> `ViceSharp.Abstractions`
   - `ViceSharp.UI.Avalonia` (View) -> `ViceSharp.ViewModels`, `ViceSharp.Abstractions`, Avalonia
   - `ViceSharp.UI.Avalonia` does NOT reference `ViceSharp.Core` (only the host/composition root does)

5. **Remote Host Client:**
   - ViewModels consume abstraction-level host client facades for TR-GRPC-BOUNDARY-001 scenarios.
   - Generated gRPC clients and transport concerns stay in infrastructure adapters or the composition root.
   - ViewModels expose UI state and commands without mutating emulator core objects directly.
   - In-process Avalonia frame presentation may use a host-owned direct render surface, but ViewModels do not reference that local emulator/frame source or any runtime internals.

### Acceptance Criteria

1. `ViceSharp.ViewModels` compiles with zero references to any UI framework (verified by dependency analysis).
2. `ViceSharp.ViewModels` compiles with zero references to `ViceSharp.Core` (only `ViceSharp.Abstractions`).
3. All ViewModel public properties and commands are exercised by unit tests that do not require a UI framework.
4. View code-behind files contain fewer than 20 lines of code each (excluding auto-generated code).
5. An architecture test (using a tool like NetArchTest or ArchUnitNET) enforces the dependency rules and fails the build on violations.
6. Swapping the UI framework (e.g., replacing Avalonia views with MAUI views) requires changes only in the View assembly.
7. A remote host UI can be tested with mocked host client facades and without starting `ViceSharp.Core`.
8. The local Avalonia render surface can be tested as a host/composition concern without adding core, chip, or architecture references to ViewModels.

### Verification Method

- Architecture test in the CI pipeline that validates assembly dependency rules.
- Line-count check on View code-behind files.
- ViewModel unit test coverage report (target: >90% of ViewModel methods covered).
- Dependency analysis output in the build log.

### Related TRs

- TR-LIB-001 (Library-first design provides the Model layer)
- TR-PLAT-001 (MVVM enables per-platform View implementations)
- TR-GRPC-BOUNDARY-001 (UI control clients consume the host through generated contracts and adapters; local rendering remains host-owned)

### Design Decisions

- The composition root (application entry point) is the only place where `ViceSharp.Core` and `ViceSharp.ViewModels` meet; it registers concrete implementations against abstraction interfaces in the DI container.
- ReactiveUI or CommunityToolkit.Mvvm is used for `INotifyPropertyChanged` and `ICommand` infrastructure.
- The ViewModel for the main emulator display exposes display state and commands through abstractions only. The in-process Avalonia render surface may bind directly to a local frame source, but that binding is owned by the host/composition layer rather than the ViewModel.
- gRPC generated clients are adapted behind ViewModel-facing interfaces so transport code does not leak into ViewModels.

## TR-PERF-HARNESS-001

**BenchmarkDotNet harness wiring under tests/ViceSharp.Benchmarks** — tests/ViceSharp.Benchmarks targets net10.0 with OutputType=Exe and BenchmarkDotNet 0.15.x, ProjectReferences ViceSharp.Architectures, ViceSharp.Chips, and ViceSharp.Core. The project is registered in ViceSharp.slnx under the /tests/ folder with IsTestProject=false so dotnet test does not invoke testhost. tests/ViceSharp.TestHarness/BenchmarksSmokeTests.cs runs one iteration of each benchmark workload through xUnit. dotnet run -c Release --project tests/ViceSharp.Benchmarks enumerates and runs all benchmarks.

## TR-PLAT-001

**Cross-Platform Support for Windows, Linux, macOS on x64 and ARM64** — ## TR-PLAT-001: Windows/Linux/macOS, x64/ARM64, .NET 10

**ID:** TR-PLAT-001
**Title:** Cross-Platform Support for Windows, Linux, macOS on x64 and ARM64
**Priority:** P0 -- Critical
**Category:** Portability

### Description

ViceSharp shall run on Windows, Linux, and macOS on both x64 and ARM64 architectures. The emulation core library shall be platform-agnostic. Platform-specific code (audio output, input handling, window management) shall be isolated behind abstractions with per-platform implementations.

### Rationale

The Commodore 64 community spans all major desktop platforms. A library-first design (TR-LIB-001) naturally supports multiple platforms, but platform-specific I/O (audio, video display, input) requires explicit abstraction.

### Technical Specification

1. **Target Framework:** .NET 10 (net10.0). No platform-specific TFMs in the core library.
2. **Runtime Identifiers:** The following RIDs are first-class targets:
   - `win-x64`, `win-arm64`
   - `linux-x64`, `linux-arm64`
   - `osx-x64`, `osx-arm64`
3. **Platform Abstraction Layer:**
   - `IAudioOutput`: Platform-specific audio output (WASAPI on Windows, PulseAudio/ALSA on Linux, CoreAudio on macOS).
   - `IVideoOutput`: Platform-specific window/surface management.
   - `IInputSource`: Platform-specific input device enumeration and event handling.
4. **Native Library Loading:**
   - FFmpeg shared libraries are loaded via `NativeLibrary.TryLoad()` with platform-specific paths.
   - SIMD capability detection uses `System.Runtime.Intrinsics` which is platform-aware.
5. **File System:**
   - All file paths use `Path.Combine()` and forward slashes internally.
   - No hardcoded path separators or drive letters.
6. **Endianness:**
   - The emulation core is little-endian (matching x86/ARM in LE mode); all platforms use little-endian.

### Acceptance Criteria

1. The emulation core library (`ViceSharp.Core`) compiles and runs on all 6 target RIDs without conditional compilation (`#if`).
2. The CI/CD pipeline builds and tests on Windows x64, Ubuntu x64, and macOS ARM64.
3. NativeAOT publish succeeds on all 6 target RIDs (per TR-AOT-001).
4. Audio output plays correctly on all three OS platforms (verified by manual testing and automated A/V sync tests).
5. Input devices (keyboard, gamepad) are recognized on all platforms.
6. The Lorenz test suite passes on all platforms (same pass rate).
7. No `PlatformNotSupportedException` is thrown during normal operation on any supported platform.

### Verification Method

- Multi-platform CI matrix (GitHub Actions for Windows/Linux/macOS).
- Cross-platform integration tests running a subset of the Lorenz test suite.
- NativeAOT publish smoke test on each target RID.

### Related TRs

- TR-AOT-001 (NativeAOT publish targets all platforms)
- TR-SIMD-001 (SIMD support differs between x64 SSE/AVX and ARM64 NEON)
- TR-LIB-001 (Core library is platform-agnostic)
- TR-MEDIA-001 (FFmpeg native libraries are platform-specific)

### Design Decisions

- The core library targets `net10.0` without any `-windows`, `-linux`, or `-macos` suffix.
- Platform-specific audio/video/input implementations are in separate assemblies (e.g., `ViceSharp.Platform.Windows`, `ViceSharp.Platform.Linux`, `ViceSharp.Platform.MacOS`).
- The platform assembly is selected at application startup via a factory, not compile-time conditionals.
- ARM64 Windows (Surface Pro X, Snapdragon laptops) is a supported target from the start.

## TR-PLAT-WIRES-001

**Cross-platform wireframes in docs/wireframes/** — docs/wireframes/ contains one markdown file per target (desktop-windows, desktop-macos, mobile-portrait, mobile-landscape, xbox) plus an index README. Each file uses ASCII layout sketches plus prose for screens, navigation flow, and per-input affordances (mouse/touch/gamepad). The Windows wireframe captures the live src/ViceSharp.Avalonia/MainWindow surface as the reference state.

## TR-PUBSUB-001

**<50ns Publish, <100ns Deliver, 0 Allocations Per Frame** — ## TR-PUBSUB-001: High-Performance Zero-Allocation Pub/Sub

**ID:** TR-PUBSUB-001
**Title:** <50ns Publish, <100ns Deliver, 0 Allocations Per Frame
**Priority:** P0 -- Critical
**Category:** Performance

### Description

The internal event/message bus used for inter-device communication (e.g., VIC-II asserting IRQ on the CPU, CIA timer underflow signaling the SID) shall achieve sub-100ns latency with zero managed heap allocations. This pub/sub system is the backbone of the emulation's device interconnect and fires hundreds of thousands of times per second.

### Rationale

At approximately 985,000 CPU cycles per second (PAL), even a 1-microsecond overhead per event would consume the entire frame budget. The pub/sub system must be faster than a virtual method call and must not produce GC pressure.

### Technical Specification

1. **Message Types:**
   - All message payloads are `readonly record struct` types with a maximum inline size of 64 bytes.
   - Messages use a discriminated union pattern (`MessageKind` enum + fixed-size payload) to avoid boxing.

2. **Publish Path:**
   - Publishing a message writes the payload to a pre-allocated slot in a lock-free ring buffer.
   - The publisher does not allocate, box, or copy to the managed heap.
   - Target latency: <50ns per publish operation.

3. **Delivery Path:**
   - Subscribers are registered as direct delegate references (not interface dispatch).
   - Delivery iterates a pre-allocated subscriber array (not a `List<T>` or `Dictionary<K,V>`).
   - Target latency: <100ns per deliver operation (including subscriber callback invocation).

4. **Per-Frame Budget:**
   - Total pub/sub overhead shall not exceed 5% of the per-frame time budget (PAL frame = 20ms, budget = 1ms).
   - Estimated message count per frame: approximately 2,000 (IRQ/NMI signals, DMA events, register writes).

5. **Zero Allocations:**
   - No allocations during publish, deliver, subscribe, or unsubscribe operations during steady-state emulation.
   - Subscriber registration may allocate during setup (before emulation starts) but not during runtime.

### Acceptance Criteria

1. Microbenchmark: publish 1 million messages in a tight loop; median per-publish latency is <50ns (measured via `Stopwatch` or hardware counters).
2. Microbenchmark: deliver 1 million messages to 3 subscribers; median per-deliver latency is <100ns.
3. Frame benchmark: a full PAL frame emulation (19,656 cycles) reports zero allocations from pub/sub (measured via `GC.GetAllocatedBytesForCurrentThread()`).
4. The pub/sub ring buffer has a fixed capacity (configurable, default 8,192 slots) and does not resize during emulation.
5. Subscriber registration and unregistration are O(1) operations using slot-based indexing.
6. Message ordering is preserved per-publisher (FIFO within a single source).

### Verification Method

- BenchmarkDotNet microbenchmarks with `[MemoryDiagnoser]` and `[HardwareCounters]`.
- Integration benchmark running 1,000 frames with allocation tracking.
- Stress test with maximum message volume (simulating worst-case sprite + badline + IRQ activity).

### Related TRs

- TR-ALLOC-001 (Zero allocation constraint applies to pub/sub)
- TR-CYCLE-001 (Sub-cycle events require fast pub/sub)
- TR-STATE-001 (State mutations may publish change events)
- TR-DET-001 (Message delivery order must be deterministic)

### Design Decisions

- The pub/sub system is not a general-purpose event bus; it is a specialized emulation interconnect.
- Message delivery is synchronous (inline) -- there is no async dispatch or thread marshaling within the emulation loop.
- Subscribers are sorted by priority at registration time; delivery order is fixed during emulation.
- The ring buffer uses `Unsafe.As<TFrom, TTo>()` for zero-copy payload reinterpretation between message types.

## TR-QA-XMLDOCS-001

**XMLDOCS convention test with ratchet baseline** — tests/ViceSharp.TestHarness/XmlDocsConventionTests.cs scans test source files via regex for [Fact]|[Theory]|[ViceFact]|[ViceTheory] method declarations and asserts each preceding doc comment contains FR-, TR-, "Use case:", and "Acceptance:" tokens. Violations are sorted; the test fails if the count exceeds ExpectedMaxViolations (current baseline 192). VICESHARP_XMLDOCS_ENFORCE=1 forces zero-tolerance. The constant must only decrease as the corpus is retrofitted.

## TR-RAM-EDGE-001

**VICE RAM Initialization Pattern Semantics** — Observable behavior: RAM initialization supports VICE-compatible start value, value inversion, pattern inversion, random span/repeat, and random chance behavior with machine-specific factory defaults.
Affected profile: C64/x64sc startup RAM plus other machine profiles when enabled.
Sources: native/vice/vice/src/ram.c:49-60,137-178,197-221,233-339.
Acceptance: given the same RAM-init resources and random seed, startup RAM bytes match VICE pattern behavior.
Related: FR-CFG-007, TEST-CFG-001, TEST-MEM-001.

## TR-SID-EDGE-001

**SID ADSR Delay and Envelope Pipeline Semantics** — Observable behavior: SID envelope generation models ADSR delay bug behavior, envelope decrement pipeline delays, exponential-counter transitions, hold-zero behavior, and rate-counter state transitions visible through audio output and ENV3 reads.
Affected profile: C64 SID 6581/8580 profiles.
Sources: native/vice/vice/src/resid/envelope.cc:42-55,94-122,230-247; native/vice/vice/src/resid/envelope.h:120-179,386-412.
Acceptance: ADSR delay, envelope freeze/hold-zero, ENV3 sampling, and envelope transition tests match VICE/reSID behavior.
Related: FR-SID-001, FR-SID-002, TEST-SID-001.

## TR-SID-EDGE-002

**SID Waveform Test-Bit Noise and Floating DAC Behavior** — Observable behavior: SID waveform generation models test-bit shift-register reset timing, combined waveform writeback, noise shift-register transitions, and floating DAC TTL differences between MOS6581 and MOS8580.
Affected profile: C64 SID 6581/8580 profiles.
Sources: native/vice/vice/src/resid/wave.cc:28-46,172-197,224-250,254-290.
Acceptance: waveform tests cover test-bit transitions, combined waveforms, noise shift state, and 6581/8580 floating-output fade behavior against VICE/reSID reference output.
Related: FR-SID-001, FR-SID-003, TEST-SID-001.

## TR-SID-EDGE-003

**MOS8580 One-Cycle Write Pipeline and Digi Boost** — Observable behavior: MOS8580 SID writes are delayed by one cycle in fast sampling mode, and the external input path can simulate the 8580 digi-boost hardware modification.
Affected profile: C64C/MOS8580 SID profiles.
Sources: native/vice/vice/src/resid/sid.cc:154,202-215,749-752.
Acceptance: 8580 mode tests prove write side effects occur with the VICE/reSID pipeline delay and digi-boost input affects generated samples when configured.
Related: FR-SID-001, FR-SID-004, TEST-SID-001.

## TR-SIMD-001

**SIMD-Accelerated Rendering and Audio with Generic Specialization for CPU Core** — ## TR-SIMD-001: SIMD-Accelerated Rendering and Audio

**ID:** TR-SIMD-001
**Title:** SIMD-Accelerated Rendering and Audio with Generic Specialization for CPU Core
**Priority:** P1 -- Important
**Category:** Performance

### Description

Performance-critical rendering and audio processing paths shall use SIMD (Single Instruction, Multiple Data) intrinsics to process multiple pixels or audio samples in parallel. The CPU core shall use generic specialization (constrained generics with `where T : struct`) to allow the JIT/AoT compiler to generate specialized machine code for different CPU variants (6502, 6510, 8502) without virtual dispatch overhead.

### Rationale

SIMD can process 4-16 pixels or audio samples per instruction, providing 4-16x throughput improvement for bulk data operations. Generic specialization eliminates virtual dispatch on the hottest code path (instruction decode and execute) while maintaining a single source implementation.

### Technical Specification

1. **SIMD Rendering Pipeline:**
   - VIC-II pixel output uses `Vector128<byte>` or `Vector256<byte>` to write 16-32 pixels per SIMD instruction.
   - Sprite rendering uses SIMD for collision detection (bitwise AND of sprite masks).
   - Color palette lookup uses SIMD gather operations where available.

2. **SIMD Audio Pipeline:**
   - SID audio mixing (combining 3 voices + filter output) uses `Vector128<float>` or `Vector256<float>`.
   - Audio resampling (SID clock rate to output sample rate) uses SIMD FMA instructions.
   - Volume envelope application uses SIMD multiply.

3. **Generic Specialization for CPU:**
   - The CPU core is implemented as `CpuCore<TBus>` where `TBus : struct, IBus`.
   - Different bus implementations (C64Bus, C128Bus, Vic20Bus) provide specialized memory access without virtual dispatch.
   - The JIT/AoT compiler generates separate native code for each `TBus` instantiation.

4. **Platform Detection:**
   - SIMD support is detected at startup via `System.Runtime.Intrinsics.X86.Sse2.IsSupported`, `Avx2.IsSupported`, `System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported`.
   - Fallback scalar implementations exist for all SIMD-accelerated paths.
   - The fastest available SIMD width is selected automatically.

### Acceptance Criteria

1. VIC-II line rendering with SIMD is at least 4x faster than the scalar fallback (measured by benchmark).
2. SID audio sample generation with SIMD is at least 2x faster than scalar.
3. Generic specialization eliminates all virtual/interface dispatch from the CPU decode-execute loop (verified by JIT disassembly inspection).
4. Scalar fallback paths produce bit-identical output to SIMD paths (verified by determinism tests).
5. All SIMD code compiles and runs correctly on: x64 (SSE2 minimum, AVX2 optional), ARM64 (NEON/AdvSIMD).
6. NativeAOT compilation produces specialized code for each generic instantiation (no shared generic fallback).

### Verification Method

- Benchmark suite comparing SIMD vs. scalar for rendering and audio pipelines.
- JIT disassembly capture (via BenchmarkDotNet `[DisassemblyDiagnoser]`) verifying absence of virtual dispatch in CPU loop.
- Cross-platform CI running on x64 and ARM64 targets.
- Determinism tests comparing SIMD and scalar output byte-by-byte.

### Related TRs

- TR-ALLOC-001 (SIMD operates on stack-allocated vectors, no heap allocation)
- TR-AOT-001 (SIMD intrinsics are AoT-compatible; generic specialization requires AoT to monomorphize)
- TR-DET-001 (Scalar and SIMD must produce identical results for determinism)

### Design Decisions

- `Vector128<T>` is the baseline SIMD width (available on all modern CPUs including ARM64 NEON).
- `Vector256<T>` is used opportunistically when AVX2 is available for bulk operations.
- The CPU core does not use SIMD internally (single-instruction-at-a-time emulation); SIMD is applied at the rendering and audio output stages.
- Generic specialization is preferred over code duplication for CPU variants.

## TR-STATE-001

**Mutation Queue with ACID State Transactions and Configurable State Window** — ## TR-STATE-001: Mutation Queue, ACID State Transactions, Configurable State Window

**ID:** TR-STATE-001
**Title:** Mutation Queue with ACID State Transactions and Configurable State Window
**Priority:** P0 -- Critical
**Category:** Reliability / Architecture

### Description

All mutations to emulated machine state shall be performed through a mutation queue that provides ACID-like transaction semantics. State changes within a single emulation step (one clock cycle or one instruction) are grouped into a transaction that is either fully applied or fully rolled back. A configurable state window maintains a ring buffer of recent states to support rewind, debugging, and snapshot operations.

### Rationale

ACID transactions ensure that a snapshot taken at any point captures a consistent state (not a half-updated state where the CPU has executed but the VIC-II has not yet ticked). The mutation queue also enables: state diffing for snapshot comparison, undo/rewind for debugging, and event sourcing for replay.

### Technical Specification

1. **Mutation Queue:**
   - All state-modifying operations enqueue a `StateMutation` record (target device, field offset, old value, new value).
   - Mutations are value types (structs) to avoid allocation (per TR-ALLOC-001).
   - The queue is drained and applied atomically at the end of each tick.

2. **ACID Properties:**
   - **Atomicity:** All mutations within a transaction (one tick) are applied together or not at all.
   - **Consistency:** After each transaction, all device states are internally consistent and cross-device invariants hold.
   - **Isolation:** The UI thread (reading state for display) sees only committed state, never in-progress mutations.
   - **Durability:** Committed state can be persisted to a snapshot at any transaction boundary.

3. **State Window:**
   - A configurable ring buffer holds the last N committed states (default: 60 frames worth, approximately 1.2 seconds at PAL rate).
   - Each entry in the ring buffer is a delta (diff from the previous state) to minimize memory usage.
   - Full state can be reconstructed by applying deltas forward from the nearest keyframe.
   - Keyframes (full state snapshots) are inserted at configurable intervals (default: every 30 frames).

4. **Transaction Boundary:**
   - For instruction-level accuracy: one transaction per instruction.
   - For cycle-level accuracy: one transaction per half-cycle (per TR-CYCLE-001).
   - The granularity is configurable via `IStateManager.TransactionGranularity`.

### Acceptance Criteria

1. A snapshot taken at any transaction boundary produces a state from which emulation can resume and produce deterministic output (per TR-DET-001).
2. Rolling back a transaction restores the exact previous state (verified by state hash comparison).
3. The state window supports rewind of at least 60 frames (1.2 seconds) without performance degradation.
4. Reconstructing full state from deltas produces a byte-identical result to the original full state.
5. The mutation queue processes at least 1 million mutations per second without exceeding the per-frame time budget.
6. UI reads of emulation state never observe a partially-committed transaction.
7. The state window memory footprint is bounded: delta entries average under 256 bytes per frame; keyframe entries are under 128KB.

### Verification Method

- Snapshot round-trip tests: save at arbitrary points, load, compare state hashes.
- Rewind tests: advance N frames, rewind N frames, advance again, verify identical state.
- Concurrency tests: UI thread reads state while emulation thread commits transactions, verify no torn reads.
- Memory benchmarks for state window under sustained operation.

### Related FRs

- FR-SNP-001 / FR-SNP-002 (Snapshot save/load relies on consistent state)
- FR-SNP-003 (Replay relies on deterministic state transitions)
- FR-SNP-004 (Snapshot diffing uses the mutation log)

### Related TRs

- TR-ALLOC-001 (Mutations are value types, no allocation)
- TR-DET-001 (Transaction ordering ensures determinism)
- TR-PUBSUB-001 (State change events flow through the pub/sub system)

### Design Decisions

- The mutation queue is a pre-allocated ring buffer of `StateMutation` structs, not a `List<T>`.
- Delta compression uses XOR encoding: the delta is the XOR of the old and new state bytes, yielding zero bytes for unchanged data which compress well.
- The UI accesses state through a "published snapshot" -- a copy-on-write reference that is updated atomically at frame boundaries.

## TR-TAP-EDGE-001

**TAP Version Long-Pulse and TurboTape Threshold Behavior** — Observable behavior: TAP parsing honors VICE pulse thresholds, version-specific long-pulse encoding, version 2 halfwave behavior, machine/video clock selection, and TurboTape pilot/header detection thresholds.
Affected profile: C64 TAP loading plus other TAP-tagged machines when enabled.
Sources: native/vice/vice/src/tape/tap.c:52-82,91-105,121-192,389-422,970-992,1251-1385.
Acceptance: TAP fixtures cover version 0/1/2 pulse encoding, long pulses, clock selection, CBM pilot detection, and TurboTape pilot/header detection against VICE behavior.
Related: FR-TAP-002, FR-TAP-003, FR-TAP-005, TEST-TAP-001.

## TR-TAPE-EDGE-001

**Tapeport Sense Motor and RTC Dongle Line Semantics** — Observable behavior: tapeport devices can assert sense independently of a datasette; the sense dongle asserts sense when enabled and after power-up, and CP Clock F83 combines motor-state and RTC data-line state to drive tape sense.
Affected profile: C64 tapeport peripherals and datasette sense-line behavior.
Sources: native/vice/vice/src/tapeport/sense-dongle.c:50-58,77,93; native/vice/vice/src/tapeport/cp-clockf83.c:161-174,178-190.
Acceptance: tapeport peripheral tests prove sense-line state, motor-line inversion, and RTC SDA/SCL effects match VICE for supported tapeport devices.
Related: FR-TAP-001, FR-TAP-004, TEST-TAP-001.

## TR-UI-SHELL-001

**Avalonia Emulator Control Shell** — ## TR-UI-SHELL-001: Avalonia Emulator Control Shell

**ID:** TR-UI-SHELL-001
**Title:** Avalonia Emulator Control Shell
**Priority:** P1 -- Important
**Category:** UI Architecture

### Description

The Avalonia shell shall present emulator controls through ViewModels and host-client abstractions while preserving the host-owned boundary for emulator state and local rendering.

### Technical Specification

1. ViewModels depend on abstractions or host client facades, not concrete emulator runtime devices.
2. The shell provides status bar, collapsible sidebar, Peripherals/Settings/Monitor tabs, and monitor dock/pop-out composition.
3. Local rendering may use a host-owned direct frame source only from the composition/render-surface layer.
4. Keyboard focus returns to the emulator display after normal controls unless monitor/text entry explicitly takes focus.
5. UI tests can fake host clients without starting the emulator runtime.

### Acceptance Criteria

1. Boundary tests fail if Avalonia ViewModels reference `ViceSharp.Core`, `ViceSharp.Chips`, or concrete architecture/device types.
2. ViewModel tests cover sidebar collapse, tab switching, attach state, settings state, VKM selection, status bar state, and monitor pop-out state.
3. UI startup succeeds while disconnected or while the in-process host is starting.

### Verification Method

- Avalonia ViewModel tests using fake host clients.
- Assembly/reference boundary tests.
- Local startup smoke test.

### Architecture Sources

- `docs/Architecture.md`: MVVM and host/UI boundary sections.
- `docs/requirements/technical/TR-MVVM.md`: strict ViewModel separation.
- `docs/requirements/technical/TR-GRPC-Boundary.md`: UI control boundary and local renderer exception.
- `src/ViceSharp.Avalonia`: shell, ViewModels, and render surface.

### Related FRs

- FR-UI-001
- FR-UI-002
- FR-UI-003
- FR-UI-004

## TR-VDC-EDGE-001

**VDC Register Display-Width and Busy-Status Edge Cases** — Observable behavior: VDC behavior includes invalid displayed widths, semigraphics width overrides, busy status timing during memory reads/writes, v0 horizontal-scroll/border quirks, register readback masks, and invalid register reads returning 0xff.
Affected profile: C128 VDC profiles only; non-Phase-1 by default.
Sources: native/vice/vice/src/vdc/vdc-draw.c:177-252; native/vice/vice/src/vdc/vdc-mem.c:53-62,335-340,388-407,545-568,572-588.
Acceptance: C128/VDC tests cover displayed-width invalid values, semigraphics overrides, register readback masks, busy timing, invalid register reads, and v0 border/xscroll quirks against VICE behavior.
Related: FR-PRF-003, TEST-PRF-001.

## TR-VIC-EDGE-001

**Invalid VIC-II Mode Priority and Collision Semantics** — Observable behavior: invalid ECM/BMM/MCM selector combinations render with no visible graphics color while preserving hidden foreground priority bits for sprite priority and sprite-background collision.
Affected profile: C64/C64C/SX-64 x64sc VIC-II.
Sources: native/vice/vice/src/viciisc/vicii-draw-cycle.c:41,133-141,196-224,401-428.
Acceptance: invalid-mode visible output, priority, and collision tests match x64sc.
Related: FR-VIC-002, FR-VIC-003, FR-VIC-005, TEST-VIC-001.

## TR-VIC-EDGE-002

**VIC-II Border Flip-Flop Cycle Checks** — Observable behavior: vertical and horizontal border state is controlled by model-specific cycle-table checks; CSEL/RSEL changes can skip side-border checks and closed borders mask sprite pixels.
Affected profile: C64/C64C/SX-64 x64sc VIC-II PAL/NTSC.
Sources: native/vice/vice/src/viciisc/vicii-cycle.c:168-202,408,480-482; native/vice/vice/src/viciisc/vicii-chip-model.c:92-95,145-147,223-225,306-308,384-386.
Acceptance: border flip-flop cycles, open-border cases, and closed-border sprite masking match x64sc.
Related: FR-VIC-007, FR-VIC-004, FR-VIC-005, TEST-VIC-001.

## TR-VIC-EDGE-003

**VIC-II Badline Idle-State and RC Windows** — Observable behavior: badline activation depends on display-enable timing, raster low bits, first/last DMA line state, idle-state transitions, and RC update windows.
Affected profile: C64/C64C/SX-64 x64sc VIC-II.
Sources: native/vice/vice/src/viciisc/vicii-cycle.c:56-61,527-565,576-598; native/vice/vice/src/vicii/vicii-fetch.c:135-166.
Acceptance: badline fetches, idle-state changes, and CPU DMA steals match x64sc traces.
Related: FR-VIC-001, FR-VIC-006, FR-VIC-008, TEST-VIC-001.

## TR-VIC-EDGE-004

**VIC-II Sprite DMA Latch and Per-Model Fetch Tables** — Observable behavior: sprite DMA is latched by enable/Y-match checks before fetch slots, and fetch/BA behavior is driven by per-model tables; clears after the latch do not cancel active DMA.
Affected profile: C64/C64C/SX-64 x64sc VIC-II PAL/NTSC.
Sources: native/vice/vice/src/viciisc/vicii-cycle.c:118-128,503,626; native/vice/vice/src/viciisc/vicii-chip-model.c:735-746; native/vice/vice/src/viciisc/vicii-fetch.c:275-309.
Acceptance: sprite latch, BA stall, p/s access, and line rollover tests match x64sc.
Related: FR-VIC-004, FR-VIC-006, FR-VIC-010, TEST-VIC-001.

## TR-VIC-EDGE-005

**VIC-II Matrix Fetch Idle and 0xff Fill Behavior** — Observable behavior: VIC-II matrix and graphics fetches use observable idle/fill values including 0xff matrix fill, RAM-derived color nibbles, and special RAM/character-ROM address latching.
Affected profile: C64/C64C/SX-64 x64sc VIC-II.
Sources: native/vice/vice/src/viciisc/vicii-fetch.c:192-199,208-227,234-264; native/vice/vice/src/vicii/vicii-fetch.c:72-109.
Acceptance: idle graphics, matrix fill, background source, and RAM/ROM transition fetches match x64sc.
Related: FR-VIC-001, FR-VIC-006, FR-VIC-008, FR-VIC-009, TEST-VIC-001.

## TR-VIC-EDGE-006

**VIC-II Register Readback and Collision Latch Semantics** — Observable behavior: VIC-II register reads expose unused one bits per register, IRQ status has fixed high bits, collision-register writes are ignored, and collision latches clear on read side effects.
Affected profile: C64/C64C/SX-64 x64sc VIC-II.
Sources: native/vice/vice/src/viciisc/vicii-mem.c:48-63,229,265-267,517,522-554,570-713.
Acceptance: D000-D03F readback, IRQ clear, and collision read-clear tests match x64sc.
Related: FR-VIC-001, FR-VIC-005, TEST-VIC-001.

## TR-VIC-EDGE-007

**VIC-II VSP Bug Memory-Corruption Behavior** — Observable behavior: x64sc simulates VSP memory corruption using line/channel probability tables when a qualifying badline follows a vulnerable idle state.
Affected profile: C64 x64sc VIC-II; deferred unless VSP/AGSP compatibility enters Phase 1.
Sources: native/vice/vice/src/viciisc/vicii-cycle.c:259-261,314-346,524,536-540.
Acceptance: controlled-seed VSP corruption tests match x64sc-compatible VSP programs when enabled.
Related: FR-VIC-008, TEST-VIC-001.
