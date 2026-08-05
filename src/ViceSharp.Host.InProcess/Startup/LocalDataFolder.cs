namespace ViceSharp.Host.Startup;

/// <summary>
/// Default <see cref="ILocalDataFolder"/> built from a root directory path. The
/// Xbox / UWP head constructs this from <c>ApplicationData.Current.LocalFolder.Path</c>;
/// off-console tests construct it from a temp directory. The root is normalised to a
/// full path so it matches the resolver's normalised data-root output exactly.
/// </summary>
public sealed class LocalDataFolder : ILocalDataFolder
{
    /// <summary>The name of the C64 resource subdirectory the VICE data resolver requires.</summary>
    public const string C64SubdirectoryName = "C64";

    /// <summary>The name of the VIC-20 resource subdirectory (Iteration 2).</summary>
    public const string Vic20SubdirectoryName = "VIC20";

    /// <summary>Create a local data folder rooted at <paramref name="rootPath"/>.</summary>
    public LocalDataFolder(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        RootPath = Path.GetFullPath(rootPath);
        C64Path = Path.Combine(RootPath, C64SubdirectoryName);
        Vic20Path = Path.Combine(RootPath, Vic20SubdirectoryName);
    }

    /// <inheritdoc />
    public string RootPath { get; }

    /// <inheritdoc />
    public string C64Path { get; }

    /// <inheritdoc />
    public string Vic20Path { get; }
}
