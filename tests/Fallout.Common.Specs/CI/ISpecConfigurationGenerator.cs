using System.IO;
using Fallout.Common.CI;

namespace Fallout.Common.Specs.CI;

public interface ITestConfigurationGenerator : IConfigurationGenerator
{
    StreamWriter Stream { set; }
}
