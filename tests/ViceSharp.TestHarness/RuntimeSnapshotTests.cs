namespace ViceSharp.TestHarness;

using ViceSharp.Core.Snapshots;
using Xunit;

public sealed class RuntimeSnapshotTests
{
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
