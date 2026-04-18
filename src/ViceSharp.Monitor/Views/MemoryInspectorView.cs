using ViceSharp.Abstractions;

namespace ViceSharp.Monitor.Views;

public sealed class MemoryInspectorView
{
    private readonly IBus _bus;

    public MemoryInspectorView(IBus bus)
    {
        _bus = bus;
    }

    public byte Read(ushort address) => _bus.Read(address);
    public void Write(ushort address, byte value) => _bus.Write(address, value);

    public Span<byte> ReadBlock(ushort startAddress, int length)
    {
        byte[] buffer = new byte[length];

        for (int i = 0; i < length; i++)
        {
            buffer[i] = _bus.Read((ushort)(startAddress + i));
        }

        return buffer;
    }

    public string FormatHexDump(ushort startAddress, int lines)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int line = 0; line < lines; line++)
        {
            ushort addr = (ushort)(startAddress + line * 16);
            sb.Append($"{addr:X4}: ");

            for (int col = 0; col < 16; col++)
            {
                sb.Append($"{Read((ushort)(addr + col)):X2} ");
            }

            sb.Append(" |");

            for (int col = 0; col < 16; col++)
            {
                byte b = Read((ushort)(addr + col));
                sb.Append(b is >= 32 and < 127 ? (char)b : '.');
            }

            sb.AppendLine("|");
        }

        return sb.ToString();
    }
}