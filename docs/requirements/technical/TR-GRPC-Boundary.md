# TR-GRPC-Boundary: gRPC Boundary Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Architecture / Process Boundary |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-05-13                     |

---

## TR-GRPC-BOUNDARY-001: Versioned gRPC Boundary Between Emulator Host and UI Clients

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
10. **Generated Contract Safety:** Generated clients, explicit service registration, and serializer configuration must avoid runtime contract discovery on hot paths.
11. **Default Exposure:** The host listens on a local endpoint by default. Remote access requires explicit configuration.
12. **Local Renderer Boundary:** The in-process Avalonia host may bind a dedicated render surface directly to a local emulator/frame source. The binding is owned by the host/composition layer, not ViewModels, and is limited to frame presentation data.

### Acceptance Criteria

1. UI assemblies do not reference `ViceSharp.Core`, `ViceSharp.Chips`, or `ViceSharp.Architectures` directly.
2. Host services expose enough control, remote output, input, media, state, and diagnostic operations to satisfy FR-HOST-001 through FR-HOST-005 and FR-UI-001.
3. Contract compatibility checks detect deleted fields, reused field numbers, and incompatible service changes.
4. An integration test can start the host, connect a remote UI client, receive a frame stream, submit input, request a screenshot, and shut down cleanly.
5. Architecture tests enforce that only host/composition assemblies reference both concrete core assemblies and boundary service implementations.
6. Streaming tests verify that client disconnects, slow frame consumers, and reconnects do not mutate emulator state.
7. Release validation includes the host and at least one reference UI client.
8. An in-process Avalonia rendering test can bind the host-owned render surface to a local frame source without introducing `ViceSharp.Core` or runtime-internal references into ViewModels.

### Verification Method

- Architecture tests for assembly dependency rules.
- gRPC contract compatibility tests for `.proto` evolution.
- Host/UI integration smoke tests over a loopback gRPC channel for control and remote-output behavior.
- Local renderer boundary tests for the host-owned Avalonia render surface.
- Streaming backpressure and reconnect tests.
- Release publish validation for host and reference UI client.

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
- TR-PLAT-001 (The boundary must support desktop hosts across target platforms)

### Design Decisions

- The host process is the only owner of live emulator sessions.
- UI control clients use command, stream, and artifact contracts instead of direct object references.
- The latest committed video frame is replayable to reconnecting clients.
- The in-process Avalonia renderer may consume a local frame source directly, but only through a narrow host-owned rendering boundary.
- Input events are normalized before reaching emulator devices.
- Snapshots and screenshots cross the boundary as artifacts with metadata and checksums.
- Local-only binding is the default until remote access is explicitly configured.
