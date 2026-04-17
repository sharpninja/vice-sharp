namespace ViceSharp.Abstractions;

/// <summary>
/// Validates an IArchitectureDescriptor for correctness. Catches errors
/// like overlapping address ranges, missing required devices, invalid
/// clock divisors, and disconnected interrupt sources.
/// </summary>
public interface IArchitectureValidator
{
    /// <summary>Runs all validation rules against the descriptor.</summary>
    bool Validate(IArchitectureDescriptor descriptor);
}