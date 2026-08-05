using ViceSharp.Abstractions;
using ViceSharp.Chips.IEC;

namespace ViceSharp.Core.Vic20;

/// <summary>
/// VIC-20 keyboard matrix and joystick state wired through VIA2 port callbacks.
/// Column select is written on Port B; rows are read on Port A (VICE vic20via2.c).
/// </summary>
/// <remarks>FR-VIC20-003, FR-VIC20-004. Board glue only; Via6522 stays machine-agnostic.</remarks>
public sealed class Vic20KeyboardMatrix : IDevice
{
    // keyarr[row] bit col set when key (row,col) is pressed (active-high internal).
    private readonly byte[] _keyRows = new byte[8];
    private byte _joystickMask; // bit0 up,1 down,2 left,3 right,4 fire (active-high pressed)

    private Via6522? _via2;
    private Via6522? _via1;

    public DeviceId Id { get; } = new(0x0B01);
    public string Name => "VIC-20 keyboard matrix";

    /// <summary>Joystick directions + fire as active-high pressed bits (0=up..3=right, 4=fire).</summary>
    public byte JoystickMask
    {
        get => _joystickMask;
        set => _joystickMask = value;
    }

    public void Reset()
    {
        Array.Clear(_keyRows);
        _joystickMask = 0;
    }

    /// <summary>Press or release a matrix key at (row, column), both 0..7.</summary>
    public void SetKey(int row, int column, bool pressed)
    {
        if ((uint)row > 7 || (uint)column > 7)
            throw new ArgumentOutOfRangeException(nameof(row), "row/column must be 0..7");

        var bit = (byte)(1 << column);
        if (pressed)
            _keyRows[row] |= bit;
        else
            _keyRows[row] = (byte)(_keyRows[row] & ~bit);
    }

    /// <summary>Clear all keys.</summary>
    public void ReleaseAllKeys() => Array.Clear(_keyRows);

    /// <summary>
    /// Connect VIA2 (keyboard / IRQ) and optionally VIA1 (joystick bits / NMI path).
    /// </summary>
    public void Connect(Via6522 via2, Via6522? via1 = null)
    {
        _via2 = via2 ?? throw new ArgumentNullException(nameof(via2));
        _via1 = via1;

        // Port A read: keyboard rows for columns driven low on Port B output.
        via2.PortAInput = ReadPortA;
        // Port B read: reverse scan + joystick right on bit 7.
        via2.PortBInput = ReadPortB;

        if (via1 is not null)
        {
            // VIA1 Port A: joystick up/down/left/fire (bits 2..5 typical VICE mapping).
            var prev = via1.PortAInput;
            via1.PortAInput = () =>
            {
                var baseVal = prev?.Invoke() ?? 0xFF;
                return (byte)(baseVal & ComposeVia1JoystickMask());
            };
        }
    }

    private byte ReadPortA()
    {
        // Idle high on undriven bits; pressed keys pull row bits low when column is selected (low).
        byte val = 0xFF;
        var columns = _via2 is null ? (byte)0xFF : GetPortBDrivenLow();
        for (var col = 0; col < 8; col++)
        {
            if ((columns & (1 << col)) != 0)
                continue; // column not selected (not driven low)

            for (var row = 0; row < 8; row++)
            {
                if ((_keyRows[row] & (1 << col)) != 0)
                    val = (byte)(val & ~(1 << row));
            }
        }

        // Joystick right also appears on VIA2 PA via column/port-B path in VICE;
        // map fire/right onto PA when active for software that peeks PA.
        if ((_joystickMask & 0x08) != 0) // right
            val = (byte)(val & 0x7F);

        return val;
    }

    private byte ReadPortB()
    {
        byte val = 0xFF;
        // Reverse matrix: when rows driven low on PA, columns appear on PB.
        var rows = _via2 is null ? (byte)0xFF : GetPortADrivenLow();
        for (var row = 0; row < 8; row++)
        {
            if ((rows & (1 << row)) != 0)
                continue;
            for (var col = 0; col < 8; col++)
            {
                if ((_keyRows[row] & (1 << col)) != 0)
                    val = (byte)(val & ~(1 << col));
            }
        }

        if ((_joystickMask & 0x08) != 0) // right on PB7
            val = (byte)(val & 0x7F);

        return val;
    }

    private byte GetPortBDrivenLow()
    {
        // Bits that are outputs and written 0 are driven low.
        // Without direct DDR/OR access, approximate: call PortB through a peek of composed output.
        // Via6522 exposes only callbacks; we track via reading last known by re-entrant free path.
        // Use PortBOutputChanged side channel if available; otherwise assume all-FF columns
        // unless we latch on PortBOutputChanged.
        return _portBLatched;
    }

    private byte GetPortADrivenLow() => _portALatched;

    private byte _portBLatched = 0xFF;
    private byte _portALatched = 0xFF;

    /// <summary>
    /// Call after Connect to latch Port A/B output changes from the VIA DDR/OR model.
    /// </summary>
    public void AttachOutputLatches(Via6522 via2)
    {
        var prevB = via2.PortBOutputChanged;
        via2.PortBOutputChanged = v =>
        {
            _portBLatched = v;
            prevB?.Invoke(v);
        };
        var prevA = via2.PortAOutputChanged;
        via2.PortAOutputChanged = v =>
        {
            _portALatched = v;
            prevA?.Invoke(v);
        };
    }

    private byte ComposeVia1JoystickMask()
    {
        // Active-low joystick bits on VIA1 PA (common VIC-20 mapping):
        // bit2 up, bit3 down, bit4 left, bit5 fire (bit for right is on VIA2).
        byte mask = 0xFF;
        if ((_joystickMask & 0x01) != 0) mask = (byte)(mask & ~(1 << 2)); // up
        if ((_joystickMask & 0x02) != 0) mask = (byte)(mask & ~(1 << 3)); // down
        if ((_joystickMask & 0x04) != 0) mask = (byte)(mask & ~(1 << 4)); // left
        if ((_joystickMask & 0x10) != 0) mask = (byte)(mask & ~(1 << 5)); // fire
        return mask;
    }

    /// <summary>
    /// Scan helper used by tests: given a column mask driven low, return the row bits pulled low.
    /// </summary>
    public byte ScanRowsForColumns(byte columnSelectActiveLow)
    {
        byte val = 0xFF;
        for (var col = 0; col < 8; col++)
        {
            if ((columnSelectActiveLow & (1 << col)) != 0)
                continue;
            for (var row = 0; row < 8; row++)
            {
                if ((_keyRows[row] & (1 << col)) != 0)
                    val = (byte)(val & ~(1 << row));
            }
        }
        return val;
    }
}
