namespace ViceSharp.Architectures.Adhoc;

/// <summary>
/// Thrown when an ad-hoc machine YAML document fails schema or
/// semantic validation. The message identifies the offending field path
/// (e.g. <c>chips[1].baseAddress</c>) where possible.
/// </summary>
public sealed class AdhocMachineValidationException : Exception
{
    public AdhocMachineValidationException(string message)
        : base(message)
    {
    }

    public AdhocMachineValidationException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
