# VICE-SHARP HANDOFF FOR CLAUDE SONNET

## PROJECT OVERVIEW
ViceSharp is a C# port of VICE (Versatile Commodore Emulator) targeting .NET 10 with NativeAOT. The goal is cycle-accurate C64 emulation with deterministic replay.

## REPOSITORY
- **GitHub**: https://github.com/sharpninja/vice-sharp
- **Azure DevOps**: https://dev.azure.com/McpServer/VICE-Sharp/_git/VICE-Sharp
- **Default Branch**: `main` (renamed from `master`)

---

## CURRENT STATUS: 2026-04-18 (Updated)

### ✅ COMPLETED: Iteration 0 (Foundations)
All core infrastructure is complete and building:
- LockFreePubSub.cs - truly lock-free publish with zero allocations
- LockFreeMutationQueue.cs - bounded capacity, back pressure
- BasicBus.cs - atomic snapshots, no race conditions
- ArchitectureBuilder - constructs IMachine from IArchitectureDescriptor
- IMachine.GetState() implemented
- DeterministicTraceLogger - VICE-compatible trace output
- README and roadmap updated

### ✅ COMPLETED: Iteration 1 Fixes (2026-04-18)
- C64Machine.cs → replaced with C64Descriptor/C64NtscDescriptor (proper IArchitectureDescriptor)
- DeterministicTraceLogger.cs → removed unused _charBufferIndex field
- Build passes: 0 errors, 2 warnings (NETSDK1210 SourceGen framework issue)

### 🚧 IN PROGRESS: Iteration 1 (C64 Bringup)

### 📋 NEXT STEPS (execute in order, one topic per commit)

1. **Memory Map Layout** ✅ COMPLETED
   - IoDevice.cs implemented for $D000-$DFFF I/O region
   - Maps VIC-II, SID, CIA1, CIA2, Color RAM, I/O1, I/O2
   - All devices implement IAddressSpace for BasicBus

2. **Mos6510 CPU Implementation** ✅ COMPLETED
   - Mos6502.cs - CPU core with Tick(), registers, Reset()
   - Mos6502.Opcodes.cs - All 151 official opcodes + unofficial NOPs
   - Mos6502.Addressing.cs - Addressing mode helpers
   - Supports ADC, SBC, ASL, LSR, ROL, ROR, branches, loads, stores

3. **VicII Raster Engine** ✅ COMPLETED
   - Mos6569.cs - 63 cycle per line timing
   - 312 lines per frame (PAL)
   - Raster interrupt generation
   - IRQ line assertion

4. **CIA Timers** ✅ COMPLETED
   - Mos6526.cs - Timer A/B countdown
   - TOD clock registers (10ths, seconds, minutes, hours)
   - Interrupt handling on timer underflow
   - Port A/B with DDR

5. **First Boot Test** ✅ COMPLETED
   - C64RomLoader.cs - ROM loading with checksum validation
   - BASIC ROM: $A000 (8KB), KERNAL ROM: $E000 (8KB), CHAR ROM: $D000 (4KB)
   - Mos6502.Reset() reads from $FFFC/$FFFD for reset vector
   - All Iteration 1 core components implemented

6. **ValidationRunner** ✅ COMPLETED
   - ValidationRunner.cs - trace generation and comparison
   - Integrated with DeterministicTraceLogger
   - Supports --expected and --cycles command line args
   - Added Monitor project reference to Console

---

## KEY TECHNICAL DECISIONS

### Lock-Free Hot Path Requirements
- Publish() must be zero-allocation
- Use Volatile.Read() + immutable snapshots
- No locks on publish path
- Subscribe/Unsubscribe are cold path (allowed to allocate)

### Memory Model
- All state is plain structs/records (no base classes)
- MutationQueue for all state changes
- POCO model throughout

### Build Requirements
- Zero warnings on all builds
- NativeAOT compatible (no reflection on hot path)
- Test harness for cycle validation

---

## BYRD DEVELOPMENT MODEL
- One logical topic per commit
- Build after every single change
- Zero warnings before proceeding
- Push after every commit

---

## FILE STRUCTURE
```
src/
  ViceSharp.Abstractions/    # 33+ public interfaces
  ViceSharp.Core/            # Bus, Clock, MutationQueue, PubSub
  ViceSharp.Chips/           # Mos6510, VicII, Mos6526, Mos6581, PLA
  ViceSharp.Architectures/   # C64Machine, machine definitions
  ViceSharp.Monitor/         # Debug tools, trace logging
  ViceSharp.SourceGen/       # Device registration source generator
  ViceSharp.Console/         # NativeAOT reference shell
  ViceSharp.Avalonia/        # Desktop UI
```

---

## VALIDATION TOOLS
- VICE nightly: `vice-nightly-win64.zip`
- Lockstep validator: `tests/ViceSharp.TestHarness/TraceComparisonValidator.cs`
- VICE trace format matching required
- 10000 cycle golden trace comparison

---

## CRITICAL PATHS
1. C64Machine must implement IMachine fully → ✅ BUILD PASSES
2. Memory map must define all regions → ✅ IoDevice implemented
3. CPU fetch/execute loop → ✅ Mos6502 with all opcodes
4. ROM loader → ✅ C64RomLoader implemented

**Iteration 1 Status: ALL CORE COMPONENTS COMPLETE**

Ready to resume at: Test harness implementation for cycle-accurate validation
