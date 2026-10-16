# TR-MVVM: MVVM Architecture Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Architecture / UI Separation   |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-07-08                     |

---

## TR-MVVM-001: ViewModels Reference Only Abstractions, Views Contain Zero Logic

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
   - ViewModels live in the `ViewModels` folder of the `ViceSharp.Avalonia` assembly (`src/ViceSharp.Avalonia/ViewModels`); they use only `ViceSharp.Abstractions` types, host client facades, and standard .NET libraries, never runtime internals.
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

4. **Dependency Rules (strict, enforced by source-level boundary tests):**
   - `ViceSharp.Abstractions` -> (no dependencies)
   - `ViceSharp.Core` -> `ViceSharp.Abstractions`
   - `ViceSharp.Avalonia` -> `ViceSharp.Protocol`, `ViceSharp.Host` (composition), `ViceSharp.Abstractions`, Avalonia
   - `ViceSharp.Avalonia` does NOT reference `ViceSharp.Core`, `ViceSharp.Chips`, `ViceSharp.Architectures`, or `ViceSharp.RomFetch` (only the host/composition root touches concrete runtime projects)
   - Enforcement is by `AvaloniaBoundaryTests` (TR-MVVM-001 citations): project-reference checks on the `.csproj` plus forbidden-identifier scans over all Avalonia source, rather than a separate-assembly compile check.

5. **Remote Host Client:**
   - ViewModels consume abstraction-level host client facades for TR-GRPC-BOUNDARY-001 scenarios.
   - Generated gRPC clients and transport concerns stay in infrastructure adapters or the composition root.
   - ViewModels expose UI state and commands without mutating emulator core objects directly.
   - In-process Avalonia frame presentation may use a host-owned direct render surface, but ViewModels do not reference that local emulator/frame source or any runtime internals.

### Acceptance Criteria

1. The `ViceSharp.Avalonia` project references `ViceSharp.Protocol` and `ViceSharp.Host` but no runtime projects (Core, Chips, Architectures, RomFetch), verified by `AvaloniaBoundaryTests.AvaloniaProject_ReferencesProtocolAndHostCompositionButNotRuntimeProjects`.
2. Avalonia source (including all ViewModels) contains no references to runtime internals (`ViceSharp.Core`, `ViceSharp.Chips`, `ViceSharp.Architectures`, `ViceSharp.RomFetch`, `IMachine`, `IVideoChip`, `ArchitectureBuilder`), verified by `AvaloniaBoundaryTests.AvaloniaSources_DoNotReferenceRuntimeInternals`.
3. All ViewModel public properties and commands are exercised by unit tests that do not require a UI framework.
4. View code-behind files contain fewer than 20 lines of code each (excluding auto-generated code).
5. Source-level boundary tests (`AvaloniaBoundaryTests`) enforce the dependency rules and fail the test gate on violations.
6. Swapping the UI framework (e.g., replacing Avalonia views with MAUI views) requires changes only in the View layer.
7. A remote host UI can be tested with mocked host client facades and without starting `ViceSharp.Core`.
8. The local Avalonia render surface can be tested as a host/composition concern without adding core, chip, or architecture references to ViewModels.

### Verification Method

- `AvaloniaBoundaryTests` in the test suite validates project references and forbidden source identifiers.
- Line-count check on View code-behind files.
- ViewModel unit test coverage report (target: >90% of ViewModel methods covered).
- Dependency analysis output in the build log.

### Related TRs

- TR-LIB-001 (Library-first design provides the Model layer)
- TR-PLAT-001 (MVVM enables per-platform View implementations)
- TR-GRPC-BOUNDARY-001 (UI control clients consume the host through generated contracts and adapters; local rendering remains host-owned)

### Design Decisions

- The host/composition root (`ViceSharp.Host`) is the only place where concrete core types and the ViewModel-facing abstractions meet; the Avalonia shell consumes the host surface, not core assemblies.
- A lightweight in-repo `ObservableObject` base (`src/ViceSharp.Avalonia/ViewModels/ObservableObject.cs`) provides the `INotifyPropertyChanged` infrastructure; no external MVVM framework dependency.
- The ViewModel for the main emulator display exposes display state and commands through abstractions only. The in-process Avalonia render surface may bind directly to a local frame source, but that binding is owned by the host/composition layer rather than the ViewModel.
- gRPC generated clients are adapted behind ViewModel-facing interfaces so transport code does not leak into ViewModels.
