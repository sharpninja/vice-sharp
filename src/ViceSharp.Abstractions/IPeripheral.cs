namespace ViceSharp.Abstractions;

/// <summary>
/// External peripheral device connected to the system.
/// </summary>
public interface IPeripheral : IDevice
{
    /// <summary>
    /// Attach peripheral to the host system.
    /// </summary>
    void Attach();

    /// <summary>
    /// Detach peripheral from the host system.
    /// </summary>
    void Detach();

    /// <summary>
    /// True if device is currently attached.
    /// </summary>
    bool IsAttached { get; }
}