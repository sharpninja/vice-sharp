namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
/// Use case: a dropped PRG must land at its load address in the current
/// session RAM, and BASIC-start programs must update BASIC pointers so RUN
/// sees the loaded program.
/// </summary>
public sealed class PrgMemoryLoaderTests
{
    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: a machine-language PRG loads at its own address without
    /// changing BASIC pointers or requesting RUN.
    /// Acceptance: bytes are written at $C000, TXTTAB/VARTAB stay as seeded,
    /// and Ran is false.
    /// </summary>
    [Fact]
    public void Load_MachineLanguagePrg_WritesPayloadAndDoesNotRun()
    {
        var memory = new byte[0x10000];
        memory[0x002B] = 0x01;
        memory[0x002C] = 0x08;
        memory[0x002D] = 0x03;
        memory[0x002E] = 0x08;
        var bus = new MemoryBus(memory);
        byte[] prg = [0x00, 0xC0, 0xA9, 0x01, 0x60];

        var result = PrgMemoryLoader.Load(bus, prg);

        Assert.True(result.Success);
        Assert.Equal(0xC000, result.LoadAddress);
        Assert.Equal(3, result.ByteCount);
        Assert.False(result.Ran);
        Assert.Equal(0xA9, memory[0xC000]);
        Assert.Equal(0x01, memory[0xC001]);
        Assert.Equal(0x60, memory[0xC002]);
        Assert.Equal(0x0801, ReadWord(memory, 0x002B));
        Assert.Equal(0x0803, ReadWord(memory, 0x002D));
    }

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: a BASIC PRG that starts at TXTTAB must be visible to RUN.
    /// Acceptance: payload is written at $0801, VARTAB/ARYTAB/STREND equal
    /// load plus payload length, and Ran is true.
    /// </summary>
    [Fact]
    public void Load_BasicStartPrg_UpdatesPointersAndRequestsRun()
    {
        var memory = new byte[0x10000];
        memory[0x002B] = 0x01;
        memory[0x002C] = 0x08;
        var bus = new MemoryBus(memory);
        byte[] prg = [0x01, 0x08, 0x0B, 0x08, 0x0A, 0x00, 0x99, 0x22, 0x48, 0x49, 0x22, 0x00, 0x00, 0x00];

        var result = PrgMemoryLoader.Load(bus, prg);

        Assert.True(result.Success);
        Assert.Equal(0x0801, result.LoadAddress);
        Assert.Equal(12, result.ByteCount);
        Assert.True(result.Ran);
        Assert.Equal(0x0B, memory[0x0801]);
        Assert.Equal(0x080D, ReadWord(memory, 0x002D));
        Assert.Equal(0x080D, ReadWord(memory, 0x002F));
        Assert.Equal(0x080D, ReadWord(memory, 0x0031));
        Assert.Equal(0x0801, ReadWord(memory, 0x002B));
    }

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: a truncated PRG must not poke RAM.
    /// Acceptance: fewer than 3 bytes returns Success false and leaves $C000
    /// as the seeded sentinel.
    /// </summary>
    [Fact]
    public void Load_TruncatedPrg_RejectsWithoutWriting()
    {
        var memory = new byte[0x10000];
        memory[0xC000] = 0xAA;
        var bus = new MemoryBus(memory);

        var result = PrgMemoryLoader.Load(bus, [0x00, 0xC0]);

        Assert.False(result.Success);
        Assert.False(result.Ran);
        Assert.Equal(0xAA, memory[0xC000]);
        Assert.Contains("load address", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: BASIC start differs by machine and VIC-20 RAM config. The loader
    /// must follow live TXTTAB, not a C64 $0801 constant.
    /// Acceptance: a PRG whose load address equals the seeded TXTTAB reports Ran
    /// true and sets VARTAB to load plus payload length for C64 $0801, VIC-20
    /// unexpanded $1001, VIC-20 +3K $0401, and VIC-20 +8K $1201.
    /// </summary>
    [Theory]
    [InlineData(0x0801)]
    [InlineData(0x1001)]
    [InlineData(0x0401)]
    [InlineData(0x1201)]
    public void Load_BasicStartFollowsLiveTxtTab_ForEachMachineConfig(ushort basicStart)
    {
        var memory = new byte[0x10000];
        memory[0x002B] = (byte)basicStart;
        memory[0x002C] = (byte)(basicStart >> 8);
        var bus = new MemoryBus(memory);
        byte[] prg = [(byte)basicStart, (byte)(basicStart >> 8), 0xEA];

        var result = PrgMemoryLoader.Load(bus, prg);

        Assert.True(result.Success);
        Assert.True(result.Ran);
        Assert.Equal(basicStart, result.LoadAddress);
        Assert.Equal(0xEA, memory[basicStart]);
        Assert.Equal((ushort)(basicStart + 1), ReadWord(memory, 0x002D));
    }

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: a C64 BASIC PRG dropped on a VIC-20 unexpanded session must
    /// not RUN just because $0801 is "the usual" BASIC start.
    /// Acceptance: TXTTAB $1001 and load $0801 writes RAM and leaves Ran false.
    /// </summary>
    [Fact]
    public void Load_C64BasicPrgWhenVic20UnexpandedTxtTab_DoesNotRun()
    {
        var memory = new byte[0x10000];
        memory[0x002B] = 0x01;
        memory[0x002C] = 0x10;
        var bus = new MemoryBus(memory);
        byte[] prg = [0x01, 0x08, 0xEA];

        var result = PrgMemoryLoader.Load(bus, prg);

        Assert.True(result.Success);
        Assert.False(result.Ran);
        Assert.Equal(0xEA, memory[0x0801]);
        Assert.Equal(0, ReadWord(memory, 0x002D));
    }

    /// <summary>
    /// FR: FR-UIDROP-002, TR: TR-HOST-PRG-001, TEST-UIDROP-002.
    /// Use case: TXTTAB of 0 is not a BASIC start, even if the PRG loads at 0.
    /// Acceptance: Ran is false so an unbooted machine does not inject RUN.
    /// </summary>
    [Fact]
    public void Load_ZeroTxtTab_DoesNotTreatAsBasicStart()
    {
        var memory = new byte[0x10000];
        var bus = new MemoryBus(memory);
        byte[] prg = [0x00, 0x00, 0xEA];

        var result = PrgMemoryLoader.Load(bus, prg);

        Assert.True(result.Success);
        Assert.Equal(0x0000, result.LoadAddress);
        Assert.False(result.Ran);
        Assert.Equal(0xEA, memory[0x0000]);
        Assert.Equal(0, ReadWord(memory, 0x002D));
    }

    private static ushort ReadWord(byte[] memory, int address)
        => (ushort)(memory[address] | (memory[address + 1] << 8));

    private sealed class MemoryBus(byte[] memory) : IBus
    {
        public byte Read(ushort address) => memory[address];

        public void Write(ushort address, byte value) => memory[address] = value;

        public byte Peek(ushort address) => memory[address];

        public void RegisterDevice(IAddressSpace device)
        {
        }

        public void UnregisterDevice(IAddressSpace device)
        {
        }
    }
}
