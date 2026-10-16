# VSF Gate 0 local evidence

Recorded: `2026-10-03T10:06:23-05:00`  
Checkout: `F:\GitHub\vice-sharp`  
Execution: local read-only commands; no web, remote runner, or cloud CI.

## Native VICE revision

`git -c safe.directory=F:/GitHub/vice-sharp/native/vice -C native/vice rev-parse HEAD` returned:

```text
cc67418cba58e71d8e70fd353e9de47199ad2075
```

This equals the plan and requirement pin.

## Supplied C64 fixture

The local fixture parser read `tests/ViceSharp.TestHarness/Fixtures/Vsf/ready-c64sc-truedrive.vsf` using the VICE framing layout and checked every inclusive little-endian module length.

```text
Length       : 190854
Sha256       : 5C284F5CF0A1BC7E01A2E4117A74158B7A9D60485F5626448F4B7655C0E23C19
Magic        : VICE Snapshot File\x1a
Container    : 2.0
Machine      : C64SC
HeaderBytes  : 58
VersionMagic : VICE Version\x1a
ViceVersion  : 3.8.0.0
Revision     : 0
ModuleCount  : 24
ExactEof     : True
```

Ordered modules, version, inclusive size, and offset:

```text
MAINCPU      1.2        113      58
C64MEM       0.1      65577     171
C64CART      0.1         23   65748
CIA1         2.5         99   65771
CIA2         2.5         99   65870
SID          1.5         58   65969
SIDEXTENDED  1.4        155   66027
VDRIVEIMAGE8 2.0         22   66182
DRIVE8       2.0         24   66204
DRIVE9       2.0        167   66228
DRIVE10      2.0        167   66395
DRIVE11      2.0        167   66562
FSDRIVE      0.0        280   66729
VIC-II       1.3     123437   67009
GLUE         1.0         25  190446
C64MEMHACKS  0.0         23  190471
TAPEPORT     1.0         24  190494
DATASETTE    1.5        100  190518
KEYBOARD     1.1        118  190618
JOYPORT0     0.0         23  190736
JOYSTICK0    1.2         24  190759
JOYPORT1     0.0         23  190783
JOYSTICK1    1.2         24  190806
USERPORT     1.0         24  190830
```

## Local MSYS2 prerequisite paths

All required paths existed locally:

```text
C:\msys64\usr\bin\bash.exe
C:\msys64\mingw64\bin\gcc.exe
C:\msys64\usr\bin\make.exe
C:\msys64\usr\bin\autoconf
C:\msys64\usr\bin\automake
C:\msys64\usr\bin\libtool
C:\msys64\mingw64\bin\pkg-config.exe
```

The sandbox identity cannot create its normal MSYS2 home directory, so this is Gate 0 path evidence rather than a successful native rebuild. Slice 1 must run the actual non-skipping preflight and build in the authorized local host context.

## Native model-selector source

The closed production selector mapping is grounded in:

- `native/vice-shim.c:496-579`: C64 aliases and model constants; canonical selectors chosen by the plan are `c64`, `c64c`, `c64old`, `c64ntsc`, `c64cntsc`, `c64oldntsc`, `paln`, `sx64pal`, `sx64ntsc`, `pet64pal`, `pet64ntsc`, `ultimax`, `c64gs`, and `c64jap`.
- `native/vice-shim-vic20.c:416-441`: xvic aliases and model constants; canonical selectors chosen by the plan are `vic20`, `vic20ntsc`, `vic21`, and `vic1001`.

## Current PE dependency closure

`C:\msys64\mingw64\bin\objdump.exe -p` was run locally on both shims and their non-system imports.

```text
vice_x64.dll  -> libstdc++-6.dll, zlib1.dll
vice_xvic.dll -> libstdc++-6.dll, zlib1.dll
libstdc++-6.dll -> libgcc_s_seh-1.dll, libwinpthread-1.dll
libgcc_s_seh-1.dll -> libwinpthread-1.dll
libwinpthread-1.dll -> system DLLs only
zlib1.dll -> system DLLs only
```

Both shims also import Windows system DLLs (`DSOUND`, `KERNEL32`, `msvcrt`, `ntdll`, `SHELL32`, `USER32`, `WINMM`, and `WS2_32`). `libiconv-2.dll` is present in `native/` because the current build scripts copy it opportunistically, but neither shim nor the transitive closure imports it. The package contract therefore stages only the verified non-system closure and reruns this import walk after every native rebuild.

Current local SHA-256 values:

```text
vice_x64.dll       E5B076517136813C589258A0550256C7A244EA34431687E4DBC72201A4F4557B
vice_xvic.dll      F03A7A3545C5964FBAB863D3F3D1AB7A2EB99C2543ED94071F80F07648513886
libstdc++-6.dll    428D22A8A4D25E39EDAD36851122701B3460C2BCF628884CDD2664530D2DD634
libgcc_s_seh-1.dll 2DE4355648441DB0230A9ABA1A149FC829B094F83C14C37EA3F775356D33D8B1
libwinpthread-1.dll B0D84F7B6346CF835EF19ECC95991CDAA6272BB8AD6FEE43F446C07AA97FCBD9
zlib1.dll          CB7AB3788D10940DF874ACD97B1821BBB5EE4A91F3EEC11982BB5BF7A3C96443
```

These binary hashes describe the present local build only. Slice 6 recomputes and records hashes from the binaries it packages rather than treating these values as permanent source requirements.
