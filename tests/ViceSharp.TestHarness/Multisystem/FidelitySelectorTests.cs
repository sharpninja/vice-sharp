namespace ViceSharp.TestHarness.Multisystem;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Multisystem;
using Xunit;

/// <summary>
/// FR/TR: ARCH-FIDELITY-001 (Phase E1).
/// Use case: A multi-system YAML topology declares each peripheral's
/// fidelity (Buffered cheap path vs TrueDevice substrate). The loader
/// parses the value, validates it, surfaces it on the blueprint, and
/// defaults to Buffered for backward compatibility.
/// </summary>
public sealed class FidelitySelectorTests
{
    private const string PeerInline = """
        schemaVersion: 1
        machine:
          name: "Peer"
          videoStandard: Ntsc
          masterClockHz: 1000000
        memory:
          regions:
            - id: ram-main
              kind: Ram
              start: 0x0000
              end:   0xFFFF
        chips:
          - id: cpu
            type: Mos6502
            role: Cpu
        """;

    private static string MakeTopology(string peripheralFidelityLine, string extFidelityLine = "")
        => $$"""
        schemaVersion: 1
        coordinator:
          host:
            id: c64-host
            yamlInline: |
        {{Indent(PeerInline, 6)}}
          peripherals:
            - id: drive-8
              role: Independent
              {{peripheralFidelityLine}}
              yamlInline: |
        {{Indent(PeerInline, 8)}}
          cartExtensions:
            - id: ext-1
              {{extFidelityLine}}
              yamlInline: |
        {{Indent(PeerInline, 8)}}
          buses: []
        """;

    /// <summary>
    /// FR/TR: ARCH-FIDELITY-001
    /// Use case: When a peripheral declares fidelity: TrueDevice the
    /// blueprint surfaces it; default fields stay Buffered.
    /// Acceptance: GetFidelity("drive-8") = TrueDevice; GetFidelity("ext-1")
    /// = Buffered.
    /// </summary>
    [Fact]
    public void Loader_RoundTrips_PeripheralFidelity_TrueDevice()
    {
        var yaml = MakeTopology("fidelity: TrueDevice");
        var loader = new MultiSystemYamlLoader();

        var bp = loader.LoadFromString(yaml);

        bp.GetFidelity("drive-8").Should().Be(Fidelity.TrueDevice);
        bp.GetFidelity("ext-1").Should().Be(Fidelity.Buffered);
    }

    /// <summary>
    /// FR/TR: ARCH-FIDELITY-001
    /// Use case: Default fidelity is Buffered when the field is absent.
    /// Acceptance: GetFidelity on a peripheral without explicit fidelity
    /// returns Buffered.
    /// </summary>
    [Fact]
    public void Loader_Default_IsBuffered_WhenFidelityFieldOmitted()
    {
        var yaml = MakeTopology(string.Empty);
        var loader = new MultiSystemYamlLoader();

        var bp = loader.LoadFromString(yaml);

        bp.GetFidelity("drive-8").Should().Be(Fidelity.Buffered);
        bp.GetFidelity("ext-1").Should().Be(Fidelity.Buffered);
    }

    /// <summary>
    /// FR/TR: ARCH-FIDELITY-001
    /// Use case: Cart extension fidelity also round-trips.
    /// Acceptance: ext-1 declared as TrueDevice surfaces accordingly.
    /// </summary>
    [Fact]
    public void Loader_RoundTrips_CartExtensionFidelity()
    {
        var yaml = MakeTopology("fidelity: Buffered", "fidelity: TrueDevice");
        var loader = new MultiSystemYamlLoader();

        var bp = loader.LoadFromString(yaml);

        bp.GetFidelity("drive-8").Should().Be(Fidelity.Buffered);
        bp.GetFidelity("ext-1").Should().Be(Fidelity.TrueDevice);
    }

    /// <summary>
    /// FR/TR: ARCH-FIDELITY-001
    /// Use case: Fidelity parsing is case-insensitive (matches the rest of
    /// the YAML loader's tolerance).
    /// Acceptance: 'truedevice' parses as TrueDevice.
    /// </summary>
    [Fact]
    public void Loader_FidelityParsing_IsCaseInsensitive()
    {
        var yaml = MakeTopology("fidelity: truedevice");
        var loader = new MultiSystemYamlLoader();

        var bp = loader.LoadFromString(yaml);

        bp.GetFidelity("drive-8").Should().Be(Fidelity.TrueDevice);
    }

    /// <summary>
    /// FR/TR: ARCH-FIDELITY-001
    /// Use case: An unknown fidelity value is rejected with a clear error.
    /// Acceptance: 'fidelity: Magic' throws MultiSystemValidationException
    /// mentioning the value.
    /// </summary>
    [Fact]
    public void Loader_UnknownFidelityValue_Throws()
    {
        var yaml = MakeTopology("fidelity: Magic");
        var loader = new MultiSystemYamlLoader();

        var ex = Assert.Throws<MultiSystemValidationException>(() => loader.LoadFromString(yaml));
        ex.Message.Should().Contain("Magic");
    }

    /// <summary>
    /// FR/TR: ARCH-FIDELITY-001
    /// Use case: Host systems always run as TrueDevice (they're the running
    /// machine; the selector applies to peripherals only).
    /// Acceptance: GetFidelity(hostId) = TrueDevice.
    /// </summary>
    [Fact]
    public void Loader_HostFidelity_IsAlways_TrueDevice()
    {
        var yaml = MakeTopology("fidelity: Buffered");
        var loader = new MultiSystemYamlLoader();

        var bp = loader.LoadFromString(yaml);

        bp.GetFidelity("c64-host").Should().Be(Fidelity.TrueDevice);
    }

    /// <summary>
    /// FR/TR: ARCH-FIDELITY-001
    /// Use case: GetFidelity for an unknown id throws clear error.
    /// Acceptance: KeyNotFoundException.
    /// </summary>
    [Fact]
    public void GetFidelity_UnknownSystemId_Throws()
    {
        var yaml = MakeTopology("fidelity: TrueDevice");
        var loader = new MultiSystemYamlLoader();
        var bp = loader.LoadFromString(yaml);

        Assert.Throws<KeyNotFoundException>(() => bp.GetFidelity("nonexistent"));
    }

    private static string Indent(string text, int spaces)
    {
        var pad = new string(' ', spaces);
        return string.Join('\n', text.Split('\n').Select(l => pad + l));
    }
}
