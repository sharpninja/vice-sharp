using System.Collections.Generic;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.AzurePipelines.Configuration;
using Nuke.Common.Execution;

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

    protected override AzurePipelinesStage GetStage(
        AzurePipelinesImage image,
        IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var stage = base.GetStage(image, relevantTargets);
        stage.Name = "default_pool";
        stage.DisplayName = "Default agent pool";
        stage.Image = null;
        return stage;
    }
}
