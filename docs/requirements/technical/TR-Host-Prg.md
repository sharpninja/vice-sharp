# TR-HOST-PRG-001: Host-owned PRG memory load

**ID:** TR-HOST-PRG-001
**Title:** Host-owned PRG memory load over the gRPC emulator host contract
**Priority:** P1 -- Important
**Category:** Host / UI Boundary

### Description

PRG drop loading belongs to the emulator host, not Avalonia ViewModels. `EmulatorHost.LoadProgram` is the versioned RPC. The host reads the PRG (file path and/or payload), writes the payload through the session machine bus, decides BASIC-start from live TXTTAB (`$2B/$2C`), updates BASIC pointers only on that match, and queues `HostKeyboardAutomation` BASIC RUN (KERNAL keyboard buffer) when RUN is required. UI shells call `IHostProtocolClient.LoadProgramAsync` and must not poke Core devices. Shared chips stay machine-agnostic; TXTTAB is the BASIC-start oracle instead of a hardcoded family address table.

### Acceptance Criteria

1. Avalonia ViewModels do not write emulator RAM or inject KERNAL keystrokes directly.
2. `LoadProgram` is declared on the `EmulatorHost` proto service and mapped by `GrpcEmulatorHostService`.
3. BASIC-start detection uses live TXTTAB (`$2B/$2C`) for the current machine and VIC-20 memory config, not a C64-only `$0801` constant.
4. RUN uses the existing host keyboard-buffer automation after READY, scanning the current KERNAL HIBASE screen page (`$0288`) so VIC-20 unexpanded `$1E00` and +8K `$1000` are found as well as C64 `$0400`.

### Related FRs

- FR-UIDROP-002
- FR-CFG-005
- FR-UI-001

### Related TRs

- TR-GRPC-BOUNDARY-001
- TR-MVVM-001
