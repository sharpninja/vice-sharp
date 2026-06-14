namespace ViceSharp.TestHarness.Wiring;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.Multisystem;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-WIRING-006 (Phase G2a).
/// Use case: A 1541 drive's VIA2 chip reads write-protect status from PB4
/// to decide whether the head can write to the disk surface. Without a
/// mounted disk the line floats low (write-protect asserted). With a disk
/// mounted it reads high. The drive firmware uses this to gate writes.
/// </summary>
public sealed class C1541DriveMechanismDeviceTests
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
        new C1541DriveMechanismDevice(disk).ConnectVia2(via);

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
        new C1541DriveMechanismDevice().ConnectVia2(via);

        (via.Read(0x1C00) & 0x10).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: The 1541 drive mechanism raises the drive CPU overflow flag
    /// when byte-ready is enabled and the motor is running, matching VICE
    /// drivecpu_set_overflow from via2d byte-ready handling.
    /// Acceptance: Connect VIA2 + enable byte-ready in PCR + motor on + 32
    /// mechanism ticks sets P.V.
    /// </summary>
    [Fact]
    public void ByteReadyTick_WithMotorOn_SetsDriveCpuOverflow()
    {
        var via = BuildIsolatedVia();
        var cpu = new Mos6502(new BasicBus()) { P = 0 };
        var disk = new D64DiskImageDevice(new D64Image(new byte[D64Image.DiskSize35Track]));
        var mechanism = new C1541DriveMechanismDevice(disk, cpu);
        mechanism.ConnectVia2(via);

        via.Write(0x1C0C, 0x02);
        via.Write(0x1C02, 0xFF);
        via.Write(0x1C00, 0x06);
        for (int i = 0; i < 512 && (cpu.P & 0x40) == 0; i++)
            mechanism.Tick();

        (cpu.P & 0x40).Should().Be(0x40);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: Byte-ready must not assert the drive CPU overflow flag while
    /// the disk motor is off.
    /// Acceptance: Connected mechanism ticks with motor off leave P.V clear.
    /// </summary>
    [Fact]
    public void ByteReadyTick_WithMotorOff_LeavesDriveCpuOverflowClear()
    {
        var via = BuildIsolatedVia();
        var cpu = new Mos6502(new BasicBus()) { P = 0 };
        var mechanism = new C1541DriveMechanismDevice(driveCpu: cpu);
        mechanism.ConnectVia2(via);

        for (int i = 0; i < 64; i++)
            mechanism.Tick();

        (cpu.P & 0x40).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007.
    /// Use case: In VICE via2d, reading VIA2 port A returns the latched GCR
    /// byte and acknowledges BYTE READY by clearing byte_ready_level.
    /// Acceptance: after byte-ready is raised, a PA read clears the private
    /// drive-glue byte-ready level without exposing 1541 policy on the VIA.
    /// </summary>
    [Fact]
    public void PortARead_AcknowledgesByteReadyLevel()
    {
        var via = BuildIsolatedVia();
        var cpu = new Mos6502(new BasicBus()) { P = 0 };
        var disk = new D64DiskImageDevice(new D64Image(new byte[D64Image.DiskSize35Track]));
        var mechanism = new C1541DriveMechanismDevice(disk, cpu);
        mechanism.ConnectVia2(via);

        EnableByteReadyAndMotor(via, 0x06);
        TickUntilOverflow(mechanism, cpu, 512).Should().BePositive();
        GetPrivateBool(mechanism, "_byteReadyLevel").Should().BeTrue();

        via.Read(0x1C01);

        GetPrivateBool(mechanism, "_byteReadyLevel").Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007.
    /// Use case: VICE also clears byte_ready_level on VIA2 port B reads while
    /// composing SYNC/write-protect/status bits for the 1541 board.
    /// Acceptance: after byte-ready is raised, a PB read clears the private
    /// drive-glue byte-ready level.
    /// </summary>
    [Fact]
    public void PortBRead_AcknowledgesByteReadyLevel()
    {
        var via = BuildIsolatedVia();
        var cpu = new Mos6502(new BasicBus()) { P = 0 };
        var disk = new D64DiskImageDevice(new D64Image(new byte[D64Image.DiskSize35Track]));
        var mechanism = new C1541DriveMechanismDevice(disk, cpu);
        mechanism.ConnectVia2(via);

        EnableByteReadyAndMotor(via, 0x06);
        TickUntilOverflow(mechanism, cpu, 512).Should().BePositive();
        GetPrivateBool(mechanism, "_byteReadyLevel").Should().BeTrue();

        via.Read(0x1C00);

        GetPrivateBool(mechanism, "_byteReadyLevel").Should().BeFalse();
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007.
    /// Use case: VICE uses 1541 VIA2 PB5/PB6 as disk speed-zone outputs; the
    /// selected zone changes the number of CPU cycles between byte-ready
    /// pulses.
    /// Acceptance: PB5/PB6 zone 3 reaches the first non-sync byte-ready event
    /// in fewer ticks than zone 0.
    /// </summary>
    [Fact]
    public void PortBZoneBits_SelectByteReadyCadence()
    {
        var slowTicks = TicksUntilFirstByteReady(0x06);
        var fastTicks = TicksUntilFirstByteReady(0x66);

        fastTicks.Should().BeLessThan(slowTicks);
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
        new C1541DriveMechanismDevice(disk).ConnectVia2(via);
        (via.Read(0x1C00) & 0x10).Should().Be(0x10);

        disk.Eject();

        (via.Read(0x1C00) & 0x10).Should().Be(0);
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: VICE's 1541 VIA2 PB7 reports disk SYNC low while the head is
    /// over a GCR sync field.
    /// Acceptance: Mounted disk + motor on eventually reads PB7 = 0 when the
    /// rotating head reaches a sync field.
    /// </summary>
    [Fact]
    public void MotorOn_AtGcrSync_Pb7ReadsLow()
    {
        var via = BuildIsolatedVia();
        var disk = new D64DiskImageDevice(new D64Image(new byte[D64Image.DiskSize35Track]));
        new C1541DriveMechanismDevice(disk).ConnectVia2(via);

        via.Write(0x1C02, 0x7F);
        via.Write(0x1C00, 0x06);

        ReadUntilSync(via, 8_000).Should().BeTrue();
    }

    /// <summary>
    /// FR/TR: ARCH-WIRING-007
    /// Use case: Once the sync bytes have passed, VIA2 PB7 returns high so
    /// the 1541 DOS can distinguish sync from normal GCR data.
    /// Acceptance: Reading PA until the first non-sync byte then reading PB
    /// reports PB7 = 1.
    /// </summary>
    [Fact]
    public void AfterGcrSync_Pb7ReadsHigh()
    {
        var via = BuildIsolatedVia();
        var disk = new D64DiskImageDevice(new D64Image(new byte[D64Image.DiskSize35Track]));
        new C1541DriveMechanismDevice(disk).ConnectVia2(via);

        via.Write(0x1C02, 0x7F);
        via.Write(0x1C00, 0x06);
        ReadUntilNonSync(via, 8_000).Should().NotBe(0xFF);

        (via.Read(0x1C00) & 0x80).Should().Be(0x80);
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

    private static byte[] ReadBytes(Via6522 via, int count)
    {
        var result = new byte[count];
        for (var i = 0; i < result.Length; i++)
            result[i] = via.Read(0x1C01);
        return result;
    }

    private static void EnableByteReadyAndMotor(Via6522 via, byte portBValue)
    {
        via.Write(0x1C0C, 0x02);
        via.Write(0x1C02, 0xFF);
        via.Write(0x1C00, portBValue);
    }

    private static int TickUntilOverflow(C1541DriveMechanismDevice mechanism, Mos6502 cpu, int maxTicks)
    {
        for (var i = 1; i <= maxTicks; i++)
        {
            mechanism.Tick();
            if ((cpu.P & 0x40) != 0)
                return i;
        }

        return -1;
    }

    private static int TicksUntilFirstByteReady(byte portBValue)
    {
        var via = BuildIsolatedVia();
        var cpu = new Mos6502(new BasicBus()) { P = 0 };
        var disk = new D64DiskImageDevice(new D64Image(new byte[D64Image.DiskSize35Track]));
        var mechanism = new C1541DriveMechanismDevice(disk, cpu);
        mechanism.ConnectVia2(via);

        EnableByteReadyAndMotor(via, portBValue);
        var ticks = TickUntilOverflow(mechanism, cpu, 512);
        ticks.Should().BePositive();
        return ticks;
    }

    private static bool GetPrivateBool(object target, string fieldName)
    {
        var field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field.Should().NotBeNull();
        return (bool)field!.GetValue(target)!;
    }

    private static byte ReadUntilNonSync(Via6522 via, int maxReads)
    {
        for (var i = 0; i < maxReads; i++)
        {
            var value = via.Read(0x1C01);
            if (value != 0xFF)
                return value;
        }

        return 0xFF;
    }

    private static bool ReadUntilSync(Via6522 via, int maxReads)
    {
        for (var i = 0; i < maxReads; i++)
        {
            if ((via.Read(0x1C00) & 0x80) == 0)
                return true;
        }

        return false;
    }
}
