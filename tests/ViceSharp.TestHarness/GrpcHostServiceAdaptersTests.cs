namespace ViceSharp.TestHarness;

using System.Threading;
using Google.Protobuf;
using Grpc.Core;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using GrpcContracts = ViceSharp.Protocol.Grpc;
using Xunit;

// xUnit1051 fires on the CreateContext(CancellationToken) helper because the
// analyzer treats every method with a CT parameter as a cancellation entry
// point. The helper is purely synchronous test infrastructure; tests that
// need an early-cancelled token construct their own CancellationTokenSource.
#pragma warning disable xUnit1051

/// <summary>
/// Direct (in-process) unit tests for <see cref="GrpcHostServiceAdapters"/>
/// and the seven Grpc*ServiceHost adapter classes that wrap the
/// in-process IEmulatorHost / IMediaService / IVideoService /
/// IInputService / ISettingsService / IMonitorService / ISnapshotService
/// / ICaptureService implementations. These tests stub each inner
/// service with a hand-rolled fake (no DI, no mocking framework) and
/// verify the marshaling boundary: request DTOs round-trip from the
/// generated protobuf shapes to the protocol record shapes, RpcStatus
/// values surface in the corresponding grpc message field with the
/// matching code and message, the underlying service is invoked
/// exactly once with the expected arguments, the gRPC
/// ServerCallContext's CancellationToken is propagated to the inner
/// service, and exceptions raised by the inner service propagate out
/// of the adapter unchanged (the adapter is intentionally a thin
/// translation layer, not a try/catch boundary).
/// </summary>
public sealed class GrpcHostServiceAdaptersTests
{
    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A gRPC client calls
    /// <see cref="GrpcEmulatorHostService.CreateSession"/> with a
    /// proto CreateEmulatorSessionRequest. The adapter must translate
    /// the proto fields into the protocol record, hand them to the
    /// inner host, and translate the response back so the proto
    /// CreateEmulatorSessionResponse carries the inner status code and
    /// session id verbatim.
    /// Acceptance: The inner host is invoked exactly once with the
    /// architecture id and display name from the proto request, and
    /// the returned proto response carries the Ok status code along
    /// with the session id minted by the inner host.
    /// </summary>
    [Fact]
    public async Task GrpcEmulatorHostService_CreateSession_DelegatesAndMapsResponse()
    {
        var fake = new FakeEmulatorHost
        {
            CreateResponse = new CreateEmulatorSessionResponse(
                RpcStatus.Ok(),
                "session-42",
                new EmulatorStatusDto(
                    "session-42",
                    "minimal",
                    EmulatorRunState.Stopped,
                    0,
                    new MachineStateDto(0, 0, 0, 0, 0, 0, 0)))
        };
        var adapter = new GrpcEmulatorHostService(fake);
        var request = new GrpcContracts.CreateEmulatorSessionRequest
        {
            ArchitectureId = "minimal",
            DisplayName = "demo"
        };

        var response = await adapter.CreateSession(request, CreateContext());

        Assert.Equal(1, fake.CreateCount);
        Assert.Equal("minimal", fake.LastCreateRequest!.ArchitectureId);
        Assert.Equal("demo", fake.LastCreateRequest.DisplayName);
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        Assert.Equal("session-42", response.SessionId);
        Assert.NotNull(response.EmulatorStatus);
        Assert.Equal("session-42", response.EmulatorStatus.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: The inner emulator host returns NotFound (the
    /// standard missing-session response). The adapter must surface
    /// the same code and the same message on the gRPC envelope so
    /// clients can match on it.
    /// Acceptance: The proto RpcStatus carries
    /// <c>RPC_STATUS_CODE_NOT_FOUND</c> and the inner message string
    /// verbatim.
    /// </summary>
    [Fact]
    public async Task GrpcEmulatorHostService_GetStatus_TranslatesNotFoundStatus()
    {
        var fake = new FakeEmulatorHost
        {
            StatusResponse = new GetEmulatorStatusResponse(
                RpcStatus.NotFound("Emulator session 'ghost' was not found."),
                null)
        };
        var adapter = new GrpcEmulatorHostService(fake);

        var response = await adapter.GetStatus(
            new GrpcContracts.SessionRequest { SessionId = "ghost" },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.NotFound, response.Status.Code);
        Assert.Contains("ghost", response.Status.Message);
        Assert.Null(response.EmulatorStatus);
        Assert.Equal("ghost", fake.LastStatusRequest!.SessionId);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: The inner host returns an InvalidArgument status
    /// (e.g. SetLimiterRate with a negative rate). The adapter must
    /// surface that code unchanged.
    /// Acceptance: The proto envelope's RpcStatusCode is
    /// <c>RPC_STATUS_CODE_INVALID_ARGUMENT</c>.
    /// </summary>
    [Fact]
    public async Task GrpcEmulatorHostService_SetLimiterRate_TranslatesInvalidArgument()
    {
        var fake = new FakeEmulatorHost
        {
            CommandResponse = new EmulatorCommandResponse(
                RpcStatus.InvalidArgument("Limiter rate must be non-negative."),
                null)
        };
        var adapter = new GrpcEmulatorHostService(fake);

        var response = await adapter.SetLimiterRate(
            new GrpcContracts.SetLimiterRateRequest { SessionId = "s", LimiterRatePercent = -1 },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.InvalidArgument, response.Status.Code);
        Assert.Equal("s", fake.LastSetLimiterRequest!.SessionId);
        Assert.Equal(-1, fake.LastSetLimiterRequest.LimiterRatePercent);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: An exception raised inside the inner host (e.g. a
    /// bug, a disposed registry, an unexpected null) must surface to
    /// the gRPC caller as-is. The adapters are intentionally not
    /// try/catch boundaries.
    /// Acceptance: The original exception type propagates out of the
    /// adapter so ASP.NET Core's gRPC pipeline can translate it into
    /// the standard <see cref="RpcException"/> with status Internal.
    /// </summary>
    [Fact]
    public async Task GrpcEmulatorHostService_Start_PropagatesInnerException()
    {
        var fake = new FakeEmulatorHost
        {
            CommandException = new InvalidOperationException("boom")
        };
        var adapter = new GrpcEmulatorHostService(fake);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            adapter.Start(new GrpcContracts.SessionRequest { SessionId = "s" }, CreateContext()));

        Assert.Equal("boom", ex.Message);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A gRPC client cancels its call. The
    /// ServerCallContext.CancellationToken must reach the inner host
    /// so co-operative cancellation can short-circuit the work.
    /// Acceptance: The inner host observes the same cancellation
    /// token instance (or one that is already cancelled when the
    /// adapter's token is cancelled).
    /// </summary>
    [Fact]
    public async Task GrpcEmulatorHostService_Pause_PropagatesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var fake = new FakeEmulatorHost
        {
            CommandResponse = new EmulatorCommandResponse(RpcStatus.Ok(), null)
        };
        var adapter = new GrpcEmulatorHostService(fake);

        await adapter.Pause(new GrpcContracts.SessionRequest { SessionId = "s" }, CreateContext(cts.Token));

        Assert.True(fake.LastCommandToken.IsCancellationRequested);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A client invokes the media-attach RPC carrying a
    /// payload byte array. The adapter must translate the proto
    /// ByteString into a managed byte[] for the inner service, and
    /// translate the inner MediaSlot enum back to the proto slot enum
    /// on the response.
    /// Acceptance: The inner service receives an exact byte-for-byte
    /// copy of the payload, the slot it sees matches the proto slot,
    /// and the response carries the same slot back to the wire.
    /// </summary>
    [Fact]
    public async Task GrpcMediaServiceHost_AttachMedia_MarshalsSlotAndPayload()
    {
        var payload = new byte[] { 1, 2, 3, 4 };
        var fake = new FakeMediaService
        {
            AttachResponse = new AttachMediaResponse(
                RpcStatus.Ok(),
                new MediaAttachmentDto(MediaSlot.Drive9, "disk.d64", "Disk", true, false, true))
        };
        var adapter = new GrpcMediaServiceHost(fake);

        var response = await adapter.AttachMedia(
            new GrpcContracts.AttachMediaRequest
            {
                SessionId = "sess",
                Slot = GrpcContracts.MediaSlot.Drive9,
                FilePath = "disk.d64",
                DisplayName = "Disk",
                IsReadOnly = false,
                Payload = ByteString.CopyFrom(payload)
            },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        Assert.Equal(GrpcContracts.MediaSlot.Drive9, response.Attachment!.Slot);
        Assert.Equal(MediaSlot.Drive9, fake.LastAttachRequest!.Slot);
        Assert.Equal(payload, fake.LastAttachRequest.Payload);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A client lists media attachments on a session. The
    /// adapter must enumerate every attachment the inner service
    /// returns into the proto repeated field, preserving order.
    /// Acceptance: The proto response carries one element per inner
    /// attachment, in the same order, with the slot enum mapped 1:1.
    /// </summary>
    [Fact]
    public async Task GrpcMediaServiceHost_ListMedia_MapsAllAttachments()
    {
        var attachments = new[]
        {
            new MediaAttachmentDto(MediaSlot.Drive8, "a.d64", "A", true, false, true),
            new MediaAttachmentDto(MediaSlot.Tape, "b.tap", "B", true, true, true),
            new MediaAttachmentDto(MediaSlot.Cartridge, "c.crt", "C", true, false, true)
        };
        var fake = new FakeMediaService
        {
            ListResponse = new ListMediaResponse(RpcStatus.Ok(), attachments)
        };
        var adapter = new GrpcMediaServiceHost(fake);

        var response = await adapter.ListMedia(
            new GrpcContracts.SessionRequest { SessionId = "sess" },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        Assert.Equal(3, response.Attachments.Count);
        Assert.Equal(GrpcContracts.MediaSlot.Drive8, response.Attachments[0].Slot);
        Assert.Equal(GrpcContracts.MediaSlot.Tape, response.Attachments[1].Slot);
        Assert.Equal(GrpcContracts.MediaSlot.Cartridge, response.Attachments[2].Slot);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: The inner video service returns a null frame (e.g.
    /// the session is not yet running). The adapter must convey
    /// "no frame yet" by leaving the proto Frame field null instead
    /// of throwing a NullReferenceException.
    /// Acceptance: The proto response status is Ok and Frame is null.
    /// </summary>
    [Fact]
    public async Task GrpcVideoServiceHost_GetFrame_NullFrameSurvivesAsNull()
    {
        var fake = new FakeVideoService
        {
            FrameResponse = new GetVideoFrameResponse(RpcStatus.Ok(), null)
        };
        var adapter = new GrpcVideoServiceHost(fake);

        var response = await adapter.GetFrame(
            new GrpcContracts.SessionRequest { SessionId = "sess" },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        Assert.Null(response.Frame);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: The inner video service returns a populated frame.
    /// The adapter must copy the BGRA bytes into a ByteString without
    /// truncation or padding.
    /// Acceptance: The proto Frame fields equal the source fields and
    /// the byte payload round-trips byte-for-byte.
    /// </summary>
    [Fact]
    public async Task GrpcVideoServiceHost_GetFrame_MarshalsFrameBytes()
    {
        var bgra = new byte[] { 0x11, 0x22, 0x33, 0xff, 0x44, 0x55, 0x66, 0xff };
        var fake = new FakeVideoService
        {
            FrameResponse = new GetVideoFrameResponse(
                RpcStatus.Ok(),
                new VideoFrameDto(1, 2, 123L, bgra))
        };
        var adapter = new GrpcVideoServiceHost(fake);

        var response = await adapter.GetFrame(
            new GrpcContracts.SessionRequest { SessionId = "sess" },
            CreateContext());

        Assert.NotNull(response.Frame);
        Assert.Equal(1, response.Frame.Width);
        Assert.Equal(2, response.Frame.Height);
        Assert.Equal(123L, response.Frame.Cycle);
        Assert.Equal(bgra, response.Frame.Bgra.ToByteArray());
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A client posts a SetKeyState request. The adapter
    /// must translate each scalar field (key, isPressed, physicalKey,
    /// text, modifiers) into the protocol record without dropping any
    /// field, since each one drives a different code path inside the
    /// host's InputServiceHost.
    /// Acceptance: Every scalar field reaches the inner service
    /// verbatim, and the response carries the Ok status back.
    /// </summary>
    [Fact]
    public async Task GrpcInputServiceHost_SetKeyState_PassesAllFields()
    {
        var fake = new FakeInputService
        {
            CommandResponse = new InputCommandResponse(RpcStatus.Ok(), null)
        };
        var adapter = new GrpcInputServiceHost(fake);

        var response = await adapter.SetKeyState(
            new GrpcContracts.SetKeyStateRequest
            {
                SessionId = "s",
                Key = "Space",
                IsPressed = true,
                PhysicalKey = "Space",
                Text = " ",
                Modifiers = 4
            },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        var request = fake.LastSetKeyRequest!;
        Assert.Equal("s", request.SessionId);
        Assert.Equal("Space", request.Key);
        Assert.True(request.IsPressed);
        Assert.Equal("Space", request.PhysicalKey);
        Assert.Equal(" ", request.Text);
        Assert.Equal(4, request.Modifiers);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A client calls SetJoystickState with a particular
    /// InputPort enum. The adapter must translate the proto enum to
    /// the protocol enum 1:1 (both are int-equivalent enums sharing
    /// the same numeric values).
    /// Acceptance: The inner service sees the
    /// <see cref="InputPort.Joystick2"/> port for a proto
    /// <c>INPUT_PORT_JOYSTICK2</c>.
    /// </summary>
    [Fact]
    public async Task GrpcInputServiceHost_SetJoystickState_MapsPortEnum()
    {
        var fake = new FakeInputService
        {
            CommandResponse = new InputCommandResponse(RpcStatus.Ok(), null)
        };
        var adapter = new GrpcInputServiceHost(fake);

        await adapter.SetJoystickState(
            new GrpcContracts.SetJoystickStateRequest
            {
                SessionId = "s",
                Port = GrpcContracts.InputPort.Joystick2,
                DirectionMask = 0x0f,
                FireButton = true
            },
            CreateContext());

        Assert.Equal(InputPort.Joystick2, fake.LastSetJoystickRequest!.Port);
        Assert.Equal((byte)0x0f, fake.LastSetJoystickRequest.DirectionMask);
        Assert.True(fake.LastSetJoystickRequest.FireButton);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A client invokes the monitor's ReadMemory RPC. The
    /// adapter must translate (uint address, uint length) from the
    /// proto request into the (int address, int length) protocol
    /// shape and return the response bytes as a ByteString.
    /// Acceptance: The protocol request carries the same numeric
    /// address and length, and the response ByteString round-trips
    /// the byte payload exactly.
    /// </summary>
    [Fact]
    public async Task GrpcMonitorServiceHost_ReadMemory_MapsAddressAndBytes()
    {
        var data = new byte[] { 0xa9, 0x00, 0x8d, 0x20, 0xd0 };
        var fake = new FakeMonitorService
        {
            ReadMemoryResponse = new MonitorMemoryResponse(RpcStatus.Ok(), 0x1000, data, null)
        };
        var adapter = new GrpcMonitorServiceHost(fake);

        var response = await adapter.ReadMemory(
            new GrpcContracts.MonitorReadMemoryRequest
            {
                SessionId = "s",
                Address = 0x1000,
                Length = (uint)data.Length
            },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        Assert.Equal(0x1000u, response.Address);
        Assert.Equal(data, response.Data.ToByteArray());
        Assert.Equal(0x1000, fake.LastReadMemoryRequest!.Address);
        Assert.Equal(data.Length, fake.LastReadMemoryRequest.Length);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A monitor read-memory call surfaces a FailedPrecondition
    /// status because the session is not paused. The adapter must
    /// surface the same code verbatim.
    /// Acceptance: The proto status code is
    /// <c>RPC_STATUS_CODE_FAILED_PRECONDITION</c>.
    /// </summary>
    [Fact]
    public async Task GrpcMonitorServiceHost_ReadMemory_TranslatesFailedPrecondition()
    {
        var fake = new FakeMonitorService
        {
            ReadMemoryResponse = new MonitorMemoryResponse(
                RpcStatus.FailedPrecondition("Session must be paused for ReadMemory."),
                0,
                Array.Empty<byte>(),
                null)
        };
        var adapter = new GrpcMonitorServiceHost(fake);

        var response = await adapter.ReadMemory(
            new GrpcContracts.MonitorReadMemoryRequest
            {
                SessionId = "s",
                Address = 0,
                Length = 1
            },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.FailedPrecondition, response.Status.Code);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A client captures a snapshot. The adapter must
    /// translate the inner SnapshotDto (format / cycle / payload)
    /// into the proto SnapshotDto, copying the payload bytes through
    /// ByteString.
    /// Acceptance: The proto response is non-null and every snapshot
    /// scalar round-trips through the adapter unchanged.
    /// </summary>
    [Fact]
    public async Task GrpcSnapshotServiceHost_CaptureSnapshot_MapsSnapshotPayload()
    {
        var payload = new byte[] { 0xde, 0xad, 0xbe, 0xef };
        var fake = new FakeSnapshotService
        {
            CaptureResponse = new CaptureSnapshotResponse(
                RpcStatus.Ok(),
                new SnapshotDto("minimal-v1", 42UL, payload))
        };
        var adapter = new GrpcSnapshotServiceHost(fake);

        var response = await adapter.CaptureSnapshot(
            new GrpcContracts.SessionRequest { SessionId = "s" },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Snapshot);
        Assert.Equal("minimal-v1", response.Snapshot.Format);
        Assert.Equal(42UL, response.Snapshot.Cycle);
        Assert.Equal(payload, response.Snapshot.Payload.ToByteArray());
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A client starts a capture session. The adapter must
    /// translate the proto CaptureKind into the protocol CaptureKind
    /// enum (same int values) and map the inner CaptureSessionDto
    /// fields onto the proto response.
    /// Acceptance: The inner service receives the matching CaptureKind
    /// and the proto response Capture carries every scalar field back.
    /// </summary>
    [Fact]
    public async Task GrpcCaptureServiceHost_StartCapture_MapsKindAndSession()
    {
        var fake = new FakeCaptureService
        {
            StartResponse = new StartCaptureResponse(
                RpcStatus.Ok(),
                new CaptureSessionDto("cap-1", CaptureKind.Video, "out.mp4", true))
        };
        var adapter = new GrpcCaptureServiceHost(fake);

        var response = await adapter.StartCapture(
            new GrpcContracts.StartCaptureRequest
            {
                SessionId = "s",
                Kind = GrpcContracts.CaptureKind.Video,
                TargetPath = "out.mp4"
            },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Capture);
        Assert.Equal("cap-1", response.Capture.CaptureId);
        Assert.Equal(GrpcContracts.CaptureKind.Video, response.Capture.Kind);
        Assert.True(response.Capture.IsActive);
        Assert.Equal(CaptureKind.Video, fake.LastStartRequest!.Kind);
        Assert.Equal("out.mp4", fake.LastStartRequest.TargetPath);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A client requests the settings for a session. The
    /// adapter must translate every SessionSettings sub-record (limiter,
    /// display, input, audio, resources) onto its proto counterpart so
    /// the UI can render the full settings form.
    /// Acceptance: Each sub-record's scalar field is present in the
    /// proto response and equals the source value.
    /// </summary>
    [Fact]
    public async Task GrpcSettingsServiceHost_GetSettings_MapsSubRecords()
    {
        var settings = new SessionSettingsDto(
            "minimal",
            new LimiterSettingsDto(75, true),
            new DisplaySettingsDto(Scale: "3x", Palette: "vice"),
            new InputSettingsDto(KeyboardMapId: "c64:gtk3_pos", PrimaryJoystickPort: InputPort.Joystick1),
            new AudioSettingsDto("muted"),
            new ResourceSettingsDto("manual"));
        var fake = new FakeSettingsService
        {
            SettingsResponse = new GetSettingsResponse(RpcStatus.Ok(), settings)
        };
        var adapter = new GrpcSettingsServiceHost(fake);

        var response = await adapter.GetSettings(
            new GrpcContracts.SessionRequest { SessionId = "s" },
            CreateContext());

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, response.Status.Code);
        Assert.NotNull(response.Settings);
        Assert.Equal("minimal", response.Settings.ProfileId);
        Assert.Equal(75, response.Settings.Limiter.RatePercent);
        Assert.True(response.Settings.Limiter.IsEnabled);
        Assert.Equal("3x", response.Settings.Display.Scale);
        Assert.Equal("vice", response.Settings.Display.Palette);
        Assert.Equal(GrpcContracts.InputPort.Joystick1, response.Settings.Input.PrimaryJoystickPort);
        Assert.Equal("muted", response.Settings.Audio.Mode);
        Assert.Equal("manual", response.Settings.Resources.Mode);
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A caller invokes
    /// <see cref="GrpcHostServiceAdapters.AddViceSharpGrpcHost"/> with
    /// a null IServiceCollection (defensive guard against unconfigured
    /// composition).
    /// Acceptance: The extension method throws
    /// <see cref="ArgumentNullException"/> immediately, mirroring the
    /// guard pattern used elsewhere in the host surface.
    /// </summary>
    [Fact]
    public void AddViceSharpGrpcHost_NullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GrpcHostServiceAdapters.AddViceSharpGrpcHost(null!));
    }

    /// <summary>
    /// FR/TR: FR-Host-UI-Boundary (BACKFILL-HOSTUI-001 GrpcAdapters).
    /// Use case: A caller invokes
    /// <see cref="GrpcHostServiceAdapters.MapViceSharpGrpcHost"/> with
    /// a null endpoint builder (defensive guard).
    /// Acceptance: The extension method throws
    /// <see cref="ArgumentNullException"/> immediately.
    /// </summary>
    [Fact]
    public void MapViceSharpGrpcHost_NullEndpoints_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            GrpcHostServiceAdapters.MapViceSharpGrpcHost(null!));
    }

    private static ServerCallContext CreateContext(CancellationToken cancellationToken = default)
        => new FakeServerCallContext(cancellationToken);

    private sealed class FakeServerCallContext : ServerCallContext
    {
        private readonly CancellationToken _cancellationToken;
        private readonly Metadata _requestHeaders = new();
        private readonly Metadata _responseTrailers = new();
        private WriteOptions? _writeOptions;
        private Status _status;

        public FakeServerCallContext(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        protected override string MethodCore => "TestMethod";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "ipv4:127.0.0.1:0";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => _requestHeaders;
        protected override CancellationToken CancellationTokenCore => _cancellationToken;
        protected override Metadata ResponseTrailersCore => _responseTrailers;
        protected override Status StatusCore { get => _status; set => _status = value; }
        protected override WriteOptions? WriteOptionsCore { get => _writeOptions; set => _writeOptions = value; }
        protected override AuthContext AuthContextCore => new("Anonymous", new Dictionary<string, List<AuthProperty>>());
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options)
            => throw new NotSupportedException();
    }

    private sealed class FakeEmulatorHost : IEmulatorHost
    {
        public CreateEmulatorSessionResponse CreateResponse { get; set; } =
            new(RpcStatus.Ok(), "session", null);
        public GetEmulatorStatusResponse StatusResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public EmulatorCommandResponse CommandResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public Exception? CommandException { get; set; }

        public CreateEmulatorSessionRequest? LastCreateRequest { get; private set; }
        public SessionRequest? LastStatusRequest { get; private set; }
        public SetLimiterRateRequest? LastSetLimiterRequest { get; private set; }
        public CancellationToken LastCommandToken { get; private set; }
        public int CreateCount { get; private set; }

        public ValueTask<CreateEmulatorSessionResponse> CreateSessionAsync(CreateEmulatorSessionRequest request, CancellationToken cancellationToken = default)
        {
            CreateCount++;
            LastCreateRequest = request;
            return ValueTask.FromResult(CreateResponse);
        }

        public ValueTask<GetEmulatorStatusResponse> GetStatusAsync(SessionRequest request, CancellationToken cancellationToken = default)
        {
            LastStatusRequest = request;
            return ValueTask.FromResult(StatusResponse);
        }

        private ValueTask<EmulatorCommandResponse> Command(CancellationToken cancellationToken)
        {
            LastCommandToken = cancellationToken;
            if (CommandException is not null)
                throw CommandException;
            return ValueTask.FromResult(CommandResponse);
        }

        public ValueTask<EmulatorCommandResponse> StartAsync(SessionRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> PauseAsync(SessionRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> ResumeAsync(SessionRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> ResetAsync(SessionRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> ResetAsync(ResetRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> ColdResetAsync(SessionRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> WarmResetAsync(SessionRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(ResetAndAutostartDrive8Request request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> StepCycleAsync(StepCycleRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> StepFrameAsync(StepFrameRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> RewindCycleAsync(RewindCycleRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
        public ValueTask<EmulatorCommandResponse> RewindFrameAsync(RewindFrameRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);

        public ValueTask<EmulatorCommandResponse> SetLimiterRateAsync(SetLimiterRateRequest request, CancellationToken cancellationToken = default)
        {
            LastSetLimiterRequest = request;
            return Command(cancellationToken);
        }

        public ValueTask<EmulatorCommandResponse> CloseSessionAsync(SessionRequest request, CancellationToken cancellationToken = default) => Command(cancellationToken);
    }

    private sealed class FakeMediaService : IMediaService
    {
        public AttachMediaResponse AttachResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public DetachMediaResponse DetachResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public ListMediaResponse ListResponse { get; set; } =
            new(RpcStatus.Ok(), Array.Empty<MediaAttachmentDto>());
        public AttachMediaRequest? LastAttachRequest { get; private set; }

        public ValueTask<AttachMediaResponse> AttachMediaAsync(AttachMediaRequest request, CancellationToken cancellationToken = default)
        {
            LastAttachRequest = request;
            return ValueTask.FromResult(AttachResponse);
        }

        public ValueTask<DetachMediaResponse> DetachMediaAsync(DetachMediaRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(DetachResponse);

        public ValueTask<ListMediaResponse> ListMediaAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ListResponse);
    }

    private sealed class FakeVideoService : IVideoService
    {
        public GetVideoStatusResponse StatusResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public GetVideoFrameResponse FrameResponse { get; set; } =
            new(RpcStatus.Ok(), null);

        public ValueTask<GetVideoStatusResponse> GetVideoStatusAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(StatusResponse);

        public ValueTask<GetVideoFrameResponse> GetFrameAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(FrameResponse);
    }

    private sealed class FakeInputService : IInputService
    {
        public InputCommandResponse CommandResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public GetInputStateResponse StateResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public ListKeyboardMapsResponse MapsResponse { get; set; } =
            new(RpcStatus.Ok(), Array.Empty<KeyboardMapDto>());
        public KeyboardMapResponse KeyboardMapResponse { get; set; } =
            new(RpcStatus.Ok(), null, null);

        public SetKeyStateRequest? LastSetKeyRequest { get; private set; }
        public SetJoystickStateRequest? LastSetJoystickRequest { get; private set; }

        public ValueTask<InputCommandResponse> SetKeyStateAsync(SetKeyStateRequest request, CancellationToken cancellationToken = default)
        {
            LastSetKeyRequest = request;
            return ValueTask.FromResult(CommandResponse);
        }

        public ValueTask<InputCommandResponse> SetJoystickStateAsync(SetJoystickStateRequest request, CancellationToken cancellationToken = default)
        {
            LastSetJoystickRequest = request;
            return ValueTask.FromResult(CommandResponse);
        }

        public ValueTask<GetInputStateResponse> GetInputStateAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(StateResponse);

        public ValueTask<ListKeyboardMapsResponse> ListKeyboardMapsAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(MapsResponse);

        public ValueTask<KeyboardMapResponse> SetKeyboardMapAsync(SetKeyboardMapRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(KeyboardMapResponse);
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        public ListSettingsProfilesResponse ProfilesResponse { get; set; } =
            new(RpcStatus.Ok(), Array.Empty<SettingsProfileDto>());
        public GetSettingsResponse SettingsResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public UpdateSettingsResponse UpdateResponse { get; set; } =
            new(RpcStatus.Ok(), null, Array.Empty<SettingApplyDiagnosticDto>());
        public ValidateSettingsResourcesResponse ValidateResponse { get; set; } =
            new(RpcStatus.Ok(), Array.Empty<SettingsResourceValidationDto>());

        public ValueTask<ListSettingsProfilesResponse> ListProfilesAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ProfilesResponse);

        public ValueTask<GetSettingsResponse> GetSettingsAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(SettingsResponse);

        public ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(UpdateSettingsRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(UpdateResponse);

        public ValueTask<ValidateSettingsResourcesResponse> ValidateResourcesAsync(ValidateSettingsResourcesRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ValidateResponse);
    }

    private sealed class FakeMonitorService : IMonitorService
    {
        public MonitorCommandResponse CommandResponse { get; set; } =
            new(RpcStatus.Ok(), string.Empty, null);
        public MonitorRegistersResponse RegistersResponse { get; set; } =
            new(RpcStatus.Ok(), null, null);
        public MonitorDisassemblyResponse DisassemblyResponse { get; set; } =
            new(RpcStatus.Ok(), Array.Empty<MonitorDisassemblyLineDto>(), null);
        public MonitorBreakpointsResponse BreakpointsResponse { get; set; } =
            new(RpcStatus.Ok(), Array.Empty<MonitorBreakpointDto>(), null);
        public MonitorMemoryResponse ReadMemoryResponse { get; set; } =
            new(RpcStatus.Ok(), 0, Array.Empty<byte>(), null);
        public MonitorMemoryWriteResponse WriteMemoryResponse { get; set; } =
            new(RpcStatus.Ok(), 0, 0, null);

        public MonitorReadMemoryRequest? LastReadMemoryRequest { get; private set; }

        public ValueTask<MonitorCommandResponse> ExecuteCommandAsync(ExecuteMonitorCommandRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(CommandResponse);

        public ValueTask<MonitorRegistersResponse> ReadRegistersAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(RegistersResponse);

        public ValueTask<MonitorDisassemblyResponse> DisassembleAsync(MonitorDisassemblyRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(DisassemblyResponse);

        public ValueTask<MonitorBreakpointsResponse> ListBreakpointsAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(BreakpointsResponse);

        public ValueTask<MonitorBreakpointsResponse> AddBreakpointAsync(MonitorBreakpointRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(BreakpointsResponse);

        public ValueTask<MonitorBreakpointsResponse> RemoveBreakpointAsync(MonitorBreakpointRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(BreakpointsResponse);

        public ValueTask<MonitorMemoryResponse> ReadMemoryAsync(MonitorReadMemoryRequest request, CancellationToken cancellationToken = default)
        {
            LastReadMemoryRequest = request;
            return ValueTask.FromResult(ReadMemoryResponse);
        }

        public ValueTask<MonitorMemoryWriteResponse> WriteMemoryAsync(MonitorWriteMemoryRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(WriteMemoryResponse);

        public ValueTask<GetTickHistoryResponse> GetTickHistoryAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new GetTickHistoryResponse(RpcStatus.Ok(), System.Array.Empty<TickHistoryEntryDto>()));

        public ValueTask<MonitorMemoryResponse> ReadMemoryAtTickAsync(ReadMemoryAtTickRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ReadMemoryResponse);

        public ValueTask<GetChipStateAtTickResponse> GetChipStateAtTickAsync(GetChipStateAtTickRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new GetChipStateAtTickResponse(RpcStatus.Ok(), System.Array.Empty<ChipStateDto>()));
    }

    private sealed class FakeSnapshotService : ISnapshotService
    {
        public CaptureSnapshotResponse CaptureResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public RestoreSnapshotResponse RestoreResponse { get; set; } =
            new(RpcStatus.Ok(), null);

        public ValueTask<CaptureSnapshotResponse> CaptureSnapshotAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(CaptureResponse);

        public ValueTask<RestoreSnapshotResponse> RestoreSnapshotAsync(RestoreSnapshotRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(RestoreResponse);
    }

    private sealed class FakeCaptureService : ICaptureService
    {
        public StartCaptureResponse StartResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public StopCaptureResponse StopResponse { get; set; } =
            new(RpcStatus.Ok(), null);
        public CaptureFrameResponse FrameResponse { get; set; } =
            new(RpcStatus.Ok(), null);

        public GetCaptureCapabilitiesResponse CapabilitiesResponse { get; set; } =
            new(RpcStatus.Ok(), ["png", "bmp"], ["wav"], System.Array.Empty<CaptureVideoFormatDto>());
        public ListCapturesResponse ListResponse { get; set; } =
            new(RpcStatus.Ok(), System.Array.Empty<CaptureSessionDto>());

        public StartCaptureRequest? LastStartRequest { get; private set; }

        public ValueTask<GetCaptureCapabilitiesResponse> GetCaptureCapabilitiesAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(CapabilitiesResponse);

        public ValueTask<StartCaptureResponse> StartCaptureAsync(StartCaptureRequest request, CancellationToken cancellationToken = default)
        {
            LastStartRequest = request;
            return ValueTask.FromResult(StartResponse);
        }

        public ValueTask<StopCaptureResponse> StopCaptureAsync(StopCaptureRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(StopResponse);

        public ValueTask<CaptureFrameResponse> CaptureFrameAsync(CaptureFrameRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(FrameResponse);

        public ValueTask<ListCapturesResponse> ListCapturesAsync(SessionRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ListResponse);
    }
}
