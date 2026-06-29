using System;
using System.IO;
using System.Linq;
using Fallout.Common.CI.SpaceAutomation;

namespace Fallout.Common.Specs.CI;

public class TestSpaceAutomationAttribute : SpaceAutomationAttribute, ITestConfigurationGenerator
{
    public TestSpaceAutomationAttribute(string jobName, string image)
        : base(jobName, image)
    {
    }

    public StreamWriter Stream { get; set; }

    protected override StreamWriter CreateStream()
    {
        return Stream;
    }
}
