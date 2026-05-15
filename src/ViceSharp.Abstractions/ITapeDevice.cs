namespace ViceSharp.Abstractions;

/// <summary>
/// Tape device interface.
/// </summary>
public interface ITapeDevice : IPeripheral
{
    /// <summary>True when a tape image is inserted.</summary>
    bool HasTape { get; }

    /// <summary>Insert a tape image.</summary>
    void InsertTape(ReadOnlySpan<byte> tapeImage);

    /// <summary>Eject the currently inserted tape.</summary>
    void EjectTape();
}
