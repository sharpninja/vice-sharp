using Xunit;
using static ViceSharp.TestHarness.Vic20.Vic20HalfCycleComparatorContractTests;

namespace ViceSharp.TestHarness.Vic20;

/// <summary>
/// The mock-proven comparator contract bound to <see cref="LockstepValidator.CompareHalfCycleStates"/> and the real
/// <see cref="HalfCycleState"/> (PLAN-NATIVERESTORE-001 D2, R13 B). FR-SNP-005 AC3, TEST-SNP-002 AC4.
/// </summary>
public sealed class Vic20HalfCycleComparatorImplementationTests
{
    private sealed class RealComparator : IHalfCycleComparator
    {
        public Mismatch? Compare(State managed, State native)
        {
            var result = LockstepValidator.CompareHalfCycleStates(ToState(managed), ToState(native));
            return result is null ? null : new Mismatch(result.HalfCycle, result.Field, result.Managed, result.Native, result.Message);
        }

        private static HalfCycleState ToState(State s) => new()
        {
            HalfCycle = s.HalfCycle, A = s.A, X = s.X, Y = s.Y, S = s.S, P = s.P, PC = s.PC, Opcode = s.Opcode, MicroOpPhase = s.MicroOpPhase,
            HasBusEvent = s.HasBusEvent, BusAddress = s.BusAddress, BusData = s.BusData, BusAccess = Enum.Parse<HalfCycleBusAccess>(s.BusAccess),
            IsDummy = s.IsDummy, IrqStage = s.IrqStage, NmiStage = s.NmiStage, BaState = s.BaState
        };
    }

    [Fact]
    public void Real_comparator_reports_the_bus_address_mismatch_at_half_cycle_37() => AssertBusAddressMismatch(new RealComparator());

    [Fact]
    public void Real_comparator_returns_null_for_equal_states() => AssertEqualStatesMatch(new RealComparator());

    [Theory]
    [InlineData("A")]
    [InlineData("PC")]
    [InlineData("Opcode")]
    [InlineData("BusAccess")]
    [InlineData("BaState")]
    public void Real_comparator_reports_the_first_differing_field_in_the_fixed_order(string field)
    {
        var managed = new HalfCycleState { HalfCycle = 5, A = 1, PC = 0x1000, Opcode = 0xEA, BusAccess = HalfCycleBusAccess.Read, BaState = 0 };
        var native = new HalfCycleState { HalfCycle = 5, A = 1, PC = 0x1000, Opcode = 0xEA, BusAccess = HalfCycleBusAccess.Read, BaState = 0 };
        switch (field)
        {
            case "A": native.A = 2; break;
            case "PC": native.PC = 0x1001; break;
            case "Opcode": native.Opcode = 0xA9; break;
            case "BusAccess": native.BusAccess = HalfCycleBusAccess.Write; break;
            case "BaState": native.BaState = 1; break;
        }
        var mismatch = LockstepValidator.CompareHalfCycleStates(managed, native);
        Assert.NotNull(mismatch);
        Assert.Equal(field, mismatch.Field);
        Assert.Equal(5L, mismatch.HalfCycle);
        Assert.Contains("half-cycle 5", mismatch.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Real_comparator_reports_the_earliest_field_when_several_differ()
    {
        var managed = new HalfCycleState { HalfCycle = 1, X = 1, BusAddress = 0x1000 };
        var native = new HalfCycleState { HalfCycle = 1, X = 2, BusAddress = 0x2000 };
        Assert.Equal("X", LockstepValidator.CompareHalfCycleStates(managed, native)!.Field);
    }
}
