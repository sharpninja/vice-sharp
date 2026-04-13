# Registry of Lore (ROL)

A knowledge base of technical facts about Commodore hardware, VICE emulation behavior, and implementation details relevant to ViceSharp.

---

## CPU — MOS 6502/6510/8502

### ROL-001: 6510 I/O Port at $0000/$0001
**Category:** CPU
**Source:** C64 Programmer's Reference Guide

The 6510 has a built-in I/O port at addresses $0000 (data direction register) and $0001 (data register). Bits 0-2 control the PLA bank switching lines (LORAM, HIRAM, CHAREN). This port does not exist on the base 6502.

### ROL-002: Undocumented Opcodes
**Category:** CPU
**Source:** Graham's 6502 Opcode Matrix, Codebase64

The 6502 has 151 documented opcodes and 105 undocumented ones. Many undocumented opcodes perform combinations of documented operations (e.g., LAX = LDA + LDX). Some are unstable and depend on analog circuit behavior. Commercial software (especially copy protection) uses these opcodes.

### ROL-003: Page-Crossing Penalty
**Category:** CPU / Timing
**Source:** MOS 6502 Programming Manual

Indexed addressing modes (abs,X; abs,Y; (ind),Y) incur an extra cycle when the index addition crosses a page boundary (high byte changes). Read-modify-write instructions always take the extra cycle regardless.

### ROL-004: Interrupt Hijacking
**Category:** CPU
**Source:** Visual 6502 analysis

If an NMI arrives during the first two cycles of a BRK instruction, the BRK vector ($FFFE) is replaced by the NMI vector ($FFFA) but the B flag is still set. Similarly, if an IRQ arrives during BRK, the interrupt is "hijacked." VICE models this accurately.

### ROL-005: BCD Mode on CMOS vs NMOS
**Category:** CPU
**Source:** 6502.org

The NMOS 6502/6510 does not set the N, V, and Z flags correctly after BCD arithmetic (ADC/SBC in decimal mode). The CMOS 65C02 fixes this and adds an extra cycle. ViceSharp must model the NMOS behavior for C64 accuracy.

### ROL-006: RDY Line and DMA
**Category:** CPU / Timing
**Source:** VIC-II article by Christian Bauer

The VIC-II pulls the RDY line low to halt the CPU during DMA cycles (sprite fetch, refresh, badlines). The CPU finishes the current read cycle, then stalls. Write cycles cannot be stalled — the CPU completes them before honoring RDY.

### ROL-007: Cycle-Exact Behavior of RMW Instructions
**Category:** CPU / Timing
**Source:** VICE source code, Visual 6502

Read-Modify-Write instructions (ASL, LSR, ROL, ROR, INC, DEC) perform: read → write-original → write-modified. The intermediate write-original is visible on the bus and affects I/O registers (notably VIC-II and SID).

### ROL-008: JMP ($xxFF) Indirect Bug
**Category:** CPU
**Source:** 6502.org

JMP indirect with the pointer at a page boundary ($xxFF) fetches the low byte from $xxFF and the high byte from $xx00 (wraps within the page) instead of $xx00+$0100. This is an NMOS bug fixed in the 65C02.

### ROL-009: Stack Page Wrapping
**Category:** CPU / Memory
**Source:** 6502.org

The stack pointer wraps within page 1 ($0100-$01FF). Pushing when SP=$00 writes to $0100 and SP becomes $FF. Pulling when SP=$FF reads from $0100 (after incrementing SP to $00, then reading $0100).

### ROL-010: IRQ and NMI Timing
**Category:** CPU / Timing
**Source:** Visual 6502

IRQ is level-triggered and sampled on the penultimate cycle of each instruction. NMI is edge-triggered (falling edge). If the I flag is set, IRQ is ignored. NMI cannot be masked. The interrupt sequence takes 7 cycles.

---

## Video — VIC-II (6567/6569)

### ROL-011: Badlines
**Category:** Video / Timing
**Source:** Christian Bauer's VIC-II article

A badline occurs when the lower 3 bits of RASTER match the lower 3 bits of $D011 (YSCROLL) AND the DEN bit is set AND we're in the visible area (lines 48-247). During a badline, VIC-II steals 40 bus cycles from the CPU to fetch character pointers.

### ROL-012: Sprite DMA Stealing
**Category:** Video / Timing
**Source:** Christian Bauer's VIC-II article

Each enabled sprite with its Y-coordinate matching the current raster line triggers a 2-cycle DMA access (p-access + 3 s-accesses = 2 bus cycles stolen). Up to 8 sprites can steal up to 16 cycles per line. Sprites are checked on specific cycles (55-62 for sprite 0-7).

### ROL-013: Border Unit and Border Opens
**Category:** Video
**Source:** Codebase64, VIC-II article

The VIC-II has separate horizontal and vertical border flip-flops. By changing $D011/$D016 at precisely timed moments, the border can be "opened" (FLD technique for vertical, 38/40 column switch for horizontal), revealing the area normally hidden by the border color.

### ROL-014: VSP Bug (VIC-II Spike)
**Category:** Video / Quirk
**Source:** VICE bugtracker, C64 demo scene

Changing YSCROLL ($D011 bits 0-2) between specific cycles can trigger a VIC-II lockup known as the VSP (Variable Screen Position) bug. The VIC-II's internal counter gets corrupted, causing a visible "spike" or system hang. Different VIC-II revisions have different susceptibility.

### ROL-015: FLI (Flexible Line Interpretation)
**Category:** Video
**Source:** Codebase64

FLI uses IRQ-driven writes to $D018 (character set/screen memory pointer) on every badline to achieve per-line color changes beyond the normal 1000 character cells. The first 3 characters of each row show garbage due to the timing window.

### ROL-016: AGSP (Advanced Graphic Soft Scroll Positioning)
**Category:** Video
**Source:** C64 demo scene

AGSP combines DMA delay (sprite stretching/multiplexing) with YSCROLL changes to create smooth scrolling effects that would otherwise be impossible within VIC-II's normal constraints.

### ROL-017: Lightpen Latching
**Category:** Video / I/O
**Source:** VIC-II datasheet

The VIC-II latches the current raster position to $D013/$D014 on a negative edge of the LP input (directly from control port 1, active low). The latch triggers once per frame and has an X resolution of 2 pixels (due to VIC-II internal clock doubling).

### ROL-018: Sprite Multicolor Mode
**Category:** Video
**Source:** C64 PRG

In multicolor mode, sprites use pixel pairs (2 bits) giving 3 colors plus transparent, at half horizontal resolution (24x21 effective pixels instead of 24x21). Individual sprites can mix hires and multicolor mode.

### ROL-019: VIC-II Color RAM
**Category:** Video / Memory
**Source:** C64 schematic

Color RAM at $D800-$DBFF is only 4 bits wide (1 nibble per cell). The upper 4 bits read as undefined on real hardware. ViceSharp should model this — reading $D800 returns (color_nibble | random_upper_nibble).

### ROL-020: PAL vs NTSC Timing
**Category:** Video / Timing
**Source:** VIC-II article

PAL (6569): 312 lines, 63 cycles/line, 985248 Hz clock, 50 Hz frame rate.
NTSC (6567R8): 263 lines, 65 cycles/line, 1022727 Hz clock, ~59.826 Hz frame rate.
Earlier NTSC (6567R56A): 262 lines, 64 cycles/line.

### ROL-021: VIC-II Phi1/Phi2 Bus Access
**Category:** Video / Timing
**Source:** VIC-II article

The VIC-II and CPU share the bus via alternating clock phases. During Phi1, VIC-II reads character pointers and sprite data. During Phi2, the CPU accesses the bus (unless VIC-II has stolen the cycle via BA/AEC).

### ROL-022: Sprite-to-Sprite and Sprite-to-Background Collision
**Category:** Video
**Source:** VIC-II datasheet

The VIC-II detects pixel-level collisions and sets bits in $D01E (sprite-sprite) and $D01F (sprite-background). Reading these registers clears them. Collisions are detected regardless of sprite/background priority.

### ROL-023: VIC-II Idle State
**Category:** Video
**Source:** VIC-II article

When DEN is cleared or outside the display window, VIC-II enters idle state and reads from $3FFF (last byte of the current VIC bank). Demos exploit this behavior for effects.

### ROL-024: VIC-II Bank Switching
**Category:** Video / Memory
**Source:** C64 PRG

CIA2 port A bits 0-1 select which 16KB bank the VIC-II sees. Bank 0: $0000-$3FFF, Bank 1: $4000-$7FFF, Bank 2: $8000-$BFFF, Bank 3: $C000-$FFFF. The VIC-II cannot see character ROM in banks 1 and 3.

### ROL-025: Raster IRQ
**Category:** Video
**Source:** C64 PRG

$D012 (and bit 7 of $D011) set the raster compare value. When the current raster line matches, the VIC-II triggers an IRQ (if enabled via $D01A bit 0). This is the primary timing mechanism for demo effects.

---

## Audio — SID (6581/8580)

### ROL-026: Combined Waveforms
**Category:** Audio
**Source:** Bob Yannes interview, resid documentation

When multiple waveform bits are set simultaneously (e.g., triangle + sawtooth), the SID ANDs the waveform outputs. This produces characteristic sounds used in many C64 tunes. The exact combination output differs between 6581 and 8580.

### ROL-027: Filter Distortion — 6581 vs 8580
**Category:** Audio
**Source:** resid-fp documentation

The 6581 has a filter that distorts at high resonance due to opamp saturation, producing a characteristic "warm" sound. The 8580 uses a different filter design with cleaner response. resid-fp models both variants with analog-accurate filter simulation.

### ROL-028: ADSR Bug
**Category:** Audio / Quirk
**Source:** SID documentation, VICE source

The SID's ADSR envelope has a bug: if the attack/decay/release rate is changed while the envelope is at a non-zero step, the counter comparison can fail, causing the envelope to skip to the next phase. Some C64 music intentionally exploits this.

### ROL-029: Test Bit (Bit 3 of Control Register)
**Category:** Audio
**Source:** SID documentation

Setting the test bit resets and locks the oscillator at zero. Clearing it releases the oscillator from its current phase. This is used for synchronization effects and digi playback techniques.

### ROL-030: Digi Playback Techniques
**Category:** Audio
**Source:** Codebase64

The SID can play digital samples through several techniques: volume register ($D418) writes for 4-bit PCM, pulse width modulation, and combined waveform tricks. The $D418 method produces audible clicks due to the DAC design.

### ROL-031: SID Voice 3 Readback
**Category:** Audio
**Source:** SID datasheet

$D01B reads the current oscillator output of voice 3, and $D01C reads the envelope output of voice 3. These are used for random number generation and as a modulation source.

### ROL-032: SID Filter Cutoff Range
**Category:** Audio
**Source:** SID datasheet

The filter cutoff frequency is set by $D015-$D016 (11 bits). On the 6581, the actual cutoff range is approximately 220 Hz to 18 kHz. On the 8580, it's approximately 30 Hz to 12 kHz. The response curve is nonlinear.

### ROL-033: Ring Modulation
**Category:** Audio
**Source:** SID datasheet

Ring modulation replaces the triangle waveform of one voice with the product of that triangle and the oscillator of the adjacent voice (1←3, 2←1, 3←2). This creates metallic, bell-like tones.

### ROL-034: Hard Sync
**Category:** Audio
**Source:** SID datasheet

Hard sync resets one oscillator when the adjacent oscillator completes a cycle. This creates harmonically rich timbres that vary with the synced oscillator's frequency.

### ROL-035: SID Paddle Readout
**Category:** Audio / I/O
**Source:** SID datasheet

$D019/$D01A read the analog paddle (potentiometer) values. The SID charges a capacitor through the paddle resistance and measures the charge time. Reading happens every 512 cycles. Paddles 1-2 on port 1, paddles 3-4 on port 2.

---

## I/O — CIA (6526)

### ROL-036: CIA Timer Cascading
**Category:** I/O
**Source:** CIA datasheet

Timer B can be configured to count Timer A underflows (cascade mode). This creates a 32-bit counter for precise long-duration timing. The cascade mode is controlled by bits 5-6 of control register B ($DC0F/$DD0F).

### ROL-037: TOD Clock
**Category:** I/O
**Source:** CIA datasheet

The CIA's Time-of-Day clock counts in BCD format (1/10 seconds, seconds, minutes, hours with AM/PM). It's clocked by the 50/60 Hz power line frequency (directly from pin 25). Writing to the hours register latches the display until the tenths register is written.

### ROL-038: CIA Serial Port Bit-Banging
**Category:** I/O
**Source:** CIA datasheet

The CIA serial port ($DC0C) can transmit 8 bits at a rate derived from Timer A. The IEC serial bus (C64 to 1541) uses the CIA's port B bits for CLK and DATA lines (directly bit-banged, not using the serial shift register).

### ROL-039: Keyboard Matrix Ghosting
**Category:** I/O / Quirk
**Source:** C64 service manual

The C64 keyboard is a passive matrix scanned via CIA1 ports A and B. When multiple keys are pressed that form a rectangle in the matrix, ghost key presses appear. The OS ROM handles this by checking for three simultaneous keys in a row/column.

### ROL-040: CIA ICR Acknowledge Race
**Category:** I/O / Timing
**Source:** VICE source, C64 community

Reading the CIA ICR ($DC0D/$DD0D) clears all interrupt flags AND acknowledges the interrupt. If an interrupt arrives on the exact cycle of the read, there's a one-cycle window where the interrupt can be lost. VICE models this race condition.

### ROL-041: CIA Timer Force Load
**Category:** I/O
**Source:** CIA datasheet

Setting bit 4 of the control register forces the timer latch value to be loaded into the timer counter immediately. The timer does not start running until bit 0 is also set.

### ROL-042: CIA2 and NMI
**Category:** I/O
**Source:** C64 schematic

CIA2 ($DD00-$DD0F) is connected to the NMI line. Timer underflows, TOD alarms, and serial port interrupts from CIA2 generate NMI (non-maskable interrupt). The RESTORE key is also connected to NMI via CIA2.

---

## I/O — VIA (6522)

### ROL-043: VIA Timer Behavior Differences
**Category:** I/O
**Source:** VIA datasheet, VIC-20 documentation

The VIA (used in VIC-20 and disk drives) has timers with different behavior from CIA timers. VIA Timer 1 can operate in free-running mode (auto-reload) and generates an interrupt each time it underflows. Timer 2 is one-shot only.

### ROL-044: VIA Shift Register
**Category:** I/O
**Source:** VIA datasheet

The VIA's shift register can shift data in or out under timer control or external clock. The 1541 drive uses this for IEC serial communication. The VIA shift register has known bugs in some chip revisions.

### ROL-045: VIA Port B Pulse Mode
**Category:** I/O
**Source:** VIA datasheet

VIA Timer 1 can toggle PB7 (port B bit 7) on each underflow, generating a square wave. This is used in the VIC-20 for audio output and in disk drives for timing.

---

## Memory and PLA

### ROL-046: PLA Propagation Delay
**Category:** Memory / Timing
**Source:** C64 schematic analysis

The PLA (906114) has propagation delays of approximately 30-60ns depending on the chip revision. This can cause brief "glitches" in bank switching that are visible to the VIC-II but not the CPU (due to different clock phase access).

### ROL-047: C64C vs Breadbin PLA Differences
**Category:** Memory / Architecture
**Source:** C64 community wiki

The original breadbin C64 uses a mask-programmed PLA (906114). Later C64C boards use a replacement (251715) or discrete logic. Some replacement PLAs have slightly different timing characteristics that affect demo compatibility.

### ROL-048: Color RAM Nibble Behavior
**Category:** Memory / Quirk
**Source:** C64 community

Color RAM ($D800-$DBFF) is implemented with 2114 SRAM chips providing only 4 bits. The upper 4 bits of a byte read from color RAM are undefined (floating bus values). Writing stores all 8 bits but only 4 are retained.

### ROL-049: Zero-Page Wrapping
**Category:** Memory / CPU
**Source:** 6502.org

Zero-page indexed addressing wraps within page zero. `LDA $FF,X` with X=1 reads from $0000, not $0100. This is true for all zero-page addressing modes including indirect: `LDA ($FF,X)` fetches the pointer from $FF and $00.

### ROL-050: I/O Area Mirror
**Category:** Memory
**Source:** C64 memory map

The I/O area at $D000-$DFFF contains mirrors: VIC-II registers repeat every 64 bytes ($D000-$D03F mirrors to $D040-$D07F, etc.). SID registers repeat every 32 bytes. CIA registers repeat every 16 bytes. Color RAM has no mirrors.

### ROL-051: Ultimax Mode
**Category:** Memory / Cartridge
**Source:** C64 schematic

The EXROM/GAME line combination 1/0 enables Ultimax (or MAX Machine) mode, which maps 8KB ROM at $E000-$FFFF and 8KB at $8000-$9FFF with RAM only at $0000-$0FFF. Most of the address space is unmapped.

### ROL-052: VIC Bank and Character ROM Availability
**Category:** Memory / Video
**Source:** C64 PRG

Character ROM is only visible to the VIC-II in banks 0 ($0000-$3FFF) and 2 ($8000-$BFFF), at the relative offsets $1000-$1FFF. In banks 1 and 3, those address ranges see RAM instead. This is because the VIC-II doesn't see the PLA banking.

### ROL-053: RAM Under ROM
**Category:** Memory
**Source:** C64 memory map

RAM exists at all 65536 addresses. ROM and I/O are overlaid by the PLA. The CPU can write to RAM under ROM directly (writes always go to RAM). Reading ROM hides the RAM value. DMA from VIC-II always reads RAM in Phi1.

---

## Storage — IEC Bus and Disk Drives

### ROL-054: IEC Serial Bus Protocol
**Category:** Storage
**Source:** IEC bus documentation

The C64 communicates with drives via a 3-wire serial bus (CLK, DATA, ATN). The protocol is handshaked and slow (~300 bytes/sec). Fast loaders replace the ROM routines with custom bit-banging protocols achieving 2-10 KB/sec.

### ROL-055: 1541 Drive — Separate Computer
**Category:** Storage / Architecture
**Source:** 1541 service manual

The 1541 is a complete computer with its own 6502 CPU, 2KB RAM, 16KB ROM, two VIA chips, and a read/write head controller. VICE emulates the drive CPU cycle-by-cycle for accurate fastloader support.

### ROL-056: GCR Encoding
**Category:** Storage
**Source:** GCR documentation

The 1541 stores data using Group Code Recording (GCR), where 4 data bits are encoded as 5 flux-transition bits. This ensures adequate flux transitions for clock recovery. A 256-byte sector becomes 325 GCR bytes on disk.

### ROL-057: Speed Zones
**Category:** Storage
**Source:** 1541 documentation

The 1541 disk has 4 speed zones: tracks 1-17 (21 sectors/track, zone 3), 18-24 (19 sectors, zone 2), 25-30 (18 sectors, zone 1), 31-35 (17 sectors, zone 0). Outer tracks spin faster relative to the head, allowing more sectors.

### ROL-058: Copy Protection — Track Alignment
**Category:** Storage / Quirk
**Source:** C64 preservation community

Some copy protections rely on precise track alignment by writing data between standard tracks (half-tracks) or at specific angular offsets. The G64 format preserves this information; D64 does not.

### ROL-059: 1571 Double-Sided Operation
**Category:** Storage
**Source:** 1571 documentation

The 1571 (C128 drive) can read both sides of a disk (70 tracks total). In C64 mode, it operates as a 1541. In native mode, it can also use MFM encoding for CP/M disk compatibility.

### ROL-060: 1581 3.5" Drive
**Category:** Storage
**Source:** 1581 documentation

The 1581 uses standard 3.5" DD disks with MFM encoding and has its own WD1770 floppy controller. It supports partitions and subdirectories. Capacity: 800KB (3160 blocks free).

---

## Storage — Datasette

### ROL-061: TAP Format Timing
**Category:** Storage / Format
**Source:** TAP format specification

The TAP file format stores pulse lengths as byte values. A value of N represents (N * 8) CPU cycles between pulses. Value 0 is an overflow marker followed by a 3-byte little-endian cycle count (TAP v1). This captures the exact tape timing.

### ROL-062: Motor Control Latency
**Category:** Storage / Timing
**Source:** C64 service manual

The datasette motor is controlled via bit 5 of CIA1 port A ($DC00). The motor has mechanical inertia — it takes approximately 300ms to reach stable speed after being turned on. Turbo loaders must account for this startup delay.

### ROL-063: Turbo Tape Loaders
**Category:** Storage
**Source:** C64 community

Turbo tape loaders bypass the slow ROM tape routines (300 baud) and use direct timer-based pulse measurement to achieve 2-4 KB/sec. They use shorter pulse lengths and reduced inter-block gaps.

---

## File Formats

### ROL-064: D64 Sector Interleave
**Category:** Format
**Source:** D64 format documentation

Standard D64 sector interleave is 10 (every 10th sector is written sequentially). This gives the CPU time to process each sector before the disk rotates to the next one. Fast loaders may use different interleave patterns.

### ROL-065: D64 Format Limitations
**Category:** Format
**Source:** D64 documentation

D64 stores exactly 174,848 bytes (683 blocks). It does not preserve: GCR encoding, bit-rate errors, track alignment, half-tracks, or inter-sector gaps. For copy protection preservation, G64 or NIB formats are required.

### ROL-066: G64 Format
**Category:** Format
**Source:** G64 specification

G64 stores the raw GCR-encoded track data including sync marks, header gaps, and inter-sector gaps. It can represent non-standard tracks, half-tracks, and copy protection schemes. Each track stores up to 7928 GCR bytes.

### ROL-067: T64 Tape Container
**Category:** Format
**Source:** T64 specification

T64 is a container format that stores multiple PRG files from tape images. It does not preserve tape timing information. It's essentially a directory listing with embedded PRG data.

### ROL-068: CRT Cartridge Format
**Category:** Format
**Source:** CRT specification

The CRT format has a file header identifying the cartridge type (73+ types defined), followed by CHIP packets containing ROM data. Each CHIP packet specifies its load address and bank number. The cartridge type determines the banking scheme.

### ROL-069: PRG File Format
**Category:** Format
**Source:** Commodore documentation

PRG files are the simplest format: 2-byte little-endian load address followed by raw data. The KERNAL's LOAD routine uses the load address to place the file in memory. BASIC programs start at $0801.

### ROL-070: REL File Format
**Category:** Format
**Source:** Commodore DOS documentation

REL (relative) files support random access via fixed-size records and side sectors. The 1541 maintains a side-sector chain that maps record numbers to disk sectors. Maximum record size: 254 bytes.

---

## Architecture Differences

### ROL-071: NTSC vs PAL Color Encoding
**Category:** Architecture
**Source:** VIC-II documentation

NTSC VIC-II (6567) produces composite NTSC video with color burst at 3.579545 MHz (subcarrier). PAL VIC-II (6569) uses 4.433619 MHz PAL subcarrier. The color palette differs between NTSC and PAL versions.

### ROL-072: C64 vs C64C Hardware Revisions
**Category:** Architecture
**Source:** C64 community wiki

The C64C (1986+) uses the 8580 SID (different filter), 85xx VIC-II, and a different PLA. The board layout changed from the original "breadbin" design. These changes affect audio, video timing, and some copy protection behavior.

### ROL-073: SX-64 Differences
**Category:** Architecture
**Source:** SX-64 service manual

The SX-64 (portable C64) has a built-in 5" CRT monitor and 1541 drive. It boots to a blue screen without the "LOAD" prompt (since it has a built-in drive). The KERNAL is slightly modified. No cassette port.

### ROL-074: C128 Mode Switching
**Category:** Architecture
**Source:** C128 PRG

The C128 operates in three modes: C128 mode (native, 2 MHz capable), C64 mode (full compatibility), and CP/M mode (Z80 processor). Mode is selected at boot by key combinations or software control via the MMU at $D500.

### ROL-075: C128 VDC 80-Column Display
**Category:** Architecture
**Source:** C128 PRG

The C128's MOS 8563 VDC provides an 80-column text display on a separate RGBI monitor output. The VDC has its own 16KB (or 64KB) RAM not accessible by the CPU — all access is through register ports $D600/$D601.

### ROL-076: VIC-20 Memory Expansion
**Category:** Architecture
**Source:** VIC-20 PRG

The VIC-20 has 5KB RAM (1KB at $0000-$03FF, 4KB at $1000-$1FFF). Memory expansion cartridges add RAM at $0400-$0FFF (3KB), $2000-$3FFF (8KB), $4000-$5FFF (8KB), $6000-$7FFF (8KB), and $A000-$BFFF (8KB).

### ROL-077: VIC Chip (6560/6561)
**Category:** Architecture / Video
**Source:** VIC-20 PRG

The VIC chip (not VIC-II) is a much simpler video chip. It generates a 22x23 character display with 4-bit color, 8x16 character cells, and no sprites. The 6560 is NTSC, 6561 is PAL. Audio is generated by the VIC chip, not a separate SID.

### ROL-078: PET CRTC Display
**Category:** Architecture / Video
**Source:** PET documentation

PET models use a 6845 CRTC for video generation, displaying 40 or 80 columns of text only (no bitmap mode). Different PET models have different screen sizes and character sets (business vs graphics keyboard).

### ROL-079: Plus/4 TED Chip
**Category:** Architecture
**Source:** Plus/4 PRG

The TED (7360/8360) combines video, audio, and timer functions in a single chip. It provides 121 colors (16 hues x 8 luminances + black), 320x200 bitmap, but only 2 voices of square wave audio (no SID).

### ROL-080: Plus/4 Built-in Software
**Category:** Architecture
**Source:** Plus/4 documentation

The Plus/4 includes built-in software ROMs: a word processor (3-Plus-1) and spreadsheet accessible via function keys. These occupy the upper ROM area and are banked in/out by the TED.

---

## Timing and Bus Behavior

### ROL-081: Phi1 and Phi2 Clock Phases
**Category:** Timing
**Source:** C64 schematic

The system clock alternates between Phi1 (VIC-II owns the bus) and Phi2 (CPU owns the bus). Each phase is ~500ns at PAL speed. The CPU performs address setup during Phi1 and data transfer during Phi2.

### ROL-082: BA Signal Timing
**Category:** Timing
**Source:** VIC-II article

The VIC-II asserts BA (Bus Available) LOW three cycles before it needs the bus for DMA. This gives the CPU three cycles to finish any pending write operations. After BA goes low, AEC follows on the next Phi2, freezing the CPU.

### ROL-083: AEC Line
**Category:** Timing
**Source:** C64 schematic

Address Enable Control (AEC) determines who drives the address bus. When AEC is high, the CPU drives the bus. When AEC is low, the VIC-II drives the bus. AEC follows BA with a delay.

### ROL-084: Refresh Cycles
**Category:** Timing
**Source:** VIC-II article

The VIC-II performs 5 DRAM refresh cycles per scanline (using idle Phi1 cycles). These are necessary to maintain the contents of dynamic RAM. The refresh addresses are generated by an internal counter.

### ROL-085: VIC-II Cycle Timing Within a Line
**Category:** Timing
**Source:** VIC-II article

Each PAL scanline consists of 63 cycles. Cycles 1-10: sprite DMA for sprites 3-7. Cycles 11-14: refresh. Cycles 15-54: character/bitmap DMA (on badlines) or idle. Cycles 55-62: sprite DMA for sprites 0-2 and pointer fetch. Cycle 63: idle.

### ROL-086: CIA Timer Underflow Timing
**Category:** Timing
**Source:** CIA datasheet, VICE source

When a CIA timer counts down to zero, it triggers an underflow interrupt on the NEXT cycle (not the same cycle). The timer is automatically reloaded from the latch value. In one-shot mode, the timer stops after the underflow.

### ROL-087: VIC-II Register Write Timing
**Category:** Timing
**Source:** VICE source

VIC-II register writes take effect at the cycle they are written. For example, changing the border color mid-line changes the color immediately at that horizontal position. This enables raster bar effects and split-screen techniques.

---

## Cartridges

### ROL-088: Ocean Type 1 Cartridge
**Category:** Cartridge
**Source:** CRT specification

Ocean type 1 cartridges bank 8KB ROMs at $8000-$9FFF using the DE00 I/O range for bank switching. Up to 512KB (64 banks). Used by many Ocean game cartridges.

### ROL-089: EasyFlash Cartridge
**Category:** Cartridge
**Source:** EasyFlash documentation

EasyFlash provides 1MB flash storage (64 x 8KB banks at $8000 + 64 x 8KB at $A000). It can be written in-system, making it suitable as a multi-game cartridge or even a writable storage device.

### ROL-090: Action Replay Cartridge
**Category:** Cartridge
**Source:** Action Replay documentation

The Action Replay uses the GAME/EXROM lines and an NMI trigger (active on freeze button) to freeze the running program. It banks its own ROM in, captures the machine state, and provides a monitor and export functionality.

### ROL-091: Cartridge GAME/EXROM Line Control
**Category:** Cartridge / Memory
**Source:** C64 schematic

Cartridges control memory mapping via two active-low lines: GAME and EXROM. The four combinations select: normal mode (1/1), 8KB at $8000 (1/0), 16KB at $8000+$A000 (0/0), and Ultimax (0/1).

### ROL-092: Final Cartridge III
**Category:** Cartridge
**Source:** FC3 documentation

The Final Cartridge III provides 64KB ROM with freezer, fast loader, desktop environment, and monitor. It uses NMI for the freeze function and has a built-in centronics printer interface emulation.

---

## Input

### ROL-093: Joystick Port Multiplexing
**Category:** I/O / Input
**Source:** C64 PRG

Joystick ports share CIA1 with the keyboard matrix. Port 2 uses CIA1 port A bits 0-4, port 1 uses port B bits 0-4. Reading joystick values must account for keyboard matrix interaction (a held key can appear as joystick input).

### ROL-094: Mouse (1351) Protocol
**Category:** I/O / Input
**Source:** 1351 documentation

The 1351 mouse uses the SID paddle registers for proportional position data. It works in "proportional" mode (SID paddles report motion) or "joystick" mode (emulates a joystick). The mouse sends pulses timed to the SID's paddle sample window.

### ROL-095: Lightpen Input
**Category:** I/O / Input
**Source:** VIC-II datasheet

The lightpen input is directly connected to control port 1's fire button line. When the CRT beam reaches the lightpen position, the photocell triggers, and the VIC-II latches the raster position. Only works with CRT displays.

---

## Monitor and Debugging

### ROL-096: VICE Monitor Commands
**Category:** Monitor
**Source:** VICE documentation

VICE's built-in monitor supports: disassembly (d), memory display (m), register display/set (r), breakpoints (break, watch, trace), fill (f), compare (c), transfer (t), hunt (h), assemble (a), and bank switching for different memory views.

### ROL-097: Breakpoint Types
**Category:** Monitor
**Source:** VICE documentation

VICE supports: exec breakpoints (break on PC reaching address), store breakpoints (break on memory write), load breakpoints (break on memory read), and conditional breakpoints (break when expression is true). ViceSharp must support all types.

### ROL-098: Memory Bank Views
**Category:** Monitor
**Source:** VICE documentation

The monitor can view memory through different "lenses": CPU (as the CPU sees it, with banking), RAM (raw RAM regardless of bank), ROM (ROM areas), and I/O (I/O register space). This helps debug banking issues.

---

## Determinism and State

### ROL-099: Deterministic Replay Requirements
**Category:** Determinism
**Source:** ViceSharp design

For bit-exact replay, the emulator must: use the same initial state (snapshot), process inputs at the exact same cycle, use deterministic pseudo-random sequences (SID noise LFSR is deterministic), and avoid any floating-point non-determinism.

### ROL-100: SID Noise LFSR
**Category:** Audio / Determinism
**Source:** SID documentation, resid source

The SID noise waveform uses a 23-bit Linear Feedback Shift Register (LFSR) with taps at bits 17 and 22 (XOR feedback). The LFSR shifts on each oscillator cycle. Given the same initial state and frequency, the noise sequence is perfectly deterministic.

### ROL-101: Snapshot Completeness
**Category:** Determinism
**Source:** ViceSharp design

A complete snapshot must capture: all RAM (64KB + expansion), all chip register state (VIC-II, SID, CIA x2, PLA), CPU state (A, X, Y, SP, PC, P), VIC-II internal state (raster counter, sprite DMA state, badline state), SID internal state (oscillators, envelopes, LFSR), CIA internal state (timer counters, latch values, shift registers), and any attached drive state.

### ROL-102: Floating Bus Values
**Category:** Determinism / Quirk
**Source:** VICE source

When the CPU reads from unmapped I/O space or open bus, the value depends on what the VIC-II last put on the data bus during Phi1. This "floating bus" value is deterministic if the VIC-II state is fully captured.

---

## Emulation Techniques

### ROL-103: Cycle-Based vs Line-Based Emulation
**Category:** Emulation
**Source:** VICE design

VICE's x64sc emulates at the cycle level (CPU and VIC-II interleaved every cycle). The faster x64 emulates at the line level (run CPU for a full line, then VIC-II for a full line). ViceSharp targets cycle-level accuracy like x64sc.

### ROL-104: Delayed Register Writes
**Category:** Emulation / Timing
**Source:** VICE source

Some VIC-II register changes take effect with a delay. For example, sprite enable ($D015) is checked at specific cycle positions within a line. Writing to it mid-line only affects sprites that haven't been processed yet.

### ROL-105: SID Resampling Quality
**Category:** Emulation / Audio
**Source:** resid documentation

resid supports three resampling methods: Fast (zero-order hold, 8-bit quality), Interpolation (linear, 16-bit quality), and Resampling (band-limited sinc interpolation, near-perfect quality). ViceSharp should offer all three.

### ROL-106: Drive Emulation Accuracy Levels
**Category:** Emulation
**Source:** VICE documentation

VICE offers: no drive emulation (virtual filesystem), "true drive emulation" (cycle-accurate 6502 + VIA + GCR), and "virtual drive" (trap-based, faster but incompatible with fast loaders). ViceSharp uses true drive emulation by default.

### ROL-107: Warp Mode Implementation
**Category:** Emulation
**Source:** VICE source

Warp mode skips all video rendering and audio output to maximize emulation speed. The CPU and VIC-II still run at full accuracy, but frame buffers aren't composited and audio samples aren't queued. Used for fast disk/tape loading.

---

## Additional Hardware Details

### ROL-108: CIA1 Keyboard Scan
**Category:** I/O
**Source:** C64 PRG

CIA1 port A ($DC00) selects the keyboard column (active-low output), and port B ($DC01) reads the row state (active-low input). The KERNAL scans all 8 columns every 1/60th second. Direct CIA access can poll specific keys without waiting.

### ROL-109: IEC Bus Fast Loader Protocols
**Category:** Storage
**Source:** C64 community

Fast loaders (JiffyDOS, Epyx FastLoad, Action Replay) replace the slow IEC serial protocol with custom bit-banging that uses the CLK and DATA lines as a 2-bit parallel bus, achieving 4-10x speedup.

### ROL-110: VIC-II Sprite Pointer Location
**Category:** Video
**Source:** C64 PRG

Sprite data pointers are stored in the last 8 bytes of screen memory (default: $07F8-$07FF). Each pointer value is multiplied by 64 to get the sprite data address. This location changes when screen memory is relocated via $D018.

### ROL-111: VIC-II Display Enable Timing
**Category:** Video / Timing
**Source:** VIC-II article

The DEN bit ($D011 bit 4) is sampled at specific cycles. If DEN is cleared before the first badline of a frame (line 48), no badlines occur for the entire frame, giving the CPU all cycles. If cleared after, the current frame still has badlines.

### ROL-112: SID External Audio Input
**Category:** Audio
**Source:** SID datasheet

The SID has an external audio input (pin 26) that is mixed with the internal audio and passed through the filter. On the C64, this pin is connected to the expansion port. Some cartridges use this for speech synthesis or audio mixing.

### ROL-113: CIA TOD Alarm
**Category:** I/O
**Source:** CIA datasheet

The CIA's TOD clock has an alarm function. When the TOD time matches the alarm time, an interrupt is generated (if enabled in ICR). The alarm is set by writing to the TOD registers with bit 7 of CRB set.

### ROL-114: VIC-II Light Pen Coordinates
**Category:** Video / I/O
**Source:** VIC-II datasheet

Light pen X position ($D013) has 2-pixel resolution (value 0-255 covers the visible screen width). Light pen Y position ($D014) has 1-line resolution. Values are latched on the first LP trigger per frame.

### ROL-115: Drive Error Channel
**Category:** Storage
**Source:** Commodore DOS documentation

The 1541 error channel (secondary address 15) reports disk status: "00, OK,00,00" for success, error codes 20-74 for various failures. The error channel also accepts DOS commands (N: format, S: scratch, R: rename, etc.).

### ROL-116: D64 BAM (Block Availability Map)
**Category:** Format
**Source:** D64 documentation

Track 18 sector 0 contains the BAM and directory header. The BAM tracks which sectors are allocated. Each track entry contains a free sector count byte followed by a 3-byte bitmap (24 bits for up to 21 sectors).

### ROL-117: VIC-II Sprite Stretching
**Category:** Video
**Source:** C64 demo scene

By toggling the Y-expand bit ($D017) on specific scanlines, individual sprite rows can be repeated or skipped, effectively stretching sprites vertically. Combined with sprite multiplexing, this enables sprite displays with more than 8 sprites.

### ROL-118: CIA NMI Acknowledge
**Category:** I/O / Timing
**Source:** VICE source

Unlike IRQ, NMI is edge-triggered. The CIA generates an NMI on the falling edge of any enabled interrupt source in CIA2's ICR. To acknowledge, the software must read $DD0D, which clears the interrupt flags and re-enables edge detection.

### ROL-119: VIC-20 Color Memory
**Category:** Architecture / Memory
**Source:** VIC-20 documentation

The VIC-20's color memory is at $9400-$97FF (only 1024 bytes used for the 22x23 display). Unlike the C64's 4-bit color RAM, the VIC-20 color memory uses full bytes. Bits 0-2: character color, bit 3: multi-color flag.

### ROL-120: 1541 Head Stepping
**Category:** Storage
**Source:** 1541 service manual

The 1541 stepper motor moves in half-track increments. Standard tracks are on even half-tracks (0, 2, 4...). The drive can position on half-tracks (odd positions) for copy protection purposes. Maximum 84 half-tracks.

### ROL-121: VIC-II Raster Counter
**Category:** Video / Timing
**Source:** VIC-II article

The VIC-II raster counter ($D011 bit 7 + $D012) is a 9-bit value counting from 0 to 311 (PAL) or 262 (NTSC). The counter wraps to 0 after the last line. The raster interrupt compares against this full 9-bit value.

### ROL-122: SID Voice On/Off Glitch
**Category:** Audio / Quirk
**Source:** SID documentation

Setting the gate bit (bit 0 of voice control register) starts the attack phase. Clearing it starts the release phase. If the gate is cleared and set within a very short time, the release phase may not complete, causing a volume "glitch" audible as a click.

### ROL-123: CIA Shift Register Direction
**Category:** I/O
**Source:** CIA datasheet

The CIA shift register ($DC0C) direction is controlled by bit 6 of CRA. When set, the shift register outputs (Timer A clocks the shift). When clear, it inputs (CNT pin provides the clock). The shift register generates an interrupt after 8 bits.

### ROL-124: VIC-II Open Sideborder Technique
**Category:** Video
**Source:** C64 demo scene

The side borders can be opened by switching between 38-column and 40-column mode ($D016 bit 3) at the exact cycle where the border flip-flop is checked. This must happen on every scanline for continuous effect.

### ROL-125: Disk Drive RAM and ROM Map
**Category:** Storage / Architecture
**Source:** 1541 schematic

1541 memory map: $0000-$07FF: 2KB RAM. $1800-$180F: VIA1 (IEC bus). $1C00-$1C0F: VIA2 (drive mechanism). $C000-$FFFF: 16KB ROM (DOS). Address decoding is partial — RAM mirrors exist at $0800-$0FFF.
