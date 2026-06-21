using System.Text;
using System.Text.Json;

namespace ViceSharp.AiReview.Tests;

/// <summary>
/// Writes aiUnit review findings JSON to a per-run log file under
/// <c>tests/ViceSharp.AiReview.Tests/AiReviewLogs/</c> and appends a one-line
/// summary to <c>ai-review.log</c>. Report-only: the AI review theories call
/// this and assert nothing, so a review run never fails the test suite
/// (operator contract: do not fail the test, write the output to a log).
/// </summary>
public static class ReviewLog
{
    private static readonly object WriteGate = new();

    /// <summary>Absolute directory the review logs are written to.</summary>
    public static string LogDirectory { get; } = ResolveLogDirectory();

    /// <summary>
    /// Writes <paramref name="resultJson"/> (and the originating prompt) to a
    /// unique log file, appends a summary line to the aggregate log, and
    /// returns the per-run file path. The result is embedded as parsed JSON
    /// when valid, otherwise as a raw string so error payloads are still kept.
    /// </summary>
    public static string Write(string reviewKind, string prompt, string resultJson)
    {
        Directory.CreateDirectory(LogDirectory);

        string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
        string fileName = $"{Sanitize(reviewKind)}-{stamp}-{Guid.NewGuid():N}.json";
        string path = Path.Combine(LogDirectory, fileName);

        lock (WriteGate)
        {
            File.WriteAllText(path, BuildEnvelope(reviewKind, prompt, resultJson, stamp));
            File.AppendAllText(
                Path.Combine(LogDirectory, "ai-review.log"),
                SummaryLine(reviewKind, resultJson, fileName) + Environment.NewLine);
        }

        return path;
    }

    private static string BuildEnvelope(string reviewKind, string prompt, string resultJson, string stamp)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("capturedUtc", stamp);
            writer.WriteString("reviewKind", reviewKind);
            writer.WriteString("prompt", prompt);
            writer.WritePropertyName("result");
            if (TryParse(resultJson, out JsonDocument? doc) && doc is not null)
            {
                using (doc)
                {
                    doc.RootElement.WriteTo(writer);
                }
            }
            else
            {
                writer.WriteStringValue(resultJson);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string SummaryLine(string reviewKind, string resultJson, string fileName)
    {
        string status = "unknown";
        if (TryParse(resultJson, out JsonDocument? doc) && doc is not null)
        {
            using (doc)
            {
                if (doc.RootElement.TryGetProperty("status", out JsonElement s)
                    && s.ValueKind == JsonValueKind.String)
                {
                    status = s.GetString() ?? "unknown";
                }
            }
        }

        return $"{DateTime.UtcNow:O}  {reviewKind,-7}  status={status,-7}  {fileName}";
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

    private static string Sanitize(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value;
    }

    private static string ResolveLogDirectory()
    {
        // Walk up from the test output directory to the repo root (the folder
        // holding ViceSharp.slnx), then anchor the log dir under the test project.
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
        {
            dir = dir.Parent;
        }

        string root = dir?.FullName ?? AppContext.BaseDirectory;
        return Path.Combine(root, "tests", "ViceSharp.AiReview.Tests", "AiReviewLogs");
    }
}
