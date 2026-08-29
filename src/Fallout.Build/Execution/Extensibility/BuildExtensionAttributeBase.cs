using System;
using System.Collections.Generic;

namespace Fallout.Common.Execution;

public interface IBuildExtension
{
    float Priority { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public abstract class BuildExtensionAttributeBase : Attribute, IBuildExtension
{
    public IFalloutBuild Build { get; internal set; }

    public virtual float Priority { get; set; }
}

public interface IOnBuildCreated : IBuildExtension
{
    void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets);
}

public interface IOnBuildInitialized : IBuildExtension
{
    void OnBuildInitialized(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        IReadOnlyCollection<ExecutableTarget> executionPlan);
}

public interface IOnTargetSummaryUpdated : IBuildExtension
{
    void OnTargetSummaryUpdated(IFalloutBuild build, ExecutableTarget target);
}

public interface IOnTargetSkipped : IBuildExtension
{
    void OnTargetSkipped(ExecutableTarget target);
}

public interface IOnTargetRunning : IBuildExtension
{
    void OnTargetRunning(ExecutableTarget target);
}

public interface IOnTargetSucceeded : IBuildExtension
{
    void OnTargetSucceeded(ExecutableTarget target);
}

public interface IOnTargetFailed : IBuildExtension
{
    void OnTargetFailed(ExecutableTarget target);
}

public interface IOnBuildFinished : IBuildExtension
{
    void OnBuildFinished();
}
