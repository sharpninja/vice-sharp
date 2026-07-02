using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Core.Input;
using ViceSharp.Chips.IEC;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using ViceSharp.Core.Media;
using ViceSharp.Core.Wiring;

namespace ViceSharp.TestHarness;

/// <summary>
/// Lockstep cycle accurate validator comparing ViceSharp against original VICE
/// </summary>
public sealed class LockstepValidator : IDisposable
{
    private const ushort KernalCloseVector = 0xFFC3;
    private static readonly string[] C64Drive8LoadSequence =
    [
        "L", "O", "A", "D", "\"", "*", "\"", ",", "8", ",", "1", "Return"
    ];

    private readonly IMachine _machine;
    private readonly IViceNative _native;
    private readonly SystemCoordinator? _coordinator;
    private readonly Queue<string> _recentTrace = new();
    private readonly LockFreePubSub _pubSub = new();
    private readonly bool _recordRecentTrace;
    private ushort _kernalCloseTarget;
    private readonly SubscriptionHandle _cpuControlTransferSubscription;
    private long _cycleCount;
    private long _lastNativeDelta;
    private bool _hasPendingKernalCloseCall;
    private CpuControlTransferEvent _pendingKernalCloseCall;
    private BasicCommandAutomation? _basicCommandAutomation;
    private bool _nativeSawLoadingPrompt;
    private long _nextNativeLoadProbeCycle;

    public LockstepValidator(
        string modelSelector = "c64",
        byte[]? cartridgeImage = null,
        CartridgeMappingMode cartridgeMappingMode = CartridgeMappingMode.Auto,
        byte[]? diskImage = null,
        string? diskPath = null,
        byte diskDriveNumber = 8,
        bool recordRecentTrace = true)
    {
        if (diskImage is not null || !string.IsNullOrWhiteSpace(diskPath))
        {
            if (diskImage is null)
                throw new ArgumentException("Managed disk image bytes are required when a native disk path is supplied.", nameof(diskImage));
            if (string.IsNullOrWhiteSpace(diskPath))
                throw new ArgumentException("Native disk path is required when managed disk image bytes are supplied.", nameof(diskPath));

            var rig = CreateC64WithTrueDrive(modelSelector, diskPath, diskDriveNumber);
            _machine = rig.Host;
            _coordinator = rig.Coordinator;
        }
        else
        {
            _machine = MachineTestFactory.CreateC64Machine(modelSelector);
        }

        _native = ViceNative.CreateInstance(modelSelector);
        _recordRecentTrace = recordRecentTrace;
        _kernalCloseTarget = KernalCloseVector;

        if (_machine.Devices.GetByRole(DeviceRole.Cpu) is Mos6502 cpu)
        {
            cpu.ConnectPubSub(_pubSub);
            _cpuControlTransferSubscription = _pubSub.Subscribe<CpuControlTransferEvent>(
                CpuControlTransferEvent.Topic,
                OnCpuControlTransfer);
        }

        if (cartridgeImage is not null)
        {
            var cartridgePort = _machine.Devices.GetAll<ICartridgePort>().SingleOrDefault()
                ?? throw new InvalidOperationException($"Machine '{modelSelector}' does not expose a cartridge port.");

            cartridgePort.AttachCartridge(cartridgeImage, cartridgeMappingMode);
            _native.AttachCartridge(cartridgeImage, cartridgeMappingMode);
        }

        if (diskImage is not null || !string.IsNullOrWhiteSpace(diskPath))
        {
            if (_coordinator is null)
            {
                var drive = _machine.Devices.GetAll<IFloppyDrive>().SingleOrDefault(drive => drive.DriveNumber == diskDriveNumber)
                    ?? throw new InvalidOperationException($"Machine '{modelSelector}' does not expose drive {diskDriveNumber}.");

                drive.InsertDisk(diskImage);
            }

            _native.AttachDisk(diskDriveNumber, 0, diskPath!);
        }
    }

    public void QueueC64Drive8LoadCommand(int cyclesPerFrame = 19656)
    {
        QueueC64BasicCommand(C64Drive8LoadSequence, C64HostKeyboardMapper.DefaultFallbackMap, cyclesPerFrame);
    }

    public void QueueC64Drive8LoadCommand(string vkmPath, int cyclesPerFrame)
    {
        QueueC64BasicCommand(C64Drive8LoadSequence, vkmPath, cyclesPerFrame);
    }

    public void QueueC64BasicCommand(IReadOnlyList<string> keySequence, string vkmPath, int cyclesPerFrame)
    {
        if (keySequence.Count == 0)
            throw new ArgumentException("The BASIC command key sequence cannot be empty.", nameof(keySequence));
        if (cyclesPerFrame <= 0)
            throw new ArgumentOutOfRangeException(nameof(cyclesPerFrame), cyclesPerFrame, "Cycles per frame must be positive.");

        var parseResult = C64VkmParser.Load(vkmPath);
        if (parseResult.HasErrors)
        {
            var errors = parseResult.Diagnostics
                .Where(diagnostic => diagnostic.Severity == C64VkmDiagnosticSeverity.Error)
                .Select(diagnostic => $"{diagnostic.Path}:{diagnostic.LineNumber}: {diagnostic.Message}");
            throw new InvalidOperationException($"VKM file '{vkmPath}' has parse errors: {string.Join("; ", errors)}");
        }

        QueueC64BasicCommand(keySequence, parseResult.KeyboardMap, cyclesPerFrame);
    }

    public void QueueC64BasicCommand(IReadOnlyList<string> keySequence, IKeyboardInputMap keyboardMap, int cyclesPerFrame)
    {
        if (keySequence.Count == 0)
            throw new ArgumentException("The BASIC command key sequence cannot be empty.", nameof(keySequence));
        if (cyclesPerFrame <= 0)
            throw new ArgumentOutOfRangeException(nameof(cyclesPerFrame), cyclesPerFrame, "Cycles per frame must be positive.");

        var mapSelection = _machine.Devices.GetAll<IKeyboardInputMapSelection>().SingleOrDefault()
            ?? throw new InvalidOperationException("The managed machine does not expose keyboard map selection.");
        var keyboardInput = _machine.Devices.GetAll<IMachineKeyboardInput>().SingleOrDefault()
            ?? throw new InvalidOperationException("The managed machine does not expose host keyboard input.");

        mapSelection.SelectKeyboardMap(keyboardMap);
        _basicCommandAutomation = new BasicCommandAutomation(
            keySequence,
            keyboardMap,
            keyboardInput,
            _native,
            cyclesPerFrame);
    }

    /// <summary>
    /// Run lockstep comparison for specified number of cycles
    /// </summary>
    public ValidationReport Run(long maxCycles)
    {
        ResetManaged();
        _native.Reset();
        _recentTrace.Clear();
        _hasPendingKernalCloseCall = false;
        _basicCommandAutomation?.Reset();
        _kernalCloseTarget = ResolveKernalJumpTarget(_machine.Bus, KernalCloseVector);
        if (_recordRecentTrace)
            RecordTrace(0);

        if (!ValidateState())
            return ValidationReport.Fail(0, 0, GetStateDiff());

        long prevNativeCycle = 0;
        for (_cycleCount = 0; _cycleCount < maxCycles; _cycleCount++)
        {
            _native.Step();
            long nativeCycle = _native.GetState().Cycle;
            long nativeDelta = nativeCycle - prevNativeCycle;
            prevNativeCycle = nativeCycle;
            _lastNativeDelta = nativeDelta;

            for (long j = 0; j < nativeDelta; j++)
                StepManaged();

            if (_recordRecentTrace)
                RecordTrace(_cycleCount + 1);

            if (!ValidateState())
            {
                var mismatchCycle = _cycleCount + 1;
                return ValidationReport.Fail(mismatchCycle, mismatchCycle, GetStateDiff());
            }
        }

        return ValidationReport.Pass(maxCycles);
    }

    /// <summary>
    /// Snapshot-staged lockstep run (TR-LOCKSTEP-VSF-001): resumes <paramref name="snapshotPath"/>
    /// in the NATIVE machine (throws with the shim's rc/snapshot_last_error when it cannot
    /// resume), stages the MANAGED machine from the resumed native state via
    /// <see cref="StageManagedFromNative"/> (CPU registers, 64K RAM, CPU port $00/$01, VIC
    /// register file + convention-corrected raster phase), then reuses the same per-cycle
    /// compare loop as <see cref="Run"/>. Without capture the run returns at the FIRST
    /// divergence, exactly like <see cref="Run"/>. When <paramref name="videoOutPath"/> is
    /// set, the managed side's video (VIC full-frame BGRA at each PAL frame boundary) and
    /// audio (SID samples at 44.1 kHz mono) are muxed through the external-ffmpeg Media
    /// recorder into that file; the run then executes the full cycle budget even past the
    /// first divergence so the capture covers the whole run, while the report still carries
    /// the FIRST divergence. Throws with a clear message when ffmpeg is unavailable.
    /// </summary>
    public ValidationReport RunFromSnapshot(string snapshotPath, long maxCycles, string? videoOutPath = null)
    {
        _native.Reset();
        var rc = _native.ReadSnapshot(snapshotPath);
        if (rc != 0)
        {
            throw new InvalidOperationException(
                $"Native VICE could not resume snapshot '{snapshotPath}': rc={rc}, " +
                $"snapshot_last_error={ViceNative.SnapshotLastError()}.");
        }

        ResetManaged();
        StageManagedFromNative(_machine, _native);
        _recentTrace.Clear();
        _hasPendingKernalCloseCall = false;
        _cycleCount = 0;
        if (_recordRecentTrace)
            RecordTrace(0);

        using var capture = videoOutPath is null ? null : ManagedAvCapture.Start(_machine, videoOutPath);

        ValidationReport? firstMismatch = null;
        if (!ValidateState())
        {
            firstMismatch = ValidationReport.Fail(0, 0, GetStateDiff());
            if (capture is null)
                return firstMismatch;
        }

        long prevNativeCycle = 0;
        for (_cycleCount = 0; _cycleCount < maxCycles; _cycleCount++)
        {
            _native.Step();
            long nativeCycle = _native.GetState().Cycle;
            long nativeDelta = nativeCycle - prevNativeCycle;
            prevNativeCycle = nativeCycle;
            _lastNativeDelta = nativeDelta;

            for (long j = 0; j < nativeDelta; j++)
            {
                StepManaged();
                capture?.OnManagedCycle();
            }

            if (_recordRecentTrace)
                RecordTrace(_cycleCount + 1);

            if (firstMismatch is null && !ValidateState())
            {
                var mismatchCycle = _cycleCount + 1;
                firstMismatch = ValidationReport.Fail(mismatchCycle, mismatchCycle, GetStateDiff());
                if (capture is null)
                    return firstMismatch;
            }
        }

        return firstMismatch ?? ValidationReport.Pass(maxCycles);
    }

    /// <summary>
    /// Stages a managed machine from a native instance that has already resumed a .vsf,
    /// using exactly the staging RasterBarLockstepTests proves works (TR-LOCKSTEP-VSF-001):
    /// reset, copy the full 64K RAM from the native side, drive the CPU port $00/$01
    /// through the bus, inject the CPU register file, and seed the VIC register file +
    /// raster phase via <see cref="Mos6569.InjectSnapshotState"/>. The in-line cycle is
    /// seeded convention-corrected ((raster_cycle + 1) mod 63) because the managed Tick()
    /// increments RasterX before its IRQ/line-wrap checks, so managed RasterX reads
    /// native raster_cycle + 1 at the same physical point.
    /// </summary>
    internal static void StageManagedFromNative(IMachine machine, IViceNative native)
    {
        machine.Reset();

        if (machine.Devices.GetByRole(DeviceRole.SystemRam) is not IMemory ram)
            throw new InvalidOperationException("Managed machine does not expose system RAM as IMemory.");
        var span = ram.Span;
        for (var address = 0; address < 0x10000; address++)
            span[address] = native.PeekRam((ushort)address);

        machine.Bus.Write(0x0000, span[0x0000]);
        machine.Bus.Write(0x0001, span[0x0001]);

        if (machine.Devices.GetByRole(DeviceRole.Cpu) is not Mos6502 cpu)
            throw new InvalidOperationException("Managed machine does not expose an Mos6502 CPU.");
        var cpu0 = native.GetState();
        cpu.A = cpu0.A;
        cpu.X = cpu0.X;
        cpu.Y = cpu0.Y;
        cpu.S = cpu0.S;
        cpu.PC = cpu0.PC;
        cpu.P = cpu0.P;

        if (machine.Devices.GetByRole(DeviceRole.VideoChip) is not Mos6569 vic)
            throw new InvalidOperationException("Managed machine does not expose Mos6569.");
        var vic0 = native.GetVicState();
        var registers = vic0.Registers
            ?? throw new InvalidOperationException("Native VIC registers are unavailable for snapshot staging.");
        vic.InjectSnapshotState(registers, vic0.RasterLine, (byte)((vic0.RasterCycle + 1) % 63));
    }

    /// <summary>
    /// Captures a per-cycle CPU run log (cycle, PC, A, X, Y, S, P) from NATIVE VICE
    /// resumed from <paramref name="snapshotPath"/> (TR-LOCKSTEP-VSF-001): one
    /// <see cref="CpuRunLogEntry"/> is recorded BEFORE each of the
    /// <paramref name="cycles"/> native master-cycle steps, then saved via
    /// <see cref="CpuRunLog.Save"/> so the run can be replayed offline against the
    /// managed core. Throws with the shim's rc/snapshot_last_error when the snapshot
    /// cannot resume.
    /// </summary>
    public static void SaveNativeRunLog(string snapshotPath, int cycles, string outPath)
    {
        using var native = ViceNative.CreateInstance("c64");
        native.Reset();
        var rc = native.ReadSnapshot(snapshotPath);
        if (rc != 0)
        {
            throw new InvalidOperationException(
                $"Native VICE could not resume snapshot '{snapshotPath}': rc={rc}, " +
                $"snapshot_last_error={ViceNative.SnapshotLastError()}.");
        }

        var entries = new List<CpuRunLogEntry>(cycles);
        for (var i = 0; i < cycles; i++)
        {
            var state = native.GetState();
            entries.Add(new CpuRunLogEntry(state.Cycle, state.PC, state.A, state.X, state.Y, state.S, state.P));
            native.Step();
        }

        CpuRunLog.Save(
            outPath,
            entries,
            $"native x64sc shim; snapshot={Path.GetFileName(snapshotPath)}; cycles={cycles}");
    }

    /// <summary>
    /// Captures a per-cycle CPU run log from the MANAGED machine resumed from
    /// <paramref name="snapshotPath"/> (TR-LOCKSTEP-VSF-001), using the same staging
    /// as the parity theory (<see cref="StageManagedFromNative"/>; a short-lived
    /// native instance decodes the .vsf because the VIC-II module is a
    /// version-specific blob only the shim can parse). One entry is recorded BEFORE
    /// each of the <paramref name="cycles"/> managed clock steps so both sides can be
    /// captured for offline diffing.
    /// </summary>
    public static void SaveManagedRunLog(string snapshotPath, int cycles, string outPath)
    {
        using var native = ViceNative.CreateInstance("c64");
        native.Reset();
        var rc = native.ReadSnapshot(snapshotPath);
        if (rc != 0)
        {
            throw new InvalidOperationException(
                $"Native VICE could not resume snapshot '{snapshotPath}': rc={rc}, " +
                $"snapshot_last_error={ViceNative.SnapshotLastError()}.");
        }

        var machine = MachineTestFactory.CreateC64Machine("c64");
        StageManagedFromNative(machine, native);

        var entries = new List<CpuRunLogEntry>(cycles);
        for (var i = 0; i < cycles; i++)
        {
            var state = machine.GetState();
            entries.Add(new CpuRunLogEntry(state.Cycle, state.PC, state.A, state.X, state.Y, state.S, state.P));
            machine.Clock.Step();
        }

        CpuRunLog.Save(
            outPath,
            entries,
            $"vice-sharp managed C64; snapshot={Path.GetFileName(snapshotPath)}; cycles={cycles}");
    }

    /// <summary>
    /// Managed-side audio/video capture for <see cref="RunFromSnapshot"/> (FR-MED-004
    /// reuse): frames come from the managed VIC's committed full-frame BGRA buffer at
    /// each <see cref="IVideoChip.FrameCompleted"/> boundary, audio comes from the
    /// managed SID via <see cref="IAudioChip.GenerateSample"/> (a pure read of the
    /// committed chain state, so sampling never perturbs determinism) at a fractional
    /// 44.1 kHz cadence (PAL 985248 Hz / 44100 ~= one sample per 22.34 cycles), and
    /// both feeds mux through the existing external-ffmpeg Media recorder.
    /// </summary>
    private sealed class ManagedAvCapture : IDisposable
    {
        private const int SampleRate = 44100;
        private const double PalClockHz = 985248.0;
        private const double PalFrameRate = PalClockHz / (63.0 * 312.0);

        private readonly FfmpegVideoRecorder _recorder;
        private readonly IVideoChip _vic;
        private readonly IAudioChip _sid;
        private readonly EventHandler _frameHandler;
        private readonly short[] _audioBatch = new short[256];
        private readonly double _cyclesPerSample = PalClockHz / SampleRate;
        private double _sampleAccumulator;
        private int _audioBatchLength;
        private bool _stopped;

        private ManagedAvCapture(FfmpegVideoRecorder recorder, IVideoChip vic, IAudioChip sid)
        {
            _recorder = recorder;
            _vic = vic;
            _sid = sid;
            _frameHandler = (_, _) => _recorder.CaptureFrame(_vic.FrameBuffer, _vic.FrameWidth, _vic.FrameHeight);
        }

        public static ManagedAvCapture Start(IMachine machine, string videoOutPath)
        {
            var ffmpegPath = FfmpegLocator.Locate()
                ?? throw new InvalidOperationException(
                    "Lockstep video capture requires ffmpeg, which was not found. " +
                    "Install ffmpeg on PATH or point VICESHARP_FFMPEG at the binary.");

            if (machine.Devices.GetByRole(DeviceRole.VideoChip) is not IVideoChip vic)
                throw new InvalidOperationException("Managed machine does not expose a video chip; cannot capture video.");
            if (machine.Devices.GetByRole(DeviceRole.AudioChip) is not IAudioChip sid)
                throw new InvalidOperationException("Managed machine does not expose an audio chip; cannot capture audio.");

            var extension = Path.GetExtension(videoOutPath).TrimStart('.');
            if (!FfmpegVideoFormats.TryGet(extension, out var format))
            {
                throw new InvalidOperationException(
                    $"Unsupported capture container '.{extension}'. " +
                    $"Supported: {string.Join(", ", FfmpegVideoFormats.All.Select(f => f.Id))}.");
            }

            var recorder = new FfmpegVideoRecorder(
                ffmpegPath,
                format,
                vic.FrameWidth,
                vic.FrameHeight,
                PalFrameRate,
                videoOutPath,
                includeAudio: true,
                SampleRate,
                channels: 1);
            recorder.Start();

            var capture = new ManagedAvCapture(recorder, vic, sid);
            vic.FrameCompleted += capture._frameHandler;
            return capture;
        }

        /// <summary>Called once per managed master cycle; emits one 44.1 kHz sample per accumulator crossing.</summary>
        public void OnManagedCycle()
        {
            _sampleAccumulator += 1.0;
            if (_sampleAccumulator < _cyclesPerSample)
                return;

            _sampleAccumulator -= _cyclesPerSample;
            var sample = _sid.GenerateSample();
            if (sample > 1f)
                sample = 1f;
            else if (sample < -1f)
                sample = -1f;
            _audioBatch[_audioBatchLength++] = (short)(sample * 32767f);
            if (_audioBatchLength == _audioBatch.Length)
                FlushAudio();
        }

        private void FlushAudio()
        {
            if (_audioBatchLength == 0)
                return;

            _recorder.WriteSamples(_audioBatch.AsSpan(0, _audioBatchLength));
            _audioBatchLength = 0;
        }

        public void Dispose()
        {
            if (_stopped)
                return;

            _stopped = true;
            _vic.FrameCompleted -= _frameHandler;
            FlushAudio();
            _recorder.Stop();
            _recorder.Dispose();
        }
    }

    /// <summary>
    /// Runs the strict validator to the first mismatch, then replays that point
    /// and advances only one side at a time to determine whether a transient
    /// load window converges to the same CPU/RAM anchor state.
    /// </summary>
    public LoadWindowIsolationReport RunWithLoadWindowIsolation(long maxCycles, long catchUpLimit)
    {
        var strictReport = Run(maxCycles);
        var strictTrace = FormatRecentTrace();
        if (strictReport.Success || strictReport.Mismatch is null)
        {
            return LoadWindowIsolationReport.StrictPass(maxCycles, strictTrace);
        }

        var mismatchCycle = strictReport.FirstMismatchCycle;
        var mismatch = strictReport.Mismatch.Value;
        var nativeTarget = CreateNativeAnchor();
        var managedTarget = CreateManagedAnchor();

        if (ReplayToMismatch(maxCycles, mismatchCycle, out var replayFailure))
        {
            if (TryAdvanceManagedTo(nativeTarget, catchUpLimit, out var managedAdvance))
            {
                    return LoadWindowIsolationReport.CreateResynchronized(
                    mismatchCycle,
                    "native",
                    managedAdvance,
                    0,
                    mismatch,
                    nativeTarget.ToString(),
                    CreateManagedAnchor().ToString(),
                    strictTrace,
                    FormatRecentTrace());
            }
        }

        if (ReplayToMismatch(maxCycles, mismatchCycle, out replayFailure))
        {
            if (TryAdvanceNativeTo(managedTarget, catchUpLimit, out var nativeAdvance))
            {
                return LoadWindowIsolationReport.CreateResynchronized(
                    mismatchCycle,
                    "managed",
                    0,
                    nativeAdvance,
                    mismatch,
                    managedTarget.ToString(),
                    CreateNativeAnchor().ToString(),
                    strictTrace,
                    FormatRecentTrace());
            }
        }

        return LoadWindowIsolationReport.FailedToResynchronize(
            mismatchCycle,
            catchUpLimit,
            replayFailure ?? mismatch,
            strictTrace,
            FormatRecentTrace());
    }

    public KernalCloseIsolationReport RunUntilKernalCloseStableAndSearchNative(long maxCycles, long nativeSearchLimit)
    {
        const int NativeRecentAnchorCapacity = 100_000;

        ResetManaged();
        _native.Reset();
        _recentTrace.Clear();
        _hasPendingKernalCloseCall = false;
        _basicCommandAutomation?.Reset();
        _kernalCloseTarget = ResolveKernalJumpTarget(_machine.Bus, KernalCloseVector);
        _nativeSawLoadingPrompt = false;
        _nextNativeLoadProbeCycle = _basicCommandAutomation?.CyclesPerFrame ?? 19656;
        if (_recordRecentTrace)
            RecordTrace(0);

        long prevNativeCycle = 0;
        var recentNativeAnchors = new Queue<AnchorSample>();
        for (_cycleCount = 0; _cycleCount < maxCycles; _cycleCount++)
        {
            _native.Step();
            long nativeCycle = _native.GetState().Cycle;
            long nativeDelta = nativeCycle - prevNativeCycle;
            prevNativeCycle = nativeCycle;
            _lastNativeDelta = nativeDelta;

            for (long j = 0; j < nativeDelta; j++)
                StepManaged();

            _basicCommandAutomation?.AdvanceTo(_machine.GetState().Cycle, _machine);
            if (_basicCommandAutomation?.LastError is { Length: > 0 } inputError)
            {
                return KernalCloseIsolationReport.InputAutomationFailed(
                    inputError,
                    maxCycles,
                    KernalCloseVector,
                    _kernalCloseTarget,
                    FormatRunDiagnostics());
            }

            RememberNativeAnchor(recentNativeAnchors, NativeRecentAnchorCapacity);

            if (_recordRecentTrace)
                RecordTrace(_cycleCount + 1);

            if (TryObserveNativeCompletedLoad(out var nativeAnchor, out var nativeStableCycle))
            {
                var searchStartManagedCycle = _machine.GetState().Cycle;
                for (long searchSteps = 0; searchSteps <= nativeSearchLimit; searchSteps++)
                {
                    if (CreateManagedAnchor().Equals(nativeAnchor))
                    {
                        var managedCyclesAdvanced = _machine.GetState().Cycle - searchStartManagedCycle;
                        return KernalCloseIsolationReport.Matched(
                            _machine.GetState().Cycle,
                            nativeStableCycle,
                            managedCyclesAdvanced,
                            default,
                            KernalCloseVector,
                            _kernalCloseTarget,
                            nativeAnchor.ToString(),
                            "native-first-forward-managed",
                            FormatRunDiagnostics());
                    }

                    if (searchSteps == nativeSearchLimit)
                        break;

                    StepManaged();
                }

                return KernalCloseIsolationReport.ManagedDidNotMatchNative(
                    nativeStableCycle,
                    nativeSearchLimit,
                    KernalCloseVector,
                    _kernalCloseTarget,
                    nativeAnchor.ToString(),
                    FormatRunDiagnostics());
            }

            if (_hasPendingKernalCloseCall && _machine.GetState().PC == _pendingKernalCloseCall.ReturnPc)
            {
                var managedAnchor = CreateManagedAnchor();
                var managedCycle = _machine.GetState().Cycle;

                foreach (var sample in recentNativeAnchors)
                {
                    if (sample.Anchor.Equals(managedAnchor))
                    {
                        return KernalCloseIsolationReport.Matched(
                            managedCycle,
                            sample.NativeCycle,
                            0,
                            _pendingKernalCloseCall,
                            KernalCloseVector,
                            _kernalCloseTarget,
                            managedAnchor.ToString(),
                            "recent-native",
                            FormatRunDiagnostics());
                    }
                }

                var searchStartNativeCycle = _native.GetState().Cycle;
                for (long searchSteps = 0; searchSteps <= nativeSearchLimit; searchSteps++)
                {
                    if (CreateNativeAnchor().Equals(managedAnchor))
                    {
                        var nativeCyclesAdvanced = _native.GetState().Cycle - searchStartNativeCycle;
                        return KernalCloseIsolationReport.Matched(
                            managedCycle,
                            _native.GetState().Cycle,
                            nativeCyclesAdvanced,
                            _pendingKernalCloseCall,
                            KernalCloseVector,
                            _kernalCloseTarget,
                            managedAnchor.ToString(),
                            "forward-native",
                            FormatRunDiagnostics());
                    }

                    if (searchSteps == nativeSearchLimit)
                        break;

                    _native.Step();
                }

                return KernalCloseIsolationReport.NativeDidNotMatch(
                    managedCycle,
                    nativeSearchLimit,
                    _pendingKernalCloseCall,
                    KernalCloseVector,
                    _kernalCloseTarget,
                    managedAnchor.ToString(),
                    FormatRunDiagnostics());
            }
        }

        return KernalCloseIsolationReport.ManagedCloseNotObserved(maxCycles, KernalCloseVector, _kernalCloseTarget, FormatRunDiagnostics());
    }

    public string FormatRamtasDiagnostic()
    {
        if (_machine.Devices.GetByRole(DeviceRole.SystemRam) is not IMemory memory)
            return "RAMTAS diagnostic unavailable: managed system RAM is not exposed as IMemory.";

        var managedState = _machine.GetState();
        var nativeState = _native.GetState();
        var ram = memory.Span;
        var managedPointer = ReadPointer(ram[0x00C1], ram[0x00C2], managedState.Y);
        var nativePointer = ReadPointer(_native.PeekRam(0x00C1), _native.PeekRam(0x00C2), nativeState.Y);

        return
            $"RAMTAS managed C1=${ram[0x00C1]:X2}, C2=${ram[0x00C2]:X2}, Y=${managedState.Y:X2}, " +
            $"EA=${managedPointer:X4}, RAM=${ram[managedPointer]:X2}; " +
            $"native C1=${_native.PeekRam(0x00C1):X2}, C2=${_native.PeekRam(0x00C2):X2}, Y=${nativeState.Y:X2}, " +
            $"EA=${nativePointer:X4}, RAM=${_native.PeekRam(nativePointer):X2}.";
    }

    public string FormatRecentTrace()
    {
        return _recentTrace.Count == 0
            ? "No recent trace captured."
            : string.Join(Environment.NewLine, _recentTrace);
    }

    private string FormatRunDiagnostics()
    {
        var input = _basicCommandAutomation is null
            ? "input automation: not configured"
            : $"input automation: {_basicCommandAutomation.Status}";

        return
            $"{FormatRecentTrace()}{Environment.NewLine}" +
            $"{input}{Environment.NewLine}" +
            $"managed screen: [{ReadManagedScreenText()}]{Environment.NewLine}" +
            $"native screen: [{ReadNativeScreenText()}]";
    }

    private bool ValidateState()
    {
        var managedState = _machine.GetState();
        var nativeState = _native.GetState();
        var comparePc = _cycleCount > 1;

        return
            managedState.A == nativeState.A &&
            managedState.X == nativeState.X &&
            managedState.Y == nativeState.Y &&
            managedState.S == nativeState.S &&
            managedState.P == nativeState.P &&
            managedState.Cycle == nativeState.Cycle &&
            (!comparePc || managedState.PC == nativeState.PC);
    }

    private StateDiff GetStateDiff()
    {
        return new StateDiff
        {
            Cycle = _cycleCount,
            Expected = _native.GetState(),
            Actual = _machine.GetState()
        };
    }

    private bool ReplayToMismatch(long maxCycles, long expectedMismatchCycle, out StateDiff? mismatch)
    {
        var report = Run(maxCycles);
        if (report.Success || report.Mismatch is null)
        {
            mismatch = null;
            return false;
        }

        mismatch = report.Mismatch.Value;
        return report.FirstMismatchCycle == expectedMismatchCycle;
    }

    private bool TryAdvanceManagedTo(ResyncAnchor target, long catchUpLimit, out long cyclesAdvanced)
    {
        for (cyclesAdvanced = 0; cyclesAdvanced <= catchUpLimit; cyclesAdvanced++)
        {
            if (CreateManagedAnchor().Equals(target))
                return true;

            if (cyclesAdvanced == catchUpLimit)
                break;

            StepManaged();
        }

        return false;
    }

    private bool TryAdvanceNativeTo(ResyncAnchor target, long catchUpLimit, out long cyclesAdvanced)
    {
        var startingCycle = _native.GetState().Cycle;
        for (long steps = 0; steps <= catchUpLimit; steps++)
        {
            if (CreateNativeAnchor().Equals(target))
            {
                cyclesAdvanced = _native.GetState().Cycle - startingCycle;
                return true;
            }

            if (steps == catchUpLimit)
                break;

            _native.Step();
        }

        cyclesAdvanced = _native.GetState().Cycle - startingCycle;
        return false;
    }

    private ResyncAnchor CreateManagedAnchor()
    {
        var state = _machine.GetState();
        return CreateAnchor(state, managed: true);
    }

    private ResyncAnchor CreateNativeAnchor()
    {
        var state = _native.GetState();
        return CreateAnchor(state, managed: false);
    }

    private ResyncAnchor CreateAnchor(MachineState state, bool managed)
    {
        var c1 = ReadRam(0x00C1, managed);
        var c2 = ReadRam(0x00C2, managed);
        var effectiveAddress = ReadPointer(c1, c2, state.Y);
        var effectiveByte = ReadRam(effectiveAddress, managed);

        return new ResyncAnchor(
            state.A,
            state.X,
            state.Y,
            state.S,
            state.P,
            state.PC,
            c1,
            c2,
            effectiveAddress,
            effectiveByte);
    }

    private byte ReadRam(ushort address, bool managed)
    {
        if (!managed)
            return _native.PeekRam(address);

        if (_machine.Devices.GetByRole(DeviceRole.SystemRam) is not IMemory memory)
            throw new InvalidOperationException("Managed system RAM does not expose IMemory.");

        return memory.Span[address];
    }

    public void Dispose()
    {
        _pubSub.Unsubscribe(_cpuControlTransferSubscription);
        _native.Dispose();
    }

    private void ResetManaged()
    {
        if (_coordinator is null)
            _machine.Reset();
        else
            _coordinator.Reset();
    }

    private void StepManaged()
    {
        if (_coordinator is null)
            _machine.Clock.Step();
        else
            _coordinator.Step();
    }

    private static (IMachine Host, SystemCoordinator Coordinator) CreateC64WithTrueDrive(
        string modelSelector,
        string diskPath,
        byte driveNumber)
    {
        var provider = MachineTestFactory.CreateC64RomProvider();
        var builder = new ArchitectureBuilder(provider);
        var host = builder.Build(new C64Descriptor(modelSelector));
        var drive = builder.Build(new C1541Descriptor(driveNumber, diskPath));
        var coordinator = new SystemCoordinator();
        var iecBus = IecInterSystemBus.Create();
        var hostEndpoint = iecBus.AttachEndpoint("c64-host");
        var driveEndpoint = iecBus.AttachEndpoint($"drive-{driveNumber}");

        coordinator.AttachSystem(host);
        coordinator.AttachSystem(drive);
        coordinator.AttachBus(iecBus);

        var cia2 = host.Devices.GetByRole(DeviceRole.Cia2) as Mos6526
            ?? throw new InvalidOperationException("C64 host does not expose CIA2 for IEC binding.");
        var hostIecInterface = host.Devices.GetAll<C64Cia2InterfaceDevice>().FirstOrDefault()
            ?? throw new InvalidOperationException("C64 host does not expose a CIA2 interface device for IEC binding.");
        hostIecInterface.ConnectCia2(cia2, iec: hostEndpoint, synchronizeIec: coordinator.SynchronizePeripheralSystemsToHost);

        var driveVias = drive.Devices.GetAll<Via6522>()
            .OrderBy(via => via.BaseAddress)
            .ToArray();
        if (driveVias.Length < 2)
            throw new InvalidOperationException("C1541 drive does not expose both VIA chips for IEC/D64 binding.");

        var driveIecInterface = drive.Devices.GetAll<C1541IecInterfaceDevice>().FirstOrDefault()
            ?? throw new InvalidOperationException("C1541 drive does not expose an IEC interface device for IEC/D64 binding.");
        driveIecInterface.ConnectVia1(driveVias[0], driveEndpoint, iecBus);
        drive.Devices.GetAll<C1541DriveMechanismDevice>().FirstOrDefault()?.ConnectVia2(driveVias[1]);

        return (host, coordinator);
    }

    private void OnCpuControlTransfer(CpuControlTransferEvent transfer)
    {
        if (transfer.Opcode != 0x20)
            return;

        RefreshKernalCloseTarget();
        if (transfer.Target != KernalCloseVector && transfer.Target != _kernalCloseTarget)
            return;

        _pendingKernalCloseCall = transfer;
        _hasPendingKernalCloseCall = true;
    }

    private bool TryObserveNativeCompletedLoad(out ResyncAnchor anchor, out long nativeStableCycle)
    {
        anchor = default;
        nativeStableCycle = 0;

        if (_basicCommandAutomation is null)
            return false;

        var managedCycle = _machine.GetState().Cycle;
        if (managedCycle < _nextNativeLoadProbeCycle)
            return false;

        while (_nextNativeLoadProbeCycle <= managedCycle)
            _nextNativeLoadProbeCycle += _basicCommandAutomation.CyclesPerFrame;

        var nativeScreen = ReadNativeScreenText();
        var loadingIndex = nativeScreen.LastIndexOf("LOADING", StringComparison.Ordinal);
        if (loadingIndex >= 0)
            _nativeSawLoadingPrompt = true;

        if (!_nativeSawLoadingPrompt)
            return false;

        var readyAfterLoading = nativeScreen.IndexOf(
            "READY.",
            loadingIndex >= 0 ? loadingIndex : 0,
            StringComparison.Ordinal);
        if (readyAfterLoading < 0)
            return false;

        RefreshKernalCloseTarget();
        anchor = CreateNativeAnchor();
        nativeStableCycle = _native.GetState().Cycle;
        return true;
    }

    private void RefreshKernalCloseTarget()
    {
        var target = ResolveKernalJumpTarget(_machine.Bus, KernalCloseVector);
        if (target != 0x0000 && target != 0xFFFF)
            _kernalCloseTarget = target;
    }

    internal static ushort ResolveKernalJumpTarget(IBus bus, ushort vectorAddress)
    {
        var opcode = bus.Read(vectorAddress);
        if (opcode == 0x4C)
        {
            var lo = bus.Read((ushort)(vectorAddress + 1));
            var hi = bus.Read((ushort)(vectorAddress + 2));
            return (ushort)(lo | (hi << 8));
        }

        if (opcode != 0x6C)
            return vectorAddress;

        var vectorLo = bus.Read((ushort)(vectorAddress + 1));
        var vectorHi = bus.Read((ushort)(vectorAddress + 2));
        var vector = (ushort)(vectorLo | (vectorHi << 8));
        var targetLo = bus.Read(vector);
        var targetHi = bus.Read((ushort)(vector + 1));
        return (ushort)(targetLo | (targetHi << 8));
    }

    private void RememberNativeAnchor(Queue<AnchorSample> recentNativeAnchors, int capacity)
    {
        if (recentNativeAnchors.Count == capacity)
            recentNativeAnchors.Dequeue();

        recentNativeAnchors.Enqueue(new AnchorSample(_native.GetState().Cycle, CreateNativeAnchor()));
    }

    private void RecordTrace(long cycle)
    {
        var managedState = _machine.GetState();
        var nativeState = _native.GetState();
        var vicTrace = FormatVicTrace();
        var cpuTrace = FormatCpuTrace();
        if (_recentTrace.Count == 16)
            _recentTrace.Dequeue();

        _recentTrace.Enqueue(
            $"cycle {cycle}: managed PC=${managedState.PC:X4} A=${managedState.A:X2} X=${managedState.X:X2} Y=${managedState.Y:X2} S=${managedState.S:X2} P=${managedState.P:X2}; " +
            $"native PC=${nativeState.PC:X4} A=${nativeState.A:X2} X=${nativeState.X:X2} Y=${nativeState.Y:X2} S=${nativeState.S:X2} P=${nativeState.P:X2}; " +
            $"nativeDelta={_lastNativeDelta}; {vicTrace}; {cpuTrace}");
    }

    private string FormatVicTrace()
    {
        if (_machine.Devices.GetByRole(DeviceRole.VideoChip) is not Mos6569 vic)
            return "managed VIC unavailable";

        var nativeVic = _native.GetVicState();
        return
            $"managed VIC line=${vic.CurrentRasterLine:X3} x={vic.RasterX} bad={vic.IsBadLine} hold={vic.IsCpuCycleStolen}; " +
            $"native VIC line=${nativeVic.RasterLine:X3} x={nativeVic.RasterCycle} bad={nativeVic.BadLine != 0} spriteDma=${nativeVic.SpriteDma:X2}";
    }

    private string FormatCpuTrace()
    {
        return _machine.Devices.GetByRole(DeviceRole.Cpu) is Mos6502 cpu
            ? $"cpuStealEligible={cpu.CanStealCurrentCycle} cpuCycle={cpu.DebugCycle} opcode=${cpu.DebugOpcode:X2} delay={cpu.DebugDelayNextFetch}"
            : "cpuStealEligible=unavailable";
    }

    private static ushort ReadPointer(byte lo, byte hi, byte y)
        => (ushort)((lo | (hi << 8)) + y);

    private string ReadManagedScreenText()
    {
        var chars = new char[BasicCommandAutomation.ReadyPromptLength];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = DecodeScreenCode(_machine.Bus.Peek((ushort)(BasicCommandAutomation.ReadyPromptStart + i)));

        return new string(chars);
    }

    private string ReadNativeScreenText()
    {
        var chars = new char[BasicCommandAutomation.ReadyPromptLength];
        for (var i = 0; i < chars.Length; i++)
            chars[i] = DecodeScreenCode(_native.PeekRam((ushort)(BasicCommandAutomation.ReadyPromptStart + i)));

        return new string(chars);
    }

    private static char DecodeScreenCode(byte code)
    {
        if (code == 0x20)
            return ' ';

        if (code is >= 1 and <= 26)
            return (char)('A' + code - 1);

        if (code is >= 0x30 and <= 0x39)
            return (char)code;

        return code switch
        {
            0x00 => '@',
            0x22 => '"',
            0x2C => ',',
            0x2E => '.',
            0x2A => '*',
            0x3A => ':',
            _ => '?'
        };
    }

    private readonly record struct ResyncAnchor(
        byte A,
        byte X,
        byte Y,
        byte S,
        byte P,
        ushort PC,
        byte C1,
        byte C2,
        ushort EffectiveAddress,
        byte EffectiveByte)
    {
        public override string ToString()
        {
            return
                $"A=${A:X2}, X=${X:X2}, Y=${Y:X2}, S=${S:X2}, P=${P:X2}, PC=${PC:X4}, " +
                $"C1=${C1:X2}, C2=${C2:X2}, EA=${EffectiveAddress:X4}, RAM=${EffectiveByte:X2}";
        }
    }

    private readonly record struct AnchorSample(long NativeCycle, ResyncAnchor Anchor);

    private sealed class BasicCommandAutomation
    {
        public const int ReadyPromptStart = 0x0400;
        public const int ReadyPromptLength = 1000;
        private const int MaxReadyWaitFrames = 600;
        private const int InitialReadyDelayFrames = 12;
        private const int KeyPressFrames = 3;
        private const int KeyReleaseFrames = 3;

        private readonly IReadOnlyList<string> _keySequence;
        private readonly IKeyboardInputMap _keyboardMap;
        private readonly IMachineKeyboardInput _keyboardInput;
        private readonly IViceNative _native;
        private readonly int _cyclesPerFrame;
        private AutomationPhase _phase;
        private int _keyIndex;
        private int _frameDelay;
        private int _readyWaitFrames;
        private long _nextFrameCycle;
        private string? _pressedKey;
        private byte[] _pressedNativeKeyCodes = [];

        public BasicCommandAutomation(
            IReadOnlyList<string> keySequence,
            IKeyboardInputMap keyboardMap,
            IMachineKeyboardInput keyboardInput,
            IViceNative native,
            int cyclesPerFrame)
        {
            _keySequence = keySequence;
            _keyboardMap = keyboardMap;
            _keyboardInput = keyboardInput;
            _native = native;
            _cyclesPerFrame = cyclesPerFrame;
            Reset();
        }

        public string? LastError { get; private set; }

        public int CyclesPerFrame => _cyclesPerFrame;

        public string Status =>
            $"phase={_phase}, keyIndex={_keyIndex}/{_keySequence.Count}, readyWaitFrames={_readyWaitFrames}, " +
            $"frameDelay={_frameDelay}, nextFrameCycle={_nextFrameCycle}, pressed='{_pressedKey ?? ""}', " +
            $"lastError='{LastError ?? ""}'";

        private bool IsActive => _phase is AutomationPhase.WaitingForReady or AutomationPhase.Pressing or AutomationPhase.Releasing;

        public void Reset()
        {
            ReleasePressedKey();
            LastError = null;
            _phase = AutomationPhase.WaitingForReady;
            _keyIndex = 0;
            _frameDelay = 0;
            _readyWaitFrames = 0;
            _nextFrameCycle = _cyclesPerFrame;
        }

        public void AdvanceTo(long managedCycle, IMachine machine)
        {
            while (IsActive && _nextFrameCycle <= managedCycle)
            {
                AdvanceFrame(machine);
                _nextFrameCycle += _cyclesPerFrame;
            }
        }

        private void AdvanceFrame(IMachine machine)
        {
            if (_phase == AutomationPhase.WaitingForReady)
            {
                if (!ContainsBasicReadyPrompt(machine))
                {
                    _readyWaitFrames++;
                    if (_readyWaitFrames > MaxReadyWaitFrames)
                        Fail("BASIC READY prompt was not observed before the command-entry timeout.");

                    return;
                }

                _phase = AutomationPhase.Pressing;
                _frameDelay = InitialReadyDelayFrames;
                return;
            }

            if (_frameDelay > 0)
            {
                _frameDelay--;
                return;
            }

            if (_phase == AutomationPhase.Pressing)
            {
                if (_keyIndex >= _keySequence.Count)
                {
                    _phase = AutomationPhase.Complete;
                    return;
                }

                var key = _keySequence[_keyIndex];
                if (!_keyboardMap.TryResolve(key, out var nativeKeyCodes))
                {
                    Fail($"The selected keyboard map could not resolve '{key}'.");
                    return;
                }

                var keyCodes = nativeKeyCodes.ToArray();
                foreach (var keyCode in keyCodes)
                    SetNativeKey(keyCode, pressed: true);

                if (!_keyboardInput.SetKeyState(key, pressed: true))
                {
                    foreach (var keyCode in keyCodes)
                        SetNativeKey(keyCode, pressed: false);

                    Fail($"The managed keyboard input could not apply '{key}'.");
                    return;
                }

                _pressedKey = key;
                _pressedNativeKeyCodes = keyCodes;
                _phase = AutomationPhase.Releasing;
                _frameDelay = KeyPressFrames;
                return;
            }

            if (_phase == AutomationPhase.Releasing)
            {
                ReleasePressedKey();
                _keyIndex++;
                _phase = _keyIndex >= _keySequence.Count ? AutomationPhase.Complete : AutomationPhase.Pressing;
                _frameDelay = KeyReleaseFrames;
            }
        }

        private static bool ContainsBasicReadyPrompt(IMachine machine)
        {
            Span<byte> screenCodes = stackalloc byte[ReadyPromptLength];
            for (var i = 0; i < screenCodes.Length; i++)
                screenCodes[i] = machine.Bus.Peek((ushort)(ReadyPromptStart + i));

            ReadOnlySpan<byte> screenCodeReady = [18, 5, 1, 4, 25];
            ReadOnlySpan<byte> asciiReady = "READY"u8;
            return screenCodes.IndexOf(screenCodeReady) >= 0 || screenCodes.IndexOf(asciiReady) >= 0;
        }

        private void ReleasePressedKey()
        {
            foreach (var keyCode in _pressedNativeKeyCodes)
                SetNativeKey(keyCode, pressed: false);

            if (_pressedKey is not null)
                _keyboardInput.SetKeyState(_pressedKey, pressed: false);

            _pressedKey = null;
            _pressedNativeKeyCodes = [];
        }

        private void Fail(string message)
        {
            ReleasePressedKey();
            LastError = string.IsNullOrWhiteSpace(message)
                ? "BASIC command automation failed."
                : message;
            _phase = AutomationPhase.Faulted;
        }

        private void SetNativeKey(byte keyCode, bool pressed)
        {
            var row = keyCode >> 3;
            var column = keyCode & 0x07;
            _native.SetKeyboardMatrixKey(row, column, pressed);
        }

        private enum AutomationPhase
        {
            WaitingForReady,
            Pressing,
            Releasing,
            Complete,
            Faulted
        }
    }
}

public sealed class LoadWindowIsolationReport
{
    private LoadWindowIsolationReport()
    {
    }

    public bool StrictSuccess { get; private init; }
    public bool DidResynchronize { get; private init; }
    public long TotalCyclesExecuted { get; private init; }
    public long FirstMismatchCycle { get; private init; }
    public string LeadingSide { get; private init; } = "";
    public long ManagedCyclesAdvanced { get; private init; }
    public long NativeCyclesAdvanced { get; private init; }
    public long CatchUpLimit { get; private init; }
    public StateDiff? Mismatch { get; private init; }
    public string TargetAnchor { get; private init; } = "";
    public string MatchedAnchor { get; private init; } = "";
    public string InitialTrace { get; private init; } = "";
    public string FinalTrace { get; private init; } = "";
    public bool Success => StrictSuccess || DidResynchronize;

    public static LoadWindowIsolationReport StrictPass(long cycles, string trace)
    {
        return new LoadWindowIsolationReport
        {
            StrictSuccess = true,
            TotalCyclesExecuted = cycles,
            FirstMismatchCycle = -1,
            InitialTrace = trace,
            FinalTrace = trace
        };
    }

    public static LoadWindowIsolationReport CreateResynchronized(
        long mismatchCycle,
        string leadingSide,
        long managedCyclesAdvanced,
        long nativeCyclesAdvanced,
        StateDiff mismatch,
        string targetAnchor,
        string matchedAnchor,
        string initialTrace,
        string finalTrace)
    {
        return new LoadWindowIsolationReport
        {
            DidResynchronize = true,
            FirstMismatchCycle = mismatchCycle,
            LeadingSide = leadingSide,
            ManagedCyclesAdvanced = managedCyclesAdvanced,
            NativeCyclesAdvanced = nativeCyclesAdvanced,
            Mismatch = mismatch,
            TargetAnchor = targetAnchor,
            MatchedAnchor = matchedAnchor,
            InitialTrace = initialTrace,
            FinalTrace = finalTrace
        };
    }

    public static LoadWindowIsolationReport FailedToResynchronize(
        long mismatchCycle,
        long catchUpLimit,
        StateDiff mismatch,
        string initialTrace,
        string finalTrace)
    {
        return new LoadWindowIsolationReport
        {
            FirstMismatchCycle = mismatchCycle,
            CatchUpLimit = catchUpLimit,
            Mismatch = mismatch,
            InitialTrace = initialTrace,
            FinalTrace = finalTrace
        };
    }

    public override string ToString()
    {
        if (StrictSuccess)
            return "Strict comparison completed without an isolated load window.";

        if (DidResynchronize)
        {
            var laggingSide = LeadingSide == "native" ? "managed" : "native";
            var advanced = LeadingSide == "native" ? ManagedCyclesAdvanced : NativeCyclesAdvanced;
            return
                $"Isolated load window at cycle {FirstMismatchCycle}: {LeadingSide} reached the post-window anchor first; " +
                $"{laggingSide} matched after {advanced} cycle(s). Target [{TargetAnchor}], matched [{MatchedAnchor}].";
        }

        return
            $"Load-window isolation failed at cycle {FirstMismatchCycle}: neither side matched the other within {CatchUpLimit} cycle(s).";
    }
}

public sealed class KernalCloseIsolationReport
{
    private KernalCloseIsolationReport()
    {
    }

    public bool Success { get; private init; }
    public long ManagedStableCycle { get; private init; }
    public long NativeMatchedCycle { get; private init; }
    public long NativeCyclesAdvanced { get; private init; }
    public long SearchLimit { get; private init; }
    public CpuControlTransferEvent CloseCall { get; private init; }
    public ushort CloseVector { get; private init; }
    public ushort CloseTarget { get; private init; }
    public string Anchor { get; private init; } = "";
    public string MatchSource { get; private init; } = "";
    public string Trace { get; private init; } = "";
    public string FailureReason { get; private init; } = "";

    public static KernalCloseIsolationReport Matched(
        long managedStableCycle,
        long nativeMatchedCycle,
        long nativeCyclesAdvanced,
        CpuControlTransferEvent closeCall,
        ushort closeVector,
        ushort closeTarget,
        string anchor,
        string matchSource,
        string trace)
    {
        return new KernalCloseIsolationReport
        {
            Success = true,
            ManagedStableCycle = managedStableCycle,
            NativeMatchedCycle = nativeMatchedCycle,
            NativeCyclesAdvanced = nativeCyclesAdvanced,
            CloseCall = closeCall,
            CloseVector = closeVector,
            CloseTarget = closeTarget,
            Anchor = anchor,
            MatchSource = matchSource,
            Trace = trace
        };
    }

    public static KernalCloseIsolationReport NativeDidNotMatch(
        long managedStableCycle,
        long searchLimit,
        CpuControlTransferEvent closeCall,
        ushort closeVector,
        ushort closeTarget,
        string anchor,
        string trace)
    {
        return new KernalCloseIsolationReport
        {
            ManagedStableCycle = managedStableCycle,
            SearchLimit = searchLimit,
            CloseCall = closeCall,
            CloseVector = closeVector,
            CloseTarget = closeTarget,
            Anchor = anchor,
            Trace = trace,
            FailureReason = "Native did not reach the managed post-CLOSE anchor within the search limit."
        };
    }

    public static KernalCloseIsolationReport ManagedCloseNotObserved(
        long maxCycles,
        ushort closeVector,
        ushort closeTarget,
        string trace)
    {
        return new KernalCloseIsolationReport
        {
            SearchLimit = maxCycles,
            CloseVector = closeVector,
            CloseTarget = closeTarget,
            Trace = trace,
            FailureReason = "Managed CPU did not call the KERNAL CLOSE routine within the run limit."
        };
    }

    public static KernalCloseIsolationReport ManagedDidNotMatchNative(
        long nativeStableCycle,
        long searchLimit,
        ushort closeVector,
        ushort closeTarget,
        string anchor,
        string trace)
    {
        return new KernalCloseIsolationReport
        {
            NativeMatchedCycle = nativeStableCycle,
            SearchLimit = searchLimit,
            CloseVector = closeVector,
            CloseTarget = closeTarget,
            Anchor = anchor,
            Trace = trace,
            FailureReason =
                "Native completed the D64 load and returned to READY before managed called KERNAL CLOSE; " +
                "managed did not reach the native post-load anchor within the search limit."
        };
    }

    public static KernalCloseIsolationReport InputAutomationFailed(
        string message,
        long maxCycles,
        ushort closeVector,
        ushort closeTarget,
        string trace)
    {
        return new KernalCloseIsolationReport
        {
            SearchLimit = maxCycles,
            CloseVector = closeVector,
            CloseTarget = closeTarget,
            Trace = trace,
            FailureReason = $"Input automation failed: {message}"
        };
    }

    public override string ToString()
    {
        if (Success)
        {
            if (CloseCall.Opcode == 0)
            {
                return
                    $"KERNAL CLOSE vector=${CloseVector:X4} target=${CloseTarget:X4}; native reached the post-load anchor first, " +
                    $"managed matched cycle={ManagedStableCycle}, native stable cycle={NativeMatchedCycle}, " +
                    $"managed advanced={NativeCyclesAdvanced} ({MatchSource}); anchor [{Anchor}].";
            }

            return
                $"KERNAL CLOSE vector=${CloseVector:X4} target=${CloseTarget:X4}, call source=${CloseCall.Source:X4}, " +
                $"return=${CloseCall.ReturnPc:X4}; managed stable cycle={ManagedStableCycle}, native matched cycle={NativeMatchedCycle}, " +
                $"native advanced={NativeCyclesAdvanced} ({MatchSource}); anchor [{Anchor}].";
        }

        return
            $"{FailureReason} KERNAL CLOSE vector=${CloseVector:X4} target=${CloseTarget:X4}, " +
            $"managed stable cycle={ManagedStableCycle}, search limit={SearchLimit}, anchor [{Anchor}].";
    }
}
