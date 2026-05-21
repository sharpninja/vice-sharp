namespace ViceSharp.RomFetch;

public static class ViceDataPathResolver
{
    private static readonly string[] EnvironmentVariableNames =
    [
        "VICESHARP_ROM_PATH",
        "VICE_DATA_PATH",
        "VICE_HOME",
    ];

    public static IReadOnlyList<string> FindDataRoots()
    {
        return EnumerateCandidates()
            .Select(TryNormalizeDataRoot)
            .Where(path => path is not null)
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool TryFindDataRoot(out string dataRoot)
    {
        dataRoot = FindDataRoots().FirstOrDefault() ?? string.Empty;
        return dataRoot.Length > 0;
    }

    public static string FindDataRoot()
    {
        if (TryFindDataRoot(out var dataRoot))
            return dataRoot;

        throw new DirectoryNotFoundException(
            "Could not locate a VICE data root containing C64 resources. Set VICESHARP_ROM_PATH or VICE_DATA_PATH to a directory containing C64/ and DRIVES/, or put x64sc.exe on PATH.");
    }

    public static string NormalizeDataRootOrDefault(string candidate)
    {
        return TryNormalizeDataRoot(candidate) ?? candidate;
    }

    public static bool TryFindDataFile(string architecture, string fileName, out string path)
    {
        foreach (var dataRoot in FindDataRoots())
        {
            var candidate = Path.Combine(dataRoot, architecture, fileName);
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        path = string.Empty;
        return false;
    }

    public static string FindDataFile(string architecture, string fileName)
    {
        if (TryFindDataFile(architecture, fileName, out var path))
            return path;

        throw new FileNotFoundException(
            $"Could not locate VICE data file {architecture}/{fileName}. Set VICESHARP_ROM_PATH or VICE_DATA_PATH to a VICE data root, or put x64sc.exe on PATH.",
            Path.Combine(architecture, fileName));
    }

    private static IEnumerable<string> EnumerateCandidates()
    {
        foreach (var variableName in EnvironmentVariableNames)
        {
            var value = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(value))
                yield return value;
        }

        foreach (var executablePath in FindExecutablesOnPath("x64sc.exe"))
        {
            yield return executablePath;

            foreach (var candidate in EnumerateChocolateyPackageCandidates(executablePath))
                yield return candidate;
        }

        yield break;
    }

    private static IEnumerable<string> FindExecutablesOnPath(string fileName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            yield break;

        foreach (var entry in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string candidate;
            try
            {
                candidate = Path.Combine(Environment.ExpandEnvironmentVariables(entry), fileName);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (File.Exists(candidate))
                yield return candidate;
        }
    }

    private static IEnumerable<string> EnumerateChocolateyPackageCandidates(string executablePath)
    {
        var executableDirectory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
            yield break;

        var binDirectory = new DirectoryInfo(executableDirectory);
        if (!string.Equals(binDirectory.Name, "bin", StringComparison.OrdinalIgnoreCase))
            yield break;

        var packageRoot = binDirectory.Parent;
        if (packageRoot is null)
            yield break;

        var libDirectory = Path.Combine(packageRoot.FullName, "lib");
        if (!Directory.Exists(libDirectory))
            yield break;

        string[] matches;
        try
        {
            matches = Directory.EnumerateFiles(libDirectory, "x64sc.exe", SearchOption.AllDirectories).ToArray();
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }
        catch (IOException)
        {
            yield break;
        }

        foreach (var match in matches)
            yield return match;
    }

    private static string? TryNormalizeDataRoot(string candidate)
    {
        string path;
        try
        {
            path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(candidate));
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }

        if (File.Exists(path))
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is null)
                return null;

            return TryNormalizeDataRoot(directory);
        }

        if (!Directory.Exists(path))
            return null;

        if (ContainsC64Data(path))
            return path;

        var directoryInfo = new DirectoryInfo(path);
        if (string.Equals(directoryInfo.Name, "C64", StringComparison.OrdinalIgnoreCase) &&
            directoryInfo.Parent is { } parent &&
            ContainsC64Data(parent.FullName))
        {
            return parent.FullName;
        }

        if (string.Equals(directoryInfo.Name, "bin", StringComparison.OrdinalIgnoreCase) &&
            directoryInfo.Parent is { } executableRoot &&
            ContainsC64Data(executableRoot.FullName))
        {
            return executableRoot.FullName;
        }

        var dataChild = Path.Combine(path, "data");
        if (ContainsC64Data(dataChild))
            return dataChild;

        var shareViceChild = Path.Combine(path, "share", "vice");
        if (ContainsC64Data(shareViceChild))
            return shareViceChild;

        return null;
    }

    private static bool ContainsC64Data(string dataRoot)
    {
        return Directory.Exists(Path.Combine(dataRoot, "C64"));
    }
}
