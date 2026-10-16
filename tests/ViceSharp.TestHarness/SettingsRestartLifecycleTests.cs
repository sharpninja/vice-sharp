namespace ViceSharp.TestHarness;

using System.Buffers.Binary;
using ViceSharp.Abstractions;
using ViceSharp.Architectures.C1541;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.IEC;
using ViceSharp.Core;
using ViceSharp.Core.Media;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR-SID-013 / FR-DRVTRUE-001 / FR-MED-002 / FR-MED-003 / TEST-SID-001 / TEST-MED-001.
/// Settings restarts retain the factory's runtime connections and finalize owned captures.
/// Uses generated disks, an idle drive ROM, in-memory recorders and fake playback only.
/// </summary>
public sealed class SettingsRestartLifecycleTests
{
    /// <summary>Restarted sessions can still record and suspend/resume their live output.</summary>
    [Fact]
    public async Task Restart_PreservesFactoryAudioTapAndWarpPolicy()
    {
        var playback = new FakePlayback();
        var tap = new CaptureAudioTap(playback);
        var factory = new RecordingFactory(_ => CreateMinimalSession(tap));
        var current = CreateMinimalSession(tap);
        var (registry, service) = CreateService(current, factory);

        var result = await service.UpdateSettingsAsync(RestartRequest(current) with
        {
            Limiter = new LimiterSettingsDto(100, false),
        }, TestContext.Current.CancellationToken);

        Assert.True(result.Status.IsSuccess, result.Status.Message);
        Assert.True(registry.TryGet(current.SessionId, out var replacement));
        Assert.Same(tap, replacement.AudioCaptureTap);
        Assert.True(playback.Paused);
        replacement.SetLimiter(100, true);
        Assert.False(playback.Paused);
        using var output = new MemoryStream();
        replacement.BeginAudioCapture("after-restart", new WavAudioRecorder(output), output);
        tap.SubmitSamples([0.25f, -0.25f]);
        Assert.Equal(4, replacement.EndAudioCapture());
        Assert.Equal(4u, BinaryPrimitives.ReadUInt32LittleEndian(output.ToArray().AsSpan(40)));
    }

    /// <summary>Normal pacing after a restart resumes the shared leaf paused by old warp/fast-forward.</summary>
    [Theory]
    [InlineData(100, false)]
    [InlineData(250, true)]
    public async Task Restart_NormalPacingResumesPreviouslySuspendedSharedTap(double rate, bool enabled)
    {
        var playback = new FakePlayback();
        var tap = new CaptureAudioTap(playback);
        var current = CreateMinimalSession(tap);
        current.SetLimiter(rate, enabled);
        Assert.True(playback.Paused);
        var (registry, service) = CreateService(current, new RecordingFactory(_ => CreateMinimalSession(tap)));

        var result = await service.UpdateSettingsAsync(RestartRequest(current) with
        {
            Limiter = new LimiterSettingsDto(100, true),
        }, TestContext.Current.CancellationToken);

        Assert.True(result.Status.IsSuccess, result.Status.Message);
        Assert.True(registry.TryGet(current.SessionId, out var replacement));
        Assert.Same(tap, replacement.AudioCaptureTap);
        Assert.False(replacement.IsLiveAudioOutputSuspended);
        Assert.False(playback.Paused);
    }

    /// <summary>The actual 1541 unit and current inserted/ejected disk survive C64 restarts.</summary>
    [Theory]
    [InlineData(8, false)]
    [InlineData(9, false)]
    [InlineData(8, true)]
    [InlineData(9, true)]
    public async Task Restart_PreservesTrueDriveUnitAndAttachedDisk(int unit, bool eject)
    {
        var directory = Path.Combine(Path.GetTempPath(), "vicesharp-settings-restart", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var initialPath = Path.Combine(directory, "initial.d64");
        var currentPath = Path.Combine(directory, "current.d64");
        var disk = new D64Image(new byte[D64Image.DiskSize35Track]);
        disk.Format();
        File.WriteAllBytes(initialPath, disk.ToArray());
        File.WriteAllBytes(currentPath, disk.ToArray());
        var realFactory = C64MediaTestHost.CreateRuntimeFactory();
        var factory = new RecordingFactory(realFactory.Create);
        var current = realFactory.Create(new CreateEmulatorSessionRequest(
            C64MachineProfiles.C64Pal.Id, TrueDrive: true, TrueDriveDevice: unit,
            TrueDriveDiskImagePath: initialPath));
        var slot = unit == 9 ? MediaSlot.Drive9 : MediaSlot.Drive8;
        var (registry, service) = CreateService(current, factory);
        var media = new MediaServiceHost(registry);
        if (eject)
        {
            var detached = await media.DetachMediaAsync(new DetachMediaRequest(current.SessionId, slot), TestContext.Current.CancellationToken);
            Assert.True(detached.Status.IsSuccess, detached.Status.Message);
        }
        else
        {
            var attached = await media.AttachMediaAsync(new AttachMediaRequest(current.SessionId, slot, currentPath,
                DisplayName: "Synthetic.t64", IsReadOnly: true), TestContext.Current.CancellationToken);
            Assert.True(attached.Status.IsSuccess, attached.Status.Message);
        }

        var result = await service.UpdateSettingsAsync(RestartRequest(current), TestContext.Current.CancellationToken);

        Assert.True(result.Status.IsSuccess, result.Status.Message);
        Assert.True(factory.Request!.TrueDrive);
        Assert.Equal(unit, factory.Request.TrueDriveDevice);
        Assert.Equal(eject ? string.Empty : currentPath, factory.Request.TrueDriveDiskImagePath);
        Assert.True(registry.TryGet(current.SessionId, out var replacement));
        var rig = Assert.IsType<CoordinatorMachine>(replacement.Machine);
        var drive = Assert.Single(rig.Coordinator.Systems.Select(machine => machine.Architecture).OfType<C1541Descriptor>());
        Assert.Equal(unit, drive.DeviceNumber);
        Assert.Equal(eject ? null : currentPath, drive.DiskImagePath);
        Assert.NotNull(replacement.IecBusActivity);
        Assert.Same(factory.Created!.IecBusActivity, replacement.IecBusActivity);
        if (eject)
            Assert.Empty(replacement.MediaAttachments);
        else
        {
            var attachment = Assert.Single(replacement.MediaAttachments).Value;
            Assert.Equal(slot, attachment.Slot);
            Assert.Equal(currentPath, attachment.FilePath);
            Assert.Equal("Synthetic.t64", attachment.DisplayName);
            Assert.True(attachment.IsReadOnly);
            Assert.True(attachment.AppliedToRuntime);
        }
        // Retain generated inputs for diagnosis; no user ROM or game image is copied.
    }

    /// <summary>Profiles without a C64 IEC connection do not request an incompatible true-drive rig.</summary>
    [Theory]
    [InlineData("minimal")]
    [InlineData("ultimax")]
    [InlineData("c64gs")]
    public async Task Restart_IncompatibleProfileDoesNotRetainTrueDrive(string profileId)
    {
        var current = C64MediaTestHost.CreateRuntimeFactory().Create(new CreateEmulatorSessionRequest(
            C64MachineProfiles.C64Pal.Id, TrueDrive: true, TrueDriveDevice: 9));
        var factory = new RecordingFactory(_ => CreateMinimalSession());
        var (_, service) = CreateService(current, factory);
        var result = await service.UpdateSettingsAsync(RestartRequest(current) with
        {
            ProfileId = profileId,
        }, TestContext.Current.CancellationToken);

        Assert.True(result.Status.IsSuccess, result.Status.Message);
        Assert.False(factory.Request!.TrueDrive);
        Assert.Empty(factory.Request.TrueDriveDiskImagePath);
    }

    /// <summary>Capture finalization precedes the registry notification and patches WAV sizes.</summary>
    [Fact]
    public async Task Restart_FinalizesCapturesBeforeReplacement()
    {
        var tap = new CaptureAudioTap();
        var current = CreateMinimalSession(tap);
        var (registry, service) = CreateService(current, new RecordingFactory(_ => CreateMinimalSession(tap)));
        using var output = new TrackingStream();
        using var recorder = new WavAudioRecorder(output);
        var sink = new TrackingVideoSink();
        current.BeginAudioCapture("audio", recorder, output);
        current.BeginVideoCapture("video", sink);
        tap.SubmitSamples([0.5f, -0.5f]);
        bool notified = false;
        registry.SessionChanged += (_, _) =>
        {
            notified = true;
            Assert.True(sink.Disposed);
            Assert.True(output.Disposed);
            Assert.False(tap.IsRecording);
            Assert.False(current.IsAudioCaptureActive);
            Assert.False(current.IsVideoCaptureActive);
        };
        try
        {
            var result = await service.UpdateSettingsAsync(RestartRequest(current), TestContext.Current.CancellationToken);
            Assert.True(result.Status.IsSuccess, result.Status.Message);
            Assert.True(notified);
            Assert.Equal(4u, BinaryPrimitives.ReadUInt32LittleEndian(output.ToArray().AsSpan(40)));
        }
        finally { current.EndAllCaptures(); }
    }

    /// <summary>A failing video finalizer still closes WAV output and leaves the old session reachable.</summary>
    [Fact]
    public async Task Restart_CaptureFailureKeepsOldSessionAndClosesOtherCaptures()
    {
        var playback = new FakePlayback();
        var tap = new CaptureAudioTap(playback);
        var current = CreateMinimalSession(tap);
        var (registry, service) = CreateService(current, new RecordingFactory(_ => CreateMinimalSession(tap)));
        using var output = new TrackingStream();
        using var recorder = new WavAudioRecorder(output);
        current.BeginAudioCapture("audio", recorder, output);
        current.BeginVideoCapture("video", new TrackingVideoSink { ThrowOnDispose = true });
        current.AudioCaptureTap!.SubmitSamples([0.5f]);
        var result = await service.UpdateSettingsAsync(RestartRequest(current) with
        {
            Limiter = new LimiterSettingsDto(100, false),
        }, TestContext.Current.CancellationToken);

        Assert.False(result.Status.IsSuccess);
        Assert.Contains("synthetic finalization failure", result.Status.Message);
        Assert.True(registry.TryGet(current.SessionId, out var retained));
        Assert.Same(current, retained);
        Assert.True(output.Disposed);
        Assert.False(current.IsAudioCaptureActive);
        Assert.False(current.IsVideoCaptureActive);
        Assert.False(current.AudioCaptureTap.IsRecording);
        Assert.False(playback.Paused);
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32LittleEndian(output.ToArray().AsSpan(40)));
    }

    /// <summary>Construction failure does not stop captures belonging to the unchanged session.</summary>
    [Theory]
    [InlineData("invalid-operation")]
    [InlineData("io")]
    [InlineData("access")]
    [InlineData("argument")]
    public async Task Restart_FactoryFailureKeepsCaptureActive(string failure)
    {
        var current = CreateMinimalSession(new CaptureAudioTap());
        Exception error = failure switch
        {
            "io" => new IOException("synthetic factory failure"),
            "access" => new UnauthorizedAccessException("synthetic factory failure"),
            "argument" => new ArgumentException("synthetic factory failure"),
            _ => new InvalidOperationException("synthetic factory failure"),
        };
        var (registry, service) = CreateService(current, new RecordingFactory(_ => throw error));
        using var output = new MemoryStream();
        using var recorder = new WavAudioRecorder(output);
        current.BeginAudioCapture("audio", recorder, output);
        try
        {
            var result = await service.UpdateSettingsAsync(RestartRequest(current), TestContext.Current.CancellationToken);
            Assert.False(result.Status.IsSuccess);
            Assert.Contains(error.Message, result.Status.Message);
            Assert.True(registry.TryGet(current.SessionId, out var retained));
            Assert.Same(current, retained);
            Assert.True(current.IsAudioCaptureActive);
            Assert.True(current.AudioCaptureTap!.IsRecording);
            Assert.True(output.CanWrite);
        }
        finally { current.EndAllCaptures(); }
    }

    /// <summary>Reopening an externally changed/removed disk fails visibly without abandoning the live rig.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Restart_InvalidatedAttachedDiskKeepsCurrentRig(bool missing)
    {
        var directory = Path.Combine(Path.GetTempPath(), "vicesharp-settings-restart", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "attached.d64");
        var image = new D64Image(new byte[D64Image.DiskSize35Track]);
        image.Format();
        File.WriteAllBytes(path, image.ToArray());
        var factory = C64MediaTestHost.CreateRuntimeFactory();
        var current = factory.Create(new CreateEmulatorSessionRequest(C64MachineProfiles.C64Pal.Id,
            TrueDrive: true, TrueDriveDiskImagePath: path));
        var (registry, service) = CreateService(current, factory);
        if (missing)
            File.Move(path, Path.Combine(directory, "retained.d64"));
        else
            File.WriteAllBytes(path, [1, 2, 3]);

        var result = await service.UpdateSettingsAsync(RestartRequest(current), TestContext.Current.CancellationToken);

        Assert.False(result.Status.IsSuccess);
        Assert.NotEmpty(result.Status.Message);
        Assert.True(registry.TryGet(current.SessionId, out var retained));
        Assert.Same(current, retained);
        Assert.Equal(path, current.MediaAttachments[MediaSlot.Drive8].FilePath);
        Assert.IsType<CoordinatorMachine>(current.Machine);
    }

    private static UpdateSettingsRequest RestartRequest(EmulatorRuntimeSession session) =>
        new(session.SessionId, Display: new DisplaySettingsDto(), RestartSession: true);

    private static (EmulatorRuntimeRegistry, SettingsServiceHost) CreateService(EmulatorRuntimeSession session, IEmulatorRuntimeFactory factory)
    {
        var registry = new EmulatorRuntimeRegistry();
        registry.Add(session);
        return (registry, new SettingsServiceHost(registry, factory));
    }

    private static EmulatorRuntimeSession CreateMinimalSession(CaptureAudioTap? tap = null)
    {
        var factory = new DefaultEmulatorRuntimeFactory(new ArchitectureBuilder(), [MinimalHostArchitectureDescriptor.Instance]);
        var session = factory.Create(new CreateEmulatorSessionRequest());
        session.AudioCaptureTap = tap;
        return session;
    }

    private sealed class RecordingFactory(Func<CreateEmulatorSessionRequest, EmulatorRuntimeSession> create) : IEmulatorRuntimeFactory
    {
        public CreateEmulatorSessionRequest? Request { get; private set; }
        public EmulatorRuntimeSession? Created { get; private set; }
        public EmulatorRuntimeSession Create(CreateEmulatorSessionRequest request)
        {
            Request = request;
            return Created = create(request);
        }
    }

    private sealed class FakePlayback : IAudioBackend
    {
        public bool Paused { get; private set; }
        public int QueuedSampleCount => 0;
        public void SubmitSamples(ReadOnlySpan<float> samples) { }
        public void Pause() => Paused = true;
        public void Resume() => Paused = false;
        public void Stop() { }
    }

    private sealed class TrackingStream : MemoryStream
    {
        public bool Disposed { get; private set; }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }

    private sealed class TrackingVideoSink : IVideoCaptureSink
    {
        public bool Disposed { get; private set; }
        public bool ThrowOnDispose { get; init; }
        public int FrameCount => 0;
        public void CaptureFrame(ReadOnlySpan<byte> bgra, int width, int height) { }
        public void Dispose()
        {
            Disposed = true;
            if (ThrowOnDispose) throw new IOException("synthetic finalization failure");
        }
    }
}
