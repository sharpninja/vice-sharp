using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    [Parameter("Force a full rebuild (skip incremental caches)")]
    readonly bool ForceRebuild;

    Target CiGitHub => _ => _
        .Description("Full GitHub Actions CI pipeline")
        .DependsOn(Clean, Restore, Compile, Test, DeterminismTest)
        .Executes(() =>
        {
            Serilog.Log.Information("GitHub Actions CI pipeline complete");
        });

    Target CiAzure => _ => _
        .Description("Full Azure DevOps CI pipeline")
        .DependsOn(Clean, Restore, Compile, Test, DeterminismTest)
        .Executes(() =>
        {
            Serilog.Log.Information("Azure DevOps CI pipeline complete");
        });

    Target Commit => _ => _
        .Description("Stage changes and create a git commit")
        .Requires(() => CommitMessage)
        .Executes(() =>
        {
            Git($"add -A");
            Git($"commit -m \"{CommitMessage}\"");
        });

    [Parameter("Commit message for the Commit target")]
    readonly string? CommitMessage;

    Target SyncAzure => _ => _
        .Description("Push current branch to origin (Azure DevOps)")
        .Executes(() =>
        {
            Git($"push origin");
        });

    Target SyncGithub => _ => _
        .Description("Push current branch to github remote (downstream mirror)")
        .Executes(() =>
        {
            Git($"push github");
        });

    Target RebuildAzure => _ => _
        .Description("Trigger a full rebuild on Azure DevOps pipeline (no commit)")
        .Executes(() =>
        {
            Serilog.Log.Information("RebuildAzure — trigger not yet configured");
        });

    Target RebuildGithub => _ => _
        .Description("Trigger a full rebuild on GitHub Actions (no commit)")
        .Executes(() =>
        {
            Serilog.Log.Information("RebuildGithub — trigger not yet configured");
        });

    static void Git(string arguments)
    {
        Nuke.Common.Tools.Git.GitTasks.Git(arguments);
    }
}
