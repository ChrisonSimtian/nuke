using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Fallout.Common.Execution;
using Fallout.Common.Utilities;
using Fallout.Common.Utilities.Collections;
using Serilog;

namespace Fallout.Common;

[EventInvoker(Priority = float.MinValue)]
public abstract partial class FalloutBuild
{
    private IReadOnlyCollection<IBuildExtension> BuildExtensions { get; }

    IReadOnlyCollection<IBuildExtension> IFalloutBuild.BuildExtensions => BuildExtensions;

    protected FalloutBuild()
    {
        BuildExtensions ??= GetType()
            .GetCustomAttributes<BuildExtensionAttributeBase>()
            .ForEachLazy(x => x.Build = this)
            .Cast<IBuildExtension>()
            .OrderByDescending(x => x.Priority).ToList();
    }

    internal void ExecuteExtension<TExtension>(Expression<Action<TExtension>> action)
        where TExtension : IBuildExtension
    {
        BuildExtensions
            .OfType<TExtension>()
            .ForEachLazy(x => Log.Verbose("{Type}.{Method} ({Priority})", x.GetType().Name.TrimEnd(nameof(Attribute)),
                action.GetMemberInfo().Name, x.Priority))
            .ForEach(action.Compile());
    }

    /// <summary>
    /// Method that is invoked after the instance of the build was created.
    /// </summary>
    protected internal virtual void OnBuildCreated()
    {
    }

    /// <summary>
    /// Method that is invoked after build instance is initialized. I.e., value injection and requirement validation has finished.
    /// </summary>
    protected internal virtual void OnBuildInitialized()
    {
    }

    /// <summary>
    /// Method that is invoked after the build has finished (succeeded or failed).
    /// </summary>
    protected internal virtual void OnBuildFinished()
    {
    }

    /// <summary>
    /// Method that is invoked before a target is about to start.
    /// </summary>
    protected internal virtual void OnTargetRunning(string target)
    {
    }

    /// <summary>
    /// Method that is invoked when a target is skipped.
    /// </summary>
    protected internal virtual void OnTargetSkipped(string target)
    {
    }

    /// <summary>
    /// Method that is invoked when a target has been executed successfully.
    /// </summary>
    protected internal virtual void OnTargetSucceeded(string target)
    {
    }

    /// <summary>
    /// Method that is invoked when a target has failed.
    /// </summary>
    protected internal virtual void OnTargetFailed(string target)
    {
    }
}
