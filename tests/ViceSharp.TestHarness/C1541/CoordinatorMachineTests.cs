namespace ViceSharp.TestHarness.C1541;

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// FR/TR: FR-IECLOAD-001 / TR-DRVLIFE-001 / TEST-IECLOAD-001.
/// Use case: The host runtime only knows IMachine. CoordinatorMachine presents
/// a wired C64 + true-drive 1541 rig as a single IMachine so the GUI runtime
/// runs both in lockstep. This proves the adapter (a) advances host and drive
/// together on RunFrame and (b) is a drop-in IMachine on which LOAD"*",8,1
/// completes - the exact path the GUI needs.
/// </summary>
[Collection("NativeVice")]
public sealed class CoordinatorMachineTests
{
    private const int PalCyclesPerFrame = 19656;

    private static readonly byte[] ProgramAt0801 =
    {
        0x07, 0x08, 0x0A, 0x00, 0x80, 0x00, 0x00, 0x00,
    };

    private static string WriteMinimalLoadableD64()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        image.Format();
        var directory = image.GetSector(18, 1);
        directory[0] = 0x00;
        directory[1] = 0xFF;
        directory[2] = 0x82;
        directory[3] = 17;
        directory[4] = 0;
        const string name = "TEST";
        for (var i = 0; i < 16; i++)
            directory[5 + i] = (byte)(i < name.Length ? name[i] : 0xA0);
        var file = image.GetSector(17, 0);
        var payload = new byte[] { 0x01, 0x08 }.Concat(ProgramAt0801).ToArray();
        file[0] = 0x00;
        file[1] = (byte)(2 + payload.Length - 1);
        payload.CopyTo(file.Slice(2));
        var path = Path.Combine(Path.GetTempPath(), $"vicesharp-coordmachine-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, image.ToArray());
        return path;
    }

    private static (CoordinatorMachine machine, IMachine drive) BuildWiredRig(string diskPath)
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var builder = new ArchitectureBuilder(provider);
        var host = builder.Build(new C64Descriptor(C64MachineProfiles.C64Pal));
        var drive = builder.Build(new C1541Descriptor(8, diskPath));

        var iec = IecInterSystemBus.Create();
        var hostEndpoint = iec.AttachEndpoint("c64");
        var driveEndpoint = iec.AttachEndpoint("drive-8");

        SystemCoordinator? coord = null;
        var cia2 = (Mos6526)host.Devices.GetByRole(DeviceRole.Cia2)!;
        host.Devices.GetAll<C64Cia2InterfaceDevice>().First().ConnectCia2(
            cia2, iec: hostEndpoint, synchronizeIec: () => coord!.SynchronizePeripheralSystemsToHost());

        var driveVias = drive.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).ToArray();
        drive.Devices.GetAll<C1541IecInterfaceDevice>().First().ConnectVia1(driveVias[0], driveEndpoint, iec);
        drive.Devices.GetAll<C1541DriveMechanismDevice>().First().ConnectVia2(driveVias[1]);

        coord = new SystemCoordinator();
        coord.AttachSystem(host);
        coord.AttachSystem(drive);
        coord.AttachBus(iec);
        coord.Reset();

        return (new CoordinatorMachine(host, coord, PalCyclesPerFrame), drive);
    }

    /// <summary>
    /// Acceptance: RunFrame on the adapter advances the host ~one PAL frame and
    /// the drive proportionally; Bus/Devices delegate to the host.
    /// </summary>
    [Fact]
    public void RunFrame_AdvancesHostAndDrive()
    {
        var diskPath = WriteMinimalLoadableD64();
        try
        {
            var (machine, drive) = BuildWiredRig(diskPath);
            machine.Devices.Should().BeSameAs(machine.Host.Devices);

            var hostBefore = machine.Clock.TotalCycles;
            var driveBefore = drive.Clock.TotalCycles;
            machine.RunFrame();

            (machine.Clock.TotalCycles - hostBefore).Should().Be(PalCyclesPerFrame);
            (drive.Clock.TotalCycles - driveBefore).Should().BeGreaterThan(0);
        }
        finally
        {
            try { File.Delete(diskPath); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Acceptance: Driven purely through the IMachine surface (RunFrame +
    /// keyboard automation), LOAD"*",8,1 places the program at $0801.
    /// </summary>
    [Fact]
    public void Load_ThroughAdapterAsIMachine_LandsProgramAt0801()
    {
        var diskPath = WriteMinimalLoadableD64();
        try
        {
            var (machine, _) = BuildWiredRig(diskPath);
            var automation = HostKeyboardAutomation.CreateC64Drive8Autostart();

            var loaded = false;
            for (var frame = 0; frame < 2000 && !loaded; frame++)
            {
                machine.RunFrame();
                automation.AdvanceFrame(machine);
                loaded = machine.Bus.Peek(0x0801) == 0x07
                    && machine.Bus.Peek(0x0803) == 0x0A
                    && machine.Bus.Peek(0x0805) == 0x80;
            }

            loaded.Should().BeTrue("LOAD over the adapter should place the program at $0801");
        }
        finally
        {
            try { File.Delete(diskPath); } catch { /* best effort */ }
        }
    }
}
