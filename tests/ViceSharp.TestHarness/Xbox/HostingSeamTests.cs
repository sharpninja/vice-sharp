namespace ViceSharp.TestHarness.Xbox;

using System.Threading.Tasks;
using ViceSharp.Abstractions;
using ViceSharp.Protocol;
using ViceSharp.TestHarness.Xbox.Fakes;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP S21 (IMPL-XBOXUWP-021), area XBOXUI. Exercises the
/// ViewModel-owned host-facade seam (<see cref="IEmulatorSessionFacade"/>,
/// <see cref="ILocalVideoFramePull"/>, <see cref="IXboxSettingsGateway"/>) against
/// off-console fakes, proving the 10-foot ViewModels are unit-testable without the
/// real head, the console, or any engine/host/XAML dependency (TR-MVVM-001).
/// </summary>
[Trait("Category", "Xbox")]
public sealed class HostingSeamTests
{
    /// <summary>
    /// FR-XBOXUI-001, TR-MVVM-001. TEST-XBOXUI-004.
    /// Use case: a ViewModel drives the emulator session lifecycle purely through
    /// the facade seam; a fake head records what the real adapter would forward to
    /// <c>IConsoleEmulatorHost</c>.
    /// Acceptance: <see cref="IEmulatorSessionFacade.CreateSessionAsync"/> yields a
    /// non-empty id, and Start/Pause/Resume/ColdReset/WarmReset are each recorded
    /// once, in call order, bound to that session id.
    /// </summary>
    [Fact]
    public async Task Facade_SessionLifecycle_MintsIdAndRecordsCallsInOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeEmulatorSessionFacade();
        IEmulatorSessionFacade facade = fake;

        var sessionId = await facade.CreateSessionAsync(ct);

        Assert.False(string.IsNullOrEmpty(sessionId));

        await facade.StartAsync(sessionId, ct);
        await facade.PauseAsync(sessionId, ct);
        await facade.ResumeAsync(sessionId, ct);
        await facade.ColdResetAsync(sessionId, ct);
        await facade.WarmResetAsync(sessionId, ct);

        Assert.Equal(
            new[]
            {
                $"Create:{sessionId}",
                $"Start:{sessionId}",
                $"Pause:{sessionId}",
                $"Resume:{sessionId}",
                $"ColdReset:{sessionId}",
                $"WarmReset:{sessionId}",
            },
            fake.Calls);
    }

    /// <summary>
    /// FR-XBOXUI-001, TR-MVVM-001. TEST-XBOXUI-004.
    /// Use case: the input-mapping / virtual-keyboard ViewModels obtain the
    /// machine-owned joystick and keyboard input surfaces through the facade seam.
    /// Acceptance: <see cref="IEmulatorSessionFacade.GetJoystickInput"/> and
    /// <see cref="IEmulatorSessionFacade.GetKeyboardInput"/> return the fake devices
    /// (non-null, and the same instances the fake head owns), and injection through
    /// them is observed.
    /// </summary>
    [Fact]
    public async Task Facade_ExposesJoystickAndKeyboardInputDevices()
    {
        var fake = new FakeEmulatorSessionFacade();
        IEmulatorSessionFacade facade = fake;

        var sessionId = await facade.CreateSessionAsync(TestContext.Current.CancellationToken);

        IMachineJoystickInput? joystick = facade.GetJoystickInput(sessionId);
        IMachineKeyboardInput? keyboard = facade.GetKeyboardInput(sessionId);

        Assert.NotNull(joystick);
        Assert.NotNull(keyboard);
        Assert.Same(fake.Joystick, joystick);
        Assert.Same(fake.Keyboard, keyboard);

        Assert.True(joystick!.SetJoystickState(2, 0x01, true));
        Assert.True(keyboard!.SetKeyState("Return", true));

        Assert.Equal(2, fake.Joystick.LastControlPort);
        Assert.Equal(0x01, fake.Joystick.LastDirectionMask);
        Assert.True(fake.Joystick.LastFireButton);
        Assert.Equal("Return", fake.Keyboard.LastKey);
        Assert.True(fake.Keyboard.LastPressed);
    }

    /// <summary>
    /// FR-XVIDEO-002, TR-MVVM-001. TEST-XBOXUI-004.
    /// Use case: the video-pull ViewModel copies the latest committed frame through
    /// the pure read-only pull seam, tolerating the "no frame yet" boundary.
    /// Acceptance: <see cref="ILocalVideoFramePull.TryCopyFrameInto"/> returns
    /// <c>false</c> (with zeroed out-params) before a frame is published, then
    /// <c>true</c> with the configured width/height/cycle once one is, mirroring
    /// <c>LocalVideoFrameSource.TryCopyFrameInto</c>; a too-small destination
    /// returns <c>false</c>.
    /// </summary>
    [Fact]
    public void VideoFramePull_ReturnsFalseBeforeFrameThenTrueWithConfiguredGeometry()
    {
        var fake = new FakeEmulatorSessionFacade();
        IEmulatorSessionFacade facade = fake;
        ILocalVideoFramePull pull = facade.VideoFrames;

        fake.Frames.Width = 8;
        fake.Frames.Height = 4;
        var destination = new byte[8 * 4 * 4];

        // Before the first frame is committed: pull fails, UI skips the tick.
        Assert.False(pull.TryCopyFrameInto("fake-session-1", destination, out var w0, out var h0, out var c0));
        Assert.Equal(0, w0);
        Assert.Equal(0, h0);
        Assert.Equal(0, c0);

        // Publish a canned frame with an explicit geometry/cycle.
        fake.Frames.PublishFrame(8, 4, 4242);

        Assert.True(pull.TryCopyFrameInto("fake-session-1", destination, out var w1, out var h1, out var c1));
        Assert.Equal(8, w1);
        Assert.Equal(4, h1);
        Assert.Equal(4242, c1);
        Assert.Equal(fake.Frames.FillByte, destination[0]);
        Assert.Equal("fake-session-1", fake.Frames.LastRequestedSessionId);

        // Too-small destination: false (the head sizes to BufferLength and retries).
        var tooSmall = new byte[4];
        Assert.False(pull.TryCopyFrameInto("fake-session-1", tooSmall, out _, out _, out _));
    }

    /// <summary>
    /// FR-XSET-001, TR-MVVM-001. TEST-XBOXUI-004.
    /// Use case: the Settings ViewModel reads the host-canonical settings, edits a
    /// value, and writes them back through the settings gateway seam that mirrors
    /// <c>IHostProtocolClient</c>.
    /// Acceptance: a GetSettings -> UpdateSettings round-trip through
    /// <see cref="IXboxSettingsGateway"/> succeeds, and the fake gateway records the
    /// exact <see cref="UpdateSettingsRequest"/> that was submitted.
    /// </summary>
    [Fact]
    public async Task SettingsGateway_RoundTripsGetThenUpdateAndRecordsRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var fake = new FakeXboxSettingsGateway();
        IXboxSettingsGateway gateway = fake;

        var get = await gateway.GetSettingsAsync(ct);

        Assert.True(get.Status.IsSuccess);
        Assert.NotNull(get.Settings);
        Assert.Equal(1, fake.GetSettingsCount);

        var request = new UpdateSettingsRequest(
            SessionId: "fake-session-1",
            Limiter: new LimiterSettingsDto(200, true),
            Display: get.Settings!.Display,
            Input: get.Settings.Input,
            ProfileId: get.Settings.ProfileId);

        var update = await gateway.UpdateSettingsAsync(request, ct);

        Assert.True(update.Status.IsSuccess);
        Assert.NotNull(update.Settings);
        Assert.Same(request, fake.LastUpdateRequest);
        Assert.Equal(200, fake.LastUpdateRequest!.Limiter!.RatePercent);
        Assert.Equal(200, update.Settings!.Limiter.RatePercent);
    }
}
