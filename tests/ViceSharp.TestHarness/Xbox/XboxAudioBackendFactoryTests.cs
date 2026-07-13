namespace ViceSharp.TestHarness.Xbox;

using System;
using ViceSharp.Abstractions;
using ViceSharp.Core;
using ViceSharp.Core.Media;
using ViceSharp.Host.Audio;
using ViceSharp.Host.Runtime;
using ViceSharp.Protocol;
using Xunit;

/// <summary>
/// PLAN-XBOXUWP slice S17 (IMPL-XBOXUWP-017). <c>XboxAudioBackendFactory</c>
/// selects the console (UWP/Xbox) real-time audio backend. It is the console
/// counterpart to the desktop <c>AudioBackendFactory</c>: it shares the identical
/// <c>VICESHARP_AUDIO</c> env gate so the two heads can never diverge on when audio
/// engages, and - like the desktop path - wraps the real device backend in a
/// <see cref="CaptureAudioTap"/> so a runtime StartCapture(Audio) can attach a WAV
/// recorder without rebuilding the machine. It stays pure of XAudio2 interop: the
/// real source-voice device (XAudio2SourceVoiceBackend) arrives in S18 and is
/// supplied here as the <c>deviceBackendFactory</c>. On console the head passes that
/// creator; in tests / headless nothing is passed, so <c>CreateDefault</c> returns
/// null, the SID never touches the audio path, and pacing/determinism is
/// unperturbed.
///
/// Convention: plain xUnit <c>[Fact]</c> off-console (no <c>[ViceFact]</c>, no
/// <c>Assert.Skip</c>), Category=Xbox. The env var is saved/restored in a finally so
/// isolation holds (the harness disables test parallelization assembly-wide).
/// </summary>
[Trait("Category", "Xbox")]
public sealed class XboxAudioBackendFactoryTests
{
    private const double PalMasterClockHz = 985248.0;

    /// <summary>
    /// FR: FR-XAV-001, TR: TR-XAUDIO-001, TEST: TEST-XAUDIO-001.
    /// Use case: the Xbox head must stay SILENT and deterministic in headless / test
    /// contexts (no audio device opened) yet select a wrapped real backend on console.
    /// Acceptance: with <c>VICESHARP_AUDIO</c> unset, <c>CreateDefault()</c> returns
    /// null even when a device creator is supplied (the env gate wins and the creator
    /// is never invoked), and a SID built with that null backend is inert
    /// (<c>IsAudioTimingSource == false</c>); a headless minimal session engages no
    /// audio device (no live capture tap). With <c>VICESHARP_AUDIO=1</c> and no
    /// creator, <c>CreateDefault()</c> still returns null (it never fabricates a
    /// device); a creator returning null yields null; a creator returning a real
    /// device yields a non-null backend wrapped in a <see cref="CaptureAudioTap"/>
    /// that forwards submitted samples to that device (mirroring the desktop WinMm
    /// wrap).
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void CreateDefault_IsSilentHeadless_AndWrapsRealDeviceWhenEnabled()
    {
        var saved = Environment.GetEnvironmentVariable("VICESHARP_AUDIO");
        try
        {
            // ---- Phase A: disabled / headless (env unset) ----
            Environment.SetEnvironmentVariable("VICESHARP_AUDIO", null);

            var deviceCreated = false;
            Func<IAudioBackend?> creator = () =>
            {
                deviceCreated = true;
                return new RecordingAudioBackend();
            };

            // Disabled + no creator -> null.
            Assert.Null(XboxAudioBackendFactory.CreateDefault());

            // Disabled + creator supplied -> STILL null: the env gate wins so a
            // headless context that happens to wire a creator stays silent, and the
            // creator is never invoked (no device fabricated / opened).
            Assert.Null(XboxAudioBackendFactory.CreateDefault(creator));
            Assert.False(deviceCreated);

            // A SID built with that null backend is inert: it is not an audio timing
            // source, so it emits no samples and cannot perturb cycle-accurate pacing.
            var bus = new BasicBus();
            var sid = SidFactory.Create(bus, profile: null, XboxAudioBackendFactory.CreateDefault(creator), PalMasterClockHz);
            Assert.False(sid.IsAudioTimingSource);
            Assert.False(deviceCreated);

            // A minimal session built headless engages no audio device (no live tap).
            var factory = new DefaultEmulatorRuntimeFactory(
                new ArchitectureBuilder(),
                [MinimalHostArchitectureDescriptor.Instance],
                MinimalHostArchitectureDescriptor.ArchitectureId);
            var session = factory.Create(new CreateEmulatorSessionRequest(MinimalHostArchitectureDescriptor.ArchitectureId));
            Assert.Null(session.AudioCaptureTap);

            // ---- Phase B: enabled (env = 1) ----
            Environment.SetEnvironmentVariable("VICESHARP_AUDIO", "1");

            // Enabled but no creator -> still null (never fabricate a device; S18
            // supplies the XAudio2 creator).
            Assert.Null(XboxAudioBackendFactory.CreateDefault());

            // Enabled + creator returns null -> null (nothing to wrap).
            Assert.Null(XboxAudioBackendFactory.CreateDefault(() => null));

            // Enabled + creator returns a real device -> non-null, wrapped exactly as
            // the desktop WinMm path wraps its backend (CaptureAudioTap) and forwarding
            // submitted samples to that device.
            var fake = new RecordingAudioBackend();
            var wrapped = XboxAudioBackendFactory.CreateDefault(() => fake);
            Assert.NotNull(wrapped);
            Assert.IsType<CaptureAudioTap>(wrapped);

            var samples = new float[] { 0.1f, -0.2f, 0.3f };
            wrapped!.SubmitSamples(samples);
            Assert.Equal(samples.Length, fake.SubmittedSampleCount);
        }
        finally
        {
            Environment.SetEnvironmentVariable("VICESHARP_AUDIO", saved);
        }
    }

    /// <summary>
    /// FR: FR-XAUDIO-002, TR: TR-XAUDIO-003, TEST: TEST-XAUDIO-003.
    /// Use case: the console master volume + mute must be honored, with the master
    /// gain applied to the SID float samples EXACTLY ONCE (in AudioSampleConverter,
    /// before samples reach the ring) so the output is neither double-applied
    /// (squared) nor skipped.
    /// Acceptance: running a known float buffer through
    /// <see cref="AudioSampleConverter.ConvertToPcm16"/> with
    /// <c>MasterAudioControl.EffectiveGain</c> yields full-scale PCM at full gain
    /// (Volume 1, unmuted), ~half-scale at Volume 0.5 (a single application, well
    /// outside the ~quarter-scale band a squared/double application would land in),
    /// and silence when muted.
    /// </summary>
    [Fact]
    [Trait("Category", "Xbox")]
    public void EffectiveGain_IsAppliedExactlyOnce_ViaAudioSampleConverter()
    {
        try
        {
            var input = new[] { 1.0f };

            // Full gain (Volume 1, unmuted): EffectiveGain 1 -> unchanged full-scale.
            MasterAudioControl.Muted = false;
            MasterAudioControl.Volume = 1f;
            Assert.Equal(1f, MasterAudioControl.EffectiveGain, 3);
            var full = new byte[2];
            AudioSampleConverter.ConvertToPcm16(input, full, MasterAudioControl.EffectiveGain);
            Assert.Equal(32767, ReadSample(full));

            // Half gain (Volume 0.5): EffectiveGain 0.5 -> ~half-scale, applied ONCE.
            MasterAudioControl.Volume = 0.5f;
            Assert.Equal(0.5f, MasterAudioControl.EffectiveGain, 3);
            var half = new byte[2];
            AudioSampleConverter.ConvertToPcm16(input, half, MasterAudioControl.EffectiveGain);
            var halfValue = ReadSample(half);

            // Applied once: ~0.5 * 32767. A squared/double application would give
            // ~0.25 * 32767 (~8191); assert the output is in the single-application
            // band and explicitly NOT in the squared band.
            Assert.InRange(halfValue, 16000, 16600);
            Assert.NotInRange(halfValue, 0, 12000);

            // Mute: EffectiveGain 0 -> silence (the stored Volume is retained).
            MasterAudioControl.Muted = true;
            Assert.Equal(0f, MasterAudioControl.EffectiveGain);
            var muted = new byte[2];
            AudioSampleConverter.ConvertToPcm16(input, muted, MasterAudioControl.EffectiveGain);
            Assert.Equal(0, ReadSample(muted));
        }
        finally
        {
            MasterAudioControl.Muted = false;
            MasterAudioControl.Volume = 1f;
        }
    }

    private static short ReadSample(byte[] littleEndianPcm16)
        => (short)(littleEndianPcm16[0] | (littleEndianPcm16[1] << 8));

    /// <summary>
    /// A minimal recording <see cref="IAudioBackend"/> that counts submitted samples,
    /// standing in for S18's real XAudio2 source-voice device so the wrap/forwarding
    /// behavior can be asserted off-console without any audio device.
    /// </summary>
    private sealed class RecordingAudioBackend : IAudioBackend
    {
        public int SubmittedSampleCount { get; private set; }

        public void SubmitSamples(ReadOnlySpan<float> samples) => SubmittedSampleCount += samples.Length;

        public int QueuedSampleCount => 0;

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Stop()
        {
        }
    }
}
