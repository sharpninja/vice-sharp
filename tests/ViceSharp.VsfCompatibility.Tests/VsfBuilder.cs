using System.Buffers.Binary;
using System.Text;

namespace ViceSharp.VsfCompatibility.Tests;

/// <summary>
/// Synthetic VSF 2.0 container builder for contract fixtures (pinned VICE cc67418 snapshot.c layout: 19-byte magic,
/// major, minor, 16-byte machine name, the 21-byte version block, then modules with a 16-byte name, major, minor
/// and a little-endian size that includes the 22-byte module header). Not a parser and not production code.
/// </summary>
internal static class VsfBuilder
{
    internal sealed record Module(string Name, byte Major, byte Minor, byte[] Payload);

    internal static Module MainCpu(byte major, byte minor, ulong clock, ushort pc, byte status)
    {
        // mainc64cpu.c maincpu_snapshot_write_module: clock, A, X, Y, SP, PC, status, then the remaining fields;
        // the payload length follows the pinned per-version layout the adapter checks (91, 99, 103).
        var length = minor switch { 2 => 91, 3 => 99, _ => 103 };
        var payload = new byte[length];
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(0, 8), clock);
        payload[8] = 0x11; payload[9] = 0x22; payload[10] = 0x33; payload[11] = 0xF8;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(12, 2), pc);
        payload[14] = status;
        return new("MAINCPU", major, minor, payload);
    }

    internal static Module Drive(int unit, byte[] payload) => new($"DRIVE{unit}", 2, 0, payload);

    internal static byte[] Build(params Module[] modules)
    {
        var bytes = new List<byte>();
        bytes.AddRange("VICE Snapshot File\x1a"u8.ToArray());
        bytes.Add(2); bytes.Add(0);
        bytes.AddRange(Name16("C64SC"));
        bytes.AddRange("VICE Version\x1a"u8.ToArray());
        bytes.AddRange([3, 9, 0, 0]);
        bytes.AddRange(BitConverter.GetBytes(45000u));
        foreach (var m in modules)
        {
            bytes.AddRange(Name16(m.Name));
            bytes.Add(m.Major); bytes.Add(m.Minor);
            bytes.AddRange(BitConverter.GetBytes((uint)(22 + m.Payload.Length)));
            bytes.AddRange(m.Payload);
        }
        return [.. bytes];
    }

    private static byte[] Name16(string name)
    {
        var buffer = new byte[16];
        Encoding.ASCII.GetBytes(name).CopyTo(buffer, 0);
        return buffer;
    }
}
