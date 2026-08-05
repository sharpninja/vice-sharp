namespace ViceSharp.TestHarness.Xbox;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using ViceSharp.Abstractions;
using ViceSharp.Host.Audio;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S18 (IMPL-XBOXUWP-018), TEST-XAUDIO-004.
/// FR-XAUDIO-003 / TR-XAUDIO-002: the AppContainer-safe console audio backend
/// <see cref="XAudio2SourceVoiceBackend"/> is a DECOUPLED, NON-blocking ring sink so
/// the deterministic emulation worker is never parked on the audio device. Unlike the
/// desktop <c>WinMmAudioBackend</c> (which applies device back-pressure and can block
/// the worker in <c>WaitAndSubmitBuffer</c>), this backend writes each SID fragment
/// into the <see cref="XAudio2AudioMath"/> ring and returns immediately, dropping the
/// oldest fragment when the ring is full. It degrades to a silent no-op when the device
/// cannot be opened (CI / headless / non-Windows), so the suite never crashes and never
/// opens a real audio device.
///
/// <para>The native XAudio2 calls sit behind the <c>ISourceVoiceDevice</c> seam so the
/// ring / submit / pause / degrade LOGIC is exercised off-console with a fake device -
/// the real <c>[LibraryImport("xaudio2_9.dll")]</c> device impl still compiles (so the
/// AOT analyzer / publish check sees the interop) but is never opened here. This matters
/// because this dev PC may actually HAVE a working audio device, so the silent-degrade
/// path cannot rely on a real device-open failing.</para>
///
/// Convention: plain xUnit <c>[Fact]</c> off-console (no <c>[ViceFact]</c>, no
/// <c>Assert.Skip</c>), Category=Xbox.
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XAudio2SourceVoiceBackendTests
{
    private const int FragmentSamples = XAudio2AudioMath.FragmentSampleCount;
    private const int RingFragments = XAudio2AudioMath.BufferFragmentCount;
    private const int RingCapacitySamples = FragmentSamples * RingFragments;

    /// <summary>
    /// FR: FR-XAUDIO-003, TR: TR-XAUDIO-002, TEST: TEST-XAUDIO-004.
    /// Use case: on CI / headless / non-Windows the console audio device cannot be
    /// opened, and the emulator must keep running silently rather than crash. Because
    /// this dev PC may HAVE a real device, the degrade path is proven with a fake device
    /// whose Open() deterministically FAILS.
    /// Acceptance: constructing the backend over a device whose Open() returns false
    /// yields a silent no-op - SubmitSamples (1000x), Pause, Resume and Stop never throw;
    /// QueuedSampleCount stays 0; the failed device is never asked to Start or accept a
    /// buffer (SubmitBuffer/Start never called); Open was attempted exactly once; and
    /// Dispose is safe and idempotent.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void OpenFails_BackendIsSilentNoOp_AndNeverTouchesDevice()
    {
        var fake = new FakeSourceVoiceDevice(openSucceeds: false);
        var backend = new XAudio2SourceVoiceBackend(() => fake);

        Assert.Equal(0, backend.QueuedSampleCount);

        var samples = new float[FragmentSamples];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = 0.25f;

        var error = Record.Exception(() =>
        {
            for (var i = 0; i < 1000; i++)
                backend.SubmitSamples(samples);
            backend.Pause();
            backend.Resume();
            backend.Stop();
        });

        Assert.Null(error);
        Assert.Equal(0, backend.QueuedSampleCount);
        Assert.Equal(1, fake.OpenCalls);        // Open attempted exactly once.
        Assert.Equal(0, fake.SubmitCalls);      // No device interaction after a failed open.
        Assert.Equal(0, fake.StartCalls);

        backend.Dispose();
        backend.Dispose();                      // Idempotent: second dispose is a no-op.
    }

    /// <summary>
    /// FR: FR-XAUDIO-003, TR: TR-XAUDIO-002, TEST: TEST-XAUDIO-004.
    /// Use case: the emulation worker submits SID fragments far faster than the device can
    /// drain them (or a single batch exceeds the ring). The backend must NEVER block the
    /// worker AND must never let the device hold more outstanding buffers than its native
    /// ring has slots - submitting past that would overwrite a slot XAudio2 is still
    /// playing, which is exactly the progressive-crackle defect. So the producer makes
    /// progress by DROPPING the excess fragment (drop-newest), not by growing the device
    /// queue unbounded.
    /// Acceptance: over an opened device, submitting many times the ring capacity of
    /// fragments completes near-instantly (non-blocking); QueuedSampleCount is in
    /// [0, ring-capacity] after every single submit and saturates at the cap; and the
    /// device's outstanding queue high-water NEVER exceeds the native ring capacity
    /// (BuffersQueued bounded), so no queued slot is ever overwritten.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void OpenDevice_SubmitBeyondCapacity_IsNonBlocking_BoundsDeviceQueue_QueuedNeverExceedsCapacity()
    {
        var fake = new FakeSourceVoiceDevice(openSucceeds: true);
        using var backend = new XAudio2SourceVoiceBackend(() => fake);

        Assert.Equal(0, backend.QueuedSampleCount);

        var oneFragment = new float[FragmentSamples];
        var submissions = RingFragments * 50;   // far past ring capacity, device never draining.

        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < submissions; i++)
        {
            backend.SubmitSamples(oneFragment);
            Assert.InRange(backend.QueuedSampleCount, 0, RingCapacitySamples);
        }

        stopwatch.Stop();

        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"SubmitSamples blocked the producer: {submissions} over-capacity submits took {stopwatch.Elapsed}.");

        // The device queue is BOUNDED to the native ring: excess fragments are dropped, not
        // piled onto XAudio2 where they would overwrite still-queued native slots.
        Assert.True(
            fake.MaxBuffersQueued <= RingFragments,
            $"device queue grew to {fake.MaxBuffersQueued}, past the {RingFragments}-slot native ring (slot overwrite).");
        Assert.Equal(RingCapacitySamples, backend.QueuedSampleCount); // Saturated ring == exactly capacity.
    }

    /// <summary>
    /// FR: FR-XAUDIO-003, TR: TR-XAUDIO-002, TEST: TEST-XAUDIO-004.
    /// Use case: this is the fix for the "music gets progressively worse / crackles more the
    /// longer it plays" defect. The device owns only <c>BufferFragmentCount</c> native ring
    /// slots and reuses them round-robin, so the backend must never let the number of
    /// outstanding (submitted-but-unplayed) buffers exceed that count - otherwise a slot
    /// still queued for playback is overwritten by a newer fragment, and the corruption
    /// escalates as the queue grows.
    /// Acceptance: submitting a hundred times the ring capacity into a device that is NOT
    /// draining never drives the device's outstanding queue past the native ring capacity.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void OpenDevice_DeviceQueue_NeverExceedsNativeRing_PreventsSlotOverwrite()
    {
        var fake = new FakeSourceVoiceDevice(openSucceeds: true);
        using var backend = new XAudio2SourceVoiceBackend(() => fake);

        var oneFragment = new float[FragmentSamples];
        for (var i = 0; i < RingFragments * 100; i++)
            backend.SubmitSamples(oneFragment);

        Assert.True(
            fake.MaxBuffersQueued <= RingFragments,
            $"device outstanding queue reached {fake.MaxBuffersQueued}, exceeding the {RingFragments} native slots: " +
            "XAudio2 would be playing a native slot this backend has already overwritten (progressive crackle).");
    }

    /// <summary>
    /// FR: FR-XAUDIO-003, TR: TR-XAUDIO-002, TEST: TEST-XAUDIO-004.
    /// Use case: bounding the device queue must not permanently wedge output - once the
    /// device plays (drains) the queued buffers, the backend must resume submitting so the
    /// music keeps flowing at steady state.
    /// Acceptance: after the device queue saturates (only the ring's worth of buffers
    /// reached the device), simulating the device playing everything lets subsequent
    /// submits reach the device again, still without exceeding the native ring.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void OpenDevice_ResumesSubmitting_WhenDeviceDrains()
    {
        var fake = new FakeSourceVoiceDevice(openSucceeds: true);
        using var backend = new XAudio2SourceVoiceBackend(() => fake);

        var oneFragment = new float[FragmentSamples];
        for (var i = 0; i < RingFragments * 4; i++)   // saturate; device not draining.
            backend.SubmitSamples(oneFragment);

        var submittedWhenFull = fake.SubmitCalls;
        Assert.Equal(RingFragments, submittedWhenFull);   // only the ring's worth reached the device.

        fake.Play(RingFragments);                         // the device plays everything queued.
        for (var i = 0; i < RingFragments; i++)
            backend.SubmitSamples(oneFragment);

        Assert.True(
            fake.SubmitCalls > submittedWhenFull,
            "submits did not resume after the device drained: output would wedge to silence.");
        Assert.True(
            fake.MaxBuffersQueued <= RingFragments,
            $"device queue grew to {fake.MaxBuffersQueued}, past the {RingFragments}-slot native ring.");
    }

    /// <summary>
    /// FR: FR-XAUDIO-003, TR: TR-XAUDIO-002, TEST: TEST-XAUDIO-004.
    /// Use case: on Suspending the console app calls Pause(); the emulation worker may be
    /// concurrently hammering SubmitSamples. Because submit is non-blocking by design
    /// there is no path where the worker parks in submit, so Pause() can never fail to
    /// unpark it and can never deadlock against a submitting worker.
    /// Acceptance: with a worker thread submitting continuously past ring capacity,
    /// Pause() returns promptly (well under a second), the worker keeps returning from
    /// SubmitSamples and completes within a generous timeout (i.e. it never parked or
    /// deadlocked), and Pause()/Resume()/Stop()/Dispose() are all safe and idempotent.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void OpenDevice_PauseUnparksWorker_SubmitNeverBlocks_AndControlIsIdempotent()
    {
        var fake = new FakeSourceVoiceDevice(openSucceeds: true);
        var backend = new XAudio2SourceVoiceBackend(() => fake);

        var oneFragment = new float[FragmentSamples];
        for (var i = 0; i < oneFragment.Length; i++)
            oneFragment[i] = 0.1f;

        var stop = false;
        var iterations = 0;
        Exception? workerError = null;

        // A raw background thread (not a Task) stands in for the emulation worker so the
        // liveness assertion below is a plain, non-blocking Thread.Join with a timeout.
        var worker = new Thread(() =>
        {
            try
            {
                while (!Volatile.Read(ref stop))
                {
                    backend.SubmitSamples(oneFragment);
                    Interlocked.Increment(ref iterations);

                    // The non-blocking contract holds on every iteration, paused or not.
                    var queued = backend.QueuedSampleCount;
                    if (queued < 0 || queued > RingCapacitySamples)
                        throw new InvalidOperationException($"queued out of range: {queued}");
                }
            }
            catch (Exception ex)
            {
                workerError = ex;
            }
        })
        {
            IsBackground = true,
            Name = "xaudio2-submit-worker",
        };
        worker.Start();

        // Let the worker saturate the ring, then Pause from this thread. Pause must
        // acquire and return promptly - it can never be parked behind a blocked submit.
        SpinUntil(() => Volatile.Read(ref iterations) > RingFragments * 4, TimeSpan.FromSeconds(2));

        var pauseStopwatch = Stopwatch.StartNew();
        backend.Pause();
        pauseStopwatch.Stop();

        // Idempotent control surface, exercised while the worker is still running.
        backend.Pause();
        backend.Resume();
        backend.Resume();
        backend.Stop();

        // Worker keeps making progress after the pause storm; then stop it.
        var iterationsAtPause = Volatile.Read(ref iterations);
        SpinUntil(() => Volatile.Read(ref iterations) > iterationsAtPause, TimeSpan.FromSeconds(2));
        Volatile.Write(ref stop, true);

        Assert.True(
            worker.Join(TimeSpan.FromSeconds(5)),
            "worker parked in SubmitSamples: the non-blocking / Pause-unparks contract was violated.");
        Assert.Null(workerError);
        Assert.True(iterations > 0, "worker never ran a submit.");
        Assert.True(
            pauseStopwatch.Elapsed < TimeSpan.FromSeconds(1),
            $"Pause() did not return promptly: {pauseStopwatch.Elapsed}.");

        backend.Dispose();
        backend.Dispose();      // Idempotent.
    }

    /// <summary>
    /// FR: FR-XAUDIO-003, TR: TR-XAUDIO-002, TEST: TEST-XAUDIO-004.
    /// Use case: the AppContainer forbids winmm (<c>waveOut*</c>) and the kernel32
    /// waitable-timer / affinity P/Invoke on the console path; console audio must be
    /// XAudio2 (<c>xaudio2_9.dll</c>) only. This guards the invariant with reflection so
    /// a regression that reintroduces a banned native import is caught by dotnet test.
    /// Acceptance: the union of every <c>[LibraryImport]</c> library name declared by
    /// <see cref="XAudio2SourceVoiceBackend"/> and its real device impl
    /// (<c>XAudio2SourceVoiceDevice</c>) is exactly {"xaudio2_9.dll"} - non-empty (the
    /// interop really exists), every entry is xaudio2_9.dll, and nothing references
    /// winmm.dll or kernel32.dll. The backend itself declares no P/Invoke (all interop is
    /// delegated to the device seam).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void ConsoleBackend_LibraryImportSet_IsExactlyXAudio2_NoWinmmNoKernel32()
    {
        var backendImports = LibraryImportNames(typeof(XAudio2SourceVoiceBackend)).ToArray();
        var deviceImports = LibraryImportNames(typeof(XAudio2SourceVoiceDevice)).ToArray();
        var union = backendImports.Concat(deviceImports).ToArray();

        // Non-vacuity: the real interop is present (the device links xaudio2_9.dll).
        Assert.NotEmpty(union);

        // Exactly xaudio2_9.dll everywhere - no winmm / kernel32 anywhere.
        Assert.All(union, name => Assert.Equal("xaudio2_9.dll", name, ignoreCase: true));
        Assert.DoesNotContain(union, name => name.Contains("winmm", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(union, name => name.Contains("kernel32", StringComparison.OrdinalIgnoreCase));

        // The backend delegates all native work to the device seam and declares no P/Invoke itself.
        Assert.Empty(backendImports);
        Assert.NotEmpty(deviceImports);
    }

    private static IEnumerable<string> LibraryImportNames(Type type)
    {
        return type
            .GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.GetCustomAttribute<LibraryImportAttribute>()?.LibraryName)
            .Where(name => name is not null)
            .Select(name => name!);
    }

    private static void SpinUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = Stopwatch.StartNew();
        while (!condition() && deadline.Elapsed < timeout)
            Thread.Yield();
    }

    /// <summary>
    /// A fake <see cref="ISourceVoiceDevice"/> that records the backend's device
    /// interaction so the ring / submit / pause / degrade LOGIC is asserted off-console
    /// without opening a real XAudio2 device. Its Open() outcome is deterministic.
    ///
    /// <para>It models a REAL source voice: <see cref="SubmitBuffer"/> adds one buffer to
    /// the outstanding (submitted-but-not-yet-played) queue, <see cref="BuffersQueued"/>
    /// reports that live outstanding count, <see cref="Play"/> simulates the device
    /// playing (draining) buffers, and <see cref="Stop"/> flushes the queue to empty
    /// (mirroring the real device's FlushSourceBuffers). This is what lets the tests prove
    /// the backend BOUNDS the device queue to the native ring capacity - the real device
    /// owns only <c>bufferFragmentCount</c> native slots, so any outstanding count above
    /// that would mean XAudio2 is still playing a slot the backend has overwritten.</para>
    ///
    /// <see cref="MaxBuffersQueued"/> is the high-water outstanding count observed, so a
    /// test can assert the queue never grew past the native ring. Touched under the
    /// backend's internal lock on the submit/stop paths; <see cref="Play"/> is only called
    /// from a single-threaded test between submit bursts.
    /// </summary>
    private sealed class FakeSourceVoiceDevice : ISourceVoiceDevice
    {
        private readonly bool _openSucceeds;
        private int _outstanding;

        public FakeSourceVoiceDevice(bool openSucceeds) => _openSucceeds = openSucceeds;

        public int OpenCalls { get; private set; }

        public int SubmitCalls { get; private set; }

        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public int DisposeCalls { get; private set; }

        /// <summary>The highest outstanding (submitted-but-unplayed) buffer count ever observed.</summary>
        public int MaxBuffersQueued { get; private set; }

        public bool Open(int sampleRate, int channels, int fragmentBytes, int bufferFragmentCount)
        {
            OpenCalls++;
            return _openSucceeds;
        }

        public void SubmitBuffer(ReadOnlySpan<byte> pcm)
        {
            SubmitCalls++;
            _outstanding++;
            if (_outstanding > MaxBuffersQueued)
                MaxBuffersQueued = _outstanding;
        }

        // The live outstanding queue, exactly as the real device's XAudio2 GetState reports it.
        public int BuffersQueued => _outstanding;

        /// <summary>Simulates the device playing (draining) <paramref name="count"/> buffers.</summary>
        public void Play(int count) => _outstanding = Math.Max(0, _outstanding - count);

        public void Start() => StartCalls++;

        // The real device's Stop() calls FlushSourceBuffers, emptying the queued buffers.
        public void Stop()
        {
            StopCalls++;
            _outstanding = 0;
        }

        public void Dispose() => DisposeCalls++;
    }
}
