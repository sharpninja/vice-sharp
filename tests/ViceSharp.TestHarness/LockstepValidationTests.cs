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

    [ViceFact]
    public void ResetStateMatches()
    {
        // Act
        var report = _validator.Run(0);

        // Assert
        report.Success.Should().BeTrue(FormatReport(report));
    }

    [ViceFact]
    public void First100CyclesMatch()
    {
        // Act
        var report = _validator.Run(100);

        // Assert
        report.Success.Should().BeTrue(FormatReport(report));
        report.TotalCyclesExecuted.Should().Be(100);
    }

    [ViceFact]
    public void First10000CyclesMatch()
    {
        // Act
        var report = _validator.Run(10000);

        // Assert
        report.Success.Should().BeTrue(FormatReport(report));
        report.TotalCyclesExecuted.Should().Be(10000);
    }

    public void Dispose()
    {
        _validator.Dispose();
    }

    private static string FormatReport(ViceSharp.Abstractions.ValidationReport report)
    {
        if (report.Success || report.Mismatch is null)
            return "No mismatch captured.";

        return $"Mismatch at cycle {report.FirstMismatchCycle}: actual PC=${report.Mismatch.Value.Actual.PC:X4} expected PC=${report.Mismatch.Value.Expected.PC:X4}, actual A=${report.Mismatch.Value.Actual.A:X2} expected A=${report.Mismatch.Value.Expected.A:X2}.";
    }
}
