namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Host.Runtime;
using ViceSharp.Host.Services;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// FR/TR: FR-SNAPSHOT, TR-AUDIO-WARP-001, FR-MED-003 (PR 7 Codex review findings).
/// Use case: snapshot restore rewinds the master clock; a settings restart keeps the live
/// audio tap and finalizes captures on the outgoing session.
/// Acceptance: see each test.
/// </summary>
public sealed class Pr7CodexFindingsTests
{
    /// <summary>Acceptance: RestoreCycle moves TotalCycles backward to the captured value.</summary>
    [Fact]
    public void SystemClock_RestoreCycle_RewindsTotalCycles()
    {
        var clock = new SystemClock(985248);
        clock.Step(100);
        Assert.Equal(100, clock.TotalCycles);

        clock.RestoreCycle(40);

        Assert.Equal(40, clock.TotalCycles);
        clock.Step();
        Assert.Equal(41, clock.TotalCycles);
    }

    /// <summary>Acceptance: a negative cycle is rejected.</summary>
    [Fact]
    public void SystemClock_RestoreCycle_Negative_Throws()
    {
        var clock = new SystemClock(985248);
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.RestoreCycle(-1));
    }

    /// <summary>
    /// Acceptance: after a restart-required update the replacement session carries the factory's
    /// audio tap, and a recorder attached to the outgoing session's tap is detached (captures finalized).
    /// </summary>
    [Fact]
    public async Task UpdateSettings_Restart_CarriesAudioTap_AndEndsOutgoingCaptures()
    {
        var tap = new ViceSharp.Core.Media.CaptureAudioTap();
        var factory = new DefaultEmulatorRuntimeFactory(
            new ArchitectureBuilder(),
            [MinimalHostArchitectureDescriptor.Instance],
            MinimalHostArchitectureDescriptor.ArchitectureId);
        var original = factory.Create(new CreateEmulatorSessionRequest());
        original.PowerState = "On";
        original.AudioCaptureTap = tap;
        var recorder = new StubRecorder();
        tap.AttachRecorder(recorder);
        Assert.True(tap.IsRecording);

        var registry = new EmulatorRuntimeRegistry();
        registry.Add(original);
        var settings = new SettingsServiceHost(registry, new TapFactory(factory, tap));

        var response = await settings.UpdateSettingsAsync(
            new UpdateSettingsRequest(original.SessionId, Display: new DisplaySettingsDto(), RestartSession: true),
            TestContext.Current.CancellationToken);

        Assert.Equal(RpcStatusCode.Ok, response.Status.Code);
        Assert.True(registry.TryGet(original.SessionId, out var restarted));
        Assert.NotSame(original, restarted);
        Assert.Same(tap, restarted!.AudioCaptureTap);
        Assert.False(tap.IsRecording);
    }

    private sealed class TapFactory(IEmulatorRuntimeFactory inner, ViceSharp.Core.Media.CaptureAudioTap tap)
        : IEmulatorRuntimeFactory
    {
        public EmulatorRuntimeSession Create(CreateEmulatorSessionRequest request)
        {
            var session = inner.Create(request);
            session.AudioCaptureTap = tap;
            return session;
        }
    }

    private sealed class StubRecorder : IAudioRecorder
    {
        public int SampleRate => 44100;
        public int Channels => 1;
        public void WriteSamples(ReadOnlySpan<short> samples) { }
        public void Stop() { }
        public void Dispose() { }
    }
}
