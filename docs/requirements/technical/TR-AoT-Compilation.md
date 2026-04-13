# TR-AoT-Compilation: NativeAOT Compilation Technical Requirement

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Quality Area   | Deployment / Startup           |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## TR-AOT-001: NativeAOT Compatible, No Reflection on Hot Path

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
