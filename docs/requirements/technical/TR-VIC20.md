# VIC-20 Technical Requirements

## Document Information

| Field | Value |
|---|---|
| Subsystem | VIC-20 flash cartridges and sound |
| Version | 1.0.0 |
| Last Updated | 2026-08-17 |
| Status | Implemented, scoped |

## TR-VIC20-FLASH-001: Cycle-driven TYPE_B flash and atomic persistence

**ID:** TR-VIC20-FLASH-001
**Title:** VICE-compatible AM29F040B timing and safe expansion write-back

`Flash040Core` implements the VICE TYPE_B command state machine and exposes `AdvanceCycles` so the owning VIC-20 machine clock controls completion. Sector timeout, sector erase, and chip erase use 50, 1,000,000, and 8,000,000 cycles respectively. Busy status, cancel, suspend, and resume are deterministic. The expansion port reports dirty FE3/Ultimem flash and Mega-Cart NVRAM separately. Host detach writes a same-directory temporary file and atomically replaces the target only after a complete write.

**Related FR:** FR-VIC20-005
**Verification:** TEST-VIC20-FLASH-001

---

## TR-VIC20-SOUND-001: Deterministic clocked PCM batching

**ID:** TR-VIC20-SOUND-001
**Title:** VIC-I sound advances on the machine clock and batches PCM without hot-path allocation

`Mos6561` owns one machine-agnostic `Vic20Sound` engine. Sound-register stores mutate oscillator state without allocating after warmup. System-clock advancement produces deterministic PCM16 mono and dispatches fixed 256-sample batches to the configured `IAudioBackend`. Native xvic comparison is scoped to the covered silence and tone inputs; the complete waveform/input space remains Partial.

**Related FR:** FR-VIC20-SOUND-001
**Verification:** TEST-VIC20-SOUND-001
