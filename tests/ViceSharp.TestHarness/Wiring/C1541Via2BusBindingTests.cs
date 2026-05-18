namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Core.Wiring;
using Xunit;

/// <summary>
/// FR/TR: ARCH-WIRING-006 (Phase G2a).
/// Use case: A 1541 drive's VIA2 chip reads write-protect status from PB4
/// to decide whether the head can write to the disk surface. Without a
/// mounted disk the line floats low (write-protect asserted). With a disk
/// mounted it reads high. The drive firmware uses this to gate writes.
/// </summary>
public sealed class C1541Via2BusBindingTests
{
    private static Via6522 BuildIsolatedVia()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        return new Via6522(bus, irq) { BaseAddress = 0x1C00, Size = 0x0400 };
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-006
    /// Use case: With a mounted, non-ejected disk, VIA2 PB4 reads 1.
    /// Acceptance: Bind(via, disk) -> via.Read(PB) & 0x10 = 0x10.
    /// </summary>
    [Fact]
    public void WithMountedDisk_Pb4ReadsHigh()
    {
        var via = BuildIsolatedVia();
        var disk = new D64DiskImageDevice(new D64Image(new byte[D64Image.DiskSize35Track]));
        C1541Via2BusBinding.Bind(via, disk);

        (via.Read(0x1C00) & 0x10).Should().Be(0x10);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-006
    /// Use case: With no disk supplied, VIA2 PB4 reads 0 (write-protect
    /// asserted; drive sees no media).
    /// Acceptance: Bind(via, null) -> via.Read(PB) & 0x10 = 0.
    /// </summary>
    [Fact]
    public void WithoutDisk_Pb4ReadsLow()
    {
        var via = BuildIsolatedVia();
        C1541Via2BusBinding.Bind(via, disk: null);

        (via.Read(0x1C00) & 0x10).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-006
    /// Use case: Ejecting the disk flips PB4 back to 0 even after binding.
    /// Acceptance: After Bind + Eject, via PB4 = 0.
    /// </summary>
    [Fact]
    public void EjectedDisk_Pb4ReadsLow()
    {
        var via = BuildIsolatedVia();
        var disk = new D64DiskImageDevice(new D64Image(new byte[D64Image.DiskSize35Track]));
        C1541Via2BusBinding.Bind(via, disk);
        (via.Read(0x1C00) & 0x10).Should().Be(0x10);

        disk.Eject();

        (via.Read(0x1C00) & 0x10).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-006
    /// Use case: End-to-end: YAML with diskImagePath gets auto-bound by
    /// BuildCoordinatorAuto. The drive's VIA2 PB4 reflects disk presence
    /// out of the box.
    /// Acceptance: Building a sample multi-system YAML with a mounted D64
    /// produces a drive whose VIA2 PB4 reads high.
    /// </summary>
    [Fact]
    public void AutoBind_With_DiskMounted_DriveVia2Pb4ReadsHigh()
    {
        var imagePath = MakeEmptyD64();
        try
        {
            var yaml = $$"""
                schemaVersion: 1
                coordinator:
                  host:
                    id: c64-host
                    kind: C64
                    busAttachments:
                      - busId: IEC
                        endpointName: c64
                  peripherals:
                    - id: drive-8
                      kind: C1541
                      deviceNumber: 8
                      diskImagePath: {{imagePath.Replace("\\", "/")}}
                      busAttachments:
                        - busId: IEC
                          endpointName: drive-8
                  buses:
                    - id: IEC
                      signals: [ATN, CLK, DATA, SRQ]
                """;
            var provider = MachineTestFactory.CreateC64RomProvider();
            var bp = new MultiSystemYamlLoader().LoadFromString(yaml);
            var build = bp.BuildCoordinatorAuto(new ArchitectureBuilder(provider));

            var driveVia2 = build.SystemsById["drive-8"].Devices.GetAll<Via6522>()
                .OrderByDescending(v => v.BaseAddress).First();

            (driveVia2.Read(0x1C00) & 0x10).Should().Be(0x10);
        }
        finally
        {
            File.Delete(imagePath);
        }
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-006
    /// Use case: Without diskImagePath the auto-bind still fires on VIA2 but
    /// the drive sees no disk, so PB4 reads low.
    /// Acceptance: YAML without diskImagePath -> drive PB4 = 0.
    /// </summary>
    [Fact]
    public void AutoBind_NoDiskMounted_DriveVia2Pb4ReadsLow()
    {
        var yaml = """
            schemaVersion: 1
            coordinator:
              host:
                id: c64-host
                kind: C64
                busAttachments:
                  - busId: IEC
                    endpointName: c64
              peripherals:
                - id: drive-8
                  kind: C1541
                  deviceNumber: 8
                  busAttachments:
                    - busId: IEC
                      endpointName: drive-8
              buses:
                - id: IEC
                  signals: [ATN, CLK, DATA, SRQ]
            """;
        var provider = MachineTestFactory.CreateC64RomProvider();
        var bp = new MultiSystemYamlLoader().LoadFromString(yaml);
        var build = bp.BuildCoordinatorAuto(new ArchitectureBuilder(provider));

        var driveVia2 = build.SystemsById["drive-8"].Devices.GetAll<Via6522>()
            .OrderByDescending(v => v.BaseAddress).First();

        (driveVia2.Read(0x1C00) & 0x10).Should().Be(0);
    }

    private static string MakeEmptyD64()
    {
        var path = Path.Combine(Path.GetTempPath(), $"viceharness-via2-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, new byte[D64Image.DiskSize35Track]);
        return path;
    }
}
