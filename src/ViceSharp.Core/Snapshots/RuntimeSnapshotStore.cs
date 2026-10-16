using System.Globalization;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Pla;
using ViceSharp.Chips.VicIi;
using ViceSharp.Chips.Snapshots;
using ViceSharp.Core;
using ViceSharp.Core.Wiring;

namespace ViceSharp.Core.Snapshots;

public sealed class RuntimeSnapshotStore : ISnapshotStore, IReplayEngine
{
    private const int AddressCount = ushort.MaxValue + 1;
    private const int PeriodicFrameInterval = 10;


    private readonly List<ReplayKeyEvent> _events = new();
    private readonly Dictionary<int, ReplaySlot> _slots = new();
    private RuntimeSnapshot? _baseSnapshot;
    private int _baseSlotFrame;
    private string? _baseSnapshotId;
    private IMachine? _boundMachine;
    private int _suppressObservedInput;
    private ReplayTransportMode _transport = ReplayTransportMode.Stopped;
    private string _lastAction = "stopped";
    private int _cursor;
    private int _positionFrame;
    private int _positionCycle;
    private int _singleSteps;

    public ISnapshot Capture(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        var memory = new byte[AddressCount];
        // VICE snapshots store mem_ram. Bus.Peek returns the banked CPU view,
        // so RAM under BASIC, KERNAL, or chargen would be replaced by the ROM byte.
        var ram = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory;
        var capturedRam = ram is not null && ram.Span.Length >= AddressCount;
        if (capturedRam)
            ram!.Span.Slice(0, AddressCount).CopyTo(memory);
        else
        {
            for (var address = 0; address < memory.Length; address++)
                memory[address] = machine.Bus.Peek((ushort)address);
        }

        var snapshot = new RuntimeSnapshot(machine.GetState(), memory);
        if (capturedRam)
        {
            var io = new byte[0x1000];
            for (var i = 0; i < io.Length; i++)
                io[i] = machine.Bus.Peek((ushort)(0xD000 + i));
            snapshot.VisibleIo = io;
            snapshot.HasProcessorPort = true;
            // VICE c64memsnapshot saves pport.data, the latch. A pin read
            // mixes that latch with InputPullUp on input bits.
            if (machine.Devices.GetByRole(DeviceRole.Pla) is Mos906114 pla)
            {
                snapshot.PortDirection = pla.DataDirection;
                snapshot.PortData = pla.DataRegister;
            }
            else
            {
                snapshot.PortDirection = machine.Bus.Peek(0x0000);
                snapshot.PortData = machine.Bus.Peek(0x0001);
            }
        }

        // VICE snapshot modules store VIC/SID/CIA execution state beside RAM.
        // Register peeks do not include the raster counter or OSC3 latch.
        snapshot.ChipExecution = C64ChipExecutionImage.Capture(machine);
        snapshot.ColorRam = CaptureColorRam(machine);
        if (machine.Devices.GetByRole(DeviceRole.CartridgePort) is C64MemoryMap keyboardMap)
        {
            var rows = keyboardMap.CaptureKeyboardRows(out var restorePressed);
            var packed = new byte[9];
            rows.AsSpan(0, Math.Min(8, rows.Length)).CopyTo(packed);
            packed[8] = restorePressed ? (byte)1 : (byte)0;
            snapshot.KeyboardRows = packed;
            snapshot.KeyboardHostKeys = keyboardMap.CaptureKeyboardHostKeys();
            snapshot.HasKeyboardHostKeyCodes = true;
            snapshot.KeyboardHostKeyCodes = keyboardMap.CaptureKeyboardHostKeyCodes();
            snapshot.HasCartridgeBanks = true;
            snapshot.CartridgeBanks = keyboardMap.CaptureCartridgeBanks();
            keyboardMap.CaptureCartridgeImage(out var cartridgeImage, out var cartridgeMapping);
            snapshot.HasCartridgeImage = true;
            snapshot.CartridgeImage = cartridgeImage;
            snapshot.CartridgeMapping = cartridgeMapping is { } mode ? (int)mode : null;
            snapshot.HasKeyboardPressCounts = true;
            snapshot.KeyboardPressCounts = keyboardMap.CaptureKeyboardPressCounts();
            if (keyboardMap.Datasette is { } tape)
            {
                snapshot.HasTapeControls = true;
                snapshot.TapePlayPressed = tape.PlayPressed;
                snapshot.TapeRecordPressed = tape.RecordPressed;
                snapshot.HasTapeTiming = true;
                snapshot.TapePulseOffset = tape.PulseOffset;
                snapshot.HasTapeImage = true;
                snapshot.TapeImage = tape.CopyImage();
                snapshot.HasTapeAttached = true;
                snapshot.TapeAttached = tape.IsAttached;
                snapshot.TapeMotorRampCounter = tape.MotorRampCounter;
                snapshot.TapeMotorRampComplete = tape.MotorRampComplete;
                snapshot.TapeMotorRampStarted = tape.MotorRampStarted;
            }

            if (machine.Devices.GetAll<DatasetteCia1FlagBinding>().FirstOrDefault() is { } binding)
            {
                var pulse = binding.CapturePulse();
                snapshot.HasTapeTiming = true;
                snapshot.TapeCyclesUntilPulse = pulse.CyclesUntilNextPulse;
                snapshot.TapeHavePendingPulse = pulse.HavePendingPulse;
            }
            keyboardMap.CaptureJoysticks(out var port1, out var port2);
            snapshot.JoystickPorts = new byte[] { port1, port2 };
            snapshot.MousePots = keyboardMap.CaptureMouse();
        }
        if (machine.Clock is SystemClock clock)
            snapshot.InterruptTiming = clock.CaptureInterruptTiming();
        return snapshot;
    }

    public void Restore(IMachine machine, ISnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(snapshot);

        var runtimeSnapshot = Materialize(snapshot);
        var memory = runtimeSnapshot.Memory.Span;
        var ramDevice = machine.Devices.GetByRole(DeviceRole.SystemRam) as IMemory;
        var copiedRam = ramDevice is not null && ramDevice.Span.Length >= memory.Length;
        if (copiedRam)
            memory.CopyTo(ramDevice!.Span);

        if (runtimeSnapshot.VisibleIo is { Length: 0x1000 } io)
        {
            ApplyProcessorPort(machine, runtimeSnapshot, memory);
            // VICE c64memsnapshot writes mem_ram, which is already copied.
            // VisibleIo is the banked CPU view of $D000. When character ROM
            // or RAM is mapped there, those peeks are not I/O registers.
            // Writing them stores chargen bytes into the RAM underneath.
            if (CpuSeesIoAtD000(machine))
            {
                for (var i = 0; i < io.Length; i++)
                    machine.Bus.Write((ushort)(0xD000 + i), io[i]);
            }
        }
        else if (runtimeSnapshot.HasProcessorPort && copiedRam)
        {
            // Deserialized images keep the port latches and mem_ram, not the
            // 4K IO window. The RAM span is already in place. Writing the
            // whole image through the bus would store the power-up pattern
            // into the PLA and bank KERNAL out.
            ApplyProcessorPort(machine, runtimeSnapshot, memory);
        }
        else
        {
            for (var address = 0; address < memory.Length; address++)
                machine.Bus.Write((ushort)address, memory[address]);

            ApplyProcessorPort(machine, runtimeSnapshot, memory);
        }

        // After the I/O window. Those writes hit bank registers such as
        // Ocean $DE00. VICE restores roml_banks, then currbank. Installing
        // the image resets the bank registers, so currbank follows it.
        if (runtimeSnapshot.HasCartridgeImage
            && machine.Devices.GetByRole(DeviceRole.CartridgePort) is C64MemoryMap cartImage)
        {
            CartridgeMappingMode? mapping = runtimeSnapshot.CartridgeMapping is int mappingValue
                ? (CartridgeMappingMode)mappingValue
                : null;
            cartImage.RestoreCartridgeImage(runtimeSnapshot.CartridgeImage, mapping);
        }

        if (runtimeSnapshot.HasCartridgeBanks
            && machine.Devices.GetByRole(DeviceRole.CartridgePort) is C64MemoryMap cartBanks)
            cartBanks.RestoreCartridgeBanks(runtimeSnapshot.CartridgeBanks);

        if (machine.Devices.GetByRole(DeviceRole.Cpu) is ICpu cpu)
        {
            cpu.PC = runtimeSnapshot.State.PC;
            cpu.Flags = runtimeSnapshot.State.P;

            // RUNTIME-SNAPSHOT-002: ICpu only exposes PC + Flags, so the
            // accumulator and index registers (A/X/Y/S) need a concrete
            // type-aware path to round-trip. Today the only ICpu
            // implementation that participates in a runtime snapshot is
            // the Mos6502 (and its 6510 derivatives via public fields);
            // extend this branch when additional CPU types ship.
            if (cpu is Mos6502 mos6502)
            {
                mos6502.A = runtimeSnapshot.State.A;
                mos6502.X = runtimeSnapshot.State.X;
                mos6502.Y = runtimeSnapshot.State.Y;
                mos6502.S = runtimeSnapshot.State.S;
            }
        }

        // After the IO register writes. Those writes replay read values and
        // leave the raster counter and oscillator latch at power-up.
        runtimeSnapshot.ChipExecution?.Restore(machine);
        if (runtimeSnapshot.ColorRam is { Length: > 0 } colorRam)
            RestoreColorRam(machine, colorRam);
        // After the chips. A restored CIA can raise IRQ while the image
        // is applied. That new edge is the one already counted in irq_clk.
        if (runtimeSnapshot.InterruptTiming is { } timing && machine.Clock is SystemClock clock)
            clock.InjectInterruptTiming(timing);
        if (runtimeSnapshot.KeyboardRows is { Length: >= 8 } keys && machine.Devices.GetByRole(DeviceRole.CartridgePort) is C64MemoryMap map)
        {
            var restorePressed = keys.Length > 8 && keys[8] != 0;
            map.RestoreKeyboardRows(
                keys.AsSpan(0, 8),
                restorePressed,
                runtimeSnapshot.KeyboardHostKeys,
                runtimeSnapshot.HasKeyboardPressCounts ? runtimeSnapshot.KeyboardPressCounts : null,
                runtimeSnapshot.HasKeyboardHostKeyCodes ? runtimeSnapshot.KeyboardHostKeyCodes : null);
        }

        if (runtimeSnapshot.HasTapeControls
            && machine.Devices.GetByRole(DeviceRole.SystemRam) is C64MemoryMap tapeMap
            && tapeMap.Datasette is { } tape)
        {
            tape.RestoreControls(runtimeSnapshot.TapePlayPressed, runtimeSnapshot.TapeRecordPressed);
        }

        if (runtimeSnapshot.HasTapeImage
            && machine.Devices.GetAll<Datasette>().FirstOrDefault() is { } imageTape)
        {
            imageTape.RestoreImage(runtimeSnapshot.TapeImage);
        }

        if (runtimeSnapshot.HasTapeAttached
            && machine.Devices.GetAll<Datasette>().FirstOrDefault() is { } attachedTape)
        {
            attachedTape.RestoreAttached(runtimeSnapshot.TapeAttached);
        }

        if (runtimeSnapshot.HasTapeTiming
            && machine.Devices.GetAll<Datasette>().FirstOrDefault() is { } timedTape)
        {
            timedTape.RestorePulseOffset(runtimeSnapshot.TapePulseOffset);
            timedTape.RestoreMotorRamp(
                runtimeSnapshot.TapeMotorRampCounter,
                runtimeSnapshot.TapeMotorRampComplete,
                runtimeSnapshot.TapeMotorRampStarted);
            if (machine.Devices.GetAll<DatasetteCia1FlagBinding>().FirstOrDefault() is { } binding)
            {
                binding.RestorePulse(runtimeSnapshot.TapeCyclesUntilPulse, runtimeSnapshot.TapeHavePendingPulse);
            }
        }

        if (runtimeSnapshot.JoystickPorts is { Length: >= 2 } joysticks
            && machine.Devices.GetByRole(DeviceRole.CartridgePort) is C64MemoryMap joystickMap)
        {
            joystickMap.RestoreJoysticks(joysticks[0], joysticks[1]);
        }

        if (runtimeSnapshot.MousePots is { Length: >= 12 } mouse
            && machine.Devices.GetByRole(DeviceRole.CartridgePort) is C64MemoryMap mouseMap)
        {
            mouseMap.RestoreMouse(mouse);
        }
    }

    public async Task SaveAsync(ISnapshot snapshot, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var data = new byte[snapshot.GetSerializedSize()];
        snapshot.Serialize(data);
        await File.WriteAllBytesAsync(filePath, data, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RuntimeSnapshot> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var data = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        var snapshot = new RuntimeSnapshot();
        snapshot.Deserialize(data);
        return snapshot;
    }

    /// <summary>
    /// FR-SNP-003. Stores one key event at <paramref name="frame"/> and <paramref name="cycle"/>.
    /// When <paramref name="machine"/> is supplied, a snapshot slot is kept every ten frames
    /// and the key is applied so a later slot includes earlier recorded input.
    /// </summary>
    /// <param name="frame">Frame number of the key event.</param>
    /// <param name="cycle">Cycle within that frame.</param>
    /// <param name="key">Host key name.</param>
    /// <param name="pressed"><c>true</c> when the key is pressed; <c>false</c> when released.</param>
    /// <param name="machine">Optional live machine. Periodic snapshots are captured from it.</param>
    public void RecordKey(int frame, int cycle, string key, bool pressed, IMachine? machine = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length == 0)
            throw new ArgumentException("A recorded key needs a name.", nameof(key));
        if (frame < 0)
            throw new ArgumentOutOfRangeException(nameof(frame));
        if (cycle < 0)
            throw new ArgumentOutOfRangeException(nameof(cycle));

        if (machine is not null)
        {
            _boundMachine = machine;
            WatchJoystick(machine);
        }

        var source = machine ?? _boundMachine;
        // VICE keyboard_alarm_handler records one edge. A second direct
        // report of the same host key at the same level is not another owner.
        if (source is not null && HostKeyAlreadyAtLevel(source, key, pressed))
            return;

        RememberPeriodicSlot(frame, source);
        var evt = new ReplayKeyEvent(frame, cycle, key, pressed, CopyHostKeyCodes(source, key));
        InsertEvent(evt);
        if (source is not null)
            ApplyKey(source, evt);
    }

    /// <inheritdoc />
    public void Record(int frame, int cycle, string key, bool pressed)
        => RecordKey(frame, cycle, key, pressed);

    /// <summary>
    /// FR-SNP-003. Restores the base snapshot when playback is at the start, then applies
    /// every recorded key that is still ahead of the cursor. A paused transport does not advance.
    /// </summary>
    /// <param name="machine">Machine that receives the snapshot and recorded input.</param>
    public void Play(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        _boundMachine = machine;
        if (_transport == ReplayTransportMode.Paused)
        {
            _ = machine.GetState();
            return;
        }

        _transport = ReplayTransportMode.Playing;
        _lastAction = "play";
        if (_cursor == 0 && _baseSnapshot is not null)
        {
            Restore(machine, _baseSnapshot);
            PlaceReplayAtCapturedCycle(machine, _baseSnapshot);
        }
        else
        {
            _ = machine.GetState();
        }

        while (_cursor < _events.Count && _transport == ReplayTransportMode.Playing)
        {
            var evt = _events[_cursor];
            AdvanceTo(machine, evt.Frame, evt.Cycle);
            ApplyKey(machine, evt);
            _cursor++;
        }
    }

    /// <summary>
    /// FR-SNP-003. Replays the stored events twice from the same baseline and compares
    /// the resulting machine images. The same recording is bit-exact.
    /// </summary>
    /// <param name="machine">Machine used for both playbacks.</param>
    /// <returns><c>true</c> when both playbacks produce the same snapshot bytes.</returns>
    public bool PlaybackMatches(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        var baseline = _baseSnapshot ?? (RuntimeSnapshot)Capture(machine);
        machine.Clock.Reset();
        var first = RenderPlayback(machine, baseline);
        machine.Clock.Reset();
        var second = RenderPlayback(machine, baseline);
        return MemoryEquals(first, second);
    }

    private static bool MemoryEquals(RuntimeSnapshot left, RuntimeSnapshot right)
    {
        return left.Memory.Span.SequenceEqual(right.Memory.Span)
            && left.State.PC == right.State.PC
            && left.State.A == right.State.A
            && left.State.X == right.State.X
            && left.State.Y == right.State.Y
            && left.State.S == right.State.S
            && left.State.P == right.State.P;
    }

    /// <summary>
    /// FR-SNP-003. Writes a compact replay description: a snapshot reference plus the
    /// recorded input. Memory images stay in the slot store and are not inlined.
    /// </summary>
    /// <returns>Replay text shorter than 64KB.</returns>
    public string SaveReplay()
    {
        var snapshotName = _baseSnapshotId ?? "snapshot:base";
        var lines = new List<string>
        {
            "snapshot=" + snapshotName,
            "baseFrame=" + Text(_baseSlotFrame),
            "transport=" + TransportName(_transport),
            "action=" + _lastAction,
            "positionFrame=" + Text(_positionFrame),
            "cursor=" + Text(_cursor),
            "singleSteps=" + Text(_singleSteps),
            "inputCount=" + Text(_events.Count)
        };

        for (var i = 0; i < _events.Count; i++)
        {
            var evt = _events[i];
            if (evt.IsCartridge)
            {
                var cartridgeValue = evt.Pressed
                    ? " mapping=" + Text(evt.DeltaX)
                        + " bytes=" + (evt.CartridgeImage is { Length: > 0 } image
                            ? Convert.ToHexString(image)
                            : string.Empty)
                    : string.Empty;
                lines.Add(
                    "input cartridge attached=" + (evt.Pressed ? "1" : "0")
                    + " frame=" + Text(evt.Frame)
                    + " cycle=" + Text(evt.Cycle)
                    + cartridgeValue);
            }
            else if (evt.IsLightPen)
            {
                if (evt.IsLightPenTiming)
                {
                    lines.Add(
                        "input lightpen timing x=" + Text(evt.DeltaX)
                        + " y=" + Text(evt.DeltaY)
                        + " frame=" + Text(evt.Frame)
                        + " cycle=" + Text(evt.Cycle));
                }
                else
                {
                    lines.Add(evt.IsLightPenTrigger
                        ? "input lightpen trigger frame=" + Text(evt.Frame) + " cycle=" + Text(evt.Cycle)
                        : "input lightpen state=" + (evt.Pressed ? "1" : "0")
                            + " frame=" + Text(evt.Frame)
                            + " cycle=" + Text(evt.Cycle));
                }
            }
            else if (evt.IsTape)
            {
                if (string.Equals(evt.Key, "seek", StringComparison.Ordinal))
                {
                    lines.Add(
                        "input tape control=seek index=" + Text(evt.DeltaX)
                        + " frame=" + Text(evt.Frame)
                        + " cycle=" + Text(evt.Cycle));
                }
                else if (string.Equals(evt.Key, "insert", StringComparison.Ordinal))
                {
                    var bytes = evt.TapeImage is { Length: > 0 } image
                        ? Convert.ToHexString(image)
                        : string.Empty;
                    lines.Add(
                        "input tape control=insert pressed=1 frame=" + Text(evt.Frame)
                        + " cycle=" + Text(evt.Cycle)
                        + " bytes=" + bytes);
                }
                else
                {
                    lines.Add(
                        "input tape control=" + evt.Key
                        + " pressed=" + (evt.Pressed ? "1" : "0")
                        + " frame=" + Text(evt.Frame)
                        + " cycle=" + Text(evt.Cycle));
                }
            }
            else if (evt.IsRestore)
            {
                lines.Add(
                    "input restore pressed=" + (evt.Pressed ? "1" : "0")
                    + " frame=" + Text(evt.Frame)
                    + " cycle=" + Text(evt.Cycle));
            }
            else if (evt.IsPot)
            {
                lines.Add(
                    "input pot port=" + Text(evt.Port)
                    + " x=" + Text(evt.PotX)
                    + " y=" + Text(evt.PotY)
                    + " frame=" + Text(evt.Frame)
                    + " cycle=" + Text(evt.Cycle));
            }
            else if (evt.IsMouse)
            {
                lines.Add(
                    "input mouse port=" + Text(evt.Port)
                    + " dx=" + Text(evt.DeltaX)
                    + " dy=" + Text(evt.DeltaY)
                    + " frame=" + Text(evt.Frame)
                    + " cycle=" + Text(evt.Cycle));
            }
            else if (evt.IsMatrix)
            {
                lines.Add(
                    "input key code=" + evt.Key
                    + " pressed=" + (evt.Pressed ? "1" : "0")
                    + " frame=" + Text(evt.Frame)
                    + " cycle=" + Text(evt.Cycle));
            }
            else if (evt.IsJoystick)
            {
                lines.Add(
                    "input joystick port=" + Text(evt.Port)
                    + " direction=" + Text(evt.Direction)
                    + " fire=" + (evt.Fire ? "1" : "0")
                    + " frame=" + Text(evt.Frame)
                    + " cycle=" + Text(evt.Cycle));
            }
            else
            {
                var codes = string.Empty;
                if (evt.KeyCodes is { Length: > 0 } keyCodes)
                {
                    var rendered = new string[keyCodes.Length];
                    for (var code = 0; code < keyCodes.Length; code++)
                        rendered[code] = keyCodes[code].ToString("X2", CultureInfo.InvariantCulture);

                    codes = " codes=" + string.Join(",", rendered);
                }

                lines.Add(
                    "input key=" + Sanitize(evt.Key)
                    + " pressed=" + (evt.Pressed ? "1" : "0")
                    + " frame=" + Text(evt.Frame)
                    + " cycle=" + Text(evt.Cycle)
                    + codes);
            }
        }

        foreach (var slot in _slots.Values.OrderBy(static item => item.Frame))
        {
            lines.Add(
                "slot frame=" + Text(slot.Frame)
                + " snapshot=snapshot:slot:" + Text(slot.Frame));
        }

        return string.Join('\n', lines);
    }

    /// <summary>FR-SNP-003. Holds the replay cursor and marks transport paused.</summary>
    public void Pause()
    {
        _transport = ReplayTransportMode.Paused;
        _lastAction = "paused";
    }

    /// <summary>
    /// FR-SNP-003. Applies one recorded event when a machine is bound, otherwise
    /// advances one event or one frame, then pauses.
    /// </summary>
    public void Step()
    {
        _lastAction = "step";
        _singleSteps++;
        if (_boundMachine is not null && _cursor < _events.Count)
        {
            var evt = _events[_cursor];
            AdvanceTo(_boundMachine, evt.Frame, evt.Cycle);
            ApplyKey(_boundMachine, evt);
        }

        if (_cursor < _events.Count)
            _cursor++;
        else if (_boundMachine is not null)
            AdvanceTo(_boundMachine, _positionFrame + 1, 0);
        else
        {
            _positionFrame++;
            _positionCycle = 0;
        }

        _transport = ReplayTransportMode.Paused;
    }

    /// <summary>
    /// FR-SNP-003. Skips ahead to the next periodic snapshot boundary.
    /// A bound machine is seeked onto that frame.
    /// </summary>
    public void FastForward()
    {
        var target = NextPeriodicFrame(_positionFrame);
        _lastAction = "fast-forward";
        _transport = ReplayTransportMode.FastForward;
        if (_boundMachine is not null)
        {
            Seek(_boundMachine, target);
            return;
        }

        _positionFrame = target;
        _cursor = IndexAfter(target, 0);
    }

    /// <inheritdoc />
    public int Seek(IMachine machine, int frame)
    {
        ArgumentNullException.ThrowIfNull(machine);
        _boundMachine = machine;

        var frameCycles = FrameCycles(machine);
        var boundary = (long)frame * frameCycles;
        var slotFrame = FindSlotAtOrBefore(frame);
        // A periodic slot is named for the frame boundary, but RecordKey
        // captures whatever cycle the machine has reached. A snapshot taken
        // after that boundary is a later moment. VICE seeks to the requested
        // frame, not to a later event that happened to create the slot.
        while (slotFrame is int past
            && _slots.TryGetValue(past, out var pastSlot)
            && pastSlot.Snapshot is not null
            && pastSlot.Snapshot.State.Cycle > boundary)
        {
            slotFrame = FindSlotBefore(past);
        }

        if (slotFrame is int chosen
            && _slots.TryGetValue(chosen, out var slot)
            && slot.Snapshot is not null)
        {
            // Reset returns clocked chips to power-up, then Restore puts the
            // slot image back, including VIC-II and SID execution state.
            // A running baseline already contains its elapsed cycles. Replay
            // starts at that cycle and steps only what is still ahead.
            // Starting again at cycle 0 re-executes the captured prefix.
            machine.Clock.Reset();
            Restore(machine, slot.Snapshot);
            if (slot.Snapshot.State.Cycle > 0)
                PlaceReplayAtCapturedCycle(machine, slot.Snapshot);
            else
            {
                _positionFrame = slot.Frame;
                _positionCycle = 0;
            }

            // VICE keyboard.c restores keyarr for the selected snapshot.
            // That matrix already contains keys held before recording and
            // events before this slot. Releasing every recorded name also
            // releases a pre-record key whose up event is still in the future.
            // ReplayUntil applies only the events that belong at this boundary.
            if (slot.Snapshot.KeyboardRows is not { Length: >= 8 })
            {
                ReleaseRecordedKeys(machine);
                ApplyKeysBefore(machine, slot.FirstEventIndex);
            }

            ReplayUntil(machine, slot.FirstEventIndex, frame, 0);
        }
        else
        {
            ReplayUntil(machine, _cursor, frame, 0);
        }

        AdvanceTo(machine, frame, 0);
        _cursor = IndexAfter(frame, 0);
        return frame;
    }

    private bool RememberPeriodicSlot(int frame, IMachine? machine)
    {
        var slotFrame = frame / PeriodicFrameInterval * PeriodicFrameInterval;
        if (_slots.ContainsKey(slotFrame))
            return false;

        RuntimeSnapshot? snapshot = null;
        if (machine is not null)
            snapshot = (RuntimeSnapshot)Capture(machine);

        _slots.Add(slotFrame, new ReplaySlot(slotFrame, _events.Count, snapshot));
        if (_baseSnapshot is null && snapshot is not null)
        {
            _baseSnapshot = snapshot;
            _baseSlotFrame = slotFrame;
        }

        _baseSnapshotId ??= snapshot is null
            ? "snapshot:base"
            : "snapshot:slot:" + Text(slotFrame);
        return true;
    }

    private int InsertEvent(ReplayKeyEvent evt)
    {
        var index = _events.Count;
        while (index > 0 && HappensBefore(evt, _events[index - 1]))
            index--;

        _events.Insert(index, evt);
        if (index < _cursor)
            _cursor++;

        if (index != _events.Count - 1)
            InvalidateSlotImages();
        return index;
    }

    // The device already applied this observed input, so the new slot image
    // contains it. Seek starts at the next event. RecordKey snapshots first
    // and still replays the key it inserts.
    private void RecordAppliedObservation(int frame, ReplayKeyEvent evt)
    {
        var created = RememberPeriodicSlot(frame, _boundMachine);
        var index = InsertEvent(evt);
        if (created)
            PlaceSlotAfterEvent(frame, index);
    }

    private void PlaceSlotAfterEvent(int frame, int eventIndex)
    {
        var slotFrame = frame / PeriodicFrameInterval * PeriodicFrameInterval;
        if (!_slots.TryGetValue(slotFrame, out var slot) || slot.Snapshot is null)
            return;

        _slots[slotFrame] = new ReplaySlot(slot.Frame, eventIndex + 1, slot.Snapshot);
    }

    private void InvalidateSlotImages()
    {
        var keys = new List<int>(_slots.Keys);
        for (var i = 0; i < keys.Count; i++)
        {
            var slot = _slots[keys[i]];
            _slots[keys[i]] = new ReplaySlot(slot.Frame, 0, null);
        }

        _baseSnapshot = null;
    }

    private void PlaceReplayAtCapturedCycle(IMachine machine, RuntimeSnapshot snapshot)
    {
        var elapsed = snapshot.State.Cycle;
        if (elapsed < 0)
            elapsed = 0;

        var frameCycles = FrameCycles(machine);
        var framesElapsed = frameCycles > 0 ? (int)(elapsed / frameCycles) : 0;
        PlaceReplayAtElapsed(machine, elapsed, _baseSlotFrame > framesElapsed ? _baseSlotFrame : framesElapsed);
    }

    private void PlaceReplayAtElapsed(IMachine machine, long elapsed, int frame)
    {
        if (elapsed < 0)
            elapsed = 0;

        var frameCycles = FrameCycles(machine);
        _positionFrame = frame;
        _positionCycle = frameCycles > 0 ? (int)(elapsed % frameCycles) : 0;
        if (machine.Clock is SystemClock clock)
            clock.AdoptElapsedCycles(elapsed);
    }

    private static bool CpuSeesIoAtD000(IMachine machine)
    {
        // Same rule as C64MemoryMap.IsIoVisible: CHAREN and (LORAM or HIRAM).
        if (machine.Devices.GetByRole(DeviceRole.Pla) is not Mos906114 pla)
            return true;

        return pla.Charen && (pla.Loram || pla.Hiram);
    }

    private static void ApplyProcessorPort(IMachine machine, RuntimeSnapshot runtimeSnapshot, ReadOnlySpan<byte> memory)
    {
        if (!runtimeSnapshot.HasProcessorPort || memory.Length < 2)
            return;

        // Bus.Write stores the latch into RAM. Put the captured RAM bytes
        // back so the power-up pattern under $0000/$0001 is kept.
        var ram0 = memory[0];
        var ram1 = memory[1];
        machine.Bus.Write(0x0000, runtimeSnapshot.PortDirection);
        machine.Bus.Write(0x0001, runtimeSnapshot.PortData);
        if (machine.Devices.GetByRole(DeviceRole.SystemRam) is IMemory portRam && portRam.Span.Length > 1)
        {
            portRam.Span[0] = ram0;
            portRam.Span[1] = ram1;
        }
    }

    private void AdvanceTo(IMachine machine, int frame, int cycle)
    {
        if (frame < 0)
            frame = 0;
        if (cycle < 0)
            cycle = 0;

        var frameCycles = FrameCycles(machine);
        if (cycle > frameCycles)
            cycle = frameCycles;

        while (_positionFrame < frame || (_positionFrame == frame && _positionCycle < cycle))
        {
            var targetCycle = _positionFrame < frame ? frameCycles : cycle;
            var delta = targetCycle - _positionCycle;
            if (delta > 0)
            {
                machine.Clock.Step(delta);
                _positionCycle += delta;
            }

            if (_positionCycle >= frameCycles)
            {
                _positionFrame++;
                _positionCycle = 0;
            }
            else if (delta <= 0)
            {
                break;
            }
        }
    }

    private static int FrameCycles(IMachine machine)
    {
        if (machine.Architecture is IProfiledArchitectureDescriptor profiled)
        {
            var cycles = profiled.MachineProfile.CyclesPerLine * profiled.MachineProfile.RasterLines;
            if (cycles > 0)
                return cycles;
        }

        return machine.Architecture.VideoStandard == VideoStandard.Ntsc ? 263 * 65 : 312 * 63;
    }

    private void ReplayUntil(IMachine machine, int startIndex, int frame, int cycle)
    {
        if (startIndex < 0)
            startIndex = 0;
        if (cycle < 0)
            cycle = 0;

        for (var i = startIndex; i < _events.Count; i++)
        {
            var evt = _events[i];
            // A later cycle in the target frame belongs to the following step.
            // AdvanceTo cannot rewind once that event has been applied.
            if (evt.Frame > frame || (evt.Frame == frame && evt.Cycle > cycle))
                break;

            AdvanceTo(machine, evt.Frame, evt.Cycle);
            ApplyKey(machine, evt);
        }
    }

    private int IndexAfter(int frame, int cycle)
    {
        if (cycle < 0)
            cycle = 0;

        for (var i = 0; i < _events.Count; i++)
        {
            var evt = _events[i];
            if (evt.Frame > frame || (evt.Frame == frame && evt.Cycle > cycle))
                return i;
        }

        return _events.Count;
    }

    private int? FindSlotBefore(int frame)
    {
        int? best = null;
        foreach (var slot in _slots.Values)
        {
            if (slot.Frame >= frame)
                continue;

            if (best is null || slot.Frame > best.Value)
                best = slot.Frame;
        }

        return best;
    }

    private static byte[]? CaptureColorRam(IMachine machine)
    {
        // VICE vicii-snapshot copies color RAM directly. The CPU window
        // at $D800 is character ROM when CHAREN is clear, so Bus.Peek is
        // the wrong source.
        if (machine.Devices.GetByRole(DeviceRole.SystemRam) is not C64MemoryMap map)
            return null;

        var color = new byte[0x400];
        for (var i = 0; i < color.Length; i++)
            color[i] = map.ReadVideoMemory((ushort)(0xD800 + i));
        return color;
    }

    private static void RestoreColorRam(IMachine machine, ReadOnlySpan<byte> color)
    {
        if (machine.Devices.GetByRole(DeviceRole.SystemRam) is not C64MemoryMap)
            return;

        var pla = machine.Devices.GetByRole(DeviceRole.Pla) as Mos906114;
        var ioVisible = pla is not null && pla.Charen && (pla.Loram || pla.Hiram);
        byte savedDirection = 0;
        byte savedData = 0;
        byte ram0 = 0;
        byte ram1 = 0;
        IMemory? ram = null;
        if (!ioVisible && pla is not null)
        {
            savedDirection = pla.DataDirection;
            savedData = pla.DataRegister;
            if (machine.Devices.GetByRole(DeviceRole.SystemRam) is IMemory systemRam && systemRam.Span.Length > 1)
            {
                ram = systemRam;
                ram0 = systemRam.Span[0];
                ram1 = systemRam.Span[1];
            }

            // LORAM|HIRAM|CHAREN makes $D800 a color-RAM store. The latch
            // and the RAM under the processor port are put back afterward.
            machine.Bus.Write(0x0001, (byte)(savedData | 0x07));
        }

        for (var i = 0; i < color.Length; i++)
            machine.Bus.Write((ushort)(0xD800 + i), color[i]);

        if (!ioVisible && pla is not null)
        {
            machine.Bus.Write(0x0000, savedDirection);
            machine.Bus.Write(0x0001, savedData);
            if (ram is not null)
            {
                ram.Span[0] = ram0;
                ram.Span[1] = ram1;
            }
        }
    }

    private int? FindSlotAtOrBefore(int frame)
    {
        int? best = null;
        foreach (var slot in _slots.Values)
        {
            if (slot.Frame > frame)
                continue;

            if (best is null || slot.Frame > best.Value)
                best = slot.Frame;
        }

        return best;
    }

    private RuntimeSnapshot RenderPlayback(IMachine machine, RuntimeSnapshot baseline)
    {
        Restore(machine, baseline);
        if (ReferenceEquals(baseline, _baseSnapshot))
            PlaceReplayAtCapturedCycle(machine, baseline);
        else
            PlaceReplayAtElapsed(machine, baseline.State.Cycle, 0);

        for (var i = 0; i < _events.Count; i++)
        {
            AdvanceTo(machine, _events[i].Frame, _events[i].Cycle);
            ApplyKey(machine, _events[i]);
        }

        return (RuntimeSnapshot)Capture(machine);
    }

    private static bool SnapshotBytesEqual(RuntimeSnapshot left, RuntimeSnapshot right)
    {
        var leftSize = left.GetSerializedSize();
        var rightSize = right.GetSerializedSize();
        if (leftSize != rightSize)
            return false;

        var leftBytes = new byte[leftSize];
        var rightBytes = new byte[rightSize];
        left.Serialize(leftBytes);
        right.Serialize(rightBytes);
        return leftBytes.AsSpan().SequenceEqual(rightBytes);
    }

    private void ReleaseRecordedKeys(IMachine machine)
    {
        var releases = _events.Count;
        if (releases == 0)
            return;

        for (var i = 0; i < _events.Count; i++)
        {
            if (_events[i].IsJoystick || _events[i].IsMouse || _events[i].IsPot || _events[i].IsRestore || _events[i].IsLightPen || _events[i].IsTape || _events[i].IsMatrix)
                continue;

            var key = _events[i].Key;
            var first = true;
            for (var earlier = 0; earlier < i; earlier++)
            {
                if (string.Equals(_events[earlier].Key, key, StringComparison.Ordinal))
                {
                    first = false;
                    break;
                }
            }

            if (!first)
                continue;

            // Press counts increment once per down event. Releasing once
            // per event clears a key that a later frame left held.
            for (var n = 0; n < releases; n++)
                ApplyKey(machine, new ReplayKeyEvent(0, 0, key, false, _events[i].KeyCodes));
        }
    }

    private void ApplyKeysBefore(IMachine machine, int endIndex)
    {
        if (endIndex < 0)
            endIndex = 0;
        if (endIndex > _events.Count)
            endIndex = _events.Count;

        for (var i = 0; i < endIndex; i++)
            ApplyKey(machine, _events[i]);
    }

    private void WatchJoystick(IMachine machine)
    {
        if (machine.Devices.GetByRole(DeviceRole.SystemRam) is C64MemoryMap map)
        {
            map.AttachJoystickRecorder(RecordObservedJoystick);
            map.AttachMouseRecorder(RecordObservedMouse);
            map.AttachPotRecorder(RecordObservedPot);
            map.AttachRestoreRecorder(RecordObservedRestore);
            map.AttachKeyRecorder(RecordObservedKey);
            map.AttachMatrixRecorder(RecordObservedMatrixKey);
            map.AttachCartridgeRecorder(RecordObservedCartridge);
            map.Datasette?.AttachControlRecorder(RecordObservedTapePlay, RecordObservedTapeRecord);
            map.Datasette?.AttachTransportRecorder(RecordObservedTapeTransport);
        }

        if (machine.Devices.GetByRole(DeviceRole.VideoChip) is Mos6569 vic)
            vic.AttachLightPenRecorder(RecordObservedLightPenState, RecordObservedLightPenTrigger, RecordObservedLightPenTiming);
    }

    private void RecordObservedTapePlay(bool pressed)
        => RecordObservedTape("play", pressed);

    private void RecordObservedTapeRecord(bool pressed)
        => RecordObservedTape("record", pressed);

    private void RecordObservedTape(string control, bool pressed)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        RecordAppliedObservation(frame, ReplayKeyEvent.Tape(frame, cycle, control, pressed));
    }

    private void RecordObservedTapeTransport(string control, int index, byte[]? image)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        var transport = string.Equals(control, "seek", StringComparison.Ordinal)
            ? ReplayKeyEvent.TapeSeek(frame, cycle, index)
            : string.Equals(control, "insert", StringComparison.Ordinal) && image is not null
                ? ReplayKeyEvent.TapeInsert(frame, cycle, image)
                : ReplayKeyEvent.Tape(frame, cycle, control, true);
        RecordAppliedObservation(frame, transport);
    }

    private void RecordObservedLightPenState(bool pressed)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        RecordAppliedObservation(frame, ReplayKeyEvent.LightPenState(frame, cycle, pressed));
    }

    private void RecordObservedLightPenTrigger()
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        // TriggerLightPen calls the recorder before it latches, so this
        // image does not contain $D013. Seek must apply the event.
        // Recorders that run after the device change still skip it.
        RememberPeriodicSlot(frame, _boundMachine);
        InsertEvent(ReplayKeyEvent.LightPenTrigger(frame, cycle));
    }

    private void RecordObservedLightPenTiming(int x, int y)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        RecordAppliedObservation(frame, ReplayKeyEvent.LightPenTiming(frame, cycle, x, y));
    }

    private void RecordObservedKey(string key, bool pressed)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        if (string.IsNullOrEmpty(key))
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        RecordAppliedObservation(frame, new ReplayKeyEvent(frame, cycle, key, pressed, CopyHostKeyCodes(_boundMachine, key)));
    }

    private static bool HostKeyAlreadyAtLevel(IMachine machine, string key, bool pressed)
    {
        if (machine.Devices.GetByRole(DeviceRole.SystemRam) is not C64MemoryMap map)
            return false;

        return map.IsHostKeyHeld(key) == pressed;
    }

    private static byte[]? CopyHostKeyCodes(IMachine? machine, string key)
    {
        if (machine is not null
            && machine.Devices.GetByRole(DeviceRole.SystemRam) is C64MemoryMap map
            && map.TryCopyHostKeyCodes(key, out var resolved))
            return resolved;

        return null;
    }

    private void RecordObservedMatrixKey(byte keyCode, bool pressed)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        RecordAppliedObservation(frame, ReplayKeyEvent.Matrix(frame, cycle, keyCode, pressed));
    }

    private void RecordObservedRestore(bool pressed)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        RecordAppliedObservation(frame, ReplayKeyEvent.Restore(frame, cycle, pressed));
    }

    private void RecordObservedPot(int port, byte x, byte y)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        RecordAppliedObservation(frame, ReplayKeyEvent.Pot(frame, cycle, port, x, y));
    }

    private void RecordObservedMouse(int port, int deltaX, int deltaY)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        RecordAppliedObservation(frame, ReplayKeyEvent.Mouse(frame, cycle, port, deltaX, deltaY));
    }

    private void RecordObservedCartridge(bool attached, byte[]? image, int mapping)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        // Attach and eject notify after the image changes. The slot already
        // contains that image, so seek must not apply the edge again.
        RecordAppliedObservation(frame, ReplayKeyEvent.Cartridge(frame, cycle, attached, mapping, image));
    }

    private void RecordObservedJoystick(int port, byte direction, bool fire)
    {
        if (_suppressObservedInput > 0 || _transport == ReplayTransportMode.Playing || _boundMachine is null)
            return;

        var frameCycles = FrameCycles(_boundMachine);
        var total = _boundMachine.Clock.TotalCycles;
        var frame = frameCycles > 0 ? (int)(total / frameCycles) : 0;
        var cycle = frameCycles > 0 ? (int)(total % frameCycles) : (int)total;
        RecordAppliedObservation(frame, ReplayKeyEvent.Joystick(frame, cycle, port, direction, fire));
    }

    private void ApplyKey(IMachine machine, ReplayKeyEvent evt)
    {
        // A stored event is playback. Recording it again grows the stream
        // and applies the mouse delta twice. VICE playback does not append.
        _suppressObservedInput++;
        try
        {
            ApplyStoredInput(machine, evt);
        }
        finally
        {
            _suppressObservedInput--;
        }
    }

    private static void ApplyStoredInput(IMachine machine, ReplayKeyEvent evt)
    {
        if (evt.IsCartridge)
        {
            if (machine.Devices.GetByRole(DeviceRole.CartridgePort) is C64MemoryMap cartridge)
            {
                if (evt.Pressed && evt.CartridgeImage is not null)
                    cartridge.AttachCartridge(evt.CartridgeImage, (CartridgeMappingMode)evt.DeltaX);
                else
                    cartridge.EjectCartridge();
            }

            return;
        }

        if (evt.IsLightPen)
        {
            if (machine.Devices.GetByRole(DeviceRole.VideoChip) is Mos6569 vic)
            {
                if (evt.IsLightPenTiming)
                    vic.SetLightPenTiming(evt.DeltaX, evt.DeltaY);
                else if (evt.IsLightPenTrigger)
                    vic.TriggerLightPen();
                else
                    vic.SetLightPen(evt.Pressed);
            }

            return;
        }

        if (evt.IsRestore)
        {
            if (machine.Devices.GetByRole(DeviceRole.SystemRam) is C64MemoryMap restoreMap)
                restoreMap.SetRestoreState(evt.Pressed);
            return;
        }

        if (evt.IsTape)
        {
            if (machine.Devices.GetByRole(DeviceRole.SystemRam) is C64MemoryMap tapeMap
                && tapeMap.Datasette is { } tape)
            {
                if (string.Equals(evt.Key, "rewind", StringComparison.Ordinal))
                    tape.Rewind();
                else if (string.Equals(evt.Key, "eject", StringComparison.Ordinal))
                    tape.EjectTape();
                else if (string.Equals(evt.Key, "detach", StringComparison.Ordinal))
                    tape.Detach();
                else if (string.Equals(evt.Key, "attach", StringComparison.Ordinal))
                    tape.Attach();
                else if (string.Equals(evt.Key, "seek", StringComparison.Ordinal))
                    tape.SeekTo(evt.DeltaX);
                else if (string.Equals(evt.Key, "insert", StringComparison.Ordinal) && evt.TapeImage is not null)
                    tape.InsertTape(evt.TapeImage);
                else if (string.Equals(evt.Key, "record", StringComparison.Ordinal))
                    tape.RecordPressed = evt.Pressed;
                else
                    tape.PlayPressed = evt.Pressed;
            }

            return;
        }

        if (evt.IsPot)
        {
            if (machine.Devices.GetByRole(DeviceRole.SystemRam) is C64MemoryMap potMap)
                potMap.SetPotState(evt.Port, evt.PotX, evt.PotY);
            return;
        }

        if (evt.IsMouse)
        {
            if (machine.Devices.GetByRole(DeviceRole.SystemRam) is C64MemoryMap map)
                map.ApplyMouseDelta(evt.Port, evt.DeltaX, evt.DeltaY);
            return;
        }

        if (evt.IsMatrix)
        {
            if (machine.Devices.GetByRole(DeviceRole.SystemRam) is IKeyboardMatrix matrix
                && byte.TryParse(evt.Key, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var keyCode))
                matrix.SetKey(keyCode, evt.Pressed);
            return;
        }

        if (evt.IsJoystick)
        {
            var joysticks = machine.Devices.GetAll<IMachineJoystickInput>();
            for (var i = 0; i < joysticks.Count; i++)
                joysticks[i].SetJoystickState(evt.Port, evt.Direction, evt.Fire);
            return;
        }

        if (evt.KeyCodes is { Length: > 0 }
            && machine.Devices.GetByRole(DeviceRole.SystemRam) is C64MemoryMap codedMap)
        {
            codedMap.ApplyRecordedKeyCodes(evt.Key, evt.KeyCodes, evt.Pressed);
            return;
        }

        var keyboards = machine.Devices.GetAll<IMachineKeyboardInput>();
        for (var i = 0; i < keyboards.Count; i++)
            keyboards[i].SetKeyState(evt.Key, evt.Pressed);
    }

    private static bool HappensBefore(ReplayKeyEvent candidate, ReplayKeyEvent existing)
    {
        if (candidate.Frame != existing.Frame)
            return candidate.Frame < existing.Frame;

        return candidate.Cycle < existing.Cycle;
    }

    private static int NextPeriodicFrame(int frame)
    {
        if (frame < 0)
            return 0;

        var bucket = frame / PeriodicFrameInterval;
        if (bucket >= (int.MaxValue / PeriodicFrameInterval) - 1)
            return frame;

        return (bucket + 1) * PeriodicFrameInterval;
    }

    private static string TransportName(ReplayTransportMode transport) => transport switch
    {
        ReplayTransportMode.Playing => "playing",
        ReplayTransportMode.Paused => "paused",
        ReplayTransportMode.FastForward => "fast-forward",
        _ => "stopped"
    };

    private static string Sanitize(string key)
    {
        if (key.IndexOf('\n') < 0 && key.IndexOf('\r') < 0)
            return key;

        return key.Replace('\r', ' ').Replace('\n', ' ');
    }

    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);

    private readonly struct ReplayKeyEvent
    {
        public ReplayKeyEvent(int frame, int cycle, string key, bool pressed, byte[]? keyCodes = null)
        {
            Frame = frame;
            Cycle = cycle;
            Key = key;
            Pressed = pressed;
            KeyCodes = keyCodes;
        }

        public int Frame { get; }

        public int Cycle { get; }

        public string Key { get; }

        public byte[]? KeyCodes { get; }

        public bool Pressed { get; }

        public bool IsJoystick { get; }

        public bool IsMouse { get; }

        public bool IsPot { get; }

        public bool IsRestore { get; }

        public bool IsMatrix { get; }

        public bool IsLightPen { get; }

        public bool IsLightPenTrigger { get; }

        public bool IsLightPenTiming { get; }

        public bool IsTape { get; }

        public byte PotX { get; }

        public byte PotY { get; }

        public int Port { get; }

        public byte Direction { get; }

        public bool Fire { get; }

        public int DeltaX { get; }

        public int DeltaY { get; }

        public byte[]? TapeImage { get; }

        public bool IsCartridge { get; }

        public byte[]? CartridgeImage { get; }

        public static ReplayKeyEvent Joystick(int frame, int cycle, int port, byte direction, bool fire)
            => new(frame, cycle, port, direction, fire);

        public static ReplayKeyEvent Mouse(int frame, int cycle, int port, int deltaX, int deltaY)
            => new(frame, cycle, port, deltaX, deltaY);

        public static ReplayKeyEvent Pot(int frame, int cycle, int port, byte x, byte y)
            => new(frame, cycle, port, x, y);

        public static ReplayKeyEvent Matrix(int frame, int cycle, byte keyCode, bool pressed)
            => new(frame, cycle, keyCode, pressed, matrix: true);

        public static ReplayKeyEvent Restore(int frame, int cycle, bool pressed)
            => new(frame, cycle, pressed, restore: true);

        public static ReplayKeyEvent LightPenState(int frame, int cycle, bool pressed)
            => new(frame, cycle, pressed, lightPenKind: 0);

        public static ReplayKeyEvent LightPenTrigger(int frame, int cycle)
            => new(frame, cycle, false, lightPenKind: 1);

        public static ReplayKeyEvent LightPenTiming(int frame, int cycle, int x, int y)
            => new(frame, cycle, x, y, lightPenTiming: true);

        public static ReplayKeyEvent Tape(int frame, int cycle, string control, bool pressed)
            => new(frame, cycle, control, pressed, tape: true);

        public static ReplayKeyEvent TapeSeek(int frame, int cycle, int index)
            => new(frame, cycle, "seek", true, tape: true, seekIndex: index);

        public static ReplayKeyEvent TapeInsert(int frame, int cycle, byte[] image)
            => new(frame, cycle, "insert", true, tape: true, image: image);

        public static ReplayKeyEvent Cartridge(int frame, int cycle, bool attached, int mapping, byte[]? image)
            => new(frame, cycle, attached, mapping, image, cartridge: true);

        private ReplayKeyEvent(int frame, int cycle, int port, byte direction, bool fire)
        {
            Frame = frame;
            Cycle = cycle;
            IsJoystick = true;
            Port = port;
            Direction = direction;
            Fire = fire;
            Key = string.Empty;
        }

        private ReplayKeyEvent(int frame, int cycle, int port, int deltaX, int deltaY)
        {
            Frame = frame;
            Cycle = cycle;
            IsMouse = true;
            Port = port;
            DeltaX = deltaX;
            DeltaY = deltaY;
            Key = string.Empty;
        }

        private ReplayKeyEvent(int frame, int cycle, int port, byte x, byte y)
        {
            Frame = frame;
            Cycle = cycle;
            IsPot = true;
            Port = port;
            PotX = x;
            PotY = y;
            Key = string.Empty;
        }

        private ReplayKeyEvent(int frame, int cycle, bool pressed, bool restore)
        {
            Frame = frame;
            Cycle = cycle;
            IsRestore = restore;
            Pressed = pressed;
            Key = string.Empty;
        }

        private ReplayKeyEvent(int frame, int cycle, byte keyCode, bool pressed, bool matrix)
        {
            Frame = frame;
            Cycle = cycle;
            IsMatrix = matrix;
            Pressed = pressed;
            Key = keyCode.ToString("X2", System.Globalization.CultureInfo.InvariantCulture);
        }

        private ReplayKeyEvent(int frame, int cycle, bool pressed, int lightPenKind)
        {
            Frame = frame;
            Cycle = cycle;
            IsLightPen = true;
            IsLightPenTrigger = lightPenKind != 0;
            Pressed = pressed;
            Key = string.Empty;
        }

        private ReplayKeyEvent(int frame, int cycle, int x, int y, bool lightPenTiming)
        {
            Frame = frame;
            Cycle = cycle;
            IsLightPen = true;
            IsLightPenTiming = lightPenTiming;
            DeltaX = x;
            DeltaY = y;
            Key = string.Empty;
        }

        private ReplayKeyEvent(
            int frame,
            int cycle,
            string control,
            bool pressed,
            bool tape,
            int seekIndex = 0,
            byte[]? image = null)
        {
            Frame = frame;
            Cycle = cycle;
            IsTape = tape;
            Pressed = pressed;
            Key = control;
            DeltaX = seekIndex;
            TapeImage = image;
        }

        private ReplayKeyEvent(int frame, int cycle, bool attached, int mapping, byte[]? image, bool cartridge)
        {
            Frame = frame;
            Cycle = cycle;
            IsCartridge = cartridge;
            Pressed = attached;
            DeltaX = mapping;
            CartridgeImage = image is null ? null : (byte[])image.Clone();
            Key = string.Empty;
        }
    }

    private readonly struct ReplaySlot
    {
        public ReplaySlot(int frame, int firstEventIndex, RuntimeSnapshot? snapshot)
        {
            Frame = frame;
            FirstEventIndex = firstEventIndex;
            Snapshot = snapshot;
        }

        public int Frame { get; }

        public int FirstEventIndex { get; }

        public RuntimeSnapshot? Snapshot { get; }
    }

    private enum ReplayTransportMode
    {
        Stopped = 0,
        Playing = 1,
        Paused = 2,
        FastForward = 3
    }

    private static RuntimeSnapshot Materialize(ISnapshot snapshot)
    {
        if (snapshot is RuntimeSnapshot runtimeSnapshot)
            return runtimeSnapshot;

        var data = new byte[snapshot.GetSerializedSize()];
        snapshot.Serialize(data);
        var materialized = new RuntimeSnapshot();
        materialized.Deserialize(data);
        return materialized;
    }
}
