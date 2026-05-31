namespace ViceSharp.TestHarness.HostShells;

using System;
using System.IO;
using System.Reflection;
using Xunit;

/// <summary>
/// FR: FR-PLATFORM-CROSS-001, TR: PLATFORM-CROSS-001.
/// Use case: ViceSharp ships a macOS host shell project that reuses the
/// existing ViceSharp.Avalonia App and ViceSharp.Host composition rather
/// than re-implementing UI. The shell project must be discoverable by
/// reflection (so it actually compiles into a real assembly with a Program
/// type) and its csproj must declare an executable on a net10.0 TFM.
///
/// Acceptance:
/// 1. The type ViceSharp.Host.MacOS.Program resolves via reflection.
/// 2. src/ViceSharp.Host.MacOS/ViceSharp.Host.MacOS.csproj declares a
///    TargetFramework (or TargetFrameworks) value containing the literal
///    "net10.0" and an OutputType of Exe.
/// </summary>
public sealed class MacOSHostShellTests
{
    [Fact]
    public void MacOSHostShell_ProgramType_IsReachableViaReflection()
    {
        var assembly = LoadMacOSHostAssembly();

        var programType = assembly.GetType("ViceSharp.Host.MacOS.Program", throwOnError: false);

        Assert.NotNull(programType);
    }

    [Fact]
    public void MacOSHostShell_Csproj_DeclaresNet10ExeTarget()
    {
        var csprojPath = Path.Combine(
            RepoRoot,
            "src",
            "ViceSharp.Host.MacOS",
            "ViceSharp.Host.MacOS.csproj");

        Assert.True(File.Exists(csprojPath), $"Expected csproj at {csprojPath}.");

        var contents = File.ReadAllText(csprojPath);

        // Either <TargetFramework> or <TargetFrameworks> must contain "net10.0".
        var hasNet10 =
            contents.Contains("<TargetFramework>", StringComparison.Ordinal) &&
            contents.Contains("net10.0", StringComparison.Ordinal);
        var hasNet10Multi =
            contents.Contains("<TargetFrameworks>", StringComparison.Ordinal) &&
            contents.Contains("net10.0", StringComparison.Ordinal);

        Assert.True(
            hasNet10 || hasNet10Multi,
            "Csproj must declare a TargetFramework(s) containing 'net10.0'.");
        Assert.Contains("<OutputType>Exe</OutputType>", contents);
    }

    private static Assembly LoadMacOSHostAssembly()
    {
        const string assemblyName = "ViceSharp.Host.MacOS";

        // Already loaded?
        foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(loaded.GetName().Name, assemblyName, StringComparison.Ordinal))
                return loaded;
        }

        var baseDir = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDir, $"{assemblyName}.dll");

        if (File.Exists(candidate))
            return Assembly.LoadFrom(candidate);

        // Fall back to assembly resolution by simple name; project reference from
        // the TestHarness ensures the assembly is built alongside the harness.
        try
        {
            return Assembly.Load(new AssemblyName(assemblyName));
        }
        catch (Exception ex)
        {
            throw new FileNotFoundException(
                $"Could not locate {assemblyName}.dll near {baseDir}. Ensure the project is referenced.",
                ex);
        }
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ViceSharp.slnx")))
                directory = directory.Parent;

            if (directory is null)
                throw new InvalidOperationException("Could not locate repository root.");

            return directory.FullName;
        }
    }
}
