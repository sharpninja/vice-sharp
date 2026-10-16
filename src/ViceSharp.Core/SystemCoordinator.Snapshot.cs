using ViceSharp.Abstractions;

namespace ViceSharp.Core;

public sealed partial class SystemCoordinator
{
    /// <summary>
    /// Captures all coordinated clock schedules and their ordered topology.
    /// Callers must exclude stepping. Peripheral device and bus state are not
    /// part of this clock-only snapshot. FR-SNP-001/002, TR-STATE-001, TEST-SNP-001.
    /// </summary>
    public byte[] CaptureClockSnapshot()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)1);
        writer.Write(_hostCycles);
        writer.Write(_systems.Count);
        foreach (var machine in _systems)
        {
            writer.Write(SystemIdentity(machine));
            writer.Write(_cartExtensions.Contains(machine));
            var payload = RequireSystemClock(machine).CaptureSnapshot();
            writer.Write(payload.Length);
            writer.Write(payload);
        }
        return stream.ToArray();
    }

    /// <summary>Validates every nested clock before any machine state changes; returns the host clock cycle.</summary>
    public long ValidateClockSnapshot(ReadOnlySpan<byte> payload) => ReadClockSnapshot(payload).HostClockCycle;

    /// <summary>Restores every clock and the coordinator counter without advancing any device.</summary>
    public void RestoreClockSnapshot(ReadOnlySpan<byte> payload)
    {
        var state = ReadClockSnapshot(payload);
        for (var i = 0; i < _systems.Count; i++)
            RequireSystemClock(_systems[i]).RestoreSnapshot(state.Clocks[i]);
        _hostCycles = state.HostCycles;
    }

    /// <summary>Checks all legacy cycle projections without mutation, including signed-range overflow.</summary>
    public void ValidateLegacyPosition(long hostCycle) => GetLegacyPositions(hostCycle);

    /// <summary>
    /// Best-effort legacy rebase: host and cart clocks share the imported cycle;
    /// independent clocks use the same frequency ratio as normal coordination.
    /// Missing device and clock microstate cannot be reconstructed from a cycle.
    /// </summary>
    public void RestoreLegacyPosition(long hostCycle)
    {
        var cycles = GetLegacyPositions(hostCycle);
        for (var i = 0; i < _systems.Count; i++)
            RequireSystemClock(_systems[i]).RestoreLegacyPosition(cycles[i]);
        _hostCycles = hostCycle;
    }

    private long[] GetLegacyPositions(long hostCycle)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hostCycle);
        if (_systems.Count == 0 || _systems[0].Clock.FrequencyHz <= 0)
            throw new ArgumentException("Snapshot requires a coordinated host with a positive clock frequency.");
        var cycles = new long[_systems.Count];
        for (var i = 0; i < cycles.Length; i++)
        {
            var machine = _systems[i];
            var clock = RequireSystemClock(machine);
            if (clock.FrequencyHz <= 0)
                throw new ArgumentException("Snapshot requires positive system clock frequencies.");
            var projected = i == 0 || _cartExtensions.Contains(machine)
                ? hostCycle
                : (Int128)hostCycle * clock.FrequencyHz / _systems[0].Clock.FrequencyHz;
            if (projected > long.MaxValue)
                throw new ArgumentException("Snapshot cycle exceeds a peripheral clock's supported range.");
            cycles[i] = (long)projected;
        }
        return cycles;
    }

    private CoordinatorClockState ReadClockSnapshot(ReadOnlySpan<byte> payload)
    {
        if (payload.Length > 1_048_576)
            throw new ArgumentException("Coordinated clock snapshot exceeds the supported size.", nameof(payload));
        try
        {
            using var stream = new MemoryStream(payload.ToArray(), writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != 1)
                throw new ArgumentException("Unsupported coordinated clock snapshot version.", nameof(payload));
            var hostCycles = reader.ReadInt64();
            if (hostCycles < 0 || reader.ReadInt32() != _systems.Count || _systems.Count == 0)
                throw new ArgumentException("Coordinated snapshot cycle or system count does not match.", nameof(payload));
            var clocks = new byte[_systems.Count][];
            long hostClockCycle = 0;
            for (var i = 0; i < clocks.Length; i++)
            {
                var machine = _systems[i];
                if (reader.ReadString() != SystemIdentity(machine))
                    throw new ArgumentException("Coordinated snapshot system identity does not match.", nameof(payload));
                var cart = reader.ReadByte();
                if (cart > 1 || (cart == 1) != _cartExtensions.Contains(machine))
                    throw new ArgumentException("Coordinated snapshot cart topology does not match.", nameof(payload));
                var length = reader.ReadInt32();
                if (length < 0 || length > stream.Length - stream.Position)
                    throw new ArgumentException("Coordinated clock payload is truncated.", nameof(payload));
                clocks[i] = reader.ReadBytes(length);
                var cycle = RequireSystemClock(machine).ValidateSnapshot(clocks[i]);
                if (i == 0) hostClockCycle = cycle;
            }
            if (stream.Position != stream.Length)
                throw new ArgumentException("Coordinated clock snapshot has trailing bytes.", nameof(payload));
            return new(hostCycles, hostClockCycle, clocks);
        }
        catch (Exception ex) when (ex is IOException or FormatException)
        {
            throw new ArgumentException("Coordinated clock snapshot is malformed.", nameof(payload), ex);
        }
    }

    private static SystemClock RequireSystemClock(IMachine machine) => machine.Clock as SystemClock
        ?? throw new ArgumentException("Coordinated snapshots require SystemClock on every system.");

    private static string SystemIdentity(IMachine machine) => machine.Architecture switch
    {
        IDriveArchitectureDescriptor drive => $"{drive.MachineName}|drive:{drive.DeviceNumber}|rom:{drive.DosRomName}",
        IProfiledArchitectureDescriptor profiled => $"{profiled.MachineName}|profile:{profiled.MachineProfile.Id}",
        var architecture => architecture.MachineName
    };

    private sealed record CoordinatorClockState(long HostCycles, long HostClockCycle, byte[][] Clocks);
}
