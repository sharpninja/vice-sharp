# Iteration 0 — Foundations

## Objective

Create the runnable scaffolding for ViceSharp: solution structure, 33+ public interfaces, source generator, ROM fetch tool, CI/CD pipelines, and comprehensive documentation. Zero actual emulation code.

## Deliverables

### Phase A: Documentation and Knowledge

1. **Repository Infrastructure** — .NET 10 solution, Nuke build system, central package management, AoT/trim analyzers
2. **MCP Server Setup** — workspace registration, TODO tracking, session logging
3. **In-Repo Documentation** — architecture docs, public API reference, ROL (122+ entries), iteration specs
4. **FR/TR Requirements** — functional and technical requirements with traceability
5. **GraphRAG Ingestion** — VICE manual, hardware datasheets, file format specs into knowledge base

### Phase B: Code Implementation (TDD)

Each phase follows the Byrd Development Process:
1. RED — Write failing tests defining the contracts
2. Mermaid diagrams — Update canonical class diagrams
3. GREEN — Implement until tests pass
4. Update TODOs — Mark tasks complete

#### Batch 5-6: Abstractions
- 33+ interfaces in `ViceSharp.Abstractions`
- Core: `IBus`, `IClockedDevice`, `IInterruptLine`, `IAddressSpace`
- System: `ISystem`, `IMachine`, `IDevice`, `IPeripheral`
- Architecture: `IArchitecture`, `IArchitectureDescriptor`, `IArchitectureBuilder`, `IArchitectureValidator`
- Services: `IRomProvider`, `IAudioBackend`, `IFrameSink`, `IInputSource`
- Media: `IScreenshotCapture`, `IVideoRecorder`, `IAudioRecorder`, `IMediaCaptureSession`
- Monitor: `IMonitor`, `IMonitorTransport`, `IMonitorEventStream`
- State: `ISnapshot`, `IMutationQueue`, `IPubSub`, `IMessagePool`
- Value types: `Address`, `ClockCycle`, `DeviceId`, `MessageHandle`

#### Batch 7-8: Source Generator
- Roslyn source generator targeting `[ViceSharpDevice]` attribute
- Generates: device registration, bus wiring, clock subscription
- Tests validate generated output matches expected patterns

#### Batch 9-10: Core
- Bus implementation, clock, interrupt lines
- Mutation queue, pub/sub, message pool, payload arena
- State snapshots, determinism support

#### Batch 11-12: Architectures
- `IArchitecture` implementations for C64 (primary), VIC-20, C128, PET, Plus/4
- `GenericMachine` wiring devices through architecture descriptors
- Architecture validation (detect missing/conflicting devices)

#### Batch 13: Chips and Peripherals (stubs)
- CPU, VIC-II, SID, CIA, VIA, PLA — stub implementations
- Each implements the correct interface, returns default values
- Validates the full device pipeline without actual emulation

#### Batch 14: Monitor, Hosting, Controls, Media Capture
- Monitor stub with command parsing
- Generic host integration
- Avalonia control stubs
- Media capture interfaces with stub implementations

#### Batch 15: Apps, ROM Fetch, Determinism, CI/CD
- Console app (NativeAOT target)
- Avalonia desktop app shell
- ROM fetch tool
- Determinism test (empty machine snapshot comparison)
- Azure DevOps and GitHub Actions pipelines

## Acceptance Criteria

- [ ] `dotnet build ViceSharp.slnx` succeeds with zero warnings
- [ ] `dotnet test ViceSharp.slnx` — all tests pass
- [ ] NativeAOT console app publishes and runs (starts, prints version, exits)
- [ ] Avalonia app launches showing an empty display surface
- [ ] Determinism test: two independent empty-machine snapshots are byte-identical
- [ ] All 33+ interfaces are mockable (NSubstitute test validates each)
- [ ] Source generator produces correct output for a test device
- [ ] ROM fetch tool downloads and validates at least one ROM set
- [ ] CI pipeline runs end-to-end on both Azure DevOps and GitHub Actions
- [ ] All documentation files are substantive (not placeholders)
- [ ] ROL contains 122+ entries in full format
