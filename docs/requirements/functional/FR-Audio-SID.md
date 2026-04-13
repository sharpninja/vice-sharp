# FR-Audio-SID: SID Audio Chip Functional Requirements

## Document Information

| Field          | Value                          |
|----------------|--------------------------------|
| Subsystem      | Audio (MOS 6581/8580 SID)      |
| Version        | 0.1.0-draft                    |
| Last Updated   | 2026-04-13                     |

---

## FR-SID-001: Three-Voice Oscillator

**ID:** FR-SID-001
**Title:** Three Independent Voice Oscillators
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The SID chip shall emulate three independent voice channels, each with a 16-bit frequency register, a 16-bit pulse width register, and independent waveform/control selection. Each oscillator generates a 24-bit phase accumulator output that drives waveform generation.

### Acceptance Criteria

1. Each voice (1-3) has an independent 16-bit frequency register (low/high byte pairs at $D400/$D401, $D407/$D408, $D40E/$D40F).
2. The phase accumulator increments by the frequency value each clock cycle.
3. Frequency values translate to output frequency via: F_out = (F_reg * clock_freq) / 16777216.
4. Each voice has a 12-bit pulse width register ($D402/$D403, $D409/$D40A, $D410/$D411).
5. The oscillator output (OSC3 readable at $D41B) reflects the upper 8 bits of voice 3's oscillator.
6. The envelope output (ENV3 readable at $D41C) reflects voice 3's current envelope value.

### Traceability

- **Interfaces:** `IAudioChip`, `IVoice`
- **Test Suite:** `OscillatorFrequencyTests`, `PhaseAccumulatorTests`, `Osc3ReadbackTests`

---

## FR-SID-002: Waveform Generation

**ID:** FR-SID-002
**Title:** Waveform Generation (Triangle, Sawtooth, Pulse, Noise)
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

Each SID voice shall generate four selectable waveforms: Triangle, Sawtooth, Pulse, and Noise. Waveforms are selected by bits 4-7 of the control register ($D404, $D40B, $D412).

### Acceptance Criteria

1. Triangle waveform: 12-bit output derived from the upper bits of the phase accumulator, folded at the midpoint.
2. Sawtooth waveform: 12-bit output is the upper 12 bits of the phase accumulator directly.
3. Pulse waveform: 12-bit output is all-1s when the upper 12 bits of the accumulator are less than the pulse width, else all-0s.
4. Noise waveform: output from a 23-bit LFSR (see FR-SID-009), clocked when bit 19 of the oscillator transitions.
5. When no waveform bit is set, the output is 0.
6. Waveform selection takes effect immediately upon register write.

### Traceability

- **Interfaces:** `IAudioChip`, `IWaveformGenerator`
- **Test Suite:** `TriangleWaveTests`, `SawtoothWaveTests`, `PulseWaveTests`, `NoiseWaveTests`

---

## FR-SID-003: Combined Waveforms

**ID:** FR-SID-003
**Title:** Combined Waveform Output
**Priority:** P1 -- Important
**Iteration:** 2

### Description

When multiple waveform bits are set simultaneously, the SID outputs a combined waveform that is the logical AND of the individual waveform outputs (on the 6581) or a slightly different combination (on the 8580). Combined waveforms produce characteristic thin, metallic sounds used by many SID tunes.

### Acceptance Criteria

1. On the 6581, combined waveforms are computed as the bitwise AND of the selected waveform outputs, with additional analog bleed-through modeled via lookup tables derived from chip analysis.
2. On the 8580, combined waveforms use a different lookup table reflecting the digital die revision behavior.
3. Triangle + Sawtooth, Triangle + Pulse, Sawtooth + Pulse, and Triangle + Sawtooth + Pulse combinations all produce distinct outputs.
4. Noise combined with any other waveform produces the AND of noise LFSR output bits and the other waveform(s), and additionally corrupts the noise LFSR state.
5. Combined waveform lookup tables are configurable (replaceable) via `IWaveformGenerator.SetCombinedWaveformTable()`.

### Traceability

- **Interfaces:** `IAudioChip`, `IWaveformGenerator`
- **Test Suite:** `CombinedWaveform6581Tests`, `CombinedWaveform8580Tests`, `NoiseLfsrCorruptionTests`

---

## FR-SID-004: Filter (6581 Variant)

**ID:** FR-SID-004
**Title:** 6581 SID Filter Emulation
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The 6581 SID filter is a state-variable filter with low-pass, band-pass, and high-pass outputs. The 6581 filter has a non-linear frequency response due to its analog implementation, with a characteristic "warm" sound. The filter cutoff frequency is controlled by an 11-bit value.

### Acceptance Criteria

1. The filter cutoff frequency is set by $D415 (low 3 bits) and $D416 (high 8 bits), forming an 11-bit value.
2. The filter resonance is set by the upper 4 bits of $D417.
3. Low-pass, band-pass, and high-pass modes are selectable via $D418 bits 4-6 and may be combined.
4. Each voice can be individually routed through the filter via $D417 bits 0-2.
5. The external audio input can be routed through the filter via $D417 bit 3.
6. The 6581 filter's non-linear cutoff frequency mapping (the "kinked" curve) is modeled.
7. Filter distortion at high resonance matches the 6581 analog behavior (soft clipping).

### Traceability

- **Interfaces:** `IAudioChip`, `IFilter`
- **Test Suite:** `Filter6581Tests`, `FilterCutoffCurveTests`, `FilterResonanceTests`, `FilterRoutingTests`

---

## FR-SID-005: Filter (8580 Variant)

**ID:** FR-SID-005
**Title:** 8580 SID Filter Emulation
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The 8580 SID filter has a more linear frequency response than the 6581, producing a "cleaner" sound. The filter topology is the same (state-variable) but the analog characteristics differ significantly.

### Acceptance Criteria

1. The 8580 filter cutoff frequency mapping is approximately linear (compared to the 6581's non-linear curve).
2. Filter resonance behavior is less pronounced at extreme settings compared to 6581.
3. The 8580 does not exhibit the same distortion/clipping at high resonance as the 6581.
4. The filter implementation is selected based on the active `IMachineProfile` SID model.
5. Runtime switching between 6581 and 8580 filter models is supported via configuration.

### Traceability

- **Interfaces:** `IAudioChip`, `IFilter`
- **Test Suite:** `Filter8580Tests`, `FilterModelComparisonTests`

---

## FR-SID-006: ADSR Envelope Generator

**ID:** FR-SID-006
**Title:** ADSR Envelope Generator
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

Each SID voice has an ADSR (Attack, Decay, Sustain, Release) envelope generator that shapes the amplitude of the waveform output. The envelope is triggered by the gate bit in the control register. The ADSR bug (where certain attack/decay transitions can cause the envelope to skip to incorrect values) shall be accurately emulated.

### Acceptance Criteria

1. Attack rate is set by the upper 4 bits of $D405/$D40C/$D413 with 16 rate values from 2ms to 8s.
2. Decay rate is set by the lower 4 bits of $D405/$D40C/$D413 with 16 rate values from 6ms to 24s.
3. Sustain level is set by the upper 4 bits of $D406/$D40D/$D414 (0-15 mapped to 0-$FF).
4. Release rate is set by the lower 4 bits of $D406/$D40D/$D414 with the same 16 rates as decay.
5. Setting the gate bit (bit 0 of control register) starts the attack phase.
6. Clearing the gate bit starts the release phase from the current envelope level.
7. The ADSR bug is reproduced: if the envelope counter reaches zero during decay/release and the rate period comparator triggers at the same cycle, the envelope can jump to $FF.
8. The envelope output is an 8-bit value (0-$FF) that multiplies the waveform output.

### Traceability

- **Interfaces:** `IAudioChip`, `IEnvelopeGenerator`
- **Test Suite:** `AdsrTimingTests`, `AdsrBugTests`, `GateToggleTests`, `EnvelopeOutputTests`

---

## FR-SID-007: Ring Modulation

**ID:** FR-SID-007
**Title:** Ring Modulation
**Priority:** P1 -- Important
**Iteration:** 2

### Description

Ring modulation replaces the triangle waveform output of a voice with the product of that voice's triangle output and the MSB of the preceding voice's oscillator (voice 1 modulated by voice 3, voice 2 by voice 1, voice 3 by voice 2).

### Acceptance Criteria

1. Ring modulation is enabled by bit 2 of the control register ($D404/$D40B/$D412).
2. When enabled, the triangle output is XORed with the MSB of the modulating voice's phase accumulator.
3. Ring mod only affects the triangle waveform; other selected waveforms are not modified.
4. The modulating voice does not need to be gated or have its output enabled for ring mod to work.
5. Ring mod and hard sync can be combined on the same voice.

### Traceability

- **Interfaces:** `IAudioChip`, `IVoice`
- **Test Suite:** `RingModTests`, `RingModCombinationTests`

---

## FR-SID-008: Hard Sync

**ID:** FR-SID-008
**Title:** Hard Sync (Oscillator Synchronization)
**Priority:** P1 -- Important
**Iteration:** 2

### Description

Hard sync resets a voice's phase accumulator to zero whenever the modulating voice's phase accumulator MSB transitions from 1 to 0. This produces harmonically rich timbres.

### Acceptance Criteria

1. Hard sync is enabled by bit 1 of the control register ($D404/$D40B/$D412).
2. When the modulating voice's oscillator MSB transitions from 1 to 0, the synced voice's phase accumulator is reset to 0.
3. Voice sync chain: voice 1 synced by voice 3, voice 2 by voice 1, voice 3 by voice 2.
4. The synced voice's frequency determines the harmonic content; the modulating voice's frequency determines the fundamental.
5. Hard sync works with all waveform types.

### Traceability

- **Interfaces:** `IAudioChip`, `IVoice`
- **Test Suite:** `HardSyncTests`, `SyncTimingTests`

---

## FR-SID-009: Noise LFSR

**ID:** FR-SID-009
**Title:** Noise Waveform Linear Feedback Shift Register
**Priority:** P0 -- Critical
**Iteration:** 1

### Description

The noise waveform is generated by a 23-bit Linear Feedback Shift Register (LFSR). The LFSR is clocked when bit 19 of the phase accumulator transitions from 0 to 1. The feedback polynomial matches the original SID chip.

### Acceptance Criteria

1. The LFSR is 23 bits wide with feedback taps at bits 17 and 22 (XOR).
2. The LFSR is clocked when bit 19 of the oscillator's phase accumulator transitions high.
3. The noise output is derived from specific bits of the LFSR (bits 0, 2, 5, 9, 11, 14, 18, 20).
4. Writing to the test bit (bit 3 of control register) resets the LFSR to all-ones.
5. Selecting noise in combination with other waveforms corrupts the LFSR by clearing bits that correspond to zero bits in the other waveform output.

### Traceability

- **Interfaces:** `IAudioChip`, `IWaveformGenerator`
- **Test Suite:** `NoiseLfsrTests`, `NoiseLfsrCorruptionTests`, `TestBitResetTests`

---

## FR-SID-010: Digi Playback ($D418)

**ID:** FR-SID-010
**Title:** Direct Digital Sample Playback via Volume Register
**Priority:** P1 -- Important
**Iteration:** 2

### Description

The SID's master volume register ($D418 bits 0-3) can be used for 4-bit direct digital sample playback by rapidly writing sample values. This technique ("digi" or "$D418 samples") is used extensively in C64 demos and games.

### Acceptance Criteria

1. Writes to $D418 bits 0-3 immediately affect the audio output level.
2. The volume register acts as a 4-bit DAC that adds a DC offset to the mixed output.
3. Rapid writes to $D418 produce audible PCM audio at the write rate.
4. The audio output pipeline has sufficiently low latency that per-rasterline $D418 writes produce clean audio without significant aliasing.
5. Galway/Daglish-style digi playback (NMI-driven 4-bit samples) is clearly audible and recognizable.

### Traceability

- **Interfaces:** `IAudioChip`
- **Test Suite:** `DigiPlaybackTests`, `VolumeRegisterTimingTests`

---

## FR-SID-011: External Audio Input

**ID:** FR-SID-011
**Title:** External Audio Input
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The SID chip has an external audio input (EXT IN) that can be mixed into the output and optionally routed through the filter. This is used by some cartridges and peripherals.

### Acceptance Criteria

1. The external audio input channel can be enabled via $D417 bit 3 (route through filter) or mixed directly.
2. External audio input is accessible via `IAudioChip.SetExternalInput()`.
3. When routed through the filter, the external input is processed identically to the voice outputs.
4. The external input level is correctly scaled relative to the SID voice outputs.

### Traceability

- **Interfaces:** `IAudioChip`
- **Test Suite:** `ExternalAudioInputTests`, `ExternalFilterRoutingTests`

---

## FR-SID-012: Dual-SID Configuration

**ID:** FR-SID-012
**Title:** Dual-SID (Stereo SID) Configuration
**Priority:** P2 -- Enhancement
**Iteration:** 3

### Description

The emulator shall support configurations with two (or more) SID chips at configurable addresses, enabling stereo SID playback as used by many modern SID compositions. Common second-SID addresses are $D420, $D500, and $DE00.

### Acceptance Criteria

1. A second SID chip can be enabled at a configurable address (default $D420).
2. The second SID operates independently with its own set of voices, filters, and envelope generators.
3. Each SID can be configured independently as 6581 or 8580 model.
4. Audio output from each SID can be routed to left/right stereo channels.
5. Third SID support at a third configurable address is available for 3SID tunes.
6. SID address ranges do not overlap with other I/O devices.

### Traceability

- **Interfaces:** `IAudioChip`, `IAddressSpace`
- **Test Suite:** `DualSidTests`, `StereoRoutingTests`, `SidAddressMappingTests`
