using SharpNinja.AiUnit.Review;

namespace ViceSharp.AiReview.Tests;

/// <summary>
/// SharpNinja.aiUnit AI Code Review + AI Project Review integration tests.
///
/// The <see cref="AiCodeReviewAttribute"/> / <see cref="AiProjectReviewAttribute"/>
/// data attributes run the configured review agent (the <c>grok</c> strategy in
/// appsettings.aiunit.json, driven through the local claude-protocol shim under
/// tools/aiunit-grok-claude-shim) at test execution (the theories set
/// DisableDiscoveryEnumeration so the agent never runs during xUnit discovery) and
/// feed the decorated theory <c>(prompt, resultJson)</c>. aiUnit's review executor never throws: a
/// missing or failing agent yields a normalized <c>status:"error"</c> JSON.
/// These theories therefore assert NOTHING that can fail - they only persist the
/// findings to <see cref="ReviewLog"/>. Per the operator contract: do not fail
/// the test, write the output to a log. Read the findings under
/// tests/ViceSharp.AiReview.Tests/AiReviewLogs/ after a run.
/// </summary>
[Trait("Category", "AiReview")]
public sealed class AiReviewTests
{
    /// <summary>
    /// Requirements-governance preamble prepended to every review prompt. ViceSharp
    /// follows AGENTS-README-FIRST.yaml and treats the MCP Server as the SOLE
    /// authoritative source of requirements; reviews must assess coverage only
    /// against MCP records, return raw aiunit.review.findings.v1 JSON, and emit a
    /// status:"error" findings object (never prose) on any runtime blocker. Kept as
    /// a <c>const</c> so it composes into the review attribute arguments at compile
    /// time.
    /// </summary>
    public const string RequirementsGovernancePreamble =
        "MANDATORY FIRST STEP: read and fully follow AGENTS-README-FIRST.yaml at the repository " +
        "root before reviewing - its session-start, signature/nonce verification, authentication, " +
        "and MCP Server usage instructions all apply to this review. " +
        "The MCP Server described in that marker is the SINGLE SOURCE OF TRUTH for requirements: " +
        "pull the authoritative functional, technical, and test requirements (FR/TR/TEST) from the " +
        "MCP Server requirements API (the /mcpserver/requirements/fr, /tr, and /test endpoints) and " +
        "assess requirements coverage and traceability ONLY against those MCP records. " +
        "FR-to-TR-to-TEST traceability is recorded in the /mcpserver/requirements/mapping endpoint " +
        "(entries shaped {frId, trIds, testIds}), NOT as inline linkedFr/linkedTr fields on a TEST record; " +
        "read that mapping endpoint to assess coverage, and do NOT report a TEST record as untraceable " +
        "merely because it has no inline link fields - that is the expected schema, not a defect. " +
        "Do NOT treat any requirements artifact outside the MCP Server as authoritative - this includes " +
        "docs/Project/Functional-Requirements.md, docs/Project/Technical-Requirements.md, " +
        "docs/Project/Testing-Requirements.md, docs/Project/Requirements-Matrix.md, " +
        "docs/Project/TR-per-FR-Mapping.md, and any GitHub/Azure DevOps wiki export. " +
        "Do NOT raise findings about coverage gaps, counts, or drift in those out-of-band documents; " +
        "they are non-authoritative mirrors of the MCP Server. " +
        "For Grok Build, the workflow.requirements.* names are plugin shim methods, not literal search_tool " +
        "results. If dedicated requirements tools are not visible but pwsh is available, use the Grok plugin " +
        "helper exactly as supported by mcpserver-grok-plugin: set GROK_PLUGIN_ROOT from the process " +
        "environment or F:\\GitHub\\mcpserver-grok-plugin, set PLUGIN_AGENT_NAME=GrokCode, then call " +
        "pwsh.exe -NoProfile -NonInteractive -File \"$env:GROK_PLUGIN_ROOT\\lib\\repl-invoke.ps1\" " +
        "-Method workflow.requirements.listFr -ParamsYaml \"workspacePath: F:\\GitHub\\vice-sharp\"; " +
        "repeat with workflow.requirements.listTr, workflow.requirements.listTest, and " +
        "workflow.requirements.listMappings. This is the supported Grok plugin path, not a raw REST " +
        "or generic helper substitute. " +
        "RUNTIME ERROR CONTRACT: if authentication, subscription, quota, token/context limit, MCP access, " +
        "tool permission, or any other runtime problem prevents completing the review, do not pause, wait " +
        "for input, retry indefinitely, or return prose. Immediately return the same raw " +
        "aiunit.review.findings.v1 JSON object with status:\"error\" and a single finding that explains " +
        "the blocker and exact evidence. " +
        "OUTPUT CONTRACT: return ONLY the raw aiunit.review.findings.v1 JSON object as your entire " +
        "response - no prose preamble, no explanation, and no markdown code fences. When status is pass, " +
        "findings must contain only actionable code defects in the requested review scope; do not include " +
        "informational, process-only, MCP bootstrap, or trust-status notes as findings. If there are no " +
        "actionable scoped code defects, return an empty findings array. ";

    private readonly ITestOutputHelper _output;

    public AiReviewTests(ITestOutputHelper output) => _output = output;

    [Theory(DisableDiscoveryEnumeration = true)]
    [AiCodeReview(
        RequirementsGovernancePreamble +
        "Perform a code-quality, correctness, and security review of the ViceSharp media-capture " +
        "surface: the capture host and recorders under src/ViceSharp.Core/Media (FfmpegVideoRecorder, " +
        "FfmpegVideoEncoding, FfmpegLocator, CaptureAudioTap, WavAudioRecorder, FrameSequenceCapture, " +
        "RecordingAudioBackend), src/ViceSharp.Host/Services/CaptureServiceHost.cs, and the capture " +
        "wiring in src/ViceSharp.Host/Runtime/EmulatorRuntimeSession.cs and " +
        "src/ViceSharp.Host/Runtime/DefaultEmulatorRuntimeFactory.cs. Focus on concurrency around the " +
        "emulation worker (frame/audio tee points, the TCP sockets feeding ffmpeg, lock ordering), " +
        "resource lifetime (process/socket/stream disposal), and input validation. Report findings only.",
        Agent = "grok")]
    public void AiCodeReview_WritesFindingsToLog_NeverFails(string prompt, string resultJson)
    {
        string path = SafeLog("code", prompt, resultJson);
        _output.WriteLine($"AI code review findings written to: {path}");
        _output.WriteLine(resultJson);
        // Report-only gate: intentionally no failing assertions.
    }

    [Theory(DisableDiscoveryEnumeration = true)]
    [AiProjectReview(
        RequirementsGovernancePreamble +
        "Perform a high-level project-health and architecture review of ViceSharp (a C# .NET 10 " +
        "cycle-accurate VICE C64 emulator with an Avalonia UI and a gRPC host control surface). Assess " +
        "requirements coverage and traceability against the MCP Server records, test-suite health and " +
        "Byrd-process (tests-first) readiness, the layering/boundary between ViceSharp.Avalonia and the " +
        "runtime internals (ViceSharp.Abstractions/Architectures/Core/Chips/Host), and packaging/build " +
        "hygiene (Nuke build, MSI/WiX installer). Report findings only.",
        Agent = "grok")]
    public void AiProjectReview_WritesFindingsToLog_NeverFails(string prompt, string resultJson)
    {
        string path = SafeLog("project", prompt, resultJson);
        _output.WriteLine($"AI project review findings written to: {path}");
        _output.WriteLine(resultJson);
        // Report-only gate: intentionally no failing assertions.
    }

    private string SafeLog(string kind, string prompt, string resultJson)
    {
        try
        {
            return ReviewLog.Write(kind, prompt, resultJson);
        }
        catch (Exception ex)
        {
            // Even a log-write failure must not fail the review gate.
            _output.WriteLine($"ReviewLog write failed (non-fatal): {ex.Message}");
            return "(log write failed)";
        }
    }
}
