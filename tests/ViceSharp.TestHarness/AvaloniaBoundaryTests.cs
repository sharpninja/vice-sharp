namespace ViceSharp.TestHarness;

using Grpc.Net.Client;
using ViceSharp.Avalonia.Host;
using ViceSharp.Avalonia.ViewModels;
using ViceSharp.Protocol;
using GrpcContracts = ViceSharp.Protocol.Grpc;
using Xunit;

public sealed class AvaloniaBoundaryTests
{
    [Fact]
    public void AvaloniaProject_ReferencesProtocolAndHostCompositionButNotRuntimeProjects()
    {
        var project = File.ReadAllText(Path.Combine(RepoRoot, "src", "ViceSharp.Avalonia", "ViceSharp.Avalonia.csproj"));

        Assert.Contains("ViceSharp.Protocol.csproj", project);
        Assert.Contains("ViceSharp.Host.csproj", project);
        Assert.DoesNotContain("ViceSharp.Core.csproj", project);
        Assert.DoesNotContain("ViceSharp.Chips.csproj", project);
        Assert.DoesNotContain("ViceSharp.Architectures.csproj", project);
        Assert.DoesNotContain("ViceSharp.RomFetch.csproj", project);
    }

    [Fact]
    public void AvaloniaSources_DoNotReferenceRuntimeInternals()
    {
        var sourceRoot = Path.Combine(RepoRoot, "src", "ViceSharp.Avalonia");
        var source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path)
                .Select(File.ReadAllText));

        foreach (var forbidden in new[]
        {
            "ViceSharp.Abstractions",
            "ViceSharp.Architectures",
            "ViceSharp.Core",
            "ViceSharp.Chips",
            "ViceSharp.RomFetch",
            "IMachine",
            "IVideoChip",
            "ArchitectureBuilder"
        })
        {
            Assert.DoesNotContain(forbidden, source);
        }
    }

    [Fact]
    public async Task AttachPanelViewModel_AttachesAndEjectsThroughHostClient()
    {
        var client = new FakeHostProtocolClient();
        var viewModel = new AttachPanelViewModel(client);
        var slot = viewModel.Slots.Single(candidate => candidate.Slot == MediaSlot.Drive8);

        slot.IsReadOnly = true;
        await viewModel.AttachAsync(slot, @"C:\media\demo.d64", TestContext.Current.CancellationToken);
        await viewModel.EjectAsync(slot, TestContext.Current.CancellationToken);

        Assert.Equal(MediaSlot.Drive8, client.AttachedSlot);
        Assert.Equal(@"C:\media\demo.d64", client.AttachedPath);
        Assert.True(client.AttachedReadOnly);
        Assert.Equal(MediaSlot.Drive8, client.DetachedSlot);
        Assert.Contains(@"C:\media\demo.d64", slot.RecentFiles);
        Assert.False(slot.IsAttached);
    }

    [Fact]
    public void AttachPanelViewModel_ChangesDockSideWithoutRuntimeAccess()
    {
        var viewModel = new AttachPanelViewModel(new FakeHostProtocolClient());

        viewModel.DockRight();
        Assert.Equal(AttachDockSide.Right, viewModel.DockSide);

        viewModel.DockLeft();
        Assert.Equal(AttachDockSide.Left, viewModel.DockSide);
    }

    [Fact]
    public async Task InProcessGrpcHost_GeneratedClientCreatesSessionAndDirectFrameSourceReturnsFrame()
    {
        await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
        using var channel = GrpcChannel.ForAddress(host.Endpoint);
        var hostClient = new GrpcContracts.EmulatorHost.EmulatorHostClient(channel);
        var mediaClient = new GrpcContracts.MediaService.MediaServiceClient(channel);

        var created = await hostClient.CreateSessionAsync(
            new GrpcContracts.CreateEmulatorSessionRequest(),
            cancellationToken: TestContext.Current.CancellationToken);
        var media = await mediaClient.ListMediaAsync(
            new GrpcContracts.SessionRequest { SessionId = created.SessionId },
            cancellationToken: TestContext.Current.CancellationToken);
        var frame = await host.VideoFrameSource.GetFrameAsync(
            created.SessionId,
            TestContext.Current.CancellationToken);

        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, created.Status.Code);
        Assert.False(string.IsNullOrWhiteSpace(created.SessionId));
        Assert.Equal(GrpcContracts.RpcStatusCode.Ok, media.Status.Code);
        Assert.Equal(RpcStatusCode.Ok, frame.Status.Code);
        Assert.NotNull(frame.Frame);
        Assert.NotEmpty(frame.Frame.Bgra);
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }

    private sealed class FakeHostProtocolClient : IHostProtocolClient
    {
        private readonly Dictionary<MediaSlot, MediaAttachmentDto> _attachments = new();

        public string SessionId => "test-session";

        public MediaSlot? AttachedSlot { get; private set; }

        public string? AttachedPath { get; private set; }

        public bool AttachedReadOnly { get; private set; }

        public MediaSlot? DetachedSlot { get; private set; }

        public ValueTask<GetEmulatorStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new GetEmulatorStatusResponse(RpcStatus.Ok(), CreateStatus()));
        }

        public ValueTask<EmulatorCommandResponse> StartAsync(CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken);

        public ValueTask<EmulatorCommandResponse> PauseAsync(CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken);

        public ValueTask<EmulatorCommandResponse> ResumeAsync(CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken);

        public ValueTask<EmulatorCommandResponse> StepCycleAsync(int cycleCount, CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken);

        public ValueTask<EmulatorCommandResponse> StepFrameAsync(int frameCount, CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken);

        public ValueTask<EmulatorCommandResponse> RewindCycleAsync(int cycleCount, CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken, RpcStatus.NotImplemented("No rewind history."));

        public ValueTask<EmulatorCommandResponse> RewindFrameAsync(int frameCount, CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken, RpcStatus.NotImplemented("No rewind history."));

        public ValueTask<EmulatorCommandResponse> ColdResetAsync(CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken);

        public ValueTask<EmulatorCommandResponse> WarmResetAsync(CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken);

        public ValueTask<EmulatorCommandResponse> ResetAndAutostartDrive8Async(CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken, RpcStatus.NotImplemented("No autostart support."));

        public ValueTask<EmulatorCommandResponse> SetLimiterRateAsync(double ratePercent, CancellationToken cancellationToken = default)
            => CommandAsync(cancellationToken);

        public ValueTask<ListMediaResponse> ListMediaAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new ListMediaResponse(RpcStatus.Ok(), _attachments.Values.ToArray()));
        }

        public ValueTask<AttachMediaResponse> AttachMediaAsync(
            MediaSlot slot,
            string filePath,
            bool isReadOnly,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AttachedSlot = slot;
            AttachedPath = filePath;
            AttachedReadOnly = isReadOnly;

            var attachment = new MediaAttachmentDto(
                slot,
                filePath,
                Path.GetFileName(filePath),
                true,
                isReadOnly,
                true);
            _attachments[slot] = attachment;

            return ValueTask.FromResult(new AttachMediaResponse(RpcStatus.Ok(), attachment));
        }

        public ValueTask<DetachMediaResponse> DetachMediaAsync(
            MediaSlot slot,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DetachedSlot = slot;
            _attachments.Remove(slot, out var attachment);
            return ValueTask.FromResult(new DetachMediaResponse(RpcStatus.Ok(), attachment));
        }

        public ValueTask<InputCommandResponse> SetKeyStateAsync(
            string key,
            bool isPressed,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new InputCommandResponse(
                RpcStatus.Ok(),
                new InputStateDto([new KeyStateDto(key, isPressed, true)], [])));
        }

        public ValueTask<ListKeyboardMapsResponse> ListKeyboardMapsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new ListKeyboardMapsResponse(RpcStatus.Ok(), []));
        }

        public ValueTask<KeyboardMapResponse> SetKeyboardMapAsync(
            string keyboardMapId,
            byte[]? payload = null,
            string displayName = "",
            string sourcePath = "",
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new KeyboardMapResponse(RpcStatus.NotFound("No keyboard maps."), null));
        }

        public ValueTask<MonitorCommandResponse> ExecuteMonitorCommandAsync(
            string command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new MonitorCommandResponse(RpcStatus.NotImplemented("No monitor."), string.Empty, CreateStatus()));
        }

        public ValueTask<GetVideoFrameResponse> GetFrameAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new GetVideoFrameResponse(RpcStatus.Unavailable("No frame."), null));
        }

        private ValueTask<EmulatorCommandResponse> CommandAsync(
            CancellationToken cancellationToken,
            RpcStatus? status = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new EmulatorCommandResponse(status ?? RpcStatus.Ok(), CreateStatus()));
        }

        private EmulatorStatusDto CreateStatus()
        {
            return new EmulatorStatusDto(
                SessionId,
                "Fake",
                EmulatorRunState.Stopped,
                0,
                new MachineStateDto(0, 0, 0, 0, 0, 0, 0));
        }
    }
}
