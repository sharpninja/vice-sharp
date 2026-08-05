namespace ViceSharp.TestHarness.Xbox;

using System;
using ViceSharp.TestHarness.Xbox.Fakes;
using ViceSharp.Xbox.ViewModels;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP S23 (IMPL-XBOXUWP-023), area XVIDEO. Exercises the pure video
/// frame-pull adapter (<see cref="VideoFramePullViewModel"/>) that the UWP video
/// surface drives ~50 Hz. The adapter is a PURE SINK: it copies the emulation
/// thread's latest committed frame through <see cref="ILocalVideoFramePull"/> into a
/// reused, geometry-sized buffer without advancing or mutating the core, with no
/// per-tick allocation and no lock (FR-XVIDEO-002, TR-MVVM-001).
/// </summary>
[Trait("Category", "Xbox")]
public sealed class VideoFramePullViewModelTests
{
    private const int C64Width = 384;
    private const int C64Height = 272;

    /// <summary>The C64 BGRA8888 frame byte length: 384 * 272 * 4 = 417792.</summary>
    private const int C64BufferLength = C64Width * C64Height * 4;

    /// <summary>
    /// FR-XVIDEO-002, TR-MVVM-001. TEST-XBOXUI-005a.
    /// Use case: the UWP video surface drives the adapter at its render cadence; each
    /// surface tick must translate to exactly one lock-free pull, never fanning out to
    /// multiple copies or driving the core.
    /// Acceptance: calling <see cref="VideoFramePullViewModel.Tick"/> 10 times against a
    /// fake that has a frame published yields exactly 10 successful copies and exactly
    /// 10 <see cref="ILocalVideoFramePull.TryCopyFrameInto"/> calls on the fake (a 1:1
    /// Tick -> pull cadence), all bound to the adapter's session id.
    /// </summary>
    [Fact]
    public void Tick_PublishingEachTime_PullsOncePerTick()
    {
        var fake = new FakeVideoFramePull();
        fake.PublishFrame(C64Width, C64Height, cycle: 0);
        var pump = new VideoFramePullViewModel(fake, "xbox-session");

        var copied = 0;
        for (var i = 0; i < 10; i++)
        {
            if (pump.Tick())
                copied++;
        }

        Assert.Equal(10, copied);
        Assert.Equal(10, fake.PullCount);
        Assert.Equal("xbox-session", fake.LastRequestedSessionId);
    }

    /// <summary>
    /// FR-XVIDEO-002, TR-MVVM-001. TEST-XVIDEO-001.
    /// Use case: the video surface uploads the exact bytes the emulation worker
    /// committed; a byte-for-byte mismatch would tear or corrupt the picture.
    /// Acceptance: before the first published frame <see cref="VideoFramePullViewModel.Tick"/>
    /// returns <c>false</c> and no frame is available; once the fake publishes a known
    /// 384x272 BGRA pattern, one Tick copies it byte-exact, the exposed
    /// <see cref="VideoFramePullViewModel.CurrentFrame"/> equals the source bytes
    /// exactly, its length equals <see cref="FrameGeometry.BufferLength"/> (417792), and
    /// Width/Height/Cycle reflect the pulled frame.
    /// </summary>
    [Fact]
    public void Tick_CopiesKnownPatternByteExactIntoGeometrySizedBuffer()
    {
        var source = new byte[C64BufferLength];
        for (var i = 0; i < source.Length; i++)
            source[i] = unchecked((byte)((i * 31) + 7));

        var fake = new FakeVideoFramePull { SourceFrame = source };
        var pump = new VideoFramePullViewModel(fake, "xbox-session");

        // Before the first committed frame: Tick returns false, nothing to upload.
        Assert.False(pump.Tick());
        Assert.False(pump.HasFrame);

        fake.PublishFrame(C64Width, C64Height, cycle: 123456);

        Assert.True(pump.Tick());
        Assert.True(pump.HasFrame);
        Assert.Equal(C64Width, pump.Width);
        Assert.Equal(C64Height, pump.Height);
        Assert.Equal(123456, pump.Cycle);

        var current = pump.CurrentFrame;
        Assert.Equal(C64BufferLength, current.Length);
        Assert.True(current.SequenceEqual(source));
    }

    /// <summary>
    /// FR-XVIDEO-002, TR-MVVM-001. TEST-XVIDEO-002.
    /// Use case: the ~50 Hz render pull runs on the zero-allocation hot path of the UI;
    /// a per-tick allocation would churn the GC under the video surface and jitter the
    /// frame cadence.
    /// Acceptance: after a warm-up Tick allocates the reused buffer ONCE, a steady-state
    /// loop of 1000 <see cref="VideoFramePullViewModel.Tick"/> calls allocates zero
    /// bytes (measured on the current thread), and every tick copies a frame.
    /// </summary>
    [Fact]
    public void Tick_SteadyState_AllocatesZeroBytesPerTick()
    {
        var fake = new FakeVideoFramePull();
        fake.PublishFrame(C64Width, C64Height, cycle: 1);
        var pump = new VideoFramePullViewModel(fake, "xbox-session");

        // Warm-up: allocate the reused buffer once and fully JIT the pull path so the
        // measured loop reflects steady state only.
        for (var i = 0; i < 16; i++)
            _ = pump.Tick();

        var copied = 0;
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            if (pump.Tick())
                copied++;
        }
        var delta = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(1000, copied);
        Assert.Equal(0, delta);
    }
}
