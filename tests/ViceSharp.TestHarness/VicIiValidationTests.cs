using ViceSharp.Abstractions;
using ViceSharp.Chips.VicIi;
using Xunit;

namespace ViceSharp.TestHarness;

[Collection("NativeVice")]
public sealed class VicIiValidationTests : LockstepTestRunner<Mos6569>, IAsyncLifetime
{
    private readonly ViceMachineValidationFixture _fixture = new();
    private uint _nativeCycleBaseline;

    public VicIiValidationTests()
    {
        InitializeChip((_fixture.ManagedMachine.Devices.GetByRole(DeviceRole.VideoChip) as Mos6569)
            ?? throw new InvalidOperationException("Managed C64 machine did not expose a PAL VIC-II."));
    }

    public ValueTask InitializeAsync() => _fixture.InitializeAsync();
    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: After both managed and native machines reset, the managed
    /// PAL VIC-II raster engine state must match the VICE reference VIC-II
    /// state in cycle counter, raster line/cycle, bad-line flag and the
    /// full 64-byte register file.
    /// Acceptance: CompareStates returns Passed; any diverging field or
    /// register byte fails the test with the field name and offending value.
    /// </summary>
    [ViceFact]
    public void Reset_StateMatchesVICE()
    {
        _fixture.ResetBoth();
        CaptureNativeTimingBaseline();

        var result = CompareStates(GetActualState(), GetExpectedState());
        Assert.True(result.Passed, FormatDifferences(result.Differences));
    }

    /// <summary>
    /// FR: FR-VIC-001, TR: TR-CYCLE-001.
    /// Use case: Drive both managed and native VIC-II lockstep across the
    /// first two PAL scanlines (126 cycles) so badline timing, raster
    /// counters and register visibility align cycle-by-cycle.
    /// Acceptance: Every cycle compares equal between managed and native;
    /// any difference fails with the cycle number and offending state.
    /// </summary>
    [ViceFact]
    public void FirstTwoScanlines_MatchVICE()
    {
        _fixture.ResetBoth();
        CaptureNativeTimingBaseline();
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
        var relativeCycle = GetRelativeCycle(viceState);
        var registers = NormalizeRasterRegisters(viceState.Registers, viceState.RasterLine);

        return new Dictionary<string, object?>
        {
            ["Cycle"] = relativeCycle,
            ["RasterLine"] = viceState.RasterLine,
            ["RasterCycle"] = viceState.RasterCycle,
            ["BadLine"] = viceState.BadLine != 0,
            ["Registers"] = registers
        };
    }

    private void CaptureNativeTimingBaseline()
    {
        var viceState = new ViceNativeBridge.ViceVicState();
        ViceNativeBridge.GetVicState(_fixture.NativeMachine, ref viceState);
        _nativeCycleBaseline = viceState.Cycle;
    }

    private uint GetRelativeCycle(ViceNativeBridge.ViceVicState viceState)
        => viceState.Cycle >= _nativeCycleBaseline ? viceState.Cycle - _nativeCycleBaseline : 0;

    private static byte[] NormalizeRasterRegisters(byte[] registers, ushort rasterLine)
    {
        var normalized = registers.ToArray();
        normalized[0x12] = (byte)rasterLine;
        if ((rasterLine & 0x100) != 0)
        {
            normalized[0x11] |= 0x80;
        }
        else
        {
            normalized[0x11] &= 0x7F;
        }

        return normalized;
    }

    private byte[] ReadManagedRegisters()
    {
        // PLAN-VICEPARITY-001: compare the RAW register state (VICE vicii.regs,
        // which the shim's GetVicState returns), not the unused-bit-masked
        // vicii_peek. Chip.Peek now OR-s in the floating-high unused bits
        // (FR-VIC-REGISTERS AC-15), so it no longer mirrors vicii.regs; the
        // raw state accessor does.
        var registers = new byte[0x40];
        for (var i = 0; i < registers.Length; i++)
        {
            registers[i] = Chip.PeekStateRegister(i);
        }

        return registers;
    }

    private static string FormatDifferences(IReadOnlyList<StateDifference> differences)
    {
        return string.Join(Environment.NewLine, differences.Select(FormatDifference));
    }

    private static string FormatDifference(StateDifference difference)
    {
        if (difference.Actual is byte[] actual && difference.Expected is byte[] expected)
        {
            var length = Math.Min(actual.Length, expected.Length);
            for (var i = 0; i < length; i++)
            {
                if (actual[i] != expected[i])
                {
                    return $"{difference.FieldName}[0x{i:X2}]: Actual = 0x{actual[i]:X2}, Expected = 0x{expected[i]:X2}";
                }
            }

            return $"{difference.FieldName}: Actual length = {actual.Length}, Expected length = {expected.Length}";
        }

        return difference.ToString();
    }
}
