using System.Linq;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;

namespace ViceSharp.Host.Runtime;

/// <summary>
/// FR-DRVTRUE-001 / FR-IECLOAD-001: builds the gated "true-drive" rig - a C64
/// host plus a cycle-accurate emulated 1541 (6502 + VIA1/VIA2 + DOS ROM) wired
/// CIA2 &lt;-&gt; IEC &lt;-&gt; VIA1 under one <see cref="SystemCoordinator"/> - and
/// presents it as a single <see cref="CoordinatorMachine"/> for the host runtime.
///
/// This is the same wiring proven end-to-end by the true-drive LOAD test; the
/// factory only builds it when the user opts a drive into True Drive, so the
/// default (simulated) path is byte-identical and native lockstep parity is
/// unaffected.
/// </summary>
public static class C64TrueDriveRigBuilder
{
    // PAL C64 frame: 312 lines x 63 cycles. Matches Commodore64.RunFrame and the
    // host-cycle budget the coordinator advances per frame.
    private const int HostCyclesPerFrame = 19656;

    /// <summary>
    /// Build the C64 + true-drive 1541 coordinator rig.
    /// </summary>
    /// <param name="builder">Architecture builder (with C64 ROMs).</param>
    /// <param name="hostDescriptor">The C64 host descriptor to build.</param>
    /// <param name="driveDevice">IEC device number for the drive (8 or 9).</param>
    /// <param name="diskImagePath">Optional D64 image to insert at build time.</param>
    /// <returns>A <see cref="CoordinatorMachine"/> driving the whole rig.</returns>
    public static CoordinatorMachine Build(
        IArchitectureBuilder builder,
        IArchitectureDescriptor hostDescriptor,
        int driveDevice = 8,
        string? diskImagePath = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(hostDescriptor);

        var host = builder.Build(hostDescriptor);
        var drive = builder.Build(new C1541Descriptor(driveDevice, diskImagePath));

        var iec = IecInterSystemBus.Create();
        var hostEndpoint = iec.AttachEndpoint("c64");
        var driveEndpoint = iec.AttachEndpoint($"drive-{driveDevice}");

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

        // Expose the rig's live IEC bus so the session activity monitor watches
        // the bus that actually carries traffic (not the host's unused always-on
        // bus). Without this the True Drive activity/LED would read idle.
        return new CoordinatorMachine(host, coord, HostCyclesPerFrame, iec);
    }
}
