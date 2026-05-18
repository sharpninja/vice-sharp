namespace ViceSharp.TestHarness.C1541;

using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR/TR: ARCH-TRUEDRIVE-1541-001 (Phase B1c).
/// Use case: ArchitectureBuilder builds a complete standalone 1541 drive
/// machine from a C1541Descriptor + IRomProvider. Drive bus exposes RAM,
/// ROM, and two VIAs at their canonical addresses; the drive CPU resets
/// from the ROM-supplied $FFFC/$FFFD vector.
/// </summary>
public sealed class C1541MachineBuildTests
{
    private static IMachine BuildC1541Machine()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        return new ArchitectureBuilder(provider).Build(new C1541Descriptor());
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: A drive machine is built standalone.
    /// Acceptance: Machine has drive CPU, drive RAM, drive ROM, two VIAs;
    /// architecture clock = 1 MHz.
    /// </summary>
    [Fact]
    public void Build_ProducesMachineWithDriveRoleDevices()
    {
        var machine = BuildC1541Machine();

        machine.Architecture.MachineName.Should().Be("Commodore 1541");
        machine.Architecture.MasterClockHz.Should().Be(1_000_000);
        machine.Clock.FrequencyHz.Should().Be(1_000_000);

        machine.Devices.GetByRole(DeviceRole.DriveCpu).Should().NotBeNull();
        machine.Devices.GetByRole(DeviceRole.DriveRam).Should().NotBeNull();
        machine.Devices.GetByRole(DeviceRole.DriveRom).Should().NotBeNull();
        var vias = machine.Devices.GetAll<Via6522>();
        vias.Count.Should().Be(2);
        vias.Select(v => v.BaseAddress).Should().BeEquivalentTo(new ushort[] { 0x1800, 0x1C00 });
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Drive RAM is writeable; reads return what was written.
    /// Acceptance: Write/Read at $0123 round-trips.
    /// </summary>
    [Fact]
    public void DriveRam_ReadsBackWhatWasWritten()
    {
        var machine = BuildC1541Machine();

        machine.Bus.Write(0x0123, 0x42);

        machine.Bus.Read(0x0123).Should().Be(0x42);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Drive ROM is read-only at $C000-$FFFF; the 16KB image is
    /// loaded into that window.
    /// Acceptance: Reading the reset vector at $FFFC/$FFFD returns the same
    /// bytes as the file's last 4-byte tail; writes to ROM are ignored.
    /// </summary>
    [Fact]
    public void DriveRom_LoadsAt_C000_AndIsReadOnly()
    {
        var machine = BuildC1541Machine();

        var provider = MachineTestFactory.CreateC64RomProvider();
        var rom = provider.LoadRom(C1541ViceRomNames.Dos1541, C1541ViceRomNames.ArchitectureKey).Span;
        rom.Length.Should().Be(C1541ViceRomNames.Dos1541RomSize);

        machine.Bus.Read(0xC000).Should().Be(rom[0]);
        machine.Bus.Read(0xFFFC).Should().Be(rom[0x3FFC]);
        machine.Bus.Read(0xFFFD).Should().Be(rom[0x3FFD]);

        machine.Bus.Write(0xC000, (byte)~rom[0]);
        machine.Bus.Read(0xC000).Should().Be(rom[0]);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: After Reset, the drive CPU PC equals the 16-bit value at
    /// the bus reset vector ($FFFC low, $FFFD high) - confirms the ROM is
    /// wired into the drive's address space.
    /// Acceptance: machine.GetState().PC matches reset-vector reconstruction.
    /// </summary>
    [Fact]
    public void Reset_LoadsPcFromRomResetVector()
    {
        var machine = BuildC1541Machine();
        machine.Reset();

        var lo = machine.Bus.Read(0xFFFC);
        var hi = machine.Bus.Read(0xFFFD);
        var expectedPc = (ushort)(lo | (hi << 8));

        machine.GetState().PC.Should().Be(expectedPc);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: VIAs respond to bus reads + writes at their assigned
    /// addresses; mirroring across the 1KB window works (1541 standard).
    /// Acceptance: Writing DDRA via $1803 reads back via $1813 + $1BFF; same
    /// for VIA2 at $1C03 / $1C13.
    /// </summary>
    [Fact]
    public void Vias_RespondToBusAccess_WithMirroring()
    {
        var machine = BuildC1541Machine();

        machine.Bus.Write(0x1803, 0xA5);
        machine.Bus.Read(0x1813).Should().Be(0xA5);

        machine.Bus.Write(0x1C03, 0x5A);
        machine.Bus.Read(0x1CF3).Should().Be(0x5A);
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Builder validates the ROM set; missing 1541 DOS ROM throws
    /// a clear error.
    /// Acceptance: Building with a provider that lacks the drive ROM throws
    /// InvalidOperationException mentioning the ROM set.
    /// </summary>
    [Fact]
    public void Build_WithMissingRom_ThrowsClearError()
    {
        var builder = new ArchitectureBuilder(new NoRomProvider());

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build(new C1541Descriptor()));
        ex.Message.Should().Contain("ROM");
    }

    /// <summary>
    /// FR/TR: ARCH-TRUEDRIVE-1541-001
    /// Use case: Builder refuses without an IRomProvider.
    /// Acceptance: Default-constructed ArchitectureBuilder + C1541Descriptor
    /// throws.
    /// </summary>
    [Fact]
    public void Build_WithoutRomProvider_ThrowsClearError()
    {
        var builder = new ArchitectureBuilder();

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build(new C1541Descriptor()));
        ex.Message.Should().Contain("IRomProvider");
    }

    private sealed class NoRomProvider : IRomProvider
    {
        public bool IsAvailable(string romName, string architecture) => false;
        public ReadOnlyMemory<byte> LoadRom(string romName, string architecture) =>
            throw new InvalidOperationException("No ROMs.");
    }
}
