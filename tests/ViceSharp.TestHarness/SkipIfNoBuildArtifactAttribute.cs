using System.Runtime.CompilerServices;
using Xunit;

namespace ViceSharp.TestHarness;

/// <summary>
/// Skips the test when the specified build artifact (relative to the test
/// assembly output directory) is not present. Used for integration smoke
/// tests that require a built executable to be present.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class SkipIfNoBuildArtifactAttribute : FactAttribute
{
    public SkipIfNoBuildArtifactAttribute(
        string artifactName,
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber)
    {
        var assemblyDir = Path.GetDirectoryName(typeof(SkipIfNoBuildArtifactAttribute).Assembly.Location)!;
        var artifactPath = Path.Combine(assemblyDir, artifactName);
        if (!File.Exists(artifactPath))
        {
            Skip = $"Skipping: artifact '{artifactName}' not found at '{artifactPath}'.";
        }
    }
}
