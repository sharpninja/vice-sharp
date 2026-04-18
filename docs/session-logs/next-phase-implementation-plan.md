# ViceSharp Next Phase Implementation Plan
Date: 2026-04-17
Status: Final
Total Estimate: 30 working days / 6 weeks

---

## ✅ Current State (Pre-Phase)
All core implementation complete:
✅ 6502 CPU, VIC-II, CIA, SID, VIA, PLA chips
✅ System Bus, Clock, Mutation Queue
✅ Full public interface specification
✅ Debug monitor infrastructure
✅ Zero build errors, zero warnings
✅ NativeAOT compatible

---

## 📅 Phase 1: Native Integration (Days 1-4)

| Day | Task | Exact Commands / Steps | Acceptance |
|-----|------|------------------------|------------|
| 1 | Initialize VICE submodule | `git submodule update --init --recursive native/vice` | VICE source checked out at correct revision |
| 1 | Configure CMake build | Add `-fPIC`, disable all frontends, enable libvice shared target | CMake generates build files without errors |
| 2 | Implement shim layer | Implement 12 C entry points in `native/vice-shim.c` | All functions exported, no unresolved symbols |
| 2 | Generate P/Invoke bindings | Create `ViceNative.cs` with explicit layout structs | Library loads successfully, no marshalling errors |
| 3 | Implement dual execution engine | Create `LockstepValidator` class | Can instantiate both ViceSharp and original VICE |
| 3 | Per-cycle capture | Capture full state after every cycle: registers, memory, I/O pins | State structs are blittable, zero allocations |
| 4 | Byte exact comparison | Compare every bit of state | Reports deviation within 1 cycle accuracy |
| 4 | Deviation reporter | Generate full trace report on mismatch | Includes PC, stack, last 16 instructions, register diff |

✅ Exit: Lockstep validator runs autonomously

---

## 📅 Phase 2: Accuracy Validation (Days 5-12)

| Day | Task | Exact Steps | Acceptance |
|-----|------|-------------|------------|
| 5 | CPU instruction validation | Run all 256 opcode variants | 100% instruction accuracy |
| 6 | Cycle timing validation | Verify every instruction cycle count | Zero timing deviations |
| 7 | Interrupt latency test | Measure cycles from IRQ assert to vector fetch | Exact match original VICE |
| 8 | Undefined behavior validation | Match illegal opcode behavior, open bus reads | All test ROMs pass |
| 9 | VIC-II raster accuracy | Validate bad lines, sprite DMA, collision detection | Graphics test ROMs pass |
| 10 | CIA timer accuracy | Validate timer underflow, interrupt generation | Exact cycle accurate |
| 11 | PLA banking logic | Test all 16 memory map configurations | Correct address decoding |
| 12 | SID waveform accuracy | Match filter response and envelope curves | Audio test ROMs pass |

✅ Exit: Zero lockstep deviations reported

---

## 📅 Phase 3: I/O Subsystems (Days 13-15)

| Day | Task | Exact Steps | Acceptance |
|-----|------|-------------|------------|
| 13 | ROM Manager | CRC32 validation, search paths, version detection | KERNAL, BASIC, CHARGEN load correctly |
| 13 | Machine reset sequence | Implement proper power-on reset behavior | Reset vector fetched correctly |
| 14 | 1541 Disk Controller | GCR 4/5 codec, IEC bus state machine | D64 images mount successfully |
| 14 | Sector read/write | Implement track/sector mapping | Can read directory from disk image |
| 15 | Keyboard Matrix | 8x8 scanning, RESTORE key, shift lock | Keyboard inputs recognized |
| 15 | Joystick Ports | Multiplexed port reading | Joystick directions and fire button work |

✅ Exit: Machine boots successfully to `READY.` prompt

---

## 📅 Phase 4: Output Systems (Days 16-19)

| Day | Task | Exact Steps | Acceptance |
|-----|------|-------------|------------|
| 16 | Frame Generation | 50.125fps PAL / 59.826fps NTSC timing | Exact frame rate |
| 16 | Raster Beam Position | Accurate cycle position for raster interrupts | Demo effects work correctly |
| 17 | Pixel Pipeline | Border, sprites, priority, multicolor mode | Correct video output |
| 17 | Frame Buffer Swap | Atomic pointer swap between emulation and UI thread | Zero copies on hot path |
| 18 | Audio Resampler | Accumulate-and-average for hot path | Clean audio output |
| 18 | Ring Buffer | Lock-free, adaptive size, 20ms target latency | No glitches or underruns |
| 19 | Frame Pacing | Implement all 3 sync strategies: Audio, Video, FreeRun | Smooth playback |
| 19 | Latency Compensation | Audio buffer level adjustment | Responsive feel |

✅ Exit: Full audio/video output working

---

## 📅 Phase 5: User Interface (Days 20-25)

| Day | Task | Exact Steps | Acceptance |
|-----|------|-------------|------------|
| 20 | Avalonia Render Surface | Hardware accelerated video display | 60fps rendering |
| 21 | Integer Scaling | Aspect ratio correction, scanline filter | Correct display proportions |
| 22 | Debug Monitor Views | Disassembly, registers, memory dump, breakpoints | Full debug functionality |
| 23 | Status Bar | FPS, cycle counter, frame time, audio level | Real time statistics |
| 24 | Settings Dialog | Machine config, input mapping, audio/video settings | Persist to config file |
| 25 | Console Shell | Headless operation, command line arguments | Can run in terminal mode |

✅ Exit: Full working desktop UI

---

## 📅 Phase 6: Release (Days 26-30)

| Day | Task | Exact Steps | Acceptance |
|-----|------|-------------|------------|
| 26 | Performance Optimization | Profile hot paths, eliminate allocations | 100% speed on 2GHz CPU |
| 27 | NativeAOT Publishing | Build single file binaries for Windows, Linux, macOS | <20MB executable size |
| 28 | Test Suite Execution | Run all functional, determinism, stress tests | All tests pass |
| 29 | Release Packaging | Create installer, documentation, release notes | Ready for distribution |
| 30 | Final Validation | Full 24 hour burn in test | Zero crashes, zero deviations |

✅ Exit: Public release ready

---

## 🎯 Final Success Criteria
1. ✅ Boots C64 to BASIC prompt
2. ✅ Runs commercial software correctly
3. ✅ Full audio/video/input working
4. ✅ Debug monitor fully functional
5. ✅ Zero lockstep deviations from original VICE
6. ✅ NativeAOT single file binaries
7. ✅ All test ROMs pass
8. ✅ Real time performance

---

## ⚠️ Non-Negotiable Rules
1. Zero allocations on hot path
2. Maintain NativeAOT compatibility for all code
3. All changes must pass full build before commit
4. Follow Public API specification exactly
5. No base classes - all state is structs/records