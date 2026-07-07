using System.Collections.Generic;
using System.Linq;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.AzurePipelines.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

/// <summary>
/// Nuke's <see cref="AzurePipelinesAttribute"/> can only emit Microsoft-hosted
/// vmImage pools (the ctor takes <see cref="AzurePipelinesImage"/> and the
/// stage writer emits a pool block only when an image is set). This repo's
/// pipelines must dispatch to the self-hosted "Default" agent pool (the
/// runners carry the native toolchain and the NUGET_API_KEY environment), so
/// this subclass nulls the stage image - no pool block is written and Azure
/// DevOps routes the stage to the project's Default pool.
/// </summary>
sealed class DefaultPoolAzurePipelinesAttribute : AzurePipelinesAttribute
{
    public DefaultPoolAzurePipelinesAttribute(string suffix, AzurePipelinesImage image)
        : base(suffix, image)
    {
    }

    /// <summary>
    /// The Default-pool agents are Linux; Nuke's default resolves to
    /// build.cmd (a Windows batch file here, not the polyglot), which bash
    /// cannot execute. Point the generated steps at build.sh instead
    /// (executable bit set in the git index).
    /// </summary>
    protected override string BuildCmdPath => "build.sh";

    protected override AzurePipelinesStage GetStage(
        AzurePipelinesImage image,
        IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var stage = base.GetStage(image, relevantTargets);
        // Emit an explicit named pool: with no pool block, Azure falls back to
        // the pipeline DEFINITION's default queue, which az pipelines create
        // pointed at a nonexistent hosted pool ("No image label found to route
        // agent pool Hosted Ubuntu 1604", runs 1021/1024).
        return new NamedPoolStage
        {
            Name = "default_pool",
            DisplayName = "Default agent pool",
            Image = null,
            Dependencies = stage.Dependencies,
            Jobs = stage.Jobs,
            PoolName = "Default",
        };
    }

    /// <summary>
    /// AzurePipelinesStage can only write a vmImage pool; this variant writes
    /// a named (self-hosted) pool block instead.
    /// </summary>
    private sealed class NamedPoolStage : AzurePipelinesStage
    {
        public string PoolName { get; set; } = "Default";

        public override void Write(CustomFileWriter writer)
        {
            using (writer.WriteBlock($"- stage: {Name}"))
            {
                writer.WriteLine($"displayName: {DisplayName.SingleQuote()}");
                writer.WriteLine($"dependsOn: [ {Dependencies.Select(x => x.Name).JoinCommaSpace()} ]");

                using (writer.WriteBlock("pool:"))
                {
                    writer.WriteLine($"name: {PoolName.SingleQuote()}");
                }

                using (writer.WriteBlock("jobs:"))
                {
                    Jobs.ForEach(x => x.Write(writer));
                }
            }
        }
    }
}
