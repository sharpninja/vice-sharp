using ViceSharp.Abstractions;

namespace ViceSharp.Core.Snapshots;

/// <summary>
/// FR-SNP-003 production contract for deterministic input replay.
/// Recording keeps input events and periodic snapshot slots; playback and
/// seek apply that log to a machine instead of storing a RAM image per frame.
/// </summary>
public interface IReplayEngine
{
    /// <summary>Records one external input event at a frame and cycle.</summary>
    /// <param name="frame">Frame number of the event.</param>
    /// <param name="cycle">Cycle within that frame.</param>
    /// <param name="key">Host key name to record.</param>
    /// <param name="pressed"><c>true</c> when the key is pressed; <c>false</c> when released.</param>
    void Record(int frame, int cycle, string key, bool pressed);

    /// <summary>Restores the base snapshot, when one exists, and replays recorded input.</summary>
    /// <param name="machine">Machine that receives the restored state and recorded input.</param>
    void Play(IMachine machine);

    /// <summary>
    /// Seeks to <paramref name="frame"/> from the latest periodic snapshot at or
    /// before that frame, then returns the requested frame.
    /// </summary>
    /// <param name="machine">Machine to restore and replay into.</param>
    /// <param name="frame">Requested frame.</param>
    /// <returns>The requested frame.</returns>
    int Seek(IMachine machine, int frame);
}
