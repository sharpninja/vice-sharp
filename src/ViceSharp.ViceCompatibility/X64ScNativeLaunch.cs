using System.Buffers.Binary;
using System.Globalization;

namespace ViceSharp.ViceCompatibility;

/// <summary>
/// External launch adapter for native VSF resume on the pinned, unmodified VICE (PLAN-NATIVERESTORE-001 D1;
/// decisions R10 A, N05, N07, N23; TR-VSFLOCKSTEP-RESUME-001 AC1 as amended by R15). It does not execute, patch or
/// replace VICE. Pinned commit cc67418cba58e71d8e70fd353e9de47199ad2075: drive-snapshot.c lines 139 to 162 write
/// has_tde and has_drives as the first two DRIVE payload bytes; the reader (lines 317 to 319, 368 to 419 and 540
/// to 550) copies three uninitialized clocks into every unit whose type is not DRIVE_TYPE_NONE when its module is
/// missing or has has_tde 0; initcmdline.c lines 279 to 286 make -limitcycles an absolute maincpu_clk limit.
/// </summary>
public static class X64ScNativeLaunch
{
    private const int ModuleHeaderSize = 22;

    /// <summary>One DRIVE unit (8 to 11) as read from the snapshot: present, and its has_tde and has_drives bytes when the payload holds them.</summary>
    public sealed record DriveUnit(int Unit, bool Present, int? HasTde, int? HasDrives);

    /// <summary>The MAINCPU register fields the resume cross-check compares (clock, A, X, Y, SP, PC, status byte).</summary>
    public sealed record MainCpuFields(byte Major, byte Minor, ulong Clock, byte A, byte X, byte Y, byte Sp, ushort Pc, byte Status);

    /// <summary>The launch budget: the absolute -limitcycles value and the rule that produced it.</summary>
    public sealed record LaunchBudget(ulong LimitCycles, string Reason);

    /// <summary>
    /// Reads the DRIVE8 to DRIVE11 modules. A unit whose module is absent is reported as not present; a present
    /// module whose payload is shorter than two bytes is reported with null fields (N05 invalid payload).
    /// </summary>
    public static DriveUnit[] ReadDriveUnits(ReadOnlySpan<byte> snapshot)
    {
        var units = new DriveUnit[4];
        for (var i = 0; i < 4; i++) units[i] = new(8 + i, false, null, null);
        foreach (var (name, offset, size) in Modules(snapshot))
        {
            if (!name.StartsWith("DRIVE", StringComparison.Ordinal) || !int.TryParse(name[5..], NumberStyles.None, CultureInfo.InvariantCulture, out var unit) || unit is < 8 or > 11)
                continue;
            var payloadLength = size - ModuleHeaderSize;
            units[unit - 8] = payloadLength < 2
                ? new(unit, true, null, null)
                : new(unit, true, snapshot[offset + ModuleHeaderSize], snapshot[offset + ModuleHeaderSize + 1]);
        }
        return units;
    }

    /// <summary>
    /// Reads the MAINCPU register fields (mainc64cpu.c maincpu_snapshot_write_module order: clock, A, X, Y, SP, PC,
    /// status). Fails closed when the module is absent or too short.
    /// </summary>
    public static MainCpuFields ReadMainCpu(ReadOnlySpan<byte> snapshot)
    {
        foreach (var (name, offset, size) in Modules(snapshot))
        {
            if (name != "MAINCPU") continue;
            if (size - ModuleHeaderSize < 15) throw Invalid("MAINCPU payload is too short for the register fields.");
            var p = offset + ModuleHeaderSize;
            return new(snapshot[offset + 16], snapshot[offset + 17],
                BinaryPrimitives.ReadUInt64LittleEndian(snapshot.Slice(p, 8)),
                snapshot[p + 8], snapshot[p + 9], snapshot[p + 10], snapshot[p + 11],
                BinaryPrimitives.ReadUInt16LittleEndian(snapshot.Slice(p + 12, 2)), snapshot[p + 14]);
        }
        throw Invalid("Missing MAINCPU module.");
    }

    /// <summary>
    /// The R10 launch precondition: "-drive&lt;n&gt;type 0" for every unit whose module is missing or has has_tde 0, in unit
    /// order. Fails closed (N05) when any present DRIVE payload is shorter than two bytes, so a native load can never
    /// start from an unvalidated drive module.
    /// </summary>
    public static string[] ComputePrecondition(ReadOnlySpan<byte> snapshot)
    {
        var flags = new List<string>();
        foreach (var unit in ReadDriveUnits(snapshot))
        {
            if (unit.Present && unit.HasTde is null)
                throw Invalid($"DRIVE{unit.Unit} payload is shorter than two bytes; has_tde and has_drives are unreadable.");
            if (!unit.Present || unit.HasTde == 0)
            {
                flags.Add($"-drive{unit.Unit}type");
                flags.Add("0");
            }
        }
        return [.. flags];
    }

    /// <summary>
    /// The launch budget (N07, N23): the absolute limit is the restored clock plus the window, except when that
    /// value falls inside the autostart prelude, in which case it is the prelude length plus the window and the
    /// reason says so. The window must be nonempty; overflow fails.
    /// </summary>
    public static LaunchBudget ComputeBudget(ulong mainCpuClock, ulong windowCycles, ulong preludeCycles)
    {
        if (windowCycles == 0) throw new ArgumentOutOfRangeException(nameof(windowCycles), "The post-load window must be nonempty.");
        var limit = checked(mainCpuClock + windowCycles);
        if (limit <= preludeCycles)
            return new(checked(preludeCycles + windowCycles), "prelude plus window");
        return new(limit, "clock plus window");
    }

    /// <summary>
    /// The native resume command line: autostart of the normalized snapshot, +logcolorize (the stock binaries crash
    /// on a non-terminal stdout otherwise), -seed 1 (deterministic RAM init), the absolute cycle limit, and the R10
    /// precondition flags last so that no later argument can override them (N05).
    /// </summary>
    public static string[] BuildArguments(string snapshotPath, string[] precondition, ulong limitCycles)
    {
        ArgumentException.ThrowIfNullOrEmpty(snapshotPath);
        ArgumentNullException.ThrowIfNull(precondition);
        return ["-autostart", snapshotPath, "+logcolorize", "-seed", "1", "-limitcycles", limitCycles.ToString(CultureInfo.InvariantCulture), .. precondition];
    }

    private static IEnumerable<(string Name, int Offset, int Size)> Modules(ReadOnlySpan<byte> snapshot)
    {
        var list = new List<(string, int, int)>();
        if (snapshot.Length < 37 || !snapshot[..19].SequenceEqual("VICE Snapshot File\x1a"u8))
            throw Invalid("Missing or truncated VSF header.");
        var offset = 37;
        if (snapshot.Length >= 50 && snapshot.Slice(37, 13).SequenceEqual("VICE Version\x1a"u8))
        {
            if (snapshot.Length < 58) throw Invalid("Truncated VICE version header.");
            offset = 58;
        }
        while (offset < snapshot.Length)
        {
            var remaining = snapshot.Length - offset;
            if (remaining < ModuleHeaderSize) throw Invalid("Truncated module header.");
            var size = BinaryPrimitives.ReadUInt32LittleEndian(snapshot.Slice(offset + 18, 4));
            if (size < ModuleHeaderSize || size > (uint)remaining) throw Invalid("Module length is outside the snapshot boundary.");
            var nameBytes = snapshot.Slice(offset, 16);
            var end = nameBytes.IndexOf((byte)0);
            var name = System.Text.Encoding.ASCII.GetString(end < 0 ? nameBytes : nameBytes[..end]);
            list.Add((name, offset, (int)size));
            offset += (int)size;
        }
        return list;
    }

    private static InvalidDataException Invalid(string message) => new(message);
}
