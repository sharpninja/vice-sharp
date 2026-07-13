namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S2 (IMPL-XBOXUWP-002). FR-INPROC (TR-INPROC-002/003/005), TEST-INPROC-003.
/// The AppContainer-safe managed emulation gate: no OS timer, no aux thread, no P/Invoke; it
/// paces inline via injectable sleep/clock seams and shares PacingMath with the desktop gate.
/// All cases run off-console (Tier H) against a minimal session (no C64 ROMs required).
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxManagedFrameGateTests
{
    [Fact]
    public void Name_IsXbox()
    {
        using var gate = new XboxManagedFrameGate();
        Assert.Equal("Xbox", gate.Name);
    }

    [Fact]
    public void StartStop_AreIdempotent_AndTheGateOwnsNoThread()
    {
        // No auxiliary timer thread field exists on the managed gate (unlike SemaphoreEmulationGate).
        Assert.DoesNotContain(
            typeof(XboxManagedFrameGate).GetFields(BindingFlags.Instance | BindingFlags.NonPublic),
            field => field.FieldType == typeof(System.Threading.Thread));

        using var gate = new XboxManagedFrameGate();
        gate.Start();
        gate.Start();
        gate.Stop();
        gate.Stop();
        // Idempotent: repeated Start/Stop must not throw.
    }

    [Fact]
    public void Gate_DeclaresNoPInvoke()
    {
        var methods = typeof(XboxManagedFrameGate).GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            Assert.Null(method.GetCustomAttribute<DllImportAttribute>());
            Assert.DoesNotContain(method.GetCustomAttributes(), a => a.GetType().Name == "LibraryImportAttribute");
        }
    }

    [Fact]
    public void Tick_EmptyRegistry_ReturnsFalse_AndDoesNotSleep()
    {
        var sleeps = new List<int>();
        using var gate = new XboxManagedFrameGate(sleep: sleeps.Add);

        var ran = gate.Tick(new EmulatorRuntimeRegistry(), static (_, cycles) => cycles);

        Assert.False(ran);
        Assert.Empty(sleeps);
    }

    [Fact]
    public void Tick_StoppedSession_ReturnsFalse()
    {
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(CreateSession()); // default RunState = Stopped
        var sleeps = new List<int>();
        using var gate = new XboxManagedFrameGate(sleep: sleeps.Add);

        Assert.False(gate.Tick(registry, static (_, cycles) => cycles));
        Assert.Empty(sleeps);
    }

    [Fact]
    public void Tick_RunningLimitedSession_Advances_AndSleepsPacedQuantum()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateSession();
        session.RunState = EmulatorRunState.Running;
        session.SetLimiter(100, enabled: true);
        registry.Add(session);

        long advanced = 0;
        var sleeps = new List<int>();
        using var gate = new XboxManagedFrameGate(sleep: sleeps.Add, nowTicks: static () => 0, stopwatchFrequency: 1_000_000);

        var ran = gate.Tick(registry, (_, cycles) => { advanced += cycles; return cycles; });

        Assert.True(ran);
        Assert.True(advanced > 0, "the paced quantum guarantees forward progress even at zero deficit");
        Assert.Equal(new[] { ExpectedPacedSleepMs }, sleeps); // limited -> non-zero managed yield
    }

    [Fact]
    public void Tick_WarpSession_Advances_AndBypassesBlockingSleep()
    {
        var registry = new EmulatorRuntimeRegistry();
        var session = CreateSession();
        session.RunState = EmulatorRunState.Running;
        session.SetLimiter(100, enabled: false); // warp
        registry.Add(session);

        long advanced = 0;
        var sleeps = new List<int>();
        using var gate = new XboxManagedFrameGate(sleep: sleeps.Add, nowTicks: static () => 0, stopwatchFrequency: 1_000_000);

        var ran = gate.Tick(registry, (_, cycles) => { advanced += cycles; return cycles; });

        Assert.True(ran);
        Assert.True(advanced > 0);
        Assert.Equal(new[] { 0 }, sleeps); // warp -> sleep(0), runs flat out
    }

    private static int ExpectedPacedSleepMs => Math.Max(1, (int)Math.Round(1000.0 / 500.0)); // PacingHz = 500 -> 2 ms

    private static EmulatorRuntimeSession CreateSession()
    {
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);

        return factory.Create(new CreateEmulatorSessionRequest(MinimalHostArchitectureDescriptor.ArchitectureId));
    }
}

/// <summary>
/// PLAN-XBOXUWP slice S2. Golden-value tests locking the pure pacing math shared by both gates
/// (PacingMath), so a future edit to the deficit/quantum/warp formulas is caught. Parity with the
/// former SemaphoreEmulationGate math is guaranteed by construction (SemaphoreEmulationGate now
/// delegates to PacingMath) and re-verified by the existing SemaphoreEmulationGatePacingTests.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class PacingMathTests
{
    private const long Freq = 985248;   // PAL C64-ish clock, stable golden values
    private const long Sw = 1_000_000;  // stopwatch frequency

    [Fact]
    public void Quantum_IsClockSecondOverPacingHz()
        => Assert.Equal(1970, PacingMath.ComputePacedQuantumCycles(Freq, 100)); // 985248 / 500 = 1970.49 -> 1970

    [Fact]
    public void Deficit_OnAnchor_IsZero()
    {
        var deficit = PacingMath.ComputeRealtimeDeficit(Freq, 100, Sw, 0, 0, 0, 0, out var resync);
        Assert.Equal(0, deficit);
        Assert.False(resync);
    }

    [Fact]
    public void Deficit_Ahead_ClampsToZero()
    {
        var deficit = PacingMath.ComputeRealtimeDeficit(Freq, 100, Sw, 0, 0, 0, 1000, out _);
        Assert.Equal(0, deficit);
    }

    [Fact]
    public void Deficit_OneSecondBehind_CapsAtStepCap()
    {
        var deficit = PacingMath.ComputeRealtimeDeficit(Freq, 100, Sw, 0, 0, Sw, 0, out var resync);
        Assert.Equal(Freq / 4, deficit);
        Assert.False(resync);
    }

    [Fact]
    public void Deficit_CatastrophicGap_ResyncsToStepCap()
    {
        var deficit = PacingMath.ComputeRealtimeDeficit(Freq, 100, Sw, 0, 0, 5 * Sw, 0, out var resync);
        Assert.Equal(Freq / 4, deficit);
        Assert.True(resync);
    }

    [Fact]
    public void LimitedAdvance_IsDeficitPlusQuantum()
    {
        var advance = PacingMath.ComputeLimitedAdvanceCycles(Freq, 100, Sw, 0, 0, Sw, 0, out _);
        Assert.Equal((Freq / 4) + 1970, advance);
    }

    [Fact]
    public void WarpSlice_IsQuantumTimesBurst()
        => Assert.Equal((Freq / 500) * 64, PacingMath.WarpSliceCycles(Freq)); // 1970 * 64 = 126080
}
