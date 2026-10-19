using System.Xml.Linq;

namespace ViceSharp.Build.Tests.Placement;

/// <summary>
/// TR-BUILD-001 ac-12: the placement rules for Nuke build tests, as pure functions over project and
/// solution files so the same rule runs against fixtures (mock gate) and against the real tree.
/// </summary>
internal static class BuildPlacementRules
{
    /// <summary>Repository-relative path of the Nuke build tests project.</summary>
    public const string BuildTestsProject = "build.tests/ViceSharp.Build.Tests.csproj";

    /// <summary>
    /// Returns the <c>Compile</c> item includes of <paramref name="projectPath"/> that resolve to a file
    /// under <c>&lt;repoRoot&gt;/build/</c>.
    /// </summary>
    public static IReadOnlyList<string> FindBuildSourceCompileItems(string projectPath, string repoRoot)
    {
        string projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectPath))!;
        string buildRoot = Path.GetFullPath(Path.Combine(repoRoot, "build")) + Path.DirectorySeparatorChar;
        var document = XDocument.Load(projectPath);
        var hits = new List<string>();
        foreach (var item in document.Descendants().Where(e => e.Name.LocalName == "Compile"))
        {
            string? include = item.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
                continue;
            string normalized = include.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            string resolved = Path.GetFullPath(Path.Combine(projectDirectory, normalized));
            if (resolved.StartsWith(buildRoot, StringComparison.Ordinal))
                hits.Add(include);
        }

        return hits;
    }

    /// <summary>Returns true when the solution file lists a project at <paramref name="projectRelativePath"/>.</summary>
    public static bool SolutionContainsProject(string solutionPath, string projectRelativePath)
    {
        string wanted = Normalize(projectRelativePath);
        var document = XDocument.Load(solutionPath);
        return document.Descendants()
            .Where(e => e.Name.LocalName == "Project")
            .Select(e => e.Attribute("Path")?.Value)
            .Any(path => path is not null && string.Equals(Normalize(path), wanted, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Every product test project under <c>&lt;repoRoot&gt;/tests</c>, excluding build output.</summary>
    public static IEnumerable<string> ProductTestProjects(string repoRoot)
    {
        string testsRoot = Path.Combine(repoRoot, "tests");
        return Directory.EnumerateFiles(testsRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Order(StringComparer.Ordinal);
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('.', '/');
}
