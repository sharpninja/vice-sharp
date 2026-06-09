namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// Direct unit tests for <see cref="BasicBus"/> and <see cref="SimpleRam"/>,
/// the core in-memory bus and 64KB RAM primitives that every system test in
/// the suite depends on. <see cref="BasicBus"/> is the dispatcher that maps
/// CPU reads / writes / peeks to the first registered
/// <see cref="IAddressSpace"/> handler that claims the address, and
/// <see cref="SimpleRam"/> is the catch-all backing store used by ad-hoc
/// machines, lockstep test fixtures, and CPU sanity tests. These tests
/// exercise the dispatch contract, Peek vs Read distinction, unmapped
/// address fallback, last-registered-wins priority, registration /
/// unregistration, and the RAM round-trip / fill / reset paths without
/// pulling in CPU, VIC, CIA, or SID dependencies.
/// </summary>
public sealed class BasicBusAndSimpleRamTests
{
    /// <summary>
    /// Minimal recording <see cref="IAddressSpace"/> that responds to a
    /// fixed inclusive [start, end] range and records every Read / Write /
    /// Peek for assertions.
    /// </summary>
    private sealed class RangeDevice : IAddressSpace
    {
        private readonly ushort _start;
        private readonly ushort _end;
        private readonly byte _readValue;
        private readonly byte _peekValue;

        public RangeDevice(ushort start, ushort end, byte readValue, byte? peekValue = null, uint id = 1)
        {
            _start = start;
            _end = end;
            _readValue = readValue;
            _peekValue = peekValue ?? readValue;
            Id = new DeviceId(id);
            Name = $"Range#{id}";
        }

        public DeviceId Id { get; }
        public string Name { get; }
        public int ReadCount { get; private set; }
        public int WriteCount { get; private set; }
        public int PeekCount { get; private set; }
        public ushort LastWriteAddress { get; private set; }
        public byte LastWriteValue { get; private set; }

        public bool HandlesAddress(ushort address) => address >= _start && address <= _end;

        public byte Read(ushort address)
        {
            ReadCount++;
            return _readValue;
        }

        public void Write(ushort address, byte value)
        {
            WriteCount++;
            LastWriteAddress = address;
            LastWriteValue = value;
        }

        public byte Peek(ushort address)
        {
            PeekCount++;
            return _peekValue;
        }

        public void Reset() { }
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: A bare <see cref="BasicBus"/> with no registered devices
    /// must still satisfy CPU reads without throwing - this is the
    /// "floating bus" condition on real hardware where an unmapped read
    /// produces 0xFF (open bus). The harness depends on this fallback
    /// when running tests that intentionally leave parts of the address
    /// space unmapped.
    /// Acceptance: <see cref="BasicBus.Read"/> and
    /// <see cref="BasicBus.Peek"/> both return 0xFF for any address
    /// when no devices are registered.
    /// </summary>
    [Fact]
    public void BasicBus_UnmappedAddress_ReturnsOpenBusDefault()
    {
        var bus = new BasicBus();

        Assert.Equal(0xFF, bus.Read(0x0000));
        Assert.Equal(0xFF, bus.Read(0x8000));
        Assert.Equal(0xFF, bus.Read(0xFFFF));
        Assert.Equal(0xFF, bus.Peek(0x4242));
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: Two address-space handlers occupy disjoint ranges - the
    /// canonical layout where ROM lives at $A000-$BFFF, RAM elsewhere,
    /// I/O at $D000-$DFFF. The bus must dispatch each read / write to
    /// the one device whose <see cref="IAddressSpace.HandlesAddress"/>
    /// claims the requested address, leaving the other untouched.
    /// Acceptance: Reads to range A return A's value and increment only
    /// A's read counter; reads to range B do the same for B; writes are
    /// routed identically.
    /// </summary>
    [Fact]
    public void BasicBus_DispatchesToHandlerWhoseRangeContainsAddress()
    {
        var bus = new BasicBus();
        var deviceA = new RangeDevice(0x0000, 0x7FFF, readValue: 0xAA, id: 1);
        var deviceB = new RangeDevice(0x8000, 0xFFFF, readValue: 0xBB, id: 2);
        bus.RegisterDevice(deviceA);
        bus.RegisterDevice(deviceB);

        var lowRead = bus.Read(0x1234);
        var highRead = bus.Read(0xC000);
        bus.Write(0x0100, 0x11);
        bus.Write(0xFFFC, 0x22);

        Assert.Equal(0xAA, lowRead);
        Assert.Equal(0xBB, highRead);
        Assert.Equal(1, deviceA.ReadCount);
        Assert.Equal(1, deviceB.ReadCount);
        Assert.Equal(1, deviceA.WriteCount);
        Assert.Equal(1, deviceB.WriteCount);
        Assert.Equal(0x0100, deviceA.LastWriteAddress);
        Assert.Equal(0x11, deviceA.LastWriteValue);
        Assert.Equal(0xFFFC, deviceB.LastWriteAddress);
        Assert.Equal(0x22, deviceB.LastWriteValue);
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: When two devices both claim the same address - exactly
    /// what happens on the C64 when the VIC-II or CIA I/O page overlays
    /// RAM at $D000-$DFFF - the most recently registered device must
    /// win, because the registration order models "I/O is mapped over
    /// the RAM that is still physically present". <see cref="BasicBus"/>
    /// implements this via <c>_devices.Insert(0, device)</c>.
    /// Acceptance: After registering RAM and then I/O for an overlapping
    /// range, a Read of the shared address returns the I/O device's
    /// value and the RAM device's Read is not invoked.
    /// </summary>
    [Fact]
    public void BasicBus_LastRegisteredDeviceWinsForOverlappingRange()
    {
        var bus = new BasicBus();
        var background = new RangeDevice(0x0000, 0xFFFF, readValue: 0xAA, id: 1);
        var overlay = new RangeDevice(0xD000, 0xDFFF, readValue: 0xBB, id: 2);
        bus.RegisterDevice(background);
        bus.RegisterDevice(overlay);

        var insideOverlay = bus.Read(0xD400);
        var outsideOverlay = bus.Read(0x0400);

        Assert.Equal(0xBB, insideOverlay);
        Assert.Equal(0xAA, outsideOverlay);
        Assert.Equal(1, overlay.ReadCount);
        Assert.Equal(1, background.ReadCount);
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: Peek is the side-effect-free read used by debuggers,
    /// disassemblers, snapshot serializers, and the lockstep validator
    /// when sampling expected state. The bus must dispatch Peek to the
    /// handler's <see cref="IAddressSpace.Peek"/> path - not its Read
    /// path - so devices that distinguish (latching I/O registers, the
    /// SID voice state, etc.) can return the snapshot value without
    /// clearing flags.
    /// Acceptance: A device whose Peek returns a different value from
    /// its Read returns its Peek value on <see cref="BasicBus.Peek"/>;
    /// only the Peek counter is incremented, never the Read counter.
    /// </summary>
    [Fact]
    public void BasicBus_PeekRoutesToHandlerPeekNotRead()
    {
        var bus = new BasicBus();
        var device = new RangeDevice(0x0000, 0xFFFF, readValue: 0x11, peekValue: 0x22, id: 1);
        bus.RegisterDevice(device);

        var peeked = bus.Peek(0x1234);
        var read = bus.Read(0x5678);

        Assert.Equal(0x22, peeked);
        Assert.Equal(0x11, read);
        Assert.Equal(1, device.PeekCount);
        Assert.Equal(1, device.ReadCount);
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: Removing a cartridge or unmapping the I/O page must
    /// take the device off the bus immediately. After
    /// <see cref="BasicBus.UnregisterDevice"/> the bus must behave as
    /// though that device was never registered: any address it used to
    /// own falls through to the next handler or to open bus.
    /// Acceptance: A device that claimed every address returns its value
    /// before unregister; after unregister, reads at the same addresses
    /// return 0xFF (open bus default).
    /// </summary>
    [Fact]
    public void BasicBus_Unregister_RemovesDeviceFromDispatch()
    {
        var bus = new BasicBus();
        var device = new RangeDevice(0x0000, 0xFFFF, readValue: 0x55, id: 1);
        bus.RegisterDevice(device);

        Assert.Equal(0x55, bus.Read(0x2000));

        bus.UnregisterDevice(device);

        Assert.Equal(0xFF, bus.Read(0x2000));
        Assert.Equal(0xFF, bus.Peek(0x2000));
    }

    /// <summary>
    /// FR/TR: FR-PERF-RUNFRAME-001, TR-System-Core (BasicBus + C64MemoryMap).
    /// Use case: The managed C64 PAL frame loop uses a single
    /// <see cref="C64MemoryMap"/> behind <see cref="BasicBus"/> for the
    /// normal machine topology, but debug cartridges and overlays must
    /// still keep the bus' last-registered-wins contract while attached.
    /// Acceptance: A plain C64 bus round-trips RAM through the memory
    /// map; a later overlay wins for its claimed address; after
    /// unregistering the overlay the C64 memory map value is visible
    /// again.
    /// </summary>
    [Fact]
    public void BasicBus_C64MemoryMapFastPath_FallsBackWhenOverlayRegistered()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var bus = Assert.IsType<BasicBus>(machine.Bus);
        const ushort address = 0x0400;

        bus.Write(address, 0x11);
        Assert.Equal(0x11, bus.Read(address));
        Assert.Equal(0x11, bus.Peek(address));

        var overlay = new RangeDevice(address, address, readValue: 0x77, peekValue: 0x88, id: 99);
        bus.RegisterDevice(overlay);

        Assert.Equal(0x77, bus.Read(address));
        Assert.Equal(0x88, bus.Peek(address));
        bus.Write(address, 0x22);
        Assert.Equal(1, overlay.WriteCount);
        Assert.Equal(0x22, overlay.LastWriteValue);

        bus.UnregisterDevice(overlay);

        Assert.Equal(0x11, bus.Read(address));
        Assert.Equal(0x11, bus.Peek(address));
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: A write to an address no device claims is the open-bus
    /// write condition - on real hardware this is harmless. The bus
    /// must swallow the write without throwing so test fixtures and
    /// programs that scribble into unmapped windows do not crash the
    /// harness.
    /// Acceptance: <see cref="BasicBus.Write"/> to an unmapped address
    /// returns normally and does not invoke any device Write.
    /// </summary>
    [Fact]
    public void BasicBus_Write_ToUnmappedAddressIsSilentlyDropped()
    {
        var bus = new BasicBus();
        var device = new RangeDevice(0x8000, 0xBFFF, readValue: 0x77, id: 1);
        bus.RegisterDevice(device);

        bus.Write(0x0000, 0xAA);
        bus.Write(0xC000, 0xBB);

        Assert.Equal(0, device.WriteCount);
        Assert.Equal(0xFF, bus.Read(0x0000));
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: <see cref="SimpleRam"/> is the catch-all backing store
    /// used everywhere CPU sanity, lockstep, and ad-hoc machines need
    /// 64KB of plain memory. Every address it sees must round-trip a
    /// byte without aliasing into neighbouring cells.
    /// Acceptance: After writing distinct bytes at the four corner
    /// addresses, each Read / Peek returns the exact value last written
    /// at that address.
    /// </summary>
    [Fact]
    public void SimpleRam_ReadWritePeek_RoundTripsAtCornerAddresses()
    {
        var ram = new SimpleRam();

        ram.Write(0x0000, 0x11);
        ram.Write(0x00FF, 0x22);
        ram.Write(0xC000, 0x33);
        ram.Write(0xFFFF, 0x44);

        Assert.Equal(0x11, ram.Read(0x0000));
        Assert.Equal(0x22, ram.Read(0x00FF));
        Assert.Equal(0x33, ram.Read(0xC000));
        Assert.Equal(0x44, ram.Read(0xFFFF));
        Assert.Equal(0x11, ram.Peek(0x0000));
        Assert.Equal(0x44, ram.Peek(0xFFFF));
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: <see cref="SimpleRam.HandlesAddress"/> returns true for
    /// every 16-bit address because RAM is the universal fallback
    /// device. Code that runs <c>foreach (var d in devices) if
    /// (d.HandlesAddress(a))</c> with RAM last would never reach the
    /// fallback; registering RAM first (as <see cref="BasicBus"/> does
    /// via prepend) ensures specialized devices override.
    /// Acceptance: HandlesAddress returns true for 0x0000, 0x8000, and
    /// 0xFFFF.
    /// </summary>
    [Fact]
    public void SimpleRam_HandlesEveryAddress()
    {
        var ram = new SimpleRam();

        Assert.True(ram.HandlesAddress(0x0000));
        Assert.True(ram.HandlesAddress(0x8000));
        Assert.True(ram.HandlesAddress(0xFFFF));
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: <see cref="SimpleRam.Fill"/> is the test-fixture helper
    /// that floods all 64KB with a single byte - the most common setup
    /// for "memory is in known state X". After Fill every cell must
    /// read back that exact byte.
    /// Acceptance: Fill(0xA5) makes every corner and a mid-range
    /// address read 0xA5; Fill(0x00) zeroes them again.
    /// </summary>
    [Fact]
    public void SimpleRam_Fill_SetsEveryAddressToValue()
    {
        var ram = new SimpleRam();

        ram.Fill(0xA5);

        Assert.Equal(0xA5, ram.Read(0x0000));
        Assert.Equal(0xA5, ram.Read(0x4000));
        Assert.Equal(0xA5, ram.Read(0xFFFF));

        ram.Fill(0x00);

        Assert.Equal(0x00, ram.Read(0x0000));
        Assert.Equal(0x00, ram.Read(0xFFFF));
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: <see cref="SimpleRam.Reset"/> is invoked on system
    /// warm-boot. It must restore the C64 power-on memory layout: zero
    /// page / stack zeroed, screen RAM at $0400 filled with the space
    /// character (0x20), $0800-$9FFF unallocated (0xFF), and the reset
    /// vector at $FFFC/$FFFD pointing at the KERNAL entry ($FCE2). The
    /// lockstep validator and boot smoke tests depend on this exact
    /// layout matching VICE's defaults.
    /// Acceptance: After Reset, $0000 is 0x00, $0400 is 0x20, $1000 is
    /// 0xFF, and the reset vector reads $FCE2.
    /// </summary>
    [Fact]
    public void SimpleRam_Reset_RestoresC64PowerOnLayout()
    {
        var ram = new SimpleRam();
        ram.Fill(0x42);

        ram.Reset();

        Assert.Equal(0x00, ram.Read(0x0000));
        Assert.Equal(0x00, ram.Read(0x01FF));
        Assert.Equal(0x20, ram.Read(0x0400));
        Assert.Equal(0x20, ram.Read(0x07FF));
        Assert.Equal(0xFF, ram.Read(0x1000));
        Assert.Equal(0xFF, ram.Read(0x9FFF));
        Assert.Equal(0xE2, ram.Read(0xFFFC));
        Assert.Equal(0xFC, ram.Read(0xFFFD));
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: <see cref="SimpleRam"/> wired up behind a
    /// <see cref="BasicBus"/> models the simplest possible C64-style
    /// memory map: every address goes to RAM. End-to-end the bus must
    /// route every CPU read / write through RAM and the values must
    /// round-trip identically to direct RAM access.
    /// Acceptance: Writing through the bus and reading through the bus
    /// produces the same byte; the same value is also visible via
    /// direct RAM Read / Peek and via bus Peek.
    /// </summary>
    [Fact]
    public void BasicBus_WithSimpleRam_RoundTripsThroughBus()
    {
        var bus = new BasicBus();
        var ram = new SimpleRam();
        bus.RegisterDevice(ram);

        bus.Write(0x0042, 0xDE);
        bus.Write(0xC000, 0xAD);
        bus.Write(0xFFFE, 0xBE);

        Assert.Equal(0xDE, bus.Read(0x0042));
        Assert.Equal(0xAD, bus.Read(0xC000));
        Assert.Equal(0xBE, bus.Read(0xFFFE));
        Assert.Equal(0xDE, bus.Peek(0x0042));
        Assert.Equal(0xDE, ram.Read(0x0042));
        Assert.Equal(0xBE, ram.Peek(0xFFFE));
    }

    /// <summary>
    /// FR/TR: TR-System-Core (BasicBus + SimpleRam).
    /// Use case: <see cref="SimpleRam.LoadRom"/> is the path that the
    /// ROM loader uses to drop BASIC, KERNAL, and character ROMs into
    /// place at boot. The data must land contiguously starting at the
    /// requested address with no offset or aliasing, and reads through
    /// the bus must see exactly the ROM bytes that were loaded.
    /// Acceptance: LoadRom(0xC000, [0x10, 0x20, 0x30, 0x40]) places
    /// those bytes at $C000-$C003 verbatim; the byte at $BFFF (just
    /// before) and $C004 (just after) are unchanged from the prior fill.
    /// </summary>
    [Fact]
    public void SimpleRam_LoadRom_PlacesBytesContiguouslyAtStartAddress()
    {
        var ram = new SimpleRam();
        ram.Fill(0x00);
        var rom = new byte[] { 0x10, 0x20, 0x30, 0x40 };

        ram.LoadRom(0xC000, rom);

        Assert.Equal(0x00, ram.Read(0xBFFF));
        Assert.Equal(0x10, ram.Read(0xC000));
        Assert.Equal(0x20, ram.Read(0xC001));
        Assert.Equal(0x30, ram.Read(0xC002));
        Assert.Equal(0x40, ram.Read(0xC003));
        Assert.Equal(0x00, ram.Read(0xC004));
    }
}
