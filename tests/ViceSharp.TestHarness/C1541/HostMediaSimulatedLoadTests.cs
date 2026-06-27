namespace ViceSharp.TestHarness.C1541;

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR/TR: FR-IECVDRIVE-001 / TEST-IECVDRIVE-005.
/// Reproduces the GUI's exact host flow for a True-Drive-OFF disk load: create a
/// simulated session through the runtime factory, attach the disk through
/// MediaServiceHost (not by poking the drive directly), then autostart LOAD.
/// This is the path the app actually exercises - the direct-InsertDisk test
/// bypassed the media service - so it catches any gap between "disk attached in
/// the UI" and "disk visible to the KERNAL serial trap".
/// </summary>
[Collection("NativeVice")]
public sealed class HostMediaSimulatedLoadTests
{
    private static readonly byte[] ProgramAt0801 =
    {
        0x07, 0x08, 0x0A, 0x00, 0x80, 0x00, 0x00, 0x00,
    };

    [Fact]
    public async System.Threading.Tasks.Task Factory_SimulatedSession_AttachDiskViaMediaService_ThenLoads()
    {
        var diskPath = WriteMinimalLoadableD64();
        try
        {
            var profile = C64MachineProfiles.C64Pal;
            var builder = new ArchitectureBuilder(MachineTestFactory.CreateC64RomProvider());
            var factory = new DefaultEmulatorRuntimeFactory(builder, new[] { new C64Descriptor(profile) }, profile.Id);
            var registry = new EmulatorRuntimeRegistry();
            var media = new MediaServiceHost(registry);

            var session = factory.Create(new CreateEmulatorSessionRequest(profile.Id, TrueDrive: false));
            session.Machine.Should().NotBeOfType<CoordinatorMachine>("True Drive OFF must be the simulated machine");
            registry.Add(session);

            var attach = await media.AttachMediaAsync(
                new AttachMediaRequest(session.SessionId, MediaSlot.Drive8, diskPath),
                TestContext.Current.CancellationToken);

            attach.Status.IsSuccess.Should().BeTrue();
            attach.Attachment!.AppliedToRuntime.Should().BeTrue(
                "the simulated drive must accept the disk at runtime so the serial trap can serve it");

            var automation = HostKeyboardAutomation.CreateC64Drive8Autostart();
            const int maxFrames = 2000;
            var loaded = false;
            for (var frame = 0; frame < maxFrames && !loaded; frame++)
            {
                session.Machine.RunFrame();
                automation.AdvanceFrame(session.Machine);
                loaded = session.Machine.Bus.Peek(0x0801) == 0x07
                    && session.Machine.Bus.Peek(0x0803) == 0x0A
                    && session.Machine.Bus.Peek(0x0805) == 0x80;
            }

            automation.LastError.Should().BeNullOrEmpty();
            loaded.Should().BeTrue(
                "attaching via MediaServiceHost then LOAD\"*\",8,1 must stream the program (the real app flow)");
        }
        finally
        {
            try { File.Delete(diskPath); } catch { /* best effort */ }
        }
    }

    /// <summary>
    /// FR: FR-DRV-001 / FR-IECLOAD-001 / BUG-THROTTLE-001, TR: TR-GRPC-BOUNDARY-001.
    /// Use case: the host "Run 8" command must exercise the C64 KERNAL LOAD path,
    ///   not preload PRG bytes from the D64 and then type RUN. It must also avoid
    ///   warp-mode physical-key drops by feeding the KERNAL keyboard buffer like VICE.
    /// Acceptance: after ResetAndAutostartDrive8 arms host automation at a READY
    ///   prompt, the KERNAL keyboard buffer starts with LOAD"*",8,1 rather than RUN.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task ResetAndAutostartDrive8_TypesLoadCommandInsteadOfPreloadingRun()
    {
        var drive = new IecDrive(8);
        drive.InsertDisk(CreateMinimalLoadableD64().ToArray());
        var bus = new AutostartMemoryBus();
        bus.SetBasicReady();
        var keyboard = new RecordingKeyboard();
        var machine = new AutostartTestMachine(bus, drive, keyboard);
        var session = new EmulatorRuntimeSession(
            "autostart-session",
            MinimalHostArchitectureDescriptor.Instance,
            machine)
        {
            RunState = EmulatorRunState.Running,
            PowerState = "On"
        };
        session.MediaAttachments[MediaSlot.Drive8] = new MediaAttachmentDto(
            MediaSlot.Drive8,
            "test.d64",
            "test.d64",
            IsAttached: true,
            IsReadOnly: true,
            AppliedToRuntime: true);
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        var host = new EmulatorHostService(registry, new ThrowingRuntimeFactory());

        var response = await host.ResetAndAutostartDrive8Async(
            new ResetAndAutostartDrive8Request(session.SessionId),
            TestContext.Current.CancellationToken);

        response.Status.IsSuccess.Should().BeTrue();
        for (var frame = 0; frame < 40 && bus.PendingKeyCount == 0; frame++)
            session.AdvanceHostAutomationFrame();

        bus.PendingKeyCount.Should().Be(10);
        bus.ReadKeyboardBuffer().Should().StartWith("LOAD\"*\",8,");
        keyboard.PressedKeys.Should().BeEmpty("Auto 8 feeds the KERNAL keyboard buffer instead of physical key toggles");
    }

    private static string WriteMinimalLoadableD64()
    {
        var image = CreateMinimalLoadableD64();
        var path = Path.Combine(Path.GetTempPath(), $"vicesharp-hostmedia-{Guid.NewGuid():N}.d64");
        File.WriteAllBytes(path, image.ToArray());
        return path;
    }

    private static D64Image CreateMinimalLoadableD64()
    {
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        image.Format();

        var directory = image.GetSector(18, 1);
        directory[0] = 0x00;
        directory[1] = 0xFF;
        directory[2] = 0x82;
        directory[3] = 17;
        directory[4] = 0;
        const string name = "TEST";
        for (var i = 0; i < 16; i++)
            directory[5 + i] = (byte)(i < name.Length ? name[i] : 0xA0);

        var file = image.GetSector(17, 0);
        var payload = new byte[] { 0x01, 0x08 }.Concat(ProgramAt0801).ToArray();
        file[0] = 0x00;
        file[1] = (byte)(2 + payload.Length - 1);
        payload.CopyTo(file.Slice(2));

        return image;
    }

    private sealed class AutostartMemoryBus : IBus
    {
        private const int ReadyPromptStart = 0x0400;
        private const int CursorBlinkEnableFlag = 0x00CC;
        private readonly byte[] _memory = new byte[0x10000];

        public byte Read(ushort address) => _memory[address];

        public void Write(ushort address, byte value) => _memory[address] = value;

        public byte Peek(ushort address) => _memory[address];

        public void RegisterDevice(IAddressSpace device)
        {
        }

        public void UnregisterDevice(IAddressSpace device)
        {
        }

        public void SetBasicReady()
        {
            ReadOnlySpan<byte> ready = [18, 5, 1, 4, 25];
            for (var i = 0; i < ready.Length; i++)
                _memory[ReadyPromptStart + i] = ready[i];
            _memory[CursorBlinkEnableFlag] = 0;
        }

        public int PendingKeyCount => _memory[0x00C6];

        public string ReadKeyboardBuffer()
            => new(_memory.Skip(0x0277).Take(PendingKeyCount).Select(b => (char)b).ToArray());
    }

    private sealed class AutostartTestMachine(
        IBus bus,
        IFloppyDrive drive,
        IMachineKeyboardInput keyboard) : IMachine
    {
        private readonly AutostartDeviceRegistry _devices = new(drive, keyboard);

        public IBus Bus => bus;

        public IClock Clock { get; } = new SystemClock(1_000_000);

        public IDeviceRegistry Devices => _devices;

        public IArchitectureDescriptor Architecture => MinimalHostArchitectureDescriptor.Instance;

        public void RunFrame() => Clock.Step(20_000);

        public void StepInstruction() => Clock.Step();

        public MachineState GetState() => new() { Cycle = Clock.TotalCycles };

        public void Reset() => Clock.Reset();
    }

    private sealed class AutostartDeviceRegistry(IFloppyDrive drive, IMachineKeyboardInput keyboard) : IDeviceRegistry
    {
        private readonly IDevice[] _devices = [drive, keyboard];

        public IReadOnlyList<IDevice> All => _devices;

        public int Count => _devices.Length;

        public IDevice? GetById(DeviceId id) => _devices.FirstOrDefault(device => device.Id == id);

        public IReadOnlyList<T> GetAll<T>()
            where T : IDevice
            => _devices.OfType<T>().ToArray();

        public IDevice? GetByRole(DeviceRole role) => null;
    }

    private sealed class RecordingKeyboard : IMachineKeyboardInput
    {
        public DeviceId Id => new(0xB001);

        public string Name => "Recording Keyboard";

        public List<string> PressedKeys { get; } = new();

        public bool SetKeyState(string key, bool pressed)
        {
            if (pressed)
                PressedKeys.Add(key);
            return true;
        }

        public void Reset()
        {
        }
    }

    private sealed class ThrowingRuntimeFactory : IEmulatorRuntimeFactory
    {
        public EmulatorRuntimeSession Create(CreateEmulatorSessionRequest request)
            => throw new NotSupportedException("This test registers its session directly.");
    }
}
