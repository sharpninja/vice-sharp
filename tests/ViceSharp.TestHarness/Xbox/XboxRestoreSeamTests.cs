namespace ViceSharp.TestHarness.Xbox;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViceSharp.Abstractions;
using ViceSharp.Core.Input;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S24 (IMPL-XBOXUWP-024), area XKBD. Exercises the dedicated
/// RESTORE/NMI keyboard seam that lets the virtual-keyboard RESTORE tile fire the C64
/// RESTORE line WITHOUT going through the ordinary 8x8 key matrix. A C64 keyboard is
/// incomplete without RESTORE, and RESTORE is a hardware NMI (wired straight to the CPU
/// through a monostable) rather than a matrix key: pressing "*" (keycode 0x31) must NOT
/// fire RESTORE, and RESTORE must only fire through the dedicated seam.
///
/// TEST-XKBD-001, FR-XKBD-001, TR-XKBD-001. Tier H, off-console, deterministic:
/// the seam adds an input push path only, so Category=Determinism is unaffected.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxRestoreSeamTests
{
    /// <summary>
    /// FR-XKBD-001, TR-XKBD-001. TEST-XKBD-001.
    /// Use case: the in-process facade's RESTORE seam drives the machine's dedicated
    /// RESTORE/NMI trigger, while the ordinary key path drives SetKeyState.
    /// Acceptance: against a spy <see cref="IMachineKeyboardInput"/> injected into a
    /// session, <c>IConsoleEmulatorHost.SetRestoreState</c> reaches the device's
    /// <see cref="IMachineKeyboardInput.SetRestoreState"/> and never
    /// <see cref="IMachineKeyboardInput.SetKeyState"/>; <c>SetKey</c> reaches
    /// <c>SetKeyState</c> and never the RESTORE seam.
    /// </summary>
    [Fact]
    public async Task Facade_SetRestoreState_RoutesToDedicatedSeam_NotSetKeyState()
    {
        var spy = new SpyMachineKeyboardInput();
        await using var host = ConsoleHostComposition.Build(MinimalDependencies());
        IConsoleEmulatorHost facade = host;

        host.Registry.Add(new EmulatorRuntimeSession(
            "restore-session",
            MinimalHostArchitectureDescriptor.Instance,
            new RestoreSeamFakeMachine(spy)));

        // The dedicated RESTORE seam reaches SetRestoreState (the NMI path), never the
        // ordinary key matrix.
        facade.SetRestoreState("restore-session", true);
        facade.SetRestoreState("restore-session", false);

        Assert.Equal(new[] { true, false }, spy.RestoreTransitions);
        Assert.Empty(spy.KeyStateTransitions);

        // The ordinary key path (SetKey -> SetKeyState) never fires the RESTORE seam,
        // even for "*", which the default map binds to keycode 0x31.
        facade.SetKey("restore-session", "*", true);

        Assert.Equal(new[] { ("*", true) }, spy.KeyStateTransitions);
        Assert.Equal(new[] { true, false }, spy.RestoreTransitions); // unchanged by SetKey
    }

    /// <summary>
    /// FR-XKBD-001, TR-XKBD-001. TEST-XKBD-001.
    /// Use case: the real C64 keyboard matrix must expose a RESTORE trigger that is
    /// independent of the "*" matrix cell (keycode 0x31).
    /// Acceptance: <c>SetRestore(true)</c> raises <c>IsRestorePressed</c>; pressing
    /// keycode 0x31 drives the matrix cell (row 6, col 1) low but leaves
    /// <c>IsRestorePressed</c> false. ROM-free and deterministic.
    /// </summary>
    [Fact]
    public void C64KeyboardMatrix_RestoreSeam_FiresTrigger_WhileAsteriskKeyDoesNot()
    {
        var matrix = new C64KeyboardMatrix();
        matrix.Reset();

        // The dedicated RESTORE seam sets (and clears) the RESTORE/NMI trigger.
        matrix.SetRestore(true);
        Assert.True(matrix.IsRestorePressed);
        matrix.SetRestore(false);
        Assert.False(matrix.IsRestorePressed);

        // The ordinary "*" key (keycode 0x31 -> matrix row 6, col 1) drives the key
        // matrix but must NOT fire the RESTORE trigger: RESTORE is not a matrix cell.
        matrix.SetKey(0x31, true);
        Assert.False(matrix.IsRestorePressed);

        // The "*" press really landed on the matrix: selecting column 1 pulls row 6 low.
        matrix.SetColumnMask(unchecked((byte)~(1 << 1)));
        Assert.Equal(0, matrix.ReadRowState() & (1 << 6));
    }

    /// <summary>
    /// FR-XKBD-001, TR-XKBD-001. TEST-XKBD-001.
    /// Use case: through the host input service on a real C64 machine, the RESTORE seam
    /// fires the actual RESTORE/NMI trigger while pressing "*" does not.
    /// Acceptance: <c>SetRestoreStateAsync(true)</c> raises the C64
    /// <see cref="IKeyboardMatrix.IsRestorePressed"/>; <c>SetRestoreStateAsync(false)</c>
    /// clears it; <c>SetKeyStateAsync("*", true)</c> is applied to the runtime yet leaves
    /// <c>IsRestorePressed</c> false. Uses the repo-vendored VICE C64 ROMs (no skip).
    /// </summary>
    [Fact]
    public async Task InputServiceHost_SetRestoreState_FiresC64RestoreTrigger_WhileAsteriskDoesNot()
    {
        var ct = TestContext.Current.CancellationToken;
        var registry = new EmulatorRuntimeRegistry();
        var machine = MachineTestFactory.CreateC64Machine();
        registry.Add(new EmulatorRuntimeSession(
            "c64-restore",
            MinimalHostArchitectureDescriptor.Instance,
            machine));
        var service = new InputServiceHost(registry);
        var matrix = machine.Devices.All.OfType<IKeyboardMatrix>().Single();

        // The dedicated seam fires the real C64 RESTORE trigger (the NMI path that
        // keycode 0x31's flag used to share), observable via IsRestorePressed.
        var pressed = await service.SetRestoreStateAsync("c64-restore", true, ct);
        Assert.Equal(RpcStatusCode.Ok, pressed.Status.Code);
        Assert.True(matrix.IsRestorePressed);

        var released = await service.SetRestoreStateAsync("c64-restore", false, ct);
        Assert.Equal(RpcStatusCode.Ok, released.Status.Code);
        Assert.False(matrix.IsRestorePressed);

        // Pressing "*" (default map -> keycode 0x31) through the ordinary keyboard path
        // is applied to the runtime but must NOT fire RESTORE.
        var star = await service.SetKeyStateAsync(
            new SetKeyStateRequest("c64-restore", "*", true), ct);
        Assert.Equal(RpcStatusCode.Ok, star.Status.Code);
        Assert.Contains(star.InputState!.Keys, key => key.Key == "*" && key.IsPressed && key.AppliedToRuntime);
        Assert.False(matrix.IsRestorePressed);
    }

    /// <summary>
    /// FIX-XKBDNMI-001 / FR-XKBD-001.
    /// Use case: pressing RESTORE must assert the open-drain NMI line (edge-latched by
    /// SystemClock into a real CPU NMI); holding must not re-assert; release clears;
    /// "*" still never touches NMI.
    /// Acceptance: isolated matrix+line unit proves Assert/Release; on a live C64,
    /// SetRestoreState(true) makes the NMI line asserted via the keyboard source and
    /// a subsequent instruction-boundary step consumes the latched NMI (PC vectors to
    /// the NMI handler at $FFFA/$FFFB after the next boundary).
    /// </summary>
    [Fact]
    public void Restore_AssertsNmiLine_AndEdgeLatchesCpuNmi()
    {
        // Unit: ConnectNmiLine + SetRestore drives the open-drain line.
        var line = new ViceSharp.Core.InterruptLine(InterruptType.Nmi);
        var matrix = new C64KeyboardMatrix();
        matrix.ConnectNmiLine(line);

        Assert.False(line.IsAsserted);
        matrix.SetRestore(true);
        Assert.True(matrix.IsRestorePressed);
        Assert.True(line.IsAsserted);

        // Hold does not re-toggle the source (still asserted once).
        matrix.SetRestore(true);
        Assert.True(line.IsAsserted);

        matrix.SetRestore(false);
        Assert.False(matrix.IsRestorePressed);
        Assert.False(line.IsAsserted);

        matrix.SetKey(0x31, true);
        Assert.False(line.IsAsserted);

        // Integration: live C64 machine wires RESTORE through C64MemoryMap to the NMI line.
        var machine = MachineTestFactory.CreateC64Machine();
        var liveMatrix = machine.Devices.All.OfType<IKeyboardMatrix>().Single();
        var keyboardInput = machine.Devices.All.OfType<IMachineKeyboardInput>().Single();

        // Warm a few frames so we are past reset vectors and at instruction boundaries.
        for (var i = 0; i < 5; i++)
            machine.RunFrame();

        var cpu = machine.Devices.All.OfType<ICpu>().Single();
        var pcBefore = cpu.PC;

        keyboardInput.SetRestoreState(true);
        Assert.True(liveMatrix.IsRestorePressed);

        // Advance enough instructions for the edge latch + instruction-boundary NMI service.
        // NMI takes the vector at $FFFA/$FFFB; after service PC lands off the prior stream.
        for (var i = 0; i < 32; i++)
            machine.StepInstruction();

        // NMI service should move PC off the pre-press stream (vector fetch).
        Assert.NotEqual(pcBefore, cpu.PC);

        keyboardInput.SetRestoreState(false);
        Assert.False(liveMatrix.IsRestorePressed);
    }

    private static ConsoleHostDependencies MinimalDependencies() =>
        new([MinimalHostArchitectureDescriptor.Instance], MinimalHostArchitectureDescriptor.ArchitectureId);

    /// <summary>
    /// Spy keyboard input that records SetKeyState and SetRestoreState calls on separate
    /// channels so the test can prove the two paths never cross.
    /// </summary>
    private sealed class SpyMachineKeyboardInput : IMachineKeyboardInput
    {
        public DeviceId Id => new(0x9A24);

        public string Name => "Spy Machine Keyboard Input";

        public List<(string Key, bool Pressed)> KeyStateTransitions { get; } = new();

        public List<bool> RestoreTransitions { get; } = new();

        public bool SetKeyState(string key, bool pressed)
        {
            KeyStateTransitions.Add((key, pressed));
            return true;
        }

        public bool SetRestoreState(bool pressed)
        {
            RestoreTransitions.Add(pressed);
            return true;
        }

        public void Reset()
        {
            KeyStateTransitions.Clear();
            RestoreTransitions.Clear();
        }
    }

    private sealed class RestoreSeamFakeMachine : IMachine
    {
        public RestoreSeamFakeMachine(IMachineKeyboardInput keyboard)
        {
            Devices = new RestoreSeamDeviceRegistry(keyboard);
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

    private sealed class RestoreSeamDeviceRegistry : IDeviceRegistry
    {
        private readonly IReadOnlyList<IDevice> _devices;

        public RestoreSeamDeviceRegistry(params IDevice[] devices)
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
}
