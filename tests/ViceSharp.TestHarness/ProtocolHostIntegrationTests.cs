namespace ViceSharp.TestHarness;

using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using ViceSharp.Core;
using ViceSharp.Chips.Cartridges;
using ViceSharp.Chips.IEC;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using GrpcContracts = ViceSharp.Protocol.Grpc;
using Xunit;

public sealed class ProtocolHostIntegrationTests
{
    [Fact]
    public async Task EmulatorHost_CreatesSessionAndStepsFrame()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, new DefaultEmulatorRuntimeFactory());

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var stepped = await emulatorHost.StepFrameAsync(
            new StepFrameRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, created.Status.Code);
        Assert.False(string.IsNullOrWhiteSpace(created.SessionId));
        Assert.NotNull(created.EmulatorStatus);
        Assert.Equal(RpcStatusCode.Ok, stepped.Status.Code);
        Assert.NotNull(stepped.EmulatorStatus);
        Assert.True(stepped.EmulatorStatus.Cycle > created.EmulatorStatus.Cycle);
    }

    [Fact]
    public async Task EmulatorHost_StatusIncludesUiControlFields()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateMinimalRuntimeFactory());

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var limited = await emulatorHost.SetLimiterRateAsync(
            new SetLimiterRateRequest(created.SessionId, 50),
            TestContext.Current.CancellationToken);
        var resumed = await emulatorHost.ResumeAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, created.Status.Code);
        var status = created.EmulatorStatus!;
        Assert.Equal("On", status.PowerState);
        Assert.Equal(EmulatorRunState.Stopped, status.RunState);
        Assert.Equal(100, status.LimiterRatePercent);
        Assert.Equal(0, status.MeasuredFps);
        Assert.Equal(MinimalHostArchitectureDescriptor.Instance.MasterClockHz, status.NominalClockHz);
        Assert.Equal(0, status.FrameCount);
        Assert.Equal(status.MachineState.Cycle, status.Cycle);
        Assert.Equal(status.MachineState.Pc, status.Pc);

        Assert.Equal(RpcStatusCode.Ok, limited.Status.Code);
        Assert.NotNull(limited.EmulatorStatus);
        Assert.Equal(50, limited.EmulatorStatus.LimiterRatePercent);
        Assert.Equal(RpcStatusCode.Ok, resumed.Status.Code);
        Assert.NotNull(resumed.EmulatorStatus);
        Assert.Equal(EmulatorRunState.Running, resumed.EmulatorStatus.RunState);
        Assert.Equal(50, resumed.EmulatorStatus.LimiterRatePercent);
    }

    [Fact]
    public async Task EmulatorHost_ControlCommandsReturnFocusedStatus()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateMinimalRuntimeFactory());

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var steppedCycle = await emulatorHost.StepCycleAsync(
            new StepCycleRequest(created.SessionId, 7),
            TestContext.Current.CancellationToken);
        var steppedFrame = await emulatorHost.StepFrameAsync(
            new StepFrameRequest(created.SessionId, 2),
            TestContext.Current.CancellationToken);
        var rewindCycle = await emulatorHost.RewindCycleAsync(
            new RewindCycleRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var rewindFrame = await emulatorHost.RewindFrameAsync(
            new RewindFrameRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var autostart = await emulatorHost.ResetAndAutostartDrive8Async(
            new ResetAndAutostartDrive8Request(created.SessionId),
            TestContext.Current.CancellationToken);
        var reset = await emulatorHost.ColdResetAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, steppedCycle.Status.Code);
        Assert.Equal(7, steppedCycle.EmulatorStatus!.Cycle);
        Assert.Equal(RpcStatusCode.Ok, steppedFrame.Status.Code);
        Assert.Equal(2, steppedFrame.EmulatorStatus!.FrameCount);
        Assert.True(steppedFrame.EmulatorStatus.Cycle > steppedCycle.EmulatorStatus.Cycle);
        Assert.Equal(RpcStatusCode.NotImplemented, rewindCycle.Status.Code);
        Assert.NotNull(rewindCycle.EmulatorStatus);
        Assert.Equal(RpcStatusCode.NotImplemented, rewindFrame.Status.Code);
        Assert.NotNull(rewindFrame.EmulatorStatus);
        Assert.Equal(RpcStatusCode.NotImplemented, autostart.Status.Code);
        Assert.NotNull(autostart.EmulatorStatus);
        Assert.Equal(RpcStatusCode.Ok, reset.Status.Code);
        Assert.NotNull(reset.EmulatorStatus);
        Assert.Equal(0, reset.EmulatorStatus.Cycle);
        Assert.Equal(0, reset.EmulatorStatus.FrameCount);
        Assert.Equal(EmulatorRunState.Stopped, reset.EmulatorStatus.RunState);
    }

    [Fact]
    public async Task SnapshotService_CapturesAndRestoresRuntimeSnapshot()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, new DefaultEmulatorRuntimeFactory());
        var snapshotService = new SnapshotServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        await emulatorHost.StepFrameAsync(
            new StepFrameRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        var captured = await snapshotService.CaptureSnapshotAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var restored = await snapshotService.RestoreSnapshotAsync(
            new RestoreSnapshotRequest(created.SessionId, captured.Snapshot!),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, captured.Status.Code);
        Assert.NotNull(captured.Snapshot);
        Assert.Equal(SnapshotServiceHost.RuntimeSnapshotFormat, captured.Snapshot.Format);
        Assert.NotEmpty(captured.Snapshot.Payload);
        Assert.Equal(RpcStatusCode.Ok, restored.Status.Code);
        Assert.NotNull(restored.EmulatorStatus);
    }

    [Fact]
    public async Task InputService_RecordsHostSideState()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, new DefaultEmulatorRuntimeFactory());
        var inputService = new InputServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var joystick = await inputService.SetJoystickStateAsync(
            new SetJoystickStateRequest(created.SessionId, InputPort.Joystick2, 0x11, true),
            TestContext.Current.CancellationToken);
        var key = await inputService.SetKeyStateAsync(
            new SetKeyStateRequest(created.SessionId, "Space", true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, joystick.Status.Code);
        Assert.NotNull(joystick.InputState);
        Assert.Contains(joystick.InputState.Joysticks, x => x.Port == InputPort.Joystick2 && x.State.FireButton);
        Assert.Equal(RpcStatusCode.Ok, key.Status.Code);
        Assert.NotNull(key.InputState);
        Assert.Contains(key.InputState.Keys, x => x.Key == "Space" && x.IsPressed);
    }

    [Fact]
    public async Task VideoService_ReportsNoVideoChipForMinimalHost()
    {
        var registry = new EmulatorRuntimeRegistry();
        var emulatorHost = new EmulatorHostService(registry, CreateMinimalRuntimeFactory());
        var videoService = new VideoServiceHost(registry);

        var created = await emulatorHost.CreateSessionAsync(
            new CreateEmulatorSessionRequest(),
            TestContext.Current.CancellationToken);
        var status = await videoService.GetVideoStatusAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);
        var frame = await videoService.GetFrameAsync(
            new SessionRequest(created.SessionId),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, status.Status.Code);
        Assert.NotNull(status.VideoStatus);
        Assert.False(status.VideoStatus.IsAvailable);
        Assert.Equal(RpcStatusCode.Unavailable, frame.Status.Code);
    }

    [Fact]
    public async Task GrpcHost_ControlsSessionThroughGeneratedClient()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var client = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);

        var created = await client.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        var started = await client.StartAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var paused = await client.PauseAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var cycleStepped = await client.StepCycleAsync(
            new GrpcContracts.StepCycleRequest { SessionId = created.SessionId, CycleCount = 3 },
            cancellationToken: TestContext.Current.CancellationToken);
        var stepped = await client.StepFrameAsync(
            new GrpcContracts.StepFrameRequest { SessionId = created.SessionId, FrameCount = 1 },
            cancellationToken: TestContext.Current.CancellationToken);
        var limited = await client.SetLimiterRateAsync(
            new GrpcContracts.SetLimiterRateRequest { SessionId = created.SessionId, LimiterRatePercent = 75 },
            cancellationToken: TestContext.Current.CancellationToken);
        var rewind = await client.RewindCycleAsync(
            new GrpcContracts.RewindCycleRequest { SessionId = created.SessionId, CycleCount = 1 },
            cancellationToken: TestContext.Current.CancellationToken);
        var autostart = await client.ResetAndAutostartDrive8Async(
            new GrpcContracts.ResetAndAutostartDrive8Request { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var shutdown = await client.ShutdownAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, created.Status.Code);
        Assert.False(string.IsNullOrWhiteSpace(created.SessionId));
        Assert.Equal(GrpcContracts.EmulatorRunState.Running, started.EmulatorStatus.RunState);
        Assert.Equal(GrpcContracts.EmulatorRunState.Paused, paused.EmulatorStatus.RunState);
        Assert.True(cycleStepped.EmulatorStatus.Cycle > paused.EmulatorStatus.Cycle);
        Assert.True(stepped.EmulatorStatus.Cycle > cycleStepped.EmulatorStatus.Cycle);
        Assert.Equal(75, limited.EmulatorStatus.LimiterRatePercent);
        Assert.Equal(limited.EmulatorStatus.MachineState.Pc, limited.EmulatorStatus.Pc);
        Assert.Equal(GrpcContracts.RpcStatusCode.NotImplemented, rewind.Status.Code);
        Assert.Equal(GrpcContracts.RpcStatusCode.NotImplemented, autostart.Status.Code);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, shutdown.Status.Code);
    }

    [Fact]
    public async Task GrpcMediaService_AttachesDiskTapeAndCartridgePayloads()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var mediaClient = new GrpcContracts.MediaService.MediaServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);

        var disk = await mediaClient.AttachMediaAsync(
            new GrpcContracts.AttachMediaRequest
            {
                SessionId = created.SessionId,
                Slot = GrpcContracts.MediaSlot.Drive8,
                DisplayName = "demo.d64",
                IsReadOnly = true,
                Payload = ByteString.CopyFrom(CreateD64Image())
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var tape = await mediaClient.AttachMediaAsync(
            new GrpcContracts.AttachMediaRequest
            {
                SessionId = created.SessionId,
                Slot = GrpcContracts.MediaSlot.Tape,
                DisplayName = "demo.tap",
                Payload = ByteString.CopyFrom(CreateTapImage(1, [10, 0, 0x40, 0x1F, 0x00]))
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var cartridge = await mediaClient.AttachMediaAsync(
            new GrpcContracts.AttachMediaRequest
            {
                SessionId = created.SessionId,
                Slot = GrpcContracts.MediaSlot.Cartridge,
                DisplayName = "demo.crt",
                Payload = ByteString.CopyFrom(CreateRawCartridgeImage(StandardCartridgeImage.RomBankSize))
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var listed = await mediaClient.ListMediaAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var ejected = await mediaClient.EjectMediaAsync(
            new GrpcContracts.DetachMediaRequest { SessionId = created.SessionId, Slot = GrpcContracts.MediaSlot.Drive8 },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, disk.Status.Code);
        Assert.Equal(GrpcContracts.MediaSlot.Drive8, disk.Attachment.Slot);
        Assert.Equal("demo.d64", disk.Attachment.DisplayName);
        Assert.True(disk.Attachment.IsReadOnly);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, tape.Status.Code);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, cartridge.Status.Code);
        Assert.True(cartridge.Attachment.AppliedToRuntime);
        Assert.Contains(listed.Attachments, attachment => attachment.Slot == GrpcContracts.MediaSlot.Drive8);
        Assert.Contains(listed.Attachments, attachment => attachment.Slot == GrpcContracts.MediaSlot.Tape);
        Assert.Contains(listed.Attachments, attachment => attachment.Slot == GrpcContracts.MediaSlot.Cartridge);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, ejected.Status.Code);
        Assert.False(ejected.Attachment.IsAttached);
    }

    [Fact]
    public async Task GrpcServices_RoundTripInputSnapshotVideoAndCaptureCommands()
    {
        using var host = await CreateGrpcHostAsync();
        using var httpClient = CreateGrpcHttpClient(host);
        using var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpClient = httpClient });
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var inputClient = new GrpcContracts.InputService.InputServiceClient(channel);
        var videoClient = new GrpcContracts.VideoService.VideoServiceClient(channel);
        var snapshotClient = new GrpcContracts.SnapshotService.SnapshotServiceClient(channel);
        var captureClient = new GrpcContracts.CaptureService.CaptureServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        var key = await inputClient.SetKeyStateAsync(
            new GrpcContracts.SetKeyStateRequest { SessionId = created.SessionId, Key = "Space", IsPressed = true },
            cancellationToken: TestContext.Current.CancellationToken);
        var joystick = await inputClient.SetJoystickStateAsync(
            new GrpcContracts.SetJoystickStateRequest
            {
                SessionId = created.SessionId,
                Port = GrpcContracts.InputPort.Joystick2,
                DirectionMask = 0x11,
                FireButton = true
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var videoStatus = await videoClient.GetVideoStatusAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var frame = await videoClient.GetFrameAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var snapshot = await snapshotClient.CaptureSnapshotAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var restored = await snapshotClient.RestoreSnapshotAsync(
            new GrpcContracts.RestoreSnapshotRequest { SessionId = created.SessionId, Snapshot = snapshot.Snapshot },
            cancellationToken: TestContext.Current.CancellationToken);
        var capture = await captureClient.StartCaptureAsync(
            new GrpcContracts.StartCaptureRequest
            {
                SessionId = created.SessionId,
                Kind = GrpcContracts.CaptureKind.Screenshot,
                TargetPath = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.bmp")
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var stopped = await captureClient.StopCaptureAsync(
            new GrpcContracts.StopCaptureRequest { SessionId = created.SessionId, CaptureId = capture.Capture.CaptureId },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, key.Status.Code);
        Assert.Contains(key.InputState.Keys, state => state.Key == "Space" && state.IsPressed);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, joystick.Status.Code);
        Assert.Contains(joystick.InputState.Joysticks, state => state.Port == GrpcContracts.InputPort.Joystick2 && state.State.FireButton);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, videoStatus.Status.Code);
        Assert.NotNull(videoStatus.VideoStatus);
        if (videoStatus.VideoStatus.IsAvailable)
        {
            Assert.Equal(GrpcContracts.RpcStatusCode.Ok, frame.Status.Code);
            Assert.NotNull(frame.Frame);
            Assert.NotEmpty(frame.Frame.Bgra);
        }
        else
        {
            Assert.Equal(GrpcContracts.RpcStatusCode.Unavailable, frame.Status.Code);
        }
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, snapshot.Status.Code);
        Assert.NotEmpty(snapshot.Snapshot.Payload);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, restored.Status.Code);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, capture.Status.Code);
        Assert.True(capture.Capture.IsActive);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, stopped.Status.Code);
        Assert.False(stopped.Capture.IsActive);
    }

    private static async Task<IHost> CreateGrpcHostAsync()
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddViceSharpGrpcHost();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapViceSharpGrpcHost());
                });
            })
            .Build();

        await host.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
        return host;
    }

    private static HttpClient CreateGrpcHttpClient(IHost host)
    {
        var httpClient = host.GetTestClient();
        httpClient.DefaultRequestVersion = new Version(2, 0);
        return httpClient;
    }

    private static IEmulatorRuntimeFactory CreateMinimalRuntimeFactory()
    {
        return new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
    }

    private static byte[] CreateD64Image()
    {
        var image = new byte[D64Image.DiskSize35Track];
        var sectorOffset = Track18Sector1Offset();
        for (var i = 0; i < 256; i++)
            image[sectorOffset + i] = (byte)(255 - i);

        return image;
    }

    private static byte[] CreateTapImage(byte version, byte[] pulseData)
    {
        var image = new byte[20 + pulseData.Length];
        "C64-TAPE-RAW"u8.CopyTo(image);
        image[12] = version;
        image[16] = (byte)pulseData.Length;
        image[17] = (byte)(pulseData.Length >> 8);
        image[18] = (byte)(pulseData.Length >> 16);
        image[19] = (byte)(pulseData.Length >> 24);
        pulseData.CopyTo(image.AsSpan(20));
        return image;
    }

    private static byte[] CreateRawCartridgeImage(int length)
    {
        var image = new byte[length];
        for (var i = 0; i < image.Length; i++)
            image[i] = (byte)(i & 0xFF);

        return image;
    }

    private static int Track18Sector1Offset()
    {
        var offset = 0;
        for (var track = 1; track < 18; track++)
            offset += 21 * 256;

        return offset + 256;
    }
}
