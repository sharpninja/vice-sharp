namespace ViceSharp.TestHarness.AdhocMachine;

/// <summary>
/// Resolves the absolute path of the ViceSharp solution root regardless of where
/// the test binary is dropped (bin/Debug, bin/Release, AOT publish, etc.).
/// </summary>
internal static class SolutionRoot
{
    public static string Find()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate ViceSharp.slnx by walking up from " +
            AppContext.BaseDirectory);
    }
}
