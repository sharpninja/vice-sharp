using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using Xunit;

namespace ViceSharp.TestHarness;

[Collection("NativeVice")]
public sealed class CpuValidationTests : LockstepTestRunner<Mos6502>, IAsyncLifetime
{
    private readonly ViceMachineValidationFixture _fixture = new();

    public CpuValidationTests()
    {
        InitializeChip((_fixture.ManagedMachine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502)
            ?? throw new InvalidOperationException("Managed C64 machine did not expose a MOS 6502 CPU."));
    }

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    /// <summary>
    /// FR: FR-CPU-001, TR: TR-CYCLE-001.
    /// Use case: Verify managed 6502 reset state matches the native VICE
    /// reference after both CPUs are reset from identical machine fixtures.
    /// Acceptance: Managed register file (A, X, Y, P, S, PC) is bit-for-bit
    /// equal to the native VICE CPU's register file immediately after reset.
    /// </summary>
    [ViceFact]
    public void Reset_StateMatchesVICE()
    {
        _fixture.ResetBoth();

        var result = CompareStates(GetActualState(), GetExpectedState());
        Assert.True(result.Passed, FormatDifferences(result.Differences));
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: Step the managed and native 6502 lockstep for the first 64
    /// post-reset cycles and compare register state after each cycle.
    /// Acceptance: Managed CPU state matches native VICE state at every one
    /// of the first 64 cycles; any mismatch fails the test with the cycle
    /// number and offending registers.
    /// </summary>
    [ViceFact]
    public void First64Cycles_MatchVICE()
    {
        _fixture.ResetBoth();

        for (var cycle = 0; cycle < 64; cycle++)
        {
            _fixture.StepNative();
            var result = StepAndCompare();
            Assert.True(result.Passed, $"Cycle {cycle + 1}: {FormatDifferences(result.Differences)}");
        }
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-DET-001.
    /// Use case: Capture a deterministic before/after register trace for the
    /// managed and native CPUs across the first 64 boot cycles so that any
    /// divergence has a side-by-side cycle log for diagnosis.
    /// Acceptance: All 64 cycles produce zero managed-vs-native differences;
    /// when any cycle differs the test throws a combined trace including the
    /// pre-step, post-step and divergence summary for each failing cycle.
    /// </summary>
    [ViceFact]
    public void BootstrapTrace_First12Cycles()
    {
        _fixture.ResetBoth();
        var lines = new List<string>();
        var failures = new List<string>();

        for (var cycle = 0; cycle < 64; cycle++)
        {
            var beforeManaged = GetActualState();
            var beforeNative = GetExpectedState();

            _fixture.StepNative();
            var native = GetExpectedState();

            var managed = StepAndCompare().Differences; // capture for inline check
            var managedAfterState = GetActualState();

            lines.Add($"Cycle {cycle + 1}:");
            lines.Add($"  Before managed = {FormatState(beforeManaged)}");
            lines.Add($"  Before native = {FormatState(beforeNative)}");
            lines.Add($"  After managed = {FormatState(managedAfterState)}");
            lines.Add($"  After native = {FormatState(native)}");
            if (managed.Count > 0)
            {
                failures.Add($"Cycle {cycle + 1}: {string.Join(Environment.NewLine, managed.Select(d => d.ToString()))}");
            }
        }

        if (failures.Count > 0)
        {
            throw new Xunit.Sdk.XunitException(string.Join(Environment.NewLine, lines.Concat(failures)));
        }
    }

    protected override void StepManaged() => _fixture.StepManaged();

    protected override Dictionary<string, object?> GetActualState()
    {
        return new Dictionary<string, object?>
        {
            ["A"] = Chip.A,
            ["X"] = Chip.X,
            ["Y"] = Chip.Y,
            ["P"] = Chip.P,
            ["S"] = Chip.S,
            ["PC"] = Chip.PC
        };
    }

    protected override Dictionary<string, object?> GetExpectedState()
    {
        return new Dictionary<string, object?>
        {
            ["A"] = ViceNativeBridge.GetCpuRegister(_fixture.NativeMachine, 0),
            ["X"] = ViceNativeBridge.GetCpuRegister(_fixture.NativeMachine, 1),
            ["Y"] = ViceNativeBridge.GetCpuRegister(_fixture.NativeMachine, 2),
            ["P"] = ViceNativeBridge.GetCpuRegister(_fixture.NativeMachine, 3),
            ["S"] = ViceNativeBridge.GetCpuRegister(_fixture.NativeMachine, 4),
            ["PC"] = (ushort)(ViceNativeBridge.GetCpuRegister(_fixture.NativeMachine, 5) | (ViceNativeBridge.GetCpuRegister(_fixture.NativeMachine, 6) << 8))
        };
    }

    private static string FormatDifferences(IReadOnlyList<StateDifference> differences)
    {
        return string.Join(Environment.NewLine, differences.Select(d => d.ToString()));
    }

    private static string FormatState(IReadOnlyDictionary<string, object?> state)
    {
        return string.Join(
            ", ",
            state
                .Where(entry => entry.Key is "A" or "X" or "Y" or "S" or "P" or "PC")
                .OrderBy(entry => entry.Key)
                .Select(entry => $"{entry.Key}={FormatRegisterValue(entry.Value)}"));
    }

    private static string FormatRegisterValue(object? value)
        => value is ushort value16 ? $"0x{value16:X4}" : value is byte value8 ? $"0x{value8:X2}" : $"{value}";

}
