namespace ViceSharp.TestHarness.Vic20;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.Vic20;
using ViceSharp.Chips.Vic;
using ViceSharp.Core;
using Xunit;

/// <summary>
/// FR-VIC20-SOUND-001: the VIC-I sound engine is part of the live host audio path.
/// </summary>
public sealed class Vic20AudioWiringTests
{
    private sealed class CollectingAudioBackend : IAudioBackend
    {
        public List<float> Samples { get; } = [];

        public int QueuedSampleCount => 0;

        public void SubmitSamples(ReadOnlySpan<float> samples)
        {
            foreach (var sample in samples)
                Samples.Add(sample);
        }

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

    [Fact]
    public void ArchitectureBuilder_WithAudioBackend_StreamsVicISamples()
    {
        var backend = new CollectingAudioBackend();
        var provider = MachineTestFactory.CreateVic20RomProvider();
        var machine = new ArchitectureBuilder(provider, backend)
            .Build(new Vic20Descriptor("vic20"));

        var vic = Assert.IsType<Mos6561>(
            machine.Devices.GetByRole(DeviceRole.VideoChip));
        var audioChip = Assert.IsAssignableFrom<IAudioChip>(vic);
        Assert.Same(vic, machine.Devices.GetByRole(DeviceRole.AudioChip));
        Assert.True(audioChip.IsAudioTimingSource);

        machine.Bus.Write(0x900E, 0x0F);
        machine.Bus.Write(0x900A, 0x80 | 64);

        const int measuredFrames = 20;
        for (var i = 0; i < measuredFrames; i++)
            machine.RunFrame();

        Assert.True(
            backend.Samples.Count > 15_000,
            $"Expected a live 44.1 kHz stream; received {backend.Samples.Count} samples.");
        Assert.True(
            backend.Samples.Distinct().Count() > 50,
            "A gated VIC-I channel must produce a varying waveform.");
    }
}
