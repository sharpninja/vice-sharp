# ViceSharp

A C# port of [VICE](https://vice-emu.sourceforge.io/) (Versatile Commodore Emulator) targeting .NET 10 with NativeAOT support.

## Status

**Iteration 0 (Foundations)** — scaffolding, interfaces, source generator, and CI/CD. No emulation yet.

## Supported Machines (planned)

| Machine | Architecture | Status |
|---------|-------------|--------|
| C64 / C64C | MOS 6510 + VIC-II + SID + CIA x2 | Iteration 1 |
| SX-64 | Same as C64 (built-in monitor + 1541) | Iteration 1 |
| VIC-20 | MOS 6502 + VIC + VIA x2 | Iteration 2 |
| C128 | MOS 8502 + VIC-IIe + SID + CIA x2 + Z80 | Iteration 3 |
| PET | MOS 6502 + PIA/VIA + CRTC | Iteration 4 |
| Plus/4 / C16 | MOS 7501 + TED | Iteration 5 |

## Building

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.201 or later)

```bash
# Restore and build
dotnet build ViceSharp.slnx

# Run tests
dotnet test ViceSharp.slnx

# Using Nuke build system
./build.sh Compile    # Linux/macOS
build.cmd Compile     # Windows
```

### Nuke Targets

| Target | Description |
|--------|-------------|
| `Clean` | Remove bin/obj/artifacts |
| `Restore` | Restore NuGet packages |
| `Compile` | Build with TreatWarningsAsErrors |
| `Test` | Run unit tests (excludes determinism) |
| `DeterminismTest` | Run determinism verification tests |
| `PublishAot` | Publish NativeAOT console app |
| `CiAzure` | Full Azure DevOps CI pipeline |
| `CiGitHub` | Full GitHub Actions CI pipeline |

## Architecture

ViceSharp is designed as a **library-first emulator**:

- **ViceSharp.Abstractions** — 33+ public interfaces defining the emulator contract
- **ViceSharp.Core** — bus, clock, devices, mutation queue, pub/sub
- **ViceSharp.Chips** — CPU (6502/6510/8502), VIC-II, SID, CIA, VIA, PLA
- **ViceSharp.Architectures** — machine definitions (C64, VIC-20, C128, PET, Plus/4)
- **ViceSharp.SourceGen** — Roslyn source generator for device registration boilerplate
- **ViceSharp.Console** — NativeAOT reference shell
- **ViceSharp.Avalonia** — Avalonia 12.x desktop UI

Key design principles:
- **Zero allocation hot path** — per-cycle emulation allocates nothing
- **POCO model** — all state is plain C# structs/records, no base classes
- **Mutation queue** — all state changes flow through an auditable queue
- **Deterministic** — bit-exact replay given identical inputs
- **NativeAOT compatible** — no reflection on the hot path

See [docs/Architecture.md](docs/Architecture.md) for the full design.

## ROMs

ViceSharp does not include Commodore ROMs. See [docs/ROMs.md](docs/ROMs.md) for legal ROM options and setup instructions.

## License

Copyright (c) 2026 ViceSharp Contributors.

Licensed under the **GNU General Public License v2.0 or later** (GPL-2.0-or-later). See [COPYING](COPYING) for the full license text.

ViceSharp is a derivative work of VICE, which is also licensed under GPL-2.0-or-later. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for attribution details.

## Contributing

1. Fork on Azure DevOps (`dev.azure.com/McpServer/VICE-Sharp`)
2. Follow the Byrd Development Process: tests first, then implementation
3. All tests must pass before submitting a PR
4. NativeAOT compatibility is required for all non-test assemblies
