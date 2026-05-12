using Xunit;
using FluentAssertions;

namespace ViceSharp.TestHarness;

[Collection("NativeVice")]
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

    [ViceFact]
    public void First100000CyclesMatch()
    {
        // Act
        var report = _validator.Run(100000);

        // Assert
        report.Success.Should().BeTrue(FormatReport(report));
        report.TotalCyclesExecuted.Should().Be(100000);
    }

    public void Dispose()
    {
        _validator.Dispose();
    }

    private static string FormatReport(ViceSharp.Abstractions.ValidationReport report)
    {
        if (report.Success || report.Mismatch is null)
            return "No mismatch captured.";

        return
            $"Mismatch at cycle {report.FirstMismatchCycle}: " +
            $"actual [A=${report.Mismatch.Value.Actual.A:X2}, X=${report.Mismatch.Value.Actual.X:X2}, Y=${report.Mismatch.Value.Actual.Y:X2}, S=${report.Mismatch.Value.Actual.S:X2}, P=${report.Mismatch.Value.Actual.P:X2}, PC=${report.Mismatch.Value.Actual.PC:X4}] " +
            $"expected [A=${report.Mismatch.Value.Expected.A:X2}, X=${report.Mismatch.Value.Expected.X:X2}, Y=${report.Mismatch.Value.Expected.Y:X2}, S=${report.Mismatch.Value.Expected.S:X2}, P=${report.Mismatch.Value.Expected.P:X2}, PC=${report.Mismatch.Value.Expected.PC:X4}].";
    }
}
