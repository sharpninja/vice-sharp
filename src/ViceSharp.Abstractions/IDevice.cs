namespace ViceSharp.Abstractions;

/// <summary>
/// Base interface for all hardware devices in the emulator.
/// Every component in the system implements this interface.
/// </summary>
public interface IDevice
{
    /// <summary>
    /// Unique identifier for this device instance.
    /// </summary>
    DeviceId Id { get; }

    /// <summary>
    /// Human readable name of the device.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Reset the device to its initial power-on state.
    /// </summary>
    void Reset();
}

/// <summary>
/// Strongly typed identifier for devices.
/// </summary>
public readonly struct DeviceId : IEquatable<DeviceId>
{
    public uint Value { get; }

    public DeviceId(uint value)
    {
        Value = value;
    }

    public bool Equals(DeviceId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is DeviceId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"Device#{Value}";

    public static bool operator ==(DeviceId left, DeviceId right) => left.Equals(right);
    public static bool operator !=(DeviceId left, DeviceId right) => !left.Equals(right);
}