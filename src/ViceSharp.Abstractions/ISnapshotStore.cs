namespace ViceSharp.Abstractions;

/// <summary>
/// Manages snapshot persistence and the in-memory history window.
/// Supports save/load to disk and ring-buffer retention of recent snapshots.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>Captures a snapshot of the current machine state.</summary>
    ISnapshot Capture(IMachine machine);

    /// <summary>Restores machine state from a snapshot.</summary>
    void Restore(IMachine machine, ISnapshot snapshot);
}