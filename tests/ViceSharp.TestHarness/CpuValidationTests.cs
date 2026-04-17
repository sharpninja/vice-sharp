namespace ViceSharp.TestHarness;

using Xunit;
using ViceSharp.Chips.Cpu;
using ViceSharp.Abstractions;

public sealed class CpuValidationTests : LockstepTestRunner<Mos6502>, IAsyncLifetime
{
    private IntPtr _viceMachine;

    public CpuValidationTests()
        : base(new Mos6502(null!))
    {
    }

    public async ValueTask InitializeAsync()
    {
        // Initialize VICE reference instance
        _viceMachine = ViceNativeBridge.CreateMachine();
        ViceNativeBridge.ResetMachine(_viceMachine);
    }

    public ValueTask DisposeAsync()
    {
        // Cleanup native resources
        return ValueTask.CompletedTask;
    }

    [Fact]
    public void Reset_StateMatchesVICE()
    {
        Chip.Reset();

        var actualState = CaptureState(Chip);
        var expectedState = GetCpuStateFromVICE();

        var result = CompareStates(actualState, expectedState);

        Assert.True(result.Passed, FormatDifferences(result.Differences));
    }

    [Fact]
    public void SingleCycle_ExecuteMatchesVICE()
    {
        // Write NOP opcode
        Chip.Write(0xFFFC, 0xEA);
        Chip.Write(0xFFFD, 0xFF);

        for (int cycle = 0; cycle < 2; cycle++)
        {
            ViceNativeBridge.StepCycle(_viceMachine);
            var result = StepAndCompare();

            Assert.True(result.Passed, $"Cycle {cycle}: {FormatDifferences(result.Differences)}");
        }
    }

    [Fact]
    public void FullInstructionSet_AllOpcodesMatchVICE()
    {
        // Test all 256 opcodes
        for (byte opcode = 0; opcode < 0xFF; opcode++)
        {
            Chip.Write(0xFFFC, opcode);
            Chip.Write(0xFFFD, 0xFF);

            for (int cycle = 0; cycle < 7; cycle++)
            {
                ViceNativeBridge.StepCycle(_viceMachine);
                var result = StepAndCompare();

                Assert.True(result.Passed, $"Opcode 0x{opcode:X2} Cycle {cycle}: {FormatDifferences(result.Differences)}");
            }
        }
    }

    protected override Dictionary<string, object> GetExpectedState()
    {
        return new Dictionary<string, object>
        {
            ["_a"] = ViceNativeBridge.GetCpuRegister(_viceMachine, 0),
            ["_x"] = ViceNativeBridge.GetCpuRegister(_viceMachine, 1),
            ["_y"] = ViceNativeBridge.GetCpuRegister(_viceMachine, 2),
            ["_p"] = ViceNativeBridge.GetCpuRegister(_viceMachine, 3),
            ["_sp"] = ViceNativeBridge.GetCpuRegister(_viceMachine, 4),
            ["_pc"] = (ushort)(ViceNativeBridge.GetCpuRegister(_viceMachine, 5) | (ViceNativeBridge.GetCpuRegister(_viceMachine, 6) << 8))
        };
    }

    private Dictionary<string, object> GetCpuStateFromVICE() => GetExpectedState();

    private static string FormatDifferences(IReadOnlyList<StateDifference> differences)
    {
        return string.Join(Environment.NewLine, differences.Select(d => d.ToString()));
    }
}