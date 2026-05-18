namespace ViceSharp.Architectures.Multisystem;

/// <summary>
/// Thrown by MultiSystemYamlLoader when a multi-system YAML document fails
/// schema validation or topology constraints (duplicate ids, unreferenced
/// buses, missing host, etc.).
/// </summary>
public sealed class MultiSystemValidationException : Exception
{
    public MultiSystemValidationException(string message) : base(message) { }
    public MultiSystemValidationException(string message, Exception inner) : base(message, inner) { }
}
