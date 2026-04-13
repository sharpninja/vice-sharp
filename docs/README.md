# ViceSharp Documentation

## Architecture and Design

- [Architecture.md](Architecture.md) — POCO model, MVVM, mutation queue, device model, architectures
- [Public-API.md](Public-API.md) — 33+ public interfaces with XML doc summaries
- [StateWindow.md](StateWindow.md) — Configuration surface, presets, memory math
- [Decoupling.md](Decoupling.md) — Video and audio decoupling, refresh strategies
- [PubSub.md](PubSub.md) — Pool, arena, handle lifecycle, refcount, hot-path cost

## Reference

- [ROMs.md](ROMs.md) — Legal ROM options, environment variable setup
- [ROL.md](ROL.md) — Registry of Lore (122+ entries)

## Iteration Plans

- [Iteration-00-Foundations.md](Iteration-00-Foundations.md) — Scaffolding, interfaces, source generator, CI/CD
- [Iteration-Roadmap.md](Iteration-Roadmap.md) — Full roadmap through MVP

## Diagrams

Mermaid class diagrams in `diagrams/` are the canonical visual reference for ViceSharp's type system. They are additive: each implementation phase extends the existing diagrams.

| File | Scope |
|------|-------|
| `diagrams/core-interfaces.mmd` | IBus, IClockedDevice, IInterruptLine, core value types |
| `diagrams/system-devices.mmd` | ISystem, IMachine, IDevice hierarchy, peripherals |
| `diagrams/architecture.mmd` | IArchitecture, builders, validators, GenericMachine |
| `diagrams/services.mmd` | IRomProvider, IAudioBackend, IFrameSink, media capture |
| `diagrams/monitor.mmd` | IMonitor, IMonitorTransport, IMonitorEventStream, ISnapshot |
| `diagrams/pubsub-mutations.mmd` | IPubSub, IMutationQueue, IMessagePool, PayloadArena |
| `diagrams/chips.mmd` | CPU, VIC-II, SID, CIA, VIA, PLA, memory chips |
| `diagrams/apps-hosting.mmd` | Reference shells, hosting, controls |

## Session Logs

Implementation session logs are stored in `session-logs/`.
