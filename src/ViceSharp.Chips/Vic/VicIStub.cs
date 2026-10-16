using ViceSharp.Abstractions;

namespace ViceSharp.Chips.Vic;

/// <summary>
/// Slice A stub for the MOS 6560/6561 VIC-I chip. Implements
/// <see cref="IVideoChip"/> with a solid black frame and simple raster
/// advancement so the host frame path and clock wiring can attach before
/// full character/bitmap rendering lands (FR-VIC20-001).
/// </summary>
/// <remarks>
/// Machine-agnostic stub: timing parameters come from the architecture
/// profile via <see cref="ConfigureTiming"/>. Register window is the
/// standard VIC-I 16-byte block at the board base address (VIC-20 $9000).
/// </remarks>
public sealed class VicIStub : IVideoChip, IAddressSpace, IInterruptSource
{
    private readonly IInterruptLine? _irqLine;
    private readonly byte[] _regs = new byte[16];
    private int _cycleInLine;
    private int _cyclesPerLine = 71;
    private int _totalLines = 312;
    private int _visibleLines = 284;
    private int _frameWidth = 224;
    private int _frameHeight = 284;
    private byte[] _frameBuffer;
    private ushort _rasterLine;

    public VicIStub(IInterruptLine? irqLine = null)
    {
        _irqLine = irqLine;
        Id = new DeviceId(0x0003);
        SourceId = Id;
        Name = "MOS 656x VIC-I (stub)";
        BaseAddress = 0x9000;
        Size = 0x0010;
        _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
    }

    /// <inheritdoc />
    public DeviceId Id { get; init; }

    /// <inheritdoc />
    public string Name { get; init; }

    /// <inheritdoc />
    public DeviceId SourceId { get; init; }

    /// <summary>Base address of the VIC-I register window on the board bus.</summary>
    public ushort BaseAddress { get; init; }

    /// <summary>Size of the VIC-I register window (16 bytes).</summary>
    public ushort Size { get; init; }

    /// <inheritdoc />
    public uint ClockDivisor => 1;

    /// <inheritdoc />
    public ClockPhase Phase => ClockPhase.Phi2;

    /// <inheritdoc />
    public IReadOnlyList<IInterruptLine> ConnectedLines =>
        _irqLine is null ? Array.Empty<IInterruptLine>() : new[] { _irqLine };

    /// <inheritdoc />
    public ushort CurrentRasterLine => _rasterLine;

    /// <inheritdoc />
    public int CyclesPerLine => _cyclesPerLine;

    /// <inheritdoc />
    public int VisibleLines => _visibleLines;

    /// <inheritdoc />
    public int TotalLines => _totalLines;

    /// <inheritdoc />
    public bool IsVBlank => _rasterLine >= _visibleLines;

    /// <inheritdoc />
    public byte[] FrameBuffer => _frameBuffer;

    /// <inheritdoc />
    public int FrameWidth => _frameWidth;

    /// <inheritdoc />
    public int FrameHeight => _frameHeight;

    /// <inheritdoc />
    public event EventHandler? FrameCompleted;

    /// <summary>
    /// Apply profile timing (PAL 71x312 or NTSC 65x261) and rebuild the black framebuffer.
    /// </summary>
    public void ConfigureTiming(int cyclesPerLine, int totalLines, int visibleLines, int frameWidth, int frameHeight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cyclesPerLine, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(totalLines, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(visibleLines, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(frameWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(frameHeight, 1);

        _cyclesPerLine = cyclesPerLine;
        _totalLines = totalLines;
        _visibleLines = Math.Min(visibleLines, totalLines);
        _frameWidth = frameWidth;
        _frameHeight = frameHeight;
        _frameBuffer = new byte[_frameWidth * _frameHeight * 4];
    }

    /// <inheritdoc />
    public bool HandlesAddress(ushort address)
        => address >= BaseAddress && address < (ushort)(BaseAddress + Size);

    /// <inheritdoc />
    public byte Read(ushort address)
        => _regs[(address - BaseAddress) & 0x0F];

    /// <inheritdoc />
    public void Write(ushort address, byte value)
        => _regs[(address - BaseAddress) & 0x0F] = value;

    /// <inheritdoc />
    public byte Peek(ushort address) => Read(address);

    /// <inheritdoc />
    public void Tick()
    {
        _cycleInLine++;
        if (_cycleInLine < _cyclesPerLine)
            return;

        _cycleInLine = 0;
        _rasterLine++;
        if (_rasterLine < _totalLines)
            return;

        _rasterLine = 0;
        FrameCompleted?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Reset()
    {
        Array.Clear(_regs);
        _cycleInLine = 0;
        _rasterLine = 0;
        Array.Clear(_frameBuffer);
    }
}
