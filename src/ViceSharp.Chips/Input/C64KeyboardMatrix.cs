using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Input;

/// <summary>
/// C64 8×8 Keyboard Matrix implementation.
/// </summary>
public sealed class C64KeyboardMatrix : IInputSource
{
    public DeviceId Id => new DeviceId(0x0008);
    public string Name => "C64 Keyboard Matrix";
    public bool IsConnected => true;

    /// <inheritdoc />
    public void Poll()
    {
        // No external polling required
    }

    // 8 rows × 8 columns
    private readonly bool[,] _matrix = new bool[8, 8];
    private byte _columnMask;
    private byte _rowMask;
    
    // VICE-style: RESTORE key is special (NMI trigger)
    private bool _restoreKeyPressed;
    private bool _stopKeyPressed;

    public C64KeyboardMatrix()
    {
    }

    /// <inheritdoc />
    public void Initialize()
    {
        Reset();
    }

    /// <inheritdoc />
    public void Reset()
    {
        Array.Clear(_matrix);
        _columnMask = 0xFF;
        _rowMask = 0xFF;
    }

    /// <summary>
    /// Set key press state for C64 keycode with VICE-style handling
    /// </summary>
    public void SetKey(byte keyCode, bool pressed)
    {
        int row = keyCode >> 3;
        int col = keyCode & 0x07;

        if (row < 8 && col < 8)
        {
            // VICE-style: RUN/STOP is row 7, col 7 (0x3F)
            if (keyCode == 0x3F) _stopKeyPressed = pressed;
            // RESTORE is row 3, col 1 (0x31) - triggers NMI
            if (keyCode == 0x31) _restoreKeyPressed = pressed;
            
            _matrix[row, col] = pressed;
        }
    }
    
    /// <summary>
    /// Check if RESTORE key is pressed (triggers NMI in VICE)
    /// </summary>
    public bool IsRestorePressed => _restoreKeyPressed;
    
    /// <summary>
    /// Check if RUN/STOP key is pressed
    /// </summary>
    public bool IsStopPressed => _stopKeyPressed;
    
    /// <summary>
    /// VICE-style: Check for SHIFT + C= combo for PETSCII
    /// </summary>
    public bool IsShiftCbmPressed => _matrix[0x0F >> 3, 0x0F & 0x07] && _matrix[0x3D >> 3, 0x3D & 0x07];

    /// <summary>
    /// Select column mask for CIA port B
    /// </summary>
    public void SetColumnMask(byte mask)
    {
        _columnMask = mask;
    }

    /// <summary>
    /// Select row mask for CIA port A.
    /// </summary>
    public void SetRowMask(byte mask)
    {
        _rowMask = mask;
    }

    /// <summary>
    /// Read row state for CIA port A
    /// </summary>
    public byte ReadRowState()
    {
        byte result = 0xFF;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if ((_columnMask & (1 << col)) == 0 && _matrix[row, col])
                {
                    result &= (byte)~(1 << row);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Read column state for CIA port B.
    /// </summary>
    public byte ReadColumnState()
    {
        byte result = 0xFF;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                if ((_rowMask & (1 << row)) == 0 && _matrix[row, col])
                {
                    result &= (byte)~(1 << col);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Standard C64 keycode mapping:
    /// 
    /// 0x00: DEL,  0x01: RETURN, 0x02: CURSOR RIGHT, 0x03: F7, 0x04: F1, 0x05: F3, 0x06: F5, 0x07: CURSOR DOWN
    /// 0x08: 3,    0x09: W,      0x0A: A,           0x0B: 4,  0x0C: Z,  0x0D: S,  0x0E: E,  0x0F: LEFT SHIFT
    /// 0x10: 5,    0x11: R,      0x12: D,           0x13: 6,  0x14: C,  0x15: F,  0x16: T,  0x17: X
    /// 0x18: 7,    0x19: Y,      0x1A: G,           0x1B: 8,  0x1C: B,  0x1D: H,  0x1E: U,  0x1F: V
    /// 0x20: 9,    0x21: I,      0x22: J,           0x23: 0,  0x24: M,  0x25: K,  0x26: O,  0x27: N
    /// 0x28: +,    0x29: P,      0x2A: L,           0x2B: -,  0x2C: .,  0x2D: :,  0x2E: @,  0x2F: ,
    /// 0x30: £,    0x31: *,      0x32: ;,           0x33: HOME,0x34: RIGHT SHIFT, 0x35: =, 0x36: ↑, 0x37: /
    /// 0x38: 1,    0x39: ←,      0x3A: CTRL,        0x3B: 2,  0x3C: SPACE, 0x3D: C=, 0x3E: Q,  0x3F: RUN/STOP
    /// </summary>
    public static readonly string[] KeyNames = new string[64]
    {
        "DEL", "RETURN", "CURSOR RIGHT", "F7", "F1", "F3", "F5", "CURSOR DOWN",
        "3", "W", "A", "4", "Z", "S", "E", "LEFT SHIFT",
        "5", "R", "D", "6", "C", "F", "T", "X",
        "7", "Y", "G", "8", "B", "H", "U", "V",
        "9", "I", "J", "0", "M", "K", "O", "N",
        "+", "P", "L", "-", ".", ":", "@", ",",
        "£", "*", ";", "HOME", "RIGHT SHIFT", "=", "↑", "/",
        "1", "←", "CTRL", "2", "SPACE", "C=", "Q", "RUN/STOP"
    };
}
