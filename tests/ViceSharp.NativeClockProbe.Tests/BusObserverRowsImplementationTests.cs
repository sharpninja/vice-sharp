namespace ViceSharp.NativeClockProbe.Tests;

/// <summary>
/// The identical mock-proven row-layout assertions execute the external C bus observer against the
/// VICE-shaped double compiled in its 32-row (x64sc) shape: static mem_read_tab[32][0x101], a global
/// mem_get_current_bank_config, global setters, and an intra-object banking change that re-points all
/// four pointers. The double is not VICE. No VICE sources, objects, build targets or emulator binaries are used here.
/// </summary>
public sealed class BusObserverRowsImplementationTests
{
    private static readonly CompiledCFixture Fixture = CompiledBusProbe.Build("bus-probe-rows", "-DVICE_SHARP_ROWS=32");

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
    [InlineData("watch-flag1-after-rebuild")]
    [InlineData("foreign-row")]
    [InlineData("translate-keep")]
    [InlineData("bank-by-store")]
    [InlineData("bank-switch")]
    [InlineData("rewrite-noncurrent-row")]
    [InlineData("bank-then-rebuild")]
    public void Compiled_observer_satisfies_identical_mock_proven_row_contract(string name) =>
        BusObserverRowsContractTests.AssertProbe(new CompiledBusProbe(Fixture), name);
}
