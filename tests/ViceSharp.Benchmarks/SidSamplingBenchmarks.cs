using System;
using BenchmarkDotNet.Attributes;
using ViceSharp.Abstractions;
using ViceSharp.Chips.Audio;
using ViceSharp.Core;

namespace ViceSharp.Benchmarks;

/// <summary>
/// Throughput + allocation benchmark for the SID sampling paths, quantifying
/// the cost of the Resample-always live-audio policy (VICE x64sc parity).
/// Compares: bare Tick() (no audio), Tick() with the live SAMPLE_RESAMPLE tail
/// (the production live-audio hot path), and the buffered ClockBuffered pull
/// at SAMPLE_RESAMPLE. Evidence for TR-SID-RESAMPLE-001.
/// </summary>
[MemoryDiagnoser]
public class SidSamplingBenchmarks
{
    private const double PalMasterClockHz = 985248.0;

    private sealed class NullBackend : IAudioBackend
    {
        public void SubmitSamples(ReadOnlySpan<float> samples) { }
        public int QueuedSampleCount => 0;
        public int AvailableSampleCount => int.MaxValue;
        public void Pause() { }
        public void Resume() { }
        public void Stop() { }
    }

    private Sid6581 _bare = null!;
    private Sid6581 _live = null!;
    private Sid6581 _buffered = null!;
    private short[] _buf = null!;

    [Params(BenchmarkMachineFactory.DefaultCycleBudget)]
    public int Cycles { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bare = MakeSid(null);
        _live = MakeSid(new NullBackend());
        _live.ConfigureAudioClock(PalMasterClockHz);
        _buffered = MakeSid(null);
        _buffered.SetSamplingParameters(PalMasterClockHz, SidSamplingMethod.Resample, 44100.0);
        _buf = new short[512];
    }

    private static Sid6581 MakeSid(IAudioBackend? backend)
    {
        var bus = new BasicBus();
        var sid = backend is null
            ? new Sid6581(bus) { BaseAddress = 0xD400 }
            : new Sid6581(bus, backend) { BaseAddress = 0xD400 };
        bus.RegisterDevice(sid);
        sid.Reset();
        // Voice 1: sawtooth + gate, full sustain, filtered, max volume.
        sid.Write(0xD400, 0x00);
        sid.Write(0xD401, 0x10);
        sid.Write(0xD405, 0x09);
        sid.Write(0xD406, 0xF0);
        sid.Write(0xD417, 0x01); // filter voice 1
        sid.Write(0xD418, 0x1F); // low-pass + volume 15
        sid.Write(0xD404, 0x21);
        return sid;
    }

    [Benchmark(Baseline = true, Description = "Tick() x N (no audio)")]
    public void TickBare()
    {
        var sid = _bare;
        for (var i = 0; i < Cycles; i++) sid.Tick();
    }

    [Benchmark(Description = "Tick() x N + live SAMPLE_RESAMPLE tail")]
    public void TickLiveResample()
    {
        var sid = _live;
        for (var i = 0; i < Cycles; i++) sid.Tick();
    }

    [Benchmark(Description = "ClockBuffered SAMPLE_RESAMPLE x N cycles")]
    public void ClockBufferedResample()
    {
        var sid = _buffered;
        int remaining = Cycles;
        while (remaining > 0)
        {
            int chunk = Math.Min(4096, remaining);
            int c = chunk;
            sid.ClockBuffered(ref c, _buf);
            remaining -= (chunk - c);
        }
    }
}
