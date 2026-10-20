using static ViceSharp.NativeClockProbe.Tests.BusObserverContractTests;

namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// The identical mock-proven assertions execute the external C bus observer through the real
/// linker's --wrap of vic_cycle, mem_initialize_memory and mem_mmu_translate against a VICE-shaped
/// double in its single-row (xvic) shape: global table pointers, static rows, intra-object toggle.
/// The double is not VICE. No VICE sources, objects, build targets or emulator binaries are used here.
/// </summary>
public sealed class BusObserverImplementationTests
{
    private static readonly CompiledCFixture Fixture = CompiledBusProbe.Build("bus-probe");

    [Theory]
    [InlineData("empty")]
    [InlineData("init-only")]
    [InlineData("read")]
    [InlineData("fetch")]
    [InlineData("write-and-dummy")]
    [InlineData("translate")]
    [InlineData("rebuild-at-runtime")]
    [InlineData("rewrite-outside-init")]
    [InlineData("dummy-alias")]
    [InlineData("watch-toggle")]
    public void Compiled_observer_satisfies_identical_mock_proven_contract(string name) =>
        AssertProbe(new CompiledBusProbe(Fixture), name);
}
