namespace ViceSharp.TestHarness;

using Xunit;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Pla;

/// <summary>
/// FR/TR: FR-CPU (BACKFILL-CPU 6510 processor port).
/// Use case: The 6510 reserves zero-page $00 (DDR) and $01 (data) for its
/// internal 6-bit I/O port. Board mapping decides which absolute addresses
/// expose the port. Writes to the data latch must drive PLA banking bits
/// (LORAM/HIRAM/CHAREN, bits 0-2). Writes to the DDR must update the
/// data direction register so subsequent reads of $01 apply the DDR mask:
/// output bits return what was written, input bits return the external
/// pull-up default.
/// </summary>
public sealed class ProcessorPortTests
{
    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 processor port).
    /// Use case: After writing the DDR via the processor-port API, reading
    /// it back returns the same value masked to the low six bits (bits 6-7
    /// of the DDR are not implemented in the 6510 port).
    /// Acceptance: WriteDataDirection(0x2F) then DataDirection == 0x2F.
    /// </summary>
    [Fact]
    public void WriteDataDirection_RoundTrips_LowSixBits()
    {
        var bus = new MockProcessorPortBus();
        var pla = new Mos906114(bus);
        pla.Reset();

        pla.WriteDataDirection(0x2F);

        Assert.Equal(0x2F, pla.DataDirection);
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 processor port).
    /// Use case: The shared chip model resets to a neutral state. Board
    /// integration supplies any machine-specific processor-port power-up
    /// values.
    /// Acceptance: After Reset() the DataDirection and DataRegister are zero.
    /// </summary>
    [Fact]
    public void Reset_InitializesPortToNeutralChipState()
    {
        var bus = new MockProcessorPortBus();
        var pla = new Mos906114(bus);

        pla.Reset();

        Assert.Equal(0x00, pla.DataDirection);
        Assert.Equal(0x00, pla.DataRegister);
        Assert.False(pla.Loram);
        Assert.False(pla.Hiram);
        Assert.False(pla.Charen);
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 processor port), ARCH-CHIPGLUE-001.
    /// Use case: C64 board reset owns the documented processor-port power-up
    /// state rather than the shared PLA core.
    /// Acceptance: A reset C64 machine exposes DDR=$2F and data=$37 through
    /// its PLA device, enabling LORAM, HIRAM, and CHAREN.
    /// </summary>
    [Fact]
    public void C64MachineReset_AppliesPowerUpProcessorPortState()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var pla = (Mos906114)machine.Devices.GetByRole(DeviceRole.Pla)!;

        Assert.Equal(0x2F, pla.DataDirection);
        Assert.Equal(0x37, pla.DataRegister);
        Assert.True(pla.Loram);
        Assert.True(pla.Hiram);
        Assert.True(pla.Charen);
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 processor port).
    /// Use case: With the default DDR ($2F) all banking bits (0-2) are
    /// configured as outputs, so writing $07 sets LORAM+HIRAM+CHAREN and
    /// the PLA exposes BASIC + KERNAL + I/O all visible.
    /// Acceptance: After WriteDataPort(0x07) the Loram, Hiram and Charen
    /// flags are all true and ReadProcessorPort reflects bits 0-2 set.
    /// </summary>
    [Fact]
    public void WriteDataPort_BankingBits_ForwardedToPla()
    {
        var bus = new MockProcessorPortBus();
        var pla = new Mos906114(bus);
        pla.Reset();
        pla.WriteDataDirection(0x2F);

        pla.WriteDataPort(0x07);

        Assert.True(pla.Loram);
        Assert.True(pla.Hiram);
        Assert.True(pla.Charen);
        Assert.Equal(0x07, (byte)(pla.ReadProcessorPort() & 0x07));
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 processor port).
    /// Use case: Clearing HIRAM (bit 1) via the processor-port write must
    /// toggle the PLA's KernalRomVisible flag so KERNAL banks out.
    /// Acceptance: WriteDataPort($05) clears bit 1 - Hiram false,
    /// KernalRomVisible false; setting it back to $07 re-asserts both.
    /// </summary>
    [Fact]
    public void WriteDataPort_ClearingHiram_BanksOutKernal()
    {
        var bus = new MockProcessorPortBus();
        var pla = new Mos906114(bus);
        pla.Reset();

        pla.WriteDataPort(0x05);

        Assert.False(pla.Hiram);
        Assert.False(pla.KernalRomVisible);

        pla.WriteDataPort(0x07);

        Assert.True(pla.Hiram);
        Assert.True(pla.KernalRomVisible);
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 processor port).
    /// Use case: When the DDR is reprogrammed to all-input ($00), the
    /// processor-port read returns only the external pull-up state for
    /// every bit because no bit drives the bus. In the simple model
    /// (no NMOS cap quirk) input bits read as zero.
    /// Acceptance: WriteDataDirection($00) followed by WriteDataPort($3F)
    /// produces ReadProcessorPort() == 0x00 - the data bits are masked
    /// out because every bit is an input.
    /// </summary>
    [Fact]
    public void ReadProcessorPort_AllInput_MasksOutDataBits()
    {
        var bus = new MockProcessorPortBus();
        var pla = new Mos906114(bus);
        pla.Reset();

        pla.WriteDataDirection(0x00);
        pla.WriteDataPort(0x3F);

        Assert.Equal(0x00, pla.ReadProcessorPort());
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 processor port).
    /// Use case: Input pins are controlled by external pull-up state. The
    /// data latch cannot drive a bit whose DDR bit is zero.
    /// Acceptance: With bit 4 configured as an input, writing the data latch
    /// with bit 4 set and clear yields the same bit 4 readback.
    /// </summary>
    [Fact]
    public void ReadProcessorPort_InputBit_IsReadOnly()
    {
        var bus = new MockProcessorPortBus();
        var pla = new Mos906114(bus);
        pla.Reset();
        pla.WriteDataDirection(0x2F);

        pla.WriteDataPort(0x37 | 0x10);
        var withBit4Set = pla.ReadProcessorPort() & 0x10;

        pla.WriteDataPort(0x37 & ~0x10);
        var withBit4Clear = pla.ReadProcessorPort() & 0x10;

        Assert.Equal(withBit4Set, withBit4Clear);
    }

    /// <summary>
    /// FR/TR: FR-CPU (BACKFILL-CPU 6510 processor port).
    /// Use case: The bus dispatcher must surface the DDR through $0000.
    /// PLA.Read(0x0000) returns the data-direction byte so debugger /
    /// memory-map reads of $00 reflect the live DDR rather than RAM.
    /// Acceptance: After WriteDataDirection($2F), Read(0x0000) returns
    /// $2F; HandlesAddress reports true for $0000 as well as $0001.
    /// </summary>
    [Fact]
    public void Read_AtZeroPageZero_ReturnsDataDirection()
    {
        var bus = new MockProcessorPortBus();
        var pla = new Mos906114(bus);
        pla.Reset();

        pla.WriteDataDirection(0x2F);

        Assert.True(pla.HandlesAddress(0x0000));
        Assert.True(pla.HandlesAddress(0x0001));
        Assert.Equal(0x2F, pla.Read(0x0000));
    }

    /// <summary>
    /// Minimal stub IBus used to construct Mos906114 in unit tests
    /// without pulling in the full C64 wiring.
    /// </summary>
    private sealed class MockProcessorPortBus : IBus
    {
        public byte Read(ushort address) => 0;
        public void Write(ushort address, byte value) { }
        public byte Peek(ushort address) => 0;
        public void RegisterDevice(IAddressSpace device) { }
        public void UnregisterDevice(IAddressSpace device) { }
    }
}
