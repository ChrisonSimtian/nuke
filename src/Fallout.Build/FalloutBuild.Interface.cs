using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fallout.Common.Execution;
using Fallout.Common.IO;
using Fallout.Common.ValueInjection;

namespace Fallout.Common;

public abstract partial class FalloutBuild
{
    IReadOnlyCollection<ExecutableTarget> IFalloutBuild.ExecutableTargets => ExecutableTargets;

    bool IFalloutBuild.IsInterceptorExecution => IsInterceptorExecution;

    string[] IFalloutBuild.LoadedLocalProfiles => LoadedLocalProfiles;

    bool IFalloutBuild.IsOutputEnabled(DefaultOutput output) => IsOutputEnabled(output);

    AbsolutePath IFalloutBuild.RootDirectory => RootDirectory;

    AbsolutePath IFalloutBuild.TemporaryDirectory => TemporaryDirectory;

    AbsolutePath IFalloutBuild.BuildAssemblyFile => BuildAssemblyFile;

    AbsolutePath IFalloutBuild.BuildAssemblyDirectory => BuildAssemblyDirectory;

    AbsolutePath IFalloutBuild.BuildProjectDirectory => BuildProjectDirectory;

    AbsolutePath IFalloutBuild.BuildProjectFile => BuildProjectFile;

    Verbosity IFalloutBuild.Verbosity => Verbosity;

    Host IFalloutBuild.Host => Host;

    bool IFalloutBuild.Plan => Plan;

    bool IFalloutBuild.Help => Help;

    bool IFalloutBuild.NoLogo => NoLogo;

    bool IFalloutBuild.IsLocalBuild => IsLocalBuild;

    bool IFalloutBuild.IsServerBuild => IsServerBuild;

    bool IFalloutBuild.Continue => Continue;

    T IFalloutBuild.TryGetValue<T>(Expression<Func<T>> parameterExpression)
    {
        return ValueInjectionUtility.TryGetValue(parameterExpression);
    }

    T IFalloutBuild.TryGetValue<T>(Expression<Func<object>> parameterExpression)
    {
        return ValueInjectionUtility.TryGetValue<T>(parameterExpression);
    }
}
