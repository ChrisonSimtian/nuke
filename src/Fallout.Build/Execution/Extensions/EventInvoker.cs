using System.Collections.Generic;

namespace Fallout.Common.Execution;

internal class EventInvoker : BuildExtensionAttributeBase,
    IOnBuildCreated,
    IOnBuildInitialized,
    IOnTargetRunning,
    IOnTargetSkipped,
    IOnTargetSucceeded,
    IOnTargetFailed,
    IOnBuildFinished
{
    public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
    {
        ((FalloutBuild)Build).OnBuildCreated();
    }

    public void OnBuildInitialized(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        IReadOnlyCollection<ExecutableTarget> executionPlan)
    {
        ((FalloutBuild)Build).OnBuildInitialized();
    }

    public void OnTargetRunning(ExecutableTarget target)
    {
        ((FalloutBuild)Build).OnTargetRunning(target.Name);
    }

    public void OnTargetSkipped(ExecutableTarget target)
    {
        ((FalloutBuild)Build).OnTargetSkipped(target.Name);
    }

    public void OnTargetSucceeded(ExecutableTarget target)
    {
        ((FalloutBuild)Build).OnTargetSucceeded(target.Name);
    }

    public void OnTargetFailed(ExecutableTarget target)
    {
        ((FalloutBuild)Build).OnTargetFailed(target.Name);
    }

    public void OnBuildFinished()
    {
        ((FalloutBuild)Build).OnBuildFinished();
    }
}
