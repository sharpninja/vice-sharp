using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed partial class SystemClock
{
    private const byte ClockSnapshotVersion = 1;

    /// <summary>
    /// Captures master-cycle position, dispatch counters and interrupt timing at
    /// a completed tick boundary. Callers must stop concurrent stepping. Device
    /// microstate is owned by the machine snapshot, not this clock payload.
    /// FR-SNP-001/002, TR-STATE-001, TR-DET-001, TEST-SNP-001.
    /// </summary>
    public byte[] CaptureSnapshot()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(ClockSnapshotVersion);
        writer.Write(FrequencyHz);
        writer.Write(_cycle);
        writer.Write(_irqAssertCycle);
        writer.Write(_irqDelayCycles);
        writer.Write(_nmiWasAsserted);
        writer.Write(_nmiPending);
        WriteLine(writer, _irqLine);
        WriteLine(writer, _nmiLine);
        writer.Write(_devices.Count);
        foreach (var device in _devices)
        {
            writer.Write(device.Id.Value);
            writer.Write((int)device.Phase);
            writer.Write(device.ClockDivisor);
            writer.Write(GetCounter(device));
        }
        return stream.ToArray();
    }

    /// <summary>Validates the entire payload and target schedule without changing state; returns its cycle.</summary>
    public long ValidateSnapshot(ReadOnlySpan<byte> payload) => ReadSnapshot(payload).Cycle;

    /// <summary>
    /// Restores a validated clock payload without ticking or resetting devices.
    /// The target must have the same frequency and ordered device schedule.
    /// </summary>
    public void RestoreSnapshot(ReadOnlySpan<byte> payload)
    {
        var state = ReadSnapshot(payload);
        _cycle = state.Cycle;
        _irqAssertCycle = state.IrqAssertCycle;
        _irqDelayCycles = state.IrqDelayCycles;
        _nmiWasAsserted = state.NmiWasAsserted;
        _nmiPending = state.NmiPending;
        RestoreLine(_irqLine, state.Irq);
        RestoreLine(_nmiLine, state.Nmi);
        for (var i = 0; i < _devices.Count; i++)
            SetCounter(_devices[i], state.Counters[i]);
        DebugFetchIrqNote = "";
    }

    /// <summary>
    /// Best-effort legacy import when only a cycle is available: derive fixed
    /// divisor phases from reset, discard stale NMI edges, and restart IRQ delay
    /// at the imported cycle. This is not exact continuation of a saved clock.
    /// Call after the legacy chip injectors, without concurrent stepping.
    /// </summary>
    public void RestoreLegacyPosition(long cycle)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cycle);
        _cycle = cycle;
        foreach (var device in _devices)
            SetCounter(device, device.ClockDivisor > 1 ? (uint)(cycle % device.ClockDivisor) : 0);
        _nmiWasAsserted = _nmiLine?.IsAsserted == true;
        _nmiPending = false;
        _irqAssertCycle = _irqLine?.IsAsserted == true ? cycle : long.MaxValue;
        _irqDelayCycles = 0;
        if (_irqLine is InterruptLine irq) irq.ConsumeRisingEdge();
        if (_nmiLine is InterruptLine nmi) nmi.ConsumeRisingEdge();
        DebugFetchIrqNote = "";
    }

    private ClockState ReadSnapshot(ReadOnlySpan<byte> payload)
    {
        if (payload.Length > 1_048_576)
            throw InvalidSnapshot("exceeds the supported size");
        try
        {
            using var stream = new MemoryStream(payload.ToArray(), writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != ClockSnapshotVersion || reader.ReadInt64() != FrequencyHz)
                throw InvalidSnapshot("version or frequency does not match");
            var cycle = reader.ReadInt64();
            var irqCycle = reader.ReadInt64();
            var irqDelay = reader.ReadInt32();
            if (cycle < 0 || irqDelay < 0 || (irqCycle != long.MaxValue && (irqCycle < 0 || irqCycle > cycle)))
                throw InvalidSnapshot("invalid cycle or interrupt delay");
            var nmiWasAsserted = ReadFlag(reader);
            var nmiPending = ReadFlag(reader);
            var irq = ReadLine(reader, _irqLine);
            var nmi = ReadLine(reader, _nmiLine);
            if (reader.ReadInt32() != _devices.Count)
                throw InvalidSnapshot("device count does not match");
            var counters = new uint[_devices.Count];
            for (var i = 0; i < counters.Length; i++)
            {
                var device = _devices[i];
                if (reader.ReadUInt32() != device.Id.Value || reader.ReadInt32() != (int)device.Phase
                    || reader.ReadUInt32() != device.ClockDivisor)
                    throw InvalidSnapshot("device identity, phase or divisor does not match");
                counters[i] = reader.ReadUInt32();
                if (device.ClockDivisor == 0 || counters[i] >= device.ClockDivisor)
                    throw InvalidSnapshot("device counter is out of range");
            }
            if (stream.Position != stream.Length)
                throw InvalidSnapshot("unexpected trailing bytes");
            return new(cycle, irqCycle, irqDelay, nmiWasAsserted, nmiPending, irq, nmi, counters);
        }
        catch (EndOfStreamException ex)
        {
            throw new ArgumentException("Clock snapshot is truncated.", nameof(payload), ex);
        }
    }

    private static void WriteLine(BinaryWriter writer, IInterruptLine? line)
    {
        writer.Write(line is not null);
        if (line is null) return;
        if (line is not InterruptLine concrete)
            throw new InvalidOperationException("Clock snapshots require snapshot-capable interrupt lines.");
        writer.Write(concrete.PendingRisingEdge);
        var sources = concrete.CaptureSources();
        writer.Write(sources.Length);
        foreach (var source in sources) writer.Write(source);
    }

    private static LineState? ReadLine(BinaryReader reader, IInterruptLine? line)
    {
        var present = ReadFlag(reader);
        if (present != (line is not null) || (present && line is not InterruptLine))
            throw InvalidSnapshot("interrupt line topology does not match");
        if (!present) return null;
        var edge = ReadFlag(reader);
        var count = reader.ReadInt32();
        if (count < 0 || count > (reader.BaseStream.Length - reader.BaseStream.Position) / sizeof(uint))
            throw InvalidSnapshot("invalid interrupt source count");
        var sources = new uint[count];
        for (var i = 0; i < count; i++)
        {
            sources[i] = reader.ReadUInt32();
            if (i > 0 && sources[i] <= sources[i - 1])
                throw InvalidSnapshot("interrupt source identities are not unique and ordered");
        }
        return new(edge, sources);
    }

    private static bool ReadFlag(BinaryReader reader) => reader.ReadByte() switch
    {
        0 => false,
        1 => true,
        _ => throw InvalidSnapshot("invalid boolean flag")
    };

    private static void RestoreLine(IInterruptLine? line, LineState? state)
    {
        if (line is InterruptLine concrete && state is not null)
            concrete.RestoreSources(state.Sources, state.Edge);
    }

    private uint GetCounter(IClockedDevice device)
    {
        foreach (var entry in _phi1SlowDevices)
            if (ReferenceEquals(entry.Device, device)) return entry.Counter;
        foreach (var entry in _phi2SlowDevices)
            if (ReferenceEquals(entry.Device, device)) return entry.Counter;
        return 0;
    }

    private void SetCounter(IClockedDevice device, uint counter)
    {
        for (var i = 0; i < _phi1SlowDevices.Length; i++)
            if (ReferenceEquals(_phi1SlowDevices[i].Device, device)) _phi1SlowDevices[i].Counter = counter;
        for (var i = 0; i < _phi2SlowDevices.Length; i++)
            if (ReferenceEquals(_phi2SlowDevices[i].Device, device)) _phi2SlowDevices[i].Counter = counter;
    }

    private static ArgumentException InvalidSnapshot(string reason) => new($"Clock snapshot {reason}.", "payload");
    private sealed record LineState(bool Edge, uint[] Sources);
    private sealed record ClockState(long Cycle, long IrqAssertCycle, int IrqDelayCycles,
        bool NmiWasAsserted, bool NmiPending, LineState? Irq, LineState? Nmi, uint[] Counters);
}
