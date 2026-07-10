using ViceSharp.Core;
using ViceSharp.Abstractions;

namespace ViceSharp.TestHarness;

/// <summary>
/// Test-local convenience wrapper around the shared VICE native interop.
/// </summary>
public static class ViceNativeBridge
{
    public static bool IsAvailable => ViceNative.IsAvailable;
    public static string AvailabilityMessage => ViceNative.AvailabilityMessage;

    public static IntPtr CreateMachine(string? modelSelector = null)
    {
        var machine = string.IsNullOrWhiteSpace(modelSelector)
            ? ViceNative.Create()
            : ViceNative.CreateModel(modelSelector);

        if (machine == IntPtr.Zero)
            throw new InvalidOperationException($"Native VICE failed to create a machine for model '{modelSelector ?? "default"}'.");

        return machine;
    }

    public static void DestroyMachine(IntPtr machine) => ViceNative.Destroy(machine);

    public static void ResetMachine(IntPtr machine) => ViceNative.ResetNative(machine);
    public static void StepCycle(IntPtr machine) => ViceNative.StepNative(machine);
    public static int GetModel(IntPtr machine) => ViceNative.GetModel(machine);
    public static byte GetCpuRegister(IntPtr machine, int registerId) => ViceNative.GetCpuRegister(machine, registerId);
    public static byte ReadMemory(IntPtr machine, ushort address) => ViceNative.ReadMemory(machine, address);
    public static byte PeekRam(IntPtr machine, ushort address) => ViceNative.PeekRam(machine, address);
    public static void WriteMemory(IntPtr machine, ushort address, byte value) => ViceNative.WriteMemory(machine, address, value);
    public static void AttachCartridge(IntPtr machine, ReadOnlyMemory<byte> image, CartridgeMappingMode mappingMode)
        => ViceNative.AttachCartridge(machine, image, mappingMode);
    public static void AttachDisk(IntPtr machine, uint unit, uint drive, string path)
        => ViceNative.AttachDisk(machine, unit, drive, path);
    public static void DetachDisk(IntPtr machine, uint unit, uint drive)
        => ViceNative.DetachDisk(machine, unit, drive);
    // Drive residue observability (TEST-NATIVE-RESIDUE-03/04).
    public static int GetDriveClockResidue(IntPtr machine, uint unit, out ulong attachClk, out ulong detachClk, out ulong attachDetachClk)
        => ViceNative.GetDriveClockResidue(machine, unit, out attachClk, out detachClk, out attachDetachClk);
    public static ulong GetDriveCycleAccum(IntPtr machine, uint unit)
        => ViceNative.GetDriveCycleAccum(machine, unit);
    public static int SetDriveCycleAccum(IntPtr machine, uint unit, ulong value)
        => ViceNative.SetDriveCycleAccum(machine, unit, value);
    public static int ReadSnapshot(IntPtr machine, string path) => ViceNative.ReadSnapshotNative(machine, path);
    public static int WriteSnapshot(IntPtr machine, string path) => ViceNative.WriteSnapshotNative(machine, path);
    public static int SetDriveTrueEmulation(IntPtr machine, uint unit, bool enabled)
        => ViceNative.SetDriveTrueEmulation(machine, unit, enabled ? 1 : 0);
    public static int GetDriveTrueEmulation(IntPtr machine, uint unit) => ViceNative.GetDriveTrueEmulation(machine, unit);
    public static void SetKeyboardMatrixKey(IntPtr machine, int row, int column, bool pressed)
        => ViceNative.SetKeyboardMatrixKey(machine, row, column, pressed);
    public static void StoreCia1Register(IntPtr machine, byte registerIndex, byte value)
        => ViceNative.StoreCia1Register(machine, registerIndex, value);
    public static byte ReadCia1Register(IntPtr machine, byte registerIndex)
        => ViceNative.ReadCia1Register(machine, registerIndex);

    public static void GetVicState(IntPtr machine, ref ViceVicState state)
    {
        var nativeState = new ViceNative.ViceVicState();
        ViceNative.GetVicState(machine, ref nativeState);

        state.Cycle = nativeState.Cycle;
        state.RasterLine = nativeState.RasterLine;
        state.RasterCycle = nativeState.RasterCycle;
        state.BadLine = nativeState.BadLine;
        state.DisplayState = nativeState.DisplayState;
        state.SpriteDma = nativeState.SpriteDma;
        state.Registers = nativeState.GetRegisters();
        state.RegistersPeek = nativeState.GetRegistersPeek();
        state.AllowBadLines = nativeState.AllowBadLines;
        state.IdleState = nativeState.IdleState;
    }

    public static void GetCiaState(IntPtr machine, int ciaIndex, ref ViceCiaState state)
    {
        var nativeState = new ViceNative.ViceCiaState();
        ViceNative.GetCiaState(machine, ciaIndex, ref nativeState);

        state.PortA = nativeState.PortA;
        state.PortB = nativeState.PortB;
        state.DdrA = nativeState.DdrA;
        state.DdrB = nativeState.DdrB;
        state.TimerA = nativeState.TimerA;
        state.TimerB = nativeState.TimerB;
        state.Icr = nativeState.Icr;
        state.Cra = nativeState.Cra;
        state.Crb = nativeState.Crb;
        state.InterruptFlag = nativeState.InterruptFlag;
        state.TimerALatch = nativeState.TimerALatch;
        state.TimerBLatch = nativeState.TimerBLatch;
        state.IrqMask = nativeState.IrqMask;
    }

    public static void GetSidState(IntPtr machine, ref ViceSidState state)
    {
        var nativeState = new ViceNative.ViceSidState();
        ViceNative.GetSidState(machine, ref nativeState);

        state.Registers = nativeState.GetRegisters();
        state.Accumulators = nativeState.GetAccumulators();
        state.Envelopes = nativeState.GetEnvelopes();
        state.FilterState = nativeState.FilterState;
    }

    /// <summary>
    /// Render PCM samples from the native SID into the buffer. Returns the
    /// number of samples actually rendered as signed 16-bit values.
    /// </summary>
    /// <param name="machine">Native VICE machine handle.</param>
    /// <param name="buffer">Sample destination; length determines maximum render count.</param>
    /// <param name="deltaTCycles">Host-cycle budget per sample (default 22 ~= 44.1kHz at C64 PAL).</param>
    public static int RenderSidSamples(IntPtr machine, short[] buffer, int deltaTCycles = 22)
    {
        var written = ViceNative.RenderSidSamples(machine, buffer, (nuint)buffer.Length, deltaTCycles);
        return (int)written;
    }

    /// <summary>
    /// PLAN-VICEPARITY-001 Phase 0 (P0-1) / TR-SID-ORACLE-001. Single-cycle reSID
    /// oracle surface: opens the shim's private reSID engine, then drives
    /// reSID::SID::clock() one cycle at a time so parity tests can assert
    /// bit-exact equality (the batched vice_sid_clock path drops the
    /// envelope/waveform single-cycle pipelines). After open, drive register
    /// writes ONLY through SidExactWrite; re-syncing from machine memory would
    /// clobber pipeline state.
    /// </summary>
    public static bool SidExactOpen(IntPtr machine) => ViceNative.SidExactOpen(machine) != 0;

    /// <summary>Reset the exact-oracle reSID engine (reSID::SID::reset()).</summary>
    public static void SidExactReset(IntPtr machine) => ViceNative.SidExactReset(machine);

    /// <summary>Advance the exact-oracle reSID engine by exactly <paramref name="cycles"/> single cycles.</summary>
    public static int SidExactClock(IntPtr machine, int cycles) => ViceNative.SidExactClock(machine, cycles);

    /// <summary>Write a SID register directly on the exact-oracle engine (reSID::SID::write()).</summary>
    public static void SidExactWrite(IntPtr machine, ushort addr, byte value) => ViceNative.SidExactWrite(machine, addr, value);

    /// <summary>Read a SID register from the exact-oracle engine (reSID::SID::read(); OSC3=$1B, ENV3=$1C).</summary>
    public static byte SidExactRead(IntPtr machine, ushort addr) => ViceNative.SidExactRead(machine, addr);

    /// <summary>Current 16-bit audio output of the exact-oracle engine (reSID::SID::output()).</summary>
    public static short SidExactOutput(IntPtr machine) => ViceNative.SidExactOutput(machine);

    /// <summary>Full reSID internal state of the exact-oracle engine (accumulators, noise shift registers, envelope pipelines, bus value).</summary>
    public static ViceNative.ViceSidExactState SidExactGetState(IntPtr machine)
    {
        var state = new ViceNative.ViceSidExactState();
        ViceNative.SidExactGetState(machine, ref state);
        return state;
    }

    /// <summary>
    /// PLAN-VICEPARITY-001 Phase 0 (P0-2) / TR-VIC-ORACLE-001. Per-pixel VIC
    /// oracle: copies the visible frame from VICE's viciisc raster draw buffer
    /// as raw palette indices (one byte per pixel, 0x00-0x0F). Index-exact
    /// comparison is palette-independent; VIC parity ACs assert colour identity
    /// against this buffer rather than an RGB conversion.
    /// </summary>
    /// <param name="machine">Native VICE machine handle.</param>
    /// <param name="indexBuffer">Destination; must hold at least visible width * height bytes (384 * 272 PAL).</param>
    /// <param name="width">Visible width reported by the native geometry.</param>
    /// <param name="height">Visible height reported by the native geometry.</param>
    /// <returns>True when the native raster draw buffer was copied.</returns>
    public static bool TryCaptureVicFrameIndices(IntPtr machine, byte[] indexBuffer, out int width, out int height)
    {
        return ViceNative.CaptureVicFrameIndices(machine, indexBuffer, indexBuffer.Length, out width, out height) != 0;
    }

    /// <summary>
    /// BACKFILL-VIDEO-001 / TR-VIC-EDGE-001 (display-mode native visible-frame extension; full buffer support) / 
    /// TR-VIC-EDGE-002 / TR-VIC-EDGE-006 (open-border + display-mode depth) / FR-VIC-002 / FR-VIC-003 / FR-VIC-005 / FR-VIC-008 / TEST-VIC-001.
    /// 
    /// Test-only contract surface for native visible-frame / full buffer copy validation of display-mode effects
    /// (e.g. invalid ECM COL_NONE black pixels + preserved priority bits per VICE draw logic; full canvas incl. borders
    /// for open-border cases).
    /// 
    /// VICE sources (from explore subagent report ID 019e6acc-29b8-77f1-a9cc-56499af366f9 + TR-VIC-EDGE-001/002/006):
    /// native/vice/vice/src/viciisc/vicii-draw-cycle.c:41 (COL_NONE), :133-141 (invalid vmode table rows all COL_NONE),
    /// :196-224 (pixel_pri = px &amp; 0x2 independent of cc; pri_buffer always written), :401-428 (pri_buffer consumed
    /// for sprite priority decision + sprite-bg collisions).
    /// Additional: vicii-cycle.c (vicii_raster_draw_handler + per-cycle pixel draw at raster cycles into line/raster buffers);
    /// viciitypes.h (vicii_t raster + VICII_SCREEN_* + draw buffers); VideoRenderer.cs (ScreenWidth=384 / ScreenHeight=272 full frame buffer target).
    /// 
    /// Use case / acceptance (BDP mocks-first):
    /// - Caller supplies pre-sized buffer (320x200 for checkpoint or 384x272 for full visible frame incl. borders).
    /// - Method detects size, returns actual w/h (320x200 or 384x272), fills BGRA from native vicii raster/line buffers (real path)
    ///   or simulator-derived expectations (mock path).
    /// - Enables native vs managed + simulator comparison for display-mode + open-border effects under TR-VIC-EDGE-001/002/006.
    /// - 320x200 calls remain compatible (checkpoint parity); larger buffer selects full canvas copy path (leveraging existing vice-shim / vicii state beyond 320x200 snapshot).
    /// - Mock/stub path (Zero) enables full contract + expectation validation before real shim buffer copy from raster.
    /// - No impact on lockstep or prod paths (test harness surface only).
    /// 
    /// Byrd: Extended for the full buffer copy slice. Mocks/stubs (size-aware mock fill + simulator) validated green first.
    /// Requirements + VICE evidence (vicii-draw-cycle.c lines + raster buffers) + VideoRenderer consts drive the surface + tests.
    /// </summary>
    /// <param name="machine">Native VICE machine handle (or IntPtr.Zero for pure mock path in tests).</param>
    /// <param name="bgraBuffer">Destination buffer (pre-allocated by caller; 320*200*4 for checkpoint or 384*272*4 for full visible frame).</param>
    /// <param name="width">Actual width written (320 or 384).</param>
    /// <param name="height">Actual height written (200 or 272).</param>
    /// <returns>True if capture succeeded (mock always succeeds for contract validation in this slice).</returns>
    public static bool TryCaptureVicVisibleFrame(IntPtr machine, byte[] bgraBuffer, out int width, out int height)
    {
        // BACKFILL-VIDEO-001 / TR-VIC-EDGE-001 (native visible-frame/checkpoint validation for display-mode effects; full buffer extension) /
        // TR-VIC-EDGE-002 / TR-VIC-EDGE-006 (open-border + display-mode depth) / FR-VIC-002 / FR-VIC-003 / FR-VIC-005 / FR-VIC-008 / TEST-VIC-001.
        // VICE sources (explore subagent 019e6acc-29b8-77f1-a9cc-56499af366f9): viciisc/vicii-draw-cycle.c:41 (COL_NONE),
        // :133-141 (all 3 invalid ECM selector combos route to COL_NONE rows forcing black visible), :196 (pixel_pri=(px&0x2)
        // unconditional), :197-203/224 (pri_buffer written regardless of cc=COL_NONE), :401-428 (pri used for sprite prio
        // + sprite-bg collision latch even on COL_NONE pixels).
        // Full buffer: vicii-cycle.c (raster/line buffers + draw handlers); viciitypes.h (vicii_t + raster); VideoRenderer.cs:10-11 (384x272 full target).
        // (Comment in shim.c: "Minimal checkpoint surface; full vicii.raster.canvas copy follow-on." - this implements that.)
        //
        // Real wiring (prior slice for 320x200; this slice for full): delegate to native P/Invoke + shim export when handle != Zero.
        // Shim extended to honor length and copy from vicii raster/line buffers for full canvas.
        // Authentic pixels from native VICE vicii state (mode + draw semantics for full visible incl borders).
        // Mock for Zero (BDP isolation; size-aware using simulator parity).
        // No prod paths or lockstep touched. Minimal diff.
        const int CheckpointW = 320;
        const int CheckpointH = 200;
        const int FullW = 384;  // matches VideoRenderer.ScreenWidth for full visible frame (borders + open-border cases)
        const int FullH = 272;  // matches VideoRenderer.ScreenHeight

        int fullRequired = FullW * FullH * 4;
        int checkpointRequired = CheckpointW * CheckpointH * 4;

        if (bgraBuffer != null && bgraBuffer.Length >= fullRequired)
        {
            width = FullW;
            height = FullH;
        }
        else if (bgraBuffer != null && bgraBuffer.Length >= checkpointRequired)
        {
            width = CheckpointW;
            height = CheckpointH;
        }
        else
        {
            width = 0;
            height = 0;
            return false;
        }

        int required = width * height * 4;

        if (machine != IntPtr.Zero)
        {
            // Real delegation (authentic from native emulation; shim now supports full via vicii raster/line buffers for this slice)
            int res = ViceNative.CaptureVisibleFrame(machine, bgraBuffer, required, out width, out height);
            return res != 0;
        }

        // Mock/stub (validated green prior to real full shim wiring per BDP)
        // Size-aware: for full buffer use simulator-derived expectations (COL_NONE black gfx + sentinel); for checkpoint prior behavior.
        Array.Clear(bgraBuffer, 0, Math.Min(required, bgraBuffer.Length));
        if (bgraBuffer.Length >= 4)
        {
            bgraBuffer[0] = 0xCC;
            bgraBuffer[1] = 0xCC;
            bgraBuffer[2] = 0xCC;
            bgraBuffer[3] = 0xFF;
        }

        // For full buffer mock path (tests-first enabling), mark a representative gfx sample black (COL_NONE parity with simulator).
        if (width == FullW && height == FullH && bgraBuffer.Length >= fullRequired)
        {
            // Simple full-canvas mock fill for invalid ECM display (gfx region black per VICE :133-141; borders 0 for checkpoint).
            // Real path in shim will use vicii raster buffers for authentic.
            int sampleGfx = (100 * FullW + 160) * 4;
            if (sampleGfx + 3 < fullRequired)
            {
                bgraBuffer[sampleGfx + 0] = 0;
                bgraBuffer[sampleGfx + 1] = 0;
                bgraBuffer[sampleGfx + 2] = 0;
                bgraBuffer[sampleGfx + 3] = 0xFF;
            }
        }

        return true;
    }

    /// <summary>
    /// BACKFILL-VIDEO-001 / TR-VIC-EDGE-001 (line pixel+pri snapshot surface reinforcement of the ECM family, final visible-frame/checkpoint) / FR-VIC-002 / FR-VIC-003 / FR-VIC-005 / FR-VIC-008 / TEST-VIC-001.
    /// 
    /// New test contract surface for native line pixel+pri snapshot at raster/draw boundaries (checkpointed pri_buffer / graphics state).
    /// Enables authentic native pri values at a raster line to be compared against InvalidEcmNativeSimulator expectations (COL_NONE black visible pixels + hidden pri bit preservation per VICE draw logic for invalid ECM combos).
    /// 
    /// VICE sources (explore subagent report ID 019e6acc-29b8-77f1-a9cc-56499af366f9 + the simulator validation work for TR-VIC-EDGE-001):
    /// native/vice/vice/src/viciisc/vicii-draw-cycle.c:41 (COL_NONE), :133-141 (colors[] table routes the three invalid (ECM=1 + BMM or MCM) combos entirely to COL_NONE rows),
    /// :196 (pixel_pri = (px &amp; 0x2) unconditional from gbuf data), :197-203 (cc = colors[vmode | px]; COL_NONE forces cc=0 visible black but does not affect pri),
    /// :224 (pri_buffer[i] = pixel_pri stored for every pixel), :401-428 (draw_sprites consumes pri_buffer[i] for priority decision and if (pixel_pri) sprite_background_collisions latch).
    /// 
    /// BDP (this tiny increment): Mocks/stubs (Zero machine path + simulator-derived pri pattern) written and validated green first (red for real native interop). Then minimal real wiring in shim + P/Invoke + bridge delegation.
    /// Requirements + precise VICE vicii-*.c line citations drive the surface and the extended fact assertions. No prod paths or lockstep touched. Minimal diff.
    /// </summary>
    /// <param name="machine">Native VICE machine handle (IntPtr.Zero for pure mock/stub path in BDP validation phase).</param>
    /// <param name="rasterLine">Raster line at which to sample the priority buffer (checkpoint boundary).</param>
    /// <param name="priBuffer">Destination buffer for per-pixel pri bits (0/1); caller pre-allocates at least 320 bytes.</param>
    /// <param name="width">Actual width written (320 for standard visible line).</param>
    /// <returns>True on success (mock always succeeds for contract validation).</returns>
    public static bool TryGetGraphicsPriorityBufferAtRaster(IntPtr machine, ushort rasterLine, byte[] priBuffer, out int width)
    {
        // BACKFILL-VIDEO-001 / TR-VIC-EDGE-001 / FR-VIC-002/003/005/008 / TEST-VIC-001 + explore report 019e6acc-29b8-77f1-a9cc-56499af366f9.
        // VICE: vicii-draw-cycle.c:196/224/401-428 (pri from px &amp; 0x2, stored in pri_buffer, consumed for collisions/prio even on COL_NONE).
        // This is the new contract surface for the increment. Mocks/stub path (below) validated first per BDP before any real delegation.
        width = 320;
        int required = 320;
        if (priBuffer == null || priBuffer.Length < required)
        {
            width = 0;
            return false;
        }

        if (machine != IntPtr.Zero)
        {
            // Real delegation (minimal wiring for the pri snapshot surface at raster boundary).
            // BACKFILL-VIDEO-001 / TR-VIC-EDGE-001 / VICE vicii-draw-cycle.c:196/224/401-428.
            int res = ViceNative.GetGraphicsPriorityAtRaster(machine, rasterLine, priBuffer, required);
            return res != 0;
        }

        // Mock/stub path (BDP "red" validated green before real wiring): self-contained pri pattern for invalid ECM checkpoint.
        // Represents a line with mixed pri bits (pri = (px &amp; 0x2) per VICE :196) for assertions vs simulator expectations (COL_NONE visible + pri preserved).
        for (int x = 0; x < 320; x++)
        {
            byte px = (byte)((x / 8) % 4); // cycling through px values to exercise pri bit (0x02 sets pri)
            bool pri = (px & 0x02) != 0;
            priBuffer[x] = pri ? (byte)1 : (byte)0;
        }
        return true;
    }

    /// <summary>
    /// TR-LOCKSTEP-VSF-001: main-CPU resume/pipeline state from the shim
    /// (vice_cpu_get_pipeline_state): the .vsf-restored x64sc in-flight context
    /// beyond the register file - MAINCPU last_opcode_info + BA-low stall flags
    /// (mainc64cpu.c snapshot module), the C64MEM 6510 processor port
    /// (c64memsnapshot.c pport block; selects ROM/IO banking), and the
    /// interrupt-status clocks. Used to stage the managed C64 so snapshot-resumed
    /// lockstep aligns from cycle 0.
    /// </summary>
    public static ViceNative.ViceCpuPipelineState GetCpuPipelineState(IntPtr machine)
    {
        var state = new ViceNative.ViceCpuPipelineState();
        ViceNative.GetCpuPipelineState(machine, ref state);
        return state;
    }

    public static void GetInterruptState(IntPtr machine, ref ViceInterruptState state)
    {
        var nativeState = new ViceNative.ViceInterruptState();
        ViceNative.GetInterruptState(machine, ref nativeState);

        state.IrqAsserted = nativeState.IrqAsserted;
        state.NmiAsserted = nativeState.NmiAsserted;
        state.GlobalPending = nativeState.GlobalPending;
        state.IrqSourceCount = nativeState.IrqSourceCount;
        state.NmiSourceCount = nativeState.NmiSourceCount;
    }

    public struct ViceVicState
    {
        public uint Cycle;
        public ushort RasterLine;
        public byte RasterCycle;
        public byte BadLine;
        public byte DisplayState;
        public byte SpriteDma;
        public byte[] Registers;

        /// <summary>
        /// Register file through native vicii_peek (vicii-mem.c:747-770); the
        /// CPU-visible debug view used by register-checkpoint comparisons
        /// against managed <c>Mos6569.Peek</c>. <see cref="Registers"/> stays
        /// the RAW vicii.regs store for raw-vs-raw parity tests.
        /// </summary>
        public byte[] RegistersPeek;

        /// <summary>TR-LOCKSTEP-VSF-001: .vsf allow_bad_lines latch (gates badline BA stalls this frame).</summary>
        public byte AllowBadLines;

        /// <summary>TR-LOCKSTEP-VSF-001: .vsf display/idle g-access state.</summary>
        public byte IdleState;
    }

    public struct ViceCiaState
    {
        public byte PortA;
        public byte PortB;
        public byte DdrA;
        public byte DdrB;
        public ushort TimerA;
        public ushort TimerB;
        public byte Icr;
        public byte Cra;
        public byte Crb;
        public byte InterruptFlag;

        /// <summary>TR-LOCKSTEP-VSF-001: Timer A reload latch.</summary>
        public ushort TimerALatch;

        /// <summary>TR-LOCKSTEP-VSF-001: Timer B reload latch.</summary>
        public ushort TimerBLatch;

        /// <summary>TR-LOCKSTEP-VSF-001: ICR interrupt-enable mask.</summary>
        public byte IrqMask;
    }

    public struct ViceSidState
    {
        public byte[] Registers;
        public uint[] Accumulators;
        public byte[] Envelopes;
        public uint FilterState;
    }

    public struct ViceInterruptState
    {
        public byte IrqAsserted;
        public byte NmiAsserted;
        public byte GlobalPending;
        public byte IrqSourceCount;
        public byte NmiSourceCount;
    }
}