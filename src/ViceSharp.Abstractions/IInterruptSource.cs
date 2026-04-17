namespace ViceSharp.Abstractions;

/// <summary>
/// Identifies a device that can raise interrupts. Used by IInterruptLine
/// to track assertion state per source.
/// </summary>
public interface IInterruptSource : IDevice
{
    /// <summary>Unique identifier for this interrupt source.</summary>
    DeviceId SourceId { get; }

    /// <summary>The interrupt line(s) this source is connected to.</summary>
    IReadOnlyList<IInterruptLine> ConnectedLines { get; }
}
