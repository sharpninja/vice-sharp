# Preexisting Native VICE Delta Preservation Receipt

Captured: 2026-10-04T08:58:55-05:00

This receipt preserves the preexisting modified native VICE checkout before any restoration. It does not authorize or perform a change under `native/vice`.

- Superproject gitlink: `cc67418cba58e71d8e70fd353e9de47199ad2075`
- Checked-out native commit: `cc67418cba58e71d8e70fd353e9de47199ad2075`
- Modified tracked entries: 12
- Untracked entries: 2,527
- Tracked patch: `native-vice-tracked.patch`
- Tracked patch SHA-256: `90604E39B827F850A382DC13DD921141290EF2C34E1483CDC6C612A07803E9E2`
- Preserved injected header: `vice-shim-runtime.h.preserved`
- Preserved injected header SHA-256: `5450FDA8CFEDB8A22BADA8CD04EE75FF830C48796592FF9EC0141E899D846259`
- Full untracked inventory: `native-vice-untracked-paths.txt`

## Tracked delta summary

```text
 vice/src/c64/c64cpusc.c         |  25 +++++++++
 vice/src/drive/drive-snapshot.c |  10 ++--
 vice/src/drive/drivecpu.c       |   4 ++
 vice/src/drive/drivecpu65c02.c  |   4 ++
 vice/src/mainc64cpu.c           |  81 +++++++++++++++++++++++++++--
 vice/src/mainviccpu.c           |  32 ++++++++++++
 vice/src/resid/filter8580new.h  |  10 ++++
 vice/src/resid/sid.h            |   4 ++
 vice/src/sid/resid.cc           | 109 ++++++++++++++++++++++++++++++++++++++++
 vice/src/vic20/vic20cpu.c       |  23 +++++++++
 vice/src/vic20/vic20sound.c     | 102 ++++++++++++++++++++++++++++++++++++-
 vice/src/vic20/vic20sound.h     |   5 ++
 12 files changed, 399 insertions(+), 10 deletions(-)
```

## Acceptance limitation

Any binary or test result produced from this modified checkout is diagnostic only. It cannot prove ViceSharp parity against immutable VICE.