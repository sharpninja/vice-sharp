namespace ViceSharp.SourceGen;

/// <summary>
/// Marks a device class for automatic registration and wiring.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ViceSharpDeviceAttribute : Attribute
{
    /// <summary>
    /// Device role identifier.
    /// </summary>
    public int Role { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ViceSharpDeviceAttribute"/> class.
    /// </summary>
    /// <param name="role">The device role.</param>
    public ViceSharpDeviceAttribute(int role)
    {
        Role = role;
    }
}
