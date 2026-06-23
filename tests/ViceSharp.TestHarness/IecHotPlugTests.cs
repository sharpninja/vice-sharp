namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// TEST-IECHOTPLUG-001 / FR-IECHOTPLUG-001. A drive can be added to, removed from, or renumbered
/// on a running rig with no restart, because devices couple only through the async wired-OR bus:
/// attaching an endpoint makes it a live wired-OR participant, detaching it drops its pull
/// contributions and recomputes the lines, and a drive's device number is the VIA1 PortB jumper
/// bits the DOS reads - settable at runtime.
/// </summary>
public sealed class IecHotPlugTests
{
    /// <summary>
    /// FR: FR-IECHOTPLUG-001, TR: TR-DRVLIFE-001, TEST-IECHOTPLUG-001 (AC3).
    /// Use case: the user changes a live drive from device 8 to 9 in the UI; the drive must then
    ///   answer device 9 without rebuilding it - i.e. the bits the DOS reads from VIA1 PortB
    ///   (bits 5-6) must change to encode the new number.
    /// Acceptance: setting DeviceNumber at runtime changes the PortB address bits the drive
    ///   presents (8->0x00, 9->0x20, 10->0x40, 11->0x60); out-of-range throws.
    /// </summary>
    [Fact]
    public void DeviceNumber_SetAtRuntime_ChangesAddressBitsTheDosReads()
    {
        var bus = new BasicBus();
        var irq = new InterruptLine(InterruptType.Irq);
        var via1 = new Via6522(bus, irq) { BaseAddress = 0x1800, Size = 0x0400 };
        via1.Reset();

        var iec = IecInterSystemBus.Create();
        var endpoint = iec.AttachEndpoint("drive");
        var dev = new C1541IecInterfaceDevice(8);
        dev.ConnectVia1(via1, endpoint, iec);

        Assert.Equal(0x00, via1.PortBInput!() & 0x60); // device 8

        dev.DeviceNumber = 9;
        Assert.Equal(0x20, via1.PortBInput!() & 0x60); // device 9

        dev.DeviceNumber = 11;
        Assert.Equal(0x60, via1.PortBInput!() & 0x60); // device 11
        Assert.Equal(11, dev.DeviceNumber);

        Assert.Throws<ArgumentOutOfRangeException>(() => dev.DeviceNumber = 7);
        Assert.Throws<ArgumentOutOfRangeException>(() => dev.DeviceNumber = 12);
    }

    /// <summary>
    /// FR: FR-IECHOTPLUG-001, TR: TR-DRVLIFE-001, TEST-IECHOTPLUG-001 (AC1).
    /// Use case: a second drive is plugged into a running bus that already has traffic; it must
    ///   immediately participate in the wired-OR resolution (answer on the bus) with no restart.
    /// Acceptance: an endpoint attached after traffic is live - it observes the current line
    ///   state and its own pull pulls the shared line low for every endpoint.
    /// </summary>
    [Fact]
    public void AttachEndpoint_MidTraffic_ParticipatesInWiredOrImmediately()
    {
        var iec = IecInterSystemBus.Create();
        var c64 = iec.AttachEndpoint("c64");
        c64.Pull(IecInterSystemBus.Atn, low: true); // existing traffic before the new drive arrives

        var drive9 = iec.AttachEndpoint("drive-9"); // hot attach mid-traffic

        Assert.False(drive9.ReadLine(IecInterSystemBus.Atn)); // sees the in-flight ATN at once
        Assert.True(drive9.ReadLine(IecInterSystemBus.Data));  // and the idle DATA line

        drive9.Pull(IecInterSystemBus.Data, low: true);        // the new drive answers
        Assert.False(c64.ReadLine(IecInterSystemBus.Data));    // every endpoint sees it
    }

    /// <summary>
    /// FR: FR-IECHOTPLUG-001, TR: TR-DRVLIFE-001, TEST-IECHOTPLUG-001 (AC2).
    /// Use case: a drive is unplugged while it is holding a line low; its contribution must be
    ///   removed and the bus must recompute so the line returns high if nobody else holds it.
    /// Acceptance: detaching an endpoint that is pulling a line low recomputes that line high and
    ///   raises a LineChanged edge; a line still held by another endpoint stays low.
    /// </summary>
    [Fact]
    public void DetachEndpoint_RemovesPullsAndRecomputes()
    {
        var iec = IecInterSystemBus.Create();
        var c64 = iec.AttachEndpoint("c64");
        var drive = iec.AttachEndpoint("drive-8");

        c64.Pull(IecInterSystemBus.Clk, low: true);   // host holds CLK
        drive.Pull(IecInterSystemBus.Data, low: true); // drive holds DATA
        Assert.False(iec.ReadLine(IecInterSystemBus.Data));

        var releasedHigh = false;
        iec.LineChanged += (_, e) =>
        {
            if (e.Signal == IecInterSystemBus.Data && e.NewState)
                releasedHigh = true;
        };

        iec.DetachEndpoint(drive); // unplug the drive while it pulls DATA

        Assert.True(iec.ReadLine(IecInterSystemBus.Data)); // DATA recomputed high (drive gone)
        Assert.True(releasedHigh);                          // edge fired for the release
        Assert.False(iec.ReadLine(IecInterSystemBus.Clk));  // CLK still held by the host
    }

    /// <summary>
    /// FR: FR-IECHOTPLUG-001, TR: TR-DRVLIFE-001, TEST-IECHOTPLUG-001 (AC1, AC2).
    /// Use case: the coordinator owns the live system list; adding/removing a peripheral system
    ///   mid-run must update that roster without disturbing the host.
    /// Acceptance: AttachSystem then DetachSystem on a running coordinator adds and removes the
    ///   peripheral from Systems, leaving the host attached.
    /// </summary>
    [Fact]
    public void Coordinator_AttachThenDetachSystem_UpdatesRosterMidRun()
    {
        var host = new HotPlugFakeMachine("c64", 985248);
        var coord = new SystemCoordinator();
        coord.AttachSystem(host);

        var drive = new HotPlugFakeMachine("drive-9", 1000000);
        coord.AttachSystem(drive);
        Assert.Equal(2, coord.Systems.Count);
        Assert.Contains(drive, coord.Systems);

        coord.DetachSystem(drive);
        Assert.Single(coord.Systems);
        Assert.Same(host, coord.Systems[0]);
    }

    private sealed class HotPlugFakeMachine : IMachine
    {
        private readonly IClock _clock;

        public HotPlugFakeMachine(string name, long clockHz)
        {
            Name = name;
            _clock = new SystemClock(clockHz);
        }

        public string Name { get; }
        public IBus Bus => null!;
        public IClock Clock => _clock;
        public IDeviceRegistry Devices => null!;
        public IArchitectureDescriptor Architecture => null!;
        public void RunFrame() { }
        public void StepInstruction() { }
        public MachineState GetState() => default;
        public void Reset() { }
    }
}
