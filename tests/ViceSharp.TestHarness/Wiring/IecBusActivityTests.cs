namespace ViceSharp.TestHarness.Wiring;

using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// FR: FR-DRV-005, FR: FR-HOST-006; TR: TR-HOST-STATUS-001; TEST-DRV-001.
/// IEC activity tests prove the UI/host signal is derived from the
/// inter-system bus LineChanged source and that D64 directory/PRG reads
/// are observable on that bus.
/// </summary>
public sealed class IecBusActivityTests
{
    [Fact]
    public void Monitor_ReportsActiveFromLineChanges_ThenReturnsIdle()
    {
        var time = new ManualTimeProvider();
        var bus = IecInterSystemBus.Create();
        var monitor = new IecBusActivityMonitor(bus, TimeSpan.FromSeconds(1), time);
        var host = bus.AttachEndpoint("host");

        host.Pull(IecInterSystemBus.Atn, low: true);

        Assert.True(monitor.IsActive);
        Assert.Equal(1, monitor.TransitionCount);

        host.Pull(IecInterSystemBus.Atn, low: false);
        Assert.True(monitor.IsActive);
        Assert.Equal(2, monitor.TransitionCount);

        time.Advance(TimeSpan.FromSeconds(2));

        Assert.False(monitor.IsActive);
        Assert.Equal("Idle", monitor.ActivityState);
    }

    [Fact]
    public void IecDrive_ReadFirstProgram_UsesIecBusActivityAndReturnsD64Program()
    {
        var time = new ManualTimeProvider();
        var bus = IecInterSystemBus.Create();
        var monitor = new IecBusActivityMonitor(bus, TimeSpan.FromSeconds(1), time);
        var drive = new IecDrive(8);
        drive.ConnectIecBus(bus);
        drive.InsertDisk(CreateD64WithFirstProgram("BOOT", CreateBasicProgramPrg()));

        var success = drive.TryReadFirstProgram(out var program, out var error);

        Assert.True(success, error);
        Assert.NotNull(program);
        Assert.Equal("BOOT", program!.FileName);
        Assert.Equal(0x0801, program.LoadAddress);
        Assert.True(monitor.TransitionCount >= 2);
        Assert.True(monitor.IsActive);

        time.Advance(TimeSpan.FromSeconds(2));

        Assert.False(monitor.IsActive);
    }

    private static byte[] CreateD64WithFirstProgram(string fileName, byte[] programBytes)
    {
        var image = new D64Image();
        image.Format();

        const int directoryEntryOffset = 2;
        image.WriteSectorByte(18, 1, directoryEntryOffset, 0x82);
        image.WriteSectorByte(18, 1, directoryEntryOffset + 1, 17);
        image.WriteSectorByte(18, 1, directoryEntryOffset + 2, 0);

        for (var index = 0; index < 16; index++)
        {
            var value = index < fileName.Length
                ? (byte)char.ToUpperInvariant(fileName[index])
                : (byte)0xA0;
            image.WriteSectorByte(18, 1, directoryEntryOffset + 3 + index, value);
        }

        image.WriteSectorByte(17, 0, 0, 0);
        image.WriteSectorByte(17, 0, 1, (byte)(programBytes.Length + 1));
        for (var offset = 0; offset < programBytes.Length; offset++)
            image.WriteSectorByte(17, 0, 2 + offset, programBytes[offset]);

        return image.ToArray();
    }

    private static byte[] CreateBasicProgramPrg()
        =>
        [
            0x01, 0x08,
            0x0B, 0x08,
            0x0A, 0x00,
            0x99,
            0x22, (byte)'O', (byte)'K', 0x22,
            0x00,
            0x00, 0x00
        ];

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 6, 8, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan elapsed) => _utcNow += elapsed;
    }
}
