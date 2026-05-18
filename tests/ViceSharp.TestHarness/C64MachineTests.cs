namespace ViceSharp.TestHarness;

using ViceSharp.Architectures.C64;
using ViceSharp.Architectures.EmptyMachine;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using System.Text;
using Xunit;

public sealed class C64MachineTests
{
    /// <summary>
    /// FR: FR-PRF-001, TR: TR-CYCLE-001.
    /// Use case: An EmptyMachineDescriptor builds the most stripped-down
    /// machine (CPU + RAM) without needing any ROM provider; the C64
    /// builder should not be required.
    /// Acceptance: The built machine exposes a CPU and SystemRam but no
    /// VideoChip role; ROM provider injection is not required.
    /// </summary>
    [Fact]
    public void EmptyMachineDescriptor_BuildsWithoutRomProvider()
    {
        var machine = new ArchitectureBuilder().Build(new EmptyMachineDescriptor());

        Assert.NotNull(machine.Devices.GetByRole(Abstractions.DeviceRole.Cpu));
        Assert.NotNull(machine.Devices.GetByRole(Abstractions.DeviceRole.SystemRam));
        Assert.Null(machine.Devices.GetByRole(Abstractions.DeviceRole.VideoChip));
    }

    /// <summary>
    /// FR: FR-CFG-002, TR: TR-CYCLE-001.
    /// Use case: Building a C64Descriptor without an IRomProvider must
    /// fail with a clear, user-friendly error message rather than a
    /// NullReferenceException deep inside the builder.
    /// Acceptance: <c>ArchitectureBuilder.Build(new C64Descriptor())</c>
    /// throws InvalidOperationException whose message contains the
    /// string "requires an IRomProvider".
    /// </summary>
    [Fact]
    public void C64Descriptor_WithoutRomProvider_ThrowsClearError()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => new ArchitectureBuilder().Build(new C64Descriptor()));

        Assert.Contains("requires an IRomProvider", ex.Message);
    }

    /// <summary>
    /// FR: FR-CFG-002, TR: TR-CYCLE-001.
    /// Use case: A ROM provider that returns bytes failing the documented
    /// length/SHA256 contract must cause the C64 builder to reject the
    /// configuration with a clear error rather than booting a corrupted
    /// machine.
    /// Acceptance: ArchitectureBuilder.Build throws InvalidOperationException
    /// whose message mentions the invalid checksum entries in the ROM set.
    /// </summary>
    [Fact]
    public void C64Descriptor_WithBadRomChecksums_ThrowsClearError()
    {
        var badRomProvider = new CorruptLengthAndHashRomProvider();
        var ex = Assert.Throws<InvalidOperationException>(() => new ArchitectureBuilder(badRomProvider).Build(new C64Descriptor()));

        Assert.Contains("ROM set is invalid or missing expected checksum entries", ex.Message);
    }

    /// <summary>
    /// FR: FR-MEM-001, FR: FR-MEM-002, TR: TR-CYCLE-001.
    /// Use case: After reset, the BASIC ROM at $A000 is visible; writing
    /// to $A000 writes-under the ROM, and disabling LORAM (by clearing
    /// the I/O port bit) exposes the underlying RAM byte to reads.
    /// Acceptance: Reading $A000 first returns the BASIC ROM byte even
    /// after writing $42 there; after clearing LORAM via $0001, the
    /// same address reads back $42 from the banked-RAM-under-ROM region.
    /// </summary>
    [Fact]
    public void BasicRom_IsVisibleByDefault_AndBankedRamAppears_WhenLoramDisabled()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var basicRomFirstByte = MachineTestFactory.LoadC64Rom("basic").Span[0];

        machine.Bus.Write(0xA000, 0x42);

        Assert.Equal(basicRomFirstByte, machine.Bus.Read(0xA000));

        machine.Bus.Write(0x0001, 0x36);

        Assert.Equal(0x42, machine.Bus.Read(0xA000));
    }

    /// <summary>
    /// FR: FR-MEM-001, FR: FR-MEM-005, TR: TR-CYCLE-001.
    /// Use case: Clearing the CHAREN bit in the I/O port at $0001 swaps
    /// the character ROM in over the I/O region at $D000; setting CHAREN
    /// returns I/O visibility.
    /// Acceptance: With CHAREN clear ($33 written to $0001), reads from
    /// $D000 return the character ROM byte; with CHAREN set ($37) the
    /// same address peeks back the previously-written I/O value.
    /// </summary>
    [Fact]
    public void CharacterRom_CanBeMappedOverIoSpace_WhenCharenCleared()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var charRomFirstByte = MachineTestFactory.LoadC64Rom("characters").Span[0];

        Assert.NotEqual(0x12, charRomFirstByte);

        machine.Bus.Write(0xD000, 0x12);
        Assert.Equal(0x12, machine.Bus.Peek(0xD000));

        machine.Bus.Write(0x0001, 0x33);
        Assert.Equal(charRomFirstByte, machine.Bus.Read(0xD000));

        machine.Bus.Write(0x0001, 0x37);
        Assert.Equal(0x12, machine.Bus.Peek(0xD000));
    }

    /// <summary>
    /// FR: FR-MEM-005, TR: TR-CYCLE-001.
    /// Use case: Color RAM at $D800-$DBFF is physically a 4-bit memory
    /// so high-nibble writes must be masked off on readback.
    /// Acceptance: Writing $FF to $D800 reads back as $0F (low nibble
    /// preserved, high nibble cleared).
    /// </summary>
    [Fact]
    public void ColorRam_WritesAreMaskedToLowNibble()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        machine.Bus.Write(0xD800, 0xFF);

        Assert.Equal(0x0F, machine.Bus.Read(0xD800));
    }

    /// <summary>
    /// FR: FR-CFG-007, FR: FR-MEM-001, TR: TR-CYCLE-001.
    /// Use case: The C64's RAM must initialise with the VICE-documented
    /// power-on pattern (alternating $00/$FF 64-byte stripes) so boot
    /// behaviour matches native VICE deterministically.
    /// Acceptance: $0400/$0401 read $00, $0402/$0403 read $FF, $C000
    /// reads $FF, and the reset vector bytes at $FFFC/$FFFD are $00 in
    /// the raw RAM span.
    /// </summary>
    [Fact]
    public void C64Ram_UsesVicePowerOnPattern()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var ram = Assert.IsAssignableFrom<IMemory>(machine.Devices.GetByRole(DeviceRole.SystemRam));

        Assert.Equal(0x00, machine.Bus.Read(0x0400));
        Assert.Equal(0x00, machine.Bus.Read(0x0401));
        Assert.Equal(0xFF, machine.Bus.Read(0x0402));
        Assert.Equal(0xFF, machine.Bus.Read(0x0403));
        Assert.Equal(0xFF, machine.Bus.Read(0xC000));
        Assert.Equal(0x00, ram.Span[0xFFFC]);
        Assert.Equal(0x00, ram.Span[0xFFFD]);
    }

    /// <summary>
    /// FR: FR-CIA-006, FR: FR-DRV-005, TR: TR-CYCLE-001.
    /// Use case: On a standard C64, CIA2 port A's high bits reflect the
    /// idle IEC serial bus state (DATA/CLOCK low at rest); the data line
    /// (bit 7) is asserted low while bit 6 floats high.
    /// Acceptance: After driving the lower CIA2 port A bits to $07 with
    /// DDR=$3F, reading $DD00 returns $47 (bit 6 high from the floating
    /// SRQ line, bit 7 low from the IEC DATA line).
    /// </summary>
    [Fact]
    public void Cia2PortA_ReadsIdleSerialInputWithBit7Low()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        machine.Bus.Write(0xDD00, 0x07);
        machine.Bus.Write(0xDD02, 0x3F);

        Assert.Equal(0x47, machine.Bus.Read(0xDD00));
    }

    /// <summary>
    /// FR: FR-PRF-002, FR: FR-CIA-006, TR: TR-CYCLE-001.
    /// Use case: On C64GS the IEC bus is not connected; with the same
    /// CIA2 port A driving as a standard C64, bits 6 and 7 must read
    /// low (no SRQ, no DATA pull-up) rather than the idle pattern.
    /// Acceptance: After the same $07/$3F driving sequence, $DD00 reads
    /// $07 instead of $47 because both high bits are pulled low.
    /// </summary>
    [Fact]
    public void C64GsCia2PortA_ReadsDisconnectedSerialInputWithBits6And7Low()
    {
        var machine = MachineTestFactory.CreateC64Machine("c64gs");

        machine.Bus.Write(0xDD00, 0x07);
        machine.Bus.Write(0xDD02, 0x3F);

        Assert.Equal(0x07, machine.Bus.Read(0xDD00));
    }

    /// <summary>
    /// FR: FR-CPU-002, TR: TR-CYCLE-001.
    /// Use case: <c>StepInstruction</c> must advance the machine clock
    /// past a single master cycle (a real instruction takes multiple
    /// master cycles to complete).
    /// Acceptance: After <c>StepInstruction</c>, the cycle delta is
    /// strictly greater than 1 and PC has moved past the previous value.
    /// </summary>
    [Fact]
    public void StepInstruction_AdvancesPastSingleMasterCycle()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var before = machine.GetState();

        machine.StepInstruction();

        var after = machine.GetState();
        Assert.True(after.Cycle - before.Cycle > 1, $"Expected >1 cycle, got {after.Cycle - before.Cycle}");
        Assert.NotEqual(before.PC, after.PC);
    }

    private sealed class CorruptLengthAndHashRomProvider : IRomProvider
    {
        private readonly Dictionary<string, byte[]> _romData = new()
        {
            ["basic"] = CreateBytes(8192),
            [C64ViceRomNames.Basic] = CreateBytes(8192),
            ["kernal"] = CreateBytes(8192),
            [C64ViceRomNames.KernalRev3] = CreateBytes(8192),
            ["characters"] = CreateBytes(4096),
            [C64ViceRomNames.Character] = CreateBytes(4096),
        };

        private static byte[] CreateBytes(int count) => Encoding.UTF8.GetBytes(new string('A', count));

        public ReadOnlyMemory<byte> LoadRom(string romName, string architecture)
            => _romData[romName];

        public bool IsAvailable(string romName, string architecture) => true;
    }
}
