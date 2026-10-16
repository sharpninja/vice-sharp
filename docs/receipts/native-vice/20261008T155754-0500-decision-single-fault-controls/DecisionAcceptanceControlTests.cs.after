using NSubstitute;
using static ViceSharp.OracleContract.Tests.ContractAssertions;

namespace ViceSharp.OracleContract.Tests;

/// <summary>
/// FR-VICE-001 AC1/AC3; TR-VICE-ORACLE-001 AC1; TEST-VICE-001 AC1.
/// Proves the shared assertion rejects acceptance-only corruption in both directions.
/// </summary>
public sealed class DecisionAcceptanceControlTests
{
    /// <summary>
    /// The correct mock passes; an otherwise identical decision with inverted acceptance
    /// must fail the same assertion without relying on a reason or half-cycle mismatch.
    /// </summary>
    [Theory]
    [InlineData("clean", true, null)]
    [InlineData("tracked-delta", false, "DirtyBefore")]
    public void AcceptanceOnlyFaultIsRejected(string scenario, bool accepted, string? error)
    {
        var input = OracleContractFixtures.IdentityInput(scenario);
        var expected = new Decision(accepted, error);
        var probe = Substitute.For<IContractProbe<OracleState>>();
        probe.Evaluate(input).Returns(expected);

        AssertProbe(probe, input, expected);
        probe.Received(1).Evaluate(input);

        var wrong = expected with { Accepted = !expected.Accepted };
        Assert.Equal(expected.Error, wrong.Error);
        Assert.Equal(expected.HalfCycle, wrong.HalfCycle);
        probe.Evaluate(input).Returns(wrong);

        Rejects(() => AssertProbe(probe, input, expected));
    }
}
