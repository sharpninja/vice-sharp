namespace ViceSharp.Core.Input;

public sealed record C64VkmDiagnostic(
    C64VkmDiagnosticSeverity Severity,
    string Message,
    string? Path,
    int LineNumber);

public enum C64VkmDiagnosticSeverity
{
    Info,
    Warning,
    Error
}
