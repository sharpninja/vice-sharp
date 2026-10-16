namespace ViceSharp.TestHarness.Audio;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViceSharp.Abstractions;
using ViceSharp.Host.Audio;
using Xunit;

/// <summary>
/// FR-SID-013 / TR-PLAT-001 / TEST-SID-001 / TR-QA-TESTSILENCE-001:
/// Linux selection and PCM/backpressure/lifecycle behavior without opening any native audio device.
/// </summary>
public sealed class LinuxSdlAudioBackendTests
{
    [Fact]
    public void EnabledLinuxFactory_SelectsLinuxDevice_WithoutOpeningWindowsDevice()
    {
        var device = new FakeDevice();
        using var expected = LinuxSdlAudioBackend.TryCreate(() => device);
        int linuxCalls = 0;
        var actual = AudioBackendFactory.SelectBackend(true, false, true,
            () => throw new InvalidOperationException("Windows factory must not run on Linux."),
            () => { linuxCalls++; return expected; });
        Assert.Equal(1, linuxCalls);
        Assert.NotNull(actual);
        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    public void DisabledOrUnsupportedFactory_NeverOpensADevice(bool enabled, bool windows, bool linux)
    {
        IAudioBackend? Unexpected() => throw new InvalidOperationException("No device may be opened.");
        Assert.Null(AudioBackendFactory.SelectBackend(enabled, windows, linux, Unexpected, Unexpected));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitializationFailure_ReturnsNullAndReleasesPartialDevice(bool throws)
    {
        var device = new FakeDevice { OpenSucceeds = false, ThrowOnOpen = throws };
        Assert.Null(LinuxSdlAudioBackend.TryCreate(() => device));
        Assert.Equal(1, device.DisposeCount);
    }

    [Fact]
    public void Submit_Uses44100MonoPcm16_AndAppliesMasterGainExactlyOnce()
    {
        var device = new FakeDevice();
        using var backend = LinuxSdlAudioBackend.TryCreate(() => device);
        Assert.NotNull(backend);
        Assert.Equal((44100, 1, 256), device.OpenFormat);
        float oldVolume = MasterAudioControl.Volume;
        bool oldMute = MasterAudioControl.Muted;
        try
        {
            MasterAudioControl.Volume = 0.5f;
            MasterAudioControl.Muted = false;
            backend.SubmitSamples([1f, -1f, 0.25f]);
            Assert.Equal(new byte[] { 0xFF, 0x3F, 0x01, 0xC0, 0xFF, 0x0F }, device.Writes.Single());
            Assert.False(device.Paused);
            MasterAudioControl.Muted = true;
            backend.SubmitSamples([1f, -1f]);
            Assert.All(device.Writes.Last(), value => Assert.Equal(0, value));
        }
        finally
        {
            MasterAudioControl.Volume = oldVolume;
            MasterAudioControl.Muted = oldMute;
        }
    }

    [Fact]
    public void QueueDepthAndCapacity_FollowTheDeviceDrain()
    {
        var device = new FakeDevice();
        using var backend = LinuxSdlAudioBackend.TryCreate(() => device);
        Assert.NotNull(backend);
        backend.SubmitSamples(new float[256]);
        Assert.Equal(256, backend.QueuedSampleCount);
        Assert.Equal(LinuxSdlAudioBackend.QueueCapacitySamples - 256, backend.AvailableSampleCount);
        device.BufferedBytes = 128;
        Assert.Equal(64, backend.QueuedSampleCount);
        Assert.Equal(LinuxSdlAudioBackend.QueueCapacitySamples - 64, backend.AvailableSampleCount);
    }

    [Fact]
    public async Task FullQueue_WaitsWithoutDroppingThenQueuesTheOriginalFragment()
    {
        var device = new FakeDevice { BufferedBytes = LinuxSdlAudioBackend.QueueCapacitySamples * 2 };
        using var backend = LinuxSdlAudioBackend.TryCreate(() => device);
        Assert.NotNull(backend);
        var submission = Task.Run(() => backend.SubmitSamples(new float[256]), TestContext.Current.CancellationToken);
        try
        {
            Assert.True(device.QueueRead.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
            Assert.False(submission.IsCompleted);
            Assert.Empty(device.Writes);
            device.BufferedBytes = 0;
            await submission.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            Assert.Single(device.Writes);
            Assert.Equal(512, device.Writes[0].Length);
        }
        finally { backend.Dispose(); }
    }

    [Theory]
    [InlineData("pause")]
    [InlineData("stop")]
    [InlineData("dispose")]
    public async Task LifecycleChange_ReleasesFullQueueWaitWithoutSubmittingStaleSamples(string action)
    {
        var device = new FakeDevice { BufferedBytes = LinuxSdlAudioBackend.QueueCapacitySamples * 2 };
        using var backend = LinuxSdlAudioBackend.TryCreate(() => device);
        Assert.NotNull(backend);
        var submission = Task.Run(() => backend.SubmitSamples(new float[256]), TestContext.Current.CancellationToken);
        try
        {
            Assert.True(device.QueueRead.Wait(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
            if (action == "pause") { backend.Pause(); backend.Resume(); }
            else if (action == "stop") backend.Stop();
            else backend.Dispose();
            await submission.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            Assert.Empty(device.Writes);
            if (action != "dispose")
            {
                device.BufferedBytes = 0;
                backend.SubmitSamples([0.1f]);
                Assert.Single(device.Writes);
            }
        }
        finally { backend.Dispose(); }
    }

    [Fact]
    public void PausePreservesQueue_StopClearsIt_AndDisposeClosesOnce()
    {
        var device = new FakeDevice();
        var backend = LinuxSdlAudioBackend.TryCreate(() => device);
        Assert.NotNull(backend);
        using (backend)
        {
            backend.SubmitSamples(new float[64]);
            backend.Pause();
            Assert.True(device.Paused);
            Assert.Equal(64, backend.QueuedSampleCount);
            backend.SubmitSamples([0.5f]);
            Assert.Single(device.Writes);
            backend.Resume();
            Assert.False(device.Paused);
            backend.Stop();
            Assert.Equal(0, backend.QueuedSampleCount);
            Assert.True(device.Paused);
            backend.SubmitSamples([0.5f]);
            Assert.False(device.Paused);
        }
        backend.Dispose();
        Assert.Equal(1, device.DisposeCount);
        Assert.Equal(0, backend.QueuedSampleCount);
        Assert.Equal(int.MaxValue, backend.AvailableSampleCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeviceWriteFailure_DisablesOutputAndReleasesDeviceWithoutThrowing(bool throws)
    {
        var device = new FakeDevice { QueueSucceeds = false, ThrowOnQueue = throws };
        using var backend = LinuxSdlAudioBackend.TryCreate(() => device);
        Assert.NotNull(backend);
        backend.SubmitSamples([1f]);
        backend.SubmitSamples([1f]);
        backend.Pause(); backend.Resume(); backend.Stop();
        Assert.False(backend.IsAvailable);
        Assert.Equal(1, device.DisposeCount);
        Assert.Equal(0, backend.QueuedSampleCount);
        Assert.Equal(int.MaxValue, backend.AvailableSampleCount);
    }

    [Fact]
    public async Task StalledDevice_BoundsTheWaitAndDisablesTheAudioTimingSource()
    {
        var device = new FakeDevice { BufferedBytes = LinuxSdlAudioBackend.QueueCapacitySamples * 2 };
        using var backend = LinuxSdlAudioBackend.TryCreate(() => device, TimeSpan.FromMilliseconds(10));
        Assert.NotNull(backend);
        Assert.True(backend.IsAvailable);
        await Task.Run(() => backend.SubmitSamples(new float[256]), TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Assert.False(backend.IsAvailable);
        Assert.Empty(device.Writes);
        Assert.Equal(1, device.DisposeCount);
    }

    [Fact]
    public void StoppedDeviceQuery_DisablesTimingBeforeQueueingMoreSamples()
    {
        var device = new FakeDevice();
        using var backend = LinuxSdlAudioBackend.TryCreate(() => device);
        Assert.NotNull(backend);
        device.DeviceStopped = true;
        backend.SubmitSamples([0.5f]);
        Assert.False(backend.IsAvailable);
        Assert.Empty(device.Writes);
        Assert.Equal(1, device.DisposeCount);
    }

    private sealed class FakeDevice : ILinuxAudioDevice
    {
        internal volatile int BufferedBytes;
        internal bool OpenSucceeds = true;
        internal bool ThrowOnOpen;
        internal bool QueueSucceeds = true;
        internal bool ThrowOnQueue;
        internal bool DeviceStopped;
        internal bool Paused = true;
        internal int DisposeCount;
        internal (int, int, int) OpenFormat;
        internal List<byte[]> Writes { get; } = [];
        internal ManualResetEventSlim QueueRead { get; } = new();
        public bool Open(int sampleRate, int channels, int fragmentSamples)
        {
            OpenFormat = (sampleRate, channels, fragmentSamples);
            if (ThrowOnOpen) throw new InvalidOperationException("synthetic no-device failure");
            return OpenSucceeds;
        }
        public int QueuedBytes
        {
            get
            {
                QueueRead.Set();
                if (DeviceStopped) throw new InvalidOperationException("synthetic stopped device");
                return BufferedBytes;
            }
        }
        public bool Queue(ReadOnlySpan<byte> pcm)
        {
            if (ThrowOnQueue) throw new InvalidOperationException("synthetic write failure");
            if (!QueueSucceeds) return false;
            Writes.Add(pcm.ToArray());
            BufferedBytes += pcm.Length;
            return true;
        }
        public void SetPaused(bool paused) => Paused = paused;
        public void Clear() => BufferedBytes = 0;
        public void Dispose() => DisposeCount++;
    }
}
