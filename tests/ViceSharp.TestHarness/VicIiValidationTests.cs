namespace ViceSharp.TestHarness;

using Xunit;
using ViceSharp.Chips.Video;

public sealed class VicIiValidationTests : LockstepTestRunner<VicII>, IAsyncLifetime
{
    private IntPtr _viceMachine;

    public VicIiValidationTests()
        : base(new VicII(null!, null!))
    {
    }

    public async ValueTask InitializeAsync()
    {
        _viceMachine = ViceNativeBridge.CreateMachine();
        ViceNativeBridge.ResetMachine(_viceMachine);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    [Fact]
    public void Reset_StateMatchesVICE()
    {
        Chip.Reset();

        var actualState = CaptureState(Chip);
        var expectedState = GetExpectedState();

        var result = CompareStates(actualState, expectedState);

        Assert.True(result.Passed, FormatDifferences(result.Differences));
    }

    [Fact]
    public void RasterLine_IncrementMatchesVICE()
    {
        for (int line = 0; line < 312; line++)
        {
            for (int cycle = 0; cycle < 63; cycle++)
            {
                ViceNativeBridge.StepCycle(_viceMachine);
                var result = StepAndCompare();

                Assert.True(result.Passed, $"Line {line} Cycle {cycle}: {FormatDifferences(result.Differences)}");
            }
        }
    }

    protected override Dictionary<string, object> GetExpectedState()
    {
        var viceState = new ViceNativeBridge.ViceVicState();
        ViceNativeBridge.GetVicState(_viceMachine, ref viceState);

        return new Dictionary<string, object>
        {
            ["_cycle"] = viceState.Cycle,
            ["_rasterLine"] = viceState.RasterLine,
            ["_rasterCycle"] = viceState.RasterCycle,
            ["_badLine"] = viceState.BadLine != 0,
            ["_displayState"] = viceState.DisplayState,
            ["_spriteDma"] = viceState.SpriteDma
        };
    }

    private static string FormatDifferences(IReadOnlyList<StateDifference> differences)
    {
        return string.Join(Environment.NewLine, differences.Select(d => d.ToString()));
    }
}