using System.Linq;
using Fallout.Common.Execution;

namespace Fallout.Common.CI;

internal class SerializeBuildServerStateAttribute : BuildServerConfigurationGenerationAttributeBase, IOnBuildFinished
{
    public void OnBuildFinished()
    {
        GetGenerators(Build)
            // TODO: bool IsRunning
            .FirstOrDefault(x => x.HostType == Build.Host.GetType())
            ?.SerializeState();
    }
}
