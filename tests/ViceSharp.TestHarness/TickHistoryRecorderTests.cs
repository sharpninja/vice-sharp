namespace ViceSharp.TestHarness;

using ViceSharp.Abstractions;
using ViceSharp.Host.Runtime;
using Xunit;

/// <summary>
/// FR-TICKHIST-001 / TR-TICKHIST-CAPTURE-001 / TEST-TICKHIST-001.
/// The tick-history recorder keeps the last 100 completed CPU instructions, each bundled
/// with the memory write-deltas (address + old byte) that occurred during it, and the
/// reconstruction engine reverse-applies those deltas to the current paused memory image
/// to recover RAM exactly as it was at any captured tick (the time-travel debugger).
/// </summary>
public sealed class TickHistoryRecorderTests
{
    private static CpuInstructionCompletedEvent Instr(ushort address, byte opcode = 0xEA)
        => new(address, opcode, A: 0, X: 0, Y: 0, S: 0xFF, P: 0x20, Pc: (ushort)(address + 1));

    private sealed class FakeStatefulDevice : IStatefulDevice
    {
        public byte Value;
        public FakeStatefulDevice(byte value) => Value = value;
        public string StateName => "FAKE";
        public int StateSize => 4;
        public void CaptureState(Span<byte> destination) => destination[..StateSize].Fill(Value);
        public IReadOnlyList<ChipStateField> DecodeState(ReadOnlySpan<byte> state)
            => new[] { new ChipStateField("V", state[0]) };
    }

    /// <summary>
    /// FR-CHIPSTATE-001 / TR-CHIPSTATE-CAPTURE-001.
    /// Use case: each tick also snapshots every stateful chip's full state.
    /// Acceptance: with one 4-byte stateful device, a captured tick carries a 4-byte chip
    ///   state equal to the device's value at capture time.
    /// </summary>
    [Fact]
    public void OnInstructionCompleted_CapturesChipState()
    {
        var recorder = new TickHistoryRecorder();
        var device = new FakeStatefulDevice(0xAB);
        recorder.SetStatefulDevices(new[] { device });

        recorder.OnInstructionCompleted(Instr(0x1000));

        var ticks = recorder.Snapshot();
        Assert.Single(ticks);
        Assert.Equal(4, ticks[0].ChipState.Length);
        Assert.Equal(0xAB, ticks[0].ChipState[0]);
    }

    /// <summary>
    /// FR-CHIPSTATE-001 / TR-CHIPSTATE-CAPTURE-001.
    /// Use case: a snapshot's chip state must be an immutable copy (the ring reuses buffers),
    ///   so later captures do not corrupt an earlier snapshot.
    /// Acceptance: after snapshotting, changing the device's value and capturing again leaves
    ///   the first snapshot's chip state unchanged.
    /// </summary>
    [Fact]
    public void Snapshot_DeepCopiesChipState()
    {
        var recorder = new TickHistoryRecorder();
        var device = new FakeStatefulDevice(0x11);
        recorder.SetStatefulDevices(new[] { device });

        recorder.OnInstructionCompleted(Instr(0x1000));
        var first = recorder.Snapshot();

        device.Value = 0x22;
        for (var i = 0; i < TickHistoryRecorder.Capacity; i++)
            recorder.OnInstructionCompleted(Instr(0x2000));

        Assert.Equal(0x11, first[0].ChipState[0]);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-CAPTURE-001.
    /// Use case: completed instructions are retained in execution order (oldest first).
    /// Acceptance: after three OnInstructionCompleted calls, Snapshot returns them in order.
    /// </summary>
    [Fact]
    public void Snapshot_ReturnsCompletedInstructionsInOrder()
    {
        var recorder = new TickHistoryRecorder();

        recorder.OnInstructionCompleted(Instr(0x1000));
        recorder.OnInstructionCompleted(Instr(0x1001));
        recorder.OnInstructionCompleted(Instr(0x1002));

        var ticks = recorder.Snapshot();

        Assert.Equal(3, ticks.Count);
        Assert.Equal(0x1000, ticks[0].Registers.InstructionAddress);
        Assert.Equal(0x1001, ticks[1].Registers.InstructionAddress);
        Assert.Equal(0x1002, ticks[2].Registers.InstructionAddress);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-CAPTURE-001.
    /// Use case: the history is a bounded ring of the last 100 instructions.
    /// Acceptance: after 150 instructions, exactly 100 are kept and the oldest retained is
    ///   the 51st (index 50) instruction, newest is the 150th.
    /// </summary>
    [Fact]
    public void Snapshot_CapsAtCapacity_KeepingTheMostRecent()
    {
        var recorder = new TickHistoryRecorder();

        for (var i = 0; i < 150; i++)
            recorder.OnInstructionCompleted(Instr((ushort)(0x2000 + i)));

        var ticks = recorder.Snapshot();

        Assert.Equal(TickHistoryRecorder.Capacity, ticks.Count);
        Assert.Equal(0x2000 + 50, ticks[0].Registers.InstructionAddress);
        Assert.Equal(0x2000 + 149, ticks[^1].Registers.InstructionAddress);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-CAPTURE-001.
    /// Use case: memory writes observed since the previous instruction are bundled into the
    ///   next completed instruction, then the pending set resets.
    /// Acceptance: writes before the first OnInstructionCompleted attach to tick 0; tick 1
    ///   (no intervening writes) has none.
    /// </summary>
    [Fact]
    public void OnInstructionCompleted_BundlesPendingWrites_ThenResets()
    {
        var recorder = new TickHistoryRecorder();

        recorder.OnMemoryWrite(0x0400, oldValue: 0x11);
        recorder.OnMemoryWrite(0x0401, oldValue: 0x22);
        recorder.OnInstructionCompleted(Instr(0x3000));
        recorder.OnInstructionCompleted(Instr(0x3003));

        var ticks = recorder.Snapshot();

        Assert.Equal(2, ticks[0].Writes.Length);
        Assert.Equal(0x0400, ticks[0].Writes[0].Address);
        Assert.Equal(0x11, ticks[0].Writes[0].OldValue);
        Assert.Empty(ticks[1].Writes);
    }

    /// <summary>
    /// FR-TICKHIST-001 / TR-TICKHIST-CAPTURE-001.
    /// Use case: reconstruct RAM as it was at a past tick by reverse-applying the deltas of
    ///   every later tick onto the current (paused) memory image.
    /// Acceptance: with current $10 = 0x33, tick1 having overwritten $10 (old 0x22) and tick2
    ///   having overwritten $10 (old... wait) - reconstructing at the newest tick yields
    ///   current; at tick1 yields 0x22; at tick0 yields 0x11.
    /// </summary>
    [Fact]
    public void ReconstructInto_ReverseAppliesLaterWrites()
    {
        var recorder = new TickHistoryRecorder();
        // tick0: no writes
        recorder.OnInstructionCompleted(Instr(0x4000));
        // tick1: wrote $10, the byte there was 0x11 before this write
        recorder.OnMemoryWrite(0x10, oldValue: 0x11);
        recorder.OnInstructionCompleted(Instr(0x4001));
        // tick2: wrote $10 again, the byte there was 0x22 before this write
        recorder.OnMemoryWrite(0x10, oldValue: 0x22);
        recorder.OnInstructionCompleted(Instr(0x4002));

        var ticks = recorder.Snapshot();
        Assert.Equal(3, ticks.Count);

        var image = new byte[0x10000];
        image[0x10] = 0x33; // current paused value

        // Newest tick (index 2): no reversal, current value.
        TickHistoryReconstruction.ReconstructInto(image, ticks, targetIndex: 2);
        Assert.Equal(0x33, image[0x10]);

        // Tick 1: reverse tick2's write -> 0x22.
        image[0x10] = 0x33;
        TickHistoryReconstruction.ReconstructInto(image, ticks, targetIndex: 1);
        Assert.Equal(0x22, image[0x10]);

        // Tick 0: reverse tick2 then tick1 -> 0x11.
        image[0x10] = 0x33;
        TickHistoryReconstruction.ReconstructInto(image, ticks, targetIndex: 0);
        Assert.Equal(0x11, image[0x10]);
    }
}
