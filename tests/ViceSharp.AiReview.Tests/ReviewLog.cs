using System.Text;
using System.Text.Json;

namespace ViceSharp.AiReview.Tests;

/// <summary>
/// Writes each aiUnit review (the prompt and the model's response) to a
/// timestamped markdown file under <c>docs/reviews/</c>. Report-only: the AI
/// review theories call this and assert nothing, so a review run never fails the
/// test suite (operator contract: do not fail the test, write the output to a
/// file). File name: <c>{kind}-review-{yyyyMMddTHHmmssfffZ}.md</c> (sorts
/// chronologically).
/// </summary>
public static class ReviewLog
{
    private static readonly object WriteGate = new();

    /// <summary>Absolute <c>docs/reviews</c> directory review markdown is written to.</summary>
    public static string ReviewsDirectory { get; } = ResolveReviewsDirectory();

    /// <summary>
    /// Writes the review to a timestamped markdown file under
    /// <see cref="ReviewsDirectory"/> and returns its path.
    /// </summary>
    public static string Write(string reviewKind, string prompt, string resultJson)
        => Write(reviewKind, prompt, resultJson, ReviewsDirectory);

    /// <summary>
    /// Writes the review to a timestamped markdown file under
    /// <paramref name="targetDirectory"/> (used by tests to redirect output) and
    /// returns its path. The response is embedded as a pretty-printed JSON block
    /// when it parses, otherwise verbatim so error payloads are still preserved.
    /// </summary>
    public static string Write(string reviewKind, string prompt, string resultJson, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
        string fileName = $"{Sanitize(reviewKind)}-review-{stamp}.md";
        string path = Path.Combine(targetDirectory, fileName);

        lock (WriteGate)
        {
            File.WriteAllText(path, BuildMarkdown(reviewKind, prompt, resultJson, stamp), new UTF8Encoding(false));
        }

        return path;
    }

    private static string BuildMarkdown(string reviewKind, string prompt, string resultJson, string stamp)
    {
        var (status, summary, prettyResponse, isJson) = Describe(resultJson);

        var sb = new StringBuilder();
        sb.Append("# ").Append(Title(reviewKind)).AppendLine(" Review");
        sb.AppendLine();
        sb.Append("- **Captured (UTC):** ").AppendLine(stamp);
        sb.Append("- **Review kind:** ").AppendLine(reviewKind);
        sb.Append("- **Status:** ").AppendLine(status);
        if (!string.IsNullOrWhiteSpace(summary))
        {
            sb.Append("- **Summary:** ").AppendLine(summary);
        }

        sb.AppendLine();
        sb.AppendLine("## Prompt");
        sb.AppendLine();
        sb.AppendLine("```text");
        sb.AppendLine(prompt);
        sb.AppendLine("```");

        sb.AppendLine();
        sb.AppendLine("## Response");
        sb.AppendLine();
        sb.AppendLine(isJson ? "```json" : "```text");
        sb.AppendLine(prettyResponse);
        sb.AppendLine("```");

        return sb.ToString();
    }

    private static (string Status, string Summary, string Response, bool IsJson) Describe(string resultJson)
    {
        if (!TryParse(resultJson, out JsonDocument? doc) || doc is null)
        {
            return ("unknown", string.Empty, resultJson, false);
        }

        using (doc)
        {
            JsonElement root = doc.RootElement;
            string status = root.TryGetProperty("status", out JsonElement s) && s.ValueKind == JsonValueKind.String
                ? s.GetString() ?? "unknown"
                : "unknown";
            string summary = root.TryGetProperty("summary", out JsonElement m) && m.ValueKind == JsonValueKind.String
                ? m.GetString() ?? string.Empty
                : string.Empty;
            string pretty = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
            return (status, summary, pretty, true);
        }
    }

    private static bool TryParse(string json, out JsonDocument? doc)
    {
        try
        {
            doc = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            doc = null;
            return false;
        }
    }

    private static string Title(string reviewKind) =>
        reviewKind switch
        {
            "code" => "AI Code",
            "project" => "AI Project",
            "plan" => "AI Plan",
            _ => "AI " + char.ToUpperInvariant(reviewKind.Length > 0 ? reviewKind[0] : 'r') +
                 (reviewKind.Length > 1 ? reviewKind[1..] : string.Empty),
        };

    private static string Sanitize(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value;
    }

    private static string ResolveReviewsDirectory()
    {
        // Walk up from the test output directory to the repo root (the folder
        // holding ViceSharp.slnx), then anchor at docs/reviews.
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
        {
            dir = dir.Parent;
        }

        string root = dir?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(root, "docs", "reviews");
    }
}
