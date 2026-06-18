using System;
using System.Linq;

namespace Fallout.Common.Tools.MSBuild;

public static partial class MSBuildSettingsExtensions
{
    /// <summary><em>Sets <see cref="MsBuildSettings.TargetPath" />.</em></summary>
    public static MsBuildSettings SetSolutionFile(this MsBuildSettings toolSettings, string solutionFile)
    {
        return toolSettings.SetTargetPath(solutionFile);
    }

    /// <summary><em>Sets <see cref="MsBuildSettings.TargetPath" />.</em></summary>
    public static MsBuildSettings SetProjectFile(this MsBuildSettings toolSettings, string projectFile)
    {
        return toolSettings.SetTargetPath(projectFile);
    }
}
