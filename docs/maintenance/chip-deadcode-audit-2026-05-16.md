# Chip Dead-Code Audit (2026-05-16)

Owner: REPO-MAINT-001 phase 1.

This document inventories every public type declared under `src/ViceSharp.Chips/` and classifies each as **KEEP**, **DELETE**, or **INVESTIGATE**. Reference counts are *external* file counts: how many files outside the type's own declaring file mention the type by its short name (ripgrep `\bTypeName\b`, scope `src/` and `tests/`, excluding generated `obj/`).

**Scope warning.** The chip directory has two generations of code:

1. A flat set of early-draft skeletons at the directory root (e.g. `src/ViceSharp.Chips/Mos6510.cs`, `Mos6526.cs`, `Mos6581.cs`, `VicII.cs`, `Cpu6510.cs`, `IecDrive.cs`, `CharacterRom.cs`, `SidOscillator.cs`).
2. The production-style chip implementations under subdirectories (`Cpu/`, `Cia/`, `Sid/`, `Audio/`, `VicIi/`, `Video/`, `IEC/`, `PLA/`, `Tape/`, `Input/`, `Cartridges/`, `Interface/`).

The new subdirectory chips all implement abstractions (`IClockedDevice`, `IAddressSpace`, `ICpu`, `ICiaChip`, `IAudioChip`, `IVideoChip`, `IFloppyDrive`, etc.) and accept dependency-injected ctors (`(IBus bus, ...)`). The flat-root chips are field-bag skeletons with parameterless ctors and `TODO`-laden `Step()` bodies.

**The flat-root chips are kept alive by a parallel draft architecture** (`src/ViceSharp.Architectures/C64Machine.cs` and `src/ViceSharp.Avalonia/`). The new architecture lives at `src/ViceSharp.Architectures/C64/Commodore64.cs` and uses the subdirectory chips. Removing the flat-root chips therefore requires deleting `C64Machine.cs` and rewiring or removing `ViceSharp.Avalonia` first. That is intentionally out of scope for phase 1; this audit only flags candidates and documents call chains.

This audit DOES NOT delete any files. Deletion is deferred to a follow-up so the blast radius can be reviewed against test counts.

## Methodology

```
# Type discovery
grep -rEoh '^\s*(public|internal)?\s*(sealed|partial|abstract|static)*\s*(class|struct|interface|record|enum)\s+\w+' src/ViceSharp.Chips/

# Per-type external reference count (files only)
grep -rlE '\b<TypeName>\b' --include='*.cs' src tests | wc -l   # all files containing the symbol
# Subtract the count of declaring files for that type to get the external count.
```

Reference numbers below are the **external file count** (callers + tests, never the declaring file itself). `0 ext` means no consumer outside the declaring file.

## Classification

| Type | File | Ext refs | Recommendation | Notes |
|---|---|---:|---|---|
| `Cpu6510` | `src/ViceSharp.Chips/Cpu6510.cs` | 0 | DELETE | Skeleton field bag. Not referenced by any caller, not even by the old `C64Machine.cs`. Pure orphan. |
| `Mos6510` | `src/ViceSharp.Chips/Mos6510.cs` (+ `Mos6510.Opcodes.cs`) | 1 | DELETE | Only `C64Machine.cs` (also dead-draft) references it. Real CPU is `Cpu/Mos6502.cs`. |
| `Mos6526` (root) | `src/ViceSharp.Chips/Mos6526.cs` | 2 | DELETE | Two references in `C64Machine.cs` (draft). Production is `Cia/Mos6526.cs` with `(IBus, IInterruptLine)` ctor. |
| `Mos6581` (root) | `src/ViceSharp.Chips/Mos6581.cs` | 1 | DELETE | One reference in `C64Machine.cs` (draft). Production is `Sid/Mos6581.cs` with `(IBus, IAudioBackend)` ctor. |
| `VicII` (root) | `src/ViceSharp.Chips/VicII.cs` | 1 | DELETE | Referenced only by `C64Machine.cs` (parameterless). Production is `Video/VicII.cs` with `(IBus, IFrameSink)` ctor. Tests target the production class. |
| `Cia6526` | `src/ViceSharp.Chips/Interface/Cia6526.cs` | 0 | DELETE | Zero external references. Independent partial declaration with no counterpart anywhere else; production CIA is `Cia/Mos6526.cs`. Pure orphan. |
| `IecDrive` (root) | `src/ViceSharp.Chips/IecDrive.cs` | 0 | DELETE | Zero external references. Production `IecDrive` lives at `IEC/IecDrive.cs` and is the one all callers use. |
| `CharacterRom` | `src/ViceSharp.Chips/CharacterRom.cs` | 1 | DELETE | The one reference is `VicII.cs` (root, itself a DELETE candidate). Production video pipeline (`VicIi/Mos6569.cs`, `VicIi/VideoRenderer.cs`) does not use this class. `RomFetch` has its own unrelated `CharacterRom` static property. |
| `SidOscillator` | `src/ViceSharp.Chips/SidOscillator.cs` | 1 | KEEP | Used by new `tests/ViceSharp.TestHarness/SidOscillatorTests.cs`. This is a public oscillator struct intentionally exposed for unit tests. |
| `Sid6581` | `src/ViceSharp.Chips/Audio/Sid6581.cs` | 7 | KEEP | Production SID. Has its own register-readback test additions (`SidRegisterReadbackTests.cs`). |
| `Sid8580` | `src/ViceSharp.Chips/Audio/Sid8580.cs` | 3 | KEEP | Production SID variant. |
| `Sid8580D` | `src/ViceSharp.Chips/Audio/Sid8580D.cs` | 0 | INVESTIGATE | No external file references but lives next to other production SID variants. Likely referenced by reflection or planned variant table; verify before deletion. |
| `Mos6502` | `src/ViceSharp.Chips/Cpu/Mos6502.cs` (+ partials) | 14 | KEEP | Production CPU. |
| `Mos6502DiskCpu` | `src/ViceSharp.Chips/IEC/Mos6502DiskCpu.cs` | 0 | INVESTIGATE | Drive-side CPU. Not externally referenced yet; might be queued for IEC integration. Confirm with drive owner before deletion. |
| `Mos6526` (Cia/) | `src/ViceSharp.Chips/Cia/Mos6526.cs` | 6 | KEEP | Production CIA. Used by `Commodore64.cs`. |
| `Mos6567`, `Mos6567R56A`, `Mos6569`, `Mos6569R1`, `Mos6572`, `Mos8562`, `Mos8565` | `src/ViceSharp.Chips/VicIi/*.cs` | 2-18 | KEEP | Production VIC-II variants. |
| `VicII` (Video/) | `src/ViceSharp.Chips/Video/VicII.cs` | 3 | KEEP | Production VIC-II surface used by `Commodore64.cs` and validation tests. |
| `Mos6581` (Sid/) | `src/ViceSharp.Chips/Sid/Mos6581.cs` | 2 | KEEP | Production SID surface. |
| `Mos906114` | `src/ViceSharp.Chips/PLA/Mos906114.cs` | 2 | KEEP | Production PLA. |
| `D64Image`, `D64ProgramFile`, `DiskController`, `GcrCodec`, `IecBus`, `IecD64Attachment`, `IecDrive` (IEC/), `Via6522` | `src/ViceSharp.Chips/IEC/*.cs` | 1-22 | KEEP | Production IEC. `DiskController`, `GcrCodec`, `IecBus`, `Via6522` show 0/1 external file refs because they are internal helpers used by `IecDrive`. Re-verify before deleting. |
| `Datasette`, `TapImage`, `TapPulseReader` | `src/ViceSharp.Chips/Tape/*.cs` | 2-3 | KEEP | Production tape subsystem. |
| `C64HostKeyboardMapper`, `C64JoystickPort`, `C64KeyboardMap`, `C64KeyboardMapEntry`, `C64KeyboardMatrix`, `C64VkmDiagnostic`, `C64VkmDiagnosticSeverity`, `C64VkmParseResult`, `C64VkmParser`, `C64VkmShiftFlags` | `src/ViceSharp.Chips/Input/*.cs` | 1-18 | KEEP | New input/keymap subsystem covered by TR-INPUT-VKM-001. |
| `StandardCartridgeImage`, `StandardCartridgeSize` | `src/ViceSharp.Chips/Cartridges/*.cs` | 2-5 | KEEP | Cartridge model. |
| `VicPalette`, `VideoRenderer` | `src/ViceSharp.Chips/VicIi/*.cs` | 3 each | KEEP | Production video pipeline. |
| Nested enums on production chips (`BorderSide`, `ColorMode`, `ColumnMode`, `EnvelopeState`, `FilterType`, `InterruptSource`, `IrqSource`, `MemoryConfig`, `PortMode`, `SpriteCollisionType`, `SpriteColorMode`, `SpriteExpansion`, `SpritePriority`, `TimerMode`, `TimerOutput`, `TodMode`, `TvSystem`, `VideoMode`, `VoiceModulation`, `Waveform`, `JoystickButtons`, `Builder`) | declaring chip file | typically 0 external file refs (in-file usage only) | KEEP | These are public enums/builders declared inside a chip's file and consumed exclusively by the same chip's methods. The 0-external-file count is expected; they are not orphans. |

## Verification quotes for DELETE candidates

`grep` output backing the DELETE recommendations (all run from the worktree root):

```
$ grep -rE '\bCpu6510\b' --include='*.cs' src tests
src/ViceSharp.Chips/Cpu6510.cs:6:public sealed class Cpu6510
src/ViceSharp.Chips/Cpu6510.cs:101:    public Cpu6510()
```

```
$ grep -rE '\bMos6510\b' --include='*.cs' src tests
src/ViceSharp.Architectures/C64Machine.cs:15:    public Mos6510 CPU { get; } = new Mos6510();
src/ViceSharp.Chips/Mos6510.Opcodes.cs:3:partial class Mos6510
src/ViceSharp.Chips/Mos6510.cs:7:public sealed partial class Mos6510
```

```
$ grep -rE '\bIecDrive\b' --include='*.cs' src tests
src/ViceSharp.Chips/IecDrive.cs:6:public sealed class IecDrive
src/ViceSharp.Chips/IEC/IecDrive.cs:3:public sealed class IecDrive
```

```
$ grep -rEn '\bVicII\b' --include='*.cs' src tests
tests/ViceSharp.TestHarness/VicIiValidationTests.cs:6:public sealed class VicIiValidationTests : LockstepTestRunner<VicII>, IAsyncLifetime
tests/ViceSharp.TestHarness/VicIiValidationTests.cs:11:        : base(new VicII(null!, null!))
src/ViceSharp.Architectures/C64Machine.cs:16:    public VicII VIC { get; } = new VicII();
src/ViceSharp.Chips/VicII.cs:8:public sealed class VicII
src/ViceSharp.Chips/Video/VicII.cs:5:public sealed partial class VicII : IClockedDevice, IAddressSpace, IVideoChip
src/ViceSharp.Chips/Video/VicII.cs:51:    public VicII(IBus bus, IFrameSink frameSink)
```

(The two-argument ctor at `Video/VicII.cs:51` matches the test usage `new VicII(null!, null!)`. The parameterless ctor `new VicII()` only resolves against the dead root-level `VicII.cs`.)

```
$ grep -rEn '\bCia6526\b' --include='*.cs' src tests
src/ViceSharp.Chips/Interface/Cia6526.cs:5:public sealed partial class Cia6526 : IClockedDevice, IAddressSpace, ICiaChip, IInterruptSource
src/ViceSharp.Chips/Interface/Cia6526.cs:46:    public Cia6526(IBus bus, IInterruptLine irqLine)
```

```
$ grep -rEn '\bCharacterRom\b' --include='*.cs' src tests
src/ViceSharp.RomFetch/C64RomLoader.cs:29:    public static readonly RomDescriptor CharacterRom = new RomDescriptor
src/ViceSharp.RomFetch/C64RomLoader.cs:68:            && LoadRom(character, CharacterRom);
src/ViceSharp.Chips/VicII.cs:274:            byte pattern = CharacterRom.GetGlyph(character)[charLine];
src/ViceSharp.Chips/CharacterRom.cs:6:public static class CharacterRom
src/ViceSharp.Chips/CharacterRom.cs:13:    static CharacterRom()
```

(`C64RomLoader.CharacterRom` is a `RomDescriptor` field, not the `CharacterRom` chip class. The only consumer of the chip class is the dead `VicII.cs` root file.)

## Recommended deletion order (deferred to phase 2)

Because the flat-root chips form a connected component with `ViceSharp.Architectures.C64Machine` and `ViceSharp.Avalonia`, deletion must proceed top-down:

1. Confirm `ViceSharp.Avalonia` (`MainWindow.axaml.cs`, `VideoSurface.cs`, `Program.cs`, `App.axaml.cs`) is the legacy prototype and not the canonical UI. If it is legacy, retire the project (or rewire it onto `Commodore64`).
2. Delete `src/ViceSharp.Architectures/C64Machine.cs` once nothing references it.
3. Delete the flat-root chip files in this order (no cross-dependencies among them):
   - `src/ViceSharp.Chips/Cpu6510.cs`
   - `src/ViceSharp.Chips/Mos6510.cs` and `src/ViceSharp.Chips/Mos6510.Opcodes.cs`
   - `src/ViceSharp.Chips/Mos6526.cs`
   - `src/ViceSharp.Chips/Mos6581.cs`
   - `src/ViceSharp.Chips/VicII.cs`
   - `src/ViceSharp.Chips/CharacterRom.cs`
   - `src/ViceSharp.Chips/IecDrive.cs`
   - `src/ViceSharp.Chips/Interface/Cia6526.cs`
4. Re-run `dotnet test ./ViceSharp.slnx --nologo` and confirm the green count is unchanged.

For the INVESTIGATE entries (`Sid8580D`, `Mos6502DiskCpu`, internal IEC helpers like `DiskController`, `IecBus`, `GcrCodec`, `Via6522`), the deletion decision requires the chip owner's input: these chips have valid production-quality implementations but no current consumers, so they may be intentional preparation for upcoming work rather than dead code.

## Numbers

- Total `.cs` files under `src/ViceSharp.Chips/`: 44 source files (excluding `obj/`).
- Public types declared: 66 (unique names).
- Hard DELETE candidates (zero or only-dead-draft references): 8.
- INVESTIGATE candidates (no external references but production-style code): 4.
- KEEP: the remainder.
