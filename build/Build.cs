using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;

sealed partial class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly string Configuration = IsLocalBuild ? "Debug" : "Release";

    [Solution]
    readonly Solution Solution = null!;

    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
            DotNetClean(s => s
                .SetProject(Solution));
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true));
        });

    Target GitCommit => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Git("add -A");
            Git("commit -m \"wip: $(Get-Date -Format 'yyyy-MM-dd HH:mm')\"");
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)
                .SetNoBuild(true)
                .SetFilter("Category!=Determinism"));
        });

    Target DeterminismTest => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetNoRestore(true)
                .SetNoBuild(true)
                .SetFilter("Category=Determinism"));
        });

    Target PublishAot => _ => _
        .DependsOn(Clean, Restore)
        .Executes(() =>
        {
            Serilog.Log.Information("PublishAot target — no publishable projects yet");
        });

    Target RomFetch => _ => _
        .Executes(() =>
        {
            Serilog.Log.Information("RomFetch target — tool not yet implemented");
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            Serilog.Log.Information("Pack target — reserved for future NuGet packaging");
        });

    Target RunConsole => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetRun(s => s
                .SetProjectFile(RootDirectory / "src" / "ViceSharp.Console" / "ViceSharp.Console.csproj")
                .SetConfiguration(Configuration)
                .SetNoBuild(true));
        });

    Target RunAvalonia => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetRun(s => s
                .SetProjectFile(RootDirectory / "src" / "ViceSharp.Avalonia" / "ViceSharp.Avalonia.csproj")
                .SetConfiguration(Configuration)
                .SetNoBuild(true));
        });
}
