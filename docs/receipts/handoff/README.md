# Handoff receipts index

Combined index of the handoff receipt folders. Both folders are retained unchanged because their receipts are hash-pinned and referenced by path (including from MCP TODO entries). The root [HANDOFF.md](../../../HANDOFF.md) is the sole current handoff; everything here is historical evidence.

## 1. Canonical reconciliation: 2026-10-11

Folder: [20261011T112946-0500-canonical-reconciliation](20261011T112946-0500-canonical-reconciliation/README.md)

Corrected stale handoff and MCP progress text. No R0 acceptance, requirement apply, implementation or gate change.

- `HANDOFF.before.md`: former 76,615-character handoff, SHA256 `B7A13C69...E909E8`.
- `goal-plan.reference.md`: byte-identical copy of the external goal plan, SHA256 `436BB2B1...6473`.
- `state-before.json`, `todo-updates.json`, `verification.json`: MCP TODO preimages, four progress-text updates, document checks.

## 2. Handoff sync and checkpoint: 2026-10-16

Folder: [20261016T092300-handoff-sync](20261016T092300-handoff-sync/README.md)

Operational preservation checkpoint: replaced and consolidated the canonical handoff, then committed and synced origin. No implementation, requirement apply or gate change.

- `HANDOFF.before.md`: prior root handoff, SHA256 `EA2737B0...9579`. `HANDOFF.origin-main-6530196.md`: origin main version.
- `exclusions.json`: 465 retained local files excluded from Git, with reasons and hashes.
- `Inspect-Checkpoint.ps1`, `Inspect-ArchiveCredentials.ps1`: value-free screening scripts.
- `validation.json`, `merge-validation.json`: validation results.

## How they relate

The Oct 16 checkpoint supersedes the Oct 11 handoff text. The goal plan hash `436BB2B1...6473` is unchanged in both. Latest R0 Opus review is AGREE 99/98 (see `docs/receipts/hv/20261013T232143-r0-anchor-opus/`); development stays paused.
