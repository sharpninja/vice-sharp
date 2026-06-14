namespace ViceSharp.Core.Input;

public sealed class C64VkmParseResult
{
    public C64VkmParseResult(C64KeyboardMap keyboardMap, IReadOnlyList<C64VkmDiagnostic> diagnostics)
    {
        KeyboardMap = keyboardMap;
        Diagnostics = diagnostics;
    }

    public C64KeyboardMap KeyboardMap { get; }

    public IReadOnlyList<C64VkmDiagnostic> Diagnostics { get; }

    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == C64VkmDiagnosticSeverity.Error);
}
