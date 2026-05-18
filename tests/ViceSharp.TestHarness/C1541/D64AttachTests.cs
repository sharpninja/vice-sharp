namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-DRIVE-IMAGE-001 (Phase G1).
/// Use case: A user supplies a D64 image path in the multi-system YAML
/// peripheral spec; the 1541 machine builds with the image mounted and
/// reachable via the device registry under DeviceRole.DriveDisk. VIA2
/// wiring (Phase G2+) will resolve sector data from this device.
/// </summary>
public sealed class D64AttachTests
{
    /// <summary>Write a minimal 174,848-byte D64 to a temp file and return its path.</summary>
    private static string MakeEmptyD64()
    {
        var path = Path.Combine(Path.GetTempPath(), $"viceharness-{Guid.NewGuid():N}.d64");
        var bytes = new byte[D64Image.DiskSize35Track];
        // Tag track 18 sector 0 BAM with VICE marker so a future read can sanity-check.
        bytes[0x16500] = 0x12; // track 18
        bytes[0x16501] = 0x01; // sector 1
        File.WriteAllBytes(path, bytes);
        return path;
    }

    /// <summary>
    /// FR/TR: ARCH-DRIVE-IMAGE-001
    /// Use case: C1541Descriptor with diskImagePath set + builder loads the
    /// image and registers a DriveDisk device on the drive machine.
    /// Acceptance: machine.Devices.GetByRole(DriveDisk) returns a non-null
    /// D64DiskImageDevice with the right source path.
    /// </summary>
    [Fact]
    public void DescriptorWithImagePath_BuildsMachineWithMountedDisk()
    {
        var imagePath = MakeEmptyD64();
        try
        {
            var provider = MachineTestFactory.CreateC64RomProvider();
            var descriptor = new C1541Descriptor(deviceNumber: 8, diskImagePath: imagePath);

            var machine = new ArchitectureBuilder(provider).Build(descriptor);

            var disk = (D64DiskImageDevice?)machine.Devices.GetByRole(DeviceRole.DriveDisk);
            disk.Should().NotBeNull();
            disk!.SourcePath.Should().Be(imagePath);
            disk.IsEjected.Should().BeFalse();
        }
        finally
        {
            File.Delete(imagePath);
        }
    }

    /// <summary>
    /// FR/TR: ARCH-DRIVE-IMAGE-001
    /// Use case: Descriptor without a diskImagePath leaves the machine
    /// without a DriveDisk device (drive is empty).
    /// Acceptance: GetByRole(DriveDisk) returns null.
    /// </summary>
    [Fact]
    public void DescriptorWithoutImagePath_HasNoDriveDiskDevice()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var descriptor = new C1541Descriptor();

        var machine = new ArchitectureBuilder(provider).Build(descriptor);

        machine.Devices.GetByRole(DeviceRole.DriveDisk).Should().BeNull();
    }

    /// <summary>
    /// FR/TR: ARCH-DRIVE-IMAGE-001
    /// Use case: A missing image path produces a clear error at build time
    /// rather than silently leaving the drive empty.
    /// Acceptance: Build with non-existent path throws FileNotFoundException.
    /// </summary>
    [Fact]
    public void Build_MissingImagePath_Throws_FileNotFound()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var descriptor = new C1541Descriptor(deviceNumber: 8, diskImagePath: "Z:\\does-not-exist.d64");

        Assert.Throws<FileNotFoundException>(() =>
            new ArchitectureBuilder(provider).Build(descriptor));
    }

    /// <summary>
    /// FR/TR: ARCH-DRIVE-IMAGE-001
    /// Use case: A wrong-sized image is rejected.
    /// Acceptance: Build with a 1KB junk file throws ArgumentException
    /// from D64Image's size validation.
    /// </summary>
    [Fact]
    public void Build_WrongSizedImage_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"viceharness-bad-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, new byte[1024]);
        try
        {
            var provider = MachineTestFactory.CreateC64RomProvider();
            var descriptor = new C1541Descriptor(deviceNumber: 8, diskImagePath: path);

            Assert.Throws<ArgumentException>(() =>
                new ArchitectureBuilder(provider).Build(descriptor));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// FR/TR: ARCH-DRIVE-IMAGE-001
    /// Use case: Multi-system YAML with diskImagePath on a C1541 peripheral
    /// produces a drive machine with the disk mounted.
    /// Acceptance: After BuildCoordinatorAuto, drive-8 has a DriveDisk
    /// device with the right source path.
    /// </summary>
    [Fact]
    public void MultiSystemYaml_WithDiskImagePath_MountsDisk()
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

            var drive = build.SystemsById["drive-8"];
            var disk = (D64DiskImageDevice?)drive.Devices.GetByRole(DeviceRole.DriveDisk);
            disk.Should().NotBeNull();
            Path.GetFullPath(disk!.SourcePath!).Should().Be(Path.GetFullPath(imagePath));
        }
        finally
        {
            File.Delete(imagePath);
        }
    }

    /// <summary>
    /// FR/TR: ARCH-DRIVE-IMAGE-001
    /// Use case: D64DiskImageDevice exposes the parsed image for downstream
    /// VIA2 wiring + supports Eject for hot-swap workflows.
    /// Acceptance: After Eject, IsEjected = true; SourcePath unchanged.
    /// </summary>
    [Fact]
    public void DiskImageDevice_Eject_FlipsState()
    {
        var imagePath = MakeEmptyD64();
        try
        {
            var disk = D64DiskImageDevice.LoadFromFile(imagePath);

            disk.Eject();

            disk.IsEjected.Should().BeTrue();
            disk.SourcePath.Should().Be(imagePath);
        }
        finally
        {
            File.Delete(imagePath);
        }
    }
}
