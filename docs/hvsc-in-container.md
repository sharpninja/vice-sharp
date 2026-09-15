# HVSC in the attached library

The High Voltage SID Collection lives on **host attached storage** under the RomM library parent so NFO `SID:` paths resolve at runtime.

## Layout

| Host (attached) | Container | Role |
|-----------------|-----------|------|
| `runtime/library/` | `/romm/library` | Structure A library parent (single bind) |
| `runtime/library/hvsc/` | `/romm/library/hvsc` | HVSC root for `SID:` metadata |
| `runtime/library/hvsc/MUSICIANS/` | same | Composer tree |
| `runtime/library/hvsc/GAMES/` | same | Game-music tree |
| `runtime/library/hvsc/DEMOS/` | same | Demo music tree |

HVSC is **not** under `roms/` (avoids treating `MUSICIANS/` as multi-file games).

## Map NFO → file

```text
SID:  MUSICIANS\W\Whittaker_David\180.sid

Host:      runtime/library/hvsc/MUSICIANS/W/Whittaker_David/180.sid
Container: /romm/library/hvsc/MUSICIANS/W/Whittaker_David/180.sid
```

```powershell
.\scripts\Download-Hvsc.ps1   # default: runtime/library/hvsc
.\scripts\Resolve-SidPath.ps1 'MUSICIANS\W\Whittaker_David\180.sid'
```

Env `HVSC_ROOT` defaults to `/romm/library/hvsc` in containers.

## Why attached library storage

Metadata stores **HVSC-relative** paths only. At runtime the resolver prefixes `HVSC_ROOT`. That root must be on the bind-mounted library volume, not inside an ephemeral container layer and not only on an unmounted `./gb64` tree.
