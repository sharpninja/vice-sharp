namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TRUEDRIVE-1541-002.
/// Use case: The native VICE shim exposes drive-CPU registers per unit
/// (8..11) so ViceSharp can lockstep its 1541 implementation against
/// upstream VICE cycle-by-cycle. This test set establishes the wiring
/// gate - the P/Invoke surface is reachable, returns sensible defaults
/// when no drive context is active, and accepts the 1541 device-number
/// convention.
///
/// Full cycle-accurate lockstep against a running VICE drive (with true-
/// drive emulation enabled in VICE) is the next slice: it requires a
/// shim-level toggle for VICE's DriveTrueEmulation resource, which is
/// separate from the read accessors landed here.
/// </summary>
[Collection("NativeVice")]
public sealed class LockstepDriveValidationTests : IAsyncLifetime
{
    private ViceMachineValidationFixture? _fixture;

    public async ValueTask InitializeAsync()
    {
        _fixture = new ViceMachineValidationFixture("c64");
        await _fixture.InitializeAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _fixture?.DisposeAsync() ?? ValueTask.CompletedTask;
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-002
    /// Use case: vice_drivecpu_get_pc accessor is callable and returns
    /// zero when no drive context is wired for the requested unit. Proves
    /// the shim P/Invoke layer is correctly built + bound.
    /// Acceptance: GetDrivePC(native, 8) returns 0 on a fresh machine.
    /// </summary>
    [Fact]
    public void GetDrivePc_IsCallable_WithoutCrashing()
    {
        // Singleton native VICE state may persist across tests; we only
        // verify the P/Invoke wiring + return-type marshalling here.
        var act = () => ViceNative.GetDrivePC(_fixture!.NativeMachine, 8);
        act.Should().NotThrow();
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-002
    /// Use case: All drive-CPU register accessors return zero when the
    /// unit number is out of range (below 8 or above 11).
    /// Acceptance: GetDrive* with unit 7, 12, or 255 all return zero.
    /// </summary>
    [Theory]
    [InlineData(7u)]
    [InlineData(12u)]
    [InlineData(255u)]
    public void DriveAccessors_OutOfRangeUnit_ReturnZero(uint unit)
    {
        var native = _fixture!.NativeMachine;
        ViceNative.GetDriveA(native, unit).Should().Be(0);
        ViceNative.GetDriveX(native, unit).Should().Be(0);
        ViceNative.GetDriveY(native, unit).Should().Be(0);
        ViceNative.GetDriveP(native, unit).Should().Be(0);
        ViceNative.GetDriveS(native, unit).Should().Be(0);
        ViceNative.GetDrivePC(native, unit).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-002
    /// Use case: Valid drive units (8..11) call without throwing, even
    /// when no drive context is wired up - we expect 0 but the contract
    /// is "non-crashing".
    /// Acceptance: GetDriveA / GetDrivePC for units 8..11 do not throw.
    /// </summary>
    [Theory]
    [InlineData(8u)]
    [InlineData(9u)]
    [InlineData(10u)]
    [InlineData(11u)]
    public void DriveAccessors_ValidUnit_DoNotThrow(uint unit)
    {
        var native = _fixture!.NativeMachine;
        var act = () =>
        {
            _ = ViceNative.GetDriveA(native, unit);
            _ = ViceNative.GetDrivePC(native, unit);
        };
        act.Should().NotThrow();
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-002
    /// Use case: vice_drive_set_true_emulation toggles VICE's
    /// DriveNTrueEmulation resource. With TDE enabled the drive CPU
    /// starts executing ROM; after N machine steps its PC is non-zero.
    /// Acceptance: SetDriveTrueEmulation(unit=8, enabled=1) returns 0,
    /// GetDriveTrueEmulation reads back 1, and after 50k steps drive PC
    /// is greater than 0.
    /// </summary>
    [Fact]
    public void TrueDriveEmulation_EnabledOnUnit8_DriveCpuAdvances()
    {
        var native = _fixture!.NativeMachine;
        var setResult = ViceNative.SetDriveTrueEmulation(native, 8, 1);
        setResult.Should().Be(0);
        ViceNative.GetDriveTrueEmulation(native, 8).Should().Be(1);

        for (int i = 0; i < 50_000; i++)
            ViceNativeBridge.StepCycle(native);

        var pc = ViceNative.GetDrivePC(native, 8);
        pc.Should().BeGreaterThan(0,
            "drive CPU should be running ROM code after 50k host cycles with TDE on");
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-002
    /// Use case: TDE toggles cleanly off + on for the same unit.
    /// Acceptance: Set 1 -> get returns 1; set 0 -> get returns 0.
    /// </summary>
    [Fact]
    public void TrueDriveEmulation_RoundTrips_Per_Unit()
    {
        var native = _fixture!.NativeMachine;
        ViceNative.SetDriveTrueEmulation(native, 8, 1).Should().Be(0);
        ViceNative.GetDriveTrueEmulation(native, 8).Should().Be(1);

        ViceNative.SetDriveTrueEmulation(native, 8, 0).Should().Be(0);
        ViceNative.GetDriveTrueEmulation(native, 8).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-002
    /// Use case: Out-of-range unit numbers are rejected by the set/get.
    /// Acceptance: Set on unit 12 returns non-zero; Get on unit 7 returns 0.
    /// </summary>
    [Fact]
    public void TrueDriveEmulation_RejectsOutOfRangeUnits()
    {
        var native = _fixture!.NativeMachine;
        ViceNative.SetDriveTrueEmulation(native, 12, 1).Should().NotBe(0);
        ViceNative.GetDriveTrueEmulation(native, 7).Should().Be(0);
    }
}
