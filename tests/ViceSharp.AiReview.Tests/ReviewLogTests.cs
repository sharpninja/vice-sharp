using System.Text.RegularExpressions;

namespace ViceSharp.AiReview.Tests;

/// <summary>
/// Deterministic guards for <see cref="ReviewLog"/> (the report-only sink the AI
/// review theories write to). These run with no AI agent and validate that each
/// review is persisted as a timestamped markdown file containing the prompt and
/// the response. They write to a temp directory so the default test gate never
/// touches docs/reviews.
/// </summary>
public sealed class ReviewLogTests
{
    [Fact]
    public void Write_CreatesTimestampedMarkdown_WithPromptAndResponse()
    {
        var dir = NewTempDir();
        try
        {
            const string result =
                "{\"schemaVersion\":\"aiunit.review.findings.v1\",\"status\":\"pass\"," +
                "\"summary\":\"All good.\",\"findings\":[]}";

            string path = ReviewLog.Write("code", "review the media surface", result, dir);

            Assert.True(File.Exists(path), $"markdown was not written: {path}");
            Assert.EndsWith(".md", path);
            // Timestamped file name: <kind>-review-<yyyyMMddTHHmmssfffZ>.md
            Assert.Matches(@"^code-review-\d{8}T\d+Z\.md$", Path.GetFileName(path));

            string md = File.ReadAllText(path);
            Assert.Contains("## Prompt", md);
            Assert.Contains("review the media surface", md);
            Assert.Contains("## Response", md);
            Assert.Contains("aiunit.review.findings.v1", md);
            Assert.Contains("pass", md); // parsed status surfaced in the header
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void Write_PreservesRawText_WhenResponseIsNotJson()
    {
        var dir = NewTempDir();
        try
        {
            string path = ReviewLog.Write("project", "p", "not-json-payload", dir);

            Assert.StartsWith("project-review-", Path.GetFileName(path));
            string md = File.ReadAllText(path);
            Assert.Contains("not-json-payload", md);
            Assert.Contains("## Response", md);
        }
        finally
        {
            CleanupTempDir(dir);
        }
    }

    [Fact]
    public void ReviewsDirectory_EndsWithDocsReviews()
    {
        Assert.EndsWith(Path.Combine("docs", "reviews"), ReviewLog.ReviewsDirectory);
    }

    private static string NewTempDir() =>
        Path.Combine(Path.GetTempPath(), "vice-aireview-" + Guid.NewGuid().ToString("N"));

    private static void CleanupTempDir(string dir)
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
