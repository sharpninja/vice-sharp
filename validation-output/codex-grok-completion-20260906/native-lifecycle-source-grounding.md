# Native lifecycle source grounding for approved Slice 1

Source inspection recorded 2026-09-06 22:30:31 -05:00. This is a read-only source index, not a passing runtime receipt or Slice 1 completion claim. No native operation was executed and no implementation was changed for this inspection.

Plan: `docs/plans/PLAN-GROK-COMPLETION-20260906.md`, Slice 1. Requirements: `FR-NATIVE-LIFECYCLE-001`, `TR-NATIVE-LIFECYCLE-001`, `TEST-NATIVE-LIFECYCLE-001`.

## Observed implementation

- Both shims define create/step limits of 5000 ms and stop limit of 2000 ms: `native/vice-shim.c:96` and `native/vice-shim-vic20.c:91`. Create still waits with `INFINITE` at x64 line 389 and xvic line 382. x64 step loops pass the full step timeout at lines 1183 and 1216; the approved absolute deadline must cover repeated wakes rather than restarting the budget.
- `native/vice-shim-vic20.c:276` returns failure when its worker does not stop, but destroy (470), reset (494), read snapshot (543), and write snapshot (574) ignore that result. Destroy subsequently clears the active pointer and frees the instance at 484. The analogous x64 calls are 615, 645, 745 and 796. A failed stop therefore does not currently prevent unsafe follow-on operations.
- `native/vice/vice/src/vic20/vic20sound.c:612` resets the global live `snd` struct and sound-register/noise state. Oracle store/render call the live initialization and sampling functions (627, 646 and 661). xvic machine reset calls that oracle reset at `native/vice-shim-vic20.c:529`. This is the concrete shared-state boundary for the already approved separation.
- `src/ViceSharp.Core/ViceNative.cs:756` and `src/ViceSharp.Core/ViceNative.Xvic.cs:302` own readonly native pointers. Their Reset/Step calls use void imports, and Dispose directly destroys the same pointer on every call (x64 915; xvic 495). Managed failure propagation and idempotence require coverage at these owning adapters as well as the native ABI.
- `tests/ViceSharp.TestHarness/Vic20/Vic20SoundLockstep.cs:88` and `:111` return successfully when xvic is unavailable and compare only `Math.Min(n, m)` samples (103, 132). The approved replacement evidence must assert prerequisites, equal exact counts and full PCM equality.
- `native/Vice.Native.proj:31` declares only `vice_x64.dll` as an output. Its inputs at lines 10-15 omit the xvic shim/script. The Windows wrapper currently invokes the x64 script; the existing separate `native/build-vice-shim-xvic.sh` is the repository toolchain surface for the second oracle.

## Inspected source SHA256

- `native/vice-shim.c`: `2326E56017960F3CA138D7D258EB8B4AE2F2803C23F0ACE7638112598953D7F8`
- `native/vice-shim-vic20.c`: `9225463FE0C657EDE249ECAE9167468C9223E779ABB9C0A662BD30C212230197`
- `native/vice/vice/src/vic20/vic20sound.c`: `CB19B0FF27502138046A4DCB777D3365128D69238E97F910F0ECAE2B950B962F`
- `src/ViceSharp.Core/ViceNative.cs`: `CC7A51BE7604FAD49A72764B02F1439B42E3EEB71FCE386B145B610C1363450B`
- `src/ViceSharp.Core/ViceNative.Xvic.cs`: `F758A1F9FF7D98928B3EBA705A38C438449F146D93D7DAED47E8FF195ECFE20B`

These are pre-implementation source hashes. They do not establish clean-patch reproducibility, native DLL identity, runtime success, or completion. Those remain required Slice 1 gates.
