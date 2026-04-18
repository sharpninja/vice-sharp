# VICE-SHARP HANDOFF FOR CLAUDE SONNET

## PROJECT OVERVIEW
ViceSharp is a C# port of VICE (Versatile Commodore Emulator) targeting .NET 10 with NativeAOT. The goal is cycle-accurate C64 emulation with deterministic replay.

## REPOSITORY
- **GitHub**: https://github.com/sharpninja/vice-sharp
- **Azure DevOps**: https://dev.azure.com/McpServer/VICE-Sharp/_git/VICE-Sharp
- **Default Branch**: `main` (renamed from `master`)

---

## CURRENT STATUS: 2026-04-18

### ✅ COMPLETED: Iteration 0 (Foundations)
All core infrastructure is complete and building:
- LockFreePubSub.cs - truly lock-free publish with zero allocations
- DoubleBufferedMutationQueue.cs - bounded capacity, back pressure
- BasicBus.cs - atomic snapshots, no race conditions
- IMachine.GetState() implemented
- README and roadmap updated

### 🚧 IN PROGRESS: Iteration 1 (C64 Bringup)

### ⚠️ BLOCKERS
The following files have compilation errors that must be fixed:
1. `src/ViceSharp.Monitor/DeterministicTraceLogger.cs` - Missing GetState() usage
2. `src/ViceSharp.Architectures/C64Machine.cs` - Interface implementation issues

### 📋 NEXT STEPS (execute in order, one topic per commit)

1. **Fix C64Machine IMachine interface**
   - C64Machine must implement IMachine fully
   - Fix missing interface members: Reset(), RunFrame(), StepInstruction(), GetState()
   - Use actual BasicBus, DeviceRegistry implementations from Core

2. **Fix DeterministicTraceLogger**
   - Remove GetState() call or implement properly
   - Fix unused field warning

3. **Memory Map Layout**
   - Define exact $0000-$FFFF region definitions
   - RAM/ROM/IO/PLA regions with priority ordering
   - Wire to BasicBus

4. **Mos6510 CPU Implementation**
   - Start with empty instruction fetch loop
   - Implement all 151 official opcodes
   - Then 105 unofficial opcodes

5. **VicII Raster Engine**
   - 63 cycle per line timing
   - 312 lines per frame (PAL)
   - Bad line detection
   - IRQ generation

6. **CIA Timers**
   - Timer A/B countdown
   - TOD clock
   - Keyboard matrix

7. **First Boot Test**
   - Load KERNAL/BASIC ROMs
   - Execute from reset vector
   - Target: reach $E55B BASIC warm start

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
1. C64Machine must implement IMachine fully → build must pass
2. Memory map must define all regions → CPU/VIC/CIA access
3. CPU fetch/execute loop → cycle accuracy validation
4. ROM loader → boot test execution

Ready to resume at: Fix C64Machine IMachine implementation