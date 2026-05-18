namespace ViceSharp.TestHarness;

using ViceSharp.Core.Snapshots;
using Xunit;

public sealed class RuntimeSnapshotTests
{
    /// <summary>
    /// FR: FR-SNP-001, TR: TR-STATE-001.
    /// Use case: Capture a runtime snapshot from a running C64 machine,
    /// persist it to disk, reload it and confirm the round-tripped
    /// snapshot is byte-identical to the original capture.
    /// Acceptance: Serialised bytes of original and loaded snapshots match
    /// exactly; the loaded snapshot still reports the user-poked byte at
    /// $0801 ($42).
    /// </summary>
    [Fact]
    public async Task SaveLoad_RoundTrips_DeterministicRuntimeSnapshot()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        machine.Bus.Write(0x0801, 0x42);
        machine.StepInstruction();

        var store = new RuntimeSnapshotStore();
        var snapshot = store.Capture(machine);
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.vssnap");

        try
        {
            await store.SaveAsync(snapshot, path, TestContext.Current.CancellationToken);
            var loaded = await store.LoadAsync(path, TestContext.Current.CancellationToken);

            var expected = new byte[snapshot.GetSerializedSize()];
            var actual = new byte[loaded.GetSerializedSize()];
            snapshot.Serialize(expected);
            loaded.Serialize(actual);

            Assert.Equal(expected, actual);
            Assert.Equal(0x42, loaded.Memory.Span[0x0801]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// FR: FR-SNP-002, TR: TR-STATE-001.
    /// Use case: Restore a previously captured snapshot on a running C64
    /// machine; both RAM contents and the public CPU state (PC, P) must
    /// roll back to the snapshot point.
    /// Acceptance: After mutating $C000 to $11 and calling Restore, the
    /// bus reads back the snapshot value $99 and the machine state's PC
    /// and P registers match the snapshot's stored values.
    /// </summary>
    [Fact]
    public void Restore_AppliesMemoryAndPublicCpuState()
    {
        var machine = MachineTestFactory.CreateC64Machine();
        var store = new RuntimeSnapshotStore();

        machine.Bus.Write(0xC000, 0x99);
        var snapshot = store.Capture(machine);
        machine.Bus.Write(0xC000, 0x11);

        store.Restore(machine, snapshot);

        Assert.Equal(0x99, machine.Bus.Peek(0xC000));
        Assert.Equal(((RuntimeSnapshot)snapshot).State.PC, machine.GetState().PC);
        Assert.Equal(((RuntimeSnapshot)snapshot).State.P, machine.GetState().P);
    }
}
