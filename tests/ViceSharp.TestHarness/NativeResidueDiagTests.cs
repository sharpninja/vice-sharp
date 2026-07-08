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
[Trait("Category", "SnapshotResume")]
public sealed class NativeResidueDiagTests
{
    private static readonly string[] SnapshotCandidates =
    [
        Path.Combine(AppContext.BaseDirectory, "TestData", "pieces_of_light.vsf"),
        @"F:\GitHub\vice-sharp\vice-snapshot-20260630171307.vsf",
    ];

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
