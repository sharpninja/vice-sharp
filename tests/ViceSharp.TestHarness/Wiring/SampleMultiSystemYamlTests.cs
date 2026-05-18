namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.TestHarness.AdhocMachine;
using Xunit;

/// <summary>
/// FR/TR: ARCH-WIRING-005 (Phase F5).
/// Use case: The canonical sample at docs/samples/c64-plus-1541.multisystem.yaml
/// loads through the console-host code path (BuildCoordinatorAuto) and
/// produces a fully-wired C64 + 1541 substrate with no inline YAML
/// fragments or factory delegates. This file is what a user actually
/// points the launcher at.
/// </summary>
public sealed class SampleMultiSystemYamlTests
{
    private static string SamplePath =>
        Path.Combine(SolutionRoot.Find(), "docs", "samples", "c64-plus-1541.multisystem.yaml");

    /// <summary>
    /// FR/TR: ARCH-WIRING-005
    /// Use case: Sample YAML loads + builds without errors and exposes the
    /// expected systems by id.
    /// Acceptance: systemIds contains c64-host + drive-8.
    /// </summary>
    [Fact]
    public void SampleYaml_LoadsAndBuilds_CleanTopology()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var bp = new MultiSystemYamlLoader().LoadFromFile(SamplePath);

        var build = bp.BuildCoordinatorAuto(new ArchitectureBuilder(provider));

        build.SystemsById.Keys.Should().Contain(new[] { "c64-host", "drive-8" });
        build.BusesById.Should().ContainKey("IEC");
        build.Endpoints.Should().ContainKeys(
            ("c64-host", "IEC"),
            ("drive-8", "IEC"));
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-005
    /// Use case: After the sample loads, host CIA2 + drive VIA1 are auto-
    /// bound. A host CIA2 write asserting ATN reaches the drive's VIA1 PB7.
    /// Acceptance: cia2.Write($DD00, $08) -> drive PB7 = 1.
    /// </summary>
    [Fact]
    public void SampleYaml_HostAssertsAtn_DriveSeesIt()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var bp = new MultiSystemYamlLoader().LoadFromFile(SamplePath);
        var build = bp.BuildCoordinatorAuto(new ArchitectureBuilder(provider));
        var hostCia2 = (Mos6526)build.SystemsById["c64-host"].Devices.GetByRole(DeviceRole.Cia2)!;
        var driveVia1 = build.SystemsById["drive-8"].Devices.GetAll<Via6522>()
            .OrderBy(v => v.BaseAddress).First();

        hostCia2.Write(0xDD00, 0x08);

        (driveVia1.Read(0x1800) & 0x80).Should().Be(0x80);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-005
    /// Use case: The fidelity field in the sample sets drive-8 to TrueDevice;
    /// this surfaces on the blueprint.
    /// Acceptance: bp.GetFidelity("drive-8") = TrueDevice.
    /// </summary>
    [Fact]
    public void SampleYaml_FidelityField_IsHonored()
    {
        var bp = new MultiSystemYamlLoader().LoadFromFile(SamplePath);

        bp.GetFidelity("drive-8").Should().Be(Fidelity.TrueDevice);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-005
    /// Use case: Coordinator can step both machines under the sample
    /// topology. 50k host cycles advances drive proportionally.
    /// Acceptance: After Step(50_000), host = 50_000; drive within 1 of
    /// expected (1MHz / 985248Hz).
    /// </summary>
    [Fact]
    public void SampleYaml_CoordinatorDrivesBothMachines()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var bp = new MultiSystemYamlLoader().LoadFromFile(SamplePath);
        var build = bp.BuildCoordinatorAuto(new ArchitectureBuilder(provider));
        var c64 = build.SystemsById["c64-host"];
        var drive = build.SystemsById["drive-8"];
        c64.Reset();
        drive.Reset();

        build.Coordinator.Step(50_000);

        c64.Clock.TotalCycles.Should().Be(50_000);
        var expectedDrive = 50_000L * 1_000_000 / 985_248;
        System.Math.Abs(drive.Clock.TotalCycles - expectedDrive).Should().BeLessThanOrEqualTo(1);
    }
}
