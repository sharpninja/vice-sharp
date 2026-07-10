# Ad-Hoc Machine Architecture Schema (v1)

## Purpose

Describes a complete ViceSharp emulated machine as a single YAML (or JSON) document. The schema captures the minimum information needed to assemble a running `IMachine` via `IArchitectureBuilder`:

- machine identity (name, video standard)
- master clock
- 64KB memory map (RAM, ROM; mirrors are expressed by overlapping regions, where later entries shadow earlier ones)
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
      kind: Ram             # Ram | Rom
      start: 0x0000
      end:   0xFFFF
      size:  0x10000        # optional; if present must equal (end-start+1)
    - id: rom-kernal
      kind: Rom
      start: 0xE000
      end:   0xFFFF
      rom:                  # ROM selection (Rom regions only)
        system: C64
        role: kernal
        candidates:         # optional; overrides ViceRomCatalog defaults, ordered
          - kernal-901227-03.bin
          - kernal-901227-02.bin

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
| id              | string    | no       | Stable variant id (e.g. `c64`, `c64c`, `ntsc`, `sx64pal`) |
| name            | string    | yes      | Human readable machine name                            |
| videoStandard   | enum      | yes      | `Pal` or `Ntsc`                                        |
| masterClockHz   | long      | yes      | Master clock in Hz                                     |
| resetVector     | uint16    | no       | Power-on reset vector. Default `0xFFFC`. Accepted for forward compatibility but not yet consumed: the loader binds the value and the builder discards it |

### memory.regions[]

| Field | Type      | Required | Notes                                                                                  |
|-------|-----------|----------|----------------------------------------------------------------------------------------|
| id    | string    | yes      | Unique region id (informational; used in error messages)                               |
| kind  | enum      | yes      | `Ram` (read/write) or `Rom` (read-only)                                                |
| start | uint16    | yes      | Start address (inclusive). Accepts `0xNNNN` hex or decimal                             |
| end   | uint16    | yes      | End address (inclusive)                                                                |
| size  | int       | no       | Optional sanity-check size. If present, must equal `end - start + 1`                   |
| rom   | mapping   | no       | ROM selection for `Rom` regions (see below). Ignored for `Ram`.                        |

Regions may overlap (later entries shadow earlier ones, matching `BasicBus.RegisterDevice` semantics).

#### memory.regions[].rom (Rom regions)

Declares which ROM dump fills a `Rom` region.

| Field      | Type     | Required | Notes                                                                                                                                                                   |
|------------|----------|----------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| system     | string   | yes      | VICE system key: `C64`, `C64DTV`, `C128`, `VIC20`, `PLUS4`, `C16`, `C116`, `PET`, `CBM-II`, `DRIVES`. Selects the `ViceRomCatalog` entry (and its data subdirectory).    |
| role       | string   | yes      | Logical ROM role within the system: `kernal`, `basic`, `chargen`, `kernal64`, `basiclo`, `basichi`, `function-lo`, a drive model, etc.                                   |
| file       | string   | no       | A single, specific ROM filename. Use in a per-variant definition that pins exactly one dump. Mutually exclusive with `candidates`.                                       |
| candidates | string[] | no       | Ordered candidate filenames, most-preferred first. When present, overrides the built-in `ViceRomCatalog` defaults for this machine - this is where a user adds their own dumps and reorders preference. When absent, the catalog defaults for `system`/`role` apply. |

The resolver tries each candidate in order and uses the first that exists and validates, so one definition matches whichever revision a given VICE install ships.

### chips[]

| Field       | Type    | Required | Notes                                                                                  |
|-------------|---------|----------|----------------------------------------------------------------------------------------|
| id          | string  | yes      | Unique chip identifier                                                                 |
| type        | enum    | yes      | One of: `Mos6502`, `Mos6526`, `Mos6569`, `Sid6581`                                     |
| role        | enum    | no       | One of `DeviceRole` values (Cpu, VideoChip, AudioChip, Cia1, Cia2, ...)                |
| baseAddress | uint16  | only if the chip is memory mapped (CIA / VIC / SID) | Where the chip's I/O register window begins  |
| irqLine     | string  | no       | id of an interrupt line this chip drives                                               |
| nmiLine     | string  | no       | id of an NMI line this chip drives                                                     |
| model       | string  | no       | Specific chip die for the variant; `type` stays the generic family. VIC-II: `Mos6569`, `Mos6569R1`, `Mos6567R56A`, `Mos6567R8`, `Mos6572`, `Mos8565`, `Mos8562`. SID: `Mos6581`, `Mos8580`. |
| cyclesPerLine | int   | no       | VIC-II raster timing: cycles per raster line (variant-specific)                        |
| rasterLines | int     | no       | VIC-II raster timing: raster lines per frame (variant-specific)                        |

Required `baseAddress` per type:

- `Mos6526` - required
- `Mos6569` - required
- `Sid6581` - must NOT be specified (SID hard-codes its window at 0xD400-0xD7FF in this iteration)
- `Mos6502` - must NOT be specified (CPU is not address-mapped as a peripheral)

### interruptLines[]

| Field | Type | Required | Notes                            |
|-------|------|----------|----------------------------------|
| id    | string | yes    | Unique line id                   |
| type  | enum   | yes    | `Irq`, `Nmi`, or `Reset`         |

### systemCore (optional, C64 family)

Board / bus configuration for a specific variant. Mirrors `C64SystemCoreDefinition`.

| Field                 | Type   | Required | Notes                                                                                  |
|-----------------------|--------|----------|----------------------------------------------------------------------------------------|
| board                 | string | no       | `Breadbox`, `BreadboxOld`, `C64C`, `Drean`, `SX64`, `PET64`, `Ultimax`, `C64GS`, `Japanese` |
| busPolicy             | string | no       | `Standard`, `Portable`, `Max`, `GameSystem`                                            |
| addressDecoderPolicy  | string | no       | PLA policy: `Standard`, `Ultimax`, `CartridgeRequired`                                 |
| keyboardConnected     | bool   | no       | Keyboard matrix present (false on C64GS)                                               |
| tapePortConnected     | bool   | no       | Datasette port present (false on SX-64 / C64GS)                                        |
| iecBusConnected       | bool   | no       | IEC serial bus present (false on Ultimax / C64GS)                                      |
| cia2Connected         | bool   | no       | Second CIA present (false on Ultimax)                                                  |
| cartridgeBootExpected | bool   | no       | Machine boots from cartridge (Ultimax / C64GS)                                         |

Per-variant machine definitions live under `docs/samples/machines/<id>.machine.yaml` (one per C64 variant), generated from `C64MachineProfiles` by `C64MachineDefinitionWriter` (`src/ViceSharp.Architectures/C64/C64MachineDefinitionWriter.cs`) via `ViceSharp.Console --export-machines docs/samples/machines`. The `id`, `systemCore`, chip `model`/raster, and `rom` fields are forward-compatible: the current loader tolerates them (unknown keys are ignored) until the builder consumes them.

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
