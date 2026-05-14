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
    }

    [Fact]
    public async Task SetKeyState_DelegatesToMachineKeyboardInput()
    {
        var keyboard = new RecordingMachineKeyboardInput();
        var service = CreateServiceWithKeyboardInput(keyboard);

        var down = await service.SetKeyStateAsync(
            new SetKeyStateRequest("test-session", "Space", true),
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
        Assert.Contains(down.InputState!.Keys, key => key.Key == "Space" && key.IsPressed && key.AppliedToRuntime);
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
