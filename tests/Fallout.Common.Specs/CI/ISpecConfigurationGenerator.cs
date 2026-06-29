using System;
using System.IO;
using System.Linq;
using Fallout.Common.CI;

namespace Fallout.Common.Specs.CI;

public interface ITestConfigurationGenerator : IConfigurationGenerator
{
    StreamWriter Stream { set; }
}
