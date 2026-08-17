namespace ViceSharp.Abstractions;

/// <summary>
/// Native VICE emulator binding interface
/// </summary>
public interface IViceNative : IDisposable
{
    /// <summary>
    /// Reset emulator to initial power on state
    /// </summary>
    void Reset();

    /// <summary>
    /// Advance emulator by one single master cycle
    /// </summary>
    void Step();

    /// <summary>
    /// Attach a raw cartridge image to the native emulator instance.
    /// </summary>
    void AttachCartridge(ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode);

    /// <summary>
    /// Attach a disk image path to the native emulator drive.
    /// </summary>
    void AttachDisk(uint unit, uint drive, string path);

    /// <summary>
    /// Detach the disk image from the native emulator drive.
    /// </summary>
    void DetachDisk(uint unit, uint drive);

    /// <summary>
    /// Apply a host keyboard matrix key state to the native emulator.
    /// </summary>
    void SetKeyboardMatrixKey(int row, int column, bool pressed);

    /// <summary>
    /// Read a byte from native physical C64 RAM without bus side effects.
    /// </summary>
    byte PeekRam(ushort address);

    /// <summary>
    /// Side-effect-free peek of the full native address space (RAM/ROM/I/O).
    /// Used for Vic20 lockstep samples of VIA ($9110/$9120) and VIC-I ($9000).
    /// C64 implementations may fall back to <see cref="PeekRam"/>.
    /// </summary>
    byte PeekBus(ushort address);

    /// <summary>
    /// Side-effecting store into the native address space (mem_store).
    /// Used for lockstep fixtures that poke VIC-I regs on both machines.
    /// </summary>
    void WriteBus(ushort address, byte value);

    /// <summary>
    /// Load a VICE snapshot (.vsf) into the native machine, resuming it from the
    /// staged state. The cycle counter is re-baselined so <see cref="GetState"/>
    /// reports cycles elapsed since the load point. Returns 0 on success.
    /// </summary>
    int ReadSnapshot(string path);

    /// <summary>
    /// Write the current native machine state to a VICE snapshot (.vsf).
    /// Returns 0 on success.
    /// </summary>
    int WriteSnapshot(string path);

    /// <summary>
    /// Get current full machine state
    /// </summary>
    MachineState GetState();

    /// <summary>
    /// Get current native VIC-II timing state for lockstep diagnostics.
    /// </summary>
    NativeVicState GetVicState();

    /// <summary>
    /// VIC-I video pipeline snapshot for every-cycle lockstep (xvic only).
    /// C64 native implementations throw <see cref="NotSupportedException"/>.
    /// </summary>
    Vic20VideoLockstepState GetVic20VideoState();

    /// <summary>
    /// Capture the visible framebuffer as BGRA (presentation surface).
    /// VIC-20 (xvic): normal-border canvas (PAL 448x284). C64 may support full
    /// 384x272. Returns false when the shim has no canvas or the buffer is too small.
    /// </summary>
    bool TryCaptureVisibleFrame(byte[] bgraBuffer, out int width, out int height);

    /// <summary>
    /// Capture the visible framebuffer as palette indices (one byte per pixel).
    /// Primary Exact path for VIC-20 pixel lockstep (FR-VIC20-001 / AC-PX-02).
    /// Returns false when unsupported or buffer too small.
    /// </summary>
    bool TryCaptureFrameIndices(byte[] indexBuffer, out int width, out int height);

    /// <summary>
    /// VIC-20 VIC-I audio: render mono int16 PCM samples for a CPU cycle budget
    /// (FR-VIC20-SOUND-001 / AC-SD-01). Returns samples written, or 0 if unsupported.
    /// </summary>
    int RenderVic20Samples(short[] buffer, int sampleRate, int cyclesPerSec, int deltaTCycles);

    /// <summary>
    /// Get the main-CPU resume/pipeline state (TR-LOCKSTEP-VSF-001): the
    /// .vsf-restored in-flight context beyond the plain register file (last
    /// opcode info, pending BA-low stall flags, the 6510 processor port that
    /// selects ROM/IO banking, and the interrupt-status clocks). Valid right
    /// after <see cref="ReadSnapshot"/> and at any paused cycle boundary; used
    /// to stage a managed machine so snapshot-resumed lockstep aligns from
    /// cycle 0.
    /// </summary>
    NativeCpuPipelineState GetCpuPipelineState();

    /// <summary>
    /// Get a CIA's resume state (TR-LOCKSTEP-VSF-001): ports/DDRs, live timer
    /// counters plus reload latches, control registers, latched interrupt
    /// flags and the ICR enable mask, as restored from the .vsf CIA modules.
    /// Index 0 = CIA1 ($DC00), 1 = CIA2 ($DD00).
    /// </summary>
    NativeCiaState GetCiaState(int ciaIndex);
}

/// <summary>
/// CIA resume state exported by the native shim (TR-LOCKSTEP-VSF-001):
/// everything a managed MOS 6526 needs to resume a .vsf mid-run - the port
/// output latches and data directions, the LIVE timer counters (ciat_read_timer)
/// with their reload latches (ciat_read_latch), the control registers, the
/// latched interrupt flags and the ICR interrupt-enable mask (ciacore
/// irq_enabled).
/// </summary>
public readonly struct NativeCiaState
{
    /// <summary>Port A output latch ($DC00 written value).</summary>
    public byte PortA { get; init; }

    /// <summary>Port B output latch ($DC01 written value).</summary>
    public byte PortB { get; init; }

    /// <summary>Port A data direction ($DC02).</summary>
    public byte DdrA { get; init; }

    /// <summary>Port B data direction ($DC03).</summary>
    public byte DdrB { get; init; }

    /// <summary>Timer A live counter.</summary>
    public ushort TimerA { get; init; }

    /// <summary>Timer B live counter.</summary>
    public ushort TimerB { get; init; }

    /// <summary>Timer A reload latch.</summary>
    public ushort TimerALatch { get; init; }

    /// <summary>Timer B reload latch.</summary>
    public ushort TimerBLatch { get; init; }

    /// <summary>Control register A ($DC0E).</summary>
    public byte Cra { get; init; }

    /// <summary>Control register B ($DC0F).</summary>
    public byte Crb { get; init; }

    /// <summary>Latched interrupt flags (ICR read side).</summary>
    public byte InterruptFlags { get; init; }

    /// <summary>ICR interrupt-enable mask (write side).</summary>
    public byte IrqMask { get; init; }
}

/// <summary>
/// Serializable machine state snapshot
/// </summary>
public readonly struct MachineState
{
    // CPU Registers
    public byte A { get; init; }
    public byte X { get; init; }
    public byte Y { get; init; }
    public byte S { get; init; }
    public byte P { get; init; }
    public ushort PC { get; init; }

    // Cycle counter
    public long Cycle { get; init; }
}

/// <summary>
/// Main-CPU resume/pipeline state exported by the native shim
/// (TR-LOCKSTEP-VSF-001): the x64sc in-flight context a .vsf carries beyond
/// the plain register file. Mirrors the native MAINCPU snapshot module
/// (last opcode info + BA-low stall flags), the C64MEM module's 6510
/// processor port (written data/dir plus effective read values selecting
/// ROM/IO banking), and the interrupt-status clocks.
/// </summary>
public readonly struct NativeCpuPipelineState
{
    /// <summary>Native master clock (maincpu_clk) at export.</summary>
    public ulong Clk { get; init; }

    /// <summary>MAINCPU module last_opcode_info (opcode + IRQ delay/enable flags).</summary>
    public uint LastOpcodeInfo { get; init; }

    /// <summary>MAINCPU module maincpu_ba_low_flags (pending BA-low stall sources).</summary>
    public uint BaLowFlags { get; init; }

    /// <summary>6510 processor port data register ($01 written value).</summary>
    public byte PportData { get; init; }

    /// <summary>6510 processor port direction register ($00 written value).</summary>
    public byte PportDir { get; init; }

    /// <summary>Effective $01 read value (pull-ups applied); selects ROM/IO banking.</summary>
    public byte PportDataRead { get; init; }

    /// <summary>Effective $00 read value.</summary>
    public byte PportDirRead { get; init; }

    /// <summary>Pending interrupt mask (IK_* bits) from the interrupt status.</summary>
    public uint GlobalPendingInt { get; init; }

    /// <summary>Clock of the last IRQ line change.</summary>
    public ulong IrqClk { get; init; }

    /// <summary>Clock of the last NMI line change.</summary>
    public ulong NmiClk { get; init; }

    /// <summary>IRQ acknowledge-delay cycle counter.</summary>
    public ulong IrqDelayCycles { get; init; }

    /// <summary>NMI acknowledge-delay cycle counter.</summary>
    public ulong NmiDelayCycles { get; init; }
}

public readonly struct NativeVicState
{
    public uint Cycle { get; init; }
    public ushort RasterLine { get; init; }
    public byte RasterCycle { get; init; }
    public byte BadLine { get; init; }
    public byte DisplayState { get; init; }
    public byte SpriteDma { get; init; }

    /// <summary>
    /// Snapshot of the 64-byte VIC-II register file ($D000-$D03F) as the native
    /// shim sees it, with $D019 reflecting the live IRQ latch. Used to seed a
    /// managed VIC for snapshot-resume lockstep diagnostics. May be null if the
    /// binding did not populate it.
    /// </summary>
    public byte[]? Registers { get; init; }

    /// <summary>
    /// TR-LOCKSTEP-VSF-001: the .vsf VIC-II module's allow_bad_lines latch
    /// (DEN seen at line $30). Gates check_badline and therefore every badline
    /// BA-low CPU stall for the remainder of the frame; must be seeded when
    /// resuming a managed VIC from a mid-frame snapshot.
    /// </summary>
    public byte AllowBadLines { get; init; }

    /// <summary>
    /// TR-LOCKSTEP-VSF-001: the .vsf VIC-II module's display/idle g-access
    /// state (vicii.idle_state).
    /// </summary>
    public byte IdleState { get; init; }
}

/// <summary>
/// VIC-I (VIC-20) video pipeline snapshot for every-cycle lockstep vs xvic
/// (<c>vice_vic20_get_video_state</c> / managed <c>Mos6561.CaptureVideoLockstepState</c>).
/// Tracks video the same way CPU lockstep tracks A/X/Y/S/P/PC: per phi2 after
/// <c>vic_cycle</c>, not end-of-frame pixels alone.
/// </summary>
public readonly struct Vic20VideoLockstepState
{
    public uint Cycle { get; init; }
    public ushort RasterLine { get; init; }
    public byte RasterCycle { get; init; }
    /// <summary>VICE <c>vic.area</c> (IDLE/PENDING/DISPLAY/DONE).</summary>
    public byte Area { get; init; }
    /// <summary>VICE <c>vic.fetch_state</c>.</summary>
    public byte FetchState { get; init; }
    public byte TextCols { get; init; }
    public byte TextLines { get; init; }
    public byte YCounter { get; init; }
    public byte RowCounter { get; init; }
    public byte BlankThisLine { get; init; }
    public byte LineWasBlank { get; init; }
    public byte CharHeight { get; init; }
    public ushort Memptr { get; init; }
    public ushort MemptrInc { get; init; }
    /// <summary>Raw $9000-$900F store (16 bytes); not bus peeks with live raster bits.</summary>
    public byte[]? Regs { get; init; }
    /// <summary>VICE <c>vic.cbuf</c> color nybbles for the current line (up to 32).</summary>
    public byte[]? Cbuf { get; init; }
    /// <summary>VICE <c>vic.gbuf</c> glyph bytes for the current line (up to 32).</summary>
    public byte[]? Gbuf { get; init; }
}
