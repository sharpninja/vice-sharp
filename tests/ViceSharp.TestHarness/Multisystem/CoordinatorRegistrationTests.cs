namespace ViceSharp.TestHarness.Multisystem;

using ViceSharp.Core;
using Xunit;

/// <summary>
/// Contract tests for SystemCoordinator registration semantics: attach /
/// detach / cart-extension preconditions.
///
/// FR/TR: ARCH-MULTISYSTEM-001 (coordinator lifecycle).
/// Use case: Plugin code or YAML loader wires up a topology; misuse must
/// surface immediately rather than silently corrupt state.
/// Acceptance: Re-attach throws; detach-unknown throws; cart-extension
/// without an attached host throws.
/// </summary>
public sealed class CoordinatorRegistrationTests
{
    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Defensive guard against duplicate-attach bugs in YAML loaders.
    /// Acceptance: Second AttachSystem with same instance throws.
    /// </summary>
    [Fact]
    public void AttachSystem_Twice_Throws()
    {
        var coord = new SystemCoordinator();
        var m = new TestMachine(1_000_000);
        coord.AttachSystem(m);

        Assert.Throws<InvalidOperationException>(() => coord.AttachSystem(m));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Defensive guard against detaching an instance that was
    /// never attached.
    /// Acceptance: DetachSystem on unattached machine throws.
    /// </summary>
    [Fact]
    public void DetachSystem_Unknown_Throws()
    {
        var coord = new SystemCoordinator();
        var m = new TestMachine(1_000_000);

        Assert.Throws<InvalidOperationException>(() => coord.DetachSystem(m));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Cart extension must reference an already-attached host.
    /// Acceptance: AttachCartExtension with unattached host throws.
    /// </summary>
    [Fact]
    public void AttachCartExtension_UnattachedHost_Throws()
    {
        var coord = new SystemCoordinator();
        var host = new TestMachine(1_000_000);
        var ext = new TestMachine(1_000_000);

        Assert.Throws<InvalidOperationException>(() => coord.AttachCartExtension(ext, host));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Attempting to attach the same extension twice must be
    /// rejected.
    /// Acceptance: AttachCartExtension twice throws on the second call.
    /// </summary>
    [Fact]
    public void AttachCartExtension_Twice_Throws()
    {
        var coord = new SystemCoordinator();
        var host = new TestMachine(1_000_000);
        var ext = new TestMachine(1_000_000);
        coord.AttachSystem(host);
        coord.AttachCartExtension(ext, host);

        Assert.Throws<InvalidOperationException>(() => coord.AttachCartExtension(ext, host));
    }

    /// <summary>
    /// FR/TR: ARCH-MULTISYSTEM-001
    /// Use case: Attaching the same bus instance twice would corrupt the
    /// coordinator's bus list.
    /// Acceptance: AttachBus twice on same instance throws.
    /// </summary>
    [Fact]
    public void AttachBus_Twice_Throws()
    {
        var coord = new SystemCoordinator();
        var bus = new InterSystemBus("IEC", new[] { "ATN", "CLK", "DATA" });
        coord.AttachBus(bus);

        Assert.Throws<InvalidOperationException>(() => coord.AttachBus(bus));
    }
}
