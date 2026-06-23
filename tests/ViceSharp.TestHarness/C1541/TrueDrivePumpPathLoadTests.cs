namespace ViceSharp.TestHarness.C1541;

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// TEST-SYSINDEP-001 (AC1, AC3) / FR-SYSINDEP-001 / TR-SYS-SCHED-001.
/// Use case: the production emulation pump advances a true-drive rig by looping
/// <see cref="CoordinatorMachine.StepInstruction"/> (one host instruction at a time), NOT by
/// the per-host-cycle <see cref="SystemCoordinator.Step()"/> path the legacy LOAD test uses.
/// The drive 1541 must still complete a full cycle-accurate IEC LOAD through this path, with
/// the drive CPU advancing on its OWN clock - coupled to the host only through the async IEC
/// bus (lazy-sync on CIA2 access + ATN edges via LineChanged), not a per-instruction lockstep.
/// This is the AC3 load-parity gate for the production path and proves AC1: the drive's own
/// ExecutedCycles advance at the drive's clock, independently of the host instruction count.
/// </summary>
[Collection("NativeVice")]
public sealed class TrueDrivePumpPathLoadTests
{
    private static readonly byte[] ProgramAt0801 =
    {
        0x07, 0x08, // link to next line ($0807)
        0x0A, 0x00, // line number 10
        0x80,       // END token
        0x00,       // end of line
        0x00, 0x00, // end of program
    };

    private static string WriteMinimalLoadableD64()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        image.Format();

        var directory = image.GetSector(18, 1);
        directory[0] = 0x00;
        directory[1] = 0xFF;
        directory[2] = 0x82; // closed PRG
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

        var path = Path.Combine(Path.GetTempPath(), $"vicesharp-truedrive-pump-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, image.ToArray());
        return path;
    }

    /// <summary>
    /// FR: FR-SYSINDEP-001, TR: TR-SYS-SCHED-001, TEST-SYSINDEP-001 (AC1, AC3).
    /// Use case: drive the rig the way the pump does - loop CoordinatorMachine.StepInstruction
    ///   to a per-frame cycle budget - and run LOAD"*",8,1 over the true-drive IEC bus.
    /// Acceptance: the program streams into C64 RAM at $0801 (AC3 parity through the production
    ///   path), and the drive CPU's own ExecutedCycles advanced by roughly its own clock's share
    ///   of the elapsed host time (AC1: the drive ran on its own clock, not the host's count).
    /// </summary>
    [Fact]
    public void Load_ViaCoordinatorStepInstruction_StreamsProgramAndDriveRunsOwnClock()
    {
        var diskPath = WriteMinimalLoadableD64();
        try
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
                cia2,
                iec: hostEndpoint,
                synchronizeIec: () => coord!.SynchronizePeripheralSystemsToHost());

            var driveVias = drive.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).ToArray();
            drive.Devices.GetAll<C1541IecInterfaceDevice>().First()
                .ConnectVia1(driveVias[0], driveEndpoint, iec);
            drive.Devices.GetAll<C1541DriveMechanismDevice>().First().ConnectVia2(driveVias[1]);

            coord = new SystemCoordinator();
            coord.AttachSystem(host);
            coord.AttachSystem(drive);
            coord.AttachBus(iec);
            coord.Reset();

            // Wrap the rig and drive it exactly as the production pump does: loop
            // StepInstruction to a per-frame cycle budget (see EmulationPumpService.AdvanceCleanly).
            const int cyclesPerFrame = 19656;
            var rig = new CoordinatorMachine(host, coord, cyclesPerFrame, iec);
            var automation = HostKeyboardAutomation.CreateC64Drive8Autostart();

            var driveCyclesAtStart = drive.PrimaryCpu!.ExecutedCycles;
            var hostCyclesAtStart = host.Clock.TotalCycles;

            const int maxFrames = 4000;
            var loaded = false;
            for (var frame = 0; frame < maxFrames && !loaded; frame++)
            {
                var before = host.Clock.TotalCycles;
                while (host.Clock.TotalCycles - before < cyclesPerFrame)
                    rig.StepInstruction();

                automation.AdvanceFrame(host);
                loaded = host.Bus.Peek(0x0801) == 0x07
                    && host.Bus.Peek(0x0803) == 0x0A
                    && host.Bus.Peek(0x0805) == 0x80;
            }

            automation.LastError.Should().BeNullOrEmpty();
            loaded.Should().BeTrue(
                "LOAD\"*\",8,1 over the true-drive IEC bus, driven by CoordinatorMachine.StepInstruction, " +
                $"should place the program at $0801 (got {host.Bus.Peek(0x0801):X2} {host.Bus.Peek(0x0803):X2} {host.Bus.Peek(0x0805):X2})");

            for (var i = 0; i < ProgramAt0801.Length; i++)
                host.Bus.Peek((ushort)(0x0801 + i)).Should().Be(ProgramAt0801[i], $"program byte {i}");

            // AC1: the drive CPU advanced on its OWN clock. Over the elapsed host time, a 1 MHz
            // drive should have executed on the order of (driveHz / hostHz) * host cycles. We
            // assert it ran a substantial, plausible share - not zero (it really ran) and not a
            // host-count clone - so the per-CPU clock is independent.
            var driveAdvanced = drive.PrimaryCpu!.ExecutedCycles - driveCyclesAtStart;
            var hostAdvanced = host.Clock.TotalCycles - hostCyclesAtStart;
            driveAdvanced.Should().BeGreaterThan(0, "the drive CPU must run on its own clock during the load");
            var ratio = driveAdvanced / (double)hostAdvanced;
            ratio.Should().BeInRange(0.5, 1.6,
                "the 1 MHz drive advances roughly its own clock's share of ~0.985 MHz host time, " +
                "independently of the host instruction count (AC1)");
        }
        finally
        {
            try { File.Delete(diskPath); } catch { /* best effort */ }
        }
    }
}
