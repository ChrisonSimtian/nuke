using System.Collections.Generic;
using System.Reflection;
using Fallout.Common.Execution;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.CI;

public class BuildServerConfigurationGenerationAttributeBase : BuildExtensionAttributeBase
{
    protected static IEnumerable<IConfigurationGenerator> GetGenerators(IFalloutBuild build)
    {
        return build.GetType().GetCustomAttributes<ConfigurationAttributeBase>()
            .ForEachLazy(x => x.Build = build);
    }
}
