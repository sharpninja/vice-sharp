using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Pla;
using ViceSharp.Chips.VicIi;

namespace ViceSharp.Chips.Snapshots;

/// <summary>
/// FR-SNP-003. In-memory VIC-II, SID, and CIA execution state for one
/// machine image. It is not part of the serialized 16-byte-plus-64KB
/// snapshot. VICE snapshot modules keep this state beside RAM so replay can
/// resume mid-frame without executing the captured cycles again.
/// </summary>
public sealed class C64ChipExecutionImage
{
    private VicSnapshotState? _vic;
    private Mos6569.IrqPipelineReplayState _vicIrq;
    private Mos6569.RasterCompareReplayState _vicRasterCompare;
    private Mos6569.Reg11DelayReplayState _vicReg11Delay;
    private Mos6569.FrameStartReplayState _vicFrameStart;
    private Mos6569.EventLatchReplay _vicLatches;
    private Mos6569.BorderReplayState _vicBorder;
    private PixelSequencer.DrawReplay? _vicDraw;
    private byte[]? _vicVideo;
    private byte[]? _vicColor;
    private bool _vicBadLine;
    private int _vicRasterXPipe;
    private byte _vicSpriteDisplay;
    private int _vicPrefetchCycles;
    private byte _vicLastColorReg;
    private byte _vicLastColorValue;
    private bool _hasVicLastColor;
    private bool[]? _vicVerticalBorder;
    private bool[]? _vicVerticalCaptured;
    private bool[]? _vicHorizontalOpen;
    private bool[]? _vicLeftOpen;
    private bool[]? _vicRightOpen;
    private bool[]? _vicHorizontalCaptured;
    private Mos6569.SpriteReplay[]? _vicSprites;
    private byte[]? _frame;
    private CiaSnapshotState _cia1;
    private CiaSnapshotState _cia2;
    private bool _hasCia1;
    private bool _hasCia2;
    private byte _cia1SerialShifter;
    private int _cia1SerialEdges;
    private bool _cia1SerialDataIn;
    private byte _cia2SerialShifter;
    private int _cia2SerialEdges;
    private bool _cia2SerialDataIn;
    private int _cia1LoadA;
    private int _cia1CountA;
    private int _cia1LoadB;
    private int _cia1CountB;
    private int _cia2LoadA;
    private int _cia2CountA;
    private int _cia2LoadB;
    private int _cia2CountB;
    private bool _cia1RunA;
    private bool _cia1RunB;
    private bool _cia2RunA;
    private bool _cia2RunB;
    private byte _cia1Sdr;
    private byte _cia2Sdr;
    private Mos6526.TodReplayState _cia1Tod;
    private Mos6526.TodReplayState _cia2Tod;
    private Mos6526.SerialOutputReplayState _cia1SerialOut;
    private Mos6526.SerialOutputReplayState _cia2SerialOut;
    private Mos6526.IrqPipelineReplayState _cia1IrqPipeline;
    private Mos6526.IrqPipelineReplayState _cia2IrqPipeline;
    private SidExecutionState? _sid;
    private byte[]? _cpu;
    private bool _hasPortCharge;
    private Mos906114.PortChargeReplay _portCharge;

    /// <summary>
    /// Copies chip execution state from <paramref name="machine"/>.
    /// Missing C64 chips are omitted.
    /// </summary>
    /// <param name="machine">Machine whose VIC-II, SID, and CIA are captured.</param>
    /// <returns>The image <see cref="Restore"/> writes back.</returns>
    public static C64ChipExecutionImage Capture(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        var image = new C64ChipExecutionImage();
        if (machine.Devices.GetByRole(DeviceRole.VideoChip) is Mos6569 vic)
        {
            image._vic = vic.CaptureSnapshotState();
            image._vicIrq = vic.CaptureIrqPipeline();
            image._vicRasterCompare = vic.CaptureRasterCompare();
            image._vicReg11Delay = vic.CaptureReg11Delay();
            image._vicFrameStart = vic.CaptureFrameStart();
            image._vicLatches = vic.CaptureEventLatches();
            image._vicBorder = vic.CaptureBorder();
            (image._vicDraw, image._vicVideo, image._vicColor, image._vicBadLine, image._vicRasterXPipe, image._vicSpriteDisplay, image._vicPrefetchCycles) = vic.CaptureRendering();
            (image._vicVerticalBorder, image._vicVerticalCaptured, image._vicHorizontalOpen, image._vicLeftOpen, image._vicRightOpen, image._vicHorizontalCaptured) = vic.CaptureBorderLines();
            image._vicSprites = vic.CaptureSprites();
            image._frame = vic.FrameBuffer.ToArray();
            image._vicLastColorReg = vic.VicLastColorRegWrite;
            image._vicLastColorValue = vic.VicLastColorValueWrite;
            image._hasVicLastColor = true;
        }

        if (machine.Devices.GetByRole(DeviceRole.Cia1) is Mos6526 cia1)
        {
            image._cia1 = cia1.CaptureSnapshotState();
            (image._cia1SerialShifter, image._cia1SerialEdges, image._cia1SerialDataIn) = cia1.CaptureSerialInput();
            (image._cia1LoadA, image._cia1CountA, image._cia1LoadB, image._cia1CountB) = cia1.CaptureTimerPipeline();
            (image._cia1RunA, image._cia1RunB) = cia1.CaptureTimerRunning();
            image._cia1Sdr = cia1.CaptureSdr();
            image._cia1Tod = cia1.CaptureTod();
            image._cia1SerialOut = cia1.CaptureSerialOutput();
            image._cia1IrqPipeline = cia1.CaptureIrqPipeline();
            image._hasCia1 = true;
        }

        if (machine.Devices.GetByRole(DeviceRole.Cia2) is Mos6526 cia2)
        {
            image._cia2 = cia2.CaptureSnapshotState();
            (image._cia2SerialShifter, image._cia2SerialEdges, image._cia2SerialDataIn) = cia2.CaptureSerialInput();
            (image._cia2LoadA, image._cia2CountA, image._cia2LoadB, image._cia2CountB) = cia2.CaptureTimerPipeline();
            (image._cia2RunA, image._cia2RunB) = cia2.CaptureTimerRunning();
            image._cia2Sdr = cia2.CaptureSdr();
            image._cia2Tod = cia2.CaptureTod();
            image._cia2SerialOut = cia2.CaptureSerialOutput();
            image._cia2IrqPipeline = cia2.CaptureIrqPipeline();
            image._hasCia2 = true;
        }

        if (machine.Devices.GetByRole(DeviceRole.AudioChip) is Sid6581 sid)
            image._sid = sid.CaptureExecutionState();

        if (machine.Devices.GetByRole(DeviceRole.Cpu) is Mos6502 cpu)
            image._cpu = cpu.CaptureExecutionImage();

        if (machine.Devices.GetByRole(DeviceRole.Pla) is Mos906114 pla)
        {
            image._portCharge = pla.CapturePortCharge();
            image._hasPortCharge = true;
        }

        return image;
    }

    /// <summary>
    /// Writes this image onto <paramref name="machine"/> after register restores.
    /// Bus writes of captured IO bytes do not recreate raster phase or OSC3.
    /// </summary>
    /// <param name="machine">Machine that receives the chip execution state.</param>
    public void Restore(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        if (_vic is { } vicState && machine.Devices.GetByRole(DeviceRole.VideoChip) is Mos6569 vic)
        {
            vic.InjectSnapshotState(
                vicState.Registers,
                vicState.RasterLine,
                vicState.InLineCycle,
                vicState.AllowBadLines,
                vicState.IdleState,
                vicState.VideoCounter,
                vicState.VideoCounterBase,
                vicState.RowCounter,
                vicState.VideoMatrixLineIndex,
                vicState.RefreshCounter,
                vicState.SpriteDmaActiveMask);
            vic.InjectIrqPipeline(_vicIrq);
            vic.InjectReg11Delay(_vicReg11Delay);
            vic.InjectRasterCompare(_vicRasterCompare);
            vic.InjectFrameStart(_vicFrameStart);
            vic.InjectEventLatches(_vicLatches);
            vic.InjectBorder(_vicBorder);
            if (_vicDraw is not null && _vicVideo is not null && _vicColor is not null)
                vic.InjectRendering(_vicDraw, _vicVideo, _vicColor, _vicBadLine, _vicRasterXPipe, _vicSpriteDisplay, _vicPrefetchCycles);
            if (_vicVerticalBorder is not null && _vicVerticalCaptured is not null && _vicHorizontalOpen is not null
                && _vicLeftOpen is not null && _vicRightOpen is not null && _vicHorizontalCaptured is not null)
            {
                vic.InjectBorderLines(
                    _vicVerticalBorder,
                    _vicVerticalCaptured,
                    _vicHorizontalOpen,
                    _vicLeftOpen,
                    _vicRightOpen,
                    _vicHorizontalCaptured);
            }

            if (_vicSprites is not null)
                vic.InjectSprites(_vicSprites);
            if (_frame is not null && vic.FrameBuffer.Length == _frame.Length)
                _frame.AsSpan().CopyTo(vic.FrameBuffer);
            // VICE restores last_color_reg after the register file. The
            // VisibleIo sweep writes $D02E last and leaves that pair pending.
            if (_hasVicLastColor)
            {
                vic.VicLastColorRegWrite = _vicLastColorReg;
                vic.VicLastColorValueWrite = _vicLastColorValue;
            }
        }

        if (_hasCia1 && machine.Devices.GetByRole(DeviceRole.Cia1) is Mos6526 cia1)
        {
            InjectCia(cia1, _cia1);
            cia1.InjectIrqPipeline(_cia1IrqPipeline);
            cia1.InjectSerialInput(_cia1SerialShifter, _cia1SerialEdges, _cia1SerialDataIn);
            cia1.InjectTimerPipeline(_cia1LoadA, _cia1CountA, _cia1LoadB, _cia1CountB);
            cia1.InjectTimerRunning(_cia1RunA, _cia1RunB);
            cia1.InjectSdr(_cia1Sdr);
            cia1.InjectTod(_cia1Tod);
            cia1.InjectSerialOutput(_cia1SerialOut);
        }

        if (_hasCia2 && machine.Devices.GetByRole(DeviceRole.Cia2) is Mos6526 cia2)
        {
            InjectCia(cia2, _cia2);
            cia2.InjectIrqPipeline(_cia2IrqPipeline);
            cia2.InjectSerialInput(_cia2SerialShifter, _cia2SerialEdges, _cia2SerialDataIn);
            cia2.InjectTimerPipeline(_cia2LoadA, _cia2CountA, _cia2LoadB, _cia2CountB);
            cia2.InjectTimerRunning(_cia2RunA, _cia2RunB);
            cia2.InjectSdr(_cia2Sdr);
            cia2.InjectTod(_cia2Tod);
            cia2.InjectSerialOutput(_cia2SerialOut);
        }

        if (_sid is { } sidState && machine.Devices.GetByRole(DeviceRole.AudioChip) is Sid6581 sid)
            sid.InjectExecutionState(sidState);

        // After the public PC setter. That setter restarts the in-flight
        // instruction. The image puts the micro-state back.
        if (_cpu is not null && machine.Devices.GetByRole(DeviceRole.Cpu) is Mos6502 cpu)
            cpu.RestoreExecutionImage(_cpu);

        // After ApplyProcessorPort. Those DDR and data writes do not
        // recreate a charge that was already counting down.
        if (_hasPortCharge && machine.Devices.GetByRole(DeviceRole.Pla) is Mos906114 pla)
            pla.InjectPortCharge(_portCharge);
    }

    private static void InjectCia(Mos6526 cia, CiaSnapshotState state) => cia.InjectSnapshotState(
        state.PortA,
        state.PortB,
        state.DdrA,
        state.DdrB,
        state.TimerACounter,
        state.TimerALatch,
        state.TimerBCounter,
        state.TimerBLatch,
        state.Cra,
        state.Crb,
        state.InterruptFlags,
        state.IrqMask);
}
