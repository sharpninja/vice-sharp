namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using ViceSharp.Core.Vic20;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// Board wiring and open-bus rules from VICE (vic20via1/2, colorram_read, idle IEC).
/// Drives shipped ArchitectureBuilder machines, not mocks of the units under test.
/// </summary>
public sealed class Vic20BoardWiringTests
{
    [Fact]
    public void DualVia_BasesAndInterruptRoles_MatchViceBoard()
    {
        var machine = MachineTestFactory.CreateVic20Machine("vic20");
        var vias = machine.Devices.GetAll<Via6522>().OrderBy(v => v.BaseAddress).ToArray();
        Assert.Equal(2, vias.Length);
        Assert.Equal(0x9110, vias[0].BaseAddress);
        Assert.Equal(0x9120, vias[1].BaseAddress);

        // VIA1 NMI, VIA2 IRQ: roles on registry
        Assert.Same(vias[0], machine.Devices.GetByRole(DeviceRole.Via1));
        Assert.Same(vias[1], machine.Devices.GetByRole(DeviceRole.Via2));
        Assert.Equal("VIA1", vias[0].Name);
        Assert.Equal("VIA2", vias[1].Name);
    }

    [Fact]
    public void Via1_IdlePortA_IsViceIec7E()
    {
        var machine = MachineTestFactory.CreateVic20Machine("vic20");
        machine.Reset();
        Assert.Equal(Vic20IecPort.IdlePortAInput, machine.Bus.Peek(0x911F));
        Assert.Equal(0x7E, Vic20IecPort.IdlePortAInput);
    }

    [Fact]
    public void ColorRam_ReadCombinesNibbleWithVBusHigh_AsViceColorramRead()
    {
        var machine = MachineTestFactory.CreateVic20Machine("vic20");
        var bus = Assert.IsType<BasicBus>(machine.Bus);
        // VICE colorram_store: low nibble only
        bus.Write(0x9600, 0xAB); // store 0x0B
        // Set V-bus last data high nibble (VICE vic20_v_bus_last_data)
        bus.NoteVBusData(0x90);
        var peek = bus.Peek(0x9600);
        // colorram_peek: nibble | (v_bus & 0xf0) without updating in peek path...
        // Our Peek does not update; Read does. Either way high should be from VBus.
        Assert.Equal(0x0B, peek & 0x0F);
        Assert.Equal(0x90, peek & 0xF0);
    }

    [Fact]
    public void SystemRam_UninstalledBlk_DoesNotClaimBus()
    {
        var machine = MachineTestFactory.CreateVic20Machine(
            new Architectures.Vic20.Vic20Descriptor("vic20").WithRamBlocks(Vic20RamBlocks.None));
        var ram = machine.Devices.GetAll<Vic20SystemRam>().Single();
        Assert.False(ram.HandlesAddress(0x2000));
        Assert.False(ram.HandlesAddress(0xA000));
        Assert.True(ram.HandlesAddress(0x1000)); // base main always
    }

    [Fact]
    public async Task ConsoleHost_TryGetVicIPixelAspect_UsesViceVicIFormulas()
    {
        await using var host = ConsoleHostComposition.BuildDefault();
        var result = host.StartC64Session(new ConsoleSessionOptions("vic20"));
        Assert.True(result.Success, result.Error);
        Assert.True(host.TryGetVicIPixelAspect(result.SessionId, out var aspect));
        Assert.Equal(Mos6561.GetPixelAspectRatio(ntsc: false), aspect, 5);

        // C64 must not report VIC-I aspect
        await using var host64 = ConsoleHostComposition.BuildDefault();
        var c64 = host64.StartC64Session(new ConsoleSessionOptions("c64"));
        if (c64.Success)
            Assert.False(host64.TryGetVicIPixelAspect(c64.SessionId, out _));
    }
}
