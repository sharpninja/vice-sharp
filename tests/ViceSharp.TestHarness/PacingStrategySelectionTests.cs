namespace ViceSharp.TestHarness;

using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using Xunit;

/// <summary>
/// FR-PACESEL-001 / TR-PACESEL-001 / TEST-PACESEL-001.
/// The emulation pacing strategy ("Semaphore" vs "VICE") is selectable: a canonical id
/// maps to the gate, and the running pump can switch strategy live by swapping its gate.
/// </summary>
public sealed class PacingStrategySelectionTests
{
    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-001.
    /// Use case: a stored "vice" id selects the VICE pacing gate.
    /// Acceptance: CreateGate("vice").Name == "VICE".
    /// </summary>
    [Fact]
    public void CreateGate_Vice_BuildsViceGate()
        => Assert.Equal("VICE", EmulationGateStrategies.CreateGate("vice").Name);

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-001.
    /// Use case: a stored "semaphore" id selects the Semaphore pacing gate.
    /// Acceptance: CreateGate("semaphore").Name == "Semaphore".
    /// </summary>
    [Fact]
    public void CreateGate_Semaphore_BuildsSemaphoreGate()
        => Assert.Equal("Semaphore", EmulationGateStrategies.CreateGate("semaphore").Name);

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-001.
    /// Use case: an unrecognized id must not crash; it falls back to the default gate.
    /// Acceptance: CreateGate("bogus").Name == "Semaphore".
    /// </summary>
    [Fact]
    public void CreateGate_Unknown_DefaultsToSemaphore()
        => Assert.Equal("Semaphore", EmulationGateStrategies.CreateGate("bogus").Name);

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-001.
    /// Use case: a null id (no stored setting) falls back to the default gate.
    /// Acceptance: CreateGate(null).Name == "Semaphore".
    /// </summary>
    [Fact]
    public void CreateGate_Null_DefaultsToSemaphore()
        => Assert.Equal("Semaphore", EmulationGateStrategies.CreateGate(null).Name);

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-001.
    /// Use case: display names and ids from any source must canonicalize to a stored id,
    ///   with unknown/null defaulting to "semaphore".
    /// Acceptance: Normalize maps each input to the expected stored id.
    /// </summary>
    [Theory]
    [InlineData("VICE", "vice")]
    [InlineData("vice", "vice")]
    [InlineData("Semaphore", "semaphore")]
    [InlineData("semaphore", "semaphore")]
    [InlineData("bogus", "semaphore")]
    [InlineData(null, "semaphore")]
    public void Normalize_CanonicalizesToStoredId(string? input, string expected)
        => Assert.Equal(expected, EmulationGateStrategies.Normalize(input));

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-001.
    /// Use case: a stored id is rendered as the gate's display name for the UI/status.
    /// Acceptance: DisplayName maps "vice" to "VICE" and everything else to "Semaphore".
    /// </summary>
    [Theory]
    [InlineData("vice", "VICE")]
    [InlineData("semaphore", "Semaphore")]
    [InlineData("bogus", "Semaphore")]
    public void DisplayName_MapsIdToGateName(string id, string expected)
        => Assert.Equal(expected, EmulationGateStrategies.DisplayName(id));

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-001.
    /// Use case: switching strategy on a not-yet-started pump takes effect immediately
    ///   (StartAsync later starts the new gate).
    /// Acceptance: after SetStrategy("vice") GateName == "VICE".
    /// </summary>
    [Fact]
    public void SetStrategy_WhenNotStarted_SwitchesGateImmediately()
    {
        var registry = new EmulatorRuntimeRegistry();
        using var pump = new EmulationPumpService(registry, EmulationGateStrategies.CreateGate("semaphore"));

        pump.SetStrategy("vice");

        Assert.Equal("VICE", pump.GateName);
    }

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-001.
    /// Use case: switching back to the Semaphore strategy works symmetrically.
    /// Acceptance: after SetStrategy("semaphore") GateName == "Semaphore".
    /// </summary>
    [Fact]
    public void SetStrategy_BackToSemaphore_Switches()
    {
        var registry = new EmulatorRuntimeRegistry();
        using var pump = new EmulationPumpService(registry, EmulationGateStrategies.CreateGate("vice"));

        pump.SetStrategy("semaphore");

        Assert.Equal("Semaphore", pump.GateName);
    }

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-001.
    /// Use case: re-selecting the active strategy must be a harmless no-op (idempotent).
    /// Acceptance: SetStrategy("VICE") on an already-VICE pump leaves GateName == "VICE".
    /// </summary>
    [Fact]
    public void SetStrategy_SameStrategy_IsNoOp()
    {
        var registry = new EmulatorRuntimeRegistry();
        using var pump = new EmulationPumpService(registry, EmulationGateStrategies.CreateGate("vice"));

        pump.SetStrategy("VICE");

        Assert.Equal("VICE", pump.GateName);
    }

    /// <summary>
    /// FR-PACESEL-001 / TR-PACESEL-001.
    /// Use case: an unknown strategy name must not crash the pump; it resolves to default.
    /// Acceptance: SetStrategy("bogus") leaves GateName == "Semaphore".
    /// </summary>
    [Fact]
    public void SetStrategy_UnknownName_DefaultsToSemaphore()
    {
        var registry = new EmulatorRuntimeRegistry();
        using var pump = new EmulationPumpService(registry, EmulationGateStrategies.CreateGate("vice"));

        pump.SetStrategy("bogus");

        Assert.Equal("Semaphore", pump.GateName);
    }
}
