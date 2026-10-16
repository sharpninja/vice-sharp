namespace ViceSharp.TestHarness.Vic20;

using System.Security.Cryptography;
using System.Text.Json;
using ViceSharp.Core;

/// <summary>
/// Validates the pinned native xvic oracle before VIC-20 differential tests run.
/// </summary>
internal static class XvicOraclePrerequisites
{
    private const string ManifestRelativePath = "docs/requirements/oracles/vic20-xvic-manifest.json";
    private const string RomEnvironmentVariable = "VICESHARP_ROM_PATH";

    private static readonly string[] RequiredVic20RomNames =
    [
        "basic-901486-01.bin",
        "kernal.901486-02.bin",
        "kernal.901486-06.bin",
        "kernal.901486-07.bin",
        "chargen-901460-02.bin",
        "chargen-901460-03.bin",
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Validates the checked-in oracle manifest, resolved native library, configured
    /// VIC-20 ROM allowlist, and every workload named by the manifest.
    /// </summary>
    public static void ValidateCurrent()
    {
        var repositoryRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(
            repositoryRoot,
            ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var romRoot = Environment.GetEnvironmentVariable(RomEnvironmentVariable) ?? string.Empty;
        var nativeLibraryPath = ViceNativeXvic.ResolvedPath ?? string.Empty;

        ValidateCore(manifestPath, nativeLibraryPath, romRoot, workloadProgram: null);
    }

    /// <summary>
    /// Validates an explicit oracle fixture and one manifest-named workload.
    /// </summary>
    public static void Validate(
        string manifestPath,
        string nativeLibraryPath,
        string romRoot,
        string workloadProgram)
        => ValidateCore(manifestPath, nativeLibraryPath, romRoot, workloadProgram);

    private static void ValidateCore(
        string manifestPath,
        string nativeLibraryPath,
        string romRoot,
        string? workloadProgram)
    {
        var manifest = ReadManifest(manifestPath);
        var manifestDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;

        var xvicPath = ResolveManifestPath(
            manifestDirectory,
            RequireValue(manifest.XvicPath, "xvic executable path"));
        RequireFile(xvicPath, "xvic executable");
        ValidateHash(
            xvicPath,
            RequireValue(manifest.ViceBuildHash, "xvic executable hash"),
            "xvic executable");

        RequireFile(nativeLibraryPath, "vice_xvic native library");
        ValidateHash(
            nativeLibraryPath,
            RequireValue(manifest.ViceXvicDllSha256, "vice_xvic native library hash"),
            "vice_xvic native library");

        var normalizedRomRoot = NormalizeRomRoot(romRoot);
        var vic20Root = Path.Combine(normalizedRomRoot, "VIC20");
        if (!Directory.Exists(vic20Root))
        {
            throw new InvalidOperationException(
                $"VIC-20 ROM directory is missing under {RomEnvironmentVariable}: '{vic20Root}'.");
        }

        ValidateRomAllowlist(manifest, vic20Root);
        ValidateWorkloads(manifest, vic20Root, workloadProgram);
    }

    private static OracleManifest ReadManifest(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
        {
            throw new InvalidOperationException(
                $"Pinned xvic oracle manifest is missing: '{manifestPath}'.");
        }

        try
        {
            using var stream = File.OpenRead(manifestPath);
            return JsonSerializer.Deserialize<OracleManifest>(stream, JsonOptions)
                ?? throw new InvalidOperationException(
                    $"Pinned xvic oracle manifest is empty: '{manifestPath}'.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Pinned xvic oracle manifest is invalid JSON: '{manifestPath}'.",
                exception);
        }
    }

    private static void ValidateRomAllowlist(OracleManifest manifest, string vic20Root)
    {
        var roms = manifest.RomHashes ?? [];
        var byName = new Dictionary<string, OracleRom>(StringComparer.OrdinalIgnoreCase);
        foreach (var rom in roms)
        {
            var name = RequireSimpleFileName(rom.Name, "ROM manifest entry");
            if (!byName.TryAdd(name, rom))
            {
                throw new InvalidOperationException(
                    $"Pinned xvic oracle manifest contains duplicate ROM entry '{name}'.");
            }
        }

        foreach (var requiredName in RequiredVic20RomNames)
        {
            if (!byName.ContainsKey(requiredName))
            {
                throw new InvalidOperationException(
                    $"Pinned xvic oracle manifest is missing required VIC-20 ROM '{requiredName}'.");
            }
        }

        foreach (var pair in byName)
        {
            var romPath = Path.Combine(vic20Root, pair.Key);
            RequireFile(romPath, $"VIC-20 ROM '{pair.Key}'");
            ValidateHash(
                romPath,
                RequireValue(pair.Value.Sha256, $"VIC-20 ROM hash for '{pair.Key}'"),
                $"VIC-20 ROM '{pair.Key}'");
        }
    }

    private static void ValidateWorkloads(
        OracleManifest manifest,
        string vic20Root,
        string? requestedWorkloadProgram)
    {
        var workloads = manifest.Workloads ?? [];
        var manifestPrograms = workloads
            .Select(workload => RequireSimpleFileName(workload.Program, "workload program"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (manifestPrograms.Length == 0)
        {
            throw new InvalidOperationException(
                "Pinned xvic oracle manifest does not name any workload programs.");
        }

        IReadOnlyList<string> programsToValidate;
        if (requestedWorkloadProgram is null)
        {
            programsToValidate = manifestPrograms;
        }
        else
        {
            var requestedProgram = RequireSimpleFileName(
                requestedWorkloadProgram,
                "requested workload program");
            if (!manifestPrograms.Contains(requestedProgram, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Pinned xvic oracle manifest does not name workload program '{requestedProgram}'.");
            }

            programsToValidate = [requestedProgram];
        }

        foreach (var program in programsToValidate)
        {
            var workloadPath = Path.Combine(vic20Root, program);
            RequireFile(workloadPath, $"manifest workload program '{program}'");
        }
    }

    private static string NormalizeRomRoot(string romRoot)
    {
        if (string.IsNullOrWhiteSpace(romRoot))
        {
            throw new InvalidOperationException(
                $"{RomEnvironmentVariable} must name the VICE data root for native xvic validation.");
        }

        try
        {
            var normalized = Path.GetFullPath(Environment.ExpandEnvironmentVariables(romRoot));
            if (!Directory.Exists(normalized))
            {
                throw new InvalidOperationException(
                    $"VIC-20 ROM root configured by {RomEnvironmentVariable} is missing: '{normalized}'.");
            }

            return normalized;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            throw new InvalidOperationException(
                $"{RomEnvironmentVariable} contains an invalid ROM root: '{romRoot}'.",
                exception);
        }
    }

    private static string ResolveManifestPath(string manifestDirectory, string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path);
        return Path.GetFullPath(
            Path.IsPathRooted(expanded)
                ? expanded
                : Path.Combine(manifestDirectory, expanded));
    }

    private static void ValidateHash(string path, string expectedHash, string label)
    {
        if (expectedHash.Length != 64 || expectedHash.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new InvalidOperationException(
                $"Pinned xvic oracle manifest contains an invalid SHA-256 hash for {label}.");
        }

        using var stream = File.OpenRead(path);
        var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Pinned xvic oracle hash mismatch for {label} '{path}': expected {expectedHash.ToLowerInvariant()}, actual {actualHash}.");
        }
    }

    private static void RequireFile(string path, string label)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Required {label} is missing: '{path}'.");
        }
    }

    private static string RequireValue(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Pinned xvic oracle manifest is missing {label}.");
        }

        return value;
    }

    private static string RequireSimpleFileName(string? value, string label)
    {
        var fileName = RequireValue(value, label);
        if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal)
            || fileName.Contains(Path.DirectorySeparatorChar)
            || fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new InvalidOperationException(
                $"Pinned xvic oracle manifest contains an invalid {label}: '{fileName}'.");
        }

        return fileName;
    }

    private static string FindRepositoryRoot()
    {
        foreach (var startingDirectory in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            for (var directory = new DirectoryInfo(startingDirectory);
                 directory is not null;
                 directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                    return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            $"Could not locate the ViceSharp repository root required for '{ManifestRelativePath}'.");
    }

    private sealed class OracleManifest
    {
        public string? ViceBuildHash { get; init; }
        public string? XvicPath { get; init; }
        public string? ViceXvicDllSha256 { get; init; }
        public OracleRom[]? RomHashes { get; init; }
        public OracleWorkload[]? Workloads { get; init; }
    }

    private sealed class OracleRom
    {
        public string? Name { get; init; }
        public string? Sha256 { get; init; }
    }

    private sealed class OracleWorkload
    {
        public string? Program { get; init; }
    }
}
