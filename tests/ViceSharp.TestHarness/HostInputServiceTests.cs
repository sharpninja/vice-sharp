namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

public sealed class HostInputServiceTests
{
    [Fact]
    public void C64Machine_ExposesKeyboardMatrixToHostInput()
    {
        var machine = MachineTestFactory.CreateC64Machine();

        Assert.Single(machine.Devices.All.OfType<IKeyboardMatrix>());
        Assert.Single(machine.Devices.All.OfType<IMachineKeyboardInput>());
        Assert.Single(machine.Devices.All.OfType<IMachineJoystickInput>());
    }

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
