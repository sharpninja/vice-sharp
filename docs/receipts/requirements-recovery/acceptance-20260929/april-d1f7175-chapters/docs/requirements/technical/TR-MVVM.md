# TR-MVVM: MVVM Architecture Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Architecture / UI Separation   |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

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

### Acceptance Criteria

1. `ViceSharp.ViewModels` compiles with zero references to any UI framework (verified by dependency analysis).
2. `ViceSharp.ViewModels` compiles with zero references to `ViceSharp.Core` (only `ViceSharp.Abstractions`).
3. All ViewModel public properties and commands are exercised by unit tests that do not require a UI framework.
4. View code-behind files contain fewer than 20 lines of code each (excluding auto-generated code).
5. An architecture test (using a tool like NetArchTest or ArchUnitNET) enforces the dependency rules and fails the build on violations.
6. Swapping the UI framework (e.g., replacing Avalonia views with MAUI views) requires changes only in the View assembly.

### Verification Method

- Architecture test in the CI pipeline that validates assembly dependency rules.
- Line-count check on View code-behind files.
- ViewModel unit test coverage report (target: >90% of ViewModel methods covered).
- Dependency analysis output in the build log.

### Related TRs

- TR-LIB-001 (Library-first design provides the Model layer)
- TR-PLAT-001 (MVVM enables per-platform View implementations)

### Design Decisions

- The composition root (application entry point) is the only place where `ViceSharp.Core` and `ViceSharp.ViewModels` meet; it registers concrete implementations against abstraction interfaces in the DI container.
- ReactiveUI or CommunityToolkit.Mvvm is used for `INotifyPropertyChanged` and `ICommand` infrastructure.
- The ViewModel for the main emulator display exposes a `WriteableBitmap`-like abstraction (defined in Abstractions as `IFrameBuffer`) that the View layer wraps in its platform-specific bitmap type.
