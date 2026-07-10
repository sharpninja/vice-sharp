namespace ViceSharp.TestHarness;

using System.Text;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR: FR-VIC-001, TR: TR-LOCKSTEP-VSF-001, TEST: TEST-NATIVE-RESIDUE-01.
/// Use case: diagnostic probe for the in-suite lockstep cascade - a fresh
/// native machine created AFTER a demo .vsf snapshot resume must present the
/// same boot state as one created before it. The probe dumps both states and
/// diffs CPU registers, cycle counter, VIC beam phase, raw and peek register
/// files, and a kernal-workspace RAM window.
/// Acceptance: the diff is empty; the failure message enumerates the exact
/// residue fields so the shim reset can be fixed at the mechanism.
/// </summary>
[Collection("NativeVice")]
public sealed class NativeResidueDiagTests
{
    private const int PalCyclesPerFrame = 63 * 312;

    private const uint Unit8 = 8;

    private static readonly string[] SnapshotCandidates =
    [
        Path.Combine(AppContext.BaseDirectory, "TestData", "pieces_of_light.vsf"),
        @"F:\GitHub\vice-sharp\vice-snapshot-20260630171307.vsf",
    ];

    private static readonly string DiskFixture =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "D64", "pieces_of_light.d64");

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-LOCKSTEP-VSF-001, TEST: TEST-NATIVE-RESIDUE-01.
    /// Use case: a fresh native machine created AFTER a demo .vsf resume must
    /// boot with the same state as one created before it, or every later
    /// lockstep test in the process inherits order-dependent divergence.
    /// Acceptance: the before/after boot-state diff (CPU registers, cycle,
    /// VIC beam phase, raw+peek register files, zero-page RAM) is empty; on
    /// failure the message names the exact residue fields.
    /// </summary>
    [ViceFact]
    public void FreshMachine_BootState_IsIdentical_AfterSnapshotResume()
    {
        var vsf = Array.Find(SnapshotCandidates, File.Exists);
        Assert.SkipWhen(vsf is null, "Staged demo .vsf snapshot not present.");

        var before = CaptureFreshBootState();

        // Poison attempt: resume the demo snapshot on a scratch machine, step a
        // little so alarms/beam advance, then destroy it.
        using (var scratch = ViceNative.CreateInstance("c64"))
        {
            scratch.Reset();
            Assert.True(scratch.ReadSnapshot(vsf!) == 0, "snapshot must resume");
            for (var i = 0; i < 5000; i++)
                scratch.Step();
        }

        var after = CaptureFreshBootState();

        var diff = new StringBuilder();
        foreach (var (key, b) in before)
        {
            var a = after[key];
            if (!string.Equals(a, b, StringComparison.Ordinal))
                diff.AppendLine($"{key}: before={b} after={a}");
        }

        Assert.True(diff.Length == 0, $"native boot state changed after snapshot resume:\n{diff}");
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-LOCKSTEP-VSF-001, TEST: TEST-NATIVE-RESIDUE-02.
    /// Use case: the surviving .vsf residue only manifests after a full frame
    /// of activity followed by a reset (X64Sc ResetAfterActivity diverges at
    /// post-reset CPU cycle ~167-197 with an $80-bit LDA difference,
    /// fingerprinting a CIA2 $DD00 serial DATA-IN read). This probe replicates
    /// that exact sequence - boot, one PAL frame, reset, then a per-cycle walk
    /// of the divergence window sampling $DD00/$DD01, CIA1+CIA2 state, VIC
    /// beam phase, and CPU registers - before and after a poison resume.
    /// Acceptance: the before/after trace diff is empty; on failure the
    /// message names the first divergent checkpoint and field.
    /// </summary>
    [ViceFact]
    public void FreshMachine_PostActivityResetTrace_IsIdentical_AfterSnapshotResume()
    {
        var vsf = Array.Find(SnapshotCandidates, File.Exists);
        Assert.SkipWhen(vsf is null, "Staged demo .vsf snapshot not present.");

        var before = CapturePostActivityResetTrace();

        using (var scratch = ViceNative.CreateInstance("c64"))
        {
            scratch.Reset();
            Assert.True(scratch.ReadSnapshot(vsf!) == 0, "snapshot must resume");
            for (var i = 0; i < 5000; i++)
                scratch.Step();
        }

        var after = CapturePostActivityResetTrace();

        var diff = new StringBuilder();
        foreach (var (key, b) in before)
        {
            var a = after[key];
            if (!string.Equals(a, b, StringComparison.Ordinal))
                diff.AppendLine($"{key}: before={b} after={a}");
        }

        Assert.True(diff.Length == 0, $"post-activity-reset trace changed after snapshot resume:\n{diff}");
    }

    /// <summary>
    /// FR: FR-NATIVERESIDUE-001, TR: TR-VSFLOCKSTEP-RESUME-001, TEST: TEST-NATIVE-RESIDUE-04.
    /// Use case: the drive CPU carries a 16.16 fixed-point fractional cycle
    /// accumulator (drivecpu_context_t.cycle_accum) that drivecpu_reset_clk
    /// resets for last_clk/last_exc_cycles/stop_clk but NOT for cycle_accum. A
    /// scratch machine that resumes a true-drive .vsf and runs drive activity
    /// advances that fraction to a nonzero remainder; because diskunit_context[]
    /// is process-global and never torn down, the next machine's create-time TDE
    /// re-baseline calls drivecpu_reset_clk (which leaves the stale fraction),
    /// phase-shifting the drive CPU and diverging the next kernal-boot lockstep
    /// at cycle 167 (BUG-LOCKSTEP-001).
    /// Acceptance: a machine created after the poison presents drive-8
    /// cycle_accum == 0 (the fresh-boot baseline); the poison step first proves
    /// the scratch drive carried a nonzero fraction.
    /// </summary>
    [ViceFact]
    public void FreshMachine_DriveCycleAccum_IsZero_AfterSnapshotResumeActivity()
    {
        var vsf = Array.Find(SnapshotCandidates, File.Exists);
        Assert.SkipWhen(vsf is null, "Staged demo .vsf snapshot not present.");

        ulong poisonAccum;
        var scratch = ViceNativeBridge.CreateMachine("c64");
        try
        {
            ViceNativeBridge.ResetMachine(scratch);
            Assert.True(ViceNativeBridge.ReadSnapshot(scratch, vsf!) == 0, "snapshot must resume");
            for (var i = 0; i < 5000; i++)
                ViceNativeBridge.StepCycle(scratch);
            poisonAccum = ViceNativeBridge.GetDriveCycleAccum(scratch, Unit8);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(scratch);
        }

        Assert.True(poisonAccum != 0,
            $"poison precondition: expected a nonzero drive-8 cycle_accum after snapshot activity (fixture may have changed), got {poisonAccum}");

        var check = ViceNativeBridge.CreateMachine("c64");
        try
        {
            var residue = ViceNativeBridge.GetDriveCycleAccum(check, Unit8);
            Assert.True(residue == 0,
                $"drive-8 cycle_accum residue after snapshot resume: expected 0, got {residue} (scratch poison was {poisonAccum})");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(check);
        }
    }

    /// <summary>
    /// FR: FR-NATIVERESIDUE-001, TR: TR-VSFLOCKSTEP-RESUME-001, TEST: TEST-NATIVE-RESIDUE-03.
    /// Use case: the three drive_t IEC-event clocks
    /// (attach_clk/detach_clk/attach_detach_clk) are set by disk attach/detach
    /// and by drive_snapshot_read_module, and survive a machine boundary because
    /// nothing at create re-zeroes them. Phase 1 (deterministic) attaches then
    /// detaches a disk on a scratch machine so the clocks go nonzero, destroys
    /// it, and asserts a fresh machine boots with all three at 0. Phase 2
    /// (mechanism guard) synthesizes the has_tde=0 uninitialized-stack path:
    /// save a snapshot with true-drive OFF (the module carries has_tde=0), then
    /// resume it and assert the restored clocks are 0 rather than stack garbage.
    /// Acceptance: after both scenarios the drive-8 clock residue is 0/0/0; the
    /// poison steps first prove the scratch drive carried nonzero clocks.
    /// </summary>
    [ViceFact]
    public void FreshMachine_DriveAttachDetachClocks_AreZero_AfterResidue()
    {
        // Phase 1: attach/detach value persistence across a machine boundary.
        Assert.SkipWhen(!File.Exists(DiskFixture), "Staged demo .d64 fixture not present.");

        ulong poisonAttach, poisonDetach;
        var scratch = ViceNativeBridge.CreateMachine("c64");
        try
        {
            ViceNativeBridge.ResetMachine(scratch);
            // Advance diskunit_clk past 0 so an attach stamps a nonzero clock.
            for (var i = 0; i < 100000; i++)
                ViceNativeBridge.StepCycle(scratch);
            ViceNativeBridge.AttachDisk(scratch, Unit8, 0, DiskFixture);
            _ = ViceNativeBridge.GetDriveClockResidue(scratch, Unit8, out poisonAttach, out _, out _);
            ViceNativeBridge.DetachDisk(scratch, Unit8, 0);
            _ = ViceNativeBridge.GetDriveClockResidue(scratch, Unit8, out _, out poisonDetach, out _);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(scratch);
        }

        Assert.True(poisonAttach != 0 || poisonDetach != 0,
            $"poison precondition: expected nonzero attach/detach clock after disk activity, got attach={poisonAttach} detach={poisonDetach}");

        var check = ViceNativeBridge.CreateMachine("c64");
        try
        {
            var rc = ViceNativeBridge.GetDriveClockResidue(check, Unit8, out var attach, out var detach, out var attachDetach);
            Assert.True(rc == 0, "clock residue readout must succeed on a fresh machine");
            Assert.True(attach == 0 && detach == 0 && attachDetach == 0,
                $"drive-8 clock residue after attach/detach: expected 0/0/0, got attach={attach} detach={detach} attachDetach={attachDetach}");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(check);
        }

        // Phase 2: has_tde=0 uninitialized-stack path. Save a true-drive-OFF
        // snapshot through the shim (the DRIVE8 module then carries has_tde=0),
        // resume it with true-drive back ON, and require the restored clocks to
        // be 0 rather than the uninitialized stack locals VICE would otherwise
        // copy in. Phase 2 red is not guaranteed pre-fix (stack luck), but its
        // green is deterministic post-fix (the zero-init makes the copy 0).
        var tdePath = Path.Combine(Path.GetTempPath(), $"vs-residue-tdeoff-{Guid.NewGuid():N}.vsf");
        var mech = ViceNativeBridge.CreateMachine("c64");
        try
        {
            ViceNativeBridge.ResetMachine(mech);
            for (var i = 0; i < 1000; i++)
                ViceNativeBridge.StepCycle(mech);

            Assert.True(ViceNativeBridge.SetDriveTrueEmulation(mech, Unit8, false) == 0, "TDE off must succeed");
            Assert.True(ViceNativeBridge.WriteSnapshot(mech, tdePath) == 0, "TDE-off snapshot must save");
            Assert.True(ViceNativeBridge.SetDriveTrueEmulation(mech, Unit8, true) == 0, "TDE on must succeed");
            Assert.True(ViceNativeBridge.ReadSnapshot(mech, tdePath) == 0, "TDE-off snapshot must resume");

            var rc = ViceNativeBridge.GetDriveClockResidue(mech, Unit8, out var attach, out var detach, out var attachDetach);
            Assert.True(rc == 0, "clock residue readout must succeed after resume");
            Assert.True(attach == 0 && detach == 0 && attachDetach == 0,
                $"drive-8 clock residue after has_tde=0 resume: expected 0/0/0, got attach={attach} detach={detach} attachDetach={attachDetach}");
            // Piggyback regression guard on the ba0f94f TDE re-baseline.
            Assert.True(ViceNativeBridge.GetDriveTrueEmulation(mech, Unit8) == 1,
                "true-drive emulation must be restored to the VICE default after resume");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(mech);
            if (File.Exists(tdePath))
                File.Delete(tdePath);
        }
    }

    private static SortedDictionary<string, string> CapturePostActivityResetTrace()
    {
        var state = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var machine = ViceNativeBridge.CreateMachine("c64");
        try
        {
            ViceNativeBridge.ResetMachine(machine);

            // One full PAL frame of boot activity, mirroring ResetAfterActivity.
            for (var i = 0; i < PalCyclesPerFrame; i++)
                ViceNativeBridge.StepCycle(machine);

            CaptureCheckpoint(machine, state, "postFrame");

            ViceNativeBridge.ResetMachine(machine);
            CaptureCheckpoint(machine, state, "postReset");

            // Walk the divergence window (the X64Sc canary fails at driver
            // cycle ~167-197) sampling the serial-visible surface per cycle.
            for (var cycle = 1; cycle <= 260; cycle++)
            {
                ViceNativeBridge.StepCycle(machine);
                if (cycle >= 140)
                {
                    var dd00 = ViceNativeBridge.ReadMemory(machine, 0xDD00);
                    var a = ViceNativeBridge.GetCpuRegister(machine, 0);
                    state[$"walk[{cycle:D3}]"] = $"DD00={dd00:X2} A={a:X2}";
                }
            }

            CaptureCheckpoint(machine, state, "postWalk");
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(machine);
        }

        return state;
    }

    private static void CaptureCheckpoint(IntPtr machine, SortedDictionary<string, string> state, string label)
    {
        for (var reg = 0; reg < 5; reg++)
            state[$"{label}.cpu[{reg}]"] = ViceNativeBridge.GetCpuRegister(machine, reg).ToString("X2");

        var vic = new ViceNativeBridge.ViceVicState();
        ViceNativeBridge.GetVicState(machine, ref vic);
        state[$"{label}.vic"] =
            $"line={vic.RasterLine} x={vic.RasterCycle} bad={vic.BadLine} allow={vic.AllowBadLines} idle={vic.IdleState} dma={vic.SpriteDma:X2}";
        state[$"{label}.vic.regsPeek"] = Convert.ToHexString(vic.RegistersPeek);

        for (var ciaIndex = 0; ciaIndex < 2; ciaIndex++)
        {
            var cia = new ViceNativeBridge.ViceCiaState();
            ViceNativeBridge.GetCiaState(machine, ciaIndex, ref cia);
            state[$"{label}.cia{ciaIndex + 1}"] =
                $"PA={cia.PortA:X2} PB={cia.PortB:X2} DDRA={cia.DdrA:X2} DDRB={cia.DdrB:X2} TA={cia.TimerA:X4} TB={cia.TimerB:X4} " +
                $"ICR={cia.Icr:X2} CRA={cia.Cra:X2} CRB={cia.Crb:X2} IFLAG={cia.InterruptFlag:X2} LA={cia.TimerALatch:X4} LB={cia.TimerBLatch:X4} MASK={cia.IrqMask:X2}";
        }

        state[$"{label}.dd00"] = ViceNativeBridge.ReadMemory(machine, 0xDD00).ToString("X2");
        state[$"{label}.dd01"] = ViceNativeBridge.ReadMemory(machine, 0xDD01).ToString("X2");
    }

    private static SortedDictionary<string, string> CaptureFreshBootState()
    {
        var state = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var machine = ViceNativeBridge.CreateMachine("c64");
        try
        {
            ViceNativeBridge.ResetMachine(machine);
            var stepEnv = Environment.GetEnvironmentVariable("VICESHARP_RESIDUE_STEPS");
            var steps = int.TryParse(stepEnv, out var parsed) ? parsed : 200;
            for (var i = 0; i < steps; i++)
                ViceNativeBridge.StepCycle(machine);

            for (var reg = 0; reg < 5; reg++)
                state[$"cpu[{reg}]"] = ViceNativeBridge.GetCpuRegister(machine, reg).ToString("X2");

            var vic = new ViceNativeBridge.ViceVicState();
            ViceNativeBridge.GetVicState(machine, ref vic);
            state["vic.cycle"] = vic.Cycle.ToString();
            state["vic.rasterLine"] = vic.RasterLine.ToString();
            state["vic.rasterCycle"] = vic.RasterCycle.ToString();
            state["vic.badLine"] = vic.BadLine.ToString();
            state["vic.allowBadLines"] = vic.AllowBadLines.ToString();
            state["vic.idleState"] = vic.IdleState.ToString();
            state["vic.spriteDma"] = vic.SpriteDma.ToString("X2");
            state["vic.regsRaw"] = Convert.ToHexString(vic.Registers);
            state["vic.regsPeek"] = Convert.ToHexString(vic.RegistersPeek);

            var ram = new byte[64];
            for (ushort address = 0; address < 64; address++)
                ram[address] = ViceNativeBridge.PeekRam(machine, address);
            state["ram.zp"] = Convert.ToHexString(ram);
        }
        finally
        {
            ViceNativeBridge.DestroyMachine(machine);
        }

        return state;
    }
}
