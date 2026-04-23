using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using Xunit;

namespace ViceSharp.TestHarness;

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

    [ViceFact]
    public void Reset_StateMatchesVICE()
    {
        _fixture.ResetBoth();

        var result = CompareStates(GetActualState(), GetExpectedState());
        Assert.True(result.Passed, FormatDifferences(result.Differences));
    }

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
}
