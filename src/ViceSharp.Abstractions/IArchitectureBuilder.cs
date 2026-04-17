namespace ViceSharp.Abstractions;

/// <summary>
/// Constructs a running IMachine from an IArchitectureDescriptor.
/// Instantiates devices, wires address spaces, connects interrupts,
/// configures clocks, and runs validation.
/// </summary>
public interface IArchitectureBuilder
{
    /// <summary>Builds a machine from the given architecture descriptor.</summary>
    IMachine Build(IArchitectureDescriptor descriptor);
}