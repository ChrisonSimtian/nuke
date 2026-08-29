using System.Collections.Generic;
using Fallout.Common.Execution;

namespace Fallout.Common.ValueInjection;

internal class InjectParameterValuesAttribute : BuildExtensionAttributeBase, IOnBuildCreated
{
    public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
    {
        ValueInjectionUtility.InjectValues(Build, (_, attribute) => attribute.GetType() == typeof(ParameterAttribute));
    }
}
