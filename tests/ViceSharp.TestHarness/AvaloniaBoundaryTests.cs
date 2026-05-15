namespace ViceSharp.TestHarness;

using Grpc.Net.Client;
using ViceSharp.Chips.IEC;
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
    public async Task AttachPanelViewModel_SendsLocalMediaPayloadThroughHostClient()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.d64");
        var payload = new byte[] { 0x44, 0x36, 0x34 };
        await File.WriteAllBytesAsync(filePath, payload, TestContext.Current.CancellationToken);

        try
        {
            var client = new FakeHostProtocolClient();
            var viewModel = new AttachPanelViewModel(client);
            var slot = viewModel.Slots.Single(candidate => candidate.Slot == MediaSlot.Drive8);

            await viewModel.AttachAsync(slot, filePath, TestContext.Current.CancellationToken);

            Assert.Equal(MediaSlot.Drive8, client.AttachedSlot);
            Assert.Equal(filePath, client.AttachedPath);
            Assert.Equal(Path.GetFileName(filePath), client.AttachedDisplayName);
            Assert.Equal(payload, client.AttachedPayload);
            Assert.Contains(filePath, slot.RecentFiles);
            Assert.DoesNotContain(client.ReturnedAttachmentPath, slot.RecentFiles);
            Assert.Equal(Path.GetFileName(filePath), slot.StatusText);
        }
        finally
        {
            File.Delete(filePath);
        }
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
    public async Task AttachPanelViewModel_RefreshLoadsHostSettingsAndProfiles()
    {
        var client = new FakeHostProtocolClient();
        var viewModel = new AttachPanelViewModel(client);

        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        var profile = Assert.Single(viewModel.MachineProfiles);
        Assert.Equal("c64", profile.Id);
        Assert.False(profile.IsPlaceholder);
        Assert.Equal(profile, viewModel.SelectedMachineProfile);
        Assert.Equal(100, viewModel.LimiterRatePercent);
        Assert.True(viewModel.LimiterEnabled);
        Assert.Equal("Host direct", viewModel.SelectedRenderer);
        Assert.Equal("2x", viewModel.SelectedDisplayScale);
        Assert.Equal("VICE default", viewModel.SelectedPalette);
        Assert.Equal("Visible area", viewModel.SelectedCropMode);
        Assert.Equal("VICE pixel aspect", viewModel.SelectedAspectMode);
        Assert.Equal("Enabled", viewModel.SelectedAudioMode);
        Assert.Equal("Keyboard + joystick", viewModel.SelectedInputMode);
        Assert.Equal("Joystick 2", viewModel.SelectedPrimaryJoystickPort);
        Assert.False(viewModel.SwapJoystickPorts);
        Assert.Equal("Auto detect", viewModel.SelectedResourceMode);
        Assert.False(viewModel.HasPendingSettingsChanges);
        Assert.False(viewModel.RequiresRestart);
        Assert.Equal("Settings loaded from host.", viewModel.SettingsStatusText);
    }

    [Fact]
    public async Task HostProtocolClientBoundary_PreservesKeyboardPayloadMetadata()
    {
        var client = new FakeHostProtocolClient();

        var response = await client.SetKeyStateAsync(
            "A",
            true,
            physicalKey: "KeyA",
            text: "a",
            modifiers: 1,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.Equal("A", client.LastKey);
        Assert.True(client.LastKeyPressed);
        Assert.Equal("KeyA", client.LastPhysicalKey);
        Assert.Equal("a", client.LastText);
        Assert.Equal(1, client.LastModifiers);
        Assert.Contains(response.InputState!.Keys, key =>
            key.Key == "A" &&
            key.IsPressed &&
            key.PhysicalKey == "KeyA" &&
            key.Text == "a" &&
            key.Modifiers == 1);
    }

    [Fact]
    public async Task AttachPanelViewModel_SettingsApplyAndRevertUseHostBoundaryOnly()
    {
        var client = new FakeHostProtocolClient();
        var viewModel = new AttachPanelViewModel(client);

        Assert.NotEmpty(viewModel.MachineProfiles);
        Assert.All(viewModel.MachineProfiles, profile => Assert.True(profile.IsPlaceholder));

        viewModel.LimiterRatePercent = 1200;
        viewModel.LimiterEnabled = false;
        viewModel.SelectedRenderer = "Software";
        viewModel.SelectedDisplayScale = "Fit window";
        viewModel.SelectedCropMode = "Borderless";
        viewModel.SelectedAspectMode = "Force 4:3";
        viewModel.SelectedPalette = "Pepto";
        viewModel.SelectedAudioMode = "Muted";
        viewModel.SelectedInputMode = "Keyboard only";
        viewModel.SelectedResourceMode = "Use configured paths";

        Assert.Equal(AttachPanelViewModel.LimiterMaximumPercent, viewModel.LimiterRatePercent);
        Assert.True(viewModel.HasPendingSettingsChanges);
        Assert.True(viewModel.RequiresRestart);

        await viewModel.ApplySettingsAsync(restartRequired: false, TestContext.Current.CancellationToken);

        Assert.Equal(AttachPanelViewModel.LimiterMaximumPercent, client.LimiterRatePercent);
        Assert.True(client.LimiterEnabled.HasValue);
        Assert.False(client.LimiterEnabled.Value);
        Assert.NotNull(client.LastDisplaySettings);
        Assert.Equal("software", client.LastDisplaySettings!.Renderer);
        Assert.Equal("fit-window", client.LastDisplaySettings.Scale);
        Assert.Equal("borderless", client.LastDisplaySettings.CropMode);
        Assert.Equal("force-4-3", client.LastDisplaySettings.AspectMode);
        Assert.NotNull(client.LastAudioSettings);
        Assert.Equal("muted", client.LastAudioSettings!.Mode);
        Assert.NotNull(client.LastInputSettings);
        Assert.Equal("keyboard-only", client.LastInputSettings!.Mode);
        Assert.NotNull(client.LastResourceSettings);
        Assert.Equal("configured-paths", client.LastResourceSettings!.Mode);
        Assert.False(client.LastRestartSession);
        Assert.False(viewModel.HasPendingSettingsChanges);
        Assert.False(viewModel.RequiresRestart);
        Assert.Contains("applied", viewModel.SettingsStatusText, StringComparison.OrdinalIgnoreCase);

        viewModel.LimiterRatePercent = 25;
        Assert.True(viewModel.HasPendingSettingsChanges);
        await viewModel.ApplySettingsAsync(restartRequired: true, TestContext.Current.CancellationToken);

        Assert.True(client.LastRestartSession);
        Assert.False(viewModel.RequiresRestart);

        viewModel.LimiterRatePercent = 50;
        Assert.True(viewModel.HasPendingSettingsChanges);

        viewModel.RevertSettings();

        Assert.Equal(25, viewModel.LimiterRatePercent);
        Assert.Equal("Fit window", viewModel.SelectedDisplayScale);
        Assert.False(viewModel.HasPendingSettingsChanges);
    }

    [Fact]
    public async Task AttachPanelViewModel_ApplyReflectsHostReturnedCanonicalSettings()
    {
        var client = new FakeHostProtocolClient
        {
            SettingsReturnedFromUpdate = new SessionSettingsDto(
                "c64",
                new LimiterSettingsDto(80, true),
                new DisplaySettingsDto("software", "amber", false, false, "fit-window", "borderless", "square-pixels"),
                new InputSettingsDto("c64:gtk3_pos", InputPort.Joystick1, true, "keyboard-only"),
                new AudioSettingsDto("muted"),
                new ResourceSettingsDto("configured-paths"))
        };
        var viewModel = new AttachPanelViewModel(client);

        viewModel.LimiterRatePercent = 250;
        viewModel.SelectedPalette = "Pepto";
        viewModel.SelectedCropMode = "Visible area";
        viewModel.SelectedAspectMode = "VICE pixel aspect";
        viewModel.SelectedPrimaryJoystickPort = "Joystick 2";

        await viewModel.ApplySettingsAsync(restartRequired: false, TestContext.Current.CancellationToken);

        Assert.Equal(80, viewModel.LimiterRatePercent);
        Assert.True(viewModel.LimiterEnabled);
        Assert.Equal("Software", viewModel.SelectedRenderer);
        Assert.Equal("Fit window", viewModel.SelectedDisplayScale);
        Assert.Equal("Amber", viewModel.SelectedPalette);
        Assert.Equal("Borderless", viewModel.SelectedCropMode);
        Assert.Equal("Square pixels", viewModel.SelectedAspectMode);
        Assert.Equal("Muted", viewModel.SelectedAudioMode);
        Assert.Equal("Keyboard only", viewModel.SelectedInputMode);
        Assert.Equal("Joystick 1", viewModel.SelectedPrimaryJoystickPort);
        Assert.True(viewModel.SwapJoystickPorts);
        Assert.Equal("Use configured paths", viewModel.SelectedResourceMode);
        Assert.False(viewModel.HasPendingSettingsChanges);
        Assert.False(viewModel.RequiresRestart);
    }

    [Fact]
    public async Task AttachPanelViewModel_SendsJoystickRoutingSettingsThroughHostBoundary()
    {
        var client = new FakeHostProtocolClient();
        var viewModel = new AttachPanelViewModel(client);

        viewModel.SelectedPrimaryJoystickPort = "Joystick 1";
        viewModel.SwapJoystickPorts = true;

        Assert.True(viewModel.HasPendingSettingsChanges);
        Assert.False(viewModel.RequiresRestart);

        await viewModel.ApplySettingsAsync(restartRequired: false, TestContext.Current.CancellationToken);

        Assert.NotNull(client.LastInputSettings);
        Assert.Equal(InputPort.Joystick1, client.LastInputSettings.PrimaryJoystickPort);
        Assert.True(client.LastInputSettings.SwapJoystickPorts);
        Assert.False(client.LastRestartSession);
        Assert.False(viewModel.HasPendingSettingsChanges);
        Assert.False(viewModel.RequiresRestart);

        viewModel.SelectedPrimaryJoystickPort = "Joystick 2";
        viewModel.SwapJoystickPorts = false;
        viewModel.RevertSettings();

        Assert.Equal("Joystick 1", viewModel.SelectedPrimaryJoystickPort);
        Assert.True(viewModel.SwapJoystickPorts);
    }

    [Fact]
    public async Task AttachPanelViewModel_ValidateSettingsUsesHostBoundaryWithoutApplying()
    {
        var client = new FakeHostProtocolClient
        {
            SettingsValidationResponse = new ValidateSettingsResourcesResponse(
                RpcStatus.Ok(),
                [
                    new SettingsResourceValidationDto(
                        "display.renderer",
                        SettingsResourceKind.Display,
                        true,
                        false,
                        "display.renderer is available."),
                    new SettingsResourceValidationDto(
                        "resources.mode",
                        SettingsResourceKind.Resource,
                        false,
                        true,
                        "Configured resource paths are missing.")
                ])
        };
        var viewModel = new AttachPanelViewModel(client);

        viewModel.SelectedRenderer = "Software";
        viewModel.SelectedResourceMode = "Use configured paths";

        await viewModel.ValidateSettingsAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(client.LastValidateSettingsRequest);
        Assert.Equal("software", client.LastValidateSettingsRequest!.Display!.Renderer);
        Assert.Equal("configured-paths", client.LastValidateSettingsRequest.Resources!.Mode);
        Assert.Null(client.LastDisplaySettings);
        Assert.Null(client.LastResourceSettings);
        Assert.True(viewModel.HasPendingSettingsChanges);
        Assert.True(viewModel.HasSettingsValidationResults);
        Assert.Equal(2, viewModel.SettingsValidationResults.Count);
        Assert.Contains(viewModel.SettingsValidationResults, resource => !resource.IsValid && resource.ResourceKey == "resources.mode");
        Assert.Equal("Settings validation found 1 issue(s).", viewModel.SettingsStatusText);

        viewModel.SelectedRenderer = "Host direct";

        Assert.False(viewModel.HasSettingsValidationResults);
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

    [Fact]
    public async Task GrpcHostProtocolClient_AttachesLocalMediaAsPayloadAcrossHostBoundary()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"vice-sharp-{Guid.NewGuid():N}.d64");
        await File.WriteAllBytesAsync(filePath, CreateD64Image(), TestContext.Current.CancellationToken);

        try
        {
            await using var host = await InProcessGrpcHost.StartAsync(TestContext.Current.CancellationToken);
            using var client = new GrpcHostProtocolClient(host.Endpoint);

            var response = await client.AttachMediaAsync(
                MediaSlot.Drive8,
                filePath,
                isReadOnly: true,
                File.ReadAllBytes(filePath),
                Path.GetFileName(filePath),
                TestContext.Current.CancellationToken);

            Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
            Assert.NotNull(response.Attachment);
            Assert.Equal(MediaSlot.Drive8, response.Attachment.Slot);
            Assert.Equal(Path.GetFileName(filePath), response.Attachment.DisplayName);
            Assert.True(response.Attachment.IsReadOnly);
            Assert.True(response.Attachment.IsAttached);
            Assert.NotEqual(filePath, response.Attachment.FilePath);
            Assert.True(File.Exists(response.Attachment.FilePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static byte[] CreateD64Image()
    {
        var image = new byte[D64Image.DiskSize35Track];
        var sectorOffset = Track18Sector1Offset();
        for (var i = 0; i < 256; i++)
            image[sectorOffset + i] = (byte)(255 - i);

        return image;
    }

    private static int Track18Sector1Offset()
    {
        var offset = 0;
        for (var track = 1; track < 18; track++)
            offset += 21 * 256;

        return offset + 256;
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

        public byte[]? AttachedPayload { get; private set; }

        public string? AttachedDisplayName { get; private set; }

        public string? ReturnedAttachmentPath { get; private set; }

        public MediaSlot? DetachedSlot { get; private set; }

        public double? LimiterRatePercent { get; private set; }

        public bool? LimiterEnabled { get; private set; }

        public bool LastRestartSession { get; private set; }

        public DisplaySettingsDto? LastDisplaySettings { get; private set; }

        public InputSettingsDto? LastInputSettings { get; private set; }

        public AudioSettingsDto? LastAudioSettings { get; private set; }

        public ResourceSettingsDto? LastResourceSettings { get; private set; }

        public SessionSettingsDto? SettingsReturnedFromUpdate { get; init; }

        public ValidateSettingsResourcesResponse? SettingsValidationResponse { get; init; }

        public ValidateSettingsResourcesRequest? LastValidateSettingsRequest { get; private set; }

        public string? LastKey { get; private set; }

        public bool LastKeyPressed { get; private set; }

        public string? LastPhysicalKey { get; private set; }

        public string? LastText { get; private set; }

        public int LastModifiers { get; private set; }

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
        {
            LimiterRatePercent = ratePercent;
            return CommandAsync(cancellationToken);
        }

        public ValueTask<ListSettingsProfilesResponse> ListSettingsProfilesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new ListSettingsProfilesResponse(
                RpcStatus.Ok(),
                [
                    new SettingsProfileDto("c64", "C64 PAL", "x64sc", true, true, "test profile")
                ]));
        }

        public ValueTask<GetSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new GetSettingsResponse(
                RpcStatus.Ok(),
                new SessionSettingsDto(
                    "c64",
                    new LimiterSettingsDto(100, true),
                    new DisplaySettingsDto("host", "vice", true, true, "2x", "visible-area", "vice-pixel-aspect"),
                    new InputSettingsDto("c64:gtk3_pos", InputPort.Joystick2, false, "keyboard-joystick"),
                    new AudioSettingsDto("enabled"),
                    new ResourceSettingsDto("auto-detect"))));
        }

        public ValueTask<UpdateSettingsResponse> UpdateSettingsAsync(
            UpdateSettingsRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LimiterRatePercent = request.Limiter?.RatePercent;
            LimiterEnabled = request.Limiter?.IsEnabled;
            LastRestartSession = request.RestartSession;
            LastDisplaySettings = request.Display;
            LastInputSettings = request.Input;
            LastAudioSettings = request.Audio;
            LastResourceSettings = request.Resources;
            return ValueTask.FromResult(new UpdateSettingsResponse(
                RpcStatus.Ok(),
                SettingsReturnedFromUpdate ?? new SessionSettingsDto(
                    string.IsNullOrWhiteSpace(request.ProfileId) ? "c64" : request.ProfileId,
                    request.Limiter ?? new LimiterSettingsDto(),
                    request.Display ?? new DisplaySettingsDto(),
                    request.Input ?? new InputSettingsDto(),
                    request.Audio ?? new AudioSettingsDto(),
                    request.Resources ?? new ResourceSettingsDto()),
                Array.Empty<SettingApplyDiagnosticDto>()));
        }

        public ValueTask<ValidateSettingsResourcesResponse> ValidateSettingsResourcesAsync(
            ValidateSettingsResourcesRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastValidateSettingsRequest = request;
            return ValueTask.FromResult(SettingsValidationResponse ??
                new ValidateSettingsResourcesResponse(RpcStatus.Ok(), Array.Empty<SettingsResourceValidationDto>()));
        }

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
            => AttachMediaAsync(slot, filePath, isReadOnly, Array.Empty<byte>(), string.Empty, cancellationToken);

        public ValueTask<AttachMediaResponse> AttachMediaAsync(
            MediaSlot slot,
            string filePath,
            bool isReadOnly,
            byte[] payload,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AttachedSlot = slot;
            AttachedPath = filePath;
            AttachedReadOnly = isReadOnly;
            AttachedPayload = payload;
            AttachedDisplayName = displayName;
            ReturnedAttachmentPath = payload.Length == 0
                ? filePath
                : Path.Combine(Path.GetTempPath(), "ViceSharp", "media", $"host-{displayName}");

            var attachment = new MediaAttachmentDto(
                slot,
                ReturnedAttachmentPath,
                string.IsNullOrWhiteSpace(displayName) ? Path.GetFileName(filePath) : displayName,
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
            string physicalKey = "",
            string text = "",
            int modifiers = 0,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastKey = key;
            LastKeyPressed = isPressed;
            LastPhysicalKey = physicalKey;
            LastText = text;
            LastModifiers = modifiers;
            return ValueTask.FromResult(new InputCommandResponse(
                RpcStatus.Ok(),
                new InputStateDto([new KeyStateDto(key, isPressed, true, physicalKey, text, modifiers)], [])));
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
