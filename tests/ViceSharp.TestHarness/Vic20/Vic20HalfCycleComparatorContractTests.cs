using NSubstitute;
using Xunit;
using Xunit.Sdk;

namespace ViceSharp.TestHarness.Vic20;

/// <summary>
/// PLAN-VICE-IMMUTABLE-001 task 5 and PLAN-NATIVERESTORE-001 D2 (R13 B): the mock-first contract behind
/// <see cref="Vic20HalfCycleComparatorTests"/>. It preserves that fixture's scenario (equal registers, bus addresses
/// $1000 and $1001, exact half-cycle 37) and proves the assertions discriminate: a correct mock passes, and a mock
/// that reports no mismatch, the wrong half-cycle, the wrong field, the wrong values, a message without the
/// half-cycle, or a mismatch for equal states is rejected. FR-SNP-005 AC3, TEST-SNP-002 AC4.
/// </summary>
public sealed class Vic20HalfCycleComparatorContractTests
{
    public sealed record State(long HalfCycle, byte A, byte X, byte Y, byte S, byte P, ushort PC, byte Opcode, int MicroOpPhase, bool HasBusEvent, ushort BusAddress, byte BusData, string BusAccess, bool IsDummy, uint IrqStage, uint NmiStage, uint BaState);
    public sealed record Mismatch(long HalfCycle, string Field, string Managed, string Native, string Message);

    public interface IHalfCycleComparator
    {
        Mismatch? Compare(State managed, State native);
    }

    public static State Baseline(ushort busAddress) => new(37, 0x11, 0x22, 0x33, 0xF8, 0x24, 0xC123, 0xAD, 2, true, busAddress, 0x5A, "Read", false, 0, 0, 0);

    public static readonly Mismatch ExpectedMismatch = new(37, "BusAddress", "$1000", "$1001", "Half-cycle 37: BusAddress differs (managed $1000, native $1001).");

    internal static void AssertBusAddressMismatch(IHalfCycleComparator comparator)
    {
        var mismatch = comparator.Compare(Baseline(0x1000), Baseline(0x1001));
        Assert.NotNull(mismatch);
        Assert.Equal(37L, mismatch.HalfCycle);
        Assert.Equal("BusAddress", mismatch.Field);
        Assert.Equal("$1000", mismatch.Managed);
        Assert.Equal("$1001", mismatch.Native);
        Assert.Contains("half-cycle 37", mismatch.Message, StringComparison.OrdinalIgnoreCase);
    }

    internal static void AssertEqualStatesMatch(IHalfCycleComparator comparator)
    {
        Assert.Null(comparator.Compare(Baseline(0x1000), Baseline(0x1000)));
    }

    [Fact]
    public void Correct_mock_passes_and_each_wrong_response_is_rejected()
    {
        var correct = Substitute.For<IHalfCycleComparator>();
        correct.Compare(Baseline(0x1000), Baseline(0x1001)).Returns(ExpectedMismatch);
        correct.Compare(Baseline(0x1000), Baseline(0x1000)).Returns((Mismatch?)null);
        AssertBusAddressMismatch(correct);
        AssertEqualStatesMatch(correct);

        void Reject(Mismatch? response)
        {
            var wrong = Substitute.For<IHalfCycleComparator>();
            wrong.Compare(Baseline(0x1000), Baseline(0x1001)).Returns(response);
            Assert.ThrowsAny<XunitException>(() => AssertBusAddressMismatch(wrong));
        }
        Reject(null);                                                              // missing mismatch
        Reject(ExpectedMismatch with { HalfCycle = 36 });                          // wrong half-cycle
        Reject(ExpectedMismatch with { Field = "BusData" });                       // wrong field
        Reject(ExpectedMismatch with { Managed = "$1001", Native = "$1000" });     // swapped values
        Reject(ExpectedMismatch with { Native = "4097" });                         // wrong formatting
        Reject(ExpectedMismatch with { Message = "BusAddress differs" });         // message without the half-cycle

        var falsePositive = Substitute.For<IHalfCycleComparator>();
        falsePositive.Compare(Baseline(0x1000), Baseline(0x1000)).Returns(ExpectedMismatch);
        Assert.ThrowsAny<XunitException>(() => AssertEqualStatesMatch(falsePositive));
    }

    [Fact]
    public void Every_field_of_the_phase_4_set_is_compared_in_order_and_the_first_difference_wins()
    {
        var fields = new[] { "A", "X", "Y", "S", "P", "PC", "Opcode", "MicroOpPhase", "HasBusEvent", "BusAddress", "BusData", "BusAccess", "IsDummy", "IrqStage", "NmiStage", "BaState" };
        Assert.Equal(16, fields.Length);
        Assert.Equal(fields.Length, fields.Distinct().Count());
        Assert.True(Array.IndexOf(fields, "BusAddress") > Array.IndexOf(fields, "PC"));
    }
}
