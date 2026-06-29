using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Fallout.Common.Execution;

namespace Fallout.Common.Specs;

public static class ExecutionTestsInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Environment.SetEnvironmentVariable(Telemetry.OptOutEnvironmentKey, "true");
    }
}
