using Xunit;

namespace ViceSharp.Build.Tests.Placement;

/// <summary>
/// PLAN-NUKETESTS-001 step 2 mock gate for TR-BUILD-001 ac-12: each placement and filter rule runs
/// against fixture inputs and scripted fakes. Correct inputs pass; every wrong input is asserted to be
/// rejected (negative controls inside a passing suite).
/// </summary>
public sealed class BuildPlacementMockGateTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("vicesharp-build-placement-").FullName;

    /// <summary>
    /// TR-BUILD-001 ac-12. Use case: a product test project compiles only its own sources.
    /// Acceptance: the rule reports no build/ compile items.
    /// </summary>
    [Fact]
    public void CompileRule_AcceptsProjectWithoutBuildLinks()
    {
        string project = WriteProject("tests/Clean/Clean.csproj", "Local.cs", "Sub\\Other.cs");

        Assert.Empty(BuildPlacementRules.FindBuildSourceCompileItems(project, _root));
    }

    /// <summary>
    /// TR-BUILD-001 ac-12, negative control. Use case: a product test project links a file from build/
    /// with a Windows-style relative path. Acceptance: the rule reports exactly that item.
    /// </summary>
    [Fact]
    public void CompileRule_RejectsBackslashLinkIntoBuild()
    {
        string project = WriteProject("tests/Dirty/Dirty.csproj", "Local.cs", "..\\..\\build\\UserLocalDeployment.cs");

        Assert.Equal(new[] { "..\\..\\build\\UserLocalDeployment.cs" }, BuildPlacementRules.FindBuildSourceCompileItems(project, _root));
    }

    /// <summary>
    /// TR-BUILD-001 ac-12, negative control. Use case: the same link written with forward slashes.
    /// Acceptance: the rule reports it.
    /// </summary>
    [Fact]
    public void CompileRule_RejectsForwardSlashLinkIntoBuild()
    {
        string project = WriteProject("tests/Dirty2/Dirty2.csproj", "../../build/CompletionTestManifest.cs");

        Assert.Single(BuildPlacementRules.FindBuildSourceCompileItems(project, _root));
    }

    /// <summary>
    /// TR-BUILD-001 ac-12, boundary. Use case: a sibling folder whose name only starts with "build".
    /// Acceptance: the rule does not report it, so the prefix check cannot over-match.
    /// </summary>
    [Fact]
    public void CompileRule_IgnoresSiblingFolderWithBuildPrefix()
    {
        string project = WriteProject("tests/Near/Near.csproj", "..\\..\\buildtools\\Helper.cs", "..\\..\\build.tests\\Shared.cs");

        Assert.Empty(BuildPlacementRules.FindBuildSourceCompileItems(project, _root));
    }

    /// <summary>
    /// TR-BUILD-001 ac-12. Use case: the solution does not list the build tests project.
    /// Acceptance: the rule reports it absent.
    /// </summary>
    [Fact]
    public void SolutionRule_AcceptsSolutionWithoutBuildTestsProject()
    {
        string solution = WriteSolution("tests/ViceSharp.Library.Tests/ViceSharp.Library.Tests.csproj");

        Assert.False(BuildPlacementRules.SolutionContainsProject(solution, BuildPlacementRules.BuildTestsProject));
    }

    /// <summary>
    /// TR-BUILD-001 ac-12, negative control. Use case: someone adds the build tests project to the
    /// solution (either separator style). Acceptance: the rule detects it.
    /// </summary>
    [Theory]
    [InlineData("build.tests/ViceSharp.Build.Tests.csproj")]
    [InlineData("build.tests\\ViceSharp.Build.Tests.csproj")]
    [InlineData("./build.tests/ViceSharp.Build.Tests.csproj")]
    public void SolutionRule_RejectsSolutionListingBuildTestsProject(string listed)
    {
        string solution = WriteSolution("tests/ViceSharp.Library.Tests/ViceSharp.Library.Tests.csproj", listed);

        Assert.True(BuildPlacementRules.SolutionContainsProject(solution, BuildPlacementRules.BuildTestsProject));
    }

    /// <summary>
    /// TR-BUILD-001 ac-12. Use case: project discovery under tests/ skips build output folders.
    /// Acceptance: only the real project files are returned.
    /// </summary>
    [Fact]
    public void ProjectDiscovery_SkipsBinAndObj()
    {
        WriteProject("tests/A/A.csproj", "A.cs");
        WriteProject("tests/A/bin/Release/A.csproj", "A.cs");
        WriteProject("tests/A/obj/A.csproj", "A.cs");

        var found = BuildPlacementRules.ProductTestProjects(_root).Select(p => Path.GetRelativePath(_root, p).Replace('\\', '/')).ToArray();

        Assert.Equal(new[] { "tests/A/A.csproj" }, found);
    }

    /// <summary>
    /// TR-BUILD-001 ac-12. Use case: a correct host filter (scripted fake). Acceptance: no violations.
    /// </summary>
    [Fact]
    public void FilterContract_AcceptsCorrectFake()
    {
        Assert.Empty(BuildTestFilterContract.Violations(isLinux => isLinux ? null : "Host!=Linux"));
    }

    /// <summary>
    /// TR-BUILD-001 ac-12, negative controls. Use case: wrong host filters (scripted fakes): no filter on
    /// Windows, the exclusion applied on Linux too, an inverted selection, and a broader filter.
    /// Acceptance: each fake is rejected with at least one violation.
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("Host!=Linux", "Host!=Linux")]
    [InlineData(null, "Host=Linux")]
    [InlineData(null, "Host!=Linux&Category!=Slow")]
    [InlineData("", "")]
    public void FilterContract_RejectsWrongFakes(string? onLinux, string? onOther)
    {
        Assert.NotEmpty(BuildTestFilterContract.Violations(isLinux => isLinux ? onLinux : onOther));
    }

    /// <summary>Removes the fixture tree.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private string WriteProject(string relative, params string[] compileIncludes)
    {
        string path = Path.Combine(_root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var items = string.Concat(compileIncludes.Select(i => $"<Compile Include=\"{i}\" />"));
        System.IO.File.WriteAllText(path, $"<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>{items}</ItemGroup></Project>");
        return path;
    }

    private string WriteSolution(params string[] projectPaths)
    {
        string path = Path.Combine(_root, "Fixture.slnx");
        var projects = string.Concat(projectPaths.Select(p => $"<Project Path=\"{p}\" />"));
        System.IO.File.WriteAllText(path, $"<Solution><Folder Name=\"/tests/\">{projects}</Folder></Solution>");
        return path;
    }
}
