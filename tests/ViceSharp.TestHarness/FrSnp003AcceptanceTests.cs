namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Architectures.C64;
using ViceSharp.Chips.Audio;
using ViceSharp.Chips.Cia;
using ViceSharp.Chips.Cpu;
using ViceSharp.Chips.Pla;
using ViceSharp.Chips.VicIi;
using ViceSharp.Core;
using ViceSharp.Core.Snapshots;
using Xunit;

/// <summary>
/// FR-SNP-003 acceptance tests for deterministic input replay. Iteration 2.
/// Snapshot capture and restore exist. Recording, playback, and seek do not.
/// </summary>
public sealed class FrSnp003AcceptanceTests
{
    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording stores external input with frame and cycle times.
    /// Acceptance: The snapshot store can record a key at a frame and cycle.
    /// </summary>
    [Fact]
    public void Recording_CapturesInputWithFrameAndCycle()
    {
        var store = new RuntimeSnapshotStore();
        store.RecordKey(4, 12, "B", true);

        var text = store.SaveReplay();

        Assert.Contains("frame=4", text, StringComparison.Ordinal);
        Assert.Contains("cycle=12", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Playback loads a base snapshot and replays recorded input.
    /// Acceptance: The snapshot store exposes playback that takes the machine
    /// and the recorded events.
    /// </summary>
    [Fact]
    public void Playback_RestoresTheSnapshotAndReplaysInput()
    {
        var store = new RuntimeSnapshotStore();
        var recorded = MachineTestFactory.CreateC64Machine();
        store.RecordKey(0, 12, "B", true, recorded);
        var playback = MachineTestFactory.CreateC64Machine();

        store.Play(playback);

        Assert.True(playback.Clock.TotalCycles >= 12);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Two playbacks of the same recording match byte for byte.
    /// Acceptance: The store exposes a bit-exact playback comparison.
    /// </summary>
    [Fact]
    public void Playback_IsBitExact()
    {
        var store = new RuntimeSnapshotStore();
        var recorded = MachineTestFactory.CreateC64Machine();
        store.RecordKey(0, 30, "A", true, recorded);
        store.RecordKey(0, 50, "A", false, recorded);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        var reference = MachineTestFactory.CreateC64Machine();
        reference.Clock.Step(30);
        SetKey(reference, "A", true);
        reference.Clock.Step(20);
        SetKey(reference, "A", false);

        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.True(SameImage(store.Capture(played), store.Capture(reference)));
        Assert.True(store.PlaybackMatches(MachineTestFactory.CreateC64Machine()));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Replay keeps the RAM image that was hidden under ROM.
    /// Acceptance: A byte stored at $A000 survives playback, and banking
    /// BASIC out still reads that byte. VICE snapshots store mem_ram.
    /// </summary>
    [Fact]
    public void Playback_PreservesRamHiddenUnderBasic()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        recorded.Bus.Write(0xA000, 0x42);
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 30, "B", true, recorded);

        var playback = MachineTestFactory.CreateC64Machine();
        store.Play(playback);
        playback.Bus.Write(0x0001, 0x36);

        var reference = MachineTestFactory.CreateC64Machine();
        reference.Bus.Write(0xA000, 0x42);
        reference.Clock.Step(30);
        SetKey(reference, "B", true);
        reference.Bus.Write(0x0001, 0x36);

        Assert.Equal(0x42, reference.Bus.Read(0xA000));
        Assert.Equal(reference.Clock.TotalCycles, playback.Clock.TotalCycles);
        Assert.Equal(0x42, playback.Bus.Read(0xA000));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts after the machine has already executed.
    /// Acceptance: Playback on a fresh machine matches an independent run
    /// that stepped the same cycles once and then applied the key.
    /// The restored image already contains those cycles, so play must not
    /// execute them a second time.
    /// </summary>
    [Fact]
    public void Playback_FromRunningBaseline_MatchesReference()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        recorded.Clock.Step(1000);
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1000, "A", true, recorded);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        var reference = MachineTestFactory.CreateC64Machine();
        reference.Clock.Step(1000);
        SetKey(reference, "A", true);

        Assert.Equal(1000L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(reference.GetState().Y, played.GetState().Y);
        Assert.Equal(reference.Bus.Peek(0x002B), played.Bus.Peek(0x002B));
        Assert.True(SameImage(store.Capture(played), store.Capture(reference)));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts after the machine has already executed,
    /// including SID voices that have been clocked.
    /// Acceptance: Playback matches an independent run at the same cycle.
    /// VIC-II raster phase and SID OSC3 are execution state, not just the
    /// last register write. AC3 requires the same VIC-II and audio output.
    /// </summary>
    [Fact]
    public void Playback_FromRunningBaseline_MatchesVicAndSid()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        WriteVoice3Saw(recorded);
        recorded.Clock.Step(1000);
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1000, "A", true, recorded);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        var reference = MachineTestFactory.CreateC64Machine();
        WriteVoice3Saw(reference);
        reference.Clock.Step(1000);
        SetKey(reference, "A", true);

        var fresh = MachineTestFactory.CreateC64Machine();
        var referenceVideo = Video(reference);
        var playedVideo = Video(played);
        var freshVideo = Video(fresh);
        var referenceOsc = reference.Bus.Read(0xD41B);
        var playedOsc = played.Bus.Read(0xD41B);
        var freshOsc = fresh.Bus.Read(0xD41B);
        var referenceRaster = reference.Bus.Read(0xD012);
        var playedRaster = played.Bus.Read(0xD012);

        Assert.NotEqual(freshVideo.CurrentRasterLine, referenceVideo.CurrentRasterLine);
        Assert.NotEqual(freshOsc, referenceOsc);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(reference.GetState().Y, played.GetState().Y);
        Assert.Equal(referenceVideo.CurrentRasterLine, playedVideo.CurrentRasterLine);
        Assert.Equal(referenceRaster, playedRaster);
        Assert.Equal(referenceOsc, playedOsc);
        Assert.Equal(referenceVideo.FrameBuffer, playedVideo.FrameBuffer);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Seek lands on a later frame when recording started after
    /// the machine had already run.
    /// Acceptance: The sought machine matches an independent run that keeps
    /// the captured cycles and steps only the cycles still ahead of that
    /// point. AC3 and AC8. Re-executing the captured prefix is a miss.
    /// </summary>
    [Fact]
    public void Seek_FromRunningBaseline_AdvancesOnlyRemainingCycles()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        WriteVoice3Saw(recorded);
        recorded.Clock.Step(1000);
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1000, "A", true, recorded);

        var sought = MachineTestFactory.CreateC64Machine();
        var frame = store.Seek(sought, 1);

        var reference = MachineTestFactory.CreateC64Machine();
        WriteVoice3Saw(reference);
        reference.Clock.Step(1000);
        SetKey(reference, "A", true);
        var frameCycles = FrameCycles(reference);
        reference.Clock.Step(frameCycles - 1000);

        var referenceVideo = Video(reference);
        var soughtVideo = Video(sought);
        var referenceOsc = reference.Bus.Read(0xD41B);
        var soughtOsc = sought.Bus.Read(0xD41B);

        Assert.Equal(1, frame);
        Assert.Equal(frameCycles, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, sought.Clock.TotalCycles);
        Assert.Equal(reference.GetState().Y, sought.GetState().Y);
        Assert.Equal(reference.GetState().PC, sought.GetState().PC);
        Assert.Equal(referenceVideo.CurrentRasterLine, soughtVideo.CurrentRasterLine);
        Assert.Equal(reference.Bus.Read(0xD012), sought.Bus.Read(0xD012));
        Assert.Equal(referenceOsc, soughtOsc);
        Assert.Equal(referenceVideo.FrameBuffer, soughtVideo.FrameBuffer);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A gated noise voice has been clocked before recording starts.
    /// Acceptance: Playback matches ENV3 and the audio sample at that cycle,
    /// then OSC3 and ENV3 after 100 more cycles. VICE stores the envelope
    /// counters and the noise shift register beside the SID registers.
    /// AC3. A saw-wave OSC3 compare does not cover this path.
    /// </summary>
    [Fact]
    public void Playback_FromRunningBaseline_MatchesSidEnvelopeAndAudio()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        WriteGatedNoise(recorded);
        recorded.Clock.Step(1000);
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1000, "A", true, recorded);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        var reference = MachineTestFactory.CreateC64Machine();
        WriteGatedNoise(reference);
        reference.Clock.Step(1000);
        SetKey(reference, "A", true);

        var fresh = MachineTestFactory.CreateC64Machine();
        var referenceEnv = reference.Bus.Read(0xD41C);
        var playedEnv = played.Bus.Read(0xD41C);
        var freshEnv = fresh.Bus.Read(0xD41C);
        var referenceSample = Sid(reference).GenerateSample();
        var playedSample = Sid(played).GenerateSample();

        Assert.NotEqual(freshEnv, referenceEnv);
        Assert.NotEqual(0f, referenceSample);
        Assert.Equal(referenceEnv, playedEnv);
        Assert.Equal(referenceSample, playedSample);

        reference.Clock.Step(100);
        played.Clock.Step(100);

        Assert.Equal(reference.Bus.Read(0xD41B), played.Bus.Read(0xD41B));
        Assert.Equal(reference.Bus.Read(0xD41C), played.Bus.Read(0xD41C));
        Assert.Equal(Sid(reference).GenerateSample(), Sid(played).GenerateSample());
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A gated noise voice is already running when recording starts, and a live audio backend is attached.
    /// Acceptance: Three PAL machines share one ROM provider and each has a
    /// passive sample collector. Write $D40E=$FF, $D40F=$FF, $D413=$00,
    /// $D414=$F0, $D412=$81, $D418=$0F. Step 1000 cycles, flush, and clear
    /// the collectors. Record A, play into the fresh machine, and press A
    /// on the reference. Step 100 cycles and flush. All clocks are 1100.
    /// The recording and the reference emit the same five nonzero samples.
    /// Playback emits those same samples. Four zeros are a miss. AC3.
    /// </summary>
    [Fact]
    public void Playback_FromRunningBaseline_MatchesLivePcm()
    {
        var roms = MachineTestFactory.CreateC64RomProvider();
        var recordedSink = new CollectingAudioBackend();
        var referenceSink = new CollectingAudioBackend();
        var playedSink = new CollectingAudioBackend();
        var recorded = new ArchitectureBuilder(roms, recordedSink).Build(new C64Descriptor());
        var reference = new ArchitectureBuilder(roms, referenceSink).Build(new C64Descriptor());
        var played = new ArchitectureBuilder(roms, playedSink).Build(new C64Descriptor());

        WriteGatedNoise(recorded);
        WriteGatedNoise(reference);
        recorded.Clock.Step(1000);
        reference.Clock.Step(1000);
        Sid(recorded).FlushAudioBuffer();
        Sid(reference).FlushAudioBuffer();
        recordedSink.Samples.Clear();
        referenceSink.Samples.Clear();

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1000, "A", true, recorded);
        store.Play(played);
        SetKey(reference, "A", true);
        recorded.Clock.Step(100);
        reference.Clock.Step(100);
        played.Clock.Step(100);
        Sid(recorded).FlushAudioBuffer();
        Sid(reference).FlushAudioBuffer();
        Sid(played).FlushAudioBuffer();

        Assert.Equal(1100L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, recorded.Clock.TotalCycles);
        Assert.Equal(5, referenceSink.Samples.Count);
        Assert.Equal(recordedSink.Samples, referenceSink.Samples);
        Assert.NotEqual(0f, referenceSink.Samples[0]);
        Assert.Equal(referenceSink.Samples, playedSink.Samples);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts in the middle of an instruction.
    /// Acceptance: One more cycle after playback matches an independent
    /// machine that continued the same instruction. Restoring only the
    /// public registers restarts the opcode. AC3.
    /// </summary>
    [Fact]
    public void Playback_FromMidInstruction_ContinuesTheCpu()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        recorded.Clock.Step(1006);
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1006, "A", true, recorded);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        var reference = MachineTestFactory.CreateC64Machine();
        reference.Clock.Step(1006);
        SetKey(reference, "A", true);
        reference.Clock.Step(1);
        played.Clock.Step(1);

        Assert.Equal(1007L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(reference.GetState().Y, played.GetState().Y);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts after a partial CIA serial input byte.
    /// Acceptance: Playback keeps the unfinished shifter. Bits 1,0,1 before
    /// the snapshot and 0,0,1,0,1 after it latch SDR $A5, and LDA $DC0C /
    /// STA $C100 stores that byte. VICE saves sr_bits and the shifter.
    /// AC3. A CIA Reset test does not cover replay restore.
    /// </summary>
    [Fact]
    public void Playback_KeepsAnUnfinishedCiaSerialByte()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        FeedSerialBits(recorded, 1, 0, 1);
        FeedSerialBits(reference, 1, 0, 1);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        FeedSerialBits(reference, 0, 0, 1, 0, 1);
        FeedSerialBits(played, 0, 0, 1, 0, 1);
        ArmSdrStore(reference);
        ArmSdrStore(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(0xA5, reference.Bus.Read(0xDC0C));
        Assert.Equal(0xA5, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Read(0xDC0C), played.Bus.Read(0xDC0C));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Seek to a frame boundary while a later cycle in that frame
    /// still has recorded input.
    /// Acceptance: Seek(1) stops at the frame boundary (one PAL frame) and
    /// leaves the later event for the next step. Applying that event during
    /// Seek cannot be rewound. AC3 and AC8.
    /// </summary>
    [Fact]
    public void Seek_DoesNotApplyInputAfterTheFrameBoundary()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var frameCycles = FrameCycles(recorded);
        recorded.Clock.Step((int)frameCycles + 100);
        store.RecordKey(1, 100, "A", false, recorded);

        var sought = MachineTestFactory.CreateC64Machine();
        var frame = store.Seek(sought, 1);

        Assert.Equal(1, frame);
        Assert.Equal(frameCycles, sought.Clock.TotalCycles);

        store.Step();
        Assert.Equal(frameCycles + 100, sought.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The processor port is an input when recording starts.
    /// Acceptance: Playback keeps the data latch, not the pulled-up pin
    /// read. DDR $00 and latch $00 stay latch $00. After the pins are
    /// driven as outputs, LDA $0001 / STA $C100 stores $00. VICE saves
    /// pport.data separately from the pin read. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsProcessorPortDataLatch()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        recorded.Bus.Write(0x0000, 0x00);
        recorded.Bus.Write(0x0001, 0x00);
        reference.Bus.Write(0x0000, 0x00);
        reference.Bus.Write(0x0001, 0x00);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        var referencePla = Pla(reference);
        var playedPla = Pla(played);
        Assert.Equal(0x00, referencePla.DataRegister);
        Assert.Equal(referencePla.DataRegister, playedPla.DataRegister);

        reference.Bus.Write(0x0000, 0x3F);
        played.Bus.Write(0x0000, 0x3F);
        ArmSdrStore(reference);
        ArmSdrStore(played);
        reference.Bus.Write(0xC001, 0x01);
        reference.Bus.Write(0xC002, 0x00);
        played.Bus.Write(0xC001, 0x01);
        played.Bus.Write(0xC002, 0x00);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(0x00, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Seek lands on a periodic snapshot taken while an earlier
    /// key is still held.
    /// Acceptance: A-down at frame 0 and B-down at frame 10 leave both keys
    /// down after Seek(10). The column read is $EB, the same as a reference
    /// that received both keys. Losing A ($EF) is a miss. The earlier cycles
    /// are not executed again. AC3 and AC8.
    /// </summary>
    [Fact]
    public void Seek_FromPeriodicSnapshot_KeepsHeldKeys()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var frameCycles = FrameCycles(recorded);
        recorded.Clock.Step((int)(frameCycles * 10));
        store.RecordKey(10, 0, "B", true, recorded);

        var sought = MachineTestFactory.CreateC64Machine();
        var frame = store.Seek(sought, 10);

        var reference = MachineTestFactory.CreateC64Machine();
        SetKey(reference, "A", true);
        reference.Clock.Step((int)(frameCycles * 10));
        SetKey(reference, "B", true);
        SelectKeyboardColumns(reference);
        SelectKeyboardColumns(sought);

        Assert.Equal(10, frame);
        Assert.Equal(frameCycles * 10, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, sought.Clock.TotalCycles);
        Assert.Equal(0xEB, reference.Bus.Read(0xDC01));
        Assert.Equal(reference.Bus.Read(0xDC01), sought.Bus.Read(0xDC01));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts one cycle after a CIA timer is started.
    /// Acceptance: One more cycle leaves Timer A at $1234 on both machines.
    /// A restored pipeline of zero counts immediately and leaves $1233.
    /// VICE saves the timer state word. AC3. This is the in-memory replay
    /// image, not the VSF one-cycle stagger.
    /// </summary>
    [Fact]
    public void Playback_KeepsCiaTimerStartPipeline()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmTimerA(recorded);
        ArmTimerA(reference);
        recorded.Clock.Step(1);
        reference.Clock.Step(1);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        reference.Clock.Step(1);
        played.Clock.Step(1);

        Assert.Equal(2L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x34, reference.Bus.Read(0xDC04));
        Assert.Equal(0x12, reference.Bus.Read(0xDC05));
        Assert.Equal(reference.Bus.Read(0xDC04), played.Bus.Read(0xDC04));
        Assert.Equal(reference.Bus.Read(0xDC05), played.Bus.Read(0xDC05));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: RAM under the character ROM is written, then replayed.
    /// Acceptance: With $0001=$33, a store of $42 at $D000 stays in RAM.
    /// Playback into a fresh machine, then $0001=$30, still reads $42.
    /// LDA $D000 / STA $C100 stores $42 on both machines after eight cycles.
    /// Writing the character-ROM peek back into RAM ($3C) is a miss. VICE
    /// saves mem_ram, not the banked CPU view. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsRamHiddenUnderCharacterRom()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        recorded.Bus.Write(0x0001, 0x33);
        reference.Bus.Write(0x0001, 0x33);
        recorded.Bus.Write(0xD000, 0x42);
        reference.Bus.Write(0xD000, 0x42);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        recorded.Bus.Write(0x0001, 0x30);
        reference.Bus.Write(0x0001, 0x30);
        played.Bus.Write(0x0001, 0x30);

        Assert.Equal(0x42, reference.Bus.Peek(0xD000));
        Assert.Equal(reference.Bus.Peek(0xD000), played.Bus.Peek(0xD000));

        ArmAbsLoadStore(reference, 0xD000, 0xC100);
        ArmAbsLoadStore(played, 0xD000, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(0x42, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Seek forward onto a periodic slot, then seek back to the start.
    /// Acceptance: A-down at frame 0 and B-down at frame 10, then Seek(10)
    /// and Seek(0) on the same machine, leave only A held. The column read
    /// is $FB. Keeping B ($EB) is a miss. Both clocks are back at cycle 0.
    /// VICE restores the keyboard matrix from the selected snapshot. AC3 and AC8.
    /// </summary>
    [Fact]
    public void Seek_BackwardDropsKeysThatBelongToALaterFrame()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var frameCycles = FrameCycles(recorded);
        recorded.Clock.Step((int)(frameCycles * 10));
        store.RecordKey(10, 0, "B", true, recorded);

        var sought = MachineTestFactory.CreateC64Machine();
        store.Seek(sought, 10);
        var frame = store.Seek(sought, 0);

        var reference = MachineTestFactory.CreateC64Machine();
        SetKey(reference, "A", true);
        SelectKeyboardColumns(reference);
        SelectKeyboardColumns(sought);

        Assert.Equal(0, frame);
        Assert.Equal(0L, sought.Clock.TotalCycles);
        Assert.Equal(0xFB, reference.Bus.Read(0xDC01));
        Assert.Equal(reference.Bus.Read(0xDC01), sought.Bus.Read(0xDC01));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A key is held before recording, then released in a later frame.
    /// Acceptance: B is down, A is recorded at frame 0, and B-up is recorded
    /// at frame 1. Seek(0) keeps B. The column read is $EB. Releasing B
    /// because a later B-up exists ($FB) is a miss. LDA $DC01 / STA $C100
    /// stores $EB and both clocks are 8. VICE restores the selected
    /// snapshot matrix. AC3 and AC8.
    /// </summary>
    [Fact]
    public void Seek_KeepsPreRecordKeyWhenALaterReleaseExists()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        SetKey(recorded, "B", true);
        store.RecordKey(0, 0, "A", true, recorded);
        var frameCycles = FrameCycles(recorded);
        recorded.Clock.Step((int)frameCycles);
        store.RecordKey(1, 0, "B", false, recorded);

        var sought = MachineTestFactory.CreateC64Machine();
        var frame = store.Seek(sought, 0);

        var reference = MachineTestFactory.CreateC64Machine();
        SetKey(reference, "B", true);
        SetKey(reference, "A", true);
        SelectKeyboardColumns(reference);
        SelectKeyboardColumns(sought);

        Assert.Equal(0, frame);
        Assert.Equal(0xEB, reference.Bus.Read(0xDC01));
        Assert.Equal(reference.Bus.Read(0xDC01), sought.Bus.Read(0xDC01));

        ArmAbsLoadStore(reference, 0xDC01, 0xC100);
        ArmAbsLoadStore(sought, 0xDC01, 0xC100);
        reference.Clock.Step(8);
        sought.Clock.Step(8);

        Assert.Equal(0xEB, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), sought.Bus.Peek(0xC100));
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, sought.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Processor-port bits 6 and 7 are still charged when recording starts.
    /// Acceptance: DDR $FF, data $F7, then DDR $3F. Playback reads $0001 as
    /// $F7. A fresh port without the charge reads $37. LDA $0001 / STA $C100
    /// stores $F7 and both clocks are 8. VICE snapshots the bit 6 and 7
    /// falloff state. The serialized image stays 16 plus 65536. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsProcessorPortCharge()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        recorded.Bus.Write(0x0000, 0xFF);
        reference.Bus.Write(0x0000, 0xFF);
        recorded.Bus.Write(0x0001, 0xF7);
        reference.Bus.Write(0x0001, 0xF7);
        recorded.Bus.Write(0x0000, 0x3F);
        reference.Bus.Write(0x0000, 0x3F);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(0xF7, reference.Bus.Read(0x0001));
        Assert.Equal(reference.Bus.Read(0x0001), played.Bus.Read(0x0001));

        ArmAbsLoadStore(reference, 0x0001, 0xC100);
        ArmAbsLoadStore(played, 0x0001, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(0xF7, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A VIC raster IRQ is latched and the line rise is still one
    /// cycle away when recording starts.
    /// Acceptance: 128 NOPs at $C000, I clear, $D011=$00, $D012=$01,
    /// $D01A=$01, 57 cycles, then eight more after playback. At cycle 65
    /// the reference PC is $C01E, S is $FD, $0100 is $C0, the IRQ was
    /// asserted at cycle 58, and the delay is 7. $D019 is $F1. Playback
    /// that keeps executing NOPs is a miss. VICE restores irq_status and
    /// the interrupt source. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsVicRasterIrqPipeline()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmVicRasterIrq(recorded);
        ArmVicRasterIrq(reference);
        recorded.Clock.Step(57);
        reference.Clock.Step(57);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 57, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        var referenceClock = (SystemClock)reference.Clock;
        var playedClock = (SystemClock)played.Clock;
        Assert.Equal(65L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC01E, reference.GetState().PC);
        Assert.Equal(0xFD, reference.GetState().S);
        Assert.Equal(0xC0, reference.Bus.Peek(0x0100));
        Assert.Equal(58L, referenceClock.DebugIrqAssertCycle);
        Assert.Equal(7, referenceClock.DebugIrqDelayCycles);
        Assert.Equal(0xF1, reference.Bus.Read(0xD019));
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(reference.GetState().S, played.GetState().S);
        Assert.Equal(reference.Bus.Peek(0x0100), played.Bus.Peek(0x0100));
        Assert.Equal(referenceClock.DebugIrqAssertCycle, playedClock.DebugIrqAssertCycle);
        Assert.Equal(referenceClock.DebugIrqDelayCycles, playedClock.DebugIrqDelayCycles);
        Assert.Equal(reference.Bus.Read(0xD019), played.Bus.Read(0xD019));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A VIC raster IRQ line is already asserted when recording starts.
    /// Acceptance: The same NOP sled steps 58 cycles before the recording.
    /// Eight more cycles leave the reference at PC $C01E. Playback that
    /// reaches $C021 has lost the asserted source. VICE calls
    /// interrupt_restore_irq when irq_status bit 7 is set. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsVicRasterIrqAfterTheLineIsAsserted()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmVicRasterIrq(recorded);
        ArmVicRasterIrq(reference);
        recorded.Clock.Step(58);
        reference.Clock.Step(58);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 58, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        var referenceClock = (SystemClock)reference.Clock;
        var playedClock = (SystemClock)played.Clock;
        Assert.Equal(66L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC01E, reference.GetState().PC);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(reference.GetState().S, played.GetState().S);
        Assert.Equal(reference.Bus.Peek(0x0100), played.Bus.Peek(0x0100));
        Assert.Equal(referenceClock.DebugIrqAssertCycle, playedClock.DebugIrqAssertCycle);
        Assert.Equal(referenceClock.DebugIrqDelayCycles, playedClock.DebugIrqDelayCycles);
        Assert.Equal(reference.Bus.Read(0xD019), played.Bus.Read(0xD019));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The programmed VIC raster compare is still ahead of the
    /// live raster when recording starts.
    /// Acceptance: LDA $D019; STA $C100; JMP $C000, I set, $D011=$00,
    /// $D012=$20, $D019=$0F, $D01A=$01. After 1000 cycles both clocks are
    /// 1000 and both rasters are 15. The reference compare is 32. Playback
    /// compare 15 is a FAIL. 1208 more cycles reach clock 2208, PC $C006,
    /// and raster 35. Reference $D019 and $C100 are $F1. Playback $70 is a
    /// FAIL. VICE vicii-snapshot.c saves raster_irq_line and
    /// raster_irq_triggered. The register file mirrors the live raster.
    /// AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsFutureVicRasterCompare()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmFutureRasterCompare(recorded);
        ArmFutureRasterCompare(reference);
        recorded.Clock.Step(1000);
        reference.Clock.Step(1000);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1000, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(1000L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(15, Vic(reference).CurrentRasterLine);
        Assert.Equal(Vic(reference).CurrentRasterLine, Vic(played).CurrentRasterLine);
        Assert.Equal(32, Vic(reference).RasterIrqLine);
        Assert.Equal(Vic(reference).RasterIrqLine, Vic(played).RasterIrqLine);

        reference.Clock.Step(1208);
        played.Clock.Step(1208);

        Assert.Equal(2208L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC006, reference.GetState().PC);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(35, Vic(reference).CurrentRasterLine);
        Assert.Equal(Vic(reference).CurrentRasterLine, Vic(played).CurrentRasterLine);
        Assert.Equal(0xF1, reference.Bus.Read(0xD019));
        Assert.Equal(0xF1, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Read(0xD019), played.Bus.Read(0xD019));
    }

    /// <summary>
    /// FR: FR-SNP-003, FR-MEM-004.
    /// Use case: CIA2 has selected VIC bank 0 and I/O is banked out when
    /// recording starts.
    /// Acceptance: $DD02=$03, $DD00=$03, RAM $2000=$42, RAM $E000=$24, then
    /// $0001=$33. Playback clocks stay 0. CIA2 port A peeks $7F. The VIC
    /// bank is 0 and ReadVideoMemory($2000) returns $42. Bank 3 returning
    /// $24 is a miss. VICE undump_ciapa restores the bank from the port
    /// pins. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsCia2BankWhenIoIsHidden()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        recorded.Bus.Write(0xDD02, 0x03);
        reference.Bus.Write(0xDD02, 0x03);
        recorded.Bus.Write(0xDD00, 0x03);
        reference.Bus.Write(0xDD00, 0x03);
        recorded.Bus.Write(0x2000, 0x42);
        reference.Bus.Write(0x2000, 0x42);
        recorded.Bus.Write(0xE000, 0x24);
        reference.Bus.Write(0xE000, 0x24);
        recorded.Bus.Write(0x0001, 0x33);
        reference.Bus.Write(0x0001, 0x33);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(0L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x7F, Cia2(reference).Peek(0xDD00));
        Assert.Equal(Cia2(reference).Peek(0xDD00), Cia2(played).Peek(0xDD00));
        Assert.Equal(0, VicBank(reference));
        Assert.Equal(0x42, Vic(reference).ReadVideoMemory(0x2000));
        Assert.Equal(VicBank(reference), VicBank(played));
        Assert.Equal(Vic(reference).ReadVideoMemory(0x2000), Vic(played).ReadVideoMemory(0x2000));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts while the VIC is already inside the display window.
    /// Acceptance: A JMP $C000 loop, $D011=$1B, $D016=$C8, $D018=$14,
    /// $D020=$06, $D021=$00, and 1000 screen and color bytes of $01, after
    /// 4000 cycles, has vertical and main border flags clear and raster 63.
    /// Playback that resets those flags to set is a miss. One hundred more
    /// cycles leave both clocks at 4100 and PC at $C000, and the frame
    /// buffers match. A 1920-byte frame difference is a miss. VICE saves
    /// vborder, set_vborder, and main_border. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsVicBorderWhileDisplayIsActive()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmActiveDisplay(recorded);
        ArmActiveDisplay(reference);
        recorded.Clock.Step(4000);
        reference.Clock.Step(4000);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 4000, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(4000L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(63, Vic(reference).CurrentRasterLine);
        Assert.Equal(Vic(reference).CurrentRasterLine, Vic(played).CurrentRasterLine);
        Assert.False(Vic(reference).IsVerticalBorderActive);
        Assert.False(Vic(reference).IsMainBorderActive);
        Assert.Equal(Vic(reference).IsVerticalBorderActive, Vic(played).IsVerticalBorderActive);
        Assert.Equal(Vic(reference).IsMainBorderActive, Vic(played).IsMainBorderActive);

        reference.Clock.Step(100);
        played.Clock.Step(100);

        Assert.Equal(4100L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC000, reference.GetState().PC);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(Vic(reference).FrameBuffer, Vic(played).FrameBuffer);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A background colour write is still pending when recording starts on a C64C.
    /// Acceptance: Active display on c64c, step 4000 cycles, write $D021=$02,
    /// then record A. At cycle 4000 the pending colour register is $21 and
    /// the value is $02 on the reference and on playback. One hundred more
    /// cycles leave both clocks at 4100, PC at $C000, and raster 65, and the
    /// frame buffers match. Playback pending $2E/$00 and frame byte $48 at
    /// offset 72992 are a miss. VICE vicii-snapshot saves last_color_reg and
    /// last_color_value. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsPendingVicColorWrite()
    {
        var recorded = MachineTestFactory.CreateC64Machine("c64c");
        var reference = MachineTestFactory.CreateC64Machine("c64c");
        ArmActiveDisplay(recorded);
        ArmActiveDisplay(reference);
        recorded.Clock.Step(4000);
        reference.Clock.Step(4000);
        recorded.Bus.Write(0xD021, 0x02);
        reference.Bus.Write(0xD021, 0x02);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 4000, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine("c64c");
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(4000L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x21, Vic(reference).VicLastColorRegWrite);
        Assert.Equal(0x02, Vic(reference).VicLastColorValueWrite);
        Assert.Equal(Vic(reference).VicLastColorRegWrite, Vic(played).VicLastColorRegWrite);
        Assert.Equal(Vic(reference).VicLastColorValueWrite, Vic(played).VicLastColorValueWrite);

        reference.Clock.Step(100);
        played.Clock.Step(100);

        Assert.Equal(4100L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC000, reference.GetState().PC);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(65, Vic(reference).CurrentRasterLine);
        Assert.Equal(Vic(reference).CurrentRasterLine, Vic(played).CurrentRasterLine);
        Assert.Equal(Vic(reference).FrameBuffer, Vic(played).FrameBuffer);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts on a late bad line while the BA prefetch countdown is still running.
    /// Acceptance: Active display, step 3990 cycles, write $D011=$1F, step
    /// one cycle, then record A. At cycle 3991 the prefetch countdown is 3
    /// on the reference and on playback. One hundred more cycles leave both
    /// clocks at 4091, PC at $C000, and raster 65, and the frame buffers
    /// match. Playback countdown 0 and frame byte $00 at offset 72800 are
    /// a miss. VICE vicii-snapshot saves and restores prefetch_cycles. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsPrefetchCountdownOnLateBadLine()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmActiveDisplay(recorded);
        ArmActiveDisplay(reference);
        recorded.Clock.Step(3990);
        reference.Clock.Step(3990);
        recorded.Bus.Write(0xD011, 0x1F);
        reference.Bus.Write(0xD011, 0x1F);
        recorded.Clock.Step(1);
        reference.Clock.Step(1);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 3991, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(3991L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(3, Vic(reference).PrefetchCycles);
        Assert.Equal(Vic(reference).PrefetchCycles, Vic(played).PrefetchCycles);

        reference.Clock.Step(100);
        played.Clock.Step(100);

        Assert.Equal(4091L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC000, reference.GetState().PC);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(65, Vic(reference).CurrentRasterLine);
        Assert.Equal(Vic(reference).CurrentRasterLine, Vic(played).CurrentRasterLine);
        Assert.Equal(Vic(reference).FrameBuffer, Vic(played).FrameBuffer);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Sprite 0 is already fetching when recording starts.
    /// Acceptance: The active display from the border fact, plus
    /// $D000=$50, $D001=$40, $D015=$01, $D027=$02, $07F8=$80, and
    /// $2000+i = i+1 for 64 bytes. After 5000 cycles the reference
    /// sprite 0 Mc and McBase are 45, the pointer is 128, and Data is
    /// 2829357. Playback zeros are a miss. Two hundred more cycles leave
    /// both clocks at 5200, PC at $C000, and raster 82, and the frame
    /// buffers match. A 57-byte frame difference is a miss. VICE saves
    /// sprite data, mc, mcbase, pointer, and the expansion flip-flop. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsActiveSprite()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmActiveSprite(recorded);
        ArmActiveSprite(reference);
        recorded.Clock.Step(5000);
        reference.Clock.Step(5000);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 5000, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(5000L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        var referenceVic = Vic(reference);
        var playedVic = Vic(played);
        Assert.Equal(45, referenceVic.GetSpriteMc(0));
        Assert.Equal(45, referenceVic.GetSpriteMcBase(0));
        Assert.Equal(128, referenceVic.GetSpritePointer(0));
        Assert.Equal(2829357u, referenceVic.GetSpriteData(0));
        Assert.Equal(referenceVic.GetSpriteMc(0), playedVic.GetSpriteMc(0));
        Assert.Equal(referenceVic.GetSpriteMcBase(0), playedVic.GetSpriteMcBase(0));
        Assert.Equal(referenceVic.GetSpritePointer(0), playedVic.GetSpritePointer(0));
        Assert.Equal(referenceVic.GetSpriteData(0), playedVic.GetSpriteData(0));

        reference.Clock.Step(200);
        played.Clock.Step(200);

        Assert.Equal(5200L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC000, reference.GetState().PC);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(82, referenceVic.CurrentRasterLine);
        Assert.Equal(referenceVic.CurrentRasterLine, playedVic.CurrentRasterLine);
        Assert.Equal(referenceVic.FrameBuffer, playedVic.FrameBuffer);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: $D011 changes on the cycle recording starts, so the delayed
    /// mode bit is still the previous value.
    /// Acceptance: Active display, $D011=$3B for 3985 cycles, then $D011=$1B
    /// and record A. One hundred more cycles leave both clocks at 4085, PC
    /// at $C000, and raster 64. The frame buffers match. A 12-byte difference
    /// at offset 72580 is a miss. VICE saves reg11_delay. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsPendingVicControlDelay()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmActiveDisplay(recorded);
        ArmActiveDisplay(reference);
        recorded.Bus.Write(0xD011, 0x3B);
        reference.Bus.Write(0xD011, 0x3B);
        recorded.Clock.Step(3985);
        reference.Clock.Step(3985);
        recorded.Bus.Write(0xD011, 0x1B);
        reference.Bus.Write(0xD011, 0x1B);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 3985, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        reference.Clock.Step(100);
        played.Clock.Step(100);

        Assert.Equal(4085L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC000, reference.GetState().PC);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(64, Vic(reference).CurrentRasterLine);
        Assert.Equal(Vic(reference).CurrentRasterLine, Vic(played).CurrentRasterLine);
        Assert.Equal(Vic(reference).FrameBuffer, Vic(played).FrameBuffer);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts on the cycle that arms the next frame.
    /// Acceptance: The active display from the border fact, stepped 19650
    /// cycles, leaves both rasters at 311. One more cycle leaves the
    /// reference raster at 0 and $D012 at $00. Playback that stays on raster
    /// 311 with $D012 $37 is a miss. LDA $D012; STA $C100 then stores $00
    /// and both clocks are 19659. VICE saves start_of_frame. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsVicStartOfFrame()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmActiveDisplay(recorded);
        ArmActiveDisplay(reference);
        recorded.Clock.Step(19650);
        reference.Clock.Step(19650);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 19650, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(19650L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(311, Vic(reference).CurrentRasterLine);
        Assert.Equal(Vic(reference).CurrentRasterLine, Vic(played).CurrentRasterLine);

        reference.Clock.Step(1);
        played.Clock.Step(1);

        Assert.Equal(19651L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0, Vic(reference).CurrentRasterLine);
        Assert.Equal(0x00, reference.Bus.Read(0xD012));
        Assert.Equal(Vic(reference).CurrentRasterLine, Vic(played).CurrentRasterLine);
        Assert.Equal(reference.Bus.Read(0xD012), played.Bus.Read(0xD012));

        WriteLoadRaster(reference);
        WriteLoadRaster(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(19659L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x00, reference.Bus.Read(0xC100));
        Assert.Equal(reference.Bus.Read(0xC100), played.Bus.Read(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts after the light pen has latched.
    /// Acceptance: Fresh PAL machines stepped 1000 cycles, then the light
    /// pen triggered, leave $D013/$D014 at $C0/$0F. Playback zeros are a
    /// miss. LDA $D014; STA $C100; JMP $C000 for 40 cycles leaves both
    /// clocks at 1040 and stores $0F. VICE saves the light-pen latches. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsVicLightPen()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        recorded.Clock.Step(1000);
        reference.Clock.Step(1000);
        Vic(recorded).TriggerLightPen();
        Vic(reference).TriggerLightPen();

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1000, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(1000L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC0, reference.Bus.Read(0xD013));
        Assert.Equal(0x0F, reference.Bus.Read(0xD014));
        Assert.Equal(reference.Bus.Read(0xD013), played.Bus.Read(0xD013));
        Assert.Equal(reference.Bus.Read(0xD014), played.Bus.Read(0xD014));

        WriteLoadLightPen(reference);
        WriteLoadLightPen(played);
        reference.Clock.Step(40);
        played.Clock.Step(40);

        Assert.Equal(1040L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x0F, reference.Bus.Read(0xC100));
        Assert.Equal(reference.Bus.Read(0xC100), played.Bus.Read(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts while two sprites overlap.
    /// Acceptance: The active sprite fact plus a second sprite at the same
    /// position, stepped 6000 cycles, leaves $D01E and $D01F at $03.
    /// Playback zeros are a miss. Copying both registers into RAM leaves
    /// both clocks at 6024 and stores $03 at $C100 and $C101. VICE saves
    /// the sprite collision latches. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsVicSpriteCollisions()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmOverlappingSprites(recorded);
        ArmOverlappingSprites(reference);
        recorded.Clock.Step(6000);
        reference.Clock.Step(6000);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 6000, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(6000L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x03, Vic(reference).SpriteSpriteCollision);
        Assert.Equal(0x03, Vic(reference).SpriteBackgroundCollision);
        Assert.Equal(Vic(reference).SpriteSpriteCollision, Vic(played).SpriteSpriteCollision);
        Assert.Equal(Vic(reference).SpriteBackgroundCollision, Vic(played).SpriteBackgroundCollision);

        WriteLoadCollisions(reference);
        WriteLoadCollisions(played);
        reference.Clock.Step(24);
        played.Clock.Step(24);

        Assert.Equal(6024L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x03, reference.Bus.Read(0xC100));
        Assert.Equal(0x03, reference.Bus.Read(0xC101));
        Assert.Equal(reference.Bus.Read(0xC100), played.Bus.Read(0xC100));
        Assert.Equal(reference.Bus.Read(0xC101), played.Bus.Read(0xC101));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A CIA Timer B one-shot is force-loaded and still running
    /// when recording starts.
    /// Acceptance: $DC06=$34, $DC07=$12, $DC0F=$19, one cycle, then three
    /// more cycles after playback, leaves Timer B at $1233. A restore that
    /// treats one-shot plus counter-equals-latch as stopped leaves $1234.
    /// The live Running flag is kept on the in-memory replay image. The VSF
    /// heuristic stays on the serialized path. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsRunningCiaTimerBOneShot()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmTimerB(recorded);
        ArmTimerB(reference);
        recorded.Clock.Step(1);
        reference.Clock.Step(1);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        reference.Clock.Step(3);
        played.Clock.Step(3);

        Assert.Equal(4L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x33, reference.Bus.Read(0xDC06));
        Assert.Equal(0x12, reference.Bus.Read(0xDC07));
        Assert.Equal(reference.Bus.Read(0xDC06), played.Bus.Read(0xDC06));
        Assert.Equal(reference.Bus.Read(0xDC07), played.Bus.Read(0xDC07));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A completed CIA Timer B one-shot keeps the VICE control read.
    /// Acceptance: Write $DC06=$02, $DC07=$00, and $DC0F=$19. Step 10 cycles,
    /// record A, and play. VICE ciacore.c reads CRB as the stored byte with
    /// START replaced by the running flag, so both machines read $08. LDA
    /// $DC0F; STA $C100 stores $08. A stored start bit of $09 is the miss. AC3.
    /// </summary>
    [Fact]
    public void Playback_ReadsCompletedTimerBStartLikeVice()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        // Flags $24 sets the interrupt disable bit. The one-shot underflow
        // must not vector away from the CRB load.
        ((Mos6502)recorded.Devices.GetByRole(DeviceRole.Cpu)!).Flags = 0x24;
        ((Mos6502)reference.Devices.GetByRole(DeviceRole.Cpu)!).Flags = 0x24;
        recorded.Bus.Write(0xDC06, 0x02);
        recorded.Bus.Write(0xDC07, 0x00);
        recorded.Bus.Write(0xDC0F, 0x19);
        reference.Bus.Write(0xDC06, 0x02);
        reference.Bus.Write(0xDC07, 0x00);
        reference.Bus.Write(0xDC0F, 0x19);
        recorded.Clock.Step(10);
        reference.Clock.Step(10);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 10, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        Assert.Equal(10L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x08, reference.Bus.Read(0xDC0F));
        Assert.Equal(reference.Bus.Read(0xDC0F), played.Bus.Read(0xDC0F));

        AlignToInstruction(reference);
        AlignToInstruction(played);
        ArmAbsLoadStore(reference, 0xDC0F, 0xC100);
        ArmAbsLoadStore(played, 0xDC0F, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        AssertCpu(reference, 0xC006);
        AssertCpu(played, 0xC006);
        Assert.Equal(0x08, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The first key inside a periodic frame is recorded after
    /// that frame's cycle 0.
    /// Acceptance: A-down at frame 0, 196660 cycles, B-down at frame 10
    /// cycle 100, then Seek(10) stops at cycle 0 of frame 10 (196560).
    /// A snapshot labeled frame 10 but captured 100 cycles later is not
    /// that boundary. Y matches a reference that held A and stepped 196560
    /// cycles. AC3 and AC8.
    /// </summary>
    [Fact]
    public void Seek_IgnoresPeriodicSlotCapturedAfterTheBoundary()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var frameCycles = FrameCycles(recorded);
        recorded.Clock.Step((int)(frameCycles * 10 + 100));
        store.RecordKey(10, 100, "B", true, recorded);

        var sought = MachineTestFactory.CreateC64Machine();
        var frame = store.Seek(sought, 10);

        var reference = MachineTestFactory.CreateC64Machine();
        SetKey(reference, "A", true);
        reference.Clock.Step((int)(frameCycles * 10));

        Assert.Equal(10, frame);
        Assert.Equal(frameCycles * 10, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, sought.Clock.TotalCycles);
        Assert.Equal(reference.GetState().Y, sought.GetState().Y);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A completed CIA serial byte is captured while character
    /// ROM is mapped over I/O.
    /// Acceptance: Eight CNT edges latch SDR $A5. Banking with $0001=$33,
    /// then Play, then $0001=$37, still reads $A5. LDA $DC0C / STA $C100
    /// stores $A5 after eight cycles. Playback $00 is a miss. VICE saves
    /// SDR in the CIA snapshot. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsCiaSdrWhenIoIsBankedOut()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        FeedSerialBits(recorded, 1, 0, 1, 0, 0, 1, 0, 1);
        FeedSerialBits(reference, 1, 0, 1, 0, 0, 1, 0, 1);
        recorded.Bus.Write(0x0001, 0x33);
        reference.Bus.Write(0x0001, 0x33);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        reference.Bus.Write(0x0001, 0x37);
        played.Bus.Write(0x0001, 0x37);

        Assert.Equal(0xA5, reference.Bus.Read(0xDC0C));
        Assert.Equal(reference.Bus.Read(0xDC0C), played.Bus.Read(0xDC0C));

        ArmSdrStore(reference);
        ArmSdrStore(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(0xA5, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Color RAM is written, then character ROM is mapped over
    /// I/O before the snapshot.
    /// Acceptance: $D800=$07 survives Play. After $0001=$37 the low nibble
    /// is 7, not the power-up nibble 14. LDA $D800 / STA $C100 and a second
    /// copy, 16 cycles, store that 7. VICE saves color RAM beside the VIC-II
    /// state. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsColorRamWhenIoIsBankedOut()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        recorded.Bus.Write(0xD800, 0x07);
        reference.Bus.Write(0xD800, 0x07);
        recorded.Bus.Write(0x0001, 0x33);
        reference.Bus.Write(0x0001, 0x33);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        reference.Bus.Write(0x0001, 0x37);
        played.Bus.Write(0x0001, 0x37);

        Assert.Equal(0x07, reference.Bus.Read(0xD800) & 0x0F);
        Assert.Equal(reference.Bus.Read(0xD800) & 0x0F, played.Bus.Read(0xD800) & 0x0F);

        // LDA $D800 / STA $C100 / LDA $D801 / STA $C101 is 16 cycles.
        ArmAbsLoadStore(reference, 0xD800, 0xC100);
        ArmAbsLoadStore(played, 0xD800, 0xC100);
        WriteProgramTail(reference);
        WriteProgramTail(played);
        reference.Clock.Step(16);
        played.Clock.Step(16);

        Assert.Equal(0x07, reference.Bus.Peek(0xC100) & 0x0F);
        Assert.Equal(reference.Bus.Peek(0xC100) & 0x0F, played.Bus.Peek(0xC100) & 0x0F);
        Assert.Equal(16L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: CIA time-of-day is written, then character ROM covers I/O
    /// before the snapshot.
    /// Acceptance: $DC0B=$01, $DC0A=$23, $DC09=$45, $DC08=$06 survive Play.
    /// After $0001=$37 the four registers read 06, 45, 23, 01. LDA $DC08 /
    /// STA $C100 stores $06 after eight cycles. Playback $00 is a miss.
    /// VICE ciacore saves the TOD registers. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsCiaTodWhenIoIsBankedOut()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        WriteTod(recorded, 0x06, 0x45, 0x23, 0x01);
        WriteTod(reference, 0x06, 0x45, 0x23, 0x01);
        recorded.Bus.Write(0x0001, 0x33);
        reference.Bus.Write(0x0001, 0x33);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        reference.Bus.Write(0x0001, 0x37);
        played.Bus.Write(0x0001, 0x37);

        Assert.Equal(0x06, reference.Bus.Read(0xDC08));
        Assert.Equal(0x45, reference.Bus.Read(0xDC09));
        Assert.Equal(0x23, reference.Bus.Read(0xDC0A));
        Assert.Equal(0x01, reference.Bus.Read(0xDC0B));
        Assert.Equal(reference.Bus.Read(0xDC08), played.Bus.Read(0xDC08));
        Assert.Equal(reference.Bus.Read(0xDC09), played.Bus.Read(0xDC09));
        Assert.Equal(reference.Bus.Read(0xDC0A), played.Bus.Read(0xDC0A));
        Assert.Equal(reference.Bus.Read(0xDC0B), played.Bus.Read(0xDC0B));

        ArmAbsLoadStore(reference, 0xDC08, 0xC100);
        ArmAbsLoadStore(played, 0xDC08, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(0x06, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A CIA serial output byte is in the middle of shifting when
    /// recording starts.
    /// Acceptance: $DC04=$02, $DC05=$00, $DC0C=$A5, $DC0E=$41, four cycles,
    /// then Play. SP and CNT match the reference. Twenty-one more cycles
    /// leave ICR $09, not $01, at cycle 25. VICE saves the serial shifter
    /// and bit count. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsCiaSerialOutputTransfer()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmSerialOutputInViceOrder(recorded);
        ArmSerialOutputInViceOrder(reference);
        recorded.Clock.Step(4);
        reference.Clock.Step(4);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 4, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        var referenceCia = Cia1(reference);
        var playedCia = Cia1(played);
        Assert.True(referenceCia.SerialPortPin);
        Assert.Equal(referenceCia.SerialPortPin, playedCia.SerialPortPin);
        Assert.False(referenceCia.SerialClockPin);
        Assert.Equal(referenceCia.SerialClockPin, playedCia.SerialClockPin);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A CIA serial input byte is unfinished and SP is still high
    /// when recording starts.
    /// Acceptance: Bits 1,0,1 leave SP high. Playback keeps that pin. Five
    /// more CNT edges, without writing SP again, latch SDR $BF. Supplying
    /// the later bits explicitly does not cover a held pin. Playback $A0
    /// is a miss. VICE snapshots sp_in_state. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsHeldCiaSerialInput()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        FeedSerialBits(recorded, 1, 0, 1);
        FeedSerialBits(reference, 1, 0, 1);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.True(Cia1(reference).SerialDataIn);
        Assert.Equal(Cia1(reference).SerialDataIn, Cia1(played).SerialDataIn);

        PulseCnt(reference, 5);
        PulseCnt(played, 5);
        ArmSdrStore(reference);
        ArmSdrStore(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(0xBF, reference.Bus.Read(0xDC0C));
        Assert.Equal(0xBF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Read(0xDC0C), played.Bus.Read(0xDC0C));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A CIA timer IRQ has already been latched when recording starts.
    /// Acceptance: 64 NOPs at $C000, I flag clear, timer A $0002 started with
    /// the IRQ enabled, six cycles, then four more after playback. The IRQ
    /// was asserted at cycle 6 and the delay count is 4 at cycle 10. The
    /// CPU matches that reference: PC $C004, S $FF, and $0100 holds $C0.
    /// A replay that latches the IRQ one cycle later is a miss. VICE saves
    /// irq_clk and irq_delay_cycles. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsPendingIrqTiming()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmPendingIrq(recorded);
        ArmPendingIrq(reference);
        recorded.Clock.Step(6);
        reference.Clock.Step(6);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 6, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);
        reference.Clock.Step(4);
        played.Clock.Step(4);

        var referenceClock = (SystemClock)reference.Clock;
        var playedClock = (SystemClock)played.Clock;
        Assert.Equal(10L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(6L, referenceClock.DebugIrqAssertCycle);
        Assert.Equal(referenceClock.DebugIrqAssertCycle, playedClock.DebugIrqAssertCycle);
        Assert.Equal(4, referenceClock.DebugIrqDelayCycles);
        Assert.Equal(referenceClock.DebugIrqDelayCycles, playedClock.DebugIrqDelayCycles);
        Assert.Equal(0xC004, reference.GetState().PC);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(0xFF, reference.GetState().S);
        Assert.Equal(reference.GetState().S, played.GetState().S);
        Assert.Equal(0xC0, reference.Bus.Peek(0x0100));
        Assert.Equal(reference.Bus.Peek(0x0100), played.Bus.Peek(0x0100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Reading the CIA ICR clears the pending IRQ, and the raise
    /// pipeline has not fired again when recording starts.
    /// Acceptance: Arm the timer IRQ, step four cycles, read $DC0D, step
    /// once, then play. Five more cycles leave PC $C005, S $00, and $0100
    /// $00. The IRQ assert cycle is 7 and the delay is 3. Restoring the
    /// flags without the raise delay takes the interrupt early. VICE keeps
    /// the CIA IRQ delay. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsCiaIrqPipelineAfterIcrRead()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmPendingIrq(recorded);
        ArmPendingIrq(reference);
        recorded.Clock.Step(4);
        reference.Clock.Step(4);
        _ = recorded.Bus.Read(0xDC0D);
        _ = reference.Bus.Read(0xDC0D);
        recorded.Clock.Step(1);
        reference.Clock.Step(1);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 5, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);
        reference.Clock.Step(5);
        played.Clock.Step(5);

        var referenceClock = (SystemClock)reference.Clock;
        var playedClock = (SystemClock)played.Clock;
        Assert.Equal(10L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC005, reference.GetState().PC);
        Assert.Equal(0x00, reference.GetState().S);
        Assert.Equal(0x00, reference.Bus.Peek(0x0100));
        Assert.Equal(referenceClock.DebugIrqAssertCycle, playedClock.DebugIrqAssertCycle);
        Assert.Equal(referenceClock.DebugIrqDelayCycles, playedClock.DebugIrqDelayCycles);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(reference.GetState().S, played.GetState().S);
        Assert.Equal(reference.Bus.Peek(0x0100), played.Bus.Peek(0x0100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: SID volume was written, then character ROM covered I/O,
    /// when recording starts.
    /// Acceptance: The gated-noise stimulus runs 1000 cycles, $D418 is
    /// written $0F again, and $0001 is $33. After playback and 100 more
    /// cycles the volume is 15 and the sample matches. A banked-out window
    /// does not restore $D418. VICE saves the SID register file. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsSidVolumeWhenIoIsBankedOut()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        WriteGatedNoise(recorded);
        WriteGatedNoise(reference);
        recorded.Clock.Step(1000);
        reference.Clock.Step(1000);
        recorded.Bus.Write(0xD418, 0x0F);
        reference.Bus.Write(0xD418, 0x0F);
        recorded.Bus.Write(0x0001, 0x33);
        reference.Bus.Write(0x0001, 0x33);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 1000, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);
        reference.Clock.Step(100);
        played.Clock.Step(100);

        Assert.Equal(1100L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(15, Sid(reference).MasterVolume);
        Assert.Equal(Sid(reference).MasterVolume, Sid(played).MasterVolume);
        Assert.Equal(Sid(reference).GenerateSample(), Sid(played).GenerateSample());
        reference.Bus.Write(0x0001, 0x37);
        played.Bus.Write(0x0001, 0x37);
        Assert.Equal(reference.Bus.Read(0xD41B), played.Bus.Read(0xD41B));
        Assert.Equal(reference.Bus.Read(0xD41C), played.Bus.Read(0xD41C));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: An 8580 Fast-mode register write is still in the one-cycle
    /// pipeline when recording starts.
    /// Acceptance: On c64c, write $D413=$00, $D414=$F0, $D412=$21, $D418=$0F,
    /// set SamplingMethod=Fast, then write $D40F=$20. Record A at cycle 0
    /// and play into a Fast playback machine. The reference pipeline slot
    /// is 1. Playback 0 is a FAIL. After 100 cycles both clocks are 100,
    /// reference $D41B is 97, and the samples match. Playback $D41B 85 is a
    /// FAIL. VICE SID::write_state saves write_pipeline and write_address.
    /// AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsPending8580FastWrite()
    {
        var recorded = MachineTestFactory.CreateC64Machine("c64c");
        var reference = MachineTestFactory.CreateC64Machine("c64c");
        var played = MachineTestFactory.CreateC64Machine("c64c");
        ArmPending8580FastWrite(recorded);
        ArmPending8580FastWrite(reference);
        Sid(played).SamplingMethod = SidSamplingMethod.Fast;

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(1, Sid(reference).PipelinedWriteSlot);
        Assert.Equal(Sid(reference).PipelinedWriteSlot, Sid(played).PipelinedWriteSlot);

        reference.Clock.Step(100);
        played.Clock.Step(100);

        Assert.Equal(100L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(97, reference.Bus.Read(0xD41B));
        Assert.Equal(reference.Bus.Read(0xD41B), played.Bus.Read(0xD41B));
        Assert.Equal(Sid(reference).GenerateSample(), Sid(played).GenerateSample());
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A key is already down before the first recorded event.
    /// Acceptance: B is held, then A is recorded and played. Column scan
    /// reads $DC01 $EB, and LDA $DC01; STA $C100 stores $EB. Replaying only
    /// the event list drops B. VICE snapshots the keyboard matrix. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsKeysHeldBeforeRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        SetKey(recorded, "B", true);
        SetKey(reference, "B", true);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        SelectKeyboardColumns(reference);
        SelectKeyboardColumns(played);
        Assert.Equal(0xEB, reference.Bus.Read(0xDC01));
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        ArmAbsLoadStore(reference, 0xDC01, 0xC100);
        ArmAbsLoadStore(played, 0xDC01, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(0xEB, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Joystick fire is already held before the first recorded key.
    /// Acceptance: Port 2 fire is down, then A is recorded and played.
    /// $DC00 reads $EF, and LDA $DC00; STA $C100 stores $EF. Both clocks
    /// are 8. Playback $FF drops the button. VICE snapshots joystick_value. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsJoystickHeldBeforeRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        SetJoystickFire(recorded);
        SetJoystickFire(reference);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(0xEF, reference.Bus.Read(0xDC00));
        Assert.Equal(reference.Bus.Read(0xDC00), played.Bus.Read(0xDC00));

        ArmAbsLoadStore(reference, 0xDC00, 0xC100);
        ArmAbsLoadStore(played, 0xDC00, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xEF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Port 2 fire goes down while a recording is already open.
    /// Acceptance: A at cycle 0, fire at cycle 30, B at cycle 60. The replay
    /// text has three inputs and the joystick event at cycle 30. At cycle 60
    /// $DC00 is $EF. LDA $DC00; STA $C100 stores $EF and both clocks are 68.
    /// Playback $FF drops the button. VICE records EVENT_JOYSTICK_VALUE. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysJoystickPressedDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        SetJoystickFire(recorded);
        SetJoystickFire(reference);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);
        SetKey(reference, "B", true);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input joystick port=2 direction=0 fire=1 frame=0 cycle=30", replay, StringComparison.Ordinal);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xEF, reference.Bus.Read(0xDC00));
        Assert.Equal(reference.Bus.Read(0xDC00), played.Bus.Read(0xDC00));

        ArmAbsLoadStore(reference, 0xDC00, 0xC100);
        ArmAbsLoadStore(played, 0xDC00, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xEF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The 1351 mouse has already moved before the first recorded key.
    /// Acceptance: ApplyMouseDelta(1, 10, 0) on a fresh PAL machine, then record
    /// and play A. $D419 is $4A and $D41A is $40. LDA $D419; STA $C100 stores
    /// $4A. Both clocks are 8. Playback $FF drops the pot. VICE
    /// write_mouse_common_snapshot saves the latest mouse position. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsMouseHeldBeforeRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ApplyMouseDelta(recorded, 1, 10, 0);
        ApplyMouseDelta(reference, 1, 10, 0);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.Equal(0x4A, reference.Bus.Read(0xD419));
        Assert.Equal(0x40, reference.Bus.Read(0xD41A));
        Assert.Equal(reference.Bus.Read(0xD419), played.Bus.Read(0xD419));
        Assert.Equal(reference.Bus.Read(0xD41A), played.Bus.Read(0xD41A));

        ArmAbsLoadStore(reference, 0xD419, 0xC100);
        ArmAbsLoadStore(played, 0xD419, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x4A, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The mouse moves while a recording is already open.
    /// Acceptance: A at cycle 0, ApplyMouseDelta(1, 10, 0) at cycle 30, B at
    /// cycle 60. The replay text has three inputs and the mouse event at
    /// cycle 30. At cycle 60 $D419 is $4A and $D41A is $40. LDA $D419;
    /// STA $C100 stores $4A and both clocks are 68. Playback $FF drops the
    /// pot. VICE records the mouse position change. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysMouseMovedDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        ApplyMouseDelta(recorded, 1, 10, 0);
        ApplyMouseDelta(reference, 1, 10, 0);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);
        SetKey(reference, "B", true);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input mouse port=1 dx=10 dy=0 frame=0 cycle=30", replay, StringComparison.Ordinal);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x4A, reference.Bus.Read(0xD419));
        Assert.Equal(0x40, reference.Bus.Read(0xD41A));
        Assert.Equal(reference.Bus.Read(0xD419), played.Bus.Read(0xD419));
        Assert.Equal(reference.Bus.Read(0xD41A), played.Bus.Read(0xD41A));

        ArmAbsLoadStore(reference, 0xD419, 0xC100);
        ArmAbsLoadStore(played, 0xD419, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x4A, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The left mouse button goes down while a recording is open.
    /// Acceptance: A at cycle 0, SetMouseButtons(1, true, false) at cycle 30,
    /// B at cycle 60. The replay text has three inputs and the port-1 fire
    /// event at cycle 30. With no keyboard column selected, $DC01 is $EF.
    /// LDA $DC01; STA $C100 stores $EF and both clocks are 68. Playback $FF
    /// drops the button. The left button is joystick fire. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysMouseButtonPressedDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        SetMouseButtons(recorded, 1, true, false);
        SetMouseButtons(reference, 1, true, false);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);
        SetKey(reference, "B", true);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input joystick port=1 direction=0 fire=1 frame=0 cycle=30", replay, StringComparison.Ordinal);

        SelectNoKeyboardColumns(reference);
        SelectNoKeyboardColumns(played);
        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xEF, reference.Bus.Read(0xDC01));
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        ArmAbsLoadStore(reference, 0xDC01, 0xC100);
        ArmAbsLoadStore(played, 0xDC01, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xEF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Single-step replay on the machine that recorded the mouse.
    /// Acceptance: A at cycle 0, ApplyMouseDelta(1, 10, 0) at cycle 30, B at
    /// cycle 60. Seek that same machine to frame 0, then Step twice. The
    /// replay still has three inputs and one mouse line. The clock is 60 and
    /// $D419 is $4A. A second copy of the delta, or POTX $54 at cycle 30, is
    /// a replay that recorded itself. AC3, AC4, AC5.
    /// </summary>
    [Fact]
    public void Step_DoesNotRerecordMouseMovedDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(30);
        ApplyMouseDelta(recorded, 1, 10, 0);
        recorded.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);

        Assert.Equal(0, store.Seek(recorded, 0));
        store.Step();
        store.Step();
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.DoesNotContain("inputCount=4", replay, StringComparison.Ordinal);
        Assert.DoesNotContain("inputCount=5", replay, StringComparison.Ordinal);
        Assert.Equal(1, replay.Split('\n').Count(static line => line.StartsWith("input mouse ", StringComparison.Ordinal)));
        Assert.Equal(60L, recorded.Clock.TotalCycles);
        Assert.Equal(0x4A, recorded.Bus.Read(0xD419));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The paddle pots are written while a recording is already open.
    /// Acceptance: A at cycle 0, SetPotState(1, $4A, $40) at cycle 30, B at
    /// cycle 60. The replay text has three inputs and the pot event at cycle
    /// 30. At cycle 60 $D419 is $4A and $D41A is $40. LDA $D419; STA $C100
    /// stores $4A and both clocks are 68. Playback $FF drops the pots. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysPotStateSetDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        SetPotState(recorded, 1, 0x4A, 0x40);
        SetPotState(reference, 1, 0x4A, 0x40);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);
        SetKey(reference, "B", true);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input pot port=1 x=74 y=64 frame=0 cycle=30", replay, StringComparison.Ordinal);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x4A, reference.Bus.Read(0xD419));
        Assert.Equal(0x40, reference.Bus.Read(0xD41A));
        Assert.Equal(reference.Bus.Read(0xD419), played.Bus.Read(0xD419));
        Assert.Equal(reference.Bus.Read(0xD41A), played.Bus.Read(0xD41A));

        ArmAbsLoadStore(reference, 0xD419, 0xC100);
        ArmAbsLoadStore(played, 0xD419, 0xC100);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x4A, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: RESTORE is pressed while a recording is already open.
    /// Acceptance: A at cycle 0, SetRestoreState(true) at cycle 30, B at
    /// cycle 60. The replay text has three inputs and the restore event at
    /// cycle 30. At cycle 60 RESTORE is down, PC is $0003, S is $FC, and
    /// the NMI return address is at $01FD/$01FE ($25/$EA). Playback that
    /// leaves RESTORE up and PC at $FDAE dropped the press. VICE records
    /// EVENT_KEYBOARD_RESTORE and replays it with machine_set_restore_key.
    /// AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysRestorePressedDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        SetRestoreState(recorded, true);
        SetRestoreState(reference, true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);
        SetKey(reference, "B", true);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input restore pressed=1 frame=0 cycle=30", replay, StringComparison.Ordinal);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.True(IsRestorePressed(reference));
        Assert.Equal(IsRestorePressed(reference), IsRestorePressed(played));
        Assert.Equal(0x0003, CpuOf(reference).PC);
        Assert.Equal(CpuOf(reference).PC, CpuOf(played).PC);
        Assert.Equal(0xFC, CpuOf(reference).S);
        Assert.Equal(CpuOf(reference).S, CpuOf(played).S);
        Assert.Equal(0x25, reference.Bus.Peek(0x01FD));
        Assert.Equal(reference.Bus.Peek(0x01FD), played.Bus.Peek(0x01FD));
        Assert.Equal(0xEA, reference.Bus.Peek(0x01FE));
        Assert.Equal(reference.Bus.Peek(0x01FE), played.Bus.Peek(0x01FE));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Single-step replay on the machine that recorded RESTORE.
    /// Acceptance: A at cycle 0, SetRestoreState(true) at cycle 30, B at
    /// cycle 60. Seek that same machine to frame 0, then Step twice. The
    /// replay still has three inputs and one restore line. The clock is 60,
    /// RESTORE is down, and PC is $0003. A second restore line is a replay
    /// that recorded itself. AC3, AC4, AC5.
    /// </summary>
    [Fact]
    public void Step_DoesNotRerecordRestorePressedDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(30);
        SetRestoreState(recorded, true);
        recorded.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);

        Assert.Equal(0, store.Seek(recorded, 0));
        store.Step();
        store.Step();
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.DoesNotContain("inputCount=4", replay, StringComparison.Ordinal);
        Assert.DoesNotContain("inputCount=5", replay, StringComparison.Ordinal);
        Assert.Equal(1, replay.Split('\n').Count(static line => line.StartsWith("input restore ", StringComparison.Ordinal)));
        Assert.Equal(60L, recorded.Clock.TotalCycles);
        Assert.True(IsRestorePressed(recorded));
        Assert.Equal(0x0003, CpuOf(recorded).PC);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The light-pen line is pulled low while a recording is open.
    /// Acceptance: A at cycle 0, SetLightPen(true) at cycle 1000, B at cycle
    /// 1030. The replay text has three inputs and the light-pen state event
    /// at cycle 1000. At cycle 1030 $D013 is $C6 and $D014 is $0F. LDA $D014;
    /// STA $C100; JMP $C000 for 40 cycles leaves both clocks at 1070 and
    /// stores $0F. Playback $00 drops the pen. VICE vicii_set_light_pen is
    /// the line input. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysLightPenSetDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(1000);
        reference.Clock.Step(1000);
        Vic(recorded).SetLightPen(true);
        Vic(reference).SetLightPen(true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 1030, "B", true, recorded);
        SetKey(reference, "B", true);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input lightpen state=1 frame=0 cycle=1000", replay, StringComparison.Ordinal);

        Assert.Equal(1030L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC6, reference.Bus.Read(0xD013));
        Assert.Equal(0x0F, reference.Bus.Read(0xD014));
        Assert.Equal(reference.Bus.Read(0xD013), played.Bus.Read(0xD013));
        Assert.Equal(reference.Bus.Read(0xD014), played.Bus.Read(0xD014));

        WriteLoadLightPen(reference);
        WriteLoadLightPen(played);
        reference.Clock.Step(40);
        played.Clock.Step(40);

        Assert.Equal(1070L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x0F, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The light pen is triggered while a recording is open.
    /// Acceptance: A at cycle 0, TriggerLightPen() at cycle 1000, B at cycle
    /// 1030. The replay text has three inputs and the light-pen trigger at
    /// cycle 1000. At cycle 1030 $D013 is $C0 and $D014 is $0F. The same
    /// $D014 load leaves both clocks at 1070 and stores $0F. Playback $00
    /// drops the trigger. VICE vicii_trigger_light_pen_internal latches X/Y.
    /// AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysLightPenTriggeredDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(1000);
        reference.Clock.Step(1000);
        Vic(recorded).TriggerLightPen();
        Vic(reference).TriggerLightPen();
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 1030, "B", true, recorded);
        SetKey(reference, "B", true);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input lightpen trigger frame=0 cycle=1000", replay, StringComparison.Ordinal);

        Assert.Equal(1030L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC0, reference.Bus.Read(0xD013));
        Assert.Equal(0x0F, reference.Bus.Read(0xD014));
        Assert.Equal(reference.Bus.Read(0xD013), played.Bus.Read(0xD013));
        Assert.Equal(reference.Bus.Read(0xD014), played.Bus.Read(0xD014));

        WriteLoadLightPen(reference);
        WriteLoadLightPen(played);
        reference.Clock.Step(40);
        played.Clock.Step(40);

        Assert.Equal(1070L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x0F, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Pointing-device light-pen timing is set while a recording is open.
    /// Acceptance: Record A-down at cycle 0. SetLightPenTiming(80, 0) at
    /// cycle 1000. Record B-down at cycle 2200. The replay has three inputs
    /// and a light-pen timing line at cycle 1000. At cycle 2200 reference
    /// and playback $D013 are $1C and $D014 are $20. LDA $D014; STA $C100;
    /// JMP $C000 for 40 cycles leaves both clocks at 2240 and stores $20.
    /// Playback $00 drops the timing. VICE vicii_lightpen_timing schedules
    /// the trigger from the host coordinates. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysLightPenTimingDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(1000);
        reference.Clock.Step(1000);
        Vic(recorded).SetLightPenTiming(80, 0);
        Vic(reference).SetLightPenTiming(80, 0);
        recorded.Clock.Step(1200);
        reference.Clock.Step(1200);
        store.RecordKey(0, 2200, "B", true, recorded);
        SetKey(reference, "B", true);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input lightpen timing x=80 y=0 frame=0 cycle=1000", replay, StringComparison.Ordinal);

        Assert.Equal(2200L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x1C, reference.Bus.Read(0xD013));
        Assert.Equal(0x20, reference.Bus.Read(0xD014));
        Assert.Equal(reference.Bus.Read(0xD013), played.Bus.Read(0xD013));
        Assert.Equal(reference.Bus.Read(0xD014), played.Bus.Read(0xD014));

        WriteLoadLightPen(reference);
        WriteLoadLightPen(played);
        reference.Clock.Step(40);
        played.Clock.Step(40);

        Assert.Equal(2240L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x20, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Single-step replay on the machine that recorded the light pen.
    /// Acceptance: A at cycle 0, SetLightPen(true) at cycle 1000, B at cycle
    /// 1030. Seek that same machine to frame 0, then Step twice. The replay
    /// still has three inputs and one light-pen state line. The clock is 1030
    /// and $D014 is $0F. A second state line is a replay that recorded itself.
    /// AC3, AC4, AC5.
    /// </summary>
    [Fact]
    public void Step_DoesNotRerecordLightPenSetDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(1000);
        Vic(recorded).SetLightPen(true);
        recorded.Clock.Step(30);
        store.RecordKey(0, 1030, "B", true, recorded);

        Assert.Equal(0, store.Seek(recorded, 0));
        store.Step();
        store.Step();
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.DoesNotContain("inputCount=4", replay, StringComparison.Ordinal);
        Assert.DoesNotContain("inputCount=5", replay, StringComparison.Ordinal);
        Assert.Equal(1, replay.Split('\n').Count(static line => line.StartsWith("input lightpen ", StringComparison.Ordinal)));
        Assert.Equal(1030L, recorded.Clock.TotalCycles);
        Assert.Equal(0x0F, recorded.Bus.Read(0xD014));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A host key changes while a recording is open.
    /// Acceptance: A-down at cycle 0, SetKeyState(B, true) at cycle 30,
    /// A-up at cycle 60. The replay text has three inputs and
    /// input key=B pressed=1 at cycle 30. With every keyboard column
    /// selected, reference and playback $DC01 are $EF at cycle 60.
    /// LDA $DC01; STA $C100 for eight cycles leaves both clocks at 68
    /// and stores $EF. Playback $FF drops the key. VICE
    /// keyboard_event_record stores EVENT_KEYBOARD_MATRIX after the host
    /// key. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysKeyPressedDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        SetKey(recorded, "B", true);
        SetKey(reference, "B", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "A", false, recorded);
        SetKey(reference, "A", false);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input key=B pressed=1 frame=0 cycle=30", replay, StringComparison.Ordinal);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        SelectKeyboardColumns(reference);
        SelectKeyboardColumns(played);
        Assert.Equal(0xEF, reference.Bus.Read(0xDC01));
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xEF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A raw matrix release drops the host key that owned that code on the recording map.
    /// Acceptance: Recording and reference use gtk3_pos_de.vkm. Playback uses
    /// the default map. Y is held before recording. A-down at cycle 0,
    /// SetKey(0x0C, false) at cycle 30, SetKeyState(Y, true) at cycle 40,
    /// and A-up at cycle 60. At cycle 60 reference $DC01 is $EF. LDA $DC01;
    /// STA $C100 leaves both clocks at 68, both PCs at $C006, and stores $EF.
    /// The replay contains the raw release and codes=0C. AC3.
    /// </summary>
    [Fact]
    public void Playback_ReplaysHostKeyAfterRawReleaseOnRecordedMap()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        UseGermanPositionalMap(recorded);
        UseGermanPositionalMap(reference);
        SetKey(recorded, "Y", true);
        SetKey(reference, "Y", true);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        MatrixOf(recorded).SetKey(0x0C, false);
        MatrixOf(reference).SetKey(0x0C, false);
        recorded.Clock.Step(10);
        reference.Clock.Step(10);
        SetKey(recorded, "Y", true);
        SetKey(reference, "Y", true);
        recorded.Clock.Step(20);
        reference.Clock.Step(20);
        store.RecordKey(0, 60, "A", false, recorded);
        SetKey(reference, "A", false);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        SelectKeyboardColumns(reference);
        SelectKeyboardColumns(played);
        Assert.Equal(0xEF, reference.Bus.Read(0xDC01));
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        AssertCpu(reference, 0xC006);
        AssertCpu(played, 0xC006);
        Assert.Equal(0xEF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));

        var replay = store.SaveReplay();
        Assert.Contains("input key code=0C pressed=0 frame=0 cycle=30", replay, StringComparison.Ordinal);
        Assert.Contains("input key=Y pressed=1 frame=0 cycle=40 codes=0C", replay, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A matrix key pressed while a recording is open is replayed.
    /// Acceptance: A-down at cycle 0, IKeyboardMatrix.SetKey(0x1C, true) at
    /// cycle 30, A-up at cycle 60. The replay has inputCount=3 and the matrix
    /// line. With every column selected, reference $DC01 is $EF at cycle 60.
    /// LDA $DC01; STA $C100 leaves both clocks at 68 and stores $EF.
    /// AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysMatrixKeyPressedDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        MatrixOf(recorded).SetKey(0x1C, true);
        MatrixOf(reference).SetKey(0x1C, true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "A", false, recorded);
        SetKey(reference, "A", false);

        SelectKeyboardColumns(reference);
        Assert.Equal(0xEF, reference.Bus.Read(0xDC01));
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input key code=1C pressed=1 frame=0 cycle=30", replay, StringComparison.Ordinal);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        SelectKeyboardColumns(played);
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xEF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A matrix release before recording stays released.
    /// Acceptance: SetKeyState(A, true), then SetKey(0x0A, false), then
    /// B-down at cycle 0. With every column selected, reference $DC01 is
    /// $EF. Playback matches. LDA $DC01; STA $C100 stores $EF at cycle 8.
    /// AC3.
    /// </summary>
    [Fact]
    public void Playback_DropsMatrixReleasedHostKey()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        SetKey(recorded, "A", true);
        SetKey(reference, "A", true);
        MatrixOf(recorded).SetKey(0x0A, false);
        MatrixOf(reference).SetKey(0x0A, false);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "B", true, recorded);
        SetKey(reference, "B", true);
        SelectKeyboardColumns(reference);
        Assert.Equal(0xEF, reference.Bus.Read(0xDC01));

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SelectKeyboardColumns(played);
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xEF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A raw matrix hold survives a host release of the same key.
    /// Acceptance: Before recording, SetKey(0x0A, true) and SetKeyState(A,
    /// true). B-down at cycle 0, A-up at cycle 30, B-up at cycle 60. The
    /// replay has those three events. With every column selected, reference
    /// $DC01 is $FB at cycle 60. LDA $DC01; STA $C100 leaves both clocks at
    /// 68, both PCs at $C006, and stores $FB. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsRawMatrixOwnerBesideHostKey()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        MatrixOf(recorded).SetKey(0x0A, true);
        MatrixOf(reference).SetKey(0x0A, true);
        SetKey(recorded, "A", true);
        SetKey(reference, "A", true);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "B", true, recorded);
        SetKey(reference, "B", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        SetKey(recorded, "A", false);
        SetKey(reference, "A", false);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "B", false, recorded);
        SetKey(reference, "B", false);

        SelectKeyboardColumns(reference);
        Assert.Equal(0xFB, reference.Bus.Read(0xDC01));
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        SelectKeyboardColumns(played);
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        AssertCpu(reference, 0xC006);
        AssertCpu(played, 0xC006);
        Assert.Equal(0xFB, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A key held before recording keeps its recorded matrix position.
    /// Acceptance: Recording and reference use the shipped German positional
    /// map gtk3_pos_de.vkm. Playback uses the default map. Host Y is down
    /// before A-down at cycle 0. With every column selected, reference
    /// $DC01 is $EB. LDA $DC01; STA $C100 leaves both clocks at 8, both
    /// PCs at $C006, and stores $EB. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsBaselineKeyOnRecordedMap()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        UseGermanPositionalMap(recorded);
        UseGermanPositionalMap(reference);
        SetKey(recorded, "Y", true);
        SetKey(reference, "Y", true);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);

        SelectKeyboardColumns(reference);
        Assert.Equal(0xEB, reference.Bus.Read(0xDC01));

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        Assert.Equal(0L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        SelectKeyboardColumns(played);
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        AssertCpu(reference, 0xC006);
        AssertCpu(played, 0xC006);
        Assert.Equal(0xEB, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A key pressed during recording keeps its recorded matrix position.
    /// Acceptance: Recording and reference use gtk3_pos_de.vkm. Playback uses
    /// the default map. A-down at cycle 0, host Y at cycle 30, A-up at cycle
    /// 60. The replay has inputCount=3 and the Y-down line. With every column
    /// selected, reference $DC01 is $EF at cycle 60. LDA $DC01; STA $C100
    /// leaves both clocks at 68, both PCs at $C006, and stores $EF. AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_KeepsRecordedKeyOnRecordedMap()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        UseGermanPositionalMap(recorded);
        UseGermanPositionalMap(reference);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        SetKey(recorded, "Y", true);
        SetKey(reference, "Y", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "A", false, recorded);
        SetKey(reference, "A", false);

        SelectKeyboardColumns(reference);
        Assert.Equal(0xEF, reference.Bus.Read(0xDC01));
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input key=Y pressed=1 frame=0 cycle=30", replay, StringComparison.Ordinal);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        SelectKeyboardColumns(played);
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        AssertCpu(reference, 0xC006);
        AssertCpu(played, 0xC006);
        Assert.Equal(0xEF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: RecordKey keeps the recording map's matrix position.
    /// Acceptance: Recording and reference use gtk3_pos_de.vkm. Playback
    /// uses the default map. RecordKey Y-down at cycle 0. With every column
    /// selected, reference $DC01 is $EF. LDA $DC01; STA $C100 leaves both
    /// clocks at 8, both PCs at $C006, and stores $EF. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsDirectRecordKeyOnRecordedMap()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        UseGermanPositionalMap(recorded);
        UseGermanPositionalMap(reference);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "Y", true, recorded);
        SetKey(reference, "Y", true);
        SelectKeyboardColumns(reference);
        Assert.Equal(0xEF, reference.Bus.Read(0xDC01));

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        Assert.Equal(0L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        SelectKeyboardColumns(played);
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        AssertCpu(reference, 0xC006);
        AssertCpu(played, 0xC006);
        Assert.Equal(0xEF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The saved replay names the matrix codes of a host key.
    /// Acceptance: One recording uses gtk3_pos_de.vkm and the other uses the
    /// default map. Each records A-down at cycle 0 and SetKeyState(Y, true)
    /// at cycle 30. At cycle 30 the German playback reads $DC01=$EB and the
    /// default playback reads $F9. The two SaveReplay texts differ and each
    /// contains codes=. AC4.
    /// </summary>
    [Fact]
    public void SaveReplay_KeepsHostKeyMatrixCodes()
    {
        var german = RecordObservedY("german");
        var standard = RecordObservedY("default");

        Assert.Equal(0xEB, german.Port);
        Assert.Equal(0xF9, standard.Port);
        Assert.NotEqual(german.Replay, standard.Replay);
        Assert.Contains(" codes=", german.Replay, StringComparison.Ordinal);
        Assert.Contains(" codes=", standard.Replay, StringComparison.Ordinal);
    }

    private readonly record struct ObservedKeyReplay(string Replay, byte Port);

    private static ObservedKeyReplay RecordObservedY(string mapName)
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var played = MachineTestFactory.CreateC64Machine();
        if (mapName == "german")
        {
            UseGermanPositionalMap(recorded);
            UseGermanPositionalMap(played);
        }

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(30);
        SetKey(recorded, "Y", true);
        var replay = store.SaveReplay();
        store.Play(played);
        SelectKeyboardColumns(played);
        Assert.Equal(30L, played.Clock.TotalCycles);
        return new ObservedKeyReplay(replay, played.Bus.Read(0xDC01));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Single-step replay on the machine that recorded a host key.
    /// Acceptance: A-down at cycle 0, SetKeyState(B, true) at cycle 30,
    /// A-up at cycle 60. Seek that same machine to frame 0, then Step
    /// twice. The replay still has three inputs and one B-down line. The
    /// clock is 60 and, with columns selected, $DC01 is $EF. A second
    /// B-down line is a replay that recorded itself. AC3, AC4, AC5.
    /// </summary>
    [Fact]
    public void Step_DoesNotRerecordKeyPressedDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(30);
        SetKey(recorded, "B", true);
        recorded.Clock.Step(30);
        store.RecordKey(0, 60, "A", false, recorded);

        Assert.Equal(0, store.Seek(recorded, 0));
        store.Step();
        store.Step();
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.DoesNotContain("inputCount=4", replay, StringComparison.Ordinal);
        Assert.DoesNotContain("inputCount=5", replay, StringComparison.Ordinal);
        Assert.Equal(1, replay.Split('\n').Count(static line => line.StartsWith("input key=B ", StringComparison.Ordinal)));
        Assert.Equal(60L, recorded.Clock.TotalCycles);
        SelectKeyboardColumns(recorded);
        Assert.Equal(0xEF, recorded.Bus.Read(0xDC01));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: LeftShift is released while F2 still holds the shared matrix key.
    /// Acceptance: Record A-down at cycle 0. SetKeyState LeftShift-down at 10,
    /// F2-down at 20, LeftShift-up at 30, and F2-up at 40. Record A-up at 60.
    /// The replay contains input key=LeftShift pressed=0 at cycle 30.
    /// With every column selected, reference and playback $DC01 are $FF at
    /// cycle 60. LDA $DC01; STA $C100 leaves both clocks at 68 and stores
    /// $FF. Playback $7F leaves Shift stuck. A second SetKeyState of a key
    /// that is already down does not add another line. VICE
    /// keyboard_event_record stores the matrix after the host key, so a
    /// release is kept even when another host key still holds the column.
    /// AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysOverlappingModifierRelease()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(10);
        reference.Clock.Step(10);
        SetKey(recorded, "LeftShift", true);
        SetKey(reference, "LeftShift", true);
        recorded.Clock.Step(10);
        reference.Clock.Step(10);
        SetKey(recorded, "F2", true);
        SetKey(reference, "F2", true);
        recorded.Clock.Step(10);
        reference.Clock.Step(10);
        SetKey(recorded, "LeftShift", false);
        SetKey(reference, "LeftShift", false);
        recorded.Clock.Step(10);
        reference.Clock.Step(10);
        SetKey(recorded, "F2", false);
        SetKey(reference, "F2", false);
        recorded.Clock.Step(20);
        reference.Clock.Step(20);
        store.RecordKey(0, 60, "A", false, recorded);
        SetKey(reference, "A", false);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=6", replay, StringComparison.Ordinal);
        Assert.Contains("input key=LeftShift pressed=0 frame=0 cycle=30", replay, StringComparison.Ordinal);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        SelectKeyboardColumns(reference);
        SelectKeyboardColumns(played);
        Assert.Equal(0xFF, reference.Bus.Read(0xDC01));
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xFF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: LeftShift and F2 are already held when recording starts, then only F2 is released.
    /// Acceptance: Hold both before RecordKey. Record A-down at cycle 0,
    /// release F2 at cycle 30, and record A-up at cycle 60. The replay has
    /// three inputs, including F2-up at cycle 30. With every column selected,
    /// reference and playback $DC01 are $7F at cycle 60. LDA $DC01; STA $C100
    /// leaves both clocks at 68 and stores $7F. Playback $FF dropped Shift.
    /// VICE snapshots keyarr and restores that matrix, so two host keys that
    /// share a column do not collapse to one owner. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsOverlappingModifiersHeldBeforeRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        SetKey(recorded, "LeftShift", true);
        SetKey(reference, "LeftShift", true);
        SetKey(recorded, "F2", true);
        SetKey(reference, "F2", true);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        SetKey(recorded, "F2", false);
        SetKey(reference, "F2", false);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "A", false, recorded);
        SetKey(reference, "A", false);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input key=F2 pressed=0 frame=0 cycle=30", replay, StringComparison.Ordinal);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        SelectKeyboardColumns(reference);
        SelectKeyboardColumns(played);
        Assert.Equal(0x7F, reference.Bus.Read(0xDC01));
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x7F, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A second direct RecordKey of a key that is already down is not another owner.
    /// Acceptance: RecordKey A-down at cycles 0 and 30, then A-up at cycle 60.
    /// The reference uses SetKeyState for the same levels. At cycle 60 both
    /// $DC01 reads are $FF. LDA $DC01; STA $C100 leaves both clocks at 68,
    /// both PCs at $C006, and stores $FF. The saved replay has one A-down. AC3.
    /// </summary>
    [Fact]
    public void Playback_ReleasesDuplicateDirectRecordKey()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 30, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "A", false, recorded);
        SetKey(reference, "A", false);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        SelectKeyboardColumns(reference);
        SelectKeyboardColumns(played);
        Assert.Equal(0xFF, reference.Bus.Read(0xDC01));
        Assert.Equal(reference.Bus.Read(0xDC01), played.Bus.Read(0xDC01));

        WriteLoadCiaPortB(reference);
        WriteLoadCiaPortB(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        AssertCpu(reference, 0xC006);
        AssertCpu(played, 0xC006);
        Assert.Equal(0xFF, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));

        var replay = store.SaveReplay();
        Assert.Contains("inputCount=2", replay, StringComparison.Ordinal);
        Assert.Equal(1, replay.Split('\n').Count(static line => line.StartsWith("input key=A pressed=1", StringComparison.Ordinal)));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The same host key is reported down again while it is already down.
    /// Acceptance: Record A-down at cycle 0, SetKeyState(A, true) again at
    /// cycle 30, and record A-up at cycle 60. The replay stays at two inputs
    /// and has one A-down line. A repeat of the same matrix level is not a
    /// second event. AC4.
    /// </summary>
    [Fact]
    public void Recording_SkipsRedundantMatrixLevelRepeat()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(30);
        SetKey(recorded, "A", true);
        recorded.Clock.Step(30);
        store.RecordKey(0, 60, "A", false, recorded);

        var replay = store.SaveReplay();
        Assert.Contains("inputCount=2", replay, StringComparison.Ordinal);
        Assert.DoesNotContain("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Equal(1, replay.Split('\n').Count(static line => line.StartsWith("input key=A pressed=1", StringComparison.Ordinal)));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A CIA serial output byte must follow the VICE half-cycle
    /// shift, including across playback.
    /// Acceptance: Output mode is selected before the SDR write. The first
    /// output phase presents the MSB on SP and drives CNT low. A later
    /// phase drives CNT high. Playback matches both pins. Sending the same
    /// byte again without a new SDR write does not set ICR bit 3. VICE
    /// ciacore alternates CNT and requires fresh SDR data. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsCiaSerialOutputPhaseAndDoesNotRetransmit()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmSerialOutputInViceOrder(recorded);
        ArmSerialOutputInViceOrder(reference);
        recorded.Clock.Step(4);
        reference.Clock.Step(4);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 4, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        SetKey(reference, "A", true);

        Assert.True(Cia1(reference).SerialPortPin);
        Assert.False(Cia1(reference).SerialClockPin);
        Assert.Equal(Cia1(reference).SerialPortPin, Cia1(played).SerialPortPin);
        Assert.Equal(Cia1(reference).SerialClockPin, Cia1(played).SerialClockPin);

        var sawHigh = false;
        for (var i = 0; i < 6; i++)
        {
            reference.Clock.Step(1);
            played.Clock.Step(1);
            Assert.Equal(Cia1(reference).SerialClockPin, Cia1(played).SerialClockPin);
            if (Cia1(reference).SerialClockPin)
                sawHigh = true;
        }

        Assert.True(sawHigh);

        var completion = 0;
        for (var i = 0; i < 80 && (reference.Bus.Peek(0xDC0D) & 0x08) == 0; i++)
        {
            reference.Clock.Step(1);
            played.Clock.Step(1);
            completion++;
        }

        Assert.Equal(0x08, reference.Bus.Read(0xDC0D) & 0x08);
        Assert.Equal(0x08, played.Bus.Read(0xDC0D) & 0x08);
        for (var i = 0; i < completion + 4; i++)
        {
            reference.Clock.Step(1);
            played.Clock.Step(1);
        }

        Assert.Equal(0x00, reference.Bus.Read(0xDC0D) & 0x08);
        Assert.Equal(0x00, played.Bus.Read(0xDC0D) & 0x08);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A saved snapshot is loaded and restored onto a fresh machine.
    /// Acceptance: Processor-port latches survive serialize and deserialize.
    /// VICE writes pport data and direction before mem_ram. After restore,
    /// peek $0000/$0001 stays $2F/$37, RAM under those addresses keeps the
    /// power-up pattern, and $E000 still reads KERNAL.
    /// </summary>
    [Fact]
    public void SerializedRestore_KeepsProcessorPortLatches()
    {
        var source = MachineTestFactory.CreateC64Machine();
        var sourceRam = source.Devices.GetByRole(DeviceRole.SystemRam) as IMemory;
        Assert.NotNull(sourceRam);
        var kernal = source.Bus.Peek(0xE000);
        Assert.Equal(0x85, kernal);
        Assert.NotEqual(sourceRam!.Span[0xE000], kernal);
        Assert.Equal(0x2F, source.Bus.Peek(0x0000));
        Assert.Equal(0x37, source.Bus.Peek(0x0001));

        var store = new RuntimeSnapshotStore();
        var captured = (RuntimeSnapshot)store.Capture(source);
        var bytes = new byte[captured.GetSerializedSize()];
        captured.Serialize(bytes);
        Assert.Equal(16 + 65536, bytes.Length);

        var loaded = new RuntimeSnapshot();
        loaded.Deserialize(bytes);
        Assert.True(loaded.HasProcessorPort);
        Assert.Equal(0x2F, loaded.PortDirection);
        Assert.Equal(0x37, loaded.PortData);

        var destination = MachineTestFactory.CreateC64Machine();
        store.Restore(destination, loaded);

        var destinationRam = destination.Devices.GetByRole(DeviceRole.SystemRam) as IMemory;
        Assert.NotNull(destinationRam);
        Assert.Equal(sourceRam.Span[0], destinationRam!.Span[0]);
        Assert.Equal(sourceRam.Span[1], destinationRam.Span[1]);
        Assert.Equal(0x2F, destination.Bus.Peek(0x0000));
        Assert.Equal(0x37, destination.Bus.Peek(0x0001));
        Assert.Equal(kernal, destination.Bus.Peek(0xE000));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The replay file stores a snapshot reference and input
    /// events, not a full RAM image per frame.
    /// Acceptance: SaveReplay returns text that names the snapshot and is
    /// smaller than a 64KB dump.
    /// </summary>
    [Fact]
    public void ReplayFile_StoresSnapshotReferenceAndInputOnly()
    {
        var save = typeof(RuntimeSnapshotStore).GetMethod("SaveReplay");

        Assert.NotNull(save);
        var store = new RuntimeSnapshotStore();
        var text = save!.Invoke(store, null) as string;

        Assert.NotNull(text);
        Assert.Contains("snapshot", text, StringComparison.OrdinalIgnoreCase);
        Assert.True(text!.Length < 64 * 1024);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Replay can pause, single-step, and fast-forward.
    /// Acceptance: The store exposes Pause, Step, and FastForward.
    /// </summary>
    [Fact]
    public void Replay_CanPauseStepAndFastForward()
    {
        var store = new RuntimeSnapshotStore();
        var machine = MachineTestFactory.CreateC64Machine();
        store.RecordKey(0, 15, "C", true, machine);
        store.Pause();

        store.Step();

        var afterStep = machine.Clock.TotalCycles;
        Assert.True(afterStep >= 15);
        store.FastForward();
        Assert.True(machine.Clock.TotalCycles > afterStep);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: IReplayEngine provides record, playback, and seek.
    /// Acceptance: A production type named IReplayEngine declares those
    /// operations. A type declared by this test does not count.
    /// </summary>
    [Fact]
    public void IReplayEngine_ExposesRecordPlaybackAndSeek()
    {
        // The scan only sees assemblies already loaded. This fact does not
        // otherwise touch ViceSharp.Core, so a parallel run can miss
        // IReplayEngine. Load the production assembly first.
        _ = typeof(RuntimeSnapshotStore).Assembly;
        var engine = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly != typeof(FrSnp003AcceptanceTests).Assembly)
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(type => type.Name == "IReplayEngine");

        Assert.NotNull(engine);
        Assert.Contains(engine!.GetMethods(), method => method.Name.Contains("Record", StringComparison.Ordinal));
        Assert.Contains(engine.GetMethods(), method => method.Name.Contains("Play", StringComparison.Ordinal));
        Assert.Contains(engine.GetMethods(), method => method.Name.Contains("Seek", StringComparison.Ordinal));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Replay restores the Ocean bank VICE saves as currbank.
    /// Acceptance: A 32KB Ocean image has four banks filled with the bank
    /// number. Recording and reference select bank 1 with $DE00=$01 and run
    /// LDA $8000; STA $C100. After Play, playback $8000 is $01. After eight
    /// cycles both clocks are 8, both PCs are $C006, and $C100 is $01. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsOceanBank()
    {
        var image = BankBytes(4);
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        Cartridge(recorded).AttachCartridge(image, CartridgeMappingMode.Ocean);
        Cartridge(reference).AttachCartridge(image, CartridgeMappingMode.Ocean);
        recorded.Bus.Write(0xDE00, 0x01);
        reference.Bus.Write(0xDE00, 0x01);
        ArmAbsLoadStore(recorded, 0x8000, 0xC100);
        ArmAbsLoadStore(reference, 0x8000, 0xC100);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        Cartridge(played).AttachCartridge(image, CartridgeMappingMode.Ocean);
        store.Play(played);

        Assert.Equal(0x01, reference.Bus.Read(0x8000));
        Assert.Equal(reference.Bus.Read(0x8000), played.Bus.Read(0x8000));

        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        AssertCpu(reference, 0xC006);
        AssertCpu(played, 0xC006);
        Assert.Equal(0x01, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Replay restores a Magic Desk bank selected through $DE00.
    /// Acceptance: The same 32KB bank image and $DE00=$01 leave playback
    /// $8000 at $01, matching the reference. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsMagicDeskBank()
    {
        var image = BankBytes(4);
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        Cartridge(recorded).AttachCartridge(image, CartridgeMappingMode.MagicDesk);
        Cartridge(reference).AttachCartridge(image, CartridgeMappingMode.MagicDesk);
        recorded.Bus.Write(0xDE00, 0x01);
        reference.Bus.Write(0xDE00, 0x01);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        Cartridge(played).AttachCartridge(image, CartridgeMappingMode.MagicDesk);
        store.Play(played);

        Assert.Equal(0x01, reference.Bus.Read(0x8000));
        Assert.Equal(reference.Bus.Read(0x8000), played.Bus.Read(0x8000));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Seek back to the baseline restores an Ocean cartridge that
    /// was ejected after the recording started.
    /// Acceptance: 32KB Ocean, four banks filled with the bank number,
    /// $DE00=$01, LDA $8000; STA $C100; JMP $C000, I set. Record A at cycle
    /// 0, step 40, eject, Seek(0). $8000 is $01. After eight cycles both
    /// clocks are 8, both PCs are $C006, and $C100 is $01. $00 means the
    /// ROM image was not restored. AC2, AC3, AC8.
    /// </summary>
    [Fact]
    public void Seek_RestoresOceanImageAfterEject()
    {
        var image = BankBytes(4);
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        Cartridge(recorded).AttachCartridge(image, CartridgeMappingMode.Ocean);
        Cartridge(reference).AttachCartridge(image, CartridgeMappingMode.Ocean);
        recorded.Bus.Write(0xDE00, 0x01);
        reference.Bus.Write(0xDE00, 0x01);
        ArmOceanReadLoop(recorded);
        ArmOceanReadLoop(reference);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(40);
        Cartridge(recorded).EjectCartridge();
        Assert.Equal(0, store.Seek(recorded, 0));

        Assert.Equal(reference.Clock.TotalCycles, recorded.Clock.TotalCycles);
        Assert.Equal(0x01, reference.Bus.Read(0x8000));
        Assert.Equal(reference.Bus.Read(0x8000), recorded.Bus.Read(0x8000));

        reference.Clock.Step(8);
        recorded.Clock.Step(8);
        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, recorded.Clock.TotalCycles);
        AssertCpu(reference, 0xC006);
        AssertCpu(recorded, 0xC006);
        Assert.Equal(0x01, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), recorded.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Ejecting a cartridge while recording is an input event.
    /// Acceptance: The same Ocean image and program. Record A at cycle 0,
    /// step 30, eject on the recording and the reference, step to 60, record
    /// B. SaveReplay contains the eject at cycle 30 and inputCount=3. At
    /// cycle 68 both clocks are 68, both PCs are $C000, and $C100 is $00.
    /// Playback $01 keeps the cartridge. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysCartridgeEject()
    {
        var image = BankBytes(4);
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        Cartridge(recorded).AttachCartridge(image, CartridgeMappingMode.Ocean);
        Cartridge(reference).AttachCartridge(image, CartridgeMappingMode.Ocean);
        recorded.Bus.Write(0xDE00, 0x01);
        reference.Bus.Write(0xDE00, 0x01);
        ArmOceanReadLoop(recorded);
        ArmOceanReadLoop(reference);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        Cartridge(recorded).EjectCartridge();
        Cartridge(reference).EjectCartridge();
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);

        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input cartridge attached=0 frame=0 cycle=30", replay, StringComparison.Ordinal);

        var played = MachineTestFactory.CreateC64Machine();
        Cartridge(played).AttachCartridge(image, CartridgeMappingMode.Ocean);
        played.Bus.Write(0xDE00, 0x01);
        store.Play(played);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x00, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));

        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        AssertCpu(reference, 0xC000);
        AssertCpu(played, 0xC000);
        Assert.Equal(0x00, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Attaching a cartridge while recording is an input event.
    /// Acceptance: No cartridge at cycle 0. At cycle 30 attach an 8K image
    /// whose first byte is $5A. At cycle 60 $8000 is $5A on the reference
    /// and on playback. Playback $00 never mounted the image. AC1, AC3.
    /// </summary>
    [Fact]
    public void Playback_ReplaysCartridgeAttach()
    {
        var image = new byte[0x2000];
        image[0] = 0x5A;
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmOceanReadLoop(recorded);
        ArmOceanReadLoop(reference);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        Cartridge(recorded).AttachCartridge(image, CartridgeMappingMode.Standard8K);
        Cartridge(reference).AttachCartridge(image, CartridgeMappingMode.Standard8K);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x5A, reference.Bus.Read(0x8000));
        Assert.Equal(reference.Bus.Read(0x8000), played.Bus.Read(0x8000));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The saved replay identifies the cartridge attached during recording.
    /// Acceptance: Two Standard8K images attached at cycle 30 have different bytes
    /// and produce different saved replay text. Each replay contains its mapping and
    /// complete image value. AC4.
    /// </summary>
    [Fact]
    public void SaveReplay_KeepsAttachedCartridgeBytesAndMapping()
    {
        AssertCartridgeReplayValueContract(RecordCartridgeAttach);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The replay value contract is validated independently of the
    /// production serializer before implementing the missing behavior.
    /// Acceptance: A test double preserves the Standard8K mapping and complete
    /// cartridge bytes, and distinct cartridge values remain distinguishable. AC4.
    /// </summary>
    [Fact]
    public void SaveReplayValueContract_PassesAgainstTestDouble()
    {
        AssertCartridgeReplayValueContract(MockCartridgeReplay);
    }

    private static void AssertCartridgeReplayValueContract(Func<byte[], string> saveReplay)
    {
        var firstImage = new byte[0x2000];
        firstImage[0] = 0x5A;
        var secondImage = new byte[0x2000];
        secondImage[0] = 0xA5;

        var firstReplay = saveReplay(firstImage);
        var secondReplay = saveReplay(secondImage);

        Assert.NotEqual(firstReplay, secondReplay);
        Assert.Contains(
            "mapping=" + ((int)CartridgeMappingMode.Standard8K).ToString(System.Globalization.CultureInfo.InvariantCulture),
            firstReplay,
            StringComparison.Ordinal);
        Assert.Contains("bytes=" + Convert.ToHexString(firstImage), firstReplay, StringComparison.Ordinal);
        Assert.Contains("bytes=" + Convert.ToHexString(secondImage), secondReplay, StringComparison.Ordinal);
    }

    private static string MockCartridgeReplay(byte[] image)
    {
        return "input cartridge attached=1 frame=0 cycle=30 mapping="
            + ((int)CartridgeMappingMode.Standard8K).ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " bytes="
            + Convert.ToHexString(image);
    }

    private static string RecordCartridgeAttach(byte[] image)
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(30);
        Cartridge(recorded).AttachCartridge(image, CartridgeMappingMode.Standard8K);
        recorded.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);
        return store.SaveReplay();
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Observed host input creates the periodic snapshot for that frame.
    /// Acceptance: A-down at cycle 0, then SetKeyState(B, true) after ten
    /// PAL frames. SaveReplay contains B-down at frame 10 cycle 0 and
    /// slot frame=10. AC8.
    /// </summary>
    [Fact]
    public void ObservedKey_KeepsPeriodicSnapshot()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step((int)(FrameCycles(recorded) * 10));
        SetKey(recorded, "B", true);

        var replay = store.SaveReplay();
        Assert.Contains("input key=B pressed=1 frame=10 cycle=0", replay, StringComparison.Ordinal);
        Assert.Contains("slot frame=10", replay, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A mouse move that creates the periodic slot is already in
    /// that image. Seek must apply the delta once.
    /// Acceptance: LDA $D419; STA $C100; JMP $C000, I set, $D011=$00.
    /// Record A at cycle 0, step ten PAL frames, ApplyMouseDelta(1, 10, 0)
    /// on the recording and on an independent reference. Seek(10). POTX is
    /// $4A on both. After 22 more cycles the clocks match and $C100 is $4A.
    /// $54 means the delta was applied twice. AC3, AC8.
    /// </summary>
    [Fact]
    public void Seek_AppliesMouseDeltaOnceAtPeriodicSlot()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmPotReadLoop(recorded, 0xD419);
        ArmPotReadLoop(reference, 0xD419);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        var span = (int)(FrameCycles(recorded) * 10);
        recorded.Clock.Step(span);
        reference.Clock.Step(span);
        ApplyMouseDelta(recorded, 1, 10, 0);
        ApplyMouseDelta(reference, 1, 10, 0);

        var played = MachineTestFactory.CreateC64Machine();
        store.Seek(played, 10);

        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x4A, reference.Bus.Read(0xD419));
        Assert.Equal(reference.Bus.Read(0xD419), played.Bus.Read(0xD419));

        reference.Clock.Step(22);
        played.Clock.Step(22);

        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(CpuOf(reference).PC, CpuOf(played).PC);
        Assert.Equal(0x4A, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The next periodic slot has the same already-applied mouse
    /// delta on the other axis.
    /// Acceptance: The same loop reads $D41A. ApplyMouseDelta(1, 0, 10)
    /// after twenty PAL frames. Seek(20). POTY and $C100 are $4A. $54 means
    /// that later slot applied the delta twice. AC3, AC8.
    /// </summary>
    [Fact]
    public void Seek_AppliesMouseDeltaOnceOnTheNextSlot()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmPotReadLoop(recorded, 0xD41A);
        ArmPotReadLoop(reference, 0xD41A);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        var span = (int)(FrameCycles(recorded) * 20);
        recorded.Clock.Step(span);
        reference.Clock.Step(span);
        ApplyMouseDelta(recorded, 1, 0, 10);
        ApplyMouseDelta(reference, 1, 0, 10);

        var played = MachineTestFactory.CreateC64Machine();
        store.Seek(played, 20);

        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x4A, reference.Bus.Read(0xD41A));
        Assert.Equal(reference.Bus.Read(0xD41A), played.Bus.Read(0xD41A));

        reference.Clock.Step(22);
        played.Clock.Step(22);

        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(CpuOf(reference).PC, CpuOf(played).PC);
        Assert.Equal(0x4A, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A light-pen trigger that creates the periodic slot is not
    /// in that image yet. Seek must apply the trigger.
    /// Acceptance: LDA $D013; STA $C100; JMP $C000, I set. Record A at cycle
    /// 0, step ten PAL frames, TriggerLightPen on the recording and on an
    /// independent reference. Seek(10). $D013 is $E0 on both. After 22 more
    /// cycles the clocks are 196582, both PCs are $C000, and $C100 is $E0.
    /// Playback $00 drops the trigger. AC3, AC8.
    /// </summary>
    [Fact]
    public void Seek_KeepsLightPenTriggerAtPeriodicSlot()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmPotReadLoop(recorded, 0xD013);
        ArmPotReadLoop(reference, 0xD013);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        var span = (int)(FrameCycles(recorded) * 10);
        recorded.Clock.Step(span);
        reference.Clock.Step(span);
        Vic(recorded).TriggerLightPen();
        Vic(reference).TriggerLightPen();

        var played = MachineTestFactory.CreateC64Machine();
        store.Seek(played, 10);

        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xE0, reference.Bus.Read(0xD013));
        Assert.Equal(reference.Bus.Read(0xD013), played.Bus.Read(0xD013));

        reference.Clock.Step(22);
        played.Clock.Step(22);

        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(CpuOf(reference).PC, CpuOf(played).PC);
        Assert.Equal(0xE0, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The next periodic slot is created by the same pre-latch
    /// light-pen trigger.
    /// Acceptance: The same loop after twenty PAL frames. Seek(20). $D013
    /// and $C100 are $E0. Playback $00 drops that later trigger. AC3, AC8.
    /// </summary>
    [Fact]
    public void Seek_KeepsLightPenTriggerOnTheNextSlot()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        ArmPotReadLoop(recorded, 0xD013);
        ArmPotReadLoop(reference, 0xD013);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        var span = (int)(FrameCycles(recorded) * 20);
        recorded.Clock.Step(span);
        reference.Clock.Step(span);
        Vic(recorded).TriggerLightPen();
        Vic(reference).TriggerLightPen();

        var played = MachineTestFactory.CreateC64Machine();
        store.Seek(played, 20);

        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xE0, reference.Bus.Read(0xD013));
        Assert.Equal(reference.Bus.Read(0xD013), played.Bus.Read(0xD013));

        reference.Clock.Step(22);
        played.Clock.Step(22);

        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(CpuOf(reference).PC, CpuOf(played).PC);
        Assert.Equal(0xE0, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Seek uses periodic auto-snapshots taken during recording.
    /// Acceptance: Seek(machine, frame) returns the requested frame.
    /// </summary>
    [Fact]
    public void Seek_UsesPeriodicSnapshots()
    {
        var seek = typeof(RuntimeSnapshotStore).GetMethod("Seek");

        Assert.NotNull(seek);
        var machine = MachineTestFactory.CreateC64Machine();
        var before = machine.Clock.TotalCycles;
        var store = new RuntimeSnapshotStore();
        var frame = seek!.Invoke(store, [machine, 1]);

        Assert.Equal(1, frame);
        Assert.True(machine.Clock.TotalCycles > before);

        var slotted = MachineTestFactory.CreateC64Machine();
        var slottedBefore = slotted.Clock.TotalCycles;
        var slottedStore = new RuntimeSnapshotStore();
        slottedStore.RecordKey(0, 0, "A", true, slotted);
        var slottedFrame = slottedStore.Seek(slotted, 1);

        Assert.Equal(1, slottedFrame);
        Assert.True(slotted.Clock.TotalCycles > slottedBefore);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Seeking the same recorded frame twice returns to that
    /// execution point, including VIC-II and SID state.
    /// Acceptance: The second Seek(1) leaves the same cycle count, raster,
    /// oscillator, and frame buffer as the first seek and as an independent
    /// machine that stepped the same input.
    /// </summary>
    [Fact]
    public void Seek_RepeatedSeekReturnsToTheSameExecutionPoint()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, machine);

        store.Seek(machine, 1);
        var once = machine.Clock.TotalCycles;
        var raster = machine.Bus.Read(0xD012);
        var oscillator = machine.Bus.Read(0xD41B);
        var video = machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
        Assert.NotNull(video);
        var frame = video!.FrameBuffer.ToArray();

        store.Seek(machine, 1);

        Assert.True(once > 0);
        Assert.Equal(once, machine.Clock.TotalCycles);
        Assert.Equal(raster, machine.Bus.Read(0xD012));
        Assert.Equal(oscillator, machine.Bus.Read(0xD41B));
        Assert.Equal(frame, video.FrameBuffer);

        var reference = MachineTestFactory.CreateC64Machine();
        SetKey(reference, "A", true);
        reference.Clock.Step(once);
        var referenceVideo = reference.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
        Assert.NotNull(referenceVideo);
        Assert.Equal(reference.Clock.TotalCycles, machine.Clock.TotalCycles);
        Assert.Equal(reference.Bus.Read(0xD012), machine.Bus.Read(0xD012));
        Assert.Equal(reference.Bus.Read(0xD41B), machine.Bus.Read(0xD41B));
        Assert.Equal(referenceVideo!.CurrentRasterLine, video.CurrentRasterLine);
        Assert.Equal(referenceVideo.FrameBuffer, video.FrameBuffer);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Datasette PLAY is pressed while a recording is open.
    /// Acceptance: Record A-down at cycle 0. Set PlayPressed at cycle 30.
    /// Record B-down at cycle 60. The replay has three inputs and a tape
    /// play line at cycle 30. At cycle 60 reference and playback $0001 are
    /// $27 and PLAY is pressed. LDA $0001; STA $C100 leaves both clocks at
    /// 68 and stores $27. Playback $37 leaves PLAY released. VICE
    /// datasette_event_record stores EVENT_DATASETTE. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysDatasettePlayDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        Tape(recorded).PlayPressed = true;
        Tape(reference).PlayPressed = true;
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);
        SetKey(reference, "B", true);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input tape control=play pressed=1 frame=0 cycle=30", replay, StringComparison.Ordinal);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.True(Tape(reference).PlayPressed);
        Assert.Equal(Tape(reference).PlayPressed, Tape(played).PlayPressed);
        Assert.Equal(0x27, reference.Bus.Read(0x0001));
        Assert.Equal(reference.Bus.Read(0x0001), played.Bus.Read(0x0001));

        WriteLoadProcessorPort(reference);
        WriteLoadProcessorPort(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x27, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Datasette RECORD is pressed while a recording is open.
    /// Acceptance: Same schedule as PLAY, using RecordPressed. The replay
    /// contains a tape record line at cycle 30. At cycle 68 reference and
    /// playback $C100 are $27. RECORD asserts the same active-low sense
    /// line as PLAY. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysDatasetteRecordDuringRecording()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        Tape(recorded).RecordPressed = true;
        Tape(reference).RecordPressed = true;
        recorded.Clock.Step(30);
        reference.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);
        SetKey(reference, "B", true);

        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains("input tape control=record pressed=1 frame=0 cycle=30", replay, StringComparison.Ordinal);

        Assert.Equal(60L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.True(Tape(reference).RecordPressed);
        Assert.Equal(Tape(reference).RecordPressed, Tape(played).RecordPressed);
        Assert.Equal(0x27, reference.Bus.Read(0x0001));
        Assert.Equal(reference.Bus.Read(0x0001), played.Bus.Read(0x0001));

        WriteLoadProcessorPort(reference);
        WriteLoadProcessorPort(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(68L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x27, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Datasette PLAY is already held when recording starts.
    /// Acceptance: Press PLAY, then record A at cycle 0 and play into a
    /// fresh machine. PLAY stays pressed. $0001 is $27. LDA $0001; STA
    /// $C100 leaves both clocks at 8 and stores $27. Playback $37 released
    /// the button. The starting snapshot must keep the control. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsDatasettePlayHeldBeforeRecording()
    {
        AssertTapeHeldBeforeRecording(play: true);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Datasette RECORD is already held when recording starts.
    /// Acceptance: Same schedule as a held PLAY button. RECORD stays
    /// pressed and $C100 is $27 on reference and playback. AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsDatasetteRecordHeldBeforeRecording()
    {
        AssertTapeHeldBeforeRecording(play: false);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A periodic seek lands after a Datasette button was pressed.
    /// Acceptance: A-down at cycle 0, PLAY at cycle 30, B-down at frame 10
    /// cycle 0. The replay contains the tape line. Seek(10) returns 10 and
    /// both clocks are 196560. PLAY stays pressed and $0001 is $27.
    /// Playback $37 dropped the held button. AC3 and AC8.
    /// </summary>
    [Fact]
    public void Seek_KeepsDatasettePlayHeldAtPeriodicSlot()
    {
        AssertTapeHeldAtPeriodicSeek(play: true);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A periodic seek lands after Datasette RECORD was pressed.
    /// Acceptance: Same schedule as PLAY. RECORD stays pressed and $0001
    /// is $27 at cycle 196560. AC3 and AC8.
    /// </summary>
    [Fact]
    public void Seek_KeepsDatasetteRecordHeldAtPeriodicSlot()
    {
        AssertTapeHeldAtPeriodicSeek(play: false);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Seeking backward returns to the snapshot from before PLAY.
    /// Acceptance: A at cycle 0, PLAY at 30, B at 60, then Seek(0) on the
    /// recording machine. The clock is 0, PLAY is released, and $0001 is
    /// $37. Leaving PLAY down and $0001 at $27 keeps a future press. AC8.
    /// </summary>
    [Fact]
    public void Seek_ReleasesDatasettePlayWhenReturningToBaseline()
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(30);
        Tape(recorded).PlayPressed = true;
        recorded.Clock.Step(30);
        store.RecordKey(0, 60, "B", true, recorded);

        var frame = store.Seek(recorded, 0);

        Assert.Equal(0, frame);
        Assert.Equal(0L, recorded.Clock.TotalCycles);
        Assert.False(Tape(recorded).PlayPressed);
        Assert.Equal(0x37, recorded.Bus.Read(0x0001));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Recording starts while a Datasette pulse is still counting down.
    /// Acceptance: Identical TAP v0 pulses 02 03 04 05. PLAY is pressed and
    /// $0000=$2F, $0001=$17. Eight NOPs at $C000, then LDA $DC0D; STA $C100;
    /// JMP $C00E. Step 8 cycles and record A. At cycle 16 reference $DC0D
    /// is $10 and playback is $00, with both PCs at $C008. At cycle 24
    /// reference A and $C100 are $10 and playback is $00, with both PCs at
    /// $C00E. VICE saves the pending datasette alarm and the tape position.
    /// AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsInFlightDatasettePulse()
    {
        var image = BuildTapImage(0, new byte[] { 0x02, 0x03, 0x04, 0x05 });
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var played = MachineTestFactory.CreateC64Machine();
        Tape(recorded).InsertTape(image);
        Tape(reference).InsertTape(image);
        Tape(played).InsertTape(image);
        Tape(recorded).PlayPressed = true;
        Tape(reference).PlayPressed = true;
        recorded.Bus.Write(0x0000, 0x2F);
        recorded.Bus.Write(0x0001, 0x17);
        reference.Bus.Write(0x0000, 0x2F);
        reference.Bus.Write(0x0001, 0x17);
        WriteDatasetteFlagProgram(recorded);
        WriteDatasetteFlagProgram(reference);
        WriteDatasetteFlagProgram(played);

        recorded.Clock.Step(8);
        reference.Clock.Step(8);
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 8, "A", true, recorded);
        store.Play(played);
        SetKey(reference, "A", true);

        recorded.Clock.Step(8);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(16L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, recorded.Clock.TotalCycles);
        Assert.Equal(0xC008, reference.GetState().PC);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(0x10, reference.Bus.Peek(0xDC0D));
        Assert.Equal(reference.Bus.Peek(0xDC0D), played.Bus.Peek(0xDC0D));

        recorded.Clock.Step(8);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(24L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0xC00E, reference.GetState().PC);
        Assert.Equal(reference.GetState().PC, played.GetState().PC);
        Assert.Equal(0x10, reference.GetState().A);
        Assert.Equal(reference.GetState().A, played.GetState().A);
        Assert.Equal(0x10, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), recorded.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Rewinding the Datasette during a recording is replayed.
    /// Acceptance: TAP v0 pulses 02 03 04 05, PLAY held, $0000=$2F,
    /// $0001=$17, and LDA $DC0D; STA $C100; JMP $C000 at $C000. A-down at
    /// cycle 0, Rewind at 40, B-down at 56. The replay has inputCount=3
    /// and the rewind line. At cycle 56 reference $DC0D is $10. At cycle
    /// 64 reference $C100 is $10 and playback matches. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysDatasetteRewindDuringRecording()
    {
        AssertTapeTransportReplay(
            "input tape control=rewind pressed=1 frame=0 cycle=40",
            tape => tape.Rewind(),
            gapAfter: 16,
            expectFlagAtGap: true,
            expectedRam: 0x10);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Ejecting the tape during a recording is replayed.
    /// Acceptance: Same machines and program as rewind. A-down at cycle 0,
    /// EjectTape at 40, B-down at 80. The replay has inputCount=3 and the
    /// eject line. At cycle 88 reference $C100 is $00 and playback matches.
    /// AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysDatasetteEjectDuringRecording()
    {
        AssertTapeTransportReplay(
            "input tape control=eject pressed=1 frame=0 cycle=40",
            tape => tape.EjectTape(),
            gapAfter: 40,
            expectFlagAtGap: false,
            expectedRam: 0x00);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Seeking the tape cursor during a recording is replayed.
    /// Acceptance: Same machines as rewind. SeekTo(0) at cycle 40 is the
    /// public cursor sibling of Rewind. The replay names seek index 0.
    /// At cycle 56 reference $DC0D is $10. At cycle 64 reference $C100
    /// is $10 and playback matches. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysDatasetteSeekDuringRecording()
    {
        AssertTapeTransportReplay(
            "input tape control=seek index=0 frame=0 cycle=40",
            tape => Assert.True(tape.SeekTo(0)),
            gapAfter: 16,
            expectFlagAtGap: true,
            expectedRam: 0x10);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Inserting a tape during a recording is replayed.
    /// Acceptance: Same machines as rewind. InsertTape of the same TAP
    /// at cycle 40 replaces the reader the way a second insert does.
    /// The replay names the insert. At cycle 56 reference $DC0D is $10.
    /// At cycle 64 reference $C100 is $10 and playback matches. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysDatasetteInsertDuringRecording()
    {
        var image = BuildTapImage(0, new byte[] { 0x02, 0x03, 0x04, 0x05 });
        AssertTapeTransportReplay(
            "input tape control=insert pressed=1 frame=0 cycle=40",
            tape => tape.InsertTape(image),
            gapAfter: 16,
            expectFlagAtGap: true,
            expectedRam: 0x10);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Seeking behind a tape eject returns the baseline tape.
    /// Acceptance: TAP v0 pulses 02 03 04 05, PLAY held, $0000=$2F,
    /// $0001=$17, and LDA $DC0D; STA $C100; JMP $C000 at $C000. A-down at
    /// cycle 0, eject at 40, B-down at 80, then Seek(0) on that machine.
    /// Seek returns 0 and the clock is 0. The baseline tape is mounted.
    /// After 29 cycles both PCs are $C006 and both $C100 values are $10.
    /// AC2, AC3, and AC8.
    /// </summary>
    [Fact]
    public void Seek_RestoresDatasetteTapeAfterEject()
    {
        var image = BuildTapImage(0, new byte[] { 0x02, 0x03, 0x04, 0x05 });
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        WriteTapeFlagLoop(recorded, image);
        WriteTapeFlagLoop(reference, image);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(40);
        Tape(recorded).EjectTape();
        recorded.Clock.Step(40);
        store.RecordKey(0, 80, "B", true, recorded);

        reference.Clock.Step(29);
        var referenceCpu = reference.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(referenceCpu);
        Assert.Equal(0xC006, referenceCpu!.PC);
        Assert.Equal(0x10, reference.Bus.Peek(0xC100));

        Assert.Equal(0, store.Seek(recorded, 0));
        Assert.Equal(0L, recorded.Clock.TotalCycles);
        Assert.True(Tape(reference).HasTape);
        Assert.Equal(Tape(reference).HasTape, Tape(recorded).HasTape);

        recorded.Clock.Step(29);
        var recordedCpu = recorded.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(recordedCpu);
        Assert.Equal(reference.Clock.TotalCycles, recorded.Clock.TotalCycles);
        Assert.Equal(referenceCpu.PC, recordedCpu!.PC);
        Assert.Equal(reference.Bus.Peek(0xC100), recorded.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: The saved replay names which tape was inserted.
    /// Acceptance: Same baseline as rewind. At cycle 40 one recording
    /// attaches TAP pulses 02 03 04 05 and the other attaches 14 1E 28 32,
    /// through Datasette.Attach. B-down at 56. At cycle 64 the first
    /// playback $C100 is $10 and the second is $00. The two SaveReplay
    /// texts differ and each contains its pulse bytes. AC4.
    /// </summary>
    [Fact]
    public void SaveReplay_KeepsInsertedTapeBytes()
    {
        var firstImage = BuildTapImage(0, new byte[] { 0x02, 0x03, 0x04, 0x05 });
        var secondImage = BuildTapImage(0, new byte[] { 0x14, 0x1E, 0x28, 0x32 });
        var first = RunAttachedTape(firstImage);
        var second = RunAttachedTape(secondImage);

        Assert.Equal(0x10, first.Ram);
        Assert.Equal(0x00, second.Ram);
        Assert.NotEqual(first.Replay, second.Replay);
        Assert.Contains(Convert.ToHexString(firstImage), first.Replay, StringComparison.Ordinal);
        Assert.Contains(Convert.ToHexString(secondImage), second.Replay, StringComparison.Ordinal);
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Disconnecting the Datasette during a recording is replayed.
    /// Acceptance: TAP v0 pulses 02 03 04 05, PLAY held, $0000=$2F,
    /// $0001=$17, and LDA $DC0D; STA $C100; JMP $C000 at $C000. A-down at
    /// cycle 0, Detach at 40, B-down at 80. The replay has inputCount=3
    /// and the detach line. The tape stays mounted. At cycle 88 every PC
    /// is $C000, reference $C100 is $00, and playback matches. AC1, AC3, AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysDatasetteDetachDuringRecording()
    {
        var image = BuildTapImage(0, new byte[] { 0x02, 0x03, 0x04, 0x05 });
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var played = MachineTestFactory.CreateC64Machine();
        WriteTapeFlagLoop(recorded, image);
        WriteTapeFlagLoop(reference, image);
        WriteTapeFlagLoop(played, image);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(40);
        reference.Clock.Step(40);
        Tape(recorded).Detach();
        Tape(reference).Detach();
        recorded.Clock.Step(40);
        reference.Clock.Step(40);
        store.RecordKey(0, 80, "B", true, recorded);
        SetKey(reference, "B", true);

        Assert.True(Tape(reference).HasTape);
        Assert.False(Tape(reference).IsAttached);
        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains(
            "input tape control=detach pressed=1 frame=0 cycle=40",
            replay,
            StringComparison.Ordinal);

        store.Play(played);
        recorded.Clock.Step(8);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(88L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, recorded.Clock.TotalCycles);
        AssertCpu(reference, 0xC000);
        AssertCpu(played, 0xC000);
        AssertCpu(recorded, 0xC000);
        Assert.True(Tape(reference).HasTape);
        Assert.False(Tape(reference).IsAttached);
        Assert.Equal(Tape(reference).HasTape, Tape(played).HasTape);
        Assert.Equal(Tape(reference).IsAttached, Tape(played).IsAttached);
        Assert.Equal(0x00, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), recorded.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: A Datasette disconnected before recording stays disconnected.
    /// Acceptance: Same tape and program. Detach before A-down at cycle 0.
    /// After Play, 29 cycles leave every PC at $C006. Reference $C100 is
    /// $00 and the Datasette stays detached. Playback matches. AC2 and AC3.
    /// </summary>
    [Fact]
    public void Playback_KeepsDatasetteDetachedFromBaseline()
    {
        var image = BuildTapImage(0, new byte[] { 0x02, 0x03, 0x04, 0x05 });
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var played = MachineTestFactory.CreateC64Machine();
        WriteTapeFlagLoop(recorded, image);
        WriteTapeFlagLoop(reference, image);
        WriteTapeFlagLoop(played, image);
        Tape(recorded).Detach();
        Tape(reference).Detach();
        Tape(played).Detach();

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        reference.Clock.Step(29);
        AssertCpu(reference, 0xC006);
        Assert.False(Tape(reference).IsAttached);
        Assert.True(Tape(reference).HasTape);
        Assert.Equal(0x00, reference.Bus.Peek(0xC100));

        store.Play(played);
        recorded.Clock.Step(29);
        played.Clock.Step(29);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, recorded.Clock.TotalCycles);
        AssertCpu(played, reference.Devices.GetByRole(DeviceRole.Cpu) is Mos6502 cpu ? cpu.PC : 0);
        Assert.Equal(Tape(reference).IsAttached, Tape(played).IsAttached);
        Assert.Equal(Tape(reference).HasTape, Tape(played).HasTape);
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    /// <summary>
    /// FR: FR-SNP-003.
    /// Use case: Reconnecting the Datasette during a recording is replayed.
    /// Acceptance: Same tape and program, detached before A-down. Attach
    /// at cycle 40, B-down at 56. The replay names that attach. Playback
    /// matches the reference connection and $C100. AC1, AC3, and AC4.
    /// </summary>
    [Fact]
    public void Playback_ReplaysDatasetteAttachDuringRecording()
    {
        var image = BuildTapImage(0, new byte[] { 0x02, 0x03, 0x04, 0x05 });
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var played = MachineTestFactory.CreateC64Machine();
        WriteTapeFlagLoop(recorded, image);
        WriteTapeFlagLoop(reference, image);
        WriteTapeFlagLoop(played, image);
        Tape(recorded).Detach();
        Tape(reference).Detach();
        Tape(played).Detach();

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(40);
        reference.Clock.Step(40);
        Tape(recorded).Attach();
        Tape(reference).Attach();
        recorded.Clock.Step(16);
        reference.Clock.Step(16);
        store.RecordKey(0, 56, "B", true, recorded);
        SetKey(reference, "B", true);

        Assert.True(Tape(reference).IsAttached);
        var replay = store.SaveReplay();
        Assert.Contains(
            "input tape control=attach pressed=1 frame=0 cycle=40",
            replay,
            StringComparison.Ordinal);

        store.Play(played);
        recorded.Clock.Step(8);
        reference.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(Tape(reference).IsAttached, Tape(played).IsAttached);
        Assert.Equal(Tape(reference).HasTape, Tape(played).HasTape);
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), recorded.Bus.Peek(0xC100));
    }

    private static void AlignToInstruction(IMachine machine)
    {
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        var guard = 0;
        while (!cpu!.IsInstructionBoundary && guard < 8)
        {
            machine.Clock.Step(1);
            guard++;
        }
    }

    private static void AssertCpu(IMachine machine, int pc)
    {
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        Assert.Equal(pc, cpu!.PC);
    }

    private readonly record struct AttachedTapeResult(string Replay, byte Ram);

    private static AttachedTapeResult RunAttachedTape(byte[] replacement)
    {
        var baseline = BuildTapImage(0, new byte[] { 0x02, 0x03, 0x04, 0x05 });
        var recorded = MachineTestFactory.CreateC64Machine();
        var played = MachineTestFactory.CreateC64Machine();
        WriteTapeFlagLoop(recorded, baseline);
        WriteTapeFlagLoop(played, baseline);
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(40);
        Assert.True(ViceSharp.Chips.Tape.TapImage.TryAttach(replacement, out var image));
        Assert.NotNull(image);
        Tape(recorded).Attach(image!);
        recorded.Clock.Step(16);
        store.RecordKey(0, 56, "B", true, recorded);
        var replay = store.SaveReplay();
        store.Play(played);
        recorded.Clock.Step(8);
        played.Clock.Step(8);
        Assert.Equal(64L, recorded.Clock.TotalCycles);
        Assert.Equal(recorded.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(recorded.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        return new AttachedTapeResult(replay, recorded.Bus.Peek(0xC100));
    }

    private static void AssertTapeTransportReplay(
        string eventLine,
        Action<Datasette> apply,
        int gapAfter,
        bool expectFlagAtGap,
        byte expectedRam)
    {
        var image = BuildTapImage(0, new byte[] { 0x02, 0x03, 0x04, 0x05 });
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        var played = MachineTestFactory.CreateC64Machine();
        WriteTapeFlagLoop(recorded, image);
        WriteTapeFlagLoop(reference, image);
        WriteTapeFlagLoop(played, image);

        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        SetKey(reference, "A", true);
        recorded.Clock.Step(40);
        reference.Clock.Step(40);
        apply(Tape(recorded));
        apply(Tape(reference));
        recorded.Clock.Step(gapAfter);
        reference.Clock.Step(gapAfter);

        var at = 40 + gapAfter;
        store.RecordKey(0, at, "B", true, recorded);
        SetKey(reference, "B", true);
        if (expectFlagAtGap)
            Assert.Equal(0x10, reference.Bus.Peek(0xDC0D));

        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains(eventLine, replay, StringComparison.Ordinal);

        store.Play(played);
        Assert.Equal(at, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, recorded.Clock.TotalCycles);
        if (expectFlagAtGap)
        {
            Assert.Equal(0x10, reference.Bus.Peek(0xDC0D));
            Assert.Equal(reference.Bus.Peek(0xDC0D), played.Bus.Peek(0xDC0D));
        }

        recorded.Clock.Step(8);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(at + 8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, recorded.Clock.TotalCycles);
        Assert.Equal(expectedRam, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), recorded.Bus.Peek(0xC100));
    }

    private static void WriteTapeFlagLoop(IMachine machine, byte[] image)
    {
        Tape(machine).InsertTape(image);
        Tape(machine).PlayPressed = true;
        machine.Bus.Write(0x0000, 0x2F);
        machine.Bus.Write(0x0001, 0x17);
        byte[] code = { 0xAD, 0x0D, 0xDC, 0x8D, 0x00, 0xC1, 0x4C, 0x00, 0xC0 };
        for (var i = 0; i < code.Length; i++)
            machine.Bus.Write((ushort)(0xC000 + i), code[i]);

        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
    }

    private static byte[] BuildTapImage(byte version, byte[] pulseData)
    {
        var buffer = new byte[20 + pulseData.Length];
        var signature = System.Text.Encoding.ASCII.GetBytes("C64-TAPE-RAW");
        signature.CopyTo(buffer, 0);
        buffer[12] = version;
        buffer[16] = (byte)pulseData.Length;
        pulseData.CopyTo(buffer, 20);
        return buffer;
    }

    private static void WriteDatasetteFlagProgram(IMachine machine)
    {
        for (var i = 0; i < 8; i++)
            machine.Bus.Write((ushort)(0xC000 + i), 0xEA);

        machine.Bus.Write(0xC008, 0xAD);
        machine.Bus.Write(0xC009, 0x0D);
        machine.Bus.Write(0xC00A, 0xDC);
        machine.Bus.Write(0xC00B, 0x8D);
        machine.Bus.Write(0xC00C, 0x00);
        machine.Bus.Write(0xC00D, 0xC1);
        machine.Bus.Write(0xC00E, 0x4C);
        machine.Bus.Write(0xC00F, 0x0E);
        machine.Bus.Write(0xC010, 0xC0);
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
    }

    private static void AssertTapeHeldBeforeRecording(bool play)
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var reference = MachineTestFactory.CreateC64Machine();
        SetTapeButton(recorded, play, true);
        SetTapeButton(reference, play, true);
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        var played = MachineTestFactory.CreateC64Machine();
        store.Play(played);

        Assert.Equal(0L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.True(TapeButton(reference, play));
        Assert.Equal(TapeButton(reference, play), TapeButton(played, play));
        Assert.Equal(0x27, reference.Bus.Read(0x0001));
        Assert.Equal(reference.Bus.Read(0x0001), played.Bus.Read(0x0001));

        WriteLoadProcessorPort(reference);
        WriteLoadProcessorPort(played);
        reference.Clock.Step(8);
        played.Clock.Step(8);

        Assert.Equal(8L, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, played.Clock.TotalCycles);
        Assert.Equal(0x27, reference.Bus.Peek(0xC100));
        Assert.Equal(reference.Bus.Peek(0xC100), played.Bus.Peek(0xC100));
    }

    private static void AssertTapeHeldAtPeriodicSeek(bool play)
    {
        var recorded = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();
        store.RecordKey(0, 0, "A", true, recorded);
        recorded.Clock.Step(30);
        SetTapeButton(recorded, play, true);
        var frameCycles = FrameCycles(recorded);
        recorded.Clock.Step((int)(frameCycles * 10 - 30));
        store.RecordKey(10, 0, "B", true, recorded);

        var replay = store.SaveReplay();
        Assert.Contains("inputCount=3", replay, StringComparison.Ordinal);
        Assert.Contains(play
            ? "input tape control=play pressed=1 frame=0 cycle=30"
            : "input tape control=record pressed=1 frame=0 cycle=30", replay, StringComparison.Ordinal);

        var reference = MachineTestFactory.CreateC64Machine();
        SetKey(reference, "A", true);
        reference.Clock.Step(30);
        SetTapeButton(reference, play, true);
        reference.Clock.Step((int)(frameCycles * 10 - 30));

        var sought = MachineTestFactory.CreateC64Machine();
        var frame = store.Seek(sought, 10);

        Assert.Equal(10, frame);
        Assert.Equal(frameCycles * 10, reference.Clock.TotalCycles);
        Assert.Equal(reference.Clock.TotalCycles, sought.Clock.TotalCycles);
        Assert.True(TapeButton(reference, play));
        Assert.Equal(TapeButton(reference, play), TapeButton(sought, play));
        Assert.Equal(0x27, reference.Bus.Read(0x0001));
        Assert.Equal(reference.Bus.Read(0x0001), sought.Bus.Read(0x0001));
    }

    private static void SetTapeButton(IMachine machine, bool play, bool pressed)
    {
        if (play)
            Tape(machine).PlayPressed = pressed;
        else
            Tape(machine).RecordPressed = pressed;
    }

    private static bool TapeButton(IMachine machine, bool play)
    {
        return play ? Tape(machine).PlayPressed : Tape(machine).RecordPressed;
    }

    private sealed class CollectingAudioBackend : IAudioBackend
    {
        public List<float> Samples { get; } = new();

        public void SubmitSamples(ReadOnlySpan<float> samples)
        {
            for (var i = 0; i < samples.Length; i++)
                Samples.Add(samples[i]);
        }

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

    private static void WriteGatedNoise(IMachine machine)
    {
        machine.Bus.Write(0xD40E, 0xFF);
        machine.Bus.Write(0xD40F, 0xFF);
        machine.Bus.Write(0xD413, 0x00);
        machine.Bus.Write(0xD414, 0xF0);
        machine.Bus.Write(0xD412, 0x81);
        machine.Bus.Write(0xD418, 0x0F);
    }

    private static Sid6581 Sid(IMachine machine)
    {
        var sid = machine.Devices.GetByRole(DeviceRole.AudioChip) as Sid6581;
        Assert.NotNull(sid);
        return sid!;
    }

    private static void WriteVoice3Saw(IMachine machine)
    {
        machine.Bus.Write(0xD40E, 0x34);
        machine.Bus.Write(0xD40F, 0x12);
        machine.Bus.Write(0xD412, 0x20);
    }

    private static IVideoChip Video(IMachine machine)
    {
        var video = machine.Devices.GetByRole(DeviceRole.VideoChip) as IVideoChip;
        Assert.NotNull(video);
        return video!;
    }

    private static long FrameCycles(IMachine machine)
    {
        var profiled = machine.Architecture as IProfiledArchitectureDescriptor;
        Assert.NotNull(profiled);
        return (long)profiled!.MachineProfile.CyclesPerLine * profiled.MachineProfile.RasterLines;
    }

    private static Mos906114 Pla(IMachine machine)
    {
        var pla = machine.Devices.GetByRole(DeviceRole.Pla) as Mos906114;
        Assert.NotNull(pla);
        return pla!;
    }

    private static void UseGermanPositionalMap(IMachine machine)
    {
        var path = ViceSharp.RomFetch.ViceDataPathResolver.FindDataFile("C64", "gtk3_pos_de.vkm");
        var parsed = ViceSharp.Core.Input.C64VkmParser.Load(path);
        Assert.False(parsed.HasErrors);
        var map = machine.Devices.GetByRole(DeviceRole.SystemRam) as C64MemoryMap;
        Assert.NotNull(map);
        map!.SelectKeyboardMap(parsed.KeyboardMap);
    }

    private static ICartridgePort Cartridge(IMachine machine)
        => machine.Devices.GetAll<ICartridgePort>().Single();

    private static byte[] BankBytes(int banks)
    {
        const int bankSize = 0x2000;
        var image = new byte[banks * bankSize];
        for (var bank = 0; bank < banks; bank++)
            image[bank * bankSize] = (byte)bank;
        return image;
    }

    private static IKeyboardMatrix MatrixOf(IMachine machine)
    {
        var matrix = machine.Devices.All.OfType<IKeyboardMatrix>().Single();
        return matrix;
    }

    private static void SelectKeyboardColumns(IMachine machine)
    {
        machine.Bus.Write(0xDC02, 0xFF);
        machine.Bus.Write(0xDC03, 0x00);
        machine.Bus.Write(0xDC00, 0x00);
    }

    private static void ArmTimerA(IMachine machine)
    {
        machine.Bus.Write(0xDC04, 0x34);
        machine.Bus.Write(0xDC05, 0x12);
        machine.Bus.Write(0xDC0E, 0x11);
    }

    private static void WriteTod(IMachine machine, byte tenths, byte seconds, byte minutes, byte hours)
    {
        machine.Bus.Write(0xDC0B, hours);
        machine.Bus.Write(0xDC0A, minutes);
        machine.Bus.Write(0xDC09, seconds);
        machine.Bus.Write(0xDC08, tenths);
    }

    private static void ArmSerialOutput(IMachine machine)
    {
        machine.Bus.Write(0xDC04, 0x02);
        machine.Bus.Write(0xDC05, 0x00);
        machine.Bus.Write(0xDC0C, 0xA5);
        machine.Bus.Write(0xDC0E, 0x41);
    }

    private static void ArmSerialOutputInViceOrder(IMachine machine)
    {
        machine.Bus.Write(0xDC04, 0x02);
        machine.Bus.Write(0xDC05, 0x00);
        machine.Bus.Write(0xDC0E, 0x41);
        machine.Bus.Write(0xDC0C, 0xA5);
    }

    private static void PulseCnt(IMachine machine, int edges)
    {
        var cia = Cia1(machine);
        for (var i = 0; i < edges; i++)
            cia.TriggerCntEdge();
    }

    private static void ArmOverlappingSprites(IMachine machine)
    {
        ArmActiveSprite(machine);
        machine.Bus.Write(0xD002, 0x50);
        machine.Bus.Write(0xD003, 0x40);
        machine.Bus.Write(0xD015, 0x03);
        machine.Bus.Write(0xD028, 0x02);
        machine.Bus.Write(0x07F9, 0x80);
    }

    private static void ArmActiveSprite(IMachine machine)
    {
        ArmActiveDisplay(machine);
        machine.Bus.Write(0xD000, 0x50);
        machine.Bus.Write(0xD001, 0x40);
        machine.Bus.Write(0xD015, 0x01);
        machine.Bus.Write(0xD027, 0x02);
        machine.Bus.Write(0x07F8, 0x80);
        for (var i = 0; i < 64; i++)
            machine.Bus.Write((ushort)(0x2000 + i), (byte)(i + 1));
    }

    private static void WriteLoadLightPen(IMachine machine)
    {
        machine.Bus.Write(0xC000, 0xAD);
        machine.Bus.Write(0xC001, 0x14);
        machine.Bus.Write(0xC002, 0xD0);
        machine.Bus.Write(0xC003, 0x8D);
        machine.Bus.Write(0xC004, 0x00);
        machine.Bus.Write(0xC005, 0xC1);
        machine.Bus.Write(0xC006, 0x4C);
        machine.Bus.Write(0xC007, 0x00);
        machine.Bus.Write(0xC008, 0xC0);
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
    }

    private static Datasette Tape(IMachine machine)
    {
        var map = machine.Devices.GetByRole(DeviceRole.SystemRam) as C64MemoryMap;
        Assert.NotNull(map);
        Assert.NotNull(map!.Datasette);
        return map.Datasette!;
    }

    private static void WriteLoadProcessorPort(IMachine machine)
    {
        machine.Bus.Write(0xC000, 0xAD);
        machine.Bus.Write(0xC001, 0x01);
        machine.Bus.Write(0xC002, 0x00);
        machine.Bus.Write(0xC003, 0x8D);
        machine.Bus.Write(0xC004, 0x00);
        machine.Bus.Write(0xC005, 0xC1);
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
    }

    private static void WriteLoadCiaPortB(IMachine machine)
    {
        machine.Bus.Write(0xC000, 0xAD);
        machine.Bus.Write(0xC001, 0x01);
        machine.Bus.Write(0xC002, 0xDC);
        machine.Bus.Write(0xC003, 0x8D);
        machine.Bus.Write(0xC004, 0x00);
        machine.Bus.Write(0xC005, 0xC1);
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
    }

    private static void WriteLoadCollisions(IMachine machine)
    {
        machine.Bus.Write(0xC000, 0xAD);
        machine.Bus.Write(0xC001, 0x1E);
        machine.Bus.Write(0xC002, 0xD0);
        machine.Bus.Write(0xC003, 0x8D);
        machine.Bus.Write(0xC004, 0x00);
        machine.Bus.Write(0xC005, 0xC1);
        machine.Bus.Write(0xC006, 0xAD);
        machine.Bus.Write(0xC007, 0x1F);
        machine.Bus.Write(0xC008, 0xD0);
        machine.Bus.Write(0xC009, 0x8D);
        machine.Bus.Write(0xC00A, 0x01);
        machine.Bus.Write(0xC00B, 0xC1);
        machine.Bus.Write(0xC00C, 0x4C);
        machine.Bus.Write(0xC00D, 0x00);
        machine.Bus.Write(0xC00E, 0xC0);
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
    }

    private static void WriteLoadRaster(IMachine machine)
    {
        machine.Bus.Write(0xC000, 0xAD);
        machine.Bus.Write(0xC001, 0x12);
        machine.Bus.Write(0xC002, 0xD0);
        machine.Bus.Write(0xC003, 0x8D);
        machine.Bus.Write(0xC004, 0x00);
        machine.Bus.Write(0xC005, 0xC1);
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
    }

    private static void ArmActiveDisplay(IMachine machine)
    {
        machine.Bus.Write(0xC000, 0x4C);
        machine.Bus.Write(0xC001, 0x00);
        machine.Bus.Write(0xC002, 0xC0);
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
        machine.Bus.Write(0xD011, 0x1B);
        machine.Bus.Write(0xD016, 0xC8);
        machine.Bus.Write(0xD018, 0x14);
        machine.Bus.Write(0xD020, 0x06);
        machine.Bus.Write(0xD021, 0x00);
        for (var i = 0; i < 1000; i++)
        {
            machine.Bus.Write((ushort)(0x0400 + i), 0x01);
            machine.Bus.Write((ushort)(0xD800 + i), 0x01);
        }
    }

    private static void ArmPending8580FastWrite(IMachine machine)
    {
        machine.Bus.Write(0xD413, 0x00);
        machine.Bus.Write(0xD414, 0xF0);
        machine.Bus.Write(0xD412, 0x21);
        machine.Bus.Write(0xD418, 0x0F);
        Sid(machine).SamplingMethod = SidSamplingMethod.Fast;
        machine.Bus.Write(0xD40F, 0x20);
    }

    private static void ArmFutureRasterCompare(IMachine machine)
    {
        machine.Bus.Write(0xC000, 0xAD);
        machine.Bus.Write(0xC001, 0x19);
        machine.Bus.Write(0xC002, 0xD0);
        machine.Bus.Write(0xC003, 0x8D);
        machine.Bus.Write(0xC004, 0x00);
        machine.Bus.Write(0xC005, 0xC1);
        machine.Bus.Write(0xC006, 0x4C);
        machine.Bus.Write(0xC007, 0x00);
        machine.Bus.Write(0xC008, 0xC0);

        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
        cpu.Flags = 0x24;
        machine.Bus.Write(0xD011, 0x00);
        machine.Bus.Write(0xD012, 0x20);
        machine.Bus.Write(0xD019, 0x0F);
        machine.Bus.Write(0xD01A, 0x01);
    }

    private static void ArmVicRasterIrq(IMachine machine)
    {
        for (var i = 0; i < 128; i++)
            machine.Bus.Write((ushort)(0xC000 + i), 0xEA);

        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
        cpu.Flags = 0x20;
        machine.Bus.Write(0xD011, 0x00);
        machine.Bus.Write(0xD012, 0x01);
        machine.Bus.Write(0xD01A, 0x01);
    }

    private static void ArmPendingIrq(IMachine machine)
    {
        for (var i = 0; i < 64; i++)
            machine.Bus.Write((ushort)(0xC000 + i), 0xEA);

        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
        cpu.Flags = 0x20;
        machine.Bus.Write(0xDC04, 0x02);
        machine.Bus.Write(0xDC05, 0x00);
        machine.Bus.Write(0xDC0D, 0x81);
        machine.Bus.Write(0xDC0E, 0x11);
    }

    private static Mos6526 Cia1(IMachine machine)
    {
        var cia = machine.Devices.GetByRole(DeviceRole.Cia1) as Mos6526;
        Assert.NotNull(cia);
        return cia!;
    }

    private static Mos6526 Cia2(IMachine machine)
    {
        var cia = machine.Devices.GetByRole(DeviceRole.Cia2) as Mos6526;
        Assert.NotNull(cia);
        return cia!;
    }

    private static int VicBank(IMachine machine)
    {
        var map = machine.Devices.GetByRole(DeviceRole.CartridgePort);
        Assert.NotNull(map);
        var property = map!.GetType().GetProperty(
            "VicBankForDiagnostics",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(property);
        return Assert.IsType<int>(property!.GetValue(map));
    }

    private static Mos6569 Vic(IMachine machine)
    {
        var vic = machine.Devices.GetByRole(DeviceRole.VideoChip) as Mos6569;
        Assert.NotNull(vic);
        return vic!;
    }

    private static void ArmTimerB(IMachine machine)
    {
        machine.Bus.Write(0xDC06, 0x34);
        machine.Bus.Write(0xDC07, 0x12);
        machine.Bus.Write(0xDC0F, 0x19);
    }

    private static void WriteProgramTail(IMachine machine)
    {
        machine.Bus.Write(0xC006, 0xAD);
        machine.Bus.Write(0xC007, 0x01);
        machine.Bus.Write(0xC008, 0xD8);
        machine.Bus.Write(0xC009, 0x8D);
        machine.Bus.Write(0xC00A, 0x01);
        machine.Bus.Write(0xC00B, 0xC1);
    }

    private static void ArmOceanReadLoop(IMachine machine)
    {
        ArmAbsLoadStore(machine, 0x8000, 0xC100);
        machine.Bus.Write(0xC006, 0x4C);
        machine.Bus.Write(0xC007, 0x00);
        machine.Bus.Write(0xC008, 0xC0);
        CpuOf(machine).Flags = 0x24;
    }

    private static void ArmPotReadLoop(IMachine machine, ushort source)
    {
        ArmAbsLoadStore(machine, source, 0xC100);
        machine.Bus.Write(0xC006, 0x4C);
        machine.Bus.Write(0xC007, 0x00);
        machine.Bus.Write(0xC008, 0xC0);
        CpuOf(machine).Flags = 0x24;
        machine.Bus.Write(0xD011, 0x00);
    }

    private static void ArmAbsLoadStore(IMachine machine, ushort source, ushort destination)
    {
        machine.Bus.Write(0xC000, 0xAD);
        machine.Bus.Write(0xC001, (byte)source);
        machine.Bus.Write(0xC002, (byte)(source >> 8));
        machine.Bus.Write(0xC003, 0x8D);
        machine.Bus.Write(0xC004, (byte)destination);
        machine.Bus.Write(0xC005, (byte)(destination >> 8));
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as ICpu;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
    }

    private static void FeedSerialBits(IMachine machine, params int[] bits)
    {
        var cia = machine.Devices.GetByRole(DeviceRole.Cia1) as Mos6526;
        Assert.NotNull(cia);
        machine.Bus.Write(0xDC0E, 0x00);
        for (var i = 0; i < bits.Length; i++)
        {
            cia!.SerialDataIn = bits[i] != 0;
            cia.TriggerCntEdge();
        }
    }

    private static void ArmSdrStore(IMachine machine)
    {
        machine.Bus.Write(0xC000, 0xAD);
        machine.Bus.Write(0xC001, 0x0C);
        machine.Bus.Write(0xC002, 0xDC);
        machine.Bus.Write(0xC003, 0x8D);
        machine.Bus.Write(0xC004, 0x00);
        machine.Bus.Write(0xC005, 0xC1);
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as ICpu;
        Assert.NotNull(cpu);
        cpu!.PC = 0xC000;
    }

    private static void SetJoystickFire(IMachine machine)
    {
        var joystick = machine.Devices.GetAll<IMachineJoystickInput>().Single();
        Assert.True(joystick.SetJoystickState(2, 0, true));
    }

    private static void ApplyMouseDelta(IMachine machine, int port, int deltaX, int deltaY)
    {
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var method = memory!.GetType().GetMethod("ApplyMouseDelta");
        Assert.NotNull(method);
        method!.Invoke(memory, [port, deltaX, deltaY]);
    }

    private static void SetPotState(IMachine machine, int port, byte x, byte y)
    {
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var method = memory!.GetType().GetMethod("SetPotState");
        Assert.NotNull(method);
        method!.Invoke(memory, [port, x, y]);
    }

    private static void SetRestoreState(IMachine machine, bool pressed)
    {
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var method = memory!.GetType().GetMethod("SetRestoreState");
        Assert.NotNull(method);
        Assert.Equal(true, method!.Invoke(memory, [pressed]));
    }

    private static bool IsRestorePressed(IMachine machine)
    {
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var property = memory!.GetType().GetProperty("IsRestorePressed");
        Assert.NotNull(property);
        return (bool)property!.GetValue(memory)!;
    }

    private static Mos6502 CpuOf(IMachine machine)
    {
        var cpu = machine.Devices.GetByRole(DeviceRole.Cpu) as Mos6502;
        Assert.NotNull(cpu);
        return cpu!;
    }

    private static void SetMouseButtons(IMachine machine, int port, bool left, bool right)
    {
        var memory = machine.Devices.GetByRole(DeviceRole.SystemRam);
        Assert.NotNull(memory);
        var method = memory!.GetType().GetMethod("SetMouseButtons");
        Assert.NotNull(method);
        method!.Invoke(memory, [port, left, right]);
    }

    private static void SelectNoKeyboardColumns(IMachine machine)
    {
        machine.Bus.Write(0xDC02, 0xFF);
        machine.Bus.Write(0xDC03, 0x00);
        machine.Bus.Write(0xDC00, 0xFF);
    }

    private static void SetKey(IMachine machine, string key, bool pressed)
    {
        var keyboards = machine.Devices.GetAll<IMachineKeyboardInput>();
        for (var i = 0; i < keyboards.Count; i++)
            keyboards[i].SetKeyState(key, pressed);
    }

    private static bool SameImage(ISnapshot left, ISnapshot right)
    {
        var leftImage = (RuntimeSnapshot)left;
        var rightImage = (RuntimeSnapshot)right;
        var leftBytes = new byte[leftImage.GetSerializedSize()];
        var rightBytes = new byte[rightImage.GetSerializedSize()];
        leftImage.Serialize(leftBytes);
        rightImage.Serialize(rightBytes);
        return leftBytes.AsSpan().SequenceEqual(rightBytes);
    }
}
