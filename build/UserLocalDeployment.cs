using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ViceSharp.Build.Packaging;

/// <summary>TR-BUILD-001 / TR-PLAT-001: user-owned Linux desktop installation paths.</summary>
internal sealed record UserDeploymentPaths(string UserHome, string DataHome)
{
    internal string InstallRoot => Path.Combine(DataHome, "vicesharp");
    internal string Releases => Path.Combine(InstallRoot, "releases");
    internal string Current => Path.Combine(InstallRoot, "current");
    internal string Launcher => Path.Combine(UserHome, ".local", "bin", "vicesharp");
    internal string Desktop => Path.Combine(DataHome, "applications", "vicesharp.desktop");
    internal string Icon => Path.Combine(DataHome, "icons", "hicolor", "scalable", "apps", "vicesharp.svg");
}

/// <summary>One fully published, retained application release.</summary>
internal sealed record UserDeploymentResult(string ReleasePath, UserDeploymentPaths Paths);

/// <summary>Installs self-contained Linux payloads without package managers or system writes.</summary>
internal static class UserLocalDeployment
{
    private const string Marker = "Managed by ViceSharp DeployUser";
    private const UnixFileMode Readable = UnixFileMode.UserRead | UnixFileMode.UserWrite
        | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
    private const UnixFileMode Executable = Readable | UnixFileMode.UserExecute
        | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint GetEffectiveUserId();

    [DllImport("libc", EntryPoint = "rename", SetLastError = true)]
    private static extern int Rename([MarshalAs(UnmanagedType.LPUTF8Str)] string source,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string destination);

    private const int AtCurrentDirectory = -100;
    private const int AtSymlinkNoFollow = 0x100;
    private const uint StatxOwnerAndMode = 0xA;

    // Linux's statx ABI has a fixed layout across supported architectures.
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct FileOwnership
    {
        [FieldOffset(0)] internal uint Mask;
        [FieldOffset(20)] internal uint UserId;
        [FieldOffset(28)] internal ushort Mode;
    }

    [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    private static extern int Statx(int directory, [MarshalAs(UnmanagedType.LPUTF8Str)] string path,
        int flags, uint mask, out FileOwnership ownership);

    internal static UserDeploymentPaths ForCurrentUser()
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("DeployUser requires Linux.");
        return CreatePaths(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Environment.GetEnvironmentVariable("XDG_DATA_HOME"), GetEffectiveUserId() == 0);
    }

    internal static UserDeploymentPaths CreatePaths(string userHome, string? dataHome, bool isRoot)
    {
        if (isRoot)
            throw new ArgumentException("DeployUser must run as a normal user, never root.");
        string userDirectory = AbsolutePath(userHome);
        if (userDirectory == Path.GetPathRoot(userDirectory))
            throw new ArgumentException("The user home must be a non-root directory.");
        string dataDirectory = string.IsNullOrEmpty(dataHome)
            ? Path.Combine(userDirectory, ".local", "share") : AbsolutePath(dataHome);
        if (!Within(dataDirectory, userDirectory))
            throw new ArgumentException("XDG_DATA_HOME must be an absolute directory inside the user home.");
        return new(userDirectory, dataDirectory);
    }

    /// <summary>
    /// Copies into a new retained release, publishes managed desktop files, then atomically
    /// switches current. Failed activation restores prior entrypoints; releases are never deleted.
    /// 2026-11-24: Existing managed installs can follow owned links at .local and .local/bin.
    /// </summary>
    internal static UserDeploymentResult Install(string payload, string icon, string version,
        UserDeploymentPaths paths, Action? beforeActivation = null)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("DeployUser requires Linux.");
        if (paths != CreatePaths(paths.UserHome, paths.DataHome, GetEffectiveUserId() == 0))
            throw new ArgumentException("Deployment paths must be normalized user-local paths.");
        if (!Regex.IsMatch(version, @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
            throw new InvalidOperationException("DeployUser requires a numeric X.Y.Z version.");
        payload = AbsolutePath(payload);
        icon = AbsolutePath(icon);
        var directoryLinks = ValidateTargets(paths);
        string resolvedRoot = ResolveManagedPath(paths.InstallRoot, directoryLinks);
        if (Within(resolvedRoot, payload) || Within(payload, resolvedRoot))
            throw new InvalidOperationException("The publish payload and installed releases must be separate directories.");
        var entries = ValidatePayload(payload);
        NoLinks(icon);
        if (!File.Exists(icon) || new FileInfo(icon).Length == 0)
            throw new InvalidOperationException("The desktop icon is missing or empty.");

        Directory.CreateDirectory(paths.InstallRoot);
        string ownership = Path.Combine(paths.InstallRoot, ".managed-by-deploy-user");
        if (!File.Exists(ownership))
            File.WriteAllText(ownership, Marker + "\n");
        using var deploymentLock = new FileStream(Path.Combine(paths.InstallRoot, ".deploy.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var activeLinks = ValidateTargets(paths);
        if (directoryLinks.Count != activeLinks.Count || directoryLinks.Any(link =>
                !activeLinks.TryGetValue(link.Key, out var target) || target != link.Value))
            throw new InvalidOperationException("DeployUser directory links changed while acquiring the deployment lock.");
        Directory.CreateDirectory(paths.Releases);
        string unique = Guid.NewGuid().ToString("N");
        string staging = Path.Combine(paths.Releases, ".staging-" + unique);
        Directory.CreateDirectory(staging);
        foreach (var entry in entries)
        {
            string source = Path.Combine(payload, entry);
            string destination = Path.Combine(staging, entry);
            NoLinks(source);
            if (Directory.Exists(source))
                Directory.CreateDirectory(destination);
            else
            {
                File.Copy(source, destination);
                File.SetUnixFileMode(destination, File.GetUnixFileMode(source));
            }
        }
        ValidatePayload(ResolveManagedPath(staging, activeLinks));
        string release = Path.Combine(paths.Releases, version + "-" + unique);
        Directory.Move(staging, release);

        string executable = Path.Combine(paths.Current, "ViceSharp.Avalonia");
        string launcher = $"#!/bin/sh\n# {Marker}\nexec {ShellQuote(executable)} \"$@\"\n";
        string desktop = $"[Desktop Entry]\n# {Marker}\nType=Application\nName=ViceSharp\n"
            + $"Comment=Commodore emulator\nExec=/bin/sh {DesktopArgument(paths.Launcher)} %f\n"
            + "Icon=vicesharp\nTerminal=false\nCategories=Game;Emulator;\n";
        string iconText = File.ReadAllText(icon) + $"\n<!-- {Marker} -->\n";
        var saved = new List<SavedEntrypoint>();
        try
        {
            Publish(paths.Launcher, Encoding.UTF8.GetBytes(launcher), Executable, saved);
            Publish(paths.Desktop, Encoding.UTF8.GetBytes(desktop), Readable, saved);
            Publish(paths.Icon, Encoding.UTF8.GetBytes(iconText), Readable, saved);
            beforeActivation?.Invoke();
            string pending = Path.Combine(paths.InstallRoot, ".current-" + unique);
            Directory.CreateSymbolicLink(pending, release);
            // Same-directory rename is the final activation step. The previous release remains intact.
            // File.Move treats directory symlinks as missing files; POSIX rename
            // replaces the link itself without following either release directory.
            if (Rename(pending, paths.Current) != 0)
                throw new IOException("Could not atomically activate the user release.",
                    new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError()));
        }
        catch
        {
            foreach (var previous in saved.AsEnumerable().Reverse())
            {
                if (previous.Bytes is null)
                    File.Move(previous.Path, previous.Path + ".failed-" + unique);
                else
                    AtomicWrite(previous.Path, previous.Bytes, previous.Mode);
            }
            throw;
        }
        return new(release, paths);
    }

    private static string AbsolutePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path) || path.Any(char.IsControl))
            throw new ArgumentException("Deployment paths must be absolute and contain no control characters.");
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private static bool Within(string path, string root) => path == root
        || path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    private static void NoLinks(string path, IReadOnlyDictionary<string, string>? directoryLinks = null)
    {
        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            FileAttributes attributes;
            try { attributes = File.GetAttributes(current); }
            catch (FileNotFoundException) { continue; }
            catch (DirectoryNotFoundException) { continue; }
            if ((attributes & FileAttributes.ReparsePoint) != 0 && directoryLinks?.ContainsKey(current) != true)
                throw new InvalidOperationException($"DeployUser refuses symbolic-link paths: {current}");
        }
    }

    /// <summary>
    /// Resolves the two conventional directory links when an existing managed install has been relocated.
    /// 2026-11-24: Require owned links and private, trusted directory targets before allowing an upgrade.
    /// </summary>
    private static Dictionary<string, string> ManagedDirectoryLinks(UserDeploymentPaths paths)
    {
        NoLinks(paths.UserHome);
        var links = new Dictionary<string, string>(StringComparer.Ordinal);
        string local = Path.Combine(paths.UserHome, ".local");
        uint userId = GetEffectiveUserId();
        foreach (string logical in new[] { local, Path.Combine(local, "bin") })
        {
            string physical = ResolveManagedPath(logical, links);
            var info = new DirectoryInfo(physical);
            if (info.LinkTarget is null)
                continue;

            links.Add(logical, ResolveOwnedDirectoryLink(info, userId));
        }
        return links;
    }

    /// <summary>
    /// Verifies each hop and the final directory without trusting aliases from other accounts.
    /// 2026-11-24: Support a launcher directory reached through a second user-owned link.
    /// </summary>
    private static string ResolveOwnedDirectoryLink(DirectoryInfo info, uint userId)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (info.LinkTarget is not null)
        {
            string parent = info.Parent!.FullName;
            NoLinks(parent);
            CheckDirectoryOwners(parent, userId);
            if (!visited.Add(info.FullName) || ReadOwnership(info.FullName).UserId != userId)
                throw new InvalidOperationException($"DeployUser refuses an unowned or cyclic directory link: {info.FullName}");
            info = new DirectoryInfo(info.ResolveLinkTarget(false)!.FullName);
        }

        NoLinks(info.FullName);
        if (!info.Exists || ReadOwnership(info.FullName).UserId != userId)
            throw new InvalidOperationException($"DeployUser requires a user-owned directory target: {info.FullName}");
        CheckDirectoryOwners(info.FullName, userId);
        return info.FullName;
    }

    private static FileOwnership ReadOwnership(string path)
    {
        if (Statx(AtCurrentDirectory, path, AtSymlinkNoFollow, StatxOwnerAndMode, out var owner) != 0
            || (owner.Mask & StatxOwnerAndMode) != StatxOwnerAndMode)
            throw new InvalidOperationException($"DeployUser could not verify path ownership: {path}");
        return owner;
    }

    private static void CheckDirectoryOwners(string path, uint userId)
    {
        for (string? current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            var ownership = ReadOwnership(current);
            uint owner = ownership.UserId;
            var mode = (UnixFileMode)ownership.Mode;
            bool shared = (mode & (UnixFileMode.GroupWrite | UnixFileMode.OtherWrite)) != 0;
            bool stickyRoot = owner == 0 && (mode & UnixFileMode.StickyBit) != 0;
            if ((owner != userId && owner != 0) || (shared && !stickyRoot))
                throw new InvalidOperationException($"DeployUser refuses an untrusted directory on the link path: {current}");
        }
    }

    private static string ResolveManagedPath(string path, IReadOnlyDictionary<string, string> links)
    {
        foreach (var link in links.OrderByDescending(link => link.Key.Length))
            if (Within(path, link.Key))
                return link.Value + path[link.Key.Length..];
        return path;
    }

    private static Dictionary<string, string> ValidateTargets(UserDeploymentPaths paths)
    {
        var links = ManagedDirectoryLinks(paths);
        foreach (string directory in new[] { paths.UserHome, paths.InstallRoot, paths.Releases,
                     Path.GetDirectoryName(paths.Launcher)!, Path.GetDirectoryName(paths.Desktop)!, Path.GetDirectoryName(paths.Icon)! })
        {
            NoLinks(directory, links);
            if (File.Exists(directory))
                throw new InvalidOperationException($"A file blocks the deployment directory: {directory}");
        }
        string ownership = Path.Combine(paths.InstallRoot, ".managed-by-deploy-user");
        NoLinks(ownership, links);
        NoLinks(Path.Combine(paths.InstallRoot, ".deploy.lock"), links);
        if ((links.Count > 0 || Directory.Exists(paths.InstallRoot))
            && (!File.Exists(ownership) || File.ReadAllText(ownership).Trim() != Marker))
            throw new InvalidOperationException($"DeployUser refuses an unmanaged installation directory: {paths.InstallRoot}");
        foreach (string file in new[] { paths.Launcher, paths.Desktop, paths.Icon })
        {
            NoLinks(file, links);
            if (Directory.Exists(file) || (File.Exists(file) && !File.ReadAllText(file).Contains(Marker, StringComparison.Ordinal)))
                throw new InvalidOperationException($"DeployUser refuses to replace an unmanaged entrypoint: {file}");
        }
        var current = new DirectoryInfo(paths.Current);
        if (current.LinkTarget is not null)
        {
            var target = current.ResolveLinkTarget(false);
            if (target is null || Path.GetDirectoryName(target.FullName) != paths.Releases || !Directory.Exists(target.FullName))
                throw new InvalidOperationException("The current release link must select an existing managed release.");
            NoLinks(target.FullName, links);
        }
        else if (Directory.Exists(paths.Current) || File.Exists(paths.Current))
            throw new InvalidOperationException("DeployUser refuses to replace an unmanaged current path.");
        return links;
    }

    private static List<string> ValidatePayload(string payload)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException();
        NoLinks(payload);
        if (!Directory.Exists(payload))
            throw new InvalidOperationException("The self-contained publish payload is missing.");
        var entries = new List<string>();
        var directories = new Stack<string>();
        directories.Push(payload);
        while (directories.TryPop(out string? directory))
        {
            foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
            {
                NoLinks(entry);
                entries.Add(Path.GetRelativePath(payload, entry));
                if (Directory.Exists(entry))
                    directories.Push(entry);
            }
        }
        foreach (string required in new[] { "ViceSharp.Avalonia", "ViceSharp.Avalonia.dll", "ViceSharp.Avalonia.runtimeconfig.json",
                     "libcoreclr.so", "libhostfxr.so", "Microsoft.AspNetCore.dll", "libSkiaSharp.so", "libHarfBuzzSharp.so" })
        {
            string file = Path.Combine(payload, required);
            if (!File.Exists(file) || new FileInfo(file).Length == 0)
                throw new InvalidOperationException($"The self-contained publish payload is missing {required}.");
        }
        if (!entries.Any(entry => entry.EndsWith(".vkm", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("The self-contained publish payload is missing bundled VICE keymaps.");
        if ((File.GetUnixFileMode(Path.Combine(payload, "ViceSharp.Avalonia")) & UnixFileMode.UserExecute) == 0)
            throw new InvalidOperationException("The published ViceSharp apphost must be executable by its owner.");
        return entries;
    }

    private sealed record SavedEntrypoint(string Path, byte[]? Bytes, UnixFileMode Mode);

    private static void Publish(string path, byte[] bytes, UnixFileMode mode, List<SavedEntrypoint> saved)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var previous = new SavedEntrypoint(path, File.Exists(path) ? File.ReadAllBytes(path) : null,
            File.Exists(path) ? File.GetUnixFileMode(path) : mode);
        AtomicWrite(path, bytes, mode);
        saved.Add(previous);
    }

    private static void AtomicWrite(string path, byte[] bytes, UnixFileMode mode)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException();
        string temporary = path + ".pending-" + Guid.NewGuid().ToString("N");
        File.WriteAllBytes(temporary, bytes);
        File.SetUnixFileMode(temporary, mode);
        File.Move(temporary, path, overwrite: true);
    }

    private static string ShellQuote(string argument) => "'" + argument.Replace("'", "'\"'\"'") + "'";

    // Desktop Entry has two escape layers: quoted Exec argument, then desktop string.
    // https://specifications.freedesktop.org/desktop-entry/latest/exec-variables.html
    private static string DesktopArgument(string argument)
    {
        string quoted = argument.Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("$", "\\$").Replace("`", "\\`").Replace("%", "%%");
        return "\"" + quoted.Replace("\\", "\\\\") + "\"";
    }
}
