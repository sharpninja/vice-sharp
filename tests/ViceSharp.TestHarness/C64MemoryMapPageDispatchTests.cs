namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR: PERF-MEM-001. Correctness regression suite for C64MemoryMap read dispatch.
///
/// Use case: C64MemoryMap.Read() dispatches each address to the correct source
///   (RAM, BASIC ROM, KERNAL ROM, I/O, character ROM) based on PLA state. When
///   the CPU writes to $0001 (processor port), the PLA state changes and subsequent
///   reads must reflect the new mapping. These tests pin the expected read values
///   so that a PERF-MEM-001 page table optimization cannot silently regress
///   correctness (e.g. stale table after PLA update).
///
/// Acceptance: Each test reads from a mapped range, changes PLA state, reads again,
///   and asserts the returned value matches the new mapping.
/// </summary>
public sealed class C64MemoryMapPageDispatchTests
{
    private const ushort ProcessorPort = 0x0001;

    // PLA control register bit patterns ($01 write - bits 0-2 are LORAM/HIRAM/CHAREN):
    //   bit 0 = LORAM, bit 1 = HIRAM, bit 2 = CHAREN
    // IsIoVisible     = CHAREN && (LORAM || HIRAM)
    // IsCpuCharRomVisible = !CHAREN && (LORAM || HIRAM)
    // BasicRomVisible = LORAM && HIRAM
    // KernalRomVisible = HIRAM
    private const byte PlaLoramHiramCharen = 0x37;  // bits 0,1,2 = 1,1,1: BASIC+KERNAL+IO
    private const byte PlaLoramHiramNoChar = 0x33;  // bits 0,1,2 = 1,1,0: BASIC+KERNAL+CHARROM
    private const byte PlaRamOnly = 0x34;           // bits 0,1,2 = 0,0,1: no LORAM/HIRAM -> RAM everywhere

    private static IMachine CreateC64()
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        return new ArchitectureBuilder(provider).Build(new C64Descriptor());
    }

    /// <summary>
    /// FR: PERF-MEM-001, TR: TR-MEM-PAGE-001.
    /// Use case: With default PLA ($37), BASIC ROM visible at $A000-$BFFF.
    ///   After changing PLA to all-RAM ($34), $A000 returns RAM content.
    /// Acceptance: Read($A000) changes from BASIC ROM byte to RAM byte after
    ///   CPU writes $34 to $0001, proving the dispatch table rebuilds on PLA change.
    /// </summary>
    [Fact]
    public void Read_BasicRomVisibility_ChangesWhenPlaUpdated()
    {
        var machine = CreateC64();

        // Default PLA: LORAM+HIRAM+CHAREN. BASIC ROM visible at $A000.
        machine.Bus.Write(ProcessorPort, PlaLoramHiramCharen);
        var basicByte = machine.Bus.Read(0xA000);

        // Known BASIC ROM start: $94, $E3 (CBM BASIC V2 "CBM").
        // The exact byte doesn't matter - just confirm it came from ROM, not RAM.
        // Stamp a distinct value into RAM at $A000 so we can detect the switch.
        // (RAM is write-always; ROM overlay hides it on read when visible.)
        machine.Bus.Write(0xA000, 0xAA);

        // With BASIC ROM visible, read should still return ROM byte (not 0xAA RAM write).
        var stillRomByte = machine.Bus.Read(0xA000);
        Assert.Equal(basicByte, stillRomByte);

        // Disable BASIC ROM: set PLA to all-RAM ($34 = HIRAM+CHAREN, no LORAM).
        machine.Bus.Write(ProcessorPort, PlaRamOnly);

        // Now RAM write 0xAA at $A000 should be visible.
        var ramByte = machine.Bus.Read(0xA000);
        Assert.Equal(0xAA, ramByte);
    }

    /// <summary>
    /// FR: PERF-MEM-001, TR: TR-MEM-PAGE-002.
    /// Use case: With default PLA ($37), KERNAL ROM visible at $E000-$FFFF.
    ///   After disabling HIRAM, $E000 returns RAM content.
    /// Acceptance: Read($E000) returns RAM byte after PLA clears HIRAM bit.
    /// </summary>
    [Fact]
    public void Read_KernalRomVisibility_ChangesWhenPlaUpdated()
    {
        var machine = CreateC64();

        // Stamp RAM at $E000 before ROM overlay.
        machine.Bus.Write(ProcessorPort, PlaLoramHiramCharen);
        var kernalByte = machine.Bus.Read(0xE000);

        // Write a sentinel to RAM at $E000.
        machine.Bus.Write(0xE000, 0xBB);

        // KERNAL ROM still visible; read returns ROM byte.
        var stillRom = machine.Bus.Read(0xE000);
        Assert.Equal(kernalByte, stillRom);

        // Disable HIRAM (bit 1). KERNAL ROM becomes invisible.
        machine.Bus.Write(ProcessorPort, PlaRamOnly);

        // RAM write 0xBB now visible.
        var ramByte = machine.Bus.Read(0xE000);
        Assert.Equal(0xBB, ramByte);
    }

    /// <summary>
    /// FR: PERF-MEM-001, TR: TR-MEM-PAGE-003.
    /// Use case: With default PLA ($37 -> CHAREN+LORAM|HIRAM), I/O visible at $D000-$DFFF.
    ///   After clearing CHAREN ($36 = LORAM+HIRAM no CHAREN), character ROM appears at $D000.
    /// Acceptance: Read($D000) returns KERNAL-region byte when IO visible, and a
    ///   character ROM byte when IO disabled (CHAREN=0 + LORAM|HIRAM).
    /// </summary>
    [Fact]
    public void Read_IoVsCharRom_ChangesWhenCharenToggled()
    {
        var machine = CreateC64();

        // IO visible ($37): $D000 is VIC register space. VIC $D000 = sprite 0 X.
        machine.Bus.Write(ProcessorPort, PlaLoramHiramCharen);
        var ioRead = machine.Bus.Read(0xD000);

        // Switch to CharROM ($36 = LORAM+HIRAM, CHAREN=0).
        machine.Bus.Write(ProcessorPort, PlaLoramHiramNoChar);
        var charRead = machine.Bus.Read(0xD000);

        // $D000 in I/O mode -> VIC register (probably 0).
        // $D000 in CharROM mode -> character ROM byte (standard charset starts with $3C).
        // We just confirm the two reads differ (different data source).
        Assert.NotEqual(ioRead, charRead);
    }

    /// <summary>
    /// FR: PERF-MEM-001, TR: TR-MEM-PAGE-004.
    /// Use case: RAM at $0002 is always RAM regardless of PLA state.
    /// Acceptance: Write to $0002 is always readable back without ROM overlay.
    /// </summary>
    [Fact]
    public void Read_RamPage_AlwaysReturnsRam()
    {
        var machine = CreateC64();

        machine.Bus.Write(ProcessorPort, PlaLoramHiramCharen);
        machine.Bus.Write(0x0002, 0x42);
        Assert.Equal(0x42, machine.Bus.Read(0x0002));

        machine.Bus.Write(ProcessorPort, PlaRamOnly);
        machine.Bus.Write(0x0002, 0x55);
        Assert.Equal(0x55, machine.Bus.Read(0x0002));
    }
}
