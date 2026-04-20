using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Video;

public sealed partial class VicII : IClockedDevice, IAddressSpace, IVideoChip
{
    public DeviceId Id => new DeviceId(0x0002);
    public string Name => "VIC-II Video Controller";
    public uint ClockDivisor => 1;
    public ClockPhase Phase => ClockPhase.Phi1;

    public ushort CurrentRasterLine => (ushort)_rasterY;
    public int CyclesPerLine => 63;
    public int VisibleLines => 200;
    public int TotalLines => 312;
    public bool IsVBlank => _rasterY >= 0xF8;
    
    // Framebuffer (placeholder - full implementation would render actual video)
    private readonly byte[] _frameBuffer = new byte[384 * 272 * 4];
    
    public byte[] FrameBuffer => _frameBuffer;
    public int FrameWidth => 384;
    public int FrameHeight => 272;
    public event EventHandler? FrameCompleted;
    
    private readonly IBus _bus;
    private readonly IFrameSink _frameSink;

    // VIC-II Registers
    private byte[] _registers = new byte[0x40];

    // Raster state
    private int _rasterX;
    private int _rasterY;
    private int _cycle;
    private int _badLineCounter;

    // Screen state
    private byte[] _screenRam = new byte[1000];
    private byte[] _colorRam = new byte[1000];
    private byte[] _spriteData = new byte[64 * 8];

    // Sprite state
    private Sprite[] _sprites = new Sprite[8];

    private struct Sprite
    {
        public ushort Address;
        public byte X;
        public byte Y;
        public bool Enabled;
        public bool MultiColor;
        public bool ExpandX;
        public bool ExpandY;
        public bool Priority;
        public byte Color;
    }

    public VicII(IBus bus, IFrameSink frameSink)
    {
        _bus = bus;
        _frameSink = frameSink;
    }

    public void Tick()
    {
        _cycle++;

        if (_cycle == 1)
        {
            // Start of line
            _rasterX = 0;
            _registers[0x12] = (byte)_rasterY;
            if (_rasterY > 0xFF)
                _registers[0x11] |= 0x80;
            else
                _registers[0x11] &= 0x7F;
        }

        if (_rasterX < 403)
        {
            // Visible area
            ProcessCycle();
        }

        _rasterX++;

        if (_rasterX >= 403)
        {
            // End of line
            _rasterX = 0;
            _rasterY++;
            _cycle = 0;

            if (_rasterY >= 312)
            {
                // End of frame
                _rasterY = 0;
                PresentFrame();
            }
        }
    }

    private void ProcessCycle()
    {
        // Bad line detection
        bool isBadLine = (_rasterY >= 0x30 && _rasterY <= 0xF7) &&
                         ((_rasterY & 7) == (_registers[0x11] & 7));

        if (isBadLine && _badLineCounter < 40)
        {
            // Fetch character and color data
            byte charCode = _screenRam[_badLineCounter];
            byte colorCode = _colorRam[_badLineCounter];
            _badLineCounter++;
        }

        // Sprite DMA handling
        for (int i = 0; i < 8; i++)
        {
            if (_sprites[i].Enabled &&
                _sprites[i].Y == _rasterY &&
                _rasterX == i * 3 + 15)
            {
                // Fetch sprite data
                _spriteData[i * 8 + (_rasterY - _sprites[i].Y)] = _bus.Read((ushort)(_sprites[i].Address + (_rasterY - _sprites[i].Y)));
            }
        }
    }

    private void PresentFrame()
    {
        Span<byte> frameBuffer = stackalloc byte[320 * 200 * 4];
        _frameSink.PresentFrame(frameBuffer);
        _badLineCounter = 0;
        FrameCompleted?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        Array.Clear(_registers);
        _rasterX = 0;
        _rasterY = 0;
        _cycle = 0;
        _badLineCounter = 0;
    }

    public byte Read(ushort address)
    {
        int register = address & 0x3F;
        return _registers[register];
    }

    public void Write(ushort address, byte value)
    {
        int register = address & 0x3F;
        _registers[register] = value;

        // Handle register side effects
        switch (register)
        {
            case 0x00:
            case 0x01:
                // Sprite X positions
                int spriteIndex = register;
                _sprites[spriteIndex].X = value;
                break;
            case 0x02:
                // Sprite Y position (first 8 sprites)
                for (int i = 0; i < 8; i++)
                {
                    _sprites[i].Y = _registers[0x02 + i];
                }
                break;
            case 0x10:
                // Sprite enable
                for (int i = 0; i < 8; i++)
                {
                    _sprites[i].Enabled = (value & (1 << i)) != 0;
                }
                break;
            case 0x15:
                // Sprite expand X
                for (int i = 0; i < 8; i++)
                {
                    _sprites[i].ExpandX = (value & (1 << i)) != 0;
                }
                break;
            case 0x17:
                // Sprite expand Y
                for (int i = 0; i < 8; i++)
                {
                    _sprites[i].ExpandY = (value & (1 << i)) != 0;
                }
                break;
            case 0x1B:
                // Sprite multicolor
                for (int i = 0; i < 8; i++)
                {
                    _sprites[i].MultiColor = (value & (1 << i)) != 0;
                }
                break;
            case 0x1C:
                // Sprite priority
                for (int i = 0; i < 8; i++)
                {
                    _sprites[i].Priority = (value & (1 << i)) != 0;
                }
                break;
            case 0x27:
                // Sprite colors
                for (int i = 0; i < 8; i++)
                {
                    _sprites[i].Color = _registers[0x27 + i];
                }
                break;
            case 0x0F:
                // Sprite MSB X coordinates
                for (int i = 0; i < 8; i++)
                {
                    _sprites[i].X = (byte)(_sprites[i].X | (byte)(((value >> i) & 1) << 8));
                }
                break;
            case 0x1F:
                // Sprite pointer base address
                for (int i = 0; i < 8; i++)
                {
                    _sprites[i].Address = (ushort)(_registers[0x1F + i] << 6);
                }
                break;
        }
    }

    public byte Peek(ushort address)
    {
        return Read(address);
    }

    public bool HandlesAddress(ushort address)
    {
        return address >= 0xD000 && address <= 0xD3FF;
    }
}