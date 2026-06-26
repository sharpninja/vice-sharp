using System.Diagnostics;
using System.Text;
using System.Text.Json;

static string JsonEnvelope(string result, bool isError = false, string? error = null)
{
    var payload = new Dictionary<string, object?>
    {
        ["type"] = "result",
        ["subtype"] = isError ? "error" : "success",
        ["is_error"] = isError,
        ["result"] = result ?? string.Empty
    };

    if (!string.IsNullOrWhiteSpace(error))
    {
        payload["error"] = error;
    }

    return JsonSerializer.Serialize(payload);
}

static string AiUnitRuntimeErrorJson(string title, string detail)
{
    var payload = new
    {
        schemaVersion = "aiunit.review.findings.v1",
        reviewType = "code",
        status = "error",
        summary = "Grok review runtime problem detected by the aiUnit Grok shim.",
        findings = new[]
        {
            new
            {
                severity = "high",
                category = "review-runtime",
                title,
                detail,
                recommendation = "Resolve the Grok CLI runtime issue and rerun the aiUnit review.",
                filePath = "tools/aiunit-grok-claude-shim/Program.cs",
                line = 1,
                ruleId = "AIUNIT-GROK-RUNTIME",
                confidence = 1.0,
                agent = "Grok"
            }
        }
    };

    return JsonSerializer.Serialize(payload);
}

static string ExtractGrokText(string stdout)
{
    if (string.IsNullOrWhiteSpace(stdout))
    {
        return string.Empty;
    }

    using JsonDocument document = JsonDocument.Parse(stdout);
    JsonElement root = document.RootElement;

    if (root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty("text", out JsonElement textElement) &&
        textElement.ValueKind == JsonValueKind.String)
    {
        return textElement.GetString() ?? string.Empty;
    }

    return stdout;
}

static string? ResolveGrokPluginRoot()
{
    string? configured = Environment.GetEnvironmentVariable("AIUNIT_GROK_PLUGIN_ROOT");
    if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
    {
        return configured;
    }

    configured = Environment.GetEnvironmentVariable("GROK_PLUGIN_ROOT");
    if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
    {
        return configured;
    }

    const string defaultLocalRoot = @"F:\GitHub\mcpserver-grok-plugin";
    return Directory.Exists(defaultLocalRoot) ? defaultLocalRoot : null;
}

static bool IsRuntimeProblemLine(string line)
{
    if (string.IsNullOrWhiteSpace(line))
    {
        return false;
    }

    string normalized = line.ToLowerInvariant();
    string[] markers =
    [
        "subscription expired",
        "subscription required",
        "subscription limit",
        "quota exceeded",
        "insufficient quota",
        "rate limit",
        "too many requests",
        "usage limit",
        "token limit",
        "context length",
        "context window",
        "maximum context",
        "billing required",
        "payment required",
        "model unavailable",
        "model is unavailable",
        "press enter",
        "waiting for input",
        "requires confirmation",
        "interactive prompt",
        "paused for input",
        "continue?"
    ];

    return markers.Any(normalized.Contains);
}

try
{
    string prompt = await Console.In.ReadToEndAsync();
    string tempPrompt = Path.Combine(Path.GetTempPath(), "aiunit-grok-prompt-" + Guid.NewGuid().ToString("N") + ".txt");
    string repoRoot = Environment.GetEnvironmentVariable("AIUNIT_GROK_REVIEW_ROOT")
        ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    try
    {
        await File.WriteAllTextAsync(tempPrompt, prompt, Encoding.UTF8);

        var psi = new ProcessStartInfo("grok")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.ArgumentList.Add("--model");
        psi.ArgumentList.Add("grok-build");
        psi.ArgumentList.Add("--output-format");
        psi.ArgumentList.Add("json");
        psi.ArgumentList.Add("--no-alt-screen");
        psi.ArgumentList.Add("--verbatim");
        psi.ArgumentList.Add("--no-plan");
        psi.ArgumentList.Add("--no-memory");
        psi.ArgumentList.Add("--no-subagents");
        psi.ArgumentList.Add("--disable-web-search");
        psi.ArgumentList.Add("--max-turns");
        psi.ArgumentList.Add("100");
        psi.ArgumentList.Add("--permission-mode");
        psi.ArgumentList.Add("bypassPermissions");
        psi.ArgumentList.Add("--cwd");
        psi.ArgumentList.Add(repoRoot);
        psi.ArgumentList.Add("--prompt-file");
        psi.ArgumentList.Add(tempPrompt);

        string? grokPluginRoot = ResolveGrokPluginRoot();
        if (!string.IsNullOrWhiteSpace(grokPluginRoot))
        {
            psi.Environment["GROK_PLUGIN_ROOT"] = grokPluginRoot;
            psi.Environment["PLUGIN_ROOT"] = grokPluginRoot;
            psi.Environment["PLUGIN_AGENT_NAME"] = "GrokCode";
        }

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start grok.");
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        string? runtimeProblem = null;
        DateTime lastStdioUtc = DateTime.UtcNow;
        int idleTimeoutSeconds = int.TryParse(
            Environment.GetEnvironmentVariable("AIUNIT_GROK_IDLE_TIMEOUT_SECONDS"),
            out int configuredIdleTimeoutSeconds)
            ? Math.Max(60, configuredIdleTimeoutSeconds)
            : 900;

        async Task ReadStreamAsync(StreamReader reader, StringBuilder target, string streamName)
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                lastStdioUtc = DateTime.UtcNow;
                target.AppendLine(line);
                if (runtimeProblem is null &&
                    string.Equals(streamName, "stderr", StringComparison.Ordinal) &&
                    IsRuntimeProblemLine(line))
                {
                    runtimeProblem = $"{streamName}: {line.Trim()}";
                    TryKillProcessTree(process);
                }
            }
        }

        Task stdoutTask = ReadStreamAsync(process.StandardOutput, stdout, "stdout");
        Task stderrTask = ReadStreamAsync(process.StandardError, stderr, "stderr");
        Task waitTask = process.WaitForExitAsync();

        while (!waitTask.IsCompleted)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            if (runtimeProblem is not null)
            {
                break;
            }

            if (DateTime.UtcNow - lastStdioUtc > TimeSpan.FromSeconds(idleTimeoutSeconds))
            {
                runtimeProblem = $"No Grok stdout/stderr observed for {idleTimeoutSeconds} seconds.";
                TryKillProcessTree(process);
                break;
            }
        }

        try
        {
            await Task.WhenAll(waitTask, stdoutTask, stderrTask).WaitAsync(TimeSpan.FromSeconds(15));
        }
        catch (TimeoutException)
        {
            runtimeProblem ??= "Grok process did not drain stdout/stderr after shutdown.";
            TryKillProcessTree(process);
        }

        string stdoutText = stdout.ToString().Trim();
        string stderrText = stderr.ToString().Trim();

        if (runtimeProblem is not null)
        {
            string detail = runtimeProblem;
            if (!string.IsNullOrWhiteSpace(stderrText))
            {
                detail += $"{Environment.NewLine}{stderrText}";
            }

            Console.WriteLine(JsonEnvelope(AiUnitRuntimeErrorJson("Grok runtime problem detected", detail)));
            Environment.Exit(0);
        }

        if (process.ExitCode != 0)
        {
            string detail = string.IsNullOrWhiteSpace(stderrText)
                ? $"grok exited with code {process.ExitCode}."
                : stderrText;
            Console.WriteLine(JsonEnvelope(AiUnitRuntimeErrorJson("Grok exited before producing a review", detail)));
            Environment.Exit(0);
        }

        string result = ExtractGrokText(stdoutText).Trim();

        if (string.IsNullOrWhiteSpace(result))
        {
            Console.WriteLine(JsonEnvelope(AiUnitRuntimeErrorJson(
                "Grok produced no review JSON",
                string.IsNullOrWhiteSpace(stderrText) ? "Grok stdout was empty." : stderrText)));
            Environment.Exit(0);
        }

        Console.WriteLine(JsonEnvelope(result));
    }
    finally
    {
        try
        {
            File.Delete(tempPrompt);
        }
        catch
        {
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine(JsonEnvelope(string.Empty, true, ex.Message));
    Environment.Exit(1);
}

static void TryKillProcessTree(Process process)
{
    try
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }
    catch
    {
    }
}
