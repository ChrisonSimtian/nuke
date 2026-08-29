using System;
using System.Collections.Generic;
using Fallout.Common.Utilities.Collections;

namespace Fallout.Common.Execution;

public class UnsetVisualStudioEnvironmentVariablesAttribute : BuildExtensionAttributeBase, IOnBuildCreated
{
    public void OnBuildCreated(IReadOnlyCollection<ExecutableTarget> executableTargets)
    {
        new[]
        {
            "MSBuildLoadMicrosoftTargetsReadOnly",
            "VisualStudioDir",
            "VisualStudioEdition",
            "VisualStudioVersion",
            "VSAPPIDDIR",
            "VSAPPIDNAME",
            "VSLANG",
            "VSLOGGER_UNIQUEID",
            "VSSKUEDITION"
        }.ForEach(x => Environment.SetEnvironmentVariable(x, value: null));
    }
}
