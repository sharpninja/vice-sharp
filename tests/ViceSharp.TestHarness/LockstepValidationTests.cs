using Xunit;
using FluentAssertions;

namespace ViceSharp.TestHarness;

public sealed class LockstepValidationTests : IDisposable
{
    private readonly LockstepValidator _validator;

    public LockstepValidationTests()
    {
        _validator = new LockstepValidator();
    }

    [Fact(Skip = "Requires native VICE DLL")]
    public void ResetStateMatches()
    {
        // Act
        _validator.Run(1);

        // Assert
        // Reset state should be identical between implementations
    }

    [Fact(Skip = "Requires native VICE DLL")]
    public void First100CyclesMatch()
    {
        // Act
        var report = _validator.Run(100);

        // Assert
        report.Success.Should().BeTrue();
        report.TotalCyclesExecuted.Should().Be(100);
    }

    [Fact(Skip = "Requires native VICE DLL")]
    public void First10000CyclesMatch()
    {
        // Act
        var report = _validator.Run(10000);

        // Assert
        report.Success.Should().BeTrue();
        report.TotalCyclesExecuted.Should().Be(10000);
    }

    public void Dispose()
    {
        _validator.Dispose();
    }
}