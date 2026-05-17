# Ad-Hoc Machine Architecture Schema (v1)

## Purpose

Describes a complete ViceSharp emulated machine as a single YAML (or JSON) document. The schema captures the minimum information needed to assemble a running `IMachine` via `IArchitectureBuilder`:

- machine identity (name, video standard)
- master clock
- 64KB memory map (RAM, ROM, mirrors)
- chip catalogue (CPU + peripheral chips, base addresses, interrupt wiring)
- optional reset port settings

The schema deliberately models only the surface area that the existing C64 builder uses today; future iterations can extend it (cartridges, expansion ports, datasette, etc.) without breaking v1.

## Schema Version

```yaml
schemaVersion: 1
```

`schemaVersion` is required and must equal `1` for the current loader.

## Top-Level Document

```yaml
schemaVersion: 1
machine:
  name: "Commodore 64 PAL"
  videoStandard: Pal        # Pal | Ntsc
  masterClockHz: 985248
  resetVector: 0xFFFC       # optional, defaults to 0xFFFC

memory:
  regions:
    - id: ram-main
      kind: Ram             # Ram | Rom | Mirror
      start: 0x0000
      end:   0xFFFF
      size:  0x10000        # optional; if present must equal (end-start+1)

chips:
  - id: cpu
    type: Mos6502           # Mos6502 | Mos6526 | Mos6569 | Sid6581
    role: Cpu               # optional IDeviceRegistry role
  - id: vic
    type: Mos6569
    role: VideoChip
    baseAddress: 0xD000
    irqLine: irq            # optional, references an interrupt line id

interruptLines:
  - id: irq
    type: Irq               # Irq | Nmi | Reset
  - id: nmi
    type: Nmi
```

## Field Reference

### machine

| Field           | Type      | Required | Notes                                                  |
|-----------------|-----------|----------|--------------------------------------------------------|
| name            | string    | yes      | Human readable machine name                            |
| videoStandard   | enum      | yes      | `Pal` or `Ntsc`                                        |
| masterClockHz   | long      | yes      | Master clock in Hz                                     |
| resetVector     | uint16    | no       | Power-on reset vector. Default `0xFFFC`               |

### memory.regions[]

| Field | Type      | Required | Notes                                                                                  |
|-------|-----------|----------|----------------------------------------------------------------------------------------|
| id    | string    | yes      | Unique region id (informational; used in error messages)                               |
| kind  | enum      | yes      | `Ram` (read/write) or `Rom` (read-only)                                                |
| start | uint16    | yes      | Start address (inclusive). Accepts `0xNNNN` hex or decimal                             |
| end   | uint16    | yes      | End address (inclusive)                                                                |
| size  | int       | no       | Optional sanity-check size. If present, must equal `end - start + 1`                   |

Regions may overlap (later entries shadow earlier ones, matching `BasicBus.RegisterDevice` semantics).

### chips[]

| Field       | Type    | Required | Notes                                                                                  |
|-------------|---------|----------|----------------------------------------------------------------------------------------|
| id          | string  | yes      | Unique chip identifier                                                                 |
| type        | enum    | yes      | One of: `Mos6502`, `Mos6526`, `Mos6569`, `Sid6581`                                     |
| role        | enum    | no       | One of `DeviceRole` values (Cpu, VideoChip, AudioChip, Cia1, Cia2, ...)                |
| baseAddress | uint16  | only if the chip is memory mapped (CIA / VIC / SID) | Where the chip's I/O register window begins  |
| irqLine     | string  | no       | id of an interrupt line this chip drives                                               |
| nmiLine     | string  | no       | id of an NMI line this chip drives                                                     |

Required `baseAddress` per type:

- `Mos6526` — required
- `Mos6569` — required
- `Sid6581` — must NOT be specified (SID hard-codes its window at 0xD400-0xD7FF in this iteration)
- `Mos6502` — must NOT be specified (CPU is not address-mapped as a peripheral)

### interruptLines[]

| Field | Type | Required | Notes                            |
|-------|------|----------|----------------------------------|
| id    | string | yes    | Unique line id                   |
| type  | enum   | yes    | `Irq`, `Nmi`, or `Reset`         |

## Validation Rules

- `schemaVersion` must be present and equal `1`.
- `machine.name`, `machine.videoStandard`, `machine.masterClockHz` are required.
- At least one memory region is required.
- Each region's `start` must be `<= end`.
- Optional `size` must match `end - start + 1` when present.
- Each chip `id` must be unique.
- Each chip `type` must be one of the supported values.
- Chips that require `baseAddress` MUST supply it.
- `irqLine` / `nmiLine` references must match a declared `interruptLines[].id`.
- Hex literals (`0xNNNN`) are supported anywhere a numeric address is expected.

## Loader Contract

`AdhocMachineYamlLoader.LoadFromFile(path)` and `LoadFromString(yaml)` both return an `AdhocMachineBlueprint`, a fully validated plan that:

1. Exposes an `IArchitectureDescriptor` built from `machine.*`.
2. Provides `BuildMachine(IArchitectureBuilder builder)` which creates the bus, clock, devices, RAM/ROM regions, and chips described in the YAML, then returns the resulting `IMachine`.

Any validation failure throws `AdhocMachineValidationException` with a message that names the offending field path (e.g., `chips[1].baseAddress`).
