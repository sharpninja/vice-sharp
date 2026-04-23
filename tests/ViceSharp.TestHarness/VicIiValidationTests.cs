using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using Xunit;

namespace ViceSharp.TestHarness;

public sealed class VicIiValidationTests : LockstepTestRunner<Mos6569>, IAsyncLifetime
{
    private readonly ViceMachineValidationFixture _fixture = new();

    public VicIiValidationTests()
    {
        InitializeChip((_fixture.ManagedMachine.Devices.GetByRole(DeviceRole.VideoChip) as Mos6569)
            ?? throw new InvalidOperationException("Managed C64 machine did not expose a PAL VIC-II."));
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
    public void FirstTwoScanlines_MatchVICE()
    {
        _fixture.ResetBoth();
        var cyclesToCompare = 63 * 2;

        for (var cycle = 0; cycle < cyclesToCompare; cycle++)
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
            ["Cycle"] = Chip.CycleCounter,
            ["RasterLine"] = Chip.CurrentRasterLine,
            ["RasterCycle"] = Chip.CurrentCycle,
            ["BadLine"] = Chip.IsBadLine,
            ["Registers"] = ReadManagedRegisters()
        };
    }

    protected override Dictionary<string, object?> GetExpectedState()
    {
        var viceState = new ViceNativeBridge.ViceVicState();
        ViceNativeBridge.GetVicState(_fixture.NativeMachine, ref viceState);

        return new Dictionary<string, object?>
        {
            ["Cycle"] = viceState.Cycle,
            ["RasterLine"] = viceState.RasterLine,
            ["RasterCycle"] = viceState.RasterCycle,
            ["BadLine"] = viceState.BadLine != 0,
            ["Registers"] = viceState.Registers
        };
    }

    private byte[] ReadManagedRegisters()
    {
        var registers = new byte[0x40];
        for (var i = 0; i < registers.Length; i++)
        {
            registers[i] = Chip.Peek((ushort)(Chip.BaseAddress + i));
        }

        return registers;
    }

    private static string FormatDifferences(IReadOnlyList<StateDifference> differences)
    {
        return string.Join(Environment.NewLine, differences.Select(d => d.ToString()));
    }
}
