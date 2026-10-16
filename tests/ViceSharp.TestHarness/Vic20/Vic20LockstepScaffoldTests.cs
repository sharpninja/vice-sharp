namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// TR-VIC20-LOCKSTEP-001 / Slice N0-N1.
/// Use case: native <c>xvic</c> oracle creates Vic20 machines and steps CPU
/// in lockstep-compatible fashion with the managed Vic20 path.
/// Acceptance: when <c>vice_xvic.dll</c> is present, Vic20 selectors create a
/// real machine, reset lands at the kernal reset vector, and multi-cycle step
/// advances PC/cycle. When the DLL is absent, create fails closed. Managed
/// dual-machine determinism remains the interim accuracy gate.
/// </summary>
public sealed class Vic20LockstepScaffoldTests
{
    /// <summary>
    /// N0: Vic20 profile selectors create via vice_xvic when the oracle is built.
    /// </summary>
    [Theory]
    [InlineData("vic20")]
    [InlineData("vic20ntsc")]
    [InlineData("xvic")]
    public void NativeShim_CreatesVic20Models_WhenXvicOraclePresent(string modelSelector)
    {
        if (!ViceNativeXvic.IsAvailable)
        {
            // Fail closed until N0 build ships vice_xvic.dll (same as pre-N0).
            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                using var native = ViceNative.CreateInstance(modelSelector);
                _ = native;
            });

            Assert.True(
                ex is InvalidOperationException or DllNotFoundException or EntryPointNotFoundException,
                $"unexpected exception type {ex.GetType().Name}: {ex.Message}");
            return;
        }

        using var instance = ViceNative.CreateInstance(modelSelector);
        instance.Reset();
        var state = instance.GetState();
        // Kernal reset vector is non-zero; post-reset PC is in kernal space.
        Assert.True(state.PC >= 0xE000, $"expected kernal PC after reset, got ${state.PC:X4}");
    }

    /// <summary>
    /// N1 precursor: native step advances master cycle when oracle is present.
    /// </summary>
    [Fact]
    public void NativeXvic_StepAdvancesCycle_WhenOraclePresent()
    {
        if (!ViceNativeXvic.IsAvailable)
        {
            return; // skip-by-return: oracle not built on this agent
        }

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var before = native.GetState();
        for (var i = 0; i < 64; i++)
            native.Step();
        var after = native.GetState();
        Assert.True(after.Cycle > before.Cycle, $"cycle did not advance: before={before.Cycle} after={after.Cycle}");
    }

    /// <summary>
    /// Interim accuracy: managed Vic20 machines stay deterministic (N2 precursor).
    /// </summary>
    [Fact]
    public void ManagedVic20_TwoMachines_AgreeAfterFrames_UntilNativeXvicExists()
    {
        var a = MachineTestFactory.CreateVic20Machine("vic20");
        var b = MachineTestFactory.CreateVic20Machine("vic20");
        for (var i = 0; i < 16; i++)
        {
            a.RunFrame();
            b.RunFrame();
        }

        Assert.Equal(a.GetState().PC, b.GetState().PC);
        Assert.Equal(a.GetState().Cycle, b.GetState().Cycle);
    }

    /// <summary>
    /// N1: short native vs managed CPU lockstep (PC + A) after equal cycle counts.
    /// </summary>
    [Fact]
    public void NativeXvic_ManagedLockstep_CpuRegs_WhenOraclePresent()
    {
        if (!ViceNativeXvic.IsAvailable)
        {
            return;
        }

        using var native = ViceNative.CreateInstance("vic20");
        native.Reset();
        var managed = MachineTestFactory.CreateVic20Machine("vic20");
        // Align both at power-on reset (factory already boots; re-run Reset if exposed).
        managed.Reset();

        const int cycles = 256;
        for (var i = 0; i < cycles; i++)
        {
            native.Step();
            managed.Clock.Step();
        }

        var n = native.GetState();
        var m = managed.GetState();
        Assert.Equal(n.PC, m.PC);
        Assert.Equal(n.A, m.A);
        Assert.Equal(n.X, m.X);
        Assert.Equal(n.Y, m.Y);
        Assert.Equal(n.S, m.S);
    }
}
