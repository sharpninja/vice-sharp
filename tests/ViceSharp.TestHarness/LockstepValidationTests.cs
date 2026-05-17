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

    /// <summary>
    /// FR: FR-Validation-Lockstep, TR: TR-LOCKSTEP-RESET.
    /// Use case: After power-on reset, ViceSharp and the upstream VICE
    /// native build must agree on every observable CPU register before
    /// either side executes a single instruction.
    /// Acceptance: <see cref="LockstepValidator"/> reports zero cycles and
    /// no register mismatch (PC, A, X, Y, S, P) against native VICE.
    /// </summary>
    [ViceFact]
    public void ResetStateMatches()
    {
        // Act
        var report = _validator.Run(0);

        // Assert
        report.Success.Should().BeTrue(FormatReport(report));
    }

    /// <summary>
    /// FR: FR-Validation-Lockstep, TR: TR-LOCKSTEP-100.
    /// Use case: Smallest cycle window that proves the very first opcode
    /// fetch and dispatch path matches the native VICE reference; cheap
    /// enough to run on every CI build and the first to fail when the CPU
    /// front-end regresses.
    /// Acceptance: 100 cycles execute with no mismatch and the validator
    /// reports exactly 100 cycles executed.
    /// </summary>
    [ViceFact]
    public void First100CyclesMatch()
    {
        // Act
        var report = _validator.Run(100);

        // Assert
        report.Success.Should().BeTrue(FormatReport(report));
        report.TotalCyclesExecuted.Should().Be(100);
    }

    /// <summary>
    /// FR: FR-Validation-Lockstep, TR: TR-LOCKSTEP-10K.
    /// Use case: Medium-window lockstep gate that exercises the KERNAL
    /// reset routine end-to-end against native VICE, catching divergences
    /// in flag math, addressing modes, and CIA/VIC-II side effects that
    /// only surface after thousands of cycles.
    /// Acceptance: 10,000 cycles execute with no register mismatch and
    /// the validator reports exactly 10,000 cycles executed.
    /// </summary>
    [ViceFact]
    public void First10000CyclesMatch()
    {
        // Act
        var report = _validator.Run(10000);

        // Assert
        report.Success.Should().BeTrue(FormatReport(report));
        report.TotalCyclesExecuted.Should().Be(10000);
    }

    /// <summary>
    /// FR: FR-Validation-Lockstep, TR: TR-LOCKSTEP-100K.
    /// Use case: Long-window lockstep regression gate that runs the full
    /// BASIC reset+IDLE loop against native VICE; the deepest CI parity
    /// signal currently shipped, covering interrupt timing and CIA TOD.
    /// Acceptance: 100,000 cycles execute with zero register mismatch and
    /// the validator reports exactly 100,000 cycles executed.
    /// </summary>
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
