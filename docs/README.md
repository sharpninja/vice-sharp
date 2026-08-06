# ViceSharp Documentation

## User guides

- [USER-GUIDE.md](USER-GUIDE.md) - install, first run, CLI launcher, machine YAML, disk images, capture, diagnostics attach, what works today
- [VICE-MIGRATION.md](VICE-MIGRATION.md) - swap classic VICE for ViceSharp: binary + flag mapping, behaviour caveats, bug compatibility
- [ROMs.md](ROMs.md) - legal ROM options, environment variable, expected directory layout
- [PRIVACY.md](PRIVACY.md) - privacy policy (store listing source)

## Architecture and Design

- [Architecture.md](Architecture.md) - POCO model, MVVM, mutation queue, device model, architectures
- [Public-API.md](Public-API.md) - 33+ public interfaces with XML doc summaries
- [StateWindow.md](StateWindow.md) - Configuration surface, presets, memory math
- [Decoupling.md](Decoupling.md) - Video and audio decoupling, refresh strategies
- [PubSub.md](PubSub.md) - Pool, arena, handle lifecycle, refcount, hot-path cost

## Reference

- [ROMs.md](ROMs.md) - Legal ROM options, environment variable setup
- [ROL.md](ROL.md) - Registry of Lore (122+ entries)
- [AI-Review.md](AI-Review.md) - aiUnit AI Code Review + Project Review tests, and running them with the Grok Build CLI
- [wiki.yaml](wiki.yaml) - wiki export manifest (`mcp-wiki-export/v1`)

## Iteration Plans

- [Iteration-00-Foundations.md](Iteration-00-Foundations.md) - Scaffolding, interfaces, source generator, CI/CD
- [Iteration-Roadmap.md](Iteration-Roadmap.md) - Full roadmap through MVP (Iteration 2 VIC-20 includes 10 s PAL/NTSC every-cycle lockstep)

## Lockstep receipts (durable)

- [receipts-lockstep-10s-2026-08-06.txt](receipts-lockstep-10s-2026-08-06.txt) - VIC-20 PAL 10 s every-cycle A/X/Y/S/P/PC vs xvic
- [receipts-lockstep-10s-ntsc-2026-08-06.txt](receipts-lockstep-10s-ntsc-2026-08-06.txt) - VIC-20 NTSC 10 s (soft-BIT / VIA bus-visible realign)
- [receipts-lockstep-vice-realign-2026-08-06.txt](receipts-lockstep-vice-realign-2026-08-06.txt) - VICE realign notes (V-bus, color RAM, order)

Ephemeral intermediate probe logs (`docs/*-focused-*.log`, `docs/2s-*.log` from debug slices) are not wiki content; prefer durable receipts above.

## Continuity

- [HANDOFF.md](../HANDOFF.md) (repo root) - canonical session continuity; do not use retired `docs/handoff.md`

## Session Logs

MCP session logs live in the MCP Server store (not on-disk under this tree for active work). Historical files under `session-logs/` if present are archival only.
