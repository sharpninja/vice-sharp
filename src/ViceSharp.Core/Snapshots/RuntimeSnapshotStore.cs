using ViceSharp.Abstractions;

namespace ViceSharp.Core.Snapshots;

public sealed class RuntimeSnapshotStore : ISnapshotStore
{
    private const int AddressCount = ushort.MaxValue + 1;

    public ISnapshot Capture(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        var memory = new byte[AddressCount];
        for (var address = 0; address < memory.Length; address++)
            memory[address] = machine.Bus.Peek((ushort)address);

        return new RuntimeSnapshot(machine.GetState(), memory);
    }

    public void Restore(IMachine machine, ISnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(snapshot);

        var runtimeSnapshot = Materialize(snapshot);
        var memory = runtimeSnapshot.Memory.Span;
        for (var address = 0; address < memory.Length; address++)
            machine.Bus.Write((ushort)address, memory[address]);

        if (machine.Devices.GetByRole(DeviceRole.Cpu) is ICpu cpu)
        {
            cpu.PC = runtimeSnapshot.State.PC;
            cpu.Flags = runtimeSnapshot.State.P;
        }
    }

    public async Task SaveAsync(ISnapshot snapshot, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var data = new byte[snapshot.GetSerializedSize()];
        snapshot.Serialize(data);
        await File.WriteAllBytesAsync(filePath, data, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RuntimeSnapshot> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var data = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
        var snapshot = new RuntimeSnapshot();
        snapshot.Deserialize(data);
        return snapshot;
    }

    private static RuntimeSnapshot Materialize(ISnapshot snapshot)
    {
        if (snapshot is RuntimeSnapshot runtimeSnapshot)
            return runtimeSnapshot;

        var data = new byte[snapshot.GetSerializedSize()];
        snapshot.Serialize(data);
        var materialized = new RuntimeSnapshot();
        materialized.Deserialize(data);
        return materialized;
    }
}
