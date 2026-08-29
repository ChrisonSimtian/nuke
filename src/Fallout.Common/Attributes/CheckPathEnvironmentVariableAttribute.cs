using System.Collections.Generic;
using Fallout.Common.Tooling;

namespace Fallout.Common.Execution;

public class CheckPathEnvironmentVariableAttribute : BuildExtensionAttributeBase, IOnBuildInitialized
{
    public void OnBuildInitialized(
        IReadOnlyCollection<ExecutableTarget> executableTargets,
        IReadOnlyCollection<ExecutableTarget> executionPlan)
    {
        ProcessTasks.CheckPathEnvironmentVariable();
    }
}
