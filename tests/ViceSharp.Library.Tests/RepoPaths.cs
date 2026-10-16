using System.Reflection;

namespace ViceSharp.Library.Tests;

/// <summary>
/// Resolves the repository root at test time so packaging/config tests can read committed
/// artifacts (NuGet.config, Directory.Packages.props). The root is injected by the csproj as
/// an <see cref="AssemblyMetadataAttribute"/> named "RepoRoot" (same idiom as the TestHarness).
/// </summary>
internal static class RepoPaths
{
    /// <summary>Absolute path to the repository root, with a trailing separator normalized away.</summary>
    public static string Root { get; } = ResolveRoot();

    private static string ResolveRoot()
    {
        string? meta = typeof(RepoPaths).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "RepoRoot")?.Value;

        return meta is { Length: > 0 }
            ? Path.GetFullPath(meta)
            : Directory.GetCurrentDirectory();
    }

    /// <summary>Absolute path to a file relative to the repository root.</summary>
    public static string File(string relative) => Path.Combine(Root, relative);
}
