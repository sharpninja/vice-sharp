# AI Code Review and Project Review (aiUnit)

ViceSharp ships AI-assisted **Code Review** and **Project Review** as
integration tests built on [SharpNinja.aiUnit](https://www.nuget.org/packages/SharpNinja.aiUnit).
They live in `tests/ViceSharp.AiReview.Tests` and run a configured frontier-model
agent at xUnit discovery, writing the model's findings to a log. They are
**report-only**: a missing, unauthenticated, or failing agent yields a normalized
`status:"error"` findings JSON and the test still passes. Per the operator
contract, an AI review never fails the build, it writes its output to a log.

## Why a separate test project

aiUnit targets **xUnit v2** (`xunit.extensibility.core` 2.9.x; its review
attributes derive from the v2 `Xunit.Sdk.DataAttribute`). The main
`ViceSharp.TestHarness` targets **xUnit v3**. The two majors define the same
`Xunit.*` types in the same namespaces, so a single project cannot reference both
(it fails to compile with `CS0433`). The AI reviews therefore live in their own
xUnit-v2 project, which also keeps the slow, non-deterministic, host-bound review
calls out of the focused unit suite.

The project is in `ViceSharp.slnx` (so it builds and is discoverable) but is tagged
`[Trait("Category", "AiReview")]` and **excluded from the default Nuke `Test`
gate** (`Category!=Determinism&Category!=AiReview&Category!=ParityPending&Category!=ParityLegacy`).
Run it on demand.

## What it reviews

| Test | Attribute | Scope |
|------|-----------|-------|
| `AiCodeReview_WritesFindingsToLog_NeverFails` | `[AiCodeReview]` | The media-capture surface (`ViceSharp.Core.Media`, `CaptureServiceHost`, capture wiring) for correctness, concurrency, lifetime, and security. |
| `AiProjectReview_WritesFindingsToLog_NeverFails` | `[AiProjectReview]` | Project health: requirements coverage (against the MCP Server), test/Byrd readiness, layering/boundaries, packaging. |

Both prompts are prefixed with a requirements-governance preamble that points the
reviewer at `AGENTS-README-FIRST.yaml` and the MCP Server as the single source of
truth for requirements.

Each review is saved as a timestamped markdown file (the prompt plus the model's
response) under [`docs/reviews/`](reviews/), named
`{kind}-review-{yyyyMMddTHHmmssfffZ}.md`. aiUnit also writes its own per-review run
logs under `tests/ViceSharp.AiReview.Tests/aiunit-results/` (git-ignored).

## Running with the Grok Build CLI (default strategy)

aiUnit's `cli` adapter speaks the **Claude CLI** stdin protocol. To drive **Grok
Build** instead, this repo ships a tiny Claude-protocol shim
(`tools/aiunit-grok-claude-shim`, an executable named `claude`) that forwards
aiUnit's prompt to `grok` in headless prompt-file mode. The active `grok` strategy
in `appsettings.aiunit.json` is therefore `Kind: cli, Command: claude,
Model: grok-build`, resolved through the shim on `PATH`.

```powershell
# 1. Publish the shim (produces claude.exe).
dotnet publish tools/aiunit-grok-claude-shim/ClaudeShim.csproj -c Release `
    -o tools/aiunit-grok-claude-shim/publish

# 2. Prepend the publish folder to PATH so 'claude' resolves to the shim
#    (which calls grok), ahead of any real Claude CLI.
$env:PATH = "F:\GitHub\vice-sharp\tools\aiunit-grok-claude-shim\publish;$env:PATH"

# 3. Make sure the Grok Build CLI is installed and logged in (grok.exe on PATH).
#    Optionally point the shim at the Grok plugin root and a smaller review root:
# $env:GROK_PLUGIN_ROOT       = "F:\GitHub\mcpserver-grok-plugin"
# $env:AIUNIT_GROK_REVIEW_ROOT = "F:\GitHub\vice-sharp"

# 4. Run only the AI reviews.
dotnet test tests/ViceSharp.AiReview.Tests/ViceSharp.AiReview.Tests.csproj `
    --filter "Category=AiReview"

# 5. Read the saved reviews (prompt + response as markdown).
#    docs/reviews/  (code-review-*.md, project-review-*.md)
```

## Other strategies

`appsettings.aiunit.json` also defines `claude` (Claude Code CLI), `claude-api`
(Anthropic HTTP), and `grok-api` (xAI OpenAI-compatible HTTP). Override the active
strategy at run time without editing the file:

```powershell
$env:AIUNIT_STRATEGY = "claude"      # use the Claude CLI directly (no shim needed)
$env:AIUNIT_STRATEGY = "grok-api"    # xAI HTTP API; set $env:XAI_API_KEY first
```

If no strategy resolves (no CLI/shim on PATH, no API key), the review attributes
auto-skip or return a `status:"error"` payload; either way the suite stays green.
