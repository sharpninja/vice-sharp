namespace ViceSharp.TestHarness.HostShells;

using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

/// <summary>
/// PLATFORM-CROSS-001 Phase 1 closeout: assert that mobile host shell csproj
/// files exist and declare the expected target frameworks. Uses raw file
/// reads instead of reflecting into the assemblies, because the Android /
/// iOS targets may not load on the test runtime (which itself runs on plain
/// net10.0 on the Windows host).
/// </summary>
public sealed class MobileHostShellTests
{
    // Matches a <TargetFramework ...>...</TargetFramework> element regardless
    // of attributes (e.g. a Condition= for the workload-fallback toggle).
    private static readonly Regex TargetFrameworkElementPattern =
        new(@"<TargetFramework(\s[^>]*)?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex OutputTypeElementPattern =
        new(@"<OutputType(\s[^>]*)?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string RepoRoot
    {
        get
        {
            // Walk up from the test binary location until we find the slnx.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ViceSharp.slnx")))
            {
                dir = dir.Parent;
            }
            dir.Should().NotBeNull("the test harness must be able to locate ViceSharp.slnx by walking up from AppContext.BaseDirectory");
            return dir!.FullName;
        }
    }

    [Fact]
    public void AndroidHostShell_Csproj_DeclaresNet10Target()
    {
        var csprojPath = Path.Combine(RepoRoot, "src", "ViceSharp.Host.Android", "ViceSharp.Host.Android.csproj");
        File.Exists(csprojPath).Should().BeTrue($"the Android host shell csproj must exist at {csprojPath}");

        var contents = File.ReadAllText(csprojPath);
        TargetFrameworkElementPattern.IsMatch(contents).Should().BeTrue(
            "the Android csproj must declare a TargetFramework element (with or without a Condition attribute)");
        contents.Should().Contain("net10.0",
            "the Android csproj target framework must be net10.0 or a net10.0 RID such as net10.0-android");
        OutputTypeElementPattern.IsMatch(contents).Should().BeTrue(
            "the Android host shell must declare an OutputType (Exe for desktop fallback, or the platform default)");
    }

    [Fact]
    public void IosHostShell_Csproj_DeclaresNet10Target()
    {
        var csprojPath = Path.Combine(RepoRoot, "src", "ViceSharp.Host.iOS", "ViceSharp.Host.iOS.csproj");
        File.Exists(csprojPath).Should().BeTrue($"the iOS host shell csproj must exist at {csprojPath}");

        var contents = File.ReadAllText(csprojPath);
        TargetFrameworkElementPattern.IsMatch(contents).Should().BeTrue(
            "the iOS csproj must declare a TargetFramework element (with or without a Condition attribute)");
        contents.Should().Contain("net10.0",
            "the iOS csproj target framework must be net10.0 or a net10.0 RID such as net10.0-ios");
    }
}
