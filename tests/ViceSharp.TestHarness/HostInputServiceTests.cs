namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

public sealed class HostInputServiceTests
{
    /// <summary>
    /// FR: FR-INP-001, FR: FR-INP-002, TR: TR-CYCLE-001.
    /// Use case: The standard C64 machine must expose exactly one
    /// keyboard matrix, one machine keyboard input, and one machine
    /// joystick input so host services have a single owner per role.
    /// Acceptance: <c>IKeyboardMatrix</c>, <c>IMachineKeyboardInput</c>
    /// and <c>IMachineJoystickInput</c> are each present exactly once.
    /// </summary>
    [Fact]
    public void C64Machine_ExposesKeyboardMatrixToHostInput()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        Assert.Single(machine.Devices.All.OfType<IKeyboardMatrix>());
        Assert.Single(machine.Devices.All.OfType<IMachineKeyboardInput>());
        Assert.Single(machine.Devices.All.OfType<IMachineJoystickInput>());
    }

    /// <summary>
    /// FR: FR-HOST-004, FR: FR-INP-001, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: The InputServiceHost must delegate each SetKeyState
    /// call to the underlying IMachineKeyboardInput, preserving the
    /// physical-key/text/modifier metadata for diagnostics.
    /// Acceptance: Down/repeat/up sequences each return Ok; the
    /// returned input state mirrors the metadata; the recording
    /// keyboard observes exactly the press-release transition pair.
    /// </summary>
    [Fact]
    public async Task SetKeyState_DelegatesToMachineKeyboardInput()
    {
        var keyboard = new RecordingMachineKeyboardInput();
        var service = CreateServiceWithKeyboardInput(keyboard);

        var down = await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "Space", true, "Space", " ", 1),
            TestContext.Current.CancellationToken);
        var repeat = await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "Space", true),
            TestContext.Current.CancellationToken);
        var up = await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "Space", false),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, down.Status.Code);
        Assert.Equal(RpcStatusCode.Ok, repeat.Status.Code);
        Assert.Equal(RpcStatusCode.Ok, up.Status.Code);
        Assert.Contains(down.InputState!.Keys, key =>
            key.Key == "Space" &&
            key.IsPressed &&
            key.AppliedToRuntime &&
            key.PhysicalKey == "Space" &&
            key.Text == " " &&
            key.Modifiers == 1);
        Assert.Contains(up.InputState!.Keys, key => key.Key == "Space" && !key.IsPressed && key.AppliedToRuntime);
        Assert.Equal(
            [new KeyTransition("Space", true), new KeyTransition("Space", false)],
            keyboard.Transitions);
    }

    /// <summary>
    /// FR: FR-INP-001, FR: FR-CIA-003, TR: TR-CYCLE-001.
    /// Use case: Pressing Space through the InputServiceHost must drive
    /// the CIA1 keyboard matrix so the standard CIA1 scan reads the
    /// pressed cell low and reads it high again after release.
    /// Acceptance: With $DC01=$EF and Space pressed, $DC00 bit 7 reads
    /// low; after releasing Space, the same scan returns bit 7 high.
    /// </summary>
    [Fact]
    public async Task SetKeyState_UsesC64KeyboardInputToDriveCiaMatrixScan()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        registry.Add(new EmulatorRuntimeSession(
            "test-session",
            MinimalHostArchitectureDescriptor.Instance,
            machine));
        var service = new InputServiceHost(registry);

        await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "Space", true),
            TestContext.Current.CancellationToken);

        machine.Bus.Write(0xDC01, 0xEF);
        Assert.Equal(0, machine.Bus.Read(0xDC00) & 0x80);

        await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "Space", false),
            TestContext.Current.CancellationToken);

        machine.Bus.Write(0xDC01, 0xEF);
        Assert.Equal(0x80, machine.Bus.Read(0xDC00) & 0x80);
    }

    /// <summary>
    /// FR: FR-INP-001, FR: FR-INP-006, TR: TR-CYCLE-001.
    /// Use case: Composite/shifted keys (e.g. Left = Shift + cursor)
    /// drive both the cursor key cell AND the shift cell; the shift
    /// must remain pressed until every composite key using it is
    /// released.
    /// Acceptance: While LeftShift is held and after Left is released,
    /// scanning column $7F still reports shift bit 1 low; only after
    /// LeftShift is released does the same scan return bit 1 high.
    /// </summary>
    [Fact]
    public async Task SetKeyState_LeavesCompositeShiftPressedUntilAllMappedKeysRelease()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        registry.Add(new EmulatorRuntimeSession(
            "test-session",
            MinimalHostArchitectureDescriptor.Instance,
            machine));
        var service = new InputServiceHost(registry);

        await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "LeftShift", true),
            TestContext.Current.CancellationToken);
        await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "Left", true),
            TestContext.Current.CancellationToken);
        await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "Left", false),
            TestContext.Current.CancellationToken);

        machine.Bus.Write(0xDC01, 0x7F);
        Assert.Equal(0, machine.Bus.Read(0xDC00) & 0x02);

        await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "LeftShift", false),
            TestContext.Current.CancellationToken);

        machine.Bus.Write(0xDC01, 0x7F);
        Assert.Equal(0x02, machine.Bus.Read(0xDC00) & 0x02);
    }

    /// <summary>
    /// FR: FR-INP-002, FR: FR-CIA-004, TR: TR-CYCLE-001.
    /// Use case: Joystick 2 must drive CIA1 port A bits 0 (up) and 4
    /// (fire); the InputServiceHost forwards the joystick state into
    /// the runtime where the bus reflects the new lines.
    /// Acceptance: Pressing direction $01 with fire returns Ok and the
    /// CIA1 $DC00 read shows bits 0 and 4 cleared; releasing returns
    /// them to the idle pattern.
    /// </summary>
    [Fact]
    public async Task SetJoystickState_UsesC64Joystick2ToDriveCia1PortA()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        registry.Add(new EmulatorRuntimeSession(
            "test-session",
            MinimalHostArchitectureDescriptor.Instance,
            machine));
        var service = new InputServiceHost(registry);

        var pressed = await service.SetJoystickStateAsync(
            new SetJoystickStateRequest("test-session", InputPort.Joystick2, 0x01, true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, pressed.Status.Code);
        Assert.Contains(pressed.InputState!.Joysticks, state =>
            state.Port == InputPort.Joystick2 &&
            state.State.DirectionMask == 0x01 &&
            state.State.FireButton &&
            state.State.AppliedToRuntime);
        Assert.Equal(0, machine.Bus.Read(0xDC00) & 0x11);

        var released = await service.SetJoystickStateAsync(
            new SetJoystickStateRequest("test-session", InputPort.Joystick2, 0x00, false),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, released.Status.Code);
        Assert.Equal(0x11, machine.Bus.Read(0xDC00) & 0x11);
    }

    /// <summary>
    /// FR/TR: FR-INPUT-JOYSTICK-001.
    /// Use case: A host service request targeting Joystick1 drives the C64's
    /// CIA1 port B input lines (control port 1) directly.
    /// Acceptance: After SetJoystickStateAsync(Joystick1, $02, fire=true),
    /// CIA1 PB reports the asserted directions + fire bit pulled low; after
    /// release the bits return to idle high.
    /// </summary>
    [Fact]
    public async Task SetJoystickState_UsesC64Joystick1ToDriveCia1PortB()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        registry.Add(new EmulatorRuntimeSession(
            "test-session",
            MinimalHostArchitectureDescriptor.Instance,
            machine));
        var service = new InputServiceHost(registry);

        var pressed = await service.SetJoystickStateAsync(
            new SetJoystickStateRequest("test-session", InputPort.Joystick1, 0x02, true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, pressed.Status.Code);
        Assert.Contains(pressed.InputState!.Joysticks, state =>
            state.Port == InputPort.Joystick1 &&
            state.State.DirectionMask == 0x02 &&
            state.State.FireButton &&
            state.State.AppliedToRuntime);
        Assert.Equal(0, machine.Bus.Read(0xDC01) & 0x12);

        var released = await service.SetJoystickStateAsync(
            new SetJoystickStateRequest("test-session", InputPort.Joystick1, 0x00, false),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, released.Status.Code);
        Assert.Equal(0x12, machine.Bus.Read(0xDC01) & 0x12);
    }

    /// <summary>
    /// FR/TR: FR-INPUT-JOYSTICK-002.
    /// Use case: A request targeting PrimaryJoystick routes via the session's
    /// configured PrimaryJoystickPort to the matching CIA1 register.
    /// Acceptance: With PrimaryJoystickPort = Joystick1, asserting state on
    /// PrimaryJoystick drives CIA1 PA (control port 2) and leaves CIA1 PB
    /// untouched.
    /// </summary>
    [Fact]
    public async Task SetJoystickState_UsesPrimaryJoystickSettingToSelectRuntimeControlPort()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        registry.Add(new EmulatorRuntimeSession(
            "test-session",
            MinimalHostArchitectureDescriptor.Instance,
            machine)
        {
            InputSettings = new InputSettingsDto("c64:gtk3_pos", InputPort.Joystick1, false)
        });
        var service = new InputServiceHost(registry);

        var pressed = await service.SetJoystickStateAsync(
            new SetJoystickStateRequest("test-session", InputPort.PrimaryJoystick, 0x02, true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, pressed.Status.Code);
        Assert.Contains(pressed.InputState!.Joysticks, state =>
            state.Port == InputPort.PrimaryJoystick &&
            state.State.DirectionMask == 0x02 &&
            state.State.FireButton &&
            state.State.AppliedToRuntime);
        Assert.Equal(0, machine.Bus.Read(0xDC01) & 0x12);
        Assert.Equal(0x12, machine.Bus.Read(0xDC00) & 0x12);
    }

    /// <summary>
    /// FR/TR: FR-INPUT-JOYSTICK-003.
    /// Use case: When a session has Joystick1/Joystick2 explicit + ports
    /// swapped, only the PrimaryJoystick alias respects the swap; explicit
    /// Joystick1 / Joystick2 requests stay on their physical port.
    /// Acceptance: Asserting Joystick1 still drives CIA1 PB regardless of
    /// the swap flag.
    /// </summary>
    [Fact]
    public async Task SetJoystickState_KeepsExplicitJoystickPortsPhysicalWhenSwapEnabled()
    {
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        registry.Add(new EmulatorRuntimeSession(
            "test-session",
            MinimalHostArchitectureDescriptor.Instance,
            machine)
        {
            InputSettings = new InputSettingsDto("c64:gtk3_pos", InputPort.Joystick2, true)
        });
        var service = new InputServiceHost(registry);

        var pressed = await service.SetJoystickStateAsync(
            new SetJoystickStateRequest("test-session", InputPort.Joystick2, 0x01, true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, pressed.Status.Code);
        Assert.Contains(pressed.InputState!.Joysticks, state =>
            state.Port == InputPort.Joystick2 &&
            state.State.DirectionMask == 0x01 &&
            state.State.FireButton &&
            state.State.AppliedToRuntime);
        Assert.Equal(0, machine.Bus.Read(0xDC00) & 0x11);
        Assert.Equal(0x11, machine.Bus.Read(0xDC01) & 0x11);

        var released = await service.SetJoystickStateAsync(
            new SetJoystickStateRequest("test-session", InputPort.Joystick2, 0x00, false),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, released.Status.Code);
        Assert.Equal(0x11, machine.Bus.Read(0xDC00) & 0x11);
    }

    private static InputServiceHost CreateServiceWithKeyboardInput(IMachineKeyboardInput keyboard)
    {
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(new EmulatorRuntimeSession(
            "test-session",
            MinimalHostArchitectureDescriptor.Instance,
            new FakeMachine(new FakeDeviceRegistry(keyboard))));

        return new InputServiceHost(registry);
    }

    private sealed class FakeMachine : IMachine
    {
        public FakeMachine(IDeviceRegistry devices)
        {
            Devices = devices;
        }

        public IBus Bus => throw new NotSupportedException();

        public IClock Clock => throw new NotSupportedException();

        public IDeviceRegistry Devices { get; }

        public IArchitectureDescriptor Architecture => MinimalHostArchitectureDescriptor.Instance;

        public void RunFrame()
        {
        }

        public void StepInstruction()
        {
        }

        public MachineState GetState() => new();

        public void Reset()
        {
        }
    }

    private sealed class FakeDeviceRegistry : IDeviceRegistry
    {
        private readonly IReadOnlyList<IDevice> _devices;

        public FakeDeviceRegistry(params IDevice[] devices)
        {
            _devices = devices;
        }

        public IDevice? GetById(DeviceId id) => _devices.FirstOrDefault(device => device.Id == id);

        public IReadOnlyList<T> GetAll<T>()
            where T : IDevice
            => _devices.OfType<T>().ToArray();

        public IReadOnlyList<IDevice> All => _devices;

        public IDevice? GetByRole(DeviceRole role) => null;

        public int Count => _devices.Count;
    }

    private sealed class RecordingMachineKeyboardInput : IMachineKeyboardInput
    {
        public DeviceId Id => new(0x9001);

        public string Name => "Recording Machine Keyboard Input";

        public List<KeyTransition> Transitions { get; } = new();

        public bool SetKeyState(string key, bool pressed)
        {
            Transitions.Add(new KeyTransition(key, pressed));
            return true;
        }

        public void Reset()
        {
            Transitions.Clear();
        }
    }

    private sealed record KeyTransition(string Key, bool Pressed);
}
