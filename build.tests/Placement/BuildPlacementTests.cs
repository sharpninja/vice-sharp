using System.Reflection;
using ViceSharp.Build.Testing;
using Xunit;

namespace ViceSharp.Build.Tests.Placement;

/// <summary>
/// PLAN-NUKETESTS-001 step 3, TR-BUILD-001 ac-12: the placement and filter rules proved in
/// <see cref="BuildPlacementMockGateTests"/> bound to the real repository and the production filter.
/// </summary>
public sealed class BuildPlacementTests
{
    /// <summary>
    /// TR-BUILD-001 ac-12. Use case: product test projects under tests/ are built by the solution.
    /// Acceptance: none of them compiles a file from build/.
    /// </summary>
    [Fact]
    public void ProductTestProjects_DoNotCompileBuildSources()
    {
        var violations = BuildPlacementRules.ProductTestProjects(RepoPaths.Root)
            .SelectMany(project => BuildPlacementRules.FindBuildSourceCompileItems(project, RepoPaths.Root)
                .Select(item => $"{Path.GetRelativePath(RepoPaths.Root, project)}: {item}"))
            .ToArray();

        Assert.Empty(violations);
    }

    /// <summary>
    /// TR-BUILD-001 ac-12. Use case: Test and CiTest run the whole solution. Acceptance: the build tests
    /// project is not in ViceSharp.slnx, so solution-wide runs never select build tests.
    /// </summary>
    [Fact]
    public void BuildTestsProject_IsNotInSolution()
    {
        Assert.True(System.IO.File.Exists(RepoPaths.File("ViceSharp.slnx")), "ViceSharp.slnx must exist at the repository root.");
        Assert.True(System.IO.File.Exists(RepoPaths.File(BuildPlacementRules.BuildTestsProject)), "The build tests project must exist beside build/.");

        Assert.False(BuildPlacementRules.SolutionContainsProject(RepoPaths.File("ViceSharp.slnx"), BuildPlacementRules.BuildTestsProject));
    }

    /// <summary>
    /// TR-BUILD-001 ac-12. Use case: the BuildTest target selects tests by host.
    /// Acceptance: the production filter satisfies the host contract.
    /// </summary>
    [Fact]
    public void BuildTestFilter_ConformsToHostContract()
    {
        Assert.Empty(BuildTestFilterContract.Violations(BuildTestFilter.For));
    }

    /// <summary>
    /// TR-BUILD-001 ac-10, ac-11, ac-12; TR-PLAT-001 ac-8. Use case: on non-Linux hosts the Host!=Linux
    /// filter excludes cases. Acceptance: the Host=Linux trait marks exactly the user-local deployment
    /// contract (one class, 6 methods, 13 cases), so the excluded set is known and nothing else is hidden.
    /// </summary>
    [Fact]
    public void LinuxHostTrait_MarksOnlyTheDeploymentContract()
    {
        var assembly = typeof(BuildPlacementTests).Assembly;
        var markedTypes = assembly.GetTypes().Where(HasLinuxHostTrait).Select(t => t.FullName).ToArray();
        var markedMethods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(HasLinuxHostTrait)
            .ToArray();

        Assert.Equal(new[] { "ViceSharp.Build.Tests.Packaging.UserLocalDeploymentTests" }, markedTypes);
        Assert.Empty(markedMethods);

        var contract = assembly.GetType("ViceSharp.Build.Tests.Packaging.UserLocalDeploymentTests", throwOnError: true)!;
        var testMethods = contract.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<FactAttribute>(inherit: true).Any())
            .ToArray();
        int cases = testMethods.Sum(m => m.GetCustomAttribute<TheoryAttribute>() is null
            ? 1
            : m.GetCustomAttributes<InlineDataAttribute>().Count());

        Assert.Equal(6, testMethods.Length);
        Assert.Equal(13, cases);
    }

    private static bool HasLinuxHostTrait(MemberInfo member) => member.GetCustomAttributesData()
        .Any(a => a.AttributeType == typeof(TraitAttribute)
            && a.ConstructorArguments.Count == 2
            && Equals(a.ConstructorArguments[0].Value, "Host")
            && Equals(a.ConstructorArguments[1].Value, "Linux"));
}
