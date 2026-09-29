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
    /// FR: FR-CIA-TIMER, TR: TR-LOCKSTEP-10K.
    /// Use case: Wolf64 2060433. Managed CIA1 Timer A was 3 counts ahead of
    ///   x64sc ($4007 vs $400A), so Timer A underflow IRQ asserted on
    ///   managed while native irqflags were still 0.
    /// Acceptance: After 10,000 lockstep cycles, CIA1 Timer A live counters
    ///   match native GetCiaState.
    /// </summary>
    [ViceFact]
    public void First10000_Cia1TimerA_MatchesNative()
    {
        var report = _validator.Run(10000);
        report.Success.Should().BeTrue(FormatReport(report));

        var mTa = (ushort)(_validator.HostMachine.Bus.Peek(0xDC04)
            | (_validator.HostMachine.Bus.Peek(0xDC05) << 8));
        var nCia = _validator.NativeMachine.GetCiaState(0);
        mTa.Should().Be(nCia.TimerA,
            $"CIA1 TA managed=${mTa:X4} native=${nCia.TimerA:X4} cra m=${_validator.HostMachine.Bus.Peek(0xDC0E):X2} n=${nCia.Cra:X2}");
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

    /// <summary>
    /// FR: FR-CIA-TIMER, TR: TR-LOCKSTEP-100K.
    /// Use case: Same Timer A phase check as the 10k gate, after the BASIC
    ///   idle loop and CIA Timer A IRQ have been running.
    /// Acceptance: After 100,000 lockstep cycles, CIA1 Timer A matches native.
    /// </summary>
    [ViceFact]
    public void First100000_Cia1TimerA_MatchesNative()
    {
        var report = _validator.Run(100000);
        report.Success.Should().BeTrue(FormatReport(report));

        var mTa = (ushort)(_validator.HostMachine.Bus.Peek(0xDC04)
            | (_validator.HostMachine.Bus.Peek(0xDC05) << 8));
        var nCia = _validator.NativeMachine.GetCiaState(0);
        mTa.Should().Be(nCia.TimerA,
            $"CIA1 TA managed=${mTa:X4} native=${nCia.TimerA:X4} cra m=${_validator.HostMachine.Bus.Peek(0xDC0E):X2} n=${nCia.Cra:X2} nicr=${nCia.InterruptFlags:X2}");
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
