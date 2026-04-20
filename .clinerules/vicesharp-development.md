## Brief overview
Project specific rules for ViceSharp Commodore emulator development. Follow these guidelines for all code contributions.

## Development Workflow
- Build after every change, ensure zero errors and zero warnings
- Follow Public API specification exactly
- Implement interfaces in batch order: Abstractions → Core → Chips → Architectures
- Verify NativeAOT compatibility for all non-test code
- Zero allocations on hot path (cycle execution)
- All state is plain structs/records, no base classes

## Coding Conventions
- Use explicit XML documentation comments for all public interfaces
- Follow .NET 10 conventions, nullability enabled
- 4 space indentation
- File per interface / per class
- Namespace matches directory structure exactly

## Chip Organization
- One chip = one location in designated subdirectory
- Canonical locations:
  - `src/ViceSharp.Chips/Cpu/` - CPU implementations (Mos6502, Mos6510)
  - `src/ViceSharp.Chips/VicIi/` - VIC-II video (Mos6567, Mos6569)
  - `src/ViceSharp.Chips/Sid/` - SID audio (Mos6581, Mos8580)
  - `src/ViceSharp.Chips/Cia/` - CIA timer/I/O (Mos6526)
  - `src/ViceSharp.Chips/IEC/` - Disk CPU (Mos6502 variant)
- All chip specs must reference `docs/requirements/functional/FR-*.md`
- No chip definitions outside designated subdirectories
- All chips implement interfaces from `ViceSharp.Abstractions`

## Implementation Order
1.  Abstractions interfaces (33+)
2.  Source Generator
3.  Core Bus / Clock / Mutation Queue
4.  Chip implementations
5.  Machine definitions
6.  App shells

## Communication
- Run full build before each progress report
- Show task_progress checklist updates
- Continue work autonomously unless blocking issue encountered
- Notify only on completion or when human input is required